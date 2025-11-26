using Framework.Temporal.Components;
using Framework.Temporal.Policies;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Resources.Resolution
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.ResolutionSystemGroup))]
    public partial struct ResourceResolutionSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        private static void ApplyHealthRegen(ref Components.Health h, float dt)
        {
            if (h.RegenPerSecond > 0 && h.Current < h.Max)
            {
                h.RegenAccumulator += h.RegenPerSecond * dt;
                int whole = (int)h.RegenAccumulator;
                if (whole > 0)
                {
                    h.RegenAccumulator -= whole;
                    int newVal = h.Current + whole;
                    h.Current = newVal > h.Max ? h.Max : newVal;
                }
            }
        }

        private static void ApplyManaRegen(ref Components.Mana m, float dt)
        {
            if (m.RegenPerSecond > 0 && m.Current < m.Max)
            {
                m.RegenAccumulator += m.RegenPerSecond * dt;
                int whole = (int)m.RegenAccumulator;
                if (whole > 0)
                {
                    m.RegenAccumulator -= whole;
                    int newVal = m.Current + whole;
                    m.Current = newVal > m.Max ? m.Max : newVal;
                }
            }
        }

        private static void ApplyStaminaRegen(ref Components.Stamina s, float dt)
        {
            if (s.RegenPerSecond > 0 && s.Current < s.Max)
            {
                s.RegenAccumulator += s.RegenPerSecond * dt;
                int whole = (int)s.RegenAccumulator;
                if (whole > 0)
                {
                    s.RegenAccumulator -= whole;
                    int newVal = s.Current + whole;
                    s.Current = newVal > s.Max ? s.Max : newVal;
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(TemporalModifiers))]
        private partial struct HealthRegenJob_Temporal : IJobEntity
        {
            public float Dt;
            public void Execute(ref Components.Health h, in TemporalModifiers tm)
            {
                var mul = TemporalPolicy.IntervalMultiplier(tm.HastePercent, tm.SlowPercent);
                if (mul <= 0f) mul = 1f;
                var scaledDt = Dt / mul;
                ApplyHealthRegen(ref h, scaledDt);
            }
        }

        [BurstCompile]
        [WithNone(typeof(TemporalModifiers))]
        private partial struct HealthRegenJob_NoTemporal : IJobEntity
        {
            public float Dt;
            public void Execute(ref Components.Health h)
            {
                ApplyHealthRegen(ref h, Dt);
            }
        }

        [BurstCompile]
        [WithAll(typeof(TemporalModifiers))]
        private partial struct ManaRegenJob_Temporal : IJobEntity
        {
            public float Dt;
            public void Execute(ref Components.Mana m, in TemporalModifiers tm)
            {
                var mul = TemporalPolicy.IntervalMultiplier(tm.HastePercent, tm.SlowPercent);
                if (mul <= 0f) mul = 1f;
                var scaledDt = Dt / mul;
                ApplyManaRegen(ref m, scaledDt);
            }
        }

        [BurstCompile]
        [WithNone(typeof(TemporalModifiers))]
        private partial struct ManaRegenJob_NoTemporal : IJobEntity
        {
            public float Dt;
            public void Execute(ref Components.Mana m)
            {
                ApplyManaRegen(ref m, Dt);
            }
        }

        [BurstCompile]
        [WithAll(typeof(TemporalModifiers))]
        private partial struct StaminaRegenJob_Temporal : IJobEntity
        {
            public float Dt;
            public void Execute(ref Components.Stamina s, in TemporalModifiers tm)
            {
                var mul = TemporalPolicy.IntervalMultiplier(tm.HastePercent, tm.SlowPercent);
                if (mul <= 0f) mul = 1f;
                var scaledDt = Dt / mul;
                ApplyStaminaRegen(ref s, scaledDt);
            }
        }

        [BurstCompile]
        [WithNone(typeof(TemporalModifiers))]
        private partial struct StaminaRegenJob_NoTemporal : IJobEntity
        {
            public float Dt;
            public void Execute(ref Components.Stamina s)
            {
                ApplyStaminaRegen(ref s, Dt);
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            state.Dependency = new HealthRegenJob_Temporal { Dt = dt }.ScheduleParallel(state.Dependency);
            state.Dependency = new HealthRegenJob_NoTemporal { Dt = dt }.ScheduleParallel(state.Dependency);
            state.Dependency = new ManaRegenJob_Temporal { Dt = dt }.ScheduleParallel(state.Dependency);
            state.Dependency = new ManaRegenJob_NoTemporal { Dt = dt }.ScheduleParallel(state.Dependency);
            state.Dependency = new StaminaRegenJob_Temporal { Dt = dt }.ScheduleParallel(state.Dependency);
            state.Dependency = new StaminaRegenJob_NoTemporal { Dt = dt }.ScheduleParallel(state.Dependency);
        }
    }
}
