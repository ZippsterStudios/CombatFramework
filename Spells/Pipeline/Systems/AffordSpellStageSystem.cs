using Unity.Burst;
using Unity.Entities;
using Unity.Collections;

using Framework.Spells.Pipeline.Components;
using Framework.Spells.Pipeline.Events;

namespace Framework.Spells.Pipeline.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SpellPipelineSystemGroup))]
    [UpdateAfter(typeof(ValidateSpellStageSystem))]
    [UpdateBefore(typeof(SpendSpellStageSystem))]
    public partial struct AffordSpellStageSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (data, context, plan, entity) in SystemAPI.Query<SpellCastData, RefRW<SpellCastContext>, DynamicBuffer<CastPlanStep>>().WithEntityAccess())
            {
                var ctxValue = context.ValueRO;
                if (!CastPipelineStepUtility.IsStepActive(ctxValue, plan, CastStepType.Afford))
                    continue;

                bool afford = true;
                ref var def = ref data.Definition.Value;
                for (int i = 0; i < def.Costs.Length; i++)
                {
                    if (!ResourceAccessUtility.CanAfford(ref em, data.Caster, def.Costs[i]))
                    {
                        afford = false;
                        break;
                    }
                }

                var ctx = context.ValueRW;
                if (!afford)
                {
                    ctx.Flags |= CastContextFlags.Terminal;
                    ctx.Termination = CastTerminationReason.CannotAfford;
                    CastPipelineStepUtility.JumpToCleanup(ref ctx, plan);
                    context.ValueRW = ctx;
                    SpellCastEventUtility.Emit<SpellFizzledEvent>(ecb, entity, data);
                    continue;
                }

                ctx.Flags |= CastContextFlags.CostsCovered;
                ctx.RequestAdvance();
                context.ValueRW = ctx;
            }
        }
    }
}
