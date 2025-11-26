using Unity.Entities;

namespace Framework.Resources.Aspects
{
    public readonly partial struct HealthAspect : IAspect
    {
        public readonly Entity Entity;
        readonly RefRW<Framework.Resources.Components.Health> _health;

        public ref Framework.Resources.Components.Health Health => ref _health.ValueRW;

        public void ApplyDelta(int delta)
        {
            long next = (long)_health.ValueRW.Current + delta;
            if (next < 0) next = 0;
            if (next > _health.ValueRW.Max) next = _health.ValueRW.Max;
            _health.ValueRW.Current = (int)next;
        }
    }
}

