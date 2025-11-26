using Framework.Core.Base;
using Unity.Entities;

namespace Framework.Temporal.Runtime
{
    public struct TemporalSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<Framework.Temporal.Resolution.TemporalResolutionSystem>(world);
            SystemRegistration.RegisterISystemInGroups<TemporalRuntimeSystem>(world);
        }
    }
}
