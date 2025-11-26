using Framework.Contracts.Perception;
using Framework.Core.Base;
using Framework.Pets.Components;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Pets.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(RequestsSystemGroup))]
    [UpdateAfter(typeof(PetFollowSystem))]
    public partial struct PetGuardSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            foreach (var (guard, leashRef, shimRef, pet) in SystemAPI.Query<RefRW<PetGuardAnchor>, RefRW<LeashConfig>, RefRW<PetLeashConfigShim>>().WithEntityAccess())
            {
                var guardData = guard.ValueRW;
                if (guardData.AnchorEntity != Entity.Null && em.Exists(guardData.AnchorEntity) && em.HasComponent<Framework.Core.Components.Position>(guardData.AnchorEntity))
                {
                    guardData.Position = em.GetComponentData<Framework.Core.Components.Position>(guardData.AnchorEntity).Value;
                    guard.ValueRW = guardData;
                }

                leashRef.ValueRW.Home = guardData.Position;
                shimRef.ValueRW.Home = guardData.Position;
            }
        }
    }
}
