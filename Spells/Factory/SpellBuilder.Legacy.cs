using System;
using Framework.Spells.Content;
using Unity.Collections;
using Unity.Mathematics;

namespace Framework.Spells.Factory
{
    public sealed partial class SpellBuilder
    {
        [Obsolete("Use AddEffect with TargetScope Radius instead.")]
        public SpellBuilder UseAOE(float radius, FixedString64Bytes filter)
        {
            QueueRadius(radius, ParseFilter(filter), false);
            return this;
        }

        [Obsolete("Use AddEffect with explicit TargetScope.")]
        public SpellBuilder UseAOEForAll(float radius, FixedString64Bytes filter)
        {
            QueueRadius(radius, ParseFilter(filter), true);
            return this;
        }

        [Obsolete("Use TargetScope.ChainJump settings when calling AddEffect.")]
        public SpellBuilder UseChain(int count) { QueueChain(count); return this; }

        [Obsolete("Provide TargetScopeShape.Cone directly when building the scope.")]
        public SpellBuilder UseCone(float angleDegrees) { QueueCone(angleDegrees); return this; }

        [Obsolete("Use SetSchool instead.")]
        public SpellBuilder School(SpellSchool school) => SetSchool(school);
        [Obsolete("Use SetCastTime instead.")]
        public SpellBuilder CastTime(float seconds) => SetCastTime(seconds);
        [Obsolete("Use SetCooldown instead.")]
        public SpellBuilder Cooldown(float seconds) => SetCooldown(seconds);
        [Obsolete("Use SetRange instead.")]
        public SpellBuilder Range(float r) => SetRange(r);
        [Obsolete("Use SetTargeting instead.")]
        public SpellBuilder Targeting(SpellTargeting t) => SetTargeting(t);

        [Obsolete("Use AddEffect with EffectPayloadKind.SpawnDot.")]
        public SpellBuilder UseDOT(FixedString64Bytes dotId)
        {
            return AddEffect(TargetScopeKind.PrimaryTarget, new EffectPayload
            {
                Kind = EffectPayloadKind.SpawnDot,
                OverTime = new DotHotPayload { Id = dotId, UseCatalogDefaults = 1 }
            });
        }

        [Obsolete("Use AddEffect with EffectPayloadKind.SpawnHot.")]
        public SpellBuilder UseHOT(FixedString64Bytes hotId)
        {
            return AddEffect(TargetScopeKind.PrimaryTarget, new EffectPayload
            {
                Kind = EffectPayloadKind.SpawnHot,
                OverTime = new DotHotPayload { Id = hotId, UseCatalogDefaults = 1 }
            });
        }

        [Obsolete("Use AddEffect with ApplyBuff payload.")]
        public SpellBuilder UseBuff(FixedString64Bytes buffId)
        {
            return AddEffect(TargetScopeKind.PrimaryTarget, new EffectPayload
            {
                Kind = EffectPayloadKind.ApplyBuff,
                Apply = new ApplyEffectPayload { Id = buffId, DurationMs = 5000, RefreshDuration = 1 }
            });
        }

        [Obsolete("Use AddEffect with ApplyDebuff payload.")]
        public SpellBuilder UseDebuff(FixedString64Bytes debuffId, int fallbackDurationSeconds = 0)
        {
            return AddEffect(TargetScopeKind.PrimaryTarget, new EffectPayload
            {
                Kind = EffectPayloadKind.ApplyDebuff,
                Apply = new ApplyEffectPayload
                {
                    Id = debuffId,
                    DurationMs = math.max(0, fallbackDurationSeconds) * 1000,
                    RefreshDuration = 1
                }
            });
        }

        [Obsolete("Use AddEffect with SpawnAreaEffect payload.")]
        public SpellBuilder UseArea(FixedString64Bytes areaId)
        {
            ClearPendingScope();
            return AddEffect(TargetScopeKind.Caster, new EffectPayload
            {
                Kind = EffectPayloadKind.SpawnAreaEffect,
                Area = new AreaEffectPayload { AreaId = areaId, Radius = 5f, Duration = 8f }
            });
        }

        [Obsolete("Use AddEffect with Damage payload.")]
        public SpellBuilder UseDirectDamage(int amount)
        {
            return AddEffect(TargetScopeKind.PrimaryTarget, new EffectPayload
            {
                Kind = EffectPayloadKind.Damage,
                Damage = new DamagePayload { Amount = math.max(0, amount), CanCrit = 1, School = ResolveDamageSchool() }
            });
        }

        [Obsolete("Use AddEffect with Heal payload.")]
        public SpellBuilder UseDirectHeal(int amount) => Heal(amount);

        [Obsolete("Use AddEffect with separate damage/heal blocks.")]
        public SpellBuilder UseLifeTransfer(int amount)
        {
            UseDirectDamage(amount);
            var scaling = new EffectScaling
            {
                UsePreviousResult = 1,
                PreviousBlockOffset = -1,
                ResultSource = EffectResultSource.Damage,
                ResultCoefficient = 1f
            };
            return AddEffect(TargetScopeKind.Caster, new EffectPayload
            {
                Kind = EffectPayloadKind.Heal,
                Heal = new HealPayload { Amount = math.max(0, amount), CanCrit = 0 }
            }, scaling: scaling);
        }

        [Obsolete("Use AddEffect with Summon payload.")]
        public SpellBuilder UseSummonPet(FixedString64Bytes petId, int count = 1, float spawnRadius = 0f)
        {
            return AddEffect(TargetScopeKind.Caster, new EffectPayload
            {
                Kind = EffectPayloadKind.SummonPet,
                Summon = new SummonPayload { PetId = petId, Count = math.max(1, count), SpawnRadius = math.max(0f, spawnRadius) }
            });
        }

        [Obsolete("Use AddEffect with SpawnHot payload targeting the caster.")]
        public SpellBuilder UseSelfHOT(FixedString64Bytes hotId, int fallbackMagnitude = 0)
        {
            return AddEffect(TargetScopeKind.Caster, new EffectPayload
            {
                Kind = EffectPayloadKind.SpawnHot,
                OverTime = new DotHotPayload
                {
                    Id = hotId,
                    UseCatalogDefaults = (byte)(hotId.Length > 0 ? 1 : 0),
                    MagnitudeOverride = fallbackMagnitude
                }
            });
        }

        [Obsolete("Use AddEffect with Damage payload.")]
        public SpellBuilder DealDamage(int amount) => UseDirectDamage(amount);

        [Obsolete("Use AddEffect with Heal payload.")]
        public SpellBuilder Heal(int amount)
        {
            return AddEffect(TargetScopeKind.PrimaryTarget, new EffectPayload
            {
                Kind = EffectPayloadKind.Heal,
                Heal = new HealPayload { Amount = math.max(0, amount), CanCrit = 1 }
            });
        }
    }
}
