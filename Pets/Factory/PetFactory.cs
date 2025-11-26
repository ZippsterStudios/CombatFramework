using Framework.AI.Components;
using Framework.AI.Runtime;
using Framework.Contracts.Intents;
using Framework.Core.Components;
using Framework.Pets.Components;
using Framework.Pets.Content;
using Framework.Pets.Contracts;
using Framework.Pets.Drivers;
using Framework.Pets.Events;
using Framework.Pets.Policies;
using Framework.Spells.Content;
using Framework.Spells.Runtime;
using Framework.Spells.Features;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using ContractsAIAgentTarget = Framework.Contracts.AI.AIAgentTarget;

namespace Framework.Pets.Factory
{
    public static partial class PetFactory
    {
        private static FixedString64Bytes BuildLifetimeEffectId(in FixedString64Bytes petId)
        {
            var name = $"pet_lifetime/{petId}";
            return (FixedString64Bytes)name.ToLowerInvariant();
        }

        private static int _sequence;

        internal static void HandleSummonEffect(ref EntityManager em, in Entity caster, in Entity target, in Framework.Spells.Runtime.SpellRuntimeMetadata meta, in SummonPayload payload, int resolvedCategoryLevel)
        {
            int count = math.max(1, payload.Count);
            float radius = payload.SpawnRadius <= 0f ? 1.5f : payload.SpawnRadius;
            Summon(ref em, caster, target, payload.PetId, count, radius, meta.CategoryId, resolvedCategoryLevel);
        }

        public static void Summon(ref EntityManager em, in Entity owner, in Entity target, in FixedString64Bytes petId, int count, float spawnRadius, in FixedString32Bytes categoryId, int categoryLevel)
        {
            if (!PetCatalog.TryGet(petId, out var def))
                return;

            int spawnCount = math.max(1, count);
            float radius = spawnRadius <= 0f ? 1.5f : spawnRadius;
            var groupId = def.DefaultGroup.Length > 0 ? def.DefaultGroup : (FixedString32Bytes)"pets";

            for (int i = 0; i < spawnCount; i++)
            {
                if (!PetLimitPolicy.TryAcquire(ref em, owner, def, out var toReplace))
                {
                    PetEventUtility.Emit<PetSummonBlockedEvent>(ref em, owner, Entity.Null, def.Id, groupId, new FixedString64Bytes("limit"));
                    continue;
                }

                if (toReplace != Entity.Null)
                    Despawn(ref em, toReplace, new FixedString64Bytes("limit_replace"));

                PetEventUtility.Emit<PetSummonBeganEvent>(ref em, owner, Entity.Null, def.Id, groupId);

                var spawnPos2D = PetTeamUtility.ResolveSpawnPosition(ref em, owner, radius, i, spawnCount);
                var pet = CreatePet(ref em, owner, in def, spawnPos2D, target, categoryId, categoryLevel);
                PetQuery.TrackPet(ref em, owner, pet, def.Id, groupId, (byte)((def.Flags & PetFlags.Swarm) != 0 ? 1 : 0), ++_sequence);
                PetEventUtility.Emit<PetSummonResolvedEvent>(ref em, owner, pet, def.Id, groupId);
            }
        }

        private static Entity CreatePet(ref EntityManager em, in Entity owner, in PetDefinition def, in float2 spawnPos, in Entity initialTarget, in FixedString32Bytes categoryId, int categoryLevel)
        {
            var pet = em.CreateEntity();
            em.AddComponentData(pet, new PetTag());
            em.AddComponentData(pet, new PetOwner { Value = owner });
            em.AddComponentData(pet, new PetIdentity { PetId = def.Id, CategoryId = categoryId, CategoryLevel = categoryLevel });

            var groupId = def.DefaultGroup;
            if (groupId.Length == 0)
                groupId = (FixedString32Bytes)"pets";
            em.AddComponentData(pet, new PetGroup
            {
                Id = groupId,
                SwarmLock = (byte)((def.Flags & PetFlags.Swarm) != 0 ? 1 : 0)
            });

            if (em.HasComponent<Position>(pet))
                em.SetComponentData(pet, new Position { Value = spawnPos });
            else
                em.AddComponentData(pet, new Position { Value = spawnPos });

            var health = new Framework.Resources.Components.Health
            {
                Current = math.max(1, def.BaseHealth),
                Max = math.max(1, def.BaseHealth)
            };
            em.AddComponentData(pet, health);

            if (def.BaseMana > 0)
            {
                var mana = new Framework.Resources.Components.Mana
                {
                    Current = math.max(0, def.BaseMana),
                    Max = math.max(0, def.BaseMana)
                };
                em.AddComponentData(pet, mana);
            }

            PetTeamUtility.ApplyTeam(ref em, owner, pet);
            em.AddComponentData(pet, new PetGuardAnchor { Position = PetTeamUtility.GetOwnerPosition(ref em, owner), AnchorEntity = owner });

            var config = AIAgentBehaviorConfig.CreateDefaults();
            config.MoveSpeed = math.max(1f, def.MoveSpeed);
            config.AttackRange = math.max(1f, def.DefaultFollowOffset + 1f);
            em.AddComponentData(pet, config);
            em.AddComponentData(pet, new AIState { Current = AIStateIds.Idle });
            em.AddComponentData(pet, AIAgentDecisionState.CreateDefault());
            em.AddComponentData(pet, AICombatRuntime.CreateDefault());
            em.AddComponentData(pet, ContractsAIAgentTarget.CreateDefault());
            em.AddComponentData(pet, new MoveIntent());
            em.AddComponentData(pet, new CastIntent());
            em.AddComponent<AIBehaviorEnabledTag>(pet);
            if (!em.HasBuffer<StateChangeRequest>(pet))
                em.AddBuffer<StateChangeRequest>(pet);
            if (!em.HasBuffer<PetWaypoint>(pet))
                em.AddBuffer<PetWaypoint>(pet);

            if (def.DefaultAIRecipeId.Length > 0)
            {
                var recipe = PetBehaviorLibrary.Resolve(def.DefaultAIRecipeId);
                if (recipe.IsCreated)
                    em.AddComponentData(pet, new Framework.AI.Behaviors.Components.AIBehaviorRecipeRef { Recipe = recipe });
            }

            PetLeashPolicy.ApplyDefault(ref em, pet, in def, PetTeamUtility.GetOwnerPosition(ref em, owner));
            ApplySymbiosis(ref em, owner, pet, in def);
            ApplyLifetime(ref em, owner, pet, in def);

            if (initialTarget != Entity.Null && em.Exists(initialTarget))
            {
                var target = em.GetComponentData<ContractsAIAgentTarget>(pet);
                target.Value = initialTarget;
                target.Visibility = 1;
                em.SetComponentData(pet, target);
            }

            return pet;
        }

        public static void Despawn(ref EntityManager em, in Entity pet, in FixedString64Bytes reason = default)
        {
            if (!em.Exists(pet))
                return;

            Entity owner = Entity.Null;
            if (em.HasComponent<PetOwner>(pet))
                owner = em.GetComponentData<PetOwner>(pet).Value;

            if (owner != Entity.Null && em.Exists(owner))
            {
                PetQuery.RemovePet(ref em, owner, pet);
                if (em.HasBuffer<PetSymbiosisParticipant>(owner))
                {
                    var participants = em.GetBuffer<PetSymbiosisParticipant>(owner);
                    for (int i = participants.Length - 1; i >= 0; i--)
                    {
                        if (participants[i].Pet == pet)
                            participants.RemoveAt(i);
                    }
                }
            }

            var group = em.HasComponent<PetGroup>(pet) ? em.GetComponentData<PetGroup>(pet).Id : default;
            var id = em.HasComponent<PetIdentity>(pet) ? em.GetComponentData<PetIdentity>(pet).PetId : default;
            PetEventUtility.Emit<PetDismissedEvent>(ref em, owner, pet, id, group, reason);
            em.DestroyEntity(pet);
        }
    }
}
