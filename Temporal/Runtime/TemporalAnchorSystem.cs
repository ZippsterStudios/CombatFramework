using Framework.Temporal.Components;
using Framework.Temporal.Drivers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Temporal.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(Framework.Core.Base.RuntimeSystemGroup))]
    public partial struct TemporalAnchorSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            float dt = SystemAPI.Time.DeltaTime;
            if (dt <= 0f)
                return;

            foreach (var (anchorRW, entity) in SystemAPI.Query<RefRW<TemporalAnchor>>().WithEntityAccess())
            {
                var anchor = anchorRW.ValueRW;
                anchor.Elapsed += dt;
                anchorRW.ValueRW = anchor;

                if (em.HasBuffer<TemporalEvent>(entity))
                {
                    var events = em.GetBuffer<TemporalEvent>(entity);
                    TrimEvents(ref events, anchor.Elapsed - anchor.Retention);
                }

                if (anchor.Elapsed >= anchor.Duration && !HasPendingRelease(ref em, entity))
                {
                    TemporalAnchorDriver.ClearAnchor(ref em, entity);
                }
            }
        }

        private static bool HasPendingRelease(ref EntityManager em, in Entity entity)
        {
            if (!em.HasBuffer<TemporalReleaseRequest>(entity))
                return false;
            return em.GetBuffer<TemporalReleaseRequest>(entity).Length > 0;
        }

        private static void TrimEvents(ref DynamicBuffer<TemporalEvent> events, float minimumTimestamp)
        {
            if (events.Length == 0)
                return;

            if (!events.IsCreated)
                return;

            if (minimumTimestamp <= 0f)
                return;

            int removeCount = 0;
            for (int i = 0; i < events.Length; i++)
            {
                if (events[i].Timestamp >= minimumTimestamp)
                    break;
                removeCount++;
            }

            if (removeCount > 0)
            {
                events.RemoveRange(0, removeCount);
            }
        }
    }
}
