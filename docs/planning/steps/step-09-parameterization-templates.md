# Step 09 — Parameterization Templates

**Phase:** C. Compilation

**Depends on:** 08

**Delivers:** Versioned parameterization templates that quantify a compiled model, with stable identities,
signature compatibility, deterministic apply/defaulting, rebase/migration, site-energy overrides, and
completeness checks.

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
| `template-model-and-persistence` | `feature/step-09-parameterization-templates/template-model-and-persistence` | Proposed | — | — | — |
| `parameterization-signature` | `feature/step-09-parameterization-templates/parameterization-signature` | Proposed | — | — | — |
| `factory-and-summary` | `feature/step-09-parameterization-templates/factory-and-summary` | Proposed | — | — | — |
| `apply-and-site-energies` | `feature/step-09-parameterization-templates/apply-and-site-energies` | Proposed | — | — | — |
| `validation-and-diagnostics` | `feature/step-09-parameterization-templates/validation-and-diagnostics` | Proposed | — | — | — |
| `diff-and-rebase` | `feature/step-09-parameterization-templates/diff-and-rebase` | Proposed | — | — | — |
| `golden-templates-docs-and-closeout` | `feature/step-09-parameterization-templates/golden-templates-docs-and-closeout` | Proposed | — | — | — |

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

The new design keeps the scientific concepts — stable/unstable pair values, group values, rule frequencies,
shared chiral values, and a migration report — but replaces the random model hash, mirrored chiral sets, dense
default-filled tables, duplicated geometry, and string-based errors with content identities, one value per
shared `EnergyId`, sparse defaults, computed descriptors, and the `PRM` diagnostic family.

## Design

### 1. Scope, non-goals, and sizing

**In scope**

- Immutable template records, project JSON persistence, canonical ordering, and structural validation.
- `ParameterizationFactory.Create` and `Describe`, including required-slot counts and quality metrics.
- `ParameterizationService.Apply`, defaulting, quantization, the site-energy background, overrides, and
  `ParameterizedModel`.
- The `ParameterizationSignatureV1` canonical projection and its golden vectors; finalization of the existing
  `signatures.parameterization` manifest entry.
- `PRM001`–`PRM016` diagnostics, catalog composition, paths, and severity rules.
- `ParameterizationDiff`, `Describe`, and the `KeepMatching`, `NearestGeometry`, and `Reset` rebase
  strategies with a report.
- Template editing operations that are pure and usable by Step 26 (set/clear values, rename, site override
  reset), plus checked-in ceria and barium-zirconate templates.
- Performance and benchmark targets, and the documentation/parity/closeout work for the step.

**Out of scope**

- Any mutation of a compiled model, project, or template in place.
- Writing `.msl`, job expansion, supercells, doping, `.mcs` files, solver execution, and results.
- The `mocassin` CLI and build orchestration. Step 12 owns `mocassin build` and `mocassin compile`; Step 09
  ships a library API only. There is no `mocassin parameterize` command (see §7.2).
- The Avalonia parameterization editor and template import/export UI (Step 26).
- Any new package/state format; Step 11 writes the `.msl` payload that embeds the Step 09 values.
- Any change to `Mocassin.Compilation` beyond making its canonical projection writer reusable (§6). The
  compiler stays parameterization-free.

**Sizing and split check.** Roadmap §3.1 estimates Step 09 at 1–3 person-weeks. Seven features are proposed,
including the documentation/closeout feature. The signature/manifest finalization, compiled-bound diagnostics,
site-energy background, and golden/rebase fixtures make the honest estimate **3–4 person-weeks**, above the
roadmap midpoint but below the four-week split trigger; the feature count is within 6–8. No roadmap split is
proposed: the features form one coherent template/apply/rebase contract, and a split would insert a new step
into the Step 09–12 dependency chain. If the first three features exceed 2 person-weeks, stop and ask the owner
to re-open the estimate; if the total exceeds four weeks, split before continuing.

**Licensing, status, and audit.** No new third-party code, data, or dependency is introduced; the step uses
the existing MIT source and committed sample projects, and copies no original MOCASSIN source or database
content. This design change does not edit `status.md`; the implementor sets the step `In progress` at step
start. The closeout feature records the feature log, parity, documentation, `./tools/verify.sh all --require`,
the closeout CI run, and requests the independent audit; no audit record is created now.

### 2. Dependency boundary and ownership

Step 08 is merged. The implemented dependency direction is:

```text
Core <- Symmetry <- Compilation <- Contracts <- Packages <- Cli
                                      └──► Results
```

Step 09 must not reverse that direction or add a parameterization edge to `Mocassin.Compilation`.

| Concern | Home | Why |
| --- | --- | --- |
| Template record and value records | `Mocassin.Core.Model` | The template is part of the immutable `Project` aggregate and is serialized in `.mocprj`; Core already owns the Step 01 template stub, project JSON, canonicalization, and `ProjectEditor`. |
| Project persistence and canonical JSON | `Mocassin.Core.Project` + `Mocassin.Packages.Projects` | `ProjectJson`/`ProjectCanonicalizer` stay the single serializer; `ProjectStore` stays the atomic file owner. |
| Structural template validation (`PRM004`, `PRM005`, `PRM010`, `PRM013`, `PRM014`) | `Mocassin.Core.Project.ProjectValidator` | It needs only the project graph, must run for every editor mutation and load, and must not depend on a compiled model. |
| Applied value DTO and signature projection | `Mocassin.Contracts.Parameterization` | `ParameterizationValues` is the persisted value section and must be shared with Step 11; Contracts already references Compilation. |
| Factory, service, compiled-bound validation, diff/rebase, summaries, `ParameterizedModel` | `Mocassin.Packages.Parameterizations` | They consume `Project` + `CompiledModel`, apply metadata, and return diagnostics; Packages already references Core, Compilation, and Contracts. |
| Parameterization diagnostics catalog | `Mocassin.Core.Diagnostics` (codes and descriptors), emitted from Core and Packages | One `PRM` family, one catalog entry, one documentation anchor; Core owns the stable code registry and the docs-drift test. |
| Compiled parameterization values | Not in `Mocassin.Compilation` | The compiler emits zero pair/cluster energies and the default `1e13 Hz`; values are applied after compilation. |

Boundary rules:

- `Mocassin.Compilation` gains no template, value, site-energy, or diagnostics dependency. It already exposes
  every identity and default Step 09 needs (`EnergyId`, `ClusterEnergyId`, `RuleGeometryId`, `FrequencyHz`,
  table values, position masks, site stability).
- `Mocassin.Contracts` may reference `Mocassin.Compilation`; it must not reference `Mocassin.Packages` or
  `Mocassin.Results`. `ParameterizationValues` contains no compiled-model type, so the persisted value section
  stays a plain contract DTO.
- `Mocassin.Packages` composes the build-time `ParameterizedModel`; Step 10 consumes it, Step 11 maps it to the
  `.msl` payload, and `Mocassin.Results` never needs it.
- The `CompiledModel` remains the only compiled geometry/rules/encoding truth. The template stores stable IDs,
  compact display descriptors, and values; it does not duplicate the compiled tables.
- Code layout follows `docs/development/code-structure.md`: Core value records in concept files under
  `Mocassin.Core/Model/` and editor operations in `Mocassin.Core/Editing/`;
  `Mocassin.Contracts/Parameterization/`; and `Mocassin.Packages/Parameterizations/`. No forbidden folder
  names, no catch-all helper folder, and no namespace baseline change.

**Actual Step 08 boundary correction.** The implemented `CompiledEnergyModel` contains pair geometries, pair
tables, and cluster tables. It does **not** compile the authored site-energy background, although the Step 08
design text mentions a "defect/site-energy background". Step 09 resolves `Project.Model.SiteEnergies` onto the
compiled `[position][particle]` background, applies template overrides, and hands the result to Step 11. This
avoids reopening the completed Step 08 and keeps the authored site energies in their existing Core home.

### 3. Template persistence, versioning, and identity

#### 3.1 One project-resident document

- A template is stored inside the `.mocprj` `parameterizations` array. There is no separate template file,
  sidecar, database, or second serialization path in v1. `Mocassin.Packages.Projects` remains the project
  persistence owner; `ProjectStore` writes the enclosing file atomically.
- The existing `Project.parameterizations` array and `BuildTemplate.parameterizationId` reference are the only
  project-level bindings. A build selects one template explicitly by `ParameterizationId`; multiple templates
  may target the same compiled model.
- `ParameterizationId` is the existing Core typed UUIDv7. It is assigned when a template is created, never
  derived from the model, and preserved by rebase so build templates keep working. A copy gets a new ID.
- Canonical JSON sorts `parameterizations` by `Id.Value` (already implemented). Within a template, the record
  constructor sorts sets and entries by their stable keys and normalizes `-0.0` to `+0.0`, so authoring order
  cannot change the document hash.
- Template names are labels, not references. They are compared case-insensitively; a duplicate is `PRM010`
  warning. Empty names remain the existing `PROJ020` error.
- There is no template-level `formatVersion` in v1. The template shape is part of the project `schemaVersion`.
  Step 09 extends the Step 01 stub additively before the first released `.mocprj` (the stub has no value
  fields and no sample or fixture populates one), so `schemaVersion` stays `1` and no migration is needed.
  After the first release, any semantic template change bumps `schemaVersion` and adds a pure migration in
  `Mocassin.Core.Project.ProjectMigrations`. Never reinterpret an existing field.
- A future standalone template export/import is a Step 26 UI convenience over the same JSON object; if it
  becomes a separate on-disk contract, it needs its own version and an ADR. It is not introduced here.

#### 3.2 Record shape

The existing `Mocassin.Core.Model.ParameterizationTemplate` stub is extended. The value records live beside it
in `Mocassin.Core.Model` (concept file `Templates.cs` or a split `ParameterizationTemplates.cs`):

```csharp
public sealed record ParameterizationTemplate
{
    public ParameterizationTemplate(
        ParameterizationId id,
        string name,
        string modelSignature,
        DateTimeOffset createdUtc,
        ImmutableArray<PairEnergySet> stablePairSets = default,
        ImmutableArray<PairEnergySet> unstablePairSets = default,
        ImmutableArray<GroupEnergySet> groupSets = default,
        ImmutableArray<SiteEnergyOverride> siteEnergyOverrides = default,
        ImmutableArray<TransitionRuleSet> transitionRuleSets = default);

    public ParameterizationId Id { get; init; }
    public string Name { get; init; }
    public string ModelSignature { get; init; }
    public DateTimeOffset CreatedUtc { get; init; }
    public ImmutableArray<PairEnergySet> StablePairSets { get; init; }
    public ImmutableArray<PairEnergySet> UnstablePairSets { get; init; }
    public ImmutableArray<GroupEnergySet> GroupSets { get; init; }
    public ImmutableArray<SiteEnergyOverride> SiteEnergyOverrides { get; init; }
    public ImmutableArray<TransitionRuleSet> TransitionRuleSets { get; init; }
}

public sealed record PairEnergySet(
    string EnergyId,
    CellSiteId CenterSiteId,
    CellSiteId PartnerSiteId,
    int DistanceBucketFm,
    bool IsChiral,
    ImmutableArray<PairEnergyEntry> Entries);

public sealed record PairEnergyEntry(
    ParticleId CenterParticleId,
    ParticleId PartnerParticleId,
    EnergyEv Energy);

public sealed record GroupEnergySet(
    string ClusterEnergyId,
    ClusterId ClusterId,
    CellSiteId CenterSiteId,
    ImmutableArray<GroupEnergyEntry> Entries);

public sealed record GroupEnergyEntry(
    ImmutableArray<ParticleId> OccupationParticleIds,
    EnergyEv Energy);

public sealed record SiteEnergyOverride(
    CellSiteId SiteId,
    ParticleId ParticleId,
    EnergyEv Energy);

public sealed record TransitionRuleSet(
    TransitionKind TransitionKind,
    Guid TransitionId,
    ImmutableArray<RuleFrequencyEntry> Rules);

public sealed record RuleFrequencyEntry(
    string RuleGeometryId,
    FrequencyHz Frequency);
```

- The `PairEnergySet` descriptor is display/rebase metadata, not identity. The identity is `EnergyId`. For a
  reciprocal chiral table, the descriptor comes from the direction whose `GeometryId` equals the shared
  `EnergyId` (the lexicographically smaller direction); the inverse direction is represented by the same set
  and is not a second value. For an achiral or unstable–stable table, every geometry sharing the ID agrees on
  the descriptor.
- The `GroupEnergySet` descriptor is display/rebase metadata. The identity is `ClusterEnergyId` plus the
  canonical `OccupationParticleIds` tuple inside the set.
- The `TransitionRuleSet` identity is `(TransitionKind, TransitionId)`. It deliberately carries both because a
  kinetic and a Metropolis transition may in principle share a GUID; the compiled `TransitionRef` already
  distinguishes them.
- `RuleFrequencyEntry` identity is `RuleGeometryId`. The dense `.msl` `rule_id` is never a template identity.
  Auto-generated inverse rules are individually addressable (they have their own `RuleGeometryId`); MMC rules
  are not parameterizable and a Metropolis rule set in a template is ignored with `PRM011`.
- `SiteEnergyOverride` identity is `(SiteId, ParticleId)`.
- `EnergyEv` and `FrequencyHz` are the existing Core unit types, so non-finite values cannot be constructed by
  the typed API. Range and positivity rules are still validated for JSON and programmatic input.
- `CreatedUtc` keeps the existing `UtcDateTimeOffsetJsonConverter` annotation from the Step 01 stub; it is
  normalized to UTC `Z` by the project serializer and excluded from the parameterization signature.

#### 3.3 Sparse values and defaults

Templates are sparse. Absence means "not parameterized":

- missing pair value → `0 eV`;
- missing group value → `0 eV`;
- missing rule frequency → the compiled Step 08 default `1e13 Hz`;
- missing site-energy override → the authored model background (not necessarily zero).

An explicit `0 eV` or explicit `1e13 Hz` is a real entry. This distinguishes "reviewed and left at the default"
from "unset" and makes completeness meaningful. The original dense/default-filled template could not express
that distinction; this is an intentional improvement recorded in the parity matrix.

Creation writes one **set header** for every compiled pair table, cluster table, and **kinetic** transition
collection, but no value entries. Headers make the template self-describing for the editor and the diff;
values stay sparse. Site-energy headers are not written because the per-site/particle slot space is large and
the model background is the default. `ParameterizationService.Describe` enumerates every required slot,
including individual kinetic rules, for the UI.

Rule frequencies are a kinetic (KMC) concept. `TransitionRuleSets` are created only for kinetic collections;
MMC rules are not parameterizable and keep their compiled default. `ParameterizationValues.TransitionRuleFrequencies`
still has one entry per compiled rule in rule order (MMC entries are the compiled default), so Step 11 and the
signature have a uniform rule-order array. `Describe` counts required frequency slots for kinetic rules only.

#### 3.4 Stable identities and geometry changes

| Value kind | Identity | Derived from |
| --- | --- | --- |
| Pair value | `EnergyId` | Step 05/08 shared pair energy identity; equals `GeometryId` for achiral and unstable–stable pairs; the lexicographically smaller `GeometryId` for a reciprocal chiral pair. The set descriptor uses the direction whose `GeometryId` equals the shared `EnergyId`; for an achiral table every geometry agrees. |
| Group value | `ClusterEnergyId` + canonical occupation tuple | Step 08 `SHA-256("mocassin/cluster-energy-id/v1" \|\| clusterId \|\| centerSiteId \|\| canonical geometry hash)`; the tuple is the compiled canonical tuple expressed as `ParticleId`s in site order. |
| Rule frequency | `RuleGeometryId` | Step 08 `SHA-256("mocassin/rule-geometry-id/v1" \|\| transition \|\| chain pattern \|\| representative direction \|\| state0 \|\| state1 \|\| state2)`. |
| Site override | `(SiteId, ParticleId)` | Authored `CellSiteId` and `ParticleId`; site energies are per reference site, not per P1 position. |
| Rule set | `(TransitionKind, TransitionId)` | The compiled collection's transition reference. |

- IDs are lowercase 64-character hexadecimal strings. A malformed or duplicate key is `PRM014`.
- IDs survive recompilation while geometry, particle order, transition semantics, and the relevant hash domains
  are unchanged. Dense indices are never identities.
- A geometry change (site coordinate, cell, cutoff, cluster offsets, chain/state changes, particle insertion
  that shifts compiled indices, or catalog/contract change that changes the model signature) changes the
  model signature. `Apply` rejects the template with `PRM001` before ID matching. Rebase is an explicit,
  reported operation.
- When an ID changes, the old entry is `Removed` and the new slot is `Added`; the value defaults. Values are
  never silently reused across changed geometry. The only copying across different IDs is the explicit
  `NearestGeometry` strategy, and every match appears in the report (§8).
- **Chiral geometry.** A reciprocal chiral pair shares one `EnergyId`, one pair table, and one value. The
  template has one set per `EnergyId`; there is no API, JSON field, or editor operation that can express
  independent A→B and B→A values. The shared table must be symmetric under particle exchange because the
  solver applies it from both centre directions: setting a chiral entry `(X, Y, E)` stores `table[X][Y] = E`
  and `table[Y][X] = E`. The template stores the lexicographically smaller `(ParticleId, ParticleId)`
  orientation; a mirrored duplicate with a different value is `PRM014`, and `Describe` counts chiral slots as
  unordered particle pairs. Achiral and unstable–stable tables are ordered and are not mirrored. If the chiral
  link changes or breaks, the shared `EnergyId` changes (or two IDs replace one), the old value is removed, and
  the new slots default. `PRM007` stays retired and is not registered.
- **Unstable–stable pairs.** They remain in `UnstablePairSets`, use `GeometryId == EnergyId`, have no chiral
  partner, and are parameterized exactly once per table.

#### 3.5 Canonical ordering and JSON

Canonical order is:

1. `stablePairSets`, `unstablePairSets` by `EnergyId` ordinal; entries by `(CenterParticleId.Value,
   PartnerParticleId.Value)`.
2. `groupSets` by `ClusterEnergyId` ordinal; entries by the `OccupationParticleIds` GUID sequence in site
   order.
3. `siteEnergyOverrides` by `(SiteId.Value, ParticleId.Value)`.
4. `transitionRuleSets` by `(TransitionKind, TransitionId)`; `rules` by `RuleGeometryId` ordinal.

The persisted JSON shape is:

```json
{
  "id": "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e6f",
  "name": "Ceria parameters",
  "modelSignature": "812a…",
  "createdUtc": "2026-11-14T12:00:00Z",
  "stablePairSets": [
    {
      "energyId": "…",
      "centerSiteId": "…",
      "partnerSiteId": "…",
      "distanceBucketFm": 248,
      "isChiral": false,
      "entries": [
        { "centerParticleId": "…", "partnerParticleId": "…", "energyEv": 0.0 }
      ]
    }
  ],
  "unstablePairSets": [],
  "groupSets": [
    {
      "clusterEnergyId": "…",
      "clusterId": "…",
      "centerSiteId": "…",
      "entries": [
        { "occupationParticleIds": ["…", "…"], "energyEv": 0.48 }
      ]
    }
  ],
  "siteEnergyOverrides": [
    { "siteId": "…", "particleId": "…", "energyEv": 0.0 }
  ],
  "transitionRuleSets": [
    {
      "transitionKind": "kinetic",
      "transitionId": "…",
      "rules": [
        { "ruleGeometryId": "…", "frequencyHz": 1.0e13 }
      ]
    }
  ]
}
```

- UTF-8 without BOM, two-space indent, `\n` line endings, invariant number formatting, `-0.0` normalized to
  `0.0`. Template values are not rounded on save; `Apply` quantizes to 12 decimals. Two templates that differ
  only beyond the 12th decimal therefore have different project document hashes but the same applied values
  and parameterization signature, which is deliberate: the signature identifies the effective model, not the
  authored text.
- Unknown properties are rejected by the existing `JsonUnmappedMemberHandling.Disallow` policy.
- A missing value array loads as empty. The Step 01 stub is readable; no legacy template format is imported.
- The `ParameterizationTemplate` constructor normalizes order and signed zero but does **not** throw for
  out-of-range or malformed user values; `ProjectValidator` reports them as `PRM` diagnostics. This keeps a bad
  template loadable so the user can fix it, instead of turning it into an opaque `PROJ002`.

#### 3.6 Bindings to the implemented Step 08 model

| Template data | Implemented source |
| --- | --- |
| Pair table value | `CompiledEnergyModel.PairTables[i].Values`, row-major `[centerParticle][partnerParticle]`, `ParticleCount` per table |
| Pair set descriptor | The representative `CompiledPairGeometry` with `PairTableIndex == table.Index`: `CenterSiteId`, `PartnerSiteId`, `DistanceBucketFm`, `IsChiral`; for a chiral table use the geometry whose `GeometryId == EnergyId` |
| Group table value | `CompiledEnergyModel.ClusterTables[i].Energies`, in `Occupations` order |
| Group set descriptor | `CompiledClusterTable.ClusterEnergyId`, `.ClusterId`, `.CenterSiteIndex`; `CenterSiteId` is `CompiledStructureModel.Sites[CenterSiteIndex].Id` (the table has no `CenterSiteId` property) |
| Rule frequency | `CompiledTransitionModel.Rules[i].FrequencyHz`; `RuleGeometryId` is on the rule |
| Rule set | `CompiledTransitionModel.Collections[i].Transition` (`TransitionRef.Kind`/`.Id`); the rule's `CollectionIndex` links it |
| Particle key | `CompiledParticleTable.Particles[index].Id` and `TryGetIndex(ParticleId, out index)`; index `0` is void |
| Site key | `CompiledStructureModel.Sites[i].Id` / `CompiledPosition.SiteId`; site index is dense ascending `CellSiteId` |
| Allowed particle slot | `CompiledPosition.AllowedParticleMask` for any position owned by the reference site; void is excluded |
| Site-energy background | `Project.Model.SiteEnergies` (`SiteEnergySet`/`SiteEnergyEntry`) mapped through the compiled sites and positions |

#### 3.7 Editing operations

- `ProjectEditor.AddParameterizationTemplate`, `UpdateParameterizationTemplate`, and
  `RemoveParameterizationTemplate` are pure project mutations validated through the existing
  `ProjectValidator`; they return `ProjectEditResult` like the other editors.
- `Mocassin.Core.Editing.ParameterizationTemplateEditor` (concept file `ParameterizationTemplates.cs`) owns
  pure template-level operations: `Rename`, `SetPairEnergy`, `ClearPairEnergy`, `SetGroupEnergy`,
  `ClearGroupEnergy`, `SetRuleFrequency`, `ClearRuleFrequency`, `SetSiteEnergyOverride`,
  `RemoveSiteEnergyOverride`, and `ClearValues`. Each returns a new template plus diagnostics; none mutates its
  input. The operations look up stable keys in the template, not in a compiled model.
- Bulk fill, import/export, and the filterable table UI are Step 26 conveniences built on these operations.
  Step 09 does not add a second project editor, a service locator, or mutable editor state.

### 4. Creating a template

```csharp
public sealed record ParameterizationTemplateOptions
{
    public required string Name { get; init; }
    public ParameterizationId? Id { get; init; }
    public DateTimeOffset? CreatedUtc { get; init; }
    public static ParameterizationTemplateOptions Default { get; } = new() { Name = "Parameters" };
}

public sealed record TemplateCreationResult(
    ParameterizationTemplate Template,
    ParameterizationSummary Summary,
    ImmutableArray<Diagnostic> Diagnostics);

public sealed class ParameterizationFactory(TimeProvider? timeProvider = null)
{
    public TemplateCreationResult Create(
        CompiledModel model,
        ParameterizationTemplateOptions options);
}
```

- The factory is pure, synchronous, and does not require a `Project`. It assigns a UUIDv7 (or the supplied ID),
  uses `TimeProvider.GetUtcNow()` (or the supplied timestamp), records `model.ModelSignature`, and creates the
  set headers (§3.3) with empty entries. Rule-set headers are kinetic only.
- It partitions pair tables into `stablePairSets`/`unstablePairSets` by the centre reference site's stability;
  every table appears exactly once. A chiral table uses the direction whose `GeometryId` equals the shared
  `EnergyId` for its descriptor. Group headers use `ClusterEnergyId`; kinetic transition headers use
  `(TransitionKind, TransitionId)`. A contract violation (for example a table with conflicting centre
  stability or no geometry) is an `InvalidOperationException`, because the compiled model is compiler-owned.
- It never fills values. A newly created template therefore applies to the same effective values as an
  unparameterized build: zero pair/cluster energies, `1e13 Hz`, and the authored site-energy background.
- It validates compiled-model contract invariants (every pair table has exactly one geometry, table lengths
  are `nParticles × nParticles`, occupation/rule arrays are consistent) with `InvalidOperationException`,
  because a `CompiledModel` is compiler-owned and a violation is a programmer/contract error, not user data.
- The returned summary reports required slots, set headers, chiral pair tables, estimated value bytes, and the
  effective-default ranges. It is the same summary produced by `Describe` with no template.
- Name duplication is checked by `ProjectValidator` when the template is added to a project (`PRM010`), not by
  the model-only factory. The factory does not mutate the project; `ProjectEditor.AddParameterizationTemplate`
  commits it.

### 5. Applying a template

```csharp
public sealed record ParameterizationResult(
    ParameterizedModel? Model,
    ImmutableArray<Diagnostic> Diagnostics,
    ParameterizationSummary Summary,
    ParameterizationDiff? Diff)
{
    public bool IsSuccess => Model is not null
        && !Diagnostics.Any(static d => d.Severity == DiagnosticSeverity.Error);
}

public sealed record ParameterizedModel(
    CompiledModel CompiledModel,
    ParameterizationValues Values,
    string Signature)
{
    public string ModelSignature => CompiledModel.ModelSignature;
}

public interface IParameterizationService
{
    ParameterizationResult Apply(
        Project project,
        CompiledModel model,
        ParameterizationTemplate template,
        ParameterizationOptions? options = null,
        CancellationToken cancellationToken = default);

    ImmutableArray<Diagnostic> Validate(
        Project project,
        CompiledModel model,
        ParameterizationTemplate template,
        ParameterizationOptions? options = null,
        CancellationToken cancellationToken = default);

    ParameterizationSummary Describe(
        Project project,
        CompiledModel model,
        ParameterizationTemplate? template = null,
        CancellationToken cancellationToken = default);

    ParameterizationDiff Diff(
        Project project,
        CompiledModel model,
        ParameterizationTemplate template,
        CancellationToken cancellationToken = default);

    ParameterizationRebaseResult Rebase(
        Project project,
        CompiledModel model,
        ParameterizationTemplate template,
        ParameterizationRebaseOptions options,
        CancellationToken cancellationToken = default);
}
```

`ParameterizedModel` lives in `Mocassin.Packages.Parameterizations`. It is the build-time composition consumed
by Step 10 and mapped by Step 11; it is **not** the persisted payload DTO. The full `.msl` payload is assembled
by Step 11 from `CompiledModel` + `ParameterizationValues` (see §6 and the data contract §3.3).

`Validate` is a non-allocating preflight: it checks the model signature, IDs, descriptor consistency, ranges,
limits, and completeness, but does not build dense applied tables or compute the signature. `Apply` performs
the full build and repeats the checks defensively. Step 12 can therefore call `Validate` after compilation and
then `Apply` without paying for a second full table allocation.

```csharp
public sealed record ParameterizationOptions
{
    public bool EmitCompletenessWarnings { get; init; } = true;
    public ParameterizationQualityOptions Quality { get; init; } = new();
    public long MaxAppliedValueBytes { get; init; } = 2L * 1024 * 1024 * 1024;
    public static ParameterizationOptions Default { get; } = new();
}

public sealed record ParameterizationQualityOptions
{
    public double MinimumPlausibleFrequencyHz { get; init; } = 1e-3;
    public double MaximumPlausibleFrequencyHz { get; init; } = 1e18;
    public int IdenticalValueMinimumCount { get; init; } = 20;
    public double IdenticalValueShareThreshold { get; init; } = 0.90;
    public static ParameterizationQualityOptions Default { get; } = new();
}
```

`Apply` requires the same `Project` snapshot that produced `CompiledModel`, because the site-energy background
comes from the project. The compiled model does not persist a project/document hash, so the service cannot
verify the pairing; Step 12's build pipeline passes the pair, and the GUI/tests pass real project/model pairs.
A mismatched project is a caller/programmer error, not user data; site IDs that do not exist in the compiled
model are reported/ignored rather than silently applied.

`Apply` algorithm:

1. Reject a null/invalid template with programmer exceptions; validate user data through diagnostics.
2. Compare `template.ModelSignature` with `model.ModelSignature`. On mismatch, return `PRM001` with the
   `ParameterizationDiff` summary, `Model = null`, and no partially applied result. Never rebase implicitly.
3. Build lookup maps: `EnergyId` → pair table index, `ClusterEnergyId` → cluster table index,
   `RuleGeometryId` → rule index, `ParticleId` → compiled index, `CellSiteId` → site index.
4. Allocate dense applied tables:
   - pair values `nParticles × nParticles` per pair table, row-major, in table order;
   - group values per cluster table, in `Occupations` order;
   - rule frequencies per compiled rule, in rule order;
   - site energies `positionCount × particleCount`, row-major, position-major.
5. Fill defaults, then overlay template entries:
   - pair/group entries map authored `ParticleId`s to compiled indices; an unknown particle, a particle not in
     the relevant site mask, or an unknown set is `PRM002`/`PRM012` and is ignored;
   - rule entries map `RuleGeometryId`; an unknown rule is `PRM002` and is ignored;
   - site overrides replace the authored background at every P1 position owned by the reference site; an
     unknown site, unknown particle, or forbidden particle is `PRM008` and is ignored.
6. Validate every effective value: finite energy in `-100..100 eV` (`PRM004`), finite frequency `> 0`
   (`PRM005`), plausible frequency range (`PRM016`, warning), and configured resource limits (`PRM013`). A
   non-finite value is a contract error for a typed value but is still checked defensively.
7. Quantize every applied value to 12 decimal places with round-half-away-from-zero on the binary64 product
   and normalize `-0.0` to `+0.0`. The quantized values are what `ParameterizationValues` stores and what
   Step 11 persists, so re-encoding the payload reproduces the signature exactly.
8. Compute `Signature = ParameterizationSignature.Compute(model.ModelSignature, values)`.
9. Return `ParameterizedModel` plus the summary and diagnostics. `Apply` never mutates the project, compiled
   model, or template.

Defaulting and warning rules:

- Missing pair/cluster values default to `0 eV`; missing rule frequencies default to the compiled
  `CompiledJumpRule.FrequencyHz` (currently `1e13 Hz`); missing site overrides use the authored background.
- `PRM003` warns once with counts when required pair/group/rule slots are unset.
- `PRM006` warns when no explicit value differs from its default.
- A build may proceed with warnings; `--warnings-as-errors`/`WarningsAsErrors` can promote them in Step 12.
- Signature mismatch is the only condition that makes the parameterization unusable. Resource-limit errors
  also fail the apply; warnings do not.
- Entries ignored by `PRM002`/`PRM008`/`PRM011`/`PRM012` do not change the applied values or the signature;
  they produce diagnostics and appear in the diff/report. This keeps the package signature an identity of the
  effective model, not of stale template text.

Site-energy background (owned by Step 09 because Step 08 does not compile it):

- Resolve each authored `SiteEnergySet` entry by `CellSiteId` to the compiled reference site and by
  `ParticleId` to the compiled particle index.
- Copy the value to every `CompiledPosition` whose `SiteIndex` owns that site. Particle `0` (void) and
  particles outside the site's allowed mask stay `0 eV`; an authored entry that `ENM016` already warned about
  is ignored rather than silently forced into the background.
- Missing authored entries stay `0 eV`. The table is dense `[position][particle]` row-major, matching the
  `.msl` `energy.defect_background` field and Step 15's per-position background consumption.
- An authored site-energy entry whose site or particle does not map to the compiled model is reported once
  (`PRM008`) and ignored; this indicates a mismatched project/model pair or a template built against another
  project, and it must not silently shift the background.
- Overrides replace the background for the named `(site, particle)` everywhere; removing an override restores
  the authored value. The editor exposes "reset to model" as removal, not as writing the old value.
- Step 15 consumes the resulting table unchanged; Step 09 does not change solver semantics, field signs, or
  the barrier convention. A test pins that the applied background equals the authored values plus overrides.

Step 10/11/12 consumption:

- Step 10 receives `ParameterizedModel` as the model half of job expansion; it does not inspect template JSON.
- Step 11 writes `models.signature` = `ParameterizedModel.Signature`, the payload `model_signature` =
  `CompiledModel.ModelSignature`, the applied pair/cluster/site values, and the applied rule frequencies. It
  computes the `.msl` `frequency_factor` from those absolute values; Step 08's default is not a second runtime
  source.
- Step 12 calls `Apply` after `IModelCompiler.Compile` and maps a failure to build error exit `4`. It passes the
  resulting `ParameterizedModel` to Step 10/11.

### 6. Parameterization signature

#### 6.1 Domain and projection

```csharp
public sealed record ParameterizationValues(
    ImmutableArray<ImmutableArray<double>> PairTables,
    ImmutableArray<ImmutableArray<double>> GroupSets,
    ImmutableArray<double> SiteEnergies,
    ImmutableArray<double> TransitionRuleFrequencies);
```

Do not assert record equality for `ParameterizationTemplate` or `ParameterizationValues`: nested
`ImmutableArray<T>` equality is reference-based. Tests compare canonical JSON bytes, array contents, and
signatures instead.

`ParameterizationSignature` is a static class in `Mocassin.Contracts.Parameterization` with the domain constant
and `Compute(string modelSignature, ParameterizationValues values)`.

```text
ParameterizedModel.Signature
  = SHA-256("mocassin/parameterization-signature/v1" || 0x00 || projection)
```

`ParameterizationSignatureV1` is a fixed MessagePack array of six fields:

```text
[
  formatVersion: uint64 = 1,
  modelSignature: string,                         // Step 08 CompiledModel.ModelSignature
  pairTables:     [ [float64 × nParticles²] × pairTableCount ],
  groupSets:      [ [float64 × occupationCount] × clusterTableCount ],
  siteEnergies:   [float64 × (positionCount × particleCount)],
  transitionRules:[float64 × ruleCount]
]
```

- `pairTables` is in ascending compiled `CompiledPairTable.Index`; each table is row-major
  `[centerParticle][partnerParticle]`, `nParticles` including void. Stable/unstable ordering is a template/UI
  concern; the signature uses one ordered list so a reader can reproduce it from the `.msl` payload without
  deriving site stability.
- `groupSets` is in ascending `CompiledClusterTable.Index`; values are in `Occupations` order.
- `siteEnergies` is the dense `[position][particle]` background in ascending `P`, row-major, with particle `0`
  (void) at the start of each row.
- `transitionRules` is in ascending compiled `CompiledJumpRule.Index`; the value is the effective absolute
  frequency in hertz.
- No stable IDs are projected. `modelSignature` cryptographically binds the table/rule order and geometry; the
  values are meaningful only inside that model. This also lets Rust re-encode the projection from the `.msl`
  payload, which does not persist `RuleGeometryId` (those IDs are inspection-only).
- Every float is `float64`, quantized to 12 decimals with round-half-away-from-zero on the binary64 product;
  `-0.0` is normalized to `+0.0`; NaN/Inf are rejected. Strings are UTF-8 as stored. Every field is present;
  arrays use forced canonical headers. Domain separation is applied before hashing.
- **Excluded:** template `Id`, `Name`, `CreatedUtc`, set headers, sparse/unset markers, display descriptors,
  diagnostics, summaries, timings, file names, cache state, job/lattice/run settings, temperature, MCSP, field,
  seeds, and any solver output. The projection contains only the compiled-model identity and the applied
  values.
- The existing `signatures.parameterization` manifest entry is finalized: `status` moves from `skeleton` to
  `final`, `projectionType` becomes `ParameterizationSignatureV1`, and the placeholder fields are replaced by
  the six typed fields above. The two placeholder pair `bin` fields (`stablePairs`, `unstablePairs`) collapse
  into one `pairTables` field; `groupSets`, `siteEnergies`, and `transitionRules` keep their names but become
  typed arrays. The data contract and generated descriptor are updated together. No second signature entry is
  added: the generator requires exactly `model`, `parameterization`, and `lattice`.
- `Mocassin.Compilation.Signatures.CanonicalProjectionWriter` is made public (with XML docs and an updated
  `PublicAPI.Unshipped.txt`/ApiCompat baseline), generalized from `ArrayBufferWriter<byte>` to
  `IBufferWriter<byte>`, and reused by `Mocassin.Contracts`. This is the smallest way to guarantee one
  quantization/header/string implementation for both signed projections; Contracts already references
  Compilation. No `Mocassin.Compilation` type gains parameterization semantics. Every new public type in Core,
  Contracts, and Packages updates its `PublicAPI.Unshipped.txt` and ApiCompat baseline; the
  `./tools/verify.sh api` gate must pass.
- `ParameterizationSignature.Compute` writes through an `IncrementalHash`-backed `IBufferWriter<byte>`, so it
  does not materialize a second full projection of large tables. `ComputeProjection` (to an
  `ArrayBufferWriter<byte>`) remains for the golden `.bin` fixtures and diagnostics.
- `CanonicalProjectionWriter.Quantize` is public (with XML docs) so `Apply` quantizes stored values with the
  same algorithm the writer uses; storing unquantized values would let the payload and signature drift.

#### 6.2 Relationship to the compiled-model signature

| Value | Meaning | Changes when |
| --- | --- | --- |
| `CompiledModel.ModelSignature` | Step 08 geometry/rules/encoding/indexing/catalog identity | Geometry, particles, transitions, active simulations, symmetry contract, or catalog changes |
| `ParameterizedModel.Signature` | Step 09 parameterization identity | The model signature or any applied energy/frequency/site value changes |

- A value-only change changes the parameterization signature but not the model signature; existing templates
  stay valid. Changing authored `model.siteEnergies` is also a value-only change: the compiled model
  signature is unchanged, but the applied background and parameterization signature change.
- Adding/removing a simulation or changing its active-transition set changes the compiled encoding and the
  model signature; templates are invalidated.
- Changing only runtime settings (temperature, MCSP, field, pre-run, seeds, job count) changes neither
  signature; runtime settings are job-expansion inputs.
- `.msl models.signature` and `.mcs metadata.modelSignature` are the **parameterization** signature;
  `.msl payload model_signature` remains the Step 08 compiled-model signature.
- Step 11 persists exactly the quantized `ParameterizationValues`; Step 13's Rust reader re-encodes the same
  six-field projection from the decoded payload and compares it with the stored parameterization signature.
  Cross-language reproduction is part of Step 11/13 acceptance; Step 09 supplies the C# encoder, golden
  projection bytes, and Python re-encoding check.

### 7. Validation and diagnostics

#### 7.1 `PRM` family

Owner: “Step 09 — parameterization templates”. Codes are stable API.

| Code | Severity | Emitted by | Rule |
| --- | --- | --- | --- |
| `PRM001` | Error | Packages `Apply`/`Validate` | Template model signature does not match the compiled model. |
| `PRM002` | Warning | Packages | Template references an unknown pair/group/rule ID, or a stored descriptor disagrees with the current model for the same ID. The model wins; the entry is ignored. |
| `PRM003` | Warning | Packages | Required pair/group/kinetic-rule slots are unset; defaults are applied. One diagnostic per template with counts. |
| `PRM004` | Error | Core and Packages | Energy is non-finite or outside `-100..100 eV`. |
| `PRM005` | Error | Core and Packages | Frequency is non-positive or non-finite. |
| `PRM006` | Warning | Packages | No explicit value differs from its default; the template may be unparameterized. |
| `PRM007` | Reserved | — | Retired. Chiral partners share one `EnergyId`; independent A→B/B→A values are unrepresentable. The code is not registered and never emitted. |
| `PRM008` | Warning | Packages | A site-energy override, or an authored background entry from the supplied project, targets an unknown site/particle or a particle the site cannot occupy; the value is ignored. A background mismatch usually means the project and compiled model are not the same pair. |
| `PRM009` | Info | Packages | Template rebase report: N copied, M defaulted, K dropped, A ambiguous. |
| `PRM010` | Warning | Core | Duplicate template name (case-insensitive). References use IDs, so this is a label warning. |
| `PRM011` | Warning | Packages | A template contains a Metropolis transition rule set. MMC rules are not parameterizable; the set is ignored and the compiled default remains. |
| `PRM012` | Warning | Packages | A pair/group entry names a particle that cannot occupy the relevant reference site; the entry is ignored. |
| `PRM013` | Error | Core and Packages | A template or apply resource limit (set/entry count, string length, applied value bytes) is exceeded. |
| `PRM014` | Error | Core and Packages | A stable ID is malformed, a value key is duplicated, or a template ID is empty. |
| `PRM015` | Warning | Packages | Suspiciously uniform non-default values (at least 20 explicit pair/group values and at least 90 % identical), a possible bulk-fill/paste error. |
| `PRM016` | Warning | Packages | An attempt frequency is outside the plausible `1e-3..1e18 Hz` range but still positive and finite. |

- `PRM004`, `PRM005`, `PRM010`, `PRM013`, and `PRM014` are structural and run in Core `ProjectValidator`, so
  load, save, and every `ProjectEditor` mutation see them. `PRM001`, `PRM002`, `PRM003`, `PRM006`, `PRM008`,
  `PRM009`, `PRM011`, `PRM012`, `PRM015`, and `PRM016` need the compiled model or the rebase result and run in
  `Mocassin.Packages`.
- `ParameterizationDiagnosticCatalog` in `Mocassin.Core.Diagnostics` registers every non-retired `PRM`
  descriptor and is composed into `CoreDiagnosticCatalog`, so `DiagnosticCatalog.Default` already includes the
  family. The docs-drift test requires every registered code in `docs/reference/diagnostics.md`; Step 09 adds
  the anchors and rows. `PRM007` is documented as retired but is not a catalog row; the implementation does
  not declare a `PRM007` constant, so the Core catalog test's "every declared code is registered" rule holds.
- Paths use `ModelPath("parameterization", template.Id.Value, property)`. Set-level properties are
  `stablePairSets`, `unstablePairSets`, `groupSets`, `siteEnergyOverrides`, `transitionRuleSets`, `name`, or
  `modelSignature`. Entry-level properties identify the stable key, for example
  `stablePairSets/{energyId}/entries/{centerParticleId}/{partnerParticleId}/energyEv`,
  `groupSets/{clusterEnergyId}/entries/{occupation}/energyEv`,
  `transitionRuleSets/{kind}/{transitionId}/{ruleGeometryId}/frequencyHz`, or
  `siteEnergyOverrides/{siteId}/{particleId}/energyEv`. Messages carry the counts and IDs needed to act.
- `PRM001` carries the `ParameterizationDiff` in the result; the diagnostic message summarizes added/removed/
  matched counts and names the first few IDs. `PRM009` carries the rebase counts.

#### 7.2 Build-stage integration and CLI ownership

- **No Step 09 `IValidationStage`.** `ValidationContext` contains only `Project`, `Options`, `Catalog`, and
  previous stage results; it has no `CompiledModel`. A Stage = Build implementation in Step 09 would either
  recompile (duplicating Step 12 orchestration and wasting a full compilation on every validation) or be
  unable to check `PRM001`–`PRM003`/`PRM008`/`PRM011`/`PRM012`. Step 09 therefore exposes
  `IParameterizationService.Validate(project, model, template, ...)` and Step 12's build pipeline calls it
  immediately after `IModelCompiler.Compile`, before `Apply`. Step 12 owns the Build-stage adapter, the compile
  error mapping, and the build event stream; Step 09 does not add a second validation pipeline or a stage that
  silently compiles.
- Structural `PRM` checks still run in `mocassin validate` because they live in Core `ProjectValidator`.
  `mocassin validate` cannot detect a stale model signature (it does not compile); `mocassin build` and the GUI
  build do, through Step 12's call. This limitation is explicit and tested.
- **No `mocassin parameterize` CLI.** Step 12 already owns `mocassin compile`, `mocassin build
  --parameterization <id>`, progress, cancellation, exit codes, and JSON output. A separate template-creation
  command would duplicate argument parsing, project loading, and build selection, and would have to change
  again when Step 12 lands. Template creation/editing is a library/API workflow used by Step 12 and Step 26.
  Step 09's acceptance is library-level plus project round-trip; it does not add CLI parsing.
- The `IParameterizationService` is registered for DI with the other Packages services in Step 12/26; it is
  stateless and safe to reuse concurrently (it only reads immutable inputs and allocates per call).

### 8. Diff and rebase

```csharp
public enum ParameterizationRebaseStrategy
{
    KeepMatching = 0,
    NearestGeometry = 1,
    Reset = 2,
}

public sealed record ParameterizationRebaseOptions
{
    public ParameterizationRebaseStrategy Strategy { get; init; } = ParameterizationRebaseStrategy.KeepMatching;
    public int NearestDistanceToleranceFm { get; init; } = 100; // 0.1 Å; the template stores the 1 fm bucket
    public static ParameterizationRebaseOptions Default { get; } = new();
}

public enum ParameterizationValueKind
{
    StablePair = 0,
    UnstablePair = 1,
    Group = 2,
    RuleFrequency = 3,
    SiteEnergyOverride = 4,
}

public sealed record ParameterizationDiffEntry(
    ParameterizationValueKind Kind,
    string Key,
    string? SecondaryKey,
    double? TemplateValue,
    string? Descriptor);

public sealed record ParameterizationGeometryChange(
    ParameterizationValueKind Kind,
    string RemovedKey,
    string AddedKey,
    string Reason);

public sealed record ParameterizationDiff(
    string TemplateModelSignature,
    string ModelSignature,
    ImmutableArray<ParameterizationDiffEntry> Added,
    ImmutableArray<ParameterizationDiffEntry> Removed,
    ImmutableArray<ParameterizationDiffEntry> Matched,
    ImmutableArray<ParameterizationGeometryChange> GeometryChanges);

public sealed record ParameterizationRebaseReport(
    ParameterizationRebaseStrategy Strategy,
    ParameterizationDiff Diff,
    int CopiedValues,
    int DefaultedValues,
    int DroppedValues,
    int AmbiguousMatches,
    int DescriptorMatches);

public sealed record ParameterizationRebaseResult(
    ParameterizationTemplate Template,
    ParameterizationRebaseReport Report,
    ImmutableArray<Diagnostic> Diagnostics);
```

In a `ParameterizationDiffEntry`, `Key` is the set/transition identity and `SecondaryKey` is the entry identity
(the particle pair, occupation tuple, or rule geometry ID). For a site override, `Key` is the site ID and
`SecondaryKey` is the particle ID. `Descriptor` is a short human-readable geometry/occupation summary when the
current model can supply one.

`ParameterizationDiff` compares the template to the current compiled model by stable identity:

- `Matched` — the template key exists in the model (and its descriptor agrees); values are unchanged.
- `Added` — a compiled slot has no template entry; applying the current template would default it.
- `Removed` — a template entry has no compiled slot; applying would ignore/drop it.
- `GeometryChanges` — a removed key and an added key are reported as a related pair for **pair and group**
  values when they share a conservative logical descriptor (pair: same unordered center/partner site IDs;
  group: same cluster ID and center site ID). Rules have no stored state/direction descriptor, so they are
  reported as removed/added inside their transition set. This is display information; it does **not**
  authorize copying values.

Strategies:

- **`KeepMatching` (default).** Copy values only for exact stable-ID matches. New slots default; removed
  entries are dropped; site overrides are kept only when `(siteId, particleId)` still exists in the model.
  This is the safe default for a model signature change caused by catalog/contract versions where IDs survive.
- **`NearestGeometry` (opt-in).** Same as `KeepMatching`, then for removed **pair** and **group** entries look
  for a unique added slot with the same logical descriptor and acceptable distance:
  - pair: same unordered `{centerSiteId, partnerSiteId}` and `IsChiral`, and
    `|sourceBucket - targetBucket| <= NearestDistanceToleranceFm` (the template stores `distanceBucketFm`, so
    no raw distance is needed); if more than one candidate matches, copy nothing and report
    `AmbiguousMatches`;
  - group: same `ClusterId` and center site ID, and the same canonical occupation tuple; unique only;
  - rules and site overrides: exact-ID matches only; no nearest copying.
  Every copied value appears in `DescriptorMatches` and in the report. A changed chiral link never matches
  across the break: the old shared ID is dropped and both new slots default.
- **`Reset`.** Return a template with the same ID and name, the new model signature, the new set headers, and
  no values/overrides. This resets values without breaking build-template references. Creating a genuinely
  new template uses `ParameterizationFactory.Create` and a new `ParameterizationId`.

Guarantees:

- Rebase never mutates the input template or model; it returns a new immutable value.
- Values are never silently reused across changed geometry. `KeepMatching` copies only exact IDs;
  `NearestGeometry` copies only unique, reported descriptor matches; `Reset` copies nothing.
- Rebase preserves `Id` and `CreatedUtc`, updates `ModelSignature`, and emits `PRM009` with the report.
- Report counts: `CopiedValues` are explicit entries copied by exact ID or nearest descriptor;
  `DescriptorMatches` is the nearest subset; `DefaultedValues` are compiled slots left without an entry (they
  use defaults); `DroppedValues` are template entries with no target that were not copied; `AmbiguousMatches`
  are nearest groups with more than one candidate (nothing copied).
- The GUI (Step 26) shows the report before the user saves the rebased template; the service does not save it.
- `Diff` and `Rebase` are pure and can be called on a stale template without applying it.

### 9. Completeness and quality

`ParameterizationSummary` is a build-time record in `Mocassin.Packages.Parameterizations` with these metrics:

- stable/unstable pair table counts, admissible pair slot count, explicitly set pair value count;
- group table count, group slot count (sum of canonical occupation counts), set group value count;
- kinetic transition rule-set count, parameterizable kinetic rule slot count, set rule frequency count;
- site-energy slot count and override count;
- chiral pair table count;
- required slot count (pair + group + kinetic rule), set value count, unset value count, non-default value
  count;
- effective energy min/max, effective frequency min/median/max;
- estimated applied-value bytes;
- identical-value share and non-parameterizable (MMC) rule-set count;
- `AllValuesDefault`.

Warnings and thresholds:

| Signal | Diagnostic | Threshold |
| --- | --- | --- |
| Required values unset | `PRM003` Warning | `UnsetValueCount > 0`; one summary diagnostic |
| No explicit non-default value | `PRM006` Warning | `AllValuesDefault` |
| Frequency outside plausible band | `PRM016` Warning | `< 1e-3` or `> 1e18 Hz`, but `> 0` |
| Metropolis rule set present | `PRM011` Warning | any Metropolis `TransitionRuleSet`; ignored, MMC default remains |
| Suspiciously uniform values | `PRM015` Warning | `>= 20` explicit pair/group values and `>= 90 %` identical non-default value |
| Invalid energy/frequency | `PRM004`/`PRM005` Error | range/positivity |
| Resource limit | `PRM013` Error | count/string/byte limit |

Required pair slots are `|centerMask| × |partnerMask|` for an achiral or unstable–stable table. For a chiral
table they are the distinct unordered particle pairs `{a, b}` with `a` in the centre mask and `b` in the
partner mask, deduplicated when the masks overlap or `a == b`; this mirrors the symmetric table. Group slots
are the canonical occupation count, and kinetic rule slots are the kinetic rule count.

The editor can filter "unset", "non-zero", "chiral", "site overrides", and "changed since the last model
signature". These are UI features (Step 26) over the summary/diff data; Step 09 supplies the data.

### 10. Determinism, security, and performance

#### 10.1 Determinism

- Canonical JSON: fixed field order, sorted sets/entries, invariant formatting, `\n`, UTF-8 no BOM, `-0.0` →
  `0.0`. Tests run under a comma-decimal culture and assert byte-identical output.
- Signature: fixed-order MessagePack arrays, fixed-width integers, float64, 12-decimal round-half-away-from-zero
  quantization, normalized signed zero, domain separation, no compression/timestamps/IDs. Same inputs →
  byte-identical projection and digest on every supported platform.
- Apply is single-threaded over immutable tables; no parallel floating-point reduction or hash-order iteration
  affects values. Template dictionaries use `StringComparer.Ordinal` and GUID ordering, never
  `string.GetHashCode()`.
- The effective values are quantized before storage, so the persisted payload and the signature cannot drift.
- `CreatedUtc` uses `TimeProvider` and is excluded from the signature; tests inject a fixed clock.

#### 10.2 Untrusted-template bounds

The project file is already bounded by `ProjectStoreOptions.MaxDocumentBytes` (64 MiB). Step 09 adds
`ParameterizationLimits` checks before any large allocation:

| Limit | Value | Enforcement |
| --- | ---: | --- |
| Templates per project | 256 | `PRM013` |
| Pair sets per template | 100,000 | `PRM013` |
| Pair entries per set | 4,096 (63² = 3,969 fits) | `PRM013` |
| Group sets per template | 100,000 | `PRM013` |
| Group entries per set | 1,000,000 | `PRM013` |
| Site overrides per template | 1,000,000 | `PRM013` |
| Transition rule sets per template | 100,000 | `PRM013` |
| Rule entries per set | 1,000,000 | `PRM013` |
| Total explicit value entries per template | 2,000,000 | `PRM013` |
| Stable ID length | 64 lowercase hex characters | `PRM014` |
| Template name length | 200 characters | `PRM013` |
| Applied value bytes | 2 GiB | `PRM013` before allocation; peak is the compiled tables plus this copy (up to ~4 GiB at the Step 08 hard limit), so Step 12 treats it as a hard build limit |

- IDs are opaque strings; they are never interpreted as paths, file names, globs, or SQL. No template content
  selects a file or executes code.
- Counts are combined with 64-bit arithmetic and checked against `int.MaxValue` and the configured byte cap
  before allocating. `positionCount × particleCount` and `nParticles² × tableCount` are overflow-checked.
- NaN/Inf are rejected by the typed units and the strict JSON reader; the validator still checks defensively.
  Duplicate keys, unknown properties, and over-limit strings fail closed.
- A focused property/fuzz test feeds arbitrary structurally valid template JSON through load and validation
  and asserts no uncaught exception, no unbounded allocation, and a diagnostic for every rejection. Full
  parser fuzzing remains a Step 29 release gate.

#### 10.3 Performance targets

On the benchmark laptop class (8 physical cores, 32 GB RAM, .NET 10 Release):

| Scenario | Target |
| --- | --- |
| Create ceria template + summary (from `samples/ceria/ceria.mocprj`) | `< 20 ms` |
| Apply + signature, ceria (energy model) | `< 50 ms` |
| Apply + signature, ceria KMC rules (`samples/simulations/ceria-kmc.mocprj`) | `< 50 ms` |
| Canonical template JSON round-trip, ceria | `< 100 ms` |
| Apply + signature, synthetic 1,000-position / 4-particle / 100 pair-table / 10,000-rule model | `< 2 s`, `< 256 MiB` additional managed memory |
| Parameterization signature throughput | `>= 100 MB/s` single-threaded, streaming (no full projection copy) |

- `Mocassin.Benchmarks/Parameterization/ParameterizationBenchmarks.cs` uses BenchmarkDotNet for create,
  apply, signature, and round-trip; it is dispatched manually via `benchmark.yml` under the CI cost policy.
- A PR-medium guard test mirrors the Step 08 performance test for ceria. Large synthetic cases are
  PR-medium/nightly, not PR-fast.
- The signature writer streams values directly into the hash; it does not allocate a second copy of every
  table. Apply allocates the applied arrays once and reuses them for signing and the payload.

### 11. Testing

Test tiers follow `03-quality-strategy.md`. Tests use real projects, the real `ModelCompiler`, real
`ProjectStore` files, and real SQLite only where the step touches it (it does not). There are no mocks of the
compiler, compiler tables, project store, or domain objects.

#### 11.1 Acceptance mapping

| Acceptance criterion | Concrete test(s) |
| --- | --- |
| Creating a template from the ceria compiled model produces exactly the expected sets/rules | Compile `samples/ceria/ceria.mocprj` and `samples/simulations/ceria-kmc.mocprj`; assert `Create` headers equal the compiled pair/cluster tables and kinetic transition collections by ID, and `Describe` kinetic rule counts equal the compiled kinetic `CompiledJumpRule` count. No magic counts: identity/count equality against the compiled model. |
| Applying the template to the same model succeeds and yields the same compiled values | Apply an empty template to ceria and ceria-kmc; assert pair/group values are `0`, frequencies are the compiled default, site background equals the authored `SiteEnergies`, and the golden signature matches. Then apply a fully populated checked-in template and compare every table/rule/site value with the fixture arrays. |
| A changed model produces a signature mismatch with a useful diff | Compile ceria, edit a site coordinate (or a cell parameter) through `ProjectEditor`, recompile, apply the old template; assert `PRM001`, `Model == null`, and the diff lists the changed/removed/added IDs. |
| Rebase copies unchanged values, defaults new ones, and reports removed ones | `KeepMatching` test with an exact-ID subset; assert copied/defaulted/dropped counts, `PRM009`, and that every copied value has an exact source ID. `NearestGeometry` test with a unique descriptor match and an ambiguous pair; assert the ambiguous case copies nothing. `Reset` test asserts no values and the new signature. |
| Chiral partners share one `EnergyId` and one value; independent A→B/B→A values are unrepresentable | Compile a real chiral model (P4mm/P1 fixture with a stable environment; the Symmetry P1 chiral fixture is the oracle), assert one pair table for both directions, one template entry, a symmetric table (`table[X][Y] == table[Y][X]`), and no public API/JSON field for a direction-specific value. A mirrored duplicate with a different value is rejected. Break the chiral link and assert the value is dropped, not reused. |
| Site energy overrides apply and can be reset | Ceria authored site energies; set an override, assert every owned position changes; remove it, assert the authored value returns; invalid site/particle produces `PRM008` and leaves the background unchanged. |
| Missing/zero values produce warnings, not silent success | Empty/partial templates produce `PRM003`/`PRM006`; zero explicit values do not trigger `PRM006` if they are intentional; `WarningsAsErrors` promotion is Step 12's policy test. |
| Template JSON is canonical and round-trips | Project JSON round-trip, shuffled authoring order, `-0.0`, comma-decimal culture, unknown-field rejection, additive missing arrays, and canonical document-hash stability. |
| Documentation written | `./tools/verify.sh docs --require` plus the docs-drift diagnostic test. |

#### 11.2 Test inventory

- **Core/contracts:** `ParameterizationTemplateTests` (normalization, ordering, `-0.0`, default arrays,
  canonical JSON, limits), `ParameterizationTemplateJsonTests` (round-trip, unknown fields, additive fields),
  `ParameterizationDiagnosticTests` (every `PRM` constant registered exactly once, documented, severity/path).
- **Contracts:** `ParameterizationSignatureTests` (golden vectors, quantization, domain separation, values-only,
  same effective values ⇒ same signature, culture independence), `ParameterizationProjectionFixtureTests`
  (C# projection bytes equal `tests/contracts/projections/parameterization-*.bin`).
- **Packages:** `ParameterizationFactoryTests`, `ParameterizationSummaryTests`, `ParameterizationApplyTests`
  (defaults, overrides, chiral, forbidden particles, quantization, signature), `ParameterizationValidationTests`
  (every compiled-bound `PRM` code with a real model), `ParameterizationDiffTests`,
  `ParameterizationRebaseTests` (all strategies, report, no silent reuse), `ParameterizationSiteEnergyTests`,
  `ParameterizationPerformanceTests`, and a property test for sparse/dense equivalence.
- **Property tests:** sparse template apply equals a dense template with explicit defaults; rebase copies only
  exact/reported descriptors; apply never mutates; signature is invariant under template entry order and
  culture; random valid templates never throw and never exceed configured allocations.
- **Golden fixtures:**
  - `tests/contracts/projections/parameterization-ceria.bin/.json`
  - `tests/contracts/projections/parameterization-ceria-kmc.bin/.json`
  - `tests/contracts/projections/parameterization-barium-zirconate.bin/.json`
  - `samples/parameterizations/ceria-parameters.mocprj` and
    `samples/parameterizations/barium-zirconate-parameters.mocprj` (project fixtures containing a populated
    `parameterizations` array; no standalone template file is introduced).
  - `tests/contracts/signature-vectors.json` entries for each parameterization vector.
- **Freshness:** golden projections are generated by the real `ParameterizationSignature` over the real
  compiled model and checked by `tools/contracts/check_parameterization_projection.py`; the generated
  descriptor is checked by `tools/contracts/generate.py --check`. A changed model, values, or projection
  without regenerated fixtures fails `verify.sh contracts`.
- **Cross-language:** the Python checker re-encodes the `.bin` fixture from the generated descriptor and
  compares the signature; Rust reproduction lands with the Step 13 reader and is recorded as a Step 13
  acceptance item, not claimed here.
- **Performance:** ceria guard test (PR-medium) and BenchmarkDotNet parameterization benchmarks (manual
  dispatch). No statistical/parity tests are needed because Step 09 changes no physics; Step 15 pins the
  site-energy/barrier consumption.
- **No mocks:** the only doubles are `TimeProvider` (fixed clock) and, if needed, a deliberately corrupt
  `ParameterizationValues` for a contract-failure test. Domain/compiler/package objects are real.

### 12. Feature list and per-feature scope

| # | Feature | Scope | Depends on | Tests | Docs |
| ---: | --- | --- | --- | --- | --- |
| 1 | `template-model-and-persistence` | Extend `ParameterizationTemplate`; add pair/group/rule/site value records, `ParameterizationLimits`, normalization/canonicalization, `ProjectJson` round-trip, `ProjectEditor`/`ParameterizationTemplateEditor` operations, structural `PRM004/005/010/013/014` in Core. | 08 | Core JSON/canonical/limits/editor/validation tests; new template fixtures. | Data contract §2.3, project-format handoff note, design decisions. |
| 2 | `parameterization-signature` | `ParameterizationValues` and `ParameterizationSignatureV1` in Contracts; public canonical writer; finalize `signatures.parameterization`; generated descriptor; golden `.bin`/`.json` vectors; Python checker; `verify.sh contracts` wiring. | 1 | Signature vectors, projection fixtures, culture/quantization tests. | Data contract §1.3/§3.3, compilation-pipeline signature section, diagnostics note. |
| 3 | `factory-and-summary` | `ParameterizationFactory.Create`, `Describe`, set headers, required-slot counts, chiral/quality metrics, `ParameterizationSummary`, sparse/dense equivalence property. | 1, 2 | Factory/summary tests on ceria, ceria-kmc, BaZrO₃. | User-guide parameterization draft (creation/summary). |
| 4 | `apply-and-site-energies` | `IParameterizationService`/`ParameterizationService.Apply`, defaults, quantization, site-energy background, overrides, `ParameterizedModel`, signature computation. | 2, 3 | Apply/default/override/golden tests. | User-guide parameterization (apply/reset), design decisions (site background). |
| 5 | `validation-and-diagnostics` | Full `PRM` catalog/descriptors, compiled-bound `Validate`, `PRM001/002/003/006/008/011/012/015/016`, paths, docs anchors, Step 12 integration contract. | 4 | Per-code emission tests; catalog/docs drift; stale-template test. | Diagnostics reference rows, troubleshooting stale/incomplete template, Step 12 note. |
| 6 | `diff-and-rebase` | `ParameterizationDiff`, `Rebase` strategies/report, chiral/link-change handling, ambiguity rules, `PRM009`; property tests. | 4 | Diff/rebase/property/no-silent-reuse tests. | User-guide rebase, troubleshooting. |
| 7 | `golden-templates-docs-and-closeout` | Checked-in ceria/BaZrO₃ templates and golden parameterized values/signatures; benchmarks; user-guide/reference/development/troubleshooting pages; nav; parity matrix; step status/closeout feature log. | 1–6 | Full `verify.sh all` at closeout; docs gate; benchmark run. | All named pages; parity; step page. |

- Estimated effort: feature 1 3–4 days, 2 3 days, 3 2 days, 4 3–4 days, 5 2–3 days, 6 2–3 days, 7 2–3 days;
  roughly 3–4 person-weeks in total (design, tests, and docs included).
- Feature 1 is the contract foundation; no later feature starts until its JSON/canonical tests pass.
- Features 2 and 3 can overlap only after feature 1; feature 4 needs both. Features 5 and 6 can run in
  parallel after feature 4. Feature 7 is last and includes the documentation/closeout work.
- Each feature is independently squash-mergeable and auditable; no feature claims a later feature's output.

### 13. Documentation and parity

Published pages and content (written in features 3–7, not by this design change):

- `docs/user-guide/parameterization.md` (new) — create a template, sparse values/defaults, edit pair/group
  energies, shared chiral sets, rule frequencies, site-energy overrides and "reset to model", completeness,
  diff/rebase report, and the warning/error semantics.
- `docs/reference/project-format.md` — the extended `parameterizations` shape, stable IDs, canonical ordering,
  versioning, limits, and the `PRM` diagnostics link.
- `docs/reference/diagnostics.md` — `PRM001`–`PRM016` rows/anchors, `PRM007` retired note.
- `docs/development/compilation-pipeline.md` — parameterization stage, signature projection, values-only
  relationship to the model signature, and the Step 11/12 boundary.
- `docs/development/design-decisions.md` — template persistence/sparsity, site-energy background ownership,
  no `IValidationStage`/no CLI, and the values-only signature rationale.
- `docs/development/architecture.md` — Core/Contracts/Packages ownership and the `ParameterizedModel`/
  `ParameterizationValues` boundary.
- `docs/troubleshooting/errors.md` — stale template (`PRM001`), incomplete values, frequency range, rebase.
- `docs/user-guide/energy.md` — cross-link the template override/reset behavior (already states the authored
  value remains the default).
- `docs/reference/energy.md` — note that the template stores one value per `EnergyId` and the background is
  Step 09's.
- `zensical.toml` — add "Parameterization" under the user guide (configuration change, feature 7).
- No planning page is published; `docs/planning/` stays excluded.

Parity matrix rows are updated in `appendix-b-parity-matrix.md` §6 (see the change in this design branch):
template persistence/signature/completeness/defaulting/apply, chiral shared values, site overrides, rebase,
and the CLI note that Step 12 replaces a hypothetical `mocassin parameterize`.

### 14. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Stable IDs change unexpectedly | Derive from compiled content hashes, never list order; assert IDs equal Step 08 `EnergyId`/`ClusterEnergyId`/`RuleGeometryId` in tests; treat a model-signature change as invalidating before ID matching. |
| Users lose values on model edits | Sparse templates + explicit `Diff`/`Rebase` report; `KeepMatching` only exact IDs; `NearestGeometry` opt-in, unique and reported; no automatic rebase. |
| Chiral divergence | One `EnergyId` and one value per chiral pair; no direction-specific field; `PRM007` retired; link-change tests. |
| Template proliferation | Stable `ParameterizationId`, case-insensitive names (`PRM010`), model signature, rebase preserving identity, and a build template that references one ID. |
| Huge templates or malicious input | `ParameterizationLimits`, 64 MiB project cap, 64-bit count checks, 2 GiB applied-value cap, no path/code interpretation, fuzz/property tests. |
| Site background silently wrong | Resolve only validated authored `SiteEnergies`, map by `CellSiteId`/`ParticleId`, test every owned P1 position, and pin Step 15's per-position consumption. |
| Signature drift between .NET and readers | Values-only fixed-order projection, quantize before storage, domain separation, golden `.bin` vectors, Python re-encoding, and Step 13 Rust reproduction. |
| Two validation homes | Structural checks in Core, compiled-bound checks in the service; one `PRM` catalog and one docs-drift test; no second compile in validation. |
| Scope creep into CLI/build/GUI | Library-only Step 09; Step 12 owns `compile`/`build`; Step 26 owns the editor/import/export; features stop at the named APIs. |

## Implementation tasks

- [ ] Extend the Core template record and add pair/group/rule/site value records, limits, normalization, and
      canonical JSON.
- [ ] Add `ProjectEditor` template operations and structural `PRM` validation.
- [ ] Add `ParameterizationValues`, `ParameterizationSignatureV1`, the public canonical writer reuse, manifest
      finalization, generated descriptor, and golden projection vectors.
- [ ] Implement `ParameterizationFactory.Create` and `Describe`, set headers, and `ParameterizationSummary`.
- [ ] Implement `ParameterizationService.Apply`, defaults, quantization, site-energy background, overrides, and
      `ParameterizedModel`.
- [ ] Implement compiled-bound `Validate` and every `PRM` emission/path.
- [ ] Implement `ParameterizationDiff` and the `KeepMatching`/`NearestGeometry`/`Reset` rebase report.
- [ ] Add ceria and barium-zirconate template fixtures, golden parameterized values/signatures, and benchmarks.
- [ ] Update docs, diagnostics, parity, and the feature log; run the closeout gates.

## Acceptance criteria

- [ ] Creating a template from the ceria compiled model produces exactly the expected sets/rules.
- [ ] Applying the template to the same model succeeds and yields the same compiled values.
- [ ] A changed model produces a signature mismatch with a useful diff.
- [ ] Rebase copies unchanged values, defaults new ones, and reports removed ones without silent reuse.
- [ ] Chiral partners share one `EnergyId` and one value; independent A→B/B→A values are unrepresentable.
- [ ] Site energy overrides apply and can be reset to the authored model value.
- [ ] Missing/zero values produce warnings, not silent success.
- [ ] Template JSON is canonical and round-trips, including culture, `-0.0`, and additive missing arrays.
- [ ] The parameterization signature is deterministic, finalized in the manifest, and reproduced by the
      checked-in projection vector.
- [ ] Untrusted-template limits reject oversized counts/strings/values with `PRM` errors before allocation.
- [ ] Documentation written.

## Documentation

At this point, document parameterization:

- `docs/user-guide/parameterization.md` — template creation, sparse values/defaults, energies, frequencies,
  shared chiral sets, site overrides/reset, diff/rebase.
- `docs/reference/project-format.md` — template fields, stable IDs, canonical order, versioning, limits.
- `docs/reference/diagnostics.md` — `PRM` rows and the retired `PRM007` note.
- `docs/development/compilation-pipeline.md` — signature/rebase flow, defaults, completeness, Step 11/12
  boundary.
- `docs/development/design-decisions.md` — persistence/sparsity, site background, validation/CLI boundaries,
  values-only signature.
- `docs/development/architecture.md` — module ownership and `ParameterizedModel`/`ParameterizationValues`.
- `docs/reference/energy.md` and `docs/user-guide/energy.md` — one value per `EnergyId`, the authored
  background and template override/reset behavior.
- `docs/troubleshooting/errors.md` — stale/incompatible templates, missing values, frequency range, rebase
  recovery.
- `zensical.toml` — add "Parameterization" under the user guide; the page is published only through the
  normal docs build and is not referenced by planning pages as a live link before it exists.

## Tests

| Test | Assertion |
| --- | --- |
| Factory | Headers/IDs and summary counts match the compiled model |
| Apply | Values map to the correct tables; defaults and site background are exact |
| Signature | Mismatch rejected with `PRM001`; golden projection/signature matches |
| Rebase | Keep/nearest/reset strategies and report; no silent reuse |
| Chiral | Shared `EnergyId`; one value; no independent-value path |
| Site overrides | Applied/reset/invalid target |
| Completeness | `PRM003`/`PRM006`/`PRM015`/`PRM016` thresholds |
| Round-trip | Canonical JSON stable; culture/`-0.0`/unknown-field behavior |
| Limits | Oversized counts/strings/values fail with `PRM013` before allocation |
| Performance | Ceria create/apply targets; synthetic medium case |

## Done when

A parameterization can be created from any compiled model, edited, validated, applied, rebased with a report,
serialized without losing or silently changing values, and handed to Step 10/11 with a deterministic
parameterization signature that Step 13 can reproduce.
