using NUnit.Framework;
using Unity.Entities;
using Framework.Buffs.Drivers;
using Framework.TimedEffect.Requests;
using Unity.Collections;

namespace Framework.Core.Tests
{
    public class BaseDriverTests
    {
        [Test]
        public void BuffDriver_EnqueuesTimedEffectRequest()
        {
            using var world = new World("TestWorld");
            var em = world.EntityManager;
            var e = em.CreateEntity();

            var id = new FixedString64Bytes("TestBuff");
            BuffDriver.Apply(ref em, e, id, 1f, 2);

            Assert.IsTrue(em.HasBuffer<TimedEffectRequest>(e));
            var requests = em.GetBuffer<TimedEffectRequest>(e);
            Assert.AreEqual(1, requests.Length);
            Assert.AreEqual(id, requests[0].EffectId);
            Assert.AreEqual(2, requests[0].AddStacks);
        }
    }
}
