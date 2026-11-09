# Step 09 — Parameterization Templates

**Phase:** C. Compilation  
**Depends on:** 08
**Delivers:** Versioned parameterization templates that quantify a compiled model, with stable identities,
signature compatibility, rebase/migration, and completeness checks.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** Not requested
- **Last updated:** —

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Separate model definition (geometry and semantics) from numerical values (interaction energies and attempt
frequencies). A user must be able to create a template from a compiled model, edit values, reuse it across
builds, and know when the model has changed enough to invalidate it.

## Original reference

- `ProjectCustomizationTemplate` contained:
  - stable/unstable pair energy sets;
  - group energy sets;
  - kinetic transition rule sets with attempt frequencies.
- Objects:
  - `PairEnergySetData` — center/partner position, distance, start/end vectors, interaction counts, chiral
    partner index, model index, pair energy entries.
  - `GroupEnergySetData` — group interaction, center position, base geometry, extension count, entries with
    center particle + occupation state.
  - `KineticRuleSetData` / `KineticRuleData` — transition, rule index, rule flags, start/transition/final
    states, attempt frequency, dependency count.
- Default attempt frequency: `1e13 Hz`.
- `ModelHash` was `string.GetHashCode()`, randomized on modern .NET; it was a portability landmine.
- The original could not edit site energies through a template; they had to be changed in the energy model.
- Chiral pair sets were linked and mirrored values.
- A "migration tool" copied values between templates by geometric/occupation matching and produced a report.

## Design

### 1. Template shape

```csharp
public sealed record ParameterizationTemplate(
    ParameterizationId Id,
    string Name,
    string ModelSignature,
    DateTimeOffset CreatedUtc,
    ImmutableArray<StablePairEnergySet> StablePairSets,
    ImmutableArray<UnstablePairEnergySet> UnstablePairSets,
    ImmutableArray<GroupEnergySet> GroupSets,
    ImmutableArray<SiteEnergyOverride> SiteEnergyOverrides,
    ImmutableArray<TransitionRuleSet> TransitionRuleSets);
```

- `ModelSignature` is the **compiled-model signature** from step 08 (geometry/rules only). It is distinct from
  the **parameterization signature** (compiled model + energy/frequency values), which becomes the `.msl`
  model signature and the `.mcs` `metadata.modelSignature`.
- Sets are sorted by their stable interaction/rule IDs for canonical serialization.
- A template may be incomplete; missing values default to 0 eV or 1e13 Hz, and the build warns.
- Multiple templates may target the same model; the build chooses one explicitly.

### 2. Stable identities

- Pair sets: ID is the shared `EnergyId` defined in Step 05. For an achiral pair it is the directed geometry ID;
  for a chiral pair it is the lexicographically smaller of the two directions' geometry IDs, so both
  directions map to one set and one value. The template stores one entry per `EnergyId`.
- Group sets: ID derived from `(clusterId, centerSiteId, canonicalGeometryHash)`.
- Rule sets: ID derived from `(transitionId, canonicalStateTriplet, canonicalDirection)`.
- Site energy overrides: `(siteId, particleId)`.
- The IDs are stable across recompilation if the geometry/semantics are unchanged.
- The template stores human-readable geometry (distance, vectors, state matrices) for display and migration,
  but identity is by ID.

### 3. Creating a template

```csharp
ParameterizationTemplate ParameterizationFactory.Create(
    CompiledModel model,
    ParameterizationOptions options);
```

- Enumerates every compiled pair set, group set, and KMC rule.
- Fills stable/unstable pair entries with 0 eV.
- Fills group entries with 0 eV.
- Fills rule frequencies with the transition's default or 1e13 Hz.
- Fills site energy overrides from the model's site energies (optional; empty means use model values).
- Records `ModelSignature`.
- Produces a summary: number of sets, entries, missing values, and estimated table size.

### 4. Applying a template

```csharp
public sealed record ParameterizationResult(
    ParameterizedModel Model,
    ImmutableArray<Diagnostic> Diagnostics);

ParameterizationResult ParameterizationService.Apply(
    CompiledModel model,
    ParameterizationTemplate template);
```

- The returned `Model.Signature` becomes the `.msl` model signature and the `.mcs`
  `metadata.modelSignature`; it distinguishes packages that share geometry but use different values.
- Verifies `template.ModelSignature == model.Signature`; otherwise returns a signature mismatch
  diagnostic with a diff summary.
- Maps each set/rule by ID; unknown IDs are warnings (the model changed); missing required sets default.
- Validates values:
  - energy finite and within −100..100 eV;
  - frequency finite and > 0 (practical range 1e-3..1e18 Hz);
  - chiral partners share one `EnergyId`, so exactly one value is stored and independent A→B/B→A values
    cannot be represented.
- Produces a `ParameterizedModel` ready for job encoding:
  `record ParameterizedModel(CompiledModel Model, string Signature, EnergyTables Energies, RuleFrequencies Frequencies)`
  in `Mocassin.Contracts`; `Mocassin.Compilation` stays parameterization-free and `Mocassin.Packages`
  applies the template.
  Its `Signature` is the **parameterization signature** (compiled-model signature + canonical values) using
  the ADR 0010 MessagePack projection.
- Never mutates the template or model.

### 5. Rebase and migration

When the model signature changes:

- `ParameterizationDiff` lists:
  - added sets/rules;
  - removed sets/rules;
  - changed geometry (same ID impossible; shown as remove+add);
  - unchanged sets.
- `ParameterizationService.Rebase(template, newModel, strategy)`:
  - `KeepMatching` — copy values for unchanged IDs, defaults for new, drop removed;
  - `NearestGeometry` — optionally match by distance/geometry within a tolerance and report every match;
  - `Reset` — create a fresh template.
- Rebase always produces a report; the GUI shows it before applying.
- Never silently reuse values across changed geometry unless the user selects the nearest-geometry strategy.

### 6. Site energy overrides

- New capability: a template may override the model's site energies.
- The override is applied after the model's site energy background is compiled.
- The GUI marks overridden values and offers "reset to model".
- The build warns if an override targets a site/particle combination that is not valid in the compiled model.
- This intentionally fixes the original limitation; it is recorded as an improvement in the parity matrix.

### 7. Completeness and quality

- `ParameterizationSummary`:
  - total pair sets/entries;
  - total group sets/entries;
  - total rule sets/rules;
  - entries with non-default values;
  - missing values;
  - chiral pairs;
  - frequency distribution (min/max/median);
  - energy range.
- Warnings:
  - all-zero energies (likely unparameterized);
  - attempt frequencies outside a plausible range;
  - duplicate values (possible copy error);
  - incomplete rule sets;
  - template signature mismatch.
- The GUI can filter "unset", "non-zero", "chiral", and "changed since last build".

### 8. Validation and diagnostics

| Code | Severity | Rule |
| --- | --- | --- |
| `PRM001` | Error | Template signature does not match the compiled model |
| `PRM002` | Warning | Template has unknown set/rule IDs |
| `PRM003` | Warning | Template is missing required sets/rules |
| `PRM004` | Error | Energy outside −100..100 eV or non-finite |
| `PRM005` | Error | Frequency ≤ 0 or non-finite |
| `PRM006` | Warning | All values default (template may be unparameterized) |
| `PRM007` | Reserved | Not used. Step 05 gives chiral partners one shared `EnergyId`, so independent values cannot exist; the code is retained as reserved rather than renumbered. |
| `PRM008` | Warning | Site energy override targets an invalid site/particle |
| `PRM009` | Info | Template rebased; N entries copied, M dropped |
| `PRM010` | Warning | Duplicate template name |

## Implementation tasks

- [ ] Implement parameterization records and stable identity derivation.
- [ ] Implement `ParameterizationFactory.Create` from a compiled model.
- [ ] Implement `ParameterizationService.Apply` with signature checks.
- [ ] Implement value validation and chiral mirroring.
- [ ] Implement `ParameterizationDiff`, `Rebase`, and the report.
- [ ] Implement site energy overrides.
- [ ] Implement summary/quality checks.
- [ ] Implement editor operations (set/clear/bulk fill/import/export).
- [ ] Add CLI `mocassin parameterize` (create/validate/export) or fold into `compile`.
- [ ] Add ceria and barium-zirconate templates.

## Acceptance criteria

- [ ] Creating a template from the ceria compiled model produces exactly the expected sets/rules.
- [ ] Applying the template to the same model succeeds and yields the same compiled values.
- [ ] A changed model produces a signature mismatch with a useful diff.
- [ ] Rebase copies unchanged values, defaults new ones, and reports removed ones.
- [ ] Chiral pair values are mirrored and cannot diverge.
- [ ] Site energy overrides apply and can be reset.
- [ ] Missing/zero values produce warnings, not silent success.
- [ ] Template JSON is canonical and round-trips.
- [ ] Documentation written

## Documentation

At this point, document parameterization:

- `docs/user-guide/parameterization.md` — template creation, energies, frequencies, chiral mirroring, rebase.
- `docs/reference/project-format.md` — template fields, stable IDs, model signature.
- `docs/development/compilation-pipeline.md` — signature/rebase flow and completeness checks.
- `docs/troubleshooting/errors.md` — stale/incompatible templates and missing values.

## Tests

| Test | Assertion |
| --- | --- |
| Factory | Set/rule counts match the compiled model |
| Apply | Values map to the correct tables |
| Signature | Mismatch rejected with `PRM001` |
| Rebase | Keep/nearest/reset strategies and report |
| Chiral | Mirrored values and conflict detection |
| Site overrides | Applied/reset/invalid target |
| Completeness | Warnings for unset/out-of-range values |
| Round-trip | Canonical JSON stable |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Stable IDs change unexpectedly | Derive from geometry, not list order; test round-trips |
| Users lose values on model edits | Rebase with explicit report; never auto-apply nearest matches |
| Chiral divergence | Single storage location and mirroring invariant |
| Template proliferation | Names, signatures, and a "rebase" workflow in the GUI |

## Done when

A parameterization can be created from any compiled model, edited, validated, applied, rebased with a report,
and serialized without losing or silently changing values.
