using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.Entities;

namespace Framework.Spells.Content
{
    public static class SpellDefinitionCatalog
    {
        private static readonly Dictionary<FixedString64Bytes, SpellDefinition> _byId = new();
        private static readonly Dictionary<FixedString64Bytes, BlobAssetReference<SpellDefinitionBlob>> _blobById = new();

        public static void Register(in SpellDefinition def)
        {
            var normalized = NormalizeDefinition(def);
            _byId[normalized.Id] = normalized;
            if (_blobById.TryGetValue(normalized.Id, out var existing) && existing.IsCreated)
                existing.Dispose();
            _blobById[normalized.Id] = SpellDefinitionBlobUtility.Create(normalized);
        }

        /// <summary>
        /// Clears all cached spell definitions and disposes their blob references.
        /// Intended primarily for tests to keep leak detection clean.
        /// </summary>
        public static void ClearAll()
        {
            foreach (var kvp in _blobById)
            {
                if (kvp.Value.IsCreated)
                    kvp.Value.Dispose();
            }

            _blobById.Clear();
            _byId.Clear();
        }

        public static bool TryGet(in FixedString64Bytes id, out SpellDefinition def)
        {
            if (_byId.TryGetValue(id, out var found))
            {
                def = found;
                return true;
            }
            def = default;
            return false;
        }

        public static bool TryGetBlob(in FixedString64Bytes id, out BlobAssetReference<SpellDefinitionBlob> blob)
        {
            if (_blobById.TryGetValue(id, out blob) && blob.IsCreated)
                return true;

            blob = default;
            if (!_byId.TryGetValue(id, out var def))
                return false;

            blob = SpellDefinitionBlobUtility.Create(def);
            _blobById[id] = blob;
            return true;
        }

        public static void LoadFromJson(string json)
        {
            var list = JsonConvert.DeserializeObject<List<SpellDefinition>>(json);
            if (list == null) return;
            foreach (var def in list)
                Register(def);
        }

        private static SpellDefinition NormalizeDefinition(in SpellDefinition def)
        {
            var normalized = def;
            normalized.CategoryId = NormalizeCategory(def.CategoryId);
            normalized.Blocks = EffectBlockConverter.Resolve(normalized);
            return normalized;
        }

        private static FixedString32Bytes NormalizeCategory(in FixedString32Bytes category)
        {
            if (category.Length == 0) return category;
            var lower = category.ToString().ToLowerInvariant();
            return (FixedString32Bytes)lower;
        }
    }
}
