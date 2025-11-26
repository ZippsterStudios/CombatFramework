using Framework.Core.Base;
using Framework.Pets.Components;
using Framework.Pets.Content;
using Framework.Resources.Components;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Pets.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(RuntimeSystemGroup), OrderLast = true)]
    public partial struct PetSymbiosisSyncSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            foreach (var (participants, ownerHealth) in SystemAPI.Query<DynamicBuffer<PetSymbiosisParticipant>, RefRO<Health>>())
            {
                for (int i = 0; i < participants.Length; i++)
                {
                    var participant = participants[i];
                    if (participant.Mode != PetSymbiosisMode.SharedPool)
                        continue;

                    if (!em.Exists(participant.Pet) || !em.HasComponent<Health>(participant.Pet))
                        continue;

                    em.SetComponentData(participant.Pet, ownerHealth.ValueRO);
                }
            }
        }
    }
}
