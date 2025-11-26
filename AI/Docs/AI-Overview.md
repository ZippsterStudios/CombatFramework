# AI Behavior Overview

```
Perception Systems → Contracts (targets/threat/leash) → Framework.AI → Contracts.Intents → Movement & Spells
```

All cross-module traffic now flows through `Framework.Contracts`: perception populates `PerceptionSensedTarget`, `PerceptionTargetCandidate`, `ThreatEntry`, and `LeashConfig`, while AI emits `MoveIntent`, `CastIntent`, and `StateChangeRequest`. No assembly points directly at Perception, Movement, or Spells.

## Systems Pipeline

| System | Description |
| --- | --- |
| `AIDecisionSystem` | Reads contracts + health, selects the best target through `ITargetPickPolicy`, evaluates Idle/Combat/Flee via rule-based or utility scoring (`IStateUtilityPolicy`), and enqueues `StateChangeRequest` when a transition is needed. |
| `AIRuntimeSystem` | Drains `StateChangeRequest` buffers, validating each state id with `AIPolicy`. |
| `AIBehaviorRecipeSystem` | Optional declarative layer. When `AIBehaviorRecipeRef` is present it evaluates the compiled blob and writes intents directly from the recipe actions. |
| `AIStateMachineSystem` | Default FSM. Builds an `AIBehaviorContext`, runs the Burst-safe drivers, shapes locomotion via `IMovementPolicy`, clamps everything to the leash contract, and writes `MoveIntent`/`CastIntent`. |

All systems live in `RequestsSystemGroup` to keep ordering consistent: Perception → Decisions → Recipe → State Machine → downstream consumers.

## Core Components

| Component / Buffer | Purpose |
| --- | --- |
| `AIBehaviorEnabledTag` | Opt-in flag for the decision/state machine loop. |
| `AIAgentBehaviorConfig` | Author-time knobs (decision cadence, spell id/cooldown, ranges, flee thresholds, move speeds, utility toggle). |
| `AIAgentDecisionState` | Stores the next evaluation timestamp to enforce throttled decisions. |
| `Framework.Contracts.AI.AIAgentTarget` | Chosen target entity plus last known distance + visibility bits. |
| `AIState` | Current integer state id (`AIStateIds.Idle/Combat/Flee`). |
| `AICombatRuntime` | Runtime primary ability cooldown used by both FSM and recipe systems. |
| `MoveIntent` (`Framework.Contracts.Intents`) | Latest locomotion request (mode, destination, desired speed, active flag). |
| `CastIntent` (`Framework.Contracts.Intents`) | Spell request (spell id, target, active flag). |
| `DynamicBuffer<StateChangeRequest>` | Pending state transitions emitted by decisions or fallback handlers. |
| `AIBehaviorRecipeRef` | Optional blob reference that drives the declarative recipe system. |

Movement and spell modules simply read/clear the intent components�€”no more tight coupling to AI internals.

> Set `UseUtilityScoring` on `AIAgentBehaviorConfig` to enable weighted selection via `DefaultStateUtilityPolicy`; otherwise the lightweight Idle/Combat/Flee rules stay active.

## Policies & Recipes

- **Target selection**: `ITargetPickPolicy` (default `NearestVisibleTargetPolicy`) chooses between the candidate buffer rows filled by perception.
- **State utility**: `IStateUtilityPolicy` (default `DefaultStateUtilityPolicy`) scores Idle/Combat/Flee using visibility, range, and health.
- **Movement shaping**: `IMovementPolicy` (default `DefaultMovementPolicy`) keeps the agent in a ring around the target, supports strafe/flee requests, and feeds `MoveIntent`.

For author-friendly behavior, attach `AIBehaviorRecipeTextAuthoring` and reference a `.behavior` TextAsset (samples live in `Framework/AI/Behaviors/Samples`). The baker parses the DSL, resolves config keys at bake time, produces a Burst-friendly blob, and adds `AIBehaviorRecipeRef`. A fluent `AIBehaviorBuilder` exists for tests/editor tooling.

## Authoring Checklist

1. Add `AIBehaviorAuthoring` to a prefab or GameObject. It bakes `AIAgentBehaviorConfig`, `AIState`, `AIAgentDecisionState`, `AICombatRuntime`, `AIBehaviorEnabledTag`, `AIAgentTarget`, empty `MoveIntent`/`CastIntent`, and the `StateChangeRequest` buffer.
2. (Optional) Add `AIStateAuthoring` to override the starting state id.
3. Provide perception data by writing the contract components/buffers from your own module.
4. For declarative behaviors, attach `AIBehaviorRecipeTextAuthoring` with a `.behavior` file or construct a blob via `AIBehaviorBuilder`.
5. Movement/pathing systems consume `MoveIntent`, while the spell runtime consumes `CastIntent`. Both should clear the component after acknowledging the request.

## Recipes DSL Cheatsheet

```
when has_target & visible & in_range(AttackRange): stop; cast(primary)
when has_target & not in_range(AttackRange): move(chase, MoveSpeed)
when health_below(FleeHealthThresholdPercent): move(flee, FleeMoveSpeed, FleeRetreatDistance)
default: stop
```

Supported condition tokens: `has_target`, `visible`, `in_range(value|Key)`, `not in_range(...)`, `health_below(value|Key)`. Actions: `stop`, `move(chase|flee, speedKeyOrValue, retreatKeyOrValue)`, `cast(primary)`, `cast(id=SpellId)`. Numerical parameters accept literals or config property names resolved at bake time.

This architecture keeps AI Burst-safe, GC-free, and fully decoupled from downstream modules while still allowing fast iteration through behavior recipes or handcrafted policies.
