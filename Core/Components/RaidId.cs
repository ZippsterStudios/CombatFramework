using Unity.Collections;
using Unity.Entities;

namespace Framework.Core.Components
{
    public struct RaidId : IComponentData
    {
        public FixedString64Bytes Value;
    }
}
