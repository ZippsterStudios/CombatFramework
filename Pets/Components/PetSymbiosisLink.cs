using Framework.Pets.Content;
using Unity.Entities;

namespace Framework.Pets.Components
{
    public struct PetSymbiosisLink : IComponentData
    {
        public PetSymbiosisMode Mode;
        public Entity Owner;
        public float SplitPercent;
    }
}
