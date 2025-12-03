using Unity.Burst;
using Unity.Entities;
using Framework.Spells.TemporalImprint.Components;

namespace Framework.Spells.TemporalImprint.Systems
{
    /// <summary>
    /// Cleans up expired temporal suppression tags.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.RuntimeSystemGroup))]
    public partial struct TemporalImprintSuppressionSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            double now = state.WorldUnmanaged.Time.ElapsedTime;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (sup, entity) in SystemAPI.Query<TemporalImprintSuppression>().WithEntityAccess())
            {
                if (now >= sup.ExpireTime)
                    ecb.RemoveComponent<TemporalImprintSuppression>(entity);
            }
        }
    }
}
