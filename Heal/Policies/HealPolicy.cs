using Unity.Burst;

namespace Framework.Heal.Policies
{
    [BurstCompile]
    public static class HealPolicy
    {
        public enum Result
        {
            Allow,
            Reject_Negative,
            Clamp_ToMax
        }

        [BurstCompile]
        public static Result Validate(int amount)
        {
            if (amount < 0) return Result.Reject_Negative;
            if (amount == 0) return Result.Clamp_ToMax;
            return Result.Allow;
        }
    }
}

