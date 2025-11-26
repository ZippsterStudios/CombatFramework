using System;

namespace Framework.Spells.Pipeline.Components
{
    public enum CastStepType : byte
    {
        None = 0,
        Validate = 1,
        Afford = 2,
        Spend = 3,
        Windup = 4,
        InterruptCheck = 5,
        FizzleCheck = 6,
        Apply = 7,
        Cleanup = 8
    }

    [Flags]
    public enum CastContextFlags : ushort
    {
        None = 0,
        Began = 1 << 0,
        Validated = 1 << 1,
        CostsCovered = 1 << 2,
        CostsSpent = 1 << 3,
        WindupComplete = 1 << 4,
        Interrupted = 1 << 5,
        Fizzled = 1 << 6,
        Resolved = 1 << 7,
        Terminal = 1 << 8,
        CleanupQueued = 1 << 9,
        PartialRefundAllowed = 1 << 10
    }

    public enum CastTerminationReason : byte
    {
        None = 0,
        InvalidSpell,
        NotInSpellbook,
        CannotAfford,
        Interrupted,
        Fizzled,
        Applied
    }
}
