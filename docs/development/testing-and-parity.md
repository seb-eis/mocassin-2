# Testing and Parity

This page summarizes the testing strategy. The internal plan contains the full quality strategy.

## Testing trophy

| Layer | Relative effort | Typical tools |
| --- | --- | --- |
| Contract / acceptance | Highest | xUnit, Rust integration tests, pytest, manifest fixtures |
| Property / invariant | High | FsCheck, `proptest`, Hypothesis |
| Golden / toy integration | High | xUnit + CLI + Rust + SQLite |
| Unit | Selective | xUnit, Rust `#[test]`, pytest |
| UI (headless) | Moderate | Avalonia headless tests |
| Statistical / performance | Nightly / release | Custom harness, Criterion, BenchmarkDotNet |

A test that only proves a private helper was called is replaced by a test of the observable result. Every bug
fix adds a regression test; every scientific change adds a physics or parity test.

## Mocks and fakes

Mocks and fakes are allowed at true process, OS, UI, network, or time boundaries, and for failure injection.
They are not allowed for domain objects, compilers, symmetry, energy/evaluation code, package writers/readers,
or SQLite. Prefer real temporary directories and real files produced by the real writers.

## Fixtures

- Real models first: ceria, yttrium-doped barium zirconate, and vehicle/interstitialcy systems.
- Generated toy data for breadth, with the seed in failure output.
- `.msl` and `.mcs` fixtures are produced by the real writers, never hand-authored binary.
- Fixtures are versioned, small, deterministic, and regenerable with a recorded command.
- `tests/contracts/` holds cross-language fixtures; `tests/reference/` holds parity and benchmark assets.

## Simulation-package fixtures (Step 11)

- `tests/contracts/packages/msl-v1/` is the checked-in `.msl` v1 fixture written by the real writer, with its
  `package-summary-v1` golden and extracted payloads. Regenerate it with
  `MOCASSIN_REGENERATE_PACKAGE_FIXTURES=1 dotnet test src/dotnet/Mocassin.Results.Tests/Mocassin.Results.Tests.csproj -c Release --filter "FullyQualifiedName~RegeneratePackageFixture"`.
- `tests/contracts/packages/amend-1-net10/` is the pre-.NET 11 compatibility package. It is byte-fixed and never
  regenerated; the net11 reader must keep reading it.
- `tests/contracts/packages/amend-1/` holds the pre-amendment codec goldens (raw payloads and frames at levels 3
  and 9); bytes may be re-captured with a recorded note, and the level mapping must not change.
- Schema, payload mapping, signature, atomicity, immutability, and `PKG` checks are PR-fast. A rebuild-and-compare
  of the canonical summary and the Step 13 cross-language read are PR-medium. The 10⁴-job budget is nightly/
  release. The Step 11 `.msl` fuzz harness is deferred to Step 29 (owner: Step 29 implementor) with the SharpFuzz
  targets; Step 11 keeps the fixed corruption, truncation, wrong-signature, foreign-key, and decompression-bound
  tests and claims no fuzz run.
- The canonical summary is the golden. Package bytes are recorded by SHA-256 and compared only as an observation,
  because a runtime patch may change compressed bytes without changing the logical content.

## Rust solver foundation (Step 13)

- The checked-in `tests/contracts/packages/msl-v1/` package is read by the Rust reader; the dry-run counts and
  signatures must equal the canonical summary for the same job. The byte-fixed `amend-1-net10/` package is read
  the same way. `tests/contracts/packages/step-13-populated/step-13-populated.msl`, built by the real writer
  from `samples/step-13-populated/step-13-populated.mocprj` (4 particles, 3 positions, 6 pair tables, 1 cluster
  table, 2 collections, 8 rules, one KMC and one MMC job), exercises the populated parameterization and MMC
  paths. The extended O2 `{O, Vc, H}` set gives the fixture a **non-trivial per-node mask difference** at KMC
  node 0 and MMC nodes 0/1; the Rust and .NET tests assert it. An equivalent-start mapping is impossible with
  the current Step 08 compiler (`DirectionKey` includes the start `P`, so `EquivalentStartPositions` can never
  differ from the representative start); the reader accepts a cross-bucket direction through the synthetic
  mapping test. The earlier P-1 `PKG010` note is not reproducible (a plain P-1 variant verifies). This is
  recorded in the Step 13 page, both fixture READMEs, and amendment `step-13-amend-1`. All reads are
  PR-medium.
- The Rust test decodes the raw payloads in `tests/contracts/packages/msl-v1/payloads/` and the populated
  package's extracted payloads, which catches field-name, minimum-width-integer, `null` optional, and
  array-vs-`bin` lattice drift without a .NET dependency.
- `tests/projections.rs` decodes every `tests/contracts/projections/parameterization/*.bin` and
  `.../lattice/*.bin` vector with a strict fixed-width reader, builds the runtime model/lattice, and re-encodes
  with the **production** `mocassin_msl::projection` functions. It must reproduce the bytes, the projection
  SHA-256, and the domain signature, and it reads `tests/contracts/signature-vectors.json` to check the
  id/signature/digest entries. This is PR-fast.
- The reader checks mass/charge conservation per rule, `path_vector`/`length` against `movement` and the cell
  vectors, and the zero-transport rule when no charge changes. Void is exempt from the occupation check; a
  non-void state particle must be in the collection-union mask, and the real `msl-v1` rules must pass. The exact
  per-direction occupation check and the full positional transport formula are recorded Step 16/15 assignments
  because the v1 payload has no representative-direction marker or fractional P1 coordinates.
- `jump_mapping` accepts a direction in any symmetry-equivalent start bucket; the reader does not require
  `direction.position_id == position`. The real populated fixture has no equivalent-start mapping (see above),
  so this acceptance is exercised by the synthetic mapping tests; the exact per-direction occupation check
  remains Step 16's.
- The model-element budget is 512M, covering a writer-valid 512 MiB payload; the 1M-rule/16M-link caps are
  per-field, and a caller that raises `CompilationOptions.MaxRuleCount` must raise `ReaderLimits`.
  `every_per_field_count_cap_is_enforced` exercises the rules, directions, collections, pair/cluster tables, and
  both link caps with lowered limits, then accepts the same payload with raised limits.
- `tests/contracts/rng-vectors.json` freezes the domain strings, seed derivation, draw streams, bounded draws,
  forks, and the `RngStateV1` round-trip, including a nonzero-stream resume. Rust reads the document; a .NET
  test asserts the job/lattice/seed-hash projections from the same file. A changed algorithm, draw order, or
  state layout fails the vector test.
- A proptest suite truncates and mutates the real model/job payloads, feeds arbitrary bytes to the decode
  entry points, and mutates SQLite pages of a real package copy; it asserts no panic, no unbounded allocation,
  and a structured error. The excluded `crates/mocassin-msl/fuzz` workspace has `payload_scan`,
  `payload_decode`, and `package_open` targets seeded from the same fixtures. The Step 13 time-boxed run
  (5 s per target, x86_64-unknown-linux-gnu) executed 186,312 / 158,648 / 6,535 inputs with zero crashes; Step 29
  extends fuzzing to `.mcs` and extension configs and owns continuous runs.
- Load and RNG benchmarks are recorded in `tests/reference/bench/step-13-*.json` with the toolchain, CPU class,
  and commit, plus the `rustLoad`/`rustRng` same-runner-class entries in
  `tests/reference/bench/baselines.json`. The ceria load medians are open 171.1 us, cold job 343.1 us, and warm
  job 71.1 us against the `< 100 ms` fixture budget (not a 512 MiB-package promise; peak RSS is not measured by
  the Criterion bench). A same-class rerun on the busy container measured open 271.0 us / cold 351.4 us / warm
  68.0 us; the open variance is environment noise, so the baseline is flagged **boundary-calibrated for Step 29**
  (the auditor saw +10.4 %). The 10 % rule is not relaxed. The RNG budgets are in the Step 13 page §4.5.
- The reader rejects a job row whose model or lattice row is absent as a package-class error (exit 11), applies
  `--seed` through `Package::load_job_with_seed` so `JobPackage.seed` is the effective seed, requires non-empty
  provenance strings, and rejects `is_vacancy[0] == true` (the writer marks the void particle `false`). Each has
  a fixture-mutation or real-package test.
- The solver exit-code rows in the CLI/architecture/step page are compared by a doc-drift check; the canonical
  meanings are architecture §7.3, including `13` numerical/internal invariant and `14` limit abort.
- Tests use real files, real SQLite, and the real checked-in packages. There are no mocks in the reader; the only
  process boundary is the CLI exit-code test, which runs the built binary.

## Test tiers

| Tier | Contents | Target |
| --- | --- | --- |
| PR-fast | Unit, property, contract checks, one tiny end-to-end run | 5–10 minutes |
| PR-medium | More model variants, cross-language fixtures, manifest drift | 20 minutes |
| Nightly | Reduced-sample parity, long runs, fuzzing, benchmarks | No wall-clock gate |
| Release | Full parity, stress, packaging, upgrade tests, manual GUI checklist | Release gate |

PR-fast tests must not depend on nightly-only assets.

## Validation and diagnostics

- The `ProjectValidationPipeline` runs load → model → cross-domain → feasibility and never runs build-stage
  checks in `validate`; build stages are gated by `RunBuildStage`.
- Validation never mutates the project; model and document hashes are unchanged after success, failure, or
  cancellation.
- `DiagnosticCatalog` is the stable code registry. Assembly-local tests prove every public Core constant and
  every internal Symmetry constant is registered exactly once, the composed catalog is the union plus package
  codes, every catalog code is documented, and no duplicate or undocumented code exists.
- Every emitted diagnostic has a real invalid fixture or focused test with severity, `ModelPath`, and hint.
  Existing Step 03–06 tests count for severity; Step 07 adds its cross-domain, feasibility, and package cases.
- Diagnostics are sorted by severity, code, domain, object ID, property, and message. Exact duplicate
  `(code, path, message)` triples are removed.
- `ENM018` does not cascade after a path-resolution error. `SIM038`–`SIM041` cover buildability and hard
  feasibility limits.
- `validation-v1` JSON is schema-checked, including load-failure reports with `null` identity fields; exit codes
  distinguish valid, warning-as-error, parse failure, usage, and I/O failure.
- Statistics are `null` only when not requested, after a load failure, after a model-stage error, or after a
  failed preview; feasibility/budget errors can coexist with non-null statistics.
- Ceria validation targets `< 200 ms` without statistics and `< 2 s` with `--stats`.

## Compilation

- The compiler is tested against real projects and the real Step 02/04/05/06 services: no mocks for
  symmetry, energy sampling, transition paths, or compiled tables.
- Golden compiled models for ceria, barium zirconate, and vehicle/interstitialcy record P1 counts, pair and
  cluster tables, rule state triplets, tracker mappings, intent, and signatures.
- Brute-force oracles cover P1 expansion, pair shells/multiplicities, cluster canonicalization, and rule
  conservation. Compiled pair/cluster counts are cross-checked against `EnergyPreviewService`; this closes
  the Step 05 preview/compiler consistency follow-up.
- Property tests assert dense indices, unique `state0` per collection, reachable directions/rules, tracker
  mapping consistency, and mass/charge conservation.
- Determinism tests compile twice and compare canonical JSON and signatures; Ubuntu is automatic, Windows and
  macOS are manual golden gates.
- Cache tests use a recording cache and edit one domain at a time to prove the invalidation graph without
  asserting private call counts, including active-transition-set changes versus runtime-settings-only edits.
- Cancellation and performance tests cover the ceria sample (`< 1 s`) and a 10k-position model
  (`< 30 s`, `< 2 GiB`).
- Every `CMP` code has an emission test, catalog completeness check, and documentation-drift check.

## Statistical parity

Reference observables are compared with the original solver using confidence intervals or Welch's t-test and
distribution tests. Bit-exact trajectories are not required. Reference builds record the original commit,
compiler, flags, package pins, seeds, and hashes; the original checkout is treated as a read-only oracle.
