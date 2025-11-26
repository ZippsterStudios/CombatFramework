using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace Framework.CombatSystem.Tests
{
    public class DotDriverTests
    {
        [Test]
        public void DotDriver_AddsInstance()
        {
            using var world = new World("dot-test");
            var em = world.EntityManager;
            var e = em.CreateEntity();
            Framework.DOT.Drivers.DotDriver.Apply(ref em, e, (FixedString64Bytes)"ignite", dps: 5, interval: 1f, duration: 3f, source: Entity.Null);
            var buf = em.GetBuffer<Framework.DOT.Components.DotInstance>(e);
            Assert.AreEqual(1, buf.Length);
            Assert.AreEqual(5, buf[0].DamagePerTick);
            Assert.AreEqual((FixedString64Bytes)"ignite", buf[0].EffectId);
        }
    }
}
