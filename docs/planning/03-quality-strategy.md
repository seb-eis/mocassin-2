# 03 — Quality Strategy

## 1. Principles

1. **Specify behaviour before code.** Each step page names the invariant, formula, or user workflow that
   must hold. Tests encode it.
2. **Test the contract, not the implementation.** Behaviour is asserted through public APIs, CLI commands,
   files, diagnostics, and observable state — not through private methods or collaborator call counts.
3. **Prefer real objects and real data.** Use actual domain objects, real temp directories, real SQLite files,
   and files produced by the real writers. Use generated toy data when a full fixture is unnecessary.
4. **Property tests for critical algorithms.** Symmetry, compilation, supercells, round-trips, and state math
   are validated against invariants and brute-force or analytic oracles.
5. **Statistical tests for physics.** A single lucky trajectory proves nothing; compare ensembles within a
   documented tolerance.
6. **Performance is a test.** Benchmarks run on every release candidate; regressions above 10 % fail.
7. **Fail loudly on invalid data.** Parsers and compilers reject malformed input with diagnostics, never
   silently coerce.
8. **Mocks and fakes only where they are truly needed and beneficial.** See §3. The default is a real object,
   a real file, or a generated toy input; a test double must have a concrete boundary or determinism reason.

## 2. Test strategy: a testing trophy, not a unit-test pyramid

Most effort goes into the components and interfaces where a defect is both likely and expensive: the formats,
the compiler, the solver physics, the evaluation formulas, and the public CLI/API surfaces. Unit tests remain
important for exact formulas and edge cases, but they are not the centre of the strategy.

| Layer | Relative effort | Scope | Typical tools | Runs |
| --- | --- | --- | --- | --- |
| Contract / acceptance | Highest | Public C#/Rust/Python APIs, CLI, `.mocprj`/`.msl`/`.mcs`, solver/state, GUI commands | xUnit, Rust integration tests, pytest, manifest-generated fixtures | Every PR |
| Property / invariant | High | Symmetry, compilation, rules, supercells, round-trips, state math, parsing | FsCheck, `proptest`, Hypothesis (Python) | Every PR |
| Golden / toy integration | High | Real ceria/BaZrO₃/vehicle/interstitialcy models; tiny build → simulate → collect → evaluate runs | xUnit + CLI + Rust + SQLite | Every PR (short) / nightly (long) |
| Unit | Selective | Exact formulas, boundary conditions, parsers, small value types | xUnit, Rust `#[test]`, pytest | Every PR |
| UI (headless) | Moderate | Real view models and project objects; commands, validation, navigation | xUnit + Avalonia headless | Every PR (view models) / PR-medium (views) / nightly (full screenshot matrix) |
| Statistical / performance | Nightly / release | Parity ensembles, long runs, fuzzing, benchmarks | Custom harness, Criterion, BenchmarkDotNet | Nightly / release |

Rules of thumb:

- A test that only proves a private helper was called is replaced by a test of the observable result.
- A test that needs five mocks to run usually indicates a missing seam or an over-coupled design; prefer a
  small real integration instead.
- New critical algorithms need property tests before they are considered done.
- New public interfaces need at least one acceptance test that exercises them the way a user or another
  language would.

## 3. Mocks and fakes policy

**“No mocks/fakes” is shorthand for “mocks/fakes only where they are truly needed and beneficial.”**
It is not an absolute ban. A test double is acceptable when it replaces a true process, OS, UI, or
nondeterministic boundary, or when it injects a failure that is otherwise expensive or impossible to trigger.
It is not acceptable as a shortcut to avoid understanding or exercising the real domain.

### 3.1 Allowed test doubles

| Boundary | Double | Why it is justified |
| --- | --- | --- |
| Solver process launcher | Fake process/exit-code/signal harness | Keeps UI and orchestration tests fast and deterministic without a full solver build |
| Native file/folder pickers and dialogs | Fake dialog service | No display / user interaction in CI |
| GPU / windowing | Headless or stub renderer | CI has no reliable GPU; rendering contracts are tested separately |
| Wall clock | `TimeProvider` fake | Deterministic timeouts, timestamps, and scheduling tests |
| Failure injection | Fault-injecting stream/file/process wrappers | Permission, disk-full, truncation, and interruption cases are hard to trigger for real |
| Network / remote deploy | Fake transport at the edge | No external service in CI; the real protocol is covered by contract fixtures |

### 3.2 Not allowed

- Mocking domain objects, model aggregates, managers, compilers, symmetry, encoders, package writers/readers,
  solver context, energy engine, evaluation formulas, or the physics.
- Mocking SQLite instead of using a real temporary database file.
- Hand-writing fake `.msl`/`.mcs` binary blobs instead of producing them with the real writer (or a checked-in
  fixture generated by the real writer).
- Asserting private call counts, call order, or internal collaborator interactions.
- Mocking a type merely because constructing the real object takes a few lines.
- Using a fake RNG when a fixed real seed gives the same determinism.

### 3.3 Decision test

Before adding a test double, answer:

1. Is this a true process/OS/UI/network/time boundary?
2. Can the real object run cheaply and deterministically with a temp directory, fixed seed, or toy input?
3. Does the double test behaviour the user can observe, or only internal wiring?

If (2) is yes, use the real object. If only (3) is true, the test is probably asserting implementation detail and
should be rewritten.

## 4. Test data and fixtures

- **Real models first.** Ceria, yttrium-doped BaZrO₃, a vehicle model, and an interstitialcy model are the
  canonical fixtures. They are small enough for PRs and cover the important physics.
- **Generated toy data for breadth.** Domain-aware generators produce valid and invalid particles, structures,
  dopings, supercells, transition paths, rules, and `.mcs` documents. Prefer generated cases over one-off
  hand-written edge cases, and keep the seed in the failure output.
- **Generated by the real writers.** `.msl` and `.mcs` fixtures are produced by the .NET/Rust writers, never
  hand-authored binary. Regeneration scripts live next to the fixtures.
- **Real files and databases.** Use real temporary directories and real SQLite files; clean up
  deterministically. Do not mock the file system or SQLite.
- **Small and fast.** Fixtures stay small; large statistical datasets, long trajectories, and screenshots
  belong in nightly/reference assets with a manifest and SHA-256 checksums.
- **Versioned and regenerable.** Fixtures carry the format version and a generation command. CI checks that
  golden fixtures are current.
- **Deterministic.** Fix RNG seeds; shrink property-test failures (FsCheck/proptest/Hypothesis).
- **No secret dependence on the developer machine.** Fixtures must not depend on locale, time zone, installed
  fonts, or absolute paths.

## 5. Property-test catalog

Property tests are required for the following, using brute-force or analytic oracles where possible:

| Area | Properties |
| --- | --- |
| Symmetry / geometry | Group laws; stabilizer chain; induced permutations; orbit membership and closure; canonical order independent of input order; coordinate round-trips; minimum-image distance equals brute force; radial-search completeness; distance-bucket stability; pair/cluster reduction is complete and non-redundant; chiral orbit linking |
| Compilation | Mass/charge conservation per rule; valid indices; rule uniqueness; deterministic byte-identical output; P1 counts match brute-force expansion; every interaction has a table entry |
| Parameterization | Sparse and explicit-default templates apply to identical values; signature stability under entry order, culture, and round-trip; apply → persist → reload identity; rebase copies only exact or uniquely reported descriptors and never silently reuses values across changed geometry; chiral partners share one `EnergyId` and one value; site overrides round-trip and reset; arbitrary structurally valid template JSON never throws or exceeds a configured allocation |
| Supercells / doping | Requested vs applied counts; never exceeds available particles; charge neutrality or explicit failure; order dependence is deterministic; same seed → same lattice |
| Package / state | Encode → decode identity for all fields and versions; unknown fields ignored / unknown major version rejected; truncated input fails cleanly; checksum verification |
| Results store | `.msr` package binding, package-signature mismatch rejection, idempotent collection, previous-major read |
| Job contracts | `solver.json`, job-folder paths, event stream, and `mmcfe.db` round-trip and reject malformed input |
| RNG and seeds | Job/lattice seed derivation matches the Step 10 vectors; bounded draws are unbiased (chi-square) and never modulo; doubles stay in `[0,1)`; forks are domain-separated; the 64-byte state round-trips and resumes at block boundaries and mid-block |
| Evaluation | MSD/diffusion/mobility/conductivity identities on analytic random walks; non-negativity; unit consistency; empty and single-particle cases |
| CLI / parsing | Library parsing conventions (arity, `--opt=value`, negative values, `--`), bounded response files (syntax, limits, `--`/`--json` interaction, `CLI001`/2 errors), parse-error→`CLI001`/2 mapping, help and root `--version` 0, subcommand `--version` `CLI001`/2, job-range parsing and normalization, full-expansion ID stability, partial selections, exit codes, report schema, JSON-Lines ordering; no crash on arbitrary arguments; required local AOT closeout gate |
| Python reader | Valid document round-trip; malformed/truncated/future-version rejection; aggregation invariants match the .NET implementation |

Property tests assert invariants, not a specific internal sequence. A failing case must be reproducible from the
reported seed and shrunk input.

## 6. Critical interface acceptance tests

These interfaces get end-to-end acceptance coverage with real objects and real files:

1. `ProjectStore` load/save/migrate/round-trip and recovery.
2. `IModelCompiler.Compile` public output, counts, and diagnostics for the reference models.
3. `.msl` writer → Rust reader → canonical summary; `.msl` writer → Python/analysis reader where applicable.
4. Rust solver CLI → `.mcs` → .NET reader/evaluation and Python reader/evaluation.
5. Evaluation API formulas against analytic toy systems and shared formula vectors.
6. Avalonia commands against real project documents and view models. **Step 23A** proves
   create/open/inspect/save/reopen/reload, locks, read-only mode, external-change handling, and settings
   migration through the real store and the real dialog overlay. **Step 23B** proves metadata editing with
   undo/redo, save-point dirty handling, the validation surface, autosave, and crash recovery on the same
   harness.
7. CLI `build`/`simulate`/`collect`/`evaluate` workflows; the Step 12 build/preview/inspect/verify path uses
   real projects, real SQLite packages, and the real writer/verifier.
8. Contract-manifest drift checks across C#, Rust, Python, and generated fixtures.

Each acceptance test should fail for a user-visible reason, not because an internal mock was not called.

## 7. Coverage and mutation testing

Coverage is a diagnostic, not the headline target.

- **Required:** critical-path coverage for the interfaces in §6 and property/invariant coverage for the
  algorithms in §5.
- **Retained:** line/branch coverage reports for `Mocassin.Core`, `Mocassin.Symmetry`, `Mocassin.Compilation`,
  `Mocassin.Contracts`, `Mocassin.Packages`, `Mocassin.Results`, `mocassin-types`, `mocassin-msl`,
  `mocassin-rng`, `mocassin-solver`, and `mocassin-state`, reviewed for untested branches.
- **Not a gate:** a global percentage target. It rewards call-count tests and punishes small, honest modules.
- **Mutation testing (recommended, not blocking at first):** `Stryker.NET` and `cargo-mutants` for symmetry,
  compilation, energy, state encoding, and evaluation. Surviving mutants in critical code are triaged.
- A bug fix adds a regression test that fails before the fix.

## 8. Test tiers

| Tier | Contents | Target |
| --- | --- | --- |
| PR-fast | Unit, property, contract checks, one tiny build → run → collect → evaluate, headless view-model tests | < 5–10 min |
| PR-medium | More model variants, cross-language fixtures, headless UI subset, manifest drift | < 20 min |
| Nightly | Full statistical parity (reduced samples), long KMC/MMC, fuzzing, benchmarks, screenshot matrix | No wall-clock gate |
| Release | Full parity with high sample counts, stress (20×20×20, 10⁴ jobs), packaging, upgrade tests, manual GUI checklist | Release gate |

Tests must declare their tier. A PR-fast test may not depend on a nightly-only asset.

The Step 02 catalog/group-law and golden symmetry tests are PR-fast. The 20×20×20 expansion and full random
reduction suites are nightly/release.

The Step 12 CLI parser, selection, build/preview, partial, cancellation, force, inspect, verify, new, compile,
doctor (fake process probe), clean dry-run, and JSON schema tests are PR-fast. The 10⁴-job build + verify and
the real solver probe are nightly/release. The real `.msl` cross-language read is PR-medium with Step 13.

The Step 13 payload DTO, projection-vector, RNG golden, state round-trip/resume, dry-run golden, and
mutation/truncation tests are PR-fast. Reading the real `msl-v1` and `amend-1-net10` packages is PR-medium
because it is the cross-language contract gate. The time-boxed `cargo-fuzz` run, the Criterion load benchmark,
and the RNG draw/refill benchmark are nightly/manual evidence; the step records the commands and results at
closeout.

## 9. Domain test strategy

### 9.1 Core and project format

- JSON round-trip: model → JSON → model is identity for all fixtures.
- Space-group identity: `(number, axis, origin)` round-trips unchanged; Hermann–Mauguin, Hall, system, and
  operations are derived from the catalog and never serialized in the project.
- Symmetry metadata: matching project metadata loads silently; a catalog or contract mismatch emits `PROJ034`;
  missing metadata is accepted without rewriting the project.
- Coordinate grid: P1 expansion, deduplication, and sorting use the exact `24 × 10^12` fractional grid; no
  tolerance comparer participates in the discrete symmetry path.
- Canonicalization: object order changes do not change serialized bytes.
- Migration tests: each schema version has a fixture and an expected migrated model.
- Validation tests: real invalid project fixtures trigger each public diagnostic code with the expected
  severity, path, and hint.
- Property tests for units (no negative lengths, valid angles, finite charge).
- Failure-injection tests for atomic save, lock contention, and stale-lock recovery.

### 9.2 Symmetry and geometry

- For every space group in the catalog:
  - operations compose associatively and have inverses;
  - the identity is present;
  - applying all operations to a general position yields the expected multiplicity;
  - the P1 expansion of a special position is closed under the group.
- Brute-force oracle: expand a cell by explicit translation + rotation, deduplicate by tolerance, and compare
  with the optimized orbit generator for random cells and sites.
- Coordinate round-trips: fractional → 4D → fractional, fractional → Cartesian → fractional, including
  positions outside `[0,1)` and periodic images.
- Distance tests: minimum-image distances match a brute-force search over neighboring cells.
- Permutation reduction: for random pair/cluster geometries, the reduced set is a complete and
  non-redundant representation of the orbit.
- Catalog provenance: catalog and multiplicity tests use checked-in expected data generated from the pinned
  spglib catalog. The original MOCASSIN database is not a CI dependency; it may be used locally only to
  confirm the 274-setting count and known defects.
- Catalog integrity: startup checks the checksum, schema version, counts, identity, determinants, and
  normalized translations; full inverse/closure and group-law tests run in CI.
- Grid closure: applying every catalog operation to random grid points yields grid points; grid conversion is
  deterministic across OSes.
- Cell geometry: all seven systems, unique-axis mapping, near-degenerate rejection, positive volume,
  right-handed vectors, and the triclinic regression fixture.
- P1 contract: golden P indices, shuffle invariance, `Vector4I` round-trip for base and neighbor positions, and
  the signed 32-bit linear-index cap with a 64-bit total-count check.
- Stabilizer chain: `Gseq ⊆ Gset ⊆ G0`, `Gseq` is normal in `Gset`, and `P ≅ Gset/Gseq` is a group.
- Orbit counts: `|G0·S| = |G0|/|Gseq|` for ordered sequences and `|G0·{S}| = |G0|/|Gset|` for unordered sets.
- Pair reduction: reduced shells equal brute-force `G0` orbits; chiral linking matches an independent
  path-orbit check.
- Cluster reduction: the canonical tuple is independent of input order; the expansion map covers every
  equivalent tuple exactly once; 6-bit codes round-trip for 63 particles and 9 sites.
- Periodic geometry: `GetMinimumImage` equals brute force for skewed triclinic cells; `RadialSearch` matches
  brute-force enumeration; cutoff-boundary candidates are included with the 1 fm margin; equivalent pairs share
  a distance bucket.
- Memory: only base P1 Cartesian positions are precomputed; no dense `A×B×C×P` Cartesian array is allocated.
- Cache determinism: per-compilation caches, content keys, and parallel output equal sequential canonical
  output.
- Contract version: `symmetry-1` covers the grid, canonical order, pair-shell order, cluster codes, and
  reduction semantics; changing it invalidates golden signatures.

### 9.3 Compilation

- Golden compiled models for ceria, barium zirconate, and a vehicle/interstitialcy model. A golden file
  records counts, indices, pair/cluster tables, rule state triplets, tracker mappings, derived solver intent,
  and the model signature.
- Invariants:
  - every active transition rule has valid particle indices and a reachable direction/collection entry;
  - mass and charge are conserved per rule (except explicitly allowed charge-transfer rules, which preserve
    the carrier-symbol multiset across the path);
  - every stable pair interaction has a table entry and every environment link points to a valid table;
  - every cluster permutation maps to a canonical occupation code and a valid table row;
  - P1 counts match brute-force expansion, and pair/cluster counts match the Step 05 preview services;
  - within one collection, `state0` maps to at most one rule; ambiguity is `CMP004`;
  - `jump_count`/`jump_mapping` are dense, consistent, and list only selectable directions;
  - charge transport vectors give `qΔx` for a single charge, the correct vacancy sign, zero for an
    identical-ion exchange, and non-zero for a distinct-charge exchange or a non-collinear multi-site path;
  - `UseDualDofCorrection` defaults on for every KMC simulation, `UsePrerun` follows `PreRunMcsp > 0`, MMC
    clears both, and Step 11 maps the intent correctly.
- Determinism: compiling the same model twice yields byte-identical `CompiledModelJson` and the same
  `ModelSignature` on Ubuntu, Windows, and macOS; runtime settings and parameterization values do not change
  the model signature.
- Cache: a recording cache proves the invalidation graph after editing one domain at a time, including a
  simulation edit that changes the referenced transition set versus a runtime-settings-only edit; unaffected
  stages are reused and the output is unchanged.
- Diagnostics: every `CMP` code has a catalog descriptor, documentation anchor, and an emission test with
  severity, `ModelPath`, and hint.
- Cancellation: a cancelled compile returns no partial model and leaves the project unchanged.
- Performance on the benchmark laptop class (8 physical cores, 32 GB RAM, .NET 11 Release): ceria
  `< 1 s`; a 10k-position model `< 30 s` and `< 2 GiB` managed memory; cancellation is checked between stages
  and top-level units, with atomic Symmetry calls documented as non-interruptible.

### 9.4 Supercell and doping

- Doping fractions respect integer truncation and dependent-exchange ratios.
- Charge compensation is exact (total charge within tolerance) or the build fails with a diagnostic.
- Doping order is respected; changing priority changes the lattice deterministically; same-site dopings apply
  to the particles remaining after earlier entries.
- Same seed + same job coordinates yields the same lattice across runs and OSes. Golden vectors freeze the
  ADR 0005 job-seed tuple, the lattice seed, and the SHA-256 counter sampler.
- Edge cases: zero doping, 100 % doping, supercell too small for requested fraction, dependent exchange
  impossible, non-compensated charged doping, fractional charge ratios, multiple dopings on the same site,
  vacancy default occupation, NaN/Inf fractions.
- Property test: for random valid inputs, the number of replaced particles never exceeds the available
  count and every replaced particle came from the source set.
- Step 10 owns expansion (< 10 s for 10⁴ small jobs) and generation (< 1 GiB peak, cancellation every job);
  Step 11 owns the full 10⁴-job package build budget (< 10 min, < 2 GB).

### 9.5 Solver physics

| Test | Oracle |
| --- | --- |
| MMC detailed balance | Boltzmann distribution for a two-state system; chi-square test |
| MMC energy minimization | Known ground state of a small Ising-like lattice |
| KMC residence time | Analytic `τ = ν₀⁻¹ exp(ΔE/kT)` for a single barrier |
| KMC time step | `Δt = K_norm / Σν` matches a hand-computed small system |
| KMC normalization | Fixed and automatic normalization reproduce the same long-time limit |
| Electric field | Drift velocity sign/magnitude for a single charge in a constant field |
| Pair energy | Hand-computed delta for a two-particle swap |
| Cluster energy | Hand-computed delta for a 3-body occupation change |
| Site energy | Barrier shift equals the configured site energy |
| Charge/mass conservation | Sum over lattice unchanged after every accepted step |
| Trackers | Displacement equals final minus initial position for a single particle |
| MSD/diffusion | 1D random walk with known `D = Γa²/2`; origin-based MSD uses the tracked ensemble count |
| Per-species transport | Conductivity, mobility, and Haven ratio match hand calculations for a two-species system |
| Conductivity | Nernst–Einstein consistency in the dilute limit |

Physics tests use analytic or hand-computed toy systems, not mocks of the energy engine or RNG.

### 9.6 Statistical parity with the original

Reference systems:

1. **Reduced ceria** — oxygen vacancy migration + polaron hopping (KMC) at 773 K and 1073 K.
2. **Yttrium-doped barium zirconate** — proton/vacancy transport with hydration dopings.
3. **A vehicle or interstitialcy model** — validates complex transition rule generation.

For each system, record:

- equilibrium lattice energy distribution (MMC) and mean energy vs temperature;
- acceptance/rejection/skip/blocked/S0/S2-unstable fractions;
- conductivity and mobility vs temperature (Arrhenius fit: prefactor and activation energy);
- diffusion coefficient and correlation factor;
- jump-barrier histograms;
- particle densities and lattice occupation statistics.

Comparison method:

- Run the original C solver and the Rust solver with the same physical inputs, multiple seeds, and enough
  samples to make the standard error small relative to the tolerance.
- Compare means with Welch's t-test or a confidence-interval overlap criterion; require the difference to be
  within `max(3σ, 2 %)` for intensive quantities and `max(3σ, 5 %)` for transport coefficients.
- Compare distributions with a Kolmogorov–Smirnov test and an effect-size threshold.
- Record the exact reference version, commit, compiler flags, and seeds in `tests/reference/README.md`.
- Build the original solver/model builder using
  [`07-reference-build.md`](07-reference-build.md); the gitignored `/workspace/mocassin-original` clone stays
  read-only.

Bit-exact trajectories are explicitly not required (see ADR 0005). Determinism is required only for the new
solver with the same binary, platform, package, and seed.

### 9.7 Validation, diagnostics, and statistics

- **Pipeline order and skipping:** load errors stop the pipeline; model errors skip cross-domain, feasibility,
  and statistics; `validate` never runs build-stage checks (`RunBuildStage = false`), and the in-memory entry
  reports `Load` as skipped only when the caller supplies no load diagnostics.
- **No mutation:** model and document hashes are unchanged after success, failure, and cancellation.
- **Catalog completeness:** every public Core code constant and every internal Symmetry constant is registered
  once in its assembly-local catalog; the composed `DiagnosticCatalog` is their union plus package and Step 07
  codes; every catalog code appears in `docs/reference/diagnostics.md`; no duplicate or extra code exists.
- **Per-code emission:** every Step 07 emitted code has a real invalid fixture or focused test with the
  expected severity, `ModelPath`, and hint. Existing Step 03–06 tests count for severity; Step 07 adds its
  cross-domain, feasibility, and package-level cases.
- **Determinism and deduplication:** identical inputs produce byte-identical diagnostic order; exact
  `(code, path, message)` duplicates are removed.
- **Cross-domain:** `ENM018` fires when a resolved KMC hop's minimum-image distance strictly exceeds the
  applicable stable/unstable cutoff; it does not cascade after path-resolution errors.
- **Feasibility:** `SIM038`–`SIM041` fire at their documented boundaries; `SIM039` suppresses `SIM038`;
  `RequireBuildable` promotes the buildability warning set and `WarningsAsErrors` promotes every warning.
- **Statistics:** P1/stable/unstable counts match `StructurePreview`; pair interactions, cluster permutations,
  memory, and the `long` rule total match `EnergyCostPreview`/`TransitionPreview` on hand-computed models.
  Statistics are `null` only when not requested, after a load failure, after a model-stage error, or after a
  failed preview; they are non-null alongside feasibility/budget errors and elevated warnings.
- **CLI:** human table and summary are stable; `validation-v1` JSON matches the generated schema; exit codes
  distinguish valid, warning-as-error, parse failure, usage, and I/O failure, and a load failure still produces
  one report with `null` identity fields.
- **Performance:** ceria validation is `< 200 ms` without statistics and `< 2 s` with statistics on the
  benchmark laptop class. Statistics are opt-in and may be excluded from the fast tier.
- **Cancellation:** a cancelled `validate` returns no partial success and writes nothing.

### 9.8 Parameterization

- **Template round-trip:** project JSON → template → project JSON is stable for shuffled authoring order,
  comma-decimal cultures, `-0.0`, additive missing arrays, and unknown-field rejection. The canonical document
  hash is stable across those permutations.
- **Identity:** pair/group/rule set IDs equal the compiled `EnergyId`/`ClusterEnergyId`/`RuleGeometryId` on
  ceria, barium zirconate, and ceria KMC; a geometry or catalog/contract change invalidates the model
  signature before ID matching.
- **Sparse/default equivalence:** applying a sparse template and applying an equivalent template with explicit
  default values produce identical `ParameterizationValues` and parameterization signatures. Missing pair and
  group values are `0 eV`; missing rule frequencies are the compiled default; missing site overrides use the
  authored background. The implicit default path (a project with `parameterizations: []`) builds ceria,
  ceria-kmc, and barium zirconate with all defaults, reports `HasExplicitValues = false`, and emits neither
  `PRM003` nor `PRM006`; an explicit empty template emits both.
- **Chiral:** the real barium-zirconate model (24 chiral geometries, i.e. 12 reciprocal pairs) has one table and one value per
  shared `EnergyId`, a symmetric particle table, and no direction-specific API or JSON field; a generated
  P1/P4mm fixture covers breaking the chiral link and asserts the old value is dropped, not reused.
- **Site energies:** the applied `[position][particle]` background equals the authored `siteEnergies` mapped
  through compiled site ownership; overrides replace every owned position and removal restores the authored
  value; invalid targets warn and do not alter the table. Step 15's site-energy/barrier test consumes the same
  table.
- **Signature:** the six-field values-only projection is deterministic; same effective values from different
  templates produce the same digest; a value-only change changes the parameterization signature but not the
  model signature; a geometry/rules/catalog change changes both. The `.msl` model payload stores each rule's
  absolute `frequency_hz`, so the checked-in `.bin` vector is re-encoded exactly by the Python checker and,
  from Step 13 on, by Rust without a factor × maximum reconstruction.
- **Rebase:** `KeepMatching` copies only exact IDs; `NearestGeometry` copies only unique, reported descriptor
  matches and never across a changed chiral link; ambiguous matches copy nothing; `Reset` copies nothing; every
  strategy emits the report.
- **Diagnostics:** every non-retired `PRM` code has a descriptor, documentation anchor, and an emission test
  with severity, `ModelPath`, and hint; structural codes are visible in `validate`, compiled-bound codes in the
  post-compile call; `PRM007` is not registered.
- **Project format:** the generated `project-v1.schema.json` accepts the extended template fields and rejects
  unknown ones; the `ProjectSchemaTests` fixture list includes the new `samples/parameterizations/*.mocprj`
  files; `tools/contracts/generate.py --check` and `verify.sh contracts` pass.
- **Limits and safety:** oversized counts, strings, and applied tables fail with `PRM013` before allocation;
  arbitrary structurally valid template JSON produces diagnostics rather than an uncaught exception.
- **Performance:** ceria create `< 20 ms`, apply + signature `< 50 ms`, and the synthetic 1,000-position case
  `< 2 s`/`< 256 MiB` on the benchmark laptop class; the signature writer streams at `>= 100 MB/s`.

### 9.9 Simulation package and `.msl` writer

- **Payload mapping identity:** every model payload field equals its documented compiled or applied source
  (`CompiledModel`, `ParameterizationValues`); pair/cluster energies and the defect background are the applied
  Step 09 values, never the compiler's zero defaults; every environment pair entry carries its compiled
  representative `offset` and `target_position`; `tracker_order` is variable-length with one entry per path
  node; the job payload equals the resolved `JobDescriptor` plus the generated lattice; the lattice byte array
  equals `GeneratedLattice.Particles` in the documented row-major order.
- **Cross-step payload conformance:** Step 14's link expansion (`cell + offset` wrapped, `target_position`)
  reproduces the compiled minimum-image links, and Step 15's `(target, pair_id)` deltas consume them; Step 18
  consumes the variable-length `tracker_order` with the `0xFF` sentinel. A fixture package exercises a partner
  position with more than one periodic image.
- **Signatures:** the parameterization projection re-encodes from the payload's applied tables and absolute
  `frequency_hz` values; the lattice projection re-encodes from the payload plus the referenced
  `model_signature`; a mutation of any signed value changes the digest. `model_signature` is checked against
  the compiled model at write time and is an identity string for readers.
- **Determinism and dedup:** the same project, parameterization, and job set produce identical model/lattice
  signatures and an identical `package-summary-v1` projection (data contracts §3.5); only build metadata
  (`created_utc`, `app_version`, `generator`), size, payload byte hashes, and SQLite/Zstd implementation details
  may differ in the bytes. Byte-identical lattices insert once; a different particle array inserts a second row.
- **Payload codec:** one fixed path — MessagePack, then a one-shot in-box `ZstandardEncoder` at
  `PackageBuildOptions.CompressionLevel` (Zstd quality, identity mapping, 1..19) with `SetSourceLength` and
  `isFinalBlock: true`. No dictionary, checksum, long-distance matching, streaming encoder, or custom framing.
  Reads accept any valid standard frame, take the declared or upper-bound decompressed size, and check it against
  the guard before allocating. Compressed bytes are an observation, not a contract (ADR 0010, data contracts
  §3.5): the same logical input at the same level is byte-stable on one runtime patch and may change across
  patches, while the canonical summary and signatures must not. The decode budget below 8 KiB is an **absolute**
  ≤ 0.15 µs per payload plus ≥ 100 MB/s, because fixed reader overhead dominates; at or above 8 KiB the relative
  ≤ +10 % rule applies (amendment 1 §9.1). The checked-in golden frames are the reproducible form of that mapping.
- **Atomicity and failure injection:** cancellation, a simulated write/disk-full error, and a crash after
  writing the temp file leave an existing package untouched and produce no `-wal`/`-shm` files or partial
  package. The published file is only replaced by the fsynced atomic rename.
- **Immutability and schema:** a published package opens read-only, has no `results` table, reports
  `application_id = 0x4D4F4353` and `user_version = 1`, and its `schema_info` matches the manifest.
- **Verification:** each `PKG001`–`PKG010` code has a corrupt, truncated, inconsistent, or injected-table
  fixture with its documented severity and docs anchor; a decompression bomb or an oversized declared count
  yields a diagnostic instead of an allocation; verification never throws for untrusted input and never mutates
  the file. Model and job payloads are bounded by `PackageVerificationOptions`. A lattice payload is bounded by
  `4 × a × b × c × position_count` from its referencing job and model, capped by
  `PackageVerificationOptions.MaxLatticePayloadBytes`; an orphan lattice row uses that cap as its fallback, and a
  lattice bomb produces `PKG004` without an unbounded allocation (`fix-lattice-decode-guard`, amendment 1 §4).
  `PKG` descriptors are Contracts-owned; the verifier's project has no `Mocassin.Packages` reference.
- **Fixtures:** a checked-in `.msl` produced by the real writer plus its canonical summary (normalized build
  metadata) and extracted model payload; the regeneration command is recorded and `verify.sh contracts` checks
  the lattice projection vectors. The amendment's `tests/contracts/packages/amend-1-net10/` compatibility package
  is byte-fixed and never regenerated; `tests/contracts/packages/amend-1/` holds the pre-amendment codec goldens
  and follows the byte-recording rule (re-capture the bytes with a note; never regenerate signatures or the
  summary).
- **Tiers and performance:** mapping/signature/atomicity/verification tests are PR-fast; the cross-language
  `.msl` read is PR-medium with Step 13; fuzzing and the 10⁴-job build are nightly/release. The writer targets
  `< 10 min`/`< 2 GB` for 10⁴ small jobs, `< 50 MB` for a complex model payload, and `>= 100` job inserts/s.

### 9.10 Build orchestration and `mocassin` CLI

- **Parser:** `System.CommandLine` 3.0.0 supplies command-line tokenization, arity, and help; the CLI
  supplies the root `--version` action. Tests pin the
  conventions the CLI relies on: every command, long/short option, `--opt=value`, `--`, missing value,
  unknown option, duplicate option, conflicting option, required option/argument, negative values
  (`--jobs -1`, `--parallel -1`, `-o -file`), and no POSIX short-option bundling. A parse error is
  `CLI001`/exit 2 with CLI-owned wording naming
  the offending token; the library's default exit 1 is never returned; help and root `--version` exit 0, and a
  subcommand `--version` is `CLI001`/2. No arbitrary
  `string[]` crashes or throws.
- **Response files:** `@file` syntax, double-quote grouping and empty quotes, golden tokenizer cases
  `a"b c"d` → `ab cd`, `a""b` → `ab`, `"a""b"` → `a` + `b`, `""` → empty, unterminated-quote discard/keep,
  literal single quotes and backslash, `#` comments only at line start, recursive references, cwd-relative
  paths, `@` literal and no escape, `--` stops expansion, `--input=@file` literal, `--input @file`
  token-expanded before arity, `--json` inside/outside the file, 1 MiB of raw bytes per file / 4 MiB of raw
  response-file bytes across the command line / depth 8 / 10,000 tokens contributed by response files / 32 files, and missing/unreadable/
  directory/invalid-UTF-8/oversized → `CLI001`/2 (not `CLI002`/3). The library default is not the contract.
- **AOT:** `verify.sh aot` publishes a minimal `System.CommandLine` 3.0.0 host with
  `-p:PublishAot=true -p:TrimMode=full -r linux-x64` and asserts zero IL2xxx/IL3xxx warnings, then publishes a
  **separate** positive control that must emit IL2026 and IL3050. The control project opts out of
  warnings-as-errors for its own build so those remain warnings; the host stays strict. The native host includes
  the `CLI001`/2 remap, so its parse-error case exits 2. Missing clang/lld/ILCompiler is a hard failure with a
  clear message, never a skip; the versions are observed apt/NuGet resolutions checked by `06` §9, not a
  containerfile pin, and a version mismatch is not a gate failure. The real `Mocassin.Cli` publish is recorded
  as evidence: the 62 real-CLI diagnostics are first-party `Mocassin.Core` reflection-based JSON converters
  (`ProjectJson.cs` 16, `ModelDefinitionJsonConverter.cs` 16, `StructureModelJsonConverter.cs` 14,
  `SimulationJsonConverter.cs` 12, `TransitionRefJsonConverter.cs` 4; 31 × IL2026 + 31 × IL3050), and Step 31
  owns the AOT/single-file decision. `Microsoft.Data.Sqlite`/`MessagePack` remain unverified for AOT.
  `aot` is part of `verify.sh all` and is a **local** closeout gate: feature 1 adds the mode/hosts, feature 7
  runs `all --require` at Step 12 closeout and records the output; the closeout GitHub workflow does not run
  `verify.sh`. Because it is in `all`, **every step closeout from Step 12 requires the Tier 1 toolchain**. It is
  not a scheduled nightly job. Only `linux-x64` is covered; Windows/macOS AOT and the `netstandard2.0` asset are
  Step 31 platform gates.
- **Selection:** build-template/job-template/parameterization precedence is table-driven; one/two/many cases,
  explicit overrides, wrong-kind IDs, unknown IDs, and the implicit default all have tests. The selected
  effective IDs, not the authored recipe, are reported.
- **Job selection:** the grammar, canonical normalization, dedup/merge, `all`, range gaps, zero/negative/
  descending/empty/out-of-range errors are property-tested; the normal form is idempotent, and equivalent
  selections produce identical `job_selection` metadata and package summaries.
- **Pipeline:** the ceria job sample builds through the real compiler, parameterizer, expander, generator, and
  writer. Partial selection keeps full-expansion IDs and omits unselected rows. `--parallel 1` and `N`
  produce identical signatures, job rows, and `package-summary-v1`. Failure kinds map to the documented exit
  codes; `BuildResult.Identity` supplies the report’s project/model/document hashes and model/parameterization
  signatures, including on an early failure or cancellation.
- **Atomicity and cancellation:** an existing package is byte-identical after a failed/cancelled build; no
  temp file remains; a cancellation after publication leaves a valid package and exits 6. Failure injection
  covers a writer error and a disk-full-like `IOException`.
- **Preview:** `--preview` never creates or modifies a `.msl`, reports `mode = "preview"`, `package = null`,
  and the same lattice/doping counts as the build; compile errors exit 4. `BuildResult.Preview` is produced
  once by the pipeline’s shared `BuildPreviewBuilder`; the CLI/Step 26 projection does not reload, recompile,
  or call a second preview service, and `build-v1.preview` equals the typed payload.
- **Options and seams:** `BuildRequest.Options` supplies the clock, throttle, and channel capacity; tests use
  a fixed clock. `doctor` injects `IDoctorCheck` outcomes for pass/warn/fail; only the solver probe launches a
  process.
- **Warnings:** validation warnings exit 1 under `--warnings-as-errors`; promoted parameterization warnings
  exit 4; an unflagged build succeeds; `CMP`/`PKG` warnings stay warnings.
- **Reports:** every `--json` report for `validate` and the seven report-backed commands validates against its
  generated schema, including usage errors; `build-v1` covers build and preview modes; `clean --json` without
  `--dry-run` reports `CLI008`; help/version/reserved commands emit no report; diagnostics use the
  `validation-v1` element shape.
- **Events:** `build.stage` events precede `build.started`; JSON-Lines `build.started`/`build.stage`/
  `build.progress`/`build.finished` ordering; `build.finished` exactly once; `--json` leaves stderr
  JSONL-only; `--quiet` suppression; `tests/contracts/events-v1.jsonl` acceptance.
- **Inspect/verify:** valid, corrupt, truncated, and missing packages; `PKG` diagnostics; exit 0/1/3.
- **New/compile:** blank and ceria templates produce valid projects with fresh identity and seed; the embedded
  ceria template’s model hash equals the sample; `compile -o` writes a re-readable
  `compiled-inspection-v1`; `CMP` errors exit 4.
- **Doctor:** a healthy CI run; injected runtime/SQLite/permission/disk/catalog/template failures; a missing
  solver is a warning; a configured broken solver is a failure. The process probe is faked in PR-fast tests.
- **Clean:** `--dry-run` lists only; no file, folder, `.msl`, or `.msr` is removed; `--older-than` filters;
  `--results` and deletion are reserved.
- **`validate` regression:** the human table, `validation-v1` JSON, and 0/1/2/3 exit codes are unchanged by the
  parser/report refactor.
- **Tiers and performance:** the above are PR-fast with the fake solver probe; the 10⁴-job build + verify
  and the real solver probe are nightly/release. The AOT smoke check is the **local** `verify.sh aot` mode
  included in `verify.sh all`; feature 7 runs it at Step 12 closeout and records the output. The closeout
  GitHub workflow does not run `verify.sh`, so the AOT gate is not a CI confirmation. The Step 12 evidence
  records time, peak RSS, package size, and verification result against the Step 11 budget (< 10 min, < 2 GB).

### 9.11 Rust solver foundation and package reader (Step 13)

- **Boundary:** the Rust solver reads only the immutable `.msl` and CLI arguments; it never reads project JSON
  and never writes `.msl`/`.msr`.
- **Reader acceptance:** the checked-in `msl-v1` package and the byte-fixed `amend-1-net10` package open
  read-only; the dry-run counts/signatures equal the canonical package summary for the same job. A new
  real-writer populated pair/cluster + MMC package exercises the non-empty parameterization and MMC paths. This
  is the first .NET-writer → Rust-reader contract run and is PR-medium.
- **Bounded decode:** a non-allocating MessagePack scan enforces the byte, element, depth, and per-field count
  limits before typed decode. Tests cover truncated frames, oversized declared sizes, decompression bombs,
  element-budget violations, duplicate known keys, trailing bytes, and malformed `schema_info`. No unbounded
  allocation or panic is acceptable.
- **Validation:** every particle/position/site/environment/pair/cluster/collection/direction/rule/tracker
  index and length; state-code node values; `tracker_order` length against the collection path; jump-count
  prefix sums; kind/flag consistency; finite values; normalized field direction; lattice length and particle
  indices; `models`/`lattices` `format_version` columns against their payloads.
- **Scientific invariants:** each rule preserves the non-void particle-symbol multiset and charge within
  `1e-9`; `path_vector`/`length` match the cell-vector sum of `movement`; a no-charge-change rule has a zero
  transport vector. Void is exempt from the occupation check; a non-void state particle must be in the
  collection-union mask, and the real `msl-v1` rules must pass. The exact per-direction occupation check and the
  full positional `charge_transport_vector` formula are recorded Step 16/15 assignments because the v1 payload
  has no representative-direction marker or fractional P1 coordinates; the trust boundary and residual risk are
  documented in the Step 13 design.
- **Equivalent starts:** a direction mapped into another symmetry-equivalent start bucket is accepted; the
  reader does not require `direction.position_id == position`.
- **Element budget:** 512M model elements covers a writer-valid 512 MiB payload; the 1M-rule/16M-link caps are
  per-field, and a raised `CompilationOptions.MaxRuleCount` requires a raised `ReaderLimits`.
- **Signatures:** the parameterization and lattice projections re-encode from the payload with the ADR 0010
  fixed-width writer, the C# quantization expression, and the domain prefix. The checked-in `.bin` vectors
  round-trip byte-identically. `model_signature` is an identity string, never re-encoded.
- **RNG:** seed derivation matches the Step 10 vectors; ChaCha12 draw goldens; Lemire bounded draws; 53-bit
  doubles; domain-separated forks; the 64-byte `RngStateV1` round-trip and mid-block resume. The RNG benchmark
  records draws/s, refill cost, state round-trip, and fork cost against the documented budget; a >10 %
  regression is measured only against the same-runner-class entry in
  `tests/reference/bench/baselines.json` and is a release failure.
- **CLI:** the dry-run golden, exit codes 0/5/10–16 with the architecture §7.3 meanings (5 reserved for Step 19 batch), parse-error remap,
  help/version, logging formats, and the write-free guarantee. `--stdout` with `--dry-run` is a usage error; no
  job folder or log file is created. A doc-drift check compares the solver rows in `cli.md`,
  `solver-architecture.md`, and the step page with architecture §7.3.
- **Fuzzing:** `payload_scan`, `payload_decode`, and `package_open` targets start from the real payload/package
  seeds; a time-boxed run is recorded at closeout. Step 29 extends fuzzing to `.mcs` and extension configs.
- **Performance:** ceria job load `< 100 ms` (release, warm) and peak RSS `< 64 MiB` are fixture budgets, not a
  promise for a 512 MiB package. The RNG budgets are in §13 and the step page; the environment (CPU class, Rust
  version, commit) is recorded with the raw numbers and the same-class `baselines.json` entry.
- **Tiers:** DTO/projection/RNG/state/CLI/mutation tests are PR-fast; the real-package read is PR-medium;
  fuzzing and the benchmarks are nightly/manual.

### 9.12 Simulation context initialization (Step 14)

- **Boundary:** one context per job, built once from the validated `JobPackage`; no I/O, no package re-read, no
  physics (no energy deltas, no cycle, no trackers, no `.mcs`).
- **Arenas and links:** lattice/environment struct-of-arrays, a translation-compact per-P1 neighbour-link CSR
  with the periodic image as a relative cell offset and the absolute target resolved at use, deterministic
  sorting (`pair_id`, target, offset) that fixes the f64 summation order, and a validated cluster
  instance/membership/code representation (centre slot `0`, own-slot `byte_position`).
- **Cluster geometry limitation (OD-1):** the v1 payload does not persist a member's relative cell offset. Step
  14 derives it only from a unique centre→member pair image and rejects zero or multiple images; the
  code-existence check makes a wrong geometry fail at initialization. The limitation is recorded in the data
  contract, the open decisions, and the step page; a future `.msl` revision persists the offset.
- **Energy runtime:** site once, pair ×0.5, cluster once at the centre; the initial lattice energy uses that
  exact decomposition and a hand-computed test; Step 15 continues with it. The original's uniform `0.5 × Σ` is a
  recorded deviation (OD-2), covered by Step 29 parity.
- **Pools and jump status:** direction-count buckets walked ascending, mask filtering, optimizer removal, O(1)
  swap-remove, `selectable_jump_count`; per-`(P1, direction)` sender/receiver links with `link_id` relative to the
  sender's P1 list, structural receiver filter, `L*(L-1) ≤ 90` derived bound, and cluster touches for the
  centre's S0/S2 terms. The exact per-direction occupation check stays in Step 16.
- **Goals and normalization:** `prerun = pre_run_mcsp × mobile_count`, `main = target_mcsp × mobile_count`,
  `total` rounded up to 100, block = `total/100`; `K_fix = exp(E_norm/kT)`, `K_auto = 1/RawMax`,
  `K_norm = K_fix × K_auto`, `dt = dof × K_norm/(nu_max × selectable)`. `nu_max` is the job cap or the maximum
  active rule frequency (not persisted in `.msl`).
- **Resume:** a plain `ResumeState` validated against the package/job/lattice; `RawMax` preserved; the mobile
  mapping re-derived and compared; a completed phase returns a distinct outcome and is never resumed silently.
- **Errors:** invalid input/cluster/limit/resume → exit 12; non-finite factor/`nu_max`/`dt`, bad RNG state,
  invariant → exit 13; cancellation → exit 15.
- **Tiers:** index/wrap, links, cluster codes, energy vectors, pools, jump status, goals, bounds, resume, and the
  allocation proof are PR-fast; the real `msl-v1`/`amend-1-net10`/populated/centered packages and the 10×10×10
  fixture are PR-medium; the Criterion context benchmark and the 500k-position stress are nightly/manual.
- **Performance:** context init `< 100 ms` for the 10×10×10 fixture, `< 20 ms` for the small fixtures; the
  `MemoryReport` total within budget; zero allocations in hot accessors; a same-runner-class
  `baselines.json` entry (`rustContext`) with the 10 % rule.

## 10. Contract tests

- **Project JSON**: .NET writes fixtures; a JSON Schema validator checks them in CI.
- **Compiled model**: .NET writes `.msl`; Rust reads it, validates invariants, and emits canonical JSON;
  compare with the .NET canonical JSON.
- **Canonical signatures**: `tests/contracts/signature-vectors.json` fixes the canonical MessagePack
  projection bytes and SHA-256 digests for model, parameterization, and lattice. C# and Rust must reproduce
  them; a serializer change must not change a signature.
- **Rust projection re-encode**: the Rust reader decodes each `.bin` parameterization/lattice projection with a
  strict fixed-width reader, re-encodes it, and must match the bytes and the domain hash.
- **RNG vectors**: `tests/contracts/rng-vectors.json` freezes the `chacha12-v1` derivation, draw, bounded-draw,
  fork, and `RngStateV1` round-trip behavior. It is a test-vector document, not a runtime format.
- **Results store**: `collect` writes `.msr`; .NET and Python read it. A `.msr` with the wrong package
  signature/version is rejected.
- **State file**: Rust writes a MessagePack `.mcs` for a synthetic small run; .NET and Python read it and emit
  canonical JSON summaries; compare all three with the schema version and shared formula test vectors.
- **Job folder**: a real solver job folder (`solver.json`, `run.mcs`, `prerun.mcs`, logs, optional `mmcfe.db`)
  is parsed by the collector and the GUI event reader.
- **Events**: `events-v1.jsonl` is validated against the event schema; unknown events/fields are ignored.
- **Version support**: current + previous major fixtures for `.msl`/`.msr`; current-major-only fixtures for
  `.mcs`. A future major is rejected with a clear error, and malformed/truncated input fails cleanly.
- **Manifest drift**: `contracts/manifest.json` generates schemas, constants, and fixture descriptors; CI fails
  if generated artifacts are stale or a language DTO/validator disagrees with the manifest.
- **Shared formula vectors**: MSD, diffusion, mobility, and conductivity are evaluated by .NET and Python on the
  same arrays; results must agree within floating-point tolerance.
- **Fuzzing**: `cargo-fuzz` on the Rust `.msl`, `.mcs`, and extension-config readers; SharpFuzz on the .NET
  project JSON, `.msr`, `solver.json`, event, and MMCFE readers; Hypothesis on the Python readers. Crashes and
  OOMs are release blockers. The Step 11 `.msl` verifier's nightly mutation/fuzz harness is **deferred to
  Step 29** (owner: Step 29 implementor) and lands with the SharpFuzz targets and the nightly fuzz job; until
  then Step 11 proves the fixed truncation, corrupt-frame, wrong-signature, foreign-key, and
  decompression-bound cases and claims no fuzz run.

Contract tests use files produced by the real writers and never share reader/writer code across languages.

## 11. UI testing

- View-model tests use real project documents and real domain objects; only true UI/process boundaries are
  faked (dialogs, storage picker, solver launcher, clock).
- Avalonia headless tests render key views and assert on the visual tree and accessibility properties.
- Accessibility target is **WCAG 2.2 AA** for desktop UI: keyboard access, logical focus order, contrast,
  scalable text, screen-reader names/roles, and no color-only information.
- Automated accessibility-tree checks run in headless tests; manual screen-reader checks (NVDA/Orca/VoiceOver)
  are part of the release checklist.
- Screenshot tests for the shell, each editor, and the 3D viewport on Windows/Linux/macOS (tolerance-based).
- The Step 24 editor-layout spike produces owner-reviewable POCs and screenshot baselines for each option. The
  chosen layout gets headless selection/keyboard/virtualization tests; no data-grid dependency is added without
  an ADR.
- Commands are tested through their observable effects: project state, diagnostics, generated files, and
  navigation — not through mock verification.
- Manual smoke checklist per release: create project, add each object type, validate, parameterize, build,
  run a tiny job, collect, plot, save/reopen.

### 11.1 Steps 23A/23B harness and tiers

Step 23A establishes the harness that 23B and Steps 24–28 extend; 23B adds the Problems/validation and
recovery fixtures.

- **Projects.** `Mocassin.App.Tests` references the app, view models, and views. It adds the first-party
  `Avalonia.Headless` and `Avalonia.Headless.XUnit` packages at the pinned Avalonia 12.1.6 version (test-only,
  no product dependency, no new licence surface). No other package is added; a `ManualTimeProvider` and an
  inline UI dispatcher are written in test support rather than adopting a fake-clock package.
- **Real objects everywhere.** Tests use the real `ProjectStore` (with `ValidateOnLoad = false` and the load
  diagnostics seeded into the validation pipeline), real temporary directories, real `Project` values, the real
  validation pipeline, the real dialog overlay, and the real App-layer settings/recovery stores when the test is
  about persistence. Fakes are limited to the true boundaries: `IDialogService` (view-model-level tests only —
  the view-level tests drive the real overlay), `IStorageService` (the OS picker), `IUiDispatcher` (the
  UI-thread boundary), `TimeProvider`, and in-memory `IAppSettingsStore`/`IRecoveryStore` for non-persistence
  tests only.
- **Architecture guards.** The App-project architecture test checks direct `.csproj` references and runs the I/O
  token scan described in the 23A page §1/§15: comments and string literals stripped, whole-identifier matching
  (so `LastOpenDirectory.Trim` does not match), `using static System.IO`/`System.Environment` detected,
  `IUiDispatcher`/`Dispatcher` and bare `Application` permitted, and a positive control so the guard cannot pass
  by matching nothing. The scan is a tripwire; the direct-reference check and review remain the primary
  enforcement. The 23B `Load`-stage test covers both a load/migration fixture (`Completed`/`Failed`) and a clean
  fixture (`Skipped`, count 0).
- **Tiers.** 23A: shell view-model, lifecycle, reload/locks/read-only/external-change, settings migration,
  navigation, palette, dialog, and primary accessibility tests. 23B: edit/undo, dirty/save-point, autosave,
  recovery, and validation (including the `Load`-stage/load-diagnostic contract) tests. Both sets are
  **PR-fast**. Headless view rendering and the screenshot set are
  **PR-medium**. Platform runs are manual. PR-fast tests assert functionality, ordering, and asynchrony, never an
  absolute duration; timing is recorded as non-gating evidence on a benchmark or manual run (§13/§16 of the
  23A page, §14 of the 23B page).
- **Screenshots.** Baselines are generated and compared on `ubuntu-latest` only, with a fixed headless render
  scale and a documented tolerance and regeneration command. Windows/macOS screenshots are manual platform-gate
  evidence and are not pixel-compared: text rendering differs per platform, so a cross-OS pixel gate would be a
  permanent false failure. Screenshots stay out of the wall-clock gate and never block a PR-fast run.
- **Accessibility.** Primary shell controls are asserted in the PR-fast set (names, roles, help text, no
  icon-only control without a name, tab order). The full visual-tree sweep is PR-medium.
- **Timers.** No `Task.Delay` and no real sleeps in tests; autosave and recovery are driven by `ManualTimeProvider`
  and the scheduler's injectable interval.
- **Determinism.** Headless tests run with invariant culture, a fixed render scale, and no network. A test that
  needs a time zone, case-sensitivity rule, or path separator states it explicitly instead of relying on the
  host.

## 12. CI pipeline

### Pull request

1. `dotnet format --verify-no-changes`
2. `dotnet build -c Release -warnaserror`
3. `dotnet test` (unit, property, contract, short integration)
4. `cargo fmt --check`
5. `cargo clippy -- -D warnings`
6. `cargo test`
7. Python `pytest`, wheel build, and clean-install check
8. Contract fixture round-trips, manifest drift, and space-group catalog drift check
9. Avalonia headless UI tests
10. License/header check and dependency audit

Matrix: automatic runs use `ubuntu-latest` only. Step closeout and `main` use path-filtered jobs. Windows and
macOS are manual platform gates. .NET 11 SDK; stable Rust; Python 3.13 automatic (3.11/3.12 manual/nightly).
Cache NuGet, Cargo, pip, and the SQLite build.

### Nightly

- Long integration (10⁶-cycle KMC/MMC)
- Statistical parity suite (reduced sample count)
- Criterion/BenchmarkDotNet with regression thresholds
- Fuzzing (time-boxed)
- Screenshot matrix
- Packaging smoke test (self-contained binaries on each OS)

### Release candidate

- Full statistical parity with high sample counts
- 20×20×20 stress test
- 10⁴-job build/collect test
- Upgrade/read test for project migration and previous-major `.msl`/`.msr` fixtures
- Manual GUI checklist on all three OSes

## 13. Performance targets and benchmarks

| Scenario | Target |
| --- | --- |
| Compile ceria model (Step 08, no jobs) | < 1 s on the benchmark laptop class |
| Compile 10k P1-position model (Step 08) | < 30 s, < 2 GiB managed memory |
| Create ceria parameterization template + summary (Step 09) | < 20 ms |
| Apply + parameterization signature, ceria (Step 09) | < 50 ms |
| Apply + signature, synthetic 1k-position/4-particle/100-table/10k-rule model (Step 09) | < 2 s, < 256 MiB |
| Parameterization signature throughput (Step 09) | >= 100 MB/s single-threaded, streaming |
| Build compiled model (ceria, 100 jobs) | < 5 s on a 4-core laptop |
| Build 10⁴ small jobs | < 10 min, < 2 GB peak RSS |
| CLI `build` ceria job sample (6 jobs) + verify | < 3 s warm on the benchmark laptop class |
| CLI `build` 10⁴ small jobs + verify | < 10 min, < 2 GB peak RSS |
| CLI `doctor` | < 2 s |
| Load `.msl` and start job | < 100 ms |
| `mocassin-sim --dry-run`, ceria fixture (Step 13) | < 250 ms end to end, < 64 MiB peak RSS |
| ChaCha12 `next_u32` (Step 13) | ≥ 250 M draws/s single-threaded, release, benchmark laptop class |
| ChaCha12 `next_f64` / `next_bounded` (Step 13) | ≥ 120 M / ≥ 200 M calls/s |
| RNG state export + import / 64-word refill (Step 13) | ≤ 500 ns / ≤ 250 ns |
| Solver context init, 10×10×10 fixture (Step 14) | < 100 ms release, warm, same-runner-class; `MemoryReport` ≤ 64 MiB |
| Solver context init, `msl-v1` 6×6×6 and populated 2×2×2 (Step 14) | < 20 ms each; zero allocations after construction |

Step 13 records the load and RNG measurements as same-runner-class entries in
`tests/reference/bench/baselines.json`; the 100 ms/250 ms values are fixture budgets, not a promise for a
512 MiB package.
| KMC throughput (ceria 10×10×10, 1 transition set) | ≥ 80 % of original C solver per core |
| MMC throughput (same) | ≥ 80 % of original C solver per core |
| Memory (20×20×20, 63 particles) | < 2 GB |
| GUI cold start | < 2 s |
| Open/save typical project | < 500 ms |
| 3D viewport (10⁵ atoms) | ≥ 30 FPS on integrated GPU |
| P1 expansion (64 P1 positions, 20×20×20 = 512,000 positions) | < 1 s mean on `ubuntu-latest`, .NET 11 Release |
| Structure preview (ceria and barium zirconate) | < 100 ms mean on `linux-x64-github-ubuntu-latest-dotnet10` |

Benchmarks:

- `benches/energy.rs`, `benches/kmc.rs`, `benches/mmc.rs`, `benches/context.rs` with Criterion.
- `Mocassin.Benchmarks` with BenchmarkDotNet for compilation, serialization, and `.msl` writes.
- `Mocassin.Benchmarks` includes the Step 02 P1 expansion/reduction fixture, the Step 03 structure-preview
  fixture (`StructurePreviewCeria`, `StructurePreviewBariumZirconate`), and the Step 09 parameterization
  create/apply/signature/round-trip fixtures. Benchmarks are dispatched manually via `benchmark.yml`.
- A reproducible benchmark package in `tests/reference/bench/` with fixed seeds and inputs.
- Baselines are stored per runner class (OS, CPU class, .NET/Rust version) in
  `tests/reference/bench/baselines.json`; comparisons use the same class only.
- When a baseline class is unavailable, compare an A/B run on the same machine and use a noise floor before
  failing the 10 % gate. Never compare absolute timings across different CI machines.
- Release reports include the benchmark environment, commit, and raw results.

## 14. Code quality

- Nullable reference types enabled; warnings as errors.
- `TreatWarningsAsErrors`, analyzers, and `.editorconfig` for both C# and Rust.
- Public APIs documented with XML comments; `GenerateDocumentationFile`.
- Rust: `#![forbid(unsafe_op_in_unsafe_fn)]`, `#![deny(missing_docs)]` on public crates, `clippy::pedantic`
  selectively.
- No `unsafe` in safe parser code; any `unsafe` block has a `// SAFETY:` comment and a test.
- Dependency policy: prefer BCL/std; new dependencies require justification in the PR and a licence check.
- Every bug fix adds a regression test.
- Coverage and mutation policy: see §7.

## 15. Documentation

- **Narrative documentation** uses **Zensical** with `docs_dir = "docs"`. `docs/planning/` is excluded from
  the published site via `exclude_docs`.
- Required standalone sections:
  - user guide (including installation, model building, building/running, results, visualization, MMCFE);
  - physics explanation (MMC/KMC, energies, symmetry, transport, tracking, units);
  - developer architecture guide (code structure, design choices, data flow, testing, release);
  - tutorials (ceria KMC/MMC, BaZrO₃, vehicle, parameter sweep, HPC, analysis);
  - CLI + formats reference, diagnostics, troubleshooting/FAQ, performance/HPC, contributing, migration,
    glossary/notation/units, examples/publications.
- **API references** are generated, not hand-written:
  - .NET via **DocFX** under `/api/dotnet/`;
  - Rust via **rustdoc** (`cargo doc`) under `/api/rust/`;
  - Python via **mkdocstrings** under `/api/python/`;
  - `docs/api/index.md` is the landing page and cross-links to all three.
- **One published site.** Zensical owns the root; API outputs are copied into `site/api/dotnet` and
  `site/api/rust` after the Zensical build.
- **Strict builds.** `zensical build --strict` fails on broken links/anchors; DocFX, rustdoc, and mkdocstrings
  fail on missing public API docs/docstrings/import errors. The combined site is built locally and by the manual
  `docs.yml` workflow; it is not part of the automatic CI gate.
- **Docs-as-code.** Step 00 creates the Zensical skeleton, navigation, build scripts, and Docs CI job. Every
  feature step updates its pages and keeps the strict build green. Step 30 completes coverage, cross-cutting
  content, and API integration; Step 31 packages the site artifact with the release.
- **Tutorials are executable.** CI runs the documented commands against `samples/` and validates expected
  outputs.
- `docs/development/testing-and-parity.md` documents the trophy model, the mock/fake policy, fixture
  regeneration, and the test tiers from this page.
- Each step page is updated to `Done` with links to the PRs and any deviations.
- ADRs record decisions that affect formats, public APIs, or scientific semantics.
- In-app help links point to stable documentation anchors.
- Planning docs remain internal; they are not part of the published user/developer site.

## 16. Release gates

A release is blocked if any of the following is true:

- A parity matrix item is marked required but not implemented.
- Statistical parity fails for a reference system.
- A `.msl`, `.msr`, `.mcs`, job-folder, or event contract test fails on any OS, or the contract manifest is
  stale.
- Fuzzing finds a crash, OOM, or unbounded allocation.
- A critical interface in §6 has no acceptance test.
- A required accessibility check (WCAG 2.2 AA or a manual screen-reader check) fails.
- A performance benchmark regresses by more than 10 % without an accepted ADR.
- A GUI workflow in the release checklist fails.
- Licence/attribution files are missing.
- The Zensical/DocFX/rustdoc/mkdocstrings documentation build fails or has broken links.
- The Python package fails to build/install, or its pytest suite fails.
