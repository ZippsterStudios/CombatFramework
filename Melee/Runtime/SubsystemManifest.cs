using Framework.Core.Base;
using Framework.Melee.Runtime.SystemGroups;
using Framework.Melee.Runtime.Systems;
using Unity.Entities;

namespace Framework.Melee.Runtime
{
    public struct MeleeSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterManagedSystemInGroups<MeleeSystemGroup>(world);
            SystemRegistration.RegisterISystemInGroups<MeleePlanBuilderSystem>(world);
            SystemRegistration.RegisterISystemInGroups<MeleePhaseSystem>(world);
            SystemRegistration.RegisterISystemInGroups<MeleeCleaveRollSystem>(world);
            SystemRegistration.RegisterISystemInGroups<MeleeHitDetectionSystem>(world);
            SystemRegistration.RegisterISystemInGroups<MeleeDefenseWindowSystem>(world);
            SystemRegistration.RegisterISystemInGroups<MeleeProcStateSystem>(world);
            SystemRegistration.RegisterISystemInGroups<MeleeCleanupSystem>(world);
        }
    }
}
