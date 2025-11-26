using NUnit.Framework;
using Unity.Entities;

namespace Framework.CombatSystem.Tests
{
    public class HealSubsystemTests
    {
        [Test]
        public void HealDriver_ClampsToMax()
        {
            using var world = new World("heal-test");
            var em = world.EntityManager;
            var e = em.CreateEntity();
            Framework.Resources.Factory.ResourceFactory.InitHealth(ref em, e, max: 100, current: 50);

            Framework.Heal.Drivers.HealDriver.Apply(ref em, e, 1000);

            var h = em.GetComponentData<Framework.Resources.Components.Health>(e);
            Assert.AreEqual(100, h.Current);
        }
    }
}

