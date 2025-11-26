using Framework.Spells.Requests;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Spells.Factory
{
    public static class SpellRequestFactory
    {
        public static void EnqueueCast(ref EntityManager em, in Entity caster, in Entity target, in FixedString64Bytes spellId, int power = 0)
        {
            if (!em.HasBuffer<SpellCastRequest>(caster))
                em.AddBuffer<SpellCastRequest>(caster);

            var buf = em.GetBuffer<SpellCastRequest>(caster);
            buf.Add(new SpellCastRequest
            {
                Caster = caster,
                Target = target,
                SpellKey = spellId,
                Power = power
            });
        }
    }
}

