using Unity.Collections;
using Unity.Entities;

namespace Framework.Pets.Events
{
    public static class PetEventUtility
    {
        public static void Emit<TEvent>(ref EntityManager em, in Entity owner, in Entity pet, in FixedString64Bytes petId, in FixedString32Bytes groupId, in FixedString64Bytes reason = default, float value = 0f)
            where TEvent : unmanaged, IComponentData
        {
            var evt = em.CreateEntity();
            em.AddComponent<TEvent>(evt);
            var payload = em.AddBuffer<PetEventPayload>(evt);
            payload.Add(new PetEventPayload
            {
                Owner = owner,
                Pet = pet,
                PetId = petId,
                GroupId = groupId,
                Reason = reason,
                Value = value
            });
        }

        public static void Emit<TEvent>(ref EntityCommandBuffer ecb, in Entity owner, in Entity pet, in FixedString64Bytes petId, in FixedString32Bytes groupId, in FixedString64Bytes reason = default, float value = 0f)
            where TEvent : unmanaged, IComponentData
        {
            var evt = ecb.CreateEntity();
            ecb.AddComponent<TEvent>(evt);
            var payload = ecb.AddBuffer<PetEventPayload>(evt);
            payload.Add(new PetEventPayload
            {
                Owner = owner,
                Pet = pet,
                PetId = petId,
                GroupId = groupId,
                Reason = reason,
                Value = value
            });
        }
    }
}
