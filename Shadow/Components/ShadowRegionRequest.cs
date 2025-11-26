using Unity.Entities;

namespace Framework.Shadow.Components
{
    public struct ShadowRegisterRequest : IBufferElementData
    {
        public Entity Region;
    }

    public struct ShadowUnregisterRequest : IBufferElementData
    {
        public Entity Region;
    }
}
