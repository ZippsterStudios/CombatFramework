using Unity.Entities;

namespace Framework.Core.Config
{
    public struct SimulationConfig : IComponentData
    {
        public float FixedTimeStep;
        public int RngSeed;
        public bool EnableTelemetry;

        public static SimulationConfig Default => new SimulationConfig
        {
            FixedTimeStep = 1f / 60f,
            RngSeed = 1337,
            EnableTelemetry = true
        };
    }
}

