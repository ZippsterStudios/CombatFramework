using Unity.Collections;
using Unity.Entities;

namespace Framework.Cooldowns.Components
{
    public struct CooldownGroup : IBufferElementData
    {
        public FixedString64Bytes GroupId;
        public double ReadyTime;
    }
}
