Spell API quick entry for building and queueing a spell request in under a minute.

When to use / warnings
- Reach for this pattern when you just need a working spell during iteration (combat prototypes, AI spikes, designer playtests).
- Assumes the classic SpellFactory helpers in `Framework/Spells/Factory/SpellFactory.cs` are synced; otherwise use the equivalent calls in `Framework/Spells/Factory/SpellRequestFactory.cs` manually.
- Always ensure the caster owns a `CastState` component and a `DynamicBuffer<SpellCastRequest>`; missing either results in silent no-ops when the pipeline runs.
- Treat every fluent helper as immutable; reassign the returned struct or you will drop modifiers (global cooldowns, resources, projectiles).

Example
```csharp
using Framework.Spells.Factory;
using Framework.Spells.Requests;
using Unity.Entities;

public static class SpellQuickstart
{
    public static void QueueFirebolt(EntityManager em, Entity caster, Entity target)
    {
        var request = SpellFactory.CreateDirectDamage(caster, target, DamageSchool.Fire, baseMagnitude: 45f);
        request = request.WithSharedCooldown("firebolt", 8f)
                         .WithResourceCost("Mana", 35)
                         .WithScalingRule(ScalingStat.Intelligence, 0.42f);

        if (!em.HasComponent<CastState>(caster))
            em.AddComponentData(caster, CastState.Default);

        var buffer = em.HasBuffer<SpellCastRequest>(caster)
            ? em.GetBuffer<SpellCastRequest>(caster)
            : em.AddBuffer<SpellCastRequest>(caster);

        buffer.Add(request);
    }
}
```
This is the same sequence the `CombatTestWindow` uses when a designer clicks “Queue Spell” inside `Framework/UnityAuthoring/Editor/FrameworkCombatTestWindow.Actions.cs`: build the request, ensure `CastState`, ensure the `SpellCastRequest` buffer, and append.

Notes
- `SpellFactory.CreateDirectDamage` is the fastest way to stand up a nuke. For serialized content, wire the same fields in `SpellDefinition` (
  `Framework/Spells/Content/SpellDefinition.cs`).
- Shared/global cooldown helpers (`WithSharedCooldown`, `WithCooldown`, `WithResourceCost`) all return new structs; chain them together as shown.
- The spell pipeline will pick up the buffer next tick. Validation (cooldowns, LOS, resources) happens in `CastPlanBuilderSystem` in `Framework/Spells/Pipeline/Systems`.
- Designers can copy/paste this code into NUnit tests; the headless harness under `Tests/HeadlessStub/Spells` mirrors the exact calls.

Links
- [Spell Factory Reference](SpellFactory-Reference.md)
- [SpellCastRequest Reference](SpellCastRequest-Reference.md)
- [Runtime Casting Lifecycle](CastingLifecycle.md)
- [Examples](Examples.md)
