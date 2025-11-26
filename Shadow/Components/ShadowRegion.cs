using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Shadow.Components
{
    /// <summary>
    /// Represents a spatial region that shadow abilities can target or occupy.
    /// </summary>
    public struct ShadowRegion : IComponentData
    {
        public FixedString64Bytes Id;
        public Entity Owner; // optional, can be Entity.Null
        public float3 Center;
        public float Radius;
        public byte Enabled;
        public byte Team;
    }
}
