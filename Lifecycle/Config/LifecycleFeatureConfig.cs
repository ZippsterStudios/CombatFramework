using Unity.Entities;

namespace Framework.Lifecycle.Config
{
    public struct LifecycleFeatureConfig : IComponentData
    {
        public bool EnableDeathEvents;
        public bool CleanupBuffsOnDeath;
        public bool CleanupDebuffsOnDeath;
        public bool CleanupDotsOnDeath;
        public bool CleanupHotsOnDeath;
        public bool CleanupTimedEffectsOnDeath;
        public bool ClearThreatOnDeath;
        public bool StopRegenOnDeath;
        public bool AutoDespawnEnabled;
        public float AutoDespawnSeconds;
        public bool BlocksRespectDead;
        public bool BlocksRespectCrowdControl;
        public bool BlocksRespectCustomRules;

        public static LifecycleFeatureConfig Default => new LifecycleFeatureConfig
        {
            EnableDeathEvents = true,
            CleanupBuffsOnDeath = true,
            CleanupDebuffsOnDeath = true,
            CleanupDotsOnDeath = true,
            CleanupHotsOnDeath = true,
            CleanupTimedEffectsOnDeath = true,
            ClearThreatOnDeath = true,
            StopRegenOnDeath = true,
            AutoDespawnEnabled = false,
            AutoDespawnSeconds = 5f,
            BlocksRespectDead = true,
            BlocksRespectCrowdControl = true,
            BlocksRespectCustomRules = true
        };
    }
}

