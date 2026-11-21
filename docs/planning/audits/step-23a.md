# Step 23A Audit Request

> Reviewer: write the evidence, findings, and verdict directly into this file. You may edit only your own
> metadata (`Auditor`, `Audit date`, `Record state`), the checklist completion marks and inline verification notes
> (not item text), the `Evidence`, `Findings`, and `Verdict` sections, and commit only this file with a
> documentation-only message and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions,
> code, tests, configuration, status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 23A — Workspace and Project Lifecycle |
| Request owner | Step agent (implementor) |
| Orchestrator | — |
| Step agent (implementor) | DeepSeek V4.1 Flash 1017 |
| Audit initiated by | Orchestrator, on request of the step agent |
| Step branch | `step/23a-workspace-and-project-lifecycle` |
| Closeout evidence | `docs/planning/closeout/step-23a.json` |
| Audit date | — |
| Auditor | — |

The closeout evidence file holds the audited commit, closeout CI run and `head_sha`, `main` SHA at freeze, merge
candidate, merge-tree CI, platform CI, and tag. Do not restate them here: one source, one place.

Step page: [`Step 23A — Workspace and Project Lifecycle`](../steps/step-23a-workspace-and-project-lifecycle.md)

> Orchestration: the orchestrator spawns a fresh auditor when the step agent has frozen the step branch and
> completed the audit request, and relays the verdict and owner decisions. The step agent applies fixes on a new
> feature branch, reruns closeout CI, adds resolutions without altering reviewer text, and asks the orchestrator
> for re-audit until the auditor no longer identifies issues. The orchestrator does not fix findings or edit the
> audit record. The human owner approves the final verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | F. GUI |
| Depends on | 01, 03, 04, 05, 06, 07, 12 |
| Delivers | Avalonia application host and composition root; shell/regions; document and navigation model; read-only Overview/JSON documents; project create/open/save/save-as/close/reload with recents, locks, read-only, and external-change detection; versioned settings and appearance; dialog/picker/console/notification/progress surfaces; structured logging and crash boundary; 23A tests and documentation. Step 23B adds editing, validation surfaces, autosave, and recovery. |

## Step-specific acceptance checklist

Copy the acceptance criteria from the step page, preserving their wording:

- [ ] The projects build on all three target platforms; the headless suite starts the shell on Ubuntu
      automatically; the Windows/macOS headless suite and the manual real-window checklist are recorded.
- [ ] The app starts with a welcome document; the shell regions, menu bar, and status bar render.
- [ ] A project can be created from both embedded templates, opened, inspected, saved, closed, and reopened;
      reopening yields identical content through the real store.
- [ ] Save-as works and migrates the lock; an already-open path activates its session; **New** refuses an
      existing target with the CLI's message, while Save As to an existing file follows the picker confirmation.
- [ ] The composed `ProjectStore` has `ValidateOnLoad = false` and retains `ProjectLoadResult.Diagnostics`; the
      session performs exactly one internal load-validation pass (see step page §5.1), and no validation UI is
      registered or shown in 23A.
- [ ] Reload and external-change detection behave as designed; no silent overwrite is possible.
- [ ] Recent files, locks, read-only mode, and the project switcher behave as designed.
- [ ] Settings round-trip through the real store, validate, migrate, ignore unknown fields, reject a future
      major without overwriting, survive a failed/oversized write, and persist last directories.
- [ ] Every primary action is reachable by keyboard alone; the palette lists the shell commands with their
      platform gestures; `Model`/`Build`/`Run` menus do not appear.
- [ ] Accessibility names/roles are present on the primary controls; the accessibility-tree assertions pass.
- [ ] No unhandled exception kills the process silently; the crash dialog and log entry are verified.
- [ ] `./tools/verify.sh all --require` passes, the strict docs build passes, and `closeout/step-23a.json` with
      `"step": "step-23a"` validates once the `step-NN[a-z]` checker support has landed.
- [ ] Documentation written (§14), and no page claims a 23B or later capability.

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

Commands run (to be confirmed by the auditor):

```text
./tools/verify.sh dotnet
./tools/verify.sh structure
./tools/verify.sh api
./tools/verify.sh contracts
./tools/verify.sh docs
./tools/verify.sh all --require
python tools/check_closeout_evidence.py docs/planning/closeout/step-23a.json
```

Evidence file: `docs/planning/closeout/step-23a.json`

## Known notes for the auditor

- The design originally said 23A never calls `ProjectValidationPipeline`; feature 4 performs one internal
  load-validation pass (store `ValidateOnLoad = false`, load diagnostics passed once). This is recorded in step
  page §5.1 and is tested for clean (`Skipped`) and diagnostic (`Completed`/`Failed`) loads. No validation UI is
  added; 23B owns the surface.
- `Avalonia.Headless.XUnit` is not used because Avalonia 12.1.6 requires xunit v3 and the repository uses xunit 2.
  The first-party `Avalonia.Headless` `HeadlessUnitTestSession` is used instead.
- The ANGLE Windows native package pulled by `Avalonia.Desktop` has an exact-version BSD-3-Clause licence
  exception in `tools/contracts/check_nuget_licenses.py`.
- `Mocassin.App` and `Mocassin.App.Views` have `InternalsVisibleTo` from the App assembly for the composition
  root and compiled XAML bindings. Only `App` is a new public application type.
- Project JSON “Save a copy…” export is deferred; copy is implemented.
- Tab drag-reorder is not wired; `Ctrl+1..9`, `Ctrl+Tab`/`Ctrl+Shift+Tab`, `Ctrl+W`, middle-click close, close
  button, and context menu are wired. `DocumentCollectionViewModel.Move` is available for the follow-up.
- Save As to the session’s current path is treated as a lock conflict; the picker normally prevents it.
- External-change detection uses length + last-write UTC (the documented cheap filter), not a fresh content hash.
- `ProjectSessionService.CreateAsync` takes a target **directory**, not a full file path.
- `ProjectWorkspaceEntry` wraps `ProjectSession`; the feature-2 constructor remains for standalone documents/tests.
- The AOT gate records the CLI publish diagnostics as evidence, not a hard failure; `./tools/verify.sh all
  --require` exits 0. The auditor should inspect `artifacts/aot/mocassin-cli.log`.
- The Ubuntu closeout run is the only CI dispatch for this step. Windows/macOS platform gates are recorded as
  not run in the closeout evidence, with the manual acceptance rationale in the step page; this is a residual
  risk for the auditor and owner.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** In audit

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

## Approval summary

The step agent completes this section before a merge, tag, or Blocker/Major waiver request. It must cover the
full history, not only the latest round.

- [ ] Requested decision and frozen commit recorded
- [ ] Every review/audit round recorded (round, date, review, verdict)
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
