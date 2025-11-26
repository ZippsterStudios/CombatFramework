using Unity.Entities;

namespace Framework.Lifecycle.Components
{
    public struct Dead : IComponentData { }

    public struct DeathEvent : IBufferElementData
    {
        public Entity Victim;
        public Entity Killer;
        public int FinalDamage;
    }
}

