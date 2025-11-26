using Framework.AreaEffects.Factory;
using Framework.Damage.Components;
using Framework.Damage.Factory;
using Framework.HOT.Factory;
using Framework.Melee.Blobs;
using Framework.Melee.Components;
using Framework.Spells.Factory;
using Framework.Buffs.Factory;
using Framework.DOT.Factory;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Framework.Melee.Runtime.Utilities
{
    public static class MeleeProcRouter
    {
        private static readonly MeleeProcPayloadArgs DefaultPayloadArgs = new MeleeProcPayloadArgs
        {
            Int0 = 0,
            Int1 = 0,
            Float0 = 0f,
            Float1 = 0f,
            DurationSeconds = 0f,
            IntervalSeconds = 0f,
            SecondaryId = default,
            TertiaryId = default
        };

        public static MeleeProcPayloadArgs ReadArgs(ref MeleeProcEntry entry)
        {
            return entry.Payload.Equals(default) ? DefaultPayloadArgs : entry.Payload.Value;
        }

        public static Entity ResolveTarget(ref MeleeProcEntry entry, in Entity attacker, in Entity defaultTarget)
        {
            switch (entry.TargetMode)
            {
                case MeleeProcTargetMode.Self:
                    return attacker;
                case MeleeProcTargetMode.Target:
                case MeleeProcTargetMode.ArcSet:
                case MeleeProcTargetMode.Group:
                default:
                    return defaultTarget != Entity.Null ? defaultTarget : attacker;
            }
        }

        public static bool Dispatch(ref EntityManager em, ref MeleeProcEntry entry, in MeleeProcPayloadArgs args, in Entity attacker, in Entity target, float damageDealt)
        {
            return Dispatch(ref em, entry.PayloadKind, entry.PayloadRef, entry.TargetMode, args, attacker, target, damageDealt);
        }

        public static bool Dispatch(ref EntityManager em, MeleeProcPayloadKind kind, in FixedString64Bytes payloadRef, MeleeProcTargetMode targetMode, in MeleeProcPayloadArgs args, in Entity attacker, in Entity target, float damageDealt)
        {
            switch (kind)
            {
                case MeleeProcPayloadKind.ExtraDamage:
                    return DispatchDamage(ref em, payloadRef, args, attacker, target, damageDealt);
                case MeleeProcPayloadKind.DamageOverTime:
                    return DispatchDot(ref em, payloadRef, args, attacker, target, damageDealt);
                case MeleeProcPayloadKind.HealOverTime:
                    return DispatchHot(ref em, payloadRef, args, attacker, target);
                case MeleeProcPayloadKind.Buff:
                case MeleeProcPayloadKind.Debuff:
                    return DispatchBuff(ref em, payloadRef, args, target);
                case MeleeProcPayloadKind.AreaEffect:
                    return DispatchArea(ref em, payloadRef, args, target);
                case MeleeProcPayloadKind.ScriptFeature:
                case MeleeProcPayloadKind.Spell:
                    return DispatchSpell(ref em, payloadRef, targetMode, attacker, target);
                default:
                    return false;
            }
        }

        private static bool DispatchDamage(ref EntityManager em, in FixedString64Bytes payloadRef, in MeleeProcPayloadArgs args, in Entity attacker, in Entity target, float damageDealt)
        {
            var packet = new DamagePacket
            {
                Amount = args.Int0 != 0 ? args.Int0 : math.max(1, (int)math.ceil(damageDealt)),
                Source = attacker,
                CritMult = args.Float0 > 0f ? args.Float0 : 1f,
                Tags = payloadRef,
                School = (DamageSchool)math.clamp(args.Int1, 0, (int)DamageSchool.Lightning)
            };

            if (args.Float1 > 0f) packet.IgnoreArmor = 1;
            if (args.Float1 < 0f) packet.IgnoreResist = 1;

            DamageFactory.EnqueueDamage(ref em, target, packet);
            return true;
        }

        private static bool DispatchDot(ref EntityManager em, in FixedString64Bytes payloadRef, in MeleeProcPayloadArgs args, in Entity attacker, in Entity target, float damageDealt)
        {
            int dps = args.Int0 != 0 ? args.Int0 : math.max(1, (int)math.ceil(damageDealt));
            float interval = args.IntervalSeconds > 0f ? args.IntervalSeconds : 1f;
            float duration = args.DurationSeconds > 0f ? args.DurationSeconds : interval * 3f;

            DotFactory.Enqueue(ref em, target, payloadRef, dps, interval, duration, attacker);
            return true;
        }

        private static bool DispatchHot(ref EntityManager em, in FixedString64Bytes payloadRef, in MeleeProcPayloadArgs args, in Entity attacker, in Entity target)
        {
            int hps = args.Int0 != 0 ? args.Int0 : 1;
            float interval = args.IntervalSeconds > 0f ? args.IntervalSeconds : 1f;
            float duration = args.DurationSeconds > 0f ? args.DurationSeconds : interval * 3f;

            HotFactory.Enqueue(ref em, target, payloadRef, hps, interval, duration, attacker);
            return true;
        }

        private static bool DispatchBuff(ref EntityManager em, in FixedString64Bytes payloadRef, in MeleeProcPayloadArgs args, in Entity target)
        {
            var stacks = args.Int0 > 0 ? args.Int0 : 1;
            var duration = args.DurationSeconds;
            if (duration <= 0f)
                duration = 0f;

            BuffFactory.Apply(ref em, target, payloadRef, duration, stacks);
            return true;
        }

        private static bool DispatchArea(ref EntityManager em, in FixedString64Bytes payloadRef, in MeleeProcPayloadArgs args, in Entity target)
        {
            if (!em.HasComponent<LocalTransform>(target))
                return false;

            var transform = em.GetComponentData<LocalTransform>(target);
            float2 center = new float2(transform.Position.x, transform.Position.z);
            float radius = args.Float0 > 0f ? args.Float0 : 3f;
            float lifetime = args.DurationSeconds > 0f ? args.DurationSeconds : 5f;

            AreaEffectFactory.SpawnCircle(ref em, payloadRef, center, radius, lifetime);
            return true;
        }

        private static bool DispatchSpell(ref EntityManager em, in FixedString64Bytes payloadRef, MeleeProcTargetMode targetMode, in Entity attacker, in Entity target)
        {
            var resolvedTarget = targetMode == MeleeProcTargetMode.Self
                ? attacker
                : (target != Entity.Null ? target : attacker);

            SpellPipelineFactory.Cast(ref em, attacker, resolvedTarget, payloadRef, 0);
            return true;
        }
    }
}
