using System;
using System.Collections.Generic;
using Framework.Buffs.Content;
using Framework.Debuffs.Content;
using Framework.DOT.Content;
using Framework.HOT.Content;
using Framework.Damage.Components;
using Framework.Spells.Content;
using Framework.Spells.Factory;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Framework.Spells.Tests
{
    /// <summary>
    /// Registers a curated library of sample spell content intended for the test harness.
    /// Ensures DOT/HOT/Buff dependencies exist and logs registration details when verbose logging is requested.
    /// </summary>
    public static class SpellTestSampleContent
    {
        private static bool _registered;
        private static readonly FixedString64Bytes SmokeTestSpellId = (FixedString64Bytes)"fireball";

        private static readonly string[] _defaultSpellIds =
        {
            "fireball",
            "frost_nova",
            "chain_lightning",
            "searing_storm",
            "healing_rain",
            "guardian_blessing",
            "temporal_shift",
            "umbral_drain",
            "lifetouch",
            "siphoning_embrace"
        };

        public static IReadOnlyList<string> DefaultSpellIds => _defaultSpellIds;

        public static void EnsureRegistered(bool verboseLogs = false)
        {
            if (_registered)
            {
                if (SpellDefinitionCatalog.TryGet(SmokeTestSpellId, out _))
                    return;
                _registered = false;
            }

            try
            {
                RegisterDots(verboseLogs);
                RegisterHots(verboseLogs);
                RegisterBuffs(verboseLogs);
                RegisterDebuffs(verboseLogs);
                RegisterSpells(verboseLogs);
                _registered = true;
            }
            catch
            {
                _registered = false;
                throw;
            }
        }

        private static void RegisterDots(bool verbose)
        {
            RegisterDot(new DotDefinition
            {
                Id = (FixedString64Bytes)"burn_dot",
                TickInterval = 1f,
                Dps = 8,
                Duration = 6f
            }, verbose);

            RegisterDot(new DotDefinition
            {
                Id = (FixedString64Bytes)"poison_dot",
                TickInterval = 0.75f,
                Dps = 5,
                Duration = 9f
            }, verbose);

            RegisterDot(new DotDefinition
            {
                Id = (FixedString64Bytes)"shadow_dot",
                TickInterval = 1.5f,
                Dps = 14,
                Duration = 4.5f
            }, verbose);

            RegisterDot(new DotDefinition
            {
                Id = (FixedString64Bytes)"siphon_rot",
                TickInterval = 1f,
                Dps = 9,
                Duration = 7f
            }, verbose);
        }

        private static void RegisterHots(bool verbose)
        {
            RegisterHot(new HotDefinition
            {
                Id = (FixedString64Bytes)"regen_hot",
                TickInterval = 1f,
                Hps = 10,
                Duration = 8f
            }, verbose);

            RegisterHot(new HotDefinition
            {
                Id = (FixedString64Bytes)"mend_hot",
                TickInterval = 2f,
                Hps = 18,
                Duration = 6f
            }, verbose);

            RegisterHot(new HotDefinition
            {
                Id = (FixedString64Bytes)"siphon_renewal",
                TickInterval = 1f,
                Hps = 12,
                Duration = 6f
            }, verbose);
        }

        private static void RegisterBuffs(bool verbose)
        {
            var guardian = new BuffDefinition
            {
                Id = (FixedString64Bytes)"guardian_blessing",
                Duration = 12f,
                DurationPolicy = BuffDurationPolicy.RefreshOnApply,
                MaxStacks = 1,
                StackingMode = BuffStackingMode.Replace,
                StackableCount = 1,
                CategoryId = (FixedString32Bytes)"defense",
                CategoryLevel = 2
            };
            guardian.StatEffects.Add(new BuffStatEffect
            {
                Kind = BuffStatEffectKind.HealthFlat,
                AdditivePerStack = 150f
            });
            guardian.StatEffects.Add(new BuffStatEffect
            {
                Kind = BuffStatEffectKind.Ward,
                AdditivePerStack = 75f
            });
            guardian.StatEffects.Add(new BuffStatEffect
            {
                Kind = BuffStatEffectKind.DefenseMultiplier,
                MultiplierPerStack = 0.95f
            });
            guardian.StatEffects.Add(new BuffStatEffect
            {
                Kind = BuffStatEffectKind.DamageReflectPercent,
                AdditivePerStack = 0.25f
            });
            RegisterBuff(guardian, verbose);

            var temporal = new BuffDefinition
            {
                Id = (FixedString64Bytes)"temporal_shift",
                Duration = 10f,
                DurationPolicy = BuffDurationPolicy.RefreshOnApply,
                MaxStacks = 1,
                StackingMode = BuffStackingMode.Replace,
                StackableCount = 1,
                CategoryId = (FixedString32Bytes)"haste",
                CategoryLevel = 1
            };
            temporal.StatEffects.Add(new BuffStatEffect
            {
                Kind = BuffStatEffectKind.HastePercent,
                AdditivePerStack = 0.2f
            });
            temporal.StatEffects.Add(new BuffStatEffect
            {
                Kind = BuffStatEffectKind.ManaPercent,
                AdditivePerStack = 0.1f
            });
            temporal.StatEffects.Add(new BuffStatEffect
            {
                Kind = BuffStatEffectKind.DamageMultiplier,
                MultiplierPerStack = 1.05f
            });
            RegisterBuff(temporal, verbose);
        }

        private static void RegisterDebuffs(bool verbose)
        {
            RegisterDebuff(new DebuffDefinition
            {
                Id = (FixedString64Bytes)"frostbite",
                Flags = DebuffFlags.Slow | DebuffFlags.Root,
                Duration = 4f,
                StackableCount = 1,
                StackingMode = DebuffStackingMode.Replace,
                DurationPolicy = DebuffDurationPolicy.Fixed,
                MaxStacks = 1
            }, () =>
            {
                var list = new FixedList128Bytes<DebuffStatEffect>();
                list.Add(new DebuffStatEffect
                {
                    StatId = (FixedString32Bytes)"MoveSpeed",
                    AdditivePerStack = -6f,
                    MultiplierPerStack = 0.5f
                });
                return list;
            }, verbose);

            RegisterDebuff(new DebuffDefinition
            {
                Id = (FixedString64Bytes)"weakened",
                Flags = DebuffFlags.Weaken,
                Duration = 8f,
                StackingMode = DebuffStackingMode.CapStacks,
                DurationPolicy = DebuffDurationPolicy.RefreshOnApply,
                MaxStacks = 3
            }, () =>
            {
                var list = new FixedList128Bytes<DebuffStatEffect>();
                list.Add(new DebuffStatEffect
                {
                    StatId = (FixedString32Bytes)"AttackPower",
                    AdditivePerStack = -5f,
                    MultiplierPerStack = 1f
                });
                return list;
            }, verbose);
        }

        private static void RegisterSpells(bool verbose)
        {
            var singleTarget = TargetScope.Single(TargetScopeKind.PrimaryTarget);
            SpellBuilder
                .NewSpell((FixedString64Bytes)"fireball")
                .SetSchool(SpellSchool.Fire)
                .SetManaCost(12)
                .SetCastTime(1.2f)
                .SetCooldown(0.8f)
                .SetRange(35f)
                .SetTargeting(SpellTargeting.Enemy)
                .AddDamage(singleTarget, 60, DamageSchool.Fire, canCrit: 0, bypassArmor: true, bypassResist: false, bypassSnapshotModifiers: true)
                .AddDebuff(singleTarget, (FixedString64Bytes)"weakened", 8)
                .Register();

            var frostNovaScope = RadiusScope(10f, (FixedString64Bytes)"enemy", applyToAll: false);
            SpellBuilder
                .NewSpell((FixedString64Bytes)"frost_nova")
                .SetSchool(SpellSchool.Frost)
                .SetManaCost(28)
                .SetCastTime(0f)
                .SetCooldown(8f)
                .SetRange(0f)
                .SetTargeting(SpellTargeting.Enemy)
                .AddDebuff(frostNovaScope, (FixedString64Bytes)"frostbite", 5)
                .AddDamage(frostNovaScope, 24, DamageSchool.Frost)
                .Register();

            var chainScope = ChainScope(4, TargetTeamFilter.Enemy);
            SpellBuilder
                .NewSpell((FixedString64Bytes)"chain_lightning")
                .SetSchool(SpellSchool.Nature)
                .SetManaCost(22)
                .SetCastTime(1.6f)
                .SetCooldown(3.5f)
                .SetRange(40f)
                .SetTargeting(SpellTargeting.Enemy)
                .AddDamage(chainScope, 38, DamageSchool.Nature)
                .Register();

            var stormScope = RadiusScope(12f, (FixedString64Bytes)"enemy", applyToAll: true);
            SpellBuilder
                .NewSpell((FixedString64Bytes)"searing_storm")
                .SetSchool(SpellSchool.Fire)
                .SetManaCost(40)
                .SetCastTime(2.2f)
                .SetCooldown(12f)
                .SetRange(35f)
                .SetTargeting(SpellTargeting.Enemy)
                .AddDamage(stormScope, 28, DamageSchool.Fire)
                .AddDot(stormScope, (FixedString64Bytes)"burn_dot")
                .Register();

            var healingRainScope = RadiusScope(10f, (FixedString64Bytes)"ally", applyToAll: true);
            SpellBuilder
                .NewSpell((FixedString64Bytes)"healing_rain")
                .SetSchool(SpellSchool.Nature)
                .SetManaCost(32)
                .SetCastTime(2.5f)
                .SetCooldown(10f)
                .SetRange(30f)
                .SetTargeting(SpellTargeting.Ally | SpellTargeting.Ground)
                .AddHeal(healingRainScope, 45)
                .AddHot(healingRainScope, (FixedString64Bytes)"regen_hot")
                .Register();

            SpellBuilder
                .NewSpell((FixedString64Bytes)"guardian_blessing")
                .SetSchool(SpellSchool.Holy)
                .SetManaCost(18)
                .SetCastTime(0f)
                .SetCooldown(6f)
                .SetRange(25f)
                .SetTargeting(SpellTargeting.Ally | SpellTargeting.Self)
                .AddBuff(singleTarget, (FixedString64Bytes)"guardian_blessing", 5)
                .Register();

            var shiftScope = RadiusScope(6f, (FixedString64Bytes)"ally", applyToAll: true);
            SpellBuilder
                .NewSpell((FixedString64Bytes)"temporal_shift")
                .SetSchool(SpellSchool.Arcane)
                .SetManaCost(30)
                .SetCastTime(0f)
                .SetCooldown(20f)
                .SetRange(0f)
                .SetTargeting(SpellTargeting.Self | SpellTargeting.Ally)
                .AddBuff(shiftScope, (FixedString64Bytes)"temporal_shift", 8)
                .Register();

            SpellBuilder
                .NewSpell((FixedString64Bytes)"umbral_drain")
                .SetSchool(SpellSchool.Shadow)
                .SetManaCost(26)
                .SetCastTime(1.8f)
                .SetCooldown(4.5f)
                .SetRange(30f)
                .SetTargeting(SpellTargeting.Enemy)
                .AddDamage(singleTarget, 20, DamageSchool.Shadow)
                .AddDot(singleTarget, (FixedString64Bytes)"shadow_dot")
                .Register();

            var casterScope = TargetScope.Single(TargetScopeKind.Caster);
            SpellBuilder
                .NewSpell((FixedString64Bytes)"lifetouch")
                .SetSchool(SpellSchool.Shadow)
                .SetManaCost(24)
                .SetCastTime(1.4f)
                .SetCooldown(3f)
                .SetRange(30f)
                .SetTargeting(SpellTargeting.Enemy | SpellTargeting.Self)
                .AddDamage(singleTarget, 44, DamageSchool.Shadow)
                .AddEffect(casterScope, new EffectPayload
                {
                    Kind = EffectPayloadKind.Heal,
                    Heal = new HealPayload { Amount = 0, CanCrit = 0 }
                }, scaling: new EffectScaling
                {
                    UsePreviousResult = 1,
                    PreviousBlockOffset = -1,
                    ResultSource = EffectResultSource.Damage,
                    ResultCoefficient = 1f
                })
                .Register();

            SpellBuilder
                .NewSpell((FixedString64Bytes)"siphoning_embrace")
                .SetSchool(SpellSchool.Shadow)
                .SetManaCost(32)
                .SetCastTime(2f)
                .SetCooldown(10f)
                .SetRange(30f)
                .SetTargeting(SpellTargeting.Enemy | SpellTargeting.Self)
                .AddDot(singleTarget, (FixedString64Bytes)"siphon_rot")
                .AddHot(casterScope, (FixedString64Bytes)"siphon_renewal")
                .Register();

            if (verbose)
            {
                foreach (var id in _defaultSpellIds)
                {
                    var fid = (FixedString64Bytes)id;
                    if (SpellDefinitionCatalog.TryGet(fid, out var def))
                    {
                        Debug.Log($"[SpellSamples] Registered spell '{id}' with {def.Blocks?.Length ?? 0} effect block(s).");
                    }
                }
            }
        }

        private static void RegisterDebuff(in DebuffDefinition def, Func<FixedList128Bytes<DebuffStatEffect>> statsFactory, bool verbose)
        {
            var mutable = def;
            if (statsFactory != null)
                mutable.StatEffects = statsFactory();
            DebuffCatalog.Register(mutable);
            if (verbose)
            {
                Debug.Log($"[SpellSamples] Debuff '{def.Id}' duration {def.Duration:0.##}s flags {def.Flags}.");
            }
        }

        private static void RegisterDot(in DotDefinition def, bool verbose)
        {
            DotCatalog.Register(def);
            if (verbose)
            {
                Debug.Log($"[SpellSamples] DOT '{def.Id}' DPS {def.Dps} for {def.Duration:0.##}s every {def.TickInterval:0.##}s.");
            }
        }

        private static void RegisterHot(in HotDefinition def, bool verbose)
        {
            HotCatalog.Register(def);
            if (verbose)
            {
                Debug.Log($"[SpellSamples] HOT '{def.Id}' HPS {def.Hps} for {def.Duration:0.##}s every {def.TickInterval:0.##}s.");
            }
        }

        private static void RegisterBuff(in BuffDefinition def, bool verbose)
        {
            BuffCatalog.Register(def);
            if (verbose)
            {
                Debug.Log($"[SpellSamples] Buff '{def.Id}' duration {def.Duration:0.##}s mode {def.StackingMode}.");
            }
        }

        private static SpellBuilder AddDamage(this SpellBuilder builder, in TargetScope scope, int amount, DamageSchool school, byte canCrit = 1, bool bypassArmor = false, bool bypassResist = false, bool bypassSnapshotModifiers = false)
        {
            return builder.AddEffect(scope, new EffectPayload
            {
                Kind = EffectPayloadKind.Damage,
                Damage = new DamagePayload
                {
                    Amount = math.max(0, amount),
                    CanCrit = canCrit,
                    School = school,
                    IgnoreArmor = (byte)(bypassArmor ? 1 : 0),
                    IgnoreResist = (byte)(bypassResist ? 1 : 0),
                    IgnoreSnapshotModifiers = (byte)(bypassSnapshotModifiers ? 1 : 0)
                }
            });
        }

        private static SpellBuilder AddHeal(this SpellBuilder builder, in TargetScope scope, int amount, byte canCrit = 1)
        {
            return builder.AddEffect(scope, new EffectPayload
            {
                Kind = EffectPayloadKind.Heal,
                Heal = new HealPayload
                {
                    Amount = math.max(0, amount),
                    CanCrit = canCrit
                }
            });
        }

        private static SpellBuilder AddDot(this SpellBuilder builder, in TargetScope scope, FixedString64Bytes dotId, int magnitudeOverride = 0)
        {
            return builder.AddEffect(scope, new EffectPayload
            {
                Kind = EffectPayloadKind.SpawnDot,
                OverTime = new DotHotPayload
                {
                    Id = dotId,
                    UseCatalogDefaults = (byte)(dotId.Length > 0 ? 1 : 0),
                    MagnitudeOverride = magnitudeOverride
                }
            });
        }

        private static SpellBuilder AddHot(this SpellBuilder builder, in TargetScope scope, FixedString64Bytes hotId, int magnitudeOverride = 0)
        {
            return builder.AddEffect(scope, new EffectPayload
            {
                Kind = EffectPayloadKind.SpawnHot,
                OverTime = new DotHotPayload
                {
                    Id = hotId,
                    UseCatalogDefaults = (byte)(hotId.Length > 0 ? 1 : 0),
                    MagnitudeOverride = magnitudeOverride
                }
            });
        }

        private static SpellBuilder AddBuff(this SpellBuilder builder, in TargetScope scope, FixedString64Bytes buffId, int durationSeconds)
        {
            return builder.AddEffect(scope, new EffectPayload
            {
                Kind = EffectPayloadKind.ApplyBuff,
                Apply = new ApplyEffectPayload
                {
                    Id = buffId,
                    DurationMs = math.max(0, durationSeconds) * 1000,
                    RefreshDuration = 1
                }
            });
        }

        private static SpellBuilder AddDebuff(this SpellBuilder builder, in TargetScope scope, FixedString64Bytes debuffId, int durationSeconds)
        {
            return builder.AddEffect(scope, new EffectPayload
            {
                Kind = EffectPayloadKind.ApplyDebuff,
                Apply = new ApplyEffectPayload
                {
                    Id = debuffId,
                    DurationMs = math.max(0, durationSeconds) * 1000,
                    RefreshDuration = 1
                }
            });
        }

        private static TargetScope RadiusScope(float radius, FixedString64Bytes filter, bool applyToAll)
        {
            return new TargetScope
            {
                Kind = TargetScopeKind.Radius,
                Center = TargetScopeCenter.Caster,
                Shape = TargetScopeShape.Sphere,
                Radius = math.max(0f, radius),
                TeamFilter = ParseTeamFilter(filter),
                MaxTargets = applyToAll ? 0 : 1
            };
        }

        private static TargetScope ChainScope(int jumps, TargetTeamFilter filter)
        {
            float radius = 5f;
            return new TargetScope
            {
                Kind = TargetScopeKind.ChainJump,
                Radius = radius,
                TeamFilter = filter,
                Chain = new TargetScopeChain
                {
                    MaxJumps = math.max(0, jumps),
                    JumpRadius = radius,
                    TeamFilter = filter
                },
                MaxTargets = math.max(1, jumps + 1)
            };
        }

        private static TargetTeamFilter ParseTeamFilter(in FixedString64Bytes filter)
        {
            if (filter.Length == 0) return TargetTeamFilter.Any;
            var lower = filter.ToString().ToLowerInvariant();
            return lower switch
            {
                "ally" or "allies" or "friendly" => TargetTeamFilter.Ally,
                "enemy" or "enemies" => TargetTeamFilter.Enemy,
                _ => TargetTeamFilter.Any
            };
        }
    }
}
