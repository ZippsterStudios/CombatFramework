using Framework.Temporal.Drivers;
using Framework.Temporal.Requests;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Temporal.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.RequestsSystemGroup))]
    public partial struct TemporalRuntimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            foreach (var (buffer, entity) in SystemAPI.Query<DynamicBuffer<TemporalRequest>>().WithEntityAccess())
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    // Placeholder: no-op until concrete haste/slow requests are defined
                    // Clear requests to avoid growth.
                }
                buffer.Clear();
            }
        }
    }
}
