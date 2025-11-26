using Unity.Burst;
using Unity.Entities;
using Framework.Spells.Pipeline.Components;

namespace Framework.Spells.Pipeline.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SpellPipelineSystemGroup))]
    [UpdateAfter(typeof(ApplySpellStageSystem))]
    public partial struct CleanupSpellStageSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (data, context, plan, entity) in SystemAPI.Query<SpellCastData, RefRW<SpellCastContext>, DynamicBuffer<CastPlanStep>>().WithEntityAccess())
            {
                var ctxValue = context.ValueRO;
                if (!CastPipelineStepUtility.IsStepActive(ctxValue, plan, CastStepType.Cleanup))
                    continue;

                var ctx = context.ValueRW;
                context.ValueRW = ctx;
                ecb.DestroyEntity(entity);
            }
        }
    }
}
