---
title: Damage Bypass Flags
tags: [subsystem, damage, reference]
updated: 2025-10-26
---

# Damage Bypass Flags

> **Scheduling:** Flags are evaluated inside `DamageRuntimeSystem` (RuntimeSystemGroup). Producers merely set the bytes on `DamagePacket`; the runtime decides how to honor them.

```mermaid
flowchart LR
    SpellPayload --> Packet[DamagePacket]
    Packet --> Runtime[DamageRuntimeSystem]
    Runtime --> Mitigation[Mitigation Steps]
```

| Flag | Effect | Example usage |
| --- | --- | --- |
| `IgnoreArmor` | Forces the armor input to zero before calling `DamagePolicy.Mitigate`. | True-strike melee hits, phased magic attacks. |
| `IgnoreResist` | Forces the resist percent to zero after the armor step. | Pure holy damage or mechanics that bypass elemental resistances. |
| `IgnoreSnapshotModifiers` | Skips `BuffStatSnapshot` defense multipliers and reflection fields. | “Unblockable” hits, damage that should not be reduced by shields. |

### Debug recipe

```csharp
// Enable verbose logs for one frame
Framework.Damage.Runtime.DamageDebugBridge.EnableDebugLogs(true);
```

Damage logs show the raw amount, post-variance, armor/resist inputs, and flag states, making it easier to reason about the mitigation chain described in `Damage.md`.

### See also

- [`Damage.md`](Damage.md) – canonical order of operations.  
- [`Spells.md`](Spells.md) – how payloads set bypass flags.  
- [`TimedEffect.md`](TimedEffect.md) – stackable effects that may supply snapshot multipliers.
