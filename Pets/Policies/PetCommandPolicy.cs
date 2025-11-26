using Framework.AI.Components;
using Framework.Contracts.Intents;
using Framework.Core.Components;
using Framework.Pets.Components;
using Framework.Pets.Content;
using Framework.Pets.Contracts;
using Framework.Pets.Drivers;
using Framework.Pets.Factory;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using ContractsAIAgentTarget = Framework.Contracts.AI.AIAgentTarget;

namespace Framework.Pets.Policies
{
    public static class PetCommandPolicy
    {
        public static void Dispatch(ref EntityManager em, in Entity owner, in PetCommandRequest request, ref NativeList<Entity> scratch)
        {
            if (owner == Entity.Null || !em.Exists(owner))
                return;

            scratch.Clear();
            ResolveTargets(ref em, owner, in request, scratch);
            if (scratch.Length == 0)
                return;

            for (int i = 0; i < scratch.Length; i++)
            {
                var pet = scratch[i];
                if (pet == Entity.Null || !em.Exists(pet))
                    continue;
                ApplyCommand(ref em, owner, pet, in request);
            }
        }

        private static void ResolveTargets(ref EntityManager em, in Entity owner, in PetCommandRequest request, NativeList<Entity> scratch)
        {
            if (request.Pet != Entity.Null && em.Exists(request.Pet))
            {
                var pet = request.Pet;
                bool swarm = false;
                FixedString32Bytes group = default;
                if (em.HasComponent<PetGroup>(pet))
                {
                    var pg = em.GetComponentData<PetGroup>(pet);
                    group = pg.Id;
                    swarm = pg.SwarmLock != 0;
                }

                if (swarm && group.Length > 0)
                {
                    PetQuery.GatherByGroup(ref em, owner, group, scratch);
                    return;
                }

                scratch.Add(pet);
                return;
            }

            if (request.Group.Length > 0)
            {
                PetQuery.GatherByGroup(ref em, owner, request.Group, scratch);
                return;
            }

            PetQuery.GatherAll(ref em, owner, scratch);
        }

        private static void ApplyCommand(ref EntityManager em, in Entity owner, in Entity pet, in PetCommandRequest request)
        {
            var ownerPos = PetTeamUtility.GetOwnerPosition(ref em, owner);
            var identity = em.HasComponent<PetIdentity>(pet) ? em.GetComponentData<PetIdentity>(pet) : default;
            PetCatalog.TryGet(identity.PetId, out var def);

            switch (request.Command)
            {
                case PetCommand.Follow:
                    PetLeashPolicy.ApplyDefault(ref em, pet, in def, ownerPos);
                    UpdateGuardAnchor(ref em, pet, ownerPos, owner);
                    break;
                case PetCommand.Guard:
                    var guardPos = ResolveTargetPosition(ref em, request.Target, ownerPos);
                    ApplyGuardLeash(ref em, pet, guardPos);
                    UpdateGuardAnchor(ref em, pet, guardPos, request.Target != Entity.Null ? request.Target : owner);
                    break;
                case PetCommand.Patrol:
                    ApplyPatrol(ref em, pet, request);
                    break;
                case PetCommand.Stay:
                case PetCommand.Sit:
                    ClearIntents(ref em, pet);
                    if (request.Command == PetCommand.Sit)
                        AttachRecipe(ref em, pet, "pet_sit");
                    break;
                case PetCommand.BackOff:
                    ClearIntents(ref em, pet);
                    ApplyAttack(ref em, pet, Entity.Null);
                    PetLeashPolicy.ApplyDefault(ref em, pet, in def, ownerPos);
                    UpdateGuardAnchor(ref em, pet, ownerPos, owner);
                    break;
                case PetCommand.Attack:
                    ApplyAttack(ref em, pet, request.Target);
                    break;
                case PetCommand.Dismiss:
                    if (!em.HasComponent<PetPendingDismiss>(pet))
                        em.AddComponent<PetPendingDismiss>(pet);
                    break;
            }
        }

        private static void ApplyGuardLeash(ref EntityManager em, in Entity pet, in float2 home)
        {
            if (em.HasComponent<PetLeashConfigShim>(pet))
            {
                var shim = em.GetComponentData<PetLeashConfigShim>(pet);
                shim.Home = home;
                em.SetComponentData(pet, shim);
            }

            if (em.HasComponent<Framework.Contracts.Perception.LeashConfig>(pet))
            {
                var leash = em.GetComponentData<Framework.Contracts.Perception.LeashConfig>(pet);
                leash.Home = home;
                em.SetComponentData(pet, leash);
            }
        }

        private static void UpdateGuardAnchor(ref EntityManager em, in Entity pet, in float2 home, in Entity anchor)
        {
            var guard = new PetGuardAnchor { Position = home, AnchorEntity = anchor };
            if (em.HasComponent<PetGuardAnchor>(pet))
                em.SetComponentData(pet, guard);
            else
                em.AddComponentData(pet, guard);
        }

        private static void ApplyPatrol(ref EntityManager em, in Entity pet, in PetCommandRequest request)
        {
            if (!em.HasBuffer<PetWaypoint>(pet))
                em.AddBuffer<PetWaypoint>(pet);
            var waypoints = em.GetBuffer<PetWaypoint>(pet);
            bool hasWaypoint = request.Waypoint.x != 0f || request.Waypoint.y != 0f || request.Waypoint.z != 0f;
            if (request.AppendWaypoint == 0)
                waypoints.Clear();
            if (hasWaypoint)
                waypoints.Add(new PetWaypoint { Value = request.Waypoint });

            if (!em.HasComponent<PetPatrolState>(pet))
                em.AddComponentData(pet, new PetPatrolState { Active = 1, NextWaypointIndex = 0 });
            else
            {
                var state = em.GetComponentData<PetPatrolState>(pet);
                state.Active = 1;
                if (state.NextWaypointIndex >= waypoints.Length)
                    state.NextWaypointIndex = 0;
                em.SetComponentData(pet, state);
            }
        }

        private static void ClearIntents(ref EntityManager em, in Entity pet)
        {
            if (em.HasComponent<MoveIntent>(pet))
            {
                var move = em.GetComponentData<MoveIntent>(pet);
                move.Clear();
                em.SetComponentData(pet, move);
            }

            if (em.HasComponent<CastIntent>(pet))
            {
                var cast = em.GetComponentData<CastIntent>(pet);
                cast.Clear();
                em.SetComponentData(pet, cast);
            }
        }

        private static void AttachRecipe(ref EntityManager em, in Entity pet, FixedString64Bytes recipeId)
        {
            var recipe = PetBehaviorLibrary.Resolve(recipeId);
            if (!recipe.IsCreated)
                return;

            if (em.HasComponent<Framework.AI.Behaviors.Components.AIBehaviorRecipeRef>(pet))
                em.SetComponentData(pet, new Framework.AI.Behaviors.Components.AIBehaviorRecipeRef { Recipe = recipe });
            else
                em.AddComponentData(pet, new Framework.AI.Behaviors.Components.AIBehaviorRecipeRef { Recipe = recipe });
        }

        private static void ApplyAttack(ref EntityManager em, in Entity pet, in Entity target)
        {
            if (!em.HasComponent<ContractsAIAgentTarget>(pet))
                em.AddComponentData(pet, ContractsAIAgentTarget.CreateDefault());

            var agentTarget = em.GetComponentData<ContractsAIAgentTarget>(pet);
            agentTarget.Value = target;
            agentTarget.Visibility = target != Entity.Null ? (byte)1 : (byte)0;
            em.SetComponentData(pet, agentTarget);
        }

        private static float2 ResolveTargetPosition(ref EntityManager em, in Entity target, in float2 fallback)
        {
            if (target != Entity.Null && em.Exists(target) && em.HasComponent<Position>(target))
            {
                return em.GetComponentData<Position>(target).Value;
            }
            return fallback;
        }
    }
}
