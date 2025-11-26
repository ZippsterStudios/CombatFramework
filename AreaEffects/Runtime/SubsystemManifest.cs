using Framework.Core.Base;
using Unity.Entities;

namespace Framework.AreaEffects.Runtime
{
    public struct AreaEffectSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<Framework.AreaEffects.Resolution.AreaEffectResolutionSystem>(world);
            SystemRegistration.RegisterISystemInGroups<AreaEffectRuntimeSystem>(world);
        }
    }
}
