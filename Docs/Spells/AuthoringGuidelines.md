Best practices for shipping JSON spell content and naming assets.

When to use / warnings
- Read this before landing new JSON in `Framework/Spells/Content`; the importer (`SpellDefinitionCatalog.LoadJson`) expects specific shapes.
- Keep authoring files free of gameplay-only comments; they are parsed inside Burst jobs after baking.
- Do not create asmdef cycles when adding content helper scripts; content assemblies stay dumb data wrappers.
- Always generate deterministic ids (`SpellIdUtility.Compute`) instead of hand-typing GUIDs—cooldowns, telemetry, and ranks depend on stable ids.

Authoring checklist
- **Location**: put `SpellDefinition` JSON in `Framework/Spells/Content/Definitions`, `CompositeSpellDefinition` JSON in `Framework/Spells/Content/Composites`.
- **Naming**: use snake-case ids (`fireball`, `fireball-impact-aoe`). Reserve camelCase for runtime-only identifiers (buffers, components).
- **Spell vs definition id**: `SpellId` identifies the runtime action (e.g., `wizard.fireball.r1`). `SpellDefinitionId` is the content blob (e.g., `fireball`). Use `SpellIdUtility.Compute(string baseId, int rank)` so both remain aligned.
- **File layout**: keep one JSON definition per file. Mirror the folder structure inside `docs/CombatFramework/Examples` if you publish docs.
- **Composite layering**: composites reference `SpellDefinitionId` strings. Avoid referencing other composites unless you need multi-phase rituals.

Minimal JSON spell definition
```json
{
  "Id": "fireball",
  "School": "Fire",
  "CategoryId": "wizard.nuke",
  "SpellLevel": 12,
  "Rank": 2,
  "ManaCost": 60,
  "CastTime": 2.2,
  "Cooldown": 8,
  "Range": 30,
  "Targeting": "Enemy",
  "Flags": ["AllowPartialRefund"],
  "Effects": [
    { "Kind": "DirectDamage", "Magnitude": 60, "Variance": 0.1 }
  ]
}
```
Composite definition
```json
{
  "CompositeId": "fireball-impact-cluster",
  "Mode": "Sequential",
  "DelayBetween": 0.15,
  "Entries": [
    { "SpellDefinitionId": "fireball", "Order": 0 },
    { "SpellDefinitionId": "flame-dot", "Order": 1, "Delay": 0.05 }
  ]
}
```

Workflow
1. Author JSON (or ScriptableObject) and drop it into the folders above.
2. Run `python Framework/sync_framework.py --delete --only-code` to sync into the Unity project. The converter bakes blobs via `SpellDefinitionCatalog.Register`.
3. Reference ids from gameplay code via constants (e.g., `static readonly FixedString64Bytes FireballId = "fireball";`).
4. Expose rank metadata through `SpellRankUtility` so scaling + SpellBook UI share the same info.

Notes
- Blob baking happens inside `SpellDefinitionBlobUtility` (Framework/Spells/Content). Keep arrays small; each entry copies data into persistent memory.
- Composite definitions support `Mode`, `Order`, `Delay`, `SharedTags`, and `Metadata` dictionaries. Stick to the documented keys so the runtime stays Burst-friendly.
- Tests can load authoring JSON via `SpellDefinitionCatalog.FromJson(json)`—see `Tests/HeadlessStub/Spells/SpellDocExamplesTests.cs` for fixtures.

Links
- [Examples](Examples.md)
- [Composite Spells](CompositeSpells.md)
- [Testing Guide](Testing.md)
