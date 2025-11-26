using Framework.AI.Behaviors.Runtime;
using Unity.Entities;

namespace Framework.AI.Behaviors.Components
{
    public struct AIBehaviorRecipeRef : IComponentData
    {
        public BlobAssetReference<AIBehaviorRecipe> Recipe;
    }
}
