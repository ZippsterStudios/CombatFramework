# Debuff Subsystem

Debuffs mirror the existing Buff/DOT/HOT pipelines:

- **Content**: Author `DebuffDefinition` assets and register them through `DebuffCatalog`.
- **Factory/Requests**: Queue debuff applications with `DebuffFactory.Enqueue` (or apply immediately in tests).
- **Driver**: `DebuffDriver` handles stacking rules, duration refresh, and telemetry.
- **Runtime**: `DebuffRuntimeSystem` consumes queued requests each frame.
- **Resolution**: `DebuffResolutionSystem` ticks timers, expires debuffs, and maintains aggregated crowd-control & stat modifier state per entity.

Crowd-control flags (root, fear, mez, stun, etc.) are tracked via `DebuffCrowdControlState`. Stat modifiers aggregate into `DebuffStatAggregate` for downstream systems (e.g., movement, AI, or stat evaluation) to consume.
