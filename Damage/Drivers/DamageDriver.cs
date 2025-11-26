using Framework.Damage.Components;
using Framework.Damage.Policies;
using Framework.Core.Telemetry;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Damage.Drivers
{
    [BurstCompile]
    public static class DamageDriver
    {
        private static readonly FixedString64Bytes DamageAppliedTag = (FixedString64Bytes)"DamageApplied";

        [BurstCompile]
        public static void Apply(ref EntityManager em, in Entity target, in DamagePacket packet)
        {
            int armor = 0; float resist = 0f;
            if (em.HasComponent<Components.Damageable>(target))
            {
                var d = em.GetComponentData<Components.Damageable>(target);
                armor = d.Armor;
                resist = d.ResistPercent;
            }

            var mitigated = DamagePolicy.Mitigate(packet.Amount, armor, resist);
            Framework.Resources.Factory.ResourceFactory.ApplyHealthDelta(ref em, target, -mitigated);

            TelemetryRouter.Emit(DamageAppliedTag, mitigated);
        }
    }
}
