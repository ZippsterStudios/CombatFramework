Combat Framework Overview

- Subsystems follow a layered pattern: Components, Content, Policies, Drivers, Factory, Requests, Resolution, Runtime, Telemetry, Utilities.
- System groups: RequestsSystemGroup, ResolutionSystemGroup, RuntimeSystemGroup, TelemetrySystemGroup order execution.
- Structural changes are performed via EntityManager in non-parallel updates; drivers encapsulate write-side logic and factories provide enqueue/spawn helpers.

Lifecycle

- Requests: Factories add buffer elements (e.g., SpellCastRequest, DamageRequest, HealRequest).
- Runtime: Systems consume requests and call Drivers to mutate ECS state.
- Resolution: Systems tick time-based state (DOT/HOT, Buff/Debuff durations, Cooldowns cleanup, AreaEffect lifetime) and emit telemetry.
- Telemetry: Non-invasive event emissions via TelemetryRouter.Enable/Emit for analytics and debugging.
