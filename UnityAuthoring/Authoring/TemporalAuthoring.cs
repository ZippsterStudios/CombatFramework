using UnityEngine;
using Unity.Entities;

namespace Framework.UnityAuthoring.Authoring
{
    public class TemporalAuthoring : MonoBehaviour
    {
        [Range(0f, 1f)] public float HastePercent = 0f;
        [Range(0f, 1f)] public float SlowPercent = 0f;

        #if FRAMEWORK_USE_BAKERS
        class Baker : Unity.Entities.Baker<TemporalAuthoring>
        {
            public override void Bake(TemporalAuthoring a)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                if (a.HastePercent > 0f || a.SlowPercent > 0f)
                {
                    AddComponent(entity, new Framework.Temporal.Components.TemporalModifiers
                    {
                        HastePercent = a.HastePercent,
                        SlowPercent = a.SlowPercent
                    });
                }
            }
        }
        #endif
    }
}
