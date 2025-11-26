using Framework.Spells.Pipeline.Components;
using Unity.Entities;

namespace Framework.Spells.Pipeline.Systems
{
    static class CastPipelineStepUtility
    {
        public static bool IsStepActive(in SpellCastContext context, in DynamicBuffer<CastPlanStep> plan, CastStepType stepType, out CastPlanStep step)
        {
            step = default;
            if (context.StepIndex < 0 || context.StepIndex >= plan.Length)
                return false;

            step = plan[context.StepIndex];
            return step.StepType == stepType;
        }

        public static bool IsStepActive(in SpellCastContext context, in DynamicBuffer<CastPlanStep> plan, CastStepType stepType)
        {
            return IsStepActive(in context, in plan, stepType, out _);
        }

        public static void JumpToCleanup(ref SpellCastContext context, in DynamicBuffer<CastPlanStep> plan)
        {
            if (plan.Length == 0)
                return;

            context.StepIndex = plan.Length - 1;
            context.CurrentStep = CastStepType.Cleanup;
        }
    }
}
