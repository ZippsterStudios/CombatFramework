using Framework.HOT.Factory;
using Framework.Resources.Factory;
using Framework.Temporal.Components;
using Framework.Temporal.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.HOT.Resolution
{
    [UpdateInGroup(typeof(Framework.Core.Base.ResolutionSystemGroup))]
    [UpdateAfter(typeof(Framework.Temporal.Runtime.TemporalReleaseSystem))]
    public partial struct TemporalReleaseHotSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            float elapsed = (float)SystemAPI.Time.ElapsedTime;

            using var entitiesToClear = new NativeList<Entity>(Allocator.Temp);

            foreach (var (results, entity) in SystemAPI.Query<DynamicBuffer<TemporalReleaseResult>>().WithEntityAccess())
            {
                if (results.Length == 0)
                    continue;

                for (int i = 0; i < results.Length; i++)
                {
                    var result = results[i];
                    float healAmount = math.max(0f, result.HealAmount);
                    if (healAmount <= 0f)
                        continue;

                    if (result.HealDuration <= 0f || result.HealTickInterval <= 0f)
                    {
                        ResourceFactory.ApplyHealthDelta(ref em, entity, (int)math.round(healAmount));
                        continue;
                    }

                    float duration = math.max(0.01f, result.HealDuration);
                    float interval = math.max(0.01f, result.HealTickInterval);
                    int tickCount = math.max(1, (int)math.ceil(duration / interval));
                    int healPerTick = (int)math.round(healAmount / tickCount);
                    if (healPerTick <= 0)
                        continue;

                    var effectId = (FixedString64Bytes)($"temporal-heal-{entity.Index}-{entity.Version}-{elapsed:0.00}-{i}");
                    HotFactory.Enqueue(ref em, entity, effectId, healPerTick, interval, duration, result.Source);
                }

                results.Clear();
                entitiesToClear.Add(entity);
            }

            for (int i = 0; i < entitiesToClear.Length; i++)
            {
                var e = entitiesToClear[i];
                if (em.Exists(e) && em.HasBuffer<TemporalReleaseResult>(e))
                    em.RemoveComponent<TemporalReleaseResult>(e);
            }
        }
    }
}
