using System;
using Unity.Collections;

namespace Framework.Pets.Content
{
    public enum PetSymbiosisMode : byte
    {
        None = 0,
        SharedPool = 1,
        Mirror = 2,
        SplitPercent = 3
    }

    [Flags]
    public enum PetFlags : ushort
    {
        None = 0,
        Swarm = 1 << 0,
        ReplaceOldestOnLimit = 1 << 1,
        LeashTeleport = 1 << 2
    }

    public struct PetDefinition
    {
        public FixedString64Bytes Id;
        public FixedString32Bytes CategoryId;
        public int CategoryLevel;
        public FixedString64Bytes PrefabRef;
        public int BaseHealth;
        public int BaseMana;
        public float MoveSpeed;
        public PetFlags Flags;
        public PetSymbiosisMode SymbiosisMode;
        public float SymbiosisSplitPercent;
        public int MaxCountPerOwner;
        public float DefaultLeashDistance;
        public float DefaultFollowOffset;
        public float DefaultDurationSeconds;
        public FixedString32Bytes DefaultGroup;
        public FixedString64Bytes DefaultAIRecipeId;
    }
}
