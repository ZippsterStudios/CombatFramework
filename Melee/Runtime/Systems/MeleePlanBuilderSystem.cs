using Framework.Melee.Blobs;
using Framework.Melee.Components;
using Framework.Melee.Runtime.SystemGroups;
using Framework.Melee.Runtime.Utilities;
using Framework.Resources.Policies;
using Framework.Resources.Requests;
using Framework.Temporal.Components;
using Framework.Temporal.Policies;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Melee.Runtime.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(MeleeSystemGroup), OrderFirst = true)]
    public partial struct MeleePlanBuilderSystem : ISystem
    {
        private EntityQuery _telemetryQuery;

        public void OnCreate(ref SystemState state)
        {
            _telemetryQuery = state.GetEntityQuery(ComponentType.ReadOnly<MeleeTelemetryState>());
            if (_telemetryQuery.IsEmptyIgnoreFilter)
            {
                var telemetryEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(telemetryEntity, new MeleeTelemetryState());
                state.EntityManager.AddComponentData(telemetryEntity, new MeleeDebugConfig { EnableVerbose = 0 });
                state.EntityManager.AddBuffer<MeleeTelemetryEvent>(telemetryEntity);
            }
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var beginSim = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = beginSim.CreateCommandBuffer(state.WorldUnmanaged);
            var em = state.EntityManager;
            double now = SystemAPI.Time.ElapsedTime;
            float delta = SystemAPI.Time.DeltaTime;
            uint frameToken = (uint)math.max(0, math.floor(now / math.max(delta, 1e-6f)));
            var telemetry = SystemAPI.GetSingletonBuffer<MeleeTelemetryEvent>();
            telemetry.Clear();

            foreach (var (lockoutRef, entity) in SystemAPI.Query<RefRW<MeleeLockout>>()
                                                           .WithAll<MeleeAttackRequestElement>()
                                                           .WithAll<MeleeWeaponSlotElement>()
                                                           .WithEntityAccess())
            {
                var requests = SystemAPI.GetBuffer<MeleeAttackRequestElement>(entity);
                var slots = SystemAPI.GetBuffer<MeleeWeaponSlotElement>(entity);

                ProcessRiposteQueue(ref em, entity, requests, now, frameToken);
                if (requests.Length == 0)
                    continue;

                ref var lockout = ref lockoutRef.ValueRW;
                var temporalMul = ResolveTemporalMultiplier(ref em, entity);
                if (!em.HasBuffer<ResourceRequest>(entity))
                    ecb.AddBuffer<ResourceRequest>(entity);

                for (int i = 0; i < requests.Length; i++)
                {
                    var request = requests[i];
                    request.Attacker = entity;
                    requests[i] = request;

                    if (!TryFindSlot(slots, request.WeaponSlot, out var slot))
                    {
                        MeleeTelemetryWriter.Write(ref telemetry, MeleeTelemetryEventType.SwingRejected, entity, Entity.Null, request.WeaponSlot, request.RequestId, value0: 1f);
                        continue;
                    }

                    if (slot.Enabled == 0 || !slot.Definition.IsCreated)
                    {
                        MeleeTelemetryWriter.Write(ref telemetry, MeleeTelemetryEventType.SwingRejected, entity, Entity.Null, request.WeaponSlot, request.RequestId, value0: 2f);
                        continue;
                    }

                    ref var weaponDef = ref slot.Definition.Value;
                    float effectiveLockout = request.ChainLockoutSeconds > 0f
                        ? request.ChainLockoutSeconds
                        : weaponDef.LockoutSeconds * temporalMul;

                    bool ignoreGlobalLockout = (request.Flags & MeleeRequestFlags.MultiAttackChain) != 0 && request.ChainLockoutSeconds <= 0f;

                    if (!IsLockoutReady(now, ref lockout, effectiveLockout, ignoreGlobalLockout))
                    {
                        MeleeTelemetryWriter.Write(ref telemetry, MeleeTelemetryEventType.SwingRejected, entity, Entity.Null, request.WeaponSlot, request.RequestId, value0: 3f);
                        continue;
                    }

                    if (!CanAffordStamina(ref em, entity, ref weaponDef, request.Flags))
                    {
                        MeleeTelemetryWriter.Write(ref telemetry, MeleeTelemetryEventType.SwingRejected, entity, Entity.Null, request.WeaponSlot, request.RequestId, value0: 4f);
                        continue;
                    }

                    SpendStamina(ecb, in entity, ref weaponDef, request.Flags);

                    var swingEntity = ecb.CreateEntity();
                    float windup = math.max(0f, weaponDef.WindupSeconds) * temporalMul;
                    float active = math.max(0f, weaponDef.ActiveSeconds) * temporalMul;
                    float recovery = math.max(0f, weaponDef.RecoverySeconds) * temporalMul;

                    var aimDir = math.normalizesafe(request.AimDirection, new float3(0, 0, 1));
                    var rng = MeleeDeterministicRng.FromSeed(entity, request.WeaponSlot, request.RequestId, frameToken);

                    var contextComponent = new MeleeCastContext
                    {
                        Attacker = entity,
                        PreferredTarget = request.PreferredTarget,
                        WeaponSlot = request.WeaponSlot,
                        Definition = slot.Definition,
                        Phase = MeleePhaseState.Windup,
                        PhaseTimer = 0f,
                        WindupTime = windup,
                        ActiveTime = active,
                        RecoveryTime = recovery,
                        PenetrationRemaining = math.max(1, weaponDef.PenetrationCount),
                        AimDirection = aimDir,
                        CleaveMode = false,
                        CleaveArcDegrees = weaponDef.DefaultCleaveArcDegrees,
                        CleaveMaxTargets = math.max(1, weaponDef.DefaultCleaveMaxTargets),
                        DeterministicSeed = rng.SerializeState(),
                        SequenceId = request.RequestId,
                        RiposteOrigin = (byte)((request.Flags & MeleeRequestFlags.Riposte) != 0 ? 1 : 0),
                        CleaveResolved = 0,
                        Completed = 0,
                        ChainDepth = request.ChainDepth,
                        MultiAttackResolved = 0,
                        ChainShape = request.ChainShape,
                        ChainArcDegrees = request.ChainArcDegrees,
                        ChainRadius = request.ChainRadius,
                        ChainMaxTargets = request.ChainMaxTargets,
                        ChainDelaySeconds = request.ChainDelaySeconds,
                        ChainLockoutSeconds = request.ChainLockoutSeconds
                    };

                    if (request.ChainShape == MeleeChainAttackShape.Arc)
                    {
                        contextComponent.CleaveMode = true;
                        contextComponent.CleaveArcDegrees = request.ChainArcDegrees > 0f ? request.ChainArcDegrees : weaponDef.DefaultCleaveArcDegrees;
                        contextComponent.CleaveMaxTargets = request.ChainMaxTargets > 0 ? request.ChainMaxTargets : math.max(1, weaponDef.DefaultCleaveMaxTargets);
                        contextComponent.CleaveResolved = 1;
                    }
                    else if (request.ChainShape != MeleeChainAttackShape.None)
                    {
                        contextComponent.CleaveResolved = 1;
                        if (request.ChainShape == MeleeChainAttackShape.TrueArea && contextComponent.ChainRadius <= 0f)
                            contextComponent.ChainRadius = weaponDef.Range;
                        if (request.ChainShape == MeleeChainAttackShape.TrueArea && contextComponent.ChainMaxTargets <= 0)
                            contextComponent.ChainMaxTargets = math.max(1, weaponDef.DefaultCleaveMaxTargets);
                        if (request.ChainShape == MeleeChainAttackShape.RearArc)
                        {
                            contextComponent.ChainArcDegrees = request.ChainArcDegrees > 0f ? request.ChainArcDegrees : weaponDef.DefaultCleaveArcDegrees;
                            contextComponent.ChainMaxTargets = request.ChainMaxTargets > 0 ? request.ChainMaxTargets : math.max(1, weaponDef.DefaultCleaveMaxTargets);
                        }
                    }

                    if (request.ChainShape != MeleeChainAttackShape.None && request.ChainMaxTargets > 0)
                    {
                        contextComponent.PenetrationRemaining = math.max(1, request.ChainMaxTargets);
                    }

                    ecb.AddComponent(swingEntity, contextComponent);

                    ecb.AddBuffer<MeleeVictimElement>(swingEntity);
                    var procBuffer = ecb.AddBuffer<MeleeProcMergedEntryElement>(swingEntity);
                    AppendWeaponProcs(ref procBuffer, ref weaponDef, slot.SlotId);
                    AppendEquipmentProcs(ref procBuffer, ref em, entity, slot.SlotId);
                    AppendAugmentProcs(ref procBuffer, ref em, entity, slot.SlotId, now);

                    lockout.LastSwingTime = now;
                    if (effectiveLockout > 0f)
                        lockout.NextReadyTimeGlobal = math.max(lockout.NextReadyTimeGlobal, now + effectiveLockout);

                    MeleeTelemetryWriter.Write(ref telemetry, MeleeTelemetryEventType.SwingBegan, entity, Entity.Null, request.WeaponSlot, request.RequestId, value0: windup, value1: active);
                }

                requests.Clear();
            }
        }

        private static void ProcessRiposteQueue(ref EntityManager em, Entity entity, DynamicBuffer<MeleeAttackRequestElement> requests, double now, uint frameToken)
        {
            if (!em.HasBuffer<MeleeRiposteRequestElement>(entity))
                return;

            var ripostes = em.GetBuffer<MeleeRiposteRequestElement>(entity);
            if (ripostes.Length == 0)
                return;

            for (int i = ripostes.Length - 1; i >= 0; i--)
            {
                var riposte = ripostes[i];
                if (riposte.ExecuteAtTime > now)
                    continue;

                var request = new MeleeAttackRequestElement
                {
                    Attacker = entity,
                    WeaponSlot = riposte.WeaponSlot,
                    AimDirection = riposte.AimDirection,
                    PreferredTarget = Entity.Null,
                    Flags = MeleeRequestFlags.Riposte | MeleeRequestFlags.SkipStaminaCost,
                    RequestId = ComposeRequestId(frameToken, (uint)requests.Length),
                    ChainDepth = 0,
                    ChainShape = MeleeChainAttackShape.None,
                    ChainArcDegrees = 0f,
                    ChainRadius = 0f,
                    ChainMaxTargets = 0,
                    ChainDelaySeconds = 0f,
                    ChainLockoutSeconds = 0f
                };
                requests.Insert(0, request);
                ripostes.RemoveAt(i);
            }
        }

        private static uint ComposeRequestId(uint frameCount, uint localIndex)
        {
            return (frameCount << 12) ^ (localIndex * 2654435761u);
        }

        private static bool TryFindSlot(in DynamicBuffer<MeleeWeaponSlotElement> slots, in FixedString32Bytes slotId, out MeleeWeaponSlotElement slot)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].SlotId.Equals(slotId))
                {
                    slot = slots[i];
                    return true;
                }
            }

            slot = default;
            return false;
        }

        private static float ResolveTemporalMultiplier(ref EntityManager em, Entity entity)
        {
            if (!em.HasComponent<TemporalModifiers>(entity))
                return 1f;

            var modifiers = em.GetComponentData<TemporalModifiers>(entity);
            var mul = TemporalPolicy.IntervalMultiplier(modifiers.HastePercent, modifiers.SlowPercent);
            return math.max(0.01f, mul);
        }

        private static bool IsLockoutReady(double now, ref MeleeLockout lockout, float lockoutDuration, bool ignoreGlobalLockout)
        {
            if (!ignoreGlobalLockout && now < lockout.NextReadyTimeGlobal)
                return false;

            if (lockoutDuration > 0f)
            {
                var ready = lockout.LastSwingTime + lockoutDuration;
                return now >= ready;
            }

            return true;
        }

        private static bool CanAffordStamina(ref EntityManager em, Entity entity, ref MeleeWeaponDefBlob weaponDef, MeleeRequestFlags flags)
        {
            if ((flags & MeleeRequestFlags.SkipStaminaCost) != 0 || weaponDef.StaminaCost <= 0)
                return true;

            return ResourcePolicy.CanAffordStamina(in em, in entity, weaponDef.StaminaCost) == ResourcePolicy.Result.Allow;
        }

        private static void SpendStamina(EntityCommandBuffer ecb, in Entity entity, ref MeleeWeaponDefBlob weaponDef, MeleeRequestFlags flags)
        {
            if ((flags & MeleeRequestFlags.SkipStaminaCost) != 0 || weaponDef.StaminaCost <= 0)
                return;

            ecb.AppendToBuffer(entity, new ResourceRequest
            {
                Target = entity,
                Kind = ResourceKind.Stamina,
                Delta = -weaponDef.StaminaCost
            });
        }

        private static void AppendWeaponProcs(ref DynamicBuffer<MeleeProcMergedEntryElement> buffer, ref MeleeWeaponDefBlob weaponDef, in FixedString32Bytes slotId)
        {
            if (weaponDef.ProcEntries.Length == 0)
                return;

            for (int i = 0; i < weaponDef.ProcEntries.Length; i++)
            {
                ref var entry = ref weaponDef.ProcEntries[i];
                var sourceKey = ResolveSourceKey(slotId, entry.SourceKeyHint, default, 0u);
                buffer.Add(new MeleeProcMergedEntryElement
                {
                    Entry = entry,
                    SourceKey = sourceKey,
                    ChargeConsumed = 0
                });
            }
        }

        private static void AppendEquipmentProcs(ref DynamicBuffer<MeleeProcMergedEntryElement> buffer, ref EntityManager em, Entity attacker, in FixedString32Bytes slotId)
        {
            if (!em.HasBuffer<EquipmentBuffElement>(attacker))
                return;

            var equipments = em.GetBuffer<EquipmentBuffElement>(attacker);
            for (int i = 0; i < equipments.Length; i++)
            {
                var equip = equipments[i];
                if (!equip.ProcTable.IsCreated)
                    continue;

                ref var table = ref equip.ProcTable.Value;
                if (table.Entries.Length == 0)
                    continue;

                var sourceKey = ResolveSourceKey(slotId, default, equip.BuffId, 1u);
                for (int j = 0; j < table.Entries.Length; j++)
                {
                    ref var entry = ref table.Entries[j];
                    buffer.Add(new MeleeProcMergedEntryElement
                    {
                        Entry = entry,
                        SourceKey = sourceKey,
                        ChargeConsumed = 0
                    });
                }
            }
        }

        private static void AppendAugmentProcs(ref DynamicBuffer<MeleeProcMergedEntryElement> buffer, ref EntityManager em, Entity attacker, in FixedString32Bytes slotId, double now)
        {
            if (!em.HasBuffer<ProcAugmentElement>(attacker))
                return;

            var augments = em.GetBuffer<ProcAugmentElement>(attacker);
            for (int i = 0; i < augments.Length; i++)
            {
                var augment = augments[i];
                if (augment.ExpireTime > 0 && now >= augment.ExpireTime)
                    continue;
                if (!augment.ProcTable.IsCreated)
                    continue;

                ref var table = ref augment.ProcTable.Value;
                if (table.Entries.Length == 0)
                    continue;

                var sourceKey = ResolveSourceKey(slotId, default, augment.SourceBuffId, 2u);
                for (int j = 0; j < table.Entries.Length; j++)
                {
                    ref var entry = ref table.Entries[j];
                    buffer.Add(new MeleeProcMergedEntryElement
                    {
                        Entry = entry,
                        SourceKey = sourceKey,
                        ChargeConsumed = 0
                    });
                }
            }
        }

        private static FixedString32Bytes ResolveSourceKey(in FixedString32Bytes slotId, in FixedString32Bytes hint, in FixedString64Bytes externalId, uint category)
        {
            if (hint.Length > 0)
                return hint;

            uint hash = math.hash(new uint4(
                (uint)slotId.GetHashCode(),
                (uint)externalId.GetHashCode(),
                category,
                0u));

            FixedString32Bytes key = default;
            key.Append(hash);
            return key;
        }
    }
}
