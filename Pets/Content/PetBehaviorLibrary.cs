using Framework.AI.Behaviors.Authoring;
using Framework.AI.Behaviors.Runtime;
using Framework.AI.Components;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Pets.Content
{
    public static class PetBehaviorLibrary
    {
        private static BlobAssetReference<AIBehaviorRecipe> _follow;
        private static BlobAssetReference<AIBehaviorRecipe> _guard;
        private static BlobAssetReference<AIBehaviorRecipe> _sit;
        private static BlobAssetReference<AIBehaviorRecipe> _patrol;

        public static BlobAssetReference<AIBehaviorRecipe> Resolve(in FixedString64Bytes recipeId)
        {
            if (recipeId.Length == 0)
                return default;

            var normalized = recipeId.ToString().ToLowerInvariant();
            return normalized switch
            {
                "pet_follow" => EnsureFollow(),
                "pet_guard" => EnsureGuard(),
                "pet_sit" => EnsureSit(),
                "pet_patrol" => EnsurePatrol(),
                _ => default
            };
        }

        private static BlobAssetReference<AIBehaviorRecipe> EnsureFollow()
        {
            if (_follow.IsCreated)
                return _follow;

            var builder = AIBehaviorBuilder.New("pet_follow");
            builder.Rule("close_attack",
                c => c.HasTarget().Visible().InRange(cfg => cfg.AttackRange),
                a => a.Stop().CastPrimary());
            builder.Rule("chase",
                c => c.HasTarget().Visible().NotInRange(cfg => cfg.AttackRange),
                a => a.MoveChase(cfg => cfg.MoveSpeed));
            builder.Default(a => a.MoveChase(cfg => cfg.MoveSpeed));
            builder.BuildBlob(AIAgentBehaviorConfig.CreateDefaults(), out _follow);
            return _follow;
        }

        private static BlobAssetReference<AIBehaviorRecipe> EnsureGuard()
        {
            if (_guard.IsCreated)
                return _guard;

            var builder = AIBehaviorBuilder.New("pet_guard");
            builder.Rule("threatened",
                c => c.HasTarget().Visible().NotInRange(cfg => cfg.AttackRange),
                a => a.MoveChase(cfg => cfg.MoveSpeed));
            builder.Rule("strike",
                c => c.HasTarget().Visible().InRange(cfg => cfg.AttackRange),
                a => a.Stop().CastPrimary());
            builder.Default(a => a.Stop());
            builder.BuildBlob(AIAgentBehaviorConfig.CreateDefaults(), out _guard);
            return _guard;
        }

        private static BlobAssetReference<AIBehaviorRecipe> EnsureSit()
        {
            if (_sit.IsCreated)
                return _sit;

            var builder = AIBehaviorBuilder.New("pet_sit");
            builder.Default(a => a.Stop());
            builder.BuildBlob(AIAgentBehaviorConfig.CreateDefaults(), out _sit);
            return _sit;
        }

        private static BlobAssetReference<AIBehaviorRecipe> EnsurePatrol()
        {
            if (_patrol.IsCreated)
                return _patrol;

            var builder = AIBehaviorBuilder.New("pet_patrol");
            builder.Rule("attack_in_range",
                c => c.HasTarget().Visible().InRange(cfg => cfg.AttackRange),
                a => a.Stop().CastPrimary());
            builder.Default(a => a.MoveChase(cfg => cfg.MoveSpeed));
            builder.BuildBlob(AIAgentBehaviorConfig.CreateDefaults(), out _patrol);
            return _patrol;
        }
    }
}
