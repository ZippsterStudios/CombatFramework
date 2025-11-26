using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Temporal.Drivers
{
    [BurstCompile]
    public static class TemporalDriver
    {
        private static readonly FixedString64Bytes HasteAppliedTag = (FixedString64Bytes)"HasteApplied";
        private static readonly FixedString64Bytes SlowAppliedTag = (FixedString64Bytes)"SlowApplied";

        [BurstCompile]
        public static void ApplyHaste(ref EntityManager em, in Entity e, float hastePercent)
        {
            if (!em.HasComponent<Components.TemporalModifiers>(e))
                em.AddComponentData(e, new Components.TemporalModifiers { HastePercent = hastePercent, SlowPercent = 0f });
            else
            {
                var t = em.GetComponentData<Components.TemporalModifiers>(e);
                t.HastePercent = hastePercent;
                em.SetComponentData(e, t);
            }
            Framework.Core.Telemetry.TelemetryRouter.Emit(HasteAppliedTag, (int)(hastePercent * 100));
        }

        [BurstCompile]
        public static void ApplySlow(ref EntityManager em, in Entity e, float slowPercent)
        {
            if (!em.HasComponent<Components.TemporalModifiers>(e))
                em.AddComponentData(e, new Components.TemporalModifiers { HastePercent = 0f, SlowPercent = slowPercent });
            else
            {
                var t = em.GetComponentData<Components.TemporalModifiers>(e);
                t.SlowPercent = slowPercent;
                em.SetComponentData(e, t);
            }
            Framework.Core.Telemetry.TelemetryRouter.Emit(SlowAppliedTag, (int)(slowPercent * 100));
        }
    }
}
