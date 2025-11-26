using System;
using Framework.Spells.Content;
using Unity.Collections;

namespace Framework.Spells.Runtime
{
    public struct EffectResultLedger : IDisposable
    {
        private NativeArray<float> _damage;
        private NativeArray<float> _heal;

        public EffectResultLedger(int blockCount, Allocator allocator)
        {
            _damage = new NativeArray<float>(blockCount, allocator, NativeArrayOptions.ClearMemory);
            _heal = new NativeArray<float>(blockCount, allocator, NativeArrayOptions.ClearMemory);
        }

        public void Record(int blockIndex, EffectResultSource source, float value)
        {
            if (blockIndex < 0) return;
            switch (source)
            {
                case EffectResultSource.Damage:
                    _damage[blockIndex] += value;
                    break;
                case EffectResultSource.Heal:
                    _heal[blockIndex] += value;
                    break;
            }
        }

        public float ResolveRelative(int currentIndex, sbyte relativeOffset, EffectResultSource source)
        {
            int index = currentIndex + relativeOffset;
            if (index < 0 || index >= _damage.Length)
                return 0f;
            return source == EffectResultSource.Damage ? _damage[index] : _heal[index];
        }

        public void Dispose()
        {
            if (_damage.IsCreated) _damage.Dispose();
            if (_heal.IsCreated) _heal.Dispose();
        }
    }
}
