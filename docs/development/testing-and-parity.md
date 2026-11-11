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
  checks in `validate`.
- Validation never mutates the project; model and document hashes are unchanged after success, failure, or
  cancellation.
- `DiagnosticCatalog` is the stable code registry. Tests prove every public code constant is registered exactly
  once, every catalog code is documented, and no duplicate or undocumented code exists.
- Every emitted diagnostic has a real invalid fixture or focused test with severity, `ModelPath`, and hint.
  Existing Step 03–06 tests count; Step 07 adds the cross-domain, feasibility, and package-level cases.
- Diagnostics are sorted by severity, code, domain, object ID, property, and message. Exact duplicate
  `(code, path, message)` triples are removed.
- `ENM018` does not cascade after a path-resolution error. `SIM038`–`SIM042` cover buildability and hard
  feasibility limits.
- `validation-v1` JSON is schema-checked; exit codes distinguish valid, warning-as-error, usage, and I/O
  failure.
- Ceria validation targets `< 200 ms` without statistics and `< 2 s` with `--stats`.

## Statistical parity

Reference observables are compared with the original solver using confidence intervals or Welch's t-test and
distribution tests. Bit-exact trajectories are not required. Reference builds record the original commit,
compiler, flags, package pins, seeds, and hashes; the original checkout is treated as a read-only oracle.
