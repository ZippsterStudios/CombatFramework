using Framework.Core.Base;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Heal.Telemetry
{
    [BurstCompile]
    [UpdateInGroup(typeof(TelemetrySystemGroup))]
    public partial struct HealTelemetrySystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
        [BurstCompile]
        public void OnUpdate(ref SystemState state) { }
    }
}

