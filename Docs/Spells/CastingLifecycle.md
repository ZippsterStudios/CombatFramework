End-to-end view of what happens after a `SpellCastRequest` is enqueued.

When to use / warnings
- Share this with anyone debugging “my spell never fired” issues—it enumerates every gate between enqueue and resolution.
- System names come from `Framework/Spells/Pipeline/Systems`; if you add new steps, register them in the same group order.
- Never reference systems across asmdefs for ordering. Use the shared Core groups or composite metadata.
- Keep the lifecycle Burst-friendly: component data + dynamic buffers only. Managed callbacks belong in authoring/editor assemblies.

Lifecycle stages
1. **Queue (SpellRequestSystem)** – Typically satisfied by `SpellPipelineFactory.Cast` which pushes requests into `DynamicBuffer<SpellCastRequest>` on the caster (file: `Framework/Spells/Factory/SpellRequestFactory.cs`).
2. **Plan Build (`CastPlanBuilderSystem`)** – Reads the buffer, validates cooldown/resource/range/LOS via `SpellPolicy`, creates a `SpellCastContext` entity, seeds the default plan (`Validate → Afford → Spend → Windup → Interrupt → Fizzle → Apply → Cleanup`), emits `SpellBeganEvent`.
3. **Validate (`ValidateSpellStageSystem`)** – Double-checks any runtime conditions (e.g., silence, target immunity) that may have changed after queue time. Fails fast and emits a fizzle event if necessary.
4. **Afford/Spend (`AffordSpellStageSystem`, `SpendSpellStageSystem`)** – Calls into Resources/Buffs to guarantee the caster can pay costs. `Afford` only checks; `Spend` consumes via `ResourceFactory` and `CooldownFactory`. Interrupt/fizzle refunds consult the flags set on the request.
5. **Windup (`WindupSpellStageSystem`)** – Handles cast time or channel loops. `Temporal` spells extend this stage to incorporate sampling ticks.
6. **Interrupt/Fizzle** – `InterruptSpellStageSystem` reacts to stun/knock events (charges interrupt cost). `FizzleSpellStageSystem` handles friendly failures (LOS break, target invalid, fizzles) and emits `SpellFizzledEvent`.
7. **Apply (`ApplySpellStageSystem`)** – Hands the `SpellDefinitionBlob` to downstream drivers (Damage, Heal, Buff, DOT/HOT, Summon). Each driver writes to its module-specific buffers (`DamageEvents`, `EffectInstances`, etc.). Projectiles/composites are scheduled here.
8. **Cleanup (`CleanupSpellStageSystem`)** – Clears plan buffers, destroys the `SpellCastContext`, emits `SpellResolvedEvent`, and removes plan modifiers from the caster.
9. **Composite follow-up (`CompositeSpellSystem`)** – Optional extra loop when the original request references a composite id. Schedules and enqueues child spells according to `CompositeMode`.

Validation checkpoints
- **Cooldown/resource** – `SpellPolicy.ValidateCast` (inside `CastPlanBuilderSystem`) checks shared cooldowns + resources before creating the cast context.
- **Spatial** – range, LOS, and ground-placement (for AoE) happen both during validation and before applying to catch movement.
- **Silence/stun** – `SpellDefinitionFlags.IgnoreSilence` and `SpellDefinitionFlags.IgnoreInterrupts` short-circuit validators.
- **Fizzle** – anything that fails after costs are charged funnels into `SpellFizzleRequest`, ensuring partial refunds trigger if allowed.

Notes
- System ordering uses the shared Core groups from `Framework.Core.Base.Groups.cs`. Keep new systems within the same group to avoid frame-order shocks.
- `SpellPipelineSystemGroup` is declared `partial` to satisfy Entities source generators (see `Framework/Spells/Pipeline/Systems/SpellPipelineSystemGroup.cs`). Follow that pattern when adding new ComponentSystemGroups.
- Telemetry lives in `SpellCastEventPayload` buffers; flush them after assertion-based tests to avoid leaking state.

Links
- [SpellCastRequest Reference](SpellCastRequest-Reference.md)
- [Examples](Examples.md)
- [Testing Guide](Testing.md)
