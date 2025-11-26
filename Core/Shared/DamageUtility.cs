using Unity.Burst;
using Unity.Mathematics;

namespace Framework.Core.Shared
{
    [BurstCompile]
    public static class DamageUtility
    {
        [BurstCompile]
        public static int ApplyMitigation(int raw, int armor)
        {
            var mitig = math.max(0, raw - armor);
            return mitig;
        }
    }
}

