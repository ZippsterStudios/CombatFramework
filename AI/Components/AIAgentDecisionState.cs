using Unity.Entities;

namespace Framework.AI.Components
{
    public struct AIAgentDecisionState : IComponentData
    {
        public double NextDecisionTime;

        public static AIAgentDecisionState CreateDefault() => new AIAgentDecisionState
        {
            NextDecisionTime = 0d
        };
    }
}

