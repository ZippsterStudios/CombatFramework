using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Framework.Spells.Content;
using Framework.AreaEffects.Spatial.Utilities;

namespace Framework.Spells.Features
{
    public struct SpellRuntimeMetadata
    {
        public FixedString32Bytes CategoryId;
        public int CategoryLevel;
        public int SpellLevel;
        public SpellRank Rank;
    }

    [BurstCompile]
    public static class FeatureRegistry
    {
        private enum TargetFilterKind
        {
            Any,
            Enemy,
            Ally
        }

        private static readonly FixedString64Bytes FilterAny = (FixedString64Bytes)"any";
        private static readonly FixedString64Bytes FilterEnemy = (FixedString64Bytes)"enemy";
        private static readonly FixedString64Bytes FilterAlly = (FixedString64Bytes)"ally";

        [BurstCompile]
        public static void Execute(ref EntityManager em, in Entity caster, in Entity target, in SpellRuntimeMetadata meta, in SpellEffect effect)
        {
            // AOE fan-out: if Radius > 0, query all matches and route per target
            if (effect.Radius > 0f)
            {
                var center = new float2(0, 0);
                if (em.HasComponent<Framework.Core.Components.Position>(caster))
                {
                    center = em.GetComponentData<Framework.Core.Components.Position>(caster).Value;
                }
                var hits = new NativeList<Entity>(Allocator.Temp);
                QueryInRadius(ref em, caster, center, effect.Radius, effect.Filter, ref hits);
                var filterKind = GetFilterKind(effect.Filter);
                for (int i = 0; i < hits.Length; i++)
                {
                    var h = hits[i];
                    // Self-exclude for hostile AoEs unless explicitly applying to all
                    bool wantsEnemy = filterKind == TargetFilterKind.Enemy;
                    if (wantsEnemy && !effect.ApplyToAll && h == caster)
                        continue;
                    RouteSingle(ref em, caster, h, in meta, in effect);
                }
                hits.Dispose();
                return;
            }

            RouteSingle(ref em, caster, target, in meta, in effect);
        }

        [BurstCompile]
        private static void RouteSingle(ref EntityManager em, in Entity caster, in Entity target, in SpellRuntimeMetadata meta, in SpellEffect effect)
        {
            float rankMultiplier = GetRankMultiplier(meta.Rank);
            switch (effect.Kind)
            {
                case SpellEffectKind.DirectDamage:
                    Framework.Damage.Factory.DamageFactory.EnqueueDamage(ref em, target,
                        new Framework.Damage.Components.DamagePacket { Amount = (int)math.round(effect.Magnitude * rankMultiplier), Source = caster });
                    break;
                case SpellEffectKind.DirectHeal:
                case SpellEffectKind.Heal:
                    Framework.Heal.Factory.HealFactory.EnqueueHeal(ref em, target, (int)math.round(effect.Magnitude * rankMultiplier));
                    break;
                case SpellEffectKind.DOT:
                    {
                        int resolvedLevel = ResolveCategoryLevel(meta);
                        if (Framework.DOT.Content.DotCatalog.TryGet(effect.RefId, out var dd))
                            Framework.DOT.Factory.DotFactory.Enqueue(ref em, target, effect.RefId, dd.Dps, dd.TickInterval, dd.Duration, caster, meta.CategoryId, resolvedLevel, 0);
                        else
                            Framework.DOT.Factory.DotFactory.Enqueue(ref em, target, effect.RefId, (int)math.round(effect.Magnitude * rankMultiplier), 1f, 8f, caster, meta.CategoryId, resolvedLevel, 0);
                    }
                    break;
                case SpellEffectKind.HOT:
                    {
                        int hotLevel = ResolveCategoryLevel(meta);
                        if (Framework.HOT.Content.HotCatalog.TryGet(effect.RefId, out var hh))
                            Framework.HOT.Factory.HotFactory.Enqueue(ref em, target, effect.RefId, hh.Hps, hh.TickInterval, hh.Duration, caster, meta.CategoryId, hotLevel, 0);
                        else
                            Framework.HOT.Factory.HotFactory.Enqueue(ref em, target, effect.RefId, (int)math.round(effect.Magnitude * rankMultiplier), 1f, 8f, caster, meta.CategoryId, hotLevel, 0);
                    }
                    break;
                case SpellEffectKind.Buff:
                    Framework.Buffs.Factory.BuffFactory.Apply(ref em, target, effect.RefId, 10f, 1);
                    break;
                case SpellEffectKind.Debuff:
                    {
                        float overrideDuration = effect.Magnitude > 0 ? effect.Magnitude : 0f;
                        if (Framework.Debuffs.Content.DebuffCatalog.TryGet(effect.RefId, out var deb))
                        {
                            Framework.Debuffs.Factory.DebuffFactory.Enqueue(ref em, target, effect.RefId,
                                overrideDuration, 1, caster);
                        }
                        else
                        {
                            float fallback = overrideDuration > 0f ? overrideDuration : 5f;
                            Framework.Debuffs.Factory.DebuffFactory.Enqueue(ref em, target, effect.RefId,
                                fallback, 1, caster);
                        }
                    }
                    break;
                case SpellEffectKind.AreaEffect:
                    Framework.AreaEffects.Factory.AreaEffectFactory.SpawnCircle(ref em, effect.RefId, default, 5f, 8f);
                    break;
            }
        }

        [BurstCompile]
        private static void QueryInRadius(ref EntityManager em, in Entity caster, in float2 center, float radius, in FixedString64Bytes filter, ref NativeList<Entity> results)
        {
            using var entities = em.GetAllEntities(Allocator.Temp);

            byte casterTeam = 0;
            if (em.HasComponent<Framework.Core.Components.TeamId>(caster))
                casterTeam = em.GetComponentData<Framework.Core.Components.TeamId>(caster).Value;

            var filterKind = GetFilterKind(filter);
            bool wantsAny = filterKind == TargetFilterKind.Any;
            bool wantsEnemy = filterKind == TargetFilterKind.Enemy;
            bool wantsAlly = filterKind == TargetFilterKind.Ally;

            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (!em.HasComponent<Framework.Resources.Components.Health>(e) ||
                    !em.HasComponent<Framework.Core.Components.Position>(e))
                    continue;

                var pos = em.GetComponentData<Framework.Core.Components.Position>(e).Value;
                if (!Framework.AreaEffects.Spatial.Utilities.Overlap.CircleContains(center, radius, pos))
                    continue;

                if (!wantsAny)
                {
                    byte team = 0;
                    if (em.HasComponent<Framework.Core.Components.TeamId>(e))
                        team = em.GetComponentData<Framework.Core.Components.TeamId>(e).Value;
                    if (wantsEnemy && team == casterTeam) continue;
                    if (wantsAlly && team != casterTeam) continue;
                }

                results.Add(e);
            }
        }

        [BurstCompile]
        private static TargetFilterKind GetFilterKind(in FixedString64Bytes filter)
        {
            if (filter.Length == 0) return TargetFilterKind.Any;
            if (filter.Equals(FilterAny)) return TargetFilterKind.Any;
            if (filter.Equals(FilterEnemy)) return TargetFilterKind.Enemy;
            if (filter.Equals(FilterAlly)) return TargetFilterKind.Ally;
            return TargetFilterKind.Any;
        }

        private static int ResolveCategoryLevel(in SpellRuntimeMetadata meta)
        {
            if (meta.CategoryLevel > 0)
                return meta.CategoryLevel;
            if (meta.SpellLevel > 0)
                return meta.SpellLevel;
            return 0;
        }

        private static float GetRankMultiplier(SpellRank rank)
        {
            return rank switch
            {
                SpellRank.Apprentice => 0.95f,
                SpellRank.Journeyman => 1.0f,
                SpellRank.Adept => 1.05f,
                SpellRank.Expert => 1.1f,
                SpellRank.Master => 1.15f,
                SpellRank.Grandmaster => 1.2f,
                _ => 1f
            };
        }
    }
}
