using Framework.AI.Behaviors.Components;
using Framework.AI.Components;
using Framework.AI.Policies;
using Framework.AI.Runtime;
using Framework.Contracts.Intents;
using Framework.Contracts.Perception;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using ContractsAIAgentTarget = Framework.Contracts.AI.AIAgentTarget;

namespace Framework.AI.Behaviors.Runtime
{
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, CompileSynchronously = true)]
    [UpdateInGroup(typeof(Framework.Core.Base.RequestsSystemGroup))]
    [UpdateAfter(typeof(Framework.AI.Runtime.AIDecisionSystem))]
    [UpdateBefore(typeof(Framework.AI.Runtime.AIStateMachineSystem))]
    public partial struct AIBehaviorRecipeSystem : ISystem
    {
        private ComponentLookup<Framework.Core.Components.Position> _positionLookupRO;
        private ComponentLookup<Framework.Resources.Components.Health> _healthLookupRO;
        private ComponentLookup<ContractsAIAgentTarget> _targetLookupRO;
        private ComponentLookup<LeashConfig> _leashLookupRO;
        private DefaultMovementPolicy _movementPolicy;

        public void OnCreate(ref SystemState state)
        {
            _positionLookupRO = state.GetComponentLookup<Framework.Core.Components.Position>(true);
            _healthLookupRO = state.GetComponentLookup<Framework.Resources.Components.Health>(true);
            _targetLookupRO = state.GetComponentLookup<ContractsAIAgentTarget>(true);
            _leashLookupRO = state.GetComponentLookup<LeashConfig>(true);
            state.RequireForUpdate<AIBehaviorEnabledTag>();
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, CompileSynchronously = true)]
        public void OnUpdate(ref SystemState state)
        {
            _positionLookupRO.Update(ref state);
            _healthLookupRO.Update(ref state);
            _targetLookupRO.Update(ref state);
            _leashLookupRO.Update(ref state);

            var now = SystemAPI.Time.ElapsedTime;

            foreach (var (recipeRef, config, combatRuntime, moveIntent, castIntent, entity) in
                     SystemAPI.Query<RefRO<AIBehaviorRecipeRef>, RefRO<AIAgentBehaviorConfig>, RefRW<AICombatRuntime>, RefRW<MoveIntent>, RefRW<CastIntent>>()
                         .WithAll<AIBehaviorEnabledTag>()
                         .WithEntityAccess())
            {
                var recipe = recipeRef.ValueRO.Recipe;
                if (!recipe.IsCreated)
                    continue;

                var facts = GatherFacts(entity);
                var action = AIBehaviorRecipeMatcher.Select(recipe, facts, in config.ValueRO);
                ApplyAction(action, facts, in config.ValueRO, now, ref combatRuntime.ValueRW, ref moveIntent.ValueRW, ref castIntent.ValueRW);
            }
        }

        private RecipeFacts GatherFacts(Entity agent)
        {
            var facts = new RecipeFacts { Target = Entity.Null, HealthPercent = 1f };
            if (_targetLookupRO.HasComponent(agent))
            {
                var target = _targetLookupRO[agent];
                facts.Target = target.Value;
                facts.HasTarget = (byte)(target.Value != Entity.Null ? 1 : 0);
                facts.Visibility = target.Visibility;
                facts.DistanceSq = target.LastSeenDistanceSq;
            }

            if (_positionLookupRO.HasComponent(agent))
            {
                facts.AgentPosition = _positionLookupRO[agent].Value;
                facts.HasAgentPosition = 1;
            }

            if (facts.Target != Entity.Null && _positionLookupRO.HasComponent(facts.Target))
            {
                facts.TargetPosition = _positionLookupRO[facts.Target].Value;
                facts.HasTargetPosition = 1;
                if (facts.HasAgentPosition != 0)
                {
                    var delta = facts.TargetPosition - facts.AgentPosition;
                    facts.DistanceSq = delta.x * delta.x + delta.y * delta.y;
                }
            }

            if (_healthLookupRO.HasComponent(agent))
            {
                var health = _healthLookupRO[agent];
                if (health.Max > 0)
                    facts.HealthPercent = math.saturate((float)health.Current / math.max(1, health.Max));
            }

            if (_leashLookupRO.HasComponent(agent))
            {
                var leash = _leashLookupRO[agent];
                facts.HasLeash = 1;
                facts.LeashHome = leash.Home;
                facts.LeashRadius = leash.Radius;
                facts.LeashSoftRadius = leash.SoftRadius;
            }

            return facts;
        }

        private void ApplyAction(in Action action, in RecipeFacts facts, in AIAgentBehaviorConfig config, double now, ref AICombatRuntime combat, ref MoveIntent moveIntent, ref CastIntent castIntent)
        {
            switch ((AIBehaviorActionKind)action.Kind)
            {
                case AIBehaviorActionKind.MoveChase:
                    WriteMoveIntent(ref moveIntent, AIMoveMode.Chase, action.MoveSpeed > 0 ? action.MoveSpeed : config.MoveSpeed, facts, config.AttackRange, now);
                    AIMovementUtility.ClampToLeash(facts.HasLeash, facts.LeashHome, facts.LeashRadius, facts.LeashSoftRadius, ref moveIntent);
                    castIntent.Clear();
                    break;

                case AIBehaviorActionKind.MoveFlee:
                    WriteMoveIntent(ref moveIntent, AIMoveMode.Flee, math.max(config.MoveSpeed, action.MoveSpeed > 0 ? action.MoveSpeed : config.FleeMoveSpeed), facts, action.RetreatDistance > 0 ? action.RetreatDistance : config.FleeRetreatDistance, now);
                    AIMovementUtility.ClampToLeash(facts.HasLeash, facts.LeashHome, facts.LeashRadius, facts.LeashSoftRadius, ref moveIntent);
                    castIntent.Clear();
                    break;

                case AIBehaviorActionKind.Stop:
                    moveIntent.Clear();
                    castIntent.Clear();
                    break;

                case AIBehaviorActionKind.CastPrimary:
                    moveIntent.Clear();
                    AICastUtility.TryWriteCast(now, config.PrimarySpellId, facts.Target, ref combat, in config, ref castIntent);
                    break;

                case AIBehaviorActionKind.CastId:
                    moveIntent.Clear();
                    AICastUtility.TryWriteCast(now, action.SpellId, facts.Target, ref combat, in config, ref castIntent);
                    break;

                default:
                    moveIntent.Clear();
                    castIntent.Clear();
                    break;
            }
        }

        private void WriteMoveIntent(ref MoveIntent intent, AIMoveMode mode, float speed, in RecipeFacts facts, float range, double now)
        {
            if (facts.HasAgentPosition == 0)
            {
                intent.Clear();
                return;
            }

            intent.Mode = (byte)mode;
            intent.Speed = speed;
            intent.Active = 1;
            var targetPos = facts.HasTargetPosition != 0 ? facts.TargetPosition : facts.AgentPosition;
            _movementPolicy.Resolve(facts.AgentPosition, targetPos, range, now, ref intent);
        }

    }
}
