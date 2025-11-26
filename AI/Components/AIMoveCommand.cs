using Framework.Contracts.Intents;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.AI.Components
{
    /// <summary>
    /// Legacy shim retained so downstream modules that still reference AIMoveCommand continue to compile.
    /// Prefer the contract-driven MoveIntent component instead.
    /// </summary>
    [System.Obsolete("AIMoveCommand has been superseded by Framework.Contracts.Intents.MoveIntent.")]
    public struct AIMoveCommand : IComponentData
    {
        public AIMoveMode Mode;
        public float2 Destination;
        public float Speed;
        public byte Active;

        public void Clear()
        {
            Mode = AIMoveMode.None;
            Destination = float2.zero;
            Speed = 0f;
            Active = 0;
        }

        internal static AIMoveCommand From(in MoveIntent intent)
        {
            return new AIMoveCommand
            {
                Mode = (AIMoveMode)intent.Mode,
                Destination = intent.Destination,
                Speed = intent.Speed,
                Active = intent.Active
            };
        }

        internal MoveIntent ToIntent()
        {
            return new MoveIntent
            {
                Mode = (byte)Mode,
                Destination = Destination,
                Speed = Speed,
                Active = Active
            };
        }
    }
}
