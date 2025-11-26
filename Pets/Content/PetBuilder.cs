using Unity.Collections;

namespace Framework.Pets.Content
{
    public sealed class PetBuilder
    {
        private PetDefinition _definition;

        private PetBuilder(FixedString64Bytes id)
        {
            _definition.Id = id;
            _definition.BaseHealth = 50;
            _definition.BaseMana = 0;
            _definition.MoveSpeed = 4f;
            _definition.MaxCountPerOwner = 1;
            _definition.DefaultLeashDistance = 20f;
            _definition.DefaultFollowOffset = 2f;
            _definition.DefaultDurationSeconds = -1f;
        }

        public static PetBuilder NewPet(FixedString64Bytes id) => new(id);

        public PetBuilder Category(FixedString32Bytes categoryId, int level)
        {
            _definition.CategoryId = categoryId;
            _definition.CategoryLevel = level;
            return this;
        }

        public PetBuilder Prefab(FixedString64Bytes prefabRef)
        {
            _definition.PrefabRef = prefabRef;
            return this;
        }

        public PetBuilder Stats(int health, int mana, float moveSpeed)
        {
            _definition.BaseHealth = health;
            _definition.BaseMana = mana;
            _definition.MoveSpeed = moveSpeed;
            return this;
        }

        public PetBuilder Flags(PetFlags flags)
        {
            _definition.Flags = flags;
            return this;
        }

        public PetBuilder Symbiosis(PetSymbiosisMode mode, float splitPercent = 0.5f)
        {
            _definition.SymbiosisMode = mode;
            _definition.SymbiosisSplitPercent = splitPercent;
            return this;
        }

        public PetBuilder Limit(int maxCount, FixedString32Bytes defaultGroup = default)
        {
            _definition.MaxCountPerOwner = maxCount;
            if (defaultGroup.Length > 0)
                _definition.DefaultGroup = defaultGroup;
            return this;
        }

        public PetBuilder Leash(float distance, float followOffset = 2f)
        {
            _definition.DefaultLeashDistance = distance;
            _definition.DefaultFollowOffset = followOffset;
            return this;
        }

        public PetBuilder Duration(float seconds)
        {
            _definition.DefaultDurationSeconds = seconds;
            return this;
        }

        public PetBuilder Recipe(FixedString64Bytes recipeId)
        {
            _definition.DefaultAIRecipeId = recipeId;
            return this;
        }

        public PetDefinition Register()
        {
            PetCatalog.Register(_definition);
            return _definition;
        }
    }
}
