using Framework.Shadow.Components;
using Framework.Shadow.Factory;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Shadow.Runtime
{
    public static class ShadowTargetingUtility
    {
        private static readonly FixedString64Bytes DefaultSpellId = (FixedString64Bytes)"shadow.step";

        public static void BeginTargeting(ref EntityManager em, in Entity caster, in FixedString64Bytes spellId, float maxRange, in ShadowRules rules)
        {
            var entity = GetOrCreateTargeting(ref em);
            var targeting = em.GetComponentData<ShadowTargetingState>(entity);
            targeting.IsActive = 1;
            targeting.Caster = caster;
            targeting.SpellId = spellId.Length > 0 ? spellId : DefaultSpellId;
            targeting.MaxRange = maxRange;
            targeting.Rules = rules;
            em.SetComponentData(entity, targeting);
        }

        public static void Cancel(ref EntityManager em)
        {
            if (!TryGetTargeting(ref em, out var entity, out _))
                return;
            em.SetComponentData(entity, ShadowTargetingState.Inactive);
            SetHover(ref em, Entity.Null);
        }

        public static bool TryAcceptTarget(ref EntityManager em, in Entity regionEntity, float3 clickPosition)
        {
            if (!TryGetTargeting(ref em, out var entity, out var state) || state.IsActive == 0)
                return false;

            if (!em.Exists(regionEntity) || !em.HasComponent<ShadowRegion>(regionEntity))
                return false;

            var region = em.GetComponentData<ShadowRegion>(regionEntity);
            if (!ShadowUtility.IsValidShadowTarget(clickPosition, in region, state.Rules))
                return false;

            ShadowUtility.TeleportCaster(ref em, state.Caster, region.Center);
            Cancel(ref em);
            return true;
        }

        public static void UpdateHover(ref EntityManager em, in Entity regionEntity)
        {
            if (!TryGetTargeting(ref em, out _, out var state) || state.IsActive == 0)
            {
                SetHover(ref em, Entity.Null);
                return;
            }

            SetHover(ref em, regionEntity);
        }

        private static void SetHover(ref EntityManager em, in Entity region)
        {
            if (!TryGetTargeting(ref em, out var entity, out _))
                return;
            em.SetComponentData(entity, new ShadowHoverState { Region = region });
        }

        private static bool TryGetTargeting(ref EntityManager em, out Entity entity, out ShadowTargetingState state)
        {
            entity = GetTargetingEntity(ref em);
            if (entity == Entity.Null || !em.HasComponent<ShadowTargetingState>(entity))
            {
                state = default;
                return false;
            }
            state = em.GetComponentData<ShadowTargetingState>(entity);
            return true;
        }

        private static Entity GetOrCreateTargeting(ref EntityManager em)
        {
            var entity = GetTargetingEntity(ref em);
            if (entity == Entity.Null)
            {
                entity = em.CreateEntity();
                em.AddBuffer<ShadowRegionRef>(entity);
                em.AddBuffer<ShadowRegisterRequest>(entity);
                em.AddBuffer<ShadowUnregisterRequest>(entity);
                em.AddComponent<ShadowHoverState>(entity);
                em.AddComponentData(entity, ShadowTargetingState.Inactive);
            }
            return entity;
        }

        private static Entity GetTargetingEntity(ref EntityManager em)
        {
            var query = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(ShadowTargetingState) },
                Options = EntityQueryOptions.IncludeSystems
            });
            return query.IsEmpty ? Entity.Null : query.GetSingletonEntity();
        }
    }
}
