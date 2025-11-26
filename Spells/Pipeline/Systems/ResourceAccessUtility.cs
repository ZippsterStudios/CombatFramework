using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Framework.Resources.Factory;
using Framework.Resources.Policies;
using Framework.Spells.Content;

namespace Framework.Spells.Pipeline.Systems
{
    static class ResourceAccessUtility
    {
        static readonly FixedString64Bytes ManaLiteral = CreateManaLiteral();
        static readonly FixedString64Bytes StaminaLiteral = CreateStaminaLiteral();
        static readonly FixedString64Bytes HealthLiteral = CreateHealthLiteral();

        public static bool CanAfford(ref EntityManager em, in Entity caster, in SpellCost cost)
        {
            if (cost.Amount == 0)
                return true;

            if (Matches(cost.Resource, ManaLiteral))
                return ResourcePolicy.CanAffordMana(em, caster, cost.Amount) == ResourcePolicy.Result.Allow;
            if (Matches(cost.Resource, StaminaLiteral))
                return ResourcePolicy.CanAffordStamina(em, caster, cost.Amount) == ResourcePolicy.Result.Allow;
            if (Matches(cost.Resource, HealthLiteral))
                return ResourcePolicy.CanAffordHealth(em, caster, cost.Amount) == ResourcePolicy.Result.Allow;
            return true;
        }

        public static void Spend(ref EntityManager em, in Entity caster, in SpellCost cost)
        {
            if (cost.Amount == 0)
                return;

            if (Matches(cost.Resource, ManaLiteral))
            {
                ResourceFactory.ApplyManaDelta(ref em, caster, -cost.Amount);
                return;
            }

            if (Matches(cost.Resource, StaminaLiteral))
            {
                ResourceFactory.ApplyStaminaDelta(ref em, caster, -cost.Amount);
                return;
            }

            if (Matches(cost.Resource, HealthLiteral))
            {
                ResourceFactory.ApplyHealthDelta(ref em, caster, -cost.Amount);
            }
        }

        public static void Refund(ref EntityManager em, in Entity caster, in SpellCost cost, float percent)
        {
            if (cost.Amount == 0 || percent <= 0f)
                return;

            var refund = (int)math.round(cost.Amount * percent);
            if (refund == 0)
                return;

            if (Matches(cost.Resource, ManaLiteral))
            {
                ResourceFactory.ApplyManaDelta(ref em, caster, refund);
                return;
            }

            if (Matches(cost.Resource, StaminaLiteral))
            {
                ResourceFactory.ApplyStaminaDelta(ref em, caster, refund);
                return;
            }

            if (Matches(cost.Resource, HealthLiteral))
            {
                ResourceFactory.ApplyHealthDelta(ref em, caster, refund);
            }
        }

        static bool Matches(in FixedString64Bytes value, in FixedString64Bytes literal)
        {
            if (value.Length == 0 || value.Length != literal.Length)
                return false;
            return value.Equals(literal);
        }

        static FixedString64Bytes CreateManaLiteral()
        {
            FixedString64Bytes s = default;
            s.Append('M');
            s.Append('a');
            s.Append('n');
            s.Append('a');
            return s;
        }

        static FixedString64Bytes CreateStaminaLiteral()
        {
            FixedString64Bytes s = default;
            s.Append('S');
            s.Append('t');
            s.Append('a');
            s.Append('m');
            s.Append('i');
            s.Append('n');
            s.Append('a');
            return s;
        }

        static FixedString64Bytes CreateHealthLiteral()
        {
            FixedString64Bytes s = default;
            s.Append('H');
            s.Append('e');
            s.Append('a');
            s.Append('l');
            s.Append('t');
            s.Append('h');
            return s;
        }
    }
}
