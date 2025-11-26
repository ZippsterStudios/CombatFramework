using Framework.AI.Components;
using Unity.Burst;
using Unity.Entities;

namespace Framework.AI.Drivers
{
    [BurstCompile]
    public static class AIDriver
    {
        [BurstCompile]
        public static void SetState(ref EntityManager em, in Entity agent, int desiredState)
        {
            if (!em.HasComponent<AIState>(agent))
                em.AddComponentData(agent, new AIState { Current = desiredState });
            else
            {
                var s = em.GetComponentData<AIState>(agent);
                s.Current = desiredState;
                em.SetComponentData(agent, s);
            }
        }
    }
}
