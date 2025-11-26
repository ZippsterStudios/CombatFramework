using System;
using System.Collections.Generic;
using Framework.Damage.Components;
using Framework.Spells.Content;
using Unity.Collections;
using Unity.Mathematics;

namespace Framework.Spells.Factory
{
    public sealed partial class SpellBuilder
    {
        private SpellDefinition _def;
        private readonly List<EffectBlock> _blocks = new();
        private readonly List<SpellCost> _costs = new();

        private float _pendingRadius;
        private TargetTeamFilter _pendingTeam = TargetTeamFilter.Any;
        private bool _pendingApplyToAll;
        private TargetScopeShape _pendingShape = TargetScopeShape.Sphere;
        private float _pendingCone;
        private int _pendingChainCount;
        private float _pendingChainRadius;

        private SpellBuilder(FixedString64Bytes id)
        {
            _def.Id = id;
            _def.School = SpellSchool.Arcane;
            _def.CastTime = 0f;
            _def.Cooldown = 0f;
            _def.Range = 0f;
            _def.Targeting = SpellTargeting.Self;
        }

        public static SpellBuilder NewSpell(FixedString64Bytes id) => new(id);

        public SpellBuilder SetSchool(SpellSchool school) { _def.School = school; return this; }
        public SpellBuilder SetCastTime(float seconds) { _def.CastTime = math.max(0f, seconds); return this; }
        public SpellBuilder SetCooldown(float seconds) { _def.Cooldown = math.max(0f, seconds); return this; }
        public SpellBuilder SetRange(float value) { _def.Range = math.max(0f, value); return this; }
        public SpellBuilder SetTargeting(SpellTargeting targeting) { _def.Targeting = targeting; return this; }
        public SpellBuilder SetManaCost(int amount) => Cost(new FixedString64Bytes("Mana"), amount);
        public SpellBuilder Cost(FixedString64Bytes resource, int amount)
        {
            _costs.Add(new SpellCost { Resource = resource, Amount = math.max(0, amount) });
            return this;
        }

        public SpellBuilder AddEffect(TargetScope scope, EffectPayload payload, EffectConditions? conditions = null, EffectTiming? timing = null, EffectScaling? scaling = null)
        {
            _blocks.Add(new EffectBlock
            {
                Scope = ApplyPending(scope),
                Payload = payload,
                Conditions = conditions ?? default,
                Timing = timing ?? default,
                Scaling = scaling ?? default
            });
            return this;
        }

        public SpellDefinition Register()
        {
            _def.Costs = _costs.ToArray();
            _def.Blocks = _blocks.ToArray();
            SpellDefinitionCatalog.Register(_def);
            return _def;
        }

        private SpellBuilder AddEffect(TargetScopeKind defaultKind, EffectPayload payload, EffectConditions? conditions = null, EffectTiming? timing = null, EffectScaling? scaling = null)
            => AddEffect(TargetScope.Single(defaultKind), payload, conditions, timing, scaling);

        private TargetScope ApplyPending(TargetScope scope)
        {
            if (_pendingTeam != TargetTeamFilter.Any && scope.TeamFilter == TargetTeamFilter.Any)
                scope.TeamFilter = _pendingTeam;

            if (_pendingChainCount > 0)
            {
                var radius = _pendingChainRadius > 0f ? _pendingChainRadius : (_pendingRadius > 0f ? _pendingRadius : 5f);
                scope.Kind = TargetScopeKind.ChainJump;
                scope.Chain = new TargetScopeChain { MaxJumps = _pendingChainCount, JumpRadius = radius, TeamFilter = scope.TeamFilter };
                scope.Radius = radius;
            }
            else if (_pendingRadius > 0f)
            {
                scope.Kind = TargetScopeKind.Radius;
                scope.Center = TargetScopeCenter.Caster;
                scope.Radius = _pendingRadius;
                scope.Shape = _pendingShape;
                scope.ConeAngleDeg = _pendingShape == TargetScopeShape.Cone ? _pendingCone : 0f;
                scope.MaxTargets = _pendingApplyToAll ? 0 : math.max(scope.MaxTargets, 1);
            }

            ClearPendingScope();
            return scope;
        }

        private void QueueRadius(float radius, TargetTeamFilter filter, bool applyToAll)
        {
            _pendingRadius = math.max(0f, radius);
            _pendingTeam = filter;
            _pendingApplyToAll = applyToAll;
            _pendingShape = TargetScopeShape.Sphere;
            _pendingCone = 0f;
        }

        private void QueueCone(float angle)
        {
            _pendingShape = TargetScopeShape.Cone;
            _pendingCone = math.clamp(angle, 0f, 360f);
        }

        private void QueueChain(int count)
        {
            _pendingChainCount = math.max(0, count);
            _pendingChainRadius = _pendingRadius;
        }

        private void ClearPendingScope()
        {
            _pendingRadius = 0f;
            _pendingTeam = TargetTeamFilter.Any;
            _pendingApplyToAll = false;
            _pendingShape = TargetScopeShape.Sphere;
            _pendingCone = 0f;
            _pendingChainCount = 0;
            _pendingChainRadius = 0f;
        }

        private TargetTeamFilter ParseFilter(FixedString64Bytes filter)
        {
            if (filter.Length == 0) return TargetTeamFilter.Any;
            var lower = filter.ToString().ToLowerInvariant();
            return lower switch
            {
                "ally" or "allies" or "friendly" => TargetTeamFilter.Ally,
                "enemy" or "enemies" => TargetTeamFilter.Enemy,
                _ => TargetTeamFilter.Any
            };
        }

        private DamageSchool ResolveDamageSchool() => _def.School switch
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
