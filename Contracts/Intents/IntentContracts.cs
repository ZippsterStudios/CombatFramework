using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.Contracts.Intents
{
    public enum AIMoveMode : byte
    {
        None = 0,
        Idle = 1,
        Chase = 2,
        StrafeLeft = 3,
        StrafeRight = 4,
        Backstep = 5,
        Flee = 6
    }

    public struct MoveIntent : IComponentData
    {
        public byte Mode;
        public float2 Destination;
        public float Speed;
        public byte Active;

        public static MoveIntent Cleared => default;

        public void Clear()
        {
            this = default;
        }
    }

    public struct CastIntent : IComponentData
    {
        public FixedString64Bytes SpellId;
        public Entity Target;
        public byte Active;

        public void Clear()
        {
            SpellId = default;
            Target = Entity.Null;
            Active = 0;
        }
    }

    public struct StateChangeRequest : IBufferElementData
    {
        public Entity Agent;
        public int DesiredState;
    }
}
