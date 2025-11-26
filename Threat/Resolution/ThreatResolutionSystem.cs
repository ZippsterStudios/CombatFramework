using Framework.Temporal.Components;
using Framework.Temporal.Policies;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Threat.Resolution
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.ResolutionSystemGroup))]
    public partial struct ThreatResolutionSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            const float decayPerSecond = 1f; // linear decay
            foreach (var (threat, tm) in SystemAPI.Query<RefRW<Components.ThreatValue>, RefRO<TemporalModifiers>>())
            {
                var mul = TemporalPolicy.IntervalMultiplier(tm.ValueRO.HastePercent, tm.ValueRO.SlowPercent);
                if (mul <= 0f) mul = 1f;
                var scaledDt = dt / mul;
                var t = threat.ValueRW;
                if (t.Value > 0)
                {
                    t.Value -= (int)(decayPerSecond * scaledDt);
                    if (t.Value < 0) t.Value = 0;
                    threat.ValueRW = t;
                }
            }

            foreach (var threat in SystemAPI.Query<RefRW<Components.ThreatValue>>().WithNone<TemporalModifiers>())
            {
                var t = threat.ValueRW;
                if (t.Value > 0)
                {
                    t.Value -= (int)(decayPerSecond * dt);
                    if (t.Value < 0) t.Value = 0;
                    threat.ValueRW = t;
                }
            }
        }
    }
}
