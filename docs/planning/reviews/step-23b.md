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
| Reviewed commit | `f629dd514f5e4e3c83584aff468969545c407b5c` |
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

**State:** Changes required — round 1 returned `Changes required` on freeze
`f629dd514f5e4e3c83584aff468969545c407b5c` (M-1 read-only diagnostic; Minors m-1–m-10; Notes n-1–n-5). The
design is otherwise coherent and the 23A seams were verified against delivered code; M-1 and the Minors need a
design update before the step can start.

`Re-review required` is a record state, not a verdict.

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
| — | — | — | — | — |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Human approval

- [ ] Accepted: Ready for implementation
- [ ] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by:
Date:

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

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
