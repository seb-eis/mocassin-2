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
| UI (headless) | Moderate | Real view models and project objects; commands, validation, navigation | xUnit + Avalonia headless | Every PR (subset) / nightly (screenshots) |
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
| Parameterization | Signature stability; apply → serialize → reload identity; rebase never silently changes unmatched values; chiral partners share one `EnergyId` and one value |
| Supercells / doping | Requested vs applied counts; never exceeds available particles; charge neutrality or explicit failure; order dependence is deterministic; same seed → same lattice |
| Package / state | Encode → decode identity for all fields and versions; unknown fields ignored / unknown major version rejected; truncated input fails cleanly; checksum verification |
| Results store | `.msr` package binding, package-signature mismatch rejection, idempotent collection, previous-major read |
| Job contracts | `solver.json`, job-folder paths, event stream, and `mmcfe.db` round-trip and reject malformed input |
| Evaluation | MSD/diffusion/mobility/conductivity identities on analytic random walks; non-negativity; unit consistency; empty and single-particle cases |
| CLI / parsing | Range parsing, partial selections, exit codes, JSON schema; no crash on arbitrary arguments |
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
6. Avalonia commands against real project documents and view models.
7. CLI `build`/`simulate`/`collect`/`evaluate` workflows.
8. Contract-manifest drift checks across C#, Rust, Python, and generated fixtures.

Each acceptance test should fail for a user-visible reason, not because an internal mock was not called.

## 7. Coverage and mutation testing

Coverage is a diagnostic, not the headline target.

- **Required:** critical-path coverage for the interfaces in §6 and property/invariant coverage for the
  algorithms in §5.
- **Retained:** line/branch coverage reports for `Mocassin.Core`, `Mocassin.Symmetry`, `Mocassin.Compilation`,
  `Mocassin.Contracts`, `Mocassin.Packages`, `Mocassin.Results`, `mocassin-solver`, and `mocassin-state`,
  reviewed for untested branches.
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
  records counts, indices, interaction energies, rule state triplets, and tracker mappings.
- Invariants:
  - every active transition rule has valid particle indices;
  - mass and charge are conserved per rule (except explicitly allowed charge-transfer rules);
  - every rule has a corresponding direction and collection entry;
  - every stable pair interaction has a table entry;
  - every cluster permutation maps to a valid occupation code;
  - P1 counts match brute-force expansion.
- Determinism: compiling the same model twice yields byte-identical payloads.
- Cache invalidation tests edit one domain at a time and assert only the affected stages are rebuilt.

### 9.4 Supercell and doping

- Doping fractions respect integer truncation and dependent-exchange ratios.
- Charge compensation is exact (total charge within tolerance) or the build fails with a diagnostic.
- Doping order is respected; changing priority changes the lattice deterministically.
- Same seed + same job coordinates yields the same lattice across runs.
- Edge cases: zero doping, 100 % doping, supercell too small for requested fraction, dependent exchange
  impossible, multiple dopings on the same site, vacancy default occupation.
- Property test: for random valid inputs, the number of replaced particles never exceeds the available
  count and every replaced particle came from the source set.

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
  [`07-reference-build.md`](07-reference-build.md); the original clone stays read-only.

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

## 10. Contract tests

- **Project JSON**: .NET writes fixtures; a JSON Schema validator checks them in CI.
- **Compiled model**: .NET writes `.msl`; Rust reads it, validates invariants, and emits canonical JSON;
  compare with the .NET canonical JSON.
- **Canonical signatures**: `tests/contracts/signature-vectors.json` fixes the canonical MessagePack
  projection bytes and SHA-256 digests for model, parameterization, and lattice. C# and Rust must reproduce
  them; a serializer change must not change a signature.
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
  OOMs are release blockers.

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
- Commands are tested through their observable effects: project state, diagnostics, generated files, and
  navigation — not through mock verification.
- Manual smoke checklist per release: create project, add each object type, validate, parameterize, build,
  run a tiny job, collect, plot, save/reopen.

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
macOS are manual platform gates. .NET 10 SDK; stable Rust; Python 3.13 automatic (3.11/3.12 manual/nightly).
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
| Build compiled model (ceria, 100 jobs) | < 5 s on a 4-core laptop |
| Build 10⁴ small jobs | < 10 min, < 2 GB peak RSS |
| Load `.msl` and start job | < 100 ms |
| KMC throughput (ceria 10×10×10, 1 transition set) | ≥ 80 % of original C solver per core |
| MMC throughput (same) | ≥ 80 % of original C solver per core |
| Memory (20×20×20, 63 particles) | < 2 GB |
| GUI cold start | < 2 s |
| Open/save typical project | < 500 ms |
| 3D viewport (10⁵ atoms) | ≥ 30 FPS on integrated GPU |
| P1 expansion (64 P1 positions, 20×20×20 = 512,000 positions) | < 1 s mean on `ubuntu-latest`, .NET 10 Release |
| Structure preview (ceria and barium zirconate) | < 100 ms mean on `linux-x64-github-ubuntu-latest-dotnet10` |

Benchmarks:

- `benches/energy.rs`, `benches/kmc.rs`, `benches/mmc.rs`, `benches/context.rs` with Criterion.
- `Mocassin.Benchmarks` with BenchmarkDotNet for compilation, serialization, and `.msl` writes.
- `Mocassin.Benchmarks` includes the Step 02 P1 expansion/reduction fixture and the Step 03 structure-preview
  fixture (`StructurePreviewCeria`, `StructurePreviewBariumZirconate`).
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
