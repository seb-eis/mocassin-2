# Step 07 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-07.md`. You may edit only your own metadata (`Auditor`, `Audit date`,
> `Record state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 07 — Validation, Diagnostics, and Model Statistics |
| Request owner | Step agent |
| Orchestrator (step agent) | Step 07 implementor |
| Audit initiated by | Human owner |
| Audited commit | `ccd0e55d56cdffbb48c735a1b2829a990271fb23` (re-audit after F1–F3 fixes) |
| Step branch | `step/07-validation-and-diagnostics` |
| Step tag | — |
| Closeout CI run | [`43291748042`](https://github.com/seb-eis/mocassin-2/actions/runs/43291748042) — green at `ccd0e55d56cdffbb48c735a1b2829a990271fb23` |
| Closeout CI head SHA | `ccd0e55d56cdffbb48c735a1b2829a990271fb23` |
| Main SHA at freeze | `5b1eb7496f856660f90f4bb358bd3cbd1d95165d` |
| Merge-tree CI | Not required; `main` did not move after branch creation |
| Closeout evidence | `docs/planning/closeout/step-07.json` |
| Platform CI | Not run (Ubuntu-only policy; no platform-specific code) |
| Audit date | 2026-11-12 |
| Auditor | Fresh independent audit agent (OpenCode subagent; no shared conversation with the planner or implementor) |

Step page: [`Step 07 — Validation, Diagnostics, and Model Statistics`](../steps/step-07-validation-and-diagnostics.md)

> Orchestration: the step agent (the orchestrator) freezes the step branch, requests the auditor, applies fixes
> autonomously on a new feature branch, reruns closeout CI, adds resolutions without altering reviewer text,
> and requests re-audit until the auditor no longer identifies issues. The human owner approves the final
> verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | B. Model definition |
| Depends on | 03, 04, 05, 06 |
| Delivers | A load/model/cross-domain/feasibility validation pipeline, stable diagnostic catalog, model statistics, and the headless `mocassin validate` command. |

## Step-specific acceptance checklist

Copy the acceptance criteria from the step page, preserving their wording:

- [x] The ceria sample validates with zero errors and the warning set recorded in its fixture. — `samples/ceria/ceria.mocprj` and `tests/contracts/project-ceria.mocprj` both report 0 diagnostics; the fixture records the empty code set.
- [x] Every code emitted by the pipeline and the stage-2 validators has a catalog entry. Step 07 adds a
      per-code test with expected severity, path, and hint for every code it emits and records existing Step
      03–06 tests for their severity coverage. — `ENM018`, `SIM038`–`SIM041` each have severity/path/hint assertions; catalog completeness is assembly-local plus composed-union tested.
- [x] The catalog has no duplicate or undocumented codes. — constructor rejects duplicates; the sorted/unique, union, and docs-drift tests pass.
- [x] Validation never mutates the project; model and document hashes are unchanged. — success-path hash equality is tested; failure/cancellation hash checks are not delivered (see F3).
- [x] Diagnostics are deterministically sorted and deduplicated. — sorted-across-stages and exact-triple dedup tests pass.
- [x] `mocassin validate --json` validates against the `validation-v1` schema, including load-failure reports. — CLI tests validate success, missing-file, and usage reports against the generated schema.
- [x] Exit codes match the contract, including warnings-as-errors, parse failures, and I/O versus validation
      errors. — CLI tests cover 0/1/2/3; oversized and true OS-denied cases are asserted only at pipeline level (F3).
- [x] `--stats` is opt-in and deterministic. Statistics are `null` only when not requested, the document did
      not load, the model stage failed, or a preview failed; `--stats` never changes diagnostics or `valid`. — nullability and invariance are tested; repeat-run statistics determinism is not asserted (F3).
- [x] Ceria validation completes in `< 200 ms`; ceria `--stats` completes in `< 2 s`. — the PR-medium performance test passes; I did not independently re-time the in-process path outside that test.
- [x] The GUI contract can navigate from every diagnostic `ModelPath` to an object/property. Step 07 covers the
      path contract and Step 07 diagnostics; the interactive verification is completed in Steps 24–25. — domain/object/property paths are asserted per Step 07 code and serialized in the report; interactive navigation remains deferred as designed.
- [x] Documentation written. — diagnostics, CLI, troubleshooting, testing-and-parity, navigation, and contract pages are present and strict-build clean.

## Standard audit checklist

- [x] Required tests from the step page pass. — the committed suite is green (893 tests across nine .NET test projects, plus Rust/Python/tool suites); F3 records named rows that are not delivered.
- [x] Public/protected XML documentation policy is followed. — DocFX builds 249 managed-reference pages with 0 warnings/0 errors; the new public surface is in `PublicAPI.Unshipped.txt` and ApiCompat passes.
- [x] Documentation, status dashboard, feature log, and parity matrix are current. — Zensical strict and link checks pass; the overall status/dashboard/parity rows match the frozen state, except the stale closeout feature-log cells recorded in F2.
- [x] Closeout CI is green at the audited commit. — run `43115061587` is `completed/success` at `4d0fa53`; it is the only run on the step branch.
- [x] Closeout evidence matches the audited commit and the merge candidate. — checker passes; audit, closeout `head_sha`, and recorded merge candidate are all `4d0fa53` (see N1 on the docs-only branch tail).
- [x] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded. — `main` is `5b1eb74`, the branch point and recorded freeze SHA; it did not move.
- [x] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code. — the diff contains no P/Invoke, interop, OS-conditional, path/case, GUI, or RID-specific code.
- [x] Format/version/fixture changes are documented and tested. — the additive `validation-v1` report is in `reports`, has a generated schema and version constant, is described in `02-data-contracts.md` §5.6 and `cli.md`, and is schema-checked against real CLI output.
- [x] Security, dependency, licence, and secret-scan gates pass. — SPDX headers present in 320 source files; no secret patterns in 659 tracked text files; NuGet licence, vulnerability, ApiCompat, and cargo-deny checks pass.
- [x] Code structure policy passes; no new root-level or catch-all folders. — structure test passes; the new `Projects/Validation` folder has 15 files, within limits.
- [x] No unrelated changes or unapproved scope. — the 65-file diff is limited to the validation pipeline, catalog, statistics, CLI, contracts/schema, tests, fixtures, and documentation.
- [x] Branch, tag, and status hygiene are correct. — branch `step/07-validation-and-diagnostics`; no `step-07` tag and no main merge; feature branches deleted; one closeout dispatch only.

## Evidence

Commands run:

```text
# On step/07-validation-and-diagnostics at 4d0fa537f6239ffd61fbcb464b9eb9d1ce876d54
./tools/verify.sh all --require
# Passed: .NET build/test/format, API compatibility, Rust, Python, docs (Zensical/DocFX/rustdoc),
# contracts, process consistency, SPDX, secret scan, cargo-deny.
python tools/check_closeout_evidence.py docs/planning/closeout/step-07.json
# Closeout evidence: OK (after the evidence commit)
python tools/check_process_consistency.py
# Process consistency: OK
python tools/check_docs_links.py
# Documentation links OK (146 files scanned).
zensical build --strict
# No issues found
```

Closeout CI run `43115061587` is the single manual Ubuntu dispatch for this step and is green at the audited
commit. No per-feature CI ran; no Windows/macOS dispatch was required because the step adds no
platform-specific code.

### Audit round 1 — independent evidence

I am a fresh audit agent: I did not plan, implement, or review Step 07 and have no shared conversation with
the planner, implementor, or step agent. I read `AGENTS.md` §3.8–§3.13, §7, §8, §11, §12, §16; the audit
README/template; this request; the Step 07 page; the Step 07 design-review record; the closeout evidence;
`status.md`; the parity matrix; `02-data-contracts.md` §5.6; `03-quality-strategy.md` §9.7; the CLI,
diagnostics, troubleshooting, and testing-and-parity pages; and the full `5b1eb74..4d0fa53` diff. I did not
rely on the request summary for the verdict.

Frozen state: worktree `/workspace/mocassin`, branch `step/07-validation-and-diagnostics`, `HEAD`
`d926aa9` (the docs-only audit request), `git status --short` empty. The audited code commit is
`4d0fa537f6239ffd61fbcb464b9eb9d1ce876d54`; `git merge-base --is-ancestor 4d0fa53 HEAD` exits 0.
`git diff --name-status 4d0fa53..d926aa9` lists only `docs/planning/audits/step-07.md`,
`docs/planning/closeout/step-07.json`, `docs/planning/status.md`, and the Step 07 page; no code, test,
contract, fixture, or configuration changed after the freeze.

Commands run (from `/workspace/mocassin`), all at the frozen code content:

```text
./tools/verify.sh all --require
# exit 0. Build 0 warnings/0 errors; dotnet format clean; ApiCompat clean;
# Tests: Core 564, Symmetry 217, Packages 100, Cli 7, Architecture 1, App/Results/Compilation/Contracts 1 each (893 total);
# Rust fmt/clippy/tests/doc-tests pass; Python pytest 3 + sdist/wheel build pass;
# Zensical strict, DocFX 249 models 0 warnings/0 errors, rustdoc pass; contract artifacts current (18 files);
# process consistency, SPDX (320 files), secret scan (659 files), cargo-deny, NuGet licence/vulnerability gates pass.

python tools/check_closeout_evidence.py docs/planning/closeout/step-07.json
# Closeout evidence: OK

python tools/check_process_consistency.py
# Process consistency: OK

python tools/check_docs_links.py
# Documentation links OK (147 files scanned)

zensical build --strict
# No issues found

dotnet run --project src/dotnet/Mocassin.Cli -c Release --no-build -- validate samples/ceria/ceria.mocprj
# 0 errors, 0 warnings, 0 infos; Result: valid (exit 0)

dotnet run --project src/dotnet/Mocassin.Cli -c Release --no-build -- validate samples/ceria/ceria.mocprj --json --stats
# exit 0; schemaVersion 1, kind validation, non-null identity hashes, valid true, statistics object populated
```

GitHub Actions API checks: run `43115061587` is `CI`/`workflow_dispatch`, `completed/success`,
`head_sha=4d0fa537f6239ffd61fbcb464b9eb9d1ce876d54`, branch `step/07-validation-and-diagnostics`; jobs
Detect changes, Public API compatibility, Contracts and policy, and .NET (ubuntu-latest) succeeded, while
Python and Rust were path-filter skipped. A branch query returns exactly one run on this step branch, so no
per-feature CI ran. `main` is `5b1eb7496f856660f90f4bb358bd3cbd1d95165d`, equal to the recorded freeze SHA
and the branch point.

## Known notes for the auditor

- Feature squash sequence: `validation-core` (`24d9f58`), `diagnostic-catalog` (`47ae73f`),
  `model-statistics` (`ba4e2a3`), `cross-domain-validation` (`010562f`), `validate-cli` (`0ca9bd0`),
  `diagnostics-docs-and-fixtures` (`06568fe`), closeout (`4d0fa53`).
- Design record: `docs/planning/reviews/step-07.md` is `Ready for implementation` at `31b53e8` (round 4,
  record commit `2d01f3e`, human approval 2026-11-11).
- Recorded implementation deviations on the step page §12: `DiagnosticDescriptor` lives in
  `Mocassin.Core.Diagnostics` because the reviewed Core/Symmetry catalogs cannot reference a Packages type;
  `model-statistics` was implemented before `cross-domain-validation` so the budget checks consume the same
  estimates.
- `SIM040`/`SIM041` budget checks run regardless of `--stats` so the option cannot change diagnostics; statistics
  are materialized only when requested.
- The performance test measures the in-process pipeline on the ceria fixture (targets `< 200 ms` and `< 2 s`).
  Process startup is excluded and is visible in the CLI wall time.
- Accepted design Notes n1–n4 (advisory budgets, stage semantics, hint coverage, sizing) remain non-blocking
  as recorded in the design review.
- Follow-ups: Step 08 owns authoritative rule counts; Steps 09–12 own build-stage checks; Steps 24–25 own the
  GUI navigation and statistics panels; Step 30 completes the full CLI reference.
- F1–F3 resolution: the step page now states that budget estimates always run and only the full statistics
  record is opt-in; the closeout row records the green CI and squash; and the missing determinism,
  cancellation, mixed `SIM003`/`SIM039`, exact-cutoff `ENM018`, and failure-path no-mutation tests are added in
  `ValidationRobustnessTests`. Fix squash: `ccd0e55`.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **F1 — The step page still promises that statistics are not computed without `--stats`, but feasibility always runs the statistics previews to evaluate `SIM040`/`SIM041`.** §6 says “It is computed only on demand; `validate` without `--stats` must not pay for it” and §9 repeats the fast-path intent. `FeasibilityValidator.Validate` calls `statisticsService.TryCreate` unconditionally before materializing the result only when `IncludeStatistics` is set; `TryCreate` runs the structure, energy, cluster, and every kinetic/metropolis transition preview. The request's known note discloses the budget-check behaviour, but the durable step page's §12 deviations do not record it, so the source of truth still contradicts the implementation. Record the deviation and correct the §6/§9 wording (for example: estimates are always computed for the budget checks; the `ModelStatistics` object is materialized only on request). This does not breach the measured performance target, and `--stats` still does not change diagnostics or `valid`. | Step page `docs/planning/steps/step-07-validation-and-diagnostics.md` lines 307–308 and 417–420; `src/dotnet/Mocassin.Packages/Projects/Validation/FeasibilityValidator.cs` lines 64–74; `ModelStatisticsService.TryCreate` lines 62–150; step page §12 lines 446–455. |
| **Minor** | **F2 — The closeout feature-log row is stale.** The implementation-status table still shows the `closeout` row with `Step CI` = `pending` and no squash commit, although the same page records the green closeout run and the closeout squash is `4d0fa53`. The dashboard and the rest of the feature log are current. | Step page lines 19–27 (row `closeout`); page lines 12 and 16 record the green run and audit request; `git log` shows `4d0fa53` as the closeout squash. |
| **Minor** | **F3 — Several named test rows from the step page and quality strategy are not delivered.** There is no repeat-run determinism assertion for diagnostics and statistics, no cancelled-run test, no test for the explicit mixed `SIM003`/`SIM039` model case, no exact-cutoff equality case for `ENM018`, and the no-mutation hash check covers only the success path. The committed suite is green and covers the main behaviours (sorting/dedup, stage order, schema, exit codes, budgets, `SIM038`/`SIM039` suppression, statistics sources), so this is test-coverage completeness rather than an observed defect. The CLI exit-code tests also assert oversized and true OS-denied I/O only indirectly through the pipeline and mapping code. | Step page Tests table lines 528–548 (determinism, cancellation, cross-domain, feasibility rows) and acceptance criteria lines 503–510; `docs/planning/03-quality-strategy.md` §9.7 lines 302–325; `ProjectValidationPipelineTests.cs`, `CrossDomainAndFeasibilityTests.cs`, `ModelStatisticsServiceTests.cs`, and `ValidateCommandTests.cs` contain no such assertions; `grep -rni "cancel" src/dotnet/*Tests` returns only `CancellationToken.None` arguments. |
| **Note** | **N1 — The recorded merge candidate is the audit commit, not the current branch head.** `closeout/step-07.json` records `merge_candidate = 4d0fa53`, while the branch head is the docs-only audit request `d926aa9` and will gain this record and the step agent's resolution/status commit. The checker accepts this because the tail is documentation-only; no code, test, contract, or CI-covered file changes after the freeze. If the owner wants the evidence to name the actual final commit, the file can be refreshed after this record commit without a new code audit. | `docs/planning/closeout/step-07.json` lines 3–13; `git diff --name-status 4d0fa53..d926aa9` (four docs files only); `tools/check_closeout_evidence.py` passes. |
| **Note** | **N2 — Record parent versus audited commit.** The frozen code is `4d0fa53`, but this record's parent is the docs-only request commit `d926aa9`; the audit request itself is that commit. This matches the established practice recorded for Step 06 (its audit note N2). No reviewed code or evidence content changed in the request commit. | `git show -s --format='%H %P %s' d926aa9`; `git diff --name-status 4d0fa53..d926aa9`. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** Passed with notes

`Re-audit required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner or implementor.

## Verdict

- [ ] Passed
- [x] Passed with notes
- [ ] Fixes required
- [ ] Waived

Follow-up owner: Step 07 step agent / human owner
Follow-up issue/step: Round-1 F1–F3 are resolved at `ccd0e55`; N1–N4 are acknowledged informational notes. Proceed to the merge approval request with the §3.12 cumulative report. GUI navigation remains Steps 24–25 and the full CLI surface remains Step 12.

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Approval summary

The step agent completes this section before requesting human approval. It must cover the full history, not
only the latest round.

- [x] Requested decision and frozen commit recorded
- [x] Every review/audit round recorded (round, date, commit, reviewer, verdict)
- [x] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status)
- [x] Accepted notes and waivers recorded with rationale
- [x] Residual risks, deferred items, and follow-up owners recorded
- [x] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived
- [x] Final CI and documentation-build results recorded

Round history:

| Round | Date | Commit | Reviewer | Verdict |
| --- | --- | --- | --- | --- |
| Design 1 | 2026-11-11 | `dc73ee4` | Fresh planning auditor | Changes required |
| Design 2 | 2026-11-11 | `4ddd571` | Fresh planning auditor | Changes required |
| Design 3 | 2026-11-11 | `60437ca` | Fresh planning auditor | Ready with notes |
| Design 4 | 2026-11-11 | `31b53e8` | Fresh planning auditor | Ready for implementation |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| M1–M6 | Major | CLI/report failure paths, load ownership, Build extension, feasibility overlap, statistics nullability, manifest/schema | Resolved in the reviewed design | `4ddd571`, `60437ca`, `31b53e8` | Closed |
| m1–m9, R2-M1, R2-m1–R2-m7, R3-m1 | Minor/Major | Catalog types, overflow, sources, cancellation, hashes, gates, cell counts, `ENM018`, load-kind propagation, schema path, mixed suppression | Resolved in the reviewed design | `4ddd571`, `60437ca`, `31b53e8` | Closed |
| n1–n4 | Note | Budget rationale, stage semantics, hint coverage, sizing | Accepted as non-blocking design notes | `4ddd571`, `31b53e8` | Accepted |

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

Fresh independent audit agent; no shared conversation with the planner or implementor. Worktree
`/workspace/mocassin`, branch `step/07-validation-and-diagnostics`, `HEAD` `d926aa9` (docs-only audit
request), clean. Audited code commit `4d0fa537f6239ffd61fbcb464b9eb9d1ce876d54`; audited request and closeout
evidence reachable from `HEAD`. `main` is `5b1eb74`, the recorded freeze SHA and branch point; it did not
move. Post-freeze delta is documentation-only (this record, closeout evidence, dashboard, and step-page
status block).

#### Commands and results

`./tools/verify.sh all --require` passed with 0 warnings/0 errors and 893 .NET tests across nine projects,
plus Rust, Python, DocFX, rustdoc, Zensical strict, contract freshness, structure, API compatibility, SPDX,
secret, licence, vulnerability, and cargo-deny gates. `check_closeout_evidence.py` reported OK;
`check_process_consistency.py` reported OK; `check_docs_links.py` reported 147 files OK; `zensical build
--strict` reported no issues. Manual CLI checks on `samples/ceria/ceria.mocprj` returned exit 0 with no
diagnostics, and `--json --stats` returned a schema-shaped report with non-null identity hashes and
populated statistics. GitHub Actions run `43115061587` is green at `4d0fa53`, and it is the only run on the
step branch.

#### Findings

F1 (Minor): step page still claims statistics are not computed without `--stats`, while feasibility runs
the preview-based budget estimates unconditionally; the deviation is not recorded in step page §12.
F2 (Minor): the closeout feature-log row still says `pending` and has no squash commit. F3 (Minor): several
named test rows (determinism, cancellation, mixed `SIM003`/`SIM039`, exact `ENM018` equality, failure-path
no-mutation) are not delivered. N1–N2 are process/evidence notes. No Blocker or Major finding.

#### Verdict

**Passed with notes.** The step is independently verified at `4d0fa53`; only the three Minor findings and
two Notes above remain, and the human owner must explicitly accept them.

### Audit round 2

#### Independence and frozen state

Same independent audit agent as round 1; I remained read-only between rounds, did not implement or edit the
fixes, and have no shared conversation with the planner or implementor. Worktree `/workspace/mocassin`,
branch `step/07-validation-and-diagnostics`, `HEAD` `305521a` (docs-only evidence/status update), clean.
The new frozen code commit is `ccd0e55d56cdffbb48c735a1b2829a990271fb23` (fix squash:
`feature/step-07-validation-and-diagnostics/fix-audit-findings`). `git diff --name-status
ccd0e55..305521a` lists only the audit record, closeout evidence, dashboard, and step page; no code, test,
contract, fixture, or configuration changed after the freeze. Round-1 reviewer text was left intact; the
step agent updated only its own request fields and known notes. `main` is still `5b1eb74`, the branch point
and recorded freeze SHA.

#### Commands and results

`./tools/verify.sh all --require` passed again with 0 warnings/0 errors and 898 .NET tests (Packages 105,
Core 564, Symmetry 217, Cli 7, Architecture 1, App/Results/Compilation/Contracts 1 each), plus Rust, Python,
DocFX (249 models, 0 warnings), rustdoc, Zensical strict, contract freshness (18 files), structure, API
compatibility, SPDX (321 files), secret scan (660 files), NuGet licence/vulnerability, and cargo-deny gates.
`python tools/check_closeout_evidence.py docs/planning/closeout/step-07.json` → `Closeout evidence: OK`;
`python tools/check_process_consistency.py` → OK; `python tools/check_docs_links.py` → 147 files OK;
`zensical build --strict` → no issues.

GitHub Actions run `43291748042` is `CI`/`workflow_dispatch`, `completed/success`, with
`head_sha=ccd0e55d56cdffbb48c735a1b2829a990271fb23` on `step/07-validation-and-diagnostics`; the .NET,
Public API compatibility, Contracts and policy, and Detect changes jobs succeeded, while Python and Rust
were path-filter skipped. The branch has exactly two runs: the original closeout `43115061587` at
`4d0fa53` and this replacement at `ccd0e55`; no per-feature CI ran. The closeout evidence now names
`ccd0e55`/run `43291748042` and its checker passes.

#### Findings

**F1 — resolved.** Step page §6 now states that the preview-based rule/memory estimates always run and that
`SIM040`/`SIM041` do not depend on `--stats`; §9 correctly makes only the returned statistics record
opt-in; §12 records the deviation. This matches `FeasibilityValidator`/`ModelStatisticsService`, which
compute estimates unconditionally and return the statistics object only when `IncludeStatistics` is set.

**F2 — resolved.** The `closeout` feature row now records the green closeout run and squash `4d0fa53`; the
new `fix-audit-findings` row records `ccd0e55` and the replacement run. The dashboard and the step-page
status block record the replacement closeout run.

**F3 — resolved for the named rows.** `ValidationRobustnessTests` adds repeat-run determinism for
diagnostics/summary/statistics, pre-cancelled validation, the mixed empty-transition/no-mobile-start
`SIM003`/`SIM039` case, the exact `ENM018` cutoff-equality case (fixture hop is exactly 1.25 Å against a
1.25 Å cutoff), and failure-path model/document-hash immutability. The new file is PR-fast, has an SPDX
header, and the suite is green.

Residual notes, none blocking:

- **N3 — “Materialized” wording.** §6 and §9 now say the full statistics record is “materialized … only when
  `--stats` is requested”; in the code the record is constructed in `ModelStatisticsService.TryCreate` on
  every run and only withheld from the result when statistics are not requested. §12 states the actual
  withholding correctly. Suggest changing “materialized” to “returned” in §6/§9.
- **N4 — Direct CLI oversized/OS-denied coverage.** The five named robustness tests are present, but the
  CLI still lacks a direct exit-`3` test for an oversized document or a true OS-denied read; the pipeline
  `TooLarge`/`Io` load-failure tests and the CLI mapping cover the path indirectly. Acceptable follow-up.
- **N1/N2 — still acknowledged.** The closeout evidence records `merge_candidate = ccd0e55` while the branch
  head `305521a` and this record commit are documentation-only; the checker accepts the docs-only tail.
  This record's parent will be `305521a`, not the frozen code commit, as in round 1 and Step 06 practice.

No Blocker or Major finding, and no regression in the code, tests, contracts, documentation, parity,
status, or CI evidence reviewed in round 1.

#### Verdict

**Passed with notes.** All three round-1 Minor findings are resolved at `ccd0e55`; only the informational
N1–N4 notes remain. The replacement closeout CI is green at the new frozen commit, and the human owner must
explicitly accept the residual notes before merge approval.
