using Unity.Entities;

namespace Framework.Spells.Pipeline.Components
{
    public struct SpellCastContext : IComponentData
    {
        public int StepIndex;
        public CastStepType CurrentStep;
        public float StepTimer;
        public float StepDuration;
        public CastContextFlags Flags;
        public CastTerminationReason Termination;

        public bool IsTerminal => (Flags & CastContextFlags.Terminal) != 0;

        public void RequestAdvance()
        {
            StepIndex++;
            StepTimer = 0f;
            StepDuration = 0f;
            CurrentStep = CastStepType.None;
        }
    }
}
