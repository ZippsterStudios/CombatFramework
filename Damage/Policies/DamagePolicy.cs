using Unity.Burst;
using Unity.Mathematics;

namespace Framework.Damage.Policies
{
    [BurstCompile]
    public static class DamagePolicy
    {
        private const float ArmorSoftCap = 50f;
        private const float MinMultiplier = 0f;
        private const float MaxMultiplier = 5f;

        /// <summary>
        /// Apply armor only (used by legacy callers and as the first step of the extended mitigation).
        /// Armor reduces damage with diminishing returns so each additional point contributes less than the previous one.
        /// </summary>
        [BurstCompile]
        public static int Mitigate(int raw, int armor)
        {
            if (raw <= 0)
                return 0;

            if (armor == 0)
                return raw;

            float softCap = math.max(1f, ArmorSoftCap);
            float reduction = armor / (math.abs(armor) + softCap);
            float multiplier = 1f - reduction;
            multiplier = math.clamp(multiplier, MinMultiplier, MaxMultiplier);
            int result = (int)math.round(raw * multiplier);
            if (result < 0) result = 0;
            return result;
        }

        /// <summary>
        /// Apply armor and resist mitigation.
        /// Armor uses the diminishing returns formula above, resist is a percentage reduction.
        /// </summary>
        [BurstCompile]
        public static int Mitigate(int raw, int armor, float resistPercent)
        {
            int afterArmor = Mitigate(raw, armor);
            if (afterArmor <= 0)
                return 0;

            resistPercent = math.clamp(resistPercent, 0f, 0.95f);
            int afterResist = (int)math.round(afterArmor * (1f - resistPercent));
            if (afterResist < 0) afterResist = 0;
            if (afterResist > raw) afterResist = raw;
            return afterResist;
        }
    }
}
