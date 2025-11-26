using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Framework.TimedEffect.Runtime;
using Framework.TimedEffect.Components;

namespace Framework.CombatSystem.Tests
{
    public class DebuffDriverTests
    {
        [SetUp]
        public void Setup()
        {
            Framework.Debuffs.Content.DebuffCatalog.Register(new Framework.Debuffs.Content.DebuffDefinition
            {
                Id = new FixedString64Bytes("Rooted"),
                Flags = Framework.Debuffs.Content.DebuffFlags.Root,
                Duration = 5f,
                StackingMode = Framework.Debuffs.Content.DebuffStackingMode.CapStacks,
                DurationPolicy = Framework.Debuffs.Content.DebuffDurationPolicy.RefreshOnApply,
                MaxStacks = 2,
                StackableCount = 1
            });
        }

        [Test]
        public void DebuffDriver_ProducesTimedEffectRequest()
        {
            using var world = new World("debuff-test");
            var simGroup = world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            var timedSystem = world.GetOrCreateSystem<TimedEffectRuntimeSystem>();
            simGroup.AddSystemToUpdateList(timedSystem);
            simGroup.SortSystems();

            var em = world.EntityManager;
            var entity = em.CreateEntity();
            var id = new FixedString64Bytes("Rooted");

            Framework.Debuffs.Drivers.DebuffDriver.Apply(ref em, entity, Entity.Null, id, 0f, 1,
                Framework.Debuffs.Content.DebuffFlags.None);

            Assert.IsTrue(em.HasBuffer<Framework.TimedEffect.Requests.TimedEffectRequest>(entity));
            var requests = em.GetBuffer<Framework.TimedEffect.Requests.TimedEffectRequest>(entity);
            Assert.AreEqual(1, requests.Length);
            Assert.AreEqual(id, requests[0].EffectId);

            simGroup.Update();

            Assert.IsTrue(em.HasBuffer<TimedEffectInstance>(entity));
            var instances = em.GetBuffer<TimedEffectInstance>(entity);
            Assert.AreEqual(1, instances.Length);
            Assert.AreEqual(id, instances[0].EffectId);
            Assert.AreEqual(1, instances[0].StackCount);
        }
    }
}
