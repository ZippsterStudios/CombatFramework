using Framework.Buffs.Drivers;
using Framework.Buffs.Components;
using Framework.Buffs.Requests;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Buffs.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.RequestsSystemGroup))]
    public partial struct BuffRuntimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            foreach (var (buffer, entity) in SystemAPI.Query<DynamicBuffer<BuffRequest>>().WithEntityAccess())
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    var req = buffer[i];
                    if (em.Exists(req.Target))
                        BuffDriver.Apply(ref em, req.Target, req.BuffId, req.AddDuration, req.AddStacks);
                }
                buffer.Clear();
            }
        }
    }
}
