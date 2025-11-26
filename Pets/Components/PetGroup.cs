using Unity.Collections;
using Unity.Entities;

namespace Framework.Pets.Components
{
    public struct PetGroup : IComponentData
    {
        public FixedString32Bytes Id;
        public byte SwarmLock;
    }
}
