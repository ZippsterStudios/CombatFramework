using Unity.Entities;
using Unity.Collections;

namespace Framework.Spells.Drivers
{
    public static class SpellDriver
    {
        public struct Casting : IComponentData
        {
            public FixedString64Bytes SpellId;
            public float CastTimeRemaining;
            public Entity Target;
        }

        public static void BeginCast(ref EntityManager em, in Entity caster, in FixedString64Bytes spellId, in Entity target, float castTime)
        {
            if (!em.HasComponent<Casting>(caster))
                em.AddComponentData(caster, new Casting { SpellId = spellId, Target = target, CastTimeRemaining = castTime });
            else
                em.SetComponentData(caster, new Casting { SpellId = spellId, Target = target, CastTimeRemaining = castTime });
        }

        public static void ClearCast(ref EntityManager em, in Entity caster)
        {
            if (em.HasComponent<Casting>(caster))
                em.RemoveComponent<Casting>(caster);
        }
    }
}
