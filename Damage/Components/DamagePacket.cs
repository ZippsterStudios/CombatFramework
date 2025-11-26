using Unity.Collections;
using Unity.Entities;

namespace Framework.Damage.Components
{
    public enum DamageSchool
    {
        Physical,
        Fire,
        Frost,
        Nature,
        Shadow,
        Holy,
        Arcane,
        Lightning
    }

    public struct DamagePacket
    {
        public DamageSchool School;
        public int Amount;
        public FixedString64Bytes Tags; // optional comma-separated tags
        public Entity Source;
        public float CritMult; // 1.0 = no crit
        public byte IgnoreArmor;
        public byte IgnoreResist;
        public byte IgnoreSnapshotModifiers;
    }
}
