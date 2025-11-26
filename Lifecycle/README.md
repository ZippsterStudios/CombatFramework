## Lifecycle subsystem

The lifecycle module emits `Dead` markers and optional `DeathEvent` entries whenever an entity’s `Health.Current` drops to zero. Feature behaviour is controlled by an optional singleton `LifecycleFeatureConfig` (attach to any entity). When no config is present the `LifecycleFeatureConfig.Default` values are used.

### Config toggles

- `EnableDeathEvents` – append `DeathEvent` for each lethal transition.
- `CleanupBuffsOnDeath`, `CleanupDebuffsOnDeath`, `CleanupDotsOnDeath`, `CleanupHotsOnDeath`, `CleanupTimedEffectsOnDeath` – remove the matching runtime components on death. Guarded with `FRAMEWORK_HAS_*` defines so the module still compiles when those assemblies are removed.
- `ClearThreatOnDeath` – drop `ThreatValue`.
- `StopRegenOnDeath` – zero all resource regen fields.
- `AutoDespawnEnabled` / `AutoDespawnSeconds` – despawn after a delay (via `DeathDespawnSystem`). Delay is clamped with `LifecyclePolicy.ClampDespawnSeconds`.
- `BlocksRespectDead`, `BlocksRespectCrowdControl`, `BlocksRespectCustomRules` – exposed for action blocking integration.

### Driver usage

```csharp
LifecycleDriver.Kill(ref world.EntityManager, enemy);
LifecycleDriver.Revive(ref world.EntityManager, ally, newHealth: 50);
```

### Integration tips

- Systems can query `Dead` to quickly bail out of update logic.
- `DeathEvent` buffer is cleared and repopulated every frame; consume it after `DeathDetectionSystem` runs.
- To enable cleanup of optional modules define the symbols (`FRAMEWORK_HAS_TIMED_EFFECTS`, `FRAMEWORK_HAS_DEBUFFS`, etc.) in `Framework.Lifecycle.asmdef`.
- Register the subsystem by keeping `LifecycleSubsystemManifest` in the central `SubsystemManifestRegistry` (already wired). Removing the folder cleanly compiles because all optional dependencies are guarded behind preprocessor symbols.
