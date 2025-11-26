---
title: Temporal Subsystem
tags: [subsystem, temporal, haste]
updated: 2025-10-26
---

# Temporal Subsystem

> **Scheduling:** `TemporalRuntimeSystem` (anchors), `TemporalAnchorSystem`, and `TemporalReleaseSystem` all run in `Framework.Core.Base.RuntimeSystemGroup`, while `TemporalReleaseHotSystem` finishes in `ResolutionSystemGroup`. Interact only through `TemporalDriver`/`TemporalAnchorDriver`.  
> **Timebase:** All durations use seconds (`SystemAPI.Time.DeltaTime`). Haste/slow percentages are normalized floats (0.25 = 25% faster). Instant requests (one-shot damage/heal) are **not** affected; only systems that read `TemporalModifiers` apply scaling (regen, DOT/HOT, cooldown resolution, timed effects).

```mermaid
flowchart LR
    Apply[TemporalDriver (haste/slow)] --> Modifiers[TemporalModifiers]
    Modifiers --> Consumers[Resources/DOT/HOT/Cooldowns]
    Damage --> Anchor[TemporalAnchorDriver.RecordDamage] --> Release[TemporalReleaseSystem]
    Release --> Hot[TemporalReleaseHotSystem] --> Heal
```

### Responsibilities

- Apply haste/slow modifiers via `TemporalModifiers`.  
- Record bursts of incoming damage inside `TemporalAnchor` buffers so later abilities can “rewind” damage.  
- Convert accumulated damage into delayed heals (`TemporalReleaseSystem` + `TemporalReleaseHotSystem`).

### Key types

| Type | Purpose |
| --- | --- |
| `TemporalModifiers` | Lightweight haste/slow component read by regen, DOT, HOT, cooldown, and timed effect systems. |
| `TemporalAnchor` + `TemporalEvent` | Captures timestamped damage/heal history for a target. |
| `TemporalReleaseRequest/Result` | Specifies how to convert stored damage into future healing. |
| `TemporalDriver` | Burst-safe helpers to apply haste/slow. |
| `TemporalAnchorDriver` | High-level API for attaching anchors, recording damage, and queuing releases. |
| `TemporalRuntimeSystem` family | Maintains anchors, trims history, and hands results to the HOT subsystem. |

### Units & invariants

- `HastePercent` and `SlowPercent` represent additive percentages (0.15 = 15%).  
- Interval multipliers clamp to `[0.1, 10]` inside `TemporalPolicy`.  
- Anchors store timestamps relative to `ElapsedTime` and assume monotonic clocks.

### Buffer ownership & lifetime

- `TemporalModifiers` lives on affected entities until explicitly removed (e.g., by clearing the component).  
- Anchors (`TemporalAnchor` + `TemporalEvent` + release buffers) are owned by `TemporalAnchorDriver`; calling `ClearAnchor` removes all related components.  
- `TemporalReleaseResult` buffers stick around until `TemporalReleaseHotSystem` consumes and clears them.

### Telemetry hooks

- Emit haste/slow start/end events when calling `TemporalDriver` to help QA confirm stacking behavior.  
- Temporal release already routes through HOT telemetry, so you only need custom logs if you create bespoke release consumers.

### Performance notes

- Haste/slow modifiers are simple components, so feel free to toggle them often.  
- Anchors store per-event history; keep retention windows tight to avoid unbounded buffers.  
- Release calculations iterate over recent events only (bounded by `Retention`), making them suitable for raid-scale mechanics.

### Example: Applying haste and slow

```csharp
using Framework.Temporal.Drivers;

void ApplyTemporalEffects(ref EntityManager em, Entity agent)
{
    if (!em.Exists(agent))
        return;

    TemporalDriver.ApplyHaste(ref em, agent, hastePercent: 0.25f); // 25% faster ticks
    TemporalDriver.ApplySlow(ref em, agent, slowPercent: 0.15f);   // later replace haste with slow
}
```

Any system that reads `TemporalModifiers` automatically reacts (e.g., `ResourceResolutionSystem` divides `DeltaTime` by the interval multiplier).

### Example: Recording and releasing anchors

```csharp
using Framework.Temporal.Drivers;

void StartAnchor(ref EntityManager em, Entity target, Entity source)
{
    TemporalAnchorDriver.AttachAnchor(ref em, target, source, duration: 12f, retention: 6f);
}

void RecordBurstDamage(ref EntityManager em, Entity target, float damage)
{
    TemporalAnchorDriver.RecordDamage(ref em, target, damage);
}

void TriggerRelease(ref EntityManager em, Entity target, Entity source)
{
    TemporalAnchorDriver.QueueRelease(
        ref em,
        target,
        source,
        factor: 0.8f,
        windowSeconds: 5f,
        healDuration: 5f,
        healTickInterval: 0.5f);
}
```

`TemporalReleaseSystem` aggregates queued releases and writes `TemporalReleaseResult` buffers. `TemporalReleaseHotSystem` converts those rows into live HOT instances via `HotFactory.Enqueue`.

### Detailed example: temporal reversal ability

The following pattern mimics a “Spirit Link” ability that records all damage on a tank for 8 seconds, then heals them for 60% of that damage over 3 seconds:

```csharp
// Step 1 — attach anchor at spell start
TemporalAnchorDriver.AttachAnchor(ref em, targetTank, caster, duration: 8f, retention: 8f);

// Step 2 — inside DamageRuntimeSystem after mitigation
TemporalAnchorDriver.RecordDamage(ref em, targetTank, mitigated);

// Step 3 — on spell end
TemporalAnchorDriver.QueueRelease(
    ref em,
    targetTank,
    caster,
    factor: 0.60f,
    windowSeconds: 8f,
    healDuration: 3f,
    healTickInterval: 0.5f);

// Step 4 — TemporalReleaseHotSystem converts the release into HoTs automatically.
```

Because the anchor driver enforces ownership (same source entity) you can have multiple temporal effects on different friendly units at once without cross-talk.

### See also

- [`TimedEffect.md`](TimedEffect.md) – haste/slow affects timed effect scheduling.  
- [`Resources.md`](Resources.md) – regen scaling example.  
- [`DamageOverTime.md`](DamageOverTime.md) & [`HealOverTime.md`](HealOverTime.md) – both read `TemporalModifiers`.  
- [`Cooldowns.md`](Cooldowns.md) – cooldown ready times are **not** scaled; only interval-based systems are.  
- [`Spells.md`](Spells.md) – applying temporal effects from spell payloads.
