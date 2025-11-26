using Framework.Core.Base;
using Unity.Entities;

namespace Framework.Spells.Tests
{
    internal static class TestWorldUtility
    {
        public static void EnsureBaseSystemGroups(World world)
        {
            if (world == null || !world.IsCreated)
                return;

            world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            world.GetOrCreateSystemManaged<RequestsSystemGroup>();
            world.GetOrCreateSystemManaged<ResolutionSystemGroup>();
            world.GetOrCreateSystemManaged<RuntimeSystemGroup>();
            world.GetOrCreateSystemManaged<TelemetrySystemGroup>();
            world.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            world.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        public static void InitializeCombatWorld(World world)
        {
            EnsureBaseSystemGroups(world);
            SubsystemBootstrap.InstallAll(world);
        }
    }
}
