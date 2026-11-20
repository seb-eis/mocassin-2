# Step 08 — Model Compilation Pipeline

**Phase:** C. Compilation
**Depends on:** 02–07
**Delivers:** A pure, staged compiler that turns a validated `Project` into a deterministic, densely indexed
`CompiledModel` with P1-extended structure, environments, pair/cluster tables, transition rules, and simulation
encoding. The compiler performs no I/O, has no UI dependency, and is consumed by the Step 09 parameterization,
Step 10 job expansion, Step 11 `.msl` writer, and Step 12 build orchestration.

## Implementation status

- **Status:** Done
- **Step branch:** `step/08-model-compilation-pipeline` (merged and deleted)
- **Step CI:** [closeout green](https://github.com/seb-eis/mocassin-2/actions/runs/43538703111); [main green](https://github.com/seb-eis/mocassin-2/actions/runs/43578897490)
- **Main merge:** `f91fc632fbba67a5757f8df7588484e5d13c2458`
- **Step tag:** `step-08`
- **Audit:** Passed with notes (round 4, `f97b2c4`) — [record](../audits/step-08.md); accepted by the human owner 2026-11-14
- **Design review:** Ready for implementation at `b84ceadda1ba8b1715592a4f6884d76bc7c36fed` — [review record](../reviews/step-08.md) `195451021031272af96ee1b2ca600d4c8cd83c2c`; accepted by the human owner 2026-11-13
- **Last updated:** 2026-11-20 (maintenance note)

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| `compiler-core` | `feature/step-08-model-compilation-pipeline/compiler-core` | Done | `verify.sh dotnet` (932 tests), `structure`, `api`, `contracts`, `docs` pass | — | `1deb182` |
| `structure-and-pair-compilation` | `feature/step-08-model-compilation-pipeline/structure-and-pair-compilation` | Done | `verify.sh dotnet` (939 tests), `structure`, `api`, `contracts` pass | — | `4280773` |
| `cluster-compilation` | `feature/step-08-model-compilation-pipeline/cluster-compilation` | Done | `verify.sh dotnet` (943 tests), `structure`, `api`, `contracts` pass | — | `dee2034` |
| `transition-rule-compilation` | `feature/step-08-model-compilation-pipeline/transition-rule-compilation` | Done | `verify.sh dotnet` (947 tests), `structure`, `api`, `contracts` pass | — | `26366aa` |
| `simulation-encoding` | `feature/step-08-model-compilation-pipeline/simulation-encoding` | Done | `verify.sh dotnet` (950 tests), `structure`, `api`, `contracts` pass | — | `fde55fd` |
| `indexing-signatures-caching` | `feature/step-08-model-compilation-pipeline/indexing-signatures-caching` | Done | `verify.sh dotnet` (954 tests), `structure`, `api`, `contracts` pass | — | `10966cf` |
| `compile-diagnostics-and-report` | `feature/step-08-model-compilation-pipeline/compile-diagnostics-and-report` | Done | `verify.sh dotnet` (960 tests), `structure`, `api`, `contracts`, `docs` pass | — | `6c59163` |
| `golden-models-and-closeout` | `feature/step-08-model-compilation-pipeline/golden-models-and-closeout` | Done | `verify.sh dotnet` (999 tests), `structure`, `api`, `contracts`, `docs` pass | — | `86348bf` |

### Implementation notes and deviations

- **Post-step correction (2026-11-14, Step 09 design).** The implemented `CompiledEnergyModel` contains pair
  geometries, pair tables, and cluster tables only. It does not compile the authored site-energy background
  mentioned in the §10 design bullet. Step 09 resolves `Project.Model.SiteEnergies` onto the compiled
  `[position][particle]` background, applies template overrides, and Step 11 writes it as the `.msl`
  `energy.defect_background`. The Step 09 page and the data contract are authoritative for that boundary.
- **Compiler namespace.** The public compiler contract lives in `Mocassin.Compilation.Compiler`, the cache
  contracts in `Mocassin.Compilation.Caching`, and the immutable stage input in
  `Mocassin.Compilation.Context`. The design §2 snippet writes `namespace Mocassin.Compilation;`, but
  `AGENTS.md` §8 requires each file namespace to match its concept folder, and the accepted architecture
  layout already assigns these types to `Compiler/`, `Caching/`, and `Context/`. Type names, signatures, and
  behaviour are unchanged; consumers add a `using` directive.
- **Compiler core.** `compiler-core` landed the public result, report, option, cache, and stage contracts plus
  the synchronous stage orchestrator. Every later feature replaced its placeholder with the real stage; the
  complete pipeline now produces a signed compiled model or a diagnostics-only failure.
- **Vehicle and interstitialcy sample correction.** `vehicle5.mocprj` now authors alternating
  `[A -> V, V -> A, AB -> void, B -> V, V -> B]` groups and a separate reverse chain. `interstitialcy5`
  authors alternating outer groups plus an explicit spectator self state change at its interior stable
  centre. Both compile, keep their explicit reverse transitions, and have pinned signatures; the spectator
  placement is validated as `TRN002` only outside an interior stable position with unstable neighbours.
- **compiler-core local evidence (2026-11-13).** `./tools/verify.sh dotnet` passes (932 tests, 0 failed),
  `./tools/verify.sh structure` passes, `./tools/verify.sh api` passes with the refreshed
  `Mocassin.Compilation` ApiCompat baseline, `./tools/verify.sh contracts` passes, and
  `./tools/verify.sh docs` passes. No per-feature CI run was triggered. Squash-merged into the step branch as
  `1deb182` (`step-08: add pure compiler core and stage orchestrator`); the feature branch was deleted locally
  and remotely.

### Complex-mechanism resolution (2026-11-13)

The original `vehicle5` and `interstitialcy5` samples repeated the same donor/acceptor direction on both sides
of a complex chain. The samples now alternate direction, and `interstitialcy5` uses an explicit **spectator
self state change** at its interior stable centre. `TransitionModelValidator` accepts a self state change only
at an interior stable position with unstable neighbours (`TRN002` otherwise), so the middle site remains
unchanged while the two unstable carriers move through it. The vehicle and interstitialcy samples compile,
keep their explicit reverse transitions, and have pinned signatures. The charge transport vector is computed
from the charge-weighted endpoint moments, which reproduces the intended field contribution for single
charges, vacancies, charge-state changes, and non-collinear paths.

### Maintenance note — translation-equivalent starts (2026-11-20)

Branch `fix/step-08-equivalent-start-mapping` (commits `1899421`, `85e27b2`, `4f5059e`) addresses the
centered-group half of audit finding m1: a centered space group no longer emits one direction per centering
image. Two expanded paths are merged into one direction only when (a) their fractional displacement sequence
`x(i) - x(0)` matches
and (b) their start positions are related by a setting operation whose linear part is the identity, i.e. a
centering/lattice translation. Rotational or mirror images stay distinct directions because they change the
relative path in the P1 frame; an unproven relation is never merged. The merged direction records every start
in `EquivalentStartPositions` (including the representative), and `EncodingCompilationStage.BuildMappings`
maps it into every matching `jump_mapping` bucket. Single-start directions keep the empty list representation,
so existing compiled-model signatures and the `msl-v1`/`amend-1-net10` canonical summaries are unchanged.

Regression: `samples/simulations/centered-exchange.mocprj` is a minimal C-centered MMC project with eight P1
positions and a minimal job template. `EquivalentStartCompilationTests` asserts two forward and two inverse
directions, two equivalent starts per direction, distinct rotational variants, and cross-bucket
`jump_mapping`; it also pins the empty single-start representation. `CenteredEquivalentStartPayloadTests`
asserts the same property on the model payload's `jump_mapping`, which is what the `.msl` writer stores, and
pins the uncentered single-start payload. The sample builds a writer-valid package with the real CLI
(`build samples/simulations/centered-exchange.mocprj -o <path>`, exit 0, 10/10 stages, `Verification: passed`,
one job). `PackageFixtureTests.RebuildReproducesTheFixtureSummary` and
`PreAmendmentCanonicalSummaryMatchesTheCheckedInGolden` pass unchanged, so the `msl-v1` and `amend-1-net10`
model signatures and canonical summaries are not regenerated.

Local gates (2026-11-20, exit 0 for every command): `./tools/verify.sh dotnet` (98 Compilation, 503 Packages,
223 Symmetry, 42 Results, 685 Core, 200 Cli, 37 Contracts, 1 App, 1 Architecture tests; build 0 warnings and
`dotnet format` clean), `./tools/verify.sh structure` (1/1), `./tools/verify.sh api` (public API compatible),
`./tools/verify.sh contracts` (26 artifacts current, projection fixtures current, 58 tool tests, SPDX/secrets/
cargo-deny OK), and `./tools/verify.sh docs` (links, math, strict site, DocFX, and rustdoc OK). No CI was
dispatched; feature branches do not trigger CI.

The Step 11 verifier observation (`PackageVerificationChecks` bounds `cluster.byte_position` by the
reference-site count instead of the cluster node slot) is **not** part of this fix and remains deferred to the
Step 11 maintenance work recorded in the `step-13-amend-1`
(`docs/planning/amendments/step-13-amend-1-fixture-properties.md` on the Step 13 branch) "Step 11 verifier
observation" section. General rotational orbit reduction and `DependentRules` emission remain the Step 29
parity item.

## Goal

This is the scientific heart of the model builder. The compiler must be deterministic, explainable,
cacheable, and independently testable. It consumes an immutable `Project` that has already passed the Step 07
validation pipeline and produces a `CompiledModel` for the parameterization, job, and package stages. It
performs no file I/O, no CLI parsing, no GUI work, and no parameterization-value editing.

## Original reference

- Two stages existed:
  1. lazy, event-invalidated cache ports inside `Mocassin.Model` (P1 lists, encoders, pair finder, group
     analyzer, rule setters);
  2. `Mocassin.Model.Translator`'s `ProjectModelContextBuilder`, which built structure/energy/transition/
     simulation contexts in parallel and linked them.
- The translator assigned dense `ModelId` indices and encoded arrays for the C solver.
- Key algorithms:
  - `SpaceGroupService.GetUnitCellP1PositionExtension` / path extension;
  - `PairInteractionFinder` (radial search, distance buckets, center-stabilizer `G0` reduction, chiral
    partners);
  - `GeometryGroupAnalyzer` / `PointOperationGroup.GetUniquePermutations`;
  - `TransitionRuleGenerator` (donor/acceptor permutation, inverse/dependent rules, conservation);
  - energy-table expansion and jump/mapping/tracker tables.
- Known performance issues: repeated point-group construction per pair, linear `Single` lookups in group-table
  creation, O(R²) inverse/dependent pairing, whole-cache invalidation.
- Known defects: conflict handling after mutation, ambiguous end-state selection, unused settings.

The new compiler keeps the good conceptual decomposition (structure → environments → pair/cluster reduction →
rules → encoding) but replaces the cache ports with content-keyed stage caches, replaces O(R²) pairing with
hash maps, and makes ambiguity a hard compile error rather than a runtime surprise.

## Design

### 1. Scope, non-goals, and sizing

**In scope**

- A pure `IModelCompiler` in `Mocassin.Compilation` that returns a `CompilationResult` with the
  `CompiledModel`, diagnostics, counts, timings, signatures, and cache statistics.
- P1 expansion consumption, environments, pair/cluster reduction and tables, transition rules, simulation
  encoding, dense indexing, stable IDs, signatures, and per-stage caching.
- A canonical, deterministic JSON inspection projection for diagnostics and golden tests.
- New `CMP` compile diagnostics and their catalog descriptors.

**Out of scope**

- Parameterization templates and energy/frequency values (Step 09).
- Job templates, supercell/doping generation, and selection-optimizer assignment (Step 10).
- `.msl` SQLite writing, Zstd/MessagePack payload encoding, and `SolverJobFlags` composition (Step 11).
- `mocassin build`, `mocassin compile`, full CLI parsing, progress events, and async orchestration (Step 12).
- Solver execution, trackers as mutable state, and `.mcs` writing (Steps 14–18).
- Any mutation of the project, any file I/O, and any dependency on `Mocassin.Packages`,
  `Mocassin.Contracts`, `Mocassin.Results`, or `Mocassin.Cli`.

> **Post-audit process annotation (2026-11-20):** this sizing record was recorded under the previous
> time-based rule and is superseded by [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers)
> from 2026-11-20. The original text, decisions, and evidence are retained unchanged.

**Sizing and owner waiver.** The roadmap estimate is 4–6 person-weeks. The feature list has eight rows,
including the documentation-only closeout. This meets the roadmap §3.1 split trigger (more than about four
weeks or roughly 6–8 features). The human owner reviewed the design-review finding M5 on 2026-11-12 and
**explicitly accepted the 4–6 person-week, eight-feature single-step scope as a recorded waiver** rather than a
roadmap split. Rationale: the eight features form one coherent compiler pipeline, a partial compiler is not a
usable deliverable for Step 09, and splitting would insert a new roadmap step and change the Step 09–12
dependency chain. The waiver is recorded in the Step 08 review record and status dashboard; it does not waive
the audit-sitting requirement, so the implementation must still keep each feature independently auditable. If
the first three features exceed 2.5 person-weeks, stop and ask the owner to re-open the split decision before
continuing.

### 2. Compiler API and boundary

```csharp
namespace Mocassin.Compilation;

public interface IModelCompiler
{
    CompilationResult Compile(
        Project project,
        CompilationOptions? options = null,
        CancellationToken cancellationToken = default);
}

public enum CompilationStatus
{
    Succeeded = 0,
    Failed = 1,
}

public sealed record CompilationResult(
    CompilationStatus Status,
    CompiledModel? Model,
    ImmutableArray<Diagnostic> Diagnostics,
    CompilationReport Report)
{
    public bool IsSuccess => Status == CompilationStatus.Succeeded && Model is not null;
}

public sealed record CompilationOptions
{
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
    public bool EnableStageCache { get; init; } = true;
    public int StageCacheCapacity { get; init; } = 256;
    public long MaxRuleCount { get; init; } = 1_000_000;
    public long AdvisoryRuleCount { get; init; } = 100_000;
    public long MaxEnergyTableBytes { get; init; } = 2L * 1024 * 1024 * 1024;
    public long AdvisoryEnergyTableBytes { get; init; } = 512L * 1024 * 1024;
    public int MaxClusterTupleCount { get; init; } = 1_000_000;
    public bool EmitUnusedTransitionWarnings { get; init; } = true;
    public ICompilationCache? Cache { get; init; }
    public static CompilationOptions Default { get; } = new();
}

public interface ICompilationCache
{
    bool TryGet<T>(string key, out T? value) where T : class;
    T GetOrAdd<T>(string key, Func<T> factory) where T : class;
    CompilationCacheStatistics Statistics { get; }
}

public sealed record CompilationCacheStatistics(int Hits, int Misses, int Capacity);
```

- **Synchronous and pure.** Compilation is CPU-bound and has no I/O. `Compile` is a synchronous method with a
  `CancellationToken`; it returns a complete result. Step 12 wraps it in its async build orchestration.
  `Mocassin.Compilation` exposes no `Task`, `ValueTask`, `IAsyncEnumerable`, `TimeProvider`, file path, or
  serializer-to-stream API.
- **Stateless service.** A compiler instance holds no mutable state when `Cache` is `null`; it may be reused
  concurrently. The default implementation is `ModelCompiler`. `EnableStageCache = false` disables internal
  caching and ignores `Cache`.
- **Limits.** `MaxRuleCount`, `MaxEnergyTableBytes`, and `MaxClusterTupleCount` are hard limits that fail with
  `CMP006` before allocation. `MaxClusterTupleCount` cannot raise the Symmetry reducer's fixed 1,000,000-tuple
  ceiling; a lower value is enforced by a pre-check. `AdvisoryRuleCount` and `AdvisoryEnergyTableBytes` emit
  `CMP010` warnings but do not fail. The defaults are guardrails, not physics constants; Step 09/11 may tune
  them through the parameterization/job limits.
- **Exceptions are for programmer errors and cancellation only.** A `null` project, invalid options
  (`MaxDegreeOfParallelism < 1`, negative limits, `StageCacheCapacity < 1`), an arithmetic/contract invariant
  violation, or a caller-supplied cache contract violation throws `ArgumentNullException`,
  `ArgumentOutOfRangeException`, `InvalidOperationException`, or `OperationCanceledException`. User-data
  problems return diagnostics with `Status = Failed` and `Model = null`. `Status = Succeeded` with a non-null
  model is valid even when warning-only `CMP009`/`CMP010`/`CMP012` diagnostics are present. Cancellation never
  returns a partial result.
- **No validation pipeline duplication.** See §3.
- **No `Mocassin.Contracts` dependency.** The internal `CompiledModel` uses typed IDs and immutable records.
  Step 09/11 map it to the Contracts `ParameterizedModel`/`.msl` DTOs. `Mocassin.Contracts` may reference
  `Mocassin.Compilation`, never the reverse.

### 3. Validation boundary and build-stage ownership

- `IModelCompiler.Compile` **assumes a project that has passed `ProjectValidationPipeline`**. It does not run
  the load/model/cross-domain/feasibility pipeline and does not accept or consume a `ValidationResult`.
  Loading, validation, policy promotion, reporting, and exit codes are Step 07/12 concerns.
- The compiler does **not** add an `IValidationStage`. `IValidationStage` and `ValidationContext` live in
  `Mocassin.Packages`; `Mocassin.Compilation` must not reference `Mocassin.Packages`. Step 12 owns the
  `ValidationStage.Build` adapter in the build orchestration and maps a compile failure to the build error
  model (exit `4`). `mocassin validate` never compiles.
- Compilation is still defensive. Each stage calls the same narrow Core/Symmetry service the validator uses
  (encoder, sampler, path resolver). If such a service rejects the input, the compiler fails closed with
  `CMP001`, adds the source diagnostics to `Diagnostics`, and adds their paths to `Related`. It does not
  re-author STR/TRN/ENM/SIM rules and does not emit a second full validation report.
- Compilation diagnostics use `Mocassin.Core.Diagnostics.Diagnostic`, `ModelPath`, `DiagnosticBag`, and
  `DiagnosticDescriptor`. They are a separate list from `ValidationResult.Diagnostics`; Step 12 merges the two
  into its build event stream without changing either contract.
- `ValidationResult.Statistics` remains advisory. The Step 08 `CompilationReport` is authoritative for
  compiled counts; `SIM040`/`SIM041` remain pre-build budget warnings/errors and may differ from the final
  compiled counts.

### 4. Pipeline stages

Stages are pure functions over the immutable `CompilationContext`. Expensive independent work may run in
parallel, but the output order is canonical and independent of scheduling.

| # | Stage | Input | Output | Cache key inputs | Primary diagnostics |
| ---: | --- | --- | --- | --- | --- |
| 1 | Resolve | `Project` | context: catalog, setting, cell geometry, encoder, sorted particles/sites; simulations are read later | not cached (cheap; avoids a whole-model key) | `CMP001`, `CMP002` |
| 2 | Structure/P1 | context | P1 positions, site mapping, orbits, stable/unstable partition, cell vectors, interaction range | catalog, contract, structure hash | `CMP002`; forwarded `STR` |
| 3 | Environments | structure, energy model | stable/unstable environments, pair-image candidates, filters applied | energy hash, structure hash, catalog, contract | `CMP003`; forwarded `ENM` |
| 4 | Pairs | environments | directed pair geometries, shared energy IDs, chiral links, pair links per position | energy hash, structure hash, catalog, contract | `CMP003` |
| 5 | Clusters | energy model, structure | point groups, canonical occupation tuples/codes, cluster tables, links | energy hash, structure hash, cluster IDs, allowed occupations | `CMP003`, `CMP006` |
| 6 | Rules | referenced transitions, structure, particles | collections, directions, state codes, forward/inverse/dependent rules | active-transition-set hash, transition closure hashes, structure hash, particle order, catalog, contract | `CMP004`–`CMP008` |
| 7 | Encoding | rules, simulations, energy links | jump collections/directions/rules, count/mapping tables, tracker definitions, derived intent | simulation hash, rule hash, energy hash, structure hash | `CMP006`, `CMP009`, `CMP010` |
| 8 | Indexing | structure, particles, energy tables/site energies, rules, simulations | dense indices, stable geometry/rule IDs, lookup maps, runtime-default snapshot | structure hash, particle hash, energy hash, pair/cluster table hashes, rule hash, simulation hash | `CMP006`, `CMP011` |
| 9 | Signing | canonical compiled projection | `ModelSignature` | canonical projection bytes (no runtime settings) | `CMP011` |
| 10 | Report | all prior | counts, per-stage timings, input hashes, cache statistics, summary | — | — |

- `Resolve` is cheap and always runs; it is not cached by the whole-model hash, so energy/transition edits do
  not force a misleading "reuse". It stores immutable context values for the later stages.
- `Structure` calls the public `IStructurePreviewService.TryResolve`/`StructurePreviewService.TryResolve`
  (not the internal `SpaceGroupReferenceResolver`), `CellGeometry.TryCreate`, and
  `UnitCellVectorEncoder.TryCreate` with reference sites sorted by `CellSiteId`. It never re-sorts canonical P1
  order.
- `Environments`, `Pairs`, and `Clusters` call the **same public Symmetry services as the Step 05 preview**:
  `IEnergyGeometrySampler`/`EnergyGeometrySampler`, `PairReduction`, `PointGroup`, and `ClusterReduction`.
  Step 08 adds table allocation, canonical table order, dense particle indices, and links.
- `Rules` uses the public `TransitionPathResolver` and the Step 04 authored transition model; it does not
  duplicate the validator's path rules. Point-level expansion uses the additive public
  `TransitionPathResolver.TryResolvePoint`/`TryResolvePath` API; the existing whole-transition `TryResolve`
  delegates to it and `FindMatches`/`TryGetComponentShift` remain private.
- `Encoding` and `Indexing` are the only stages that assign dense indices. They run after all geometry and
  rule content is known.
- `Signing` is a pure canonical projection; `Report` never affects the signature.

### 5. Structure, P1 expansion, and environments

- **Particle index.** `0` is the implicit void particle. Authored particles are sorted by ascending
  `ParticleId.Value`. This is deliberately not the `TransitionPreviewService.GetParticleOrder` order, which is
  authored particles ascending and then void last; the preview's state-change matrix order is not a compiled
  index contract. The compiler maps every `ParticleId` to its compiled index explicitly, and tests pin the
  mapping so state codes and Step 09/11 consumers cannot diverge.
- **Position index.** The canonical P1 order from Step 02: expand, trim, exact-grid deduplicate, sort by
  fractional `(a,b,c)`, assign `P = 0..Pcount-1`.
- **Site index.** Reference sites sorted by ascending `CellSiteId.Value`. The compiler feeds the encoder the
  same sorted order, so `ExpandedSite.ReferenceSiteIndex` is stable. `site_index` is the reference-site index,
  not `P`.
- **Structure output.** Cell vectors in ångström, P1 positions `[cellA, cellB, cellC, P]`, owning
  `site_index`, stability, allowed-particle mask, orbit membership, and the reference-site table.
- **Environments.** One stable environment plus the effective unstable environments (authored environment or
  inherited stable cutoff, as Step 05 defines). A position's environment carries its selectable particle
  mask, pair links, cluster links, and update particle IDs.
- **Pair sampling.** Use `IEnergyGeometrySampler.TrySamplePairs(model, 1, 1, 1, ...)`: the compiled model is
  per unit cell, and the sampler's cell counts only affect its cache key, not the sampled geometry. The
  sampler already applies the cutoff, filters, `1 fm` buckets, `G0` reduction, and chiral linking. It uses
  `PeriodicGeometry.RadialSearch`/minimum-image APIs internally; the compiler does not reimplement periodic
  image enumeration or tolerance deduplication.
- **Re-sorting.** The sampler returns previews sorted by site/P/distance/geometry ID for preview stability.
  The compiler re-sorts pair tables by the canonical shell order from Step 02:
  `(distanceBucket, targetP, cellA, cellB, cellC)`, then center/partner site index. Geometry IDs are content
  hashes, not table order.
- **Step 05 cross-check.** The deferred preview/compiler consistency check is a test, not a runtime
  dependency: compiled pair-image, unique-pair-table, cluster, and permutation counts must equal
  `EnergyPreviewService`/`EnergyCostPreview` for ceria, barium zirconate, the vehicle, the interstitialcy, and
  random valid models. `ENM018` remains Step 07's boundary; Step 08 does not re-emit it.

### 6. Pair reduction and chirality

- Pair reduction, `G0` grouping, distance bucketing, representative selection, and chiral-partner detection
  are owned by `PairReduction` and `EnergyGeometrySampler`; Step 08 consumes their output.
- Every directed pair geometry carries:
  - `GeometryId` — SHA-256 over `centerSiteId`, `centerP`, `partnerSiteId`, `partnerP`, representative
    offset `(cellA, cellB, cellC)`, and `DistanceBucketFm`;
  - `EnergyId` — the shared table/parameterization identity; equal to `GeometryId` for an achiral or
    unstable–stable pair, and the lexicographically smaller geometry ID for a reciprocal chiral pair;
  - `ChiralPartnerGeometryId` — present only for a reciprocal chiral link.
- Chiral directions share one `EnergyId` and one pair table; each direction keeps its own `GeometryId` and
  inverse partner. A one-way orbit match is not a chiral pair and stays achiral. This is an orbit
  relationship, not physical chirality; in P1 every directed pair is its own orbit.
- Unstable–stable pairs are asymmetric, have `GeometryId == EnergyId`, and are compiled per unstable
  environment.
- The compiled pair table is `n_particles × n_particles`, row-major `[centerParticle][partnerParticle]` in eV.
  Entries for particle combinations the site pair cannot realize are zero; the environment's particle mask is
  authoritative and `mocassin-msl` validates it.
- The environment link records the table index (`EnergyId`), target P, and the image's relative offset. Step 11
  requires one link per periodic image to keep the solver's runtime neighbour list complete: the compiler
  expands `PairShell.EquivalentImages` (exposed additively on `PairInteractionPreview`) into one
  `CompiledPairLink` per image. The existing four-field link projection then signs every image; its values
  change for multi-image shells and the Step 08/09 signature fixtures are regenerated (Step 11 design, M-1). The
  solver applies `0.5` pair double-count correction as Step 15 defines.

### 7. Cluster compilation

- For each authored `Cluster`, resolve the center and every offset to exactly one canonical P1 position using
  the Step 02 encoder and the Step 04 minimizing-shift rule. Resolve each surrounding position to its owning
  stable reference site and allowed particle set.
- Build the ordered full geometry `[center, offset0, …]` and call
  `ClusterReduction.Reduce(setting, center.Fractional, geometry, allowedParticles)`. The reducer owns `G0`,
  `Gset`, `Gseq`, the induced permutation group `P`, canonical tuples, 6-bit codes, and the expansion map.
  Catch its invalid-input `ArgumentException` as `CMP003` and its bounded-enumeration
  `InvalidOperationException` as `CMP006`; a reducer failure must not escape as an unstructured exception.
- The center particle is always site 0 of the tuple; the raw combination count includes the center particle
  factor. `ClusterReduction` enumerates tuples and has a fixed 1,000,000 bound but no Burnside output or
  configurable bound. Before calling it, compute the raw count with saturating `long` arithmetic and emit
  `CMP006` when it exceeds `MaxClusterTupleCount` or the fixed 1,000,000 ceiling; then call the reducer and
  convert its bounded-enumeration exception to `CMP006`. The preview's Burnside count is a test cross-check,
  not a compiler output.
- Occupation codes are `ulong`, 6 bits per site, site 0 least significant, up to 9 sites, unused high bits
  zero. Particle indices must be `<= 63`; Step 03's 63-particle limit makes this exact.
- A cluster table is shared by all symmetry-equivalent centers of the authored cluster. Its stable ID is
  `SHA-256("mocassin/cluster-energy-id/v1" || clusterId || centerSiteId || canonical geometry hash)`; Step 09
  uses it as the group-set identity. The table stores:
  - `occupation_codes` sorted ascending;
  - `energies` in eV, default `0.0` until Step 09 applies values;
  - `particle_table_mapping` — a row for each allowed center particle index; row order is ascending compiled
    particle index.
- Cluster contributions are counted at the center only. The environment's cluster link stores the cluster
  table index and the byte position of the owning position in the occupation code so Step 15 can update codes
  incrementally.
- Preserve chiral distinctions; never merge mirror tuples unless the mirror permutation is in `P`.

### 8. Transition rule compilation

#### 8.1 Inputs and direction expansion

For each kinetic or Metropolis transition referenced by at least one simulation:

1. Resolve the reference path with `TransitionPathResolver` (kinetic) or the two sites (Metropolis). Path
   resolution errors are `CMP007` with the source `TRN` diagnostic in `Related`.
2. Build the integer relative encoding of the reference path:
   `relative[i] = (encoding[i].A - encoding[0].A, …, encoding[i].P)`. Node 0 is `(0,0,0,P0)`.
3. Expand the path under every setting operation, re-resolve each transformed point with the public
   `TransitionPathResolver.TryResolvePath` to a canonical `(P, offset)`, and deduplicate exact relative
   encodings. Then reduce by the center stabilizer `G0` of `P0`. The canonical representative is the
   lexicographically smallest relative encoding sequence. The compiler does not duplicate the nearest-image
   or tie rule.
4. Emit one `CompiledJumpDirection` per reduced orbit. Its `position_id` is the representative start P.
   The same direction is mapped to every symmetry-equivalent start P through `jump_count`/`jump_mapping`.
5. A direction's `movement[i]` is the **fractional** displacement of the particle at node `i` from S0 to S2,
   used for tracking. The compiler converts each movement to Cartesian with the validated cell geometry,
   sums the Cartesian movements into `pathVector`, and stores both the fractional `movement` array and the
   Cartesian `path_vector`. Directions are deterministic and independent of the simulation field.

#### 8.2 State triplets and conservation

For each direction, enumerate the state-change combinations from the chain groups:

- **Stable node:** choose one state change from its group. S0 receives the donor particle; S2 receives the
  acceptor particle. A void donor is valid only for an authored explicit reverse vacancy pair.
- **Unstable node:** S0 and S2 are **void**; S1 receives the group's single `[D, ∅]` donor. The group must
  contain exactly one such pair (Step 04 `TRN026`).
- **S1:** stable nodes are void; unstable nodes carry the transition species. MMC has no S1; store `0` and
  document that the MMC solver ignores it.
- **Mass conservation:** the multiset of non-void particle symbols in S0 must equal S2. Charge-transfer
  mechanisms preserve the carrier symbol multiset across the full path; a single-site species change without
  a balancing change is rejected.
- **Charge conservation:** total charge of S0 equals S2 within `1e-9` elementary charge. The formal carrier
  may change (polaron hop), but the total must not.
- Rejected combinations are counted by reason (`mass`, `charge`, `invalid-state`) and never emitted.
- If a transition produces no valid rule, emit `CMP005` and fail compilation.

#### 8.3 Forward, inverse, and dependent rules

- **Forward rule:** one rule per accepted `(collection, direction, state0, state1, state2)` combination.
- **Inverse rule:** only when `chain.GeneratesReverse` is true (validated by Step 04 to be exactly
  `Metropolis2` and `Migration3`). Swap S0/S2, reverse the path/direction, and emit a rule unless the reverse
  is an authored explicit `ReverseTransitionId` transition. An explicit reverse transition generates its own
  rules and is never auto-generated here.
- **Dependent rule:** a rule instance for a symmetry-equivalent start P/direction that shares the canonical
  state triplet with the representative but is needed because the solver selects by position. Emit it,
  deduplicate it by the full rule outcome, and count it as `dependent`.
- **Deduplication key:** `(collectionIndex, state0, state1, state2, movement, chargeTransportVector,
  trackerOrder)`. If two rules in one collection share `state0` but differ in any of those fields, emit
  `CMP004` with both transition/rule IDs. If they are identical in every field, merge them into one rule
  referenced by both directions. This is the compile-time guarantee behind Step 16's per-collection
  `StateCode0 → rule` lookup.
- **Rule IDs.** The persisted `.msl` `rule_id` is a dense `u32` assigned in canonical order. The stable
  `RuleGeometryId` is a domain-separated SHA-256 over `(transitionId, chain pattern, canonical representative
  direction encoding, state0, state1, state2)`. The representative direction is the lexicographically smallest
  direction among merged identical-outcome directions. Step 09 derives its template rule-set identity from the
  stable transition/state/direction inputs, not from the dense index.
- **Frequency.** Every rule carries `DefaultFrequencyHz = 1e13`. Step 09 replaces it with the parameterized
  absolute value; Step 11 writes it as the model payload's `frequency_hz`. Step 16 derives any per-job
  normalization/pre-check factor from `frequency_hz` and the job's `max_attempt_frequency_hz`; the factor is
  not a model payload field.
- **Charge transport vector.** The compiler computes `rule.ChargeTransportVector` from the
  charge-weighted Cartesian movement along the path (see §8.4). Static S2 corrections are deferred: v1 stores
  `null`/NaN and uses Step 15's dynamic S2 path.
- **Derived flags.** Association, dissociation, vehicle, and charge-transfer classification is derived from
  the state triplet and linkers for the report/JSON inspection only. The solver does not consume these flags
  in v1; Step 04 deliberately does not persist `IsAssociation`. A test pins the derivation so it cannot
  silently disagree with the rules.

#### 8.4 Charge-transport field convention

Step 08 supplies a per-rule **charge transport vector**; it does not collapse the field work into a scalar.
The vector is the difference of the charge-weighted endpoint moments at each path node, not a particle-identity
pairing sum. This is exact for a charge-state change at a fixed site (for example A⁻ → A), where the particle
does not move but its charge changes:

```text
For each path node i:
  deltaCharge_i = q(S2[i]) - q(S0[i])                      (void has charge 0)
  x_i           = Cell.ToCartesian(absolute path node i)    (Å)
chargeTransportVector = Σ_i deltaCharge_i * x_i             (e·Å)
pathVector            = Σ_i Cell.ToCartesian(movement_i)    (Å; display/tracking only)
field                 = -0.5 * (|E| / kT) * (chargeTransportVector · ê)
```

A node whose charge is unchanged contributes nothing; void contributes no charge. For a single charge `q`
moving by `Δx`, the vector is `qΔx`. For a vacancy hop, the ion moves once and the void contributes nothing, so
the sign is correct. For an exchange of two identical ions the net charge transport is zero; for an exchange of
different charges it is non-zero. A particle that changes charge at a fixed node contributes through the
endpoint-moment difference even though its movement is zero. The dot product is exact for non-collinear
multi-site mechanisms. `pathVector` is display/tracking only and is not used to derive the field. The Å/V/m unit
conversion is applied by Step 15's existing field convention. Tests pin a single charge, a vacancy, an
identical-ion exchange (zero), a distinct-charge exchange (non-zero), a charge-state change at a fixed site, and
a non-collinear multi-site path.

The `TransitionPayloadDtos.cs` and `CompiledTransitionModel.cs` XML comments still say “particle-identity”;
that source-doc correction is assigned to Step 13 feature 4 (which touches the transition payloads), not to
this planning change.

### 9. Simulation encoding and derived solver intent

For each authored simulation (sorted by `SimulationId`):

- **KMC collections.** One `CompiledJumpCollection` per distinct kinetic transition used by any KMC
  simulation, shared across simulations. The collection carries the stable transition ID, mobile particle
  mask, rule IDs, and direction IDs.
- **MMC collections.** One collection per distinct Metropolis transition. Directions are two-site exchange
  paths; the partner cell is selected uniformly over the supercell by Step 17. No field, pre-run, S1, or
  trackers.
- **Jump directions.** `object_id` (stable SHA-256), representative `position_id`, `collection_id`, relative
  `path` encodings, fractional `movement` per node, Cartesian path vector/length, and inverse flag. Negative
  cell offsets are relative-path coordinates; the solver normalizes them with `ToSupercellOffset`.
- **Jump rules.** Dense `rule_id`, stable `rule_geometry_id`, collection ID, `state0/1/2`, default frequency,
  `chargeTransportVector`, static correction (null), tracker-order code, and flags. State codes use 6 bits per
  path node, node 0 least significant, unused bits zero. `tracker_order` uses `0xFF` for a non-mobile node.
- **Jump count/mapping.** `jumpCount[position][particle]` is the number of selectable directions for that
  start state; `jumpMapping[position][particle]` lists the direction indices in relative-slot order. A
  direction is listed only when at least one rule can start from that particle at that position. Zero means
  the site is blocked for that particle.
- **Selection optimizers.** Step 08 exposes the compiled site, position, environment, and selection masks; it
  does not interpret authored optimizers. Step 10 expands job-level optimizers to compiled `(position,
  particle)` pairs, computes the dual-DOF predicate, and reports removed start options. Optimizers are not
  part of the compiled model signature.
- **MMC interaction range.** `interaction_range = [maxCellA, maxCellB, maxCellC, 0]`, derived from the maximum
  stable/unstable cutoff and the cell interplanar spacings, plus one cell for boundary images. Step 17 uses it
  to bound local updates.
- **Derived intent.** `CompiledSolverIntent` is stored per compiled simulation:

```csharp
public sealed record CompiledSolverIntent(
    SimulationKind Kind,
    bool UseDualDofCorrection,
    bool UsePrerun,
    long PreRunMcsp);
```

  - `Kind` is the authored simulation kind.
  - `UsePrerun = Kind == Kmc && PreRunMcsp > 0`.
  - `UseDualDofCorrection = Kind == Kmc` by default. This matches the original job builder, which sets
    `UseDualDofCorrection` for every KMC job before the post-build selection optimizer runs. Step 08 does not
    infer the flag from the mechanism shape (`Migration3` versus vehicle/complex). Step 10 owns the optional
    selection-optimizer override and must record and test any rule that clears the flag; until that rule is
    proven, every KMC job keeps the original default.
  - The `.msl` flag word is **not** composed here. Step 10 copies the intent to `JobDescriptor`; Step 11 calls
    `Mocassin.Packages.Jobs.SolverJobFlagsMapper.Compose` with the authored `ExecutionFlags`, kind,
    pre-run MCSP, and the dual-DOF boolean. `Mocassin.Compilation` does not reference `SolverJobFlags`.

### 10. Compiled model and data contract

The internal result is a pure `Mocassin.Compilation` record:

```csharp
public sealed record CompiledModel(
    int FormatVersion,                 // 1
    string ModelSignature,             // geometry/rules/encoding signature, Step 08
    string SymmetryContractVersion,    // "symmetry-1"
    string SpaceGroupDataVersion,
    string SpaceGroupDataSha256,
    CompiledParticleTable Particles,
    CompiledStructureModel Structure,
    CompiledEnergyModel Energy,
    CompiledTransitionModel Transitions,
    ImmutableArray<CompiledSimulation> Simulations);
```

- `CompiledParticleTable`: symbols, charges, vacancy flags, count; index 0 is void.
- `CompiledStructureModel`: cell vectors, P1 positions, site table, environments, interaction range, tracker
  counts.
- `CompiledEnergyModel`: pair tables and cluster tables. The authored site-energy background is **not**
  compiled here; Step 09 resolves it per `[position][particle]` and Step 11 persists it (see the post-step
  correction note).
- `CompiledTransitionModel`: collections, directions, rules, jump count/mapping, tracker definitions and
  mappings.
- `CompiledSimulation`: simulation ID, kind, active collection indices, runtime default settings snapshot,
  `CompiledSolverIntent`, tracker models.

**Persisted in `.msl` (Step 11):** the compiled geometry/rules/encoding, the parameterized energy/frequency
values, the symmetry contract/catalog versions, and both signatures. The persisted payload is the
`ParameterizedModel` contract DTO; `Mocassin.Compilation` never references that DTO.

**JSON-inspectable only (not persisted):** stable `GeometryId`/`RuleGeometryId` strings, cluster expansion
maps, pair equivalent-image lists, input hashes, per-stage timings, diagnostic counts, cache statistics,
derived association/vehicle flags, compiler version, and the index→stable-ID mapping. These are emitted by
`CompiledModelJson.Write` for tests, diagnostics, and future CLI inspection. They are not part of `.msl`.

**Composed later:** parameterization values and the parameterization signature (Step 09), job/lattice/settings
and selection optimizers (Steps 10–11), the `SolverJobFlags` word (Step 11), and mutable tracker state/`.mcs`
metadata (Step 18).

**Contract changes recorded in [`02-data-contracts.md`](../02-data-contracts.md) §3.3:**

- Add top-level `model_signature` (Step 08 geometry/rules/encoding signature). Keep `signature` as the Step 09
  parameterization signature, which is the `models.signature` uniqueness key and the `.mcs`
  `metadata.modelSignature`.
- Clarify `jump_rules.frequency_hz` is the absolute parameterized value written by Step 09/11; Step 08's
  internal default frequency is not persisted as a second runtime source. Per-job normalization factors are
  derived by the solver, not stored in the model payload.
- Clarify `jump_directions.movement`/`path_vector`/`length` are the authoritative path movement; the
  direction stores no field scalar. `jump_rules` stores a Cartesian `charge_transport_vector`; Step 15
  computes the field energy from its dot product with the normalized field direction.
- Clarify `simulations.tracker_models` are **definitions** (global/static index maps), not mutable tracker
  values.
- Clarify `interaction_range` is the MMC local-update cube and `structure.environments` carries pair/cluster
  links and update particle IDs.

### 11. Indexing, stable IDs, signatures, and determinism

#### 11.1 Dense index rules

- `ParticleIndex`: 0 = void, then ascending `ParticleId.Value`.
- `PositionIndex`: canonical P1 order.
- `SiteIndex`: ascending `CellSiteId.Value`.
- `PairTableIndex`: canonical shell order `(distanceBucket, targetP, cellA, cellB, cellC)`, then center/
  partner site index; one table per distinct `EnergyId`.
- `ClusterTableIndex`: ascending `ClusterId.Value`, then canonical geometry hash and canonical occupation
  code.
- `TransitionIndex`: kinetic transitions ascending `KineticTransitionId.Value`; Metropolis transitions
  ascending `MetropolisTransitionId.Value`.
- `JumpCollectionIndex`: transition order; one collection per transition.
- `JumpDirectionIndex`: collection order, then canonical relative-path order.
- `RuleIndex`: collection order, then `(state0, state1, state2)` and the representative direction.
- `GlobalTrackerIndex`: `(collectionIndex, particleIndex)` ascending, excluding void.
- `StaticTrackerIndex`: `positionIndex * (particleCount - 1) + (particleIndex - 1)`, excluding void.
- All indices are dense from 0, with no gaps or reuse. Lookup maps are frozen after signing and never mutated.
  Dense indices are valid only inside one compiled package and are recorded with the model signature.

#### 11.2 Stable IDs

- Pair `GeometryId` and shared `EnergyId` are the Step 05/08 content hashes; they survive recompilation while
  the geometry is unchanged.
- Cluster energy ID is `SHA-256("mocassin/cluster-energy-id/v1" || clusterId || centerSiteId || canonical
  geometry hash)`.
- Rule `RuleGeometryId` is `SHA-256("mocassin/rule-geometry-id/v1" || transitionId || pattern ||
  canonical representative direction || state0 || state1 || state2)`. The representative is the
  lexicographically smallest direction among merged identical-outcome rules; it is stable because the merge
  key is the full rule outcome.
- Stable IDs are used by Step 09 templates, JSON inspection, and diagnostics, never as table indices.

#### 11.3 Model signature and Step 09 boundary

```text
CompiledModel.ModelSignature
  = SHA-256("mocassin/model-signature/v1" || 0x00 || canonical compiled projection)

ParameterizedModel.Signature
  = SHA-256("mocassin/parameterization-signature/v1" || 0x00 ||
            canonical(CompiledModel.ModelSignature, energy/frequency values))
```

- The Step 08 projection covers particles, structure/P1, environments, pair/cluster geometry and table
  shapes, transition rules, jump encoding, tracker definitions, active-collection selection, indexing rules,
  the symmetry contract version, and the catalog data version/SHA-256.
- It **excludes** runtime simulation settings (temperature, MCSP, write-out, field, pre-run, break settings,
  job count, seed), parameterization values, timings, diagnostics, file names, and cache state.
- Step 08 defines the projection field order and types in the Step 08 feature and mirrors ADR 0010's rules
  (structs as arrays in fixed order, fixed-width integers, float64, normalized `-0.0`, domain separation, no
  compression/timestamps). Step 08 finalizes the **existing** `signatures.model` manifest entry
  (`ModelSignatureV1`/`model-v1`) and its golden vector; it does not add a second `compiledModel` signature
  entry, because the generator requires exactly the `model`, `parameterization`, and `lattice` set. Step 11
  reuses the model projection for the stored payload and cross-language vectors. The Step 08 encoder is
  hand-coded in `Mocassin.Compilation` because `Compilation` cannot reference `Mocassin.Contracts`.
- The projection layout is:

  ```text
  [
    format_version,
    symmetry_contract_version,
    space_group_data_version,
    space_group_data_sha256,
    particles,
    structure,
    energy,                  # table IDs, geometry, codes, mappings; not energy values
    transitions,             # collections, directions, rules (defaults, not parameter values)
    simulation_encodings     # id, kind, active collection indices, tracker definitions
  ]
  ```

  Runtime simulation settings and tracker values are not in this layout.
- Step 09 treats `ModelSignature` as an opaque stable string and stores it in `ParameterizationTemplate`.
  It computes the parameterization signature in `Mocassin.Contracts` over the full parameterized payload.
  Step 11 stores `models.signature` = parameterization signature and `models.payload.model_signature` = Step 08
  signature.
- A model-signature change invalidates templates and parameterizations; a parameterization-only change
  changes the package model signature but not the compiled-model signature. Adding or removing a simulation,
  or changing a simulation's active-transition set, changes the compiled simulation encoding and therefore the
  model signature. Changing only runtime settings (temperature, MCSP, field, pre-run, break settings, jobs,
  seeds) does not change the model signature.

#### 11.4 Determinism

- Same `Project` + options + catalog/contract versions → byte-identical `CompiledModelJson` and identical
  `ModelSignature` across runs and supported platforms.
- Canonical ordering, invariant-culture formatting, normalized doubles, and stable sorting are mandatory.
- Distances are exact-grid-derived and used only for `1 fm` buckets and display; the signature records buckets
  and indices, not raw computed distances.
- All projected floating-point values are computed with deterministic scalar arithmetic and quantized to 12
  decimal places using round-half-away-from-zero on the binary64 product before persistence and signing. No parallel floating-point reduction, FMA-dependent accumulation, or
  hash-order iteration affects the output.
- Golden JSON/signature fixtures are generated by the real compiler and checked for freshness. Windows/macOS
  golden runs are manual platform gates; Ubuntu is automatic.

### 12. Caching and invalidation

- **Default lifetime:** one `Compile` call. The compiler creates a bounded stage cache, uses it for repeated
  point groups, pair shells, cluster reductions, expanded directions, and rule lookups, and releases it on
  return.
- **Optional caller-owned cache:** `CompilationOptions.Cache` accepts an `ICompilationCache`. Step 12 may keep
  one cache per build/session to avoid recomputing unchanged stages across builds. Step 08 never uses a static
  or process-wide cache. A caller-supplied cache is the caller's lifetime and must be thread-safe.
- **Thread safety:** cache entries are immutable records; the implementation uses a concurrent dictionary
  with single-flight insertion and a bounded capacity. Cache insertion order never affects results.
- **Keys:** `domain | catalog version | catalog SHA-256 | symmetry contract version | stage-specific content
  hashes`. Stage-specific inputs:
  - structure: structure hash;
  - environments/pairs: energy hash + structure hash;
  - clusters: energy hash + structure hash + cluster IDs + allowed-occupation hash;
  - rules: active-transition-set hash + transition closure hashes + structure hash + particle order;
  - encoding: rule hash + simulation hash + energy hash + structure hash;
  - indexing: structure hash + particle hash + energy hash + pair/cluster table hashes + rule hash + simulation
    hash;
  - signing: canonical projection bytes (runtime settings excluded).
- **Active-transition-set hash.** `ProjectHasher.ComputeSimulationSelection(Project)` (new, Core) hashes the
  canonical simulation IDs, kinds, and authored-order transition references. It is the cache-key input that
  makes adding/removing a simulation reference change the rule set. The compiler report exposes it as
  `InputHashes.ActiveTransitions`; `InputHashes.Simulations` hashes the full simulation section.
- **Invalidation graph.** `Resolve` always runs and is not a cache stage. The remaining graph is:

  | Edit | Rebuilds | Reuses |
  | --- | --- | --- |
  | Structure/particles | Structure, Environments, Pairs, Clusters, Rules, Encoding, Indexing, Signing | nothing |
  | Energy environments/clusters/filters | Environments, Pairs, Clusters, Encoding (links), Indexing, Signing | Structure, Rules |
  | Site-energy values only | None in `CompiledModel` (Step 09 re-applies the background and parameterization signature) | All compiled stages/content; the energy hash may cause cache misses but not content changes |
  | Transitions/state changes/chains | Rules, Encoding, Indexing, Signing | Structure, Pairs, Clusters |
  | Simulation active-transition set changes | Rules, Encoding, Indexing, Signing | Structure, Pairs, Clusters |
  | Simulation runtime settings only | Encoding, Indexing (runtime-default snapshot) | Structure, Pairs, Clusters, Rules, Signing |
  | Catalog or symmetry contract | all cached stages | nothing |

- Cache statistics (`hits`, `misses`, `capacity`) are reported but never signed. A cached entry is accepted
  only when its key echo, format version, array lengths/index bounds, and cached content hash match the current
  key; a mismatch recomputes the stage and emits `CMP012`. Tests inject a wrong-version or wrong-shape entry
  directly into the caller cache; normal operation never emits `CMP012`.

### 13. Diagnostics, report, cancellation, parallelism, and performance

#### 13.1 `CMP` diagnostics

Owner prefix: `CMP`, owner “Step 08 — model compilation pipeline”. Codes are stable API.

| Code | Severity | Rule | Path |
| --- | --- | --- | --- |
| `CMP001` | Error | A required compilation precondition failed; source diagnostics are added to `Diagnostics` and their paths to `Related`. | object-specific |
| `CMP002` | Error | The structure cannot be encoded/P1-expanded for compilation. | `structure` |
| `CMP003` | Error | Pair or cluster geometry cannot be resolved during compilation. | `energy` / `cluster` |
| `CMP004` | Error | Two rules in one collection share `state0` but differ in outcome (ambiguity). | `kineticTransition` / `rule` |
| `CMP005` | Error | A transition produces no valid rules after conservation filtering. | `kineticTransition` |
| `CMP006` | Error | A configured compilation limit (rules, tuples, tables, indices, memory) is exceeded. | `model` / collection |
| `CMP007` | Error | A transition path cannot be expanded into compiled directions. | `kineticTransition` |
| `CMP008` | Error | Mass/charge conservation cannot be evaluated (non-finite charge or missing particle). | `kineticTransition` / `stateChange` |
| `CMP009` | Warning | A transition is not referenced by any simulation and was not compiled; suppressed when `EmitUnusedTransitionWarnings` is false. | `kineticTransition` or `metropolisTransition` |
| `CMP010` | Warning | The advisory rule/table budget (`AdvisoryRuleCount`/`AdvisoryEnergyTableBytes`) is exceeded but the hard limit is not. | `model` |
| `CMP011` | Error | The compiled model cannot be signed (canonical projection failure or missing provenance). | `model` |
| `CMP012` | Warning | A cached stage result failed its integrity check and was recomputed. | `model` |

- `Mocassin.Compilation.Diagnostics.CompilationDiagnosticCodes` owns the constants.
  `CompilationDiagnosticCatalog` exposes `DiagnosticDescriptor` values. `DiagnosticCatalog.Default` in
  `Mocassin.Packages` concatenates Core + Symmetry + Compilation + Step 07 descriptors;
  `Mocassin.Packages -> Mocassin.Compilation` is already an allowed dependency direction.
- `Mocassin.Compilation.Tests` proves every `CMP` constant is registered exactly once. The package-level
  catalog test proves the composed catalog is the union and has no duplicates. The docs-drift test requires
  every catalog code in `docs/reference/diagnostics.md`; the `compile-diagnostics-and-report` feature adds the
  CMP anchors and rows there. Step 09–12 codes are not stubbed.
- `CMP001` is a wrapper, not a duplicate rule: it points at the source `STR`/`TRN`/`ENM`/`SIM` diagnostic and
  never replaces the original severity/path.
- `CMP004` includes both conflicting transition/rule identities in the message and their paths in `Related`;
  `CMP005` includes the rejection counts by reason in the message and the report.

#### 13.2 Report

`CompilationReport` contains:

- `CompilerVersion`, `ModelSignature`, `SymmetryContractVersion`, `SpaceGroupDataVersion`,
  `SpaceGroupDataSha256`;
- `InputHashes` for model, structure, energy, transitions, simulations, and the active-transition set;
- `Counts`: particles, P1 positions, stable/unstable positions, sites, environments, pair images, pair
  geometries, pair energy tables, chiral pairs, clusters, cluster tables, cluster occupations, transitions,
  collections, directions, rules, forward/inverse/dependent rules, rejected rules split by reason
  (`RejectedByMass`, `RejectedByCharge`, `RejectedByInvalidState`), global trackers, static trackers per cell,
  and estimated energy-table bytes;
- per-stage `Status`, `DiagnosticCount`, and `Elapsed`;
- `Cache` hits/misses/capacity;
- a human-readable `Summary` suitable for CLI/GUI.

Timings, cache statistics, and summary are excluded from the signature and from golden byte comparisons except
where tests explicitly compare counts.

#### 13.3 Cancellation and parallelism

- Check cancellation at every stage boundary, before each top-level position/cluster/transition/simulation,
  and around each atomic Symmetry call. The Step 02/04/05 services (`IEnergyGeometrySampler`,
  `PairReduction`, `ClusterReduction`, `TransitionPathResolver`) take no `CancellationToken`; one call is
  atomic and cannot be interrupted. The 250 ms responsiveness target therefore applies between checks, not
  inside a single atomic reduction/radial-search/path call. Adding tokens to those APIs is a later change.
- `MaxDegreeOfParallelism = 1` disables parallelism. Independent per-position pair/cluster work and
  per-transition direction/rule enumeration may run in parallel with local buffers. Merge and sort before
  assigning indices. Never parallelize floating-point reductions or signature projection.
- Bounded allocations only: enforce `MaxRuleCount`, `MaxClusterTupleCount`, and `MaxEnergyTableBytes` before
  allocation; compute estimates with saturating `long` arithmetic and trip `CMP006` on saturation or breach.
  A breach is a diagnostic, not an OOM.

#### 13.4 Performance

Benchmark laptop class: 8 physical x86-64 cores, 32 GB RAM, SSD, .NET 10 Release, no debugger, Ubuntu.

- ceria sample: compile `< 1 s`;
- 10k P1-position model: compile `< 30 s`, peak managed memory `< 2 GiB`;
- cancellation responsive within 250 ms of the next stage/top-level check; a single atomic Symmetry
  reduction/path call may exceed this, and that is an accepted limitation until those APIs take a token;
- no per-feature CI; run `Mocassin.Benchmarks/Compilation` locally and record the hardware and result in the
  step page.

### 14. `mocassin compile` ownership and JSON inspection

- **Step 08 ships a library and test harness only. Step 12 owns `mocassin compile` and the full CLI.** This
  preserves the purity boundary and avoids a second command parser/orchestration path that Step 12 would
  replace. The data contract's `mocassin compile <project> -o <compiled-model.json>` command is implemented in
  Step 12.
- Step 08 provides `CompiledModelJson.Write(CompiledModel)` (pure, returns UTF-8 bytes/string) as the
  canonical `compiled-inspection-v1` projection. It includes the stable IDs, counts, index maps, and
  provenance that are not persisted in `.msl`; it is not a format contract and is never written by the
  compiler.
- Step 12's `compile` command loads and validates the project with `ProjectValidationPipeline`, calls
  `IModelCompiler.Compile`, writes the JSON inspection if requested, and maps failures to the build error
  model. It does not reimplement compiler rules.

## Implementation tasks

- [x] `compiler-core`: add `IModelCompiler`, `ModelCompiler`, `CompilationOptions`, `CompilationResult`,
  `CompilationStatus`, stage/result records, cancellation, option validation, and the stage orchestrator.
  **Done — squash `1deb182`; closeout suite 999 tests.**
- [x] `structure-and-pair-compilation`: resolve the structure, build the P1 context, sample environments and
  pairs with the shared Step 05 kernel, allocate pair tables, and compile chiral/shared energy links.
  **Done — squash `4280773`; P1/pair/chiral tests in `Mocassin.Compilation.Tests`.**
- [x] `cluster-compilation`: resolve cluster geometry, call `ClusterReduction`, allocate canonical cluster
  tables, build expansion maps, and link cluster byte positions. **Done — squash `dee2034`; canonical
  permutation/expansion tests.**
- [x] `transition-rule-compilation`: expand directions, build S0/S1/S2, conservation filtering,
  forward/inverse/dependent rules, deduplication, ambiguity detection, rule IDs, and charge transport
  vectors. Add the additive public `TransitionPathResolver.TryResolvePoint`/`TryResolvePath` API and Step 04
  path-resolver tests; do not duplicate private nearest-image logic. **Done — squash `26366aa`; forward and
  inverse rules are covered by `TransitionCompilationTests`, explicit reverse transitions by
  `ExplicitReverseCompilationTests`, and unidirectional complex mechanisms by `ComplexMechanismTests`.
  Residual: equivalent-start orbit reduction and `DependentRules` emission are the accepted audit m1 gap,
  assigned to Step 29 (§9 release gate: every required parity item implemented or explicitly deferred).**
- [x] `simulation-encoding`: build collections, directions, rules, jump count/mapping, tracker
  definitions/mappings, MMC interaction range, selection-optimizer resolution, and derived solver intent.
  **Done — squash `fde55fd`; encoding/intent tests.**
- [x] `indexing-signatures-caching`: dense index assignment, stable IDs, canonical model-signature projection,
  stage cache, invalidation, and `CompiledModelJson`. **Done — squash `10966cf`; signature/cache/inspection
  tests.**
- [x] `compile-diagnostics-and-report`: `CMP` codes, `CompilationDiagnosticCatalog`, report/counts/timings,
  docs anchors, catalog completeness, and per-code tests. **Done — squash `6c59163`; `CMP001`–`CMP012`
  catalog/emission tests.**
- [x] `golden-models-and-closeout`: ceria/BaZrO₃/vehicle/interstitialcy golden compiled models and signature
  vectors, performance and cancellation tests, parity-matrix/status/docs closeout, and the audit request.
  Add a `Mocassin.Compilation` project reference and `Compilation/` benchmarks to `Mocassin.Benchmarks`, or
  record a different harness if the benchmark project is not the right fit. **Done — squash `86348bf`;
  closeout CI `43538703111` and audit record `Passed with notes` at `28e741f`.**
- [x] Finalize the existing `signatures.model` (`ModelSignatureV1`/`model-v1`) manifest types and golden
  vector in `indexing-signatures-caching`; do not add a second signature entry. Step 11 reuses it for the
  stored payload and cross-language checks. **Done — squash `10966cf`; `model-v1` golden vectors are current
  and Step 11 reuses the entry.**
- [x] Add SPDX headers, `PublicAPI.Unshipped.txt` entries, and ApiCompat updates for every new public type,
  including the additive `Mocassin.Symmetry.TransitionPathResolver.TryResolvePoint`/`TryResolvePath` members and
  the new `Mocassin.Compilation` public surface. **Done — `./tools/verify.sh api` and the SPDX check are green
  at closeout.**
- [x] Update `docs/reference/diagnostics.md`, `docs/development/compilation-pipeline.md`,
  `docs/development/architecture.md`, `docs/development/design-decisions.md`, `docs/planning/03-quality-strategy.md`,
  `docs/development/testing-and-parity.md`, and the parity matrix in the same features. **Done — `docs` gate
  green at closeout; dependent-rule status corrected in the XC-04 maintenance pass.**

## Acceptance criteria

- [x] Ceria compiles to the expected P1 positions, pair sets/tables, cluster sets, and oxygen migration rules.
- [x] A 2-site MMC model compiles forward and inverse exchange rules.
- [x] A 3-site migration compiles forward and inverse rules, including a chiral case, and keeps the KMC
  dual-DOF default.
- [x] A 5-site vehicle compiles unidirectional rules only, with no automatic inverse; as a KMC simulation it
  keeps the dual-DOF default unless a documented Step 10 optimizer rule clears it.
- [x] Rule generation enforces unstable positions void in S0/S2, stable positions void in S1, and the
  documented mass/charge conservation rules; rejected counts are reported.
- [x] Charge transport vectors give `qΔx` for a single charge, the correct vacancy sign, zero for an
  identical-ion exchange, and non-zero for a distinct-charge exchange or a non-collinear multi-site path.
- [x] Rule ambiguity is detected as `CMP004` with both conflicting transitions/rules.
- [x] Every compiled direction/rule is reachable through `jump_count`/`jump_mapping`; every selectable
  `(position, particle, relative slot)` has at most one rule for its `state0`.
- [x] Tracker definitions/mappings are emitted for KMC and absent for MMC; a 3-site migration's
  `tracker_order` follows particle identity.
- [x] `CompiledModel.ModelSignature` is stable across recompilation, excludes runtime settings and
  parameterization values, and changes exactly when compiled geometry/rules/encoding changes.
- [ ] Compiling the same project twice yields byte-identical `CompiledModelJson` and signature on Ubuntu,
  Windows, and macOS.
  **Open — accepted audit note N2:** Ubuntu determinism is verified, but Windows/macOS were not run under the
  manual platform policy. Step 29's three-OS CI acceptance criterion closes this; the accepted risk is carried
  in the approval summary.
- [x] Stage caches reuse unaffected stages after a single-domain edit and never change the output.
- [x] Ceria compiles in `< 1 s`; a 10k-position model compiles in `< 30 s` with `< 2 GiB` managed memory.
- [x] Cancellation returns no partial model and leaves the project unchanged.
- [x] Every `CMP` code has a catalog descriptor, documentation anchor, and emission test.
- [x] Documentation written.

**Completion evidence (2026-11-18):** every criterion was exercised by the 999-test closeout suite and the
Step 08 audit; the audit returned `Passed with notes` at `28e741f` after the closeout CI run `43538703111` and
the `f91fc63` merge. The feature log above records the squash commit for each task. The only open residual is
audit finding m1 (equivalent-start orbit reduction / `DependentRules` emission), which is accepted for Step 08
and assigned to the Step 29 release gate (see the parity matrix); it does not make any ticked criterion false.

## Documentation

- `docs/development/compilation-pipeline.md` — normative stage order, algorithms, signatures, caches,
  tracker boundary, and diagnostics ownership.
- `docs/development/architecture.md` — `Mocassin.Compilation` layout, purity, dependency direction, and
  Step 11 mapping.
- `docs/development/design-decisions.md` — synchronous compiler, validation boundary, cache lifetime,
  signature boundary, derived intent, tracker split, and `CMP` ownership.
- `docs/planning/02-data-contracts.md` §3.3 — compiled payload, `model_signature`, inspection-only fields,
  tracker definitions, and the Step 12 `compile` command ownership.
- `docs/planning/03-quality-strategy.md` §9.3 and `docs/development/testing-and-parity.md` — compilation
  tests, oracles, determinism, caches, and performance.
- `docs/reference/diagnostics.md` — `CMP` rows and anchors, added with the diagnostics feature.
- `docs/physics/energies.md` and `docs/physics/symmetry.md` — the scientific conventions the compiler applies
  (pair/cluster tables, chirality, charge transport, conservation).
- `docs/glossary/notation.md` — compiled indices, state codes, and rule identities.

## Tests

| Test | Assertion |
| --- | --- |
| P1 positions | Compiled positions match the Step 02 encoder for all samples |
| Particle mapping | Compiled 0=void + authored-ascending indices map explicitly from `ParticleId`; preview matrix order never leaks into state codes |
| Pair sets | Counts, distances, multiplicities, chiral links, and table order match brute force |
| Pair bucketing | Symmetry-equivalent images share a bucket; cutoff-boundary images are included with the `1 fm` margin |
| Pair preview cross-check | Compiled pair-image/unique-table counts equal `EnergyCostPreview`/`TryCreatePairPreview` |
| Clusters | Canonical tuples/codes match exhaustive permutation oracles; expansion maps are complete |
| Cluster tables | Rows, code order, particle mapping, and zero defaults are exact |
| Rules | S0/S1/S2, stability roles, conservation, rejection counts, forward/inverse/dependent counts |
| Charge transport | Single charge -> `qΔx`; vacancy -> correct sign; identical-ion exchange zero; distinct-charge exchange and non-collinear path non-zero |
| Rule ambiguity | Duplicate `state0` with different outcomes fails with `CMP004` and both identities |
| Vehicle | Unidirectional only; no inverse generation; KMC still carries the dual-DOF default |
| Intent | KMC always sets `UseDualDofCorrection` by default; Step 10 may clear it only with a recorded optimizer rule; MMC false |
| MMC | Two-site exchanges; no S1/field/pre-run/trackers; MMC intent |
| Encoding | Jump count/mapping/tracker tables are dense, consistent, and reachable |
| Trackers | Global/static definitions match indices; 3-site `tracker_order` follows particle identity |
| Signature | Known vectors; runtime settings and parameterization changes do not alter the model signature |
| Determinism | Byte-identical JSON/signature across runs; Ubuntu automatic, Windows/macOS manual golden |
| Cache | Recording cache proves the invalidation graph after a one-domain edit |
| Diagnostics | Every `CMP` code has severity/path/hint/catalog/docs coverage |
| Cancellation | Cancelled compile returns no model and does not mutate the project |
| Performance | Ceria `< 1 s`; 10k-position model `< 30 s`, `< 2 GiB` |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Rule combinatorics explode | Early conservation pruning, hard limits, cancellation, per-transition counts |
| Ambiguous `state0` lookup | Compile-time uniqueness check; `CMP004` with both identities |
| Pair/cluster reduction diverges from preview | Shared Step 05 kernel and explicit preview-count cross-check tests |
| Chiral handling | Reciprocal-orbit rule, shared `EnergyId`, explicit chiral/achiral tests |
| Signature boundary mixes geometry and values | Separate Step 08 and Step 09 signatures; tests prove runtime/parameter changes do not alter the model signature |
| Cache invalidation bugs | Content-hash keys, per-compilation default, recording-cache tests, no static cache |
| Charge-transport field convention | Particle-identity Cartesian vector; single-charge, vacancy, exchange, and non-collinear tests; no scalar projection |
| Step 08 is too large to audit | Owner explicitly waived the 4–6 pw/eight-feature split on 2026-11-12; features remain independently auditable, and the owner re-opens the split decision if the first three exceed 2.5 pw |
| CLI ownership duplication | Step 08 library only; Step 12 owns `compile`/`build` and orchestration |

## Done when

The compiler produces a deterministic, validated `CompiledModel` for every supported mechanism, with counts,
diagnostics, signatures, cache statistics, and a canonical JSON inspection that explain what was generated and
why. Step 09 can parameterize it, Step 10 can expand jobs, Step 11 can write it, and Step 12 can orchestrate the
build without reimplementing compiler rules.
