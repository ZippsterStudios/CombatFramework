using Unity.Burst;

namespace Framework.Core.Shared
{
    [BurstCompile]
    public static class BuffUtility
    {
        [BurstCompile]
        public static int StackClamp(int stacks, int maxStacks) => stacks > maxStacks ? maxStacks : stacks;
    }
}

