using Framework.AI.Components;
using Framework.AI.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Framework.AI.Authoring
{
    [DisallowMultipleComponent]
    public sealed class AIStateAuthoring : MonoBehaviour
    {
        public int StartState = AIStateIds.Idle;

#if UNITY_EDITOR && UNITY_ENTITIES_1_0_0_OR_NEWER
        private sealed class Baker : Unity.Entities.Baker<AIStateAuthoring>
        {
            public override void Bake(AIStateAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var data = new AIState { Current = math.max(0, authoring.StartState) };

                if (!HasComponent<AIState>(entity))
                    AddComponent(entity, data);
                else
                    SetComponent(entity, data);
            }
        }
#endif
    }
}
