using Framework.Pets.Components;
using Framework.Pets.Content;
using Framework.TimedEffect.Content;
using Framework.TimedEffect.Requests;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Pets.Factory
{
    public static partial class PetFactory
    {
        private static void ApplySymbiosis(ref EntityManager em, in Entity owner, in Entity pet, in PetDefinition def)
        {
            if (def.SymbiosisMode == PetSymbiosisMode.None || owner == Entity.Null)
                return;

            var link = new PetSymbiosisLink
            {
                Owner = owner,
                Mode = def.SymbiosisMode,
                SplitPercent = math.clamp(def.SymbiosisSplitPercent, 0f, 1f)
            };
            if (em.HasComponent<PetSymbiosisLink>(pet))
                em.SetComponentData(pet, link);
            else
                em.AddComponentData(pet, link);

            if (!em.HasBuffer<PetSymbiosisParticipant>(owner))
                em.AddBuffer<PetSymbiosisParticipant>(owner);

            var participants = em.GetBuffer<PetSymbiosisParticipant>(owner);
            for (int i = participants.Length - 1; i >= 0; i--)
            {
                if (participants[i].Pet == pet)
                {
                    var entry = participants[i];
                    entry.Mode = link.Mode;
                    entry.SplitPercent = link.SplitPercent;
                    participants[i] = entry;
                    return;
                }
            }

            participants.Add(new PetSymbiosisParticipant
            {
                Pet = pet,
                Mode = link.Mode,
                SplitPercent = link.SplitPercent
            });
        }

        private static void ApplyLifetime(ref EntityManager em, in Entity owner, in Entity pet, in PetDefinition def)
        {
            if (def.DefaultDurationSeconds < 0f)
                return;

            var effectId = BuildLifetimeEffectId(def.Id);
            if (!em.HasBuffer<TimedEffectRequest>(pet))
                em.AddBuffer<TimedEffectRequest>(pet);

            var requests = em.GetBuffer<TimedEffectRequest>(pet);
            requests.Add(new TimedEffectRequest
            {
                Target = pet,
                EffectId = effectId,
                Type = TimedEffectType.Custom,
                StackingMode = TimedEffectStackingMode.Replace,
                CategoryId = (FixedString32Bytes)"pet_lifetime",
                CategoryLevel = def.CategoryLevel,
                StackableCount = 1,
                AddStacks = 1,
                MaxStacks = 1,
                Duration = def.DefaultDurationSeconds,
                TickInterval = 0f,
                Source = owner
            });

            if (em.HasComponent<PetLifetimeTag>(pet))
                em.SetComponentData(pet, new PetLifetimeTag { EffectId = effectId, DefaultDurationSeconds = def.DefaultDurationSeconds });
            else
                em.AddComponentData(pet, new PetLifetimeTag { EffectId = effectId, DefaultDurationSeconds = def.DefaultDurationSeconds });
        }
    }
}
