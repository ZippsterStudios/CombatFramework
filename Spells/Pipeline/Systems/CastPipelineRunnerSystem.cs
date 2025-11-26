using Unity.Burst;
using Unity.Entities;

using Framework.Spells.Pipeline.Components;

namespace Framework.Spells.Pipeline.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SpellPipelineSystemGroup))]
    [UpdateBefore(typeof(ValidateSpellStageSystem))]
    public partial struct CastPipelineRunnerSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (context, plan, entity) in SystemAPI.Query<RefRW<SpellCastContext>, DynamicBuffer<CastPlanStep>>().WithEntityAccess())
            {
                var ctx = context.ValueRW;
                if (plan.Length == 0)
                {
                    ctx.Flags |= CastContextFlags.Terminal;
                    ctx.CurrentStep = CastStepType.Cleanup;
                    context.ValueRW = ctx;
                    continue;
                }

                if ((ctx.Flags & CastContextFlags.Terminal) != 0 && ctx.CurrentStep != CastStepType.Cleanup)
                {
                    CastPipelineStepUtility.JumpToCleanup(ref ctx, plan);
                    context.ValueRW = ctx;
                    continue;
                }

                if (ctx.StepIndex >= plan.Length)
                {
                    ctx.Flags |= CastContextFlags.Terminal;
                    CastPipelineStepUtility.JumpToCleanup(ref ctx, plan);
                    context.ValueRW = ctx;
                    continue;
                }

                var desired = plan[ctx.StepIndex];
                if (ctx.CurrentStep != desired.StepType)
                {
                    ctx.CurrentStep = desired.StepType;
                    ctx.StepDuration = desired.Params.Float0;
                    ctx.StepTimer = 0f;
                }

                context.ValueRW = ctx;
            }
        }
    }
}
