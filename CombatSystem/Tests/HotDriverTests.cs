using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace Framework.CombatSystem.Tests
{
    public class HotDriverTests
    {
        [Test]
        public void HotDriver_AddsInstance()
        {
            using var world = new World("hot-test");
            var em = world.EntityManager;
            var e = em.CreateEntity();
            Framework.HOT.Drivers.HotDriver.Apply(ref em, e, (FixedString64Bytes)"renew", hps: 5, interval: 1f, duration: 3f, source: Entity.Null);
            var buf = em.GetBuffer<Framework.HOT.Components.HotInstance>(e);
            Assert.AreEqual(1, buf.Length);
            Assert.AreEqual(5, buf[0].HealPerTick);
            Assert.AreEqual((FixedString64Bytes)"renew", buf[0].EffectId);
        }
    }
}

