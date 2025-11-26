How projectile-enabled spells travel, impact, and chain into follow-up effects.

When to use / warnings
- Use projectiles for anything that should respect travel time (visual fireballs, arrows, missiles, chain lightning seeds).
- `ProjectileDefinition` lives next to `SpellFactory` in `Framework/Spells/Factory/ProjectileDefinition.cs`; keep it blittable so it can ride on `SpellCastRequest`.
- Never spawn projectiles manually from gameplay code—always let `ProjectileSpawner.QueueFromActiveCast` handle it so ownership, FX, and Composite hooks stay in sync.
- Remember to set `ProjectileDef.BaseMagnitude` or `ImpactSpellDefinitionId`; otherwise the projectile lands but nothing happens.

Example: configure projectile data + impact composite
```csharp
using Framework.Spells.Factory;
using Framework.Spells.Projectiles;

var request = SpellFactory.CreateRangedAttack(caster, target, DamageSchool.Fire, baseHitDamage: 60f);
request = request.WithProjectileSpawn(new ProjectileDefinition
{
    Id = "fireball",
    Speed = 30f,
    Gravity = 0f,
    Lifetime = 5f,
    DestroyOnImpact = true,
    BaseMagnitude = 60f,
    ImpactSpellDefinitionId = "fireball-impact-aoe",
    ImpactCompositeId = "fireball-impact-cluster",
    Payload = new ProjectilePayload
    {
        ChainCount = 0,
        BurstRadius = 5f,
        BurstSchool = DamageSchool.Fire
    }
});
```
Runtime pipeline
1. `SpellFactory` sets `SpellCastRequest.UsesProjectile = true` and copies the `ProjectileDefinition` into the request.
2. `CastPlanBuilderSystem` detects the flag and tags the cast context so `ProjectileSpawner.QueueFromActiveCast` (Runtime/Projectiles) runs after the spell resolves.
3. `ProjectileSpawner` spawns an entity, attaches motion/FX components, and carries the serialized payload.
4. On impact, the spawner either:
   - Re-applies the base request (direct damage), or
   - If `ImpactSpellDefinitionId` is set, enqueues a brand-new `SpellCastRequest` using that id (preferred when layering AoE/DoT combos), or
   - If `ImpactCompositeId` is set, hands control to `CompositeSpellFactory` for larger chains.
5. Optional: call `ProjectileDefinition.OnTravelCompositeId` to spawn trails or split projectiles at mid-flight markers.

Tuning tips
- `Speed` is meters per second; pair with `Lifetime` to cap range. Example: `Speed=25`, `Lifetime=4` → 100m cap.
- `Gravity` lets you arc projectiles; positive values pull downward (units/sec²).
- `BurstRadius` + `BurstSchool` let you hand off the post-impact AoE to the same policies used by ground spells—no custom logic required.
- For hit-scan weapons, leave `UsesProjectile = false` and rely on `SpellFactory.CreateDirectDamage`; the pipeline resolves immediately.

Links
- [Examples](Examples.md)
- [Composite Spells](CompositeSpells.md)
- [Casting Lifecycle](CastingLifecycle.md)
