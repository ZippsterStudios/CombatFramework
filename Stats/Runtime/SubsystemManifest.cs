using Framework.Core.Base;
using Unity.Entities;

namespace Framework.Stats.Runtime
{
    public struct StatSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<Framework.Stats.Resolution.StatsResolutionSystem>(world);
            SystemRegistration.RegisterISystemInGroups<StatRuntimeSystem>(world);
        }
    }
}
