using Unity.Entities;

namespace Framework.Contracts.AI
{
    public struct AIAgentTarget : IComponentData
    {
        public Entity Value;
        public float LastSeenDistanceSq;
        public byte Visibility;

        public static AIAgentTarget CreateDefault()
        {
            return new AIAgentTarget
            {
                Value = Entity.Null,
                LastSeenDistanceSq = 0f,
                Visibility = 0
            };
        }
    }
}
