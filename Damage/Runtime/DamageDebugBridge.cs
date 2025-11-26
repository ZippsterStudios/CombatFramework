namespace Framework.Damage.Runtime
{
    /// <summary>
    /// Public entry point that allows Editor tools and gameplay harnesses to toggle damage logging
    /// without taking a dependency on the internal logger implementation.
    /// </summary>
    public static class DamageDebugBridge
    {
        public static void EnableDebugLogs(bool enabled) => DamageDebug.SetEnabled(enabled);
    }
}
