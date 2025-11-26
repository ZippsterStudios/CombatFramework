using Framework.AI.Components;
using Framework.Contracts.Intents;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Framework.AI.Runtime
{
    public static class AICastUtility
    {
        public static bool TryWriteCast(double now, in FixedString64Bytes spellId, Entity target, ref AICombatRuntime runtime, in AIAgentBehaviorConfig config, ref CastIntent intent)
        {
            if (target == Entity.Null || spellId.Length == 0)
            {
                intent.Clear();
                return false;
            }

            if (now < runtime.NextPrimaryAbilityTime - 1e-5f)
                return false;

            intent.SpellId = spellId;
            intent.Target = target;
            intent.Active = 1;
            runtime.NextPrimaryAbilityTime = now + math.max(0.05f, config.PrimarySpellCooldownSeconds);
            return true;
        }
    }
}
