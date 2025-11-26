using Unity.Burst;
using Unity.Entities;
using Framework.Core.Base;

namespace Framework.Cooldowns.Resolution
{
    [BurstCompile]
    [UpdateInGroup(typeof(ResolutionSystemGroup))]
    public partial struct CooldownResolutionSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            double now = SystemAPI.Time.ElapsedTime;
            foreach (var (buf, entity) in SystemAPI.Query<DynamicBuffer<Components.CooldownGroup>>().WithEntityAccess())
            {
                var b = buf;
                for (int i = b.Length - 1; i >= 0; i--)
                {
                    if (now >= b[i].ReadyTime)
                        b.RemoveAt(i);
                }
            }
        }
    }
}
