using Framework.Melee.Components;
using Framework.Melee.Runtime.SystemGroups;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Melee.Runtime.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(MeleeSystemGroup))]
    [UpdateAfter(typeof(MeleeHitDetectionSystem))]
    public partial struct MeleeDefenseWindowSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            double now = SystemAPI.Time.ElapsedTime;
            foreach (var window in SystemAPI.Query<RefRW<MeleeDefenseWindowState>>())
            {
                if (window.ValueRO.ParryWindowActive != 0 && window.ValueRO.WindowExpiry <= now)
                {
                    window.ValueRW = new MeleeDefenseWindowState
                    {
                        ParryWindowActive = 0,
                        WindowExpiry = 0,
                        WindowId = window.ValueRO.WindowId
                    };
                }
            }
        }
    }
}
