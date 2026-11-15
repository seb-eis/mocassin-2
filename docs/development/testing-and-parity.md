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
  of the canonical summary and the Step 13 cross-language read are PR-medium. Corruption/fuzz and the 10⁴-job
  budget are nightly/release.
- The canonical summary is the golden. Package bytes are recorded by SHA-256 and compared only as an observation,
  because a runtime patch may change compressed bytes without changing the logical content.

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
