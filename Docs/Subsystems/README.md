# Framework Subsystem Library

The combat framework is intentionally modular: every feature is packaged as a subsystem with its own data layout, drivers, and `ComponentSystemGroup` hooks. Instead of keeping a monolithic reference, this folder contains one document per subsystem so that designers and engineers can zoom straight to the topic they are working on.

| Document | Scope |
| --- | --- |
| `Lifecycle.md` | Shared request → runtime → resolution orchestration, manifests, and partial system requirements. |
| `Temporal.md` | Haste/slow modifiers, anchors, and release hooks. |
| `TimedEffect.md` | General-purpose engine that powers buffs, DOT, HOT, and temporal release. |
| `Resources.md` | Health, mana, stamina components, regen, and request helpers. |
| `Damage.md` + `Damage.BypassFlags.md` | Packets, mitigation, bypass flags, drivers, and telemetry. |
| `Heal.md` | Burst-safe heal helpers, direct applications, and telemetry taps. |
| `Stats.md` | Base/additive/multiplier pattern, requests, and cached reads. |
| `Buffs.md` | Catalogs, stat effects, snapshots, and TimedEffect integration. |
| `Cooldowns.md` | Cooldown buffers, factories, and pruning logic. |
| `DamageOverTime.md` | DOT catalog, runtime, mitigation, and spell payload wiring. |
| `HealOverTime.md` | HOT catalog, runtime, regen stacking, and temporal release links. |
| `AreaEffects.md` | Spawn helpers, lifetime management, and spatial queries. |
| `AI.md` | Agent factory, decision/state systems, and recipe authoring. |
| `Spells.md` | Spell definition pipelines (builder, JSON, runtime edits) and execution hooks. |

**Authoring workflow**

1. Pick the subsystem doc that matches the feature you are extending.  
2. Mirror the examples when adding drivers/factories to keep write-side code centralized.  
3. Update `Framework/Core/Base/SubsystemBootstrap.cs` or your CombatSystem aggregator manifest if you add new systems.  
4. Run `python Framework/sync_framework.py --delete --only-code` when you are ready to sync into Unity.

Extended spell authoring docs (DSL/JSON samples) live in `Framework/Docs/Spells/README.md`.
