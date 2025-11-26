using Unity.Collections;
using Unity.Entities;

namespace Framework.Pets.Components
{
    public struct PetLifetimeTag : IComponentData
    {
        public FixedString64Bytes EffectId;
        public float DefaultDurationSeconds;
    }
}
