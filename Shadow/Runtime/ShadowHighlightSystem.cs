using Framework.Core.Base;
using Framework.Shadow.Components;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Shadow.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(RuntimeSystemGroup))]
    public partial struct ShadowHighlightSystem : ISystem
    {
        [BurstCompile] public void OnCreate(ref SystemState state) { }
        [BurstCompile] public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;

            foreach (var (hover, refs) in SystemAPI.Query<ShadowHoverState, DynamicBuffer<ShadowRegionRef>>())
            {
                for (int i = 0; i < refs.Length; i++)
                {
                    var region = refs[i].Region;
                    if (!em.Exists(region) || !em.HasComponent<ShadowRegionHighlight>(region))
                        continue;

                    var highlight = em.GetComponentData<ShadowRegionHighlight>(region);
                    highlight.Enabled = region == hover.Region ? (byte)1 : (byte)0;
                    em.SetComponentData(region, highlight);
                }
            }
        }
    }
}
