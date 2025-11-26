using Framework.DOT.Components;
using Framework.TimedEffect.Content;
using Framework.TimedEffect.Requests;
using Unity.Collections;
using Unity.Entities;

namespace Framework.DOT.Factory
{
    public static class DotFactory
    {
        public static void Enqueue(ref EntityManager em, in Entity target, in FixedString64Bytes id, int dps, float interval, float duration, in Entity source)
        {
            Enqueue(ref em, target, id, dps, interval, duration, source, default, 0, 0);
        }

        public static void Enqueue(ref EntityManager em, in Entity target, in FixedString64Bytes id, int dps, float interval, float duration, in Entity source, in FixedString32Bytes categoryIdOverride, int categoryLevelOverride, int stackableCountOverride)
        {
            if (!em.HasBuffer<TimedEffectRequest>(target))
                em.AddBuffer<TimedEffectRequest>(target);
            var requests = em.GetBuffer<TimedEffectRequest>(target);
            requests.Add(new TimedEffectRequest
            {
                Target = target,
                EffectId = id,
                Type = TimedEffectType.DamageOverTime,
                StackingMode = TimedEffectStackingMode.AddStacks,
                CategoryId = categoryIdOverride,
                CategoryLevel = categoryLevelOverride,
                StackableCount = stackableCountOverride > 0 ? stackableCountOverride : 1,
                AddStacks = 1,
                MaxStacks = 0,
                Duration = duration,
                TickInterval = interval,
                Source = source
            });

            if (!em.HasBuffer<DotInstance>(target))
                em.AddBuffer<DotInstance>(target);
            var dots = em.GetBuffer<DotInstance>(target);
            int index = IndexOf(dots, id);
            var instance = new DotInstance
            {
                EffectId = id,
                DamagePerTick = dps,
                Source = source
            };

            if (index >= 0)
                dots[index] = instance;
            else
                dots.Add(instance);
        }

        private static int IndexOf(DynamicBuffer<DotInstance> buffer, in FixedString64Bytes id)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].EffectId.Equals(id))
                    return i;
            }
            return -1;
        }
    }
}
