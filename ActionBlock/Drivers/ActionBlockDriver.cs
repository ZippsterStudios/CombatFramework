using Framework.ActionBlock.Components;
using Unity.Burst;
using Unity.Entities;

namespace Framework.ActionBlock.Drivers
{
    [BurstCompile]
    public static class ActionBlockDriver
    {
        [BurstCompile]
        public static void EnsureMask(ref EntityManager em, in Entity entity)
        {
            if (!em.Exists(entity))
                return;
            if (!em.HasComponent<ActionBlockMask>(entity))
                em.AddComponentData(entity, new ActionBlockMask());
        }

        [BurstCompile]
        public static void Block(ref EntityManager em, in Entity entity, ActionKind kind)
        {
            if (!em.Exists(entity))
                return;
            if (!em.HasComponent<ActionBlockMask>(entity))
                em.AddComponentData(entity, new ActionBlockMask());

            var mask = em.GetComponentData<ActionBlockMask>(entity);
            ActionBits.Set(ref mask, kind);
            em.SetComponentData(entity, mask);
        }

        [BurstCompile]
        public static void Unblock(ref EntityManager em, in Entity entity, ActionKind kind)
        {
            if (!em.Exists(entity) || !em.HasComponent<ActionBlockMask>(entity))
                return;

            var mask = em.GetComponentData<ActionBlockMask>(entity);
            ActionBits.Clear(ref mask, kind);
            em.SetComponentData(entity, mask);
        }
    }
}

