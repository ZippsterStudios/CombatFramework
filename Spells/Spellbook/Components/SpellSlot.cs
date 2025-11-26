using Unity.Collections;
using Unity.Entities;

namespace Framework.Spells.Spellbook.Components
{
    public struct SpellSlot : IBufferElementData
    {
        public FixedString64Bytes SpellId;
    }
}
