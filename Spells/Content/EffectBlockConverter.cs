using System;

namespace Framework.Spells.Content
{
    internal static class EffectBlockConverter
    {
        public static EffectBlock[] Resolve(in SpellDefinition def)
        {
            if (def.Blocks != null && def.Blocks.Length > 0)
                return def.Blocks;

            return Array.Empty<EffectBlock>();
        }
    }
}
