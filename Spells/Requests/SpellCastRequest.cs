using Unity.Collections;
using Unity.Entities;

namespace Framework.Spells.Requests
{
    public struct SpellCastRequest : IBufferElementData
    {
        public Entity Caster;
        public Entity Target;
        public FixedString64Bytes SpellKey;
        public int Power;
    }
}

