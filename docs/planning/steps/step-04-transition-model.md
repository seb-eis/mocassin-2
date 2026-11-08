# Step 04 — Transition Model

**Phase:** B. Model definition  
**Depends on:** 03
**Delivers:** State changes, state-change groups, abstract chains, and KMC/MMC transitions bound to reference
geometry, with validation of transition semantics.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** [Re-review required at `43bad05`](../reviews/step-04.md)
- **Last updated:** 2026-11-08

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Let a user describe every supported transport mechanism abstractly, bind it to symmetry-reduced reference
geometry, and validate that the definition is physically meaningful before rule generation.

## Non-goals

- No compiled transition rules, rule deduplication, inverse/dependent rule pairing, or conservation filtering;
  those belong to Step 08. Step 04 validates only the authored definition.
- No simulation ownership, transition assignment, attempt frequencies, or run settings; Step 06 owns those.
- No interaction cutoffs, environments, or energy values; Step 05 owns those.
- No symmetry expansion of the full supercell or package encoding; Step 02 and Step 08 own those.
- No transition preview persistence. `TransitionPreview` is derived data and is never written to the project.
- No GUI implementation; Step 24 consumes the model, query, and preview contracts defined here.
- No legacy `.mocprj` transition import or WPF-compatible connector strings beyond the explicit `D`/`S` grammar.

## Original reference

- `IStateExchangePair` — donor and acceptor particles; exactly one vacancy for a vacancy pair; acceptor is void
  for an unstable-position pair.
- `IStateExchangeGroup` — a set of pairs with the same role in a chain; may not mix physical exchange and
  charge-state exchange.
- `IAbstractTransition` — a chain of state-change groups with `StateCount` and `StateCount − 1` connectors of
  type `Dynamic` or `Static`; `IsMetropolis` when `StateCount == 2`; `IsAssociation` flag.
- `IKineticTransition` — an abstract chain plus a 3D reference path; geometry count equals state count;
  endpoints stable, every unstable path position unstable, no ring, no consecutive unstable positions.
- `IMetropolisTransition` — an abstract chain (2-site) plus two reference cell sites; selecting the same site
  twice means an intra-sublattice exchange.
- Original connector patterns:
  - `D` — 2-site Metropolis;
  - `D-D+` — basic kinetic migration (3 sites);
  - `D+ SS D+` — normal vehicle (5 sites);
  - `DD (S DD)+` — split vehicle (6 sites).
- Original supported mechanisms: 2-site MMC, 3-site migration, 5-site interstitialcy, 7-site interstitialcy,
  5-site 2-species vehicle, 6-site 2-species vehicle, charge transfer/polaron hop.
- Original limits: 100 transitions, 2–8 positions per chain.
- Original defects: empty-list indexing in the normal-vehicle validation, disabled self-exchange check,
  ambiguous end-state selection, conservation enforced only during rule generation.

## Design

### 1. Model composition and persistence

`ModelDefinition` gains the flat transition sections alongside the Step 03 particle and structure sections:

```csharp
public sealed record ModelDefinition
{
    ImmutableArray<Particle> Particles { get; init; }
    ImmutableArray<ParticleSet> ParticleSets { get; init; }
    StructureModel Structure { get; init; }

    ImmutableArray<StateChange> StateChanges { get; init; }
    ImmutableArray<StateChangeGroup> StateChangeGroups { get; init; }
    ImmutableArray<TransitionChain> Chains { get; init; }
    ImmutableArray<KineticTransition> KineticTransitions { get; init; }
    ImmutableArray<MetropolisTransition> MetropolisTransitions { get; init; }
}
```

- The model stays flat on disk: `model.stateChanges`, `model.stateChangeGroups`, `model.chains`,
  `model.kineticTransitions`, and `model.metropolisTransitions`. There is no nested `model.transitions`
  object; the placeholder `transitions` object in the earlier contract sketch is replaced by these flat
  sections, exactly as Step 03 established for `particles`, `particleSets`, and `structure`.
- The parameterless `ModelDefinition` constructor initializes every new collection to empty, so
  `ModelDefinition.Empty` and a persisted `"model": {}` still load without transition data.
- `ModelDefinitionJsonConverter` reads and writes the new sections. Missing sections load as empty arrays;
  unknown or duplicate properties are still rejected.
- `ProjectJsonContext` is the nested internal context in `Project/ProjectJson.cs`; it is updated there. There is
  no standalone `ProjectJsonContext.cs` and none is added.
- `ProjectJson.CanonicalizeModel` is extended to rebuild and canonicalize all model sections. This is part of the
  contract, not an implementation detail: `ProjectJsonConverter.Write`, `ProjectStore.SaveAsync`, and
  `ProjectHasher.Compute` all consume the canonical model. Without this change the new sections would be
  dropped on save and transition edits would compare equal to the previous document hash.
- `ProjectJsonContext`, `ProjectValidator`, and the canonicalization/hash path are extended for the new
  sections. `ProjectSchema` needs no change while `schemaVersion` remains 1.
- `TransitionPattern`, `TransitionLinker`, `TransitionKind`, and `SiteStability` serialize as camelCase strings
  through the existing `CamelCaseEnumJsonConverter<T>` (`Model/ViewSettings.cs`). Unknown, numeric, or empty
  enum strings are rejected with `JsonException`; there is no numeric fallback.
- Semantic array order:
  - `StateChangeGroup.StateChangeIds` — group role order;
  - `TransitionChain.GroupIds` and `TransitionChain.Linkers` — position order;
  - `KineticTransition.ReferencePath` — position order;
  - simulation transition lists — authored order (Step 06).
  Every other collection is sorted by `id` in the canonical model.
- Canonical model sorting covers `StateChanges`, `StateChangeGroups`, `Chains`, `KineticTransitions`, and
  `MetropolisTransitions`; it never re-sorts a semantic array.
- The project `schemaVersion` remains 1 because the transition sections are additive and the project format is
  still pre-release. This follows the Step 03 additive-v1 precedent. ADR 0009's version-bump rule is not waived
  generally: the manifest `status`/`notes` are updated, the generated schema and fixtures are refreshed, and the
  first released format will start the normal version-bump discipline.
- `TransitionPreview` is derived data and is never persisted.

### 2. State changes

```csharp
public sealed record StateChange(
    StateChangeId Id,
    string Name,
    ParticleId DonorParticleId,
    ParticleId AcceptorParticleId);

public sealed record StateChangeGroup(
    StateChangeGroupId Id,
    string Name,
    ImmutableArray<StateChangeId> StateChangeIds);
```

- `[D, A]` means donor `D` is replaced by acceptor `A` at a position.
- The void particle is a valid acceptor. A void donor is allowed only for an explicit reverse vacancy pair; it
  is never valid at an unstable position.
- A group is a set of state changes with the same role. It may contain one or many pairs (e.g. all halides
  migrating into a vacancy).
- A group must not mix a physical exchange (donor and acceptor symbols differ) with a charge-state exchange
  (same symbol, different charge). This is an unconditional group rule, matching the original
  `StateExchangeGroupValidator`. Charge-transfer chains therefore use homogeneous charge-state groups; there is
  no "charge-transfer chain" exception at group level. A chain-level check (TRN006) reports a mixed group at the
  chain reference when the group itself is invalid.
- A state change with donor == acceptor is rejected (no-op).
- A state change where both donor and acceptor are void is rejected.
- `StateChange`, `StateChangeGroup`, `TransitionChain`, `KineticTransition`, and `MetropolisTransition` names are
  unique within their own collection, case-insensitive. Kinetic and Metropolis transition names may coincide
  because Step 06 treats them as different kinds.

### 3. Chains

```csharp
public enum TransitionPattern
{
    Metropolis2,
    Migration3,
    Interstitialcy5,
    Interstitialcy7,
    Vehicle5,
    Vehicle6,
    Custom
}

public enum TransitionLinker { Dynamic, Static }

public sealed record TransitionChain(
    TransitionChainId Id,
    string Name,
    TransitionPattern Pattern,
    ImmutableArray<StateChangeGroupId> GroupIds,
    ImmutableArray<TransitionLinker> Linkers,
    bool GeneratesReverse);
```

Pattern rules:

| Pattern | Positions | Linkers | Stability sequence | Reverse generated |
| --- | ---: | --- | --- | --- |
| `Metropolis2` | 2 | `D` | S, S | Yes (required) |
| `Migration3` | 3 | `D,D` | S, U, S | Yes (required) |
| `Interstitialcy5` | 5 | `D,D,D,D` | S, U, S, U, S | No |
| `Interstitialcy7` | 7 | `D,D,D,D,D,D` | S, U, S, U, S, U, S | No |
| `Vehicle5` | 5 | `D,S,S,D` | S, S, U, S, S | No |
| `Vehicle6` | 6 | `D,D,S,D,D` | S, U, S, S, U, S | No |
| `Custom` | 2–8 | validated `D/S` sequence | inferred from the resolved reference path | No; explicit reverse only |

(S = stable, U = unstable.)

- `Linkers.Length` is always `GroupIds.Length − 1`. A `Static` linker means "no exchange at this connector";
  it is a geometric offset to the next position, not a state change. Static connectors may appear only inside a
  vehicle-shaped sequence; they never appear at either end.
- `Custom` preserves the original regex-validated connector grammar without hard-coding only the six common
  names:
  - a 2-position custom chain is a Metropolis chain: `S,S` with one `D` linker, usable only by a
    `MetropolisTransition`; there is no reference path to infer;
  - a 3–8-position custom chain is a kinetic chain and must match either all-dynamic `D{2,7}` or the
    vehicle-shaped `D+ S+ D+` grammar, with at least one dynamic linker on each side of the static run;
  - a kinetic custom chain's stability sequence is inferred from the resolved reference path after site
    matching, then checked against the chain groups;
  - endpoints are stable, at least one position is unstable, and no two unstable positions are consecutive.
- `GeneratesReverse` is persisted and user-visible but constrained:
  - it must be `true` for `Metropolis2` and `Migration3`;
  - it must be `false` for every other pattern;
  - a mismatch is TRN024. Step 08 reads the flag but the validator guarantees it agrees with the pattern.
- Complex mechanisms (`Interstitialcy5`, `Interstitialcy7`, `Vehicle5`, `Vehicle6`, `Custom`) never generate an
  inverse automatically. The user may link an explicit reverse transition with `ReverseTransitionId`; TRN018 is
  emitted on each kinetic or metropolis transition whose chain is complex and whose reverse link is null. It is
  a transition-level warning, not a chain-level property. The reverse link is a separate transition, not an
  inverse rule generated inside the forward transition.
- Chain groups are ordered and semantically meaningful.
- A chain may reference a group more than once (e.g. the same `[A,V]` group at both ends).
- A group may not be used at both a stable and an unstable position of the same chain; the group's stability
  role is fixed by its pairs (TRN025).

### 4. Transition references

```csharp
public enum TransitionKind { Kinetic, Metropolis }

[JsonConverter(typeof(TransitionRefJsonConverter))]
public readonly record struct TransitionRef(TransitionKind Kind, Guid Id)
{
    public static TransitionRef From(KineticTransitionId id);
    public static TransitionRef From(MetropolisTransitionId id);
    public bool IsKinetic { get; }
}
```

- `TransitionRef` is the common in-memory reference for simulations and previews. It is defined in
  `Mocassin.Core.Model.Transitions`; Step 06 uses it for `Simulation.TransitionIds`.
- Project JSON keeps the existing simulation shape: `kineticTransitionIds` and `metropolisTransitionIds`.
  `TransitionRef` is an in-memory composition; it is not a new persisted project field.
- A `TransitionRef` serialized on its own (for example in a `TransitionPreview` diagnostic) uses
  `{ "kind": "kinetic", "id": "…" }`. `TransitionKind` uses the same camelCase enum rules as the other enums.
- `TransitionRef` never points across kinds. A Metropolis transition cannot reference a kinetic transition, and
  the reverse.

### 5. KMC transitions

```csharp
public sealed record KineticTransition(
    KineticTransitionId Id,
    string Name,
    TransitionChainId ChainId,
    ImmutableArray<Fractional3> ReferencePath,
    KineticTransitionId? ReverseTransitionId);

public sealed record MetropolisTransition(
    MetropolisTransitionId Id,
    string Name,
    TransitionChainId ChainId,
    CellSiteId SiteAId,
    CellSiteId SiteBId,
    MetropolisTransitionId? ReverseTransitionId);
```

- `ReferencePath` length equals the chain position count for kinetic transitions.
- The path is expressed in unit-cell fractional coordinates and may leave `[0,1)`. Persisted points are never
  wrapped; wrapping is used only for site matching and ring detection, as defined in section 7.
- The path's stability sequence must match the pattern's **Stability sequence** in the table above. In
  particular, vehicles are not simple `S,U,S` migrations: `Vehicle5` is `S,S,U,S,S` and `Vehicle6` is
  `S,U,S,S,U,S`.
- Endpoints must be stable. Every unstable position must map to an unstable reference site and use an unstable
  group; every stable position must map to a stable reference site and use a stable group. It is not true that
  every intermediate position must be unstable: `Vehicle5` has stable intermediates.
- The path must not contain a repeated position. Ring detection compares the periodic canonical forms of
  every pair of points, so an interior return or a path ending at a periodic image of an earlier point is
  rejected.
- Metropolis transitions use exactly two reference sites. `SiteA == SiteB` means an exchange within one
  sublattice and is valid. Both sites must exist and be stable.
- A transition name is unique within its kind.
- A transition may be referenced by multiple simulations.
- `ReverseTransitionId` is optional, same-kind, and symmetric: if A points to B, B must point to A. Self-links
  are rejected. The reverse link is a model-authoring relationship, not an automatically generated rule.

### 6. Symmetry extension

- The model stores one reference path/site pair; Step 08 generates all symmetry-equivalent paths and rules.
- The preview API (`TransitionPreview`) shows:
  - the transition reference and chain;
  - the reference path in fractional coordinates and the resolved reference-site IDs/stabilities;
  - the symmetry-expanded paths (count and endpoints); for MMC these are expanded site pairs;
  - the state-change matrices for the chain;
  - the number of expected rules before conservation filtering;
  - the requested `(A, B, C)` cell counts.
- Preview is advisory; the compiled model is authoritative.

### 7. Path resolution and stability

```csharp
public sealed record ResolvedPathPosition(
    CellSiteId SiteId,
    SiteStability Stability,
    Fractional3 CanonicalPosition);

public sealed record ResolvedTransitionPath(
    ImmutableArray<ResolvedPathPosition> Positions);
```

- `TransitionPathResolver` in `Mocassin.Core.Model.Transitions` resolves every `ReferencePath` point to the
  authored reference sites before stability or group checks run. The resolver is shared by the validator and
  the preview service.
- For matching, each finite coordinate is canonicalized with `Fractional3.TrimToUnitCell()`, which snaps
  components within `GeometryTolerances.FractionalTrimTolerance` of an integer and then maps into `[0, 1)`.
  The canonical form is compared component-wise to `CellSite.Fractional` with
  `GeometryTolerances.PathMatchTolerance = 1e-6` (fractional units), a new named tolerance. The persisted path
  point is not changed. A non-finite component is rejected before calling `TrimToUnitCell()`.
- Exactly one reference site must match each canonical point. Zero matches is TRN020 with the path index;
  more than one match is also TRN020 with the competing site IDs in `Related`. Structure overlap validation
  should make ambiguity impossible, but the check is explicit and defensive.
- Stability is taken from the matched `CellSite.Stability`. The chain's group at the same index must agree:
  stable site → stable group (acceptor not void); unstable site → unstable group (exactly one `[D, ∅]` pair,
  TRN026).
- Pattern stability: the resolved stability sequence of a built-in kinetic pattern must equal the table
  sequence (TRN028). This is stricter than endpoint/consecutive checks alone: an `Interstitialcy5` path of
  `S,S,U,S,S` is rejected even though it has stable endpoints and no consecutive unstable positions.
- Ring detection compares every pair of canonical resolved positions. Any repeated canonical position is
  TRN014, so `0→1→0→2` is rejected as well as a literal or periodic-image endpoint ring.
- Consecutive unstable positions are compared from the resolved path (TRN013). A kinetic chain with no unstable
  position is TRN022.
- MMC transitions do not use `ReferencePath`; their preview path is derived from `SiteAId` and `SiteBId` and
  both sites are validated directly.

### 8. Validation rules

| Code | Severity | Rule |
| --- | --- | --- |
| `TRN001` | Error | State change references unknown particle |
| `TRN002` | Error | Donor equals acceptor |
| `TRN003` | Error | Both donor and acceptor are void |
| `TRN004` | Warning | Acceptor is charged and donor is void (likely an implicit reverse pair) |
| `TRN005` | Error | Group is empty or references unknown state change |
| `TRN006` | Error | Group mixes physical and charge-state exchanges |
| `TRN007` | Error | Chain group count/linker count mismatch |
| `TRN008` | Error | Chain pattern has the wrong number of positions |
| `TRN009` | Error | Linker sequence invalid for the pattern or custom grammar |
| `TRN010` | Error | Kinetic path length ≠ chain position count |
| `TRN011` | Error | Kinetic path endpoint is not stable |
| `TRN012` | Error | Kinetic path position has the wrong group stability |
| `TRN013` | Error | Consecutive unstable positions |
| `TRN014` | Error | Path contains a repeated canonical position (ring, self-intersection, or periodic image) |
| `TRN015` | Error | Metropolis transition does not use `Metropolis2` or a 2-position `Custom` chain |
| `TRN016` | Error | Metropolis site reference is unknown, unstable, or invalid |
| `TRN017` | Error | Duplicate name within a named collection |
| `TRN018` | Warning | Complex transition without an explicit reverse link |
| `TRN019` | Error | More than 100 transitions in total (`kinetic + metropolis`) |
| `TRN020` | Error | Path point does not match exactly one reference site, or is non-finite |
| `TRN021` | Error | Chain references an unknown state-change group |
| `TRN022` | Error | Kinetic chain has no unstable position |
| `TRN023` | Error | Kinetic transition uses `Metropolis2` or a 2-position `Custom` chain |
| `TRN024` | Error | `GeneratesReverse` disagrees with the pattern |
| `TRN025` | Error | Group is used at both stable and unstable positions |
| `TRN026` | Error | An unstable position's group contains more than one state change |
| `TRN027` | Error | Reverse link is unknown, cross-kind, asymmetric, or self-referential |
| `TRN028` | Error | Resolved path stability sequence does not match the built-in pattern sequence |

- `TRN001`–`TRN027` are owned by `Mocassin.Core` and emitted by `TransitionModelValidator` and
  `TransitionPathResolver`. Each has a stable code, a structured `ModelPath`, an actionable hint, and a help
  anchor documented in `docs/reference/diagnostics.md`.
- All definition-level inconsistencies are Errors; only `TRN004` and `TRN018` are advisory Warnings. This is an
  intentional severity change from the original validator, which emitted warnings for many unbuildable models.
- Conservation of mass and charge is checked during rule generation (Step 08), where the full state triplet is
  known. Step 04 only checks definition-level consistency.
- Interaction-range validation ("the path leaves the relevant environment cutoff") is not a Step 04 rule
  because the cutoffs are Step 05 data and Step 05 depends on Step 04. It is deferred to the Step 07
  cross-domain pipeline as `ENM018` (Warning); see the Step 05 and Step 07 pages.
- The validator reports the exact position index, group index, path index, and related object IDs.

### 9. Editing operations

- Add/update/remove state change, group, chain, kinetic transition, metropolis transition.
- Removing an object that is referenced by another object produces a diagnostic and a list of dependents;
  the editor offers "remove dependents" as an explicit operation.
- Duplicating a chain/transition creates new IDs, keeps references consistent, and does not copy reverse links
  by accident; the duplicate starts with no reverse link until the user links it.
- "Make group from selected state changes" is a UI convenience backed by a single editor operation.
- Transition editing lives in partial `ProjectEditor` aspect files under `Editing/` so the existing 506-line
  `ProjectEditor.cs` does not approach the 800-line limit.

### 10. Public API

```csharp
// Mocassin.Core
public interface ITransitionQuery
{
    ImmutableArray<StateChange> StateChanges { get; }
    ImmutableArray<StateChangeGroup> StateChangeGroups { get; }
    ImmutableArray<TransitionChain> Chains { get; }
    ImmutableArray<KineticTransition> KineticTransitions { get; }
    ImmutableArray<MetropolisTransition> MetropolisTransitions { get; }

    StateChange? Find(StateChangeId id);
    StateChangeGroup? Find(StateChangeGroupId id);
    TransitionChain? Find(TransitionChainId id);
    KineticTransition? Find(KineticTransitionId id);
    MetropolisTransition? Find(MetropolisTransitionId id);
}

// Mocassin.Symmetry
public sealed record TransitionPreview(
    TransitionRef Transition,
    TransitionChain Chain,
    ImmutableArray<ParticleId> ParticleOrder,
    ImmutableArray<Fractional3> ReferencePath,
    ImmutableArray<ResolvedPathPosition> ResolvedPath,
    ImmutableArray<ImmutableArray<Fractional3>> ExpandedPaths,
    ImmutableArray<ImmutableArray<int>> StateChangeMatrices,
    int ExpectedRuleCount,
    int CellCountA,
    int CellCountB,
    int CellCountC);

public interface ITransitionPreviewService
{
    TransitionPreview? TryCreatePreview(
        KineticTransition transition,
        ITransitionQuery transitions,
        IParticleQuery particles,
        StructureModel structureModel,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<Diagnostic> diagnostics);

    TransitionPreview? TryCreatePreview(
        MetropolisTransition transition,
        ITransitionQuery transitions,
        IParticleQuery particles,
        StructureModel structureModel,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<Diagnostic> diagnostics);
}
```

- `ITransitionQuery` is implemented by the immutable `Project` snapshot.
- `IParticleQuery` is the Step 03 contract that exposes `Particles` and `VoidParticle`; the preview service
  receives it to build the canonical `ParticleOrder` for `StateChangeMatrices`. It does not reach into the
  project or infer particles from transition data.
- `ITransitionPreviewService` lives in `Mocassin.Symmetry`; Core never references Symmetry. It receives the
  full `ITransitionQuery` so it can resolve the chain, groups, and state changes; a transition alone exposes
  only `ChainId`. The parameter is named `structureModel` to avoid the Step 03 CA1716 reserved-keyword
  suppression.
- The preview service follows the Step 03 failure boundary: `TryCreatePreview` returns `null` and diagnostics
  for invalid user data. It never throws for invalid user data and never returns a partial preview.
- The preview echoes the requested `(A, B, C)` counts and the transition reference, so it cannot be confused
  with another supercell or another transition sharing the same chain.
- The preview is cached by `(transitionRef, transitionModelHash, structureHash, A, B, C, catalogDataVersion,
  catalogDataSha256, symmetryContractVersion)`, where `transitionModelHash` is a SHA-256 of the canonical
  closure (transition, chain, groups, state changes, and referenced particles) computed by
  `ProjectHasher.ComputeTransition`. The symmetry-version components match `StructurePreviewService` and are
  injectable for tests. The cache is scoped to the preview service, bounded, and thread-safe.
- `StateChangeMatrices` contract:
  - `StateChangeMatrices.Length` equals the chain position count (2 for `Metropolis2`).
  - `ParticleOrder` is the canonical particle order supplied by `IParticleQuery`: its `Particles` sorted by
    ID, followed by `VoidParticle`. `ParticleCount = ParticleOrder.Length`.
  - each matrix is dense row-major with `ParticleCount * ParticleCount` entries; the entry at
    `donorIndex * ParticleCount + acceptorIndex` is `1` when the position's group contains that
    `[donor, acceptor]` pair and `0` otherwise;
  - an unstable position must have exactly one `1`, at `[donor, void]`; a stable position must have at least
    one `1`; an empty or default matrix array means the chain is invalid and is never returned by a successful
    preview.
- The preview is advisory; the compiled model is authoritative in Step 08.
- `StateChangeMatrices` are the per-position donor/acceptor sets used by Step 08 rule generation and the
  Step 24 S0/S1/S2 preview.

### 11. Omitted and derived original fields

- `IsMetropolis` is derived from `Pattern == Metropolis2` (or a 2-position `Custom` chain) and is not persisted.
- `StateCount` is derived from `GroupIds.Length` and is not persisted.
- `IsAssociation` is intentionally not persisted. Step 08 derives association/dissociation and vehicle movement
  flags from the state triplet and connector sequence. This avoids a second source of truth that can disagree
  with the generated rules. Step 08 must add a test for the derived flag.
- The original `IsVacancyPair` and `IsUnstablePositionPair` predicates are derived helpers on `StateChange`, not
  persisted fields.
- The original empty-list indexing, disabled self-exchange check, and ambiguous end-state selection are
  corrected as follows:

| Original defect | Step 04 rule |
| --- | --- |
| Empty-list indexing in normal-vehicle validation | Validate counts before indexing; a vehicle with an empty side is TRN009, not an exception |
| Disabled self-exchange check | `MetropolisTransition` with `SiteA == SiteB` is a valid intra-sublattice exchange; TRN016 checks existence/stability, not inequality |
| Ambiguous end-state selection | S2 is derived deterministically from the group sequence; consecutive unstable positions are TRN013 and a no-unstable kinetic chain is TRN022 |
| Conservation only during rule generation | Definition-level validation here; mass/charge conservation remains a Step 08 build gate |
| Original warnings for unbuildable definitions | Definition errors are now Errors; only TRN004/TRN018 remain Warnings |
| Multiple pairs on a single unstable position | TRN026 requires exactly one `[D, ∅]` state change per unstable position; it does not limit the number of unstable positions (so `Vehicle6` is valid) |
| Charge-transfer ambient ambiguity | Homogeneous groups plus Step 08 total-charge conservation |

## Code layout

The new code follows the structure policy: PascalCase domain folders, namespaces that match folders, no new
root-level files, no catch-all folders, and no folder above 20 direct files.

```text
Mocassin.Core/
  Model/
    ModelDefinition.cs                  # add transition sections and empty defaults
    ModelDefinitionJsonConverter.cs     # read/write the new sections
    Transitions/
      StateChange.cs
      StateChangeGroup.cs
      TransitionPattern.cs
      TransitionLinker.cs
      TransitionKind.cs
      TransitionRef.cs
      TransitionRefJsonConverter.cs
      TransitionChain.cs
      KineticTransition.cs
      MetropolisTransition.cs
      ResolvedTransitionPath.cs
      TransitionPathResolver.cs
      ITransitionQuery.cs
      TransitionModelValidator.cs
  Diagnostics/
    TransitionDiagnosticCodes.cs        # TRN001-TRN028
  Editing/
    ProjectEditor.cs                    # existing Apply/validation core
    ProjectEditor.Transitions.StateChanges.cs
    ProjectEditor.Transitions.Chains.cs
    ProjectEditor.Transitions.Transitions.cs
  Project/
    Project.cs                          # implements ITransitionQuery
    ProjectJson.cs                      # canonicalization and nested ProjectJsonContext
    ProjectHasher.cs                    # ComputeTransition closure hash
    ProjectValidator.cs

The five transition typed IDs already exist in `Ids/TypedIds.cs` from Step 03; no new ID file is added.

Mocassin.Symmetry/
  Transitions/
    TransitionPreview.cs
    TransitionPreviewService.cs
    ITransitionPreviewService.cs
```

Tests mirror the source concept:

- `Mocassin.Core.Tests/Transitions/StateChangeTests.cs`
- `Mocassin.Core.Tests/Transitions/TransitionChainTests.cs`
- `Mocassin.Core.Tests/Transitions/TransitionPathTests.cs`
- `Mocassin.Core.Tests/Transitions/TransitionValidationTests.cs`
- `Mocassin.Core.Tests/Transitions/TransitionModelTests.cs`
- `Mocassin.Core.Tests/Project/TransitionPersistenceTests.cs`
- `Mocassin.Symmetry.Tests/Transitions/TransitionPreviewTests.cs`
- `Mocassin.Benchmarks/Transitions/TransitionPreviewBenchmarks.cs`

New standalone samples live under `samples/transitions/`; the existing `samples/ceria/ceria.mocprj` is updated
in place for the dedicated unstable-site set. The new user-guide, physics, and reference pages are added to
`zensical.toml` navigation in the same step.

## Implementation tasks

- [ ] Extend `ModelDefinition` with the flat transition sections and empty defaults; update
  `ModelDefinitionJsonConverter`, the nested `ProjectJsonContext`, `ProjectValidator`, and
  `ProjectJson.CanonicalizeModel`; confirm `ProjectJsonConverter.Write`, `ProjectStore.SaveAsync`, and
  `ProjectHasher.Compute` preserve transitions.
- [ ] Implement `StateChange`, `StateChangeGroup`, `TransitionChain`, `KineticTransition`,
  `MetropolisTransition`, `TransitionRef`, `TransitionKind`, and `TransitionRefJsonConverter`. Reuse the five
  transition typed IDs already in `Ids/TypedIds.cs`; do not duplicate them.
- [ ] Implement pattern/linker validation, including the `Custom` grammar, the built-in pattern stability
  sequences (`TRN028`), and `GeneratesReverse` consistency.
- [ ] Implement `TransitionPathResolver` and path/stability validation, including `TrimToUnitCell`
  canonicalization, the `PathMatchTolerance` match, any-repeated-position ring detection, and ambiguous-site
  diagnostics.
- [ ] Implement `ITransitionQuery` (implemented by `Project`), `ProjectHasher.ComputeTransition`, and
  `ITransitionPreviewService`, including the `TryCreatePreview` failure boundary, the self-describing preview,
  `StateChangeMatrices`, and the bounded cache with symmetry-version key components.
- [ ] Implement transition preview using the symmetry engine and the requested `(A, B, C)` cell counts,
  including MMC site-pair expansion.
- [ ] Implement reverse-link validation and `TRN018`.
- [ ] Implement reference, duplicate-name, count, and cross-kind validation.
- [ ] Implement editor operations in partial `ProjectEditor.Transitions.*.cs` files and dependent-removal
  diagnostics.
- [ ] Add `TransitionDiagnosticCodes` for TRN001–TRN028, including hints and help anchors. Add
  `docs/reference/transitions.md` with the TRN table and links; Step 07 creates the consolidated
  `docs/reference/diagnostics.md` and `docs/troubleshooting/errors.md` and imports/links that page.
- [ ] Update `contracts/manifest.json`: set `status` to `step-04`, update `notes`, add the transition fields,
  add the `project-transitions-minimal` and `project-transitions-populated` fixtures, and regenerate the
  project schema. Keep the `model` signature skeleton for Step 08; record that the transition fields feed it.
- [ ] Add XML docs, update `PublicAPI.Unshipped.txt`, and refresh the ApiCompat baseline for every new public
  type and member.
- [ ] Add `TransitionPreviewCeria` to `Mocassin.Benchmarks/Transitions/TransitionPreviewBenchmarks.cs` and
  record its baseline in `tests/reference/bench/baselines.json` and `tests/reference/bench/`.
- [ ] Add ceria KMC (oxygen migration), ceria MMC (O/V exchange), and polaron-hop samples under
  `samples/transitions/`.
- [ ] Add a 5-site vehicle and a 5-site interstitialcy sample under `samples/transitions/`.
- [ ] Update `samples/ceria/ceria.mocprj` in place so the unstable oxygen transition site uses a dedicated
  transition-state occupation set instead of the shared oxygen set (the Step 03 deferred item).
- [ ] Add the new user-guide, physics, and reference pages to `zensical.toml` navigation.
- [ ] Update the transition rows in `appendix-b-parity-matrix.md`.
- [ ] At closeout, update the Step 04 status page and dashboard, run `./tools/verify.sh all`, dispatch the
  single Ubuntu closeout CI run, and prepare `docs/planning/audits/step-04.md`. No per-feature CI.

## Acceptance criteria

- [ ] Ceria oxygen migration can be defined as a 3-site chain with `[O,V_O]`, `[O,∅]`, `[O,V_O]` and a
  stable-unstable-stable path.
- [ ] A 2-site MMC exchange `[A,B]` can be defined and previewed, including a same-site exchange.
- [ ] A 5-site vehicle with five chain groups (`[A,V_A]`, `[A,V_A]`, `[AB,∅]`, `[B,V_B]`, `[B,V_B]`),
  a `D,S,S,D` chain, and an `S,S,U,S,S` reference path validates.
- [ ] A 5-site interstitialcy with an `S,U,S,U,S` path validates.
- [ ] Invalid paths (unstable endpoint, consecutive unstable, pattern-sequence mismatch, any repeated
  position including a periodic-image ring, unmatched point) are rejected with precise diagnostics.
- [ ] A complex transition without a reverse link warns `TRN018`; adding a symmetric reverse link clears it.
- [ ] `GeneratesReverse` set inconsistently with the pattern is rejected with `TRN024`.
- [ ] The preview enumerates the expected symmetry-equivalent paths and echoes `(A, B, C)` and the transition
  reference.
- [ ] An invalid transition returns `null` and diagnostics from `TryCreatePreview`; no partial preview is
  returned.
- [ ] `StateChangeMatrices` has the documented dimensions, particle order, binary encoding, and
  stable/unstable cardinality.
- [ ] The transition sections round-trip through save/load, the project JSON context, and the canonical model;
  semantic arrays preserve order and transition edits change the document hash.
- [ ] The generated project schema accepts the transition sections, and fixtures cover empty and populated
  models.
- [ ] Public API files and the ApiCompat baseline are current.
- [ ] Documentation written.

## Documentation

At this point, document the transition model:

- `docs/user-guide/transitions.md` — state changes, groups, chains, pattern/linker rules, KMC/MMC paths,
  reverse links, and preview semantics.
- `docs/physics/monte-carlo.md` and `docs/physics/kmc.md` — transition mechanisms and state triplets.
- `docs/physics/mmc.md` — two-site exchange semantics, including same-site exchange.
- `docs/development/compilation-pipeline.md` — transition inputs and the deferred rule-generation stage.
- `docs/reference/project-format.md` — transition sections, field names, semantic ordering, reverse links,
  path matching, and constraints.
- `docs/reference/transitions.md` — transition sections, field names, semantic ordering, reverse links, path
  matching, and the TRN001–TRN028 table with severities, paths, hints, and help anchors. Step 07 owns the
  consolidated `docs/reference/diagnostics.md` and `docs/troubleshooting/errors.md` and links to this page.

Add the new user-guide, physics, and reference pages to `zensical.toml` navigation in the same step. Update the
Step 04 status page, `docs/planning/status.md`, and the transition rows in `appendix-b-parity-matrix.md` at
closeout.

## Tests

Tiers: PR-fast for unit/acceptance/contract tests; PR-medium for random property sweeps; Nightly/release for the
preview benchmark. The full benchmark is not a PR-fast wall-clock gate.

| Test | Assertion | Tier |
| --- | --- | --- |
| State changes | Donor/acceptor/void validation, including donor-void reverse pairs | PR-fast |
| Groups | Mixed physical/charge exchange rejected; homogeneous charge-state group accepted | PR-fast |
| Patterns | Every built-in pattern accepts only its valid linker sequence | PR-fast |
| Custom | Valid/invalid 2-position and 3–8-position custom chains; static placement rules | PR-fast |
| Paths | Stability, length, no-unstable, pattern-sequence mismatch, any repeated position (interior, literal endpoint, periodic image), consecutive-unstable checks | PR-fast |
| Path matching | `[0,1)` and outside-`[0,1)` points trim with `TrimToUnitCell` and map to sites within `PathMatchTolerance`; zero/ambiguous matches are TRN020 | PR-fast |
| Metropolis | Same-site and different-site exchanges; unknown/unstable sites rejected | PR-fast |
| Reverse links | Symmetric same-kind link accepted; unknown/cross-kind/asymmetric/self link rejected; TRN018 warning | PR-fast |
| Preview | Symmetry-expanded path count matches brute force; MMC pair expansion; `(A,B,C)` echo | PR-fast |
| Preview failure/cache | Invalid input returns null + diagnostics; cache keys separate `(A,B,C)`, structure hash, transition hash, and symmetry catalog/contract versions | PR-fast |
| State-change matrices | Dimensions, particle order, binary cells, unstable cardinality | PR-fast |
| Editing | Add/update/remove/duplicate/make-group keep references valid; dependents are reported | PR-fast |
| Model envelope | Empty, transition-only, and combined models round-trip through the project JSON context | PR-fast |
| Persistence/canonical hash | Save/load preserves transitions; transition edits change `ComputeDocument`; semantic arrays do not re-sort | PR-fast |
| Schema | Generated project schema accepts the transition sections and rejects unknown fields | PR-fast |
| Semantic order | Group IDs, linker order, and reference path order survive serialization unchanged | PR-fast |
| Contract fixtures | `project-transitions-minimal` and `project-transitions-populated` load and round-trip | PR-fast |
| Samples | Ceria and vehicle fixtures validate | PR-fast |
| Property tests | Random transition models round-trip canonically; random paths match a brute-force site/stability oracle | PR-medium |
| Performance | `TransitionPreviewCeria` BenchmarkDotNet case records a baseline; mean < 100 ms on the Step 02 benchmark runner class | Nightly/release |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Canonicalization drops transition data | Explicit `CanonicalizeModel` extension, save/load test, document-hash edit test |
| Ambiguous vehicle semantics | Encode the documented unidirectional behavior; add a golden vehicle model and a `Vehicle5` acceptance case |
| Custom chains create unsupported mechanisms | Restrict to the documented `D`/`S` grammar; rule generation rejects unsupported cases |
| Reverse-generation confusion | Constrained `GeneratesReverse`, explicit reverse links, TRN018, and a Step 08 test |
| Path stability mismatch with energy model | Resolve paths against reference sites with a named tolerance; revalidate against the structure in Step 08 |
| Charge-transfer groups mix semantics | Unconditional homogeneous-group rule and Step 08 total-charge conservation test |
| Preview cache returns stale data | Cache key includes transition hash, structure hash, and `(A,B,C)`; test key separation |
| Interaction-range warning is misowned | Deferred to Step 07 as `ENM018`; Step 04 does not read Step 05 cutoffs |

## Done when

All original transition mechanisms can be defined, previewed, and validated, and invalid definitions produce
actionable diagnostics before compilation.
