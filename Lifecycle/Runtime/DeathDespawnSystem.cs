using Framework.Lifecycle.Components;
using Framework.Lifecycle.Config;
using Framework.Lifecycle.Policies;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Lifecycle.Runtime
{
    [UpdateInGroup(typeof(Framework.Core.Base.RuntimeSystemGroup))]
    public partial struct DeathDespawnSystem : ISystem
    {
        private EntityQuery _deadQuery;

        public void OnCreate(ref SystemState state)
        {
            _deadQuery = state.GetEntityQuery(ComponentType.ReadOnly<Dead>(), ComponentType.ReadWrite<DeadTimer>());
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            if (_deadQuery.IsEmptyIgnoreFilter)
                return;

            LifecycleFeatureConfig config;
            bool hasConfig = SystemAPI.TryGetSingleton(out config);
            config = LifecyclePolicy.EnsureDefaults(hasConfig, config);

            if (!LifecyclePolicy.ShouldAutoDespawn(config))
                return;

            var em = state.EntityManager;
            var threshold = LifecyclePolicy.ClampDespawnSeconds(config.AutoDespawnSeconds);
            float delta = SystemAPI.Time.DeltaTime;

            var query = SystemAPI.QueryBuilder()
                                  .WithAll<Dead, DeadTimer>()
                                  .Build();
            using var entities = query.ToEntityArray(Allocator.Temp);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var timer = em.GetComponentData<DeadTimer>(entity);
                timer.Seconds += delta;
                em.SetComponentData(entity, timer);

                if (timer.Seconds >= threshold)
                    ecb.DestroyEntity(entity);
            }

            ecb.Playback(em);
            ecb.Dispose();
        }
    }
}
