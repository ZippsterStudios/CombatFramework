using Unity.Burst;
using Unity.Collections;

namespace Framework.Buffs.Policies
{
    [BurstCompile]
    public static class BuffPolicy
    {
        public enum ApplyResult
        {
            Allow,
            Replace,
            Refresh,
            CapReached
        }

        [BurstCompile]
        public static ApplyResult ValidateStacking(in Content.BuffDefinition def, int existingStacks)
        {
            switch (def.StackingMode)
            {
                case Content.BuffStackingMode.Independent:
                    return ApplyResult.Allow;
                case Content.BuffStackingMode.RefreshDuration:
                    return ApplyResult.Refresh;
                case Content.BuffStackingMode.Replace:
                    return ApplyResult.Replace;
                case Content.BuffStackingMode.CapStacks:
                    if (existingStacks >= def.MaxStacks) return ApplyResult.CapReached;
                    return ApplyResult.Allow;
            }
            return ApplyResult.Allow;
        }
    }
}
