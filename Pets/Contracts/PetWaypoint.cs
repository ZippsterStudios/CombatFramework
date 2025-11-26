using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Pets.Contracts
{
    public struct PetWaypoint : IBufferElementData
    {
        public float3 Value;
    }
}
