using Unity.Collections;
using Unity.Entities;

using Framework.Spells.Pipeline.Components;
using Framework.Spells.Pipeline.Events;

namespace Framework.Spells.Pipeline.Systems
{
    static class SpellCastEventUtility
    {
        public static void Emit<TEvent>(EntityCommandBuffer ecb, in Entity castEntity, in SpellCastData data, FixedString64Bytes reason = default, float value = 0f)
            where TEvent : unmanaged, IComponentData
        {
            var evt = ecb.CreateEntity();
            ecb.AddComponent<TEvent>(evt);
            var payload = ecb.AddBuffer<SpellCastEventPayload>(evt);
            payload.Add(new SpellCastEventPayload
            {
                CastEntity = castEntity,
                Caster = data.Caster,
                Target = data.Target,
                SpellId = data.SpellId,
                Reason = reason,
                Value = value
            });
        }
    }
}
