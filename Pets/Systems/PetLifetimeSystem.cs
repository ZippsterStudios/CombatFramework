using Framework.Core.Base;
using Framework.Pets.Components;
using Framework.Pets.Events;
using Framework.Pets.Factory;
using Framework.TimedEffect.Events;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Pets.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(ResolutionSystemGroup))]
    public partial struct PetLifetimeSystem : ISystem
    {
        private struct PendingExpiration
        {
            public Entity Owner;
            public Entity Pet;
            public FixedString64Bytes PetId;
            public FixedString32Bytes GroupId;
        }

        private static readonly FixedString64Bytes LifetimeExpiredReason = (FixedString64Bytes)"lifetime_expired";

        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var expired = new NativeList<PendingExpiration>(Allocator.Temp);

            foreach (var (events, lifetime, entity) in SystemAPI.Query<DynamicBuffer<TimedEffectEvent>, RefRO<PetLifetimeTag>>().WithEntityAccess())
            {
                var petId = em.HasComponent<PetIdentity>(entity) ? em.GetComponentData<PetIdentity>(entity).PetId : default;
                var groupId = em.HasComponent<PetGroup>(entity) ? em.GetComponentData<PetGroup>(entity).Id : default;

                for (int i = events.Length - 1; i >= 0; i--)
                {
                    var evt = events[i];
                    if (!evt.EffectId.Equals(lifetime.ValueRO.EffectId))
                        continue;

                    if (evt.Kind == TimedEffectEventKind.Removed)
                    {
                        expired.Add(new PendingExpiration
                        {
                            Owner = evt.Source,
                            Pet = entity,
                            PetId = petId,
                            GroupId = groupId
                        });
                    }

                    events.RemoveAt(i);
                }
            }

            for (int i = 0; i < expired.Length; i++)
            {
                var entry = expired[i];
                PetEventUtility.Emit<PetExpiredEvent>(ref em, entry.Owner, entry.Pet, entry.PetId, entry.GroupId, LifetimeExpiredReason);
                PetFactory.Despawn(ref em, entry.Pet, LifetimeExpiredReason);
            }

            expired.Dispose();
        }
    }
}
