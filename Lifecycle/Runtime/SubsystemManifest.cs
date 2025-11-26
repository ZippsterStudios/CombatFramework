using Framework.Core.Base;
using Framework.Lifecycle.Resolution;
using Unity.Entities;

namespace Framework.Lifecycle.Runtime
{
    public struct LifecycleSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<DeathDetectionSystem>(world);
            SystemRegistration.RegisterISystemInGroups<DeathCleanupSystem>(world);
            SystemRegistration.RegisterISystemInGroups<DeathDespawnSystem>(world);
        }
    }
}
