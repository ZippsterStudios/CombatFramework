using Framework.Core.Base;
using Unity.Entities;

namespace Framework.Shadow.Runtime
{
    public struct ShadowSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<ShadowManagerSystem>(world);
            SystemRegistration.RegisterISystemInGroups<ShadowHighlightSystem>(world);
        }
    }
}
