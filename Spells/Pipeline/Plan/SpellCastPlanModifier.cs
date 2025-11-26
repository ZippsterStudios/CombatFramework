using Unity.Entities;

using Framework.Spells.Pipeline.Components;

namespace Framework.Spells.Pipeline.Plan
{
    public enum CastPlanOperation : byte
    {
        Append,
        InsertBefore,
        InsertAfter,
        Remove
    }

    public struct SpellCastPlanModifier : IBufferElementData
    {
        public CastPlanOperation Operation;
        public CastStepType StepType;
        public CastStepType ReferenceStep;
        public CastStepParams Params;
    }
}
