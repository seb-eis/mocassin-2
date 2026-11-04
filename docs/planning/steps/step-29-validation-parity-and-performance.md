# Step 29 — Validation, Statistical Parity, Performance, and Hardening

**Phase:** G. Release  
**Depends on:** 16–22
**Delivers:** A validated release candidate: statistical parity with the original solver, performance within
targets, fuzzed/robust parsers, and a documented scientific validation suite.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** —
- **Last updated:** —

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Prove that the reimplementation is scientifically faithful and production-ready. This step is not a final
bug-fix pass; it is the formal validation and hardening phase, run continuously from the first KMC/MMC
implementation.

## Original reference

- The original archive has no solver tests, no symmetry tests, no CI, and no statistical validation suite.
- Published results and the user guide provide reference systems (ceria, barium zirconate) and formulas.
- The original C solver can be compiled separately as a reference oracle; it is not shipped.

## Design

### 1. Reference systems

Build reproducible fixtures in `tests/reference/`:

| System | Purpose | Key physics |
| --- | --- | --- |
| Reduced ceria | Baseline KMC/MMC | O vacancy migration, polaron hopping, Ce/O dopings |
| Y-doped BaZrO₃ | Proton/vacancy transport | Hydration dopings, multiple mobile species |
| Vehicle demo | Complex mechanisms | 5-site vehicle, static linkers, unidirectional rules |
| Interstitialcy demo | Complex mechanisms | 5-/7-site push chains |
| Two-state lattice | Analytic | Detailed balance, residence time |
| 1D random walk | Analytic | MSD/diffusion |
| Single charge in field | Analytic | Mobility/conductivity sign |

Each fixture includes:

- project JSON (new format);
- parameterization with documented values;
- package (or a build recipe) and its `.msr` results;
- expected observables with sample size/uncertainty;
- the original solver version/commit/flags used for reference values (where applicable).

### 2. Statistical parity protocol

For each reference system:

1. Build the same physical model/parameterization in the new format.
2. Run multiple independent seeds (e.g. 8–32) for both solvers.
3. Compare:
   - mean lattice energy (MMC), acceptance/rejection/skip/blocked fractions;
   - conductivity/mobility vs temperature (Arrhenius prefactor/activation energy);
   - diffusion coefficients and Haven ratio;
   - jump-barrier histograms;
   - particle densities/occupation statistics.
4. Acceptance:
   - intensive quantities within `max(3σ, 2 %)`;
   - transport coefficients within `max(3σ, 5 %)`;
   - distributions via KS test with an effect-size threshold;
   - no systematic trend across temperatures/dopings.
5. Record raw results, analysis scripts, and a summary in `tests/reference/reports/`.

- The original C solver is used only to generate reference data; it is not a runtime dependency.
- If a discrepancy is found, bisect with a per-cycle trace and a minimal system before changing physics.
- Document any intentional deviation (e.g. attempt histograms, per-species transport, origin-based MSD) and
  validate the new quantity independently.

### 3. Physics test suite

Expand step 15–18 tests into a release suite:

- detailed balance for MMC;
- KMC residence time and time-step;
- normalization invariance;
- field sign/magnitude;
- pair/cluster/site energy hand calculations;
- mass/charge conservation per accepted step;
- tracker/MSD/diffusion analytic;
- conductivity/Nernst–Einstein consistency, per species and total;
- origin-based MSD normalization uses the tracked ensemble count;
- restart/resume equivalence;
- determinism (same seed → same state) on each platform.

### 4. Performance benchmarks

| Scenario | Target |
| --- | --- |
| Ceria 10×10×10 KMC, 10⁸ cycles | ≥ 80 % of original C per core |
| Ceria 10×10×10 MMC, 10⁸ cycles | ≥ 80 % of original C per core |
| Context init | < 100 ms |
| Job load/decode | < 100 ms |
| State write | < 50 ms for 10⁶ positions |
| 20×20×20 memory | < 2 GB |
| Package build 10⁴ jobs | < 10 min, < 2 GB |
| CLI evaluate 10³ jobs | < 1 min |

- Criterion benches in `src/rust/benches/`; BenchmarkDotNet for .NET compilation/serialization.
- Store baselines per runner class in `tests/reference/bench/baselines.json`; compare only within the same
  class (OS, CPU class, runtime version).
- When no baseline exists, use a same-machine A/B run and a noise floor before applying the 10 % gate. Never
  compare absolute timings across different machines.
- Profile with `perf`/Instruments/Windows Performance Analyzer; record flamegraphs.
- Optimize only after correctness; keep a performance log per release.
- Regressions > 10 % block release unless an ADR accepts the trade-off.

### 5. Robustness and fuzzing

- Fuzz:
  - project JSON parser (SharpFuzz);
  - `.msl` reader (cargo-fuzz): truncated/oversized blobs, bad indices, bad Zstd, schema versions;
  - `.mcs` reader (cargo-fuzz): malformed MessagePack, wrong types, oversized arrays, invalid indices,
    unsupported versions;
  - extension config parser (cargo-fuzz);
  - `.msr` reader (SharpFuzz/Hypothesis): package-binding mismatch, corrupt blobs, missing rows, wrong hashes;
  - job-folder/`solver.json`, event stream, and MMCFE log parsers (SharpFuzz/Hypothesis).
- Canonical signature vectors pass for C# and Rust; previous-major `.msl`/`.msr` fixtures still read.
- Security cases: path traversal, symlinks, zip-slip, decompression bombs, malicious SQLite pages, and
  extension panic/exception containment.
- Allocation caps and timeouts for parsers.
- Property tests for geometry, supercells, rule generation, and state round-trip.
- Negative tests for every diagnostic/error code.
- No `unsafe` without a `// SAFETY:` comment and a test; run Miri on core crates where feasible.

### 6. Cross-platform validation

- CI matrix: Windows 11, Ubuntu 24.04, macOS 14/15 (arm64 + x64 where available).
- Same package + seed:
  - identical state content (excluding timestamp/runner metadata) on the same platform/build;
  - statistical equivalence across platforms.
- Floating-point checks: no fast-math/FMA contraction that changes results; document compiler flags.
- Path/encoding/locale tests:
  - non-ASCII project names/paths;
  - invariant numeric parsing;
  - CRLF/LF handling;
  - read-only directories and long paths.
- 3D viewer smoke test on each OS.

### 7. Security and data safety

- Treat project/package/results/state/job-folder/event input as untrusted.
- Validate all counts before allocation; cap job/model sizes and Zstd decompression.
- Atomic writes everywhere; no overwriting a valid file on failure.
- Path traversal, symlink, and zip-slip prevention in collect/deploy/import/extension paths.
- Extensions opt-in; document the trust boundary, panic containment, and `--no-extensions`.
- `.msr` must match the package signature; reject mismatches.
- Dependency audit (`cargo deny`, NuGet audit) and licence check.
- No telemetry/network calls by default.

### 8. Documentation and scientific review

- `docs/physics/` — formulas, units, conventions, deviations.
- `docs/reference/` — project/package/state schemas, CLI, diagnostics.
- `docs/user-guide/` and `docs/tutorials/` — rewritten chapters and worked examples matching the new UI.
- `tests/reference/README.md` — how to reproduce parity runs.
- A scientific reviewer checks:
  - energy conventions;
  - normalization/time step;
  - tracker semantics;
  - transport formulas;
  - doping/charge compensation.
- A code reviewer checks safety, error handling, and performance.

### 9. Release gates

- [ ] All reference systems pass statistical parity.
- [ ] All physics/contract/UI tests pass on three OSes.
- [ ] Fuzzing finds no crash/OOM/timeout.
- [ ] Performance targets met or deviations accepted.
- [ ] No critical/high dependency vulnerabilities.
- [ ] Parity matrix has no required item missing.
- [ ] Docs/reference reports complete.
- [ ] Packaging smoke test passes.

## Implementation tasks

- [ ] Build the reference fixtures and parameterizations.
- [ ] Implement the parity harness (multi-seed runs, aggregation, stats tests).
- [ ] Run the original C solver to generate reference observables (documented, not shipped).
- [ ] Expand physics tests and add per-cycle trace tooling.
- [ ] Implement Criterion/BenchmarkDotNet suites and baselines.
- [ ] Add fuzz targets and CI jobs.
- [ ] Run cross-platform/locale/path tests.
- [ ] Complete the security/dependency audit.
- [ ] Write physics/format docs and run scientific review.
- [ ] Fix discrepancies and record deviations.

## Acceptance criteria

- [ ] Ceria KMC/MMC pass parity at 773 K and 1073 K within tolerances.
- [ ] Barium zirconate hydration/transport passes parity.
- [ ] Vehicle/interstitialcy mechanisms produce the expected rule sets and statistics.
- [ ] Analytic tests pass exactly/within tolerance.
- [ ] Performance is within 20 % of C per core on the reference hardware.
- [ ] 10⁴-job build and 10³-job evaluation meet budgets.
- [ ] Manifest-generated contract checks are current and all three readers agree.
- [ ] Fuzzing and negative tests are clean.
- [ ] Three-OS CI is green; WCAG 2.2 AA and manual screen-reader checks pass.
- [ ] Current + previous major `.msl`/`.msr` fixtures read successfully; `.mcs` rejects old major versions.
- [ ] Scientific review sign-off is recorded.
- [ ] Documentation written

## Documentation

At this point, document validation/parity/performance results:

- `docs/development/testing-and-parity.md` — reference systems, tolerances, reproducible parity protocol.
- `docs/performance/benchmarks.md`, `profiling.md` — benchmark methodology and results.
- `docs/troubleshooting/performance.md` — diagnosing slow jobs/builds.
- `docs/examples/` — validated example outputs and reference observables.

## Tests

| Test | Assertion |
| --- | --- |
| Acceptance audit | Every critical interface in the quality strategy has a real-object acceptance test |
| Test-double audit | Remaining fakes are limited to process/UI/time/failure-injection boundaries |
| Parity | Mean/σ/D/histograms within tolerance |
| Arrhenius | Prefactor/Ea match reference |
| Determinism | Same seed → same state per platform |
| Cross-platform | Statistical equivalence |
| Python reader | `pytest` fixture parity with .NET/Rust |
| Contract manifest | Generated schemas/constants current; DTO drift check passes |
| Signature vectors | C# and Rust reproduce canonical projection digests |
| Results store | `.msr` package binding, hash verification, previous-major fixture |
| Job contracts | `solver.json`, events, and `mmcfe.db` fixtures parse in .NET/Python |
| Security | Path traversal, symlink, zip-slip, decompression, and extension failure cases |
| Fuzz | No panic/OOM |
| Performance | Benchmarks within targets |
| Locale/path | Non-ASCII/read-only/long paths |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Late parity failures | Run parity continuously from step 16 |
| Statistical false positives | Multiple seeds, effect sizes, pre-registered tolerances |
| Performance regressions | Benchmarks per PR/release, profiling |
| Fuzz findings late | Start fuzzing at step 13, run nightly |
| Scientific disagreement | Written conventions + independent review |

## Done when

The release candidate is statistically equivalent to the original on the reference systems, meets performance
targets, survives adversarial input, and has documented scientific sign-off.
