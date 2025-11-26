using Framework.Core.Base;
using Framework.Shadow.Components;
using Unity.Burst;
using Unity.Entities;

namespace Framework.Shadow.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(RequestsSystemGroup))]
    public partial struct ShadowManagerSystem : ISystem
    {
        [BurstCompile] public void OnCreate(ref SystemState state) { }
        [BurstCompile] public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;

            foreach (var (refs, regs, unregs, entity) in SystemAPI.Query<
                         DynamicBuffer<ShadowRegionRef>,
                         DynamicBuffer<ShadowRegisterRequest>,
                         DynamicBuffer<ShadowUnregisterRequest>>()
                         .WithEntityAccess())
            {
                // process unregisters
                for (int i = 0; i < unregs.Length; i++)
                {
                    var target = unregs[i].Region;
                    for (int r = refs.Length - 1; r >= 0; r--)
                    {
                        if (refs[r].Region == target)
                            refs.RemoveAt(r);
                    }
                }
                unregs.Clear();

                // process registers
                for (int i = 0; i < regs.Length; i++)
                {
                    var target = regs[i].Region;
                    bool exists = false;
                    for (int r = 0; r < refs.Length; r++)
                    {
                        if (refs[r].Region == target)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                        refs.Add(new ShadowRegionRef { Region = target });
                }
                regs.Clear();
            }
        }
    }
}
