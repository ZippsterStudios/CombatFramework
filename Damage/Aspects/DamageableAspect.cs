using Unity.Entities;

namespace Framework.Damage.Aspects
{
    public readonly partial struct DamageableAspect : IAspect
    {
        public readonly Entity Entity;
        readonly RefRO<Framework.Damage.Components.Damageable> _dmg;

        public int Armor => _dmg.IsValid ? _dmg.ValueRO.Armor : 0;
        public float Resist => _dmg.IsValid ? _dmg.ValueRO.ResistPercent : 0f;
        public bool HasDamageable => _dmg.IsValid;
    }
}

