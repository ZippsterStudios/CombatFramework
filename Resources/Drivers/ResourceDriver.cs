using Unity.Burst;
using Unity.Entities;

namespace Framework.Resources.Drivers
{
    [BurstCompile]
    public static class ResourceDriver
    {
        [BurstCompile]
        public static void ApplyHealthDelta(ref EntityManager em, in Entity e, int delta)
        {
            Factory.ResourceFactory.ApplyHealthDelta(ref em, e, delta);
        }

        [BurstCompile]
        public static void ApplyManaDelta(ref EntityManager em, in Entity e, int delta)
        {
            Factory.ResourceFactory.ApplyManaDelta(ref em, e, delta);
        }

        [BurstCompile]
        public static void ApplyStaminaDelta(ref EntityManager em, in Entity e, int delta)
        {
            Factory.ResourceFactory.ApplyStaminaDelta(ref em, e, delta);
        }
    }
}

