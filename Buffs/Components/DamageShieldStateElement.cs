using Unity.Collections;
using Unity.Entities;

namespace Framework.Buffs.Components
{
    public struct DamageShieldStateElement : IBufferElementData
    {
        public FixedString32Bytes ShieldId;
        public FixedString64Bytes BuffId;
        public byte RemainingActivations;
        public byte MaxActivations;
        public double ExpireTime;
        public double NextReadyTime;
        public float InternalCooldownSeconds;
        public byte PayloadKind;
        public byte TargetMode;
        public byte TriggerOnZeroDamage;
        public byte TriggerOnBlock;
        public byte TriggerOnParry;
        public FixedString64Bytes PayloadRef;
        public int ArgInt0;
        public int ArgInt1;
        public float ArgFloat0;
        public float ArgFloat1;
        public float IntervalSeconds;
    }

    public struct MeleeWardStateElement : IBufferElementData
    {
        public FixedString32Bytes WardId;
        public FixedString64Bytes BuffId;
        public byte RemainingActivations;
        public byte MaxActivations;
        public double ExpireTime;
        public float AbsorbFlat;
        public float AbsorbPercent;
        public int RemainingPool;
        public byte TriggerOnZeroDamage;
    }
}


