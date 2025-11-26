using System.Collections.Generic;
using Unity.Collections;

namespace Framework.Stats.Content
{
    public struct StatEntry
    {
        public FixedString64Bytes Id;
        public float BaseValue;
    }

    public struct StatProfile
    {
        public FixedString64Bytes Id;
        public StatEntry[] Entries;
    }
}

