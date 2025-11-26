using Unity.Entities;

namespace Framework.Spells.Content
{
    internal static class EffectPayloadBlobWriter
    {
        public static void CopyBlock(ref BlobBuilder builder, in EffectBlock src, ref EffectBlockBlob dst)
        {
            dst.Scope = src.Scope;
            dst.Conditions = src.Conditions;
            dst.Timing = src.Timing;
            dst.Scaling = src.Scaling;
            CopyPayload(ref builder, in src.Payload, ref dst.Payload);
        }

        public static void CopyPayload(ref BlobBuilder builder, in EffectPayload src, ref EffectPayloadBlob dst)
        {
            dst.Kind = src.Kind;
            dst.Damage = src.Damage;
            dst.Heal = src.Heal;
            dst.Apply = src.Apply;
            dst.Script = src.Script;
            dst.OverTime = src.OverTime;
            dst.Summon = src.Summon;
            dst.Area = src.Area;

            var ops = src.StatOps.Operations;
            var length = ops?.Length ?? 0;
            var allocated = builder.Allocate(ref dst.StatOps, length);
            for (int i = 0; i < length; i++)
            {
                allocated[i] = new StatOperationBlob
                {
                    StatId = ops[i].StatId,
                    Operation = ops[i].Operation,
                    Value = ops[i].Value,
                    DurationMs = ops[i].DurationMs,
                    StackingKey = ops[i].StackingKey,
                    StackingPolicy = ops[i].StackingPolicy
                };
            }
        }
    }
}
