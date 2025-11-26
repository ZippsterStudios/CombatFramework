using Framework.HOT.Components;
using Framework.TimedEffect.Content;
using Framework.TimedEffect.Requests;
using Unity.Collections;
using Unity.Entities;

namespace Framework.HOT.Factory
{
    public static class HotFactory
    {
        public static void Enqueue(ref EntityManager em, in Entity target, in FixedString64Bytes id, int hps, float interval, float duration, in Entity source)
        {
            Enqueue(ref em, target, id, hps, interval, duration, source, default, 0, 0);
        }

        public static void Enqueue(ref EntityManager em, in Entity target, in FixedString64Bytes id, int hps, float interval, float duration, in Entity source, in FixedString32Bytes categoryIdOverride, int categoryLevelOverride, int stackableCountOverride)
        {
            if (!em.HasBuffer<TimedEffectRequest>(target))
                em.AddBuffer<TimedEffectRequest>(target);
            var requests = em.GetBuffer<TimedEffectRequest>(target);
            requests.Add(new TimedEffectRequest
            {
                Target = target,
                EffectId = id,
                Type = TimedEffectType.HealOverTime,
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

            if (!em.HasBuffer<HotInstance>(target))
                em.AddBuffer<HotInstance>(target);
            var hots = em.GetBuffer<HotInstance>(target);
            int index = IndexOf(hots, id);
            var instance = new HotInstance
            {
                EffectId = id,
                HealPerTick = hps,
                Source = source
            };

            if (index >= 0)
                hots[index] = instance;
            else
                hots.Add(instance);
        }

        private static int IndexOf(DynamicBuffer<HotInstance> buffer, in FixedString64Bytes id)
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
