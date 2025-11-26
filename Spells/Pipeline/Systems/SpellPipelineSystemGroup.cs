using Framework.Core.Base;
using Unity.Entities;

namespace Framework.Spells.Pipeline.Systems
{
    [UpdateInGroup(typeof(RuntimeSystemGroup))]
    public partial class SpellPipelineSystemGroup : ComponentSystemGroup
    {
    }
}
