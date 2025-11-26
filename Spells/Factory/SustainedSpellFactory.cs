using Framework.Spells.Content;
using Framework.Spells.Sustain;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Spells.Factory
{
    /// <summary>
    /// Helpers for spells that stay active and drain resources until explicitly stopped.
    /// </summary>
    public static class SustainedSpellFactory
    {
        private static readonly FixedString64Bytes DefaultResource = CreateDefaultResource();

        /// <summary>
        /// Enqueue the spell cast and register a sustain drain on the caster. The drain starts immediately by default.
        /// </summary>
        public static void CastWithSustain(ref EntityManager em, in Entity caster, in Entity target, in FixedString64Bytes spellId, float drainPerSecond, float tickIntervalSeconds = 1f, bool tickImmediately = true, int power = 0, FixedString64Bytes resourceId = default)
        {
            SpellPipelineFactory.Cast(ref em, caster, target, spellId, power);
            AddOrRefreshDrain(ref em, caster, spellId, drainPerSecond, tickIntervalSeconds, tickImmediately, resourceId);
        }

        /// <summary>
        /// Register or refresh a sustain drain entry without enqueuing a cast (useful for buff-driven toggles).
        /// </summary>
        public static void AddOrRefreshDrain(ref EntityManager em, in Entity caster, in FixedString64Bytes spellId, float drainPerSecond, float tickIntervalSeconds = 1f, bool tickImmediately = true, FixedString64Bytes resourceId = default)
        {
            var normalizedResource = resourceId;
            if (normalizedResource.Length == 0)
                normalizedResource = DefaultResource;

            if (!em.HasBuffer<SustainedSpellDrain>(caster))
                em.AddBuffer<SustainedSpellDrain>(caster);

            var drains = em.GetBuffer<SustainedSpellDrain>(caster);
            double now = em.WorldUnmanaged.Time.ElapsedTime;
            var entry = SustainedDrainUtility.BuildEntry(
                spellId,
                normalizedResource,
                drainPerSecond,
                tickIntervalSeconds,
                now,
                tickImmediately,
                DefaultResource);

            if (entry.AmountPerTick <= 0)
                return;

            for (int i = 0; i < drains.Length; i++)
            {
                if (drains[i].SpellId.Equals(spellId))
                {
                    drains[i] = entry;
                    return;
                }
            }

            drains.Add(entry);
        }

        /// <summary>
        /// Remove a sustain drain entry, effectively deactivating the ongoing drain.
        /// </summary>
        public static void Stop(ref EntityManager em, in Entity caster, in FixedString64Bytes spellId)
        {
            if (!em.HasBuffer<SustainedSpellDrain>(caster))
                return;

            var drains = em.GetBuffer<SustainedSpellDrain>(caster);
            for (int i = 0; i < drains.Length; i++)
            {
                if (drains[i].SpellId.Equals(spellId))
                {
                    drains.RemoveAt(i);
                    return;
                }
            }
        }

        private static FixedString64Bytes CreateDefaultResource()
        {
            FixedString64Bytes value = default;
            value.Append("Mana");
            return value;
        }
    }
}
