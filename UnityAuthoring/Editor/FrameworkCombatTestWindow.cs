#if UNITY_EDITOR
using Framework.Core.Base;
using Framework.Damage.Runtime;
using Framework.Spells.Factory;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Framework.UnityAuthoring.Editor
{
    public sealed partial class FrameworkCombatTestWindow
    {
        private static readonly string[] Tabs = { "Target", "Actions", "Spawn", "Spells", "Library", "Buffs", "Pets", "Skirmish" };

        [MenuItem("Window/Framework/Combat Test Panel")]
        public static void Open()
        {
            var window = GetWindow<FrameworkCombatTestWindow>();
            window.titleContent = new GUIContent("Combat Test");
            window.minSize = new Vector2(680, 420);
            window.Show();
        }

        private bool _worldBootstrapped;
        private bool _spellDebugLogs;

        private void OnEnable()
        {
            _worldBootstrapped = false;
            _spellDebugLogs = false;
            SpellPipelineFactory.EnableDebugLogs(_spellDebugLogs);
            DamageDebugBridge.EnableDebugLogs(_spellDebugLogs);
            _nextRepaint = EditorApplication.timeSinceStartup + RepaintInterval;
            if (_skirmishTeams == null || _skirmishTeams.Count == 0)
                ResetSkirmishDefaults();
            SceneView.duringSceneGui -= DrawSkirmishSceneOverlay;
            SceneView.duringSceneGui += DrawSkirmishSceneOverlay;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawSkirmishSceneOverlay;
        }

        private void Update()
        {
            if (!EditorApplication.isPlaying) return;
            if (EditorApplication.timeSinceStartup >= _nextRepaint)
            {
                Repaint();
                _nextRepaint = EditorApplication.timeSinceStartup + RepaintInterval;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Framework Combat Test", EditorStyles.boldLabel);
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use the combat test tools.", MessageType.Info);
                return;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                EditorGUILayout.HelpBox("No active ECS World detected.", MessageType.Warning);
                return;
            }

            if (!_worldBootstrapped)
            {
                SubsystemBootstrap.InstallAll(world);
                _worldBootstrapped = true;
            }

            var em = world.EntityManager;

            using (new EditorGUILayout.HorizontalScope())
            {
                _anchorObject = (GameObject)EditorGUILayout.ObjectField("Anchor", _anchorObject, typeof(GameObject), true);
                _casterIsTarget = EditorGUILayout.Toggle("Caster Uses Target", _casterIsTarget);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                bool newDebug = EditorGUILayout.Toggle("Spell Debug Logs", _spellDebugLogs);
                if (newDebug != _spellDebugLogs)
                {
                    _spellDebugLogs = newDebug;
                    SpellPipelineFactory.EnableDebugLogs(_spellDebugLogs);
                    DamageDebugBridge.EnableDebugLogs(_spellDebugLogs);
                }
            }

            bool hasTarget = EntityExists(em, _targetEntity);
            bool hasCaster = EntityExists(em, _casterEntity);
            if (!hasCaster && _casterIsTarget && hasTarget)
                _casterEntity = _targetEntity;

            _tabIndex = GUILayout.Toolbar(_tabIndex, Tabs);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    switch (_tabIndex)
                    {
                        case 0:
                            DrawTargetTab(em, hasTarget);
                            break;
                        case 1:
                            DrawActionsTab(em, hasTarget, hasCaster);
                            break;
                        case 2:
                            DrawSpawnTab(em, hasTarget);
                            break;
                        case 3:
                            DrawSpellsTab(em, hasTarget, hasCaster);
                            break;
                        case 4:
                            DrawLibraryTab(em);
                            break;
                        case 5:
                            DrawBuffsTab(em, hasTarget);
                            break;
                        case 6:
                            DrawPetsTab(em, hasTarget, hasCaster);
                            break;
                        case 7:
                            DrawSkirmishTab(em);
                            break;
                    }
                }

                using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(520)))
                {
                    DrawEntitiesList(em, hasTarget, hasCaster);
                }
            }
        }

        private static bool EntityExists(EntityManager em, Entity e) => e != Entity.Null && em.Exists(e);
    }
}
#endif
