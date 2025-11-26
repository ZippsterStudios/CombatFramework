using Framework.Melee.Blobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Melee.Components
{
    public struct MeleeWeaponSlotElement : IBufferElementData
    {
        public Entity Weapon;
        public BlobAssetReference<MeleeWeaponDefBlob> Definition;
        public FixedString32Bytes SlotId;
        public byte Enabled;
        public byte SwingOrder;
        public float FamilyLockoutSeconds;
    }

    public struct MeleeAttackRequestElement : IBufferElementData
    {
        public Entity Attacker;
        public FixedString32Bytes WeaponSlot;
        public float3 AimDirection;
        public Entity PreferredTarget;
        public MeleeRequestFlags Flags;
        public uint RequestId;
        public byte ChainDepth;
        public MeleeChainAttackShape ChainShape;
        public float ChainArcDegrees;
        public float ChainRadius;
        public int ChainMaxTargets;
        public float ChainDelaySeconds;
        public float ChainLockoutSeconds;
    }

    public struct MeleeRiposteRequestElement : IBufferElementData
    {
        public Entity SourceSwing;
        public FixedString32Bytes WeaponSlot;
        public float3 AimDirection;
        public double ExecuteAtTime;
        public uint SourceRequestId;
    }

    public struct MeleeCastContext : IComponentData
    {
        public Entity Attacker;
        public Entity PreferredTarget;
        public FixedString32Bytes WeaponSlot;
        public BlobAssetReference<MeleeWeaponDefBlob> Definition;
        public float PhaseTimer;
        public MeleePhaseState Phase;
        public float WindupTime;
        public float ActiveTime;
        public float RecoveryTime;
        public int PenetrationRemaining;
        public float3 AimDirection;
        public bool CleaveMode;
        public float CleaveArcDegrees;
        public int CleaveMaxTargets;
        public uint DeterministicSeed;
        public uint SequenceId;
        public byte RiposteOrigin;
        public byte CleaveResolved;
        public byte Completed;
        public byte ChainDepth;
        public byte MultiAttackResolved;
        public MeleeChainAttackShape ChainShape;
        public float ChainArcDegrees;
        public float ChainRadius;
        public int ChainMaxTargets;
        public float ChainDelaySeconds;
        public float ChainLockoutSeconds;
    }

    public struct MeleeVictimElement : IBufferElementData
    {
        public Entity Target;
        public uint LastHitTick;
    }

    public struct EquipmentBuffElement : IBufferElementData
    {
        public FixedString64Bytes BuffId;
        public BlobAssetReference<MeleeProcTableBlob> ProcTable;
        public FixedString32Bytes SourceItemId;
        public byte IsProcCarrier;
        public byte StackCount;
    }

    public struct ProcAugmentElement : IBufferElementData
    {
        public FixedString64Bytes SourceBuffId;
        public BlobAssetReference<MeleeProcTableBlob> ProcTable;
        public double ExpireTime;
        public byte StackIndex;
    }

    public struct MeleeProcMergedEntryElement : IBufferElementData
    {
        public MeleeProcEntry Entry;
        public FixedString32Bytes SourceKey;
        public byte ChargeConsumed;
    }

    public struct MeleeDefenseTuning : IComponentData
    {
        public float ParryChance;
        public float DodgeChance;
        public float BlockChance;
        public float BlockFlat;
        public float BlockPercent;
        public float GuardCost;
        public Entity GuardResource;
        public MeleeRipostePolicy RipostePolicy;
        public FixedString32Bytes RiposteWeaponSlot;
    }

    public struct MeleeDefenseWindowState : IComponentData
    {
        public byte ParryWindowActive;
        public double WindowExpiry;
        public uint WindowId;
    }

    public struct MeleeStatSnapshot : IComponentData
    {
        public float ParryChance;
        public float DodgeChance;
        public float BlockChance;
        public float BlockFlat;
        public float BlockPercent;
        public float GuardPool;
        public float FrontalArcChance;
        public float FrontalArcDegrees;
        public int FrontalArcMaxTargets;
        public int FrontalArcPenetration;
        public float MultiDoubleChance;
        public float MultiTripleChance;
        public float MultiFlurryChance;
        public float MultiFlurryPerAttack;
        public int MultiFlurryMaxExtra;
        public float MultiAreaChance;
        public MeleeChainAttackShape MultiAreaShape;
        public float MultiAreaArcDegrees;
        public int MultiAreaMaxTargets;
        public float MultiAreaRadius;
        public byte MultiMaxChainDepth;
        public float MultiChainLockoutSeconds;
        public float MultiChainDelaySeconds;
    }

    public struct MeleeProcRuntimeStateElement : IBufferElementData
    {
        public FixedString32Bytes ProcId;
        public FixedString32Bytes SourceKey;
        public double NextReadyTime;
        public double WindowExpiry;
        public double ExpireTime;
        public byte TriggerCount;
        public byte RemainingActivations;
    }

    public struct MeleeRequestSequence : IComponentData
    {
        public uint NextId;
    }

    public struct MeleeLockout : IComponentData
    {
        public double NextReadyTimeGlobal;
        public double LastSwingTime;
        public FixedString32Bytes LastWeaponFamily;
    }

    public struct MeleeWeaponLockoutElement : IBufferElementData
    {
        public FixedString32Bytes WeaponSlot;
        public double ReadyTime;
    }

    public struct MeleeComboState : IComponentData
    {
        public byte StepIndex;
        public double ExpiryTime;
        public FixedString32Bytes ComboId;
    }

    public struct MeleeDebugConfig : IComponentData
    {
        public byte EnableVerbose;
        public FixedString128Bytes TagFilter;
    }

    public struct MeleeTelemetryState : IComponentData { }
}
