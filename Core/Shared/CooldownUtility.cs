using Unity.Burst;

namespace Framework.Core.Shared
{
    [BurstCompile]
    public static class CooldownUtility
    {
        [BurstCompile]
        public static float NextReadyTime(float now, float cooldown) => now + cooldown;
    }
}

