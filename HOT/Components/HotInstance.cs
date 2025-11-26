using Unity.Collections;
using Unity.Entities;

namespace Framework.HOT.Components
{
    public struct HotInstance : IBufferElementData
    {
        public FixedString64Bytes EffectId;
        public int HealPerTick;
        public Entity Source;
    }
}
