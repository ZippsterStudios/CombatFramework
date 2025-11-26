using UnityEngine;
using Unity.Entities;

namespace Framework.UnityAuthoring.Authoring
{
    public class ResourcesAuthoring : MonoBehaviour
    {
        [Header("Health")] public bool HealthEnabled = true;
        public int HealthMax = 100;
        public int HealthCurrent = -1; // -1 => use max
        public int HealthRegenPerSecond = 0;

        [Header("Mana")] public bool ManaEnabled = false;
        public int ManaMax = 100;
        public int ManaCurrent = -1;
        public int ManaRegenPerSecond = 0;

        [Header("Stamina")] public bool StaminaEnabled = false;
        public int StaminaMax = 100;
        public int StaminaCurrent = -1;
        public int StaminaRegenPerSecond = 0;

        #if FRAMEWORK_USE_BAKERS
        class Baker : Unity.Entities.Baker<ResourcesAuthoring>
        {
            public override void Bake(ResourcesAuthoring a)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                if (a.HealthEnabled)
                {
                    var max = a.HealthMax < 0 ? 0 : a.HealthMax;
                    var cur = a.HealthCurrent < 0 ? max : a.HealthCurrent;
                    AddComponent(entity, new Framework.Resources.Components.Health
                    {
                        Current = cur,
                        Max = max,
                        RegenPerSecond = a.HealthRegenPerSecond,
                        RegenAccumulator = 0
                    });
                }

                if (a.ManaEnabled)
                {
                    var max = a.ManaMax < 0 ? 0 : a.ManaMax;
                    var cur = a.ManaCurrent < 0 ? max : a.ManaCurrent;
                    AddComponent(entity, new Framework.Resources.Components.Mana
                    {
                        Current = cur,
                        Max = max,
                        RegenPerSecond = a.ManaRegenPerSecond,
                        RegenAccumulator = 0
                    });
                }

                if (a.StaminaEnabled)
                {
                    var max = a.StaminaMax < 0 ? 0 : a.StaminaMax;
                    var cur = a.StaminaCurrent < 0 ? max : a.StaminaCurrent;
                    AddComponent(entity, new Framework.Resources.Components.Stamina
                    {
                        Current = cur,
                        Max = max,
                        RegenPerSecond = a.StaminaRegenPerSecond,
                        RegenAccumulator = 0
                    });
                }
            }
        }
        #endif
    }
}
