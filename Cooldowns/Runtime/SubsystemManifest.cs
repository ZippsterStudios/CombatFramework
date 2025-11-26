using Framework.Core.Base;
using Unity.Entities;

namespace Framework.Cooldowns.Runtime
{
    public struct CooldownSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<Framework.Cooldowns.Resolution.CooldownResolutionSystem>(world);
            SystemRegistration.RegisterISystemInGroups<CooldownRuntimeSystem>(world);
        }
    }
}
