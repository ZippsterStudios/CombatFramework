using System;
using Unity.Entities;

namespace Framework.Core.Config
{
    [Flags]
    public enum StatBits : ulong
    {
        None = 0,
        Health = 1UL << 0,
        Mana = 1UL << 1,
        Stamina = 1UL << 2,
        SpellPower = 1UL << 3,
        HealingPower = 1UL << 4,
        Armor = 1UL << 5,
        Resist = 1UL << 6,
        Haste = 1UL << 7,
        Slow = 1UL << 8,
        CritChance = 1UL << 9,
        CritMult = 1UL << 10,
        All = 0xFFFFFFFFFFFFFFFF
    }

    public struct StatMaskConfig : IComponentData
    {
        public StatBits Mask;

        public static bool IsEnabled(StatBits mask, StatBits bit) => (mask & bit) != 0;
    }
}

