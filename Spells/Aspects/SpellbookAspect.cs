using Unity.Entities;
using Unity.Collections;

namespace Framework.Spells.Aspects
{
    /// <summary>
    /// Utility helpers for interacting with spellbook buffers without relying on deprecated IAspect.
    /// </summary>
    public static class SpellbookAspect
    {
        public static bool HasSpell(this DynamicBuffer<Framework.Spells.Spellbook.Components.SpellSlot> slots, in FixedString64Bytes id)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].SpellId.Equals(id))
                    return true;
            }
            return false;
        }
    }
}
