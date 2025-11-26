using Framework.Lifecycle.Components;
using Framework.Resources.Components;
using Framework.Resources.Factory;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Lifecycle.Drivers
{
    [BurstCompile]
    public static class LifecycleDriver
    {
        [BurstCompile]
        public static void Kill(ref EntityManager em, in Entity entity)
        {
            if (!em.Exists(entity))
                return;

            if (em.HasComponent<Health>(entity))
            {
                var health = em.GetComponentData<Health>(entity);
                if (health.Current > 0)
                {
                    health.Current = 0;
                    em.SetComponentData(entity, health);
                }
            }
            else
            {
                ResourceFactory.InitHealth(ref em, entity, max: 0, current: 0, regenPerSecond: 0);
            }
        }

        [BurstCompile]
        public static void Revive(ref EntityManager em, in Entity entity, int newHealth)
        {
            if (!em.Exists(entity))
                return;

            if (em.HasComponent<Dead>(entity))
                em.RemoveComponent<Dead>(entity);

            if (!em.HasComponent<Health>(entity))
            {
                ResourceFactory.InitHealth(ref em, entity, math.max(1, newHealth), math.max(1, newHealth));
            }
            else
            {
                var health = em.GetComponentData<Health>(entity);
                int clamped = math.max(1, newHealth);
                health.Current = math.min(clamped, health.Max > 0 ? health.Max : clamped);
                if (health.Max < health.Current)
                    health.Max = health.Current;
                em.SetComponentData(entity, health);
            }

            if (em.HasComponent<DeadTimer>(entity))
                em.RemoveComponent<DeadTimer>(entity);
        }
    }
}
