using Framework.Damage.Components;
using Framework.DamageModifiers.Components;
using Unity.Entities;

namespace Framework.DamageModifiers.Factory
{
    public static class DamageModifierFactory
    {
        public static void SetGlobalMultiplier(ref EntityManager em, in Entity target, float multiplier)
        {
            if (!em.Exists(target))
                return;

            var data = new DamageModifierDefaults { GlobalMultiplier = multiplier };
            if (em.HasComponent<DamageModifierDefaults>(target))
                em.SetComponentData(target, data);
            else
                em.AddComponentData(target, data);
        }

        public static void ClearGlobalMultiplier(ref EntityManager em, in Entity target)
        {
            if (em.HasComponent<DamageModifierDefaults>(target))
                em.RemoveComponent<DamageModifierDefaults>(target);
        }

        public static void SetTypeMultiplier(ref EntityManager em, in Entity target, DamageSchool school, float multiplier)
        {
            if (!em.Exists(target))
                return;

            if (!em.HasBuffer<DamageTypeModifier>(target))
                em.AddBuffer<DamageTypeModifier>(target);

            var buffer = em.GetBuffer<DamageTypeModifier>(target);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].School == school)
                {
                    buffer[i] = new DamageTypeModifier { School = school, Multiplier = multiplier };
                    return;
                }
            }

            buffer.Add(new DamageTypeModifier { School = school, Multiplier = multiplier });
        }

        public static void RemoveTypeMultiplier(ref EntityManager em, in Entity target, DamageSchool school)
        {
            if (!em.HasBuffer<DamageTypeModifier>(target))
                return;

            var buffer = em.GetBuffer<DamageTypeModifier>(target);
            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                if (buffer[i].School == school)
                {
                    buffer.RemoveAt(i);
                    break;
                }
            }

            if (buffer.Length == 0)
                em.RemoveComponent<DamageTypeModifier>(target);
        }

        public static void ClearAll(ref EntityManager em, in Entity target)
        {
            if (em.HasComponent<DamageModifierDefaults>(target))
                em.RemoveComponent<DamageModifierDefaults>(target);
            if (em.HasBuffer<DamageTypeModifier>(target))
                em.RemoveComponent<DamageTypeModifier>(target);
        }
    }
}

