using Framework.HOT.Components;
using Framework.HOT.Factory;
using Unity.Collections;
using Unity.Burst;
using Unity.Entities;

namespace Framework.HOT.Drivers
{
    [BurstCompile]
    public static class HotDriver
    {
        [BurstCompile]
        public static void Apply(ref EntityManager em, in Entity target, in FixedString64Bytes effectId, int hps, float interval, float duration, in Entity source)
        {
            HotFactory.Enqueue(ref em, target, effectId, hps, interval, duration, source);
        }
    }
}
