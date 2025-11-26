using Framework.Spells.Content;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Spells.Runtime
{
    internal static class EffectConditionEvaluator
    {
        public static bool ShouldApply(ref EntityManager em, in EffectConditions conditions, in Entity caster, in Entity target, uint seed, int blockIndex)
        {
            if (!EvaluateChance(conditions.ChancePercent, seed, blockIndex, target))
                return false;
            if (conditions.RequireLineOfSight != 0 && !EffectLineOfSight.HasLineOfSight(ref em, caster, target))
                return false;
            if (!ValidateTag(ref em, target, conditions.RequiresTag, true))
                return false;
            if (!ValidateTag(ref em, target, conditions.TargetMustHaveTag, true))
                return false;
            if (!ValidateTag(ref em, target, conditions.TargetMustNotHaveTag, false))
                return false;
            if (!ValidateHealth(ref em, target, conditions.TargetHealthPercentLT))
                return false;
            return true;
        }

        private static bool EvaluateChance(byte percent, uint seed, int blockIndex, in Entity target)
        {
            if (percent == 0) return true;
            if (percent >= 100) return true;
            var random = new Random((uint)math.hash(new int4(blockIndex, target.Index, target.Version, (int)seed)));
            return random.NextInt(0, 100) < percent;
        }

        private static bool ValidateTag(ref EntityManager em, in Entity target, in FixedString64Bytes tag, bool expectPresence)
        {
            if (tag.Length == 0) return true;
            if (!em.HasBuffer<Framework.Core.Components.TagElement>(target))
                return !expectPresence;
            var buffer = em.GetBuffer<Framework.Core.Components.TagElement>(target);
            bool hasTag = false;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Value.Equals(tag))
                {
                    hasTag = true;
                    break;
                }
            }
            return expectPresence ? hasTag : !hasTag;
        }

        private static bool ValidateHealth(ref EntityManager em, in Entity target, float threshold)
        {
            if (threshold <= 0f) return true;
            if (!em.HasComponent<Framework.Resources.Components.Health>(target))
                return false;
            var health = em.GetComponentData<Framework.Resources.Components.Health>(target);
            if (health.Max <= 0) return false;
            float pct = (float)health.Current / math.max(1, health.Max);
            return pct < threshold;
        }
    }
}
