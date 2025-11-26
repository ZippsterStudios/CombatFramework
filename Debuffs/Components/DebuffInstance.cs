using Framework.Debuffs.Content;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Debuffs.Components
{
    public struct DebuffInstance : IBufferElementData
    {
        public FixedString64Bytes DebuffId;
        public DebuffFlags Flags;
        public FixedList128Bytes<DebuffStatEffect> StatEffects;
        public Entity Source;
    }
}
