using Unity.Entities;

namespace Framework.ActionBlock.Components
{
    public enum ActionKind : byte
    {
        Attack = 0,
        Move = 1,
        Cast = 2,
        Interact = 3,
        UseItem = 4,
        Custom0 = 5,
        Custom1 = 6,
        Custom2 = 7,
        Custom3 = 8
    }

    public struct ActionBlockMask : IComponentData
    {
        public uint Bits;
    }

    public static class ActionBits
    {
        public static void Set(ref ActionBlockMask mask, ActionKind kind)
        {
            mask.Bits |= 1u << (int)kind;
        }

        public static void Clear(ref ActionBlockMask mask, ActionKind kind)
        {
            mask.Bits &= ~(1u << (int)kind);
        }

        public static bool IsBlocked(in ActionBlockMask mask, ActionKind kind)
        {
            return (mask.Bits & (1u << (int)kind)) != 0u;
        }
    }
}

