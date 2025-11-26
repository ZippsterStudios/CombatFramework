using System;
using Framework.Melee.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Melee.Runtime.Utilities
{
    public static class MeleeRequestFactory
    {
        public struct ChainOverride
        {
            public MeleeChainAttackShape Shape;
            public float ArcDegrees;
            public float Radius;
            public int MaxTargets;
            public float DelaySeconds;
            public float LockoutSeconds;
        }

        public struct AttackOptions
        {
            public FixedString32Bytes WeaponSlot;
            public float3 AimDirection;
            public Entity PreferredTarget;
            public bool AllowRiposte;
            public bool SkipStaminaCost;
            public bool MarkAsRiposte;
            public bool UseChainOverride;
            public ChainOverride ChainOverride;
            public bool UseExplicitRequestId;
            public uint ExplicitRequestId;
        }

        public static AttackOptions CreateOptions(FixedString32Bytes weaponSlot, float3 aimDirection, Entity preferredTarget = default)
        {
            return new AttackOptions
            {
                WeaponSlot = weaponSlot,
                AimDirection = aimDirection,
                PreferredTarget = preferredTarget,
                AllowRiposte = true,
                SkipStaminaCost = false,
                MarkAsRiposte = false,
                UseChainOverride = false,
                UseExplicitRequestId = false,
                ExplicitRequestId = 0
            };
        }

        public static uint QueueAttack(ref EntityManager em, Entity attacker, FixedString32Bytes weaponSlot, float3 aimDirection, Entity preferredTarget = default)
        {
            var options = CreateOptions(weaponSlot, aimDirection, preferredTarget);
            return QueueAttack(ref em, attacker, in options);
        }

        public static uint QueueAttack(ref EntityManager em, Entity attacker, in AttackOptions options)
        {
            if (!em.Exists(attacker))
                throw new ArgumentException("Attacker entity does not exist.", nameof(attacker));

            if (options.WeaponSlot.Length == 0)
                throw new ArgumentException("Weapon slot must be specified.", nameof(options));

            if (!em.HasBuffer<MeleeAttackRequestElement>(attacker))
                em.AddBuffer<MeleeAttackRequestElement>(attacker);

            var buffer = em.GetBuffer<MeleeAttackRequestElement>(attacker);
            var requestId = options.UseExplicitRequestId && options.ExplicitRequestId != 0
                ? options.ExplicitRequestId
                : AllocateRequestId(ref em, attacker);

            var flags = MeleeRequestFlags.None;
            if (options.AllowRiposte)
                flags |= MeleeRequestFlags.AllowRiposte;
            if (options.SkipStaminaCost)
                flags |= MeleeRequestFlags.SkipStaminaCost;
            if (options.MarkAsRiposte)
                flags |= MeleeRequestFlags.Riposte;

            var request = new MeleeAttackRequestElement
            {
                Attacker = attacker,
                WeaponSlot = options.WeaponSlot,
                AimDirection = math.normalizesafe(options.AimDirection, new float3(0, 0, 1)),
                PreferredTarget = options.PreferredTarget,
                Flags = flags,
                RequestId = requestId,
                ChainDepth = 0,
                ChainShape = MeleeChainAttackShape.None,
                ChainArcDegrees = 0f,
                ChainRadius = 0f,
                ChainMaxTargets = 0,
                ChainDelaySeconds = 0f,
                ChainLockoutSeconds = 0f
            };

            if (options.UseChainOverride && options.ChainOverride.Shape != MeleeChainAttackShape.None)
            {
                var overrideData = options.ChainOverride;
                request.ChainShape = overrideData.Shape;
                request.ChainArcDegrees = overrideData.ArcDegrees;
                request.ChainRadius = overrideData.Radius;
                request.ChainMaxTargets = overrideData.MaxTargets;
                request.ChainDelaySeconds = overrideData.DelaySeconds;
                request.ChainLockoutSeconds = overrideData.LockoutSeconds;
            }

            buffer.Add(request);
            return requestId;
        }

        private static uint AllocateRequestId(ref EntityManager em, Entity attacker)
        {
            if (!em.HasComponent<MeleeRequestSequence>(attacker))
                em.AddComponentData(attacker, new MeleeRequestSequence { NextId = 1 });

            var sequence = em.GetComponentData<MeleeRequestSequence>(attacker);
            var id = sequence.NextId == 0 ? 1u : sequence.NextId;
            sequence.NextId = id + 1;
            em.SetComponentData(attacker, sequence);
            return id;
        }
    }
}
