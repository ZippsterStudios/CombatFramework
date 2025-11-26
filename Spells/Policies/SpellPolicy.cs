using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

#if FRAMEWORK_HAS_ACTION_BLOCK
using Framework.ActionBlock.Components;
using Framework.ActionBlock.Policies;
#endif

namespace Framework.Spells.Policies
{
    [BurstCompile]
    public static class SpellPolicy
    {
        public enum Result
        {
            Allow,
            Reject_UnknownSpell,
            Reject_NotInSpellbook,
            Reject_OnCooldown,
            Reject_InsufficientResources,
            Reject_Spatial_Invalid
        }

        [BurstCompile]
        public static Result ValidateCast(in EntityManager em, in Entity caster, in FixedString64Bytes spellId, double now)
        {
            if (!em.Exists(caster)) return Result.Reject_Spatial_Invalid;
            // Basic validation: spell known
            if (!em.HasBuffer<Framework.Spells.Spellbook.Components.SpellSlot>(caster))
                return Result.Reject_NotInSpellbook;

            var buf = em.GetBuffer<Framework.Spells.Spellbook.Components.SpellSlot>(caster);
            bool known = false;
            for (int i = 0; i < buf.Length; i++)
                if (buf[i].SpellId.Equals(spellId)) { known = true; break; }
            if (!known) return Result.Reject_NotInSpellbook;

            // Cooldowns: if any cooldown group matches spell id and not ready, reject
            if (em.HasBuffer<Framework.Cooldowns.Components.CooldownGroup>(caster))
            {
                var cd = em.GetBuffer<Framework.Cooldowns.Components.CooldownGroup>(caster);
                for (int i = 0; i < cd.Length; i++)
                {
                    if (cd[i].GroupId.Equals(spellId) && now < cd[i].ReadyTime)
                        return Result.Reject_OnCooldown;
                }
            }

#if FRAMEWORK_HAS_ACTION_BLOCK
            if (!ActionBlockPolicy.Can(em, caster, ActionKind.Cast))
                return Result.Reject_Spatial_Invalid;
#endif

            return Result.Allow;
        }
    }
}
