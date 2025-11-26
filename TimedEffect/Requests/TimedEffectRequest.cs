using Framework.TimedEffect.Content;
using Unity.Collections;
using Unity.Entities;

namespace Framework.TimedEffect.Requests
{
    public struct TimedEffectRequest : IBufferElementData
    {
        public Entity Target;
        public FixedString64Bytes EffectId;
        public TimedEffectType Type;
        public TimedEffectStackingMode StackingMode;
        public FixedString32Bytes CategoryId;
        public int CategoryLevel;
        public int StackableCount;
        public int AddStacks;
        public int MaxStacks;
        public float Duration;
        public float TickInterval;
        public Entity Source;
    }
}

