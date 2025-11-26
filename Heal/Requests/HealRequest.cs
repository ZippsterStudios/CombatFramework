using Unity.Entities;

namespace Framework.Heal.Requests
{
    public struct HealRequest : IBufferElementData
    {
        public Entity Target;
        public int Amount;
    }
}

