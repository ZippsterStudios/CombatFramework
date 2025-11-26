using Framework.HOT.Components;
using Framework.HOT.Resolution;
using Framework.Resources.Components;
using Framework.Resources.Factory;
using Framework.Temporal.Components;
using NUnit.Framework;
using Unity.Entities;

namespace Framework.HOT.Tests
{
    public class TemporalReleaseHotSystemTests
    {
        [Test]
        public void TemporalReleaseHotSystem_AppliesInstantHeal()
        {
            using var world = new World("TemporalReleaseHot.Instant");
            var em = world.EntityManager;

            var entity = em.CreateEntity();
            ResourceFactory.InitHealth(ref em, entity, max: 200, current: 100);

            var results = em.AddBuffer<TemporalReleaseResult>(entity);
            results.Add(new TemporalReleaseResult
            {
                Source = Entity.Null,
                HealAmount = 75f,
                HealDuration = 0f,
                HealTickInterval = 0f
            });

            var releaseHandle = world.GetOrCreateSystem<TemporalReleaseHotSystem>();
            var resolutionGroup = world.GetOrCreateSystemManaged<Framework.Core.Base.ResolutionSystemGroup>();
            resolutionGroup.AddSystemToUpdateList(releaseHandle);

            var simGroup = world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            simGroup.AddSystemToUpdateList(resolutionGroup);
            simGroup.SortSystems();
            simGroup.Update();

            Assert.IsFalse(em.HasBuffer<TemporalReleaseResult>(entity));
            var health = em.GetComponentData<Health>(entity);
            Assert.AreEqual(175, health.Current);
            Assert.IsFalse(em.HasBuffer<HotInstance>(entity));
        }

        [Test]
        public void TemporalReleaseHotSystem_SchedulesHotWhenDurationProvided()
        {
            using var world = new World("TemporalReleaseHot.HoT");
            var em = world.EntityManager;

            var entity = em.CreateEntity();
            ResourceFactory.InitHealth(ref em, entity, max: 100, current: 50);

            var results = em.AddBuffer<TemporalReleaseResult>(entity);
            results.Add(new TemporalReleaseResult
            {
                Source = Entity.Null,
                HealAmount = 40f,
                HealDuration = 4f,
                HealTickInterval = 1f
            });

            var releaseHandle = world.GetOrCreateSystem<TemporalReleaseHotSystem>();
            var resolutionGroup = world.GetOrCreateSystemManaged<Framework.Core.Base.ResolutionSystemGroup>();
            resolutionGroup.AddSystemToUpdateList(releaseHandle);

            var simGroup = world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            simGroup.AddSystemToUpdateList(resolutionGroup);
            simGroup.SortSystems();
            simGroup.Update();

            Assert.IsFalse(em.HasBuffer<TemporalReleaseResult>(entity));
            Assert.IsTrue(em.HasBuffer<HotInstance>(entity));
            var hotBuffer = em.GetBuffer<HotInstance>(entity);
            Assert.AreEqual(1, hotBuffer.Length);
            Assert.Greater(hotBuffer[0].HealPerTick, 0);
            Assert.AreEqual(10, hotBuffer[0].HealPerTick);
        }
    }
}
