using Unity.Collections;
using Unity.Entities;

namespace Framework.Melee.Components
{
    public enum MeleePhaseState : byte
    {
        None = 0,
        Windup = 1,
        Active = 2,
        Recovery = 3
    }

    [System.Flags]
    public enum MeleeRequestFlags : byte
    {
        None = 0,
        AllowRiposte = 1 << 0,
        Riposte = 1 << 1,
        SkipStaminaCost = 1 << 2,
        MultiAttackChain = 1 << 3
    }

    public enum MeleeRipostePolicy : byte
    {
        None = 0,
        Immediate = 1,
        NextFrame = 2
    }

    public enum MeleeProcPayloadKind : byte
    {
        ExtraDamage = 0,
        DamageOverTime = 1,
        HealOverTime = 2,
        Buff = 3,
        Debuff = 4,
        AreaEffect = 5,
        ScriptFeature = 6,
        Spell = 7
    }

    public enum MeleeProcTargetMode : byte
    {
        Self = 0,
        Target = 1,
        ArcSet = 2,
        Group = 3
    }

    public enum MeleeProcChargeMode : byte
    {
        PerTriggerSuccess = 0,
        PerTargetApplied = 1
    }

    public enum MeleeChainAttackShape : byte
    {
        None = 0,
        Arc = 1,
        TrueArea = 2,
        RearArc = 3
    }

    public enum MeleeTelemetryEventType : byte
    {
        SwingBegan = 0,
        SwingRejected = 1,
        SwingCompleted = 2,
        Hit = 3,
        Dodged = 4,
        Parried = 5,
        Blocked = 6,
        RiposteQueued = 7,
        ProcTriggered = 8,
        CleaveTriggered = 9,
        WardConsumed = 10,
        DamageShieldTriggered = 11
    }

    public struct MeleeTelemetryEvent : IBufferElementData
    {
        public MeleeTelemetryEventType EventType;
        public Entity Attacker;
        public Entity Target;
        public FixedString64Bytes WeaponSlot;
        public uint RequestId;
        public float Value0;
        public float Value1;
        public byte Flags;
    }
}
