using Framework.AI.Runtime;
using Unity.Mathematics;

namespace Framework.AI.Policies
{
    public interface IStateUtilityPolicy
    {
        int ChooseState(in AIBehaviorContext context);
    }

    public struct DefaultStateUtilityPolicy : IStateUtilityPolicy
    {
        public int ChooseState(in AIBehaviorContext context)
        {
            float idleUtility = 0.25f;
            float combatUtility = 0.25f;
            float fleeUtility = 0.1f;

            if (context.HasTarget != 0)
            {
                combatUtility += 0.45f;
                if (context.IsTargetVisible != 0)
                    combatUtility += 0.2f;

                var range = math.max(0.5f, context.Config.AttackRange);
                if (context.DistanceToTargetSq <= range * range)
                    combatUtility += 0.15f;
            }
            else
            {
                idleUtility += 0.4f;
            }

            var fleeThreshold = math.max(0.01f, context.Config.FleeHealthThresholdPercent);
            if (context.HealthPercent <= fleeThreshold)
            {
                fleeUtility += 1.0f;
                combatUtility *= 0.1f;
                idleUtility *= 0.1f;
            }

            return SelectState(context.StateId, idleUtility, combatUtility, fleeUtility);
        }

        private static int SelectState(int current, float idleUtility, float combatUtility, float fleeUtility)
        {
            float best = idleUtility;
            int state = AIStateIds.Idle;

            if (combatUtility > best || (math.abs(combatUtility - best) <= 1e-5f && current == AIStateIds.Combat))
            {
                best = combatUtility;
                state = AIStateIds.Combat;
            }

            if (fleeUtility > best || (math.abs(fleeUtility - best) <= 1e-5f && current == AIStateIds.Flee))
            {
                state = AIStateIds.Flee;
            }

            return state;
        }
    }
}
