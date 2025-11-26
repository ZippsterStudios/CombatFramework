using Framework.Core.Base;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Heal.Resolution
{
    [BurstCompile]
    [UpdateInGroup(typeof(ResolutionSystemGroup))]
    public partial struct HealResolutionSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // No periodic resolution needed for direct heal requests; they are applied in runtime.
        }
    }
}
