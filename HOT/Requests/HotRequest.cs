using Unity.Collections;
using Unity.Entities;

namespace Framework.HOT.Requests
{
    public struct HotRequest : IBufferElementData
    {
        public Entity Target;
        public FixedString64Bytes EffectId;
        public int Hps;
        public float TickInterval;
        public float Duration;
        public Entity Source;
    }
}
