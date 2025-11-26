using Unity.Collections;

namespace Framework.Spells.Content
{
    public struct EffectTiming
    {
        public float DelayMs;
        public EffectRepeat Repeat;
        public FixedString64Bytes PhaseTag;
    }

    public struct EffectRepeat
    {
        public int Count;
        public float PeriodMs;
    }
}
