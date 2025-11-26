using Unity.Burst;
using Unity.Entities;

namespace Framework.Core.Telemetry
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.TelemetrySystemGroup))]
    public partial struct TelemetryTimeUpdateSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            TelemetryTime.ElapsedSeconds = SystemAPI.Time.ElapsedTime;
        }
    }
}

