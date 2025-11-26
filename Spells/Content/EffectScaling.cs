using Unity.Collections;

namespace Framework.Spells.Content
{
    public struct EffectScaling
    {
        public ScalingSourceKind AttributeSource;
        public FixedString64Bytes AttributeId;
        public float AttributeCoefficient;
        public byte AttributeFromCaster;

        public float LevelCoefficient;

        public byte UsePreviousResult;
        public sbyte PreviousBlockOffset;
        public EffectResultSource ResultSource;
        public float ResultCoefficient;

        public float ClampMin;
        public float ClampMax;
    }

    public enum ScalingSourceKind : byte
    {
        None = 0,
        Attribute = 1,
        Level = 2,
        PreviousBlockResult = 3
    }

    public enum EffectResultSource : byte
    {
        Damage = 0,
        Heal = 1
    }
}
