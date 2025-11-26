using Framework.Core.Base;
using Framework.Debuffs.Components;
using Framework.Debuffs.Drivers;
using Framework.Debuffs.Requests;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Debuffs.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(RequestsSystemGroup))]
    public partial struct DebuffRuntimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.EntityManager.CompleteDependencyBeforeRW<DebuffRequest>();
            state.EntityManager.CompleteDependencyBeforeRW<DebuffInstance>();

            var em = state.EntityManager;
            var query = SystemAPI.QueryBuilder()
                                 .WithAll<DebuffRequest>()
                                 .Build();

            using var entities = query.ToEntityArray(Allocator.Temp);
            for (int eIndex = 0; eIndex < entities.Length; eIndex++)
            {
                var entity = entities[eIndex];
                if (!em.HasBuffer<DebuffRequest>(entity))
                    continue;

                var buffer = em.GetBuffer<DebuffRequest>(entity);
                using var requests = buffer.ToNativeArray(Allocator.Temp);
                buffer.Clear();

                for (int i = 0; i < requests.Length; i++)
                {
                    var req = requests[i];
                    DebuffDriver.Apply(ref em, req.Target, req.Source, req.DebuffId,
                        req.DurationOverride, req.AddStacks, req.ExtraFlags);
                }
            }
        }
    }
}
