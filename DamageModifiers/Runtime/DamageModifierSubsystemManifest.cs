using Framework.Core.Base;
using Unity.Entities;

namespace Framework.DamageModifiers.Runtime
{
    public struct DamageModifierSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<Framework.DamageModifiers.Resolution.DamageModifierSystem>(world);
        }
    }
}
