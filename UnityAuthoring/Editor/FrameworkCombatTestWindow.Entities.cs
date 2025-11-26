#if UNITY_EDITOR
using System;
using Framework.Damage.Components;
using Framework.Resources.Components;
using Framework.Spells.Factory;
using Framework.Spells.Spellbook.Components;
using Framework.Spells.Tests;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using Unity.Mathematics;

namespace Framework.UnityAuthoring.Editor
{
    public sealed partial class FrameworkCombatTestWindow
    {
        private void DrawEntitiesList(EntityManager em, bool hasTarget, bool hasCaster)
        {
            EditorGUILayout.LabelField("Entities", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                GUILayout.Label($"Caster: {(hasCaster ? FormatEntity(_casterEntity) : "<none>")}", GUILayout.Width(180));
                GUILayout.Label($"Target: {(hasTarget ? FormatEntity(_targetEntity) : "<none>")}", GUILayout.Width(180));
                GUILayout.FlexibleSpace();
            }

            _filterByHealth = EditorGUILayout.Toggle("Only With Health", _filterByHealth);
            _maxEntities = Mathf.Clamp(EditorGUILayout.IntField("Max Results", _maxEntities), 1, 5000);

            var queryDesc = new EntityQueryDesc
            {
                All = _filterByHealth
                    ? new ComponentType[] { ComponentType.ReadOnly<Health>() }
                    : Array.Empty<ComponentType>(),
                None = new ComponentType[] { ComponentType.ReadOnly<Disabled>() }
            };
            using var query = em.CreateEntityQuery(queryDesc);
            using var entities = query.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0)
            {
                EditorGUILayout.HelpBox("No matching entities found.", MessageType.Info);
                return;
            }

            var managed = entities.ToArray();
            Array.Sort(managed, (a, b) => a.Index.CompareTo(b.Index));
            var anchor = ResolveAnchorPosition(em, hasTarget);

            _entityScroll = EditorGUILayout.BeginScrollView(_entityScroll, GUILayout.MinHeight(260));
            int shown = 0;
            foreach (var e in managed)
            {
                if (shown++ >= _maxEntities) break;
                EditorGUILayout.BeginHorizontal();
                bool isTarget = hasTarget && e == _targetEntity;
                bool isCaster = (hasCaster && e == _casterEntity) || (_casterIsTarget && isTarget);
                GUILayout.Label(isCaster ? "C" : (isTarget ? "T" : " "), GUILayout.Width(20));
                GUILayout.Label(FormatEntity(e), GUILayout.Width(82));

                if (em.HasComponent<Health>(e))
                {
                    var h = em.GetComponentData<Health>(e);
                    GUILayout.Label($"HP {h.Current}/{h.Max}", GUILayout.Width(120));
                }
                else
                {
                    GUILayout.Label("HP -", GUILayout.Width(120));
                }

                if (em.HasComponent<Mana>(e))
                {
                    var m = em.GetComponentData<Mana>(e);
                    GUILayout.Label($"MP {m.Current}/{m.Max}", GUILayout.Width(120));
                }
                else
                {
                    GUILayout.Label("MP -", GUILayout.Width(120));
                }

                float dist = ComputeDistance(em, e, anchor);
                GUILayout.Label(dist >= 0f ? $"{dist:0.0}m" : "-", GUILayout.Width(60));

                if (GUILayout.Button("Target", GUILayout.Width(60)))
                {
                    _targetEntity = e;
                }
                if (GUILayout.Button("Caster", GUILayout.Width(60)))
                {
                    _casterEntity = e;
                    _casterIsTarget = false;
                }

                using (new EditorGUI.DisabledScope(!em.Exists(e)))
                {
                    if (GUILayout.Button("Spell", GUILayout.Width(56)))
                    {
                        var spellId = GetActiveSpellId();
                        var caster = ResolveCaster(em, hasTarget, hasCaster);
                        _targetEntity = e;
                        CastSpell(em, caster, e, spellId);
                    }
                    if (GUILayout.Button("Hit", GUILayout.Width(48)))
                    {
                        ApplyMagnitude(em, e, -(int)math.abs(_manualAmount), _manualSchool);
                    }
                    if (GUILayout.Button("Heal", GUILayout.Width(48)))
                    {
                        ApplyHeal(em, e, (int)math.abs(_manualAmount));
                    }
                    if (GUILayout.Button("Kill", GUILayout.Width(48)))
                    {
                        KillEntity(em, e);
                    }
                    if (GUILayout.Button("Despawn", GUILayout.Width(64)))
                    {
                        DespawnEntity(em, e);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Batch Actions", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Cast Selected Spell"))
                    {
                        var caster = ResolveCaster(em, hasTarget, hasCaster);
                        if (EntityExists(em, caster) && hasTarget)
                        {
                            CastSpell(em, caster, _targetEntity, GetActiveSpellId());
                        }
                    }
                    if (GUILayout.Button("Despawn All Test Targets"))
                    {
                        DespawnAllTagged<TestTargetTag>(em);
                    }
                }
            }
        }

        private void CastSpell(EntityManager em, Entity caster, Entity target, FixedString64Bytes spellId)
        {
            if (!EntityExists(em, target))
            {
                Debug.LogWarning("[CombatTest] No valid target to cast onto.");
                return;
            }

            if (!EntityExists(em, caster))
            {
                caster = target;
            }

            EnsureSpellKnown(ref em, caster, spellId);
            SpellPipelineFactory.Cast(ref em, caster, target, spellId, 0);
            Debug.Log($"[CombatTest] Requested spell '{spellId.ToString()}' from {FormatEntity(caster)} onto {FormatEntity(target)}.");
        }

        private void ApplyMagnitude(EntityManager em, Entity entity, int amount, DamageSchool school)
        {
            if (!EntityExists(em, entity))
                return;

            var packet = new DamagePacket
            {
                Amount = math.abs(amount),
                School = school,
                Source = _casterIsTarget ? entity : (_casterEntity == Entity.Null ? entity : _casterEntity),
                CritMult = 1f
            };
            Framework.Damage.Factory.DamageFactory.EnqueueDamage(ref em, entity, packet);
        }

        private void ApplyHeal(EntityManager em, Entity entity, int amount)
        {
            if (!EntityExists(em, entity))
                return;
            Framework.Heal.Factory.HealFactory.EnqueueHeal(ref em, entity, amount);
        }

        private void KillEntity(EntityManager em, Entity entity)
        {
            if (!EntityExists(em, entity) || !em.HasComponent<Health>(entity)) return;
            var h = em.GetComponentData<Health>(entity);
            h.Current = 0;
            em.SetComponentData(entity, h);
            Debug.Log($"[CombatTest] Forced kill of {FormatEntity(entity)}.");
        }

        private void DespawnEntity(EntityManager em, Entity entity)
        {
            if (!EntityExists(em, entity)) return;
            em.DestroyEntity(entity);
        }

        private void DespawnAllTagged<T>(EntityManager em) where T : unmanaged, IComponentData
        {
            using var q = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            using var ents = q.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < ents.Length; i++)
            {
                if (em.Exists(ents[i]))
                    em.DestroyEntity(ents[i]);
            }
            Debug.Log($"[CombatTest] Despawned {ents.Length} entity(ies) tagged with {typeof(T).Name}.");
        }

        private static string FormatEntity(Entity e) => $"{e.Index}:{e.Version}";

        private Vector3 ResolveAnchorPosition(EntityManager em, bool hasTarget)
        {
            if (_anchorObject != null)
                return _anchorObject.transform.position;

            if (hasTarget && em.HasComponent<Framework.Core.Components.Position>(_targetEntity))
            {
                var pos = em.GetComponentData<Framework.Core.Components.Position>(_targetEntity).Value;
                return new Vector3(pos.x, 0f, pos.y);
            }

            return SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        }

        private float ComputeDistance(EntityManager em, Entity e, Vector3 anchor)
        {
            if (!em.HasComponent<Framework.Core.Components.Position>(e))
                return -1f;
            var pos = em.GetComponentData<Framework.Core.Components.Position>(e).Value;
            return Vector3.Distance(anchor, new Vector3(pos.x, 0f, pos.y));
        }

        private Entity ResolveCaster(EntityManager em, bool hasTarget, bool hasCaster)
        {
            if (_casterIsTarget && hasTarget) return _targetEntity;
            if (hasCaster) return _casterEntity;
            return hasTarget ? _targetEntity : Entity.Null;
        }

        private Entity ResolvePetOwner(EntityManager em, bool hasTarget, bool hasCaster)
        {
            if (_petsUseCaster)
            {
                var caster = ResolveCaster(em, hasTarget, hasCaster);
                if (EntityExists(em, caster))
                    return caster;
            }

            if (hasTarget && EntityExists(em, _targetEntity))
                return _targetEntity;

            return Entity.Null;
        }

        private FixedString64Bytes GetActiveSpellId()
        {
            SpellTestSampleContent.EnsureRegistered();
            if (_includeSampleSpells && SpellTestSampleContent.DefaultSpellIds.Count > 0)
            {
                int idx = math.clamp(_selectedSampleSpell, 0, SpellTestSampleContent.DefaultSpellIds.Count - 1);
                return (FixedString64Bytes)SpellTestSampleContent.DefaultSpellIds[idx];
            }
            return new FixedString64Bytes(string.IsNullOrWhiteSpace(_customSpellId) ? "fireball" : _customSpellId.Trim().ToLowerInvariant());
        }

        private void EnsureSpellKnown(ref EntityManager em, in Entity entity, in FixedString64Bytes spellId)
        {
            if (!EntityExists(em, entity))
                return;

            if (!em.HasBuffer<SpellSlot>(entity))
                em.AddBuffer<SpellSlot>(entity);

            var spellSlots = em.GetBuffer<SpellSlot>(entity);
            for (int i = 0; i < spellSlots.Length; i++)
            {
                if (spellSlots[i].SpellId.Equals(spellId))
                    return;
            }

            spellSlots.Add(new SpellSlot { SpellId = spellId });
        }
    }
}
#endif
