using Unity.Entities;

namespace Framework.Shadow.Components
{
    public struct ShadowRegionRef : IBufferElementData
    {
        public Entity Region;
    }
}
