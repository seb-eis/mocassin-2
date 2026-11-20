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
| Audit date | — |
| Auditor | — |

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

- [ ] `mocassin-sim --db tests/contracts/packages/msl-v1/ceria-temperature-sweep.msl --job 1 --dry-run` exits 0
      and prints the documented summary; every count/signature equals the canonical summary for job 1.
- [ ] The `amend-1-net10` package also opens and validates; the populated pair/cluster + MMC package validates
      and both signatures re-encode.
- [ ] Each rule preserves the non-void particle-symbol multiset and charge within `1e-9`; `path_vector` matches
      the cell-vector sum of `movement` and `length` matches its norm; a no-charge-change rule has a zero
      transport vector. Void is exempt from the occupation check; every non-void state particle is accepted by
      the collection-union mask and all four `msl-v1` rules pass; exact per-direction occupation is recorded as
      a Step 16 assignment. The full positional transport formula is recorded as a Step 15 assignment with the
      trust-boundary rationale.
- [ ] A symmetry-equivalent-start direction mapped into another start bucket is accepted; the reader does not
      require `direction.position_id == position`.
- [ ] A 512 MiB writer-valid payload is within the 512M model-element budget; the 1M-rule/16M-link caps are
      per-field, and a raised `MaxRuleCount` requires a raised `ReaderLimits`. *The budget is verified by the
      documented arithmetic and the `ReaderLimits`/scan tests; no 512 MiB fixture is materialized.*
- [ ] `models.format_version`/`lattices.format_version` columns match their payloads; the job settings are not
      compared with `simulations[].default_settings`.
- [ ] The solver exit-code rows in `cli.md`, `solver-architecture.md`, and this page match architecture §7.3
      code-for-code, including reserved 5 (batch partial success) and 13/14/15.
- [ ] Future package/payload versions and corrupt, truncated, oversized, or bomb payloads are rejected with the
      documented error/exit class, before unbounded allocation.
- [ ] A changed frequency, energy, site background, or lattice particle is detected by the parameterization or
      lattice signature check; a valid package reproduces both signatures from the payload.
- [ ] Every index/length/finite/kind/tracker invariant in §3.5 is validated; malformed input never panics.
- [ ] Seed derivation matches the Step 10 vectors; RNG golden vectors, bounded draws, `[0,1)` doubles,
      domain-separated forks, and the 64-byte state round-trip/resume all pass.
- [ ] The RNG draw/refill benchmark meets §4.5 and the numbers are recorded with the environment in the
      same-runner-class `tests/reference/bench/baselines.json` entry.
- [ ] Loading the ceria job takes `< 100 ms` (release, warm, benchmark laptop class) and the measurement is
      recorded as a fixture budget, not a large-package promise.
- [ ] The reader opens `0444` packages read-only, needs no `-wal`/`-shm`, and has no write API.
- [ ] The time-boxed fuzz run finds no panic/OOM; the PR-fast mutation/property suite passes.
- [ ] `cargo fmt --check`, `cargo clippy --workspace --all-targets -- -D warnings`, and `cargo test --workspace`
      pass; `missing_docs` is clean.
- [ ] Documentation written and the strict docs build passes.

## Standard audit checklist

- [ ] Required tests from the step page pass.
- [ ] Public/protected XML documentation policy is followed.
- [ ] Documentation, status dashboard, feature log, and parity matrix are current.
- [ ] Closeout CI is green at the audited commit.
- [ ] Closeout evidence matches the audited commit and the merge candidate.
- [ ] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded.
- [ ] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code.
- [ ] Format/version/fixture changes are documented and tested.
- [ ] Security, dependency, licence, and secret-scan gates pass.
- [ ] Code structure policy passes; no new root-level or catch-all folders.
- [ ] No unrelated changes or unapproved scope.
- [ ] Branch, tag, and status hygiene are correct.

## Evidence

Commands run:

```text
./tools/verify.sh all --require                          exit 0
cargo fmt --manifest-path src/rust/Cargo.toml --all --check
cargo clippy --manifest-path src/rust/Cargo.toml --workspace --all-targets -- -D warnings
cargo test --manifest-path src/rust/Cargo.toml --workspace
cargo build --manifest-path src/rust/Cargo.toml --workspace --release
cargo bench --manifest-path src/rust/Cargo.toml -p mocassin-rng --bench draws
cargo bench --manifest-path src/rust/Cargo.toml -p mocassin-msl --bench load
cargo +nightly fuzz build --fuzz-dir src/rust/crates/mocassin-msl/fuzz --target x86_64-unknown-linux-gnu
cargo +nightly fuzz run ... payload_scan|payload_decode|package_open -- -max_total_time=5
./tools/verify.sh structure
./tools/verify.sh docs --require
./tools/verify.sh contracts
dotnet build Mocassin.slnx -c Release
dotnet format Mocassin.slnx --verify-no-changes
dotnet test src/dotnet/Mocassin.Results.Tests/... --filter "FullyQualifiedName~PopulatedPackageFixtureTests"
dotnet test src/dotnet/Mocassin.Packages.Tests/... --filter "FullyQualifiedName~RngVectorsTests"
python tools/check_closeout_evidence.py docs/planning/closeout/step-13.json
```

Closeout CI: the run in the closeout evidence file (single Ubuntu `workflow_dispatch`).

Evidence file: `docs/planning/closeout/step-13.json`

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
- The benchmark `sourceCommit` provenance fields were completed in the docs/evidence commit after the closeout
  CI run because a commit cannot contain its own SHA; the measured tree is the audited commit's tree. No code,
  test, contract, or configuration file changed after the CI run.
- Feature 6 had left `proptest` in `mocassin-msl`'s production dependencies; feature 7 moved it to
  dev-dependencies.
- The populated package has one deduplicated lattice row (both jobs use the same 2x2x2 lattice) and four
  extracted payloads, not six; the fixture README records the shape.
- The `--stdout` option is deliberately not accepted in Step 13 (usage error 10); the run-dump option is
  Step 19 work. The fuzz build uses `x86_64-unknown-linux-gnu` because the musl target is not installed.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** Ready for audit

`Re-audit required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Waived

Follow-up owner:
Follow-up issue/step:

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

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

#### Commands and results

#### Findings

#### Verdict
