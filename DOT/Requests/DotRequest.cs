using Unity.Collections;
using Unity.Entities;

namespace Framework.DOT.Requests
{
    public struct DotRequest : IBufferElementData
    {
        public Entity Target;
        public FixedString64Bytes EffectId;
        public int Dps;
        public float TickInterval;
        public float Duration;
        public Entity Source;
    }
}
