Quick answers to the most common Spell API questions.

When to use / warnings
- Glance here before filing bugs—most issues boil down to a missing reassignment or buffer.
- Entries assume you already read the Quickstart; they focus on root causes and fixes.
- If the answer references a doc, follow the link for deeper context.
- Keep answers short and actionable. Longer workflows belong in the other docs.

**Q: Why didn’t my `WithSharedCooldown` call do anything?**
- You likely forgot to reassign the struct. `SpellCastRequest` is a value type—`request.WithSharedCooldown(...)` returns a copy.
- Double-check that the request’s `SpellDefinitionId` matches the id the cooldown system tracks. Shared cooldowns index by string id, not by entity.
- Validators only run after the request hits `CastPlanBuilderSystem`; ensure the caster actually has a `SpellCastRequest` buffer (see Quickstart).

**Q: Why did my projectile do zero damage?**
- `ProjectileDefinition.BaseMagnitude` defaults to 0. Set it explicitly or provide an `ImpactSpellDefinitionId`/`ImpactCompositeId` so the projectile knows what to apply.
- Confirm `request.UsesProjectile = true`; otherwise the pipeline treats it as instant and never spawns FX.
- If you rely on `ImpactSpellDefinitionId`, make sure that spell definition actually contains damage effects.

**Q: My spell fizzles immediately with no log. What gives?**
- Missing `CastState` or buffer. `CastPlanBuilderSystem` returns early when it can’t find state, so no event is emitted.
- Cooldown/resource validators reject the request. Enable verbose logging in `CastGlobalConfig` or run inside the CombatTestWindow to see HUD output.

**Q: `WithSummonReplaceOldest(true)` doesn’t remove my oldest pet.**
- Ensure the request also sets `SummonOwnerGroupId` so the summon runtime knows which collection to prune.
- Combat log needs the owning entity to have `SummonLedger` component. Call `SummonFactory.EnsureLedger(ref em, caster)` during bootstrap.

**Q: Composite spells run out of order.**
- Check the `CompositeSpellDefinition.Mode` and `Order` values. `Sequential` honors numeric order; `Parallel` ignores it.
- Verify that each entry has a unique `SpellDefinitionId`. Duplicate ids can appear to “skip” because cooldowns block the second cast.

**Q: Temporal spell keeps releasing nothing.**
- You probably set `TemporalSampleInterval` higher than `TemporalDuration`, yielding zero samples. Fix either value.
- Burst mode releases all samples only when the channel resolves. Interrupts before completion drop the bank unless `AllowPartialRefund` is set.

Notes
- File real bugs (not doc updates) when validators misbehave. Attach the `SpellCastRequest` dump using `SpellDebugUtility.DumpRequest` so others can reproduce.
- Update this FAQ whenever you answer the same Slack/Discord question twice.

Links
- [Quickstart](SpellAPI-QuickStart.md)
- [Examples](Examples.md)
- [Projectiles](Projectiles.md)
- [Composite Spells](CompositeSpells.md)
