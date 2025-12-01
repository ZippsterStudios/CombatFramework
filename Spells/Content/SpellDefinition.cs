using System;
using Unity.Collections;

namespace Framework.Spells.Content
{
    public enum SpellSchool
    {
        Arcane,
        Fire,
        Frost,
        Nature,
        Holy,
        Shadow,
        Physical,
    }

    [Flags]
    public enum SpellTargeting
    {
        None = 0,
        Self = 1 << 0,
        Ally = 1 << 1,
        Enemy = 1 << 2,
        Ground = 1 << 3,
    }

    public struct SpellCost
    {
        public FixedString64Bytes Resource; // e.g., "Mana", "Stamina"
        public int Amount;
    }

    public enum SpellRank : byte
    {
        Unspecified = 0,
        Apprentice = 1,
        Journeyman = 2,
        Adept = 3,
        Expert = 4,
        Master = 5,
        Grandmaster = 6
    }

    public struct SpellDefinition
    {
        public FixedString64Bytes Id;
        public FixedString32Bytes CategoryId;
        public int CategoryLevel;
        public int SpellLevel;
        public SpellRank Rank;
        public SpellSchool School;
        public SpellCost[] Costs;
        public float CastTime;
        public float Cooldown;
        public float Range;
        public SpellTargeting Targeting;
        public SpellEffect[] Effects; // Legacy support; new data should populate Blocks.
        public EffectBlock[] Blocks;
        public SpellDefinitionFlags Flags;
        public float InterruptChargePercentOverride;
        public float FizzleChargePercentOverride;
    }
}
