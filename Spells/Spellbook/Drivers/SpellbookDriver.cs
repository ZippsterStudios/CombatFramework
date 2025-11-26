using Unity.Collections;
using Unity.Entities;

namespace Framework.Spells.Spellbook.Drivers
{
    public static class SpellbookDriver
    {
        public static void LearnSpell(ref EntityManager em, in Entity e, in FixedString64Bytes spellId)
        {
            if (!em.HasBuffer<Components.SpellSlot>(e))
                em.AddBuffer<Components.SpellSlot>(e);

            var buf = em.GetBuffer<Components.SpellSlot>(e);
            for (int i = 0; i < buf.Length; i++)
                if (buf[i].SpellId.Equals(spellId)) return; // already known
            buf.Add(new Components.SpellSlot { SpellId = spellId });
        }

        public static void ForgetSpell(ref EntityManager em, in Entity e, in FixedString64Bytes spellId)
        {
            if (!em.HasBuffer<Components.SpellSlot>(e)) return;
            var buf = em.GetBuffer<Components.SpellSlot>(e);
            for (int i = 0; i < buf.Length; i++)
            {
                if (buf[i].SpellId.Equals(spellId))
                {
                    buf.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
