using System;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Spells.Factory
{
    /// <summary>
    /// Convenience helpers so gameplay code can trigger spellcasts with a single call
    /// (e.g. <c>caster.Cast(ref em, target, "fireball")</c>).
    /// </summary>
    public static class SpellCastExtensions
    {
        /// <summary>
        /// Queue a spellcast request for the given caster/target pair.
        /// </summary>
        public static void Cast(this Entity caster, ref EntityManager em, in Entity target, in FixedString64Bytes spellId, int power = 0)
        {
            SpellRequestFactory.EnqueueCast(ref em, caster, target, spellId, power);
        }

        /// <summary>
        /// Queue a spellcast request using a string id. The id is trimmed and lower-cased
        /// to match the catalog convention.
        /// </summary>
        public static void Cast(this Entity caster, ref EntityManager em, in Entity target, string spellId, int power = 0)
        {
            if (string.IsNullOrWhiteSpace(spellId))
                throw new ArgumentException("Spell id must be a non-empty string.", nameof(spellId));

            var normalized = spellId.Trim().ToLowerInvariant();
            var fixedId = new FixedString64Bytes(normalized);
            SpellRequestFactory.EnqueueCast(ref em, caster, target, fixedId, power);
        }

        /// <summary>
        /// Queue a spellcast that targets the caster.
        /// </summary>
        public static void CastSelf(this Entity caster, ref EntityManager em, in FixedString64Bytes spellId, int power = 0)
        {
            SpellRequestFactory.EnqueueCast(ref em, caster, caster, spellId, power);
        }

        /// <summary>
        /// Queue a spellcast that targets the caster using a string id.
        /// </summary>
        public static void CastSelf(this Entity caster, ref EntityManager em, string spellId, int power = 0)
        {
            caster.Cast(ref em, caster, spellId, power);
        }
    }
}
