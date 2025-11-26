using Unity.Collections;
using Framework.Spells.Content;

namespace Framework.Spells.Features
{
    /// <summary>
    /// Compatibility shim so existing code that referenced Framework.Spells.Features.SpellRuntimeMetadata
    /// continues to compile. Use Framework.Spells.Runtime.SpellRuntimeMetadata directly instead.
    /// </summary>
    [System.Obsolete("SpellRuntimeMetadata moved to Framework.Spells.Runtime. Update namespaces to reference the runtime version.", false)]
    public struct SpellRuntimeMetadata
    {
        public FixedString32Bytes CategoryId;
        public int CategoryLevel;
        public int SpellLevel;
        public SpellRank Rank;

        public static implicit operator Framework.Spells.Runtime.SpellRuntimeMetadata(SpellRuntimeMetadata value)
        {
            return new Framework.Spells.Runtime.SpellRuntimeMetadata
            {
                CategoryId = value.CategoryId,
                CategoryLevel = value.CategoryLevel,
                SpellLevel = value.SpellLevel,
                Rank = value.Rank
            };
        }

        public static implicit operator SpellRuntimeMetadata(Framework.Spells.Runtime.SpellRuntimeMetadata value)
        {
            return new SpellRuntimeMetadata
            {
                CategoryId = value.CategoryId,
                CategoryLevel = value.CategoryLevel,
                SpellLevel = value.SpellLevel,
                Rank = value.Rank
            };
        }
    }
}
