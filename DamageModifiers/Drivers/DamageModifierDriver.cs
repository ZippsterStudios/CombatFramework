using Framework.Damage.Components;
using Framework.DamageModifiers.Components;
using Framework.DamageModifiers.Factory;
using Unity.Burst;
using Unity.Entities;

namespace Framework.DamageModifiers.Drivers
{
    [BurstCompile]
    public static class DamageModifierDriver
    {
        [BurstCompile]
        public static void SetGlobalMultiplier(ref EntityManager em, in Entity target, float multiplier) =>
            DamageModifierFactory.SetGlobalMultiplier(ref em, target, multiplier);

        [BurstCompile]
        public static void ClearGlobalMultiplier(ref EntityManager em, in Entity target) =>
            DamageModifierFactory.ClearGlobalMultiplier(ref em, target);

        [BurstCompile]
        public static void SetTypeMultiplier(ref EntityManager em, in Entity target, DamageSchool school, float multiplier) =>
            DamageModifierFactory.SetTypeMultiplier(ref em, target, school, multiplier);

        [BurstCompile]
        public static void RemoveTypeMultiplier(ref EntityManager em, in Entity target, DamageSchool school) =>
            DamageModifierFactory.RemoveTypeMultiplier(ref em, target, school);

        [BurstCompile]
        public static void ClearAll(ref EntityManager em, in Entity target) =>
            DamageModifierFactory.ClearAll(ref em, target);
    }
}

