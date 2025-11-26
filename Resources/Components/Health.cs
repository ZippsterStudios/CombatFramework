using Unity.Entities;

namespace Framework.Resources.Components
{
    public struct Health : IComponentData
    {
        public int Current;
        public int Max;
        public int RegenPerSecond;
        public float RegenAccumulator;
    }
}
