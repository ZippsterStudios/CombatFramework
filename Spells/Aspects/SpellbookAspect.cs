using Unity.Entities;
using Unity.Collections;

#pragma warning disable 0618 // IAspect is obsolete; kept for backward compatibility
namespace Framework.Spells.Aspects
{
    public readonly partial struct SpellbookAspect : IAspect
    {
        public readonly Entity Entity;
        readonly DynamicBuffer<Framework.Spells.Spellbook.Components.SpellSlot> _slots;

        public bool HasSpell(in FixedString64Bytes id)
        {
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i].SpellId.Equals(id)) return true;
            return false;
        }
    }
}
#pragma warning restore 0618
