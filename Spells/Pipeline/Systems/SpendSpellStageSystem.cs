using Unity.Burst;
using Unity.Entities;

using Framework.Spells.Pipeline.Components;

namespace Framework.Spells.Pipeline.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SpellPipelineSystemGroup))]
    [UpdateAfter(typeof(AffordSpellStageSystem))]
    [UpdateBefore(typeof(WindupSpellStageSystem))]
    public partial struct SpendSpellStageSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;

            foreach (var (data, context, plan) in SystemAPI.Query<SpellCastData, RefRW<SpellCastContext>, DynamicBuffer<CastPlanStep>>())
            {
                if (context.ValueRO.CurrentStep != CastStepType.Spend)
                    continue;

                var ctx = context.ValueRW;
                ctx.Flags |= CastContextFlags.CostsCovered;
                ctx.RequestAdvance();
                context.ValueRW = ctx;
            }
        }
    }
}
