using Framework.Melee.Components;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Melee.Blobs
{
    public struct MeleeProcPayloadArgs
    {
        public int Int0;
        public int Int1;
        public float Float0;
        public float Float1;
        public float DurationSeconds;
        public float IntervalSeconds;
        public FixedString64Bytes SecondaryId;
        public FixedString64Bytes TertiaryId;
    }

    public struct MeleeProcEntry
    {
        public FixedString32Bytes ProcId;
        public FixedString32Bytes SourceKeyHint;
        public float ChancePercent;
        public float InternalCooldownSeconds;
        public byte MaxTriggers;
        public float WindowSeconds;
        public MeleeProcPayloadKind PayloadKind;
        public FixedString64Bytes PayloadRef;
        public MeleeProcTargetMode TargetMode;
        public MeleeProcChargeMode ChargeMode;
        public byte MaxActivations;
        public float DurationSeconds;
        public byte TriggerOnZeroDamage;
        public byte MeleeOnly;
        public BlobPtr<MeleeProcPayloadArgs> Payload;
    }

    public struct MeleeProcTableBlob
    {
        public BlobArray<MeleeProcEntry> Entries;
    }
}
