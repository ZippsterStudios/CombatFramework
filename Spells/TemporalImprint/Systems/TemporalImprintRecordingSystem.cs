using Unity.Burst;
using Unity.Entities;
using Framework.Spells.TemporalImprint.Components;

namespace Framework.Spells.TemporalImprint.Systems
{
    /// <summary>
    /// Maintains active recording windows and expires them after the configured duration.
    /// Actual event capture should be done by callers via TemporalImprintUtility.AppendEvent.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.RuntimeSystemGroup))]
    public partial struct TemporalImprintRecordingSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            double now = state.WorldUnmanaged.Time.ElapsedTime;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (recorder, entity) in SystemAPI.Query<TemporalImprintRecorder>().WithEntityAccess())
            {
                if (now - recorder.StartTime >= recorder.Duration)
                {
                    ecb.RemoveComponent<TemporalImprintRecorder>(entity);
                }
            }
        }
    }
}
