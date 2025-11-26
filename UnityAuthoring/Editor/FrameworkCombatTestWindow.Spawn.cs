#if UNITY_EDITOR
using Framework.Core.Components;
using Framework.Damage.Components;
using Framework.Damage.Requests;
using Framework.Resources.Factory;
using Framework.Spells.Requests;
using Framework.Spells.Spellbook.Components;
using Framework.Spells.Tests;
using Framework.TimedEffect.Requests;
using Framework.DOT.Components;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using Unity.Mathematics;

namespace Framework.UnityAuthoring.Editor
{
    public sealed partial class FrameworkCombatTestWindow
    {
        private void DrawSpawnTab(EntityManager em, bool hasTarget)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Spawn Test Entities", EditorStyles.boldLabel);
                _spawnCount = Mathf.Clamp(EditorGUILayout.IntField("Count", _spawnCount), 1, 64);
                _spawnHealth = Mathf.Max(1f, EditorGUILayout.FloatField("Health", _spawnHealth));
                _spawnHealthRegen = Mathf.Max(0f, EditorGUILayout.FloatField("Health Regen", _spawnHealthRegen));
                _spawnGiveMana = EditorGUILayout.Toggle("Give Mana", _spawnGiveMana);
                if (_spawnGiveMana)
                {
                    _spawnMana = Mathf.Max(0, EditorGUILayout.IntField("Mana", _spawnMana));
                    _spawnManaRegen = Mathf.Max(0f, EditorGUILayout.FloatField("Mana Regen", _spawnManaRegen));
                }
                _spawnArmor = Mathf.Max(0f, EditorGUILayout.FloatField("Armor", _spawnArmor));
                _spawnResist = Mathf.Clamp01(EditorGUILayout.FloatField("Resist", _spawnResist));
                _spawnTeam = (byte)Mathf.Clamp(EditorGUILayout.IntField("Team", _spawnTeam), 0, 32);
                _spawnAsTarget = EditorGUILayout.Toggle("Tag As Target", _spawnAsTarget);
                _spawnAsCaster = EditorGUILayout.Toggle("Tag As Caster", _spawnAsCaster);
                _spawnOffset = EditorGUILayout.FloatField("Forward Offset", _spawnOffset);
                _spawnRadius = Mathf.Max(0.1f, EditorGUILayout.FloatField("Ring Radius", _spawnRadius));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Spawn Forward"))
                    {
                        SpawnEntities(em, hasTarget, false);
                    }
                    if (GUILayout.Button("Spawn In Ring"))
                    {
                        SpawnEntities(em, hasTarget, true);
                    }
                }
            }
        }

        private void SpawnEntities(EntityManager em, bool hasTarget, bool useRing)
        {
            CompleteSpawnDependencies(em);

            var origin = ResolveAnchorPosition(em, hasTarget);
            var forward = ResolveAnchorForward(em, hasTarget);
            int count = Mathf.Max(1, _spawnCount);

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPos;
                if (useRing && count > 1)
                {
                    float t = (i / (float)count) * math.PI * 2f;
                    var offset = new Vector3(math.cos(t), 0f, math.sin(t)) * _spawnRadius;
                    spawnPos = origin + offset;
                }
                else
                {
                    spawnPos = origin + forward * Mathf.Max(0f, _spawnOffset + i * 1f);
                }

                CreateSpawn(em, spawnPos);
            }
        }

        private void CreateSpawn(EntityManager em, Vector3 position)
        {
            var entity = em.CreateEntity();
            ResourceFactory.InitHealth(ref em, entity, Mathf.CeilToInt(_spawnHealth), Mathf.CeilToInt(_spawnHealth), Mathf.CeilToInt(_spawnHealthRegen));
            if (_spawnGiveMana)
            {
                ResourceFactory.InitMana(ref em, entity, _spawnMana, _spawnMana, Mathf.CeilToInt(_spawnManaRegen));
            }

            if (!em.HasComponent<Damageable>(entity))
                em.AddComponentData(entity, new Damageable());
            var dmg = em.GetComponentData<Damageable>(entity);
            dmg.Armor = Mathf.RoundToInt(_spawnArmor);
            dmg.ResistPercent = math.saturate(_spawnResist);
            em.SetComponentData(entity, dmg);

            em.AddComponentData(entity, new TeamId { Value = _spawnTeam });
            em.AddComponentData(entity, new Position { Value = new float2(position.x, position.z) });

            if (_spawnAsTarget && !em.HasComponent<TestTargetTag>(entity))
                em.AddComponent<TestTargetTag>(entity);
            if (_spawnAsCaster && !em.HasComponent<TestCasterTag>(entity))
                em.AddComponent<TestCasterTag>(entity);

            if (!em.HasComponent<TestId>(entity))
            {
                _spawnIdCounter++;
                em.AddComponentData(entity, new TestId { Value = _spawnIdCounter });
            }
            int startingHealth = Mathf.CeilToInt(_spawnHealth);
            if (!em.HasComponent<LastHealth>(entity))
            {
                em.AddComponentData(entity, new LastHealth { Value = startingHealth });
            }
            else
            {
                em.SetComponentData(entity, new LastHealth { Value = startingHealth });
            }

            if (!em.HasBuffer<SpellSlot>(entity))
                em.AddBuffer<SpellSlot>(entity);
            var spellSlots = em.GetBuffer<SpellSlot>(entity);
            var activeSpell = GetActiveSpellId();
            bool hasEntry = false;
            for (int i = 0; i < spellSlots.Length; i++)
            {
                if (spellSlots[i].SpellId.Equals(activeSpell))
                {
                    hasEntry = true;
                    break;
                }
            }
            if (!hasEntry)
            {
                spellSlots.Add(new SpellSlot { SpellId = activeSpell });
            }

            if (!em.HasBuffer<SpellCastRequest>(entity))
                em.AddBuffer<SpellCastRequest>(entity);

            if (!em.HasBuffer<TimedEffectRequest>(entity))
                em.AddBuffer<TimedEffectRequest>(entity);

            if (!em.HasBuffer<DotInstance>(entity))
                em.AddBuffer<DotInstance>(entity);

            if (!em.HasBuffer<DamageRequest>(entity))
                em.AddBuffer<DamageRequest>(entity);

            Debug.Log($"[CombatTest] Spawned entity {FormatEntity(entity)} at {position} team={_spawnTeam} targetTag={_spawnAsTarget} casterTag={_spawnAsCaster}");
        }

        private Vector3 ResolveAnchorForward(EntityManager em, bool hasTarget)
        {
            if (_anchorObject != null)
                return _anchorObject.transform.forward.normalized;

            if (hasTarget && em.HasComponent<Position>(_targetEntity))
            {
                return Vector3.forward;
            }

            return Vector3.forward;
        }

        private void CompleteSpawnDependencies(EntityManager em)
        {
            em.CompleteDependencyBeforeRW<Framework.Resources.Components.Health>();
            em.CompleteDependencyBeforeRW<Framework.Resources.Components.Mana>();
            em.CompleteDependencyBeforeRW<Framework.Resources.Components.Stamina>();
            em.CompleteDependencyBeforeRW<Framework.Damage.Components.Damageable>();
            em.CompleteDependencyBeforeRW<Framework.Core.Components.TeamId>();
            em.CompleteDependencyBeforeRW<Framework.Core.Components.Position>();
            em.CompleteDependencyBeforeRW<Framework.Spells.Spellbook.Components.SpellSlot>();
            em.CompleteDependencyBeforeRW<Framework.Spells.Requests.SpellCastRequest>();
            em.CompleteDependencyBeforeRW<TestCasterTag>();
            em.CompleteDependencyBeforeRW<TestTargetTag>();
            em.CompleteDependencyBeforeRW<LastHealth>();
            em.CompleteDependencyBeforeRW<TestId>();
            em.CompleteDependencyBeforeRW<SkirmishAgent>();
            em.CompleteDependencyBeforeRW<SkirmishTeamColor>();
        }
    }
}
#endif
