using Unity.Collections;
using Unity.Entities;

namespace Framework.Spells.Sustain
{
    /// <summary>
    /// Tracks an active sustain drain on a caster. Each entry is debited on a cadence until removed.
    /// </summary>
    public struct SustainedSpellDrain : IBufferElementData
    {
        public FixedString64Bytes SpellId;
        public FixedString64Bytes ResourceId;
        public int AmountPerTick;
        public float TickInterval;
        public double NextTickTime;
    }
}
