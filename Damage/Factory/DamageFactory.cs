using Framework.Damage.Components;
using Framework.Damage.Requests;
using Unity.Entities;

namespace Framework.Damage.Factory
{
    public static class DamageFactory
    {
        public static void EnqueueDamage(ref EntityManager em, in Entity target, in DamagePacket packet)
        {
            if (!em.HasBuffer<DamageRequest>(target))
                em.AddBuffer<DamageRequest>(target);
            var buf = em.GetBuffer<DamageRequest>(target);
            buf.Add(new DamageRequest { Target = target, Packet = packet });
        }
    }
}
