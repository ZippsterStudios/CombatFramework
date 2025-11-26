using System.Diagnostics;
using Unity.Entities;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
#endif

namespace Framework.Damage.Runtime
{
    /// <summary>
    /// Lightweight debug logger for the damage pipeline. Logging is disabled by default and
    /// only available in editor/development builds to avoid Burst restrictions in players.
    /// </summary>
    internal static class DamageDebug
    {
        private static bool _enabled;

        public static void SetEnabled(bool enabled) => _enabled = enabled;

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_enabled)
                return;
            UnityEngine.Debug.Log(message);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_enabled)
                return;
            UnityEngine.Debug.LogWarning(message);
#endif
        }

        public static string Format(Entity entity)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return entity == Entity.Null ? "Entity.Null" : $"{entity.Index}:{entity.Version}";
#else
            return entity.ToString();
#endif
        }
    }
}
