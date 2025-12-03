using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Framework.Damage.Components;

namespace Framework.Spells.TemporalImprint.Components
{
    public enum TimelineEventKind : byte
    {
        Damage = 0,
        Heal = 1,
        ApplyBuff = 2,
        ApplyDebuff = 3,
        Dot = 4,
        Hot = 5,
        SummonPet = 6,
        Script = 7
    }

    public enum TimelineEventState : byte
    {
        Pending = 0,
        Processed = 1
    }

    public struct TimelineEvent : IBufferElementData
    {
        public float Time;
        public TimelineEventKind Kind;
        public TimelineEventState State;
        public Entity Caster;
        public Entity Target;
        public FixedString64Bytes EffectId;
        public int Amount;
        public float Radius;
        public float TickInterval;
        public float Duration;
        public DamageSchool School;
        public float3 Position;
        // Optional per-event hitbox tuning
        public float HitboxScale;
        public byte AutoAim; // 1 = snap to target position instead of original hitbox
    }

    /// <summary>
    /// Added to an entity to mark an active recording window.
    /// </summary>
    public struct TemporalImprintRecorder : IComponentData
    {
        public double StartTime;
        public float Duration;
        public byte RecursionDepth;
    }

    /// <summary>
    /// Added to an entity representing a replay ghost/echo.
    /// </summary>
    public struct TemporalEcho : IComponentData
    {
        public double StartTime;
        public float ReplayDuration;
        public int Cursor; // index into the timeline buffer
        public byte RecursionDepth;
        // Modifiers applied to all replayed payloads
        public float DamageMultiplier;
        public float HealMultiplier;
        public float HitboxScale;
        public byte AutoAim; // 1 = snap hitboxes to current target positions
    }

    /// <summary>
    /// Optional health for an echo; when it reaches zero the remaining timeline backfires (inverted).
    /// </summary>
    public struct TemporalEchoHealth : IComponentData
    {
        public float Current;
        public float Max;
    }

    /// <summary>
    /// Blocks new temporal recordings on an entity until ExpireTime.
    /// </summary>
    public struct TemporalImprintSuppression : IComponentData
    {
        public double ExpireTime;
    }
}
