using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Framework.Core.Base;
using Framework.Resources.Components;
using Framework.Resources.Factory;
using Framework.Damage.Components;
using Framework.Spells.Content;
using Framework.Spells.Factory;
using Framework.Spells.Spellbook.Components;

namespace Framework.Spells.Tests
{
    public class SpellDamageIntegrationTests
    {
        [Test]
        public void HeadlessCastDealsDamage()
        {
            using var world = new World("SpellDamageIntegration");
            TestWorldUtility.InitializeCombatWorld(world);
            var em = world.EntityManager;

            SpellTestSampleContent.EnsureRegistered();
            ConfigureInstantFireball();
            var spellId = (FixedString64Bytes)"fireball";

            var caster = CreateCaster(ref em, spellId);

            var target = em.CreateEntity();
            ResourceFactory.InitHealth(ref em, target, 500);
            if (!em.HasComponent<Damageable>(target))
                em.AddComponentData(target, new Damageable { Armor = 0, ResistPercent = 0f });

            SpellPipelineFactory.Cast(ref em, caster, target, spellId, 0);

            StepWorld(world, 360);

            var health = em.GetComponentData<Health>(target);
            Assert.Less(health.Current, health.Max, "Spell cast should reduce target health.");
        }

        [Test]
        public void FireballBypassesArmorButRespectsResist()
        {
            using var world = new World("SpellDamageArmorBypassIntegration");
            TestWorldUtility.InitializeCombatWorld(world);
            var em = world.EntityManager;

            SpellTestSampleContent.EnsureRegistered();
            ConfigureInstantFireball();
            var spellId = (FixedString64Bytes)"fireball";
            var caster = CreateCaster(ref em, spellId);

            int baseline = CastSpellAndMeasureDamage(world, ref em, caster, spellId, armor: 0, resistPercent: 0f);
            int armored = CastSpellAndMeasureDamage(world, ref em, caster, spellId, armor: 600, resistPercent: 0f);
            Assert.AreEqual(baseline, armored, "Fireball sets IgnoreArmor so armor changes should have no effect.");

            int resisted = CastSpellAndMeasureDamage(world, ref em, caster, spellId, armor: 0, resistPercent: 0.5f);
            Assert.Less(resisted, baseline, "Fireball does not ignore resist percent.");
        }

        [Test]
        public void FullBypassSpellIgnoresArmorAndResist()
        {
            using var world = new World("SpellDamageFullBypassIntegration");
            TestWorldUtility.InitializeCombatWorld(world);
            var em = world.EntityManager;

            SpellTestSampleContent.EnsureRegistered();
            var spellId = RegisterFullBypassSpell();
            var caster = CreateCaster(ref em, spellId);

            int baseline = CastSpellAndMeasureDamage(world, ref em, caster, spellId, armor: 0, resistPercent: 0f);
            int heavyMitigation = CastSpellAndMeasureDamage(world, ref em, caster, spellId, armor: 900, resistPercent: 0.8f);
            Assert.AreEqual(baseline, heavyMitigation, "Bypass spell should ignore armor + resist.");

            int resistOnly = CastSpellAndMeasureDamage(world, ref em, caster, spellId, armor: 0, resistPercent: 0.9f);
            Assert.AreEqual(baseline, resistOnly, "Resist alone should have no impact when both flags are set.");
        }

        private static void EnsureSpellBuffers(ref EntityManager em, in Entity caster, in FixedString64Bytes spellId)
        {
            if (!em.HasBuffer<SpellSlot>(caster))
                em.AddBuffer<SpellSlot>(caster);
            var slots = em.GetBuffer<SpellSlot>(caster);
            bool known = false;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].SpellId.Equals(spellId))
                {
                    known = true;
                    break;
                }
            }
            if (!known)
                slots.Add(new SpellSlot { SpellId = spellId });

            if (!em.HasBuffer<Framework.Spells.Requests.SpellCastRequest>(caster))
                em.AddBuffer<Framework.Spells.Requests.SpellCastRequest>(caster);
        }

        private static void StepWorld(World world, int frames)
        {
            var begin = world.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            var runtime = world.GetExistingSystemManaged<Framework.Core.Base.RuntimeSystemGroup>();
            var end = world.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();

            double elapsed = world.Time.ElapsedTime;
            for (int i = 0; i < frames; i++)
            {
                elapsed += 1f / 60f;
                world.SetTime(new TimeData(elapsed, 1f / 60f));

                begin?.Update();
                runtime?.Update();
                end?.Update();
            }
        }

        private static void ConfigureInstantFireball()
        {
            var spellId = (FixedString64Bytes)"fireball";
            if (SpellDefinitionCatalog.TryGet(spellId, out var fireball))
            {
                fireball.CastTime = 0f;
                fireball.Cooldown = 0f;
                SpellDefinitionCatalog.Register(fireball);
            }
        }

        private static Entity CreateCaster(ref EntityManager em, in FixedString64Bytes spellId)
        {
            var caster = em.CreateEntity();
            ResourceFactory.InitHealth(ref em, caster, 250);
            ResourceFactory.InitMana(ref em, caster, 250);
            EnsureSpellBuffers(ref em, caster, spellId);
            return caster;
        }

        private static int CastSpellAndMeasureDamage(World world, ref EntityManager em, in Entity caster, in FixedString64Bytes spellId, int armor, float resistPercent)
        {
            const int targetHealth = 500;

            var target = em.CreateEntity();
            ResourceFactory.InitHealth(ref em, target, targetHealth, targetHealth);
            var damageable = new Damageable { Armor = armor, ResistPercent = resistPercent };
            if (!em.HasComponent<Damageable>(target))
                em.AddComponentData(target, damageable);
            else
                em.SetComponentData(target, damageable);

            SpellPipelineFactory.Cast(ref em, caster, target, spellId, 0);
            StepWorld(world, 240);

            var health = em.GetComponentData<Health>(target);
            int delta = targetHealth - health.Current;
            em.DestroyEntity(target);
            return delta;
        }

        private static FixedString64Bytes RegisterFullBypassSpell()
        {
            var spellId = (FixedString64Bytes)"test_full_bypass";
            if (SpellDefinitionCatalog.TryGet(spellId, out _))
                return spellId;

            var scope = TargetScope.Single(TargetScopeKind.PrimaryTarget);
            var payload = new EffectPayload
            {
                Kind = EffectPayloadKind.Damage,
                Damage = new DamagePayload
                {
                    Amount = 72,
                    CanCrit = 0,
                    School = DamageSchool.Fire,
                    IgnoreArmor = 1,
                    IgnoreResist = 1,
                    IgnoreSnapshotModifiers = 1
                }
            };

            SpellBuilder
                .NewSpell(spellId)
                .SetSchool(SpellSchool.Fire)
                .SetCastTime(0f)
                .SetCooldown(0f)
                .SetRange(35f)
                .SetTargeting(SpellTargeting.Enemy)
                .SetManaCost(10)
                .AddEffect(scope, payload)
                .Register();

            return spellId;
        }
    }
}
