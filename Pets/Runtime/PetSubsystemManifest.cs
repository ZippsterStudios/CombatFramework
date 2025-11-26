using Framework.Core.Base;
using Framework.Pets.Systems;
using Unity.Entities;

namespace Framework.Pets.Runtime
{
    public struct PetSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<SummonFromSpellHook>(world);
            SystemRegistration.RegisterISystemInGroups<PetCommandRouterSystem>(world);
            SystemRegistration.RegisterISystemInGroups<PetFollowSystem>(world);
            SystemRegistration.RegisterISystemInGroups<PetGuardSystem>(world);
            SystemRegistration.RegisterISystemInGroups<PetPatrolSystem>(world);
            SystemRegistration.RegisterISystemInGroups<PetAttackSystem>(world);
            SystemRegistration.RegisterISystemInGroups<PetDismissSystem>(world);
            SystemRegistration.RegisterISystemInGroups<PetSymbiosisSystem>(world);
            SystemRegistration.RegisterISystemInGroups<PetLifetimeSystem>(world);
            SystemRegistration.RegisterISystemInGroups<PetSymbiosisSyncSystem>(world);
        }
    }
}
