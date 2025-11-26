using Framework.Spells.Content;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Spells.Runtime
{
    internal static class EffectValueCalculator
    {
        public static float Resolve(float baseValue, in EffectScaling scaling, ref EffectExecutionContext ctx, in Entity target, int blockIndex)
        {
            float value = baseValue;

            if (scaling.AttributeSource == ScalingSourceKind.Attribute && scaling.AttributeCoefficient != 0f && scaling.AttributeId.Length > 0)
            {
                var source = scaling.AttributeFromCaster != 0 ? ctx.Caster : target;
                value += ResolveAttribute(ref ctx.EntityManager, source, scaling.AttributeId) * scaling.AttributeCoefficient;
            }

            if (scaling.LevelCoefficient != 0f)
            {
                int level = ctx.Metadata.CategoryLevel > 0 ? ctx.Metadata.CategoryLevel : ctx.Metadata.SpellLevel;
                value += scaling.LevelCoefficient * level;
            }

            if (scaling.UsePreviousResult != 0)
            {
                value += ctx.Results.ResolveRelative(blockIndex, scaling.PreviousBlockOffset, scaling.ResultSource) * scaling.ResultCoefficient;
            }

            if (scaling.ClampMin != 0f || scaling.ClampMax != 0f)
            {
                float min = scaling.ClampMin;
                float max = scaling.ClampMax == 0f ? float.MaxValue : scaling.ClampMax;
                value = math.clamp(value, min, max);
            }

            return value;
        }

        private static float ResolveAttribute(ref EntityManager em, in Entity entity, in FixedString64Bytes attributeId)
        {
            if (entity == Entity.Null)
                return 0f;

            if (em.HasComponent<Framework.Stats.Components.StatValue>(entity))
            {
                var stat = em.GetComponentData<Framework.Stats.Components.StatValue>(entity);
                return stat.Value != 0f ? stat.Value : stat.BaseValue + stat.Additive;
            }

            return 0f;
        }
    }
}
