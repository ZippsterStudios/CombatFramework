using Unity.Burst;
using Unity.Entities;

using Framework.Spells.Pipeline.Components;

namespace Framework.Spells.Pipeline.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SpellPipelineSystemGroup))]
    [UpdateAfter(typeof(SpendSpellStageSystem))]
    [UpdateBefore(typeof(InterruptSpellStageSystem))]
    public partial struct WindupSpellStageSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            foreach (var (context, plan) in SystemAPI.Query<RefRW<SpellCastContext>, DynamicBuffer<CastPlanStep>>())
            {
                var ctxValue = context.ValueRO;
                if (!CastPipelineStepUtility.IsStepActive(ctxValue, plan, CastStepType.Windup, out var step))
                    continue;

                var ctx = context.ValueRW;
                var duration = step.Params.Float0;
                if (duration <= 0f)
                {
                    ctx.Flags |= CastContextFlags.WindupComplete;
                    ctx.RequestAdvance();
                    context.ValueRW = ctx;
                    continue;
                }

                ctx.StepDuration = duration;
                ctx.StepTimer += dt;
                if (ctx.StepTimer >= duration)
                {
                    ctx.Flags |= CastContextFlags.WindupComplete;
                    ctx.RequestAdvance();
                }
                context.ValueRW = ctx;
            }
        }
    }
}
