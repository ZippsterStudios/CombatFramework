using Framework.AI.Components;

namespace Framework.AI.Runtime
{
    internal static class AIBehaviorSnapshotUtility
    {
        public static AIBehaviorConfigSnapshot CreateSnapshot(in AIAgentBehaviorConfig config)
        {
            return new AIBehaviorConfigSnapshot
            {
                DecisionIntervalSeconds = config.DecisionIntervalSeconds,
                AttackRange = config.AttackRange,
                ChaseRange = config.ChaseRange,
                MoveSpeed = config.MoveSpeed,
                FleeMoveSpeed = config.FleeMoveSpeed,
                FleeHealthThresholdPercent = config.FleeHealthThresholdPercent,
                FleeRetreatDistance = config.FleeRetreatDistance,
                PrimarySpellId = config.PrimarySpellId,
                PrimarySpellCooldownSeconds = config.PrimarySpellCooldownSeconds
            };
        }
    }
}

