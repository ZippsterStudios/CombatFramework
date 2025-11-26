using Framework.TimedEffect.Content;
using Unity.Collections;
using Unity.Entities;

namespace Framework.TimedEffect.Components
{
    public struct TimedEffectInstance : IBufferElementData
    {
        public FixedString64Bytes EffectId;
        public TimedEffectType Type;
        public TimedEffectStackingMode StackingMode;
        public FixedString32Bytes CategoryId;
        public int CategoryLevel;
        public int StackableCount;
        public int StackCount;
        public int MaxStacks;
        public float Duration;
        public float TimeRemaining;
        public float TickInterval;
        public float TimeUntilTick;
        public Entity Source;
    }
}

