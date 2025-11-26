using Unity.Collections;

namespace Framework.Buffs.Content
{
    public enum BuffStackingMode
    {
        Independent,
        RefreshDuration,
        Replace,
        CapStacks
    }

    public enum BuffDurationPolicy
    {
        Fixed,
        RefreshOnApply
    }

    public struct DamageShieldSpec
    {
        public FixedString32Bytes ShieldId;
        public float DurationSeconds;
        public byte MaxActivations;
        public float InternalCooldownSeconds;
        public byte TriggerOnZeroDamage;
        public byte TriggerOnBlock;
        public byte TriggerOnParry;
        public byte PayloadKind;
        public byte TargetMode;
        public int ArgInt0;
        public int ArgInt1;
        public float ArgFloat0;
        public float ArgFloat1;
        public float IntervalSeconds;
        public FixedString64Bytes PayloadRef;
    }

    public struct MeleeWardSpec
    {
        public FixedString32Bytes WardId;
        public float DurationSeconds;
        public byte MaxActivations;
        public float AbsorbFlat;
        public float AbsorbPercent;
        public int TotalPool;
        public byte TriggerOnZeroDamage;
    }

    public struct BuffDefinition
    {
        public FixedString64Bytes Id;
        public BuffStackingMode StackingMode;
        public int MaxStacks;
        public BuffDurationPolicy DurationPolicy;
        public float Duration;
        public FixedString32Bytes CategoryId;
        public int CategoryLevel;
        public int StackableCount;
        public FixedList512Bytes<BuffStatEffect> StatEffects;
        public FixedList128Bytes<DamageShieldSpec> DamageShields;
        public FixedList128Bytes<MeleeWardSpec> MeleeWards;
    }
}
