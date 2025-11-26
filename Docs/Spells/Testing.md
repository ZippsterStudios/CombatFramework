How to validate spells in the headless/NUnit harness.

When to use / warnings
- Use this when adding regression tests for new spell archetypes or when documenting copy/paste code for QA.
- Headless tests run in `Tests/HeadlessStub/CombatFramework.Tests.csproj`; keep them Burst-safe (no UnityEngine, no Entities worlds unless stubbed).
- Avoid depending on live Unity packages—tests rely on lightweight stubs in `UnityStubs/*.cs`.
- Always run `dotnet test Tests/HeadlessStub/CombatFramework.Tests.csproj` before submitting doc or content PRs.

Example tests (see `Tests/HeadlessStub/Spells/SpellDocExamplesTests.cs`)
```csharp
using Framework.Spells.Factory;
using Framework.Spells.Requests;
using NUnit.Framework;

namespace CombatFramework.Tests.Spells
{
    public sealed class SpellDocExamplesTests
    {
        [Test]
        public void DirectDamageQuickstartMatchesDocs()
        {
            var caster = new Entity(1, 1);
            var target = new Entity(2, 1);
            var request = SpellFactory.CreateDirectDamage(caster, target, DamageSchool.Fire, 45f)
                .WithSharedCooldown("firebolt", 8f)
                .WithResourceCost("Mana", 35);

            Assert.That(request.SpellDefinitionId.ToString(), Is.EqualTo("firebolt"));
            Assert.That(request.SharedCooldownSeconds, Is.EqualTo(8f));
            Assert.That(request.AdditionalResourceCosts.Length, Is.EqualTo(1));
        }
    }
}
```
Recommended test coverage
1. **BasicCastingTests** – enqueue a direct damage request, simulate a tick (or call factory stubs), assert the resulting struct matches doc expectations.
2. **ProjectileSpawnTests** – ensure `ProjectileDefinition` copies speed/magnitude and that `ImpactSpellDefinitionId` is preserved.
3. **CompositeSpellTests** – register a small composite, queue via `CompositeSpellFactory.Queue`, assert queued child requests in order.
4. **ResourceCooldownTests** – verify `.WithSharedCooldown` and `.WithResourceCost` fields persist when chained.

Structure tips
- Group spell tests under `Tests/HeadlessStub/Spells`. Update the csproj to include new files so the CLI harness sees them.
- Use the shared stubs guarded by `#if CF_HEADLESS` (see `SpellDocSampleHarness.cs`) to avoid duplicating runtime logic while still compiling without Unity packages.
- Prefer deterministic asserts (no randomness). When validating variance or sampling, inject the variance roll explicitly (`varianceRoll: 0f`).

Notes
- Tests double as documentation—they mirror the snippets in `Framework/Docs/Spells/Examples.md`.
- Keep tests fast (<5ms). Avoid spinning up Worlds unless you absolutely need system integration; most scenarios can be expressed as pure data tests.
- If you simulate the pipeline, dispose worlds in `[TearDown]` to avoid interfering with other suites.

Links
- [Quickstart](SpellAPI-QuickStart.md)
- [Examples](Examples.md)
- [FAQ](FAQ.md)
