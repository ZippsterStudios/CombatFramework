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
    [UpdateAfter(typeof(InterruptSpellStageSystem))]
    [UpdateBefore(typeof(ApplySpellStageSystem))]
    public partial struct FizzleSpellStageSystem : ISystem
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
                bool isActive = CastPipelineStepUtility.IsStepActive(context.ValueRO, plan, CastStepType.FizzleCheck);
                bool hasRequest = em.HasComponent<SpellFizzleRequest>(entity);
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

                var request = em.GetComponentData<SpellFizzleRequest>(entity);
                ref var def = ref data.Definition.Value;
                ChargeFizzleCost(ref em, data.Caster, ref def, config.Reference.Value);
                ctx.Flags |= CastContextFlags.Fizzled | CastContextFlags.Terminal;
                ctx.Termination = CastTerminationReason.Fizzled;
                CastPipelineStepUtility.JumpToCleanup(ref ctx, plan);
                context.ValueRW = ctx;
                SpellCastEventUtility.Emit<SpellFizzledEvent>(ecb, entity, data, request.Reason);
                ecb.RemoveComponent<SpellFizzleRequest>(entity);
            }
        }

        static void ChargeFizzleCost(ref EntityManager em, in Entity caster, ref SpellDefinitionBlob def, in CastGlobalConfig cfg)
        {
            var percent = def.FizzleChargePercentOverride;
            if (percent <= 0f)
                percent = cfg.FizzleChargePercent;

            if (percent <= 0f || def.Costs.Length == 0)
                return;

            for (int i = 0; i < def.Costs.Length; i++)
            {
                var cost = def.Costs[i];
                if (cost.Amount == 0)
                    continue;
                var partial = cost;
                partial.Amount = math.max(1, (int)math.ceil(cost.Amount * percent));
                ResourceAccessUtility.Spend(ref em, caster, partial);
            }
        }
    }
}
