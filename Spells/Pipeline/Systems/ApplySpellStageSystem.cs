using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

using Framework.Spells.Runtime;
using Framework.Spells.Pipeline.Components;
using Framework.Spells.Pipeline.Events;

namespace Framework.Spells.Pipeline.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SpellPipelineSystemGroup))]
    [UpdateAfter(typeof(FizzleSpellStageSystem))]
    [UpdateBefore(typeof(CleanupSpellStageSystem))]
    public partial struct ApplySpellStageSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(state.WorldUnmanaged);

            var query = SystemAPI.QueryBuilder()
                                 .WithAll<SpellCastData>()
                                 .WithAll<CastPlanStep>()
                                 .WithAllRW<SpellCastContext>()
                                 .Build();

            using var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var data = em.GetComponentData<SpellCastData>(entity);
                var ctx = em.GetComponentData<SpellCastContext>(entity);
                var plan = em.GetBuffer<CastPlanStep>(entity);

                if (!CastPipelineStepUtility.IsStepActive(in ctx, in plan, CastStepType.Apply))
                    continue;

                if ((ctx.Flags & CastContextFlags.CostsSpent) == 0)
                {
                    SpendFinalCosts(ref em, in data);
                    ctx.Flags |= CastContextFlags.CostsSpent;
                }

                ref var def = ref data.Definition.Value;
                var meta = new SpellRuntimeMetadata
                {
                    CategoryId = def.CategoryId,
                    CategoryLevel = def.CategoryLevel,
                    SpellLevel = def.SpellLevel,
                    Rank = def.Rank
                };

                ref var blocks = ref def.Blocks;
                if (blocks.Length > 0)
                {
                    using var ledger = new EffectResultLedger(blocks.Length, Allocator.Temp);
                    var exec = new EffectExecutionContext
                    {
                        EntityManager = em,
                        Caster = data.Caster,
                        PrimaryTarget = data.Target,
                        Metadata = meta,
                        RandomSeed = (uint)math.hash(new int3(data.Caster.Index, data.Target.Index, (int)(state.WorldUnmanaged.Time.ElapsedTime * 1000))),
                        Results = ledger
                    };
                    EffectBlockRouter.Execute(ref exec, ref blocks);
                }

                ctx.Flags |= CastContextFlags.Resolved;
                ctx.Termination = CastTerminationReason.Applied;
                ctx.RequestAdvance();
                em.SetComponentData(entity, ctx);
                SpellCastEventUtility.Emit<SpellResolvedEvent>(ecb, entity, data);
            }
        }

        static void SpendFinalCosts(ref EntityManager em, in SpellCastData data)
        {
            ref var def = ref data.Definition.Value;
            for (int i = 0; i < def.Costs.Length; i++)
                ResourceAccessUtility.Spend(ref em, data.Caster, def.Costs[i]);
        }
    }
}
