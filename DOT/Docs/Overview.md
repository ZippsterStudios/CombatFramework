# DOT Subsystem

- Applies periodic damage using `DotInstance` buffer elements.
- Driver (`ExampleDotDriver`) appends stacks with 1s tick and duration.
- Resolution system consumes `TimedEffectEvent` ticks, applying mitigated damage (armor then resist%).
- Damage applies through `ExampleResourceDriver` as negative Health; emits `DotTick` telemetry.
- Expired instances are removed; stacking is additive.
- Registered via `DotSubsystemManifest`.
