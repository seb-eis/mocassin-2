# Step 13 — Rust Solver Foundation and Package Reader

**Phase:** D. Solver
**Depends on:** 11
**Delivers:** A Rust workspace with typed package structures, a validated read-only `.msl` reader, a
deterministic ChaCha12 RNG with an exact-resume state, a `mocassin-sim` CLI skeleton with structured logging and
a write-free `--dry-run` job inspector, and the first .NET↔Rust contract fixtures. No Monte Carlo physics.

## Implementation status

- **Status:** **In audit (re-audit required)** — all seven features are squashed; audit round 1 fixes are squashed as `020e70a` and the evidence is rebound to that commit
- **Step branch:** `step/13-rust-solver-foundation-and-package-reader`
- **Step CI:** [Ubuntu 44815711484](https://github.com/seb-eis/mocassin-2/actions/runs/44815711484) — green at `020e70ad4efcb06f79607cfeb7c75e370147cdd9` (re-audit closeout dispatch on the merged remediation tree)
- **Main merge:** —
- **Step tag:** —
- **Audit:** **Re-audit required** — round 1 returned `Fixes required`; resolutions are recorded in [the audit record](../audits/step-13.md) and [closeout evidence](../closeout/step-13.json). F-1 is now closed by the real-writer `step-13-centered` fixture and the Step 08 fix `1f5b473`; the earlier `step-13-amend-1` is withdrawn.
- **Design review:** Ready with notes — accepted by the human owner on 2026-11-18 for freeze `b9cda11`;
  implementation hold lifted 2026-11-19. See [`docs/planning/reviews/step-13.md`](../reviews/step-13.md).
- **Feature list:** Approved by the human owner on 2026-11-18 (seven features); implementation authorized
  2026-11-19.
- **Sizing:** Owner-approved on 2026-11-18 at **4.5 person-weeks** (3.5 pw base + 1.0 pw contingency) as a
  §3.1 single-step exception, no split; the 13A/13B split was considered and rejected. See §13.
- **Last updated:** 2026-11-20 (re-audit remediation)

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| 1 Workspace, types, and error model | `feature/step-13-rust-solver-foundation-and-package-reader/workspace-types-and-errors` | Squashed | `cargo fmt --check`, `cargo clippy -- -D warnings`, `cargo test` (32 tests + 1 doctest), `cargo build --release`, `structure`, `docs --require` all exit 0 | Not run (no per-feature CI) | `9474638dd5917047e6afc66d60c1b8f9e791f630` |
| 2 RNG streams and state | `feature/step-13-rust-solver-foundation-and-package-reader/rng-streams-and-state` | Squashed | `cargo fmt --check`, `cargo clippy -- -D warnings`, `cargo test` (19 unit + 10 vector + 7 property tests in `mocassin-rng`), `cargo build --release`, `cargo bench --no-run`, one full bench run, `structure`, `docs --require` all exit 0 | Not run (no per-feature CI) | `54820f258bccfa021b8efd16e5248f9432656dda` |
| 3 `.msl` reader and bounded decode | `feature/step-13-rust-solver-foundation-and-package-reader/msl-reader-and-bounded-decode` | Squashed | `cargo fmt --check`, `cargo clippy -- -D warnings`, `cargo test` (44 `mocassin-msl` tests: 4 unit, 15 hostile, 12 schema, 5 decode, 8 acceptance), `cargo build --release`, `structure`, `docs --require` all exit 0 | Not run (no per-feature CI) | `70f443181d269ca5547f7216f005f1f39c664b1f` |
| 4 Model/job mapping and signatures | `feature/step-13-rust-solver-foundation-and-package-reader/model-job-mapping-and-signatures` | Squashed | `cargo fmt --check`, `cargo clippy -- -D warnings`, `cargo test` (111 `mocassin-msl` tests: 65 mapping unit, 15 hostile, 12 schema, 5 decode, 9 acceptance, 5 signature), `cargo build --release`, `structure`, `docs --require` all exit 0 | Not run (no per-feature CI) | `087856b73033d20d5607d8dadedf9ee6da6746ca` |
| 5 Dry-run CLI and logging | `feature/step-13-rust-solver-foundation-and-package-reader/dry-run-cli-and-logging` | Squashed | `cargo fmt --check`, `cargo clippy -- -D warnings`, `cargo test` (27 CLI tests: 8 parser unit, 15 process, 4 exit-code drift), `cargo build --release`, `structure`, `docs --require` all exit 0 | Not run (no per-feature CI) | `b2474efdda044f4802cffdab9dd84bd1d42492be` |
| 6 Cross-language fixtures, property tests, and fuzzing | `feature/step-13-rust-solver-foundation-and-package-reader/cross-language-fixtures-and-fuzzing` | Squashed | `cargo fmt --check`, `cargo clippy -- -D warnings`, `cargo test` (RNG vectors 5, projection round-trips 2, populated fixture 2, mutation properties 5; .NET RNG-vector 1, populated-fixture 6), `cargo build --release`, fuzz compile + 5 s runs, `structure`, `docs --require`, `contracts`, `dotnet build`/`format` all exit 0 | Not run (no per-feature CI) | `c0ea5472aafdbf4e680de8cebfce4616b99b70bd` |
| 7 Performance evidence, documentation, and closeout | `feature/step-13-rust-solver-foundation-and-package-reader/performance-docs-and-closeout` | Squashed | `all --require` exit 0; `docs --require`, `contracts`, `structure`, `dotnet build`/`format`, and the load bench pass | Closeout run 44809489478 green at `fe48dc8` | `45eb181656380ce258d20c8c4183727c62d9cc1a` |
| Fix 1 Audit round-1 remediation | `feature/step-13-rust-solver-foundation-and-package-reader/fix-audit-round-1` | Squashed | `all --require` exit 0 (AOT green; real CLI publish fails with 62 IL2xxx/IL3xxx as recorded evidence), `cargo fmt/clippy/test/build`, `structure`, `docs --require`, `contracts`, `dotnet build`/`format`, 7 populated-fixture tests, load bench rerun | Re-audit closeout run 44815711484 green at `020e70a` | `020e70ad4efcb06f79607cfeb7c75e370147cdd9` |

**Feature 1 resequencing (orchestrator-directed sequence, owner-accepted 2026-11-19).** The approved feature
row lists payload DTOs, runtime model/job/lattice types, and the `PackageError`/exit-code model. Feature 1
delivers the workspace/lint baseline, the typed IDs, `ReaderLimits`, the settings and composed job-flag types,
and the exit-code/error-class model. Payload DTOs, the runtime model types, `JobPackage`, `InitialLattice`, and
`PackageError` move to **features 3–4**, where `rmp-serde`, Zstd, `rusqlite`, and the real payload fixtures
exist. The owner accepted this resequencing on 2026-11-19; no design decision changed and the overall feature
list is unchanged.

The exit-code model lives in `mocassin-types` rather than in the CLI: both `mocassin-msl` (feature 3) and
`mocassin-cli` (feature 5) must classify failures with the same table, and the types crate has no I/O
dependency. `ErrorClass` is plain data with hand-written `Display`/`Error` impls; `mocassin-types` keeps the
`serde`/`serde_bytes`-only dependency set from §1, and the `thiserror` boundary rule applies to the crates that
define real errors (`mocassin-msl`, `mocassin-rng`, `mocassin-cli`).

**Feature 2 RNG evidence.** `tests/reference/bench/step-13-rng.json` records the run measured on the tree
squashed as `54820f2` with the raw Criterion log `step-13-rng-2026-11-19.txt`, and
`tests/reference/bench/baselines.json` gains the first
`runnerClass = linux-x64-local-container-amd-ryzen-7-7840u-rust-1.99.1` entry. The 10 % rule is applied only
within that class: a release candidate fails when the median of the identical Criterion benchmark is more than
1.10 × the recorded median. Absolute timings are never compared across CPUs, operating systems, or Rust
versions; a different runner records its own entry. Step 29 owns the automated same-class regression job; the
manual benchmark gate re-verifies the declared class.

Measured medians (single-threaded, release): `next_u32` 1.364 ns (733 M draws/s, budget ≥ 250 M), `next_u64`
2.290 ns (437 M/s, ≥ 150 M), `next_f64` 2.546 ns (393 M/s, ≥ 120 M), `next_bounded(2/10/1024)` 1.747–1.759 ns
(569–572 M calls/s, ≥ 200 M), 64-word refill 75.4 ns (3.40 GB/s, ≤ 250 ns), state export+restore 123.4 ns
(≤ 500 ns), 32-byte fork 113.1 ns (≤ 1 µs). Every budget is met; measured draws/call is 1.00.

The golden vectors currently live in `crates/mocassin-rng/tests/vectors.rs`, produced by an independent
ChaCha12 implementation and cross-checked against the Step 10 seed vectors. The shared
`tests/contracts/rng-vectors.json` document stays with feature 6, whose fixture harness owns the cross-language
JSON artifacts; feature 2 freezes the same values in code so the algorithm cannot drift before then.

**Feature 3 reader evidence.** `Package::open_readonly` opens the SQLite file with
`SQLITE_OPEN_READONLY | SQLITE_OPEN_NO_MUTEX`, sets `query_only`/`foreign_keys`/a 2 s busy timeout, checks
`application_id`, `user_version`, `schema_info` (nine keys, `format_version`, duplicate rows), and the used
tables/columns. Payloads are decompressed with the byte guard, scanned by the bounded MessagePack walk, then
decoded into the `mocassin-types::payload` DTOs; the lattice guard is `a*b*c*position_count` (checked, ≤
`max_positions`, ≤ `i32::MAX`, `4×` ≤ `max_lattice_payload_bytes`). The model cache is keyed by
`(model_id, signature)` and shared through `Arc`; the feature-3 reader returned the interim `LoadedJob` (row +
job DTO + model DTO + lattice DTO) and `load_model`/`job_ids` for inspection. Feature 4 replaced `LoadedJob`
with the validated `JobPackage`.

- **Real packages:** both `msl-v1` and `amend-1-net10` open and load all six jobs; the model/lattice signatures,
  counts, settings, and lattice lengths match `package-summary-v1`; the amended package yields the same logical
  summary. A `0444` copy opens with no `-wal`/`-shm` sidecars. Repeated `load_job` calls share one model `Arc`.
- **Hostile matrix (all clean package errors, no panic/OOM):** duplicate known key, missing required key,
  non-map document, trailing bytes, truncated payload, declared array length beyond the input, 20-level nesting,
  element-budget violation, lowered depth limit, 4 MiB decompression bomb (declared size rejected before
  allocation), content-size-less frame streamed with a hard limit, truncated/empty frames, oversized supercell,
  lattice count mismatch, and a particle above `u8::MAX`.
- **Schema matrix:** wrong `application_id`, future `user_version`, future `schema_info.format_version`, missing
  key, duplicate key, renamed table, renamed column, `models`/`lattices` `format_version` column mismatch,
  corrupt model/job payloads, and empty/non-database files.
- **Measured decode (indicative, release, local container):** open 1.18 ms, cold `load_job` 346 µs, warm
  `load_job` 92 µs for the ceria fixture (864-particle lattice, model cached). The formal load benchmark and the
  `< 100 ms` fixture budget evidence are feature 7.
- **Deviations/notes:**
1. **F-1 fixture properties — now met by two real-writer fixtures.** The populated fixture carries the
   non-trivial per-node mask difference (O2 has `{O, Vc, H}`, O1/U1 have `{O, Vc}`; KMC node 0 and MMC nodes
   0/1). The new **`step-13-centered`** fixture, derived from the Step 08 C-centered sample after the compiler
   fix `1f5b473`, carries **both** properties: C-centering produces translation-equivalent starts and the
   real writer emits cross-bucket `jump_mapping` entries (for example bucket position 2 maps direction 3 with
   `position_id` 6), while the extended B set also gives a per-node mask difference. The Rust
   `centered_fixture` tests and the .NET `CenteredPackageFixtureTests` assert the cross-bucket mapping and the
   mask difference on the real package. The earlier [`step-13-amend-1`](../amendments/step-13-amend-1-fixture-properties.md)
   amendment is **Withdrawn — resolved by the Step 08 fix `1f5b473`**; it is retained for history and no
   longer requests owner acceptance. The P-1 `PKG010` claim from feature 6 remains non-reproducible; the Step 11
   verifier `byte_position >= site_count` observation is a separate deferred maintenance item (cluster slot vs
   site index), and no valid package in the repository trips it.
2. **Audit F-8 correction — the implemented inverse vacancy check.** The audit asked for
   `is_vacancy[0] == true`, but the Step 08 compiler (`ResolveCompilationStage`) writes the void particle with
   `IsVacancy: false`, and both checked-in model payloads carry `false`. The reader therefore requires the
   **inverse** check: index 0 is void, `charges[0] == 0`, and `is_vacancy[0] == false`; a `true` flag at index 0
   is rejected. The provenance strings must be non-empty. This is the implemented contract, and the re-audit
   should verify it against the writer rather than the original finding text.
3. The fuzz build used the `x86_64-unknown-linux-gnu` target because the musl target is not installed;
   `cargo-fuzz` otherwise defaults to musl. `libfuzzer-sys` is confined to the excluded fuzz workspace; no
   production dependency or root lockfile entry changes.
4. Both populated jobs share one 2x2x2 lattice, so the package has one lattice row (deduplicated), not two.


**Audit round 1 remediation (F-1–F-8, N-1–N-4).** The remediation branch
`feature/step-13-rust-solver-foundation-and-package-reader/fix-audit-round-1` records:

- **F-1:** the populated fixture carries the per-node mask difference, and the new `step-13-centered` fixture
  carries both the cross-bucket equivalent-start mapping and the mask difference, after the Step 08 fix
  `1f5b473`. The amendment is withdrawn; the Step 11 verifier observation remains a separate deferred item.
- **F-2:** `tests/contracts/packages/msl-v1/dry-run-v1.txt` is the checked-in CLI golden and the CLI test reads
  it (with `{package}`/`{version}` placeholders).
- **F-3:** `tests/projections.rs` now decodes the `.bin` vectors into the runtime types and re-encodes with the
  production `mocassin_msl::projection` encoder; the test also reads `tests/contracts/signature-vectors.json`
  and checks the id/signature/digest fields.
- **F-4:** a job row whose model or lattice row is absent is `PackageError::DanglingReference` (exit 11), with a
  mutated-fixture test for both cases.
- **F-5:** the per-field caps and raised-limit behaviour are covered by
  `every_per_field_count_cap_is_enforced`.
- **F-6:** `DirectionPool` and `JumpStatus`/`JumpLink` are type-only containers with invariant tests.
- **F-7:** `Package::load_job_with_seed` writes the override into `JobPackage.seed`; the CLI passes `--seed`
  there, and `the_seed_override_is_the_effective_job_seed` covers it.
- **F-8:** provenance strings must be non-empty; `is_vacancy[0] == true` is rejected because the Step 08 writer
  marks the void particle `false` (the audit claim is corrected in the deviations above).
- **N-1:** the evidence and page now say the real CLI AOT publish **fails** with 62 IL2xxx/IL3xxx diagnostics.
- **N-2:** the unused `serde_bytes`, `tracing` (in `mocassin-msl`), and `thiserror` (in `mocassin-cli`)
  declarations are removed; `tracing` and `thiserror` stay only where used.
- **N-3:** a same-class load rerun is recorded and the baseline is flagged boundary-calibrated for Step 29; the
  10% rule is not relaxed.
- **N-4:** the first remediation merged `main` `ec301a4`; this second remediation merges the Step 08 fix
  `1f5b473` (`main` at the new freeze) before the replacement closeout CI, which covers the merged tree.

**Feature 7 performance, documentation, and closeout evidence.** The final feature adds the load/decode
benchmark, reconciles the documentation with the implemented behaviour, records the parity rows, and prepares
the closeout/audit evidence.

- **Load benchmark:** `src/rust/crates/mocassin-msl/benches/load.rs` measures open, cold `load_job(1)`, and warm
  `load_job(1)` on the ceria fixture. Medians on the local runner class (AMD Ryzen 7 7840U, Rust 1.99.1,
  release): **open 171.1 µs, cold job 343.1 µs, warm job 71.1 µs**, all far below the `< 100 ms` fixture budget.
  A same-class rerun on the busy container measured open 271.0 µs / cold 351.4 µs / warm 68.0 µs; the open
  variance (+58%) is environment noise, and the baseline is explicitly **boundary-calibrated for Step 29** (the
  auditor saw +10.4%). The 10% rule is not relaxed. The budget is scoped to the ceria fixture, not a 512 MiB
  package. Evidence: `tests/reference/bench/step-13-load.json` (with the rerun), the raw
  `step-13-load-2026-11-20*.txt` files, and the `rustLoad` entry in `baselines.json`. The RNG numbers remain `next_u32` 733 M/s, `next_u64` 437 M/s, `next_f64` 393 M/s,
  `next_bounded` 569–572 M/s, refill 75.4 ns, state 123.4 ns, fork 113.1 ns.
- **Documentation:** `solver-architecture.md` (RNG format-breaking split from m-16, full CLI surface, load
  evidence, exact `--stdout`/`--extension` behaviour), `msl-format.md` (solver read path), `cli.md` (Step 13
  options, dry-run format, `--stdout` not accepted, exit codes), `testing-and-parity.md` (fixtures, synthetic
  mask/mapping tests, fuzz runs, load numbers), the performance page (new solver-load section), the existing Rust layout
  section, and the `zensical.toml` navigation entry. The Step 08 "The Rust ..." label and the Step 16
  "Step 14/16" label are corrected (n-17); `max_model_elements` now says "covers the writer-valid payload with
  headroom" (n-16).
- **Parity:** the Step 13-specific rows are `Done` (reader/foundation and RNG); the full Rust-solver row remains
  `Partial` because the engines land in Steps 14–19, fuzzing remains `Partial` pending Step 29's continuous run,
  and the Rust API reference remains `Partial` until Step 30. No solver row is marked Done.
- **Correction:** feature 6 had left `proptest` in `mocassin-msl`'s `[dependencies]`; it is now dev-only, as the
  step page's dev-crate rule requires. No production dependency was added.
- **Closeout:** `./tools/verify.sh all --require` exited 0 on the remediation tree (AOT green; the real
  `Mocassin.Cli` AOT publish **fails** with 62 IL2xxx/IL3xxx diagnostics, recorded as non-gating evidence). The
  re-audit closeout run
  [44815711484](https://github.com/seb-eis/mocassin-2/actions/runs/44815711484) is green at
  `020e70ad4efcb06f79607cfeb7c75e370147cdd9`, the audited commit and merge candidate. `main` `ec301a4` was
  merged as `d327e74` before the freeze; the run covers the merged tree.
  `docs/planning/closeout/step-13.json` passes
  `python tools/check_closeout_evidence.py`; `docs/planning/audits/step-13.md` is `Re-audit required` with the
  F-1 amendment and the F-8 correction flagged. No tag or merge has been performed.

### Approved feature list (owner, 2026-11-18; implementation authorized 2026-11-19)

| # | Feature | Branch | Planned scope | Outcome |
| --- | --- | --- | --- | --- |
| 1 | Workspace, types, and error model | `feature/step-13-rust-solver-foundation-and-package-reader/workspace-types-and-errors` | Crate boundaries and dependency edges (the existing CLI→solver edge stays for the future run path); `mocassin-types` payload DTOs, typed IDs, runtime model/job/lattice types, `ReaderLimits`, `PackageError`/exit-code model; `mocassin-sim` `clap` skeleton, logging, `--version`, parse-error remap; DTO key/decode tests | `cargo test` green; the crate graph matches §1; the CLI parses and rejects options with stable exit codes |
| 2 | RNG streams and state | `feature/step-13-rust-solver-foundation-and-package-reader/rng-streams-and-state` | SHA-256 seed derivation; ChaCha12 wrapper over `rand_chacha`; `next_u32`/`next_u64`/`next_f64`/`next_bounded`/`fork`; 64-byte `RngStateV1`; golden vectors; Criterion draw/refill benchmark | Seed derivation matches the Step 10 vectors; draw/state goldens pass; the budget is measured and recorded |
| 3 | `.msl` reader and bounded decode | `feature/step-13-rust-solver-foundation-and-package-reader/msl-reader-and-bounded-decode` | Read-only `rusqlite` open; schema/application-id/version/provenance checks; Zstd frame handling; MessagePack scan and typed decode; lattice bound; one-model cache; structured errors | The real `.msl` fixture opens read-only and yields raw payloads; malformed/truncated/bomb payloads fail cleanly |
| 4 | Model/job mapping and signatures | `feature/step-13-rust-solver-foundation-and-package-reader/model-job-mapping-and-signatures` | Payload → validated runtime model/job; every index/length/finite/kind invariant; mass/charge conservation, state/path/movement checks, and the documented transport-vector split; canonical projection writer; parameterization and lattice signature re-encoding; column/version/payload cross-check; fix the `TransitionPayloadDtos.cs`/`CompiledTransitionModel.cs` XML comments to the node-wise endpoint-moment formula | A ceria job becomes a fully validated `JobPackage`; both stored signatures are reproduced from the payload; a mutated index, state, movement, or value is rejected; source comments match the contract |
| 5 | Dry-run CLI and logging | `feature/step-13-rust-solver-foundation-and-package-reader/dry-run-cli-and-logging` | `--db`/`--job`/`--io`/`--seed`/`--threads`/`--jump-log-max-ev`/`--log-*`/`--dry-run`; write-free summary; unified exit mapping; tracing human/JSON; panic containment; solver exit-code drift check | `mocassin-sim --dry-run` reports the documented summary and exits 0; the summary counts equal `inspect-v1` for the fixture; no file is written; the exit-code rows match architecture §7.3 |
| 6 | Cross-language fixtures, property tests, and fuzzing | `feature/step-13-rust-solver-foundation-and-package-reader/cross-language-fixtures-and-fuzzing` | Rust tests over `tests/contracts/packages/msl-v1/`, `amend-1-net10/`, projection `.bin` vectors, raw payloads, and `rng-vectors.json`; a new real-writer populated pair/cluster + MMC fixture; proptest mutation/truncation suite; `cargo-fuzz` scan/decode/package targets and a time-boxed run | The .NET-written packages and projection vectors are consumed byte-for-byte; populated and MMC payload→signature paths pass; mutation tests and the fuzz run find no panic/OOM |
| 7 | Performance evidence, documentation, and closeout | `feature/step-13-rust-solver-foundation-and-package-reader/performance-docs-and-closeout` | Load and RNG Criterion benches plus same-runner-class baselines; `solver-architecture.md`, `msl-format.md`, `cli.md`, `testing-and-parity.md`, Rust layout docs; parity/status/closeout evidence (`docs/planning/closeout/step-13.json`) and the audit request | Load `< 100 ms` on the fixture and the RNG budgets are recorded with the environment; documentation builds strictly; `check_closeout_evidence.py` passes and the audit request is complete |

The human owner approved this seven-feature list and the 4.5 pw single-step sizing on 2026-11-18, accepted the
design as `Ready with notes`, and lifted the implementation hold on 2026-11-19. The design merged into `main`
as `056d8c1` and the step branch was created from that commit. Implementation is in progress; individual
features are squash-merged without a further approval. If features 1–3 exceed 2.0 pw, the step agent stops and
reports to the orchestrator before feature 4.

## Goal

Establish the solver foundation without Monte Carlo physics. `mocassin-sim` must open an immutable `.msl`
read-only, reject schema/payload corruption before allocating memory, validate every index and length, derive
and report the effective job seed, reproduce both stored signatures from the payload, and expose a safe,
data-oriented `JobPackage` to Steps 14–19. The RNG must be reproducible, exactly resumable, and fast enough that
it is not the KMC bottleneck.

The step deliberately ends before any physics: there is no lattice initialization, no energy evaluation, no
KMC/MMC loop, no tracker value, no `.mcs` writer, and no job-folder output.

## Scope

**In scope**

- The Rust workspace crate boundaries, dependency direction, module layout, and public API documentation.
- `mocassin-types`: serde payload DTOs matching the `.msl` v1 payload schema; typed IDs; the validated runtime
  model, job, lattice, and settings types; `ReaderLimits`; the solver error model.
- `mocassin-msl`: read-only SQLite access, schema/version/provenance checks, bounded Zstd+MessagePack decoding,
  a non-allocating MessagePack scan, index/length/finite validation, canonical projection re-encoding, and the
  parameterization/lattice signature checks.
- `mocassin-rng`: SHA-256 seed derivation, ChaCha12 streams, bounded draws, `[0,1)` doubles, domain-separated
  forks, the 64-byte `RngStateV1` blob, golden vectors, and the performance benchmark.
- `mocassin-cli`: the `mocassin-sim` parser, logging, exit-code mapping, `--version`, and the write-free
  `--dry-run` inspector.
- The data-oriented base structures that Steps 14–18 will fill: lattice, positions, environment definitions,
  flat energy tables, selection-pool containers, and the jump-status container.
- Cross-language fixtures: the .NET-written `.msl` v1 package, the pre-amendment package, canonical projection
  vectors, raw payloads, RNG vectors, and the dry-run golden.
- Documentation, parity/status updates, closeout evidence, and the audit request.

**Non-goals (explicitly out of scope)**

- No KMC or MMC engine, no attempt/acceptance logic, no time step, no normalization, no pre-run, no abort
  conditions (Steps 16–17).
- No simulation-context initialization, neighbor-link expansion, environment-state arena, selection-pool
  population, jump-status cache, energy evaluation, field terms, trackers, histograms, statistics, or `.mcs`
  output (Steps 14–18).
- No `run.mcs`, `prerun.mcs`, `solver.json`, `stdout.log`, `stderr.log`, or `mmcfe.db`; Step 13 writes no job
  folder. `--io` is accepted but not created; a non-dry-run invocation is a stable usage error until Step 16.
- No extension ABI, `--extension` loading, batch/HPC mode, `--validate-only`, `--no-state`, `--log-file`, or
  `.msr` interaction (Steps 19–20).
- No `.msl` writer or verifier; `mocassin verify` remains the .NET verifier. Rust never writes `.msl`/`.msr` and
  never reads project JSON.
- No change to `contracts/manifest.json`, the `.msl` schema, the payload field set, the package/payload versions,
  or the canonical projection layouts.
- No new external crate in the production workspace. The step uses workspace dependencies only (`serde`,
  `rmp-serde`, `zstd`, `rusqlite`, `clap`, `thiserror`, `tracing`, `tracing-subscriber`, `rand`,
  `rand_chacha`, `sha2`, and the dev-only `proptest`, `criterion`, `insta`). The fuzz crate is outside the
  workspace and uses the standard `libfuzzer-sys` harness generated by `cargo-fuzz`; it is not a product
  dependency.
- No change to ADR 0005. ChaCha12, SHA-256 derivation, and job-level parallelism stand.
- No platform-specific code. The closeout is Ubuntu; Windows/macOS are not platform gates for this step.
- No `--json` output for `mocassin-sim`; the human dry-run format is the Step 13 contract and `cli.md` freezes
  it. A machine-readable solver report is Step 19 work.

## Original reference

Read `/workspace/mocassin-original` as a read-only reference. Do not copy GPL code or data.

- `JobLoader/Sqlite3JobLoader.c` reads EF Core blobs into raw native structs. `Array_t` headers encode
  dimensions and blocks, the loader trusts sizes, the move-sequence count uses a wrong element size
  (`bytes/16` rather than the 24-byte entry), and 32-bit `1 << id` masks are undefined for particle IDs ≥ 32.
- `Simulator/Data/Jobs/JobDbModel.h` is the in-memory model: lattice, job info/header, structure metadata,
  environments, pair/cluster tables, jump collections/directions/rules, and tracker mappings.
- The CLI takes `-dbPath`, `-jobId`, `-ioPath`, `-stdout`, `-extDir`, and `-jumpLogMaxEv`, with optional plugin
  paths. Errors exit with an internal error code; an already-completed job exits 0.
- `Libraries/Framework/Math/PcgRandom.h` seeds from the wall clock, has seed/`NextBytes`/`NextDouble` defects,
  and uses modulo rejection. ADR 0005 replaces it; bit-exact trajectories are out of scope.

Preserved intents: a separate solver process, one job per invocation, a read-only package input, a job-id
selector, a human summary, and stable exit codes.

Corrected in this step: typed MessagePack schema instead of native structs; bounded decoding before allocation;
64-bit masks; SHA-256-derived seeds and ChaCha12; no time-based seeds; an explicit versioned RNG state; and a
reader that never mutates the package.

## Design

### 1. Workspace and crate boundaries

The workspace already exists at `src/rust/Cargo.toml` with seven placeholder crates. Step 13 fills the
foundation crates and leaves the physics crates as placeholders.

| Crate | Step 13 content | Depends on | Later owner |
| --- | --- | --- | --- |
| `mocassin-types` | Payload DTOs, typed IDs, validated runtime model, `JobPackage`, `InitialLattice`, `JobSettings`, `SolverJobFlags`, `ReaderLimits`; no I/O | `serde`, `serde_bytes` (existing workspace crate) | Steps 14–18 add runtime fields |
| `mocassin-msl` | Read-only SQLite reader, schema checks, bounded Zstd/MessagePack decode, scan, validation, canonical projection and signature re-encoding | `mocassin-types`, `rusqlite`, `rmp-serde`, `zstd`, `sha2`, `thiserror`, `tracing` | Step 19 adds batch selection; Step 20 does not use it |
| `mocassin-rng` | SHA-256 derivation, ChaCha12 wrapper, draw methods, `RngStateV1`, golden vectors, Criterion bench | `rand`, `rand_chacha`, `sha2`, `thiserror` | Steps 16/18 consume it |
| `mocassin-solver` | Placeholder API; Step 13 re-exports the foundation types only | `mocassin-types`, `mocassin-msl`, `mocassin-rng`, `mocassin-state`, `mocassin-extension` | Steps 14–19 |
| `mocassin-state` | Placeholder; no `.mcs` schema in Step 13 | `mocassin-types`, `serde`, `rmp-serde` | Step 18 |
| `mocassin-extension` | Placeholder; no ABI in Step 13 | `thiserror` | Step 19 |
| `mocassin-cli` | `mocassin-sim` parser, logging, version, dry-run, exit mapping; the future run path | `mocassin-msl`, `mocassin-rng`, `mocassin-types`, `mocassin-solver` (existing edge, placeholder), `clap`, `thiserror`, `tracing`, `tracing-subscriber` | Step 19 completes the command |

Dependency direction:

```text
mocassin-cli
   ├──► mocassin-msl ──► mocassin-types
   ├──► mocassin-rng
   ├──► mocassin-types
   └──► mocassin-solver (existing placeholder edge; used by the future run path)
mocassin-solver (placeholder) ──► types, msl, rng, state, extension
```

Rules:

- `mocassin-types` performs no I/O and has no SQLite, Zstd, tracing, or CLI dependency. It is the shared
  vocabulary for the reader, the CLI, and the later engine.
- `mocassin-msl` is the only crate that opens SQLite, decodes Zstd/MessagePack, and re-encodes canonical
  projections. It never writes a database.
- `mocassin-rng` is the only crate that names `rand_chacha` state methods. No other crate depends on the RNG
  crate's internal representation; the `RngStateV1` bytes are a first-party contract.
- The CLI keeps its existing `mocassin-solver` edge because the Step 16/19 run path will use the solver library.
  Step 13's inspection path calls `mocassin-msl` and `mocassin-rng` directly and does not call the placeholder
  solver; there is still one package-reader implementation.
- `Package` holds a `rusqlite::Connection` and is not `Sync`. Keep one package per thread/process; a future
  batch worker opens its own `Package`; Step 13 is single-process/single-job.
- No new external crate. `criterion`, `proptest`, and `insta` stay dev-dependencies; the fuzz crate is outside
  the workspace and uses `libfuzzer-sys` from the standard `cargo-fuzz` template.

Rust layout follows [`docs/development/code-structure.md`](../../development/code-structure.md) §2.2: crate
roots contain only `lib.rs`/`main.rs`, `Cargo.toml`, and README; concept modules are snake_case; no
`utils`/`helpers`/`common`/`models` modules; files stay below 800 lines.

### 2. Typed package, model, and job contract

#### 2.1 Payload DTOs

The `.msl` payload field names and types are owned by `Mocassin.Contracts.Packages` and
`contracts/generated/schema/package-payloads-v1.schema.json`. Rust must not invent a second field set.

`mocassin-types::payload` mirrors the payload maps exactly:

| Payload | Keys |
| --- | --- |
| Model | `format_version`, `signature`, `model_signature`, `symmetry_contract_version`, `space_group_data_version`, `space_group_data_sha256`, `particle`, `structure`, `energy`, `transitions`, `simulations` |
| Particle | `charges`, `symbols`, `is_vacancy`, `count` |
| Structure | `cell_vectors_ang`, `positions`, `position_count`, `site_count`, `environments`, `interaction_range`, `static_trackers_per_cell`, `global_tracker_count` |
| Position | `vector4`, `site_index`, `stable`, `particle_ids` |
| Environment | `selection_particle_mask`, `pairs`, `clusters`, `update_particle_ids` |
| Pair link | `target_position`, `offset`, `energy_id` |
| Cluster link | `cluster_id`, `byte_position` |
| Energy | `pair_tables`, `cluster_tables`, `defect_background` |
| Pair table | `object_id`, `energies` |
| Cluster table | `object_id`, `occupation_codes`, `energies`, `particle_table_mapping` |
| Transitions | `jump_collections`, `jump_directions`, `jump_rules`, `jump_count`, `jump_mapping`, `static_tracker_mapping`, `global_tracker_mapping` |
| Collection | `kind`, `transition_id`, `mobile_particle_mask`, `rule_ids`, `direction_ids` |
| Direction | `object_id`, `position_id`, `collection_id`, `path`, `movement`, `path_vector`, `length`, `is_inverse` |
| Rule | `rule_id`, `collection_id`, `state0`, `state1`, `state2`, `frequency_hz`, `charge_transport_vector`, `static_correction`, `tracker_order` |
| Simulation | `id`, `kind`, `jump_collection_ids`, `default_settings`, `tracker_models` |
| Settings | `temperature_k`, `target_mcsp`, `write_out_count`, `pre_run_mcsp`, `time_limit_s`, `min_success_rate`, `electric_field_vm`, `electric_field_direction`, `normalization_energy_ev`, `max_attempt_frequency_hz`, `break_tolerance`, `break_sample_length_mcsp`, `break_sample_interval_mcsp`, `result_sample_mcsp`, `flags` |
| Tracker models | `global_tracker_indices`, `static_tracker_indices` |
| Job | `format_version`, `simulation_index`, `kind`, `temperature_k`, `target_mcsp`, `write_out_count`, `pre_run_mcsp`, `time_limit_s`, `min_success_rate`, `electric_field_vm`, `electric_field_direction`, `normalization_energy_ev`, `max_attempt_frequency_hz`, `break_tolerance`, `break_sample_length_mcsp`, `break_sample_interval_mcsp`, `result_sample_mcsp`, `flags`, `selection_optimizers`, `extensions`, `instruction`, `doping_info` |
| Optimizer | `position_id`, `particle_id` |
| Extension | `id`, `config_json` |
| Lattice | `format_version`, `lattice_size`, `position_count`, `lattice` |

Serde rules:

- Use `#[serde(rename_all = "snake_case")]` and the field names above. Do **not** use
  `deny_unknown_fields`; readers ignore unknown keys.
- A missing required field is a decode error. Nullable payload fields are `Option<T>`.
- Vectors are fixed arrays where the schema fixes the length: `vector4`/`interaction_range` are `[i32;4]`,
  `cell_vectors_ang` is `[[f64;3];3]`, `offset` is `[i32;3]`, `movement` entries are `[f64;3]`,
  `path` entries are `[i32;4]`, and field/charge vectors are `[f64;3]`.
- `state0`/`state1`/`state2` are `u64`; particle masks are `u64`.
- `lattice` is accepted as either a MessagePack array of `u8` or a `bin`. The v1 .NET writer emits an array
  (`ImmutableArray<byte>`); the canonical projection uses `bin`. The reader normalizes both to `Vec<u8>`.
- Stored payload integers use the writer's **smallest-width** MessagePack encoding (for example `cc 96` for
  `target_mcsp = 150`). Fixed-width `uint64`/`int64` applies only to the canonical projection. The Rust decoder
  accepts every integer width for the target type and never re-encodes stored payloads.
- The DTO test decodes every checked-in raw payload and every payload in the `.msl` fixture, so a field-name
  or type drift fails in CI.

#### 2.2 Runtime model

The reader maps the payload DTOs to validated runtime types. The runtime types are the Step 14 input; they are
not a second persisted schema.

```rust
pub struct ParticleId(u8);
pub struct PositionId(u32);
pub struct CellIndex(u32);
pub struct JumpCollectionId(u32);
pub struct JumpDirectionId(u32);
pub struct RuleId(u32);
pub struct StateCode(u64);

pub struct ParameterizedModel {
    pub format_version: u32,
    pub signature: String,                 // Step 09 parameterization signature
    pub model_signature: String,           // Step 08 compiled-model signature (identity string)
    pub symmetry_contract_version: String,
    pub space_group_data_version: String,
    pub space_group_data_sha256: String,
    pub particle: ParticleTable,
    pub structure: StructureModel,
    pub energy: EnergyModel,
    pub transitions: TransitionModel,
    pub simulations: Vec<SimulationModel>,
    pub counts: ModelCounts,
}

pub struct ParticleTable {
    pub charges: Vec<f64>,
    pub symbols: Vec<String>,
    pub is_vacancy: Vec<bool>,
}

pub struct StructureModel {
    pub cell_vectors_ang: [[f64; 3]; 3],
    pub positions: Vec<Position>,
    pub site_count: u32,
    pub environments: Vec<EnvironmentModel>, // indexed by PositionId
    pub interaction_range: [i32; 4],
    pub static_trackers_per_cell: u32,
    pub global_tracker_count: u32,
}

pub struct Position {
    pub vector4: [i32; 4],       // [cellA, cellB, cellC, P]; base positions are [0,0,0,P]
    pub site_index: u32,
    pub stable: bool,
    pub allowed_mask: u64,       // reconstructed from the payload particle_ids
}

pub struct EnvironmentModel {
    pub selection_particle_mask: u64,
    pub pairs: Vec<PairLink>,
    pub clusters: Vec<ClusterLink>,
    pub update_particle_ids: Vec<u8>,
}

pub struct PairLink {
    pub target_position: PositionId,
    pub offset: [i32; 3],
    pub pair_id: u32,            // payload key `energy_id`, the shared pair table index
}

pub struct ClusterLink {
    pub cluster_id: u32,
    pub byte_position: u8,
}

pub struct EnergyModel {
    pub pair_tables: Vec<PairTable>,
    pub cluster_tables: Vec<ClusterTable>,
    pub defect_background: Vec<f64>, // [position][particle], row-major
}

pub struct TransitionModel {
    pub collections: Vec<JumpCollection>,
    pub directions: Vec<JumpDirection>,
    pub rules: Vec<JumpRule>,
    pub jump_count: Vec<u32>,          // [position][particle]
    pub jump_offsets: Vec<u32>,        // prefix sums, length jump_count.len() + 1
    pub jump_mapping: Vec<u32>,        // [position][particle][relative]
    pub static_tracker_mapping: Vec<i32>,
    pub global_tracker_mapping: Vec<i32>,
}

pub struct ModelCounts {
    pub particle_count: u32,
    pub position_count: u32,
    pub site_count: u32,
    pub environment_count: u32,
    pub pair_table_count: u32,
    pub cluster_table_count: u32,
    pub collection_count: u32,
    pub direction_count: u32,
    pub rule_count: u32,
    pub simulation_count: u32,
    pub global_tracker_count: u32,
    pub static_trackers_per_cell: u32,
}

pub enum SimulationKind { Kmc, Mmc }

pub struct PairTable {
    pub object_id: String,
    pub energies: Vec<f64>,            // particle_count², [center][partner]
}

pub struct ClusterTable {
    pub object_id: String,
    pub occupation_codes: Vec<u64>,    // strictly ascending
    pub energies: Vec<f64>,            // aligned with occupation_codes
    pub particle_table_mapping: Vec<u8>,
}

pub struct JumpCollection {
    pub kind: SimulationKind,
    pub transition_id: String,
    pub mobile_particle_mask: u64,
    pub rule_ids: Vec<RuleId>,
    pub direction_ids: Vec<JumpDirectionId>,
}

pub struct JumpDirection {
    pub object_id: String,
    pub position_id: PositionId,
    pub collection_id: JumpCollectionId,
    pub path: Vec<[i32; 4]>,           // [cellA, cellB, cellC, P]; node 0 is the start
    pub movement: Vec<[f64; 3]>,       // fractional movement per node
    pub path_vector: [f64; 3],         // Cartesian net displacement, Å
    pub length: f64,                   // |path_vector|, Å
    pub is_inverse: bool,
}

pub struct JumpRule {
    pub rule_id: RuleId,
    pub collection_id: JumpCollectionId,
    pub state0: StateCode,
    pub state1: StateCode,             // zero and ignored for MMC
    pub state2: StateCode,
    pub frequency_hz: f64,             // absolute applied value
    pub charge_transport_vector: [f64; 3], // e·Å
    pub static_correction: Option<[f64; 3]>,
    pub tracker_order: Vec<u8>,        // global tracker index or 0xFF per path node
}

pub struct SimulationModel {
    pub id: String,
    pub kind: SimulationKind,
    pub jump_collection_ids: Vec<u32>,
    pub default_settings: SimulationSettings,
    pub tracker_models: TrackerModels,
}

pub struct SimulationSettings {
    pub temperature_k: f64,
    pub target_mcsp: u64,
    pub write_out_count: u32,
    pub pre_run_mcsp: u64,
    pub time_limit_s: Option<u64>,
    pub min_success_rate: Option<f64>,
    pub electric_field_vm: f64,
    pub electric_field_direction: [f64; 3],
    pub normalization_energy_ev: Option<f64>,
    pub max_attempt_frequency_hz: Option<f64>,
    pub break_tolerance: Option<f64>,
    pub break_sample_length_mcsp: Option<u64>,
    pub break_sample_interval_mcsp: Option<u64>,
    pub result_sample_mcsp: Option<u64>,
    pub flags: u32,                    // authored bits only
}

pub struct TrackerModels {
    pub global_tracker_indices: Vec<u32>,
    pub static_tracker_indices: Vec<u32>,
}

pub struct SolverJobFlags(u32);        // composed job word; see data contracts §3.4
```

- `SimulationSettings.flags` keeps only the authored bits; `JobSettings.flags` is the composed word.
- A `SimulationKind` string is `"kmc"`/`"mmc"`; serde maps it with `#[serde(rename_all = "lowercase")]`.

- `StateCode` is always `u64`; the six-bit node accessor is `node(index) -> u8` for `index < 10`. Never a
  signed `i64`.
- `PairLink.pair_id` is the runtime name for the payload's `energy_id`; the writer and data contract keep
  `energy_id`.
- `EnvironmentModel` remains one entry per position, as Step 14's link builder expects. Step 13 does not expand
  `cell + offset`; that is Step 14. Step 14 owns the supercell CSR and sorting.
- `PairTable`/`ClusterTable` keep the payload order and lengths; Step 15 converts eV to kT.
- `jump_offsets` is derived from `jump_count` during mapping, so no consumer recomputes prefix sums.
- `ModelCounts` is derived once and is the dry-run summary source.

#### 2.3 Job package, settings, and lattice

```rust
pub struct JobPackage {
    pub job_id: u32,
    pub kind: SimulationKind,
    pub model: Arc<ParameterizedModel>,
    pub lattice: InitialLattice,
    pub settings: JobSettings,
    pub seed: [u8; 32],                    // effective seed after an optional --seed override
    pub optimizers: Vec<SelectionOptimizer>,
    pub extensions: Vec<ExtensionConfig>,
    pub instruction: Option<String>,
    pub doping_info: String,
}

pub struct JobSettings {
    pub simulation_index: u32,
    pub temperature_k: f64,
    pub target_mcsp: u64,
    pub write_out_count: u32,
    pub pre_run_mcsp: u64,
    pub time_limit_s: Option<u64>,
    pub min_success_rate: Option<f64>,
    pub electric_field_vm: f64,
    pub electric_field_direction: [f64; 3],
    pub normalization_energy_ev: Option<f64>,
    pub max_attempt_frequency_hz: Option<f64>,
    pub break_tolerance: Option<f64>,
    pub break_sample_length_mcsp: Option<u64>,
    pub break_sample_interval_mcsp: Option<u64>,
    pub result_sample_mcsp: Option<u64>,
    pub flags: SolverJobFlags,
}

pub struct InitialLattice {
    pub signature: String,                 // lattices.signature
    pub lattice_size: [u32; 3],
    pub position_count: u32,
    pub particles: Vec<ParticleId>,        // len = a*b*c*position_count
}

pub struct SelectionOptimizer {
    pub position_id: PositionId,
    pub particle_id: ParticleId,
}

pub struct ExtensionConfig {
    pub id: String,
    pub config_json: String,
}
```

- The package's `jobs.seed` is the effective seed unless `--seed` overrides it for debugging. The override is
  process-local; the package is never modified. `seed_hash = SHA-256(seed)` is reported in the dry-run summary
  and, from Step 18, in `.mcs` metadata.
- `SolverJobFlags` is the composed job word from data contracts §3.4: bit 0 `noSaving`, bit 1 `noJumpLogging`,
  bit 2 `useFastExp`, bit 3 `deterministic`, bit 4 `useDualDofCorrection`, bit 5 `usePrerun`, bit 6
  `kmcSimulation`, bit 7 `mmcSimulation`. Reserved bits must be zero in v1. Exactly one kind bit must be set and
  it must match `kind`; `usePrerun` and `useDualDofCorrection` are KMC-only.
- `InitialLattice` uses the documented index
  `((a * B + b) * C + c) * position_count + P`, validated in `u64` and bounded by the signed 32-bit linear-index
  limit. `particles.len()` must equal `a*b*c*position_count`.
- Step 14 consumes `JobPackage` unchanged. It does not re-read the package.

### 3. `.msl` reader

#### 3.1 Open and schema checks

`Package::open_readonly(path, limits)`:

1. Reject a missing path, directory, or unreadable file as `PackageError::Io` (exit 16) before SQLite opens it.
2. Open with `SQLITE_OPEN_READ_ONLY | SQLITE_OPEN_NO_MUTEX`, no URI handling, cache private, pooling off.
3. Set `PRAGMA query_only = ON`, `PRAGMA foreign_keys = ON`, and a bounded `busy_timeout` (2 s).
4. Read `PRAGMA application_id` (must be `0x4D4F4353`), `PRAGMA user_version` (must be 1), and
   `schema_info.format_version` (must be in the supported range `1..=1`).
5. Require `schema_info`, `models`, `lattices`, and `jobs`, and the columns the reader uses. A missing element is
   `MissingSchemaElement`.
6. Require the `schema_info` keys `format_version`, `app_version`, `created_utc`, `project_id`, `generator`,
   `job_selection`, `symmetry_contract_version`, `space_group_data_version`, and `space_group_data_sha256`.
7. Treat `app_version`, `created_utc`, and `generator` as build metadata: read them, but never include them in a
   deterministic summary or golden.
8. Do **not** run `PRAGMA integrity_check` or mirror the ten `PKG` checks. Those are `mocassin verify`'s job.
   The solver verifies only what it reads: identity, schema, references, payload decode, invariants, signatures,
   and column/payload consistency. This keeps the normal fixture path within the 100 ms budget; a large payload
   is bounded by the guards but may take proportionally longer.
9. Never expose a writable connection and never write a database. A read-only package on `0444` media must open
   normally, with no `-wal`/`-shm` sidecar.

The supported package/payload range is a constant. v1 is the first format; current + previous-major support
starts at the first release (ADR 0013). A future major is rejected with an actionable
`SchemaVersion { found, supported }` error.

#### 3.2 Read pipeline

```text
jobs row (columns + model_id/lattice_id)
  → models row (signature, format_version, Zstd blob)
  → frame content size → bounded decompress → MessagePack scan → typed decode
  → payload DTO → runtime mapping → index/length/finite validation
  → parameterization signature re-encode (and lattice signature for the lattice row)
  → column/payload cross-check → JobPackage
```

- `load_job(job_id)` reads one joined row, loads/caches the model by `(model_id, signature)`, loads the lattice,
  and builds `JobPackage`.
- `load_model(model_id)` is public for inspection and tests; a package-level `Arc` cache keyed by
  `(model_id, signature)` avoids decoding the same model twice. The cache uses interior mutability
  (`RefCell`) because `load_job` takes `&self`; `Package` is single-threaded. Production runs one job per
  process, so the cache is a test/short-batch convenience, not a shared global.
- `job_ids()` returns the ascending ids for error messages and tests. Step 19 may add a range selector.
- A missing job id is `JobNotFound` and maps to exit 10 (invalid arguments), not a package error.
- Column/payload cross-check:
  - `kind`, `temperature_k`, `target_mcsp`, `write_out_count`, `pre_run_mcsp`, `time_limit_s`,
    `min_success_rate`, `electric_field_vm`, `flags`, and `doping_info` must equal the **job payload**.
  - `lattice_a`/`lattice_b`/`lattice_c` must equal the **lattice payload's** `lattice_size`, not the job payload.
  - `models.format_version` must equal the model payload's `format_version`; `lattices.format_version` must
    equal the lattice payload's `format_version`; the job payload's `format_version` is checked on decode.
  - `models.signature` and `lattices.signature` must equal the recomputed projections.
  - Authored metadata (`collection_name`, `config_name`, the three indices, `seed`, `created_utc`) is checked
    for presence/format only.
- A job's resolved settings are **not** required to equal the referenced `simulations[].default_settings`. The
  simulation defaults are build-time defaults; the job payload is authoritative. The ceria fixture is the
  counter-example: job 1 has `target_mcsp = 150` while simulation 0's default is 200.
- `jobs.seed` must be exactly 32 bytes. `lattices.signature` is compared with the recomputed lattice
  projection; `models.signature` with the recomputed parameterization projection.

#### 3.3 Read limits

`ReaderLimits` is a public, overridable struct. Defaults:

| Limit | Default | Reason |
| --- | ---: | --- |
| `max_model_payload_bytes` | 512 MiB | Matches the .NET verifier's model guard |
| `max_job_payload_bytes` | 8 MiB | Matches the .NET verifier's job guard |
| `max_lattice_payload_bytes` | 512 MiB | Matches the .NET verifier's lattice cap |
| `max_model_elements` | 512,000,000 | Non-allocating scan budget; it covers the writer-valid payload with headroom. Per-field caps bound typed decode |
| `max_job_elements` | 1,000,000 | Small fixed schema |
| `max_messagepack_depth` | 8 | The model nests map → structure → positions → position → `particle_ids` → scalar (five container levels); 8 leaves headroom |
| `max_positions` | 500,000 | Matches `SupercellGenerator.MaximumPositions`; total linear index still ≤ `i32::MAX` |
| `max_position_count` (P) | 10,000 | Matches `UnitCellVectorEncoder.MaxP1Positions` |
| `max_particles` | 64 | 6-bit state code and the 63-custom-particle limit |
| `max_rules` | 1,000,000 | Matches the **default** `CompilationOptions.MaxRuleCount`; that option is `init`, so raise `ReaderLimits` if raised |
| `max_directions` | 1,000,000 | Same order as the rule cap; Step 13 allocation cap |
| `max_collections` | 4,096 | Step 13 allocation cap; no .NET equivalent |
| `max_pair_tables` | 1,000,000 | Step 13 allocation cap |
| `max_cluster_tables` | 1,000,000 | Step 13 allocation cap |
| `max_pair_links` | 16,000,000 | Total environment links |
| `max_pair_links_per_position` | 65,536 | One position's link vector |
| `max_cluster_links_per_position` | 4,096 | A position's cluster-link vector |
| `max_path_nodes` | 10 | Six bits per node in a `u64` |
| `max_extensions` | 256 | Job payload |
| `max_simulations` | 4,096 | Model payload |

- The position and P1 defaults are hard .NET build limits; `max_rules` matches the **default**
  `CompilationOptions.MaxRuleCount` (1,000,000), which is an `init` option a caller can raise. A package built
  with a raised rule limit, or with a future writer that raises a position/P1 limit, needs a raised
  `ReaderLimits` in the same release.
- **Envelope, not a universal guarantee (m-15).** Within the documented `ReaderLimits` envelope
  (positions/P1/particles/rules/directions/collections/tables/links/paths/extensions/simulations and the 512M
  element budget), every payload within the 512 MiB guard is readable. The direction/collection/table/link/
  extension/simulation caps are solver caps with no .NET writer equivalent; a writer-valid package above one of
  them is rejected with a clear error and requires a raised `ReaderLimits`. Do not claim that every package the
  writer can produce is readable.
- The lattice guard is `4 * a * b * c * position_count` in checked `u64`, capped by
  `max_lattice_payload_bytes`. The factor 4 covers the MessagePack array-of-`u8` encoding. The expected particle
  array is exactly `a*b*c*position_count` bytes.
- `a*b*c*position_count` must be ≤ `max_positions` and ≤ `i32::MAX` (2,147,483,647). Reject before allocating.
- Every derived length uses checked arithmetic; overflow is `InvalidValue`, not a panic.
- The model payload is decoded once and dropped after mapping; peak memory is bounded by the payload guard plus
  the runtime model (about 2× the logical model).
- `ReaderLimits` is a library parameter; Step 19 may expose it on the CLI. Step 13 does not add a CLI option.

#### 3.4 Bounded MessagePack scan

The scan is the "validate before allocation" gate. It walks the MessagePack document once without allocating
per element:

- Enforce `max_messagepack_depth`, the section's element budget, and that every declared array/map/string/bin
  length fits in the remaining input.
- Read known map keys into a small stack buffer; reject duplicate known keys in one map. Unknown keys are
  skipped with the same depth/element rules.
- Record `ScanFacts`: `format_version`, particle count, position count, site count, pair/cluster table counts,
  collection/direction/rule counts, path lengths, tracker counts, simulation count, and every top-level array
  length.
- Cross-check facts that must agree: particle arrays vs `count`; positions vs `position_count`; environments vs
  `position_count`; pair-table `energies` vs `particle_count²`; `defect_background` vs
  `position_count * particle_count`; `jump_count`/`static_tracker_mapping` vs `position_count * particle_count`;
  `global_tracker_mapping` vs `collection_count * particle_count`; each direction's `path` vs `movement`;
  each rule's `tracker_order` vs its collection's path length; each simulation's `kind` vs its collections.
- Reject trailing bytes after the top-level map.
- Reject a top-level value that is not a map or a required key that is absent.

Only after the scan passes does `rmp_serde::from_slice` decode into the DTOs. Serde's cautious collection
growth and the scan's element budget together bound the decode. A second typed pass then validates indices and
semantics (§3.5).

The scan is the primary fuzz target. It must never panic, allocate unboundedly, or accept a document whose
declared lengths exceed the input.

#### 3.5 Model validation

The typed pass checks, per section:

**Particle and structure**

- `format_version == 1`; `signature` and `model_signature` are 64 lowercase hex;
  `symmetry_contract_version`, `space_group_data_version`, and `space_group_data_sha256` are non-empty and
  match the package `schema_info` rows.
- `1 <= particle_count <= 64`; index 0 is void; `charges`, `symbols`, and `is_vacancy` lengths equal
  `particle_count`; charges are finite; symbols are UTF-8 and non-empty.
- `position_count == positions.len() == environments.len()`; `position_count <= max_position_count`.
- Position `i` has `vector4 == [0, 0, 0, i]` (base P1 positions), `site_index < site_count`, and
  `particle_ids` strictly ascending with every value `< particle_count`; reconstruct `allowed_mask` and reject
  duplicates or bits above 63.
- `site_count <= position_count`; `interaction_range[0..3] >= 0`.
- `static_trackers_per_cell == position_count * (particle_count - 1)` in checked `u64`;
  `global_tracker_count <= max_positions` (a cheap upper bound; exact tracker uses are checked below).
- `selection_particle_mask` has no bits at or above `particle_count`.
- Each pair link has `target_position < position_count`, `|offset[i]| <= interaction_range[i] + 1`, and
  `pair_id < pair_table_count`.
- Each cluster link has `cluster_id < cluster_table_count` and `byte_position < 10`.
- `update_particle_ids` is strictly ascending and `< particle_count`.
- Total links respect `max_pair_links` and per-position caps.

**Energy**

- Pair table count and cluster table count respect their caps.
- Each pair `energies.len() == particle_count²`; all values finite; `object_id` is 64 lowercase hex.
- `occupation_codes` is strictly ascending; `energies.len() == occupation_codes.len()`; each 6-bit slot is
  `< particle_count`; unused high bits are zero; `particle_table_mapping` is strictly ascending, every value
  is `< particle_count`, and its length is the table's row count; `object_id` is 64 lowercase hex.
- `defect_background.len() == position_count * particle_count`; all values finite.

**Transitions**

- Collection, direction, and rule counts respect their caps. Collection `rule_ids`/`direction_ids` are unique
  and in range; each direction's `collection_id` is in range, its `position_id < position_count`, and its
  `object_id` is 64 lowercase hex.
- Each direction has `path.len() == movement.len()`, `2 <= path.len() <= max_path_nodes`, node 0 at the
  representative position, every node's `P < position_count`, finite movements, finite `path_vector`, and
  `length >= 0`; `length` matches `|path_vector|` within `1e-9 * max(1, |path_vector|)`.
- Every direction in one collection has the same `path.len()`. This is the rule's path-node count.
- Rule `rule_id == index`; `collection_id` in range; `state0/1/2` node slots are `< particle_count` and the
  bits above `6 * path_len` are zero. For MMC, `state1 == 0` and ignored.
- `frequency_hz` is finite and `>= 0`; a zero frequency is a disabled rule, not a decode error.
- `charge_transport_vector` is three finite values; `static_correction` is `None` or three finite values.
- `tracker_order.len()` equals the collection's common path length; every non-sentinel value is
  `< global_tracker_count`; the sentinel is `0xFF`.
- `jump_count.len() == position_count * particle_count`; `sum(jump_count) == jump_mapping.len()`; each mapped
  direction exists and its `collection_id` is in range; duplicates and out-of-range values are rejected. Do
  **not** require the mapped direction's `position_id` to equal the bucket position: the writer maps a direction
  into every symmetry-equivalent start bucket, and `EquivalentStartPositions` is not persisted. Exact
  equivalent-start equivalence is a Step 08 writer invariant; Step 14 consumes the mapping as authored and
  records the residual risk.
- `static_tracker_mapping.len() == position_count * particle_count`; each value is `-1` or
  `< static_trackers_per_cell`. `global_tracker_mapping.len() == collection_count * particle_count`; each value
  is `-1` or `< global_tracker_count`.

**Scientific invariants (M-2)**

The `.msl` is untrusted and the parameterization signature covers only energies/frequencies, so the reader
checks the rule-level invariants that do not require project-side geometry:

- **Allowed occupations (narrowed, M-3).** Decode `state0`, `state1`, and `state2` into `path_len` six-bit
  nodes. **Exempt void (particle 0) from the mask check**: the writer never includes void in `particle_ids`
  (`StructureCompilationStage.BuildMask` rejects compiled index 0), and void is the empty occupation. For every
  non-void node particle, require it to be in the **union of the allowed masks of the rule's collection at the
  same node index**. A rule has no stored representative direction (`JumpRulePayload` has no direction index;
  Step 11 R2-3 records the missing marker), so the union is the only payload-supported path source. A collection
  with no directions is invalid. The **exact per-direction** occupation check is assigned to **Step 16**, which
  owns the selected direction/rule lookup; it validates the state particle against the resolved path position's
  mask. The residual risk is recorded in §11 and the Step 16 interface.
- **Mass conservation.** For each rule, the multiset of non-void particle **symbols** in `state0` must equal the
  multiset in `state2`. Symbols come from the particle table; this is the same rule as
  `TransitionRuleHelpers.MassConserved`.
- **Charge conservation.** `|Σ q(state0) - Σ q(state2)| <= 1e-9`, using the particle charges and omitting void;
  this is `TransitionRuleHelpers.ChargeConserved`.
- **Movement and path vector.** `path_vector` must equal `Σ_i (movement_i.a * a + movement_i.b * b +
  movement_i.c * c)` using `cell_vectors_ang`, and `length` must equal `|path_vector|`, both within
  `1e-9 * max(1, |path_vector|)`. `path_vector` must also match the sum of the per-node movements the writer
  emitted; a changed movement or path vector is rejected.
- **Charge-transport vector.** The vector must be finite and, when no node changes charge (`q(state0[node]) ==
  q(state2[node])` for every node), its norm must be `<= 1e-9`. The full positional formula
  `Σ_node (q(state2[node]) - q(state0[node])) * x(path node)` is **assigned to Step 15**: the v1 payload carries
  no fractional coordinates for P1 positions (only `vector4 = [0,0,0,P]` and the fractional movements), so it
  cannot be recomputed from the package alone. Step 15 owns the field term and must bound/validate the vector's
  use; the residual risk is recorded in §11 and the Step 15 interface. A future `.msl` format can add the
  fractional positions or sign the transport vector; that is a format change, not a Step 13 change.
- **Direction/rule ordering.** A direction's `path[0]` must be `[0, 0, 0, position_id]`; a rule's
  `collection_id` must own a direction with the same `path_len`; a rule's `tracker_order` length must match that
  path length. This catches a mismatched rule/direction pair even when the indices are in range.

If a check needs a tolerance, use the documented absolute/relative tolerance and a fixture with a deliberately
changed value; do not silently accept a mismatch.

**Simulations and job**

- `simulation_index < simulations.len()`; the job kind, row kind, and simulation kind agree; kind strings are
  exactly `"kmc"` or `"mmc"`. The job's resolved settings are validated in their own right and are **not**
  required to equal `simulations[simulation_index].default_settings` (the fixture's job target is 150, the
  simulation default is 200).
- `jump_collection_ids` are unique and in range; the referenced collections have the simulation's kind.
- `tracker_models.global_tracker_indices` are sorted, unique, and `< global_tracker_count`;
  `static_tracker_indices` are sorted, unique, and `< static_trackers_per_cell`; both are empty for MMC.
  `default_settings.flags` contains only the authored bits 0–3.
- Default settings and job settings are finite where required; `temperature_k > 0`; `target_mcsp > 0`;
  `time_limit_s > 0` when present; `min_success_rate >= 0` when present;
  `max_attempt_frequency_hz > 0` when present; `electric_field_vm >= 0`; the field direction is normalized when
  the modulus is non-zero (tolerance `1e-9`).
- `pre_run_mcsp == 0` for MMC; break fields are MMC-only (`null`/0 for KMC).
- `flags` has no reserved bits, exactly one kind bit, and matches `kind`; KMC-only bits are clear for MMC.
- `selection_optimizers` are sorted by `(position_id, particle_id)` and in range; `extensions` are sorted by
  `id`, unique, and `config_json` is a UTF-8 string (Step 13 does not parse the config); `instruction` and
  `doping_info` are UTF-8.

**Lattice**

- `format_version == 1`; `lattice_size == [lattice_a, lattice_b, lattice_c]`;
  `position_count == model.position_count`; `particles.len() == a*b*c*position_count`; every particle index is
  `< particle_count`.
- The recomputed five-field lattice projection equals `lattices.signature`.

Any failure is a structured error with the model/job id and the section, not a panic.

#### 3.6 Signature re-encoding

Rust implements the ADR 0010 canonical projection exactly. The .NET `CanonicalProjectionWriter` uses forced
headers, not the compact MessagePack forms:

| Value | Bytes |
| --- | --- |
| array header | `0xdd` + `u32` big-endian length (always array32, even for empty arrays) |
| string | `0xdb` + `u32` big-endian UTF-8 byte length + bytes |
| binary | `0xc6` + `u32` big-endian length + bytes |
| uint64 | `0xcf` + 8 bytes big-endian |
| int64 | `0xd3` + 8 bytes big-endian |
| float64 | `0xcb` + 8 IEEE-754 bytes big-endian |
| nil / bool | `0xc0` / `0xc3` or `0xc2` |

**Quantization.** Every projected float is `q(x)`: scale by `1e12`; for `x >= 0` use
`floor(scaled + 0.5)`, otherwise `ceil(scaled - 0.5)`; divide by `1e12`; normalize `-0.0` to `+0.0`. Reject
NaN/Inf and a scaled overflow. Use the same expression as C#, not `f64::round()`, so the two implementations
agree on ties and large values.

**Parameterization signature** (`mocassin/parameterization-signature/v1`, `0x00`, then the projection):

```text
array32(6)
  uint64(1)
  str32(model_signature)
  pairTables:   array32(pair_table_count) of array32(particle_count²) float64
  groupSets:    array32(cluster_table_count) of array32(table length) float64
  siteEnergies: array32(position_count * particle_count) float64
  transitionRules: array32(rule_count) float64
```

- Values are the payload's applied tables in payload order: `energy.pair_tables[].energies`,
  `energy.cluster_tables[].energies`, `energy.defect_background`, and
  `transitions.jump_rules[].frequency_hz`.
- The signature is recomputed from the payload and must equal both `models.signature` and the payload's
  `signature`. No factor × maximum reconstruction; the absolute `frequency_hz` is the signature input.
- `model_signature` is an identity string and is not re-encoded from the payload (data contracts §3.3).

**Lattice signature** (`mocassin/lattice-signature/v1`, `0x00`, then the projection):

```text
array32(5)
  uint64(1)
  str32(model_signature)
  array32(3) uint64(a), uint64(b), uint64(c)
  uint64(position_count)
  bin32(particles)
```

- The lattice projection binds the referenced model's `model_signature`, the lattice size, the position count,
  and the particle array. It must equal `lattices.signature`.
- The canonical projection uses `bin` for the particle array even though the stored payload uses an array.

`ComputeProjection` materializes bytes for golden tests; `ComputeSignature` streams the projection into
SHA-256. The Rust test decodes each checked-in `tests/contracts/projections/parameterization/*.bin` and
`.../lattice/*.bin` with a strict fixed-width reader, re-encodes it, and compares both the bytes and the domain
hash. That proves the encoder against .NET-generated vectors without a .NET dependency.

#### 3.7 Errors and exit mapping

```rust
pub enum PackageError {
    Io(std::io::Error),
    Sqlite(rusqlite::Error),
    SchemaVersion { found: u32, supported: RangeInclusive<u32> },
    MissingSchemaElement { what: &'static str, name: &'static str },
    InvalidSchemaInfo(String),
    InvalidFrame { section: &'static str, detail: String },
    PayloadTooLarge { section: &'static str, declared: u64, limit: u64 },
    ElementBudgetExceeded { section: &'static str, limit: u64 },
    Scan { section: &'static str, detail: String },
    Decode { section: &'static str, detail: String },
    SignatureMismatch { what: &'static str, expected: String, actual: String },
    InvalidIndex { what: &'static str, value: u64, limit: u64 },
    LengthMismatch { what: &'static str, expected: u64, found: u64 },
    InvalidValue { what: &'static str, detail: String },
    ColumnMismatch { column: &'static str, column_value: String, payload_value: String },
    JobNotFound { job_id: u32 },
    Unsupported(String),
}
```

`PackageError` is wrapped in `Context { job_id: Option<u32>, model_id: i64, section: &'static str, source }`
so every diagnostic names the job/model and section. `kind()` unwraps the context for exit mapping.

The **canonical solver exit-code table is [`01-target-architecture.md`](../01-target-architecture.md) §7.3**:
`0` completed; `5` partial success (multi-job/batch; at least one job failed, others completed; reserved until
Step 19); `10` invalid arguments; `11` package error; `12` model/data error; `13` numerical failure or
internal invariant violation (including a caught panic); `14` aborted by a limit; `15` cancelled; `16` I/O
error. The solver table is the `mocassin-sim` process contract; it is not the .NET `mocassin` table, and a
consumer must select the table by process.

Mapping in this reader/CLI:

| Error class | Exit |
| --- | ---: |
| Success (dry run) | 0 |
| Batch partial success (reserved; Step 19) | 5 |
| Invalid arguments, unknown job, unsupported option, extension requested | 10 |
| Package/schema/frame/decode/scan/size error | 11 |
| Signature, index, length, value, column mismatch | 12 |
| RNG state/internal invariant failure, caught panic | 13 |
| Run aborted by a configured limit (reserved in Step 13) | 14 |
| Cancelled (reserved; not emitted in Step 13) | 15 |
| I/O error (missing/unreadable path) | 16 |

Feature 5 adds a documentation drift check that the solver rows in `docs/reference/cli.md`,
`docs/development/solver-architecture.md`, and this page match the architecture §7.3 table exactly (code and
meaning); if a script is not justified, the closeout records the manual comparison.

### 4. RNG

ADR 0005 stands: ChaCha12, SHA-256 seed derivation, job-level parallelism only. This step does not change the
algorithm.

#### 4.1 Seed derivation

Rust reproduces the Step 10 byte orders exactly:

```text
derive_job_seed(master[32], collection: u32, config: u32, job: u32)
  = SHA-256(master[32] || u32_le(collection) || u32_le(config) || u32_le(job)
            || UTF8("mocassin/job/v1"))

derive_lattice_seed(job_seed[32], lattice_request_hash[32])
  = SHA-256(UTF8("mocassin/lattice/v1") || job_seed[32] || lattice_request_hash[32])
```

- The `purpose` text is fixed by the Step 10 contract; it is not a free parameter of `derive_job_seed`.
  A private `derive_domain_seed(domain, parts)` supports forks and tests.
- `derive_job_seed` is exposed for verification and tests. A normal run uses the package's `jobs.seed`, which
  the .NET build already derived; the reader never re-derives it silently.
- `seed_hash(seed) = SHA-256(seed)` in lowercase hex. It is the only seed value in summaries and logs; the raw
  seed is never logged.
- `--seed <hex32>` replaces the effective seed for this process only. It is for debugging/reproduction and is
  recorded as the effective seed hash; production runs use the package seed.

#### 4.2 ChaCha12 stream

```rust
pub struct Rng { inner: rand_chacha::ChaCha12Rng }

impl Rng {
    pub fn from_seed(seed: [u8; 32]) -> Self;
    pub fn from_state(state: &RngStateV1) -> Result<Self, RngStateError>;
    pub fn export_state(&self) -> RngStateV1;
    pub fn next_u32(&mut self) -> u32;
    pub fn next_u64(&mut self) -> u64;
    pub fn next_f64(&mut self) -> f64;
    pub fn next_bounded(&mut self, upper: u32) -> u32;
    pub fn fork(&mut self, domain: &[u8]) -> Self;
}
```

- `rand_chacha::ChaCha12Rng` is the cipher and buffering implementation. `mocassin-rng` uses its public
  `get_seed`, `get_stream`, `set_stream`, `get_word_pos`, and `set_word_pos` APIs. It never serializes the
  crate's internal memory layout.
- The stream is the crate's documented 64-bit block counter + 64-bit stream id; `word_pos` is the 32-bit-word
  offset (a 68-bit value).
- Draw semantics:

| Method | Words consumed | Result |
| --- | ---: | --- |
| `next_u32` | 1 | Uniform `u32` |
| `next_u64` | 2 | Low word first, then high word |
| `next_f64` | 2 | `(next_u64() >> 11) as f64 * 2^-53`, in `[0,1)` |
| `next_bounded(u)` | 1 per attempt | Lemire multiply-and-reject; returns `[0, u)`; `upper == 0` returns 0 and consumes nothing |
| `fork(domain)` | 0 (parent) | New stream from a SHA-256 derivation |

- `next_bounded` is unbiased: `m = (u32 as u64) * (upper as u64)`, `l = m as u32`; reject while
  `l < (upper.wrapping_neg() % upper)`. Never use modulo. The result is half-open: `0 <= result < upper`.
- `fork(domain)` hashes
  `"mocassin/rng-fork/v1" || 0x00 || key[32] || stream_le64 || word_pos_le128 || domain` and returns a new
  `Rng` from the 32-byte digest with `stream = 0`, `word_pos = 0`. The parent is not advanced. `domain` must be
  non-empty and at most 255 bytes; the widths are 8 bytes for `stream` and 16 bytes for `word_pos`, both
  little-endian. The derivation is part of the reproducibility contract.
- The solver never uses `thread_rng`, a time seed, or an OS random source.

#### 4.3 `RngStateV1` (64 bytes)

`rngAlgorithm = "chacha12-v1"` and `rngState` in `.mcs` metadata are bound to this layout:

| Offset | Size | Field |
| ---: | ---: | --- |
| 0 | 4 | magic `0x4D 0x43 0x52 0x31` (`"MCR1"`) |
| 4 | 4 | `format_version` (`u32` little-endian, value 1) |
| 8 | 8 | `stream` (`u64` little-endian) |
| 16 | 16 | `word_pos` (`u128` little-endian) |
| 32 | 32 | ChaCha12 key / seed (`[u8; 32]`) |

- `export_state` records the key, stream, and the current `word_pos`; it does not expose the crate's buffered
  block or memory layout. **The state carries the raw 32-byte key by design**, so a `.mcs` is as sensitive as
  the seed: never log `rngState` or the key, and expose only `seedHash` in logs/summaries. A future `.mcs`
  consumer must treat the key as secret material, not as a leak or a checksum.
- `from_state` validates the magic, version, and a `word_pos` whose block counter (`word_pos / 16`) fits the
  crate's 64-bit counter (so `word_pos < 2^68`); it reconstructs the cipher and resumes at the exact 32-bit
  word. Mid-block resume repopulates the buffer, so the next draw is identical to an uninterrupted stream.
- Round-trip tests must resume at word positions 0, 15, 16, 17, 1000, and a position after a `fork`.
- The `.mcs` reader (Step 18/20) rejects a different `rngAlgorithm`, a `rngState` length other than 64, or a
  bad magic/version.
- **Format-breaking scope (n-2).** A change to the ChaCha12 **draw semantics** (word consumption, `next_f64`
  construction, `next_bounded` algorithm), the **64-byte state layout**, or the algorithm id is a state-format
  break: new algorithm id such as `chacha12-v2` plus a new ADR. A change to the **seed derivation** or
  **fork derivation** changes reproducibility for new runs but not the state layout; it is still a
  reproducibility-contract change and needs an ADR/version note because it changes every derived job seed.
  A change to the **`fork` domain** or `config_hash` construction is in the reproducibility-contract group.

#### 4.4 Streams and determinism

- **Simulation stream:** one continuous stream for the whole trajectory. A KMC pre-run and its main run share
  the stream because the state is continuous; `prerun.mcs` carries the `RngStateV1` that the main run resumes.
  Do not fork between pre-run and run.
- **Extension stream:** each extension gets
  `rng.fork(b"extension:" || id_bytes || 0x00 || config_hash[32])`, where `config_hash` is the 32-byte
  SHA-256 of the UTF-8 `config_json`; optional extension draws cannot perturb the core trajectory.
- **Lattice stream:** build-side only, in .NET Step 10. Rust exposes `derive_lattice_seed` for vectors and
  verification but never generates a lattice.
- Same package + same binary + same platform + same effective seed ⇒ identical draws and state content.
  Cross-platform results are statistically equivalent; bit-exact floating-point comparisons are not a gate
  (ADR 0005).
- Golden vectors freeze derivation, draws, bounded draws, forks, and state export. A change to any of them is a
  format break, not a refactor.

#### 4.5 Performance obligation

The RNG must not dominate KMC. `crates/mocassin-rng/benches/draws.rs` (Criterion) measures, single-threaded,
release, on the benchmark laptop class:

| Measurement | Budget |
| --- | ---: |
| `next_u32` | ≥ 250 M draws/s |
| `next_u64` | ≥ 150 M draws/s |
| `next_f64` | ≥ 120 M draws/s |
| `next_bounded(2/10/1024)` | ≥ 200 M calls/s, ≤ 1.01 draws/call average |
| buffer refill (64 words) | ≤ 250 ns (≥ 1 GB/s effective) |
| `export_state` + `from_state` round trip | ≤ 500 ns |
| `fork` (32-byte domain) | ≤ 1 µs |

- The refill measurement compares a cold `from_seed` + 64 draws with a warm 64-draw loop, or uses
  `set_word_pos` at block boundaries; the recorded method and raw numbers go to
  `tests/reference/bench/step-13-rng.json`.
- The baseline is recorded in `tests/reference/bench/baselines.json` as a **same-runner-class** entry (OS, CPU
  class, Rust version, commit), next to the raw `step-13-rng.json`. A 10 % regression is measured only against
  that class; never compare absolute timings across machines. Step 13 records the first RNG/load baseline;
  Step 29 owns the automated same-class regression job.
- If a budget is missed, profile first. Do not change the algorithm or draw semantics; those are format
  decisions under ADR 0005. A genuine algorithm change is a new ADR and a format version.

### 5. `mocassin-sim` CLI scaffold

```text
mocassin-sim --db <project.msl> --job <id> [--io <dir>]
             [--stdout <file>] [--seed <hex32>] [--threads <n>]
             [--extension <path>] [--no-extensions] [--jump-log-max-ev <f64>]
             [--log-level <error|warn|info|debug|trace>]
             [--log-format human|json] [--dry-run] [--version]
```

| Option | Step 13 behavior |
| --- | --- |
| `--db <path>` | Required. Read-only `.msl` path. |
| `--job <id>` | Required. 1-based `u32`; unknown id is exit 10 with the available ids. |
| `--io <dir>` | Accepted; not created in Step 13. Required only when a future step writes a job folder. |
| `--dry-run` | Loads, validates, and prints the summary; writes nothing; exits 0. |
| `--stdout <file>` | Rejected with `--dry-run` (usage error 10): a dry run must not write. Reserved for the run dump. |
| `--seed <hex32>` | Exactly 64 hex characters (case-insensitive); overrides the effective seed; reported by hash. |
| `--threads <n>` | Accepted and validated `1..=256`; unused until Step 19. |
| `--extension <path>` | Parsed; Step 13 returns `Unsupported` (10), because the ABI is Step 19. |
| `--no-extensions` | Accepted; no-op in Step 13. Conflicts with `--extension` (10). |
| `--jump-log-max-ev <f64>` | Accepted and validated finite `> 0`; unused until Step 18. |
| `--log-level` | `error`/`warn`/`info`/`debug`/`trace`; default `info`. |
| `--log-format` | `human`/`json`; default `human`. Logs go to stderr. |
| `--version` | Prints `mocassin-sim <version>`; exit 0. |

- In Step 13, `--dry-run` is required for the inspection path. Without it, the CLI loads and validates the job
  and exits 10 with `unsupported: simulation engines are implemented in Steps 14–19`; it does not create
  `--io`, write a state, or claim success. Step 16 enables a real run and Step 19 completes the option surface.
- Parse errors (including unknown options, a bad enum value, or `--dry-run --stdout`) are remapped from
  `clap`'s default exit 2 to exit 10 with a CLI-owned `error: ...` line. Help and `--version` exit 0.
- `RUST_LOG` is not consulted; `--log-level` is the only filter so scripts get deterministic output.
- A panic in the inspection path is caught at `main`, logged as an internal error, and exits **13** (numerical
  failure or internal invariant violation). The dry-run path writes nothing, so a caught panic cannot leave a
  partial file.

**Dry-run output.** One line per group, stable order, invariant formatting, stdout (unless `--stdout` is
rejected):

```text
mocassin-sim 0.1.0 dry-run
package: <path>
schema: format_version=1 project_id=<uuid> job_selection=all
model: signature=<64hex> model_signature=<64hex> particles=5 positions=4 sites=4 environments=4 pair_tables=0 cluster_tables=0
lattice: signature=<64hex> size=6x6x6 length=864
job: id=1 kind=kmc simulation_index=0
settings: temperature_k=773 target_mcsp=150 write_out_count=100 pre_run_mcsp=250 time_limit_s=31536000 min_success_rate=null electric_field_vm=10000000 flags=0x72
transitions: collections=1 directions=2 rules=4
trackers: global=2 static_per_cell=16
rng: algorithm=chacha12-v1 seed_hash=<64hex>
validation: ok
```

- The `settings:` line prints the **resolved job settings** and the composed flags, not
  `simulations[].default_settings`. The fixture's job 1 has `target_mcsp = 150` while simulation 0's default
  is 200; that difference is expected.
- The model/lattice/job counts and signatures must equal `inspect-v1`/`package-summary-v1` for the same job.
- `app_version`, `created_utc`, and `generator` are not printed; they are build metadata.
- No `--json` in Step 13. The human format is frozen in `docs/reference/cli.md`; a machine report is Step 19.

### 6. Data-oriented base structures

Step 13 fixes the layout, but only the reader fills it. Physics state is Step 14.

- `InitialLattice`: flat `Vec<ParticleId>` in the documented row-major order, with `[u32;3]` dimensions and
  explicit strides. No `A+B+C`; `A*B*C*P`, checked in `u64`.
- `Position`: `Vector4I` as `[i32;4]`, site index, stable flag, `u64` allowed mask. The mask is reconstructed
  from `particle_ids`; no `1 << id` on a 32-bit integer.
- `EnvironmentModel`: per-position pair and cluster link vectors; Step 14 builds the supercell CSR, target
  instances, cluster links, and sorting.
- Energy tables: flat `Vec<f64>` with explicit row strides (`particle_count²` for pairs); cluster tables keep
  sorted `u64` codes and aligned energies.
- `DirectionPool`: `{ direction_count: u32, members: Vec<(PositionId, ParticleId)>, slot_of: Vec<u32> }` with
  deterministic insertion order and `swap_remove`; Step 14 populates it from the selection mask and applies the
  optimizers. The `slot_of` array gives O(1) removal and is updated on swap.
- `JumpStatus`: `{ offsets: Vec<u32>, links: Vec<JumpLink> }`; Step 13 defines the container and invariants,
  Step 14 computes `[cell][direction]` entries.
- No `HashMap` in a per-cycle structure. The model cache may use `HashMap`; it is not hot.
- All hot arrays are allocated at load/initialization; Step 14 proves the no-allocation-in-cycle claim. Step 13
  only guarantees that the reader's allocations are bounded.

### 7. Cross-language contract artifacts

| Artifact | Location | Producer | Consumer | Tier |
| --- | --- | --- | --- | --- |
| `.msl` v1 package + summary | `tests/contracts/packages/msl-v1/` | .NET Step 11 writer | Rust reader, dry-run | PR-medium |
| Pre-amendment package | `tests/contracts/packages/amend-1-net10/` | .NET Step 11 writer (net10) | Rust reader | PR-medium |
| Populated pair/cluster + MMC package + summary | `tests/contracts/packages/step-13-populated/` | .NET real writer from a checked-in sample | Rust reader, signature and MMC paths | PR-medium |
| C-centered equivalent-start + MMC package + summary | `tests/contracts/packages/step-13-centered/` | .NET real writer from the Step 08 C-centered sample | Rust mapping/iteration, Step 16 exact check | PR-medium |
| Raw model/job/lattice payloads | `tests/contracts/packages/msl-v1/payloads/` and the populated package's extracted payloads | .NET writer | Rust scan/decode | PR-fast |
| Parameterization projection vectors | `tests/contracts/projections/parameterization/*.bin` | .NET Step 09 | Rust projection reader/writer | PR-fast |
| Lattice projection vectors | `tests/contracts/projections/lattice/*.bin` | .NET Step 11 | Rust projection reader/writer | PR-fast |
| Signature metadata | `tests/contracts/signature-vectors.json` | .NET | Rust test reads ids/digests | PR-fast |
| RNG vectors | `tests/contracts/rng-vectors.json` | Rust (frozen once) | Rust RNG tests | PR-fast |
| Dry-run golden | `tests/contracts/packages/msl-v1/dry-run-v1.txt` | Rust CLI | Rust CLI test | PR-fast |
| Load/RNG bench evidence | `tests/reference/bench/step-13-*.json` | Criterion runner | closeout evidence | Nightly/release |
| Same-class baseline entry | `tests/reference/bench/baselines.json` | Criterion runner | 10 % regression gate | Nightly/release |
| Fuzz corpus | `src/rust/crates/mocassin-msl/fuzz/corpus/` | Rust + fixture seeds | fuzz targets | Nightly |

- **Round-trip meaning.** Rust never writes `.msl`. The contract is: .NET writes, Rust reads and validates; the
  Rust logical summary (counts, signatures, seed hash) equals the .NET `package-summary-v1` subset. The
  canonical projection has a true round-trip: Rust decodes the .NET `.bin` projection, re-encodes it, and must
  produce byte-identical output and the same domain hash.
- `rng-vectors.json` is a test-vector document, not a runtime format; it has `format =
  "mocassin.rng-vectors"`, `formatVersion = 1`, and `algorithm = "chacha12-v1"`. It is not added to
  `contracts/manifest.json`, because it is not a package/state/job/event contract and no writer emits it.
- The new `step-13-populated` package is built by the real .NET writer from a committed sample that has
  non-empty pair and cluster tables, an MMC job, **a non-trivial per-node mask difference** (at least one node
  where one direction in a collection admits a particle another direction does not), and **at least one
  direction mapped into a different symmetry-equivalent start bucket**. Its Step 13 tests cover the void-exempt
  collection-union check and equivalent-start mapping; the exact per-direction check is a separate Step 16 test
  that consumes the same fixture. It is a test artifact, not a schema change. If no existing sample combines
  these, feature 6 adds a small sample under `samples/` and builds the package from it; do not hand-author the
  `.msl`. **Resolved:** the populated fixture carries the mask difference and the `step-13-centered` fixture
  carries both the real cross-bucket equivalent-start mapping and the mask difference after the Step 08 fix
  `1f5b473`; the earlier `step-13-amend-1` amendment is withdrawn. Step 16 consumes the centered fixture.
- The `step-13-centered` fixture is built by the real writer from
  `samples/step-13-centered/step-13-centered.mocprj`, derived from `samples/simulations/centered-exchange.mocprj`.
  Its C-centering gives four translation-equivalent starts per site; the real `jump_mapping` contains
  cross-bucket entries, and the extended B particle set gives the mask difference. `CenteredPackageFixtureTests`
  and the Rust `centered_fixture` tests assert both properties and rebuild reproducibility.
- Fixtures are produced by the real writers and never hand-edited. The Step 11 regeneration command remains the
  `.msl` update path; the pre-amendment package is byte-fixed.
- A failed fixture comparison is a contract failure, not a test flake.
- No `contracts/manifest.json`, generated schema, or package/payload version changes in this step.

### 8. Documentation plan

- **New** `docs/development/solver-architecture.md`: crate graph, package read pipeline, limits, runtime model
  layout, RNG streams/state, CLI foundation, and step ownership.
- **Update** `docs/reference/msl-format.md`: the solver read path, stored-width vs canonical-width rule, lattice
  array encoding, bounded decode, signature re-encoding, and the `mocassin verify` division.
- **Update** `docs/reference/cli.md`: the `mocassin-sim` synopsis, options, dry-run format, stdout/stderr,
  logging, and solver exit codes.
- **Update** `docs/development/testing-and-parity.md`: Step 13 fixtures, tiers, property/mutation tests, fuzzing,
  and the load/RNG budgets.
- **Update** `docs/development/code-structure.md`: the Rust workspace layout rules.
- **Update** parity/status/step page/closeout. **Closeout evidence:** `docs/planning/closeout/step-13.json`,
  created from [`_template.json`](../closeout/_template.json) and checked with
  `python tools/check_closeout_evidence.py docs/planning/closeout/step-13.json`. It binds the audited commit,
  the closeout CI `head_sha`, the `main` SHA at freeze, and the merge candidate. Rust API docs are generated by
  `cargo doc --workspace --no-deps` with `missing_docs = deny`; the site's `/api/rust/` link already exists. The
  implementor adds the new page to `zensical.toml` navigation in feature 7 (configuration is
  implementation-owned; the planner does not edit it).
- No hand-written API reference page; public Rust items carry doc comments.

### 9. Test plan

| Test | Assertion | Tier |
| --- | --- | --- |
| Reader acceptance | the `msl-v1` fixture opens read-only; the dry-run counts/signatures equal the canonical summary for job 1 | PR-medium |
| Reader compatibility | the `amend-1-net10` fixture opens and yields the same logical summary | PR-medium |
| Populated/MMC acceptance | the populated real-writer package has non-empty pair/cluster tables, an MMC job, and a non-trivial per-node mask difference; both signatures re-encode; MMC kind/break fields validate. The `step-13-centered` real-writer package carries the cross-bucket equivalent-start mapping and a mask difference | PR-medium |
| Schema/version | wrong application id, user version, future `format_version`, missing table/column, malformed `schema_info`; `models`/`lattices` `format_version` columns match their payloads | PR-fast |
| Decode | every raw payload decodes; unknown keys are ignored; minimum-width integers, array `lattice`, `null` optionals | PR-fast |
| Bounds | truncated frame, oversized declared size, decompression bomb, element-budget/depth violation fail without OOM | PR-fast |
| Validation | each invalid index/length/NaN/kind/flag/tracker case returns the documented error | PR-fast |
| Occupation | void is exempt; every non-void state particle is in the collection-union mask; the four `msl-v1` rules pass; exact per-direction occupation is a Step 16 test | PR-fast |
| Equivalent starts | a direction mapped into another symmetry-equivalent start bucket is accepted; the reader does not require `direction.position_id == position` | PR-fast |
| Element budget | a 512 MiB writer-valid payload is within 512M elements; the 1M-rule/16M-link caps are per-field; a raised `MaxRuleCount` needs a raised `ReaderLimits` | PR-fast |
| Conservation | each rule preserves the non-void particle-symbol multiset and charge within `1e-9`; a mutated state/particle is rejected | PR-fast |
| Movement/transport | `path_vector` equals the cell-vector sum of `movement`; `length` equals its norm; a no-charge-change rule has a zero transport vector; a mutated movement/path vector is rejected | PR-fast |
| Signatures | parameterization and lattice re-encode from the payload; every `.bin` vector round-trips byte-identically; a mutated value is rejected | PR-fast |
| RNG derivation | job/lattice seed vectors match Step 10 | PR-fast |
| RNG draws | 16-word streams, `next_u64`, `next_f64` range, `next_bounded` goldens/uniformity and half-open range, `fork` separation and widths | PR-fast |
| RNG state | 64-byte round-trip; resume at 0/15/16/17/1000; mid-block continuation equals the uninterrupted stream | PR-fast |
| CLI | dry-run golden; exit 0/10/11/12/13/14/15/16, with 5 reserved for Step 19 batch and not emitted; parse-error remap; help/version; `--stdout` conflict; no files written | PR-fast |
| Exit-code drift | the solver rows in `cli.md`, `solver-architecture.md`, and the step page match architecture §7.3 code-for-code | PR-fast (script) or closeout manual |
| Property/mutation | random truncations and byte mutations of real payloads never panic or exceed the limits | PR-fast |
| Read-only | `0444` package opens; no `-wal`/`-shm`; no write API | PR-fast |
| Fuzz | `payload_scan`, `payload_decode`, `package_open` run time-boxed with no crash/OOM | Nightly/manual |
| Performance | ceria job load `< 100 ms`; RNG budgets; same-class baseline recorded | Nightly/release |

No mocks: the tests use real SQLite files, real checked-in packages, real raw payloads, and a real temporary
directory. The only process boundary is the CLI exit-code test, which runs the built binary.

### 10. Performance budgets

| Scenario | Budget |
| --- | ---: |
| Load + validate one ceria job from the fixture (release, warm) | `< 100 ms` |
| `mocassin-sim --dry-run` end to end, ceria fixture | `< 250 ms` on the benchmark laptop class |
| Peak RSS, ceria fixture | `< 64 MiB` |
| RNG | §4.5 |

The load budget covers the schema checks, one model decode, one lattice decode, one job decode, validation, and
two signature re-encodes. It is a **fixture budget**, not a promise for a 512 MiB package: a large payload is
bounded by the guard but may take proportionally longer. The benchmark records the toolchain, CPU class,
commit, and raw timings in `tests/reference/bench/step-13-load.json` and a same-class entry in
`tests/reference/bench/baselines.json`.

### 11. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| .NET↔Rust schema drift | DTO tests decode every real payload; cross-language fixture and projection vectors; no hand-written second schema |
| Malicious/corrupt package | Non-allocating scan, element/depth/byte budgets, checked arithmetic, fuzzing, no `unwrap` on input |
| RNG change breaks reproducibility | `chacha12-v1` id, 64-byte state layout, golden vectors, ADR 0005; a change requires a new ADR/version |
| RNG performance misses the KMC budget | Criterion budget early, profile before changing; the draw semantics are fixed |
| `rand_chacha` API change | Pin the workspace version; only `mocassin-rng` touches the state APIs; the byte layout is first-party |
| SQLite/Zstd build issues | `rusqlite` bundled; workspace cache; the `zstd` crate is already a dependency |
| Reader too strict and rejects a valid future package | Supported range `1..=1` is explicit; a future major is rejected with a clear message; `ReaderLimits` is overridable |
| Reader too permissive and accepts a corrupt package | Semantic pass plus signature re-encode; mass/charge/movement checks; `mocassin verify` remains the full check |
| Per-direction occupation is not fully reader-verifiable (M-3) | v1 payload has no rule→representative-direction marker; reader exempts void and checks the collection-union mask, Step 16 validates the resolved direction/node mask. Residual risk recorded in §3.5/§12 and the Step 16 page |
| `charge_transport_vector` positional formula is not reader-verifiable | Reader checks finite/zero-consistency; Step 15 owns the field-term bound/use; v1 payload has no fractional P1 coordinates. Residual risk recorded in §3.5 and the Step 15 interface; a future `.msl` can add positions or sign the vector |
| CLI contract drift | `cli.md` freezes the dry-run format and exit codes; golden test and doc-drift check; no `--json` yet |
| `--seed` misuse | Documented as debugging; recorded only as a hash; production uses the package seed |
| Fuzz toolchain unavailable | Nightly + `cargo-fuzz` are Tier 2; run the time-boxed targets; if unavailable, record the accepted risk and keep the PR-fast mutation suite |
| Step size | 4.5 pw owner-approved single-step exception; no split; guard: stop before feature 4 if features 1–3 exceed 2.0 pw (§13) |

### 12. Dependencies and interfaces

- **Step 11 (Done):** the `.msl` v1 schema, payload field set, `application_id`/`user_version`, publication
  barrier, `msl-v1` and `amend-1-net10` fixtures, `package-summary-v1`, and the payload/limit conventions. The
  Rust reader consumes these without changing them.
- **Step 10 (Done):** the job/lattice seed tuple, endianness, and domain strings. Rust reproduces them for
  vectors and verification; the package seed remains authoritative.
- **Step 09/08 (Done):** the six-field parameterization projection, the five-field lattice projection, the
  fixed-width canonical encoding, and the `.bin` vectors. Rust reproduces them.
- **Step 12 (Done):** the solver exit-code table (0, 5, 10–16; 5 reserved for Step 19 batch) and the `mocassin simulate` reservation. Step 13
  implements the solver side only.
- **Step 14 (next):** consumes `JobPackage`, `ParameterizedModel`, `InitialLattice`, `EnvironmentModel`, `Rng`,
  and `DirectionPool`; owns supercell expansion, links, pools, jump status, energy runtime, resume, and building
  the context. Step 16 validates the exact per-direction state/position occupation that the v1 payload cannot
  prove to Step 13 (M-3). Step 13 must not implement context initialization.
- **Step 15:** owns the field term and the residual `charge_transport_vector` trust boundary; Step 13 validates
  finite/zero-consistency but cannot recompute the positional formula from the v1 payload. The Step 15 page
  records the assignment and must reject a non-finite field result.
- **Step 18/20:** consume `RngStateV1` and `seed_hash`; `.mcs` `rngAlgorithm`/`rngState` are bound to §4.3.
- **Step 19:** completes the CLI (`batch`, `--validate-only`, `--no-state`, `--log-file`, extensions, HPC).
- **Step 29:** extends fuzzing to `.mcs`/extension configs and owns automated benchmark regression.

No unresolved cross-step interface question remains. The only deliberate asymmetry is that Rust does not write
`.msl` and does not run `integrity_check`; both are stated in the contract and the reader tests.

### 13. Sizing and split-trigger check

- **Estimate:** **4.5 person-weeks** = 3.5 pw base implementation + 1.0 pw risk contingency. The upper bound
  exceeds the four-week §3.1 trigger. The earlier “3.0–4.0 pw, no split” claim was arithmetically wrong and is
  withdrawn.
- **Owner decision (2026-11-18):** approved as a **single step at 4.5 pw** under the §3.1 single-step exception,
  following the Step 09/12 precedent, with **no split**.
- **Breakdown:** feature 1 ≈ 0.3 pw; feature 2 ≈ 0.5 pw; feature 3 ≈ 0.7 pw; feature 4 ≈ 1.0 pw (adds the
  scientific-invariant checks); feature 5 ≈ 0.3 pw; feature 6 ≈ 0.5 pw (adds the populated pair/cluster + MMC
  fixture); feature 7 ≈ 0.5 pw; ~0.2 pw integration; 1.0 pw contingency. Total 4.5 pw.
- **§3.1 triggers:** seven features is inside the 6–8 range, but the upper estimate is beyond four weeks. The
  audit is still a bounded new-crate/reader review.
- **13A/13B split — considered and rejected (owner, 2026-11-18).** `13A` (workspace/types/RNG/bounded reader +
  fixture, ~3.0–3.5 pw) and `13B` (mapping/M-2/signatures/dry-run/fuzz/benchmarks/docs/closeout, ~1.5–2.0 pw)
  would separate the reader from its CLI acceptance, duplicate fixture/closeout work, add a roadmap step, and
  change the Phase D closeout. The seven features form one coherent reader/RNG/CLI contract. The split is
  withdrawn.
- **Guard:** if features 1–3 exceed **2.0 pw**, the step agent stops and reports to the orchestrator before
  feature 4. Do not silently absorb the overrun.
- **Owner approval:** the feature list and the 4.5 pw single-step exception were approved by the human owner on
  2026-11-18; the design review is accepted `Ready with notes`. The implementation hold was lifted on
  2026-11-19, the design merged as `056d8c1`, and the step branch is created from it. The decisions are
  not re-approved per feature.

### 14. Decisions recorded and owner input

| Decision | Record |
| --- | --- |
| ChaCha12, SHA-256 derivation, job-level parallelism unchanged | ADR 0005 |
| RNG state is a 64-byte first-party `RngStateV1`; `rngAlgorithm = "chacha12-v1"`; the layout and the state/layout-change-is-format-breaking rule are recorded as a **data-contract decision note**, not an ADR, with no schema bump | This page §4.3; [`02-data-contracts.md`](../02-data-contracts.md) §4.3/§4.5 |
| Stored payloads use smallest-width integers; fixed width is canonical-projection only | This page §2.1; data contracts §3.3 |
| `.msl` v1 lattice payload is a MessagePack array of `u8`; the canonical projection uses `bin` | This page §2.1/§3.6; `msl-format.md` |
| Solver reader bounds and the no-`integrity_check` split with `mocassin verify` | This page §3.3/§3.1; data contracts §3.4 |
| Solver exit codes 0/5/10–16; parse errors remap to 10; dry-run is write-free | This page §3.7/§5; architecture §7.3 (canonical) |
| RNG performance budget and 10 % same-runner-class regression rule | This page §4.5; quality strategy §13; `tests/reference/bench/baselines.json` |
| Seven features; 4.5 pw single-step exception; no split; 13A/13B considered and rejected | This page §13; owner-approved 2026-11-18 |

**New ADRs:** none. ADR 0005 stands; this step implements it. The `RngStateV1` layout is recorded as the
data-contract decision note in [`02-data-contracts.md`](../02-data-contracts.md) §4.3/§4.5: the field already
existed and was documented as “length/algorithm documented in Step 13”, so no schema version bump is needed.
The same note carries the `chacha12-v1` binding and the rule that a change to the draw semantics, the 64-byte
layout, or the algorithm id is format-breaking. The reader limits and exit-code mapping are step-level
decisions recorded here, in the data contracts, and in the architecture.

**Owner decisions:** the design verdict (`Ready with notes`), the seven-feature list, the 4.5 pw single-step
exception, and n-1 (data-contract note, no ADR) are accepted by the owner on 2026-11-18. The implementation hold
was lifted on 2026-11-19, the design merged into `main` as `056d8c1`, and the step branch was created from that
commit. No other design decision is open.

## Implementation tasks

- [x] Create the `mocassin-types` payload DTOs, typed IDs, runtime model/job/lattice types, `ReaderLimits`, and
      the package error model.
- [x] Create the `mocassin-rng` derivation, ChaCha12 wrapper, draw methods, `RngStateV1`, golden vectors, and
      benchmark.
- [x] Create the `mocassin-msl` read-only open, schema/version/provenance checks, Zstd frame handling, bounded
      MessagePack scan, typed decode, and the model cache.
- [x] Implement the payload → runtime mapping and every index/length/finite/kind/tracker validation.
- [x] Fix the `TransitionPayloadDtos.cs`/`CompiledTransitionModel.cs` XML comments to the node-wise
      endpoint-moment charge-transport formula (feature 4; source docs only).
- [x] Implement the M-2 scientific checks: allowed occupations, mass/charge conservation, movement/path-vector
      consistency, and the zero-consistency transport-vector check; record the Step 15 assignment for the full
      positional formula.
- [x] Implement the canonical projection writer, quantization, and the parameterization/lattice signature
      re-encoding.
- [x] Implement the `mocassin-sim` parser, logging, `--version`, dry-run summary, unified exit mapping, panic
      containment, and the exit-code drift check.
- [x] Implement the data-oriented base structures (lattice, positions, environments, tables, pools, jump-status
      container). `DirectionPool` and the `JumpStatus`/`JumpLink` containers are type-only definitions with
      their invariants tested; Step 14 populates them.
- [x] Add the cross-language fixtures (including the populated pair/cluster + MMC package), RNG vectors,
      dry-run golden, property/mutation tests, and fuzz targets.
- [x] Run the time-boxed fuzz targets and record the result.
- [x] Record the load and RNG benchmarks and add the same-runner-class baseline entry.
- [x] Write `solver-architecture.md` and update `msl-format.md`, `cli.md`, `testing-and-parity.md`, and
      `code-structure.md`; add the nav entry.
- [x] Update the parity matrix, status, step page, and `docs/planning/closeout/step-13.json`; run
      `python tools/check_closeout_evidence.py`; request the audit.

## Acceptance criteria

- [x] `mocassin-sim --db tests/contracts/packages/msl-v1/ceria-temperature-sweep.msl --job 1 --dry-run` exits 0
      and prints the documented summary; every count/signature equals the canonical summary for job 1.
- [x] The `amend-1-net10` package also opens and validates; the populated pair/cluster + MMC package validates
      and both signatures re-encode.
- [x] Each rule preserves the non-void particle-symbol multiset and charge within `1e-9`; `path_vector` matches
      the cell-vector sum of `movement` and `length` matches its norm; a no-charge-change rule has a zero
      transport vector. Void is exempt from the occupation check; every non-void state particle is accepted by
      the collection-union mask and all four `msl-v1` rules pass; exact per-direction occupation is recorded as
      a Step 16 assignment. The full positional transport formula is recorded as a Step 15 assignment with the
      trust-boundary rationale.
- [x] A symmetry-equivalent-start direction mapped into another start bucket is accepted; the reader does not
      require `direction.position_id == position`. The real-writer `step-13-centered` package (Step 08 fix
      `1f5b473`) exercises cross-bucket entries, and the synthetic mapping test remains the focused reader
      assertion.
- [x] A 512 MiB writer-valid payload is within the 512M model-element budget; the 1M-rule/16M-link caps are
      per-field, and a raised `MaxRuleCount` requires a raised `ReaderLimits`. *The budget is verified by the
      documented arithmetic and the `ReaderLimits`/scan tests; no 512 MiB fixture is materialized. The
      `every_per_field_count_cap_is_enforced` test exercises rules, directions, collections, pair/cluster
      tables, and both link caps with lowered limits and a raised-limit acceptance.*
- [x] `models.format_version`/`lattices.format_version` columns match their payloads; the job settings are not
      compared with `simulations[].default_settings`.
- [x] The solver exit-code rows in `cli.md`, `solver-architecture.md`, and this page match architecture §7.3
      code-for-code, including reserved 5 (batch partial success) and 13/14/15.
- [x] Future package/payload versions and corrupt, truncated, oversized, or bomb payloads are rejected with the
      documented error/exit class, before unbounded allocation.
- [x] A changed frequency, energy, site background, or lattice particle is detected by the parameterization or
      lattice signature check; a valid package reproduces both signatures from the payload.
- [x] Every index/length/finite/kind/tracker invariant in §3.5 is validated; malformed input never panics.
- [x] Seed derivation matches the Step 10 vectors; RNG golden vectors, bounded draws, `[0,1)` doubles,
      domain-separated forks, and the 64-byte state round-trip/resume all pass.
- [x] The RNG draw/refill benchmark meets §4.5 and the numbers are recorded with the environment in the
      same-runner-class `tests/reference/bench/baselines.json` entry.
- [x] Loading the ceria job takes `< 100 ms` (release, warm, benchmark laptop class) and the measurement is
      recorded as a fixture budget, not a large-package promise.
- [x] The reader opens `0444` packages read-only, needs no `-wal`/`-shm`, and has no write API.
- [x] The time-boxed fuzz run finds no panic/OOM; the PR-fast mutation/property suite passes.
- [x] `cargo fmt --check`, `cargo clippy --workspace --all-targets -- -D warnings`, and `cargo test --workspace`
      pass; `missing_docs` is clean.
- [x] Documentation written and the strict docs build passes.

## Documentation

At this point, write:

- `docs/development/solver-architecture.md` — crates, reader pipeline, limits, runtime model, RNG streams/state,
  CLI foundation, and step ownership.
- `docs/reference/msl-format.md` — the solver read path and its validation/decoding contract.
- `docs/reference/cli.md` — `mocassin-sim` arguments, `--dry-run`, stdout/stderr, logging, and exit codes.
- `docs/development/testing-and-parity.md` — contract fixtures, tiers, property tests, fuzzing, and budgets.
- `docs/development/code-structure.md` — the Rust workspace layout.
- `docs/planning/appendix-b-parity-matrix.md`, `docs/planning/status.md`, this page,
  `docs/planning/closeout/step-13.json` (checked with `python tools/check_closeout_evidence.py`), and the audit
  request.
- Rust API docs via `cargo doc --workspace --no-deps`; no hand-written API reference.

## Tests

| Test | Assertion |
| --- | --- |
| Reader property | Random valid payloads round-trip; random truncations/mutations fail without panic/OOM |
| Acceptance | A real ceria package built by .NET opens and yields the expected dry-run summary |
| Schema | Application-id/user-version/format/provenance checks |
| Decode | MessagePack/Zstd round-trip, unknown keys, minimum-width integers, array/`bin` lattice |
| Validation | Bad indices, lengths, kinds, flags, trackers, finite values, signatures |
| Conservation | Per-rule symbol multiset and charge; mutated state/particle rejected |
| Movement/transport | `path_vector`/`length` vs `movement`; zero transport when no charge changes |
| Populated/MMC | Real populated pair/cluster + MMC package with a non-trivial per-node mask difference validates and re-encodes both signatures; the real C-centered package asserts cross-bucket equivalent starts and the mask difference |
| Signatures | Parameterization/lattice re-encode, `.bin` vectors, mutation detection |
| RNG | Derivation/draw/state golden vectors, bounded uniformity, double range, fork separation |
| CLI | Dry-run output, exit codes 0/5/10–16 (5 reserved), logging, parse-error remap, write-free |
| Exit-code drift | Solver rows match architecture §7.3 |
| Fuzz | No panic/OOM on malformed input |
| Performance | Load time and RNG budgets with same-class baseline |
| Read-only | `0444` package opens; write attempt fails; no WAL |

## Risks and mitigations

See §11. The dominant risks are contract drift with .NET, malicious/corrupt packages, and an RNG format or
performance change. They are mitigated by real-writer fixtures, projection vectors, a non-allocating scan,
fuzzing, and the versioned `RngStateV1`/`chacha12-v1` identity.

## Done when

The Rust solver can open a `.msl` read-only, validate and decode one job within the documented limits, reproduce
both stored signatures, derive/report the effective seed, export and restore the RNG state, and print a complete
dry-run summary without running any physics or writing any file.
