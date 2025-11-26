using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.AreaEffects.Factory
{
    public static class AreaEffectFactory
    {
        public struct AreaEffectInstance : IComponentData
        {
            public FixedString64Bytes Id;
            public float Lifetime;
            public float2 Center;
            public float Radius; // circle
        }

        public static Entity SpawnCircle(ref EntityManager em, in FixedString64Bytes id, float2 center, float radius, float lifetime)
        {
            var e = em.CreateEntity();
            em.AddComponentData(e, new AreaEffectInstance { Id = id, Lifetime = lifetime, Center = center, Radius = radius });
            return e;
        }
    }
}

