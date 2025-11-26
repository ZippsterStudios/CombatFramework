using System.Collections.Generic;
using Framework.Damage.Components;
using Unity.Collections;
using Unity.Mathematics;

#pragma warning disable 0618 // uses SpellEffect for legacy definitions

namespace Framework.Spells.Content
{
    internal static partial class LegacyEffectTranslator
    {
        public static EffectBlock[] Convert(in SpellDefinition def)
        {
            var list = new List<EffectBlock>(def.Effects.Length);
            for (int i = 0; i < def.Effects.Length; i++)
                Append(list, def.Effects[i], in def);
            return list.ToArray();
        }

        static void Append(List<EffectBlock> list, in SpellEffect effect, in SpellDefinition def)
        {
            switch (effect.Kind)
            {
                case SpellEffectKind.DirectDamage:
                    list.Add(Damage(effect, def.School));
                    break;
                case SpellEffectKind.DirectHeal:
                case SpellEffectKind.Heal:
                    list.Add(Heal(effect, TargetScope.Single(TargetScopeKind.PrimaryTarget)));
                    break;
                case SpellEffectKind.DOT:
                    list.Add(OverTime(effect, false, null));
                    break;
                case SpellEffectKind.HOT:
                    list.Add(OverTime(effect, true, null));
                    break;
                case SpellEffectKind.SelfHOT:
                    list.Add(OverTime(effect, true, TargetScope.Single(TargetScopeKind.Caster)));
                    break;
                case SpellEffectKind.Buff:
                    list.Add(Aura(effect, true));
                    break;
                case SpellEffectKind.Debuff:
                    list.Add(Aura(effect, false));
                    break;
                case SpellEffectKind.AreaEffect:
                    list.Add(Area(effect));
                    break;
                case SpellEffectKind.SummonPet:
                    list.Add(Summon(effect));
                    break;
                case SpellEffectKind.LifeTransfer:
                    var damage = Damage(effect, def.School);
                    list.Add(damage);
                    var heal = Heal(effect, TargetScope.Single(TargetScopeKind.Caster));
                    heal.Scaling = new EffectScaling
                    {
                        UsePreviousResult = 1,
                        PreviousBlockOffset = -1,
                        ResultSource = EffectResultSource.Damage,
                        ResultCoefficient = 1f
                    };
                    list.Add(heal);
                    break;
                default:
                    list.Add(Script(effect));
                    break;
            }
        }

        static EffectBlock Damage(in SpellEffect effect, SpellSchool school)
        {
            return new EffectBlock
            {
                Scope = BuildScope(effect, false),
                Payload = new EffectPayload
                {
                    Kind = EffectPayloadKind.Damage,
                    Damage = new DamagePayload
                    {
                        Amount = math.max(0, effect.Magnitude),
                        CanCrit = 1,
                        School = MapSchool(school)
                    }
                }
            };
        }

        static EffectBlock Heal(in SpellEffect effect, TargetScope scope)
        {
            return new EffectBlock
            {
                Scope = scope,
                Payload = new EffectPayload
                {
                    Kind = EffectPayloadKind.Heal,
                    Heal = new HealPayload
                    {
                        Amount = math.max(0, effect.Magnitude),
                        CanCrit = 1
                    }
                }
            };
        }

        static EffectBlock OverTime(in SpellEffect effect, bool isHot, TargetScope? overrideScope)
        {
            return new EffectBlock
            {
                Scope = overrideScope ?? BuildScope(effect, isHot && effect.Kind == SpellEffectKind.SelfHOT),
                Payload = new EffectPayload
                {
                    Kind = isHot ? EffectPayloadKind.SpawnHot : EffectPayloadKind.SpawnDot,
                    OverTime = new DotHotPayload
                    {
                        Id = effect.RefId,
                        UseCatalogDefaults = (byte)(effect.RefId.Length > 0 ? 1 : 0),
                        MagnitudeOverride = effect.Magnitude,
                        TickIntervalOverride = 1f,
                        DurationOverride = 8f
                    }
                }
            };
        }

        static EffectBlock Aura(in SpellEffect effect, bool isBuff)
        {
            return new EffectBlock
            {
                Scope = BuildScope(effect, false),
                Payload = new EffectPayload
                {
                    Kind = isBuff ? EffectPayloadKind.ApplyBuff : EffectPayloadKind.ApplyDebuff,
                    Apply = new ApplyEffectPayload
                    {
                        Id = effect.RefId,
                        DurationMs = effect.Magnitude > 0 ? effect.Magnitude * 1000 : 5000,
                        RefreshDuration = 1
                    }
                }
            };
        }

        static EffectBlock Area(in SpellEffect effect)
        {
            return new EffectBlock
            {
                Scope = TargetScope.Single(TargetScopeKind.Caster),
                Payload = new EffectPayload
                {
                    Kind = EffectPayloadKind.SpawnAreaEffect,
                    Area = new AreaEffectPayload
                    {
                        AreaId = effect.RefId,
                        Radius = effect.Radius > 0f ? effect.Radius : 5f,
                        Duration = effect.Magnitude > 0 ? effect.Magnitude : 8f
                    }
                }
            };
        }

        static EffectBlock Summon(in SpellEffect effect)
        {
            return new EffectBlock
            {
                Scope = TargetScope.Single(TargetScopeKind.Caster),
                Payload = new EffectPayload
                {
                    Kind = EffectPayloadKind.SummonPet,
                    Summon = new SummonPayload
                    {
                        PetId = effect.RefId,
                        Count = math.max(1, effect.Magnitude),
                        SpawnRadius = effect.Radius
                    }
                }
            };
        }

        static EffectBlock Script(in SpellEffect effect)
        {
            return new EffectBlock
            {
                Scope = TargetScope.Single(TargetScopeKind.PrimaryTarget),
                Payload = new EffectPayload
                {
                    Kind = EffectPayloadKind.ScriptReference,
                    Script = new ScriptPayload
                    {
                        FeatureId = new FixedString64Bytes("Legacy." + effect.Kind.ToString()),
                        Arguments = effect.RefId
                    }
                }
            };
        }
    }
}

#pragma warning restore 0618
