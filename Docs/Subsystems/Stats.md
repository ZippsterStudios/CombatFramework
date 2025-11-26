---
title: Stats Subsystem
tags: [subsystem, stats]
updated: 2025-10-26
---

# Stats Subsystem

> **Scheduling:** `StatRuntimeSystem` runs in `Framework.Core.Base.RuntimeSystemGroup` (consumes `StatRequest`s). `StatsResolutionSystem` runs in `ResolutionSystemGroup` (recomputes cached values). Producers should modify stats only through `StatFactory`.  
> **Timebase:** Stats themselves are timeless; only auxiliary systems (like decay timers) rely on `SystemAPI.Time.DeltaTime`.

```mermaid
flowchart LR
    Producers[Buffs / Scripts / Items] --> Requests[StatRequest buffers]
    Requests --> Runtime[StatRuntimeSystem]
    Runtime --> Resolution[StatsResolutionSystem]
    Resolution --> Consumers[Damage / UI / AI]
```

### Responsibilities

- Maintain per-entity stats using the `(BaseValue + Additive) * Multiplier` pattern.  
- Provide request buffers for additive changes (Burst-friendly).  
- Recompute cached `Value` each frame in `StatsResolutionSystem`.

### Key types

| Type | Purpose |
| --- | --- |
| `StatValue` | Component storing base/additive/multiplier/cached value. |
| `StatFactory` | Helpers to set base values, additive deltas, and multipliers. |
| `StatRequest` | Buffer element with `(Target, Delta)` for additive changes. |
| `StatRuntimeSystem` | Consumes requests and updates `StatValue`. |
| `StatsResolutionSystem` | Recomputes cached values every frame. |

### Units & invariants

- Formula is **always** `(BaseValue + Additive) * Multiplier`. Do not write custom math per stat.  
- Clamp multipliers to ≥ 0 to avoid negative stats (the factory already enforces this).  
- Store derived values (crit chance, haste %) as floats; convert to ints only when applying gameplay effects.

### Buffer ownership & lifetime

- Each entity may own a `DynamicBuffer<StatRequest>`. The factory adds/removes it automatically, and `StatRuntimeSystem` clears it after processing.  
- `StatValue` components are persistent; only the cached `Value` field is recomputed each frame.

### Telemetry hooks

- Track stat changes (e.g., “AttackPowerModified”) by listening for buffers before they clear or by diffing `StatValue.Value` in a telemetry pass.  
- For debugging, log the base/additive/multiplier tuple rather than just the cached value.

### Performance notes

- Applying additive deltas via requests keeps Burst-critical loops free of structural changes.  
- Keep per-entity stat counts small; if you need dozens of stats, consider storing them in a blob to keep chunk data compact.  
- Avoid re-running expensive calculations outside the resolution system—always read `StatValue.Value`.

### Example: Initializing stats

```csharp
using Framework.Stats.Factory;

void InitStats(ref EntityManager em, Entity agent)
{
    if (!em.Exists(agent))
        return;

    StatFactory.SetBase(ref em, agent, baseValue: 120f);
    StatFactory.SetMultiplier(ref em, agent, multiplier: 1.10f);
}
```

### Example: Applying additive bonuses via requests

```csharp
using Framework.Stats.Requests;

void AddTemporaryStrength(ref EntityManager em, Entity target, int delta)
{
    if (!em.Exists(target))
        return;

    if (!em.HasBuffer<StatRequest>(target))
        em.AddBuffer<StatRequest>(target);

    em.GetBuffer<StatRequest>(target).Add(new StatRequest
    {
        Target = target,
        Delta = delta
    });
}
```

Consumers should read `StatValue.Value`; it always reflects the latest modifiers.

### Detailed example: stat-driven buff with decay

Suppose you need a “Battle Focus” mechanic that grants +5 attack per stack and decays 1 stack every 3 seconds:

```csharp
public struct BattleFocus : IComponentData
{
    public int Stacks;
    public float TimeUntilDecay;
}

[UpdateInGroup(typeof(Framework.Core.Base.RuntimeSystemGroup))]
public partial struct BattleFocusRuntimeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var em = state.EntityManager;
        foreach (var (focus, entity) in SystemAPI.Query<RefRW<BattleFocus>>().WithEntityAccess())
        {
            if (CritTracker.TryConsumeCrit(entity)) // stub for docs
            {
                focus.ValueRW.Stacks++;
                StatFactory.ApplyAdditive(ref em, entity, additiveDelta: 5);
                focus.ValueRW.TimeUntilDecay = 3f;
            }
        }
    }
}

[UpdateInGroup(typeof(Framework.Core.Base.ResolutionSystemGroup))]
public partial struct BattleFocusDecaySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        var em = state.EntityManager;
        foreach (var (focus, entity) in SystemAPI.Query<RefRW<BattleFocus>>().WithEntityAccess())
        {
            focus.ValueRW.TimeUntilDecay -= dt;
            if (focus.ValueRW.TimeUntilDecay > 0f)
                continue;

            focus.ValueRW.TimeUntilDecay = 3f;
            focus.ValueRW.Stacks--;
            StatFactory.ApplyAdditive(ref em, entity, additiveDelta: -5);
            if (focus.ValueRW.Stacks <= 0)
                em.RemoveComponent<BattleFocus>(entity);
        }
    }
}
```

_(Helpers like `CritTracker` are stubs for documentation.)_

This pattern keeps all stat math centralized in `StatFactory` so downstream systems never need to recompute `(Base + Additive) * Multiplier` themselves.

### See also

- [`Buffs.md`](Buffs.md) – primary producer of stat changes.  
- [`Damage.md`](Damage.md) / [`Resources.md`](Resources.md) – systems that read stats for mitigation/regeneration.  
- [`Spells.md`](Spells.md) – stat-based scaling inside effect payloads.
