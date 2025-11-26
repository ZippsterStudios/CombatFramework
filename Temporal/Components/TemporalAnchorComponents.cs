using Unity.Collections;
using Unity.Entities;

namespace Framework.Temporal.Components
{
    public struct TemporalAnchor : IComponentData
    {
        public Entity Source;
        public float Duration;
        public float Retention;
        public float Elapsed;
        public bool IsArmed;
    }

    public struct TemporalEvent : IBufferElementData
    {
        public float Timestamp;
        public TemporalEventType Type;
        public float Magnitude;
    }

    public struct TemporalReleaseRequest : IBufferElementData
    {
        public Entity Source;
        public float Factor;
        public float WindowSeconds;
        public float HealDuration;
        public float HealTickInterval;
    }

    public struct TemporalReleaseResult : IBufferElementData
    {
        public Entity Source;
        public float HealAmount;
        public float HealDuration;
        public float HealTickInterval;
    }

    public enum TemporalEventType : byte
    {
        Damage = 0,
        Heal = 1
    }
}

