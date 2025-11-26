using Unity.Collections;
using Unity.Entities;

using Framework.Spells.Content;

namespace Framework.Spells.Pipeline.Components
{
    public struct SpellCastData : IComponentData
    {
        public Entity Caster;
        public Entity Target;
        public FixedString64Bytes SpellId;
        public BlobAssetReference<SpellDefinitionBlob> Definition;
    }
}
