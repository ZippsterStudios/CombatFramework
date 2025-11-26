using Unity.Burst;

namespace Framework.Core.Telemetry
{
    public static class TelemetryTime
    {
        private struct ElapsedSecondsTag { }

        private static readonly SharedStatic<double> _elapsed =
            SharedStatic<double>.GetOrCreate<ElapsedSecondsTag>();

        public static double ElapsedSeconds
        {
            get => _elapsed.Data;
            set => _elapsed.Data = value;
        }
    }
}
