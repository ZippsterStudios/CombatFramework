using Framework.AI.Behaviors.Components;
using Framework.AI.Components;
using Framework.AI.Policies;
using Framework.Contracts.Intents;
using Framework.Contracts.Perception;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using ContractsAIAgentTarget = Framework.Contracts.AI.AIAgentTarget;

namespace Framework.AI.Runtime
{
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, CompileSynchronously = true)]
    [UpdateInGroup(typeof(Framework.Core.Base.RequestsSystemGroup))]
    [UpdateAfter(typeof(AIRuntimeSystem))]
    public partial struct AIStateMachineSystem : ISystem
    {
        private ComponentLookup<Framework.Core.Components.Position> _positionLookupRO;
        private ComponentLookup<Framework.Resources.Components.Health> _healthLookupRO;
        private ComponentLookup<LeashConfig> _leashLookupRO;
        private DefaultMovementPolicy _movementPolicy;

        public void OnCreate(ref SystemState state)
        {
            _positionLookupRO = state.GetComponentLookup<Framework.Core.Components.Position>(true);
            _healthLookupRO = state.GetComponentLookup<Framework.Resources.Components.Health>(true);
            _leashLookupRO = state.GetComponentLookup<LeashConfig>(true);
            state.RequireForUpdate<AIBehaviorEnabledTag>();
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, CompileSynchronously = true)]
        public void OnUpdate(ref SystemState state)
        {
            _positionLookupRO.Update(ref state);
            _healthLookupRO.Update(ref state);
            _leashLookupRO.Update(ref state);

            var em = state.EntityManager;
            var now = SystemAPI.Time.ElapsedTime;

            foreach (var (aiState, config, target, combatRuntime, moveIntent, castIntent, requests, entity) in
                     SystemAPI.Query<RefRO<AIState>, RefRO<AIAgentBehaviorConfig>, RefRW<ContractsAIAgentTarget>, RefRW<AICombatRuntime>, RefRW<MoveIntent>, RefRW<CastIntent>, DynamicBuffer<StateChangeRequest>>()
                         .WithAll<AIBehaviorEnabledTag>()
                         .WithNone<AIBehaviorRecipeRef>()
                         .WithEntityAccess())
            {
                if (target.ValueRO.Value != Entity.Null && !em.Exists(target.ValueRO.Value))
                    target.ValueRW = ContractsAIAgentTarget.CreateDefault();

                var snapshot = AIBehaviorSnapshotUtility.CreateSnapshot(in config.ValueRO);
                var context = BuildContext(entity, aiState.ValueRO.Current, in snapshot, in target.ValueRO);
                var commands = default(AIBehaviorCommands);
                commands.Clear();

                AIBehaviorDrivers.Run(in context, ref commands);

                switch (commands.Status)
                {
                    case AIBehaviorResultStatus.Success:
                        ApplyMoveIntent(in context, in commands, ref moveIntent.ValueRW, now);
                        ApplyLeash(in context, ref moveIntent.ValueRW);
                        ApplyCastIntent(in context, in commands, ref castIntent.ValueRW, ref combatRuntime.ValueRW, in config.ValueRO, now);
                        if (context.TargetIsValid)
                            target.ValueRW.LastSeenDistanceSq = context.DistanceToTargetSq;
                        if (commands.RequestStateChange != 0)
                            requests.Add(new StateChangeRequest { Agent = entity, DesiredState = commands.RequestedState });
                        break;

                    case AIBehaviorResultStatus.MissingTarget:
                        target.ValueRW = ContractsAIAgentTarget.CreateDefault();
                        moveIntent.ValueRW.Clear();
                        castIntent.ValueRW.Clear();
                        requests.Add(new StateChangeRequest { Agent = entity, DesiredState = AIStateIds.Idle });
                        break;

                    default:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        UnityEngine.Debug.LogWarning($"[AI] Driver failed for {entity}. Status={commands.Status}");
#endif
                        moveIntent.ValueRW.Clear();
                        castIntent.ValueRW.Clear();
                        requests.Add(new StateChangeRequest { Agent = entity, DesiredState = AIStateIds.Idle });
                        break;
                }
            }
        }

        private AIBehaviorContext BuildContext(Entity agent, int stateId, in AIBehaviorConfigSnapshot config, in ContractsAIAgentTarget target)
        {
            var ctx = new AIBehaviorContext
            {
                Agent = agent,
                StateId = stateId,
                Config = config,
                Target = target.Value,
                HasTarget = (byte)(target.Value != Entity.Null ? 1 : 0),
                IsTargetVisible = target.Visibility,
                DistanceToTargetSq = target.LastSeenDistanceSq,
                HealthPercent = ResolveHealthPercent(agent)
            };

            if (_positionLookupRO.HasComponent(agent))
            {
                ctx.AgentPosition = _positionLookupRO[agent].Value;
                ctx.HasAgentPosition = 1;
            }

            if (ctx.Target != Entity.Null && _positionLookupRO.HasComponent(ctx.Target))
            {
                ctx.TargetPosition = _positionLookupRO[ctx.Target].Value;
                ctx.HasTargetPosition = 1;
                if (ctx.HasAgentPosition != 0)
                {
                    var delta = ctx.TargetPosition - ctx.AgentPosition;
                    ctx.DistanceToTargetSq = delta.x * delta.x + delta.y * delta.y;
                }
            }

            if (_leashLookupRO.HasComponent(agent))
            {
                var leash = _leashLookupRO[agent];
                ctx.HasLeash = 1;
                ctx.LeashHome = leash.Home;
                ctx.LeashRadius = leash.Radius;
                ctx.LeashSoftRadius = leash.SoftRadius;
            }

            return ctx;
        }

        private float ResolveHealthPercent(Entity agent)
        {
            if (_healthLookupRO.HasComponent(agent))
            {
                var health = _healthLookupRO[agent];
                if (health.Max > 0)
                    return math.saturate((float)health.Current / math.max(1, health.Max));
            }

            return 1f;
        }

        private void ApplyMoveIntent(in AIBehaviorContext context, in AIBehaviorCommands commands, ref MoveIntent moveIntent, double now)
        {
            if (commands.MoveRequested == 0 || context.HasAgentPosition == 0)
            {
                moveIntent.Clear();
                return;
            }

            moveIntent.Mode = (byte)commands.MoveMode;
            moveIntent.Speed = commands.MoveSpeed;
            moveIntent.Active = 1;

            var targetPos = context.HasTargetPosition != 0 ? context.TargetPosition : context.AgentPosition;
            _movementPolicy.Resolve(context.AgentPosition, targetPos, context.Config.AttackRange, now, ref moveIntent);
        }

        private static void ApplyCastIntent(in AIBehaviorContext context, in AIBehaviorCommands commands, ref CastIntent castIntent, ref AICombatRuntime runtime, in AIAgentBehaviorConfig config, double now)
        {
            if (commands.SpellRequested == 0 || commands.SpellTarget == Entity.Null)
            {
                castIntent.Clear();
                return;
            }

            AICastUtility.TryWriteCast(now, commands.SpellId, commands.SpellTarget, ref runtime, in config, ref castIntent);
        }

        private static void ApplyLeash(in AIBehaviorContext context, ref MoveIntent moveIntent)
        {
            AIMovementUtility.ClampToLeash(context.HasLeash, context.LeashHome, context.LeashRadius, context.LeashSoftRadius, ref moveIntent);
        }
    }
}
