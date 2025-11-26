using Unity.Burst;
using Unity.Entities;
using Unity.Collections;

using Framework.Spells.Pipeline.Components;
using Framework.Spells.Pipeline.Events;

namespace Framework.Spells.Pipeline.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SpellPipelineSystemGroup))]
    [UpdateAfter(typeof(CastPipelineRunnerSystem))]
    [UpdateBefore(typeof(AffordSpellStageSystem))]
    public partial struct ValidateSpellStageSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var em = state.EntityManager;

            foreach (var (data, context, plan, entity) in SystemAPI.Query<SpellCastData, RefRW<SpellCastContext>, DynamicBuffer<CastPlanStep>>().WithEntityAccess())
            {
                var ctxValue = context.ValueRO;
                if (!CastPipelineStepUtility.IsStepActive(ctxValue, plan, CastStepType.Validate))
                    continue;

                var ctx = context.ValueRW;
                bool valid = em.Exists(data.Caster) && em.Exists(data.Target);
                if (!valid)
                {
                    ctx.Flags |= CastContextFlags.Terminal;
                    ctx.Termination = CastTerminationReason.InvalidSpell;
                    CastPipelineStepUtility.JumpToCleanup(ref ctx, plan);
                    context.ValueRW = ctx;
                    SpellCastEventUtility.Emit<SpellFizzledEvent>(ecb, entity, data);
                    continue;
                }

                ctx.Flags |= CastContextFlags.Validated;
                ctx.RequestAdvance();
                context.ValueRW = ctx;
            }
        }
    }
}
