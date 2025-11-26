using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

using Framework.Spells.Content;
using Framework.Spells.Pipeline.Components;
using Framework.Spells.Pipeline.Config;
using Framework.Spells.Pipeline.Events;

namespace Framework.Spells.Pipeline.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SpellPipelineSystemGroup))]
    [UpdateAfter(typeof(WindupSpellStageSystem))]
    [UpdateBefore(typeof(FizzleSpellStageSystem))]
    public partial struct InterruptSpellStageSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<CastGlobalConfigSingleton>(out var config) || !config.IsCreated)
                return;

            var em = state.EntityManager;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (data, context, plan, entity) in SystemAPI.Query<SpellCastData, RefRW<SpellCastContext>, DynamicBuffer<CastPlanStep>>().WithEntityAccess())
            {
                bool isActive = CastPipelineStepUtility.IsStepActive(context.ValueRO, plan, CastStepType.InterruptCheck);
                bool hasRequest = em.HasComponent<SpellInterruptRequest>(entity);
                if (!hasRequest && !isActive)
                    continue;

                var ctx = context.ValueRW;
                if (!hasRequest)
                {
                    if (isActive)
                    {
                        ctx.RequestAdvance();
                        context.ValueRW = ctx;
                    }
                    continue;
                }

                ref var def = ref data.Definition.Value;
                if ((def.Flags & SpellDefinitionFlags.IgnoreInterrupts) != 0)
                {
                    ecb.RemoveComponent<SpellInterruptRequest>(entity);
                    ctx.RequestAdvance();
                    context.ValueRW = ctx;
                    continue;
                }

                var request = em.GetComponentData<SpellInterruptRequest>(entity);
                ChargeInterruptCost(ref em, data.Caster, ref def, config.Reference.Value);
                ctx.Flags |= CastContextFlags.Interrupted | CastContextFlags.Terminal;
                ctx.Termination = CastTerminationReason.Interrupted;
                CastPipelineStepUtility.JumpToCleanup(ref ctx, plan);
                context.ValueRW = ctx;
                SpellCastEventUtility.Emit<SpellInterruptedEvent>(ecb, entity, data, request.Reason);
                ecb.RemoveComponent<SpellInterruptRequest>(entity);
            }
        }

        static void ChargeInterruptCost(ref EntityManager em, in Entity caster, ref SpellDefinitionBlob def, in CastGlobalConfig cfg)
        {
            var percent = def.InterruptChargePercentOverride;
            if (percent <= 0f)
                percent = cfg.InterruptChargePercent;

            if (percent <= 0f || def.Costs.Length == 0)
                return;

            for (int i = 0; i < def.Costs.Length; i++)
            {
                var cost = def.Costs[i];
                if (cost.Amount == 0)
                    continue;
                var partial = cost;
                partial.Amount = (int)math.ceil(cost.Amount * percent);
                ResourceAccessUtility.Spend(ref em, caster, partial);
            }
        }
    }
}
