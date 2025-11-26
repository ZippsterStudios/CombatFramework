using Unity.Burst;
using Unity.Mathematics;

namespace Framework.Core.Shared
{
    [BurstCompile]
    public static class CombatMath
    {
        [BurstCompile]
        public static float Clamp01(float v) => math.clamp(v, 0f, 1f);

        [BurstCompile]
        public static int RoundToInt(float v) => (int)math.round(v);

        [BurstCompile]
        public static float SafeDiv(float a, float b) => b == 0f ? 0f : a / b;

        [BurstCompile]
        public static float Lerp(float a, float b, float t) => math.lerp(a, b, Clamp01(t));
    }
}

