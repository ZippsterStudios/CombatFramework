using Framework.Melee.Components;
using Framework.Melee.Runtime.SystemGroups;
using Framework.Melee.Runtime.Utilities;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Melee.Runtime.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(MeleeSystemGroup))]
    [UpdateAfter(typeof(MeleeProcStateSystem))]
    public partial struct MeleeCleanupSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var beginSim = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = beginSim.CreateCommandBuffer(state.WorldUnmanaged);
            var telemetry = SystemAPI.GetSingletonBuffer<MeleeTelemetryEvent>();

            foreach (var (context, entity) in SystemAPI.Query<RefRO<MeleeCastContext>>().WithEntityAccess())
            {
                if (context.ValueRO.Completed == 0)
                    continue;

                MeleeTelemetryWriter.Write(ref telemetry, MeleeTelemetryEventType.SwingCompleted, context.ValueRO.Attacker, Entity.Null, context.ValueRO.WeaponSlot, context.ValueRO.SequenceId);
                ecb.DestroyEntity(entity);
            }
        }
    }
}
