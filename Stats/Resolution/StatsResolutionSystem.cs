using Unity.Burst;
using Unity.Entities;

namespace Framework.Stats.Resolution
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.ResolutionSystemGroup))]
    public partial struct StatsResolutionSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var stat in SystemAPI.Query<RefRW<Components.StatValue>>())
            {
                var s = stat.ValueRW;
                var mul = s.Multiplier == 0f ? 1f : s.Multiplier;
                s.Value = (s.BaseValue + s.Additive) * mul;
                stat.ValueRW = s;
            }
        }
    }
}
