using Framework.AI.Components;
using Framework.AI.Policies;
using Framework.Contracts.Intents;
using Framework.Contracts.Perception;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using ContractsAIAgentTarget = Framework.Contracts.AI.AIAgentTarget;

namespace Framework.AI.Runtime
{
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, CompileSynchronously = true)]
    [UpdateInGroup(typeof(Framework.Core.Base.RequestsSystemGroup))]
    [UpdateBefore(typeof(AIRuntimeSystem))]
    public partial struct AIDecisionSystem : ISystem
    {
        private ComponentLookup<Framework.Resources.Components.Health> _healthLookupRO;
        private ComponentLookup<PerceptionSensedTarget> _sensedLookupRO;
        private ComponentLookup<PerceptionVisibility> _visibilityLookupRO;
        private BufferLookup<PerceptionTargetCandidate> _candidateLookupRO;
        private DefaultStateUtilityPolicy _statePolicy;
        private NearestVisibleTargetPolicy _targetPolicy;

        public void OnCreate(ref SystemState state)
        {
            _healthLookupRO = state.GetComponentLookup<Framework.Resources.Components.Health>(true);
            _sensedLookupRO = state.GetComponentLookup<PerceptionSensedTarget>(true);
            _visibilityLookupRO = state.GetComponentLookup<PerceptionVisibility>(true);
            _candidateLookupRO = state.GetBufferLookup<PerceptionTargetCandidate>(true);
            state.RequireForUpdate<AIBehaviorEnabledTag>();
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, CompileSynchronously = true)]
        public void OnUpdate(ref SystemState state)
        {
            _healthLookupRO.Update(ref state);
            _sensedLookupRO.Update(ref state);
            _visibilityLookupRO.Update(ref state);
            _candidateLookupRO.Update(ref state);

            var now = SystemAPI.Time.ElapsedTime;

            foreach (var (aiState, config, decisionState, target, requests, entity) in
                     SystemAPI.Query<RefRO<AIState>, RefRO<AIAgentBehaviorConfig>, RefRW<AIAgentDecisionState>, RefRW<ContractsAIAgentTarget>, DynamicBuffer<StateChangeRequest>>()
                         .WithAll<AIBehaviorEnabledTag>()
                         .WithEntityAccess())
            {
                if (!AIDecisionUtility.ShouldEvaluate(now, decisionState.ValueRO.NextDecisionTime))
                    continue;

                var snapshot = AIBehaviorSnapshotUtility.CreateSnapshot(in config.ValueRO);
                int candidateCount = RefreshTarget(entity, ref target.ValueRW);
                var healthPercent = ResolveHealthPercent(entity);
                var input = BuildInput(aiState.ValueRO.Current, in target.ValueRO, healthPercent, config.ValueRO.FleeHealthThresholdPercent, candidateCount);
                int desiredState = config.ValueRO.UseUtilityScoring != 0
                    ? _statePolicy.ChooseState(BuildUtilityContext(entity, aiState.ValueRO.Current, in snapshot, in target.ValueRO, healthPercent))
                    : AIDecisionUtility.RuleBasedState(in input);

                decisionState.ValueRW.NextDecisionTime = AIDecisionUtility.ScheduleNext(now, config.ValueRO.DecisionIntervalSeconds);

                if (desiredState != aiState.ValueRO.Current)
                {
                    requests.Add(new StateChangeRequest { Agent = entity, DesiredState = desiredState });
                }
            }
        }

        private AIDecisionInput BuildInput(int currentState, in ContractsAIAgentTarget target, float healthPercent, float fleeThreshold, int candidateCount)
        {
            return new AIDecisionInput
            {
                CurrentState = currentState,
                HasTarget = (byte)(target.Value != Entity.Null ? 1 : 0),
                TargetVisible = target.Visibility,
                TargetDistanceSq = target.LastSeenDistanceSq,
                HealthPercent = healthPercent,
                LowHealthThresholdPercent = fleeThreshold,
                CandidateCount = candidateCount
            };
        }

        private AIBehaviorContext BuildUtilityContext(Entity agent, int currentState, in AIBehaviorConfigSnapshot snapshot, in ContractsAIAgentTarget target, float healthPercent)
        {
            return new AIBehaviorContext
            {
                Agent = agent,
                StateId = currentState,
                Config = snapshot,
                Target = target.Value,
                HasTarget = (byte)(target.Value != Entity.Null ? 1 : 0),
                IsTargetVisible = target.Visibility,
                DistanceToTargetSq = target.LastSeenDistanceSq,
                HealthPercent = healthPercent
            };
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

        private int RefreshTarget(Entity agent, ref ContractsAIAgentTarget target)
        {
            int candidateCount = 0;

            if (_candidateLookupRO.HasBuffer(agent))
            {
                var buffer = _candidateLookupRO[agent];
                candidateCount = buffer.Length;

                if (buffer.Length > 0)
                {
                    var array = buffer.AsNativeArray();
                    var selected = _targetPolicy.Select(array, out var selectedIndex);
                    if (selected != Entity.Null && selectedIndex >= 0)
                    {
                        var entry = array[selectedIndex];
                        target.Value = entry.Target;
                        target.Visibility = entry.Visible;
                        target.LastSeenDistanceSq = entry.DistanceSq;
                        return candidateCount;
                    }
                }
            }

            if (_sensedLookupRO.HasComponent(agent))
            {
                var sensed = _sensedLookupRO[agent];
                target.Value = sensed.Value;
                target.Visibility = sensed.VisibleNow;
                target.LastSeenDistanceSq = sensed.LastDistSq;
            }
            else if (target.Value != Entity.Null)
            {
                target = ContractsAIAgentTarget.CreateDefault();
            }

            if (_visibilityLookupRO.HasComponent(agent))
            {
                var vis = _visibilityLookupRO[agent];
                target.Visibility = vis.Flags;
            }

            return candidateCount;
        }
    }
}
