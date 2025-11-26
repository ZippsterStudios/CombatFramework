using Unity.Collections;

namespace Framework.Buffs.Content
{
    public enum BuffStatEffectKind : byte
    {
        None = 0,
        HealthFlat,
        HealthPercent,
        ManaFlat,
        ManaPercent,
        StaminaFlat,
        StaminaPercent,
        Ward,
        DamageReflectFlat,
        DamageReflectPercent,
        DamageMultiplier,
        DefenseMultiplier,
        HastePercent,
        CustomAdditive,
        CustomMultiplier
    }

    public struct BuffStatEffect
    {
        public BuffStatEffectKind Kind;
        public FixedString32Bytes StatId;
        public float AdditivePerStack;
        public float MultiplierPerStack;

        public bool HasCustomId => Kind is BuffStatEffectKind.CustomAdditive or BuffStatEffectKind.CustomMultiplier;
    }
}
