using Framework.AI.Authoring;
using Framework.AI.Behaviors.Components;
using Framework.AI.Components;
using Unity.Entities;
using UnityEngine;

namespace Framework.AI.Behaviors.Authoring
{
    [DisallowMultipleComponent]
    public sealed class AIBehaviorRecipeTextAuthoring : MonoBehaviour
    {
        [SerializeField] private TextAsset _behaviorAsset;

        public TextAsset BehaviorAsset
        {
            get => _behaviorAsset;
            set => _behaviorAsset = value;
        }

#if UNITY_EDITOR && UNITY_ENTITIES_1_0_0_OR_NEWER
        private sealed class Baker : Unity.Entities.Baker<AIBehaviorRecipeTextAuthoring>
        {
            public override void Bake(AIBehaviorRecipeTextAuthoring authoring)
            {
                if (authoring._behaviorAsset == null || string.IsNullOrWhiteSpace(authoring._behaviorAsset.text))
                    return;

                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var config = ResolveConfig(authoring);
                var template = AIBehaviorRecipeTextParser.Parse(authoring._behaviorAsset.text);
                var recipe = template.Build(in config);
                AddBlobAsset(ref recipe);
                AddComponent(entity, new AIBehaviorRecipeRef { Recipe = recipe });
            }

            private static AIAgentBehaviorConfig ResolveConfig(AIBehaviorRecipeTextAuthoring authoring)
            {
                var configAuthoring = authoring.GetComponent<AIBehaviorAuthoring>();
                return configAuthoring != null ? configAuthoring.ToComponentData() : AIAgentBehaviorConfig.CreateDefaults();
            }
        }
#endif
    }
}
