Explains how temporal spell samples accumulate, release, and integrate with the spell pipeline.

When to use / warnings
- Use temporal spells for channeled bursts, stasis-style effects, or “record then unleash” mechanics where sampling matters.
- Temporal payloads live in `Framework/Spells/Runtime/TemporalSpellDefinition.cs`; keep them blittable so Burst jobs can sample.
- Respect the sampling budget—short intervals over long durations explode buffer sizes. Clamp `Duration / SampleInterval` to something reasonable (<= 64) for mobile targets.
- Release modes change gameplay drastically; ensure designers understand the difference before mixing them into rotations.

Example: channel a 6-second arcane beam, release all damage at the end
```csharp
using Framework.Spells.Factory;
using Framework.Spells.Requests;

public static class TemporalSpellExamples
{
    public static SpellCastRequest CreateArcaneChannel(Entity caster, Entity target)
    {
        var request = SpellFactory.CreateTemporalDirectDamage(caster, target, DamageSchool.Arcane,
            sampleMagnitude: 14f, sampleInterval: 0.5f, duration: 6f);

        return request.WithTemporalReleaseMode(TemporalReleaseMode.BurstAtEnd)
                      .WithTemporalEnvelope("arcane-channel")
                      .WithTemporalSampleMode(TemporalSampleMode.WeightedAverage)
                      .WithSharedCooldown("arcane-channel", 20f);
    }
}
```
`TemporalSpellDefinition` fields
- `Duration` – total sampling window (seconds).
- `SampleInterval` – time between samples. The runtime computes `SampleCount = ceil(Duration / SampleInterval)` and allocates buffers accordingly.
- `ReleaseMode` – `StreamImmediate`, `BurstAtEnd`, `Split` (half now, half later).
- `SampleMode` – `FlatMean`, `WeightedAverage`, `Envelope` (driven by a blob curve id).
- `EnvelopeId` – optional `FixedString64Bytes` referencing a blobbed curve for weighting samples before release.

Runtime flow
1. `SpellFactory.CreateTemporal*` populates `SpellCastRequest.Temporal`.
2. `CastPlanBuilderSystem` spots the temporal flag and appends channel-specific steps (windup + periodic sampling) inside the plan buffer.
3. `TemporalSampleSystem` (Runtime) writes each sample into a fixed list per cast context.
4. `TemporalReleaseSystem` either emits ticks as they arrive (`StreamImmediate`) or schedules a burst event after the channel completes.
5. Downstream damage/heal policies treat temporal releases like regular `SpellResolvedEvent` payloads, so telemetry and logs stay consistent.

Notes
- Temporal spells respect the same validators as normal spells—if a channel is interrupted, only the samples flagged as “eligible for partial refund” return mana.
- Sample storage uses `FixedList128Bytes<float>` for Burst-friendliness; keep durations short if you need high-frequency sampling.
- Designer tip: Keep `SampleInterval` aligned with server tick (default 0.5s or 1s) unless you truly need extra fidelity.

Links
- [Spell Factory Reference](SpellFactory-Reference.md)
- [Examples](Examples.md)
- [Casting Lifecycle](CastingLifecycle.md)
