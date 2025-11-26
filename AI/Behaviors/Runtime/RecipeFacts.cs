using Unity.Entities;
using Unity.Mathematics;

namespace Framework.AI.Behaviors.Runtime
{
    internal struct RecipeFacts
    {
        public Entity Target;
        public byte HasTarget;
        public byte Visibility;
        public byte HasAgentPosition;
        public byte HasTargetPosition;
        public byte HasLeash;
        public float2 AgentPosition;
        public float2 TargetPosition;
        public float DistanceSq;
        public float HealthPercent;
        public float2 LeashHome;
        public float LeashRadius;
        public float LeashSoftRadius;
    }
}
