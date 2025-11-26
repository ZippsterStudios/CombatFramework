using System;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Spells.Content
{
    [Flags]
    public enum SpellDefinitionFlags : ushort
    {
        None = 0,
        Channeled = 1 << 0,
        AllowPartialRefund = 1 << 1,
        IgnoreSilence = 1 << 2,
        IgnoreInterrupts = 1 << 3
    }

    public struct SpellDefinitionBlob
    {
        public FixedString64Bytes Id;
        public FixedString32Bytes CategoryId;
        public int CategoryLevel;
        public int SpellLevel;
        public SpellRank Rank;
        public SpellDefinitionFlags Flags;
        public float CastTime;
        public float Cooldown;
        public float InterruptChargePercentOverride;
        public float FizzleChargePercentOverride;
        public BlobArray<SpellCost> Costs;
        public BlobArray<EffectBlockBlob> Blocks;
    }

    public struct EffectBlockBlob
    {
        public TargetScope Scope;
        public EffectPayloadBlob Payload;
        public EffectConditions Conditions;
        public EffectTiming Timing;
        public EffectScaling Scaling;
    }

    public struct EffectPayloadBlob
    {
        public EffectPayloadKind Kind;
        public DamagePayload Damage;
        public HealPayload Heal;
        public ApplyEffectPayload Apply;
        public ScriptPayload Script;
        public DotHotPayload OverTime;
        public SummonPayload Summon;
        public AreaEffectPayload Area;
        public BlobArray<StatOperationBlob> StatOps;
    }

    public struct StatOperationBlob
    {
        public FixedString64Bytes StatId;
        public StatOperationKind Operation;
        public float Value;
        public int DurationMs;
        public FixedString64Bytes StackingKey;
        public StatOperationStackingPolicy StackingPolicy;
    }

    public static class SpellDefinitionBlobUtility
    {
        public static BlobAssetReference<SpellDefinitionBlob> Create(in SpellDefinition def, Allocator allocator = Allocator.Persistent)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<SpellDefinitionBlob>();
            root.Id = def.Id;
            root.CategoryId = def.CategoryId;
            root.CategoryLevel = def.CategoryLevel;
            root.SpellLevel = def.SpellLevel;
            root.Rank = def.Rank;
            root.CastTime = def.CastTime;
            root.Cooldown = def.Cooldown;
            root.Flags = def.Flags;
            root.InterruptChargePercentOverride = def.InterruptChargePercentOverride;
            root.FizzleChargePercentOverride = def.FizzleChargePercentOverride;

            var costArray = builder.Allocate(ref root.Costs, def.Costs?.Length ?? 0);
            if (def.Costs != null)
            {
                for (int i = 0; i < def.Costs.Length; i++)
                    costArray[i] = def.Costs[i];
            }

            var blocks = EffectBlockConverter.Resolve(in def);
            var blockArray = builder.Allocate(ref root.Blocks, blocks.Length);
            for (int i = 0; i < blocks.Length; i++)
                EffectPayloadBlobWriter.CopyBlock(ref builder, in blocks[i], ref blockArray[i]);

            var blob = builder.CreateBlobAssetReference<SpellDefinitionBlob>(allocator);
            builder.Dispose();
            return blob;
        }
    }
}
