using Unity.Burst;
using Unity.Entities;

namespace Framework.Stats.Drivers
{
    [BurstCompile]
    public static class StatDriver
    {
        [BurstCompile]
        public static void AddAdditive(ref EntityManager em, in Entity e, int delta)
        {
            Factory.StatFactory.ApplyAdditive(ref em, e, delta);
        }

        [BurstCompile]
        public static void SetBase(ref EntityManager em, in Entity e, float baseValue)
        {
            Factory.StatFactory.SetBase(ref em, e, baseValue);
        }

        [BurstCompile]
        public static void SetMultiplier(ref EntityManager em, in Entity e, float multiplier)
        {
            Factory.StatFactory.SetMultiplier(ref em, e, multiplier);
        }
    }
}

