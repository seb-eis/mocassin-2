# Compilation Pipeline

Compilation is a pure, staged pipeline. Each stage has explicit inputs, outputs, and cache boundaries; no stage
performs file I/O or UI work. Step 02 owns the symmetry kernels, Step 04 the transition input, Step 05 the
energy input and shared sampling kernel, Step 06 the simulation input, and Step 07 the validation gate. Step 08
adds the compiler that composes those inputs into a deterministic `CompiledModel`.

## Stage order

```text
validated Project
  → resolve (catalog, setting, cell, encoder, sorted particles/sites, simulations)
  → structure / P1 expansion
  → environments and pair-image sampling
  → pair reduction, chirality, pair tables
  → cluster geometry and permissible reduction
  → transition direction expansion and rule generation
  → simulation encoding (collections, directions, rules, mappings, trackers, intent)
  → dense indexing and stable IDs
  → canonical model signature
  → report
```

Step 09 adds parameterization values and the parameterization signature; Step 10 adds jobs/lattices; Step 11
writes the `.msl` package and composes the `SolverJobFlags` word; Step 12 owns build orchestration and the
`mocassin` CLI.

## Compiler API and purity

```csharp
namespace Mocassin.Compilation;

public interface IModelCompiler
{
    CompilationResult Compile(
        Project project,
        CompilationOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

- Compilation is CPU-bound and synchronous. It returns a complete `CompilationResult` and takes a
  `CancellationToken`. Step 12 wraps it in the async build event stream. `Mocassin.Compilation` has no
  `Task`, file path, stream, serializer-to-file, or `Mocassin.Packages`/`Mocassin.Contracts` dependency.
- User-data problems produce `Diagnostic` values and `Status = Failed`; programmer errors, contract
  violations, and cancellation throw. Cancellation returns no partial model.
- The compiler assumes a project that passed `ProjectValidationPipeline`. It does not run the validation
  pipeline, does not consume `ValidationResult`, and does not expose an `IValidationStage`. Step 12 owns the
  Build validation stage and maps compilation failures to the build error model.
- Stage services may reject invalid input defensively; the compiler then emits `CMP001` and copies the source
  `STR`/`TRN`/`ENM`/`SIM` diagnostics into `Related` instead of re-authoring them.

## Stage details

### Resolve and structure

- Resolve the catalog setting with `IStructurePreviewService.TryResolve`, the cell with
  `CellGeometry.TryCreate`, and the encoder with `UnitCellVectorEncoder.TryCreate`.
- Reference sites are sorted by `CellSiteId` before encoding, so `ExpandedSite.ReferenceSiteIndex` is stable.
  Particle index `0` is void; authored particles are sorted by `ParticleId`.
- The canonical P1 order remains the Step 02 contract: expand, trim, exact-grid deduplicate, sort by
  fractional `(a,b,c)`, assign `P`.
- The compiled structure records the cell vectors, P1 positions `[cellA, cellB, cellC, P]`, owning site index,
  stability, allowed-particle mask, orbit membership, environments, interaction range, and tracker counts.

### Environments and pairs

- One stable environment plus effective unstable environments (authored or inherited stable cutoff).
- Pair sampling uses `IEnergyGeometrySampler`/`EnergyGeometrySampler` with unit-cell counts. That kernel owns
  radial search, `1 fm` buckets, the `cutoff + 1 fm` margin, filters, `G0` reduction, and chiral linking.
- The compiler allocates pair tables, maps directed geometries to shared `EnergyId` values, and re-sorts the
  tables into canonical shell order `(distanceBucket, targetP, cellA, cellB, cellC)`, then center/partner site
  index. Table order is not derived from the preview's display order.
- Pair tables are `n_particles × n_particles` row-major in eV. Invalid particle combinations are zero; the
  position's allowed-particle mask is authoritative.
- Step 08 does not call the preview service at runtime. A test cross-checks compiled pair-image, unique-table,
  cluster, and permutation counts against `EnergyCostPreview`/`TryCreatePairPreview`. This closes the Step 05
  consistency follow-up without adding a second implementation.

### Clusters

- Resolve the center and ordered offsets to canonical P1 positions and their stable reference sites.
- Call `ClusterReduction.Reduce` with the ordered full geometry and allowed particle indices. The reducer
  owns `G0`, `Gset`, `Gseq`, induced permutations, canonical tuples, 6-bit codes, and expansion maps.
- The center factor is mandatory; raw combinations are `centerParticleCount × Π(surroundingSetSize)`.
  Compute the raw count with saturating `long` arithmetic and emit `CMP006` before calling the reducer when it
  exceeds the configured limit or the reducer's fixed 1,000,000-tuple ceiling.
- Occupation codes use 6 bits per site, site 0 least significant, up to 9 sites. Particle indices must be
  `<= 63`.
- One cluster table is shared by symmetry-equivalent centers. It is keyed by the stable cluster energy ID and
  stores sorted occupation codes, energies (default `0.0`), and a center-particle row mapping.
- Cluster contributions are counted at the center. The environment link records the table and the byte
  position used for incremental code updates.

### Transition directions and rules

- `TransitionPathResolver` resolves the authored reference path to canonical `(P, cell offset)` positions.
  Point/path expansion uses the additive public `TryResolvePoint`/`TryResolvePath` API; the compiler does not
  duplicate the private nearest-image or tie logic.
- Expand every setting operation, re-resolve with `TryResolvePath`, deduplicate exact relative encodings, and
  reduce by the start position's `G0`. The canonical representative is the lexicographically smallest relative
  path.
- A direction stores the representative start P, relative path encodings, per-node fractional movement, the
  Cartesian path vector/length, and an inverse flag. The direction stores no field scalar; the rule carries the
  Cartesian charge transport vector used by Step 15.
- For each direction, enumerate state-change combinations:
  - stable nodes: donor in S0, acceptor in S2;
  - unstable nodes: void in S0/S2, transition species in S1;
  - stable nodes: void in S1;
  - MMC has no S1.
- Reject combinations that fail mass or total-charge conservation. Charge-transfer mechanisms must preserve
  the carrier-symbol multiset across the full path. Report rejected counts by reason.
- Forward rules are emitted for every accepted combination. Inverse rules are emitted only for
  `GeneratesReverse` chains (`Metropolis2`, `Migration3`) and never for an authored explicit reverse
  transition. Dependent rules are symmetry-equivalent instances required at other start positions. They are
  deduplicated by the full rule outcome
  `(state0, state1, state2, movement, chargeTransportVector, trackerOrder)`; two directions may share one
  rule only when every outcome field is identical.
- Rule lookup is guaranteed unambiguous: within one collection, a `state0` maps to at most one rule. A
  collision with a different outcome is `CMP004`.
- Rule IDs in the payload are dense `u32`; stable rule geometry IDs are domain-separated SHA-256 strings used
  by templates and inspection, not table indices.
- Default attempt frequency is `1e13 Hz`; Step 09 supplies parameterized values. Static S2 corrections are
  deferred; v1 stores `null`/NaN and uses the dynamic S2 path.
- The compiler computes the charge transport vector from the charge-weighted endpoint moments over the path
  nodes: `Σ_nodes (q(S2,node) - q(S0,node)) * x(node)`. For balanced particle counts this equals the
  particle-identity sum `Σ_p q_p * (x_p(S2) - x_p(S0))`; the moment form also covers charge-state changes.
  Step 15 computes `field = -0.5*(|E|/kT)*(chargeTransportVector·ê)`, which is exact for non-collinear paths
  and gives the correct single-charge/vacancy/exchange signs. The direction's `pathVector` is display/tracking
  only.
- Movement follows the physical species flow in path order. Identical species (including different charge
  states of one symbol) are paired in path order, so an interior stable spectator site can remain chemically
  unchanged while its particle still moves physically.

### Simulation encoding

- One collection per distinct transition used by a simulation, shared across simulations. KMC collections
  carry rules/directions; MMC collections are two-site exchanges with uniform partner-cell selection.
- `jumpCount[position][particle]` and `jumpMapping[position][particle]` list only selectable directions and
  resolve the solver's uniform direction selection. Zero means blocked.
- Tracker **definitions** and index mappings are compiled:
  - global: `(collection, particle)`, excluding void;
  - static: `position * (particleCount - 1) + (particle - 1)`, excluding void;
  - `tracker_order` per rule follows particle identity and uses `0xFF` for non-mobile nodes.
  Step 18 owns mutable tracker values, mobile-tracker assignment, and `.mcs` output. MMC has no trackers.
- MMC `interaction_range` is the `[maxCellA, maxCellB, maxCellC, 0]` local-update cube derived from the
  maximum cutoff and interplanar spacings.
- `CompiledSolverIntent` carries `Kind`, `UsePrerun = Kmc && PreRunMcsp > 0`, and
  `UseDualDofCorrection = Kind == Kmc` by default, matching the original job builder. Step 10 may clear it only
  through a documented and tested selection-optimizer rule. Step 10 copies the intent to `JobDescriptor`;
  Step 11 composes the `SolverJobFlags` word. `Mocassin.Compilation` never references `SolverJobFlags`.

## Compiled model and package boundary

The internal `CompiledModel` is a `Mocassin.Compilation` record with typed IDs and immutable arrays. It is not
the persisted DTO.

- **Persisted in `.msl`:** compiled geometry/rules/encoding, parameterized energy/frequency values, symmetry
  contract/catalog versions, and both signatures. Step 11 maps the internal model to the Contracts
  `ParameterizedModel` payload defined in the data-contract §3.3.
- **Inspection only:** stable geometry/rule IDs, expansion maps, equivalent images, input hashes, timings,
  diagnostics, cache statistics, derived flags, and the index→ID mapping. `CompiledModelJson` emits these as
  `compiled-inspection-v1`; it is not a `.msl` contract.
- **Composed later:** parameterization values/signature (Step 09), jobs/lattices/selection optimizers (Steps
  10–11), `.msl` flags (Step 11), tracking state/`.mcs` metadata (Step 18).

## Signatures

```text
CompiledModel.ModelSignature
  = SHA-256("mocassin/model-signature/v1" || 0x00 || canonical compiled projection)

ParameterizedModel.Signature
  = SHA-256("mocassin/parameterization-signature/v1" || 0x00 ||
            canonical(model signature + energy/frequency values))
```

- The Step 08 projection covers particles, structure/P1, environments, pair/cluster geometry and table
  shapes, transitions/rules, jump encoding, tracker definitions, active-collection selection, indexing rules,
  the symmetry contract version, and the catalog version/SHA-256.
- It excludes runtime simulation settings, parameterization values, timings, diagnostics, file names, and
  cache state.
- The projection follows ADR 0010 and the full nested `signatures.model` manifest schema: arrays in fixed
  field order, fixed-width `uint64`/`int64`, float64, normalized `-0.0`, domain separation, and no
  compression/timestamps. `Mocassin.Compilation` owns a small `CanonicalProjectionWriter` built on the
  centrally pinned `MessagePack` 3.1.12 package; it writes fixed-width scalars and forced canonical headers.
- All projected float64 values are quantized to 12 decimal places with round-half-away-from-zero on the
  binary64 product. Energy values, runtime settings, display distances, `pathLength`, and `StaticCorrection`
  (null in v1) are excluded.
- `tests/contracts/projections/*.bin` are the canonical byte fixtures for ceria KMC, ceria MMC, vehicle5,
  interstitialcy5, and BaZrO₃; `tools/contracts/check_model_projection.py` independently re-encodes them from
  the generated descriptor in `verify.sh contracts`. Step 11 reuses the same manifest entry and projection.
- Step 09 treats the model signature as opaque and computes the parameterization signature in
  `Mocassin.Contracts`. `models.signature` is the parameterization signature; `models.payload.model_signature`
  is the Step 08 signature.

## Caches and determinism

- Default stage caches are per compilation and released on return. `CompilationOptions.Cache` allows a
  caller-owned, thread-safe, bounded cache for repeated builds; there is no static/process-wide cache.
- Keys include catalog version/SHA-256, symmetry contract version, and stage-specific content hashes. The cheap
  resolve stage always runs and is not keyed by the whole-model hash. Pair/cluster work keys on energy +
  structure hashes; rules key on the active-transition-set hash + transition closures + structure; encoding
  keys on rules + simulations + energy links; indexing includes the energy and simulation hashes and rebuilds
  for site-energy or runtime-only edits; signing uses the canonical projection and excludes energy values and
  runtime settings.
  `ProjectHasher.ComputeSimulationSelection` hashes the referenced simulation transition set so adding/removing
  a reference invalidates rules.
- Editing structure invalidates everything; editing energy rebuilds environments/pairs/clusters/encoding/indexing
  and reuses structure and rules, while site-energy-only edits reuse signing because values are excluded;
  editing transitions rebuilds rules/encoding but reuses geometry; changing a simulation's active-transition
  set rebuilds rules/encoding, while changing runtime settings rebuilds the encoding defaults and the indexed
  runtime snapshot but not the signature. Tests use a recording cache to prove the graph.
- Output order is fixed independently of parallelism, cache insertion, or hash iteration. No parallel
  floating-point reductions. All projected floating-point values are quantized with
  round-half-away-from-zero on the binary64 product before persistence/signing.
- Same project/options/versions produce byte-identical `CompiledModelJson` and the same signature across runs
  and supported platforms. Golden fixtures are generated by the real compiler.

## Diagnostics

- Compilation reuses `Diagnostic`, `ModelPath`, `DiagnosticBag`, and `DiagnosticDescriptor`.
- `CMP001`–`CMP012` are owned by `Mocassin.Compilation.Diagnostics`. `CompilationDiagnosticCatalog` registers
  them; `DiagnosticCatalog.Default` composes Core + Symmetry + Compilation + Step 07.
- `CMP001` wraps a failed precondition, adds the source diagnostic to `Diagnostics`, and preserves its path
  in `Related`. `CMP004` is rule ambiguity, `CMP005` is no valid rules, `CMP006` is a compile limit, `CMP007`
  is path expansion, `CMP008` is conservation evaluation, `CMP009` is an unused transition warning, `CMP010`
  is an advisory budget warning, `CMP011` is a signature failure, and `CMP012` is a defensive cache-integrity
  warning.
- Catalog completeness, duplicate detection, and docs drift are tests. Step 08 adds the `CMP` rows/anchors to
  `docs/reference/diagnostics.md` in its diagnostics feature.

## Test tiers

- **PR-fast:** P1/pair/cluster golden counts, small rule-generation cases, conservation, tracker mappings,
  derived intent, signature vectors, cancellation, `CMP` emission.
- **PR-medium:** randomized pair/cluster/rule property tests, preview cross-checks, cache invalidation,
  determinism.
- **Nightly/release:** 10k-position performance/memory, cross-platform golden runs, larger random suites,
  fuzzed project inputs.
