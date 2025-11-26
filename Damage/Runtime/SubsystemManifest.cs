using Framework.Core.Base;
using Unity.Entities;

namespace Framework.Damage.Runtime
{
    public struct DamageSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<DamageRequestBufferSyncSystem>(world);
            SystemRegistration.RegisterISystemInGroups<DamageRuntimeSystem>(world);
            SystemRegistration.RegisterISystemInGroups<Framework.Damage.Resolution.DamageResolutionSystem>(world);
        }
    }
}
