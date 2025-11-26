using System;
using Unity.Collections;

namespace Framework.Debuffs.Content
{
    [Flags]
    public enum DebuffFlags
    {
        None = 0,
        Root = 1 << 0,
        Fear = 1 << 1,
        Mez = 1 << 2,
        Stun = 1 << 3,
        Silence = 1 << 4,
        Disarm = 1 << 5,
        Slow = 1 << 6,
        Weaken = 1 << 7
    }

    public enum DebuffStackingMode
    {
        Independent,
        RefreshDuration,
        Replace,
        CapStacks
    }

    public enum DebuffDurationPolicy
    {
        Fixed,
        RefreshOnApply
    }

    public struct DebuffStatEffect
    {
        public FixedString32Bytes StatId;
        public float AdditivePerStack;
        public float MultiplierPerStack;
    }

    public struct DebuffDefinition
    {
        public FixedString64Bytes Id;
        public DebuffFlags Flags;
        public DebuffStackingMode StackingMode;
        public DebuffDurationPolicy DurationPolicy;
        public int MaxStacks;
        public float Duration;
        public FixedString32Bytes CategoryId;
        public int CategoryLevel;
        public int StackableCount;
        public FixedList128Bytes<DebuffStatEffect> StatEffects;
    }
}
