using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Pets.Components
{
    public struct PetGuardAnchor : IComponentData
    {
        public float2 Position;
        public Entity AnchorEntity;
    }
}
