using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Pets.Components
{
    public struct PetLeashConfigShim : IComponentData
    {
        public float2 Home;
        public float Radius;
        public float SoftRadius;
        public byte TeleportOnBreach;
    }
}
