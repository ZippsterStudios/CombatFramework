# Buffs Subsystem

- Represents active buffs as `BuffInstance` buffer elements (id, stacks, time remaining).
- Driver (`ExampleBuffDriver`) applies or updates stacks/duration by `BuffId`.
- Resolution system decrements timers and removes expired entries; stacking is additive.
- Telemetry hooks available via subsystem systems; pure policies validate caps and rules.
- Registers via `BuffSubsystemManifest`.

