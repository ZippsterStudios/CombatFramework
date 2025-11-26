using NUnit.Framework;
using Unity.Collections;
using Framework.Damage.Components;
using Framework.Spells.Content;
using Framework.Spells.Factory;

namespace Framework.CombatSystem.Tests
{
    public class SpellBuilderTests
    {
        [Test]
        public void Builder_CreatesBlocks_WithExplicitScopes()
        {
            var radiusScope = new TargetScope
            {
                Kind = TargetScopeKind.Radius,
                Center = TargetScopeCenter.Caster,
                Shape = TargetScopeShape.Sphere,
                Radius = 10f,
                TeamFilter = TargetTeamFilter.Enemy,
                MaxTargets = 0
            };
            var def = SpellBuilder
                .NewSpell(new FixedString64Bytes("Meteor"))
                .SetManaCost(120)
                .SetCastTime(3.5f)
                .AddEffect(radiusScope, new EffectPayload
                {
                    Kind = EffectPayloadKind.SpawnDot,
                    OverTime = new DotHotPayload { Id = (FixedString64Bytes)"BurningGround", UseCatalogDefaults = 1 }
                })
                .AddEffect(radiusScope, new EffectPayload
                {
                    Kind = EffectPayloadKind.Damage,
                    Damage = new DamagePayload
                    {
                        Amount = 150,
                        CanCrit = 1,
                        School = DamageSchool.Fire
                    }
                })
                .Register();

            Assert.That(def.Blocks.Length, Is.EqualTo(2));
            Assert.AreEqual(TargetScopeKind.Radius, def.Blocks[0].Scope.Kind);
            Assert.AreEqual(EffectPayloadKind.SpawnDot, def.Blocks[0].Payload.Kind);
        }

        [Test]
        public void LifeTransferPatternUsesPreviousResult()
        {
            var damageScope = TargetScope.Single(TargetScopeKind.PrimaryTarget);
            var healScope = TargetScope.Single(TargetScopeKind.Caster);
            var def = SpellBuilder
                .NewSpell(new FixedString64Bytes("life_transfer"))
                .AddEffect(damageScope, new EffectPayload
                {
                    Kind = EffectPayloadKind.Damage,
                    Damage = new DamagePayload { Amount = 40, School = DamageSchool.Physical }
                })
                .AddEffect(healScope, new EffectPayload
                {
                    Kind = EffectPayloadKind.Heal,
                    Heal = new HealPayload { Amount = 0 }
                }, scaling: new EffectScaling
                {
                    UsePreviousResult = 1,
                    PreviousBlockOffset = -1,
                    ResultSource = EffectResultSource.Damage,
                    ResultCoefficient = 1f
                })
                .Register();

            Assert.That(def.Blocks.Length, Is.EqualTo(2));
            Assert.AreEqual(EffectPayloadKind.Heal, def.Blocks[1].Payload.Kind);
            Assert.AreEqual(EffectResultSource.Damage, def.Blocks[1].Scaling.ResultSource);
            Assert.AreEqual(-1, def.Blocks[1].Scaling.PreviousBlockOffset);
        }
    }
}
