using Unity.Collections;
using Unity.Entities;

namespace Framework.Buffs.Requests
{
    public struct BuffRequest : IBufferElementData
    {
        public Entity Target;
        public FixedString64Bytes BuffId;
        public int AddStacks;
        public float AddDuration;
    }
}

