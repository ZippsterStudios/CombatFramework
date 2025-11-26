using Framework.Temporal.Components;
using Framework.Temporal.Drivers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Temporal.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.ResolutionSystemGroup))]
    public partial struct TemporalReleaseSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;

            foreach (var (anchorRW, requests, entity) in SystemAPI
                     .Query<RefRW<TemporalAnchor>, DynamicBuffer<TemporalReleaseRequest>>()
                     .WithEntityAccess())
            {
                if (requests.Length == 0)
                    continue;

                var anchor = anchorRW.ValueRW;
                var events = em.HasBuffer<TemporalEvent>(entity)
                    ? em.GetBuffer<TemporalEvent>(entity)
                    : default;

                if (!events.IsCreated || events.Length == 0)
                {
                    requests.Clear();
                    TemporalAnchorDriver.ClearAnchor(ref em, entity);
                    continue;
                }

                var results = em.HasBuffer<TemporalReleaseResult>(entity)
                    ? em.GetBuffer<TemporalReleaseResult>(entity)
                    : em.AddBuffer<TemporalReleaseResult>(entity);

                for (int i = 0; i < requests.Length; i++)
                {
                    var request = requests[i];
                    float window = request.WindowSeconds > 0f ? request.WindowSeconds : anchor.Duration;
                    float minTime = math.max(0f, anchor.Elapsed - window);
                    float damage = SumDamage(events, minTime, anchor.Elapsed);
                    float heal = damage * math.max(0f, request.Factor);
                    if (heal <= 0f)
                        continue;

                    results.Add(new TemporalReleaseResult
                    {
                        Source = request.Source,
                        HealAmount = heal,
                        HealDuration = math.max(0f, request.HealDuration),
                        HealTickInterval = math.max(0f, request.HealTickInterval)
                    });
                }

                requests.Clear();
                TemporalAnchorDriver.ClearAnchor(ref em, entity);
            }
        }

        private static float SumDamage(in DynamicBuffer<TemporalEvent> events, float minTime, float currentTime)
        {
            float total = 0f;
            for (int i = events.Length - 1; i >= 0; i--)
            {
                var evt = events[i];
                if (evt.Timestamp < minTime)
                    break;
                if (evt.Timestamp > currentTime)
                    continue;
                if (evt.Type != TemporalEventType.Damage)
                    continue;
                total += math.max(0f, evt.Magnitude);
            }
            return total;
        }
    }
}

