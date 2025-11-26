using Framework.Debuffs.Content;
using Unity.Entities;

namespace Framework.Debuffs.Components
{
    public struct DebuffCrowdControlState : IComponentData
    {
        public DebuffFlags ActiveFlags;
    }
}
