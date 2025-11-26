using Framework.Pets.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Pets.Drivers
{
    public static class PetQuery
    {
        public static DynamicBuffer<PetIndex> EnsureIndex(ref EntityManager em, in Entity owner)
        {
            if (!em.HasBuffer<PetIndex>(owner))
                em.AddBuffer<PetIndex>(owner);
            return em.GetBuffer<PetIndex>(owner);
        }

        public static void TrackPet(ref EntityManager em, in Entity owner, in Entity pet, in FixedString64Bytes petId, in FixedString32Bytes groupId, byte swarmLock, int sequence)
        {
            var buffer = EnsureIndex(ref em, owner);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Pet == pet)
                {
                    var entry = buffer[i];
                    entry.PetId = petId;
                    entry.GroupId = groupId;
                    entry.SwarmLock = swarmLock;
                    entry.Sequence = sequence;
                    buffer[i] = entry;
                    return;
                }
            }

            buffer.Add(new PetIndex
            {
                Pet = pet,
                PetId = petId,
                GroupId = groupId,
                SwarmLock = swarmLock,
                Sequence = sequence
            });
        }

        public static void RemovePet(ref EntityManager em, in Entity owner, in Entity pet)
        {
            if (!em.HasBuffer<PetIndex>(owner))
                return;

            var buffer = em.GetBuffer<PetIndex>(owner);
            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                if (buffer[i].Pet == pet)
                {
                    buffer.RemoveAt(i);
                    break;
                }
            }
        }

        public static void GatherAll(ref EntityManager em, in Entity owner, NativeList<Entity> results)
        {
            if (!em.HasBuffer<PetIndex>(owner))
                return;

            var buffer = em.GetBuffer<PetIndex>(owner);
            results.Capacity = math.max(results.Capacity, buffer.Length);
            for (int i = 0; i < buffer.Length; i++)
            {
                var pet = buffer[i].Pet;
                if (pet != Entity.Null && em.Exists(pet))
                    results.Add(pet);
            }
        }

        public static void GatherByGroup(ref EntityManager em, in Entity owner, in FixedString32Bytes group, NativeList<Entity> results)
        {
            if (!em.HasBuffer<PetIndex>(owner))
                return;

            var buffer = em.GetBuffer<PetIndex>(owner);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (group.Length > 0 && !buffer[i].GroupId.Equals(group))
                    continue;

                var pet = buffer[i].Pet;
                if (pet != Entity.Null && em.Exists(pet))
                    results.Add(pet);
            }
        }

        public static bool TryGetGroup(ref EntityManager em, in Entity owner, in Entity pet, out FixedString32Bytes groupId, out byte swarmLock)
        {
            groupId = default;
            swarmLock = 0;

            if (!em.HasBuffer<PetIndex>(owner))
                return false;

            var buffer = em.GetBuffer<PetIndex>(owner);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Pet == pet)
                {
                    groupId = buffer[i].GroupId;
                    swarmLock = buffer[i].SwarmLock;
                    return true;
                }
            }

            return false;
        }
    }
}
