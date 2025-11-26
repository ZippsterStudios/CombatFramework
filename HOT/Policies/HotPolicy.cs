using Unity.Burst;

namespace Framework.HOT.Policies
{
    [BurstCompile]
    public static class HotPolicy
    {
        public enum Result
        {
            Allow,
            Reject_InvalidMagnitude,
            Reject_InvalidDuration
        }

        [BurstCompile]
        public static Result Validate(int hps, float duration)
        {
            if (hps <= 0) return Result.Reject_InvalidMagnitude;
            if (duration <= 0f) return Result.Reject_InvalidDuration;
            return Result.Allow;
        }
    }
}

