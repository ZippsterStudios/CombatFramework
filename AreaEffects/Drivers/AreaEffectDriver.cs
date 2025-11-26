using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.AreaEffects.Drivers
{
    [BurstCompile]
    public static class AreaEffectDriver
    {
        [BurstCompile]
        public static void Spawn(ref EntityManager em, in float2 origin, float radius, float lifetime, ref Entity entity)
        {
            entity = Factory.AreaEffectFactory.SpawnCircle(ref em, default, origin, radius, lifetime);
        }

        [BurstDiscard]
        public static Entity Spawn(ref EntityManager em, in float2 origin, float radius, float lifetime)
        {
            return Factory.AreaEffectFactory.SpawnCircle(ref em, default, origin, radius, lifetime);
        }
    }
}
