using Unity.Collections;
using Unity.Mathematics;

namespace Framework.Spells.Content
{
    /// <summary>
    /// Declarative target selection parameters for an effect block.
    /// </summary>
    public struct TargetScope
    {
        public TargetScopeKind Kind;
        public TargetScopeCenter Center;
        public TargetScopeShape Shape;
        public TargetTeamFilter TeamFilter;
        public float Radius;
        public float ConeAngleDeg;
        public int MaxTargets;
        public float3 CustomPoint;
        public TargetScopeChain Chain;
        public byte IncludeCasterIfNoPrimary;

        public static TargetScope Single(TargetScopeKind kind)
        {
            return new TargetScope
            {
                Kind = kind,
                Center = TargetScopeCenter.PrimaryTarget,
                Shape = TargetScopeShape.Sphere,
                TeamFilter = TargetTeamFilter.Any,
                Radius = 0f,
                ConeAngleDeg = 0f,
                MaxTargets = 1,
                CustomPoint = float3.zero,
                Chain = TargetScopeChain.Default,
                IncludeCasterIfNoPrimary = 0
            };
        }
    }

    public enum TargetScopeKind : byte
    {
        Caster = 0,
        PrimaryTarget = 1,
        AlliesInGroupOfCaster = 2,
        AlliesInRaidOfCaster = 3,
        Radius = 4,
        ChainJump = 5
    }

    public enum TargetScopeCenter : byte
    {
        Caster = 0,
        PrimaryTarget = 1,
        Point = 2
    }

    public enum TargetScopeShape : byte
    {
        Sphere = 0,
        Cone = 1
    }

    public enum TargetTeamFilter : byte
    {
        Any = 0,
        Ally = 1,
        Enemy = 2
    }

    public struct TargetScopeChain
    {
        public int MaxJumps;
        public float JumpRadius;
        public TargetTeamFilter TeamFilter;

        public static TargetScopeChain Default => new TargetScopeChain
        {
            MaxJumps = 0,
            JumpRadius = 0f,
            TeamFilter = TargetTeamFilter.Any
        };
    }
}
