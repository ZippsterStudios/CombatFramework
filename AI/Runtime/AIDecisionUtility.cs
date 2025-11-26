using Unity.Mathematics;

namespace Framework.AI.Runtime
{
    public struct AIDecisionInput
    {
        public int CurrentState;
        public byte HasTarget;
        public byte TargetVisible;
        public float TargetDistanceSq;
        public float HealthPercent;
        public float LowHealthThresholdPercent;
        public int CandidateCount;
    }

    public readonly struct AIDecisionResult
    {
        public readonly int DesiredState;
        public readonly bool ShouldRequestTransition;

        public AIDecisionResult(int desired, bool shouldTransition)
        {
            DesiredState = desired;
            ShouldRequestTransition = shouldTransition;
        }
    }

    public static class AIDecisionUtility
    {
        public static bool ShouldEvaluate(double now, double nextAllowedTime) => now >= nextAllowedTime - 1e-6d;

        public static double ScheduleNext(double now, float intervalSeconds)
        {
            var interval = math.max(0.05f, intervalSeconds);
            return now + interval;
        }

        public static AIDecisionResult FromState(int currentState, int desired)
        {
            return new AIDecisionResult(desired, desired != currentState);
        }

        public static int RuleBasedState(in AIDecisionInput input)
        {
            if (input.HealthPercent > 0f && input.HealthPercent <= math.max(0.01f, input.LowHealthThresholdPercent))
                return AIStateIds.Flee;

            if (input.HasTarget != 0 && input.TargetVisible != 0)
                return AIStateIds.Combat;

            if (input.CandidateCount > 0)
                return AIStateIds.Combat;

            return AIStateIds.Idle;
        }
    }
}
