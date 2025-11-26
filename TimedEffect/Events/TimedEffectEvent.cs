using Framework.TimedEffect.Content;
using Unity.Collections;
using Unity.Entities;

namespace Framework.TimedEffect.Events
{
    public enum TimedEffectEventKind : byte
    {
        Added,
        Refreshed,
        Removed,
        StackChanged,
        Tick
    }

    public struct TimedEffectEvent : IBufferElementData
    {
        public TimedEffectEventKind Kind;
        public Entity Target;
        public Entity Source;
        public FixedString64Bytes EffectId;
        public TimedEffectType Type;
        public FixedString32Bytes CategoryId;
        public int CategoryLevel;
        public int StackCount;
        public int StackDelta;
        public int TickCount;
    }
}

