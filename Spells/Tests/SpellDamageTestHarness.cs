using System;
using System.Collections.Generic;
using Framework.Debuffs.Content;
using Framework.Spells.Content;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace Framework.Spells.Tests
{
    // Drop this on any GameObject in a Unity scene to bootstrap a simple spell damage test.
    public class SpellDamageTestHarness : MonoBehaviour
    {
        [Header("Spells")] public string PrimarySpellId = "fireball";
        public bool RegisterSampleSpells = true;
        public bool IncludeSampleRotation = true;
        public string[] AdditionalSpellIds = Array.Empty<string>();

        [Header("Spell Sequence")]
        public bool UseSpellSequence = true;
        public bool EnsurePrimaryInSequence = true;

        [Header("Debug & Logging")]
        public bool VerboseLogging = true;

        [Header("Casters")]
        public int CasterCount = 1;
        public int CasterHealth = 100;
        public int CasterMana = 200;

        [Header("Targets")]
        public int TargetCount = 5;
        public int TargetHealth = 150;
        public int BaseArmor = 5;
        [Range(0f, 0.95f)] public float BaseResist = 0.1f;

        [Header("Casting Behavior")]
        public float CastIntervalSeconds = 1.6f; // recommended: >= cast time + cooldown

        void Awake()
        {
            if (RegisterSampleSpells)
                RegisterSpells();
        }

        void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("No default world found; ensure Entities bootstrap is set up.");
                return;
            }

            var em = world.EntityManager;
            var spellIds = new List<FixedString64Bytes>();
            var primary = string.IsNullOrWhiteSpace(PrimarySpellId) ? "fireball" : PrimarySpellId.Trim();
            var primaryFixed = new FixedString64Bytes(primary);
            AddUniqueSpell(spellIds, primaryFixed);

            // Create a singleton config used by the runtime test system
            var cfgEntity = em.CreateEntity();
            em.AddComponent<SpellDamageTestConfig>(cfgEntity);

            var cfg = new SpellDamageTestConfig
            {
                SpellId = primaryFixed,
                Interval = CastIntervalSeconds,
                Accumulator = 0f,
                NextTargetIndex = 0,
                NextSpellIndex = 0,
                VerboseLogging = (byte)(VerboseLogging ? 1 : 0),
                UseSequence = 0,
                Sequence = default
            };

            if (UseSpellSequence)
            {
                FixedList512Bytes<FixedString64Bytes> sequence = default;
                var seen = new HashSet<string>(StringComparer.Ordinal);

                if (IncludeSampleRotation)
                {
                    foreach (var sampleId in SpellTestSampleContent.DefaultSpellIds)
                    {
                        if (TryMakeFixed(sampleId, out var fid) && seen.Add(fid.ToString()))
                        {
                            sequence.Add(fid);
                            AddUniqueSpell(spellIds, fid);
                        }
                    }
                }

                if (AdditionalSpellIds != null)
                {
                    for (int i = 0; i < AdditionalSpellIds.Length; i++)
                    {
                        var raw = AdditionalSpellIds[i];
                        if (TryMakeFixed(raw, out var fid) && seen.Add(fid.ToString()))
                        {
                            sequence.Add(fid);
                            AddUniqueSpell(spellIds, fid);
                        }
                    }
                }

                if (EnsurePrimaryInSequence && seen.Add(primaryFixed.ToString()))
                {
                    sequence.Add(primaryFixed);
                }

                if (sequence.Length > 0)
                {
                    cfg.UseSequence = 1;
                    cfg.SpellId = sequence[0];
                    cfg.Sequence = sequence;
                }
            }

            em.SetComponentData(cfgEntity, cfg);

            // Spawn casters
            for (int i = 0; i < Mathf.Max(1, CasterCount); i++)
            {
                var e = em.CreateEntity();
                Framework.Resources.Factory.ResourceFactory.InitHealth(ref em, e, Mathf.Max(1, CasterHealth), Mathf.Max(1, CasterHealth));
                Framework.Resources.Factory.ResourceFactory.InitMana(ref em, e, Mathf.Max(0, CasterMana), Mathf.Max(0, CasterMana));
                // Position + Team
                if (!em.HasComponent<Framework.Core.Components.Position>(e))
                    em.AddComponentData(e, new Framework.Core.Components.Position { Value = new float2(0, 0) });
                if (!em.HasComponent<Framework.Core.Components.TeamId>(e))
                    em.AddComponentData(e, new Framework.Core.Components.TeamId { Value = 1 });
                // Ensure spell request buffer exists to avoid structural changes during test system update
                if (!em.HasBuffer<Framework.Spells.Requests.SpellCastRequest>(e))
                    em.AddBuffer<Framework.Spells.Requests.SpellCastRequest>(e);
                if (!em.HasBuffer<Framework.Spells.Spellbook.Components.SpellSlot>(e))
                    em.AddBuffer<Framework.Spells.Spellbook.Components.SpellSlot>(e);
                var sb = em.GetBuffer<Framework.Spells.Spellbook.Components.SpellSlot>(e);
                for (int s = 0; s < spellIds.Count; s++)
                {
                    sb.Add(new Framework.Spells.Spellbook.Components.SpellSlot { SpellId = spellIds[s] });
                }
                em.AddComponent<TestCasterTag>(e);
                if (VerboseLogging)
                {
                    Debug.Log($"[SpellDamageTest] Spawned caster Entity {e.Index}:{e.Version} with {spellIds.Count} spell(s).");
                }
            }

            // Spawn targets with incremental armor/resist for variety
            for (int i = 0; i < Mathf.Max(1, TargetCount); i++)
            {
                var e = em.CreateEntity();
                Framework.Resources.Factory.ResourceFactory.InitHealth(ref em, e, Mathf.Max(1, TargetHealth), Mathf.Max(1, TargetHealth));
                em.AddComponentData(e, new Framework.Damage.Components.Damageable
                {
                    Armor = Mathf.Max(0, BaseArmor + i),
                    ResistPercent = Mathf.Clamp01(BaseResist + i * 0.02f)
                });
                // Position targets on a circle around origin and assign to team 2
                var angle = (math.PI * 2f) * (i / (float)Mathf.Max(1, TargetCount));
                var radius = 5f + i * 0.5f;
                var pos = new float2(math.cos(angle) * radius, math.sin(angle) * radius);
                if (!em.HasComponent<Framework.Core.Components.Position>(e))
                    em.AddComponentData(e, new Framework.Core.Components.Position { Value = pos });
                if (!em.HasComponent<Framework.Core.Components.TeamId>(e))
                    em.AddComponentData(e, new Framework.Core.Components.TeamId { Value = 2 });
                em.AddComponent<TestTargetTag>(e);
                em.AddComponentData(e, new TestId { Value = i });
                em.AddComponentData(e, new LastHealth { Value = TargetHealth });
                if (VerboseLogging)
                {
                    Debug.Log($"[SpellDamageTest] Spawned target Entity {e.Index}:{e.Version} armor {BaseArmor + i} resist {Mathf.Clamp01(BaseResist + i * 0.02f):0.00}.");
                }
            }

            Debug.Log($"SpellDamageTestHarness initialized: {CasterCount} caster(s), {TargetCount} target(s), primary spell '{PrimarySpellId}'.");
        }

        private void RegisterSpells()
        {
            var frostbite = new DebuffDefinition
            {
                Id = new FixedString64Bytes("frostbite"),
                Flags = DebuffFlags.Slow | DebuffFlags.Root,
                Duration = 4f,
                StackingMode = DebuffStackingMode.Replace,
                StackableCount = 1,
                DurationPolicy = DebuffDurationPolicy.Fixed,
                MaxStacks = 1
            };
            frostbite.StatEffects.Add(new DebuffStatEffect
            {
                StatId = new FixedString32Bytes("MoveSpeed"),
                AdditivePerStack = -6f,
                MultiplierPerStack = 0.5f
            });
            DebuffCatalog.Register(frostbite);

            var weakened = new DebuffDefinition
            {
                Id = new FixedString64Bytes("weakened"),
                Flags = DebuffFlags.Weaken,
                Duration = 8f,
                StackingMode = DebuffStackingMode.CapStacks,
                DurationPolicy = DebuffDurationPolicy.RefreshOnApply,
                MaxStacks = 3
            };
            weakened.StatEffects.Add(new DebuffStatEffect
            {
                StatId = new FixedString32Bytes("AttackPower"),
                AdditivePerStack = -5f,
                MultiplierPerStack = 1f
            });
            DebuffCatalog.Register(weakened);

            SpellTestSampleContent.EnsureRegistered(VerboseLogging);
        }

        private static bool TryMakeFixed(string value, out FixedString64Bytes fixedString)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                fixedString = default;
                return false;
            }

            fixedString = new FixedString64Bytes(value.Trim().ToLowerInvariant());
            return fixedString.Length > 0;
        }

        private static void AddUniqueSpell(List<FixedString64Bytes> list, in FixedString64Bytes id)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(id))
                    return;
            }
            list.Add(id);
        }
    }
}
