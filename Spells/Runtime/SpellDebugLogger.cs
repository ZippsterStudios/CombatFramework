using Unity.Entities;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
#endif

namespace Framework.Spells.Runtime
{
    internal static class SpellDebugLogger
    {
        private static bool _enabled;

        public static bool Enabled => _enabled;

        public static void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_enabled)
                return;
            Debug.Log(message);
#endif
        }

        public static string FormatEntity(in Entity entity)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return entity == Entity.Null ? "Entity.Null" : $"{entity.Index}:{entity.Version}";
#else
            return entity.ToString();
#endif
        }
    }
}
