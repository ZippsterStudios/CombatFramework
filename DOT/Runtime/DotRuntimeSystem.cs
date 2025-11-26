using Framework.DOT.Drivers;
using Framework.DOT.Requests;
using Unity.Collections;
using Unity.Burst;
using Unity.Entities;

namespace Framework.DOT.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.RequestsSystemGroup))]
    public partial struct DotRuntimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            foreach (var (buffer, entity) in SystemAPI.Query<DynamicBuffer<DotRequest>>().WithEntityAccess())
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    var req = buffer[i];
                    if (!em.Exists(req.Target) || req.EffectId.Length == 0)
                        continue;
                    var effectId = req.EffectId;
                    var interval = req.TickInterval <= 0f ? 1f : req.TickInterval;
                    DotDriver.Apply(ref em, req.Target, effectId, req.Dps, interval, req.Duration, req.Source);
                }
                buffer.Clear();
            }
        }
    }
}
