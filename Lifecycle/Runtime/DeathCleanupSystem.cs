using Framework.Lifecycle.Components;
using Framework.Lifecycle.Config;
using Framework.Lifecycle.Policies;
using Framework.Resources.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

#if FRAMEWORK_HAS_SPELLS
using Framework.Spells.Drivers;
#endif
#if FRAMEWORK_HAS_TIMED_EFFECTS
using Framework.TimedEffect.Components;
using Framework.TimedEffect.Events;
using Framework.TimedEffect.Requests;
#endif
#if FRAMEWORK_HAS_DOT
using Framework.DOT.Components;
#endif
#if FRAMEWORK_HAS_HOT
using Framework.HOT.Components;
#endif
#if FRAMEWORK_HAS_BUFFS
using Framework.Buffs.Components;
#endif
#if FRAMEWORK_HAS_DEBUFFS
using Framework.Debuffs.Components;
#endif
#if FRAMEWORK_HAS_THREAT
using Framework.Threat.Components;
#endif

namespace Framework.Lifecycle.Runtime
{
    [UpdateInGroup(typeof(Framework.Core.Base.RuntimeSystemGroup))]
    public partial struct DeathCleanupSystem : ISystem
    {
        private EntityQuery _deadQuery;

        public void OnCreate(ref SystemState state)
        {
            _deadQuery = state.GetEntityQuery(ComponentType.ReadOnly<Dead>());
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            if (_deadQuery.IsEmptyIgnoreFilter)
                return;

            var em = state.EntityManager;
            LifecycleFeatureConfig config;
            bool hasConfig = SystemAPI.TryGetSingleton(out config);
            config = LifecyclePolicy.EnsureDefaults(hasConfig, config);

            var dead = _deadQuery.ToEntityArray(Allocator.Temp);

            if (LifecyclePolicy.ShouldStopRegen(config))
            {
                em.CompleteDependencyBeforeRW<Health>();
                em.CompleteDependencyBeforeRW<Mana>();
                em.CompleteDependencyBeforeRW<Stamina>();
            }

#if FRAMEWORK_HAS_SPELLS
            em.CompleteDependencyBeforeRW<SpellDriver.Casting>();
#endif
#if FRAMEWORK_HAS_TIMED_EFFECTS
            em.CompleteDependencyBeforeRW<TimedEffectInstance>();
            em.CompleteDependencyBeforeRW<TimedEffectEvent>();
            em.CompleteDependencyBeforeRW<TimedEffectRequest>();
#endif
#if FRAMEWORK_HAS_DOT
            em.CompleteDependencyBeforeRW<DotInstance>();
#endif
#if FRAMEWORK_HAS_HOT
            em.CompleteDependencyBeforeRW<HotInstance>();
#endif
#if FRAMEWORK_HAS_BUFFS
            em.CompleteDependencyBeforeRW<BuffInstance>();
            em.CompleteDependencyBeforeRW<BuffStatSnapshot>();
            em.CompleteDependencyBeforeRW<BuffCustomStatAggregate>();
#endif
#if FRAMEWORK_HAS_DEBUFFS
            em.CompleteDependencyBeforeRW<DebuffInstance>();
            em.CompleteDependencyBeforeRW<DebuffStatAggregate>();
            em.CompleteDependencyBeforeRW<DebuffCrowdControlState>();
#endif
#if FRAMEWORK_HAS_THREAT
            em.CompleteDependencyBeforeRW<ThreatValue>();
#endif

            for (int i = 0; i < dead.Length; i++)
            {
                var entity = dead[i];

#if FRAMEWORK_HAS_SPELLS
                if (em.HasComponent<SpellDriver.Casting>(entity))
                    em.RemoveComponent<SpellDriver.Casting>(entity);
#endif

#if FRAMEWORK_HAS_TIMED_EFFECTS
                if (LifecyclePolicy.ShouldCleanupTimedEffects(config))
                {
                    if (em.HasBuffer<TimedEffectInstance>(entity))
                        em.RemoveComponent<TimedEffectInstance>(entity);
                    if (em.HasBuffer<TimedEffectEvent>(entity))
                        em.RemoveComponent<TimedEffectEvent>(entity);
                    if (em.HasBuffer<TimedEffectRequest>(entity))
                        em.RemoveComponent<TimedEffectRequest>(entity);
                }
#endif

#if FRAMEWORK_HAS_DOT
                if (LifecyclePolicy.ShouldCleanupDots(config) && em.HasBuffer<DotInstance>(entity))
                    em.RemoveComponent<DotInstance>(entity);
#endif

#if FRAMEWORK_HAS_HOT
                if (LifecyclePolicy.ShouldCleanupHots(config) && em.HasBuffer<HotInstance>(entity))
                    em.RemoveComponent<HotInstance>(entity);
#endif

#if FRAMEWORK_HAS_BUFFS
                if (LifecyclePolicy.ShouldCleanupBuffs(config))
                {
                    if (em.HasBuffer<BuffInstance>(entity))
                        em.RemoveComponent<BuffInstance>(entity);
                    if (em.HasComponent<BuffStatSnapshot>(entity))
                        em.RemoveComponent<BuffStatSnapshot>(entity);
                    if (em.HasBuffer<BuffCustomStatAggregate>(entity))
                        em.RemoveComponent<BuffCustomStatAggregate>(entity);
                }
#endif

#if FRAMEWORK_HAS_DEBUFFS
                if (LifecyclePolicy.ShouldCleanupDebuffs(config))
                {
                    if (em.HasBuffer<DebuffInstance>(entity))
                        em.RemoveComponent<DebuffInstance>(entity);
                    if (em.HasBuffer<DebuffStatAggregate>(entity))
                        em.RemoveComponent<DebuffStatAggregate>(entity);
                    if (em.HasComponent<DebuffCrowdControlState>(entity))
                        em.RemoveComponent<DebuffCrowdControlState>(entity);
                }
#endif

#if FRAMEWORK_HAS_THREAT
                if (LifecyclePolicy.ShouldClearThreat(config) && em.HasComponent<ThreatValue>(entity))
                    em.RemoveComponent<ThreatValue>(entity);
#endif

                if (LifecyclePolicy.ShouldStopRegen(config))
                {
                    if (em.HasComponent<Health>(entity))
                    {
                        var h = em.GetComponentData<Health>(entity);
                        h.RegenPerSecond = 0;
                        h.RegenAccumulator = 0f;
                        em.SetComponentData(entity, h);
                    }

                    if (em.HasComponent<Mana>(entity))
                    {
                        var m = em.GetComponentData<Mana>(entity);
                        m.RegenPerSecond = 0;
                        m.RegenAccumulator = 0f;
                        em.SetComponentData(entity, m);
                    }

                    if (em.HasComponent<Stamina>(entity))
                    {
                        var s = em.GetComponentData<Stamina>(entity);
                        s.RegenPerSecond = 0;
                        s.RegenAccumulator = 0f;
                        em.SetComponentData(entity, s);
                    }
                }

                if (em.HasComponent<DeadTimer>(entity))
                {
                    var timer = em.GetComponentData<DeadTimer>(entity);
                    timer.Seconds = 0f;
                    em.SetComponentData(entity, timer);
                }
            }

            dead.Dispose();
        }
    }
}
