## Damage Bypass Flags

Damage packets produced by the spells pipeline (and any other gameplay feature) can opt-out of specific mitigation layers:

| Flag | Effect |
| --- | --- |
| `IgnoreArmor` | Sets the effective armor curve input to `0`, so the spell skips diminishing returns armor mitigation. |
| `IgnoreResist` | Sets the effective resist percent to `0`, so the spell ignores elemental/percent-based resist reduction. |
| `IgnoreSnapshotModifiers` | Skips defensive multipliers captured in `BuffStatSnapshot` and prevents damage reflection from the snapshot. |

### Flow Overview

1. **Spells/Features** set the bypass flags on `DamagePacket` before enqueuing `DamageRequest` (see `Framework.Spells.Runtime.EffectBlockRouter`).  
2. **DamageModifierSystem** can still change the base amount (e.g., school multipliers) because these operate before mitigation.  
3. **DamageRuntimeSystem** and **DamageResolutionSystem** both call `DamageResolverUtility.Resolve`, which zeros out armor/resist when the matching flag is set and logs the decision through `DamageDebug`.  
4. The systems write the final HP delta and, when allowed, apply reflection via the snapshot.

### Debugging Tips

- Enable logging with `Framework.Damage.Runtime.DamageDebugBridge.EnableDebugLogs(true)` (Editor/Development build only).  
- The log line shows the raw amount, final amount, original armor/resist, effective armor/resist after bypass, and flag states.  
- The combat test window exposes a toggle for these logs via *Window ▸ Framework ▸ Combat Test Panel*.

### Tests

`Framework.Damage.Tests/DamageBypassTests.cs` covers the armor- and resist-bypass combinations and guards against regressions.  
`Framework.Spells.Tests/SpellDamageIntegrationTests.cs` contains smoke tests that cast sample spells to verify pipeline-to-damage integration.
