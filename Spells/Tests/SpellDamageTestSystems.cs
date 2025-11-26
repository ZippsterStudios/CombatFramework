using System.Text;
using Framework.Spells.Content;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

namespace Framework.Spells.Tests
{
    public struct TestCasterTag : IComponentData { }
    public struct TestTargetTag : IComponentData { }
    public struct TestId : IComponentData { public int Value; }
    public struct LastHealth : IComponentData { public int Value; }

    public struct SpellDamageTestConfig : IComponentData
    {
        public FixedString64Bytes SpellId;
        public float Interval;
        public float Accumulator;
        public int NextTargetIndex;
        public int NextSpellIndex;
        public byte UseSequence;
        public byte VerboseLogging;
        public FixedList512Bytes<FixedString64Bytes> Sequence;
    }

    [UpdateInGroup(typeof(Framework.Core.Base.RuntimeSystemGroup))]
    public partial struct SpellDamageTestSystem : ISystem
    {
        public void OnCreate(ref SystemState s) { }
        public void OnDestroy(ref SystemState s) { }

        public void OnUpdate(ref SystemState s)
        {
            if (!SystemAPI.HasSingleton<SpellDamageTestConfig>())
                return;

            var em = s.EntityManager;
            var cfgEntity = SystemAPI.GetSingletonEntity<SpellDamageTestConfig>();
            var cfg = em.GetComponentData<SpellDamageTestConfig>(cfgEntity);
            cfg.Accumulator += SystemAPI.Time.DeltaTime;
            if (cfg.Accumulator < cfg.Interval)
            {
                em.SetComponentData(cfgEntity, cfg);
                return;
            }
            using var casters = SystemAPI.QueryBuilder().WithAll<TestCasterTag>().Build().ToEntityArray(Allocator.Temp);
            using var targets = SystemAPI.QueryBuilder().WithAll<TestTargetTag>().Build().ToEntityArray(Allocator.Temp);

            if (casters.Length == 0 || targets.Length == 0)
            {
                cfg.Accumulator = 0f;
                em.SetComponentData(cfgEntity, cfg);
                return;
            }

            var sequence = cfg.Sequence;
            bool hasSequence = cfg.UseSequence != 0 && sequence.Length > 0;

            for (int i = 0; i < casters.Length; i++)
            {
                var caster = casters[i];
                var target = targets[cfg.NextTargetIndex % targets.Length];

                var spellId = cfg.SpellId;
                if (hasSequence)
                {
                    spellId = sequence[cfg.NextSpellIndex % sequence.Length];
                    cfg.NextSpellIndex++;
                    cfg.SpellId = spellId;
                }

                if (cfg.VerboseLogging != 0)
                {
                    if (SpellDefinitionCatalog.TryGet(spellId, out var def))
                    {
                        Debug.Log(BuildVerboseMessage(ref em, caster, target, spellId, ref def));
                    }
                    else
                    {
                        Debug.LogWarning($"[SpellDamageTest] Spell '{spellId.ToString()}' not found in catalog.");
                    }
                }

                Framework.Spells.Factory.SpellPipelineFactory.Cast(ref em, caster, target, spellId, 0);
                cfg.NextTargetIndex++;
            }

            cfg.Accumulator -= cfg.Interval;
            em.SetComponentData(cfgEntity, cfg);
        }

        private static string BuildVerboseMessage(ref EntityManager em, in Entity caster, in Entity target, in FixedString64Bytes spellId, ref SpellDefinition def)
        {
            var sb = new StringBuilder(256);
            sb.Append("[SpellDamageTest] Cast ");
            sb.Append(spellId.ToString());
            sb.Append(" caster=");
            sb.Append(FormatEntity(caster));
            sb.Append(" target=");
            sb.Append(FormatEntity(target));

            if (em.HasComponent<Framework.Core.Components.Position>(caster) && em.HasComponent<Framework.Core.Components.Position>(target))
            {
                var cPos = em.GetComponentData<Framework.Core.Components.Position>(caster).Value;
                var tPos = em.GetComponentData<Framework.Core.Components.Position>(target).Value;
                float dist = math.distance(cPos, tPos);
                sb.Append($" dist={dist:0.0}");
            }

            if (em.HasComponent<Framework.Resources.Components.Health>(target))
            {
                var health = em.GetComponentData<Framework.Resources.Components.Health>(target);
                sb.Append($" targetHP={health.Current}/{health.Max}");
            }

            sb.Append(" effectBlocks=");
            if (def.Blocks == null || def.Blocks.Length == 0)
            {
                sb.Append("<none>");
            }
            else
            {
                for (int i = 0; i < def.Blocks.Length; i++)
                {
                    if (i > 0) sb.Append(", ");
                    var block = def.Blocks[i];
                    sb.Append(block.Payload.Kind);
                    switch (block.Payload.Kind)
                    {
                        case EffectPayloadKind.Damage:
                            sb.Append($"({block.Payload.Damage.Amount})");
                            break;
                        case EffectPayloadKind.Heal:
                            sb.Append($"(+{block.Payload.Heal.Amount})");
                            break;
                        case EffectPayloadKind.SpawnDot:
                        case EffectPayloadKind.SpawnHot:
                            if (block.Payload.OverTime.Id.Length > 0)
                                sb.Append($"[{block.Payload.OverTime.Id}]");
                            break;
                        case EffectPayloadKind.ApplyBuff:
                        case EffectPayloadKind.ApplyDebuff:
                            if (block.Payload.Apply.Id.Length > 0)
                                sb.Append($"[{block.Payload.Apply.Id}]");
                            break;
                        case EffectPayloadKind.ScriptReference:
                            sb.Append($"[{block.Payload.Script.FeatureId}]");
                            break;
                    }
                    if (block.Scope.Kind == TargetScopeKind.Radius && block.Scope.Radius > 0f)
                        sb.Append($"@r{block.Scope.Radius:0.0}");
                    if (block.Scope.Kind == TargetScopeKind.ChainJump && block.Scope.Chain.MaxJumps > 0)
                        sb.Append($"~chain{block.Scope.Chain.MaxJumps}");
                }
            }

            return sb.ToString();
        }

        private static string FormatEntity(in Entity e) => $"{e.Index}:{e.Version}";
    }

    [UpdateInGroup(typeof(Framework.Core.Base.TelemetrySystemGroup))]
    public partial struct SpellDamageTestLogSystem : ISystem
    {
        public void OnCreate(ref SystemState s) { }
        public void OnDestroy(ref SystemState s) { }

        public void OnUpdate(ref SystemState s)
        {
            var em = s.EntityManager;
            using var targets = SystemAPI.QueryBuilder().WithAll<TestTargetTag, Framework.Resources.Components.Health>().Build().ToEntityArray(Allocator.Temp);

            var toAdd = new NativeList<Entity>(Allocator.Temp);
            var toUpdate = new NativeList<(Entity e, int cur, int last)>(Allocator.Temp);

            for (int i = 0; i < targets.Length; i++)
            {
                var e = targets[i];
                var cur = em.GetComponentData<Framework.Resources.Components.Health>(e).Current;
                if (!em.HasComponent<LastHealth>(e))
                {
                    toAdd.Add(e);
                    continue;
                }
                var last = em.GetComponentData<LastHealth>(e).Value;
                if (last != cur)
                {
                    toUpdate.Add((e, cur, last));
                }
            }

            for (int i = 0; i < toAdd.Length; i++)
            {
                var e = toAdd[i];
                var cur = em.GetComponentData<Framework.Resources.Components.Health>(e).Current;
                em.AddComponentData(e, new LastHealth { Value = cur });
            }
            for (int i = 0; i < toUpdate.Length; i++)
            {
                var tup = toUpdate[i];
                int id = em.HasComponent<TestId>(tup.e) ? em.GetComponentData<TestId>(tup.e).Value : -1;
                int delta = tup.cur - tup.last;
                UnityEngine.Debug.Log($"[SpellDamageTest] Target#{id} Health {tup.last} -> {tup.cur} (delta {delta})");
                em.SetComponentData(tup.e, new LastHealth { Value = tup.cur });
            }
            toAdd.Dispose();
            toUpdate.Dispose();
        }
    }
}
