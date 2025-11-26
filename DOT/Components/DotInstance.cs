using Unity.Collections;
using Unity.Entities;

namespace Framework.DOT.Components
{
    public struct DotInstance : IBufferElementData
    {
        public FixedString64Bytes EffectId;
        public int DamagePerTick;
        public Entity Source;
    }
}
