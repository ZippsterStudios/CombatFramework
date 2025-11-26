using Framework.Core.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Pets.Factory
{
    internal static class PetTeamUtility
    {
        public static float2 ResolveSpawnPosition(ref EntityManager em, in Entity owner, float radius, int index, int total)
        {
            var origin = GetOwnerPosition(ref em, owner);
            float spawnRadius = radius <= 0f ? 1.5f : radius;
            if (total <= 1)
                return origin + new float2(spawnRadius, 0f);

            float angle = (math.PI * 2f) * (index / (float)math.max(1, total));
            var offset = new float2(math.cos(angle), math.sin(angle)) * spawnRadius;
            return origin + offset;
        }

        public static float2 GetOwnerPosition(ref EntityManager em, in Entity owner)
        {
            if (owner != Entity.Null && em.HasComponent<Position>(owner))
                return em.GetComponentData<Position>(owner).Value;
            return float2.zero;
        }

        public static void ApplyTeam(ref EntityManager em, in Entity owner, in Entity pet)
        {
            if (owner != Entity.Null && em.HasComponent<TeamId>(owner))
            {
                var team = em.GetComponentData<TeamId>(owner);
                if (em.HasComponent<TeamId>(pet))
                    em.SetComponentData(pet, team);
                else
                    em.AddComponentData(pet, team);
            }
        }
    }
}
