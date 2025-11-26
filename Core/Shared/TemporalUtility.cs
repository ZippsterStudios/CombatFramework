using Unity.Burst;

namespace Framework.Core.Shared
{
    [BurstCompile]
    public static class TemporalUtility
    {
        [BurstCompile]
        public static float Advance(float now, float deltaTime) => now + deltaTime;
    }
}

