using Unity.Burst;

namespace Framework.Core.Shared
{
    [BurstCompile]
    public static class ThreatUtility
    {
        [BurstCompile]
        public static int AddThreat(int current, int amount) => current + amount;
    }
}

