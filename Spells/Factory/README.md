Spells factory helpers.

- `SpellRequestFactory` – low level helper that enqueues `SpellCastRequest` buffer elements.
- `SpellPipelineFactory` – high level facade for the new pipeline. Provides `Cast`, `AppendStep`, `InsertStep`, `RemoveStep`, and `ClearOverrides` so gameplay code can describe cast plans without touching buffers.
