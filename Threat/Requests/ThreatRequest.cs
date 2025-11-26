using Unity.Entities;

namespace Framework.Threat.Requests
{
    public struct ThreatRequest : IBufferElementData
    {
        public Entity Target;
        public int Delta;
    }
}

