using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Contracts.Perception
{
    public struct PerceptionSenseConfig : IComponentData
    {
        public float VisionRange;
        public float VisionFovDeg;
        public float HearingRadius;
        public float MemoryTtlSeconds;
    }

    public struct PerceptionVisibility : IComponentData
    {
        public byte Flags;
    }

    public struct PerceptionSensedTarget : IComponentData
    {
        public Entity Value;
        public float2 LastKnownPos;
        public float LastDistSq;
        public double LastSeenTime;
        public byte VisibleNow;
    }

    public struct PerceptionTargetCandidate : IBufferElementData
    {
        public Entity Target;
        public float Score;
        public float DistanceSq;
        public byte Visible;
    }

    public struct ThreatEntry : IBufferElementData
    {
        public Entity Source;
        public float Threat;
    }

    public struct ThreatTally : IComponentData
    {
        public float DecayPerSecond;
        public float ClampMin;
        public float ClampMax;
    }

    public struct LeashConfig : IComponentData
    {
        public float2 Home;
        public float Radius;
        public float SoftRadius;
    }
}
