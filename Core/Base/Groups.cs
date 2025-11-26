using Unity.Entities;

namespace Framework.Core.Base
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public sealed partial class RequestsSystemGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RequestsSystemGroup))]
    public sealed partial class ResolutionSystemGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ResolutionSystemGroup))]
    public sealed partial class RuntimeSystemGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RuntimeSystemGroup))]
    public sealed partial class TelemetrySystemGroup : ComponentSystemGroup { }
}
