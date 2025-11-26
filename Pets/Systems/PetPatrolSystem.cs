using Framework.Contracts.Intents;
using Framework.Core.Base;
using Framework.Core.Components;
using Framework.Pets.Components;
using Framework.Pets.Contracts;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Pets.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(RequestsSystemGroup))]
    [UpdateAfter(typeof(PetGuardSystem))]
    public partial struct PetPatrolSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (waypoints, patrolState, moveIntent, position) in SystemAPI.Query<DynamicBuffer<PetWaypoint>, RefRW<PetPatrolState>, RefRW<MoveIntent>, RefRO<Position>>())
            {
                var stateData = patrolState.ValueRW;
                if (stateData.Active == 0 || waypoints.Length == 0)
                    continue;

                if (stateData.NextWaypointIndex >= waypoints.Length)
                    stateData.NextWaypointIndex = 0;

                var waypoint = waypoints[stateData.NextWaypointIndex].Value;
                var destination = new float2(waypoint.x, waypoint.y);
                var delta = destination - position.ValueRO.Value;

                if (math.lengthsq(delta) <= 0.25f)
                {
                    stateData.NextWaypointIndex = (stateData.NextWaypointIndex + 1) % math.max(1, waypoints.Length);
                    patrolState.ValueRW = stateData;
                    continue;
                }

                var intent = moveIntent.ValueRW;
                intent.Active = 1;
                intent.Mode = (byte)AIMoveMode.Chase;
                intent.Destination = destination;
                intent.Speed = math.max(intent.Speed, 3f);
                moveIntent.ValueRW = intent;
                patrolState.ValueRW = stateData;
            }
        }
    }
}
