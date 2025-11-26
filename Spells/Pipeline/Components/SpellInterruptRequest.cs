using Unity.Collections;
using Unity.Entities;

namespace Framework.Spells.Pipeline.Components
{
    public struct SpellInterruptRequest : IComponentData
    {
        public Entity Source;
        public FixedString64Bytes Reason;
    }

    public struct SpellFizzleRequest : IComponentData
    {
        public FixedString64Bytes Reason;
    }
}
