using Framework.Core.Base;
using Unity.Entities;

namespace Framework.TimedEffect.Runtime
{
    public struct TimedEffectSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<TimedEffectRuntimeSystem>(world);
        }
    }
}
