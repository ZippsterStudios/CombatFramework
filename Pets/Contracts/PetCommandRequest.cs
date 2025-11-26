using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Pets.Contracts
{
    public enum PetCommand : byte
    {
        Follow,
        Guard,
        Patrol,
        Stay,
        Sit,
        BackOff,
        Attack,
        Dismiss
    }

    public struct PetCommandRequest : IBufferElementData
    {
        public PetCommand Command;
        public FixedString32Bytes Group;
        public Entity Pet;
        public Entity Target;
        public float3 Waypoint;
        public byte AppendWaypoint;
    }
}
