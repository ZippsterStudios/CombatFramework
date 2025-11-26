Reference for the high-level helpers that live in `Framework/Spells/Factory/SpellFactory.cs` and surface common spell archetypes.

When to use / warnings
- Prefer these helpers whenever code needs to enqueue or author runtime-only spells; they normalize every optional field on `SpellCastRequest`.
- Use data-driven `SpellDefinition` blobs for production content; the factory is best for prototyping, scripted bosses, or tests.
- Every helper returns a new `SpellCastRequest` struct; reassign the result so fluent modifiers (cooldowns, costs, scaling) stick.
- Helpers set sane defaults but never bypass validators; range, LOS, cooldown, resource, silence, and spatial checks still happen in `CastPlanBuilderSystem`.

Examples (pick the helper that matches your archetype)
```csharp
using Framework.Spells.Factory;
using Unity.Entities;

// 1. Direct Nuke
var direct = SpellFactory.CreateDirectDamage(caster, target, DamageSchool.Fire, 90f)
    .WithSharedCooldown("fire-nuke", 10f)
    .WithVariance(0.15f)
    .WithThreatScale(1.25f);

// 2. Damage-over-time ground puddle
var dot = SpellFactory.CreateGroundAoEDoT(caster, target.Position, DamageSchool.Poison,
                                          tickMagnitude: 18f, radius: 6f, period: 1.5f, duration: 12f)
    .WithAreaFalloff(AreaFalloffMode.Linear)
    .WithProjectilePayload("acid-puddle");

// 3. Projectile combo
var projectile = SpellFactory.CreateRangedAttack(caster, target, DamageSchool.Fire,
                                                 baseHitDamage: 40f, travelSpeed: 30f)
    .WithProjectileSpawn(new ProjectileDefinition
    {
        Id = "firebolt",
        Speed = 30f,
        DestroyOnImpact = true,
        BaseMagnitude = 40f,
        ImpactSpellDefinitionId = "firebolt-impact-aoe"
    });

// 4. Temporal stasis burst
var temporal = SpellFactory.CreateTemporalDirectDamage(caster, target, DamageSchool.Arcane,
                                                       sampleMagnitude: 12f, duration: 6f, sampleInterval: 0.5f)
    .WithTemporalReleaseMode(TemporalReleaseMode.BurstAtEnd);

// 5. Summon a pet from prefab/template
var pet = SpellFactory.CreateSummonPet(caster, templateId: "fire-imp", duration: 60f)
    .WithSummonOwnerGroup(SpellOwnerGroupId.Companions)
    .WithSummonReplaceOldest(true);
```

Helper reference
- `CreateDirectDamage(Entity caster, Entity target, DamageSchool school, float baseMagnitude, float varianceRoll = 0.05f, float manaCost = 0f)` – fastest path to a nuke. Sets `AreaMode = AreaMode.SingleTarget` and `Duration = 0`.
- `CreateDoT(Entity caster, Entity target, DamageSchool school, float tickDamage, float period, float duration, bool startWithInstantTick = false)` – schedules effect ticks through the periodic systems. Sets `EffectMode = SpellEffectMode.OverTime` and fills `Period/Duration`.
- `CreateHoT(...)` – mirrors `CreateDoT` but flags the request as healing and routes to `Framework.HOT` policies. Use for pulsing heals, not shields.
- `CreateGroundAoEDoT(Entity caster, float3 center, DamageSchool school, float tickMagnitude, float radius, float period, float duration)` – writes `AreaMode = AreaMode.GroundPosition`, `AreaRadius`, `AreaFilter`, and toggles `ApplyToAll`. Pair with `AreaEffectDefinition` JSON for designer-authored puddles.
- `CreateBuffSpell(Entity caster, Entity target, FixedString64Bytes buffId, float durationOverride = -1f)` – wraps `BuffFactory.ApplyBuff` so systems receive the correct `BuffModifier`, `BuffDuration`, and stacking metadata. Works for both ally buffs and self pulses.
- `CreateDebuffSpell(...)` – same call path but flags hostiles, drives `DebuffFactory`, and honors `SpellDefinitionFlags.IgnoreSilence` for essential debuffs.
- `CreateRangedAttack(Entity caster, Entity target, DamageSchool school, float baseHitDamage, float travelSpeed = 20f)` – toggles `UsesProjectile`, seeds a `ProjectileDefinition`, and copies `SpellDefinitionId` into the projectile payload so composite/impact spreads run automatically.
- `CreateTemporalDirectDamage(...)` and `CreateTemporalHoT(...)` – build `TemporalSpellDefinition` payloads, set `TemporalDuration`, `TemporalSampleInterval`, and `TemporalReleaseMode`. See `TemporalSpells.md`.
- `CreateSummonPet(Entity caster, FixedString64Bytes templateId, float duration)` – points to `SummonPrefabId` and uses `SummonOwnerGroupId.Companions` by default. Works for named pets with persistence.
- `CreateSummonTemplate(Entity caster, FixedString64Bytes templateId, int count, bool temporary)` – emits a `SummonTemplate` payload for swarm-style summons; `SummonReplaceOldest` kicks in whenever the owner already controls the requested cap.

Notes
- Signatures live in `Framework/Spells/Factory/SpellFactory.cs`. The helpers call into `SpellRequestFactory.EnqueueCast` or `CompositeSpellFactory.Queue`, both in the same folder.
- Every helper only sets fields required for the archetype; chain `.With*` modifiers (projectiles, cooldowns, resources, ranks, scaling) afterward.
- Factories are Burst-friendly because they only populate blittable structs; avoid managed allocations in your extension methods so they stay Burst-safe.
- `SpellPipelineFactory.Cast` is the official queue entry point. Call it after building/modifying the request if you do not want to manage buffers manually.

Links
- [Quickstart](SpellAPI-QuickStart.md)
- [SpellCastRequest Reference](SpellCastRequest-Reference.md)
- [Projectiles](Projectiles.md)
- [Temporal Spells](TemporalSpells.md)
- [Composite Spells](CompositeSpells.md)
