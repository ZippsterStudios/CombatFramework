using Framework.Contracts.Perception;
using Unity.Collections;
using Unity.Entities;

namespace Framework.AI.Policies
{
    public interface ITargetPickPolicy
    {
        Entity Select(NativeArray<PerceptionTargetCandidate> candidates, out int selectedIndex);
    }

    public struct NearestVisibleTargetPolicy : ITargetPickPolicy
    {
        public Entity Select(NativeArray<PerceptionTargetCandidate> candidates, out int selectedIndex)
        {
            selectedIndex = -1;
            if (!candidates.IsCreated || candidates.Length == 0)
                return Entity.Null;

            float bestDistance = float.MaxValue;
            for (int i = 0; i < candidates.Length; i++)
            {
                var entry = candidates[i];
                if (entry.Target == Entity.Null || entry.Visible == 0)
                    continue;

                if (entry.DistanceSq < bestDistance)
                {
                    bestDistance = entry.DistanceSq;
                    selectedIndex = i;
                }
            }

            if (selectedIndex >= 0)
                return candidates[selectedIndex].Target;

            float bestScore = float.MinValue;
            for (int i = 0; i < candidates.Length; i++)
            {
                var entry = candidates[i];
                if (entry.Target == Entity.Null)
                    continue;

                if (entry.Score > bestScore)
                {
                    bestScore = entry.Score;
                    selectedIndex = i;
                }
            }

            return selectedIndex >= 0 ? candidates[selectedIndex].Target : Entity.Null;
        }
    }
}
