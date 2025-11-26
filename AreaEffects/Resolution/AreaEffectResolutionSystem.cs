using Framework.Temporal.Components;
using Framework.Temporal.Policies;
using Unity.Burst;
using Unity.Entities;
using Framework.AreaEffects.Factory;

namespace Framework.AreaEffects.Resolution
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.ResolutionSystemGroup))]
    public partial struct AreaEffectResolutionSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var em = state.EntityManager;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (inst, tm, entity) in SystemAPI.Query<RefRW<AreaEffectFactory.AreaEffectInstance>, RefRO<TemporalModifiers>>().WithEntityAccess())
            {
                var mul = TemporalPolicy.IntervalMultiplier(tm.ValueRO.HastePercent, tm.ValueRO.SlowPercent);
                if (mul <= 0f) mul = 1f;
                var scaledDt = dt / mul;
                var v = inst.ValueRW;
                v.Lifetime -= scaledDt;
                inst.ValueRW = v;
                if (v.Lifetime <= 0f)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            foreach (var (inst, entity) in SystemAPI.Query<RefRW<AreaEffectFactory.AreaEffectInstance>>().WithEntityAccess().WithNone<TemporalModifiers>())
            {
                var v = inst.ValueRW;
                v.Lifetime -= dt;
                inst.ValueRW = v;
                if (v.Lifetime <= 0f)
                {
                    ecb.DestroyEntity(entity);
                }
            }
            // Playback is scheduled by the ECB system; no manual playback/dispose here
        }
    }
}
