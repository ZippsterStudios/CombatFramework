using Framework.Heal.Requests;
using Unity.Entities;

namespace Framework.Heal.Factory
{
    public static class HealFactory
    {
        public static void EnqueueHeal(ref EntityManager em, in Entity target, int amount)
        {
            if (!em.HasBuffer<HealRequest>(target))
                em.AddBuffer<HealRequest>(target);
            em.GetBuffer<HealRequest>(target).Add(new HealRequest { Target = target, Amount = amount });
        }
    }
}
