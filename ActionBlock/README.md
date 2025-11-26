## Action blocking subsystem

The action-block module manages coarse interaction locks by storing bit masks on entities. Each bit maps to an `ActionKind` (Attack, Move, Cast, Interact, UseItem, Custom0-3). Requests are buffered via `ActionBlockRequest` and consumed by `ActionBlockRuntimeSystem` which sets/clears bits through `ActionBlockDriver`.

### Configuration

Attach `ActionBlockConfig` (or rely on `ActionBlockConfig.Default`) to tune policy behaviour:

- `BlocksRespectDead` – deny actions when `Dead` is present (set to `false` to allow actions while dead).
- `BlocksRespectCrowdControl` – map debuff flags to blocks (requires `FRAMEWORK_HAS_DEBUFFS`).
- `BlocksRespectCustomRules` – honour explicit mask bits written via `ActionBlockDriver` or queued requests.

The runtime system publishes the latest config through `ActionBlockConfigAccess`. Without a singleton the defaults are used.

### Usage snippets

```csharp
// Block casting for an entity
ActionBlockDriver.Block(ref em, caster, ActionKind.Cast);

// Later release
ActionBlockDriver.Unblock(ref em, caster, ActionKind.Cast);

// Policy guard before casting
if (!Framework.ActionBlock.Policies.ActionBlockPolicy.Can(em, caster, ActionKind.Cast))
    return SpellPolicy.Result.Reject_Spatial_Invalid;
```

```csharp
// Movement guard before applying velocity
if (!Framework.ActionBlock.Policies.ActionBlockPolicy.Can(em, mover, ActionKind.Move))
    return;

// Example melee attack guard
bool CanPerformAttack(Entity attacker)
{
    return Framework.ActionBlock.Policies.ActionBlockPolicy.Can(em, attacker, ActionKind.Attack);
}
```

Define `FRAMEWORK_HAS_DEBUFFS` and/or `FRAMEWORK_HAS_LIFECYCLE` in `Framework.ActionBlock.asmdef` to enable optional integrations. Removing the entire ActionBlock folder keeps the project compiling because other modules only call into it through optional partial registrations.
