using Unity.Entities;

namespace Framework.Threat.Factory
{
    public static class ThreatFactory
    {
        public static void EnsureBuffer(ref EntityManager em, in Entity e)
        {
            if (!em.HasBuffer<Requests.ThreatRequest>(e)) em.AddBuffer<Requests.ThreatRequest>(e);
        }

        public static void AddThreat(ref EntityManager em, in Entity e, in Entity source, int amount)
        {
            EnsureBuffer(ref em, e);
            var buf = em.GetBuffer<Requests.ThreatRequest>(e);
            buf.Add(new Requests.ThreatRequest { Target = e, Delta = amount });
        }
    }
}
