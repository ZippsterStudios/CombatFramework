using Unity.Collections;
using Unity.Entities;

namespace Framework.AI.Behaviors.Runtime
{
    public struct AIBehaviorRecipe
    {
        public BlobArray<Rule> Rules;
        public sbyte HasDefault;
        public Action DefaultAction;
    }

    public struct Rule
    {
        public Condition Condition;
        public Action Action;
        public int Priority;
    }

    public struct Condition
    {
        public byte Flags;
        public float AttackRange;
        public float HealthBelow;
        public byte NotInRange;
    }

    public struct Action
    {
        public byte Kind;
        public float MoveSpeed;
        public float RetreatDistance;
        public FixedString64Bytes SpellId;
    }

    public static class AIBehaviorConditionFlags
    {
        public const byte HasTarget = 1 << 0;
        public const byte TargetVisible = 1 << 1;
        public const byte UseRange = 1 << 2;
        public const byte UseHealthBelow = 1 << 3;
    }

    public enum AIBehaviorActionKind : byte
    {
        None = 0,
        MoveChase = 1,
        MoveFlee = 2,
        Stop = 3,
        CastPrimary = 4,
        CastId = 5
    }
}
