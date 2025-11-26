using Framework.Pets.Components;
using Framework.Pets.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Pets.Drivers
{
    public static class PetDriver
    {
        public static void CommandAll(ref EntityManager em, in Entity owner, PetCommand command, in Entity target = default)
        {
            if (!em.Exists(owner))
                return;

            var buffer = EnsureBuffer(ref em, owner);
            buffer.Add(new PetCommandRequest
            {
                Command = command,
                Target = target
            });
        }

        public static void CommandGroup(ref EntityManager em, in Entity owner, in FixedString32Bytes group, PetCommand command, in Entity target = default)
        {
            if (!em.Exists(owner))
                return;

            var buffer = EnsureBuffer(ref em, owner);
            buffer.Add(new PetCommandRequest
            {
                Command = command,
                Group = group,
                Target = target
            });
        }

        public static void CommandPet(ref EntityManager em, in Entity pet, PetCommand command, in Entity target = default)
        {
            if (!em.Exists(pet) || !em.HasComponent<PetOwner>(pet))
                return;

            var owner = em.GetComponentData<PetOwner>(pet).Value;
            if (owner == Entity.Null || !em.Exists(owner))
                return;

            var buffer = EnsureBuffer(ref em, owner);
            buffer.Add(new PetCommandRequest
            {
                Command = command,
                Pet = pet,
                Target = target
            });
        }

        public static void CommandAttackAll(ref EntityManager em, in Entity owner, in Entity target) =>
            CommandAll(ref em, owner, PetCommand.Attack, target);

        public static void CommandBackOffAll(ref EntityManager em, in Entity owner) =>
            CommandAll(ref em, owner, PetCommand.BackOff);

        public static void CommandAttackGroup(ref EntityManager em, in Entity owner, in FixedString32Bytes group, in Entity target) =>
            CommandGroup(ref em, owner, group, PetCommand.Attack, target);

        public static void CommandBackOffGroup(ref EntityManager em, in Entity owner, in FixedString32Bytes group) =>
            CommandGroup(ref em, owner, group, PetCommand.BackOff);

        public static void CommandAttackPet(ref EntityManager em, in Entity pet, in Entity target) =>
            CommandPet(ref em, pet, PetCommand.Attack, target);

        public static void CommandBackOffPet(ref EntityManager em, in Entity pet) =>
            CommandPet(ref em, pet, PetCommand.BackOff);

        public static void AddWaypoint(ref EntityManager em, in Entity owner, in FixedString32Bytes group, float3 waypoint, bool append = true)
        {
            if (!em.Exists(owner))
                return;

            var buffer = EnsureBuffer(ref em, owner);
            buffer.Add(new PetCommandRequest
            {
                Command = PetCommand.Patrol,
                Group = group,
                Waypoint = waypoint,
                AppendWaypoint = append ? (byte)1 : (byte)0
            });
        }

        public static int Count(ref EntityManager em, in Entity owner, in FixedString64Bytes petId)
        {
            if (!em.Exists(owner) || !em.HasBuffer<PetIndex>(owner))
                return 0;

            var buffer = em.GetBuffer<PetIndex>(owner);
            int total = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].PetId.Equals(petId))
                    total++;
            }
            return total;
        }

        private static DynamicBuffer<PetCommandRequest> EnsureBuffer(ref EntityManager em, in Entity owner)
        {
            if (!em.HasBuffer<PetCommandRequest>(owner))
                em.AddBuffer<PetCommandRequest>(owner);
            return em.GetBuffer<PetCommandRequest>(owner);
        }
    }
}
