using Framework.Resources.Factory;
using Framework.Resources.Requests;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Resources.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.RequestsSystemGroup))]
    public partial struct ResourceRuntimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            foreach (var (buffer, entity) in SystemAPI.Query<DynamicBuffer<ResourceRequest>>().WithEntityAccess())
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    var req = buffer[i];
                    if (!em.Exists(req.Target))
                        continue;

                    switch (req.Kind)
                    {
                        case Framework.Resources.Requests.ResourceKind.Mana:
                            ResourceFactory.ApplyManaDelta(ref em, req.Target, req.Delta);
                            break;
                        case Framework.Resources.Requests.ResourceKind.Stamina:
                            ResourceFactory.ApplyStaminaDelta(ref em, req.Target, req.Delta);
                            break;
                        case Framework.Resources.Requests.ResourceKind.Health:
                        default:
                            ResourceFactory.ApplyHealthDelta(ref em, req.Target, req.Delta);
                            break;
                    }
                }
                buffer.Clear();
            }
        }
    }
}
