using Framework.Core.Base;
using Framework.Damage.Requests;
using Framework.Pets.Components;
using Framework.Pets.Content;
using Framework.Pets.Events;
using Framework.Pets.Factory;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Pets.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(RequestsSystemGroup))]
    public partial struct PetSymbiosisSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            foreach (var (link, requests, entity) in SystemAPI.Query<RefRO<PetSymbiosisLink>, DynamicBuffer<DamageRequest>>().WithEntityAccess())
            {
                RouteFromPet(ref em, entity, link.ValueRO, requests);
            }

            foreach (var (participants, requests, owner) in SystemAPI.Query<DynamicBuffer<PetSymbiosisParticipant>, DynamicBuffer<DamageRequest>>().WithEntityAccess())
            {
                if (participants.Length == 0)
                    continue;
                RouteFromOwner(ref em, owner, participants, requests);
            }
        }

        private static void RouteFromPet(ref EntityManager em, in Entity pet, in PetSymbiosisLink link, DynamicBuffer<DamageRequest> requests)
        {
            if (link.Owner == Entity.Null || !em.Exists(link.Owner) || requests.Length == 0)
                return;

            for (int i = requests.Length - 1; i >= 0; i--)
            {
                var request = requests[i];
                int amount = request.Packet.Amount;
                if (amount <= 0)
                    continue;

                switch (link.Mode)
                {
                    case PetSymbiosisMode.SharedPool:
                        EnqueueDamage(ref em, link.Owner, amount, request);
                        requests.RemoveAt(i);
                        PetEventUtility.Emit<PetHealthLinkedDamageEvent>(ref em, link.Owner, pet, default, default, new FixedString64Bytes("shared_pool"), amount);
                        break;
                    case PetSymbiosisMode.Mirror:
                        EnqueueDamage(ref em, link.Owner, amount, request);
                        break;
                    case PetSymbiosisMode.SplitPercent:
                        float split = math.clamp(link.SplitPercent, 0f, 1f);
                        int ownerShare = (int)math.round(amount * (1f - split));
                        int petShare = amount - ownerShare;
                        request.Packet.Amount = math.max(0, petShare);
                        requests[i] = request;
                        if (ownerShare > 0)
                            EnqueueDamage(ref em, link.Owner, ownerShare, request);
                        break;
                }
            }
        }

        private static void RouteFromOwner(ref EntityManager em, in Entity owner, DynamicBuffer<PetSymbiosisParticipant> participants, DynamicBuffer<DamageRequest> requests)
        {
            for (int i = 0; i < requests.Length; i++)
            {
                var request = requests[i];
                int amount = request.Packet.Amount;
                if (amount <= 0)
                    continue;

                for (int p = 0; p < participants.Length; p++)
                {
                    var participant = participants[p];
                    if (participant.Pet == Entity.Null || !em.Exists(participant.Pet))
                        continue;

                    switch (participant.Mode)
                    {
                        case PetSymbiosisMode.Mirror:
                            EnqueueDamage(ref em, participant.Pet, amount, request);
                            break;
                        case PetSymbiosisMode.SplitPercent:
                        {
                            float split = math.clamp(participant.SplitPercent, 0f, 1f);
                            int petShare = (int)math.round(amount * split);
                            int ownerShare = amount - petShare;
                            request.Packet.Amount = math.max(0, ownerShare);
                            requests[i] = request;
                            if (petShare > 0)
                                EnqueueDamage(ref em, participant.Pet, petShare, request);
                            break;
                        }
                        case PetSymbiosisMode.SharedPool:
                            // Owner already acts as pool; no additional routing needed here.
                            break;
                    }
                }
            }
        }

        private static void EnqueueDamage(ref EntityManager em, in Entity target, int amount, in DamageRequest template)
        {
            if (amount <= 0)
                return;

            if (!em.HasBuffer<DamageRequest>(target))
                em.AddBuffer<DamageRequest>(target);
            var buffer = em.GetBuffer<DamageRequest>(target);
            buffer.Add(new DamageRequest
            {
                Target = target,
                Packet = new Framework.Damage.Components.DamagePacket
                {
                    Amount = amount,
                    CritMult = template.Packet.CritMult,
                    School = template.Packet.School,
                    Source = template.Packet.Source,
                    Tags = template.Packet.Tags
                }
            });
        }
    }
}
