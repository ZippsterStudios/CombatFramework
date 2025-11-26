# HOT Subsystem

- Applies periodic, deterministic healing using `HotInstance` buffer elements.
- Driver (`ExampleHotDriver`) appends stacks with 1s tick and duration.
- Resolution system consumes `TimedEffectEvent` ticks to heal via `ExampleResourceDriver.ApplyHealthDelta`.
- Expired instances are removed; stacking is additive.
- Registered via `HotSubsystemManifest`.
