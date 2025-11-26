# Gizmo Test Harness

Unity-only helpers for visualizing DOTS entities in editor test scenes.

## Usage

1. Create a scene (e.g., `Assets/Scenes/TestHarness.unity`) in your Unity project.
2. Add an empty GameObject and attach one of the gizmo tester MonoBehaviours (start with `PetsSubsystemGizmoTest`).
3. Enter Play Mode or enable *Execute in Edit Mode*. The script will locate the active `World`, query the relevant components, and draw color-coded spheres. HP is mapped from red (0%) to green (100%).
4. Toggle *Label Entity Ids* in the inspector if you want to see the entity handles above each gizmo.

## Extending for Other Subsystems

Derive from `SubsystemGizmoTestBase`, override `QueryComponents` with the component filter for the subsystem you want to inspect, and implement `TryResolveColor` to supply any diagnostics you want (cooldowns, stats, etc.). Because the harness runs only inside `#if UNITY_EDITOR`, it won't impact builds or headless tests.
