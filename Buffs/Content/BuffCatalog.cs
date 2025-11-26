using System;
using Newtonsoft.Json;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Framework.Buffs.Content
{
    public static class BuffCatalog
    {
        private struct MapTag { }
        private struct InitTag { }

        private static readonly SharedStatic<UnsafeHashMap<FixedString64Bytes, BuffDefinition>> _map =
            SharedStatic<UnsafeHashMap<FixedString64Bytes, BuffDefinition>>.GetOrCreate<MapTag>();
        private static readonly SharedStatic<bool> _initialized =
            SharedStatic<bool>.GetOrCreate<InitTag>();

        public static void Register(in BuffDefinition def)
        {
            EnsureInitialized();
            if (!_initialized.Data || !_map.Data.IsCreated)
                return;

            ref var map = ref _map.Data;
            if (!map.TryAdd(def.Id, def))
            {
                map.Remove(def.Id);
                map.Add(def.Id, def);
            }
        }

        public static bool TryGet(in FixedString64Bytes id, out BuffDefinition def)
        {
            EnsureInitialized();
            if (!_initialized.Data || !_map.Data.IsCreated)
            {
                def = default;
                return false;
            }

            return _map.Data.TryGetValue(id, out def);
        }

        public static void LoadFromJson(string json)
        {
            var list = JsonConvert.DeserializeObject<BuffDefinition[]>(json);
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
            _map.Data = new UnsafeHashMap<FixedString64Bytes, BuffDefinition>(32, Allocator.Persistent);
            _initialized.Data = true;
            AppDomain.CurrentDomain.DomainUnload += (_, __) => DisposeManaged();
        }

        [BurstDiscard]
        private static void DisposeManaged()
        {
            if (!_initialized.Data) return;
            if (_map.Data.IsCreated)
                _map.Data.Dispose();
            _initialized.Data = false;
        }
    }
}
