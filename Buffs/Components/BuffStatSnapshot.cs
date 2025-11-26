using Unity.Collections;
using Unity.Entities;

namespace Framework.Buffs.Components
{
    public struct BuffStatSnapshot : IComponentData
    {
        public int BonusHealthFlat;
        public float BonusHealthPercent;
        public int BonusManaFlat;
        public float BonusManaPercent;
        public int BonusStaminaFlat;
        public float BonusStaminaPercent;
        public float DamageMultiplier;
        public float DefenseMultiplier;
        public int DamageReflectFlat;
        public float DamageReflectPercent;
        public float HastePercent;
        public int WardCurrent;
        public int WardMax;
    }

    public struct BuffCustomStatAggregate : IBufferElementData
    {
        public FixedString32Bytes StatId;
        public float Additive;
        public float Multiplier;
    }
}
