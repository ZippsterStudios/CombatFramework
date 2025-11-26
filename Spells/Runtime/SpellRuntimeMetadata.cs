using Framework.Spells.Content;
using Unity.Collections;

namespace Framework.Spells.Runtime
{
    public struct SpellRuntimeMetadata
    {
        public FixedString32Bytes CategoryId;
        public int CategoryLevel;
        public int SpellLevel;
        public SpellRank Rank;
    }
}
