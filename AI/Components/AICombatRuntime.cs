using Unity.Entities;

namespace Framework.AI.Components
{
    public struct AICombatRuntime : IComponentData
    {
        public double NextPrimaryAbilityTime;

        public static AICombatRuntime CreateDefault() => new AICombatRuntime
        {
            NextPrimaryAbilityTime = 0d
        };
    }
}

