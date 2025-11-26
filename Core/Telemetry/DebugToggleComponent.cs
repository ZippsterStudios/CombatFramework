using Unity.Entities;

namespace Framework.Core.Telemetry
{
    public struct DebugToggleComponent : IComponentData
    {
        public bool Enabled;
    }
}

