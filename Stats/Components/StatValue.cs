using Unity.Entities;

namespace Framework.Stats.Components
{
    // Generic stat: Value = (BaseValue + Additive) * Multiplier
    public struct StatValue : IComponentData
    {
        public float BaseValue;
        public float Additive;
        public float Multiplier; // e.g., 1.10 for +10%
        public float Value;      // cached computed
    }
}
