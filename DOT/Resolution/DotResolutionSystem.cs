using Framework.Core.Telemetry;
using Framework.DOT.Components;
using Framework.Damage.Components;
using Framework.Damage.Policies;
using Framework.Resources.Components;
using Framework.TimedEffect.Content;
using Framework.TimedEffect.Events;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.DOT.Resolution
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.ResolutionSystemGroup))]
    public partial struct DotResolutionSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var query = SystemAPI.QueryBuilder()
                                 .WithAll<TimedEffectEvent, DotInstance>()
                                 .Build();

            using var entities = query.ToEntityArray(Allocator.Temp);
            bool telemetryEnabled = TelemetryRouter.IsEnabled();

            for (int idx = 0; idx < entities.Length; idx++)
            {
                var entity = entities[idx];
                if (!em.HasComponent<Health>(entity))
                    continue;

                var events = em.GetBuffer<TimedEffectEvent>(entity);
                if (events.Length == 0)
                    continue;

                var dots = em.GetBuffer<DotInstance>(entity);
                if (dots.Length == 0)
                    continue;

                int totalDamage = 0;

                for (int i = events.Length - 1; i >= 0; i--)
                {
                    var evt = events[i];
                    if (evt.Type != TimedEffectType.DamageOverTime)
                        continue;

                    switch (evt.Kind)
                    {
                        case TimedEffectEventKind.Tick:
                        {
                            int dotIndex = IndexOf(dots, evt.EffectId);
                            if (dotIndex < 0)
                            {
                                events.RemoveAt(i);
                                continue;
                            }

                            var dot = dots[dotIndex];
                            int stacks = math.max(1, evt.StackCount);
                            int ticks = math.max(1, evt.TickCount);
                            int damage = dot.DamagePerTick * stacks * ticks;
                            if (damage > 0)
                                totalDamage += damage;

                            events.RemoveAt(i);
                            break;
                        }
                        case TimedEffectEventKind.Removed:
                        {
                            RemoveDot(ref dots, evt.EffectId);
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

                if (totalDamage <= 0)
                {
                    if (dots.Length == 0)
                        em.RemoveComponent<DotInstance>(entity);
                    continue;
                }

                var health = em.GetComponentData<Health>(entity);
                int mitigated = totalDamage;
                if (em.HasComponent<Damageable>(entity))
                {
                    var dmg = em.GetComponentData<Damageable>(entity);
                    mitigated = DamagePolicy.Mitigate(totalDamage, dmg.Armor, dmg.ResistPercent);
                }

                if (mitigated > 0)
                {
                    long next = (long)health.Current - mitigated;
                    if (next < 0) next = 0;
                    health.Current = (int)next;
                    em.SetComponentData(entity, health);

                    if (telemetryEnabled)
                    {
                        // TODO: hook up telemetry stream for DOT ticks when available.
                    }
                }

                if (dots.Length == 0)
                    em.RemoveComponent<DotInstance>(entity);
            }
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

        private static void RemoveDot(ref DynamicBuffer<DotInstance> buffer, in FixedString64Bytes id)
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
