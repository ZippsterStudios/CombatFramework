using Unity.Entities;

namespace Framework.Resources.Factory
{
    public static class ResourceFactory
    {
        public static void InitHealth(ref EntityManager em, in Entity e, int max, int current = -1, int regenPerSecond = 0)
        {
            if (current < 0) current = max;
            if (!em.HasComponent<Components.Health>(e))
                em.AddComponentData(e, new Components.Health { Current = current, Max = max, RegenPerSecond = regenPerSecond, RegenAccumulator = 0 });
            else em.SetComponentData(e, new Components.Health { Current = current, Max = max, RegenPerSecond = regenPerSecond, RegenAccumulator = 0 });
        }

        public static void ApplyHealthDelta(ref EntityManager em, in Entity e, int delta)
        {
            if (!em.HasComponent<Components.Health>(e))
                em.AddComponentData(e, new Components.Health { Current = 0, Max = 0, RegenPerSecond = 0, RegenAccumulator = 0 });
            var h = em.GetComponentData<Components.Health>(e);
            long next = (long)h.Current + delta;
            if (next < 0) next = 0; if (next > h.Max) next = h.Max;
            h.Current = (int)next;
            em.SetComponentData(e, h);
        }

        public static void InitMana(ref EntityManager em, in Entity e, int max, int current = -1, int regenPerSecond = 0)
        {
            if (current < 0) current = max;
            if (!em.HasComponent<Components.Mana>(e))
                em.AddComponentData(e, new Components.Mana { Current = current, Max = max, RegenPerSecond = regenPerSecond, RegenAccumulator = 0 });
            else em.SetComponentData(e, new Components.Mana { Current = current, Max = max, RegenPerSecond = regenPerSecond, RegenAccumulator = 0 });
        }

        public static void ApplyManaDelta(ref EntityManager em, in Entity e, int delta)
        {
            if (!em.HasComponent<Components.Mana>(e))
                em.AddComponentData(e, new Components.Mana { Current = 0, Max = 0, RegenPerSecond = 0, RegenAccumulator = 0 });
            var m = em.GetComponentData<Components.Mana>(e);
            long next = (long)m.Current + delta;
            if (next < 0) next = 0; if (next > m.Max) next = m.Max;
            m.Current = (int)next;
            em.SetComponentData(e, m);
        }

        public static void InitStamina(ref EntityManager em, in Entity e, int max, int current = -1, int regenPerSecond = 0)
        {
            if (current < 0) current = max;
            if (!em.HasComponent<Components.Stamina>(e))
                em.AddComponentData(e, new Components.Stamina { Current = current, Max = max, RegenPerSecond = regenPerSecond, RegenAccumulator = 0 });
            else em.SetComponentData(e, new Components.Stamina { Current = current, Max = max, RegenPerSecond = regenPerSecond, RegenAccumulator = 0 });
        }

        public static void ApplyStaminaDelta(ref EntityManager em, in Entity e, int delta)
        {
            if (!em.HasComponent<Components.Stamina>(e))
                em.AddComponentData(e, new Components.Stamina { Current = 0, Max = 0, RegenPerSecond = 0, RegenAccumulator = 0 });
            var s = em.GetComponentData<Components.Stamina>(e);
            long next = (long)s.Current + delta;
            if (next < 0) next = 0; if (next > s.Max) next = s.Max;
            s.Current = (int)next;
            em.SetComponentData(e, s);
        }
    }
}

