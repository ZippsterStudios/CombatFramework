#if UNITY_EDITOR
using System.Text;
using Framework.Damage.Components;
using Framework.Debuffs.Components;
using Framework.Resources.Components;
using Framework.Pets.Components;
using Framework.Pets.Content;
using Framework.Pets.Drivers;
using Framework.Pets.Factory;
using Framework.Spells.Content;
using Framework.Spells.Tests;
using Framework.TimedEffect.Components;
using Framework.TimedEffect.Content;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using Unity.Mathematics;

namespace Framework.UnityAuthoring.Editor
{
    public sealed partial class FrameworkCombatTestWindow
    {
        private static readonly string[] PetSampleLabels =
        {
            "Wolf Guardian",
            "Imp Swarm",
            "Drone Swarm"
        };

        private static readonly FixedString64Bytes[] PetSamplePetIds =
        {
            (FixedString64Bytes)"wolf",
            (FixedString64Bytes)"imp",
            (FixedString64Bytes)"drone_swarm"
        };

        private static readonly FixedString64Bytes[] PetSampleSpellIds =
        {
            (FixedString64Bytes)"summon_wolf",
            (FixedString64Bytes)"summon_imps",
            (FixedString64Bytes)"deploy_drones"
        };

        private static readonly FixedString32Bytes DefaultPetCategory = (FixedString32Bytes)"summons";

        private void DrawTargetTab(EntityManager em, bool hasTarget)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Target Overview", EditorStyles.boldLabel);
                if (!hasTarget)
                {
                    EditorGUILayout.HelpBox("Select a target from the list to inspect.", MessageType.Info);
                    return;
                }

                var e = _targetEntity;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Entity", FormatEntity(e));
                }

                if (em.HasComponent<Health>(e))
                {
                    var h = em.GetComponentData<Health>(e);
                    EditorGUILayout.LabelField($"Health", $"{h.Current} / {h.Max}  (Regen {h.RegenPerSecond}/s)");
                }

                if (em.HasComponent<Mana>(e))
                {
                    var m = em.GetComponentData<Mana>(e);
                    EditorGUILayout.LabelField($"Mana", $"{m.Current} / {m.Max}  (Regen {m.RegenPerSecond}/s)");
                }

                if (em.HasComponent<Stamina>(e))
                {
                    var s = em.GetComponentData<Stamina>(e);
                    EditorGUILayout.LabelField($"Stamina", $"{s.Current} / {s.Max}  (Regen {s.RegenPerSecond}/s)");
                }

                if (em.HasComponent<Framework.Damage.Components.Damageable>(e))
                {
                    var dmg = em.GetComponentData<Framework.Damage.Components.Damageable>(e);
                    EditorGUILayout.LabelField("Damageable", $"Armor {dmg.Armor}  Resist {dmg.ResistPercent:P1}");
                }

                if (em.HasComponent<Framework.Core.Components.TeamId>(e))
                {
                    var team = em.GetComponentData<Framework.Core.Components.TeamId>(e);
                    EditorGUILayout.LabelField("Team", team.Value.ToString());
                }

                if (em.HasComponent<Framework.Core.Components.Position>(e))
                {
                    var pos = em.GetComponentData<Framework.Core.Components.Position>(e).Value;
                    EditorGUILayout.LabelField("Position", $"({pos.x:0.00}, {pos.y:0.00})");
                }

                if (em.HasComponent<TestId>(e))
                {
                    var id = em.GetComponentData<TestId>(e).Value;
                    EditorGUILayout.LabelField("Test Id", id.ToString());
                }

                if (em.HasBuffer<DebuffInstance>(e))
                {
                    em.CompleteDependencyBeforeRW<DebuffInstance>();
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Debuffs", EditorStyles.boldLabel);
                    var debuffs = em.GetBuffer<DebuffInstance>(e);
                    DynamicBuffer<TimedEffectInstance> timedEffects = default;
                    bool hasTimed = em.HasBuffer<TimedEffectInstance>(e);
                    if (hasTimed)
                    {
                        em.CompleteDependencyBeforeRW<TimedEffectInstance>();
                        timedEffects = em.GetBuffer<TimedEffectInstance>(e);
                    }
                    if (debuffs.Length == 0)
                    {
                        EditorGUILayout.LabelField("<none>");
                    }
                    else
                    {
                        for (int i = 0; i < debuffs.Length; i++)
                        {
                            var d = debuffs[i];
                            int stacks = 1;
                            float timeRemaining = 0f;
                            if (hasTimed && timedEffects.IsCreated)
                            {
                                for (int j = 0; j < timedEffects.Length; j++)
                                {
                                    var inst = timedEffects[j];
                                    if (inst.Type != TimedEffectType.Debuff)
                                        continue;
                                    if (!inst.EffectId.Equals(d.DebuffId))
                                        continue;

                                    stacks = math.max(1, inst.StackCount);
                                    timeRemaining = math.max(0f, inst.TimeRemaining);
                                    break;
                                }
                            }
                            EditorGUILayout.LabelField($"- {d.DebuffId.ToString()} stacks={stacks} time={timeRemaining:0.0}s flags={d.Flags}");
                        }
                    }
                }
            }
        }

        private void DrawActionsTab(EntityManager em, bool hasTarget, bool hasCaster)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Manual Actions", EditorStyles.boldLabel);
                _manualAmount = EditorGUILayout.FloatField("Magnitude", _manualAmount);
                _manualSchool = (DamageSchool)EditorGUILayout.EnumPopup("Damage School", _manualSchool);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!hasTarget))
                    {
                        if (GUILayout.Button("Apply Damage"))
                        {
                            ApplyMagnitude(em, _targetEntity, -(int)math.abs(_manualAmount), _manualSchool);
                        }
                        if (GUILayout.Button("Apply Heal"))
                        {
                            ApplyHeal(em, _targetEntity, (int)math.abs(_manualAmount));
                        }
                        if (GUILayout.Button("Reset Resources"))
                        {
                            ResetResources(em, _targetEntity);
                        }
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Caster Settings", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Caster Entity", hasCaster ? FormatEntity(_casterEntity) : (_casterIsTarget ? "<target>" : "<none>"));
                _casterIsTarget = EditorGUILayout.Toggle("Use Target As Caster", _casterIsTarget);
            }
        }

        private void DrawSpellsTab(EntityManager em, bool hasTarget, bool hasCaster)
        {
            SpellTestSampleContent.EnsureRegistered();
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Active Spell", EditorStyles.boldLabel);
                _includeSampleSpells = EditorGUILayout.Toggle("Use Sample Library", _includeSampleSpells);

                if (_includeSampleSpells && SpellTestSampleContent.DefaultSpellIds.Count > 0)
                {
                    var labels = SpellTestSampleContent.DefaultSpellIds;
                    string[] options = new string[labels.Count];
                    for (int i = 0; i < labels.Count; i++)
                        options[i] = labels[i];
                    _selectedSampleSpell = Mathf.Clamp(_selectedSampleSpell, 0, options.Length - 1);
                    _selectedSampleSpell = GUILayout.SelectionGrid(_selectedSampleSpell, options, 3);
                }

                _customSpellId = EditorGUILayout.TextField("Custom Spell Id", _customSpellId);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!hasTarget))
                    {
                        if (GUILayout.Button("Cast On Target"))
                        {
                            var caster = ResolveCaster(em, hasTarget, hasCaster);
                            CastSpell(em, caster, _targetEntity, GetActiveSpellId());
                        }
                    }
                    if (GUILayout.Button("Log Definition"))
                    {
                        var spellId = GetActiveSpellId();
                        if (SpellDefinitionCatalog.TryGet(spellId, out var def))
                        {
                            Debug.Log(DescribeSpell(def));
                        }
                        else
                        {
                            Debug.LogWarning($"[CombatTest] Spell '{spellId.ToString()}' not registered.");
                        }
                    }
                }
            }
        }

        private void DrawLibraryTab(EntityManager em)
        {
            SpellTestSampleContent.EnsureRegistered();
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Sample Spell Library", EditorStyles.boldLabel);
                _libraryScroll = EditorGUILayout.BeginScrollView(_libraryScroll);
                foreach (var id in SpellTestSampleContent.DefaultSpellIds)
                {
                    var fixedId = (FixedString64Bytes)id;
                    if (!SpellDefinitionCatalog.TryGet(fixedId, out var def))
                        continue;
                    EditorGUILayout.LabelField(id, EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(DescribeSpell(def), MessageType.None);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void ResetResources(EntityManager em, Entity entity)
        {
            if (!EntityExists(em, entity)) return;
            if (em.HasComponent<Health>(entity))
            {
                var h = em.GetComponentData<Health>(entity);
                h.Current = h.Max;
                em.SetComponentData(entity, h);
            }
            if (em.HasComponent<Mana>(entity))
            {
                var m = em.GetComponentData<Mana>(entity);
                m.Current = m.Max;
                em.SetComponentData(entity, m);
            }
            if (em.HasComponent<Stamina>(entity))
            {
                var s = em.GetComponentData<Stamina>(entity);
                s.Current = s.Max;
                em.SetComponentData(entity, s);
            }
        }

        private string DescribeSpell(in SpellDefinition def)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Id: {def.Id}");
            sb.AppendLine($"School: {def.School} Range: {def.Range:0.##}m CastTime: {def.CastTime:0.##}s Cooldown: {def.Cooldown:0.##}s Targeting: {def.Targeting}");
            if (def.Costs != null && def.Costs.Length > 0)
            {
                sb.Append("Costs: ");
                for (int i = 0; i < def.Costs.Length; i++)
                {
                    if (i > 0) sb.Append(", ");
                    var c = def.Costs[i];
                    sb.Append($"{c.Amount} {c.Resource}");
                }
                sb.AppendLine();
            }
            if (def.Blocks != null && def.Blocks.Length > 0)
            {
                sb.AppendLine("Effect Blocks:");
                for (int i = 0; i < def.Blocks.Length; i++)
                {
                    var block = def.Blocks[i];
                    sb.Append("- ");
                    sb.Append(block.Payload.Kind);
                    sb.Append($" scope={block.Scope.Kind}");
                    if (block.Scope.Kind == TargetScopeKind.Radius && block.Scope.Radius > 0f)
                        sb.Append($" radius={block.Scope.Radius:0.##}");
                    if (block.Scope.Kind == TargetScopeKind.ChainJump && block.Scope.Chain.MaxJumps > 0)
                        sb.Append($" jumps={block.Scope.Chain.MaxJumps}");
                    switch (block.Payload.Kind)
                    {
                        case EffectPayloadKind.Damage:
                            sb.Append($" amount={block.Payload.Damage.Amount}");
                            sb.Append(" ignoreArmor=");
                            sb.Append(block.Payload.Damage.IgnoreArmor != 0 ? "yes" : "no");
                            sb.Append(" ignoreResist=");
                            sb.Append(block.Payload.Damage.IgnoreResist != 0 ? "yes" : "no");
                            sb.Append(" ignoreSnapshot=");
                            sb.Append(block.Payload.Damage.IgnoreSnapshotModifiers != 0 ? "yes" : "no");
                            break;
                        case EffectPayloadKind.Heal:
                            sb.Append($" amount={block.Payload.Heal.Amount}");
                            break;
                        case EffectPayloadKind.ApplyBuff:
                        case EffectPayloadKind.ApplyDebuff:
                            sb.Append($" id={block.Payload.Apply.Id}");
                            break;
                        case EffectPayloadKind.SpawnDot:
                        case EffectPayloadKind.SpawnHot:
                            sb.Append($" id={block.Payload.OverTime.Id}");
                            break;
                        case EffectPayloadKind.SummonPet:
                            sb.Append($" pet={block.Payload.Summon.PetId} x{block.Payload.Summon.Count}");
                            break;
                        case EffectPayloadKind.SpawnAreaEffect:
                            sb.Append($" area={block.Payload.Area.AreaId}");
                            break;
                        case EffectPayloadKind.ScriptReference:
                            sb.Append($" script={block.Payload.Script.FeatureId}");
                            break;
                    }
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("Effect Blocks: <none>");
            }
            return sb.ToString();
        }

        private void DrawBuffsTab(EntityManager em, bool hasTarget)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Buffs & Debuffs", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Buff authoring UI is not yet implemented for the Framework variant. Use the manual actions and spell library to apply effects.", MessageType.Info);
            }
        }

        private void DrawPetsTab(EntityManager em, bool hasTarget, bool hasCaster)
        {
            PetSampleContent.RegisterDefaults();

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Pets & Summons", EditorStyles.boldLabel);
                _petsUseCaster = EditorGUILayout.Toggle("Control Caster Pets", _petsUseCaster);
                _petCategoryId = EditorGUILayout.TextField("Category Id", _petCategoryId ?? string.Empty);
                _petCategoryLevel = Mathf.Clamp(EditorGUILayout.IntField("Category Level", _petCategoryLevel), 0, 10);

                var owner = ResolvePetOwner(em, hasTarget, hasCaster);
                bool hasOwner = EntityExists(em, owner);
                EditorGUILayout.LabelField("Owner Entity", hasOwner ? FormatEntity(owner) : "<none>");

                if (!hasOwner)
                {
                    EditorGUILayout.HelpBox("Select a target or caster entity to manage their pets.", MessageType.Info);
                    return;
                }

                _petSummonCount = Mathf.Clamp(EditorGUILayout.IntField("Summon Count", _petSummonCount), 1, 12);
                _petSummonRadius = Mathf.Clamp(EditorGUILayout.FloatField("Spawn Radius", _petSummonRadius), 0.5f, 12f);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Sample Definitions", EditorStyles.miniBoldLabel);
                if (PetSampleLabels.Length > 0)
                {
                    _petSelectedSample = Mathf.Clamp(_petSelectedSample, 0, PetSampleLabels.Length - 1);
                    _petSelectedSample = GUILayout.SelectionGrid(_petSelectedSample, PetSampleLabels, 3);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Summon Sample"))
                        {
                            SummonPets(ref em, owner, PetSamplePetIds[_petSelectedSample], _petSummonCount, _petSummonRadius);
                        }
                        if (GUILayout.Button("Cast Sample Spell"))
                        {
                            var spellId = PetSampleSpellIds[_petSelectedSample];
                            var target = EntityExists(em, _targetEntity) ? _targetEntity : owner;
                            CastSpell(em, owner, target, spellId);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No sample pet definitions configured.", MessageType.Info);
                }

                _petCustomId = EditorGUILayout.TextField("Custom Pet Id", _petCustomId ?? string.Empty);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_petCustomId)))
                    {
                        if (GUILayout.Button("Summon Custom Pet"))
                        {
                            var normalized = (_petCustomId ?? string.Empty).Trim().ToLowerInvariant();
                            if (normalized.Length > 0)
                            {
                                if (normalized.Length > 64)
                                    normalized = normalized.Substring(0, 64);
                                SummonPets(ref em, owner, (FixedString64Bytes)normalized, _petSummonCount, _petSummonRadius);
                            }
                        }
                    }
                    if (GUILayout.Button("Dismiss All Pets"))
                    {
                        DismissAllPets(ref em, owner);
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Active Pets", EditorStyles.miniBoldLabel);
                _petListScroll = EditorGUILayout.BeginScrollView(_petListScroll, GUILayout.MinHeight(180));
                DrawPetList(ref em, owner);
                EditorGUILayout.EndScrollView();
            }
        }

        private void SummonPets(ref EntityManager em, in Entity owner, in FixedString64Bytes petId, int count, float radius)
        {
            if (!EntityExists(em, owner))
            {
                Debug.LogWarning("[CombatTest] Select a valid caster or target to act as the pet owner.");
                return;
            }

            int resolvedCount = math.max(1, count);
            float resolvedRadius = math.max(0.5f, radius);
            var category = BuildPetCategoryId();
            var initialTarget = EntityExists(em, _targetEntity) ? _targetEntity : owner;

            PetFactory.Summon(ref em, owner, initialTarget, petId, resolvedCount, resolvedRadius, category, math.max(0, _petCategoryLevel));
            Debug.Log($"[CombatTest] Summoned {resolvedCount}x '{petId.ToString()}' for {FormatEntity(owner)}.");
        }

        private FixedString32Bytes BuildPetCategoryId()
        {
            var value = (_petCategoryId ?? string.Empty).Trim().ToLowerInvariant();
            if (value.Length == 0)
                return DefaultPetCategory;
            if (value.Length > 32)
                value = value.Substring(0, 32);
            return (FixedString32Bytes)value;
        }

        private void DismissAllPets(ref EntityManager em, in Entity owner)
        {
            if (!EntityExists(em, owner))
                return;

            using var pets = new NativeList<Entity>(Allocator.Temp);
            PetQuery.GatherAll(ref em, owner, pets);
            if (pets.Length == 0)
            {
                Debug.Log("[CombatTest] Owner has no tracked pets.");
                return;
            }

            for (int i = 0; i < pets.Length; i++)
            {
                PetFactory.Despawn(ref em, pets[i], (FixedString64Bytes)"editor_clear");
            }
            Debug.Log($"[CombatTest] Dismissed {pets.Length} pet(s) for {FormatEntity(owner)}.");
        }

        private void DrawPetList(ref EntityManager em, in Entity owner)
        {
            using var pets = new NativeList<Entity>(Allocator.Temp);
            PetQuery.GatherAll(ref em, owner, pets);
            if (pets.Length == 0)
            {
                EditorGUILayout.HelpBox("Owner has no tracked pets.", MessageType.Info);
                return;
            }

            for (int i = 0; i < pets.Length; i++)
            {
                var pet = pets[i];
                if (!EntityExists(em, pet))
                    continue;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"{i + 1}.", GUILayout.Width(28));
                GUILayout.Label(FormatEntity(pet), GUILayout.Width(82));

                string petLabel = em.HasComponent<PetIdentity>(pet)
                    ? em.GetComponentData<PetIdentity>(pet).PetId.ToString()
                    : "<unknown>";
                GUILayout.Label(petLabel, GUILayout.Width(120));

                GUILayout.Label(BuildPetStatus(ref em, pet), GUILayout.ExpandWidth(true));

                if (GUILayout.Button("Target", GUILayout.Width(60)))
                {
                    _targetEntity = pet;
                    _casterIsTarget = false;
                }
                if (GUILayout.Button("Dismiss", GUILayout.Width(70)))
                {
                    PetFactory.Despawn(ref em, pet, (FixedString64Bytes)"editor_dismiss");
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private string BuildPetStatus(ref EntityManager em, in Entity pet)
        {
            var sb = new StringBuilder(96);
            if (em.HasComponent<Health>(pet))
            {
                var hp = em.GetComponentData<Health>(pet);
                sb.Append($"HP {hp.Current}/{hp.Max}");
            }
            else
            {
                sb.Append("HP -");
            }

            if (em.HasComponent<Mana>(pet))
            {
                var mp = em.GetComponentData<Mana>(pet);
                sb.Append($" | MP {mp.Current}/{mp.Max}");
            }

            if (em.HasComponent<PetGroup>(pet))
            {
                var group = em.GetComponentData<PetGroup>(pet).Id;
                if (group.Length > 0)
                    sb.Append($" | Group {group.ToString()}");
            }

            if (em.HasComponent<PetLifetimeTag>(pet))
            {
                var timer = em.GetComponentData<PetLifetimeTag>(pet).DefaultDurationSeconds;
                if (timer > 0f)
                    sb.Append($" | Duration {timer:0.#}s");
            }

            return sb.ToString();
        }
    }
}
#endif
