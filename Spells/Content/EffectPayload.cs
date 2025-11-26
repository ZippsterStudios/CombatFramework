using Framework.Damage.Components;
using Unity.Collections;

namespace Framework.Spells.Content
{
    public struct EffectPayload
    {
        public EffectPayloadKind Kind;
        public DamagePayload Damage;
        public HealPayload Heal;
        public StatOpsPayload StatOps;
        public ApplyEffectPayload Apply;
        public ScriptPayload Script;
        public DotHotPayload OverTime;
        public SummonPayload Summon;
        public AreaEffectPayload Area;
    }

    public enum EffectPayloadKind : byte
    {
        None = 0,
        Damage = 1,
        Heal = 2,
        StatOps = 3,
        ApplyBuff = 4,
        ApplyDebuff = 5,
        SpawnDot = 6,
        SpawnHot = 7,
        ScriptReference = 8,
        SummonPet = 9,
        SpawnAreaEffect = 10
    }

    public struct DamagePayload
    {
        public DamageSchool School;
        public int Amount;
        public float VariancePercent;
        public byte CanCrit;
        public FixedString64Bytes Tags;
        public byte IgnoreArmor;
        public byte IgnoreResist;
        public byte IgnoreSnapshotModifiers;
    }

    public struct HealPayload
    {
        public int Amount;
        public float VariancePercent;
        public byte CanCrit;
    }

    public struct StatOpsPayload
    {
        public StatOperation[] Operations;
    }

    public struct StatOperation
    {
        public FixedString64Bytes StatId;
        public StatOperationKind Operation;
        public float Value;
        public int DurationMs;
        public FixedString64Bytes StackingKey;
        public StatOperationStackingPolicy StackingPolicy;
    }

    public enum StatOperationKind : byte
    {
        Add = 0,
        Multiply = 1,
        Set = 2
    }

    public enum StatOperationStackingPolicy : byte
    {
        Refresh = 0,
        StackDuration = 1,
        IgnoreWhileActive = 2
    }

    public struct ApplyEffectPayload
    {
        public FixedString64Bytes Id;
        public int DurationMs;
        public byte RefreshDuration;
    }

    public struct DotHotPayload
    {
        public FixedString64Bytes Id;
        public byte UseCatalogDefaults;
        public int MagnitudeOverride;
        public float TickIntervalOverride;
        public float DurationOverride;
    }

    public struct ScriptPayload
    {
        public FixedString64Bytes FeatureId;
        public FixedString64Bytes Arguments;
    }

    public struct SummonPayload
    {
        public FixedString64Bytes PetId;
        public int Count;
        public float SpawnRadius;
    }

    public struct AreaEffectPayload
    {
        public FixedString64Bytes AreaId;
        public float Radius;
        public float Duration;
    }
}
