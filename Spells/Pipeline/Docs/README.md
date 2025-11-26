# Spell Cast Pipeline

This folder hosts the DOTS/Burst friendly spell cast pipeline. Each incoming SpellCastRequest becomes its own cast entity with a dynamic CastPlanStep buffer that lists ordered stages. CastPipelineRunnerSystem keeps the entity pinned to the correct step while the specialized stage systems (Validate/Afford/Spend/Windup/Interrupt/Fizzle/Apply/Cleanup) do the work in Burst jobs.

## Adding a Stage
1. Create a new CastStepType entry.
2. Author a tiny ISystem in Systems/ that filters casts whose SpellCastContext.CurrentStep matches that new step.
3. Run the logic in a Burst job, mutate SpellCastContext, and call ctx.RequestAdvance() when the stage succeeds.
4. Emit events (see SpellCastEventUtility) or write to SpellCastContext.Flags to affect later stages.
5. Update SpellSubsystemManifest so the world bootstraps the new system and inject the stage into plans through SpellCastPlanModifier buffers or by editing the default list in CastPlanBuilderSystem.

## Plan Building
CastPlanBuilderSystem converts buffered SpellCastRequests into cast entities. The builder:
- Validates spell + cooldown via SpellPolicy.
- Copies the SpellDefinition blob into the cast.
- Seeds the default plan [Validate, Afford, Spend, (Windup), Interrupt, Fizzle, Apply, Cleanup].
- Consumes optional SpellCastPlanModifier buffers on the caster to append/insert/remove steps.
- Applies cooldowns immediately so we match the legacy runtime contract.

## Extensibility Notes
- Use SpellInterruptRequest/SpellFizzleRequest components to signal failures from other modules without tight coupling.
- Global knobs live in CastGlobalConfig (interrupt/fizzle charge and refund policy). Override per spell through fields on SpellDefinition.
- Events are emitted as temporary entities with tag components (SpellBeganEvent, SpellInterruptedEvent, etc.) and a SpellCastEventPayload buffer for observers.

