using Framework.Core.Base;
using Framework.Pets.Components;
using Framework.Pets.Factory;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Pets.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(RequestsSystemGroup))]
    [UpdateAfter(typeof(PetAttackSystem))]
    public partial struct PetDismissSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var reason = new FixedString64Bytes("dismiss");
            foreach (var (owner, entity) in SystemAPI.Query<RefRO<PetOwner>>().WithAll<PetPendingDismiss>().WithEntityAccess())
            {
                PetFactory.Despawn(ref em, entity, reason);
            }
        }
    }
}
