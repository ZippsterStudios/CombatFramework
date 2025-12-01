using NUnit.Framework;
using Unity.Entities;
using RuntimeSpellMetadata = Framework.Spells.Runtime.SpellRuntimeMetadata;

namespace Framework.CombatSystem.Tests
{
    public class FeatureRegistryTests
    {
        [Test]
        public void DirectHeal_EnqueuesRequest()
        {
            using var world = new World("feature-test");
            var em = world.EntityManager;
            var caster = em.CreateEntity();
            var target = em.CreateEntity();
            var meta = new RuntimeSpellMetadata
            {
                CategoryId = default,
                CategoryLevel = 0,
                SpellLevel = 1,
                Rank = Framework.Spells.Content.SpellRank.Unspecified
            };

            var eff = new Framework.Spells.Content.SpellEffect
            {
                Kind = Framework.Spells.Content.SpellEffectKind.DirectHeal,
                Magnitude = 10
            };

            Framework.Spells.Features.FeatureRegistry.Execute(ref em, caster, target, in meta, in eff);
            var buf = em.GetBuffer<Framework.Heal.Requests.HealRequest>(target);
            Assert.AreEqual(1, buf.Length);
            Assert.AreEqual(10, buf[0].Amount);
        }
    }
}
