using Framework.HOT.Components;
using Framework.Resources.Components;
using Framework.TimedEffect.Content;
using Framework.TimedEffect.Events;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.HOT.Resolution
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.ResolutionSystemGroup))]
    public partial struct HotResolutionSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var query = SystemAPI.QueryBuilder()
                                 .WithAll<TimedEffectEvent, HotInstance>()
                                 .Build();

            using var entities = query.ToEntityArray(Allocator.Temp);

            for (int idx = 0; idx < entities.Length; idx++)
            {
                var entity = entities[idx];
                if (!em.HasComponent<Health>(entity))
                    continue;

                var events = em.GetBuffer<TimedEffectEvent>(entity);
                if (events.Length == 0)
                    continue;

                var hots = em.GetBuffer<HotInstance>(entity);
                if (hots.Length == 0)
                    continue;

                int totalHeal = 0;

                for (int i = events.Length - 1; i >= 0; i--)
                {
                    var evt = events[i];
                    if (evt.Type != TimedEffectType.HealOverTime)
                        continue;

                    switch (evt.Kind)
                    {
                        case TimedEffectEventKind.Tick:
                        {
                            int hotIndex = IndexOf(hots, evt.EffectId);
                            if (hotIndex < 0)
                            {
                                events.RemoveAt(i);
                                continue;
                            }

                            var hot = hots[hotIndex];
                            int stacks = math.max(1, evt.StackCount);
                            int ticks = math.max(1, evt.TickCount);
                            int heal = hot.HealPerTick * stacks * ticks;
                            if (heal > 0)
                                totalHeal += heal;

                            events.RemoveAt(i);
                            break;
                        }
                        case TimedEffectEventKind.Removed:
                        {
                            RemoveHot(ref hots, evt.EffectId);
                            events.RemoveAt(i);
                            break;
                        }
                        case TimedEffectEventKind.Added:
                        case TimedEffectEventKind.StackChanged:
                        case TimedEffectEventKind.Refreshed:
                            events.RemoveAt(i);
                            break;
                        default:
                            events.RemoveAt(i);
                            break;
                    }
                }

                if (totalHeal <= 0)
                {
                    if (hots.Length == 0)
                        em.RemoveComponent<HotInstance>(entity);
                    continue;
                }

                var health = em.GetComponentData<Health>(entity);
                long next = (long)health.Current + totalHeal;
                if (next > health.Max) next = health.Max;
                health.Current = (int)next;
                em.SetComponentData(entity, health);

                if (hots.Length == 0)
                    em.RemoveComponent<HotInstance>(entity);
            }
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

        private static void RemoveHot(ref DynamicBuffer<HotInstance> buffer, in FixedString64Bytes id)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].EffectId.Equals(id))
                {
                    buffer.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
