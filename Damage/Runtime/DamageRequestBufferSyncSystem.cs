using Framework.Core.Base;
using Framework.Damage.Requests;
using Framework.Resources.Components;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Damage.Runtime
{
    /// <summary>
    /// Ensures every entity with Health has a DamageRequest buffer before runtime systems
    /// start processing casts. Prevents structural-change exceptions when damage is applied.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(RequestsSystemGroup))]
    public partial struct DamageRequestBufferSyncSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var beginSim = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = beginSim.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (_, entity) in SystemAPI.Query<RefRO<Health>>()
                                                 .WithNone<DamageRequest>()
                                                 .WithEntityAccess())
            {
                ecb.AddBuffer<DamageRequest>(entity);
            }
        }
    }
}
