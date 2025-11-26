using Framework.Buffs.Components;
using Framework.Buffs.Content;
using Framework.TimedEffect.Components;
using Framework.TimedEffect.Content;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Buffs.Resolution
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct BuffResolutionSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            double now = SystemAPI.Time.ElapsedTime;
            var query = SystemAPI.QueryBuilder()
                                 .WithAllRW<TimedEffectInstance, BuffInstance>()
                                 .Build();
            using var entities = query.ToEntityArray(Allocator.Temp);

            for (int eIndex = 0; eIndex < entities.Length; eIndex++)
            {
                var entity = entities[eIndex];
                var timed = em.GetBuffer<TimedEffectInstance>(entity);
                var buffData = em.GetBuffer<BuffInstance>(entity);

                var customTotals = new NativeList<BuffCustomStatAggregate>(Allocator.Temp);
                var shieldAccumulator = new NativeList<DamageShieldStateElement>(Allocator.Temp);
                var wardAccumulator = new NativeList<MeleeWardStateElement>(Allocator.Temp);
                var activeIds = new NativeHashSet<FixedString64Bytes>(timed.Length, Allocator.Temp);
                try
                {
                    var snapshot = new BuffStatSnapshot
                    {
                        DamageMultiplier = 1f,
                        DefenseMultiplier = 1f
                    };

                    bool hasBuff = false;

                    for (int i = 0; i < timed.Length; i++)
                    {
                        var inst = timed[i];
                        if (inst.Type != TimedEffectType.Buff)
                            continue;

                        int dataIndex = IndexOf(ref buffData, inst.EffectId);
                        if (dataIndex < 0)
                            continue;

                        hasBuff = true;
                        activeIds.Add(inst.EffectId);

                        int stacks = math.max(1, inst.StackCount);
                        AccumulateStatEffects(ref snapshot, ref customTotals, buffData[dataIndex].StatEffects, stacks);
                        AccumulateShieldSpecs(ref shieldAccumulator, buffData[dataIndex].DamageShields, stacks, inst, now, buffData[dataIndex].BuffId);
                        AccumulateWardSpecs(ref wardAccumulator, buffData[dataIndex].MeleeWards, stacks, inst, now, buffData[dataIndex].BuffId);
                    }

                    if (!hasBuff)
                    {
                        if (em.HasComponent<BuffStatSnapshot>(entity))
                            em.RemoveComponent<BuffStatSnapshot>(entity);
                        if (em.HasBuffer<BuffCustomStatAggregate>(entity))
                            em.RemoveComponent<BuffCustomStatAggregate>(entity);
                        if (em.HasBuffer<DamageShieldStateElement>(entity))
                            em.RemoveComponent<DamageShieldStateElement>(entity);
                        if (em.HasBuffer<MeleeWardStateElement>(entity))
                            em.RemoveComponent<MeleeWardStateElement>(entity);
                        buffData.Clear();
                        continue;
                    }

                    snapshot.DamageMultiplier = math.max(0.0001f, snapshot.DamageMultiplier);
                    snapshot.DefenseMultiplier = math.max(0.0001f, snapshot.DefenseMultiplier);
                    snapshot.WardCurrent = snapshot.WardMax;

                    if (!em.HasComponent<BuffStatSnapshot>(entity))
                        em.AddComponentData(entity, snapshot);
                    else
                        em.SetComponentData(entity, snapshot);

                    if (customTotals.Length > 0)
                    {
                        var customBuffer = em.HasBuffer<BuffCustomStatAggregate>(entity)
                            ? em.GetBuffer<BuffCustomStatAggregate>(entity)
                            : em.AddBuffer<BuffCustomStatAggregate>(entity);
                        customBuffer.Clear();
                        for (int i = 0; i < customTotals.Length; i++)
                            customBuffer.Add(customTotals[i]);
                    }
                    else if (em.HasBuffer<BuffCustomStatAggregate>(entity))
                    {
                        em.RemoveComponent<BuffCustomStatAggregate>(entity);
                    }

                    for (int i = buffData.Length - 1; i >= 0; i--)
                    {
                        if (!activeIds.Contains(buffData[i].BuffId))
                            buffData.RemoveAt(i);
                    }

                    ApplyDamageShields(ref em, entity, shieldAccumulator, now);
                    ApplyMeleeWards(ref em, entity, wardAccumulator, now);
                }
                finally
                {
                    activeIds.Dispose();
                    customTotals.Dispose();
                    shieldAccumulator.Dispose();
                    wardAccumulator.Dispose();
                }
            }
        }

        private static void AccumulateStatEffects(ref BuffStatSnapshot snapshot, ref NativeList<BuffCustomStatAggregate> customTotals, in FixedList128Bytes<BuffStatEffect> effects, int stacks)
        {
            for (int i = 0; i < effects.Length; i++)
            {
                var eff = effects[i];
                switch (eff.Kind)
                {
                    case BuffStatEffectKind.HealthFlat:
                        snapshot.BonusHealthFlat += (int)math.round(eff.AdditivePerStack * stacks);
                        break;
                    case BuffStatEffectKind.HealthPercent:
                        snapshot.BonusHealthPercent += eff.AdditivePerStack * stacks;
                        break;
                    case BuffStatEffectKind.ManaFlat:
                        snapshot.BonusManaFlat += (int)math.round(eff.AdditivePerStack * stacks);
                        break;
                    case BuffStatEffectKind.ManaPercent:
                        snapshot.BonusManaPercent += eff.AdditivePerStack * stacks;
                        break;
                    case BuffStatEffectKind.StaminaFlat:
                        snapshot.BonusStaminaFlat += (int)math.round(eff.AdditivePerStack * stacks);
                        break;
                    case BuffStatEffectKind.StaminaPercent:
                        snapshot.BonusStaminaPercent += eff.AdditivePerStack * stacks;
                        break;
                    case BuffStatEffectKind.DamageMultiplier:
                        snapshot.DamageMultiplier *= math.pow(math.max(eff.MultiplierPerStack, 0f), stacks);
                        break;
                    case BuffStatEffectKind.DefenseMultiplier:
                        snapshot.DefenseMultiplier *= math.pow(math.max(eff.MultiplierPerStack, 0f), stacks);
                        break;
                    case BuffStatEffectKind.HastePercent:
                        snapshot.HastePercent += eff.AdditivePerStack * stacks;
                        break;
                    case BuffStatEffectKind.Ward:
                        int ward = (int)math.round(eff.AdditivePerStack * stacks);
                        snapshot.WardMax += ward;
                        snapshot.WardCurrent += ward;
                        break;
                    case BuffStatEffectKind.DamageReflectFlat:
                        snapshot.DamageReflectFlat += (int)math.round(eff.AdditivePerStack * stacks);
                        break;
                    case BuffStatEffectKind.DamageReflectPercent:
                        snapshot.DamageReflectPercent += eff.AdditivePerStack * stacks;
                        break;
                    case BuffStatEffectKind.CustomAdditive:
                        AccumulateCustom(ref customTotals, eff.StatId, eff.AdditivePerStack * stacks, 1f);
                        break;
                    case BuffStatEffectKind.CustomMultiplier:
                        float mul = math.pow(math.max(eff.MultiplierPerStack, 0f), stacks);
                        AccumulateCustom(ref customTotals, eff.StatId, 0f, mul);
                        break;
                }
            }
        }

        private static void AccumulateCustom(ref NativeList<BuffCustomStatAggregate> list, FixedString32Bytes statId, float additive, float multiplier)
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].StatId.Equals(statId))
                {
                    var entry = list[i];
                    entry.Additive += additive;
                    entry.Multiplier = entry.Multiplier == 0f ? (multiplier <= 0f ? 1f : multiplier) : entry.Multiplier * (multiplier <= 0f ? 1f : multiplier);
                    list[i] = entry;
                    return;
                }
            }

            list.Add(new BuffCustomStatAggregate
            {
                StatId = statId,
                Additive = additive,
                Multiplier = multiplier <= 0f ? 1f : multiplier
            });
        }

        private static void AccumulateShieldSpecs(ref NativeList<DamageShieldStateElement> list, in FixedList64Bytes<DamageShieldSpec> specs, int stacks, in TimedEffectInstance inst, double now, in FixedString64Bytes buffId)
        {
            for (int i = 0; i < specs.Length; i++)
            {
                var spec = specs[i];
                var remaining = spec.MaxActivations > 0 ? math.min(255, spec.MaxActivations * stacks) : 0;
                double specExpiry = spec.DurationSeconds > 0f ? now + spec.DurationSeconds : 0;
                double buffExpiry = inst.Duration > 0f ? now + inst.TimeRemaining : 0;
                double expireTime = CombineExpiry(buffExpiry, specExpiry);

                var shield = new DamageShieldStateElement
                {
                    ShieldId = spec.ShieldId,
                    BuffId = buffId,
                    RemainingActivations = (byte)remaining,
                    MaxActivations = spec.MaxActivations,
                    ExpireTime = expireTime,
                    NextReadyTime = now,
                    InternalCooldownSeconds = spec.InternalCooldownSeconds,
                    PayloadKind = spec.PayloadKind,
                    TargetMode = spec.TargetMode,
                    TriggerOnZeroDamage = spec.TriggerOnZeroDamage,
                    TriggerOnBlock = spec.TriggerOnBlock,
                    TriggerOnParry = spec.TriggerOnParry,
                    PayloadRef = spec.PayloadRef,
                    ArgInt0 = spec.ArgInt0 * stacks,
                    ArgInt1 = spec.ArgInt1 * stacks,
                    ArgFloat0 = spec.ArgFloat0 * stacks,
                    ArgFloat1 = spec.ArgFloat1 * stacks,
                    IntervalSeconds = spec.IntervalSeconds
                };

                MergeShield(ref list, shield);
            }
        }

        private static void AccumulateWardSpecs(ref NativeList<MeleeWardStateElement> list, in FixedList64Bytes<MeleeWardSpec> specs, int stacks, in TimedEffectInstance inst, double now, in FixedString64Bytes buffId)
        {
            for (int i = 0; i < specs.Length; i++)
            {
                var spec = specs[i];
                var remaining = spec.MaxActivations > 0 ? math.min(255, spec.MaxActivations * stacks) : 0;
                double specExpiry = spec.DurationSeconds > 0f ? now + spec.DurationSeconds : 0;
                double buffExpiry = inst.Duration > 0f ? now + inst.TimeRemaining : 0;

                var ward = new MeleeWardStateElement
                {
                    WardId = spec.WardId,
                    BuffId = buffId,
                    RemainingActivations = (byte)remaining,
                    MaxActivations = spec.MaxActivations,
                    ExpireTime = CombineExpiry(buffExpiry, specExpiry),
                    AbsorbFlat = spec.AbsorbFlat * stacks,
                    AbsorbPercent = spec.AbsorbPercent * stacks,
                    RemainingPool = spec.TotalPool > 0 ? spec.TotalPool * stacks : 0,
                    TriggerOnZeroDamage = spec.TriggerOnZeroDamage
                };

                MergeWard(ref list, ward);
            }
        }

        private static void MergeShield(ref NativeList<DamageShieldStateElement> list, in DamageShieldStateElement shield)
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (!list[i].ShieldId.Equals(shield.ShieldId))
                    continue;

                var existing = list[i];

                if (shield.RemainingActivations > 0)
                {
                    if (existing.RemainingActivations == 0)
                        existing.RemainingActivations = shield.RemainingActivations;
                    else
                        existing.RemainingActivations = (byte)math.max((int)existing.RemainingActivations, (int)shield.RemainingActivations);
                }

                existing.MaxActivations = (byte)math.max((int)existing.MaxActivations, (int)shield.MaxActivations);
                existing.ArgInt0 += shield.ArgInt0;
                existing.ArgInt1 += shield.ArgInt1;
                existing.ArgFloat0 += shield.ArgFloat0;
                existing.ArgFloat1 += shield.ArgFloat1;
                existing.IntervalSeconds = math.max(existing.IntervalSeconds, shield.IntervalSeconds);
                existing.InternalCooldownSeconds = math.max(existing.InternalCooldownSeconds, shield.InternalCooldownSeconds);
                existing.ExpireTime = CombineExpiry(existing.ExpireTime, shield.ExpireTime);

                list[i] = existing;
                return;
            }

            list.Add(shield);
        }

        private static void MergeWard(ref NativeList<MeleeWardStateElement> list, in MeleeWardStateElement ward)
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (!list[i].WardId.Equals(ward.WardId))
                    continue;

                var existing = list[i];

                if (ward.RemainingActivations > 0)
                {
                    if (existing.RemainingActivations == 0)
                        existing.RemainingActivations = ward.RemainingActivations;
                    else
                        existing.RemainingActivations = (byte)math.max((int)existing.RemainingActivations, (int)ward.RemainingActivations);
                }

                existing.MaxActivations = (byte)math.max((int)existing.MaxActivations, (int)ward.MaxActivations);
                existing.AbsorbFlat += ward.AbsorbFlat;
                existing.AbsorbPercent = math.max(existing.AbsorbPercent, ward.AbsorbPercent);
                existing.RemainingPool += ward.RemainingPool;
                existing.ExpireTime = CombineExpiry(existing.ExpireTime, ward.ExpireTime);

                list[i] = existing;
                return;
            }

            list.Add(ward);
        }

        private static void ApplyDamageShields(ref EntityManager em, Entity entity, in NativeList<DamageShieldStateElement> desired, double now)
        {
            if (desired.Length == 0)
            {
                if (em.HasBuffer<DamageShieldStateElement>(entity))
                    em.RemoveComponent<DamageShieldStateElement>(entity);
                return;
            }

            var buffer = em.HasBuffer<DamageShieldStateElement>(entity)
                ? em.GetBuffer<DamageShieldStateElement>(entity)
                : em.AddBuffer<DamageShieldStateElement>(entity);

            var activeIds = new NativeHashSet<FixedString32Bytes>(desired.Length, Allocator.Temp);
            for (int i = 0; i < desired.Length; i++)
            {
                var target = desired[i];
                if (target.ExpireTime > 0 && now >= target.ExpireTime)
                    continue;

                activeIds.Add(target.ShieldId);
                int index = FindShieldIndex(buffer, target.ShieldId);
                if (index >= 0)
                {
                    var existing = buffer[index];
                    if (target.RemainingActivations > 0)
                    {
                        if (existing.RemainingActivations == 0)
                            existing.RemainingActivations = target.RemainingActivations;
                        else
                            existing.RemainingActivations = (byte)math.max((int)existing.RemainingActivations, (int)target.RemainingActivations);
                }

                if (target.ExpireTime > 0)
                    existing.ExpireTime = target.ExpireTime;
                existing.InternalCooldownSeconds = target.InternalCooldownSeconds;
                existing.PayloadKind = target.PayloadKind;
                existing.TargetMode = target.TargetMode;
                existing.TriggerOnZeroDamage = target.TriggerOnZeroDamage;
                existing.TriggerOnBlock = target.TriggerOnBlock;
                existing.TriggerOnParry = target.TriggerOnParry;
                existing.PayloadRef = target.PayloadRef;
                existing.ArgInt0 = target.ArgInt0;
                existing.ArgInt1 = target.ArgInt1;
                existing.ArgFloat0 = target.ArgFloat0;
                existing.ArgFloat1 = target.ArgFloat1;
                existing.IntervalSeconds = target.IntervalSeconds;
                existing.BuffId = target.BuffId;
                existing.MaxActivations = target.MaxActivations;
                buffer[index] = existing;
            }
            else
            {
                buffer.Add(target);
                }
            }

            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                if (!activeIds.Contains(buffer[i].ShieldId))
                    buffer.RemoveAtSwapBack(i);
            }

            activeIds.Dispose();
        }

        private static void ApplyMeleeWards(ref EntityManager em, Entity entity, in NativeList<MeleeWardStateElement> desired, double now)
        {
            if (desired.Length == 0)
            {
                if (em.HasBuffer<MeleeWardStateElement>(entity))
                    em.RemoveComponent<MeleeWardStateElement>(entity);
                return;
            }

            var buffer = em.HasBuffer<MeleeWardStateElement>(entity)
                ? em.GetBuffer<MeleeWardStateElement>(entity)
                : em.AddBuffer<MeleeWardStateElement>(entity);

            var activeIds = new NativeHashSet<FixedString32Bytes>(desired.Length, Allocator.Temp);
            for (int i = 0; i < desired.Length; i++)
            {
                var target = desired[i];
                if (target.ExpireTime > 0 && now >= target.ExpireTime)
                    continue;

                activeIds.Add(target.WardId);
                int index = FindWardIndex(buffer, target.WardId);
                if (index >= 0)
                {
                    var existing = buffer[index];
                    if (target.RemainingActivations > 0)
                    {
                        if (existing.RemainingActivations == 0)
                            existing.RemainingActivations = target.RemainingActivations;
                        else
                            existing.RemainingActivations = (byte)math.max((int)existing.RemainingActivations, (int)target.RemainingActivations);
                }

                if (target.ExpireTime > 0)
                    existing.ExpireTime = target.ExpireTime;
                existing.AbsorbFlat = math.max(existing.AbsorbFlat, target.AbsorbFlat);
                existing.AbsorbPercent = math.max(existing.AbsorbPercent, target.AbsorbPercent);
                existing.RemainingPool = existing.RemainingPool > target.RemainingPool ? existing.RemainingPool : target.RemainingPool;
                existing.TriggerOnZeroDamage = target.TriggerOnZeroDamage;
                existing.BuffId = target.BuffId;
                existing.MaxActivations = target.MaxActivations;
                buffer[index] = existing;
            }
            else
            {
                buffer.Add(target);
                }
            }

            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                if (!activeIds.Contains(buffer[i].WardId))
                    buffer.RemoveAtSwapBack(i);
            }

            activeIds.Dispose();
        }

        private static int FindShieldIndex(DynamicBuffer<DamageShieldStateElement> buffer, in FixedString32Bytes shieldId)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].ShieldId.Equals(shieldId))
                    return i;
            }
            return -1;
        }

        private static int FindWardIndex(DynamicBuffer<MeleeWardStateElement> buffer, in FixedString32Bytes wardId)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].WardId.Equals(wardId))
                    return i;
            }
            return -1;
        }

        private static double CombineExpiry(double a, double b)
        {
            if (a <= 0) return b;
            if (b <= 0) return a;
            return math.min(a, b);
        }

        private static int IndexOf(ref DynamicBuffer<BuffInstance> buffer, in FixedString64Bytes id)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].BuffId.Equals(id))
                    return i;
            }
            return -1;
        }
    }
}

