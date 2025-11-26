using Unity.Entities;

namespace Framework.Stats.Requests
{
    public struct StatRequest : IBufferElementData
    {
        public Entity Target;
        public int Delta;
    }
}

