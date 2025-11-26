#if UNITY_EDITOR
using Framework.AI.Components;
using Framework.AI.Runtime;
using Framework.Contracts.Intents;
using Framework.Core.Components;
using Framework.Resources.Components;
using Framework.Spells.Factory;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using ContractsAIAgentTarget = Framework.Contracts.AI.AIAgentTarget;

namespace Framework.UnityAuthoring.Editor
{
    public struct SkirmishAgent : IComponentData
    {
        public FixedString64Bytes SpellId;
        public float CooldownSeconds;
        public float CooldownTimer;
        public float VisionRange;
        public float AttackRange;
        public float MoveSpeed;
        public byte TeamId;
        public Entity LastTarget;
    }

    public struct SkirmishTeamColor : IComponentData
    {
        public float4 Value;
    }

    [UpdateInGroup(typeof(Framework.Core.Base.RuntimeSystemGroup))]
    public partial struct SkirmishAutoCombatSystem : ISystem
    {
        private ComponentLookup<Health> _healthLookup;
        private EntityQuery _agents;

        public void OnCreate(ref SystemState state)
        {
            _healthLookup = state.GetComponentLookup<Health>(true);
            _agents = state.GetEntityQuery(
                ComponentType.ReadWrite<SkirmishAgent>(),
                ComponentType.ReadWrite<Position>(),
                ComponentType.ReadOnly<Health>());
            state.RequireForUpdate(_agents);
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            if (_agents.IsEmptyIgnoreFilter)
                return;

            var dt = (float)SystemAPI.Time.DeltaTime;
            var em = state.EntityManager;
            _healthLookup.Update(ref state);

            using var entities = _agents.ToEntityArray(Allocator.Temp);
            using var positions = _agents.ToComponentDataArray<Position>(Allocator.Temp);
            using var agents = _agents.ToComponentDataArray<SkirmishAgent>(Allocator.Temp);

            for (int i = 0; i < agents.Length; i++)
            {
                var agent = agents[i];
                var entity = entities[i];
                var pos = positions[i];

                if (!_healthLookup.HasComponent(entity))
                    continue;

                var selfHealth = _healthLookup[entity];
                if (selfHealth.Current <= 0)
                {
                    agent.CooldownTimer = agent.CooldownSeconds;
                    agent.LastTarget = Entity.Null;
                    em.SetComponentData(entity, agent);
                    continue;
                }

                agent.CooldownTimer = math.max(0f, agent.CooldownTimer - dt);

                Entity bestTarget = Entity.Null;
                float2 bestTargetPos = float2.zero;
                float bestDistSq = float.MaxValue;

                for (int j = 0; j < agents.Length; j++)
                {
                    if (i == j)
                        continue;

                    var candidate = agents[j];
                    if (candidate.TeamId == agent.TeamId)
                        continue;

                    var candidateEntity = entities[j];
                    if (!_healthLookup.HasComponent(candidateEntity))
                        continue;

                    var candidateHealth = _healthLookup[candidateEntity];
                    if (candidateHealth.Current <= 0)
                        continue;

                    float distSq = math.lengthsq(positions[j].Value - pos.Value);
                    if (distSq > agent.VisionRange * agent.VisionRange)
                        continue;

                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestTarget = candidateEntity;
                        bestTargetPos = positions[j].Value;
                    }
                }

                if (bestTarget != Entity.Null)
                {
                    float dist = math.sqrt(bestDistSq);
                    UpdateTargetComponent(ref em, entity, bestTarget, bestDistSq);

                    bool isMoving = false;
                    if (agent.MoveSpeed > 0f)
                    {
                        float desired = math.max(agent.AttackRange * 0.9f, 0.1f);
                        if (dist > desired + 0.05f)
                        {
                            var dir = math.normalizesafe(bestTargetPos - pos.Value);
                            var step = math.min(agent.MoveSpeed * dt, dist - desired);
                            pos.Value += dir * step;
                            em.SetComponentData(entity, pos);
                            WriteMoveIntent(ref em, entity, pos.Value, agent.MoveSpeed, AIMoveMode.Chase);
                            isMoving = true;
                        }
                    }
                    if (!isMoving)
                        WriteMoveIntent(ref em, entity, pos.Value, 0f, AIMoveMode.Idle);

                    if (dist <= agent.AttackRange + 0.1f && agent.CooldownTimer <= 0f)
                    {
                        SpellPipelineFactory.Cast(ref em, entity, bestTarget, agent.SpellId, 0);
                        agent.CooldownTimer = math.max(0.1f, agent.CooldownSeconds);
                        WriteCastIntent(ref em, entity, agent.SpellId, bestTarget, true);
                    }
                    else
                    {
                        WriteCastIntent(ref em, entity, agent.SpellId, bestTarget, false);
                    }

                    agent.LastTarget = bestTarget;
                }
                else
                {
                    agent.LastTarget = Entity.Null;
                    UpdateTargetComponent(ref em, entity, Entity.Null, 0f);
                    WriteMoveIntent(ref em, entity, pos.Value, 0f, AIMoveMode.Idle);
                    WriteCastIntent(ref em, entity, agent.SpellId, Entity.Null, false);
                }

                em.SetComponentData(entity, agent);
            }
        }

        private static void UpdateTargetComponent(ref EntityManager em, in Entity entity, in Entity target, float distSq)
        {
            if (!em.HasComponent<ContractsAIAgentTarget>(entity))
                return;
            var data = em.GetComponentData<ContractsAIAgentTarget>(entity);
            data.Value = target;
            data.Visibility = target == Entity.Null ? (byte)0 : (byte)1;
            data.LastSeenDistanceSq = target == Entity.Null ? 0f : distSq;
            em.SetComponentData(entity, data);
        }

        private static void WriteMoveIntent(ref EntityManager em, in Entity entity, in float2 destination, float speed, AIMoveMode mode)
        {
            if (!em.HasComponent<MoveIntent>(entity))
                return;
            var intent = em.GetComponentData<MoveIntent>(entity);
            intent.Mode = (byte)mode;
            intent.Destination = destination;
            intent.Speed = speed;
            intent.Active = mode == AIMoveMode.Idle && speed <= 0f ? (byte)0 : (byte)1;
            em.SetComponentData(entity, intent);
        }

        private static void WriteCastIntent(ref EntityManager em, in Entity entity, in FixedString64Bytes spellId, in Entity target, bool active)
        {
            if (!em.HasComponent<CastIntent>(entity))
                return;
            var intent = em.GetComponentData<CastIntent>(entity);
            if (active && target != Entity.Null && spellId.Length > 0)
            {
                intent.SpellId = spellId;
                intent.Target = target;
                intent.Active = 1;
            }
            else
            {
                intent.Clear();
            }
            em.SetComponentData(entity, intent);
        }
    }
}
#endif
