using System;
using Unity.Burst;
using Unity.Collections;

namespace Framework.Core.Telemetry
{
    public static class TelemetryRouter
    {
        public struct TelemetryEvent
        {
            public FixedString64Bytes Tag;
            public int Value;
            public double Time;
        }

        private struct EnabledTag { }

        private static readonly TelemetryEvent[] _ring = new TelemetryEvent[1024];
        private static int _writeIndex;
        private static bool _enabledManaged = true;
        private static readonly SharedStatic<int> _enabledFlag = SharedStatic<int>.GetOrCreate<EnabledTag>();

        [BurstDiscard]
        public static void SetEnabled(bool enabled)
        {
            _enabledManaged = enabled;
            _enabledFlag.Data = enabled ? 1 : 0;
        }

        public static bool IsEnabled() => _enabledFlag.Data != 0;

        [BurstDiscard]
        public static void Emit(in FixedString64Bytes tag, int value)
        {
            if (!_enabledManaged) return;
            int idx = _writeIndex++ & (_ring.Length - 1);
            _ring[idx] = new TelemetryEvent
            {
                Tag = tag,
                Value = value,
                Time = TelemetryTime.ElapsedSeconds
            };
        }

        public static ReadOnlySpan<TelemetryEvent> Snapshot(out int newestIndex)
        {
            newestIndex = _writeIndex;
            return _ring;
        }
    }
}
