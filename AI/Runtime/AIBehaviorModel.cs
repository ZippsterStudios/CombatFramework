using Framework.Contracts.Intents;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.AI.Runtime
{
    public static class AIStateIds
    {
        public const int Idle = 0;
        public const int Combat = 1;
        public const int Flee = 2;
    }

    public enum AIBehaviorResultStatus : byte
    {
        Success = 0,
        MissingTarget = 1,
        InvalidState = 2
    }

    public struct AIBehaviorConfigSnapshot
    {
        public float DecisionIntervalSeconds;
        public float AttackRange;
        public float ChaseRange;
        public float MoveSpeed;
        public float FleeMoveSpeed;
        public float FleeHealthThresholdPercent;
        public float FleeRetreatDistance;
        public FixedString64Bytes PrimarySpellId;
        public float PrimarySpellCooldownSeconds;
    }

    public struct AIBehaviorContext
    {
        public Entity Agent;
        public int StateId;
        public float DeltaTime;
        public AIBehaviorConfigSnapshot Config;
        public Entity Target;
        public byte HasTarget;
        public float2 AgentPosition;
        public byte HasAgentPosition;
        public float2 TargetPosition;
        public byte HasTargetPosition;
        public float HealthPercent;
        public float DistanceToTargetSq;
        public byte IsTargetVisible;
        public byte HasLeash;
        public float2 LeashHome;
        public float LeashRadius;
        public float LeashSoftRadius;

        public bool TargetIsValid => HasTarget != 0 && Target != Entity.Null && HasTargetPosition != 0;
        public bool LowHealth => HealthPercent > 0f && HealthPercent <= math.max(0.01f, Config.FleeHealthThresholdPercent);
    }

    public struct AIBehaviorCommands
    {
        public AIBehaviorResultStatus Status;
        public byte MoveRequested;
        public AIMoveMode MoveMode;
        public float2 MoveDestination;
        public float MoveSpeed;
        public byte SpellRequested;
        public FixedString64Bytes SpellId;
        public Entity SpellTarget;
        public byte RequestStateChange;
        public int RequestedState;

        public void Clear()
        {
            Status = AIBehaviorResultStatus.Success;
            MoveRequested = 0;
            MoveMode = AIMoveMode.None;
            MoveDestination = float2.zero;
            MoveSpeed = 0f;
            SpellRequested = 0;
            SpellId = default;
            SpellTarget = Entity.Null;
            RequestStateChange = 0;
            RequestedState = AIStateIds.Idle;
        }

        public void RequestMove(float2 destination, float speed, AIMoveMode mode)
        {
            MoveRequested = 1;
            MoveDestination = destination;
            MoveSpeed = speed;
            MoveMode = mode;
        }

        public void RequestSpell(in FixedString64Bytes spellId, in Entity target)
        {
            if (spellId.Length == 0 || target == Entity.Null)
                return;

            SpellRequested = 1;
            SpellId = spellId;
            SpellTarget = target;
        }

        public void RequestState(int desiredState)
        {
            RequestStateChange = 1;
            RequestedState = desiredState;
        }
    }

    public static class AIBehaviorDrivers
    {
        public static void Run(in AIBehaviorContext context, ref AIBehaviorCommands commands)
        {
            commands.Status = AIBehaviorResultStatus.Success;

            switch (context.StateId)
            {
                case AIStateIds.Idle:
                    RunIdle(in context, ref commands);
                    break;
                case AIStateIds.Combat:
                    RunCombat(in context, ref commands);
                    break;
                case AIStateIds.Flee:
                    RunFlee(in context, ref commands);
                    break;
                default:
                    commands.Status = AIBehaviorResultStatus.InvalidState;
                    break;
            }
        }

        private static void RunIdle(in AIBehaviorContext context, ref AIBehaviorCommands commands)
        {
            // Idle intentionally minimal – system just keeps locomotion cleared.
            commands.MoveRequested = 0;
        }

        private static void RunCombat(in AIBehaviorContext context, ref AIBehaviorCommands commands)
        {
            if (!context.TargetIsValid)
            {
                commands.Status = AIBehaviorResultStatus.MissingTarget;
                return;
            }

            float range = math.max(0.5f, context.Config.AttackRange);
            float rangeSq = range * range;

            if (context.DistanceToTargetSq > rangeSq)
            {
                commands.RequestMove(context.TargetPosition, math.max(0.1f, context.Config.MoveSpeed), AIMoveMode.Chase);
                return;
            }

            commands.RequestMove(context.AgentPosition, 0f, AIMoveMode.Idle);
            commands.RequestSpell(context.Config.PrimarySpellId, context.Target);
        }

        private static void RunFlee(in AIBehaviorContext context, ref AIBehaviorCommands commands)
        {
            if (!context.TargetIsValid || context.HasAgentPosition == 0)
            {
                commands.Status = AIBehaviorResultStatus.MissingTarget;
                return;
            }

            var away = context.AgentPosition - context.TargetPosition;
            var direction = NormalizeSafe(away, new float2(1f, 0f));
            var destination = context.AgentPosition + direction * math.max(1f, context.Config.FleeRetreatDistance);
            commands.RequestMove(destination, math.max(context.Config.MoveSpeed, context.Config.FleeMoveSpeed), AIMoveMode.Flee);
        }

        private static float2 NormalizeSafe(float2 value, float2 fallback)
        {
            float magSq = value.x * value.x + value.y * value.y;
            if (magSq <= 1e-5f)
            {
                float fallbackMagSq = math.max(1e-5f, fallback.x * fallback.x + fallback.y * fallback.y);
                return fallback / math.sqrt(fallbackMagSq);
            }

            return value / math.sqrt(magSq);
        }
    }
}
