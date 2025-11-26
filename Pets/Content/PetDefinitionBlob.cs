using Unity.Collections;
using Unity.Entities;

namespace Framework.Pets.Content
{
    public struct PetDefinitionBlob
    {
        public FixedString64Bytes Id;
        public FixedString32Bytes CategoryId;
        public int CategoryLevel;
        public FixedString64Bytes PrefabRef;
        public int BaseHealth;
        public int BaseMana;
        public float MoveSpeed;
        public PetFlags Flags;
        public PetSymbiosisMode SymbiosisMode;
        public float SymbiosisSplitPercent;
        public int MaxCountPerOwner;
        public float DefaultLeashDistance;
        public float DefaultFollowOffset;
        public float DefaultDurationSeconds;
        public FixedString32Bytes DefaultGroup;
        public FixedString64Bytes DefaultAIRecipeId;
    }

    public static class PetDefinitionBlobUtility
    {
        public static BlobAssetReference<PetDefinitionBlob> Create(in PetDefinition def, Allocator allocator = Allocator.Persistent)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<PetDefinitionBlob>();
            root.Id = def.Id;
            root.CategoryId = def.CategoryId;
            root.CategoryLevel = def.CategoryLevel;
            root.PrefabRef = def.PrefabRef;
            root.BaseHealth = def.BaseHealth;
            root.BaseMana = def.BaseMana;
            root.MoveSpeed = def.MoveSpeed;
            root.Flags = def.Flags;
            root.SymbiosisMode = def.SymbiosisMode;
            root.SymbiosisSplitPercent = def.SymbiosisSplitPercent;
            root.MaxCountPerOwner = def.MaxCountPerOwner;
            root.DefaultLeashDistance = def.DefaultLeashDistance;
            root.DefaultFollowOffset = def.DefaultFollowOffset;
            root.DefaultDurationSeconds = def.DefaultDurationSeconds;
            root.DefaultGroup = def.DefaultGroup;
            root.DefaultAIRecipeId = def.DefaultAIRecipeId;

            var blob = builder.CreateBlobAssetReference<PetDefinitionBlob>(allocator);
            builder.Dispose();
            return blob;
        }
    }
}
