using Framework.AI.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.AI.Behaviors.Runtime
{
    internal static class AIBehaviorRecipeMatcher
    {
        public static Action Select(BlobAssetReference<AIBehaviorRecipe> recipeRef, in RecipeFacts facts, in AIAgentBehaviorConfig config)
        {
            ref var recipe = ref recipeRef.Value;
            for (int i = 0; i < recipe.Rules.Length; i++)
            {
                var rule = recipe.Rules[i];
                if (Matches(rule.Condition, facts, config))
                    return rule.Action;
            }

            return recipe.HasDefault != 0 ? recipe.DefaultAction : default;
        }

        private static bool Matches(in Condition condition, in RecipeFacts facts, in AIAgentBehaviorConfig config)
        {
            if ((condition.Flags & AIBehaviorConditionFlags.HasTarget) != 0 && facts.HasTarget == 0)
                return false;
            if ((condition.Flags & AIBehaviorConditionFlags.TargetVisible) != 0 && facts.Visibility == 0)
                return false;
            if ((condition.Flags & AIBehaviorConditionFlags.UseRange) != 0 && facts.HasTargetPosition != 0 && facts.HasAgentPosition != 0)
            {
                float range = condition.AttackRange > 0 ? condition.AttackRange : config.AttackRange;
                float rangeSq = range * range;
                bool inRange = facts.DistanceSq <= rangeSq;
                if ((condition.NotInRange != 0) == inRange)
                    return false;
            }
            if ((condition.Flags & AIBehaviorConditionFlags.UseHealthBelow) != 0)
            {
                float threshold = condition.HealthBelow > 0 ? condition.HealthBelow : config.FleeHealthThresholdPercent;
                if (!(facts.HealthPercent > 0 && facts.HealthPercent <= math.max(0.01f, threshold)))
                    return false;
            }
            return true;
        }
    }
}
