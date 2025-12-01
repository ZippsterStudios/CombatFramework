using Framework.Melee.Components;
using Framework.Melee.Runtime.SystemGroups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Melee.Runtime.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(MeleeSystemGroup))]
    [UpdateAfter(typeof(MeleeDefenseWindowSystem))]
    public partial struct MeleeProcStateSystem : ISystem
    {
        private EntityQuery _procQuery;

        public void OnCreate(ref SystemState state)
        {
            _procQuery = state.GetEntityQuery(ComponentType.ReadWrite<MeleeProcRuntimeStateElement>());
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            double now = SystemAPI.Time.ElapsedTime;
            using var entities = _procQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var runtime = SystemAPI.GetBuffer<MeleeProcRuntimeStateElement>(entity);
                for (int i = runtime.Length - 1; i >= 0; i--)
                {
                    var entry = runtime[i];
                    if (entry.WindowExpiry > 0 && now >= entry.WindowExpiry)
                    {
                        entry.TriggerCount = 0;
                        entry.WindowExpiry = 0;
                    }

                    if (entry.ExpireTime > 0 && now >= entry.ExpireTime)
                    {
                        runtime.RemoveAtSwapBack(i);
                        continue;
                    }

                    runtime[i] = entry;
                }
            }
        }
    }
}
