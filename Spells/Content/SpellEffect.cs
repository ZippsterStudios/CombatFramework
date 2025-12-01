using Unity.Collections;

namespace Framework.Spells.Content
{
    // Legacy per-effect definition kept for backward compatibility; new content should use EffectBlock authoring instead.
    public struct SpellEffect
    {
        public SpellEffectKind Kind;
        public int Magnitude;
        public FixedString64Bytes RefId;
        public float Radius;
        public float ConeAngle;
        public FixedString64Bytes Filter;
        public byte ChainCount;
        public bool ApplyToAll;
    }

    public enum SpellEffectKind : byte
    {
        DirectDamage = 0,
        DirectHeal = 1,
        Heal = 2,
        DOT = 3,
        HOT = 4,
        Buff = 5,
        Debuff = 6,
        AreaEffect = 7,
        SummonPet = 8,
        LifeTransfer = 9,
        SelfHOT = 10,
        Script = 11
    }
}
