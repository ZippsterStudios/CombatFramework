using Unity.Entities;

namespace Framework.Spells.Runtime
{
    public struct EffectExecutionContext
    {
        public EntityManager EntityManager;
        public Entity Caster;
        public Entity PrimaryTarget;
        public SpellRuntimeMetadata Metadata;
        public uint RandomSeed;
        public EffectResultLedger Results;
    }
}

