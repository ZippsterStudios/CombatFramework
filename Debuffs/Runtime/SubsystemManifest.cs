using Framework.Core.Base;
using Framework.Debuffs.Resolution;
using Unity.Entities;

namespace Framework.Debuffs.Runtime
{
    public struct DebuffSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<DebuffRuntimeSystem>(world);
            SystemRegistration.RegisterISystemInGroups<DebuffResolutionSystem>(world);
        }
    }
}
