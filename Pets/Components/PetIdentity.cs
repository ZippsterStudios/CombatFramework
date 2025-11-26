using Unity.Collections;
using Unity.Entities;

namespace Framework.Pets.Components
{
    public struct PetIdentity : IComponentData
    {
        public FixedString64Bytes PetId;
        public FixedString32Bytes CategoryId;
        public int CategoryLevel;
    }
}
