## Damage Modifiers

Applies per-school multipliers to damage packets before mitigation. Supports global and damage-type specific adjustments:

- `1.0` leaves damage unchanged.
- `0.0` grants immunity.
- Negative values convert incoming damage into healing.

Registered via `DamageModifierSubsystemManifest` and scheduled before the core damage resolution system.
