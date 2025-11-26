using Framework.Resources.Components;
using Framework.Temporal.Components;
using Framework.Temporal.Drivers;
using Framework.Temporal.Runtime;
using NUnit.Framework;
using Unity.Entities;

namespace Framework.Temporal.Tests
{
    public class TemporalAnchorTests
    {
        [Test]
        public void AttachAnchor_RecordsDamageEvent()
        {
            using var world = new World("TemporalAnchorTests.Attach");
            var em = world.EntityManager;

            var entity = em.CreateEntity();
            em.AddComponentData(entity, new Health { Current = 100, Max = 100 });

            Assert.IsTrue(TemporalAnchorDriver.AttachAnchor(ref em, entity, Entity.Null, duration: 10f, retention: 10f));
            TemporalAnchorDriver.RecordDamage(ref em, entity, 45f);

            var events = em.GetBuffer<TemporalEvent>(entity);
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual(TemporalEventType.Damage, events[0].Type);
            Assert.AreEqual(45f, events[0].Magnitude, 1e-3f);
            Assert.AreEqual(0f, events[0].Timestamp, 1e-4f);
        }

        [Test]
        public void ReleaseRequest_ProducesTemporalResult()
        {
            using var world = new World("TemporalAnchorTests.Release");
            var em = world.EntityManager;

            var entity = em.CreateEntity();
            em.AddComponentData(entity, new Health { Current = 100, Max = 100 });

            TemporalAnchorDriver.AttachAnchor(ref em, entity, Entity.Null, duration: 8f, retention: 8f);
            TemporalAnchorDriver.RecordDamage(ref em, entity, 60f);
            TemporalAnchorDriver.QueueRelease(ref em, entity, Entity.Null, factor: 0.5f, windowSeconds: 8f, healDuration: 0f, healTickInterval: 0f);

            var releaseHandle = world.GetOrCreateSystem<TemporalReleaseSystem>();
            var resolutionGroup = world.GetOrCreateSystemManaged<Framework.Core.Base.ResolutionSystemGroup>();
            resolutionGroup.AddSystemToUpdateList(releaseHandle);

            var simulation = world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            simulation.AddSystemToUpdateList(resolutionGroup);
            simulation.SortSystems();
            simulation.Update();

            Assert.IsFalse(em.HasComponent<TemporalAnchor>(entity));
            Assert.IsFalse(em.HasBuffer<TemporalEvent>(entity));
            var results = em.GetBuffer<TemporalReleaseResult>(entity);
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(30f, results[0].HealAmount, 1e-3f);
            Assert.AreEqual(0f, results[0].HealDuration, 1e-3f);
        }
    }
}
