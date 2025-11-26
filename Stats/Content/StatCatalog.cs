using System;
using Newtonsoft.Json;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Framework.Stats.Content
{
    public struct StatProfileBlob
    {
        public FixedString64Bytes Id;
        public BlobArray<StatEntry> Entries;
    }

    public static class StatCatalog
    {
        private struct MapTag { }
        private struct InitTag { }

        private static readonly SharedStatic<UnsafeHashMap<FixedString64Bytes, BlobAssetReference<StatProfileBlob>>> _map =
            SharedStatic<UnsafeHashMap<FixedString64Bytes, BlobAssetReference<StatProfileBlob>>>.GetOrCreate<MapTag>();
        private static readonly SharedStatic<bool> _initialized =
            SharedStatic<bool>.GetOrCreate<InitTag>();

        public static void Register(in StatProfile profile)
        {
            EnsureInitialized();
            if (!_initialized.Data || !_map.Data.IsCreated)
                return;

            var entries = profile.Entries ?? Array.Empty<StatEntry>();

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<StatProfileBlob>();
            root.Id = profile.Id;
            var blobEntries = builder.Allocate(ref root.Entries, entries.Length);
            for (int i = 0; i < entries.Length; i++)
                blobEntries[i] = entries[i];

            var blob = builder.CreateBlobAssetReference<StatProfileBlob>(Allocator.Persistent);
            builder.Dispose();

            ref var map = ref _map.Data;
            if (map.TryGetValue(profile.Id, out var existing))
            {
                if (existing.IsCreated)
                    existing.Dispose();
                map.Remove(profile.Id);
            }

            map.Add(profile.Id, blob);
        }

        public static bool TryGetBlob(in FixedString64Bytes id, out BlobAssetReference<StatProfileBlob> blob)
        {
            EnsureInitialized();
            if (!_initialized.Data || !_map.Data.IsCreated)
            {
                blob = BlobAssetReference<StatProfileBlob>.Null;
                return false;
            }

            return _map.Data.TryGetValue(id, out blob);
        }

        public static bool TryGet(in FixedString64Bytes id, out BlobAssetReference<StatProfileBlob> blob) =>
            TryGetBlob(id, out blob);

        [BurstDiscard]
        public static bool TryGetManaged(in FixedString64Bytes id, out StatProfile profile)
        {
            if (TryGetBlob(id, out var blob) && blob.IsCreated)
            {
                var length = blob.Value.Entries.Length;
                var entries = new StatEntry[length];
                for (int i = 0; i < length; i++)
                    entries[i] = blob.Value.Entries[i];

                profile = new StatProfile
                {
                    Id = blob.Value.Id,
                    Entries = entries
                };
                return true;
            }

            profile = default;
            return false;
        }

        public static void LoadFromJson(string json)
        {
            var list = JsonConvert.DeserializeObject<StatProfile[]>(json);
            if (list == null) return;
            for (int i = 0; i < list.Length; i++)
                Register(in list[i]);
        }

        private static void EnsureInitialized()
        {
            if (_initialized.Data) return;
            InitializeManaged();
        }

        [BurstDiscard]
        private static void InitializeManaged()
        {
            if (_initialized.Data) return;
            _map.Data = new UnsafeHashMap<FixedString64Bytes, BlobAssetReference<StatProfileBlob>>(32, Allocator.Persistent);
            _initialized.Data = true;
            AppDomain.CurrentDomain.DomainUnload += (_, __) => DisposeManaged();
        }

        [BurstDiscard]
        private static void DisposeManaged()
        {
            if (!_initialized.Data) return;
            if (_map.Data.IsCreated)
            {
                var enumerator = _map.Data.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var blob = enumerator.Current.Value;
                    if (blob.IsCreated)
                        blob.Dispose();
                }
                _map.Data.Dispose();
            }
            _initialized.Data = false;
        }
    }
}
