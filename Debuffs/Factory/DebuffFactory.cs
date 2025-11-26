using Framework.Debuffs.Content;
using Framework.Debuffs.Drivers;
using Framework.Debuffs.Requests;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Debuffs.Factory
{
    public static class DebuffFactory
    {
        public static void EnsureRequestBuffer(ref EntityManager em, in Entity e)
        {
            if (!em.HasBuffer<DebuffRequest>(e))
                em.AddBuffer<DebuffRequest>(e);
        }

        public static void Enqueue(ref EntityManager em, in Entity target, in FixedString64Bytes debuffId,
            float durationOverride = 0f, int stacks = 1, in Entity source = default,
            DebuffFlags extraFlags = DebuffFlags.None)
        {
            if (!em.Exists(target)) return;
            EnsureRequestBuffer(ref em, target);
            var buf = em.GetBuffer<DebuffRequest>(target);
            buf.Add(new DebuffRequest
            {
                Target = target,
                Source = source,
                DebuffId = debuffId,
                AddStacks = stacks < 1 ? 1 : stacks,
                DurationOverride = durationOverride,
                ExtraFlags = extraFlags
            });
        }

        public static void ApplyImmediate(ref EntityManager em, in Entity target, in FixedString64Bytes debuffId,
            float durationOverride = 0f, int stacks = 1, in Entity source = default,
            DebuffFlags extraFlags = DebuffFlags.None)
        {
            DebuffDriver.Apply(ref em, target, source, debuffId, durationOverride, stacks, extraFlags);
        }
    }
}
