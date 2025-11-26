using Framework.Contracts.AI;
using Framework.Core.Base;
using Framework.Pets.Components;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Pets.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(RequestsSystemGroup))]
    [UpdateAfter(typeof(PetPatrolSystem))]
    public partial struct PetAttackSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            foreach (var (owner, targetRef) in SystemAPI.Query<RefRO<PetOwner>, RefRW<AIAgentTarget>>())
            {
                var target = targetRef.ValueRW;
                if (target.Value != Entity.Null && !em.Exists(target.Value))
                {
                    target = AIAgentTarget.CreateDefault();
                    targetRef.ValueRW = target;
                    continue;
                }

                if (target.Value == Entity.Null && owner.ValueRO.Value != Entity.Null && em.Exists(owner.ValueRO.Value) && em.HasComponent<AIAgentTarget>(owner.ValueRO.Value))
                {
                    var ownerTarget = em.GetComponentData<AIAgentTarget>(owner.ValueRO.Value);
                    if (ownerTarget.Value != Entity.Null && em.Exists(ownerTarget.Value))
                    {
                        target.Value = ownerTarget.Value;
                        target.Visibility = ownerTarget.Visibility;
                        targetRef.ValueRW = target;
                    }
                }
            }
        }
    }
}
