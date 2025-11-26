using Unity.Collections;
using Unity.Entities;

namespace Framework.Core.Components
{
    public struct GroupId : IComponentData
    {
        public FixedString64Bytes Value;
    }
}
