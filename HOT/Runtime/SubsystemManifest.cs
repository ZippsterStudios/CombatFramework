using Framework.Core.Base;
using Unity.Entities;

namespace Framework.HOT.Runtime
{
    public struct HotSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<Framework.HOT.Resolution.HotResolutionSystem>(world);
            SystemRegistration.RegisterISystemInGroups<HotRuntimeSystem>(world);
        }
    }
}
