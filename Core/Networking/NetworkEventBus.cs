using Unity.Collections;
using Unity.Entities;

namespace Framework.Core.Networking
{
    // Lightweight event bus placeholder for network-relevant events (deterministic payloads only).
    public struct NetworkEvent : IBufferElementData
    {
        public FixedString64Bytes Tag;
        public int Payload;
    }

    public static class NetworkEventBus
    {
        public static void EnsureBuffer(EntityManager em, Entity e)
        {
            if (!em.HasBuffer<NetworkEvent>(e)) em.AddBuffer<NetworkEvent>(e);
        }
    }
}

