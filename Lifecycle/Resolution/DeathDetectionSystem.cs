using Framework.Lifecycle.Components;
using Framework.Lifecycle.Config;
using Framework.Lifecycle.Policies;
using Framework.Resources.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Lifecycle.Resolution
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.ResolutionSystemGroup))]
    public partial struct DeathDetectionSystem : ISystem
    {
        private Entity _deathEventStream;

        public void OnCreate(ref SystemState state)
        {
            _deathEventStream = Entity.Null;
            state.RequireForUpdate<Health>();
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;

            LifecycleFeatureConfig config;
            bool hasConfig = SystemAPI.TryGetSingleton(out config);
            config = LifecyclePolicy.EnsureDefaults(hasConfig, config);

            DynamicBuffer<DeathEvent> deathEvents = default;
            if (LifecyclePolicy.ShouldEmitDeathEvent(config))
            {
                deathEvents = EnsureDeathEventBuffer(ref state);
                em.CompleteDependencyBeforeRW<DeathEvent>();
                deathEvents.Clear();
            }

            em.CompleteDependencyBeforeRW<Health>();

            var query = SystemAPI.QueryBuilder()
                                  .WithAll<Health>()
                                  .WithNone<Dead>()
                                  .Build();
            using var candidates = query.ToEntityArray(Allocator.Temp);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            for (int i = 0; i < candidates.Length; i++)
            {
                var entity = candidates[i];
                var health = em.GetComponentData<Health>(entity);
                if (health.Current > 0)
                    continue;

                if (health.Current != 0)
                {
                    health.Current = 0;
                    em.SetComponentData(entity, health);
                }

                if (!em.HasComponent<Dead>(entity))
                    ecb.AddComponent<Dead>(entity);

                if (!em.HasComponent<DeadTimer>(entity))
                    ecb.AddComponent(entity, new DeadTimer { Seconds = 0f });

                if (LifecyclePolicy.ShouldEmitDeathEvent(config))
                {
                    deathEvents.Add(new DeathEvent
                    {
                        Victim = entity,
                        Killer = Entity.Null,
                        FinalDamage = 0
                    });
                }
            }

            ecb.Playback(em);
            ecb.Dispose();
        }

        private DynamicBuffer<DeathEvent> EnsureDeathEventBuffer(ref SystemState state)
        {
            var em = state.EntityManager;
            if (_deathEventStream == Entity.Null || !em.Exists(_deathEventStream))
            {
                _deathEventStream = em.CreateEntity();
                em.AddBuffer<DeathEvent>(_deathEventStream);
            }
            else if (!em.HasBuffer<DeathEvent>(_deathEventStream))
            {
                em.AddBuffer<DeathEvent>(_deathEventStream);
            }

            return em.GetBuffer<DeathEvent>(_deathEventStream);
        }
    }
}
