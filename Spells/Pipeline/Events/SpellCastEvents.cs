using Unity.Collections;
using Unity.Entities;

namespace Framework.Spells.Pipeline.Events
{
    public struct SpellCastEventPayload : IBufferElementData
    {
        public Entity CastEntity;
        public Entity Caster;
        public Entity Target;
        public FixedString64Bytes SpellId;
        public FixedString64Bytes Reason;
        public float Value;
    }

    public struct SpellBeganEvent : IComponentData { }
    public struct SpellInterruptedEvent : IComponentData { }
    public struct SpellFizzledEvent : IComponentData { }
    public struct SpellResolvedEvent : IComponentData { }
}
