using Unity.Entities;

namespace Framework.Stats.Factory
{
    public static class StatFactory
    {
        public static void ApplyAdditive(ref EntityManager em, in Entity e, int additiveDelta)
        {
            if (!em.HasComponent<Components.StatValue>(e)) return;
            var c = em.GetComponentData<Components.StatValue>(e);
            c.Additive += additiveDelta;
            c.Value = (c.BaseValue + c.Additive) * (c.Multiplier == 0f ? 1f : c.Multiplier);
            em.SetComponentData(e, c);
        }

        public static void SetBase(ref EntityManager em, in Entity e, float baseValue)
        {
            if (!em.HasComponent<Components.StatValue>(e))
            {
                em.AddComponentData(e, new Components.StatValue
                {
                    BaseValue = baseValue,
                    Additive = 0,
                    Multiplier = 1f,
                    Value = baseValue
                });
                return;
            }

            var c = em.GetComponentData<Components.StatValue>(e);
            c.BaseValue = baseValue;
            c.Value = (c.BaseValue + c.Additive) * (c.Multiplier == 0f ? 1f : c.Multiplier);
            em.SetComponentData(e, c);
        }

        public static void SetMultiplier(ref EntityManager em, in Entity e, float multiplier)
        {
            if (!em.HasComponent<Components.StatValue>(e)) return;
            var c = em.GetComponentData<Components.StatValue>(e);
            c.Multiplier = multiplier <= 0f ? 1f : multiplier;
            c.Value = (c.BaseValue + c.Additive) * c.Multiplier;
            em.SetComponentData(e, c);
        }
    }
}
