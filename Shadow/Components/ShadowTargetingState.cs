using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace Framework.Shadow.Components
{
    /// <summary>
    /// Singleton describing current shadow-targeting interaction (e.g., Shadow Step, clones, grapples).
    /// </summary>
    public struct ShadowTargetingState : IComponentData
    {
        public byte IsActive;
        public Entity Caster;
        public FixedString64Bytes SpellId;
        public float MaxRange;
        public ShadowRules Rules;

        public static ShadowTargetingState Inactive => new ShadowTargetingState { IsActive = 0, Caster = Entity.Null, MaxRange = 0f, Rules = ShadowRules.Default };
    }

    public struct ShadowHoverState : IComponentData
    {
        public Entity Region;
    }

    public struct ShadowRules
    {
        public byte RequireEnabled;
        public byte TeamFilter; // 0 = any, otherwise match Team component value
        public byte RequireOwner; // 0 = any owner, 1 = must have owner

        public static ShadowRules Default => new ShadowRules
        {
            RequireEnabled = 1,
            TeamFilter = 0,
            RequireOwner = 0
        };
    }
}
