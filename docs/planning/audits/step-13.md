# Step 13 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-13.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 13 — Rust Solver Foundation and Package Reader |
| Request owner | Step agent (implementor) |
| Orchestrator | — (relayed by the orchestrator; do not edit) |
| Step agent (implementor) | Step 13 step agent |
| Audit initiated by | Orchestrator, on request of the step agent |
| Step branch | `step/13-rust-solver-foundation-and-package-reader` |
| Closeout evidence | [`docs/planning/closeout/step-13.json`](../closeout/step-13.json) |
| Audit date | 2026-11-20 |
| Auditor | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh context, no shared conversation with the planner, implementors, design reviewers, or orchestrator |

The closeout evidence file holds the audited commit, closeout CI run and `head_sha`, `main` SHA at freeze, merge candidate,
merge-tree CI, platform CI, and tag. Do not restate them here: one source, one place.

Step page: [`Step 13 — Rust Solver Foundation and Package Reader`](../steps/step-13-rust-solver-foundation-and-package-reader.md)

> Orchestration: the orchestrator spawns a fresh auditor when the step agent has frozen the step branch and
> completed the audit request, and relays the verdict and owner decisions. The step agent applies fixes on a new
> feature branch, reruns closeout CI, adds resolutions without altering reviewer text, and asks the orchestrator
> for re-audit until the auditor no longer identifies issues. The orchestrator does not fix findings or edit the
> audit record. The human owner approves the final verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | D. Solver |
| Depends on | 11 (simulation package and `.msl` writer); 10, 09, 08 for the seed/signature/fixture contracts |
| Delivers | A Rust workspace, validated runtime model/job types, a read-only bounded `.msl` reader, deterministic ChaCha12 RNG with a 64-byte resume state, canonical signature re-encoding, and the write-free `mocassin-sim --dry-run` inspection CLI. No Monte Carlo physics. |

## Step-specific acceptance checklist

Copy the acceptance criteria from the step page, preserving their wording:

- [x] `mocassin-sim --db tests/contracts/packages/msl-v1/ceria-temperature-sweep.msl --job 1 --dry-run` exits 0
      and prints the documented summary; every count/signature equals the canonical summary for job 1.
      — *Auditor: re-ran the release binary; output matches the frozen golden and `package-summary-v1`
      (signatures `1845b70e…`/`92cf375e…`, lattice `880b6eac…`, `6x6x6 length=864`, `target_mcsp=150`,
      `flags=0x72`, `seed_hash=7a92d942…`).*
- [x] The `amend-1-net10` package also opens and validates; the populated pair/cluster + MMC package validates
      and both signatures re-encode.
      — *Auditor: both packages open; populated job 1/2 (KMC/MMC) re-encode the stored signatures
      `7a23241c…`/`7036aac8…`/`cc146e07…`; the .NET verifier accepts the populated package.*
- [x] Each rule preserves the non-void particle-symbol multiset and charge within `1e-9`; `path_vector` matches
      the cell-vector sum of `movement` and `length` matches its norm; a no-charge-change rule has a zero
      transport vector. Void is exempt from the occupation check; every non-void state particle is accepted by
      the collection-union mask and all four `msl-v1` rules pass; exact per-direction occupation is recorded as
      a Step 16 assignment. The full positional transport formula is recorded as a Step 15 assignment with the
      trust-boundary rationale.
      — *Auditor: the mapping tests exercise the union, conservation, movement/path and zero-transport cases;
      the four `msl-v1` rules map; Step 15/16 pages carry the assignments.*
- [x] A symmetry-equivalent-start direction mapped into another start bucket is accepted; the reader does not
      require `direction.position_id == position`.
      — *Auditor: `map::tests::model::a_direction_may_be_mapped_into_another_start_bucket` passes; the
      real-writer populated fixture does not carry this property — see F-1.*
- [x] A 512 MiB writer-valid payload is within the 512M model-element budget; the 1M-rule/16M-link caps are
      per-field, and a raised `MaxRuleCount` requires a raised `ReaderLimits`. *The budget is verified by the
      documented arithmetic and the `ReaderLimits`/scan tests; no 512 MiB fixture is materialized.*
      — *Auditor: the code enforces the caps and the arithmetic is documented; the cap-specific tests are
      narrower than the checkbox claims — see F-5. No 512 MiB fixture is required by the design.*
- [x] `models.format_version`/`lattices.format_version` columns match their payloads; the job settings are not
      compared with `simulations[].default_settings`.
      — *Auditor: `package_schema` tests cover both column/payload mismatches; the fixture's job 1
      `target_mcsp=150` vs default `200` is accepted.*
- [x] The solver exit-code rows in `cli.md`, `solver-architecture.md`, and this page match architecture §7.3
      code-for-code, including reserved 5 (batch partial success) and 13/14/15.
      — *Auditor: `exit_code_drift` 4 tests pass; observed help/version 0, usage 10, package 11, model 12,
      panic 13, I/O 16; 5/14/15 are reserved.*
- [x] Future package/payload versions and corrupt, truncated, oversized, or bomb payloads are rejected with the
      documented error/exit class, before unbounded allocation.
      — *Auditor: schema/hostile tests pass; observed corrupt payload 11; decompression bomb rejected by
      declared size; the frame-without-size path streams under a hard limit.*
- [x] A changed frequency, energy, site background, or lattice particle is detected by the parameterization or
      lattice signature check; a valid package reproduces both signatures from the payload.
      — *Auditor: `signature_mismatch` 5 tests pass; a changed defect background, row signature, and payload
      signature are rejected as model class 12.*
- [x] Every index/length/finite/kind/tracker invariant in §3.5 is validated; malformed input never panics.
      — *Auditor: the mapping matrix covers the structural invariants; the provenance non-empty and
      `is_vacancy[0]` checks are missing — see F-8; fuzz/mutation runs found no panic.*
- [x] Seed derivation matches the Step 10 vectors; RNG golden vectors, bounded draws, `[0,1)` doubles,
      domain-separated forks, and the 64-byte state round-trip/resume all pass.
      — *Auditor: Rust RNG vectors and .NET `RngVectorsTests` pass; resume at 0/15/16/17/1000 and the
      nonzero-stream case pass.*
- [x] The RNG draw/refill benchmark meets §4.5 and the numbers are recorded with the environment in the
      same-runner-class `tests/reference/bench/baselines.json` entry.
      — *Auditor: re-ran `cargo bench -p mocassin-rng --bench draws`; every budget met (next_u32 734 M/s,
      next_u64 440 M/s, next_f64 392 M/s, bounded 562–571 M/s, refill 77.07 ns, state 121.25 ns, fork
      120.18 ns); `rustRng` entry present.*
- [x] Loading the ceria job takes `< 100 ms` (release, warm, benchmark laptop class) and the measurement is
      recorded as a fixture budget, not a large-package promise.
      — *Auditor: re-ran the load bench; open 188.83 µs, cold 352.62 µs, warm 68.65 µs; budget met; the
      `open` median is at the 10 % baseline boundary — see N-3.*
- [x] The reader opens `0444` packages read-only, needs no `-wal`/`-shm`, and has no write API.
      — *Auditor: a `0444` copy opened and loaded with no sidecars; `Package` exposes no write method and the
      connection is opened `SQLITE_OPEN_READ_ONLY` with `query_only`.*
- [x] The time-boxed fuzz run finds no panic/OOM; the PR-fast mutation/property suite passes.
      — *Auditor: re-ran 5 s per target on `x86_64-unknown-linux-gnu`: 151,171 / 139,332 / 5,076 runs, zero
      crashes, peak RSS 408/249/362 MiB; the 5 proptest properties pass.*
- [x] `cargo fmt --check`, `cargo clippy --workspace --all-targets -- -D warnings`, and `cargo test --workspace`
      pass; `missing_docs` is clean.
      — *Auditor: all ran in `./tools/verify.sh all --require` and in the dedicated Rust stage; clean.*
- [x] Documentation written and the strict docs build passes.
      — *Auditor: math/link checks, Zensical strict, DocFX and rustdoc all pass; the `dry-run-v1.txt`
      artifact named by the data contract is missing — see F-2.*

## Standard audit checklist

- [x] Required tests from the step page pass.
      — *Auditor: `./tools/verify.sh all --require` exit 0; focused PopulatedPackageFixtureTests (6) and
      RngVectorsTests (1) pass; fuzz/mutation runs clean.*
- [x] Public/protected XML documentation policy is followed.
      — *Auditor: `missing_docs` is denied and `cargo doc --workspace --no-deps` is clean; the .NET
      production-source change is XML-comment only, and the new .NET test code is covered by the suite.*
- [x] Documentation, status dashboard, feature log, and parity matrix are current.
      — *Auditor: dashboard/parity/step page agree; the parity rows are conservative (Rust solver, fuzzing,
      performance, Rust API all Partial). The `dry-run-v1.txt` claim is wrong — see F-2.*
- [x] Closeout CI is green at the audited commit.
      — *Auditor: GitHub run `44809489478`, `conclusion=success`, `head_sha=fe48dc8…`; .NET/Rust/contracts/API
      jobs success, Python skipped.*
- [x] Closeout evidence matches the audited commit and the merge candidate.
      — *Auditor: `audit_commit`, `closeout_ci.head_sha`, and `merge_candidate` are all `fe48dc8…`;
      `main_sha_at_freeze` `bd0d94e…` is an ancestor; `check_closeout_evidence.py` exits 0.*
- [x] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded.
      — *Auditor: at the freeze `main` was `bd0d94e` and an ancestor; `main` has since moved to `ec301a43`
      (three docs-only commits). The merge-tree rule is a post-audit obligation, not a finding against the
      step — see N-4.*
- [x] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code.
      — *Auditor: no P/Invoke, native library, RID package, path/case/locking, or GUI code; Ubuntu closeout and
      local gate are the evidence, recorded as an accepted risk in the closeout evidence.*
- [x] Format/version/fixture changes are documented and tested.
      — *Auditor: no manifest/schema/version change; the new real-writer populated fixture and the RNG vector
      document are documented; the `amend-1-net10` package is still consumed.*
- [x] Security, dependency, licence, and secret-scan gates pass.
      — *Auditor: NuGet licence/vulnerability, ApiCompat, `cargo-deny` advisories/bans/licenses/sources,
      SPDX headers (773 files), and no-secret scan (1,195 files) pass.*
- [x] Code structure policy passes; no new root-level or catch-all folders.
      — *Auditor: `./tools/verify.sh structure` exit 0; crate roots contain only `Cargo.toml` and
      `src`/`tests`/`benches`/`fuzz`; no `utils`/`helpers`/`common` module; files below 800 lines.*
- [x] No unrelated changes or unapproved scope.
      — *Auditor: the code diff is reader/RNG/CLI plus fixture and source-comment changes; the unmet fixture
      and container deviations are recorded — see F-1/F-6.*
- [x] Branch, tag, and status hygiene are correct.
      — *Auditor: step branch frozen at the audited commit; no `step-13` tag, no merge; the only CI runs are
      the three documented dispatches; `main` moving is the pending merge-tree obligation.*

## Evidence

**Independence.** I am a fresh context. I did not author, design, implement, review, or coordinate this work
and have no shared conversation with the planner, implementors, design reviewers, or orchestrator. I read the
repository at the frozen commit and ran the checks myself in an isolated worktree.

**Frozen state.**

- Worktree checked out at `e81e12e024861c5730473247248a53353f913aec` (the request head). `git diff
  --name-status fe48dc8..e81e12e` lists only `docs/planning/audits/step-13.md`,
  `docs/planning/closeout/step-13.json`, `docs/planning/status.md`, and the Step 13 page. No code, test,
  configuration, contract, or generated artifact changed after the audited commit.
- Audited commit `fe48dc850f7bbb7a7527abe8450e7937ebb0befe`; `main_sha_at_freeze`
  `bd0d94e667a0ed68280b637af93a852f7b502b3d` is an ancestor of it. `main` has since moved to
  `ec301a434e3379c245faf4381c1653834a303755` (three docs-only `[skip ci]` commits); this is a post-audit
  merge-tree obligation, recorded as N-4, not a finding against the step.
- Closeout CI run `44809489478` (`workflow_dispatch`, Ubuntu) has `conclusion=success` and
  `head_sha=fe48dc850f7bbb7a7527abe8450e7937ebb0befe`; the .NET, Rust, Contracts and policy, and Public API
  jobs succeeded and Python was skipped by its path filter. The failed first dispatch `44808204483` at the same
  SHA has the .NET job failed and every other job green; the step evidence and the recorded Step 11 precedent
  attribute it to the shared-runner performance test (`TenThousandPositionModelCompilesWithinTheBudget`,
  31.3088447 s vs < 30 s). The earlier green dispatch `44807199574` is at `45eb181`. The repository's run list
  for the branch contains exactly these three runs; there is no per-feature or per-push CI run.
- The failed job's public check-run annotation only reports "Process completed with exit code 1"; the exact
  failing test name and the 31.3 s duration cannot be read from the public API without authenticated logs, so
  the recorded test name and the owner's flake acceptance are treated as evidence rather than independently
  re-derived. The job/step conclusions and SHAs were verified.

**Commands and results (all re-run in this audit).**

```text
./tools/verify.sh all --require                          exit 0; "Closeout gate completed with required checks enabled."
  .NET: 1,792 tests passed, 0 failed (App 1, Contracts 37, Symmetry 223, Results 48, Core 685, Cli 200,
        Packages 501, Compilation 96, Architecture 1); build, test, format all clean
  Rust: cargo fmt --check, cargo clippy --workspace --all-targets -- -D warnings, cargo test --workspace,
        cargo build --release all clean; 121 mocassin-msl tests, 42 mocassin-rng tests, 27 mocassin-cli tests,
        32 mocassin-types tests, plus doctests
  Python: 3 passed; sdist and wheel built
  Documentation: math check OK; links OK (176 files); Zensical strict "No issues found"; DocFX 0 warnings /
        0 errors; cargo doc --workspace --no-deps clean
  Contracts: 26 artifacts current; model projection 5/5; parameterization 4/4; lattice 2/2; space-group catalog
        current (1f3bf67d…); process consistency OK; 58 tool tests; SPDX headers in 773 files; no secret
        patterns in 1,195 files; cargo-deny advisories/bans/licenses/sources OK
  AOT: zero-warning smoke host and IL2026/IL3050 positive control pass; the real Mocassin.Cli publish fails
        with 62 IL2xxx/IL3xxx errors, recorded as evidence, not a gate
./tools/verify.sh structure                              exit 0; Architecture.Tests 1 passed
cargo build -p mocassin-cli --release                    exit 0
mocassin-sim --db tests/contracts/packages/msl-v1/ceria-temperature-sweep.msl --job 1 --dry-run
                                                         exit 0; exact frozen summary, signatures
                                                         1845b70e…/92cf375e…, lattice 880b6eac…, 6x6x6
                                                         length=864, target_mcsp=150, flags=0x72,
                                                         seed_hash=7a92d942…
mocassin-sim --db tests/contracts/packages/amend-1-net10/ceria-temperature-sweep.msl --job 1 --dry-run
                                                         exit 0; same logical summary
mocassin-sim --db tests/contracts/packages/step-13-populated/step-13-populated.msl --job 1/2 --dry-run
                                                         exit 0; 3 particles, 3 positions, 6 pair tables,
                                                         1 cluster table, 2 collections, 4 directions, 8 rules,
                                                         KMC/MMC, 2x2x2 length=24, signatures
                                                         7a23241c…/7036aac8…/cc146e07…
--help / --version                                       0
missing arguments / unknown job / --stdout / --extension 10
corrupt model payload                                    11
wrong stored signature                                   12
deliberate debug panic (MOCASSIN_SIM_TEST_PANIC)         13
missing path                                             16
0444 package                                             exit 0; no -wal/-shm sidecar
--io with --dry-run                                      exit 0; no directory created
--seed 00…                                               exit 0; seed_hash 66687aad…
cargo bench -p mocassin-msl --bench load                 exit 0; open 188.83 us, cold 352.62 us,
                                                         warm 68.65 us
cargo bench -p mocassin-rng --bench draws                exit 0; next_u32 1.3617 ns (734 M/s), next_u64
                                                         2.2741 ns (440 M/s), next_f64 2.5508 ns
                                                         (392 M/s), bounded 1.764-1.778 ns (562-571 M/s),
                                                         refill 77.07 ns, state 121.25 ns, fork 120.18 ns;
                                                         every section 4.5 budget met
cargo +nightly fuzz run --target x86_64-unknown-linux-gnu payload_scan -- -max_total_time=5
                                                         exit 0; 151,171 runs, zero crashes, peak RSS 408 MiB
cargo +nightly fuzz run --target x86_64-unknown-linux-gnu payload_decode -- -max_total_time=5
                                                         exit 0; 139,332 runs, zero crashes, peak RSS 249 MiB
cargo +nightly fuzz run --target x86_64-unknown-linux-gnu package_open -- -max_total_time=5
                                                         exit 0; 5,076 runs, zero crashes, peak RSS 362 MiB
dotnet test … --filter FullyQualifiedName~PopulatedPackageFixtureTests
                                                         6 passed
dotnet test … --filter FullyQualifiedName~RngVectorsTests
                                                         1 passed
python tools/check_closeout_evidence.py docs/planning/closeout/step-13.json
                                                         exit 0; "Closeout evidence: OK"
git diff --check                                         clean
```

The fuzz default target is `x86_64-unknown-linux-musl`, which is not installed; the documented
`x86_64-unknown-linux-gnu` target was used, as the step page records.

**Files and artefacts reviewed.** `AGENTS.md` §3, §7, §8-§16; `docs/planning/audits/README.md` and template;
`docs/planning/reviews/step-13.md` (design review); the Step 13 page and the Step 11/14/16/19 pages; ADRs
0005/0010/0011/0013; `docs/planning/02-data-contracts.md`; `01-target-architecture.md` §7.3;
`03-quality-strategy.md` §9.11; `docs/planning/closeout/step-13.json` and `closeout/README.md`;
`docs/planning/status.md`; `appendix-b-parity-matrix.md`; `docs/development/solver-architecture.md`,
`testing-and-parity.md`, `code-structure.md`; `docs/reference/cli.md`, `msl-format.md`; all `src/rust` crate
sources and tests; the touched .NET contracts/compiler/package sources; the `msl-v1`, `amend-1-net10`, and
`step-13-populated` fixtures; `samples/step-13-populated`; `tests/reference/bench/`; `tools/verify.sh` and
`tools/check_closeout_evidence.py`; `contracts/manifest.json` (unchanged).

**Closeout evidence.** `python tools/check_closeout_evidence.py docs/planning/closeout/step-13.json` passes.
The recorded values are consistent at the audited commit: `audit_commit`, `closeout_ci.head_sha`, and
`merge_candidate` are `fe48dc8…`, and `main_sha_at_freeze` is an ancestor. `merge_tree_ci` is empty, which is
correct for the freeze; it must be populated or replaced by a re-audit after the later `main` move.

**Limits of this audit.** The remote CI logs are not readable without authentication; the public run and job
conclusions and SHAs were verified. The fuzz and benchmark numbers are machine-dependent; they were reproduced
on the same declared runner class and the results are recorded above. No repository file was modified except
this record.

## Known notes for the auditor

- **Fixture limitation (flag).** The real populated fixture does not carry the design's per-node mask
  difference or equivalent-start mapping. The writer rejects a mixed particle set on a transition node with
  `CMP004` ("Two rules in collection 0 share state0 ... with different outcomes") and an MMC mixed-set exchange
  with `CMP004`/`CMP005`; a P-1 variant compiles the KMC collection but fails the verifier with `PKG010`
  ("Environment 3 has a cluster link out of range"). No checked-in project combines a centered space group with
  transitions. The reader semantics are covered by the synthetic tests named on the step page and in the
  fixture READMEs; n-14 is not claimed as met. Please review the deviation and decide whether it is acceptable
  for Step 13 or needs a follow-up.
- `DirectionPool` and the jump-status container are Step 14 runtime state and were not added; the implementation
  task is marked partial.
- The 512 MiB model-element budget is verified by arithmetic and the `ReaderLimits`/scan tests; no 512 MiB
  fixture is materialized.
- Platform CI was not run; Step 13 adds no platform-specific code. The Ubuntu closeout run and the local
  `all --require` gate are the evidence.
- The AOT stage records the known Step 12 evidence: publishing the real `Mocassin.Cli` still emits 62
  IL2xxx/IL3xxx diagnostics from reflection-based JSON converters in `Mocassin.Core`; the strict smoke host and
  positive control pass, and the AOT gate is green.
- The two benchmark `sourceCommit` provenance fields are part of the audited tree at `fe48dc8`, so no file
  outside `docs/` changes after the closeout CI run. The earlier green run at `45eb181` (44807199574) and the
  failed first dispatch at `fe48dc8` (44808204483, the known shared-runner performance flake: 31.3088447 s
  against < 30 s for `TenThousandPositionModelCompilesWithinTheBudget`, owner-accepted with the Step 11
  precedent) are superseded by the replacement run 44809489478 at `fe48dc8`, which is the closeout gate.
- Feature 6 had left `proptest` in `mocassin-msl`'s production dependencies; feature 7 moved it to
  dev-dependencies.
- The populated package has one deduplicated lattice row (both jobs use the same 2x2x2 lattice) and four
  extracted payloads, not six; the fixture README records the shape.
- The `--stdout` option is deliberately not accepted in Step 13 (usage error 10); the run-dump option is
  Step 19 work. The fuzz build uses `x86_64-unknown-linux-gnu` because the musl target is not installed.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **F-1 — The real-writer populated fixture does not carry the reviewed n-14 properties, and the substitution was not recorded as a design amendment.** Design §7 and the Step 13 Test plan require the populated/MMC package to have a non-trivial per-node mask difference and at least one direction mapped into a symmetry-equivalent start bucket; design-review resolution n-14 repeats that Step 16 consumes the same fixture for its exact check. The real `step-13-populated` package (3 particles, 3 positions, 6 pair tables, 1 cluster table, 2 collections, 8 rules, both kinds) has neither property. The step page and both fixture READMEs record the limitation and cite `CMP004`/`CMP005`/`PKG010`; the reader semantics are covered by synthetic mutations of the real model DTO (`a_collection_union_admits_a_particle_only_one_direction_allows`, `a_particle_outside_the_union_is_rejected_even_when_mass_and_charge_hold`, `a_direction_may_be_mapped_into_another_start_bucket`). That mitigates the reader risk, but it does not satisfy the reviewed design/test commitment, and no Step 13 amendment or owner-approved design change exists. The P-1 `PKG010` failure is also not reproducible from a checked-in project or command and may be a compiler/verifier defect. This is Major, not Blocker, because the reader code is correct and the synthetic coverage is meaningful. | Step page §7, "Populated/MMC acceptance" Test row, and the "n-14 is not claimed as met" deviation; `docs/planning/reviews/step-13.md` n-14; `tests/contracts/packages/step-13-populated/README.md` "Intentional limits"; `samples/step-13-populated/README.md`; `src/rust/crates/mocassin-msl/src/map/tests/model.rs`; no committed P-1 variant or reproduction command; no amendment record. |
| **Minor** | **F-2 — The documented `dry-run-v1.txt` golden does not exist.** `02-data-contracts.md` says `tests/contracts/packages/msl-v1/` carries the Rust CLI's `dry-run-v1.txt` golden, and the Step 13 §7 fixture table lists it as a checked-in artifact consumed by the CLI test. The directory contains no such file; the CLI test asserts an inline exact golden (`tests/cli.rs::golden`). The frozen format is still tested, but the normative data-contract statement is false. | `ls tests/contracts/packages/msl-v1`; `02-data-contracts.md` test-fixture bullet; Step 13 §7 table; `src/rust/crates/mocassin-cli/tests/cli.rs:33-48`. |
| **Minor** | **F-3 — The `.bin` projection round-trip does not exercise the production encoder, and `signature-vectors.json` is not consumed by Rust.** `tests/projections.rs` has its own `parse`/`encode` implementation and never calls `mocassin_msl::projection`; the step page's claim that the `.bin` round-trip "proves the encoder" is therefore overstated. The production encoder is proven indirectly by the real-package signature comparisons, but the `.bin` vectors do not protect it. The design artifact table also names `tests/contracts/signature-vectors.json` as Rust-read; no Rust test reads it (the Rust projection test reads the `.bin` sidecars, and the .NET tests read the signature document). | `src/rust/crates/mocassin-msl/tests/projections.rs:38-108`; no `mocassin_msl::projection` import; `grep signature-vectors` finds .NET and documentation consumers but no Rust consumer; Step 13 §3.6/feature 6. |
| **Minor** | **F-4 — A job row with a missing model or lattice reference is classified as `JobNotFound` (exit 10), not a package error (11).** `JOB_ROW_SQL` inner-joins `models` and `lattices`; when the referenced row is absent the query returns no row and `read_job_row` maps that to `PackageError::JobNotFound`. Deleting `models.model_id = 1` (and separately `lattices.lattice_id = 1`) from copies of the real fixture produced exit 10 with "job 1 ... is not present in the package". A dangling reference is a corrupt package, not an invalid argument; the documented package-error class is 11. | `src/rust/crates/mocassin-msl/src/package.rs:83-92, 457-491`; mutated fixture copies; observed exit 10. |
| **Minor** | **F-5 — The 1M-rule/16M-link per-field caps and the raised-`MaxRuleCount` requirement are not exercised by a test.** The acceptance checkbox says the caps are per-field and a raised `MaxRuleCount` requires a raised `ReaderLimits`, and its footnote cites the `ReaderLimits`/scan tests. `limits.rs` asserts only the defaults and one override; the scan tests cover the element budget and depth, not `max_rules`, `max_pair_links`, `max_directions`, `max_collections`, `max_pair_tables`, or `max_cluster_tables`. The code enforces the caps, but the claimed test evidence is narrower than stated. The absence of a 512 MiB fixture is accepted because the design does not require one. | `src/rust/crates/mocassin-types/src/limits.rs` tests; `src/rust/crates/mocassin-msl/src/scan/model.rs` cap calls; `tests/hostile_payloads.rs` covers only `max_model_elements`/`max_messagepack_depth`; acceptance item 5. |
| **Minor** | **F-6 — `DirectionPool` and the `JumpStatus` container are not implemented, although design §6 and the step Scope list them as Step 13 data-oriented base structures.** The implementation task is correctly left partial and the page says they are Step 14 runtime state, but no amendment records the design change. There is no Step 13 consumer, so the functional risk is low; Step 14 must add them or the design text must be amended. | Step page §6, Scope, and the partial implementation task; no `DirectionPool`/`JumpStatus` in `src/rust/crates/mocassin-types/src/runtime`; feature-4 note. |
| **Minor** | **F-7 — The `--seed` override is not reflected in `JobPackage.seed`, contrary to design §2.3.** The CLI computes `effective_seed` only for the dry-run summary (`main.rs:103-107`); the `JobPackage` keeps the package seed, and the struct doc says the override is "applied by the CLI". Design §2.3 calls `JobPackage.seed` the "effective seed after an optional `--seed` override". The dry-run output is correct, but a Step 14 consumer of `JobPackage` would use the package seed unless the CLI threads the override separately. | `src/rust/crates/mocassin-types/src/runtime/job.rs` doc; `src/rust/crates/mocassin-cli/src/main.rs:103-107`; Step 13 §2.3; `the_seed_override_is_reported_by_hash` test. |
| **Minor** | **F-8 — Two §3.5 particle/provenance checks are missing.** The model provenance strings are compared with `schema_info` but never required non-empty, although §3.5 says "non-empty and match"; a payload with empty provenance and empty `schema_info` values was accepted (exit 0). `is_vacancy[0]` is also not required to be true; only `charges[0] == 0` is checked. Both are design-specified untrusted-input checks; the current fixtures happen to satisfy them. | `src/rust/crates/mocassin-msl/src/map/model.rs:28-39` and `map_particles`; an empty-provenance fixture copy was accepted by the release binary; `the_particle_table_is_checked` does not cover `is_vacancy[0]`. |
| **Note** | **N-1 — The AOT evidence should say the real CLI publish fails, not merely emits.** The gate is green and the smoke host/positive control pass, but `artifacts/aot/mocassin-cli.log` contains 62 diagnostics (31 `IL2026` + 31 `IL3050`) classified as `error`, and `verify.sh` reports "Mocassin.Cli publish failed with 62 ...". "Emits" in the step page and closeout note is literally true but can read as a successful publish. Step 31 owns the AOT decision. | `artifacts/aot/mocassin-cli.log`; `verify-all.log` AOT line; Step 13 feature 7; closeout note 19. |
| **Note** | **N-2 — Three declared dependencies are unused.** `mocassin-types` declares `serde_bytes`, `mocassin-msl` declares `tracing`, and `mocassin-cli` declares `thiserror`; no source file uses them. They are listed in the design, so this is hygiene, not scope creep; remove or use them when Step 14/19 adds the engine or logging. | `grep` over the three crates; their `Cargo.toml` files; `cargo-deny` is clean. |
| **Note** | **N-3 — The load baseline's `open` median is at the 10 % boundary on a fresh same-class run.** My Criterion run: `open` 188.83 us (recorded 171.09, +10.4 %), cold 352.62 us (343.08, +2.8 %), warm 68.65 us (71.075, -3.4 %). All are far below the 100 ms fixture budget; the 10 % rule is a release-candidate gate owned by Step 29. Re-baseline on a quiet runner before using this entry as a release gate. | `tests/reference/bench/baselines.json` `rustLoad`; `tests/reference/bench/step-13-load.json`; the audit's `cargo bench` run. |
| **Note** | **N-4 — `main` moved after the freeze; the merge-tree rule is pending.** `main` is now `ec301a43` (three docs-only `[skip ci]` commits after `bd0d94e`). This is not a finding against the audited tree; the step agent must merge the new `main`, rerun `all --require`, dispatch a replacement closeout CI, and either re-audit or record an equivalent merge-tree CI for the final candidate before merge. | `git log main`; closeout evidence note 20; `docs/planning/closeout/README.md` merge-tree rule. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** Re-audit required

The first remediation was squashed as `020e70ad4efcb06f79607cfeb7c75e370147cdd9`; the F-1 closure was
squashed as `ce00f8edd7012a513a20b8c85d94c9e639bd12c5` after the Step 08 fix `1f5b473`. F-2–F-8 and N-1–N-3 are
fixed. F-1 is closed by the real-writer `step-13-centered` fixture (cross-bucket equivalent starts and a
per-node mask difference); the earlier
[`step-13-amend-1`](../amendments/step-13-amend-1-fixture-properties.md) is **Withdrawn — resolved by
`1f5b473`** and retained for history. The re-audit can verify the real cross-bucket and mask assertions. N-4's
merge-tree obligation is satisfied by merging `main` `1f5b473` (`b8c7cca`) and the compile-budget smoke bound
`4973d02` (`64def37`) before the closeout run; the run covers the merged tree. The audited commit is
`31281210f64552ca3bb704512d028a1d28499650`; closeout CI run `44831177039` is green at that commit, and the
following evidence/status commit is docs-only.

`Fixes required` is the round-1 verdict; `Re-audit required` is the current record state, not a verdict.

## Resolution status

| Finding | Status | Resolution | Commit |
| --- | --- | --- | --- |
| F-1 | **Fixed by the Step 08 fix and the `step-13-centered` fixture** | The populated fixture carries the per-node mask difference. The new real-writer `tests/contracts/packages/step-13-centered/` package, derived from the Step 08 C-centered sample after the compiler fix `1f5b473`, carries both the translation-equivalent cross-bucket `jump_mapping` and a per-node mask difference. The Rust `centered_fixture` tests and .NET `CenteredPackageFixtureTests` assert both properties and rebuild reproducibility. The earlier `step-13-amend-1` is **Withdrawn — resolved by `1f5b473`**, retained for history. The P-1 `PKG010` claim remains non-reproducible; the Step 11 verifier `byte_position` observation is a separate deferred maintenance item. | `ce00f8edd7012a513a20b8c85d94c9e639bd12c5` |
| F-2 | Fixed | `tests/contracts/packages/msl-v1/dry-run-v1.txt` is the checked-in golden; the CLI test reads it with `{package}`/`{version}` placeholders. | `020e70ad4efcb06f79607cfeb7c75e370147cdd9` |
| F-3 | Fixed | `tests/projections.rs` builds the runtime model/lattice and calls the production `mocassin_msl::projection` encoder; it also reads `tests/contracts/signature-vectors.json` and checks id/signature/digest fields. | `020e70ad4efcb06f79607cfeb7c75e370147cdd9` |
| F-4 | Fixed | `PackageError::DanglingReference` (package class, exit 11) for a job row whose model/lattice row is absent; mutated real-fixture tests cover both. | `020e70ad4efcb06f79607cfeb7c75e370147cdd9` |
| F-5 | Fixed | `every_per_field_count_cap_is_enforced` exercises rules, directions, collections, pair/cluster tables, and both link caps with lowered limits and accepts the same payload with raised limits. | `020e70ad4efcb06f79607cfeb7c75e370147cdd9` |
| F-6 | Fixed | `DirectionPool`, `JumpStatus`, and `JumpLink` are type-only containers with invariant tests; Step 14 populates them. | `020e70ad4efcb06f79607cfeb7c75e370147cdd9` |
| F-7 | Fixed | `Package::load_job_with_seed` writes the override into `JobPackage.seed`; the CLI uses it and `the_seed_override_is_the_effective_job_seed` covers the field. | `020e70ad4efcb06f79607cfeb7c75e370147cdd9` |
| F-8 | **Fixed as the inverse vacancy check** | Non-empty provenance is enforced. The implemented check is `is_vacancy[0] == false` (index 0 is void and is not an authored vacancy); a `true` flag is rejected. The Step 08 writer writes `false`, and both checked-in model payloads carry `false`, so the original `== true` requirement would reject every real package. The step page records the implemented inverse check. | `ce00f8edd7012a513a20b8c85d94c9e639bd12c5` |
| N-1 | Fixed | The step page, closeout evidence, and parity/evidence wording say the real `Mocassin.Cli` AOT publish **fails** with 62 IL2xxx/IL3xxx diagnostics; the strict smoke host and positive control remain green. | `020e70ad4efcb06f79607cfeb7c75e370147cdd9` |
| N-2 | Fixed | Removed the unused `serde_bytes` (`mocassin-types`), `tracing` (`mocassin-msl`), and `thiserror` (`mocassin-cli`) declarations; `tracing`/`thiserror` stay only where used. | `020e70ad4efcb06f79607cfeb7c75e370147cdd9` |
| N-3 | Fixed / flagged | A same-class load rerun (open 271.0 µs / cold 351.4 µs / warm 68.0 µs) is recorded; the baseline is flagged boundary-calibrated for Step 29. The 10 % rule is not relaxed. | `020e70ad4efcb06f79607cfeb7c75e370147cdd9` |
| N-4 | Fixed | `main` `1f5b473` (Step 08 equivalent-start fix) and `4973d02` (shared-runner smoke bound) are merged before the closeout run; the run covers the merged tree. | `64def37` (main `4973d02` merge; `b8c7cca` merged `1f5b473`) |

The resolution table is documentation-only after the audited code commit; the re-audit should verify the
code/tests and the amendment decision.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [x] Fixes required
- [ ] Waived

Follow-up owner: Step agent and planner — resolve F-1 (build the real-writer fixture or record a Step 13
amendment that accepts synthetic coverage and re-plans the Step 16 exact-check fixture; investigate
`CMP004`/`CMP005`/`PKG010` as a possible compiler/verifier defect) and fix F-2–F-8. Then apply the merge-tree
rule for `main` `ec301a43`, record the new evidence, and request a re-audit.
Follow-up issue/step: Step 13 re-audit; Step 16 fixture/exact-check follow-up.

## Approval summary

The step agent completes this section before a merge, tag, or Blocker/Major waiver request. It must cover the
full history, not only the latest round.

- [ ] Requested decision and frozen commit recorded
- [ ] Every review/audit round recorded (round, date, commit, reviewer, verdict)
- [ ] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status)
- [ ] Accepted notes and waivers recorded with rationale
- [ ] Residual risks, deferred items, and follow-up owners recorded
- [ ] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived
- [ ] Final CI and documentation-build results recorded

Round history:

| Round | Date | Commit | Reviewer | Verdict |
| --- | --- | --- | --- | --- |
| — | — | — | — | — |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Human approval

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:

## Audit rounds

Append each new round below. Never overwrite an earlier round.

### Audit round 1

#### Independence and frozen state

I am a fresh context. I did not author, design, implement, review, or coordinate this work and have no shared
conversation with the planner, implementors, design reviewers, or orchestrator. The checks were run in an
isolated worktree of the step branch; the record is committed on the branch checkout at the request head
`e81e12e`. The audited commit is `fe48dc8`; `git diff --name-status fe48dc8..e81e12e` contains only the audit
record, the closeout evidence, the status dashboard, and the Step 13 page. The closeout CI run `44809489478`
is green at the audited commit. `main` was `bd0d94e` at the freeze and has since moved to `ec301a43`; the
merge-tree rule is the pending post-audit obligation (N-4), not a finding against the audited tree.

#### Commands and results

`./tools/verify.sh all --require` exit 0 (1,792 .NET tests; Rust fmt/clippy/test clean; Python 3 passed;
strict docs, DocFX, rustdoc, contracts, SPDX/secret scan, cargo-deny, AOT smoke and positive control all
green; the real `Mocassin.Cli` AOT publish fails with 62 IL2xxx/IL3xxx errors and is correctly non-gating).
`./tools/verify.sh structure` exit 0. The real `msl-v1`, `amend-1-net10`, and populated packages all open
read-only and validate; the dry-run output matches the frozen golden and the canonical summary; the exit-code
cases 0/10/11/12/13/16 were observed; the `0444` read-only case has no sidecars and the dry run writes nothing.
The load bench met `< 100 ms` (open 188.83 us, cold 352.62 us, warm 68.65 us); the RNG bench met every §4.5
budget; the 5 s fuzz run per target found zero crashes; the focused .NET populated-fixture (6) and RNG-vector
(1) tests pass; `check_closeout_evidence.py` passes; `git diff --check` is clean. Full command list and
numbers are in `Evidence` above.

#### Findings

F-1 (Major): the real-writer populated fixture lacks the n-14 per-node mask difference and equivalent-start
mapping, and the synthetic substitution was not recorded as an amendment; the shared Step 16 fixture plan is
therefore unfulfilled. F-2–F-8 (Minor): missing `dry-run-v1.txt` golden; the `.bin` projection test does not
exercise the production encoder and `signature-vectors.json` is not Rust-consumed; a dangling model/lattice
reference maps to exit 10 instead of 11; the 1M-rule/16M-link cap tests are narrower than claimed; the
`DirectionPool`/`JumpStatus` containers are deferred without an amendment; `--seed` is not reflected in
`JobPackage.seed`; provenance non-empty and `is_vacancy[0]` are not checked. N-1–N-4 (Notes): AOT publish
wording, unused dependencies, the load `open` median at the 10 % boundary, and the pending merge-tree
obligation.

#### Verdict

**Fixes required.** The reader/RNG/CLI implementation is strong and the acceptance checks mostly pass, but
F-1 is an explicit reviewed design/test commitment that is not met and was not amended, and several narrow
correctness/documentation gaps remain. No Blocker was found.
