Defines the runtime contract consumed by the spell pipeline when you enqueue work via `SpellFactory` or `SpellRequestFactory`.

When to use / warnings
- Read this before adding new fields; the struct (`Framework/Spells/Requests/SpellCastRequest.cs`) is Burst-blittable and mirrored in authoring/import pipelines.
- Use the helpers in `SpellFactory`/`SpellCastRequestExtensions` instead of mutating the struct directly; they centralize invariants (clamping, enums, ref requirements).
- Every `.WithX` extension returns a copy; ALWAYS reassign (`request = request.WithSharedCooldown(...)`).
- Keep structs tiny: prefer blob references (`SpellDefinitionId`, `CompositeId`) over raw authoring data to avoid copying large payloads per request.

Example field walkthrough
```csharp
public struct SpellCastRequest : IBufferElementData
{
    public Entity Caster;                // Required. Entity issuing the spell.
    public Entity Target;                // Optional. When null, systems use area data.
    public FixedString64Bytes SpellDefinitionId;
    public FixedString64Bytes CompositeId;
    public SpellCastAreaMode AreaMode;   // None, SingleTarget, GroundPosition, Cone, Chain, Radius.
    public float AreaRadius;
    public float3 AreaPosition;          // Used when AreaMode == GroundPosition.
    public byte AreaAffectsAllies;       // Stored as byte for Burst.
    public DamageSchool School;
    public float BaseMagnitude;
    public float Variance;
    public float Period;                 // Seconds between periodic ticks.
    public float Duration;               // Lifetime of periodic/buff content.
    public bool UsesProjectile;
    public ProjectileDefinition ProjectileDef;
    public bool AppliesBuff;
    public BuffModifier BuffModifier;
    public float BuffDuration;
    public bool RefreshBuffDuration;
    public TemporalSpellPayload Temporal; // Duration, Interval, ReleaseMode, SampleCount.
    public SummonPayload Summon;         // TemplateId, Count, OwnerGroup, ReplaceOldest, Temporary.
    public FixedString32Bytes SharedCooldownId;
    public float SharedCooldownSeconds;
    public FixedList128Bytes<ResourceCost> AdditionalResourceCosts;
    public FixedList128Bytes<ScalingRule> ScalingRules;
    public bool ConsumeGlobalCooldown;
    public SpellCastFlags Flags;          // Bits for allowing partial refunds, bypassing silence, etc.
}
```

Field semantics
- **Caster/Target** – required for validation (range, LOS, faction). If you are casting ground-only spells, still supply a target for auditing (usually the caster) and set `AreaMode` to ground.
- **`SpellDefinitionId`** – points at the canonical definition. Anything generated on the fly (e.g., SpellFactory) should still set this so cooldowns, telemetry, and rank systems work.
- **`CompositeId`** – optional pointer to a `CompositeSpellDefinition`. Set it when this request represents a composite container (see `CompositeSpells.md`).
- **Area fields** – `AreaMode`, `AreaRadius`, `AreaPosition`, `AreaFalloff`, `AreaAffectsAllies`, `MaxTargets` describe how to find recipients. `AreaMode.SingleTarget` is default.
- **Damage/Healing** – `School`, `BaseMagnitude`, `Variance` describe the scalar delivered to downstream policies (Damage, Heal, DOT, HOT). `Variance` is applied as +/- percent.
- **Periodic controls** – `Period` (seconds between ticks) and `Duration` (total lifetime). The pipeline automatically populates `Elapsed` counters via `EffectInstance` buffers.
- **Projectile** – `UsesProjectile = true` switches the request into projectile orchestration. Populate `ProjectileDef` with speed, gravity, FX ids, payload, and optional impact `SpellDefinitionId` for AoE-on-impact.
- **Buff/Debuff** – Set `AppliesBuff = true`, populate `BuffModifier` (category, magnitude, stacks), `BuffDuration` (seconds), and `RefreshBuffDuration`. Debuffs follow the same fields but use debuff-specific categories.
- **Temporal** – `TemporalSpellPayload` holds `Duration`, `SampleInterval`, `ReleaseMode` (Immediate, BurstAtEnd, StreamAsYouGo), and optional `EnvelopeCurveId` for weighting.
- **Summoning** – `SummonPayload` includes `TemplateId`, `Count`, `OwnerGroupId`, `SummonTemporary`, `SummonReplaceOldest`, `SummonDuration`. Setting it automatically routes to the summon systems in `Framework.Spells.Runtime`.
- **Costing** – `SharedCooldownId/Seconds` drive `SpellCooldownValidator`. `AdditionalResourceCosts` is a `FixedList` so you can charge mana + reagents without allocations. Pass `ref EntityManager` into resource drivers because they mutate ECS state.
- **Scaling rules** – `ScalingRules` entries identify the stat (`ScalingStat.Intelligence`) and coefficient. Use `request = request.WithScalingRule(stat, coeff)` so the helper deduplicates.
- **Flags** – bitmask for niche behaviors (ignore silence, allow partial refund, bypass LOS). Keep the struct lean by packing booleans into `SpellCastFlags`.

Notes
- Source of truth: `Framework/Spells/Requests/SpellCastRequest.cs` (core fields) plus the generated partial produced during Unity sync (`SpellCastRequest.Generated.cs`).
- The struct stays blittable so it can sit inside a `DynamicBuffer` without GC pressure. Avoid adding managed references or non-default constructors.
- Pass `ref EntityManager` when calling helpers that mutate world state (`BuffFactory.EnsureBuffer`, `CooldownFactory.ApplyCooldown`). This avoids copies and reflects the policy that all content mutations happen via refs.
- Validators (`SpellCooldownValidator`, `SpellResourceValidator`, `SpellSpatialValidator`) read these fields before the pipeline even creates a `SpellCastContext`. Missing data causes the request to fizzle without events.

Links
- [Quickstart](SpellAPI-QuickStart.md)
- [Spell Factory Reference](SpellFactory-Reference.md)
- [Projectiles](Projectiles.md)
- [Composite Spells](CompositeSpells.md)
- [Temporal Spells](TemporalSpells.md)
