# Step 11 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-11.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 11 — Simulation Package and `.msl` Writer |
| Request owner | Step agent (implementor) |
| Orchestrator | — |
| Step agent (implementor) | Step agent (implementor) |
| Audit initiated by | Orchestrator, on request of the step agent |
| Step branch | `step/11-simulation-package-and-msl-writer` |
| Closeout evidence | `docs/planning/closeout/step-11.json` |
| Audit date | — |
| Auditor | — |

The closeout evidence file holds the audited commit, closeout CI run and `head_sha`, `main` SHA at freeze, merge candidate,
merge-tree CI, platform CI, and tag. Do not restate them here: one source, one place.

Step page: [`Step 11 — Simulation Package and `.msl` Writer`](../steps/step-11-simulation-package-and-msl-writer.md)

> Orchestration: the orchestrator spawns a fresh auditor when the step agent has frozen the step branch and
> completed the audit request, and relays the verdict and owner decisions. The step agent applies fixes on a new
> feature branch, reruns closeout CI, adds resolutions without altering reviewer text, and asks the orchestrator
> for re-audit until the auditor no longer identifies issues. The orchestrator does not fix findings or edit the
> audit record. The human owner approves the final verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 08, 09, 10 |
| Delivers | A versioned SQLite simulation package containing one parameterized model, content-addressed initial lattices, and streamed job payloads, with a verified publication barrier, contract fixtures, and a read-only inspection/verification library for the build CLI. |

The step includes one integrated amendment:
[`step-11-amend-1`](../amendments/step-11-amend-1-dotnet-11-native-zstd.md) — .NET 11 toolchain and native
Zstandard — integrated as `00753e7` after a `Ready with notes` design review
([record](../reviews/step-11-amend-1.md)). The parent design review is
[`step-11`](../reviews/step-11.md) (`Ready with notes`, accepted 2026-11-15).

## Step-specific acceptance checklist

Copy the acceptance criteria from the step page, preserving their wording:

- [x] A package built from `samples/jobs/ceria-temperature-sweep.mocprj` (six jobs, `parameterizations: []`)
      uses the implicit default parameterization, opens read-only, and contains one model row, the expected
      job rows, and the expected lattice rows.
- [ ] A package built with an explicit in-test parameterization stores the applied pair/group/site/frequency
      values, not compiled defaults, and `models.signature` equals `ParameterizedModel.Signature`.
- [x] The published package has no `results` table, needs no `-wal`/`-shm` files, and reports
      `application_id = 0x4D4F4353` and `user_version = 1`.
- [x] Every job references a valid model and lattice; `job_id` is unique and ascending; a partial build leaves
      gaps exactly as recorded in `job_selection`.
- [x] `verify` re-encodes the parameterization signature from the payload's absolute `frequency_hz` values and
      re-encodes the lattice signature from the particle array and the referenced `model_signature`.
- [x] Every environment pair entry is one row per periodic image with its relative `offset`; the compiler emits
      one `CompiledPairLink` per image and the writer maps it one-to-one, Step 14's target expansion
      (`cell + offset` wrapped, `target_position`) reproduces the compiled minimum-image link for every pair
      table, and a multi-image shell (for example a six-image nearest-neighbour shell) is fully present.
- [ ] The writer asserts `tracker_order.Length` equals the representative direction's path-node count; the
      verifier proves the payload-checkable part (non-empty, non-sentinel values below `global_tracker_count`,
      length within the documented bound), and Step 13's reader loads it as a variable `u8[]` with `0xFF` for
      non-mobile nodes; Step 18's mobile-tracker permutation follows the same indices.
- [x] `PKG001`–`PKG010` are documented with severities and anchors, `PKG` descriptors are composed into
      `DiagnosticCatalog.Default`, and `Mocassin.Results` still references only `Core` and `Contracts`.
- [x] The checked-in `.msl` fixture's `package-summary-v1` golden matches a rebuild after normalizing
      `created_utc`, `app_version`, `generator`, size, and payload byte hashes (`PackageFixtureTests`).
- [x] Two jobs whose lattices are byte-identical share one `lattices` row; a different particle array does not.
- [x] Rebuilding the same project produces identical model/lattice signatures and an identical canonical
      summary; stored bytes may differ only in build metadata (`PackageFixtureTests.RebuildReproducesTheFixtureSummary`).
- [x] The job `flags` word equals `SolverJobFlagsMapper.Compose(...)` and contains exactly one kind bit.
- [x] The job `electric_field_direction` is a unit vector equal to the compiled cell transform of the authored
      fractional direction (tested on a non-orthogonal cell).
- [x] `extensions[].config_json` is byte-equal to the project canonical JSON for the same config, including a
      number token above 2^53.
- [x] A cancelled build leaves no temp file and does not modify an existing package.
- [x] A simulated disk-full/write error leaves the previous package intact.
- [x] `verify` detects a truncated payload, a corrupt Zstd frame, a wrong signature, a missing FK, a wrong
      payload-derived column, a stray `-wal`, and an injected `results` table, each with the expected `PKG` code.
- [x] 10⁴ small jobs build within the time/size budget (amended: 3.87 s, 10.05 MB, 118 MB peak RSS, 5,853 jobs/s; net10 reference: 4.03 s, equal size, 119 MB, 5,230 jobs/s).
- [x] No EF Core, no native structs, no custom binary header, and no results table.
- [x] Documentation written

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

```

Evidence file: `docs/planning/closeout/step-11.json`

## Known notes for the auditor

- **Two unchecked acceptance criteria are deliberate.** The explicit in-test parameterization package is a
  Step 12 follow-up (the sample fixture uses the implicit default parameterization; the write-time re-encode is
  covered by model-payload tests against real `ParameterizationValues`). The Step 13/18 half of the
  `tracker_order` criterion needs the Rust reader and the mobile-tracker permutation, which do not exist yet;
  the writer assertion and the verifier's payload-checkable part are implemented and tested.
- **Amendment 1 is integrated** as `00753e7`: `global.json` and all projects on .NET 11, the in-box
  `System.IO.Compression` Zstandard codec replacing `ZstdSharp.Port`, and the pre-amendment net10 `.msl`
  fixture and codec goldens checked in. The amendment review record is linked above. The owner authorized the
  factual `.NET 11` wording change in `AGENTS.md` §1/§8 as part of that amendment.
- **Container image not built — accepted risk (owner, 2026-11-15).** There is no OCI/podman builder in the agent
  environment, so `docs/opencode/mocassin.containerfile` is not build-verified. It pins SDK 11.0.100 only and
  asserts the `global.json` pin. Closeout CI resolves the same pin through `actions/setup-dotnet`; the local
  gates ran on the pinned SDK under `/tmp/opencode/dotnet11`. This is recorded in the closeout evidence notes.
- **Part A small-payload gate refinement (owner-approved).** Below 8 KiB the codec comparison uses an absolute
  decode penalty of ≤ 0.15 µs, throughput ≥ 100 MB/s, and ≤ +10 % relative decode at or above 8 KiB. Part A
  passed 240/240 checks with 24/24 byte-identical frames; the rule and results are in amendment 1 §9.1.
- **Platform gate.** The step adds the solution's first platform-specific native code (Unix directory `fsync`
  interop; Windows skips the directory call and relies on file fsync plus atomic replace). The focused Windows
  platform run is green at the audited commit; it built the solution and ran `Mocassin.Packages.Tests` and
  `Mocassin.Results.Tests`. No macOS run is required because the Unix path is exercised on Ubuntu.
- **Closeout CI scope.** The Ubuntu closeout dispatch ran the path-filtered jobs for the changed paths; Rust and
  Python were skipped because this step changes neither. The full local `all --require` gate covers them and
  passed on the same tree.
- **Main-CI flake re-run.** The main CI run for the platform-gate merge `bca6637` failed attempt 1 only at
  `CompilationPerformanceTests.TenThousandPositionModelCompilesWithinTheBudget` (30.134 s against the documented
  `< 30 s` budget) on the shared runner. The owner accepted the miss as a flake and authorized a re-run; attempt
  2 was green. No test, threshold, or workflow file changed.
- **Merge-candidate bookkeeping.** The closeout evidence and this request are a documentation-only commit on top
  of the audited commit. A commit cannot contain its own SHA, so `merge_candidate` in the evidence records the
  audited commit; verify that the later delta changes only documentation.
- **Cleanup.** `fix/ci-platform-gate` and the superseded `docs/dotnet-11-container` branch were deleted locally
  and remotely, and the `mocassin-dotnet11` worktree was removed. No `step-11` tag and no step-to-main merge
  exist at the freeze.

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
