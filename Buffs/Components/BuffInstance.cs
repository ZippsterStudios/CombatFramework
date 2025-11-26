using Framework.Buffs.Content;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Buffs.Components
{
    public struct BuffInstance : IBufferElementData
    {
        public FixedString64Bytes BuffId;
        public FixedList128Bytes<BuffStatEffect> StatEffects;
        public FixedList64Bytes<DamageShieldSpec> DamageShields;
        public FixedList64Bytes<MeleeWardSpec> MeleeWards;
    }
}

