using NUnit.Framework;
using Unity.Mathematics;
using Unity.Entities;

namespace Framework.Core.Tests
{
    public class DeterminismTests
    {
        [Test]
        public void RandomSequence_IsDeterministic_WithSameSeed()
        {
            const uint seed = 12345u;
            var rngA = new Random(seed);
            var rngB = new Random(seed);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(rngA.NextUInt(), rngB.NextUInt());
            }
        }

        [Test]
        public void ResourceRegen_IsDeterministic()
        {
            using var worldA = new World("DeterminismA");
            using var worldB = new World("DeterminismB");
            Framework.Core.Base.SubsystemBootstrap.InstallAll(worldA);
            Framework.Core.Base.SubsystemBootstrap.InstallAll(worldB);

            var emA = worldA.EntityManager;
            var emB = worldB.EntityManager;

            var eA = emA.CreateEntity();
            var eB = emB.CreateEntity();

            emA.AddComponentData(eA, new Framework.Resources.Components.Health { Current = 0, Max = 100, RegenPerSecond = 5, RegenAccumulator = 0 });
            emB.AddComponentData(eB, new Framework.Resources.Components.Health { Current = 0, Max = 100, RegenPerSecond = 5, RegenAccumulator = 0 });

            var simA = worldA.GetExistingSystemManaged<Unity.Entities.SimulationSystemGroup>();
            var simB = worldB.GetExistingSystemManaged<Unity.Entities.SimulationSystemGroup>();

            for (int i = 0; i < 10; i++) { simA.Update(); simB.Update(); }

            var ha = emA.GetComponentData<Framework.Resources.Components.Health>(eA);
            var hb = emB.GetComponentData<Framework.Resources.Components.Health>(eB);
            Assert.AreEqual(ha.Current, hb.Current);
        }
    }
}
