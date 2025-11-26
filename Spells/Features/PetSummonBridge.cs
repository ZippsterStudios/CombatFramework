using Unity.Entities;
using Framework.Spells.Content;

namespace Framework.Spells.Features
{
    using RuntimeSpellMetadata = Framework.Spells.Runtime.SpellRuntimeMetadata;

    public static class PetSummonBridge
    {
        public delegate void SummonHandler(ref EntityManager em, in Entity caster, in Entity target, in RuntimeSpellMetadata meta, in SummonPayload payload, int resolvedCategoryLevel);

        private static SummonHandler _handler;

        public static void Register(SummonHandler handler)
        {
            _handler = handler;
        }

        public static bool TrySummon(ref EntityManager em, in Entity caster, in Entity target, in RuntimeSpellMetadata meta, in SummonPayload payload, int resolvedCategoryLevel)
        {
            if (_handler == null)
                return false;

            _handler(ref em, caster, target, meta, payload, resolvedCategoryLevel);
            return true;
        }
    }
}
