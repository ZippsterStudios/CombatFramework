using Unity.Collections;
using Unity.Entities;

namespace Framework.Melee.Runtime.Utilities
{
    public struct MeleeDeterministicRng
    {
        private uint _state;

        public static MeleeDeterministicRng FromSeed(Entity attacker, in FixedString32Bytes weaponSlot, uint swingSequence, uint frameCount, uint salt = 0)
        {
            uint seed = (uint)attacker.Index * 73856093u;
            seed ^= (uint)weaponSlot.GetHashCode();
            seed = HashCombine(seed, swingSequence);
            seed = HashCombine(seed, frameCount);
            seed = HashCombine(seed, salt);
            return FromRaw(seed);
        }

        public static MeleeDeterministicRng FromRaw(uint seed)
        {
            if (seed == 0)
                seed = 0x9E3779B9u;
            return new MeleeDeterministicRng { _state = seed };
        }

        public uint SerializeState() => _state;

        public float NextFloat()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return (_state & 0xFFFFFFu) / 16777216f;
        }

        public bool RollPercent(float percent)
        {
            if (percent <= 0f)
                return false;
            if (percent >= 100f)
                return true;
            return NextFloat() * 100f < percent;
        }

        private static uint HashCombine(uint seed, uint value)
        {
            return seed ^ (value + 0x9e3779b9u + (seed << 6) + (seed >> 2));
        }
    }
}
