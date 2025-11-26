using Framework.Core.Base;
using Unity.Entities;

namespace Framework.CombatSystem.Bootstrap
{
    public static class CombatSystemInstaller
    {
        public static void Install(World world)
        {
            var em = world.EntityManager;
            SubsystemManifestRegistry.Build(world, em);
        }
    }
}

