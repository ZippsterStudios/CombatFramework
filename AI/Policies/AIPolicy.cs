using Unity.Burst;

namespace Framework.AI.Policies
{
    [BurstCompile]
    public static class AIPolicy
    {
        public enum Result { Allow, Reject_InvalidState }

        [BurstCompile]
        public static Result ValidateState(int state)
        {
            // For now any non-negative state is considered valid
            return state >= 0 ? Result.Allow : Result.Reject_InvalidState;
        }
    }
}

