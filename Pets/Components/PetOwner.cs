using Unity.Entities;

namespace Framework.Pets.Components
{
    public struct PetOwner : IComponentData
    {
        public Entity Value;
    }
}
