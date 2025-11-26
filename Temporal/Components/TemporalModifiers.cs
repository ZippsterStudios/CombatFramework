using Unity.Entities;

namespace Framework.Temporal.Components
{
    public struct TemporalModifiers : IComponentData
    {
        public float HastePercent; // 0..1 (0.2 = 20% faster)
        public float SlowPercent;  // 0..1 (0.2 = 20% slower)
    }
}

