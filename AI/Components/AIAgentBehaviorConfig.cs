using Unity.Collections;
using Unity.Entities;

namespace Framework.AI.Components
{
    public struct AIAgentBehaviorConfig : IComponentData
    {
        public float DecisionIntervalSeconds;
        public byte UseUtilityScoring;
        public float AttackRange;
        public float ChaseRange;
        public float MoveSpeed;
        public float FleeMoveSpeed;
        public float FleeHealthThresholdPercent;
        public float FleeRetreatDistance;
        public FixedString64Bytes PrimarySpellId;
        public float PrimarySpellCooldownSeconds;

        public static AIAgentBehaviorConfig CreateDefaults()
        {
            return new AIAgentBehaviorConfig
            {
                DecisionIntervalSeconds = 0.2f,
                UseUtilityScoring = 0,
                AttackRange = 8f,
                ChaseRange = 12f,
                MoveSpeed = 5f,
                FleeMoveSpeed = 7.5f,
                FleeHealthThresholdPercent = 0.2f,
                FleeRetreatDistance = 6f,
                PrimarySpellId = "basic_attack",
                PrimarySpellCooldownSeconds = 1.25f
            };
        }
    }
}
