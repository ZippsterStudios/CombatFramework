using Unity.Collections;
using Unity.Entities;

namespace Framework.Spells.Pipeline.Components
{
    public struct CastStepParams
    {
        public float Float0;
        public float Float1;
        public int Int0;
        public int Int1;
        public FixedString64Bytes Payload;

        public static CastStepParams Empty => default;
    }

    public struct CastPlanStep : IBufferElementData
    {
        public CastStepType StepType;
        public CastStepParams Params;
    }
}
