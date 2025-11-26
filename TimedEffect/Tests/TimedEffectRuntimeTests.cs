using Framework.TimedEffect.Components;
using Framework.TimedEffect.Content;
using Framework.TimedEffect.Events;
using Framework.TimedEffect.Requests;
using Framework.TimedEffect.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace Framework.TimedEffect.Tests
{
    public class TimedEffectRuntimeTests
    {
        [Test]
        public void TimedEffectRuntime_AddsInstanceAndEvent()
        {
            using var world = new World("TimedEffectTestWorld");
            var systemHandle = world.GetOrCreateSystem<TimedEffectRuntimeSystem>();

            var entity = world.EntityManager.CreateEntity();
            var requests = world.EntityManager.AddBuffer<TimedEffectRequest>(entity);
            requests.Add(new TimedEffectRequest
            {
                Target = entity,
                EffectId = (FixedString64Bytes)"test_effect",
                Type = TimedEffectType.Buff,
                StackingMode = TimedEffectStackingMode.AddStacks,
                AddStacks = 1,
                Duration = 3f,
                TickInterval = 0f,
                Source = Entity.Null
            });

            var simGroup = world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            simGroup.AddSystemToUpdateList(systemHandle);
            simGroup.SortSystems();
            simGroup.Update();

            var instances = world.EntityManager.GetBuffer<TimedEffectInstance>(entity);
            Assert.AreEqual(1, instances.Length);
            Assert.AreEqual("test_effect", instances[0].EffectId.ToString());

            var events = world.EntityManager.GetBuffer<TimedEffectEvent>(entity);
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual(TimedEffectEventKind.Added, events[0].Kind);
        }
    }
}
