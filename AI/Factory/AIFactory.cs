using Framework.AI.Components;
using Framework.AI.Runtime;
using Framework.Contracts.Intents;
using Unity.Entities;
using ContractsAIAgentTarget = Framework.Contracts.AI.AIAgentTarget;

namespace Framework.AI.Factory
{
    public static class AIFactory
    {
        public static void SpawnAgent(ref EntityManager em, out Entity e)
        {
            e = em.CreateEntity();
            em.AddComponentData(e, new AIState { Current = AIStateIds.Idle });
            em.AddComponentData(e, AIAgentBehaviorConfig.CreateDefaults());
            em.AddComponentData(e, AIAgentDecisionState.CreateDefault());
            em.AddComponentData(e, AICombatRuntime.CreateDefault());
            em.AddComponentData(e, ContractsAIAgentTarget.CreateDefault());
            em.AddComponentData(e, new MoveIntent());
            em.AddComponentData(e, new CastIntent());
            em.AddComponent<AIBehaviorEnabledTag>(e);
            if (!em.HasBuffer<StateChangeRequest>(e))
                em.AddBuffer<StateChangeRequest>(e);
        }
    }
}
