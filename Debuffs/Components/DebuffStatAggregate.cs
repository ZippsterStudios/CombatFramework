using Unity.Collections;
using Unity.Entities;

namespace Framework.Debuffs.Components
{
    public struct DebuffStatAggregate : IBufferElementData
    {
        public FixedString32Bytes StatId;
        public float Additive;
        public float Multiplier;
    }
}
