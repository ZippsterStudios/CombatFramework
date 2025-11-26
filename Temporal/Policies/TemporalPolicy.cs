using Unity.Burst;

namespace Framework.Temporal.Policies
{
    [BurstCompile]
    public static class TemporalPolicy
    {
        // Returns multiplier applied to tick/cooldown intervals (e.g., haste <1, slow >1)
        public static float IntervalMultiplier(float hastePercent, float slowPercent)
        {
            var mul = 1f;
            if (hastePercent > 0f) mul *= (1f - hastePercent);
            if (slowPercent > 0f) mul *= (1f + slowPercent);
            if (mul < 0.1f) mul = 0.1f;
            if (mul > 10f) mul = 10f;
            return mul;
        }
    }
}

