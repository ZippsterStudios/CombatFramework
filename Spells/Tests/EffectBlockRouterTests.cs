using System;
using NUnit.Framework;
using Framework.Damage.Requests;
using Framework.Spells.Content;
using Framework.Spells.Runtime;
using Framework.Core.Components;
using Framework.Resources.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Spells.Tests
{
    public class EffectBlockRouterTests
    {
        World _world;
        EntityManager Em => _world.EntityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("EffectBlockRouterTests");
            TestWorldUtility.EnsureBaseSystemGroups(_world);
        }

        [TearDown]
        public void TearDown()
        {
            SpellScriptBridge.Register(null);
            if (_world != null && _world.IsCreated)
                _world.Dispose();
        }

        [Test]
        public void TargetScope_GroupRaidRadiusChain()
        {
            var caster = CreateActor(new float2(0, 0), team: 1, group: "alpha", raid: "raid");
            var allySameGroup = CreateActor(new float2(1, 0), team: 1, group: "alpha", raid: "raid");
            var allyRaidOnly = CreateActor(new float2(2, 0), team: 1, group: "beta", raid: "raid");
            var enemyNear = CreateActor(new float2(3, 0), team: 2);
            var enemyChain = CreateActor(new float2(5, 0), team: 2);

            ExecuteBlocks(caster, enemyNear, new EffectBlock
            {
                Scope = new TargetScope { Kind = TargetScopeKind.AlliesInGroupOfCaster },
                Payload = DamagePayload()
            });
            AssertDamageRequests(allySameGroup, 1);
            AssertDamageRequests(allyRaidOnly, 0);

            ClearDamage(allySameGroup);

            ExecuteBlocks(caster, enemyNear, new EffectBlock
            {
                Scope = new TargetScope { Kind = TargetScopeKind.AlliesInRaidOfCaster },
                Payload = DamagePayload()
            });
            AssertDamageRequests(allySameGroup, 1);
            AssertDamageRequests(allyRaidOnly, 1);

            ClearDamage(allySameGroup);
            ClearDamage(allyRaidOnly);

            ExecuteBlocks(caster, enemyNear, new EffectBlock
            {
                Scope = new TargetScope
                {
                    Kind = TargetScopeKind.Radius,
                    TeamFilter = TargetTeamFilter.Enemy,
                    Radius = 10f,
                    Center = TargetScopeCenter.Caster
                },
                Payload = DamagePayload()
            });
            AssertDamageRequests(enemyNear, 1);
            AssertDamageRequests(enemyChain, 1);

            ClearDamage(enemyNear);
            ClearDamage(enemyChain);

            ExecuteBlocks(caster, enemyNear, new EffectBlock
            {
                Scope = new TargetScope
                {
                    Kind = TargetScopeKind.ChainJump,
                    TeamFilter = TargetTeamFilter.Enemy,
                    Radius = 5f,
                    Chain = new TargetScopeChain { MaxJumps = 1, JumpRadius = 5f, TeamFilter = TargetTeamFilter.Enemy }
                },
                Payload = DamagePayload()
            });
            AssertDamageRequests(enemyNear, 1);
            AssertDamageRequests(enemyChain, 1);
        }

        [Test]
        public void Ordering_ScriptSetsTagForNextBlock()
        {
            var caster = CreateActor(new float2(0, 0), team: 1);
            var target = CreateActor(new float2(1, 0), team: 2);

            SpellScriptBridge.Register((ref EntityManager em, in Entity c, in Entity t, in SpellRuntimeMetadata meta, in FixedString64Bytes id, in FixedString64Bytes args) =>
            {
                if (!em.HasBuffer<TagElement>(t))
                    em.AddBuffer<TagElement>(t);
                em.GetBuffer<TagElement>(t).Add(new TagElement { Value = (FixedString64Bytes)"scripted" });
            });

            ExecuteBlocks(caster, target,
                new EffectBlock
                {
                    Scope = TargetScope.Single(TargetScopeKind.PrimaryTarget),
                    Payload = new EffectPayload { Kind = EffectPayloadKind.ScriptReference, Script = new ScriptPayload { FeatureId = "tagger" } }
                },
                new EffectBlock
                {
                    Scope = TargetScope.Single(TargetScopeKind.PrimaryTarget),
                    Payload = DamagePayload(),
                    Conditions = new EffectConditions { RequiresTag = (FixedString64Bytes)"scripted" }
                });

            AssertDamageRequests(target, 1);
        }

        [Test]
        public void Ledger_HealScalesFromDamage()
        {
            var caster = CreateActor(new float2(0, 0), team: 1);
            var target = CreateActor(new float2(1, 0), team: 2);

            ExecuteBlocks(caster, target,
                new EffectBlock
                {
                    Scope = TargetScope.Single(TargetScopeKind.PrimaryTarget),
                    Payload = DamagePayload(40)
                },
                new EffectBlock
                {
                    Scope = TargetScope.Single(TargetScopeKind.Caster),
                    Payload = new EffectPayload { Kind = EffectPayloadKind.Heal, Heal = new HealPayload { Amount = 0 } },
                    Scaling = new EffectScaling
                    {
                        UsePreviousResult = 1,
                        PreviousBlockOffset = -1,
                        ResultSource = EffectResultSource.Damage,
                        ResultCoefficient = 0.5f
                    }
                });

            AssertDamageRequests(target, 1);
            AssertHealRequests(caster, 1, expectedAmount: 20);
        }

        [Test]
        public void Performance_AoeOverTwoHundredTargets_NoManagedAllocations()
        {
            var caster = CreateActor(new float2(0, 0), team: 1);
            var targets = new Entity[256];
            for (int i = 0; i < targets.Length; i++)
            {
                float angle = (math.PI * 2f * i) / targets.Length;
                var pos = new float2(math.cos(angle), math.sin(angle)) * 10f;
                targets[i] = CreateActor(pos, team: 2);
            }

            var scope = new TargetScope
            {
                Kind = TargetScopeKind.Radius,
                TeamFilter = TargetTeamFilter.Enemy,
                Radius = 25f,
                Center = TargetScopeCenter.Caster
            };

            long before = GC.GetAllocatedBytesForCurrentThread();
            ExecuteBlocks(caster, targets[0], new EffectBlock { Scope = scope, Payload = DamagePayload(15) });
            long after = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < targets.Length; i++)
                AssertDamageRequests(targets[i], 1);

            Assert.LessOrEqual(after - before, 1024);
        }

        private void ExecuteBlocks(Entity caster, Entity primaryTarget, params EffectBlock[] blockSpecs)
        {
            var spell = new SpellDefinition
            {
                Id = (FixedString64Bytes)$"test-{Guid.NewGuid():N}",
                Blocks = blockSpecs,
                Costs = Array.Empty<SpellCost>()
            };
            using var blob = SpellDefinitionBlobUtility.Create(spell, Allocator.Temp);
            ref var runtimeBlocks = ref blob.Value.Blocks;
            using var ledger = new EffectResultLedger(runtimeBlocks.Length, Allocator.Temp);
            var ctx = new EffectExecutionContext
            {
                EntityManager = Em,
                Caster = caster,
                PrimaryTarget = primaryTarget,
                Metadata = new SpellRuntimeMetadata(),
                RandomSeed = 123u,
                Results = ledger
            };
            EffectBlockRouter.Execute(ref ctx, ref runtimeBlocks);
        }

        private static EffectPayload DamagePayload(int amount = 10) => new EffectPayload
        {
            Kind = EffectPayloadKind.Damage,
            Damage = new DamagePayload { Amount = amount, CanCrit = 0, School = Framework.Damage.Components.DamageSchool.Physical }
        };

        private Entity CreateActor(float2 position, byte team, FixedString64Bytes group = default, FixedString64Bytes raid = default)
        {
            var entity = Em.CreateEntity();
            Em.AddComponentData(entity, new Position { Value = position });
            Em.AddComponentData(entity, new Health { Current = 100, Max = 100, RegenPerSecond = 0 });
            Em.AddComponentData(entity, new TeamId { Value = team });
            if (group.Length > 0)
                Em.AddComponentData(entity, new GroupId { Value = group });
            if (raid.Length > 0)
                Em.AddComponentData(entity, new RaidId { Value = raid });
            return entity;
        }

        private void AssertDamageRequests(Entity target, int expected)
        {
            if (!Em.HasBuffer<DamageRequest>(target))
            {
                Assert.AreEqual(0, expected, $"Expected damage requests on {Format(target)}.");
                return;
            }
            var buffer = Em.GetBuffer<DamageRequest>(target);
            Assert.AreEqual(expected, buffer.Length, $"Unexpected damage request count on {Format(target)}.");
            buffer.Clear();
        }

        private void AssertHealRequests(Entity target, int expectedCount, int expectedAmount)
        {
            if (!Em.HasBuffer<Framework.Heal.Requests.HealRequest>(target))
            {
                Assert.Fail("Expected heal buffer on caster.");
                return;
            }
            var buf = Em.GetBuffer<Framework.Heal.Requests.HealRequest>(target);
            Assert.AreEqual(expectedCount, buf.Length);
            Assert.AreEqual(expectedAmount, buf[0].Amount);
            buf.Clear();
        }

        private void ClearDamage(Entity target)
        {
            if (Em.HasBuffer<DamageRequest>(target))
                Em.GetBuffer<DamageRequest>(target).Clear();
        }

        private static string Format(Entity e) => $"{e.Index}:{e.Version}";
    }
}
