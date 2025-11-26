using Unity.Entities;

namespace Framework.Damage.Requests
{
    public struct DamageRequest : IBufferElementData
    {
        public Entity Target;
        public Framework.Damage.Components.DamagePacket Packet;
    }
}
