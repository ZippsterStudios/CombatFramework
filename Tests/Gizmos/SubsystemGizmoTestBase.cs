using Framework.Core.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Framework.Tests.Gizmos
{
    /// <summary>
    /// Editor-only base harness that visualizes DOTS entities by drawing color-coded gizmos.
    /// Attach a derived MonoBehaviour to a GameObject in a test scene to inspect subsystem data.
    /// </summary>
    [ExecuteAlways]
    public abstract class SubsystemGizmoTestBase : MonoBehaviour
    {
        [Header("Gizmo Style")]
        [SerializeField] private float gizmoRadius = 0.35f;
        [SerializeField] private Vector3 verticalOffset = new Vector3(0f, 0.05f, 0f);
        [SerializeField] private bool labelEntityIds;

        /// <summary>Component filter used to build the entity query for this visualizer.</summary>
        protected abstract ComponentType[] QueryComponents { get; }

        /// <summary>Derivations provide per-entity colors (e.g., HP gradients).</summary>
        protected abstract bool TryResolveColor(EntityManager em, in Entity entity, out Color gizmoColor);

        protected virtual Vector3 ResolveWorldPosition(EntityManager em, in Entity entity)
        {
            if (em.HasComponent<Position>(entity))
            {
                var pos2D = em.GetComponentData<Position>(entity).Value;
                return new float3(pos2D.x, 0f, pos2D.y);
            }

            return float3.zero;
        }

        private void OnDrawGizmos()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;

            var components = QueryComponents;
            if (components == null || components.Length == 0)
                return;

            var em = world.EntityManager;
            using var query = em.CreateEntityQuery(components);
            using var entities = query.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (!TryResolveColor(em, entity, out var gizmoColor))
                    continue;

                var worldPos = ResolveWorldPosition(em, entity) + verticalOffset;
                UnityEngine.Gizmos.color = gizmoColor;
                UnityEngine.Gizmos.DrawSphere(worldPos, gizmoRadius);
#if UNITY_EDITOR
                if (labelEntityIds)
                    UnityEditor.Handles.Label(worldPos, entity.ToString());
#endif
            }
        }
    }
}