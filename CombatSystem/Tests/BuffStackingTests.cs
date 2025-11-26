using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Framework.TimedEffect.Runtime;
using Framework.TimedEffect.Components;

namespace Framework.CombatSystem.Tests
{
    public class BuffStackingTests
    {
        [SetUp]
        public void Setup()
        {
            Framework.Buffs.Content.BuffCatalog.Register(new Framework.Buffs.Content.BuffDefinition
            {
                Id = new FixedString64Bytes("StackCap"),
                StackingMode = Framework.Buffs.Content.BuffStackingMode.CapStacks,
                MaxStacks = 3,
                DurationPolicy = Framework.Buffs.Content.BuffDurationPolicy.RefreshOnApply,
                Duration = 10f,
                StackableCount = 1
            });
            Framework.Buffs.Content.BuffCatalog.Register(new Framework.Buffs.Content.BuffDefinition
            {
                Id = new FixedString64Bytes("Refresh"),
                StackingMode = Framework.Buffs.Content.BuffStackingMode.RefreshDuration,
                MaxStacks = 0,
                DurationPolicy = Framework.Buffs.Content.BuffDurationPolicy.RefreshOnApply,
                Duration = 10f,
                StackableCount = 1
            });
        }

        [Test]
        public void CapStacks_CapsAtMax()
        {
            using var world = new World("buff-test");
            var simGroup = world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            var timedSystem = world.GetOrCreateSystem<TimedEffectRuntimeSystem>();
            simGroup.AddSystemToUpdateList(timedSystem);
            simGroup.SortSystems();

            var em = world.EntityManager;
            var e = em.CreateEntity();
            var id = new FixedString64Bytes("StackCap");

            Framework.Buffs.Drivers.BuffDriver.Apply(ref em, e, id, 10f, 2);
            simGroup.Update();
            Framework.Buffs.Drivers.BuffDriver.Apply(ref em, e, id, 10f, 2);
            simGroup.Update();

            Assert.IsTrue(em.HasBuffer<TimedEffectInstance>(e));
            var instances = em.GetBuffer<TimedEffectInstance>(e);
            Assert.AreEqual(1, instances.Length);
            Assert.AreEqual(3, instances[0].StackCount);
        }
    }
}
