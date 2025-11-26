using Framework.Lifecycle.Config;
using Unity.Mathematics;

namespace Framework.Lifecycle.Policies
{
    public static class LifecyclePolicy
    {
        public static LifecycleFeatureConfig EnsureDefaults(bool hasConfig, in LifecycleFeatureConfig config)
        {
            return hasConfig ? config : LifecycleFeatureConfig.Default;
        }

        public static float ClampDespawnSeconds(float seconds)
        {
            return math.max(0.1f, seconds);
        }

        public static bool ShouldEmitDeathEvent(in LifecycleFeatureConfig config) => config.EnableDeathEvents;
        public static bool ShouldCleanupBuffs(in LifecycleFeatureConfig config) => config.CleanupBuffsOnDeath;
        public static bool ShouldCleanupDebuffs(in LifecycleFeatureConfig config) => config.CleanupDebuffsOnDeath;
        public static bool ShouldCleanupDots(in LifecycleFeatureConfig config) => config.CleanupDotsOnDeath;
        public static bool ShouldCleanupHots(in LifecycleFeatureConfig config) => config.CleanupHotsOnDeath;
        public static bool ShouldCleanupTimedEffects(in LifecycleFeatureConfig config) => config.CleanupTimedEffectsOnDeath;
        public static bool ShouldClearThreat(in LifecycleFeatureConfig config) => config.ClearThreatOnDeath;
        public static bool ShouldStopRegen(in LifecycleFeatureConfig config) => config.StopRegenOnDeath;
        public static bool ShouldAutoDespawn(in LifecycleFeatureConfig config) => config.AutoDespawnEnabled;
    }
}

