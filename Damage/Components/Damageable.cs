using Unity.Entities;

namespace Framework.Damage.Components
{
    public struct Damageable : IComponentData
    {
        public int Armor;
        public float ResistPercent; // 0..1
    }
}
