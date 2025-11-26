using Unity.Burst;

namespace Framework.Threat.Policies
{
    [BurstCompile]
    public static class ThreatPolicy
    {
        public enum Result { Allow, Reject_InvalidDelta }

        [BurstCompile]
        public static Result Validate(int delta)
        {
            // allow any integer delta; clamp happens in driver
            return Result.Allow;
        }
    }
}

