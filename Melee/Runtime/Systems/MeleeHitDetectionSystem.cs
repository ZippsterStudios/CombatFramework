using Framework.Buffs.Components;
using Framework.Damage.Components;
using Framework.Damage.Requests;
using Framework.Melee.Blobs;
using Framework.Melee.Components;
using Framework.Melee.Runtime.SystemGroups;
using Framework.Melee.Runtime.Utilities;
using Framework.Resources.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Framework.Melee.Runtime.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(MeleeSystemGroup))]
    [UpdateAfter(typeof(MeleeCleaveRollSystem))]
    public partial struct MeleeHitDetectionSystem : ISystem
    {
        private ComponentLookup<MeleeDefenseTuning> _defenseLookupRO;
        private ComponentLookup<MeleeDefenseWindowState> _defenseWindowLookupRO;
        private ComponentLookup<MeleeStatSnapshot> _statLookupRO;
        private ComponentLookup<LocalTransform> _transformLookupRO;
        private ComponentLookup<Health> _healthLookupRO;
        private BufferLookup<DamageRequest> _damageBuffersRW;
        private BufferLookup<MeleeProcRuntimeStateElement> _procRuntimeBuffersRW;
        private BufferLookup<DamageShieldStateElement> _shieldLookupRW;
        private BufferLookup<MeleeWardStateElement> _wardLookupRW;
        private EntityQuery _potentialTargetQuery;

        public void OnCreate(ref SystemState state)
        {
            _defenseLookupRO = state.GetComponentLookup<MeleeDefenseTuning>(true);
            _defenseWindowLookupRO = state.GetComponentLookup<MeleeDefenseWindowState>(true);
            _statLookupRO = state.GetComponentLookup<MeleeStatSnapshot>(true);
            _transformLookupRO = state.GetComponentLookup<LocalTransform>(true);
            _healthLookupRO = state.GetComponentLookup<Health>(true);
            _damageBuffersRW = state.GetBufferLookup<DamageRequest>();
            _procRuntimeBuffersRW = state.GetBufferLookup<MeleeProcRuntimeStateElement>();
            _shieldLookupRW = state.GetBufferLookup<DamageShieldStateElement>();
            _wardLookupRW = state.GetBufferLookup<MeleeWardStateElement>();
            _potentialTargetQuery = state.GetEntityQuery(ComponentType.ReadOnly<Health>(), ComponentType.ReadOnly<LocalTransform>());
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _defenseLookupRO.Update(ref state);
            _defenseWindowLookupRO.Update(ref state);
            _statLookupRO.Update(ref state);
            _transformLookupRO.Update(ref state);
            _healthLookupRO.Update(ref state);
            _damageBuffersRW.Update(ref state);
            _procRuntimeBuffersRW.Update(ref state);
            _shieldLookupRW.Update(ref state);
            _wardLookupRW.Update(ref state);

            var em = state.EntityManager;
            var beginSim = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = beginSim.CreateCommandBuffer(state.WorldUnmanaged);
            var telemetry = SystemAPI.GetSingletonBuffer<MeleeTelemetryEvent>();

            var targetEntities = _potentialTargetQuery.ToEntityArray(Allocator.Temp);
            var targetTransforms = _potentialTargetQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var candidates = new NativeList<Entity>(Allocator.Temp);

            double now = SystemAPI.Time.ElapsedTime;
            float deltaTime = SystemAPI.Time.DeltaTime;
            uint frameToken = (uint)math.max(0, math.floor(now / math.max(deltaTime, 1e-6f)));

            try
            {
                foreach (var (contextRef, victims, procEntries, swingEntity) in SystemAPI.Query<RefRW<MeleeCastContext>, DynamicBuffer<MeleeVictimElement>, DynamicBuffer<MeleeProcMergedEntryElement>>().WithEntityAccess())
                {
                    ref var context = ref contextRef.ValueRW;
                    if (context.Phase != MeleePhaseState.Active || context.PenetrationRemaining <= 0 || !context.Definition.IsCreated)
                        continue;

                    if (!_transformLookupRO.HasComponent(context.Attacker))
                        continue;

                    var attackerTransform = _transformLookupRO[context.Attacker];
                    var attackerPos = attackerTransform.Position;
                    candidates.Clear();

                    ref var weaponDef = ref context.Definition.Value;

                    if (context.ChainShape == MeleeChainAttackShape.TrueArea)
                    {
                        GatherAreaCandidates(in context, attackerPos, targetEntities, targetTransforms, ref candidates);
                    }
                    else if (context.ChainShape == MeleeChainAttackShape.RearArc)
                    {
                        GatherRearArcCandidates(ref weaponDef, in context, attackerPos, targetEntities, targetTransforms, ref candidates);
                    }
                    else if (context.CleaveMode)
                    {
                        GatherArcCandidates(ref weaponDef, in context, attackerPos, targetEntities, targetTransforms, ref candidates);
                    }
                    else
                    {
                        if (context.PreferredTarget != Entity.Null &&
                            IsTargetValid(context.PreferredTarget, context.Attacker, ref weaponDef, attackerPos, targetEntities, targetTransforms, out var _))
                        {
                            candidates.Add(context.PreferredTarget);
                        }
                        else
                        {
                            var fallback = FindNearestTarget(context.Attacker, attackerPos, ref weaponDef, targetEntities, targetTransforms);
                            if (fallback != Entity.Null)
                                candidates.Add(fallback);
                        }
                    }

                    if (candidates.Length == 0)
                        continue;

                    var rng = MeleeDeterministicRng.FromRaw(context.DeterministicSeed);
                    var frameStamp = frameToken;

                    for (int i = 0; i < candidates.Length && context.PenetrationRemaining > 0; i++)
                    {
                        var target = candidates[i];
                        if (target == Entity.Null || target == context.Attacker)
                            continue;
                        if (!_healthLookupRO.HasComponent(target))
                            continue;

                        if (AlreadyVictim(victims, target, frameStamp))
                            continue;

                        var defense = _defenseLookupRO.HasComponent(target) ? _defenseLookupRO[target] : default;
                        var defenseWindow = _defenseWindowLookupRO.HasComponent(target) ? _defenseWindowLookupRO[target] : default;

                        if (RollDodge(ref rng, defense))
                        {
                            MeleeTelemetryWriter.Write(ref telemetry, MeleeTelemetryEventType.Dodged, context.Attacker, target, context.WeaponSlot, context.SequenceId);
                            MarkVictim(victims, target, frameStamp);
                            continue;
                        }

                        if (RollParry(ref rng, defense, defenseWindow, now))
                        {
                            ScheduleRiposte(ecb, target, context.Attacker, defense, now, deltaTime, ref telemetry);
                            MeleeTelemetryWriter.Write(ref telemetry, MeleeTelemetryEventType.Parried, context.Attacker, target, context.WeaponSlot, context.SequenceId);
                            TriggerDamageShields(ref em,
                                                 target,
                                                 context.Attacker,
                                                 context.WeaponSlot,
                                                 context.SequenceId,
                                                 now,
                                                 0,
                                                 blocked: false,
                                                 parried: true,
                                                 ref telemetry);
                            MarkVictim(victims, target, frameStamp);
                            continue;
                        }

                        bool blocked = RollBlock(ref rng, defense);
                        int finalDamage = weaponDef.BaseDamage.Amount;

                        if (blocked)
                        {
                            finalDamage = MeleeDefenseUtility.ApplyBlock(finalDamage, defense.BlockPercent, defense.BlockFlat);
                            MeleeTelemetryWriter.Write(ref telemetry, MeleeTelemetryEventType.Blocked, context.Attacker, target, context.WeaponSlot, context.SequenceId, value0: finalDamage);
                        }

                        if (finalDamage < 0)
                            finalDamage = 0;

                        ApplyMeleeWards(ref em,
                                        target,
                                        context.Attacker,
                                        context.WeaponSlot,
                                        context.SequenceId,
                                        now,
                                        ref finalDamage,
                                        ref telemetry);

                        if (finalDamage < 0)
                            finalDamage = 0;

                        if (finalDamage > 0)
                        {
                            if (!_damageBuffersRW.HasBuffer(target))
                                ecb.AddBuffer<DamageRequest>(target);

                            if (_damageBuffersRW.HasBuffer(target))
                            {
                                var packet = weaponDef.BaseDamage;
                                packet.Source = context.Attacker;
                                packet.Amount = finalDamage;
                                var damageBuffer = _damageBuffersRW[target];
                                damageBuffer.Add(new DamageRequest { Target = target, Packet = packet });
                            }
                        }

                        TriggerDamageShields(ref em,
                                             target,
                                             context.Attacker,
                                             context.WeaponSlot,
                                             context.SequenceId,
                                             now,
                                             finalDamage,
                                             blocked,
                                             parried: false,
                                             ref telemetry);

                        EvaluateProcs(ref em, procEntries, ref context, target, ref rng, now, finalDamage, ref telemetry, context.Attacker, ref ecb);

                        MeleeTelemetryWriter.Write(ref telemetry, MeleeTelemetryEventType.Hit, context.Attacker, target, context.WeaponSlot, context.SequenceId, value0: finalDamage);

                        context.PenetrationRemaining -= 1;
                        MarkVictim(victims, target, frameStamp);
                    }

                    context.DeterministicSeed = rng.SerializeState();
                }
            }
            finally
            {
                candidates.Dispose();
                targetEntities.Dispose();
                targetTransforms.Dispose();
            }
        }

        private static void GatherAreaCandidates(in MeleeCastContext context,
                                                float3 attackerPosition,
                                                NativeArray<Entity> targetEntities,
                                                NativeArray<LocalTransform> targetTransforms,
                                                ref NativeList<Entity> results)
        {
            float radius = context.ChainRadius > 0f ? context.ChainRadius : 3f;
            float radiusSq = radius * radius;
            int maxTargets = context.ChainMaxTargets > 0 ? context.ChainMaxTargets : int.MaxValue;

            for (int i = 0; i < targetEntities.Length && results.Length < maxTargets; i++)
            {
                var target = targetEntities[i];
                if (target == Entity.Null || target == context.Attacker)
                    continue;

                var targetPos = targetTransforms[i].Position;
                float3 toTarget = targetPos - attackerPosition;
                if (math.lengthsq(toTarget) > radiusSq)
                    continue;

                results.Add(target);
            }
        }

        private static void GatherRearArcCandidates(ref MeleeWeaponDefBlob def,
                                                    in MeleeCastContext context,
                                                    float3 attackerPosition,
                                                    NativeArray<Entity> targetEntities,
                                                    NativeArray<LocalTransform> targetTransforms,
                                                    ref NativeList<Entity> results)
        {
            float maxRangeSq = def.Range * def.Range;
            float arcDegrees = context.ChainArcDegrees > 0f ? context.ChainArcDegrees : def.BaselineArcDegrees;
            float arcCos = math.cos(math.radians(math.clamp(arcDegrees * 0.5f, 0f, 180f)));
            int maxTargets = context.ChainMaxTargets > 0 ? context.ChainMaxTargets : int.MaxValue;
            float3 forward = math.normalizesafe(context.AimDirection, new float3(0, 0, 1));

            for (int i = 0; i < targetEntities.Length && results.Length < maxTargets; i++)
            {
                var target = targetEntities[i];
                if (target == Entity.Null || target == context.Attacker)
                    continue;

                var targetPos = targetTransforms[i].Position;
                float3 toTarget = targetPos - attackerPosition;
                float distanceSq = math.lengthsq(toTarget);
                if (distanceSq > maxRangeSq || distanceSq <= 0.001f)
                    continue;

                float3 direction = math.normalizesafe(toTarget);
                if (math.dot(forward, direction) >= 0f)
                    continue;

                if (math.dot(-forward, direction) < arcCos)
                    continue;

                results.Add(target);
            }
        }

        private static void GatherArcCandidates(ref MeleeWeaponDefBlob def,
                                                  in MeleeCastContext context,
                                                  float3 attackerPosition,
                                                  NativeArray<Entity> targetEntities,
                                                  NativeArray<LocalTransform> targetTransforms,
                                                  ref NativeList<Entity> results)
        {
            float maxRangeSq = def.Range * def.Range;

            for (int i = 0; i < targetEntities.Length && results.Length < context.CleaveMaxTargets; i++)
            {
                var target = targetEntities[i];
                if (target == Entity.Null || target == context.Attacker)
                    continue;

                var targetPos = targetTransforms[i].Position;
                float3 toTarget = targetPos - attackerPosition;
                float distanceSq = math.lengthsq(toTarget);
                if (distanceSq > maxRangeSq || distanceSq <= 0.001f)
                    continue;

                if (MeleeArcUtility.IsWithinArc(context.AimDirection, toTarget, context.CleaveArcDegrees))
                    results.Add(target);
            }
        }

        private static bool IsTargetValid(Entity target,
                                          Entity attacker,
                                          ref MeleeWeaponDefBlob def,
                                          float3 attackerPosition,
                                          NativeArray<Entity> targetEntities,
                                          NativeArray<LocalTransform> targetTransforms,
                                          out float3 targetPosition)
        {
            targetPosition = float3.zero;
            float maxRangeSq = def.Range * def.Range;

            for (int i = 0; i < targetEntities.Length; i++)
            {
                if (targetEntities[i] != target)
                    continue;

                targetPosition = targetTransforms[i].Position;
                var offset = targetPosition - attackerPosition;
                var distSq = math.lengthsq(offset);
                return target != attacker && distSq <= maxRangeSq;
            }

            return false;
        }

        private static Entity FindNearestTarget(Entity attacker,
                                                float3 attackerPosition,
                                                ref MeleeWeaponDefBlob def,
                                                NativeArray<Entity> targetEntities,
                                                NativeArray<LocalTransform> targetTransforms)
        {
            float maxRangeSq = def.Range * def.Range;
            float bestDist = float.MaxValue;
            Entity best = Entity.Null;

            for (int i = 0; i < targetEntities.Length; i++)
            {
                var target = targetEntities[i];
                if (target == attacker)
                    continue;

                var distSq = math.lengthsq(targetTransforms[i].Position - attackerPosition);
                if (distSq <= maxRangeSq && distSq < bestDist)
                {
                    bestDist = distSq;
                    best = target;
                }
            }

            return best;
        }

        private static bool AlreadyVictim(DynamicBuffer<MeleeVictimElement> victims, Entity target, uint frameStamp)
        {
            for (int i = 0; i < victims.Length; i++)
            {
                if (victims[i].Target == target && victims[i].LastHitTick == frameStamp)
                    return true;
            }

            return false;
        }

        private static void MarkVictim(DynamicBuffer<MeleeVictimElement> victims, Entity target, uint frameStamp)
        {
            for (int i = 0; i < victims.Length; i++)
            {
                if (victims[i].Target == target)
                {
                    victims[i] = new MeleeVictimElement { Target = target, LastHitTick = frameStamp };
                    return;
                }
            }

            victims.Add(new MeleeVictimElement { Target = target, LastHitTick = frameStamp });
        }

        private static bool RollDodge(ref MeleeDeterministicRng rng, in MeleeDefenseTuning tuning)
        {
            return rng.RollPercent(math.max(0f, tuning.DodgeChance));
        }

        private static bool RollParry(ref MeleeDeterministicRng rng, in MeleeDefenseTuning tuning, in MeleeDefenseWindowState window, double now)
        {
            if (window.ParryWindowActive != 0 && window.WindowExpiry > now)
                return true;

            return rng.RollPercent(math.max(0f, tuning.ParryChance));
        }

        private static bool RollBlock(ref MeleeDeterministicRng rng, in MeleeDefenseTuning tuning)
        {
            return rng.RollPercent(math.max(0f, tuning.BlockChance));
        }

        private static void ScheduleRiposte(EntityCommandBuffer ecb,
                                            Entity defender,
                                            Entity attacker,
                                            in MeleeDefenseTuning tuning,
                                            double now,
                                            float deltaTime,
                                            ref DynamicBuffer<MeleeTelemetryEvent> telemetry)
        {
            if (tuning.RipostePolicy == MeleeRipostePolicy.None)
                return;

            double executeAt = tuning.RipostePolicy == MeleeRipostePolicy.Immediate
                ? now
                : now + deltaTime;

            ecb.AppendToBuffer(defender, new MeleeRiposteRequestElement
            {
                SourceSwing = attacker,
                WeaponSlot = tuning.RiposteWeaponSlot,
                AimDirection = float3.zero,
                ExecuteAtTime = executeAt,
                SourceRequestId = 0
            });

            MeleeTelemetryWriter.Write(ref telemetry, MeleeTelemetryEventType.RiposteQueued, defender, attacker, tuning.RiposteWeaponSlot, 0);
        }

        private void EvaluateProcs(ref EntityManager em,
                                   DynamicBuffer<MeleeProcMergedEntryElement> mergedEntries,
                                   ref MeleeCastContext context,
                                   Entity target,
                                   ref MeleeDeterministicRng rng,
                                   double now,
                                   int finalDamage,
                                   ref DynamicBuffer<MeleeTelemetryEvent> telemetry,
                                   Entity attacker,
                                   ref EntityCommandBuffer ecb)
        {
            if (mergedEntries.Length == 0)
                return;

            DynamicBuffer<MeleeProcRuntimeStateElement> runtime;
            if (_procRuntimeBuffersRW.HasBuffer(attacker))
            {
                runtime = _procRuntimeBuffersRW[attacker];
            }
            else
            {
                if (!em.HasBuffer<MeleeProcRuntimeStateElement>(attacker))
                    em.AddBuffer<MeleeProcRuntimeStateElement>(attacker);
                runtime = em.GetBuffer<MeleeProcRuntimeStateElement>(attacker);
            }

            for (int i = 0; i < mergedEntries.Length; i++)
            {
                ref var merged = ref mergedEntries.ElementAt(i);
                ref var entry = ref merged.Entry;
                var sourceKey = merged.SourceKey.Length > 0 ? merged.SourceKey : entry.SourceKeyHint;

                if (entry.TriggerOnZeroDamage == 0 && finalDamage <= 0)
                    continue;

                if (!rng.RollPercent(entry.ChancePercent))
                    continue;

                if (!IsProcReady(ref runtime, ref entry, sourceKey, now, out var stateIndex))
                    continue;

                var args = MeleeProcRouter.ReadArgs(ref entry);
                var resolvedTarget = MeleeProcRouter.ResolveTarget(ref entry, context.Attacker, target);

                if (!MeleeProcRouter.Dispatch(ref em, ref entry, args, context.Attacker, resolvedTarget, finalDamage))
                    continue;

                bool consumeCharge = entry.ChargeMode == MeleeProcChargeMode.PerTargetApplied || merged.ChargeConsumed == 0;
                StampProc(ref runtime, stateIndex, ref entry, now, consumeCharge);

                MeleeTelemetryWriter.Write(ref telemetry,
                    MeleeTelemetryEventType.ProcTriggered,
                    attacker,
                    resolvedTarget,
                    context.WeaponSlot,
                    context.SequenceId,
                    value0: entry.ChancePercent,
                    value1: entry.InternalCooldownSeconds);
                if (entry.ChargeMode == MeleeProcChargeMode.PerTriggerSuccess && merged.ChargeConsumed == 0)
                    merged.ChargeConsumed = 1;
            }
        }

        private static bool IsProcReady(ref DynamicBuffer<MeleeProcRuntimeStateElement> runtime, ref MeleeProcEntry entry, in FixedString32Bytes sourceKey, double now, out int index)
        {
            for (int i = 0; i < runtime.Length; i++)
            {
                var state = runtime[i];
                if (!state.ProcId.Equals(entry.ProcId) || !state.SourceKey.Equals(sourceKey))
                    continue;

                if (state.ExpireTime > 0 && now >= state.ExpireTime)
                {
                    runtime.RemoveAtSwapBack(i);
                    i--;
                    continue;
                }

                if (entry.WindowSeconds > 0 && state.WindowExpiry > 0 && now >= state.WindowExpiry)
                {
                    state.TriggerCount = 0;
                    state.WindowExpiry = 0;
                }

                if (entry.MaxActivations > 0 && state.RemainingActivations == 0)
                {
                    runtime[i] = state;
                    index = i;
                    return false;
                }

                if (now < state.NextReadyTime)
                {
                    runtime[i] = state;
                    index = i;
                    return false;
                }

                if (entry.MaxTriggers > 0 && state.TriggerCount >= entry.MaxTriggers)
                {
                    if (entry.WindowSeconds <= 0)
                    {
                        runtime[i] = state;
                        index = i;
                        return false;
                    }

                    if (state.WindowExpiry == 0 || now >= state.WindowExpiry)
                    {
                        state.TriggerCount = 0;
                        state.WindowExpiry = 0;
                    }
                    else
                    {
                        runtime[i] = state;
                        index = i;
                        return false;
                    }
                }

                runtime[i] = state;
                index = i;
                return true;
            }

            var newState = new MeleeProcRuntimeStateElement
            {
                ProcId = entry.ProcId,
                SourceKey = sourceKey,
                NextReadyTime = now,
                WindowExpiry = 0,
                ExpireTime = entry.DurationSeconds > 0f ? now + entry.DurationSeconds : 0,
                TriggerCount = 0,
                RemainingActivations = entry.MaxActivations
            };

            runtime.Add(newState);
            index = runtime.Length - 1;
            return true;
        }

        private static void StampProc(ref DynamicBuffer<MeleeProcRuntimeStateElement> runtime, int index, ref MeleeProcEntry entry, double now, bool consumeCharge)
        {
            if (index < 0 || index >= runtime.Length)
                return;

            var state = runtime[index];
            state.TriggerCount += 1;
            state.NextReadyTime = now + entry.InternalCooldownSeconds;

            if (entry.WindowSeconds > 0 && state.WindowExpiry == 0)
                state.WindowExpiry = now + entry.WindowSeconds;

            if (entry.MaxActivations > 0 && consumeCharge && state.RemainingActivations > 0)
                state.RemainingActivations--;

            runtime[index] = state;
        }

        private float ApplyMeleeWards(ref EntityManager em,
                                      Entity defender,
                                      Entity attacker,
                                      in FixedString32Bytes weaponSlot,
                                      uint sequenceId,
                                      double now,
                                      ref int damage,
                                      ref DynamicBuffer<MeleeTelemetryEvent> telemetry)
        {
            if (!_wardLookupRW.HasBuffer(defender))
                return 0f;

            var wards = _wardLookupRW[defender];
            float totalAbsorbed = 0f;

            for (int i = 0; i < wards.Length; i++)
            {
                var ward = wards[i];

                if (ward.ExpireTime > 0 && now >= ward.ExpireTime)
                {
                    wards.RemoveAtSwapBack(i--);
                    continue;
                }

                bool hasFiniteCharges = ward.MaxActivations > 0;
                if (hasFiniteCharges && ward.RemainingActivations == 0 && ward.RemainingPool <= 0)
                {
                    wards.RemoveAtSwapBack(i--);
                    continue;
                }

                bool allowZeroDamage = ward.TriggerOnZeroDamage != 0;
                if (damage <= 0 && !allowZeroDamage)
                    continue;

                int originalDamage = damage;
                bool consumed = false;

                if (damage > 0 && ward.RemainingPool > 0)
                {
                    int poolAbsorb = math.min(damage, ward.RemainingPool);
                    if (poolAbsorb > 0)
                    {
                        damage -= poolAbsorb;
                        ward.RemainingPool -= poolAbsorb;
                        consumed = true;
                    }
                }

                if (damage > 0 && ward.AbsorbFlat > 0f)
                {
                    int before = damage;
                    int reduction = (int)math.round(ward.AbsorbFlat);
                    if (reduction > 0)
                    {
                        damage = math.max(0, damage - reduction);
                        if (damage != before)
                            consumed = true;
                    }
                }

                if (damage > 0 && ward.AbsorbPercent > 0f)
                {
                    float percent = math.clamp(ward.AbsorbPercent, 0f, 0.99f);
                    int before = damage;
                    damage = (int)math.round(damage * math.max(0f, 1f - percent));
                    if (damage != before)
                        consumed = true;
                }

                if (!consumed && damage <= 0 && allowZeroDamage)
                    consumed = true;

                if (!consumed)
                    continue;

                if (hasFiniteCharges && ward.RemainingActivations > 0)
                    ward.RemainingActivations--;

                float absorbed = math.max(0, originalDamage - damage);
                totalAbsorbed += absorbed;

                if (hasFiniteCharges && ward.RemainingActivations == 0 && ward.RemainingPool <= 0)
                {
                    wards.RemoveAtSwapBack(i--);
                }
                else
                {
                    wards[i] = ward;
                }

                MeleeTelemetryWriter.Write(ref telemetry, MeleeTelemetryEventType.WardConsumed, defender, attacker, weaponSlot, sequenceId, value0: absorbed, value1: ward.RemainingActivations);

                if (damage <= 0)
                    break;
            }

            return totalAbsorbed;
        }

        private void TriggerDamageShields(ref EntityManager em,
                                          Entity defender,
                                          Entity attacker,
                                          in FixedString32Bytes weaponSlot,
                                          uint sequenceId,
                                          double now,
                                          int damage,
                                          bool blocked,
                                          bool parried,
                                          ref DynamicBuffer<MeleeTelemetryEvent> telemetry)
        {
            if (!_shieldLookupRW.HasBuffer(defender))
                return;

            var shields = _shieldLookupRW[defender];

            for (int i = 0; i < shields.Length; i++)
            {
                var shield = shields[i];

                if (shield.ExpireTime > 0 && now >= shield.ExpireTime)
                {
                    shields.RemoveAtSwapBack(i--);
                    continue;
                }

                bool hasFiniteCharges = shield.MaxActivations > 0;
                if (hasFiniteCharges && shield.RemainingActivations == 0)
                {
                    shields.RemoveAtSwapBack(i--);
                    continue;
                }

                if (now < shield.NextReadyTime)
                    continue;

                if (parried && shield.TriggerOnParry == 0)
                    continue;

                if (!parried && blocked && shield.TriggerOnBlock == 0)
                    continue;

                bool parryTrigger = parried && shield.TriggerOnParry != 0;
                bool blockTrigger = !parried && blocked && shield.TriggerOnBlock != 0;
                bool zeroDamage = damage <= 0;
                bool zeroAllowed = shield.TriggerOnZeroDamage != 0 || parryTrigger || blockTrigger;
                if (zeroDamage && !zeroAllowed)
                    continue;

                var kind = (MeleeProcPayloadKind)shield.PayloadKind;
                var targetMode = (MeleeProcTargetMode)shield.TargetMode;
                var effectTarget = ResolveShieldTarget(targetMode, defender, attacker);
                var args = new MeleeProcPayloadArgs
                {
                    Int0 = shield.ArgInt0,
                    Int1 = shield.ArgInt1,
                    Float0 = shield.ArgFloat0,
                    Float1 = shield.ArgFloat1,
                    DurationSeconds = 0f,
                    IntervalSeconds = shield.IntervalSeconds,
                    SecondaryId = default,
                    TertiaryId = default
                };

                if (!MeleeProcRouter.Dispatch(ref em, kind, shield.PayloadRef, targetMode, args, defender, effectTarget, damage))
                    continue;

                if (hasFiniteCharges && shield.RemainingActivations > 0)
                    shield.RemainingActivations--;

                shield.NextReadyTime = now + shield.InternalCooldownSeconds;

                if (hasFiniteCharges && shield.RemainingActivations == 0)
                {
                    shields.RemoveAtSwapBack(i--);
                }
                else
                {
                    shields[i] = shield;
                }

                MeleeTelemetryWriter.Write(ref telemetry, MeleeTelemetryEventType.DamageShieldTriggered, defender, effectTarget, weaponSlot, sequenceId, value0: shield.RemainingActivations, value1: shield.InternalCooldownSeconds);
            }
        }

        private static Entity ResolveShieldTarget(MeleeProcTargetMode mode, in Entity defender, in Entity attacker)
        {
            return mode switch
            {
                MeleeProcTargetMode.Self => defender,
                MeleeProcTargetMode.Target => attacker != Entity.Null ? attacker : defender,
                MeleeProcTargetMode.ArcSet => attacker != Entity.Null ? attacker : defender,
                MeleeProcTargetMode.Group => attacker != Entity.Null ? attacker : defender,
                _ => attacker != Entity.Null ? attacker : defender
            };
        }
    }
}
