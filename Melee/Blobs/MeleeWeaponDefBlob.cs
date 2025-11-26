using Framework.Damage.Components;
using Framework.Melee.Components;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Melee.Blobs
{
    public struct MeleeMultiAttackConfig
    {
        public float DoubleChancePercent;
        public float TripleChancePercent;
        public float FlurryChancePercent;
        public float FlurryPerAttackPercent;
        public byte FlurryMaxExtraAttacks;
        public byte MaxChainDepth;
        public float ChainLockoutSeconds;
        public float ChainDelaySeconds;
        public float AreaChancePercent;
        public MeleeChainAttackShape AreaShape;
        public float AreaArcDegrees;
        public int AreaMaxTargets;
        public float AreaRadius;
    }

    public struct MeleeWeaponDefBlob
    {
        public FixedString64Bytes WeaponId;
        public DamagePacket BaseDamage;
        public float WindupSeconds;
        public float ActiveSeconds;
        public float RecoverySeconds;
        public float Range;
        public float BaselineArcDegrees;
        public int PenetrationCount;
        public int StaminaCost;
        public float LockoutSeconds;
        public byte DefaultBypassFlags;
        public BlobArray<MeleeProcEntry> ProcEntries;
        public float DefaultCleaveArcDegrees;
        public int DefaultCleaveMaxTargets;
        public float GuardCost;
        public MeleeMultiAttackConfig MultiAttack;
    }
}
