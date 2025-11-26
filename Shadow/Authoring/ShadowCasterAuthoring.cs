using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Framework.Shadow.Components;
using Framework.Shadow.Factory;

namespace Framework.Shadow.Authoring
{
    public class ShadowCasterAuthoring : MonoBehaviour
    {
        [Tooltip("Unique id for this shadow region. Leave empty to auto-generate from object name.")]
        public string ShadowId = string.Empty;
        [Tooltip("Radius of the shadow region (meters).")]
        public float Radius = 4f;
        [Tooltip("Offset from transform position.")]
        public Vector3 Offset = Vector3.zero;
        [Tooltip("Optional team filter; 0 = any.")]
        public byte Team;
        [Tooltip("Highlight color when hovered/targeted.")]
        public Color HighlightColor = new Color(0f, 0f, 0f, 0.6f);

        #if FRAMEWORK_USE_BAKERS
        class Baker : Unity.Entities.Baker<ShadowCasterAuthoring>
        {
            public override void Bake(ShadowCasterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var id = string.IsNullOrWhiteSpace(authoring.ShadowId)
                    ? new FixedString64Bytes(authoring.gameObject.name)
                    : new FixedString64Bytes(authoring.ShadowId);

                var center = authoring.transform.position + authoring.Offset;
                AddComponent(entity, new ShadowRegion
                {
                    Id = id,
                    Center = new float3(center.x, center.y, center.z),
                    Radius = math.max(0.1f, authoring.Radius),
                    Enabled = 1,
                    Team = authoring.Team,
                    Owner = Entity.Null
                });

                AddComponent(entity, new ShadowRegionHighlight
                {
                    Color = new float4(authoring.HighlightColor.r, authoring.HighlightColor.g, authoring.HighlightColor.b, authoring.HighlightColor.a),
                    Intensity = 1f,
                    Enabled = 0
                });

                ShadowUtility.RegisterRegion(ref EntityManager, entity);
            }
        }
        #endif
    }
}
