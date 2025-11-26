# Framework.Pets Overview

Framework.Pets adds contract-driven, DOTS-friendly pet gameplay. Spells feed SummonPet effects into the spell pipeline, which forwards the request through PetSummonBridge into PetFactory. Pets are pure ECS entities tagged with PetTag, PetOwner, AI state, and leash data so they coexist with the rest of the combat framework.

## Flow
1. SpellEffectKind.SummonPet -> FeatureRegistry -> PetSummonBridge.
2. SummonFromSpellHook registers PetFactory.HandleSummonEffect when the subsystem boots.
3. PetFactory enforces limits, spawns ECS agents, attaches AI configs/recipes, sets up Command/Lifetime/Symbiosis data, and emits telemetry events.
4. Owners enqueue PetCommandRequest buffers via PetDriver; PetCommandRouterSystem fans requests out to concrete pets while respecting swarms.
5. Follow/Guard/Patrol/Attack/BackOff/Dismiss systems translate commands into MoveIntent, AIAgentTarget, and leash updates.
6. PetSymbiosisSystem routes damage/heal deltas between owners and pets while PetLifetimeSystem consumes TimedEffect events to despawn expiring pets.

## Key Components
- **Content**: PetDefinition, PetCatalog, PetBuilder, built-in behaviors (Pet_Follow, Pet_Guard, Pet_Sit, Pet_Patrol).
- **Contracts**: PetCommandRequest (owner-scoped buffer) and PetWaypoint for patrol chains.
- **Events**: PetSummon*, PetExpiredEvent, PetHealthLinkedDamageEvent mirror the spell pipeline payloads.
- **Policies**: limit enforcement, leash refresh, command handling, and symbiosis routing keep the module Burst-friendly and data oriented.

## Systems
Requests Group: SummonFromSpellHook, PetCommandRouterSystem, PetFollowSystem, PetGuardSystem, PetPatrolSystem, PetAttackSystem, PetDismissSystem, PetSymbiosisSystem.
Resolution Group: PetLifetimeSystem.

PetSubsystemManifest registers the stack with SubsystemManifestRegistry, so the CombatSystem aggregator picks it up automatically.

## Commanding Pets
- Use `PetDriver.CommandAttackAll(owner, target)` (and the group/pet variants) to order pets onto a specific enemy. The router ensures swarm groups respect the target choice.
- Call `PetDriver.CommandBackOffAll(owner)` to clear attack targets, reset leashes/anchors, and return pets to the owner’s guard position. This maps to the new `PetCommand.BackOff` value if you need to enqueue requests manually.
- UI/editor tooling can use the same helpers; once the `PetCommandRequest` buffer is filled, `PetCommandRouterSystem` handles the rest in the next Requests frame.
