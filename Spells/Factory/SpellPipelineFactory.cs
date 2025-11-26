using Framework.Core.Base;
using Framework.Damage.Runtime;
using Framework.Spells.Pipeline.Components;
using Framework.Spells.Pipeline.Plan;
using Framework.Spells.Requests;
using Framework.Spells.Runtime;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Spells.Factory
{
    /// <summary>
    /// Human-friendly helpers for interacting with the spell cast pipeline
    /// without touching dynamic buffers or custom components directly.
    /// </summary>
    public static class SpellPipelineFactory
    {
        private static World _lastBootstrappedWorld;

        /// <summary>
        /// Enqueue a spell cast against a target. This is the most common entrypoint
        /// and mirrors the old SpellRequestFactory API.
        /// </summary>
        public static void Cast(ref EntityManager em, in Entity caster, in Entity target, in FixedString64Bytes spellId, int power = 0)
        {
            EnsureWorldBootstrapped(em.World);
            SpellDebugLogger.Log($"[SpellPipeline] Cast request world='{em.World?.Name ?? "<null>"}' caster={SpellDebugLogger.FormatEntity(caster)} target={SpellDebugLogger.FormatEntity(target)} spell={spellId.ToString()} power={power}");
            SpellRequestFactory.EnqueueCast(ref em, caster, target, spellId, power);
        }

        /// <summary>
        /// Ensure the caster has a plan modifier buffer and append a custom step to the end.
        /// Useful for role-specific injections (e.g., add channel ticks).
        /// </summary>
        public static void AppendStep(ref EntityManager em, in Entity caster, CastStepType stepType, in CastStepParams parameters)
        {
            var buffer = GetOrCreateModifierBuffer(ref em, caster);
            buffer.Add(new SpellCastPlanModifier
            {
                Operation = CastPlanOperation.Append,
                StepType = stepType,
                ReferenceStep = CastStepType.None,
                Params = parameters
            });
        }

        /// <summary>
        /// Insert a step relative to another step. Set insertAfter = true to place it after the reference step.
        /// </summary>
        public static void InsertStep(ref EntityManager em, in Entity caster, CastStepType stepType, CastStepType referenceStep, in CastStepParams parameters, bool insertAfter = false)
        {
            var buffer = GetOrCreateModifierBuffer(ref em, caster);
            buffer.Add(new SpellCastPlanModifier
            {
                Operation = insertAfter ? CastPlanOperation.InsertAfter : CastPlanOperation.InsertBefore,
                StepType = stepType,
                ReferenceStep = referenceStep,
                Params = parameters
            });
        }

        /// <summary>
        /// Remove the first occurrence of a step from the plan modifiers buffer.
        /// </summary>
        public static void RemoveStep(ref EntityManager em, in Entity caster, CastStepType stepType)
        {
            var buffer = GetOrCreateModifierBuffer(ref em, caster);
            buffer.Add(new SpellCastPlanModifier
            {
                Operation = CastPlanOperation.Remove,
                StepType = stepType,
                ReferenceStep = CastStepType.None,
                Params = CastStepParams.Empty
            });
        }

        /// <summary>
        /// Clear any plan overrides from the caster.
        /// </summary>
        public static void ClearOverrides(ref EntityManager em, in Entity caster)
        {
            if (em.HasBuffer<SpellCastPlanModifier>(caster))
                em.RemoveComponent<SpellCastPlanModifier>(caster);
        }

        public static void EnableDebugLogs(bool enabled)
        {
            SpellDebugLogger.SetEnabled(enabled);
            DamageDebugBridge.EnableDebugLogs(enabled);
            SpellDebugLogger.Log($"[SpellPipeline] Debug logging {(enabled ? "enabled" : "disabled")}.");
        }

        static DynamicBuffer<SpellCastPlanModifier> GetOrCreateModifierBuffer(ref EntityManager em, in Entity caster)
        {
            if (!em.HasBuffer<SpellCastPlanModifier>(caster))
                em.AddBuffer<SpellCastPlanModifier>(caster);
            return em.GetBuffer<SpellCastPlanModifier>(caster);
        }

        private static void EnsureWorldBootstrapped(World world)
        {
            if (world == null || !world.IsCreated)
                return;

            if (_lastBootstrappedWorld != null && !_lastBootstrappedWorld.IsCreated)
                _lastBootstrappedWorld = null;

            if (_lastBootstrappedWorld == world)
                return;

            SubsystemBootstrap.InstallAll(world);
            SpellDebugLogger.Log($"[SpellPipeline] Bootstrapped world '{world.Name}'.");
            _lastBootstrappedWorld = world;
        }
    }
}
