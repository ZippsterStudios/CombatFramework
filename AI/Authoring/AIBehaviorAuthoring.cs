using Framework.AI.Components;
using Framework.AI.Runtime;
using Framework.Contracts.AI;
using Framework.Contracts.Intents;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using ContractsAIAgentTarget = Framework.Contracts.AI.AIAgentTarget;

namespace Framework.AI.Authoring
{
    [DisallowMultipleComponent]
    public sealed class AIBehaviorAuthoring : MonoBehaviour
    {
        [Min(0.05f)] public float DecisionIntervalSeconds = 0.2f;
        public bool UseUtilityScoring;
        [Min(0.25f)] public float AttackRange = 8f;
        [Min(0.25f)] public float ChaseRange = 12f;
        [Min(0.1f)] public float MoveSpeed = 5f;
        [Min(0.1f)] public float FleeMoveSpeed = 7.5f;
        [Range(0f, 1f)] public float FleeHealthThresholdPercent = 0.2f;
        [Min(0.1f)] public float FleeRetreatDistance = 6f;
        [SerializeField] private string _primarySpellId = "basic_attack";
        [Min(0.05f)] public float PrimarySpellCooldownSeconds = 1.25f;

        public AIAgentBehaviorConfig ToComponentData()
        {
            var spellId = new Unity.Collections.FixedString64Bytes(_primarySpellId ?? string.Empty);
            return new AIAgentBehaviorConfig
            {
                DecisionIntervalSeconds = math.max(0.05f, DecisionIntervalSeconds),
                UseUtilityScoring = (byte)(UseUtilityScoring ? 1 : 0),
                AttackRange = math.max(0.25f, AttackRange),
                ChaseRange = math.max(AttackRange, ChaseRange),
                MoveSpeed = math.max(0.1f, MoveSpeed),
                FleeMoveSpeed = math.max(0.1f, FleeMoveSpeed),
                FleeHealthThresholdPercent = math.clamp(FleeHealthThresholdPercent, 0f, 1f),
                FleeRetreatDistance = math.max(0.1f, FleeRetreatDistance),
                PrimarySpellId = spellId,
                PrimarySpellCooldownSeconds = math.max(0.05f, PrimarySpellCooldownSeconds)
            };
        }

#if UNITY_EDITOR && UNITY_ENTITIES_1_0_0_OR_NEWER
        private sealed class Baker : Unity.Entities.Baker<AIBehaviorAuthoring>
        {
            public override void Bake(AIBehaviorAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var config = authoring.ToComponentData();
                AddComponent(entity, config);
                AddComponent(entity, AIAgentDecisionState.CreateDefault());

                if (!HasComponent<AIState>(entity))
                    AddComponent(entity, new AIState { Current = AIStateIds.Idle });

                AddComponent(entity, AICombatRuntime.CreateDefault());
                AddComponent<AIBehaviorEnabledTag>(entity);

                if (!HasComponent<ContractsAIAgentTarget>(entity))
                    AddComponent(entity, ContractsAIAgentTarget.CreateDefault());

                if (!HasComponent<MoveIntent>(entity))
                    AddComponent(entity, new MoveIntent());

                if (!HasComponent<CastIntent>(entity))
                    AddComponent(entity, new CastIntent());

                if (!HasBuffer<StateChangeRequest>(entity))
                    AddBuffer<StateChangeRequest>(entity);
            }
        }
#endif
    }
}
