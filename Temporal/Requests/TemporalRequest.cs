using Unity.Entities;

namespace Framework.Temporal.Requests
{
    public struct TemporalRequest : IBufferElementData
    {
        public Entity Target;
        public float DeltaTime;
    }
}

