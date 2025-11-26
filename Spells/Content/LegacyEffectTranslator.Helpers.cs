using Framework.Damage.Components;
using Unity.Collections;
using Unity.Mathematics;

#pragma warning disable 0618 // uses SpellEffect for legacy definitions

namespace Framework.Spells.Content
{
    internal static partial class LegacyEffectTranslator
    {
        static TargetScope BuildScope(in SpellEffect effect, bool self)
        {
            var scope = TargetScope.Single(self ? TargetScopeKind.Caster : TargetScopeKind.PrimaryTarget);
            scope.TeamFilter = ParseFilter(effect.Filter);
            scope.IncludeCasterIfNoPrimary = (byte)(self ? 1 : 0);
            if (effect.ChainCount > 0)
            {
                scope.Kind = TargetScopeKind.ChainJump;
                scope.Chain = new TargetScopeChain
                {
                    MaxJumps = effect.ChainCount,
                    JumpRadius = effect.Radius > 0f ? effect.Radius : 5f,
                    TeamFilter = scope.TeamFilter
                };
                scope.Radius = scope.Chain.JumpRadius;
                return scope;
            }

            if (effect.Radius > 0f)
            {
                scope.Kind = TargetScopeKind.Radius;
                scope.Center = TargetScopeCenter.Caster;
                scope.Radius = effect.Radius;
                scope.Shape = effect.ConeAngle > 0f ? TargetScopeShape.Cone : TargetScopeShape.Sphere;
                scope.ConeAngleDeg = effect.ConeAngle;
                scope.MaxTargets = effect.ApplyToAll ? 0 : 1;
                return scope;
            }

            scope.MaxTargets = 1;
            return scope;
        }

        static TargetTeamFilter ParseFilter(in FixedString64Bytes filter)
        {
            if (filter.Length == 0) return TargetTeamFilter.Any;
            var value = filter.ToString().ToLowerInvariant();
            return value switch
            {
                "enemy" or "enemies" => TargetTeamFilter.Enemy,
                "ally" or "allies" or "friendly" => TargetTeamFilter.Ally,
                _ => TargetTeamFilter.Any
            };
        }

        static DamageSchool MapSchool(SpellSchool school)
        {
            return school switch
            {
                SpellSchool.Fire => DamageSchool.Fire,
                SpellSchool.Frost => DamageSchool.Frost,
                SpellSchool.Nature => DamageSchool.Nature,
                SpellSchool.Holy => DamageSchool.Holy,
                SpellSchool.Shadow => DamageSchool.Shadow,
                SpellSchool.Arcane => DamageSchool.Arcane,
                _ => DamageSchool.Physical
            };
        }
    }
}

#pragma warning restore 0618
