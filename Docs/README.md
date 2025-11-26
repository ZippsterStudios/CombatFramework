# Combat Framework Documentation

All gameplay documentation now lives inside this folder and follows a single structure. The `Subsystems/` directory contains one Markdown reference per module (Damage, Heal, AI, Spells, etc.) so engineers and designers can find examples without sifting through stale guides.

## Directory layout

| Path | Purpose |
| --- | --- |
| `Subsystems/README.md` | Index of every subsystem doc plus quick navigation tips. |
| `Subsystems/*` | Detailed guides covering responsibilities, key types, and multi-step examples. |
| `Spells/` | Extended spell authoring docs (DSL/JSON schemas, long-form examples). |
| `.obsidian/` | Optional Obsidian workspace settings (safe to ignore if you do not use Obsidian). |

If you need to add new docs, create a new file under `Subsystems/` (or extend an existing one) so we keep everything in one place. When old docs become obsolete, delete them immediately instead of leaving parallel guides around—this keeps onboarding fast and prevents drift between modules.
