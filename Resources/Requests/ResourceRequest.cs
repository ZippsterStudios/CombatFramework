using Unity.Entities;

namespace Framework.Resources.Requests
{
    public enum ResourceKind : byte { Health = 0, Mana = 1, Stamina = 2 }

    public struct ResourceRequest : IBufferElementData
    {
        public Entity Target;
        public ResourceKind Kind;
        public int Delta;
    }
}
