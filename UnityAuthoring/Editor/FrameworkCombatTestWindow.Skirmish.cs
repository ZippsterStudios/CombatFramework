#if UNITY_EDITOR
using System.Collections.Generic;
using Framework.AI.Components;
using Framework.AI.Runtime;
using Framework.Contracts.Intents;
using Framework.Core.Components;
using Framework.Damage.Components;
using Framework.Damage.Requests;
using Framework.Resources.Components;
using Framework.Resources.Factory;
using Framework.Spells.Requests;
using Framework.Spells.Spellbook.Components;
using Framework.Spells.Tests;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using ContractsAIAgentTarget = Framework.Contracts.AI.AIAgentTarget;

namespace Framework.UnityAuthoring.Editor
{
    public sealed partial class FrameworkCombatTestWindow
    {
        private void DrawSkirmishTab(EntityManager em)
        {
            if (_skirmishTeams == null || _skirmishTeams.Count == 0)
                ResetSkirmishDefaults();

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Skirmish Sandbox", EditorStyles.boldLabel);
                _skirmishShowGizmos = EditorGUILayout.Toggle("Draw Gizmos", _skirmishShowGizmos);
                _skirmishAutoClear = EditorGUILayout.Toggle("Clear Existing Agents", _skirmishAutoClear);
                _skirmishDefaultSpell = EditorGUILayout.TextField("Fallback Spell", _skirmishDefaultSpell);

                EditorGUILayout.Space();

                for (int i = 0; i < _skirmishTeams.Count; i++)
                {
                    var team = _skirmishTeams[i];
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            team.Label = EditorGUILayout.TextField("Label", team.Label);
                            if (GUILayout.Button("Remove", GUILayout.Width(70)))
                            {
                                _skirmishTeams.RemoveAt(i);
                                i--;
                                continue;
                            }
                        }

                        team.TeamId = (byte)Mathf.Clamp(EditorGUILayout.IntField("Team Id", team.TeamId), 0, 200);
                        team.Color = EditorGUILayout.ColorField("Color", team.Color);
                        team.Count = Mathf.Clamp(EditorGUILayout.IntField("Agents", team.Count), 1, 128);
                        team.Center = EditorGUILayout.Vector2Field("Center (XZ)", team.Center);
                        team.Radius = Mathf.Max(0.5f, EditorGUILayout.FloatField("Spawn Radius", team.Radius));
                        team.Health = Mathf.Max(1, EditorGUILayout.IntField("Health", team.Health));
                        team.Mana = Mathf.Max(0, EditorGUILayout.IntField("Mana", team.Mana));
                        team.Armor = Mathf.Max(0f, EditorGUILayout.FloatField("Armor", team.Armor));
                        team.Resist = Mathf.Clamp01(EditorGUILayout.FloatField("Resist", team.Resist));
                        team.VisionRange = Mathf.Max(1f, EditorGUILayout.FloatField("Vision Range", team.VisionRange));
                        team.AttackRange = Mathf.Max(0.5f, EditorGUILayout.FloatField("Attack Range", team.AttackRange));
                        team.MoveSpeed = Mathf.Max(0f, EditorGUILayout.FloatField("Move Speed", team.MoveSpeed));
                        team.Cooldown = Mathf.Max(0.1f, EditorGUILayout.FloatField("Cast Cooldown", team.Cooldown));
                        team.SpellId = EditorGUILayout.TextField("Spell Id", string.IsNullOrWhiteSpace(team.SpellId) ? _skirmishDefaultSpell : team.SpellId);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Use Scene View Position"))
                            {
                                var pivot = SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
                                team.Center = new Vector2(pivot.x, pivot.z);
                            }
                            GUILayout.FlexibleSpace();
                        }
                    }

                    if (i >= 0 && i < _skirmishTeams.Count)
                        _skirmishTeams[i] = team;
                }

                if (GUILayout.Button("Add Team"))
                    _skirmishTeams.Add(CreateDefaultTeam(_skirmishTeams.Count));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Spawn Teams"))
                        SpawnSkirmishTeams(em);
                    if (GUILayout.Button("Despawn Skirmish Agents"))
                        DespawnSkirmishAgents(em);
                }
            }
        }

        private void SpawnSkirmishTeams(EntityManager em)
        {
            if (_skirmishTeams == null || _skirmishTeams.Count == 0)
                ResetSkirmishDefaults();

            if (_skirmishAutoClear)
                DespawnSkirmishAgents(em);

            CompleteSpawnDependencies(em);
            SpellTestSampleContent.EnsureRegistered();

            int spawned = 0;
            foreach (var team in _skirmishTeams)
            {
                var spellId = ResolveSkirmishSpellId(team);
                var center = new float2(team.Center.x, team.Center.y);
                for (int i = 0; i < team.Count; i++)
                {
                    var offset2D = team.Radius > 0.1f
                        ? UnityEngine.Random.insideUnitCircle * team.Radius
                        : Vector2.zero;
                    var spawnPos = center + new float2(offset2D.x, offset2D.y);
                    CreateSkirmishAgent(em, team, spawnPos, spellId);
                    spawned++;
                }
            }

            Debug.Log($"[CombatTest] Spawned {spawned} skirmish agent(s) across {_skirmishTeams.Count} team(s).");
        }

        private void DespawnSkirmishAgents(EntityManager em)
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<SkirmishAgent>());
            using var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.Exists(entities[i]))
                    em.DestroyEntity(entities[i]);
            }
            if (entities.Length > 0)
                Debug.Log($"[CombatTest] Despawned {entities.Length} skirmish agent(s).");
        }

        private void CreateSkirmishAgent(EntityManager em, SkirmishTeamConfig team, in float2 spawnPos, in FixedString64Bytes spellId)
        {
            var entity = em.CreateEntity();

            ResourceFactory.InitHealth(ref em, entity, team.Health, team.Health);
            if (team.Mana > 0)
                ResourceFactory.InitMana(ref em, entity, team.Mana, team.Mana);

            em.AddComponentData(entity, new Damageable
            {
                Armor = Mathf.RoundToInt(team.Armor),
                ResistPercent = math.saturate(team.Resist)
            });

            em.AddComponentData(entity, new TeamId { Value = team.TeamId });
            em.AddComponentData(entity, new Position { Value = spawnPos });
            em.AddComponentData(entity, new SkirmishAgent
            {
                SpellId = spellId,
                CooldownSeconds = math.max(0.1f, team.Cooldown),
                CooldownTimer = UnityEngine.Random.Range(0f, math.max(0.1f, team.Cooldown)),
                VisionRange = math.max(team.VisionRange, team.AttackRange + 1f),
                AttackRange = math.max(0.5f, team.AttackRange),
                MoveSpeed = math.max(0f, team.MoveSpeed),
                TeamId = team.TeamId,
                LastTarget = Entity.Null
            });
            em.AddComponentData(entity, new SkirmishTeamColor
            {
                Value = new float4(team.Color.r, team.Color.g, team.Color.b, 1f)
            });

            if (!em.HasBuffer<SpellCastRequest>(entity))
                em.AddBuffer<SpellCastRequest>(entity);
            if (!em.HasBuffer<SpellSlot>(entity))
                em.AddBuffer<SpellSlot>(entity);
            EnsureSpellKnown(ref em, entity, spellId);

            if (!em.HasBuffer<DamageRequest>(entity))
                em.AddBuffer<DamageRequest>(entity);

            _spawnIdCounter++;
            if (!em.HasComponent<TestId>(entity))
                em.AddComponentData(entity, new TestId { Value = _spawnIdCounter });

            // Track runtime health deltas for the test logger if present.
            if (!em.HasComponent<LastHealth>(entity))
                em.AddComponentData(entity, new LastHealth { Value = team.Health });

            EnsureSkirmishAIComponents(ref em, entity, team, spellId);
        }

        private static void EnsureSkirmishAIComponents(ref EntityManager em, in Entity entity, SkirmishTeamConfig team, in FixedString64Bytes spellId)
        {
            if (!em.HasComponent<AIState>(entity))
                em.AddComponentData(entity, new AIState { Current = AIStateIds.Combat });
            else
            {
                var state = em.GetComponentData<AIState>(entity);
                if (state.Current == AIStateIds.Idle)
                {
                    state.Current = AIStateIds.Combat;
                    em.SetComponentData(entity, state);
                }
            }

            if (!em.HasComponent<AIAgentBehaviorConfig>(entity))
                em.AddComponentData(entity, AIAgentBehaviorConfig.CreateDefaults());

            var config = em.GetComponentData<AIAgentBehaviorConfig>(entity);
            config.DecisionIntervalSeconds = math.clamp(team.Cooldown * 0.25f, 0.05f, 0.75f);
            config.AttackRange = math.max(0.5f, team.AttackRange);
            config.ChaseRange = math.max(config.AttackRange + 4f, team.VisionRange);
            config.MoveSpeed = math.max(0.1f, team.MoveSpeed);
            config.FleeMoveSpeed = math.max(config.MoveSpeed, team.MoveSpeed + 1f);
            config.PrimarySpellId = spellId;
            config.PrimarySpellCooldownSeconds = math.max(0.1f, team.Cooldown);
            em.SetComponentData(entity, config);

            if (!em.HasComponent<AIAgentDecisionState>(entity))
                em.AddComponentData(entity, AIAgentDecisionState.CreateDefault());

            if (!em.HasComponent<AICombatRuntime>(entity))
                em.AddComponentData(entity, AICombatRuntime.CreateDefault());

            if (!em.HasComponent<ContractsAIAgentTarget>(entity))
                em.AddComponentData(entity, ContractsAIAgentTarget.CreateDefault());

            if (!em.HasComponent<MoveIntent>(entity))
                em.AddComponentData(entity, MoveIntent.Cleared);
            else
                em.SetComponentData(entity, MoveIntent.Cleared);

            if (!em.HasComponent<CastIntent>(entity))
                em.AddComponentData(entity, new CastIntent());
            else
            {
                var cast = em.GetComponentData<CastIntent>(entity);
                cast.Clear();
                em.SetComponentData(entity, cast);
            }

            if (!em.HasBuffer<StateChangeRequest>(entity))
                em.AddBuffer<StateChangeRequest>(entity);
        }

        private FixedString64Bytes ResolveSkirmishSpellId(SkirmishTeamConfig team)
        {
            if (!string.IsNullOrWhiteSpace(team.SpellId))
                return (FixedString64Bytes)team.SpellId.Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(_skirmishDefaultSpell))
                return (FixedString64Bytes)_skirmishDefaultSpell.Trim().ToLowerInvariant();

            return GetActiveSpellId();
        }

        private SkirmishTeamConfig CreateDefaultTeam(int index)
        {
            var cfg = new SkirmishTeamConfig();
            cfg.Label = index % 2 == 0 ? "Crimson" : "Cobalt";
            cfg.TeamId = (byte)(index + 1);
            cfg.Center = index % 2 == 0 ? new Vector2(-12f, 0f) : new Vector2(12f, 0f);
            cfg.Color = index % 2 == 0 ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.2f, 0.4f, 0.85f);
            return cfg;
        }

        private void ResetSkirmishDefaults()
        {
            _skirmishTeams ??= new List<SkirmishTeamConfig>();
            _skirmishTeams.Clear();
            _skirmishTeams.Add(new SkirmishTeamConfig
            {
                Label = "Crimson",
                TeamId = 1,
                Center = new Vector2(-12f, 0f),
                Color = new Color(0.85f, 0.25f, 0.25f),
                SpellId = "fireball"
            });
            _skirmishTeams.Add(new SkirmishTeamConfig
            {
                Label = "Cobalt",
                TeamId = 2,
                Center = new Vector2(12f, 0f),
                Color = new Color(0.2f, 0.45f, 0.9f),
                SpellId = "frost_nova"
            });
        }

        private void DrawSkirmishSceneOverlay(SceneView view)
        {
            if (!_skirmishShowGizmos || _skirmishTeams == null)
                return;

            Handles.zTest = CompareFunction.Always;

            foreach (var team in _skirmishTeams)
            {
                var color = team.Color;
                Handles.color = new Color(color.r, color.g, color.b, 0.8f);
                var center = new Vector3(team.Center.x, 0f, team.Center.y);
                Handles.DrawWireDisc(center, Vector3.up, team.Radius);
                Handles.DrawWireDisc(center, Vector3.up, team.VisionRange);
                Handles.Label(center + Vector3.up * 0.15f, $"{team.Label} (Team {team.TeamId})");
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            var em = world.EntityManager;
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<SkirmishAgent>(), ComponentType.ReadOnly<Position>(), ComponentType.ReadOnly<SkirmishTeamColor>());
            if (query.CalculateEntityCount() == 0)
                return;

            using var positions = query.ToComponentDataArray<Position>(Allocator.Temp);
            using var agents = query.ToComponentDataArray<SkirmishAgent>(Allocator.Temp);
            using var colors = query.ToComponentDataArray<SkirmishTeamColor>(Allocator.Temp);

            for (int i = 0; i < positions.Length; i++)
            {
                var pos2 = positions[i].Value;
                var color = colors[i].Value;
                Handles.color = new Color(color.x, color.y, color.z, 0.9f);
                var pos3 = new Vector3(pos2.x, 0f, pos2.y);
                Handles.DrawSolidDisc(pos3, Vector3.up, 0.4f);

                var target = agents[i].LastTarget;
                if (target != Entity.Null && em.Exists(target) && em.HasComponent<Position>(target))
                {
                    var tgt = em.GetComponentData<Position>(target).Value;
                    Handles.DrawLine(pos3, new Vector3(tgt.x, 0f, tgt.y));
                }
            }
        }
    }
}
#endif
