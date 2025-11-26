using Framework.Core.Telemetry;
using Framework.Debuffs.Components;
using Framework.Debuffs.Content;
using Framework.TimedEffect.Content;
using Framework.TimedEffect.Requests;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Debuffs.Drivers
{
    public static class DebuffDriver
    {
        private static readonly FixedString64Bytes DebuffAppliedTag = (FixedString64Bytes)"DebuffApplied";

        public static void Apply(ref EntityManager em, in Entity target, in Entity source, in FixedString64Bytes debuffId,
            float durationOverride, int stacks, DebuffFlags extraFlags)
        {
            if (!em.Exists(target)) return;

            bool hasDefinition = DebuffCatalog.TryGet(debuffId, out var def);
            var statEffects = hasDefinition ? CloneStatEffects(def.StatEffects) : default;
            var combinedFlags = (hasDefinition ? def.Flags : DebuffFlags.None) | extraFlags;

            EnsureDebuffData(ref em, target, debuffId, statEffects, combinedFlags, source);

            if (!em.HasBuffer<TimedEffectRequest>(target))
                em.AddBuffer<TimedEffectRequest>(target);
            var requests = em.GetBuffer<TimedEffectRequest>(target);

            var request = new TimedEffectRequest
            {
                Target = target,
                EffectId = debuffId,
                Type = TimedEffectType.Debuff,
                StackingMode = hasDefinition ? ConvertStackingMode(def.StackingMode) : TimedEffectStackingMode.AddStacks,
                CategoryId = hasDefinition ? def.CategoryId : default,
                CategoryLevel = hasDefinition ? def.CategoryLevel : 0,
                StackableCount = hasDefinition && def.StackableCount > 0 ? def.StackableCount : 1,
                AddStacks = math.max(1, stacks),
                MaxStacks = hasDefinition && def.MaxStacks > 0 ? def.MaxStacks : int.MaxValue,
                Duration = ResolveDuration(def, durationOverride),
                TickInterval = 0f,
                Source = source
            };
            requests.Add(request);

            TelemetryRouter.Emit(DebuffAppliedTag, stacks);
        }

        private static void EnsureDebuffData(ref EntityManager em, in Entity target, in FixedString64Bytes debuffId,
            in FixedList128Bytes<DebuffStatEffect> statEffects, DebuffFlags flags, in Entity source)
        {
            if (!em.HasBuffer<DebuffInstance>(target))
                em.AddBuffer<DebuffInstance>(target);
            var buffer = em.GetBuffer<DebuffInstance>(target);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].DebuffId.Equals(debuffId))
                {
                    buffer[i] = new DebuffInstance { DebuffId = debuffId, Flags = flags, StatEffects = statEffects, Source = source };
                    return;
                }
            }
            buffer.Add(new DebuffInstance { DebuffId = debuffId, Flags = flags, StatEffects = statEffects, Source = source });
        }

        private static TimedEffectStackingMode ConvertStackingMode(DebuffStackingMode mode)
        {
            return mode switch
            {
                DebuffStackingMode.RefreshDuration => TimedEffectStackingMode.RefreshDuration,
                DebuffStackingMode.Replace => TimedEffectStackingMode.Replace,
                DebuffStackingMode.CapStacks => TimedEffectStackingMode.CapStacks,
                _ => TimedEffectStackingMode.AddStacks
            };
        }

        private static float ResolveDuration(in DebuffDefinition def, float overrideDuration)
        {
            if (overrideDuration > 0f)
                return overrideDuration;
            return def.Duration > 0f ? def.Duration : 0f;
        }

        private static FixedList128Bytes<DebuffStatEffect> CloneStatEffects(in FixedList128Bytes<DebuffStatEffect> src)
        {
            var clone = new FixedList128Bytes<DebuffStatEffect>();
            for (int i = 0; i < src.Length; i++)
                clone.Add(src[i]);
            return clone;
        }
    }
}
