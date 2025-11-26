using Framework.Core.Telemetry;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Threat.Drivers
{
    [BurstCompile]
    public static class ThreatDriver
    {
        private static readonly FixedString64Bytes ThreatUpdatedTag = (FixedString64Bytes)"ThreatUpdated";

        [BurstCompile]
        public static void Apply(ref EntityManager em, in Entity target, int delta)
        {
            if (!em.HasComponent<Components.ThreatValue>(target))
                em.AddComponentData(target, new Components.ThreatValue { Value = 0 });
            var t = em.GetComponentData<Components.ThreatValue>(target);
            long v = (long)t.Value + delta;
            if (v < 0) v = 0;
            t.Value = (int)v;
            em.SetComponentData(target, t);

            TelemetryRouter.Emit(ThreatUpdatedTag, t.Value);
        }
    }
}
