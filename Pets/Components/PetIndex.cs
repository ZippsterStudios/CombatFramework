using Unity.Collections;
using Unity.Entities;

namespace Framework.Pets.Components
{
    public struct PetIndex : IBufferElementData
    {
        public Entity Pet;
        public FixedString64Bytes PetId;
        public FixedString32Bytes GroupId;
        public int Sequence;
        public byte SwarmLock;
    }
}
