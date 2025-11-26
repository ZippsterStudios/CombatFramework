using Framework.Pets.Content;
using Unity.Entities;

namespace Framework.Pets.Components
{
    public struct PetSymbiosisParticipant : IBufferElementData
    {
        public Entity Pet;
        public PetSymbiosisMode Mode;
        public float SplitPercent;
    }
}
