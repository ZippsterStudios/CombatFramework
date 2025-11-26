using Unity.Collections;
using Unity.Entities;

namespace Framework.Core.Components
{
    public struct TagElement : IBufferElementData
    {
        public FixedString64Bytes Value;
    }
}
