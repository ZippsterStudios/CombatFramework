using Framework.Threat.Drivers;
using Framework.Threat.Requests;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Threat.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.RequestsSystemGroup))]
    public partial struct ThreatRuntimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            foreach (var (buffer, entity) in SystemAPI.Query<DynamicBuffer<ThreatRequest>>().WithEntityAccess())
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    var req = buffer[i];
                    if (em.Exists(req.Target))
                        ThreatDriver.Apply(ref em, req.Target, req.Delta);
                }
                buffer.Clear();
            }
        }
    }
}
