# Step 12 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-12.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 12 — Build Orchestration and `mocassin` CLI |
| Request owner | Step agent (implementor) |
| Orchestrator | — |
| Step agent (implementor) | Step agent (implementor) |
| Audit initiated by | Orchestrator, on request of the step agent |
| Step branch | `step/12-build-orchestration-and-cli` |
| Closeout evidence | `docs/planning/closeout/step-12.json` |
| Audit date | 2026-11-18 |
| Auditor | — |

The closeout evidence file holds the audited commit, closeout CI run and `head_sha`, `main` SHA at freeze, merge candidate,
merge-tree CI, platform CI, and tag. Do not restate them here: one source, one place.

Step page: [`Step 12 — Build Orchestration and mocassin CLI`](../steps/step-12-build-orchestration-and-cli.md)

> Orchestration: the orchestrator spawns a fresh auditor when the step agent has frozen the step branch and
> completed the audit request, and relays the verdict and owner decisions. The step agent applies fixes on a new
> feature branch, reruns closeout CI, adds resolutions without altering reviewer text, and asks the orchestrator
> for re-audit until the auditor no longer identifies issues. The orchestrator does not fix findings or edit the
> audit record. The human owner approves the final verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 10, 11 |
| Delivers | A headless `mocassin` CLI and the shared `Mocassin.Packages.Build` orchestration library: recipe/job/parameterization selection, job expansion and normalization, partial selections with stable full-expansion IDs, preview, progress and cancellation, atomic package publication, post-publication verification, and the `new`, `validate`, `compile`, `build`, `inspect`, `verify`, `doctor`, and `clean --dry-run` command surface with seven manifest-owned reports. |

The step page records deviations 1–16. The design review is
[`step-12`](../reviews/step-12.md) (`Ready with notes`, accepted by the human owner 2026-11-17 at `1b6af90`).

## Step-specific acceptance checklist

Copy the acceptance criteria from the step page, preserving their wording:

- [ ] `mocassin validate samples/ceria/ceria.mocprj` exits 0 with the expected warnings; the human table and
      `validation-v1` report are unchanged from Step 07.
- [ ] `mocassin build samples/jobs/ceria-temperature-sweep.mocprj -o /tmp/ceria.msl` produces a package that
      `mocassin verify` accepts, using the implicit default parameterization (`parameterizations: []`), and
      writes `job_selection = "all"`. The ceria model-definition sample has no job template and is not a
      build sample.
- [ ] `--jobs 1-3` writes exactly jobs 1–3 with full-expansion IDs and `job_selection = "1-3"`; `--jobs 2,5`
      writes exactly those IDs with a gap and the same IDs in a full build.
- [ ] `--jobs` normalizes equivalently (`1,2,3` = `1-3`; a full selection = `all`) and rejects malformed,
      zero, descending, and out-of-range selections with exit 2.
- [ ] Build/job-template/parameterization selection auto-selects a single recipe, lists IDs on ambiguity,
      honours explicit overrides, and uses the implicit default when no template exists.
- [ ] `--force` atomically replaces an existing package; without it, an existing target exits 3 and the
      original file is byte-identical.
- [ ] Cancelling before publication leaves no temp file and preserves an existing package; cancelling after
      publication leaves the package and exits 6.
- [ ] `--json` prints exactly one schema-valid report with `exitCode` for `validate` and the seven
      report-backed commands, including usage errors; help/version and reserved commands emit no report;
      `--json` leaves stderr JSONL-only; JSON-Lines events validate against the `events` contract; `--quiet`
      additionally suppresses stage/progress/log events.
- [ ] `mocassin --help`, `mocassin <command> --help`, and `mocassin --version` exit 0; `mocassin --version`
      prints the assembly informational version and the runtime description from the CLI action, and
      `mocassin <command> --version` is `CLI001`/2; a parse error exits
      2 with `CLI001` and never exposes the library's default 1; `--jobs -1`, `--parallel -1`, and
      `-o -file` reach the handlers as values; `--` ends option parsing.
- [ ] `@file` response files follow §5.3: quoting/whitespace and `#` comment rules, recursive references,
      cwd-relative paths, `@` literal and no escape, `@file` before `--` expanded and after `--` literal,
      `--input=@file` literal, `--input @file` token-expanded before arity, `--json` inside a file honored,
      and the §5.3 limits (1 MiB/4 MiB raw bytes, depth 8, 10,000 tokens contributed by response files,
      32 files) enforced. Missing, unreadable, invalid, or
      oversized files exit 2 with `CLI001`, not `CLI002`/3. A `--json` on the command line produces the
      command report for an expansion failure only when a command is recognised outside the failed file
      (`mocassin <command> @missing --json`); `mocassin @missing --json` has no recognised command and emits
      no report.
- [ ] `./tools/verify.sh aot` passes: the smoke host publishes for `linux-x64` with `PublishAot=true` and
      `TrimMode=full` with zero IL2xxx/IL3xxx warnings, the separate positive control emits IL2026 and IL3050,
      and the native binary runs `--help`, `--version`, and one parse-error case that exits 2 through the
      host's `CLI001` remap. Missing clang/lld/ILCompiler is a hard failure with a clear message, never a
      skip. Feature 7 runs `./tools/verify.sh all --require` at Step 12 closeout and records the output; the
      real `Mocassin.Cli` publish result is recorded as evidence; dependency-owned warnings are attributed to
      Step 31. The closeout GitHub workflow does not run `verify.sh`, so this is a local gate.
- [ ] `build --preview` writes no package, reports `mode = "preview"`, projects `BuildResult.Preview`, and
      exits with the build table. The same payload is available to a library caller (Step 26) without a
      reload, recompile, or CLI reference.
- [ ] `clean --json` without `--dry-run` emits a `clean-v1` report with `dryRun = false`, `removedCount = 0`,
      and `CLI008`, and deletes nothing.
- [ ] `new --template blank` and `new --template ceria` create valid projects with fresh identity and seed;
      `compile -o` writes a `compiled-inspection-v1` document; `inspect`/`verify` read a real package.
- [ ] `doctor` exits 0 on CI; a missing solver is a warning. `clean --dry-run` lists only and never deletes.
- [ ] `--parallel` 1 and 4 produce the same signatures and `package-summary-v1`; output order is ascending
      `JobId`.
- [ ] The CLI builds and verifies 10⁴ jobs without the GUI within the Step 11 budget (< 10 min, < 2 GB).
- [ ] Documentation written (§18.1).

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
The auditor records the exact commands, results, and files behind the verdict here.
```

Evidence file: `docs/planning/closeout/step-12.json`

## Known notes for the auditor

- **Scope.** Seven features are squash-merged into `step/12-build-orchestration-and-cli`; the feature log on the
  step page records each branch, squash commit, local gates, and the absence of per-feature CI. The step adds
  the shared build pipeline, the command graph, seven report schemas, and the auxiliary commands; it does not
  add the solver, results collection, evaluation, GUI build UI, or full `clean`.
- **10⁴-job evidence.** The workload is a synthetic project derived from
  `samples/jobs/ceria-temperature-sweep.mocprj` with 20 configurations × `500` jobs = 10,000 jobs (four
  configurations × 2,500 would exceed `JOB008`, which caps a configuration at 1,000 jobs). It built and
  verified in 3.76 s wall time with 151,820 KB peak RSS and a 9,625,600-byte package; standalone `verify` was
  valid in 1.21 s. The step page has the exact command, the performance page has the method and stage timings.
- **Warnings-as-errors gap closed.** `tests/contracts/project-parameterization-warning.mocprj` is a clean
  buildable ceria variant with no validation or build-stage warnings and a deliberately unset explicit
  parameterization. `BuildParameterizationWarningTests` pins warning-free validation, exit 1 for validation
  warnings on the sample, exit 4 with `PRM003`/`PRM006` for the fixture under `--warnings-as-errors`, and a
  successful unflagged build and verify of the same fixture. The earlier scripted-pipeline mapping test remains.
- **`inspect` tolerance is intended.** `inspect` reports what it can read; corrupt payloads and signature
  mismatches are `verify` findings, and only fatal structure failures are `CLI009`/1. The CLI reference and
  data contracts state this reconciliation.
- **Doctor and clean deviations.** `doctor` treats free space below 100 MiB as `warn`, and a throwing check as
  a recorded `fail`; `clean` without `--dry-run` still performs the read-only listing and adds `CLI008`, and
  the reserved `--results` emits a minimal report. Both delete nothing. Deviations 14–16 record the reasons.
- **Report contracts.** `verify-v1` has one `diagnostics[]` array (the shared envelope) and a `checks[]` list;
  `compile-v1.stages[]` names the ten compiler stages including `report`; `doctor-v1` names the seven checks.
  The data contracts and CLI reference were reconciled to the implemented code in feature 7.
- **AOT and platform gates.** `aot` is part of `all --require`; the required smoke host publishes with zero
  trim warnings, the positive control emits IL2026/IL3050, and the native parse error exits 2. The real
  `Mocassin.Cli` publish emits 62 IL2026/IL3050 diagnostics from `Mocassin.Core` and is recorded as evidence,
  not a gate; Step 31 owns the AOT/single-file decision. No Windows/macOS run is planned for this step because
  it adds no new platform-specific code beyond the Step 11 native fsync path; platform CI is `not run`.
- **Closeout CI policy.** The closeout dispatch is the only step-branch CI run. The GitHub workflow does not run
  `tools/verify.sh`; the local `all --require` output is the gate evidence, and the closeout CI confirms the
  same tree through the repository's CI jobs. Rust and Python jobs may be skipped by their path filters because
  this step changes neither.
- **Docs-only closeout delta.** The closeout evidence, this audit request, the feature log, and the dashboard
  status are documentation-only commits after the audited code commit; they contain no code, test, contract,
  or configuration change. The closeout evidence binds the audited commit, the merge candidate, and the `main`
  SHA at freeze.

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
