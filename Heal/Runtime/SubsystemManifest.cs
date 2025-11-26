using Framework.Core.Base;
using Unity.Entities;

namespace Framework.Heal.Runtime
{
    public struct HealSubsystemManifest : ISubsystemManifest
    {
        public void Register(World world, EntityManager em)
        {
            SystemRegistration.RegisterISystemInGroups<Framework.Heal.Resolution.HealResolutionSystem>(world);
            SystemRegistration.RegisterISystemInGroups<HealRuntimeSystem>(world);
            SystemRegistration.RegisterISystemInGroups<Framework.Heal.Telemetry.HealTelemetrySystem>(world);
        }
    }
}
