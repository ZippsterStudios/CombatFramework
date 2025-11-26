using Framework.Core.Base;
using Unity.Entities;

namespace Framework.DOT.Runtime
{
    public struct DotSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<Framework.DOT.Resolution.DotResolutionSystem>(world);
            SystemRegistration.RegisterISystemInGroups<DotRuntimeSystem>(world);
        }
    }
}
