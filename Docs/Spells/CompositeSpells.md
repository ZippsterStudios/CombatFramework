How to author, register, and execute multi-step composite spells.

When to use / warnings
- Use composites when a single player action should orchestrate several child spells (projectile → AoE → DoT, or multi-phase rituals).
- Keep composites data-driven: prefer `CompositeSpellDefinition` JSON + blobs so designers can tweak order/delays without code.
- Avoid cross-asmdef references for ordering; rely on composite metadata or Core system groups for scheduling.
- Never make Core depend on your composite aggregator—load the optional CombatSystem assembly or use reflection when bootstrapping (`SubsystemBootstrap`).

Example: projectile that spawns an AoE then applies a lingering DoT
```json
{
  "CompositeId": "fireball-impact-cluster",
  "Mode": "Sequential",
  "DelayBetween": 0.15,
  "Entries": [
    {
      "SpellDefinitionId": "fireball-impact-aoe",
      "Order": 0,
      "SharedTags": ["impact"]
    },
    {
      "SpellDefinitionId": "flame-dot",
      "Order": 1,
      "Delay": 0.05,
      "SharedTags": ["impact", "burn"]
    }
  ]
}
```
```csharp
using Framework.Spells.Composite;
using Framework.Spells.Factory;
using Unity.Entities;

public static class CompositeExample
{
    public static void QueueFireballCluster(EntityManager em, Entity caster, Entity target)
    {
        if (!CompositeSpellCatalog.TryGet("fireball-impact-cluster", out var def))
            throw new InvalidOperationException("Composite not registered");

        CompositeSpellFactory.Queue(ref em, caster, target, def);
    }
}
```
Modes and behaviors
- `Sequential` – runs entries strictly in the listed order. Optional `DelayBetween` applies between entries unless an entry overrides `Delay`.
- `Parallel` – fires every entry at once. Use `CompositeDelayBetween` only when you need deterministic staggering.
- `Chain` – uses the previous entry’s target(s) as the next entry’s source (e.g., jumps). Combine with `MaxTargets` + `AreaMode` for arc lightning.
- `OrderMode = Explicit` vs `OrderMode = TagsFirst` – explicit uses `Order` integers, tags-first groups by tag before order. Pick one to avoid designer confusion.

Runtime contract
- Registration happens through `SpellDefinitionCatalog.RegisterComposite(def)` or `SpellDefinitionCatalog.LoadCompositeJson(path)`. Keep JSON in `Framework/Spells/Content/Composites`.
- Execution flows through `CompositeSpellFactory.Queue(ref EntityManager, caster, target, def)`. The factory creates an internal `CompositeCastContext` entity so state is tracked between entries.
- `CompositeSpellSystem` (Runtime) watches the contexts, dequeues entries according to `Mode`, and emits child `SpellCastRequest` instances targeting the same or derived entities.
- Child requests inherit `SharedCooldownId`, `SpellDefinitionId`, and `Temporal` metadata unless an entry overrides them. Always set those fields at the composite level to keep telemetry consistent.

Notes
- Keep composite definitions lean—store authoring-only notes in a parallel content file, not inside the JSON consumed by Burst systems.
- Use group ids (e.g., `ResolutionSystemGroup`) instead of referencing other systems for ordering. This avoids cross-asmdef attributes like `[UpdateAfter(typeof(FireballDetonationSystem))]`.
- Composites can wrap other composites, but never create cycles; the catalog rejects recursive references during registration.

Links
- [Examples](Examples.md)
- [Projectiles](Projectiles.md)
- [Casting Lifecycle](CastingLifecycle.md)
- [Authoring Guidelines](AuthoringGuidelines.md)
