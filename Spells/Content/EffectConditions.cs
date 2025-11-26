using Unity.Collections;

namespace Framework.Spells.Content
{
    public struct EffectConditions
    {
        public byte ChancePercent;
        public byte RequireLineOfSight;
        public byte OnCritOnly;
        public float TargetHealthPercentLT;
        public FixedString64Bytes RequiresTag;
        public FixedString64Bytes TargetMustHaveTag;
        public FixedString64Bytes TargetMustNotHaveTag;
    }
}
