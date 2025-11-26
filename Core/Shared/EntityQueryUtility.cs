using Unity.Entities;

namespace Framework.Core.Shared
{
    public static class EntityQueryUtility
    {
        public static bool HasComponent<T>(in EntityManager em, in Entity e) where T : unmanaged, IComponentData
            => em.HasComponent<T>(e);
    }
}

