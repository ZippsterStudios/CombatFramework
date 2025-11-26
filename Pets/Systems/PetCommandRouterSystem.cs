using Framework.Core.Base;
using Framework.Pets.Contracts;
using Framework.Pets.Policies;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Pets.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(RequestsSystemGroup))]
    [UpdateAfter(typeof(SummonFromSpellHook))]
    public partial struct PetCommandRouterSystem : ISystem
    {
        private NativeList<Entity> _targets;

        public void OnCreate(ref SystemState state)
        {
            _targets = new NativeList<Entity>(Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_targets.IsCreated)
                _targets.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            foreach (var (requests, owner) in SystemAPI.Query<DynamicBuffer<PetCommandRequest>>().WithEntityAccess())
            {
                for (int i = 0; i < requests.Length; i++)
                    PetCommandPolicy.Dispatch(ref em, owner, requests[i], ref _targets);
                requests.Clear();
            }
        }
    }
}
