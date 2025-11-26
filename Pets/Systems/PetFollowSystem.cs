using Framework.Contracts.Perception;
using Framework.Core.Base;
using Framework.Pets.Components;
using Framework.Pets.Factory;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Pets.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(RequestsSystemGroup))]
    [UpdateAfter(typeof(PetCommandRouterSystem))]
    public partial struct PetFollowSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            foreach (var (ownerRef, leash, shim, guard, pet) in SystemAPI.Query<RefRO<PetOwner>, RefRW<LeashConfig>, RefRW<PetLeashConfigShim>, RefRO<PetGuardAnchor>>().WithEntityAccess())
            {
                var owner = ownerRef.ValueRO.Value;
                if (owner == Entity.Null || !em.Exists(owner))
                    continue;

                if (guard.ValueRO.AnchorEntity != Entity.Null && guard.ValueRO.AnchorEntity != owner)
                    continue;

                var home = PetTeamUtility.GetOwnerPosition(ref em, owner);
                leash.ValueRW.Home = home;
                shim.ValueRW.Home = home;
            }
        }
    }
}
