using Framework.Core.Base;
using Unity.Entities;

namespace Framework.Threat.Runtime
{
    public struct ThreatSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<Framework.Threat.Resolution.ThreatResolutionSystem>(world);
            SystemRegistration.RegisterISystemInGroups<ThreatRuntimeSystem>(world);
        }
    }
}
