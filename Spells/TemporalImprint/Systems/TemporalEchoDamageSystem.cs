using Unity.Burst;
using Unity.Entities;
using Framework.Damage.Requests;
using Framework.Spells.TemporalImprint.Components;

namespace Framework.Spells.TemporalImprint.Systems
{
    /// <summary>
    /// Applies damage requests to temporal echoes and reduces their health. When health is depleted,
    /// the replay system will handle inversion and cleanup.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.RuntimeSystemGroup), OrderFirst = true)]
    public partial struct TemporalEchoDamageSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (requests, hpRef, entity) in SystemAPI.Query<DynamicBuffer<DamageRequest>, RefRW<TemporalEchoHealth>>().WithEntityAccess())
            {
                ref var hp = ref hpRef.ValueRW;
                for (int i = 0; i < requests.Length; i++)
                {
                    hp.Current -= requests[i].Packet.Amount;
                    if (hp.Current <= 0f)
                    {
                        hp.Current = 0f;
                        break;
                    }
                }
                requests.Clear();

                if (hp.Current <= 0f)
                    ecb.DestroyEntity(entity);
            }
        }
    }
}
