using Unity.Burst;
using Unity.Entities;

namespace Framework.Temporal.Aspects
{
    [BurstCompile]
    public readonly partial struct TemporalAspect : IAspect
    {
        public readonly Entity Entity;
        readonly RefRO<Framework.Temporal.Components.TemporalModifiers> _tm;

        [BurstCompile]
        public float IntervalMultiplier()
        {
            if (!_tm.IsValid) return 1f;
            var t = _tm.ValueRO;
            var mul = Framework.Temporal.Policies.TemporalPolicy.IntervalMultiplier(t.HastePercent, t.SlowPercent);
            return mul <= 0f ? 1f : mul;
        }

        public bool HasTemporal => _tm.IsValid;
    }
}
