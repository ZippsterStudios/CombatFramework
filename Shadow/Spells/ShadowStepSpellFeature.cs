using Framework.Shadow.Components;
using Framework.Shadow.Runtime;
using Framework.Spells.Runtime;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Shadow.Spells
{
    /// <summary>
    /// Optional handler that wires a shadow-targeting feature into the spell script bridge.
    /// Feature IDs: "feature.shadow-step", "shadow.step".
    /// </summary>
    public static class ShadowStepSpellFeature
    {
        private static SpellScriptBridge.ScriptHandler _chained;
        private static readonly FixedString64Bytes FeatureA = (FixedString64Bytes)"feature.shadow-step";
        private static readonly FixedString64Bytes FeatureB = (FixedString64Bytes)"shadow.step";

        /// <summary>
        /// Installs a handler that consumes shadow-step script payloads and forwards unknown features to any prior handler.
        /// </summary>
        public static void Install()
        {
            _chained = null;
            SpellScriptBridge.Register(Dispatch);
        }

        /// <summary>
        /// Installs while preserving an existing handler; unrecognized features are forwarded.
        /// </summary>
        public static void InstallAndChain(SpellScriptBridge.ScriptHandler existing)
        {
            _chained = existing;
            SpellScriptBridge.Register(Dispatch);
        }

        private static void Dispatch(ref EntityManager em, in Entity caster, in Entity target, in SpellRuntimeMetadata meta, in FixedString64Bytes featureId, in FixedString64Bytes args)
        {
            if (featureId.Equals(FeatureA) || featureId.Equals(FeatureB))
            {
                var rules = ShadowRules.Default;
                var range = ParseRange(args, 30f);
                ShadowTargetingUtility.BeginTargeting(ref em, caster, FeatureA, range, rules);
                return;
            }

            _chained?.Invoke(ref em, caster, target, in meta, in featureId, in args);
        }

        private static float ParseRange(in FixedString64Bytes args, float fallback)
        {
            if (args.Length == 0)
                return fallback;

            var str = args.ToString();
            var parts = str.Split('=');
            if (parts.Length == 2 && parts[0].Trim().ToLowerInvariant() == "range" && float.TryParse(parts[1], out var parsed))
                return parsed;
            return fallback;
        }
    }
}
