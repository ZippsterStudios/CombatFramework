using Framework.Core.Components;
using Framework.Shadow.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Shadow.Factory
{
    public static class ShadowUtility
    {
        private static readonly float3 Up = new float3(0f, 1f, 0f);

        public static bool RegisterRegion(ref EntityManager em, in Entity regionEntity)
        {
            if (!em.Exists(regionEntity))
                return false;

            var manager = EnsureManager(ref em);
            var buffer = em.GetBuffer<ShadowRegisterRequest>(manager);
            buffer.Add(new ShadowRegisterRequest { Region = regionEntity });
            return true;
        }

        public static bool UnregisterRegion(ref EntityManager em, in Entity regionEntity)
        {
            if (!em.Exists(regionEntity))
                return false;

            var manager = EnsureManager(ref em);
            var buffer = em.GetBuffer<ShadowUnregisterRequest>(manager);
            buffer.Add(new ShadowUnregisterRequest { Region = regionEntity });
            return true;
        }

        public static bool FindClosestShadow(ref EntityManager em, float3 position, float maxRange, in ShadowRules rules, out Entity regionEntity, out ShadowRegion region)
        {
            regionEntity = Entity.Null;
            region = default;

            if (!TryGetManager(ref em, out var manager) || !em.HasBuffer<ShadowRegionRef>(manager))
                return false;

            var refs = em.GetBuffer<ShadowRegionRef>(manager);
            float bestDistSq = maxRange > 0f ? maxRange * maxRange : float.MaxValue;
            bool found = false;

            for (int i = 0; i < refs.Length; i++)
            {
                var candidate = refs[i].Region;
                if (!em.Exists(candidate) || !em.HasComponent<ShadowRegion>(candidate))
                    continue;

                var data = em.GetComponentData<ShadowRegion>(candidate);
                if (!IsValidShadowTarget(position, in data, rules))
                    continue;

                float distSq = math.lengthsq(data.Center - position);
                if (distSq > bestDistSq)
                    continue;

                bestDistSq = distSq;
                regionEntity = candidate;
                region = data;
                found = true;
            }

            return found;
        }

        public static bool IsValidShadowTarget(float3 position, in ShadowRegion region, in ShadowRules rules)
        {
            if (rules.RequireEnabled != 0 && region.Enabled == 0)
                return false;

            if (rules.RequireOwner != 0 && region.Owner == Entity.Null)
                return false;

            if (rules.TeamFilter != 0 && region.Team != 0 && rules.TeamFilter != region.Team)
                return false;

            var distSq = math.lengthsq(position - region.Center);
            return distSq <= region.Radius * region.Radius;
        }

        public static void ToggleHighlight(ref EntityManager em, in Entity region, bool enabled, float intensity = 1f)
        {
            if (!em.Exists(region))
                return;

            if (!em.HasComponent<ShadowRegionHighlight>(region))
                em.AddComponentData(region, new ShadowRegionHighlight { Color = new float4(0f, 0f, 0f, 0.6f), Intensity = intensity, Enabled = enabled ? (byte)1 : (byte)0 });
            else
            {
                var h = em.GetComponentData<ShadowRegionHighlight>(region);
                h.Enabled = enabled ? (byte)1 : (byte)0;
                h.Intensity = intensity;
                em.SetComponentData(region, h);
            }
        }

        public static void TeleportCaster(ref EntityManager em, in Entity caster, float3 destination)
        {
            var pos2D = new float2(destination.x, destination.z);
            if (em.HasComponent<Position>(caster))
            {
                em.SetComponentData(caster, new Position { Value = pos2D });
            }
            else
            {
                em.AddComponentData(caster, new Position { Value = pos2D });
            }
        }

        public static bool TryResolveHover(ref EntityManager em, float3 pointerWorldPos, float maxRange, in ShadowRules rules, out Entity region)
        {
            if (FindClosestShadow(ref em, pointerWorldPos, maxRange, rules, out region, out _))
                return true;
            region = Entity.Null;
            return false;
        }

        private static Entity EnsureManager(ref EntityManager em)
        {
            if (TryGetManager(ref em, out var existing))
                return existing;

            var entity = em.CreateEntity();
            em.AddBuffer<ShadowRegionRef>(entity);
            em.AddBuffer<ShadowRegisterRequest>(entity);
            em.AddBuffer<ShadowUnregisterRequest>(entity);
            em.AddComponent<ShadowHoverState>(entity);
            em.AddComponentData(entity, ShadowTargetingState.Inactive);
            return entity;
        }

        private static bool TryGetManager(ref EntityManager em, out Entity entity)
        {
            var query = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(ShadowRegionRef) },
                Options = EntityQueryOptions.IncludeSystems
            });

            if (!query.IsEmpty)
            {
                entity = query.GetSingletonEntity();
                return true;
            }

            entity = Entity.Null;
            return false;
        }
    }
}
