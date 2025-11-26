using Unity.Burst;
using Unity.Entities;
using Unity.Collections;

namespace Framework.Spells.Spellbook.Policies
{
    [BurstCompile]
    public static class SpellbookPolicy
    {
        [BurstCompile]
        public static bool CanLearn(in DynamicBuffer<Components.SpellSlot> book, in FixedString64Bytes spellId)
        {
            for (int i = 0; i < book.Length; i++)
                if (book[i].SpellId.Equals(spellId)) return false;
            return true;
        }

        [BurstCompile]
        public static bool HasSpell(in DynamicBuffer<Components.SpellSlot> book, in FixedString64Bytes spellId)
        {
            for (int i = 0; i < book.Length; i++)
                if (book[i].SpellId.Equals(spellId)) return true;
            return false;
        }
    }
}
