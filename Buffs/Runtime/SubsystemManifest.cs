using Framework.Core.Base;
using Unity.Entities;

namespace Framework.Buffs.Runtime
{
    public struct BuffSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<Framework.Buffs.Resolution.BuffResolutionSystem>(world);
            SystemRegistration.RegisterISystemInGroups<BuffRuntimeSystem>(world);
        }
    }
}
