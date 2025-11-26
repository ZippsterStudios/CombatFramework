---
title: Resources Subsystem
tags: [subsystem, resources, health]
updated: 2025-10-26
---

# Resources Subsystem (Health/Mana/Stamina)

> **Scheduling:** `ResourceRuntimeSystem` lives in `RequestsSystemGroup` (consumes `ResourceRequest` buffers), and `ResourceResolutionSystem` lives in `ResolutionSystemGroup` (regen). Consumers should not depend on either system directly—always go through `ResourceFactory`.  
> **Timebase:** Regen uses `SystemAPI.Time.DeltaTime`; timers referencing cooldowns/elapsed durations use `SystemAPI.Time.ElapsedTime`. All pool values are integers.

```mermaid
flowchart LR
    Producers --> Requests[ResourceRequest buffers]
    Requests --> Runtime[ResourceRuntimeSystem]
    Runtime --> Resolution[ResourceResolutionSystem]
    Resolution --> Consumers[UI / Telemetry]
```

### Responsibilities

- Provide component data for pools (`Health`, `Mana`, `Stamina`).  
- Handle clamped delta application via `ResourceFactory` and request buffers.  
- Tick regeneration in `ResourceResolutionSystem`, respecting `TemporalModifiers`.

### Key types

| Type | Purpose |
| --- | --- |
| `Framework.Resources.Components.*` | Per-entity pool data with regen accumulators. |
| `ResourceFactory` | Centralized helpers for init and delta application. |
| `ResourceRequest` | Buffer used by jobs or gameplay code to queue resource changes. |
| `ResourceRuntimeSystem` | Consumes requests each frame and clears buffers. |
| `ResourceResolutionSystem` | Applies regen with optional temporal modifiers. |

### Units & invariants

- Pool values (`Current`, `Max`) are `int`s; regen accumulators are `float` so fractional regen works.  
- All regen rates are “per second”; multiply by `DeltaTime` internally.  
- Pass positive deltas for gains, negative for spending. Factories clamp to `[0, Max]`.

### Buffer ownership & lifetime

- Each entity owns its `ResourceRequest` buffer. Producers must call `ResourceFactory` helpers which create the buffer if missing.  
- `ResourceRuntimeSystem` clears buffers every frame after processing.  
?- Factories add components lazily (e.g., `Health`) so authoring systems don’t need to pre-bake them.

### Telemetry hooks

- Capture health/mana delta stats in a `TelemetrySystemGroup` system that reads `Health`/`Mana` after resolution.  
- For UI sync, mirror values to HUD/network after the Resolution phase only.  
- Stub helpers like `HudBridge` in examples are placeholders; replace them with your project-specific bridge.

### Performance notes

- Buffer scans are O(#entities with requests). Batch multiple deltas per entity to reduce structural changes.  
- Regen jobs run Burst-compiled; keep per-entity math lightweight.  
- Avoid per-frame `EntityManager` structural changes—use `ResourceFactory.Init*` once at spawn.

### Example: Spawning an entity with regen

```csharp
using Framework.Resources.Factory;

void SpawnPlayer(ref EntityManager em, Entity player)
{
    if (!em.Exists(player))
        return;

    ResourceFactory.InitHealth(ref em, player, max: 1000, regenPerSecond: 5);
    ResourceFactory.InitMana(ref em, player, max: 500, regenPerSecond: 12);
    ResourceFactory.InitStamina(ref em, player, max: 150, regenPerSecond: 20);
}
```

### Example: Queueing resource deltas

```csharp
using Framework.Resources.Requests;

void SpendMana(ref EntityManager em, Entity caster, int amount)
{
    if (!em.Exists(caster))
        return;

    if (!em.HasBuffer<ResourceRequest>(caster))
        em.AddBuffer<ResourceRequest>(caster);

    em.GetBuffer<ResourceRequest>(caster).Add(new ResourceRequest
    {
        Target = caster,
        Kind = ResourceKind.Mana,
        Delta = -math.abs(amount)
    });
}
```

`ResourceRuntimeSystem` clamps each delta to the pool’s `[0, Max]` range and clears the buffer automatically.

### Detailed example: integrating damage, regen, and UI sync

1. **Initialization** – call `ResourceFactory.Init*` during spawn/convert.  
2. **Gameplay deltas** – enqueue `ResourceRequest` entries from systems (damage, spells, consumables).  
3. **Resolution** – `ResourceResolutionSystem` performs regen (scaled by `TemporalModifiers`).  
4. **Presentation** – after resolution, read `Health` for UI or replication.

```csharp
public static class HealthUtility
{
    public static void ApplyHit(ref EntityManager em, Entity victim, int amount)
        => QueueDelta(ref em, victim, ResourceKind.Health, -math.abs(amount));

    public static void DrinkPotion(ref EntityManager em, Entity target, int amount)
        => QueueDelta(ref em, target, ResourceKind.Health, math.abs(amount));

    static void QueueDelta(ref EntityManager em, Entity entity, ResourceKind kind, int delta)
    {
        if (!em.Exists(entity))
            return;

        if (!em.HasBuffer<ResourceRequest>(entity))
            em.AddBuffer<ResourceRequest>(entity);

        em.GetBuffer<ResourceRequest>(entity).Add(new ResourceRequest
        {
            Target = entity,
            Kind = kind,
            Delta = delta
        });
    }
}

[UpdateInGroup(typeof(Framework.Core.Base.TelemetrySystemGroup))]
public partial struct HealthUiSyncSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (health, entity) in SystemAPI.Query<RefRO<Framework.Resources.Components.Health>>().WithEntityAccess())
        {
            HudBridge.PushHealth(entity, health.ValueRO.Current, health.ValueRO.Max);
        }
    }
}
```

_(`HudBridge` is a stub for documentation—swap in your real UI adapter.)_

### See also

- [`Damage.md`](Damage.md) / [`Heal.md`](Heal.md) – subsystems that mutate health.  
- [`Temporal.md`](Temporal.md) – explains how haste/slow scales regen.  
- [`Cooldowns.md`](Cooldowns.md) – uses the same elapsed-time clock for ready times.  
- [`Spells.md`](Spells.md) – requests that spend resources during casting.
