using Unity.Mathematics;

namespace Framework.Melee.Runtime.Utilities
{
    public static class MeleeDefenseUtility
    {
        public static int ApplyBlock(int incomingDamage, float blockPercent, float blockFlat)
        {
            if (incomingDamage <= 0)
                return 0;

            float percent = math.clamp(blockPercent, 0f, 1f);
            float reduced = incomingDamage * (1f - percent);
            reduced -= math.max(0f, blockFlat);
            return math.max(0, (int)math.floor(reduced));
        }
    }
}
