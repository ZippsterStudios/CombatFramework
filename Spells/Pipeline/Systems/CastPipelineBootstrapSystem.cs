using Unity.Collections;
using Unity.Entities;
using Unity.Burst;

using Framework.Spells.Pipeline.Config;

namespace Framework.Spells.Pipeline.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SpellPipelineSystemGroup))]
    [UpdateBefore(typeof(CastPlanBuilderSystem))]
    public partial struct CastPipelineBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            using var query = state.EntityManager.CreateEntityQuery(typeof(CastGlobalConfigSingleton));
            if (!query.IsEmptyIgnoreFilter)
                return;

            var builder = new BlobBuilder(Allocator.Temp);
            ref var cfg = ref builder.ConstructRoot<CastGlobalConfig>();
            var defaults = CastGlobalConfigSingleton.DefaultValues;
            cfg.InterruptChargePercent = defaults.InterruptChargePercent;
            cfg.FizzleChargePercent = defaults.FizzleChargePercent;
            cfg.AllowPartialRefundOnPreResolve = defaults.AllowPartialRefundOnPreResolve;
            var blob = builder.CreateBlobAssetReference<CastGlobalConfig>(Allocator.Persistent);
            builder.Dispose();

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new CastGlobalConfigSingleton { Reference = blob });
        }

        public void OnDestroy(ref SystemState state)
        {
            using var query = state.EntityManager.CreateEntityQuery(typeof(CastGlobalConfigSingleton));
            if (query.IsEmptyIgnoreFilter)
                return;

            var singletonEntity = query.GetSingletonEntity();
            var singleton = state.EntityManager.GetComponentData<CastGlobalConfigSingleton>(singletonEntity);
            if (singleton.Reference.IsCreated)
                singleton.Reference.Dispose();
            state.EntityManager.DestroyEntity(singletonEntity);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
        }
    }
}
