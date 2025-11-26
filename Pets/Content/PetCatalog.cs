using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Pets.Content
{
    public static class PetCatalog
    {
        private static readonly Dictionary<FixedString64Bytes, PetDefinition> _defs = new();
        private static readonly Dictionary<FixedString64Bytes, BlobAssetReference<PetDefinitionBlob>> _blobCache = new();

        public static void Register(in PetDefinition definition)
        {
            var normalized = Normalize(definition);
            _defs[normalized.Id] = normalized;
            if (_blobCache.TryGetValue(normalized.Id, out var existing) && existing.IsCreated)
                existing.Dispose();
            _blobCache[normalized.Id] = PetDefinitionBlobUtility.Create(normalized);
        }

        public static bool TryGet(in FixedString64Bytes petId, out PetDefinition definition)
        {
            return _defs.TryGetValue(NormalizeId(petId), out definition);
        }

        public static bool TryGetBlob(in FixedString64Bytes petId, out BlobAssetReference<PetDefinitionBlob> blob)
        {
            var id = NormalizeId(petId);
            if (_blobCache.TryGetValue(id, out blob) && blob.IsCreated)
                return true;

            if (!_defs.TryGetValue(id, out var def))
            {
                blob = default;
                return false;
            }

            blob = PetDefinitionBlobUtility.Create(def);
            _blobCache[id] = blob;
            return true;
        }

        public static void LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            var list = JsonConvert.DeserializeObject<List<PetDefinition>>(json);
            if (list == null)
                return;

            foreach (var def in list)
                Register(def);
        }

        public static void ClearAll()
        {
            foreach (var kvp in _blobCache)
            {
                if (kvp.Value.IsCreated)
                    kvp.Value.Dispose();
            }
            _blobCache.Clear();
            _defs.Clear();
        }

        private static PetDefinition Normalize(in PetDefinition def)
        {
            var normalized = def;
            normalized.Id = NormalizeId(def.Id);
            normalized.CategoryId = NormalizeCategory(def.CategoryId);
            normalized.DefaultGroup = NormalizeCategory(def.DefaultGroup);
            normalized.DefaultAIRecipeId = NormalizeId(def.DefaultAIRecipeId);
            normalized.PrefabRef = NormalizeId(def.PrefabRef);
            return normalized;
        }

        private static FixedString64Bytes NormalizeId(in FixedString64Bytes value)
        {
            if (value.Length == 0)
                return default;
            var lower = value.ToString().ToLowerInvariant();
            return (FixedString64Bytes)lower;
        }

        private static FixedString32Bytes NormalizeCategory(in FixedString32Bytes value)
        {
            if (value.Length == 0)
                return default;
            var lower = value.ToString().ToLowerInvariant();
            return (FixedString32Bytes)lower;
        }
    }
}
