using Framework.Core.Base;
using Unity.Entities;

namespace Framework.Resources.Runtime
{
    public struct ResourceSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<Framework.Resources.Resolution.ResourceResolutionSystem>(world);
            SystemRegistration.RegisterISystemInGroups<ResourceRuntimeSystem>(world);
        }
    }
}
