using Framework.Damage.Components;
using Unity.Entities;

namespace Framework.DamageModifiers.Components
{
    public struct DamageTypeModifier : IBufferElementData
    {
        public DamageSchool School;
        public float Multiplier;
    }

    public struct DamageModifierDefaults : IComponentData
    {
        public float GlobalMultiplier;
    }
}

