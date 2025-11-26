using Framework.AreaEffects.Requests;
using Framework.AreaEffects.Factory;
using Unity.Burst;
using Unity.Entities;
using Unity.Collections;

namespace Framework.AreaEffects.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.RequestsSystemGroup))]
    public partial struct AreaEffectRuntimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var job = new SpawnAreaEffectsJob
            {
                ECB = ecb
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct SpawnAreaEffectsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute([EntityIndexInQuery] int sortKey, DynamicBuffer<AreaEffectRequest> requests)
        {
            for (int i = 0; i < requests.Length; i++)
            {
                var req = requests[i];
                var entity = ECB.CreateEntity(sortKey);
                ECB.AddComponent(sortKey, entity, new AreaEffectFactory.AreaEffectInstance
                {
                    Id = req.Id,
                    Center = req.Origin,
                    Radius = req.Radius,
                    Lifetime = req.LifetimeSeconds
                });
            }

            requests.Clear();
        }
    }
}
