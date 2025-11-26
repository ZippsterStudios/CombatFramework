using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Shadow.Components
{
    public struct ShadowRegionHighlight : IComponentData
    {
        public float4 Color;
        public float Intensity;
        public byte Enabled;
    }
}
