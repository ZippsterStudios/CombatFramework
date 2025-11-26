using Unity.Burst;

namespace Framework.Stats.Policies
{
    [BurstCompile]
    public static class StatPolicy
    {
        [BurstCompile]
        public static float Clamp(float value, float min = float.NegativeInfinity, float max = float.PositiveInfinity)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        [BurstCompile]
        public static float Round(float value, int decimals = 2)
        {
            // simple rounding without System.Math to keep Burst-friendly
            float scale = 1f;
            for (int i = 0; i < decimals; i++) scale *= 10f;
            return (int)(value * scale + 0.5f) / scale;
        }
    }
}

