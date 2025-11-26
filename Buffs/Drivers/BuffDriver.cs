using Framework.Buffs.Components;
using Framework.Buffs.Content;
using Framework.Core.Telemetry;
using Framework.TimedEffect.Content;
using Framework.TimedEffect.Requests;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Buffs.Drivers
{
    public static class BuffDriver
    {
        private static readonly FixedString64Bytes BuffAppliedTag = (FixedString64Bytes)"BuffApplied";

        public static void Apply(ref EntityManager em, in Entity target, in FixedString64Bytes buffId, float duration, int stacks)
        {
            if (!em.Exists(target))
                return;

            bool hasDefinition = BuffCatalog.TryGet(buffId, out var def);
            var statEffects = hasDefinition ? CloneStatEffects(def.StatEffects) : default;
            var shields = hasDefinition ? CloneDamageShields(def.DamageShields) : default;
            var wards = hasDefinition ? CloneMeleeWards(def.MeleeWards) : default;

            EnsureBuffData(ref em, target, buffId, statEffects, shields, wards);

            if (!em.HasBuffer<TimedEffectRequest>(target))
                em.AddBuffer<TimedEffectRequest>(target);
            var requests = em.GetBuffer<TimedEffectRequest>(target);

            var request = new TimedEffectRequest
            {
                Target = target,
                EffectId = buffId,
                Type = TimedEffectType.Buff,
                StackingMode = hasDefinition ? ConvertStackingMode(def.StackingMode) : TimedEffectStackingMode.AddStacks,
                CategoryId = hasDefinition ? def.CategoryId : default,
                CategoryLevel = hasDefinition ? def.CategoryLevel : 0,
                StackableCount = hasDefinition && def.StackableCount > 0 ? def.StackableCount : 1,
                AddStacks = math.max(1, stacks),
                MaxStacks = hasDefinition && def.MaxStacks > 0 ? def.MaxStacks : int.MaxValue,
                Duration = duration > 0f ? duration : (hasDefinition ? def.Duration : 0f),
                TickInterval = 0f,
                Source = Entity.Null
            };
            requests.Add(request);

            TelemetryRouter.Emit(BuffAppliedTag, stacks);
        }

        private static void EnsureBuffData(ref EntityManager em, in Entity target, in FixedString64Bytes buffId, in FixedList128Bytes<BuffStatEffect> statEffects, in FixedList64Bytes<DamageShieldSpec> shields, in FixedList64Bytes<MeleeWardSpec> wards)
        {
            if (!em.HasBuffer<BuffInstance>(target))
                em.AddBuffer<BuffInstance>(target);
            var buffer = em.GetBuffer<BuffInstance>(target);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].BuffId.Equals(buffId))
                {
                    buffer[i] = new BuffInstance
                    {
                        BuffId = buffId,
                        StatEffects = statEffects,
                        DamageShields = shields,
                        MeleeWards = wards
                    };
                    return;
                }
            }
            buffer.Add(new BuffInstance
            {
                BuffId = buffId,
                StatEffects = statEffects,
                DamageShields = shields,
                MeleeWards = wards
            });
        }

        private static TimedEffectStackingMode ConvertStackingMode(BuffStackingMode mode)
        {
            return mode switch
            {
                BuffStackingMode.RefreshDuration => TimedEffectStackingMode.RefreshDuration,
                BuffStackingMode.Replace => TimedEffectStackingMode.Replace,
                BuffStackingMode.CapStacks => TimedEffectStackingMode.CapStacks,
                _ => TimedEffectStackingMode.AddStacks
            };
        }

        private static FixedList128Bytes<BuffStatEffect> CloneStatEffects(in FixedList128Bytes<BuffStatEffect> src)
        {
            var dst = new FixedList128Bytes<BuffStatEffect>();
            for (int i = 0; i < src.Length; i++)
                dst.Add(src[i]);
            return dst;
        }

        private static FixedList64Bytes<DamageShieldSpec> CloneDamageShields(in FixedList64Bytes<DamageShieldSpec> src)
        {
            var dst = new FixedList64Bytes<DamageShieldSpec>();
            for (int i = 0; i < src.Length; i++)
                dst.Add(src[i]);
            return dst;
        }

        private static FixedList64Bytes<MeleeWardSpec> CloneMeleeWards(in FixedList64Bytes<MeleeWardSpec> src)
        {
            var dst = new FixedList64Bytes<MeleeWardSpec>();
            for (int i = 0; i < src.Length; i++)
                dst.Add(src[i]);
            return dst;
        }
    }
}
