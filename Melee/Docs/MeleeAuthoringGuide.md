# Melee Authoring Guide

This guide explains how to author melee weapons, defense tuning, and proc tables for the Framework.Melee subsystem.

## Weapon Definitions

1. Create or select a `BlobAssetReference<MeleeWeaponDefBlob>` in authoring tools.
2. Populate base timings (windup/active/recovery), range, baseline arc, penetration, stamina cost, lockout duration, bypass flags, and proc table reference.
3. Link the blob to an attacker by adding a `DynamicBuffer<MeleeWeaponSlotElement>` and assigning one slot per weapon (e.g., `MainHand`, `OffHand`, `TailSwipe`).
4. Optional overrides allow class/buff data to adjust weapon behavior without mutating the original blob.

## Defense Tuning

1. Add `MeleeDefenseTuning` to defenders with final stat-derived values for dodge, parry, block, guard pools, and riposte policy.
2. Parry windows supplied by `TimedEffect` should toggle `MeleeDefenseWindowState` to give the runtime systems a fast lookup path.
3. Guard resources reference existing stamina/guard pools in the Resources subsystem; the melee systems will spend from them when blocks succeed.

## Proc Tables

1. Author `MeleeProcTableBlob` assets describing each proc entry (chance, ICD, caps, payload).
2. Weapon blobs reference proc tables; buffs can add additional proc entries via runtime buffers that merge with weapon defaults.
3. Payloads map to subsystem factories (Damage, DOT, HOT, Buffs, AreaEffects, Script). Script payloads should reference a feature id registered in `Framework.Spells.Features.FeatureRegistry`.

## Debugging & Telemetry

- Enable verbose logging by setting `MeleeDebugConfig.EnableVerbose = true`. Logs use fixed strings to avoid GC allocs even when verbose.
- Telemetry events (`MeleeSwingBegan`, `MeleeHit`, `MeleeDodged`, etc.) are written to `MeleeTelemetryEventBuffer` each frame and consumed by the analytics pipeline or editor tooling.

## Checklist
- [ ] Weapon slot buffer present with valid blobs.  
- [ ] Defense tuning configured and parry windows wired through TimedEffect.  
- [ ] Proc tables reference valid payload factories.  
- [ ] Resource pools provide enough stamina/guard budget for the intended encounter.  
- [ ] Tests in `Framework.Melee.Tests` cover new weapons or defenses before enabling them in combat content.
