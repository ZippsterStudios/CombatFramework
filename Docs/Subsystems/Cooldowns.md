---
title: Cooldown Subsystem
tags: [subsystem, cooldowns, timing]
updated: 2025-10-26
---

# Cooldown Subsystem

> **Scheduling:** `CooldownResolutionSystem` runs in `Framework.Core.Base.ResolutionSystemGroup`. Always set/read cooldowns through `CooldownFactory`/`CooldownDriver`; never touch the buffer directly in other systems.  
> **Timebase:** Ready times are stored as `double` values derived from `SystemAPI.Time.ElapsedTime`. Use the same clock when checking readiness.

```mermaid
flowchart LR
    Spend[Ability Spend] --> Factory[CooldownFactory.Apply]
    Factory --> Buffer[DynamicBuffer<CooldownGroup>]
    Buffer --> Resolution[CooldownResolutionSystem]
    Resolution --> Cleanup[Entries removed when ready]
```

### Responsibilities

- Track cooldown groups per entity using `DynamicBuffer<CooldownGroup>`.  
- Provide read/write helpers for setting or querying cooldown readiness.  
- Prune expired entries automatically in `CooldownResolutionSystem`.

### Key types

| Type | Purpose |
| --- | --- |
| `CooldownGroup` | Buffer element storing `GroupId` and `ReadyTime` (seconds). |
| `CooldownFactory` | Applies cooldowns (uses `max` logic, respects existing entries). |
| `CooldownDriver` | Burst-friendly read/write helpers for gameplay systems. |
| `CooldownResolutionSystem` | Removes entries where `now >= ReadyTime`. |

### Units & invariants

- Ready times are absolute (`ElapsedTime + duration`), not durations.  
- `CooldownFactory.ApplyCooldown` writes `max(existing.ReadyTime, newReadyTime)` to avoid shortening active cooldowns.  
- If the simulation clock moves backward (e.g., server time skew), `CooldownResolutionSystem` keeps entries until the clock catches back up—ready times never decrease automatically.

### Buffer ownership & lifetime

- Each entity owns a single `DynamicBuffer<CooldownGroup>`. Let `CooldownFactory` create it lazily.  
- `CooldownResolutionSystem` trims entries when the cooldown expires. Do not remove entries manually; just call the factory again.  
- Storing multiple groups (ability-level + category-level) in the same buffer is encouraged.

### Telemetry hooks

- Emit cooldown start/ready events from higher-level systems (spellcasting, ability managers) rather than inside the cooldown subsystem to keep it generic.  
- For debugging, log `ReadyTime - now` to confirm the correct durations, especially when stacking category cooldowns.

### Performance notes

- Buffers are typically tiny (a handful of groups per entity). The resolution loop simply iterates and removes items when ready.  
- Avoid per-frame writes—only touch the buffer when spending an ability or forcibly resetting cooldowns (e.g., raid wipe).

### Example: Starting a cooldown

```csharp
using Framework.Cooldowns.Factory;

void StartCooldown(ref EntityManager em, Entity caster, float seconds)
{
    if (!em.Exists(caster))
        return;

    var groupId = (FixedString64Bytes)"spell.fireball";
    double readyTime = SystemAPI.Time.ElapsedTime + seconds;
    CooldownFactory.ApplyCooldown(ref em, caster, groupId, readyTime);
}
```

### Example: Checking readiness before casting

```csharp
using Framework.Cooldowns.Drivers;

bool CanCast(ref EntityManager em, Entity caster)
{
    if (!em.Exists(caster))
        return false;

    var groupId = (FixedString64Bytes)"spell.fireball";
    double now = SystemAPI.Time.ElapsedTime;
    return !CooldownDriver.IsOnCooldown(em, caster, groupId, now);
}
```

Always gate spell requests with cooldown checks to avoid unnecessary pipeline work.

### Detailed example: multi-group cooldown controller

Consider an ability that should put both “Fireball” and the shared “Fire School” group on cooldown:

```csharp
static readonly FixedString64Bytes FireballId = (FixedString64Bytes)"spell.fireball";
static readonly FixedString64Bytes FireSchoolId = (FixedString64Bytes)"school.fire";

void SpendFireball(ref EntityManager em, Entity caster, float fireballCd, float schoolCd)
{
    double now = SystemAPI.Time.ElapsedTime;
    CooldownFactory.ApplyCooldown(ref em, caster, FireballId, now + fireballCd);
    CooldownFactory.ApplyCooldown(ref em, caster, FireSchoolId, now + schoolCd);
}

bool IsFireballReady(ref EntityManager em, Entity caster)
{
    double now = SystemAPI.Time.ElapsedTime;
    if (CooldownDriver.IsOnCooldown(em, caster, FireSchoolId, now))
        return false;
    return !CooldownDriver.IsOnCooldown(em, caster, FireballId, now);
}
```

This pattern mirrors MMO-style lockouts: category cooldowns (school-level) and ability cooldowns (single-spell) share the same buffer and resolution flow.

### See also

- [`Spells.md`](Spells.md) – cooldown checks inside the spell pipeline.  
- [`Resources.md`](Resources.md) – spending resources happens alongside cooldown checks.  
- [`Temporal.md`](Temporal.md) – haste/slow effects that **do not** change absolute cooldown clocks (only timed effects/regen).  
- [`Lifecycle.md`](Lifecycle.md) – explains why cooldown cleanup lives in the Resolution phase.
