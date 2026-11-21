# Step 23B Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-23b.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 23B — Editing, Diagnostics, and Recovery |
| Request owner | Step agent (implementor) |
| Orchestrator | Orchestrator agent (spawns the fresh auditor) |
| Step agent (implementor) | DeepSeek V4.1 Flash 1017 (opencode-go) |
| Audit initiated by | Orchestrator, on request of the step agent |
| Step branch | `step/23b-editing-diagnostics-and-recovery` at `eb120e17ce43a152c8905fc1f54528b69448b3e3` |
| Closeout evidence | `docs/planning/closeout/step-23b.json` |
| Audit date | 2026-11-21 |
| Auditor | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh, independent audit context |

The closeout evidence file holds the audited commit, closeout CI run and `head_sha`, `main` SHA at freeze, merge
candidate, merge-tree CI, platform CI, and tag.

Step page: [`Step 23B — Editing, Diagnostics, and Recovery`](../steps/step-23b-editing-diagnostics-and-recovery.md)

## Step summary

| Field | Value |
| --- | --- |
| Phase | F. GUI |
| Depends on | 23A (workspace and project lifecycle) plus the merged library surfaces of 01, 03–07, and 12 |
| Delivers | The single-project edit pipeline with undo/redo, validation and Problems surface, metadata editor, dirty state and save point, autosave, crash recovery, recovery prompts, and unclean-exit session restore on the 23A shell |

## Step-specific acceptance checklist

Copy the acceptance criteria from the step page, preserving their wording:

- [x] Metadata editing works with undo/redo across the save point in both directions; a rejected structural edit
      changes nothing; a read-only session returns the app-level `ReadOnly` outcome with no diagnostic code.
- [x] The validity indicator and Problems channel show code, severity, path, source, and help; double-click
      navigates through a registered provider and produces a notice otherwise; the retained load/migration
      result reaches Problems and a clean load leaves the `Load` stage `Skipped`; opening does not run a second
      validation or a statistics run; the help map matches the documented anchors.
- [ ] Dirty state is correct across save, undo, and redo; closing with unsaved changes prompts; Cancel closes
      nothing and leaves autosave running; Discard and discard-reload delete the sidecar/recovery file; a return
      to the save point or a clean close deletes the sidecar, except a deferred sidecar candidate, which
      survives until Recover/Discard and is cleared by the first accepted content change (a no-op same-hash
      edit does not clear it, n-12); a deferred `recovery/` candidate remains subject to the 7-day sweep.
      — **Not fully met:** the single-session paths pass, but a window/exit close with several open projects
      closes earlier sessions before a later Cancel and cannot "close nothing"; see F2.
- [x] Autosave writes only dirty documents and deletes its file on save; save/autosave/close/reload/recovery do
      not interleave; a simulated crash leaves a `recovery/` snapshot; recovery restores a dirty document equal
      to the snapshot, updates `LoadDiagnostics`, and recomputes validation before the `Replaced` event; a
      timestamp-only snapshot is not offered; the crash path completes within its budget or reports the timeout.
      — Behaviour matches; see F3 for the autosave UI-thread finding.
- [x] Recovery candidates include the sidecar source; the newest valid candidate wins; recovery files are swept
      after 7 days; sidecars are never swept; unclean-exit session restore works; missing project files are
      reported without an untitled session.
      — Restore works, but a `--project` startup open corrupts the remembered restore state before the offer;
      see F1.
- [x] The `autosave`/`session` settings fields round-trip without a schema bump and older documents still load.
- [x] No unhandled exception kills the process silently, and the crash dialog reports recovery results; the
      dead-UI path is documented.
- [x] `./tools/verify.sh all --require` passes, the strict docs build passes, and `closeout/step-23b.json` with
      `"step": "step-23b"` validates.
- [x] Documentation written (§12), and no page claims a later step's capability or an untitled-project model.
      — One `Cancel` sentence and the `--project` restore interaction are inaccurate; see F1/F2.

## Standard audit checklist

- [x] Required tests from the step page pass. — 2,099 solution tests, including 211 App tests; gaps noted in F7.
- [x] Public/protected XML documentation policy is followed.
- [x] Documentation, status dashboard, feature log, and parity matrix are current. — Status/dashboard/parity
      match the branch; the two documentation inaccuracies are under F1/F2.
- [x] Closeout CI is green at the audited commit.
- [x] Closeout evidence matches the audited commit and the merge candidate.
- [x] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded. — `main` is still
      `3a20481`; it did not move after the freeze.
- [x] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code. — Both platform gates ran and are green at `eb120e1`.
- [x] Format/version/fixture changes are documented and tested.
- [x] Security, dependency, licence, and secret-scan gates pass.
- [x] Code structure policy passes; no new root-level or catch-all folders.
- [x] No unrelated changes or unapproved scope.
- [x] Branch, tag, and status hygiene are correct. — No `step-23b` tag and no merge to `main`; the head delta
      after `eb120e1` is documentation-only.

## Evidence

Reviewed tree: `eb120e17ce43a152c8905fc1f54528b69448b3e3` (audited commit). Branch head
`d3c927004a03839a3482851b9094c19df6ef0c4d`; `git diff --name-status eb120e1..d3c9270` is
`A docs/planning/audits/step-23b.md`, `A docs/planning/closeout/step-23b.json`,
`M docs/planning/status.md`, `M docs/planning/steps/step-23b-editing-diagnostics-and-recovery.md`; `git diff
--name-only eb120e1..HEAD` contains no non-docs path. `main` is `3a2048150488e6b14f3cc4f3da59aa44bae93908`
(unchanged), the merge is `7a2f463e1726f3d4fc7d28d426626ef35360af26` (parents `9010784` and `3a20481`), and no
`step-23b` tag exists. The branch head is therefore a documented documentation-only freeze after the audited
tree.

Commands run in this worktree (all exit 0 unless noted):

```text
./tools/verify.sh all --require            # exit 0; ran at d3c9270, non-doc tree identical to eb120e1
python tools/check_closeout_evidence.py docs/planning/closeout/step-23b.json   # "Closeout evidence: OK"
git diff --check 3a20481..eb120e1          # clean
git merge-base --is-ancestor 3a20481 eb120e1   # true
```

`./tools/verify.sh all --require` results: solution test projects all green — Contracts 38, Symmetry 223, Core
689, Results 91, Compilation 99, Cli 203, Packages 536, Architecture 9, App 211 (2,099 tests total, 0 failed);
`dotnet format` clean; Rust fmt/clippy/tests/build green; Python tests/build green; strict docs build
(`Documentation built in .../site`), link/math checks, DocFX and rustdoc green; contracts/policy current
(26 artifacts, 60 policy tests, spglib catalog current, SPDX 1,095 files, 1,606 tracked files secret-scanned,
cargo-deny ok); AOT evidence gate passed with the documented 62 IL diagnostics; `Closeout gate completed with
required checks enabled.` (`EXIT:0`).

Closeout CI and platform gates (GitHub API, `actions/runs`):

- Ubuntu closeout `44929014836`: `head_sha` `eb120e1...`, `workflow_dispatch`, conclusion `success`; jobs
  Detect changes, Contracts and policy, Public API compatibility, .NET (ubuntu-latest) succeeded; Rust/Python
  skipped by path filters.
- Windows platform gate `44929276522`: same `head_sha`, `success`; `.NET (windows-latest)` succeeded.
- macOS platform gate `44929582584`: same `head_sha`, `success`; `.NET (macos-latest)` succeeded.
- Red history confirmed: run `44926916792` at `a14ef1f` failed the Ubuntu .NET job; run `44927688791` at
  `833f135` failed the Windows .NET job; run `44927339855` at `833f135` was the green Ubuntu replacement. The
  audited `eb120e1` runs are green, consistent with the request notes.

Code and design inspection: `ProjectDocument.MaxUndoDepth`/`CreateRecovered` and
`PublicAPI.Unshipped.txt` (the only public API delta; no Packages/App `PublicAPI` or baseline file changes);
`ProjectEditCoordinator`, `ValidationCoordinator`, `ProblemsChannelViewModel`, `DiagnosticHelpLinks`,
`ProjectOverviewViewModel`, `ProjectSession`/`ProjectSessionService`, `ProjectRecoveryDetector`,
`RecoveryStore`/`RecoveryComparison`/`RecoveryRetentionPolicy`, `AutosaveScheduler`/`AutosaveTimer`,
`CrashRecoveryWriter`/`CrashDialogViewModel`/`CrashHandler`, `ShellLifecycleCoordinator`,
`ShellProjectOperations`, `ShellEditCommands`, `ShellViewModel`/`ShellWindow`, settings blocks, and the new
tests. Line references for the findings are in the Findings table.

Documentation/parity: `docs/development/ui-architecture.md`, `testing-and-parity.md`,
`design-decisions.md`, the user-guide workspace/projects/settings pages, `reference/file-layout.md`,
`reference/diagnostics.md`, `troubleshooting/errors.md`, and the parity rows describe the delivered 23B
surface; the strict build passes. The two inaccurate statements behind F1/F2 are in
`docs/troubleshooting/errors.md` (Cancel "keeps the window and sessions open") and `docs/user-guide/projects.md`
(the `--project` startup interaction with the session restore offer).

No new diagnostic codes, no contract/format change, and no `contracts/manifest.json` change were observed;
`ProjectDiagnosticCodes`/`DiagnosticCatalog` are not in the delta. The AOT outcome (62 IL diagnostics recorded,
gate passed) matches the step page.

Evidence file: `docs/planning/closeout/step-23b.json` (validated).

## Known notes for the auditor

- **Accepted design-review state:** the design record is
  [`reviews/step-23b.md`](../reviews/step-23b.md), accepted `Ready with notes` by the human owner 2026-11-21 at
  `a33711d`/`c4ded0e`. The owner accepted m-14 as a recorded limitation (deferred `recovery/` candidate can
  expire after seven days; `.autosave` sidecars are sweep-exempt).
- **Closeout red history:** the first Ubuntu run at `a14ef1f` failed one timing-sensitive
  `RevalidateRunsImmediatelyWithoutTheDebounce` assertion; fixed by making the test timing-independent (`833f135`).
  The first Windows gate at `833f135` failed 19 headless tests because the async close path still had a settings
  temp file open when `TempWorkspace` was disposed; fixed by awaiting the close and draining dispatched work
  (`eb120e1`). The final audited commit is `eb120e1`; all Ubuntu/Windows/macOS runs above are green.
- **Known limitations to examine:** per-project close/exit prompts (no apply-to-all batch choice); ordinary Open
  on a missing main file reports the load failure without a dedicated snapshot-path/discard dialog, while
  session restore reports missing paths; the dead-UI crash path writes no recovery files; the autosave
  failure-to-`recovery/` fallback is implemented but not failure-injected in a PR-fast test; a deferred
  `recovery/` candidate may expire under the accepted m-14 seven-day sweep.
- The step adds two additive Core APIs (`ProjectDocument.MaxUndoDepth`, `ProjectDocument.CreateRecovered`) and
  no Packages/App public API. The `autosave`/`session` settings blocks are additive and app-local with no schema
  bump.
- Documentation changes cover the 23B column in `ui-architecture`, `testing-and-parity`, the user-guide
  workspace/projects/settings pages, `file-layout`, `troubleshooting/errors`, diagnostic help anchors, and the
  parity matrix.
- No `main` merge, tag, or code change has occurred after the audited commit.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **F1 — A `--project` startup open overwrites the remembered session before the restore offer, producing a false unclean-exit prompt and losing the previous project list.** `ShellViewModel` starts `StartupProjectTask = OpenProjectPathAsync(startupProjectPath)` in its constructor (`ShellViewModel.cs:178-180`); every `OpenAsync` ends in `RecordOpenAsync`, which writes `RecordSessionStateAsync(cleanExit: false)` (`ProjectSessionService.cs:703-712`, state at `:627-641`) with the *current* session list. `ShellWindow.Opened` then calls `BeginStartupRestore` (`ShellWindow.axaml.cs:47-54`), and `RestoreSessionAsync` awaits the startup task, sweeps, and only then reads `settings.Current.Session` (`ShellLifecycleCoordinator.cs:120-126`). A clean prior exit followed by `--project` therefore reaches the "previous MOCASSIN session ended unexpectedly" prompt; the remembered `openProjects` list has already been replaced by the current session, so "Restore session" cannot restore it. The same window exists for a project opened while the startup sweep is running. Step page §5/§6 and the test plan require a clean exit not to offer restore; `WorkspaceRenderTests.TheProjectArgumentOpensAtStartup` does not assert the restore offer and the restore tests use no startup project. Fix: capture the startup `Session` before opening, or pass the startup snapshot into `RestoreSessionAsync`, and do not write the new session block until the restore decision is made. | `ShellViewModel.cs:178-180`; `ProjectSessionService.cs:627-641`, `:703-712`; `ShellWindow.axaml.cs:47-54`; `ShellLifecycleCoordinator.cs:116-126`; step page §5 lines 589-593, test plan line 798; `WorkspaceRenderTests.cs:120-155`; `ShellLifecycleTests.cs:120-149` |
| **Major** | **F2 — Closing the window is not all-or-nothing across several projects: a later Cancel leaves earlier projects closed (and discarded).** `RequestCloseAsync` loops over a snapshot of sessions and closes each one before prompting the next (`ShellLifecycleCoordinator.cs:72-98`). If a later dirty project returns Cancel, the method returns `false` at `:78-80`, but earlier sessions are already removed; an earlier Discard choice has already deleted that project's sidecar/recovery files through `CloseAsync(session, discard: true)` (`:98`). The acceptance criterion says "Cancel closes nothing"; step page §4 lines 463-469 says "Cancel aborts the close and closes nothing"; `docs/troubleshooting/errors.md` says Cancel "keeps the window and sessions open". Only the one-session path is tested (`ShellLifecycleTests.CloseCancelLeavesTheSessionOpenAndDiscardClosesIt`). Fix: collect every dirty decision first (or implement the owner-approved apply-to-all/batch decision) and close only after all prompts pass; add a two-session regression test. | `ShellLifecycleCoordinator.cs:67-108`, `:192-212`; `ShellLifecycleTests.cs:49-74`; step page §4 lines 463-469 and acceptance line 973; `docs/troubleshooting/errors.md` "Closing the window does not close" row |
| **Major** | **F3 — Autosave mutates UI-bound status and `ProjectDocument.MarkAutosaved` on a thread-pool continuation.** `AutosaveTimer` posts `RunOnceAsync` to the UI dispatcher, but the method awaits the session gate and sidecar/fallback writes with `.ConfigureAwait(false)` (`AutosaveScheduler.cs:89`, `:108-117`), then sets `StatusText`/`StatusGlyph`/`Severity` (`:103-105`, `:120-122`, `:127-133`) and calls `session.Document.MarkAutosaved(now)` (`:127`) on the resumed thread. Those status properties are bound through `AutosaveStatusItemProvider` (`AutosaveStatusItemProvider.cs:30-37`) to `StatusBarView`; step page §7 says all `ProjectDocument` mutations happen on the UI thread, and `ValidationCoordinator` shows the expected explicit `DispatchAsync` pattern. No test asserts the thread. This can produce a cross-thread Avalonia update (an unobserved task exception under the crash boundary) or a stale/duplicated autosave status and misses the `MarkAutosaved`/due bookkeeping update. Fix: marshal the observable/status and `MarkAutosaved` back to the UI dispatcher, or remove `ConfigureAwait(false)` from the UI-observable path, and add a dispatcher-affinity regression test. | `AutosaveScheduler.cs:69-140`; `AutosaveStatusItemProvider.cs:11-37`; `AutosaveTimer.cs:62-70`; `StatusBarView.axaml:16-33`; `ValidationCoordinator.cs:412-438`; step page §7 line 634 |
| **Minor** | **F4 — Deferred-sidecar cleanup after Save As and discard does not use the candidate path, and `DiscardRecoveryAsync` does not reject a foreign project id.** `DiscardRecoveryAsync` deletes `ProjectStore.DeleteAutosave(session.Path)` for a `Sidecar` candidate (`ProjectSessionService.cs:497-500`) instead of `candidate.SnapshotPath`, and does not check `candidate.ProjectId == session.Id` (unlike `RecoverAsync` at `:446-449`). After `SaveAsAsync` with a deferred sidecar, the old path's sidecar is intentionally preserved (`:300-310` skips cleanup while `PendingRecovery` is non-null), but a later Discard removes the new path's sidecar and leaks the old one; a mismatched candidate could delete another project's sidecar. The shell passes matching candidates, so this is an internal robustness/cleanup defect and the Save-As-with-pending path is untested. | `ProjectSessionService.cs:296-312`, `:446-449`, `:486-515`; `ProjectRecoveryLifecycleTests.cs:84-131` |
| **Minor** | **F5 — Autosave/fallback/recovery failures are not logged, despite the design.** Step page §5 says "A failed autosave logs a warning" and §6 says recovery "failures are logged, not fatal". `AutosaveScheduler` has no logger and only changes the status item (`AutosaveScheduler.cs:118-124`); the fallback's `RecoveryWriteResult` diagnostics are discarded; `RecoveryStore.ListAsync` swallows malformed/oversized/IO/unauthorized candidates (`RecoveryStore.cs:84-95`) without a log. The visible status item is correct, but operational diagnosis is lost. | `AutosaveScheduler.cs:39-51`, `:110-125`; `RecoveryStore.cs:43-101`; step page §5 line 512, §6 line 614 |
| **Note** | **F6 — `CleanupAsync` reads the workspace and document state off the UI thread.** After `await session.Gate.WaitAsync().ConfigureAwait(false)` (`ProjectSessionService.cs:564`), the continuation checks `Sessions.Contains` and `session.Document.IsDirty` (`:567-569`) on a thread-pool thread; the workspace and `ProjectDocument` are UI-thread-owned. The operation is read-only and serialized by the gate, but it can race a UI close/edit. Consider capturing the state on the UI thread or dispatching the check. | `ProjectSessionService.cs:557-579` |
| **Note** | **F7 — Test coverage gaps for the new shell lifecycle.** No test combines `--project` with the restore offer (F1), no test closes a window with two sessions where the second is cancelled (F2), `RecoverAppliesTheSnapshotAndTheNextSaveDeletesIt` does not assert that `LoadDiagnostics` changed to the snapshot's diagnostics, and no test covers a same-`SavedUtc` sidecar/manifest tie or the newest-candidate choice. The autosave fallback is intentionally not failure-injected (recorded limitation). These are test-matrix gaps, not failures of the existing assertions. | `WorkspaceRenderTests.cs:120-155`; `ShellLifecycleTests.cs:49-74`; `ProjectRecoveryLifecycleTests.cs:84-113`; `ProjectRecoveryDetector.cs:70-96` |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** Fixes required

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [x] Fixes required
- [ ] Waived

Follow-up owner: Step agent (implementor) — fix F1–F5 on a new feature branch; the owner accepts/closes F6–F7
and the recorded limitations.
Follow-up issue/step: Step 23B — `feature/step-23b-editing-diagnostics-and-recovery/fix-audit-round-1`; re-audit
after a new squash, current `main` merge, local gate, and replacement closeout CI.

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

I am a fresh context. I did not author or implement this work and have no shared conversation with the step
agent, planner, design reviewers, or orchestrator. I read `AGENTS.md` §3.10 and §7–§16, the audits README, the
23B step page, the 23B design-review record and its rounds, the 23A audit context, the ADRs/architecture/quality/
code-structure pages, the closeout evidence, and the App/ViewModels/Core code at the frozen tree.

Frozen state: audited tree `eb120e17ce43a152c8905fc1f54528b69448b3e3`; branch head
`d3c927004a03839a3482851b9094c19df6ef0c4d`; the head delta from the audited tree is documentation-only (audit
record, closeout evidence, step page, status dashboard) and the non-doc diff is empty. `main` is
`3a2048150488e6b14f3cc4f3da59aa44bae93908`; the step branch merged `main` at
`7a2f463e1726f3d4fc7d28d426626ef35360af26`; `main` has not moved. No `step-23b` tag and no merge to `main`
exist.

#### Commands and results

`./tools/verify.sh all --require` exited 0 (2,099 tests, 0 failed; docs/DocFX/rustdoc, Rust, Python, contracts/
policy, structure, AOT evidence). `python tools/check_closeout_evidence.py docs/planning/closeout/step-23b.json`
returned `Closeout evidence: OK`. The three recorded CI runs were queried through the GitHub API: Ubuntu
`44929014836`, Windows `44929276522`, and macOS `44929582584`, all `success` with `head_sha`
`eb120e17ce43a152c8905fc1f54528b69448b3e3`; the earlier red runs at `a14ef1f` (`44926916792`) and `833f135`
(`44927688791`) and the green Ubuntu replacement at `833f135` (`44927339855`) match the recorded fix-forward
history. `git diff --check` is clean. Full details, counts, and file paths are in the Evidence section.

#### Findings

F1 (Major), F2 (Major), and F3 (Major) are open; F4/F5 are open Minor findings; F6/F7 are notes. F1 is a
startup-restore state bug; F2 is an acceptance/documentation mismatch in the multi-project close; F3 is a
UI-thread/autosave defect. The acceptance matrix is otherwise satisfied: the edit pipeline/undo/save-point,
validation coordinator and Problems surface, metadata form, dirty markers, autosave sidecar/cleanup, recovery
manifests/retention/PendingRecovery, Recover/Discard/Keep, crash writer, settings blocks, and the two additive
Core APIs were verified against code and tests. The m-14 retention limitation is accepted and documented.

#### Verdict

**Fixes required.** The audited tree is not merge-ready until F1–F3 are fixed and re-audited (F4/F5 should be
fixed or explicitly accepted; F6/F7 may remain as notes). The step branch should move to `Re-audit required`
only after the step agent records resolutions, applies the fixes on a new feature branch, merges current
`main`, re-runs the full local gate, dispatches one replacement closeout CI run, and appends the new evidence.
