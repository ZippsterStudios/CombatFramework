using Unity.Collections;
using Unity.Entities;

namespace Framework.Spells.Runtime
{
    public static class SpellScriptBridge
    {
        public delegate void ScriptHandler(ref EntityManager em, in Entity caster, in Entity target, in SpellRuntimeMetadata meta, in FixedString64Bytes featureId, in FixedString64Bytes args);

        private static ScriptHandler _handler;

        public static void Register(ScriptHandler handler) => _handler = handler;

        public static bool TryInvoke(ref EntityManager em, in Entity caster, in Entity target, in SpellRuntimeMetadata meta, in FixedString64Bytes featureId, in FixedString64Bytes args)
        {
            if (_handler == null)
                return false;
            _handler(ref em, caster, target, in meta, in featureId, in args);
            return true;
        }
    }
}
