using NUnit.Framework;
using Unity.Entities;
using Framework.Buffs.Requests;
using Framework.Buffs.Components;
using Framework.CombatSystem.Bootstrap;

namespace Framework.CombatSystem.Tests
{
    public class SubsystemLinkTests
    {
        [Test]
        public void BuffRequest_BufferExists_AfterFactoryEnsure()
        {
            using var world = new World("LinkWorld");
            CombatWorldBootstrap.Initialize(world);
            var em = world.EntityManager;
            var e = em.CreateEntity();
            Framework.Buffs.Factory.BuffFactory.EnsureBuffer(ref em, e);
            Assert.IsTrue(em.HasBuffer<Framework.Buffs.Factory.BuffFactory.PendingBuff>(e));
        }
    }
}
