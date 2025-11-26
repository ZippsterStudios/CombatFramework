using Framework.DOT.Components;
using Framework.DOT.Factory;
using Unity.Collections;
using Unity.Burst;
using Unity.Entities;

namespace Framework.DOT.Drivers
{
    [BurstCompile]
    public static class DotDriver
    {
        [BurstCompile]
        public static void Apply(ref EntityManager em, in Entity target, in FixedString64Bytes effectId, int dps, float interval, float duration, in Entity source)
        {
            DotFactory.Enqueue(ref em, target, effectId, dps, interval, duration, source);
        }
    }
}
