using Framework.Core.Components;
using Framework.Pets.Components;
using Framework.Resources.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Framework.Tests.Gizmos
{
    /// <summary>
    /// Visualizer for the Pets subsystem. Draws HP-colored gizmos above each pet entity.
    /// </summary>
    [AddComponentMenu("Framework/Tests/Gizmos/Pets Subsystem Gizmo Test")]
    public sealed class PetsSubsystemGizmoTest : SubsystemGizmoTestBase
    {
        private static readonly ComponentType[] _components =
        {
            ComponentType.ReadOnly<Position>(),
            ComponentType.ReadOnly<PetTag>(),
            ComponentType.ReadOnly<Health>()
        };

        protected override ComponentType[] QueryComponents => _components;

        protected override bool TryResolveColor(EntityManager em, in Entity entity, out Color color)
        {
            if (!em.HasComponent<Health>(entity))
            {
                color = Color.gray;
                return true;
            }

            var health = em.GetComponentData<Health>(entity);
            float normalized = health.Max > 0 ? math.saturate((float)health.Current / math.max(1, health.Max)) : 0f;
            color = Color.Lerp(Color.red, Color.green, normalized);
            return true;
        }
    }
}