# Step 23B Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-23b.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 23B — Editing, Diagnostics, and Recovery |
| Request owner | Planner (design freeze); the step agent takes ownership of resolutions at the start gate |
| Orchestrator | Orchestrator agent (spawns the planning auditor) |
| Step agent (implementor) | Not assigned — created from the reviewed `main` commit after the start-gate approval |
| Review initiated by | Orchestrator, on request of the planner |
| Reviewed commit | `a33711dbf9fb35bd782f5bd88eba111e461634d6` |
| Docs branch | `docs/step-23b-design` |
| Review date | 2026-11-21 |
| Reviewer | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh planning auditor |

Step page: [`Step 23B — Editing, Diagnostics, and Recovery`](../steps/step-23b-editing-diagnostics-and-recovery.md)

**Freeze note.** The review freeze is `f629dd514f5e4e3c83584aff468969545c407b5c`, the commit that completes
the 23B design. It is based on `main` at `63cdd0638b952e9175f75681d864bb95db0b4f68` (23A merged and tagged
`step-23a`). Earlier commits `82b045a`, `a529a71`, and `ab96aab` were pre-review snapshots; they are superseded
before any reviewer was spawned. The cell above is filled by a documentation-only request follow-up, so the
reviewer's record commit is the only delta. The freeze contains no code, test, configuration, contract,
generated file, or status-dashboard edit.

**Refreeze note (round 2, 2026-11-21).** Round 1 returned `Changes required` on `f629dd5` for M-1 and m-1–m-10,
with n-1–n-5. The design was refrozen at `022132e`. The refreeze chooses the **app-level** read-only refusal
(`EditApplyOutcome.ReadOnly`, no `PROJ031` and no new code), completes the §11 seam-edit list (shell,
session writes, workspace markers, window close, registration), adds the `NotifySaved`/`EditRejected`
notification contract, deletes sidecars on explicit discard, adds the `Sidecar` recovery source and the
timestamp-normalized recovery comparison, makes statistics an explicit user action, recomputes
`ProjectSession.Validation` on recovery, specifies `Document`/`CanEdit` reset, the `MaxUndoDepth` init-accessor
rejection, the per-session gate for close/reload, and the n-1–n-5 clarifications. **Round 1 reviewer text
(Evidence, Findings, Verdict, and the round-1 body) is byte-identical to `3efae49`**; only request-owned
fields (this note, the `Reviewed commit` cell, `Record state`, the approval-summary history, and `Resolution
status`) changed.

**Refreeze note (round 4, 2026-11-21).** The owner relayed two round-3 findings on `a55fd61` and instructed a
refreeze for round 4: **m-13** (clean-close cleanup could delete a deferred `PendingRecovery` candidate) and
**n-10** (the §2.1 consumer table omitted the `ProjectSessionService` cleanup handler and its non-reentrant gate
rule). The design was refrozen at `a33711d` after the step-page status pointer was aligned. Clean-close/return-
to-clean cleanup is skipped while a deferred candidate exists, the first edit clears it, and the cleanup handler
ignores `Saved`/`Replaced` and queues `Edited`/`Undo`/`Redo` cleanup outside the non-reentrant gate. The owner
also recorded the class, feature-list, and §17.1 scope approvals on 2026-11-21 (see `Human approval`). **Round 1
and round 2 reviewer text is byte-identical to `362aa75`**; only request-owned fields and the new owner decisions
changed.

**Refreeze note (round 3, 2026-11-21).** Round 2 returned `Changes required` on `022132e` for M-2 and m-11/m-12,
with n-6–n-9. The design was refrozen at `a55fd61`. The refreeze keeps `RecoveryComparison` in the ViewModels
layer but removes its `ProjectHasher` call: it normalizes `ModifiedUtc` and reads
`ProjectDocument.CreateRecovered(...).IsDirty`, so the hash stays in Core. It adds the
`StatusBarItemViewModel` seam, deletes the sidecar when the document returns to the save point or closes clean,
updates `LoadDiagnostics` as well as `Validation` before `ReplaceDocument` raises `Replaced`, clears
`IsClosing` on `UnsavedChanges`/Cancel, and adds the explicit §17.1 owner-question list. **Round 1 and round 2
reviewer text (Evidence, Findings, Verdict, and both round bodies) is byte-identical to `362aa75`**; only
request-owned fields (this note, the `Reviewed commit` cell, `Record state`, the approval-summary history, and
`Resolution status`) changed.

**Split note.** Step 23 was class High with three independent acceptance stories and an audit surface too large
for one sitting. The human owner approved the split on 2026-11-20 into **23A** (workspace and project lifecycle)
and **23B** (this step: editing, diagnostics, recovery). 23A is merged and tagged. 23B contains no 23A task,
introduces no step branch, and depends on nothing from Steps 08–11 or 13–22. Step 24 now depends on 23B. The
superseded unsplit record is [`step-23.md`](step-23.md), closed as superseded with its rounds unchanged; the 23A
review is [`step-23a.md`](step-23a.md).

**23A context.** 23B is designed against the delivered 23A code, not only the 23A design. The relevant
deviations are: first-party `Avalonia.Headless` with `HeadlessUnitTestSession` (no `Avalonia.Headless.XUnit`,
because it requires xunit v3); the ANGLE licence exception; `InternalsVisibleTo` for `Mocassin.App` and
`Mocassin.App.Views`; no tab drag-reorder; automated Xvfb evidence; `ProjectPathRules` with `ExistingTarget`;
and 23A's single internal load-validation pass, which retains `ProjectSession.Validation` for 23B. 23B consumes
that result and does not revalidate on open.

> Orchestration: the orchestrator spawns a fresh planning auditor after the planner has frozen the design and
> completed this request, and relays the verdict and owner decisions. The planner and step agent update the
> design docs, apply fixes on the design docs or a maintenance branch, add resolutions without altering reviewer
> text, and ask the orchestrator for re-review until the auditor no longer identifies issues. The orchestrator
> does not edit the design or the review record. The human owner approves the final verdict and the start of
> implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | F. GUI (second half of the split Step 23) |
| Depends on | 23A (merged, tagged `step-23a`) plus the merged library surfaces of 01, 03–07, and 12. Not dependent on 02/08–11 or Steps 13–22 |
| Delivers | The single-project edit pipeline and bounded undo/redo, the validation/Problems surface, dirty state and save-point handling, autosave, crash recovery, and unclean-exit session restore, on top of the 23A shell. No model editors, build UI, 3D, or run monitoring |
| Complexity and split | Class **High** on cross-step interfaces and test-matrix breadth (Medium on the other five dimensions). **Owner acceptance of the class is required at the start gate**; no further split is proposed (page §16 states the one-unit case and guard) |

## Design decisions reviewed

- [x] Step page design decisions — goal, scope, non-goals, the 23A seam map, the edit pipeline and
      `ProjectChanged` consumers, save-point semantics, validation surface and retained-result contract, dirty
      markers and prompts, autosave, recovery/crash handling, settings additions, the two library changes, the
      23A seam-edit list, documentation, tests, performance, risks, and the class. — Reviewed all sections;
      findings M-1, m-1–m-10, n-1–n-5.
- [x] Data contracts and format changes — the application-local `mocassin.settings` v1 `autosave`/`session`
      additions with no schema bump; the app-local `recovery/` manifest; no change to `.mocprj`, `.msl`, `.msr`,
      `.mcs`, CLI reports/exit codes, diagnostic codes, or signatures. — Checked; the freeze is docs-only and no
      format file changes, but the “no diagnostic codes” claim conflicts with M-1.
- [x] Architecture and module boundaries — `01-target-architecture.md` §3.2 (App/ViewModels/Views ownership,
      the I/O boundary, one writer per project, visibility), §3.1, and the UI-thread row in §5.3. — Checked;
      `IRecoveryStore` in ViewModels + `RecoveryStore` in App matches the architecture, and the UI-thread rules
      match §5.3.
- [x] ADRs and open decisions — ADR 0003/0006/0008, the resolved 23B rows in `05-open-decisions.md`, and the
      class acceptance gate. — Checked; ADR 0003’s no-new-control rule is honoured and row “23B complexity
      class” records the start-gate decision.
- [x] Quality strategy and test plan — `03-quality-strategy.md` §6/§8/§11.1 and the page's §13 test table with
      tiers, including the `Load`-stage fixtures and the no-real-sleep rule. — Checked; tiers, real-object
      rules, `ManualTimeProvider`, and the no-real-sleep rule agree; m-6, n-4.
- [x] Parity matrix and documentation plan — the 23B rows and the §12 documentation column. — Checked; the
      parity rows now say “no untitled-project model” and the docs column covers the 23A pages.
- [x] Licensing and provenance — no new product or test package; the 23A ANGLE exception is unchanged; no GPL
      code or data. — Checked; the docs-only diff adds no package and the ANGLE exception is untouched.
- [x] Post-implementation audit plan — implementation-status block, feature log, `closeout/step-23b.json`, the
      platform gate, and the audit request. — verify the landed `step-NN[a-z]` checker. — Checked; the checker
      accepts `^step-\d{2}[a-z]?$` and its tests cover `step-23a`/`step-23b`; n-5 for the dashboard.

### Step-specific decisions

- [x] **Split boundary.** 23B contains features 1–4 (edit, validation, dirty/autosave/recovery, tests/docs/
      closeout) and no 23A task; 23A contains no 23B task. Verify the feature table and §11 seam-edit list
      against the 23A page and delivered code. — Verified: 23A’s page/audit show no 23B type and 23B adds no
      lifecycle beyond §11; m-1 lists seams §11 omits.
- [x] **Retained validation result.** 23A performs exactly one internal load-validation pass and stores it as
      `ProjectSession.Validation`; 23B starts from that result and does not run the pipeline again on open.
      Verify this is consistent with the 23A feature-4 deviation and Step 07 §8, and that the clean/migration
      `Load`-stage fixtures still hold. — Verified in `ProjectSession.CreateFromLoad` and the pipeline; clean
      load is `Skipped`. m-7 covers recovery.
- [x] **Edit validation boundary.** `ProjectEditor.Apply` uses the structural `ProjectValidator`, not the full
      pipeline; a structurally valid but cross-domain-invalid edit is accepted and reported by the validation
      surface. Verify the design does not claim that a successful `Apply` means a fully valid model. — Verified;
      the page says so explicitly. n-1: `ProjectValidator` also runs the per-domain model validators.
- [x] **Save point.** The save calls `MarkSaved()` (no-arg) and does not install the store's returned
      `modifiedUtc`; the four save-point consequences and the in-memory timestamp display are documented. Verify
      the redo-after-save trace and that the 23A `MarkSaved(savedProject)` call is changed. — Verified: 23A calls
      `MarkSaved(result.SavedProject!)` at `ProjectSessionService` lines 220 and 270, and `ComputeDocument`
      includes `modifiedUtc`. m-5 covers the recovery comparison.
- [x] **Undo depth.** `MaxUndoDepth` is init-only, default `0` (unbounded, preserving Step 01); the GUI sets
      200; negative values are rejected. Verify the property is additive, the GUI construction sets 200, and
      the boundary/unbounded cases are tested. — Verified in §2/§10/§13; `MaxUndoDepth` is absent from the
      current `ProjectDocument` (additive). m-9 asks where negative values are rejected.
- [x] **No untitled-project model.** 23A's New writes the file immediately; 23B introduces no untitled session
      and uses `recovery/` only for crash snapshots/manifest-backed fallbacks. Verify this reconciles the
      superseded unsplit wording and the parity/open-decision rows. — Verified in the page, parity row
      “Autosave/recovery”, and open-decision row “Autosave and recovery scope”.
- [x] **Recovery sources.** The sidecar is primary; `recovery/` is the location-independent crash/fallback
      snapshot with a manifest. `IRecoveryStore` + `CrashRecoveryWriter` are the types; no `IRecoveryWriter` and
      no new `ProjectStore` overload. Verify `WriteAutosaveAsync` is sufficient for the recovery base path. —
      Verified: `WriteAutosaveAsync` derives `<path>.autosave` and `AppPaths.RecoveryDirectory` is created. m-4:
      `RecoverySource` has no sidecar member.
- [x] **Crash path.** Async writes with a 2 s budget, per-session results, no sync-over-async; the dead-UI path
      writes no recovery files and is recorded as a limitation. Verify the dialog/handler flow is implementable
      with the 23A crash boundary. — Verified against `CrashHandler`/`CrashDialogViewModel`; the dead-UI path is
      `ReportFatal`. n-3 on the budget wording.
- [x] **Settings additions.** `autosave`/`session` are additive, capped/normalized, and need no schema bump; a
      23A document still loads. Verify the source-generated context and validator coverage. — Verified:
      `AppSettings`/`AppSettingsValidator`/unknown-key policy are compatible; `"problems"` already round-trips
      in the 23A test. m-1 covers the unlisted settings-write seams.
- [x] **Shell seams.** The console channel selector, the settings dialog's autosave control, the dirty marker,
      and the project-provider metadata mapping are bounded seam fills, not a shell redesign. Verify no region,
      command-model, or lifecycle change is hidden. — Checked; m-1 lists required shell/session edits §11 omits.
- [x] **Complexity class.** The seven-dimension table, the High class and its normative justification, the
      one-unit case, the no-further-split conclusion, and the guard. Verify owner acceptance is required and the
      class is the highest dimension, not an average. — Verified against roadmap §3.1; m-1 notes the guard’s
      seam list is incomplete.

## Dependency and interface checks

- [x] Step 23A is `Done`, merged, and tagged; `ProjectSession`/`ProjectDocument`/`ProjectSessionService`/
      `LoadDiagnostics`/`Validation`, the document model, navigation/explorer/console/status seams, settings,
      crash boundary, and harness are stable. Verify the actual signatures the design relies on. — Verified
      against the delivered types: `ProjectSession.Validation`/`LoadDiagnostics`/`LastWrittenUtc`, the
      `ProjectDocument` API, `IProjectSessionService`, `IShellLauncher`, `IStatusBarItemProvider`,
      `IExplorerNodeProvider`, `IConsoleChannelProvider`, `IShellLifetime`, and the 23A `step-23a` tag.
- [x] Step 01's `ProjectDocument` (`Apply`/`Undo`/`Redo`/`MarkSaved`/`IsAutosaveDue`/`MarkAutosaved`) and
      `ProjectStore` (`WriteAutosaveAsync`/`DeleteAutosave`/`LoadAutosaveAsync`/`GetAutosavePath`) support the
      design without a second stack or a second autosave API. — Verified; `WriteAutosaveAsync(project, path)`
      derives `<path>.autosave`, so the recovery base path works without an overload.
- [x] Step 07's validation contract is consumed correctly: `ProjectValidationPipeline.Validate` signature,
      `ValidationOptions.Default`, `ValidationResult.DocumentHash`, `Load`-stage semantics, and the retained
      23A result. Verify no second validator, catalog, or duplicate validation. — Verified; `Default` has all
      flags false and the clean-load `Load` stage is `Skipped`.
- [x] Steps 03–06 and 12 are `Done` and their editing/template APIs support the metadata edit and project path
      behaviour without a new API. — Spot-checked; 23B uses no new domain/template API.
- [x] Steps 13–22 are not required: no design element depends on their types, packages, or behaviour. —
      Verified; the page names no solver/results/analysis type.
- [x] Format, versioning, and migration impact is confined to the app-local settings document and the app-local
      recovery manifest; the project, package, results, and state formats and the contract manifest are
      untouched. — Verified; the freeze diff touches docs only and `contracts/manifest.json` is unchanged.
- [x] The `step-NN[a-z]` closeout-evidence checker and process parser support `step-23b` on `main`; the design
      does not claim an unattainable closeout gate. — Verified: `STEP_RE = ^step-\d{2}[a-z]?$` and its tests
      include `step-23a`/`step-23b`; `status.md` has a `23b` row.

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable — checked; m-3, m-6.
- [x] Complexity class and split decision are recorded and justified per
      [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers) — checked; High is the highest
      dimension and the one-unit case is sound.
- [x] Design decisions are complete and internally consistent — checked; M-1 and m-1–m-10.
- [x] Required tests, property tests, golden fixtures, and performance targets are defined — checked; m-6, n-4.
- [x] Documentation, status, parity, and audit updates are planned — checked; n-5.
- [x] Licensing, provenance, and dependency checks are resolved — checked; no new package.
- [x] Risks and mitigations are realistic — checked; m-1, m-3, m-5.
- [x] Code layout and namespace plan follow the structure policy — checked; the §8 folders match
      `code-structure.md` §2.3.
- [x] No hidden scope, stale placeholders, or unowned decisions — checked; m-1.

## Evidence

Reviewed the frozen design at `f629dd514f5e4e3c83584aff468969545c407b5c`; record HEAD
`5358ce0f883ea687c6984b12d50529ddebe77ee4` (the request's documentation-only follow-up that fills the
`Reviewed commit` cell and the freeze note). `git diff --name-status f629dd5 5358ce0` is exactly
`M docs/planning/reviews/step-23b.md`; `git show --stat f629dd5` is the step page only (+4/−2); the full freeze
delta `git diff --stat 63cdd06 f629dd5` is ten documentation files and no code, test, configuration, contract,
generated, or status-dashboard file. The worktree was clean before this record edit.

Files reviewed:

- `AGENTS.md` §3, §8–§12; `docs/planning/reviews/README.md`; `docs/planning/audits/README.md`;
  `docs/planning/closeout/README.md`, `_template.json`, and `tools/check_closeout_evidence.py` with
  `tools/tests/test_closeout_evidence.py`.
- The 23B request record and step page (all sections), the 23A step page, the closed `step-23.md` review and
  the unsplit page, and the 23A review/audit records including the accepted R3-4/R3-5 notes.
- `04-roadmap.md` §3.1/§3/§5, `01-target-architecture.md` §3.2/§5.3, `03-quality-strategy.md` §6/§8/§11.1,
  `05-open-decisions.md`, `code-structure.md` §2.3, `appendix-b-parity-matrix.md`, `status.md`,
  `docs/reference/diagnostics.md`, and `docs/reference/file-layout.md`.
- ADR 0003 and ADR 0006.
- Delivered code: `Mocassin.Core.Editing.ProjectDocument`, `ProjectEditor`, `ProjectEditor` partials,
  `ProjectEditResult`, `ProjectChange`, `Mocassin.Core.Project.ProjectValidator`, `ProjectHasher`,
  `ProjectCanonicalizer`, `Project`, `ProjectMetadata`, `ProjectDiagnosticCodes`, `Diagnostic`;
  `Mocassin.Packages.Projects.ProjectStore`/`ProjectStoreOptions`/`ProjectStoreResults`/`ProjectFileLock` and
  `Validation/ProjectValidationPipeline`/`ValidationOptions`/`ValidationResult`;
  `Mocassin.App.ViewModels.Projects.ProjectSession`/`ProjectSessionService`/`IProjectSessionService`/
  `ProjectSessionResult`/`ProjectSessionOutcome`/`ProjectWorkspaceEntry`/`IProjectWorkspace`;
  `Documents.DocumentViewModel`/`ProjectOverviewViewModel`/`ProjectJsonViewModel`; `Shell.StatusBarItemViewModel`/
  `IShellLauncher`/`ShellViewModel`; `Consoles.ConsoleViewModel`/`IConsoleChannelProvider`;
  `Navigation.ProjectNavigationTargetProvider`; `Dialogs.CrashDialogViewModel`/`CrashDialogRequest`;
  `Settings.AppSettings`/`AppSettingsValidator`/`AppSettingsJsonReader`/`AppSettingsJsonContext`;
  `Mocassin.App.Startup.CrashHandler`; `Composition.AppPaths`/`AppServiceRegistration`; and the 23A tests
  `ProjectSessionServiceTests`, `ProjectDocumentTests`, `DocumentCollectionTests`, `AppHostTests`,
  `AppSettingsReaderTests`.

Commands run (serially, as requested):

- `./tools/verify.sh docs --require` — pass; final line "Closeout gate completed with required checks enabled."
  (strict Zensical build, DocFX, rustdoc).
- `./tools/verify.sh policy --require` — pass; "Public API is compatible with the checked-in baseline";
  ApiCompat for the checked App assemblies; contract artifacts current (26 files); process consistency OK;
  60 tool tests OK; SPDX headers in 1002 source files; no secret patterns in 1479 tracked files; cargo-deny
  advisories/bans/licenses/sources OK; final line "Closeout gate completed with required checks enabled."
- `python tools/check_doc_math.py` — pass; "Documentation math conventions OK (191 files scanned)."
- `git status --porcelain` — empty before this record edit.
- `git diff --name-status f629dd5 5358ce0` — `M docs/planning/reviews/step-23b.md` only.
- `git diff --stat 63cdd06 f629dd5` — ten docs files, no code; `git tag --list 'step-23*'` — `step-23a`
  only; `git ls-remote --tags origin 'step-23*'` — `step-23a` only.
- `STEP_RE` inspection: `^step-\d{2}[a-z]?$`; `tools/tests/test_closeout_evidence.py` includes valid
  `step-23a`/`step-23b` and invalid `step-23aa`/`step-23A`; `check_process_consistency.py` uses
  `\d{2}[a-z]?`; `status.md` has a `23b` row.
- `grep`/read inspection of the 23A save calls (`MarkSaved(result.SavedProject!)` at
  `ProjectSessionService.cs:220,270`), `ProjectDocument.MarkSaved()`/`MarkSaved(Project)`,
  `ProjectHasher.ComputeDocument` (`ProjectCanonicalizer` includes `modifiedUtc`), `ProjectStore.SaveAsync`
  (`WithModifiedUtc`), `WriteAutosaveAsync`/`GetAutosavePath`, `AppPaths.RecoveryDirectory` and
  `EnsureDirectories`, `ProjectDiagnosticCodes` (no read-only/edit code), `ProjectValidator` (calls the six
  domain validators), and `ProjectValidationPipeline` (`Load` `Skipped` with no load diagnostics).

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M-1 — The read-only refusal is specified as a `ReadOnly` diagnostic that does not exist, contradicting the no-new-code contract.** §2 says `CanEdit` is false and `Apply` “returns a `ReadOnly` diagnostic”; `ProjectEditResult` carries `Diagnostic` values, and every `Diagnostic` requires a stable non-empty code. `ProjectDiagnosticCodes` has no read-only/edit code, and the page's non-goal and §12 docs plan say no diagnostic code changes and no new codes are invented. As written, feature 1 cannot implement the refusal without adding a third Core API/code or redefining the return. Resolve by reusing `PROJ031` (`LockHeld`) with its exact existing message for a lock-held read-only session, or by returning an app-level rejection/outcome rather than a `Diagnostic`, and state it in §2/§10/§13 and the docs plan. | 23B page lines 165, 244, 524; `ProjectDiagnosticCodes.cs` (PROJ001–PROJ034, no read-only code); `Diagnostic` constructor rejects an empty code; `ProjectEditResult.cs`; docs plan line 505 (“no new codes”). |
| **Minor** | **m-1 — §11's “23A seam edits owned by 23B” list omits required 23A edits, so the step's own guard would stop it.** The design requires edits to `ShellViewModel` (title/tab/status dirty markers, unsaved prompts, restore, `CanExecute`; §2.1/§4/§6), `ProjectSessionService` on open/activate (session-block record writes; §6/§9), `WorkspaceViewModel`/`ProjectWorkspaceEntry` (project-switcher dirty marker; §4), the shell window-close path (§4), and `AppServiceRegistration` (§1); none appear in the §11 table. §16's guard says any seam beyond that list stops the step for an amendment. Add them to §11 and restate the guard. | 23B page §1/§2.1/§4/§6/§9, §11 table lines 474-486, §16 guard lines 582-584; delivered `ShellViewModel`, `ProjectSessionService` (`RecordOpenAsync`), `ProjectWorkspaceEntry`; `AppServiceRegistration`. |
| **Minor** | **m-2 — The save and rejection notification contract is underspecified.** `NotifySaved()` is named (§2/§4) but the page says only successful apply/undo/redo and `ReplaceDocument` raise `ProjectChanged`; without a save notification the shell/title/tab/status and Overview consumers cannot clear the dirty marker or refresh “last saved”. The §2.1 consumer table also omits the `EditRejected` subscriber that supplies the Overview inline error and the Problems source `edit` (§2), since a rejected edit raises no `ProjectChanged`. State the save event and the `EditRejected` consumer. | 23B page §2/§2.1/§4; event sentences lines 166-169 and 187-189; `EditRejectedEventArgs`/`ProjectChangedEventArgs` in §2. |
| **Minor** | **m-3 — Explicit discard paths do not delete the autosave sidecar.** The page deletes the sidecar only on a successful save (§5) and on `DiscardRecoveryAsync` (§6); closing with “Discard” in the Save/Discard/Cancel prompt and confirming “Reload and discard changes” leave the sidecar, so the next open can offer the explicitly discarded edits as recovery. State the deletion (and test it) or record the re-offer as intentional. | 23B page §4 lines 255-266, §5 line 285-286, §6 `DiscardRecoveryAsync`; the acceptance criteria only cover “deletes its file on save”. |
| **Minor** | **m-4 — `RecoverySource` cannot represent the primary sidecar candidate.** The enum has `AutosaveFallback` and `Crash` only, while §6 says the startup offer is composed from the `.autosave` sidecar and the `recovery/` manifest candidate (“newest valid wins”) and passed to the same operations. Add a sidecar source (or a separate candidate type) so the primary source is representable, and test the composition. | 23B page §6 lines 303-310, 334-348. |
| **Minor** | **m-5 — The recovery comparison sees a timestamp-only difference after the save-point decision.** Because a save keeps the old in-memory `modifiedUtc` (§2.2) and `ProjectHasher.ComputeDocument` includes it, a sidecar written after a save and then undone back to the save point differs from the saved file only by `modifiedUtc`; §6's “canonical content differs from the saved file” then offers a spurious recovery and a “dirty” recovered document. Compare against the counterpart last-saved value or a timestamp-normalized projection, and test save → edit → autosave → undo-to-clean → reopen. | 23B page §2.2 lines 193-207, §6 lines 340-348; `ProjectDocument.MarkSaved()`; `ProjectHasher.ComputeDocument`/`ProjectCanonicalizer` includes `modifiedUtc`. |
| **Minor** | **m-6 — The statistics trigger conflicts with the no-second-validation contract.** §3 says the Overview “requests `IncludeStatistics = true` in the background” and caches by document hash; the Overview is a default document, so an automatic request would run the pipeline a second time on open, while §3/§13 and the acceptance criteria forbid a second validation on open. State the explicit trigger (panel/command/after first edit) and test that opening alone starts no run. | 23B page §3 lines 241-243, §13 line 520, acceptance line 649; feature table line 45 says “on-demand”. |
| **Minor** | **m-7 — Recovery leaves the retained validation result stale.** After `RecoverAsync`, the document is the recovered snapshot but `ProjectSession.Validation` still describes the saved file; §2.1's “re-seeded from the replacement session state” would show the file's Problems until an edit. State that recovery clears or recomputes the retained result (a recovery run is not the forbidden open-time second validation) and add a test. | 23B page §2.1 lines 187-189, §6 lines 344-348; `ProjectSession.Validation` retained from the load. |
| **Minor** | **m-8 — `ProjectEditCoordinator.Document` is declared `{ get; }` but `ReplaceDocument` must replace it.** Clarify `{ get; private set; }` or a backing field, and state whether `ReplaceDocument` also resets `CanEdit`/read-only state and the cached dirty value. | 23B page §2 snippet lines 130-145; `ReplaceDocument` described at lines 187-189. |
| **Minor** | **m-9 — `MaxUndoDepth` negative rejection is unspecified.** The property is init-only, but §10 says “negative values are rejected when an edit is applied”; this leaves an invalid document constructible and a failure that is neither a diagnostic nor a named exception. State the rejection point (init accessor/constructor throw) and exception, or define the apply-time failure; test negative and 0. | 23B page §10 line 453, §13 line 522. |
| **Minor** | **m-10 — Autosave/close/reload serialization is claimed but not designed.** §5 says a per-session semaphore serializes save/autosave and the tick “never races a close”, but `CloseAsync`/`ReloadAsync` are not stated to take the semaphore and `ProjectSession` has no closing state. State how close/reload waits for or skips an in-flight autosave, and whether the semaphore is acquired in `CloseAsync`/`ReloadAsync`. | 23B page §5 lines 289-290, §7 line 393; delivered `ProjectSessionService.CloseAsync`/`ReloadAsync`; `ProjectSession` has no semaphore/closing flag yet. |
| **Note** | **n-1 — `ProjectValidator` is broader than the §2 parenthetical.** It also calls `ParticleModelValidator`, `StructureModelValidator`, `TransitionModelValidator`, `EnergyModelValidator`, `LatticeModelValidator`, and `SimulationModelValidator`. The important distinction (not the full pipeline; cross-domain/feasibility errors are accepted and reported) is correct, but §2 should not imply that only metadata/references are checked. | 23B page §2 lines 153-157; `ProjectValidator.Validate`. |
| **Note** | **n-2 — Recovery manifest/snapshot bounds are named but not quantified.** §6 says the store bounds the manifest size and entry count; name the caps (and the inherited 64 MiB snapshot cap) so the untrusted-input test can assert them. | 23B page §6 lines 374-375. |
| **Note** | **n-3 — The crash-budget wording is not a valid API pairing.** “`Task.WhenAny` against a `CancellationTokenSource`” — `Task.WhenAny` takes tasks; say `Task.WhenAny(writes, Task.Delay(budget, timeProvider, cts.Token))` (or similar) and name the timeout cancellation behaviour. | 23B page §6 lines 359-364. |
| **Note** | **n-4 — The help-anchor drift check is asserted but unnamed.** The test plan covers an unknown code (unlinked) but not a known code's resolved URL/launch; `DiagnosticHelpLinks` is hand-maintained and the risk table says it is “checked against the documented code list” without naming the check. Add a pure map test (and a launcher test) so the map cannot drift silently. | 23B page §3 lines 233-236, §13 line 525, §15 line 556. |
| **Note** | **n-5 — `status.md` row 23b still says design review “Not requested”** while the design is frozen and this review is in progress. The planner may not edit step status; record it as the step-agent start-gate bookkeeping (same as 23A n-1). | `status.md` row 23b; 23B page §18 line 624. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Accepted — Ready with notes (2026-11-21); the record does not mark implementation started. The human
owner accepted the round-4 verdict at design freeze `a33711dbf9fb35bd782f5bd88eba111e461634d6` (record HEAD
`c4ded0ecb8ee5e6df989918d63ec255b487022d1`), accepted **m-14** as a recorded limitation, and assigned
**n-11–n-13** to the step agent's start/status commit. All reviewer text and rounds are unchanged.

`Ready with notes` is the accepted design verdict; it is not an implementation-start record and does not create
a step branch.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

Follow-up owner: planner/step agent — resolve M-1 (read-only refusal representation) and the Minors, then
re-freeze and request a re-review.
Follow-up step: 23B design re-review.

## Approval summary

Not applicable at the design-start gate. AGENTS §3.9 step 7 states that the cumulative approval report is for a
merge, a tag, or a Blocker/Major waiver, not for this gate. The section is kept so the record matches the
template; it is completed before any later approval request that cites this review.

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
| 1 | 2026-11-21 | `f629dd5` | DeepSeek V4.1 Flash 1017 (fresh planning auditor) | Changes required |
| 2 | 2026-11-21 | `022132e` | DeepSeek V4.1 Flash 1017 (fresh planning auditor) | Changes required |
| 3 | 2026-11-21 | `a55fd61` | Owner-relayed findings (no reviewer record committed) | Changes required (m-13/n-10) |
| 4 | 2026-11-21 | `a33711d` | DeepSeek V4.1 Flash 1017 (fresh planning auditor) | Ready with notes — accepted by the human owner 2026-11-21 |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| M-1 | Major | `ReadOnly` diagnostic did not exist | App-level `EditApplyOutcome.ReadOnly`; no code/`PROJ031` reuse | `022132e` | Fixed |
| m-1 | Minor | §11 seam list incomplete | Shell/session/workspace/close/registration seams added; `StatusBarItemViewModel` added in round 3 | `022132e`/`a55fd61` | Fixed |
| m-2 | Minor | `NotifySaved`/`EditRejected` contract incomplete | `ProjectChangeKind` and `EditRejected` consumer added | `022132e` | Fixed |
| m-3 | Minor | Discard paths kept the sidecar | Discard-close and discard-reload delete it | `022132e` | Fixed |
| m-4 | Minor | `RecoverySource` lacked the sidecar | `Sidecar` added and composed | `022132e` | Fixed |
| m-5 | Minor | Timestamp-only recovery offer | `RecoveryComparison` normalizes `ModifiedUtc`; round 3 uses `IsDirty` and deletes on return-to-clean | `022132e`/`a55fd61` | Fixed |
| m-6 | Minor | Automatic statistics run | Statistics are an explicit user action | `022132e` | Fixed |
| m-7 | Minor | Recovery left validation stale | Recovery recomputes `Validation`; round 3 adds `LoadDiagnostics`/ordering | `022132e`/`a55fd61` | Fixed |
| m-8 | Minor | `Document` get-only vs `ReplaceDocument` | Private setter and reset contract stated | `022132e` | Fixed |
| m-9 | Minor | Negative `MaxUndoDepth` unspecified | Init accessor throws `ArgumentOutOfRangeException` | `022132e` | Fixed |
| m-10 | Minor | Close/reload gate unspecified | Session gate/`IsClosing`; round 3 adds the `UnsavedChanges`/Cancel reset | `022132e`/`a55fd61` | Fixed |
| m-11 | Minor | `StatusBarItemViewModel` seam omitted | Added to §11 | `a55fd61` | Fixed |
| m-12 | Minor | Return-to-clean still left the sidecar | Sidecar cleanup on return-to-clean/clean close; explicit timestamp-only test | `a55fd61` | Fixed |
| n-1 | Note | `ProjectValidator` breadth | Six per-domain validators named | `022132e` | Fixed in design |
| n-2 | Note | Manifest/snapshot caps unnamed | 64 KiB/100 entries and 64 MiB named | `022132e` | Fixed in design |
| n-3 | Note | `Task.WhenAny`/CTS wording | `Task.WhenAny(writes, Task.Delay(...))` and cancel stated | `022132e` | Fixed in design |
| n-4 | Note | Help-anchor drift check unnamed | Pure map test and launcher test named | `022132e` | Fixed in design |
| n-5 | Note | `status.md` 23b design-review cell | Step-agent-owned; recorded in §18 | — | Deferred |
| M-2 | Major | ViewModels `RecoveryComparison` called `ProjectHasher` | Uses `ProjectDocument.CreateRecovered(...).IsDirty`; no ViewModels hash/App contract | `a55fd61` | Fixed |
| n-6 | Note | Recovery did not update `LoadDiagnostics` | Added to recovery flow | `a55fd61` | Fixed in design |
| n-7 | Note | `Validation` assigned after `Replaced` | Assign `LoadDiagnostics`/`Validation` before `ReplaceDocument` | `a55fd61` | Fixed in design |
| n-8 | Note | `IsClosing` not cleared on `UnsavedChanges`/Cancel | Set only after the guard; clear on non-closing returns | `a55fd61` | Fixed in design |
| n-9 | Note | No numbered owner-question list | §17.1 lists seven questions | `a55fd61` | Fixed in design |
| m-13 | Minor | Clean-close cleanup could delete a deferred recovery candidate | Cleanup skipped while `PendingRecovery` is non-null; first edit clears it | `a33711d` | Fixed |
| n-10 | Note | §2.1 omitted the cleanup handler/lifetime | Cleanup handler row added; ignores `Saved`/`Replaced`, no gate re-entry | `a33711d` | Fixed in design |
| m-14 | Minor | Deferred `recovery/` candidate still swept after 7 days | Accepted as a recorded limitation; the preservation claim is narrowed to `.autosave` sidecars only | `c4ded0e` | Accepted limitation |
| n-11 | Note | §11/§13 summaries lacked the pending-candidate exception and recovery clearing | Assigned to the step agent at start | — | Assigned |
| n-12 | Note | First-edit clear should key on a real content change | Assigned to the step agent at start | — | Assigned |
| n-13 | Note | Step-page status block stale | Assigned to the step agent at start/status commit | — | Assigned |

## Human approval

**Owner decisions recorded 2026-11-21.** The owner accepted the round-4
`Ready with notes` verdict on 2026-11-21 at design freeze `a33711d` (record HEAD `c4ded0e`) and instructed
implementation to start; n-11–n-13 are assigned to the step agent. These are scope/class and verdict-acceptance
approvals; the `Accepted:` checkbox below records the design verdict, not an implementation-start claim.

| Decision | Status |
| --- | --- |
| Class **High** as one unit, no further split | **Approved** |
| Four-feature list and order | **Approved** |
| Two additive Core APIs (`MaxUndoDepth`, `CreateRecovered`); GUI depth 200, library default `0` | **Confirmed** |
| App-level read-only refusal (`EditApplyOutcome.ReadOnly`, no `PROJ031`) | **Confirmed** |
| §11 seam fills, including the console selector, autosave control, and status glyph/severity | **Confirmed** |
| No untitled-project model; `recovery/` is the crash/fallback snapshot store | **Confirmed** |
| Missing-file and dead-UI recovery limitations | **Accepted** |
| Fix m-13/n-10, refreeze, and request round 4 | **Instructed** |
| Accept **m-14** as a recorded limitation: seven-day `recovery/` retention; only `.autosave` sidecars are sweep-exempt; the “preserved until Recover or Discard” claim is narrowed accordingly | **Accepted** |
| Fix **n-11–n-13** at the step agent's start/status commit | **Assigned** |
| Accept the round-4 verdict and **start implementation now** | **Accepted / Instructed** |

- [ ] Accepted: Ready for implementation
- [x] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by: Human owner
Date: 2026-11-21

## Resolution status

| Finding | Status | Owner | Commit | Resolution |
| --- | --- | --- | --- | --- |
| **M-1** | Fixed in design | Planner | `022132e` | Chose the app-level option: `ProjectEditCoordinator.Apply` returns `EditApplyOutcome.ReadOnly` with an app-owned message and no `Diagnostic`; `PROJ031` is not reused because its lock-held message describes another process's lock. `ProjectDiagnosticCodes`/messages are unchanged. §2/§10/§13 and the docs plan record it. |
| **m-1** | Fixed | Planner | `022132e`/`a55fd61` | §11 lists `ShellViewModel`, `ShellWindow`/`IShellLifetime`, `ProjectSessionService` open/activate/close/reload writes and recovery detection, `WorkspaceViewModel`/`ProjectWorkspaceEntry`, `ProjectsRegionView`/`DocumentsRegionView`, `AppSettings`/validator/context, `AppServiceRegistration`/`App.axaml.cs`, and (round 3) `StatusBarItemViewModel`/`StatusBarView`. §16's guard now matches the list. |
| **m-2** | Fixed | Planner | `022132e` | `ProjectChangedEventArgs` carries `ProjectChangeKind`; `NotifySaved()` raises `Kind = Saved`; `ReplaceDocument` raises `Kind = Replaced`; the §2.1 table adds the `EditRejected` consumers (`ProjectOverviewViewModel`, `ProblemsChannelViewModel`). |
| **m-3** | Fixed | Planner | `022132e` | `CloseAsync(discardChanges: true)` and `ReloadAsync(discardChanges: true)` delete the sidecar/recovery snapshot before closing/reloading; the acceptance criterion and test row cover discard-close and discard-reload. |
| **m-4** | Fixed | Planner | `022132e` | `RecoverySource` now has `Sidecar`; the sidecar candidate participates in the newest-valid-wins composition and the test asserts it. |
| **m-5** | Fixed | Planner | `022132e`/`a55fd61` | `RecoveryComparison` normalizes `Metadata.ModifiedUtc`; round 3 replaces the `ProjectHasher` call with `ProjectDocument.CreateRecovered(...).IsDirty`, and deletes the sidecar on return to the save point (m-12). The save → edit → autosave → undo-to-clean → reopen and explicit timestamp-only tests are added. |
| **m-6** | Fixed | Planner | `022132e` | Statistics are an explicit Overview toggle/command, never an automatic open request; the test asserts opening starts no statistics run and the toggle runs one cached `IncludeStatistics = true` run. |
| **m-7** | Fixed | Planner | `022132e`/`a55fd61` | `RecoverAsync` reloads the candidate snapshot and recomputes `ProjectSession.Validation`; round 3 updates `LoadDiagnostics` too and assigns both before `ReplaceDocument` raises `Replaced` (n-6/n-7). |
| **m-8** | Fixed | Planner | `022132e` | `Document { get; private set; }`; `ReplaceDocument(document, canEdit)` resets `CanEdit`, stacks, cached dirty state, raises `PropertyChanged`, and raises `Kind = Replaced`. |
| **m-9** | Fixed | Planner | `022132e` | The `MaxUndoDepth` init accessor throws `ArgumentOutOfRangeException` for a negative value; `0` is unbounded; the test constructs `-1` and `0`. |
| **m-10** | Fixed | Planner | `022132e`/`a55fd61` | `ProjectSession` owns the gate and `IsClosing`; autosave uses non-blocking `Wait(0)` and skips busy/closing sessions; save/close/reload/recovery await the gate; round 3 states that `IsClosing` is set only after the guard and cleared on `UnsavedChanges`/Cancel (n-8). |
| **n-1** | Fixed in design | Planner | `022132e` | §2 names the six per-domain validators and keeps the cross-domain/feasibility distinction. |
| **n-2** | Fixed in design | Planner | `022132e` | Manifest 64 KiB/100 entries, snapshot 64 MiB; the untrusted-input test asserts them. |
| **n-3** | Fixed in design | Planner | `022132e` | The budget is `Task.WhenAny(allWrites, Task.Delay(budget, timeProvider, cts.Token))`; a timeout cancels the writes and reports them. |
| **n-4** | Fixed in design | Planner | `022132e` | A pure map test checks every `DiagnosticCatalog.Default` code against the documented anchors (or the explicit unlinked set); a launcher test asserts the URI. |
| **n-5** | Fixed (step start) | Step agent | Start commit | `status.md` now records the `23b` row as `In progress` with the step branch and the accepted `Ready with notes` design review at `a33711d`/`c4ded0e`. |
| **M-2** | Fixed in design | Planner | `a55fd61` | Chose the `IsDirty` option: `RecoveryComparison` normalizes `ModifiedUtc` and reads `ProjectDocument.CreateRecovered(...).IsDirty`; no `ProjectHasher` call in `Mocassin.App.ViewModels`, no App contract, and no third Core API. §2/§6/§11, `01` §3.2, `05`, and the test row state it. |
| **m-11** | Fixed | Planner | `a55fd61` | §11 adds `StatusBarItemViewModel`/`StatusBarView` (optional glyph/severity; existing text/tooltip/command remains). |
| **m-12** | Fixed | Planner | `a55fd61` | Sidecar/recovery cleanup runs when a content change returns the document to the save point and on a clean close, through the session gate; the save→edit→autosave→undo-to-clean→reopen test and an explicit timestamp-only snapshot test are added. |
| **n-6** | Fixed in design | Planner | `a55fd61` | Recovery assigns the snapshot load's `ProjectSession.LoadDiagnostics` as well as `Validation`. |
| **n-7** | Fixed in design | Planner | `a55fd61` | `LoadDiagnostics`/`Validation` are assigned before `ReplaceDocument` raises `Replaced`, so the validation coordinator seeds the recovered result. |
| **n-8** | Fixed in design | Planner | `a55fd61` | `CloseAsync` sets `IsClosing` only after the `discardChanges` guard and clears it on `UnsavedChanges`/Cancel; shell Cancel leaves autosave running. |
| **n-9** | Fixed in design | Planner | `a55fd61` | §17.1 lists the seven start-gate owner questions explicitly. |
| **m-13** | Fixed | Planner | `a33711d` | Clean-close and return-to-clean cleanup is skipped while `PendingRecovery` is non-null; the candidate survives close and is re-offered. The first accepted edit clears `PendingRecovery`, after which cleanup removes the sidecar. A save with a pending candidate preserves it. §4/§5/§11/§13 and `05-open-decisions` are aligned; the test covers close-preserve, first-edit-clear, and later cleanup. |
| **n-10** | Fixed in design | Planner | `a33711d` | §2.1 adds the `ProjectSessionService` cleanup consumer with session lifetime. It handles `Edited`/`Undo`/`Redo`, ignores `Saved`/`Replaced` (raised while the non-reentrant gate is held), clears `PendingRecovery` on the first edit, and queues cleanup through the gate without blocking the event thread. The test asserts no deadlock/re-entry. |
| **m-14** | Accepted limitation | Human owner | `c4ded0e` | The 7-day `recovery/` retention sweep can still delete a deferred crash/fallback candidate before the next scan. Accepted as a recorded limitation: only `.autosave` sidecars are sweep-exempt; a deferred `recovery/` candidate can expire. §4/§5/§6/§13 and `05-open-decisions` narrow the “preserved until Recover or Discard” claim accordingly. |
| **n-11** | Fixed (step start) | Step agent | Start commit | §11 now states that a successful save deletes the sidecar/recovery file only when no deferred `PendingRecovery` candidate is pending, and that `RecoverAsync`/`DiscardRecoveryAsync` consume and clear `PendingRecovery`. §6 and the §13 Dirty/autosave and Recovery rows carry the same pending-candidate exception. |
| **n-12** | Fixed (step start) | Step agent | Start commit | §2.1, §11, §13, §15, and the acceptance criterion now key the `PendingRecovery` clear and cleanup on a real accepted content change (`EditApplyResult.Change` non-null / project value differs); a no-op same-hash `Apply` must not clear the candidate. |
| **n-13** | Fixed (step start) | Step agent | Start commit | The step page's top `Status` and the implementation-status design-review bullet now record the accepted round-4 verdict, the step branch, and the start state; the stale "not design-reviewed"/"round 2" text is gone. |

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

I am a fresh context. I did not author, design, or implement the reviewed work and have no shared conversation
with the planner, implementor, the earlier Step 23/23A reviewers, or the orchestrator. I received only the frozen
commit `f629dd514f5e4e3c83584aff468969545c407b5c`, record HEAD `5358ce0f883ea687c6984b12d50529ddebe77ee4`, the
request record, repository links, and the required commands/scope; no desired verdict or summary was supplied.

The design under review is `f629dd5` (the step page +4/−2 for the session-depth clarification). The record HEAD
`5358ce0` is the request's documentation-only follow-up that fills the `Reviewed commit` cell and freeze note;
`git diff --name-status f629dd5 5358ce0` is exactly `M docs/planning/reviews/step-23b.md`, so the record commit
is the only delta and I record the delta as a note per the review README §8.1. The freeze delta from `main`
(`63cdd06`) is ten documentation files; no code, test, configuration, contract, generated, or status-dashboard
file changed.

The 23A deviations named in the request are respected: no `Avalonia.Headless.XUnit`/xunit v3 package, the ANGLE
exception unchanged, `internal` + `InternalsVisibleTo`, no drag-reorder, Xvfb screenshots Ubuntu-only PR-medium,
`ProjectPathRules`/`ExistingTarget` unchanged, the single 23A load-validation pass consumed (not repeated), the
23A crash dialog gaining only the recovery action, and `LastWrittenUtc` used instead of a saved-project property.

#### Commands and results

- `./tools/verify.sh docs --require` — pass; final line "Closeout gate completed with required checks enabled."
  (strict Zensical build, DocFX, rustdoc).
- `./tools/verify.sh policy --require` — pass; "Public API is compatible with the checked-in baseline";
  ApiCompat for the checked App assemblies; contract artifacts current (26 files); process consistency OK;
  60 tool tests OK; SPDX headers in 1002 source files; no secret patterns in 1479 tracked files; cargo-deny
  advisories/bans/licenses/sources OK.
- `python tools/check_doc_math.py` — pass; "Documentation math conventions OK (191 files scanned)."
- `git status --porcelain` — empty before this record edit; `git diff --name-status f629dd5 5358ce0` — the
  record only; `git diff --stat 63cdd06 f629dd5` — ten docs files, no code.
- `git tag --list 'step-23*'` and `git ls-remote --tags origin 'step-23*'` — `step-23a` only (the `step-23a`
  tag points to the 23A merge/tag commits); no `step-23` alias.
- `STEP_RE = ^step-\d{2}[a-z]?$`; `tools/tests/test_closeout_evidence.py` accepts `step-23a`/`step-23b` and
  rejects `step-23aa`/`step-23A`; `check_process_consistency.py` uses `\d{2}[a-z]?`; `status.md` has a `23b`
  row. `check_closeout_evidence.py` and the closeout README support the suffixed ID.
- Read-only inspection confirmed `ProjectSession.Validation`/`LoadDiagnostics`/`LastWrittenUtc`;
  `ProjectSession.CreateFromLoad` runs the pipeline once; `ProjectDocument.MarkSaved()`/`MarkSaved(Project)`;
  `ProjectSessionService` calls `MarkSaved(result.SavedProject!)` at lines 220/270 and replaces the document on
  reload; `ProjectHasher.ComputeDocument`/`ProjectCanonicalizer` include `modifiedUtc`; `ProjectStore.SaveAsync`
  only changes `ModifiedUtc`; `WriteAutosaveAsync`/`GetAutosavePath` derive `<path>.autosave`;
  `AppPaths.RecoveryDirectory` is created; `ProjectValidationPipeline.Validate` has the stated signature and
  `ValidationOptions.Default` has all flags false; the `Load` stage is `Skipped` with no load diagnostics;
  `ProjectDiagnosticCodes` has no read-only/edit code; `ProjectValidator` calls the six domain validators;
  `IShellLauncher` currently has only `OpenFolderAsync`; `StatusBarItemViewModel`/`DocumentViewModel` have the
  stated shapes; `IProjectWorkspace.ActiveProjectChanged` exists; `CrashHandler`/`CrashDialogViewModel` have the
  stated crash boundary; `AppSettings`/`AppSettingsValidator` tolerate unknown keys and the `"problems"` channel
  already round-trips; `Mocassin.Core` is ApiCompat-checked and `PublicAPI.Unshipped.txt` exists.

#### Findings

**M-1 (Major).** The read-only refusal is specified as a `ReadOnly` diagnostic, but no such code/type exists and
the design forbids new or reworded codes. `ProjectEditResult` carries `Diagnostic` values, each requiring a
stable code. Resolve by reusing `PROJ031` with its exact existing message for the lock-held read-only case, or by
returning an app-level rejection/outcome instead of a `Diagnostic`, and update §2/§10/§13 and the docs plan.

**m-1 (Minor).** §11's seam-edit list omits `ShellViewModel`, the `ProjectSessionService` open/activate session
writes, `WorkspaceViewModel`/`ProjectWorkspaceEntry` dirty markers, the window-close path, and the registration
call, so the §16 guard is inconsistent with the required edits.

**m-2 (Minor).** The save notification (`NotifySaved`) and the `EditRejected` consumer are not in the
`ProjectChanged` consumer contract, leaving the dirty markers/Overview inline error without a specified event.

**m-3 (Minor).** Explicit Discard (close prompt) and “Reload and discard changes” do not delete the sidecar, so
discarded edits can be offered as recovery on the next open.

**m-4 (Minor).** `RecoverySource` lacks a sidecar member although the sidecar is the primary candidate in the
“newest valid wins” offer.

**m-5 (Minor).** The no-arg save point keeps the old in-memory `modifiedUtc`, so §6's sidecar-vs-saved-file
comparison can differ only by `modifiedUtc` after save → edit → autosave → undo-to-clean, producing a spurious
recovery offer and a timestamp-only “dirty” recovered document.

**m-6 (Minor).** The Overview's background `IncludeStatistics = true` request can run a second pipeline on open;
state the explicit trigger and test that open alone runs none.

**m-7 (Minor).** Recovery leaves the retained `ProjectSession.Validation` describing the saved file; state that
recovery clears or recomputes it.

**m-8 (Minor).** `ProjectEditCoordinator.Document` is get-only but `ReplaceDocument` must replace it; clarify the
setter/backing field and the reset state.

**m-9 (Minor).** `MaxUndoDepth` negative rejection is “when an edit is applied” for an init-only property;
specify the rejection point/exception.

**m-10 (Minor).** The autosave/close/reload serialization is claimed but `CloseAsync`/`ReloadAsync` are not
stated to take the per-session semaphore and `ProjectSession` has no closing state.

**Notes.** n-1 `ProjectValidator` breadth; n-2 manifest/snapshot caps unnamed; n-3 `Task.WhenAny` wording; n-4
help-anchor drift check unnamed; n-5 `status.md` block 23b design-review cell. Details and evidence are in the
top-level `## Findings` table.

#### Verdict

**Changes required.** The step is well scoped as its own High unit: the 23A seams match the delivered code, the
retained-validation/no-double-validation contract is correct, the save-point rationale and `MaxUndoDepth`
default are sound, recovery reuses the Step 01 APIs without a second stack or store, the settings additions are
additive with no schema bump, the inherited 23A deviations are respected, the docs/parity/dependency rows are
consistent, and no format/contract/licensing change is introduced. It cannot start as frozen because M-1 names a
diagnostic that cannot exist under the step's own no-new-code rule; the Minors also need a design update, mostly
around the seam list and the sidecar/recovery lifecycle. The human owner decides the next freeze; this round is
the record for `f629dd5`.

### Re-review round 2

#### Independence and frozen state

I am a fresh context (DeepSeek V4.1 Flash 1017, opencode-go). I did not author, design, or implement the reviewed
work and have no shared conversation with the planner, implementor, the round-1 reviewer, or the orchestrator. I
received only the frozen commit, the request record, repository links, and the required commands/scope; no
desired verdict or summary was supplied. The round-1 reviewer used the same model name in a different context; I
read only the durable record, not its conversation.

The design under review is `022132e24e38ca4b90a3df37d7f37140c4313cd1` (the round-1 resolution/refreeze). The
record HEAD `1a586beab707ad7f62b483c3f497d980a319018e` is the request's documentation-only round-1
resolution/refreeze commit; `git diff --name-status 022132e 1a586be` is exactly
`M docs/planning/reviews/step-23b.md`, so the record delta is the only later change and I record it as a note per
the review README §8.1. `git diff --name-status f629dd5 022132e` is six documentation files
(`testing-and-parity`, `01-target-architecture`, `03-quality-strategy`, `05-open-decisions`, the record, and the
step page). `git diff --name-status 63cdd06 022132e` is ten documentation files; `git diff 63cdd06 022132e --
contracts src tests tools .github` is empty. No code, test, configuration, contract, generated, or
status-dashboard file changed. `step-23a` is the only `step-23*` tag locally and on `origin`; no `step-23` alias
exists. The `### Review round 1` body text is unchanged from `3efae49` (103 content lines; the only added bytes
are the blank separator before this section and this round-2 section).

The 23A deviations named in the request are respected: first-party `Avalonia.Headless` (no
`Avalonia.Headless.XUnit`), the ANGLE exception is unchanged, `InternalsVisibleTo` covers `Mocassin.App`,
`Mocassin.App.Views`, and `Mocassin.App.Tests`, no tab drag-reorder is claimed, Xvfb evidence is the Linux
route, `ProjectPathRules`/`ExistingTarget` is unchanged, the single 23A load-validation pass is consumed and not
repeated, and `ProjectSession.LastWrittenUtc` is the only saved-time surface. The design adds no 24–28 task and
no format/contract/licensing change.

#### Commands and results

Run serially in a clean worktree at `1a586be`; `git status --porcelain` was empty before the record edit.

- `./tools/verify.sh docs --require` — **pass**; final line "Closeout gate completed with required checks
  enabled." (strict Zensical build, DocFX, rustdoc).
- `./tools/verify.sh policy --require` — **pass**; "Public API is compatible with the checked-in baseline";
  ApiCompat for the checked App assemblies; contract artifacts current (26 files); process consistency OK; 60
  tool tests OK; SPDX headers in 1002 source files; no secret patterns in 1479 tracked text files; cargo-deny
  advisories/bans/licenses/sources OK; final line "Closeout gate completed with required checks enabled."
- `python tools/check_doc_math.py` — **pass**; "Documentation math conventions OK (191 files scanned)."
- `git diff --name-status 022132e 1a586be` — `M docs/planning/reviews/step-23b.md` only.
- `git diff --name-status f629dd5 022132e` — six documentation files; `git diff --name-status 63cdd06 022132e` —
  ten documentation files.
- `git diff 63cdd06 022132e -- contracts src tests tools .github` — empty; `git tag --list 'step-23*'` and
  `git ls-remote --tags origin 'step-23*'` — `step-23a` only.
- Round-1 preservation: the `### Review round 1` content from `3efae49` and the current file is unchanged
  (103 content lines; the only diff is the blank separator before the appended round-2 section).
- Read-only inspection: `ProjectHasher` has no current use in `Mocassin.App.ViewModels`; `ProjectDocument.IsDirty`
  is the Core hash owner; `ProjectSession.LoadDiagnostics` (line 91) and `ProjectSessionService.ReloadAsync`
  (lines 290-292) update both load state fields; `StatusBarItemViewModel` has only `Text`/`ToolTip`/`Command`;
  `ProjectSessionService.CloseAsync` currently has no `discardChanges`/gate; `ProjectSessionService` uses
  `MarkSaved(result.SavedProject!)` at lines 220/270; `ProjectStore.LoadAsync`/`LoadAutosaveAsync` accept the
  sidecar and recovery paths; `ProjectDiagnosticCodes` still has no read-only/edit code.

Owner-facing decisions: the record and page contain no separately numbered "owner questions 1–7" list. The
equivalent decisions are recorded at page §16 (class High/one unit; no further split), the implementation-status
and feature table (four features), §10/§17 (two Core APIs; `MaxUndoDepth` default 0/GUI 200), §2/§17 (app-level
read-only refusal; no `PROJ031`), and §17 (no-untitled recovery scope; 23A seam and Core-additivity open items).
If a seven-item list was intended as a separate gate artifact, it is not in the frozen record; I did not invent
items for it.

#### Closure of round-1 findings

| Finding | Round-2 status | Evidence |
| --- | --- | --- |
| **M-1** | **Closed** | The read-only refusal is `EditApplyOutcome.ReadOnly` with an app-owned message and no `Diagnostic`; `PROJ031` is explicitly not reused. §2/§10/§12/§13, the §17 decisions row, `01-target-architecture.md` §3.2, `05-open-decisions.md`, and `testing-and-parity.md` agree. `ProjectDiagnosticCodes` and the catalog are unchanged; the freeze touches no code. |
| **m-1** | **Closed, with m-11 residual** | §11 now lists `ShellViewModel`, `ShellWindow`/`IShellLifetime`, session open/activate/close/reload writes, the gate/`IsClosing`, `CreateFromLoad`, workspace dirty markers, both shell views, settings registration, and `AppServiceRegistration`/`App.axaml.cs`. The one remaining 23A type edit is `StatusBarItemViewModel` (m-11). |
| **m-2** | **Closed** | `ProjectChangeKind { Edited, Undo, Redo, Saved, Replaced }`; `NotifySaved()` raises `Saved`; `ReplaceDocument` raises `Replaced`; the §2.1 table gives `ShellViewModel`, the Overview, the JSON view, `ValidationCoordinator`, and `ProblemsChannelViewModel` (`EditRejected`) their own subscriptions. |
| **m-3** | **Closed** | §4/§5, §11, §13, and acceptance require discard-close and discard-reload to delete the sidecar/recovery file, with tests that the files are not re-offered. |
| **m-4** | **Closed** | `RecoverySource.Sidecar` is in §6; the sidecar candidate participates in the newest-valid-wins composition, and the recovery test asserts the source and the winner. |
| **m-5** | **Closed for the timestamp-only helper; m-12 for the named trace** | `RecoveryComparison.HasContentDifference` normalizes only `Metadata.ModifiedUtc` and compares the existing `ProjectHasher` projection; `IsDirty` still includes `modifiedUtc`. The named save→edit→autosave→undo-to-clean trace still needs m-12. |
| **m-6** | **Closed** | Statistics are an explicit Overview toggle/command; one `IncludeStatistics = true` run is cached by hash; the test asserts that opening starts no statistics run and that the toggle runs once. |
| **m-7** | **Closed, with n-6/n-7** | §6 recomputes `ProjectSession.Validation` from the snapshot load diagnostics and the test asserts the recovered hash; the `LoadDiagnostics` copy and the `Replaced` ordering are n-6/n-7. |
| **m-8** | **Closed** | `Document { get; private set; }`; `ReplaceDocument(document, canEdit)` resets `CanEdit`, the stacks, the cached dirty state, raises `PropertyChanged`, and raises `Replaced`. |
| **m-9** | **Closed** | The init accessor throws `ArgumentOutOfRangeException` for a negative value; 0 is unbounded; the test constructs `-1` and `0`. |
| **m-10** | **Closed, with n-8** | The per-session gate, non-blocking autosave `Wait(0)`, `IsClosing`, save/close/reload/recovery awaiting, and the cancel reset are stated; the `UnsavedChanges` return needs n-8. |
| **n-1** | **Closed in design** | §2 names the six per-domain validators and keeps the structural/full-pipeline distinction. |
| **n-2** | **Closed in design** | Manifest 64 KiB/100 entries and snapshot 64 MiB are named and asserted by the untrusted-input test. |
| **n-3** | **Closed in design** | The budget is `Task.WhenAny(allWrites, Task.Delay(budget, timeProvider, cts.Token))`; a timeout cancels and reports. |
| **n-4** | **Closed in design** | The pure map test and the launcher URI test are named in §3/§13/§15. |
| **n-5** | **Deferred** | `status.md` remains step-agent-owned; §18 records the `23b` design-review cell update at the start gate. |

#### New findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M-2 — `RecoveryComparison` is a `Mocassin.App.ViewModels` type but computes a project hash with `ProjectHasher.ComputeDocument`, contradicting the one-writer/no-hash rule.** §2 says "view models ... never call `ProjectHasher`"; architecture §3.2 says "View models never ... compute a project hash"; §8 places `RecoveryComparison` in the ViewModels `Recovery/` folder; and §6 says it "compares `ProjectHasher.ComputeDocument`". `Mocassin.App.ViewModels` currently has no `ProjectHasher` use, and `ProjectDocument.IsDirty` is the sanctioned hash owner. As frozen, feature 3 cannot implement the recovery offer without either breaking the architecture rule or adding an unplanned Core/App API. Resolve by moving the comparison to `Mocassin.App` behind a ViewModels contract, or by comparing normalized `ProjectDocument` values through `IsDirty` (no direct `ProjectHasher` call), or by recording a Core-owned comparison as a third API; update §2/§6/§8/§11 and the architecture/decision rows. | 23B page §2 line 166; §6 lines 403-405; §8 lines 462-463; `01-target-architecture.md` §3.2 line 207; `ProjectDocument.IsDirty` (`ProjectDocument.cs:44-49`); `ProjectHasher.ComputeDocument` (`ProjectHasher.cs:56`); `rg ProjectHasher src/dotnet/Mocassin.App.ViewModels` — no hits. |
| **Minor** | **m-11 — The §11 seam-edit list still omits the `StatusBarItemViewModel` change that §8 requires.** §8 says `StatusBarItemViewModel` gains an optional status glyph/severity for the validity item, but the §11 "23A seam edits owned by 23B" table has no `StatusBarItemViewModel` row. §16's guard says any 23A seam needing a signature change beyond §11 stops for an amendment, so the implementor would have to stop for a trivial additive property or edit outside the guard. Add the type (and its status-bar host if one changes) to §11, or implement the validity item with the existing text/tooltip/command and remove the §8 property. | 23B page §8 lines 464-465; §11 table lines 543-564; §16 guard lines 663-665; `StatusBarItemViewModel.cs` (Text/ToolTip/Command only); 23A page §4 line 427 (the item is the base for the later validity/autosave slots). |
| **Minor** | **m-12 — The named m-5 regression trace still leaves a content-bearing sidecar, so the timestamp normalization alone does not make it pass.** The sequence save → edit → autosave → undo-to-clean leaves the sidecar holding the edited project. Undo does not rewrite the sidecar, and a clean close is neither a successful save nor an explicit discard, so §5/§6 keep the sidecar. On reopen `HasContentDifference(sidecar, savedFile)` normalizes only `ModifiedUtc`; the edited content still differs, so recovery is offered even though the in-memory document is clean at the save point. Either delete the sidecar when the document returns to the save point or closes clean (and test that), or make the regression test construct an explicitly timestamp-only snapshot. As written, the m-5 test does not validate the stated "no spurious recovery" outcome. | 23B page §5 lines 334-336; §6 lines 403-406 and 428-429; §13 Recovery row line 607; m-5 resolution text; `ProjectDocument.IsDirty`/`IsAutosaveDue` (`ProjectDocument.cs:44`, `:157`); `ProjectSessionService.CloseAsync` (no `discardChanges`/sidecar deletion in 23A). |
| **Note** | **n-6 — Recovery recomputes `Validation` but does not state that it updates `ProjectSession.LoadDiagnostics`.** §6 says to use the snapshot load's diagnostics and not the saved file's `LoadDiagnostics`, but the retained `ProjectSession.LoadDiagnostics` property would otherwise keep the saved file's array while `Validation` describes the snapshot. `ReloadAsync` updates both fields; recovery should do the same so the two load-state surfaces agree. | 23B page §6 lines 399-400; `ProjectSession.LoadDiagnostics` (`ProjectSession.cs:91`); `ProjectSessionService.ReloadAsync` (`ProjectSessionService.cs:290-292`). |
| **Note** | **n-7 — The recovery flow must assign the recomputed `Validation` before `ReplaceDocument` raises `Replaced`.** §2.1 says `ValidationCoordinator` seeds from `ProjectSession.Validation` on `Replaced`; §6's prose says recovery "replaces the document ... It also recomputes `ProjectSession.Validation`". If the replace/event precedes the recompute, the coordinator seeds the saved file's result, recreating the m-7 symptom for the Problems surface. State the order (compute and assign `Validation`/`LoadDiagnostics`, then raise `Replaced`, or notify the coordinator after the recompute). | 23B page §2.1 line 211; §6 lines 396-400; §6 line 548 (seam row). |
| **Note** | **n-8 — `IsClosing` must be cleared on the `UnsavedChanges` return and the shell's Cancel, not only on a "cancelled" close.** `CloseAsync(discardChanges: false)` on a dirty session returns `UnsavedChanges` and closes nothing; the shell then prompts, and Cancel aborts. If the session was set `IsClosing` before the dirty check and is only reset "if the close is cancelled", a user Cancel can leave `IsClosing` true and autosave permanently skipped. State the reset on the `UnsavedChanges` return and on Cancel explicitly. | 23B page §4 lines 296-301; §5 lines 331-333; §13 Dirty/autosave row line 606. |
| **Note** | **n-9 — The request has no separately numbered "owner questions 1–7" record.** The equivalent owner-facing decisions are recorded at page §16 (class/one unit/no further split), the feature table (four features), §10/§17 (two Core APIs, `MaxUndoDepth` 0/200), §2/§17 (read-only refusal), and §17 (no-untitled scope; seams and Core additivity). If the orchestrator intended a seven-item list as a gate artifact, add it; otherwise no action. | 23B record request/step summary; 23B page §16/§17; no "owner question" string in either file. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

#### Verdict

**Changes required.** Round 2 confirms that the round-1 design edits are real and consistent: M-1 is closed
with the app-level `EditApplyOutcome.ReadOnly` and no code/catalog change across §2/§10/§12/§13, the decisions,
the architecture, and the quality strategy; m-1's main seam list, m-2's notification contract, m-3's discard
deletion, m-4's sidecar source, m-6's explicit statistics, m-8's private setter/reset, m-9's init rejection, and
m-10's gate are all stated and tested; n-1–n-4 are closed and n-5 remains step-agent bookkeeping. The High
class/one-unit case, the four features, the 23A deviations, the no-24–28 scope, and the no-format/contract/
licensing delta are consistent, and the three required gates pass.

It cannot start as frozen because the m-5 resolution introduces M-2: `RecoveryComparison` is a ViewModels type
that directly computes a project hash, contradicting both the architecture's one-writer rule and the page's own
"view models ... never call `ProjectHasher`" statement. m-11 and m-12 are small but concrete inconsistencies in
the §11 guard and the m-5 regression trace; n-6–n-9 are clarifying notes. The design must be refrozen and
re-reviewed after those are resolved or explicitly accepted; this round is the record for `022132e`.

- [ ] Ready for implementation
- [x] Changes required
- [ ] Ready with notes
- [ ] Waived

Follow-up owner: planner/step agent — resolve M-2 (the ViewModels hash boundary), m-11 (the status-bar seam),
and m-12 (the sidecar trace), then decide n-6–n-9 and re-freeze for a fresh re-review. The human owner decides
the next freeze; this round does not create the step branch, mark the step `In progress`, or authorize
implementation.

### Re-review round 4

#### Independence and frozen state

I am a fresh context (DeepSeek V4.1 Flash 1017, opencode-go). I did not author, design, or implement the
reviewed work and have no shared conversation with the planner, the implementor, the earlier reviewers, or the
orchestrator. I received only the frozen commit `a33711dbf9fb35bd782f5bd88eba111e461634d6`, record HEAD
`c4ded0ecb8ee5e6df989918d63ec255b487022d1`, the request record, repository links, and the required commands/scope;
no desired verdict or summary was supplied. The round-3 findings were owner-relayed and have no reviewer round
body; I read them from the request's round-4 refreeze note and the findings/resolution tables.

The design under review is `a33711d` (the round-4 refreeze; the m-13/n-10 design edits are in `02bb9ab`, and
`a33711d` aligns the step-page status pointer). Record HEAD `c4ded0e` is the request's owner-approval/refreeze
commit: `git diff --name-status a33711d c4ded0e` is exactly `M docs/planning/reviews/step-23b.md`, so the record
delta is the only later change; I record it as a process note per review README §8.1. The record commit's parent
is `c4ded0e`, not the design freeze, which is its parent. The full freeze delta `git diff --name-status 63cdd06
a33711d` is ten documentation files; `git diff 63cdd06 a33711d -- contracts src tests tools .github` is empty.
`step-23a` is the only `step-23*` tag locally and on `origin`. The `### Review round 1` and `### Re-review round
2` text is byte-identical to `362aa75` (227 lines from `### Review round 1` to EOF; `diff` empty).

The 23A deviations are respected: first-party `Avalonia.Headless` (no `Avalonia.Headless.XUnit`), the ANGLE
exception unchanged, `InternalsVisibleTo`, no tab drag-reorder, Xvfb evidence, `ProjectPathRules`/`ExistingTarget`
unchanged, the single 23A load-validation pass consumed, `LastWrittenUtc` only, and the crash dialog gaining only
the recovery action. The design adds no 24–28 task and no format/contract change.

#### Commands and results

Run serially in a clean worktree at `c4ded0e`; `git status --porcelain` was empty before this record edit.

- `./tools/verify.sh docs --require` — **pass**; strict Zensical build, DocFX, and rustdoc; final line
  "Closeout gate completed with required checks enabled."
- `./tools/verify.sh policy --require` — **pass**; "Public API is compatible with the checked-in baseline";
  ApiCompat for the checked App assemblies; contract artifacts current (26 files); process consistency OK; 60 tool
  tests OK; SPDX headers in 1002 source files; no secret patterns in 1479 tracked text files; cargo-deny
  advisories/bans/licenses/sources OK; final line "Closeout gate completed with required checks enabled."
- `python tools/check_doc_math.py` — **pass**; "Documentation math conventions OK (191 files scanned)."
- `git diff --name-status a33711d c4ded0e` — `M docs/planning/reviews/step-23b.md` only; `git diff --name-status
  63cdd06 a33711d` — ten documentation files; `git diff 63cdd06 a33711d -- contracts src tests tools .github` —
  empty.
- `git tag --list 'step-23*'` and `git ls-remote --tags origin 'step-23*'` — `step-23a` only.
- Round-1/round-2 preservation: `git show 362aa75:docs/planning/reviews/step-23b.md` and the worktree file from
  `### Review round 1` to EOF are identical (227 lines each; `diff` empty).
- Read-only inspection: `ProjectDocument.IsDirty` (`ProjectDocument.cs:48`) is `ProjectHasher.ComputeDocument`
  over `Project`/`LastSavedProject`; `MarkSaved()` (`:149`) and `IsAutosaveDue` (`:157`) exist; there is no
  `MaxUndoDepth`/`CreateRecovered` yet (additive). `ProjectHasher.ComputeDocument` uses
  `ProjectCanonicalizer.ToCanonicalUtf8`; `ProjectJson` includes `Metadata.ModifiedUtc` (lines 315/376/421).
  `rg ProjectHasher src/dotnet/Mocassin.App.ViewModels` has no hits. `ProjectDiagnosticCodes` has PROJ001–PROJ034
  and `LockHeld` = `PROJ031`, no read-only/edit code. `ProjectEditor.Apply` returns `Change = null` for a
  reference-equal or same-hash candidate (`ProjectEditor.cs:55-61`). `ProjectSessionService` calls
  `MarkSaved(result.SavedProject!)` at lines 220/270; `ReloadAsync` sets `LoadDiagnostics` and `Validation`
  (lines 290-292); `CloseAsync` has no `discardChanges`/gate yet; `IProjectSessionService` has no
  `RecoverAsync`/`DiscardRecoveryAsync`; `ProjectSessionOutcome.UnsavedChanges` is reserved for 23B.
  `StatusBarItemViewModel` has only `Text`/`ToolTip`/`Command`; `ProjectSession` exposes `LoadDiagnostics`,
  `Validation`, `LastWrittenUtc`; `ProjectValidationPipeline.Validate` has the stated signature and a clean load
  yields `Load` `Skipped`; `ValidationOptions.Default` has all flags false; `AppPaths.RecoveryDirectory` is
  created; `WriteAutosaveAsync` derives `GetAutosavePath(projectPath)` = `<path>.autosave`.

#### Closure of round-3 findings and re-audit

| Finding | Round-4 status | Evidence |
| --- | --- | --- |
| **m-13** | **Closed in the clean-close/return-to-clean lifecycle; one related retention path is m-14** | §4 lines 306-312: cleanup is skipped while `PendingRecovery` is non-null; the first accepted content edit clears it and a later return-to-clean/clean-close removes the sidecar. §5 lines 353-359: a successful save with a pending candidate preserves it; clean-close/return-to-clean cleanup is skipped. §6 lines 413-427: "Decide later" keeps the candidate; `RecoverAsync` loads and leaves the document dirty; the next save deletes the snapshot. §7 line 478: `PendingRecovery` is a `ProjectSession` field. §11 lines 575/580: the handler and `ProjectSession` own it. §13 line 641: close-preserve/first-edit-clear/later-cleanup test row. §15 lines 669-670, §17 lines 717-718, and acceptance lines 818-821 repeat the rule; `05-open-decisions.md` line 111 is aligned. |
| **n-10** | **Closed** | §2.1 line 214 adds the `ProjectSessionService` cleanup consumer with session lifetime; it handles `Edited`/`Undo`/`Redo`, ignores `Saved`/`Replaced`, and queues cleanup through the gate. §2.1 lines 224-228 state the gate is non-reentrant, that `NotifySaved`/`ReplaceDocument` are raised while `SaveAsync`/`ReloadAsync`/`RecoverAsync` holds it, and that the cleanup is queued on the UI thread without blocking. §11 line 575 repeats the seam; §13 line 641 asserts no deadlock/re-entry and autosave resumption after a cancelled close. |
| **M-1** | **Re-confirmed closed** | §2 lines 170-176 and §10 lines 549-552: `EditApplyOutcome.ReadOnly` is app-level, no `Diagnostic`, no `PROJ031`, no code/message change; §13 line 637 and acceptance line 813 agree. `ProjectDiagnosticCodes` still has no read-only/edit code; `LockHeld` remains `PROJ031` for the other-process lock. |
| **M-2** | **Re-confirmed closed** | §2.2 lines 245-251 and §6 lines 429-434: `RecoveryComparison` normalizes `ModifiedUtc` and reads `ProjectDocument.CreateRecovered(...).IsDirty`; no `ProjectHasher` call in ViewModels and no App contract. `rg ProjectHasher src/dotnet/Mocassin.App.ViewModels` has no hits; `ProjectDocument.IsDirty` (`:48`) is the Core hash owner; `CreateRecovered` is additive. §11 lines 599-601 and `01-target-architecture.md` line 210 agree. |
| **m-11** | **Re-confirmed closed** | §8 lines 493-494 and §11 line 583 list `StatusBarItemViewModel`/`StatusBarView` with the optional glyph/severity; the delivered type currently has only `Text`/`ToolTip`/`Command`, so the change is additive and within the seam list. |
| **m-12** | **Re-confirmed closed** | §4/§5 and §13 line 640: return-to-clean and clean-close cleanup with the m-13 pending exception; the save→edit→autosave→undo-to-clean→reopen trace leaves no sidecar once the first edit has cleared the candidate. |
| **n-6** | **Re-confirmed closed** | §6 lines 420-422: recovery assigns the snapshot load's `ProjectSession.LoadDiagnostics` as well as `Validation`; `ProjectSession` exposes both. |
| **n-7** | **Re-confirmed closed** | §6 lines 420-423: `LoadDiagnostics`/`Validation` are assigned before `ReplaceDocument` raises `Replaced`, so `ValidationCoordinator` seeds the recovered result. |
| **n-8** | **Re-confirmed closed** | §4 lines 313-323 and §5 lines 349-353: `IsClosing` is set only after the `discardChanges` guard and cleared on `UnsavedChanges`/cancel. |
| **n-9** | **Re-confirmed closed** | §17.1 lines 741-755 lists the seven start-gate questions; §17.2 and the record's `Human approval` table record the owner's answers. |
| **n-5** | **Deferred (step agent)** | `status.md` row 23b still says design review "Not requested"; §18 says the step agent updates the lowercase `23b` row at the start gate. Unchanged from round 2. |
| **High/one unit** | **Re-confirmed** | §16 lines 679-705: High on cross-step interfaces and test-matrix breadth, Medium on the other five dimensions; the one-unit case is sound; the owner approved it on 2026-11-21 (§17.2, record `Human approval`). |
| **Four features** | **Re-confirmed** | Feature table lines 44-49: edit pipeline, validation surface, dirty/autosave/recovery, tests/docs/closeout; owner-approved 2026-11-21; no 23A or 24–28 task. |
| **Owner approvals** | **Recorded** | Record `Human approval` lines 377-389 and page §17.2 lines 757-771: class High/one unit, four features, the two Core APIs, the app-level read-only refusal, the §11 seam fills, the no-untitled scope, the recovery limitations, and the m-13/n-10 refreeze instruction. The formal `Accepted:` checkboxes and signature lines remain unset because the design verdict acceptance is the next gate. |
| **Scope** | **No 24–28 scope; no format/contract change** | Non-goals lines 97-100 exclude model editors (24–25), build UI (26), 3D (27), and run monitoring (28); §11 lines 597-598 lists them only as future consumers. The freeze diff is documentation-only; `contracts/manifest.json` is unchanged; the settings additions are app-local and have no schema bump; no `.mocprj`/`.msl`/`.msr`/`.mcs` change. |

**Judgment on m-13's deferred candidate.** Preserving the deferred candidate is better than discarding it on a
clean close: the user explicitly chose "Decide later", and a clean close is not a decision to throw the snapshot
away. The design keeps the candidate on disk, shows "Recovery available", and re-offers it on reopen; cleanup is
skipped only while pending, and the first real content edit is the documented signal to proceed from the saved
file. The paths that remove a deferred candidate are explicit Recover/Discard, explicit discard-close/reload, and
a first accepted content edit followed by return-to-clean (the documented tradeoff, not a silent loss). A
successful save preserves it per §5. The one path the cleanup guard does not cover is the `recovery/` retention
sweep (m-14): a deferred `Crash`/`AutosaveFallback` candidate older than 7 days is deleted at the next startup
before the scan, because `PendingRecovery` is in-memory and only the sidecar is sweep-exempt.

#### New findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **m-14 — The deferred-candidate preservation claim is not true for `recovery/` candidates: the startup sweep deletes recovery files older than 7 days before scanning, and `PendingRecovery` is in-memory, so a user who chooses Decide later on a `Crash`/`AutosaveFallback` candidate and returns after 7 days loses it.** §6 says "a deferred `PendingRecovery` candidate is preserved across close until Recover or Discard", but the same section says the startup sequence "sweeps `recovery/` first, then scans it" and deletes files older than 7 days; only `.autosave` sidecars are never swept. §7 records the candidate as memory plus sidecar/recovery, so the recovery source is in scope. Either persist the deferral (a manifest flag) or exempt the deferred candidate's snapshot/manifest from the sweep, or state that the 7-day retention also ends a deferred `recovery/` candidate as a recorded limitation and test it. | 23B page §6 lines 413-427 (composition), 454-460 (sweep and the "preserved" clause); §7 line 478; m-13 resolution record line 426; §13 Retention row line 642 ("retention removes only old recovery files; sidecars are never swept"); the Pending recovery test row line 641 covers only clean-close/reopen. |
| **Note** | **n-11 — The §11 save/recovery seam rows and the §13 save/Recovery assertions do not carry the §5 pending-candidate exception or state that `RecoverAsync`/`DiscardRecoveryAsync` clear `PendingRecovery`.** §11 line 574 says `SaveAsync`/`SaveAsAsync` "delete the sidecar/recovery file on success" without the pending exception; §11 line 578 says the new recovery operations recompute `Validation` but not that they consume/clear `PendingRecovery`; §13 line 640 says "a clean save or explicit discard deletes the sidecar/recovery file" and §13 line 642 says "The next save writes the main file and deletes the snapshot" without the pending condition. §5 lines 353-355 and acceptance line 821 are correct, but the seam/test summaries should repeat the exception so an implementor following §11/§13 cannot lose the candidate or leave a stale snapshot after recovery. | 23B page §5 lines 353-355; §6 lines 426-427; §11 lines 574/578; §13 lines 640/642; acceptance lines 818-821. |
| **Note** | **n-12 — The first-edit clear should key on a real content change, not merely any `Edited` event.** §2.1 line 214 says "clears `PendingRecovery` on the first edit"; §4 line 310 and acceptance line 821 say "first accepted content edit". `ProjectEditor.Apply` returns `Change = null` for a reference-equal or same-hash candidate (`ProjectEditor.cs:55-61`), so a no-op accepted edit is possible; if the coordinator maps that to `Applied` and raises `Edited`, the handler would clear the deferred candidate and, if the document is clean, queue cleanup. State that the clear (and the cleanup) runs only when `Edit.Change` is non-null or the project value actually differs. | 23B page §2.1 line 214, §4 line 310, §13 line 641, acceptance line 821; `ProjectEditor.cs:55-61`; `ProjectDocument.Apply` (`ProjectDocument.cs:88-101`) only records a change when `Edit.Change` is non-null. |
| **Note** | **n-13 — The step-page status block still says the design is "not design-reviewed" and that "round 2 returned `Changes required`".** The record state and round history show rounds 3–4, and the page's own design-review bullet already says the current freeze is linked. Update the top `Status` line and the design-review bullet at the start gate (same ownership as n-5), or state that the block is intentionally historical. This is a status/bookkeeping item, not a design blocker. | 23B page lines 8 and 25-27; record `Record state` and round history; `status.md` 23b row is the separate n-5 item. |

#### Verdict

**Ready with notes.** No Blocker or Major remains. m-13 is closed in the normative clean-close/return-to-clean
lifecycle: cleanup is skipped while `PendingRecovery` is non-null, the candidate survives a clean close and is
re-offered on reopen, the first accepted content edit clears it, and a later return-to-clean deletes the sidecar;
the save path preserves it per §5. The design is better than discarding, because it does not silently throw away a
snapshot the user explicitly deferred. n-10 is closed: the `ProjectSessionService` cleanup consumer has session
lifetime, ignores `Saved`/`Replaced`, handles `Edited`/`Undo`/`Redo`, and queues cleanup through the non-reentrant
gate with a no-deadlock test. M-1, M-2, m-11/m-12, n-6–n-9, the High/one-unit class, the four owner-approved
features, the recorded approvals, the no-24–28 scope, and the no-format/contract delta all re-confirm at
`a33711d`. The review gates pass.

One Minor remains: m-14, the 7-day `recovery/` retention sweep can still delete a deferred
`Crash`/`AutosaveFallback` candidate before the next scan, because `PendingRecovery` is in-memory and only
sidecars are sweep-exempt; this contradicts the §6 "preserved until Recover or Discard" wording for recovery-source
candidates. n-11–n-13 are wording/status Notes. The human owner must explicitly accept m-14 and n-11–n-13 (or ask
for the retention clarification and the §11/§13 wording corrections) before the start gate; this review does not
create the step branch, mark the step `In progress`, or authorize implementation.

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Waived

Follow-up owner: human owner — accept `Ready with notes` with m-14/n-11–n-13, or request the retention
clarification and the §11/§13 wording corrections; planner/step agent for the corrections if requested. Follow-up
step: 23B design start gate.
