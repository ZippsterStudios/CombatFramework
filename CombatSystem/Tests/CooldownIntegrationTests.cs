using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace Framework.CombatSystem.Tests
{
    public class CooldownIntegrationTests
    {
        [Test]
        public void Cooldown_PreventsCast()
        {
            using var world = new World("cd-test");
            var em = world.EntityManager;
            var caster = em.CreateEntity();
            var key = new FixedString64Bytes("SpellX");
            var now = 10.0;
            Framework.Cooldowns.Factory.CooldownFactory.ApplyCooldown(ref em, caster, key, now + 5.0);
            var res = Framework.Spells.Policies.SpellPolicy.ValidateCast(em, caster, key, now);
            Assert.AreEqual(Framework.Spells.Policies.SpellPolicy.Result.Reject_NotInSpellbook, res);
            // learn spell to make cooldown check
            Framework.Spells.Spellbook.Drivers.SpellbookDriver.LearnSpell(ref em, caster, key);
            res = Framework.Spells.Policies.SpellPolicy.ValidateCast(em, caster, key, now);
            Assert.AreEqual(Framework.Spells.Policies.SpellPolicy.Result.Reject_OnCooldown, res);
        }
    }
}

