using Framework.Stats.Factory;
using Framework.Stats.Requests;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Stats.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.RuntimeSystemGroup))]
    public partial struct StatRuntimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            foreach (var (buffer, entity) in SystemAPI.Query<DynamicBuffer<StatRequest>>().WithEntityAccess())
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    var req = buffer[i];
                    if (em.Exists(req.Target))
                        StatFactory.ApplyAdditive(ref em, req.Target, req.Delta);
                }
                buffer.Clear();
            }
        }
    }
}
