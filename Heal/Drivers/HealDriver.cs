using Framework.Core.Telemetry;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Heal.Drivers
{
    [BurstCompile]
    public static class HealDriver
    {
        private static readonly FixedString64Bytes HealAppliedTag = (FixedString64Bytes)"HealApplied";

        [BurstCompile]
        public static void Apply(ref EntityManager em, in Entity target, int amount)
        {
            if (!em.Exists(target) || amount <= 0) return;
            Framework.Resources.Factory.ResourceFactory.ApplyHealthDelta(ref em, target, amount);
            TelemetryRouter.Emit(HealAppliedTag, amount);
        }
    }
}
