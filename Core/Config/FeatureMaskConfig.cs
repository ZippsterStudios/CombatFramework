using System;
using Unity.Entities;

namespace Framework.Core.Config
{
    [Flags]
    public enum FeatureBits : uint
    {
        None = 0,
        Fizzle = 1u << 0,
        Crit = 1u << 1,
        Proc = 1u << 2,
        Reflection = 1u << 3,
        Projectile = 1u << 4,
        AreaEffect = 1u << 5,
        DOT = 1u << 6,
        HOT = 1u << 7,
        Buffs = 1u << 8,
        Threat = 1u << 9,
        All = 0xFFFFFFFF
    }

    public struct FeatureMaskConfig : IComponentData
    {
        public FeatureBits Mask;

        public static bool IsEnabled(FeatureBits mask, FeatureBits bit) => (mask & bit) != 0;
    }
}

