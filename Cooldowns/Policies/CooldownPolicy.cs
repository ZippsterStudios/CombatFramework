using Unity.Burst;

namespace Framework.Cooldowns.Policies
{
    [BurstCompile]
    public static class CooldownPolicy
    {
        [BurstCompile]
        public static bool IsReady(double now, double readyTime) => now >= readyTime;
    }
}
