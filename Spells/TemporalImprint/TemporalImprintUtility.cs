using Unity.Collections;
using Unity.Entities;
using Framework.Spells.TemporalImprint.Components;

namespace Framework.Spells.TemporalImprint
{
    /// <summary>
    /// Helper methods for starting/stopping recordings and replays.
    /// Callers (e.g., a spell) can invoke these to drive the imprint flow.
    /// </summary>
    public static class TemporalImprintUtility
    {
        public static void StartRecording(ref EntityManager em, in Entity target, float duration, byte recursionDepth = 0)
        {
            if (!em.Exists(target))
                return;

            double now = em.World.Time.ElapsedTime;
            if (em.HasComponent<Components.TemporalImprintSuppression>(target))
            {
                var sup = em.GetComponentData<Components.TemporalImprintSuppression>(target);
                if (now < sup.ExpireTime)
                    return;
            }

            var recorder = new TemporalImprintRecorder
            {
                StartTime = em.World.Time.ElapsedTime,
                Duration = duration,
                RecursionDepth = recursionDepth
            };
            if (!em.HasComponent<TemporalImprintRecorder>(target))
                em.AddComponentData(target, recorder);
            else
                em.SetComponentData(target, recorder);

            if (!em.HasBuffer<TimelineEvent>(target))
                em.AddBuffer<TimelineEvent>(target).Clear();
            else
                em.GetBuffer<TimelineEvent>(target).Clear();
        }

        public static Entity SpawnEcho(ref EntityManager em, in Entity source, float replayDuration, byte recursionDepth = 0)
        {
            if (!em.Exists(source) || !em.HasBuffer<TimelineEvent>(source))
                return Entity.Null;

            var echo = em.CreateEntity();
            var sourceTimeline = em.GetBuffer<TimelineEvent>(source);
            var echoTimeline = em.AddBuffer<TimelineEvent>(echo);
            for (int i = 0; i < sourceTimeline.Length; i++)
                echoTimeline.Add(sourceTimeline[i]);

            var echoData = new TemporalEcho
            {
                StartTime = em.World.Time.ElapsedTime,
                ReplayDuration = replayDuration,
                Cursor = 0,
                RecursionDepth = recursionDepth,
                DamageMultiplier = 1f,
                HealMultiplier = 1f,
                HitboxScale = 1f,
                AutoAim = 0
            };
            em.AddComponentData(echo, echoData);
            em.AddComponentData(echo, new Components.TemporalEchoHealth { Current = 1f, Max = 1f });
            return echo;
        }

        public static void AppendEvent(ref EntityManager em, in Entity target, in TimelineEvent evt)
        {
            if (!em.Exists(target))
                return;
            if (!em.HasBuffer<TimelineEvent>(target))
                em.AddBuffer<TimelineEvent>(target);
            em.GetBuffer<TimelineEvent>(target).Add(evt);
        }
    }
}
