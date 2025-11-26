using Framework.Core.Base;
using Unity.Entities;

namespace Framework.Melee.Runtime.SystemGroups
{
    [UpdateInGroup(typeof(RuntimeSystemGroup))]
    public sealed partial class MeleeSystemGroup : ComponentSystemGroup { }
}
