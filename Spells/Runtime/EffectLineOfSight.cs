using Unity.Entities;

namespace Framework.Spells.Runtime
{
    public interface IEffectLineOfSight
    {
        bool HasLineOfSight(ref EntityManager em, in Entity caster, in Entity target);
    }

    public static class EffectLineOfSight
    {
        private sealed class DefaultProvider : IEffectLineOfSight
        {
            public bool HasLineOfSight(ref EntityManager em, in Entity caster, in Entity target) => true;
        }

        private static IEffectLineOfSight _provider = new DefaultProvider();

        public static void SetProvider(IEffectLineOfSight provider) => _provider = provider ?? new DefaultProvider();

        public static bool HasLineOfSight(ref EntityManager em, in Entity caster, in Entity target) =>
            _provider.HasLineOfSight(ref em, in caster, in target);
    }
}
