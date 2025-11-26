using Unity.Entities;

namespace Framework.Core.Base
{
    // Custom group to allow future ordering between combat systems if needed.
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CombatSimulationSystemGroup : ComponentSystemGroup { }
}
