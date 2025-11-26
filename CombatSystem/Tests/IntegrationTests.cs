using NUnit.Framework;
using Unity.Entities;
using Framework.Core.Base;
using Framework.Resources.Components;
using Framework.DOT.Requests;
using Framework.HOT.Requests;
using Framework.Damage.Components;
using Unity.Collections;

namespace Framework.CombatSystem.Tests
{
    public class IntegrationTests
    {
        [Test]
        public void Subsystems_Register_And_Tick()
        {
            using var world = new World("IntegrationWorld");
            SubsystemBootstrap.InstallAll(world);
            var em = world.EntityManager;

            var entity = em.CreateEntity();
            em.AddComponentData(entity, new Health { Current = 50, Max = 100, RegenPerSecond = 0, RegenAccumulator = 0 });
            em.AddComponentData(entity, new Damageable { Armor = 0, ResistPercent = 0 });

            var dotBuf = em.AddBuffer<DotRequest>(entity);
            dotBuf.Add(new DotRequest { Target = entity, EffectId = (FixedString64Bytes)"integration_dot", Dps = 5, TickInterval = 1f, Duration = 3f, Source = Entity.Null });

            var hotBuf = em.AddBuffer<HotRequest>(entity);
            hotBuf.Add(new HotRequest { Target = entity, EffectId = (FixedString64Bytes)"integration_hot", Hps = 3, TickInterval = 1f, Duration = 3f, Source = Entity.Null });

            var sim = world.GetExistingSystemManaged<SimulationSystemGroup>();

            for (int i = 0; i < 4; i++)
            {
                sim.Update();
            }

            var h = em.GetComponentData<Health>(entity);
            Assert.GreaterOrEqual(h.Current, 0);
            Assert.LessOrEqual(h.Current, h.Max);
        }
    }
}
