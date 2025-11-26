using Framework.Cooldowns.Factory;
using Framework.Cooldowns.Requests;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Cooldowns.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.RequestsSystemGroup))]
    public partial struct CooldownRuntimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            using var pending = new NativeList<PendingCooldown>(Allocator.Temp);
            foreach (var (buffer, entity) in SystemAPI.Query<DynamicBuffer<CooldownRequest>>().WithEntityAccess())
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    var req = buffer[i];
                    pending.Add(new PendingCooldown
                    {
                        Target = req.Target,
                        ReadyTime = req.Now + req.Cooldown
                    });
                }
                buffer.Clear();
            }

            for (int i = 0; i < pending.Length; i++)
            {
                var request = pending[i];
                if (!em.Exists(request.Target))
                    continue;
                FixedString64Bytes groupId = default;
                CooldownFactory.ApplyCooldown(ref em, request.Target, groupId, request.ReadyTime);
            }
        }

        private struct PendingCooldown
        {
            public Entity Target;
            public double ReadyTime;
        }
    }
}
