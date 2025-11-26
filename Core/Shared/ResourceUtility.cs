using Unity.Burst;
using Unity.Mathematics;

namespace Framework.Core.Shared
{
    [BurstCompile]
    public static class ResourceUtility
    {
        [BurstCompile]
        public static int Spend(int current, int cost) => math.max(0, current - cost);
    }
}

