using Unity.Entities;

namespace Framework.CombatSystem.Bootstrap
{
    public static class CombatWorldBootstrap
    {
        // Call from your World initialization to install the combat systems.
        public static void Initialize(World world)
        {
            CombatSystemInstaller.Install(world);
        }
    }
}

