using Unity.Burst;
using Unity.Mathematics;

namespace Framework.Spells.Policies
{
    [BurstCompile]
    public static class SpatialPolicy
    {
        public enum Result
        {
            Allow,
            Reject_OutOfRange,
            Reject_NoLineOfSight,
            Reject_BadFacing
        }

        // Minimal spatial: only checks distance
        public static Result InRange(float3 casterPos, float3 targetPos, float range)
        {
            if (range <= 0f) return Result.Allow;
            var d = targetPos - casterPos;
            return math.length(d) <= range ? Result.Allow : Result.Reject_OutOfRange;
        }
    }
}

