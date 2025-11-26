using System;
using Framework.Spells.Content;
using Unity.Collections;
using Unity.Mathematics;

namespace Framework.Spells.Sustain
{
    public static class SustainedDrainUtility
    {
        private const float MinInterval = 0.05f;

        public static SustainedSpellDrain BuildEntry(in FixedString64Bytes spellId, in FixedString64Bytes resourceId, float drainPerSecond, float tickIntervalSeconds, double now, bool tickImmediately, in FixedString64Bytes defaultResource)
        {
            var interval = math.max(MinInterval, tickIntervalSeconds);
            var amountPerTick = (int)System.Math.Ceiling(System.Math.Max(0f, drainPerSecond) * interval);
            var resource = resourceId.Length == 0 ? defaultResource : resourceId;
            var next = tickImmediately ? now : now + interval;

            return new SustainedSpellDrain
            {
                SpellId = spellId,
                ResourceId = resource,
                AmountPerTick = amountPerTick,
                TickInterval = interval,
                NextTickTime = next
            };
        }

        public static bool Normalize(ref SustainedSpellDrain drain, in FixedString64Bytes defaultResource)
        {
            if (drain.AmountPerTick <= 0)
                return false;

            if (drain.ResourceId.Length == 0)
                drain.ResourceId = defaultResource;

            if (drain.TickInterval < MinInterval)
                drain.TickInterval = MinInterval;

            return true;
        }

        /// <summary>
        /// Processes a single drain tick if due. Returns false when the entry should be removed (cannot afford or invalid).
        /// </summary>
        public static bool TryProcess(ref SustainedSpellDrain drain, double now, in FixedString64Bytes defaultResource, Func<SpellCost, bool> canAfford, Action<SpellCost> spend)
        {
            if (!Normalize(ref drain, defaultResource))
                return false;

            if (drain.NextTickTime > now)
                return true; // keep, not yet time

            var cost = new SpellCost
            {
                Resource = drain.ResourceId,
                Amount = drain.AmountPerTick
            };

            if (canAfford != null && !canAfford(cost))
                return false;

            spend?.Invoke(cost);
            drain.NextTickTime = now + drain.TickInterval;
            return true;
        }
    }
}
