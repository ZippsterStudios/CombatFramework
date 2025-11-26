using Unity.Entities;

namespace Framework.Pets.Components
{
    public struct PetPatrolState : IComponentData
    {
        public int NextWaypointIndex;
        public byte Active;
    }
}
