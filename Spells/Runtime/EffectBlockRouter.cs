using Framework.Buffs.Factory;
using Framework.DOT.Content;
using Framework.DOT.Factory;
using Framework.Debuffs.Factory;
using Framework.Heal.Factory;
using Framework.HOT.Content;
using Framework.HOT.Factory;
using Framework.Spells.Content;
using Framework.Spells.Features;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Spells.Runtime
{
    public static class EffectBlockRouter
    {
        public static void Execute(ref EffectExecutionContext context, ref BlobArray<EffectBlockBlob> blocks)
        {
            var resolverCtx = new EffectTargetResolver.Context
            {
                EntityManager = context.EntityManager,
                Caster = context.Caster,
                PrimaryTarget = context.PrimaryTarget
            };

            var targets = new NativeList<Entity>(Allocator.Temp);
            try
            {
                for (int i = 0; i < blocks.Length; i++)
                {
                    targets.Clear();
                    ref var block = ref blocks[i];
                    EffectTargetResolver.Resolve(ref resolverCtx, in block.Scope, ref targets);
                    if (targets.Length == 0)
                    {
                        SpellDebugLogger.Log($"[SpellPipeline] Block#{i} produced no targets (scope={block.Scope.Kind}).");
                        continue;
                    }
                    SpellDebugLogger.Log($"[SpellPipeline] Block#{i} resolved {targets.Length} target(s) for caster {SpellDebugLogger.FormatEntity(context.Caster)}.");
                    ApplyBlock(ref context, ref block, i, ref targets);
                }
            }
            finally
            {
                if (targets.IsCreated)
                    targets.Dispose();
            }
        }

        private static void ApplyBlock(ref EffectExecutionContext ctx, ref EffectBlockBlob block, int blockIndex, ref NativeList<Entity> targets)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                if (!ctx.EntityManager.Exists(target))
                    continue;

                if (!EffectConditionEvaluator.ShouldApply(ref ctx.EntityManager, block.Conditions, ctx.Caster, target, ctx.RandomSeed, blockIndex))
                {
                    SpellDebugLogger.Log($"[SpellPipeline] Block#{blockIndex} skipped target {SpellDebugLogger.FormatEntity(target)} due to conditions.");
                    continue;
                }

                ExecutePayload(ref ctx, ref block, blockIndex, target);
            }
        }

        private static void ExecutePayload(ref EffectExecutionContext ctx, ref EffectBlockBlob block, int blockIndex, in Entity target)
        {
            switch (block.Payload.Kind)
            {
                case EffectPayloadKind.Damage:
                    {
                        float amount = EffectValueCalculator.Resolve(block.Payload.Damage.Amount, block.Scaling, ref ctx, target, blockIndex);
                        amount = ApplyVariance(amount, block.Payload.Damage.VariancePercent, ctx.RandomSeed, blockIndex, target);
                        int final = math.max(0, (int)math.round(amount));
                        if (final <= 0) return;
                        var packet = new Framework.Damage.Components.DamagePacket
                        {
                            Amount = final,
                            School = block.Payload.Damage.School,
                            Source = ctx.Caster,
                            CritMult = 1f,
                            Tags = block.Payload.Damage.Tags,
                            IgnoreArmor = block.Payload.Damage.IgnoreArmor,
                            IgnoreResist = block.Payload.Damage.IgnoreResist,
                            IgnoreSnapshotModifiers = block.Payload.Damage.IgnoreSnapshotModifiers
                        };
                        Framework.Damage.Factory.DamageFactory.EnqueueDamage(ref ctx.EntityManager, target, packet);
                        ctx.Results.Record(blockIndex, EffectResultSource.Damage, final);
                        SpellDebugLogger.Log($"[SpellPipeline] Damage block#{blockIndex} -> {SpellDebugLogger.FormatEntity(target)} amount={final} school={block.Payload.Damage.School} ignoreArmor={packet.IgnoreArmor} ignoreResist={packet.IgnoreResist} ignoreSnapshot={packet.IgnoreSnapshotModifiers}.");
                        break;
                    }
                case EffectPayloadKind.Heal:
                    {
                        float amount = EffectValueCalculator.Resolve(block.Payload.Heal.Amount, block.Scaling, ref ctx, target, blockIndex);
                        int final = math.max(0, (int)math.round(amount));
                        if (final <= 0) return;
                        HealFactory.EnqueueHeal(ref ctx.EntityManager, target, final);
                        ctx.Results.Record(blockIndex, EffectResultSource.Heal, final);
                        SpellDebugLogger.Log($"[SpellPipeline] Heal block#{blockIndex} -> {SpellDebugLogger.FormatEntity(target)} amount={final}.");
                        break;
                    }
                case EffectPayloadKind.StatOps:
                    {
                        ref var statOps = ref block.Payload.StatOps;
                        for (int i = 0; i < statOps.Length; i++)
                        {
                            var op = statOps[i];
                            StatOperationApplier.Apply(ref ctx.EntityManager, target, in op);
                        }
                        break;
                    }
                case EffectPayloadKind.ApplyBuff:
                    {
                        float duration = block.Payload.Apply.DurationMs > 0 ? block.Payload.Apply.DurationMs / 1000f : 10f;
                        BuffFactory.Apply(ref ctx.EntityManager, target, block.Payload.Apply.Id, duration, 1);
                        SpellDebugLogger.Log($"[SpellPipeline] Buff block#{blockIndex} -> {SpellDebugLogger.FormatEntity(target)} buffId={block.Payload.Apply.Id.ToString()} duration={duration:0.##}s.");
                        break;
                    }
                case EffectPayloadKind.ApplyDebuff:
                    {
                        float duration = block.Payload.Apply.DurationMs > 0 ? block.Payload.Apply.DurationMs / 1000f : 5f;
                        DebuffFactory.Enqueue(ref ctx.EntityManager, target, block.Payload.Apply.Id, duration, 1, ctx.Caster);
                        SpellDebugLogger.Log($"[SpellPipeline] Debuff block#{blockIndex} -> {SpellDebugLogger.FormatEntity(target)} debuffId={block.Payload.Apply.Id.ToString()} duration={duration:0.##}s.");
                        break;
                    }
                case EffectPayloadKind.SpawnDot:
                    ResolveDot(ref ctx, target, block.Payload.OverTime);
                    break;
                case EffectPayloadKind.SpawnHot:
                    ResolveHot(ref ctx, target, block.Payload.OverTime);
                    break;
                case EffectPayloadKind.SummonPet:
                    {
                        int resolvedLevel = ctx.Metadata.CategoryLevel > 0 ? ctx.Metadata.CategoryLevel : ctx.Metadata.SpellLevel;
                        PetSummonBridge.TrySummon(ref ctx.EntityManager, ctx.Caster, target, ctx.Metadata, block.Payload.Summon, resolvedLevel);
                        SpellDebugLogger.Log($"[SpellPipeline] Summon block#{blockIndex} caster={SpellDebugLogger.FormatEntity(ctx.Caster)} target={SpellDebugLogger.FormatEntity(target)} petId={block.Payload.Summon.PetId.ToString()} count={block.Payload.Summon.Count}.");
                        break;
                    }
                case EffectPayloadKind.SpawnAreaEffect:
                    {
                        var center = ResolveCenter(ref ctx.EntityManager, ctx.PrimaryTarget, ctx.Caster);
                        Framework.AreaEffects.Factory.AreaEffectFactory.SpawnCircle(ref ctx.EntityManager, block.Payload.Area.AreaId, center, block.Payload.Area.Radius, block.Payload.Area.Duration);
                        SpellDebugLogger.Log($"[SpellPipeline] AreaEffect block#{blockIndex} center=({center.x:0.00},{center.y:0.00}) radius={block.Payload.Area.Radius} duration={block.Payload.Area.Duration}.");
                        break;
                    }
                case EffectPayloadKind.ScriptReference:
                    SpellScriptBridge.TryInvoke(ref ctx.EntityManager, ctx.Caster, target, ctx.Metadata, block.Payload.Script.FeatureId, block.Payload.Script.Arguments);
                    SpellDebugLogger.Log($"[SpellPipeline] Script block#{blockIndex} feature={block.Payload.Script.FeatureId.ToString()} target={SpellDebugLogger.FormatEntity(target)}.");
                    break;
            }
        }

        private static void ResolveDot(ref EffectExecutionContext ctx, in Entity target, in DotHotPayload payload)
        {
            int level = ctx.Metadata.CategoryLevel > 0 ? ctx.Metadata.CategoryLevel : ctx.Metadata.SpellLevel;
            if (payload.Id.Length > 0 && DotCatalog.TryGet(payload.Id, out var dot))
            {
                DotFactory.Enqueue(ref ctx.EntityManager, target, payload.Id, dot.Dps, dot.TickInterval, dot.Duration, ctx.Caster, ctx.Metadata.CategoryId, level, 0);
                SpellDebugLogger.Log($"[SpellPipeline] DOT '{payload.Id.ToString()}' applied to {SpellDebugLogger.FormatEntity(target)} (catalog).");
            }
            else
            {
                int magnitude = payload.MagnitudeOverride != 0 ? payload.MagnitudeOverride : 10;
                DotFactory.Enqueue(ref ctx.EntityManager, target, payload.Id, magnitude, payload.TickIntervalOverride <= 0 ? 1f : payload.TickIntervalOverride, payload.DurationOverride <= 0 ? 8f : payload.DurationOverride, ctx.Caster, ctx.Metadata.CategoryId, level, 0);
                SpellDebugLogger.Log($"[SpellPipeline] DOT '{payload.Id.ToString()}' applied to {SpellDebugLogger.FormatEntity(target)} (override magnitude={magnitude}).");
            }
        }

        private static void ResolveHot(ref EffectExecutionContext ctx, in Entity target, in DotHotPayload payload)
        {
            int level = ctx.Metadata.CategoryLevel > 0 ? ctx.Metadata.CategoryLevel : ctx.Metadata.SpellLevel;
            if (payload.Id.Length > 0 && HotCatalog.TryGet(payload.Id, out var hot))
            {
                HotFactory.Enqueue(ref ctx.EntityManager, target, payload.Id, hot.Hps, hot.TickInterval, hot.Duration, ctx.Caster, ctx.Metadata.CategoryId, level, 0);
                SpellDebugLogger.Log($"[SpellPipeline] HOT '{payload.Id.ToString()}' applied to {SpellDebugLogger.FormatEntity(target)} (catalog).");
            }
            else
            {
                int magnitude = payload.MagnitudeOverride != 0 ? payload.MagnitudeOverride : 10;
                HotFactory.Enqueue(ref ctx.EntityManager, target, payload.Id, magnitude, payload.TickIntervalOverride <= 0 ? 1f : payload.TickIntervalOverride, payload.DurationOverride <= 0 ? 8f : payload.DurationOverride, ctx.Caster, ctx.Metadata.CategoryId, level, 0);
                SpellDebugLogger.Log($"[SpellPipeline] HOT '{payload.Id.ToString()}' applied to {SpellDebugLogger.FormatEntity(target)} (override magnitude={magnitude}).");
            }
        }

        private static float ApplyVariance(float amount, float variancePercent, uint seed, int blockIndex, in Entity target)
        {
            if (variancePercent <= 0f) return amount;
            var random = new Random((uint)math.hash(new int4(blockIndex, target.Index, target.Version, (int)seed)));
            float variance = variancePercent / 100f;
            float offset = random.NextFloat(-variance, variance);
            return math.max(0f, amount + (amount * offset));
        }

        private static float2 ResolveCenter(ref EntityManager em, in Entity preferred, in Entity fallback)
        {
            if (preferred != Entity.Null && em.HasComponent<Framework.Core.Components.Position>(preferred))
                return em.GetComponentData<Framework.Core.Components.Position>(preferred).Value;
            if (fallback != Entity.Null && em.HasComponent<Framework.Core.Components.Position>(fallback))
                return em.GetComponentData<Framework.Core.Components.Position>(fallback).Value;
            return float2.zero;
        }
    }
}
