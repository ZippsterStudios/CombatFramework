using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace Framework.AreaEffects.Requests
{
    public struct AreaEffectRequest : IBufferElementData
    {
        public FixedString64Bytes Id;
        public float2 Origin;
        public float Radius;
        public float LifetimeSeconds;
    }
}
