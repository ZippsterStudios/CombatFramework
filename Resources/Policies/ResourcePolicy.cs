using Unity.Burst;
using Unity.Entities;

namespace Framework.Resources.Policies
{
    [BurstCompile]
    public static class ResourcePolicy
    {
        public enum Result
        {
            Allow,
            Reject_Insufficient
        }

        public static Result CanAffordHealth(in EntityManager em, in Entity e, int cost)
        {
            if (!em.HasComponent<Components.Health>(e)) return Result.Reject_Insufficient;
            var h = em.GetComponentData<Components.Health>(e);
            return h.Current >= cost ? Result.Allow : Result.Reject_Insufficient;
        }

        public static Result CanAffordMana(in EntityManager em, in Entity e, int cost)
        {
            if (!em.HasComponent<Components.Mana>(e)) return Result.Reject_Insufficient;
            var m = em.GetComponentData<Components.Mana>(e);
            return m.Current >= cost ? Result.Allow : Result.Reject_Insufficient;
        }

        public static Result CanAffordStamina(in EntityManager em, in Entity e, int cost)
        {
            if (!em.HasComponent<Components.Stamina>(e)) return Result.Reject_Insufficient;
            var s = em.GetComponentData<Components.Stamina>(e);
            return s.Current >= cost ? Result.Allow : Result.Reject_Insufficient;
        }
    }
}
