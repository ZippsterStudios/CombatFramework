using Framework.Temporal.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Temporal.Drivers
{
    public static class TemporalAnchorDriver
    {
        private const float k_MinDuration = 0.1f;
        private const float k_DefaultDuration = 30f;
        private const float k_MinRetention = 0.1f;

        public static bool AttachAnchor(ref EntityManager em, in Entity target, in Entity source, float duration = k_DefaultDuration, float retention = -1f)
        {
            if (!em.Exists(target))
                return false;

            if (em.HasComponent<TemporalAnchor>(target))
                return false;

            var anchor = new TemporalAnchor
            {
                Source = source,
                Duration = math.max(k_MinDuration, duration <= 0f ? k_DefaultDuration : duration),
                Retention = retention <= 0f ? math.max(k_MinRetention, duration <= 0f ? k_DefaultDuration : duration) : math.max(k_MinRetention, retention),
                Elapsed = 0f,
                IsArmed = true
            };

            em.AddComponentData(target, anchor);
            EnsureEventBuffer(ref em, target).Clear();
            if (em.HasBuffer<TemporalReleaseRequest>(target))
                em.GetBuffer<TemporalReleaseRequest>(target).Clear();
            if (em.HasBuffer<TemporalReleaseResult>(target))
                em.GetBuffer<TemporalReleaseResult>(target).Clear();
            return true;
        }

        public static void RecordDamage(ref EntityManager em, in Entity target, float amount)
        {
            if (!em.Exists(target) || amount <= 0f)
                return;
            if (!em.HasComponent<TemporalAnchor>(target))
                return;

            var anchor = em.GetComponentData<TemporalAnchor>(target);
            if (!anchor.IsArmed)
                return;

            var events = EnsureEventBuffer(ref em, target);
            events.Add(new TemporalEvent
            {
                Timestamp = anchor.Elapsed,
                Type = TemporalEventType.Damage,
                Magnitude = amount
            });
        }

        public static void QueueRelease(ref EntityManager em, in Entity target, in Entity source, float factor, float windowSeconds, float healDuration, float healTickInterval)
        {
            if (!em.Exists(target) || !em.HasComponent<TemporalAnchor>(target))
                return;

            var anchor = em.GetComponentData<TemporalAnchor>(target);
            if (!anchor.IsArmed || (anchor.Source != Entity.Null && anchor.Source != source))
                return;

            var requests = em.HasBuffer<TemporalReleaseRequest>(target)
                ? em.GetBuffer<TemporalReleaseRequest>(target)
                : em.AddBuffer<TemporalReleaseRequest>(target);

            requests.Add(new TemporalReleaseRequest
            {
                Source = source,
                Factor = math.max(0f, factor),
                WindowSeconds = math.max(0f, windowSeconds),
                HealDuration = math.max(0f, healDuration),
                HealTickInterval = math.max(0f, healTickInterval)
            });

            anchor.IsArmed = false;
            em.SetComponentData(target, anchor);
        }

        public static void ClearAnchor(ref EntityManager em, in Entity target)
        {
            if (!em.Exists(target))
                return;

            if (em.HasComponent<TemporalAnchor>(target))
                em.RemoveComponent<TemporalAnchor>(target);
            if (em.HasBuffer<TemporalEvent>(target))
                em.RemoveComponent<TemporalEvent>(target);
            if (em.HasBuffer<TemporalReleaseRequest>(target))
                em.RemoveComponent<TemporalReleaseRequest>(target);
            // Keep TemporalReleaseResult buffer for downstream systems to consume.
        }

        private static DynamicBuffer<TemporalEvent> EnsureEventBuffer(ref EntityManager em, in Entity target)
        {
            return em.HasBuffer<TemporalEvent>(target)
                ? em.GetBuffer<TemporalEvent>(target)
                : em.AddBuffer<TemporalEvent>(target);
        }
    }
}
