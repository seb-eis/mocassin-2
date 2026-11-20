# Step 23 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-23.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 23 — Avalonia Shell and Project Lifecycle |
| Request owner | Planner (design freeze); the step agent takes ownership of resolutions at the start gate |
| Orchestrator | Orchestrator agent (spawns the planning auditor) |
| Step agent (implementor) | Not assigned — created from the reviewed `main` commit after the start-gate approval |
| Review initiated by | Orchestrator, on request of the planner |
| Reviewed commit | `1b82fde744d02cfdb92cecbdad5a5165d62e8b9c` |
| Docs branch | `docs/step-23-design` |
| Review date | 2026-11-20 |
| Reviewer | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh planning auditor |

Step page: [`Step 23 — Avalonia Shell and Project Lifecycle`](../steps/step-23-avalonia-shell-and-project-lifecycle.md)

**Freeze note.** The reviewed commit `9916166a664f0a588e82eb8dbfc1097bc4949845` is the design freeze. It
contains the rewritten Step 23 page and the reconciled
[`01-target-architecture.md`](../01-target-architecture.md) §3.2, [`03-quality-strategy.md`](../03-quality-strategy.md)
§11.1, [`04-roadmap.md`](../04-roadmap.md) §3/§5, [`05-open-decisions.md`](../05-open-decisions.md),
[`appendix-b-parity-matrix.md`](../appendix-b-parity-matrix.md), and
[`docs/development/code-structure.md`](../../development/code-structure.md) §2.3. This request record is a
documentation-only follow-up so the freeze SHA can be written; no design content changed between the freeze and
this record. The freeze contains no code, test, configuration, contract, generated file, or status-dashboard
edit.

**Refreeze note (round 2, 2026-11-20).** Round 1 is historical: it reviewed the first freeze `9916166` under the
previous person-week sizing rule. After it returned `Changes required`, `main` adopted the complexity-class rule
([`04-roadmap.md`](../04-roadmap.md) §3.1) and this design was refrozen at
`1b82fde744d02cfdb92cecbdad5a5165d62e8b9c`. The refreeze applies the new rule (class **High**, dimension
evidence, and a proposed dependency-clean split in §23), resolves M-1–M-3 and every Minor/Note in the
`Resolution status` section below, and reorders the feature list so the claimed 23A/23B boundary is
dependency-clean. **Round-1 reviewer text — Evidence, Findings, Reviewer independence, Verdict, and the round-1
report — is byte-identical to `94cfe46`**; only the request-owned fields (this note, the `Reviewed commit` cell,
the Step summary row, the request checklists, the `Record state` line, and the `Resolution status` section)
changed. The round-1 findings and verdict are kept intact as history.

**Parallel-track note.** The human owner started the GUI track in parallel with the solver step (Step 13) on
2026-11-20. The design deliberately consumes only the library surfaces of Steps 01–07 and 12. Reviewers should
check that §18 contains no assumption about Steps 13–22 and that the seams are sufficient for Steps 24–28.

**Known follow-up owned by the step agent.** `docs/planning/status.md` row 23 still shows
`Design review: Not requested`. The planner may not edit step status ([`AGENTS.md` §3.8](../../../AGENTS.md)), so
the step agent updates the dashboard cell to `Ready for review` with this record when the start gate is
prepared. This is an acknowledged bookkeeping gap, not a design change.

> Orchestration: the orchestrator spawns a fresh planning auditor after the planner has frozen the design and
> completed this request, and relays the verdict and owner decisions. The planner and step agent update the
> design docs, apply fixes on the design docs or a maintenance branch, add resolutions without altering reviewer
> text, and ask the orchestrator for re-review until the auditor no longer identifies issues. The orchestrator
> does not edit the design or the review record. The human owner approves the final verdict and the start of
> implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | F. GUI (first step) |
| Depends on | 01, 03, 04, 05, 06, 07, 12 — all `Done`, merged, and tagged. **Not** dependent on 02/08–11 and not on Steps 13–22 |
| Delivers | The Avalonia application shell, composition root, document/navigation model, project open/create/save/save-as/close with recents, locks and external-change detection, dirty tracking, autosave and crash recovery, the single-project edit pipeline with undo/redo, the validation/problems surface, versioned application settings, theming/density/accessibility, keyboard shortcuts, dialogs and in-app consoles, structured logging with a crash boundary, the headless test harness, and the shell/project documentation. No editors, build UI, 3D, or run monitoring |
| Complexity and split | Class **High** (four of the seven §3.1 dimensions are High: cross-step interfaces, test-matrix breadth, independent acceptance stories, evidence size). §23 proposes a dependency-clean split after feature 4 (23A = features 1–4; 23B = features 5–8) and recommends it; owner acceptance as one unit is allowed with the recorded case and risk guard. **No owner decision is recorded yet.** Person-weeks are a non-gating forecast; no time waiver is requested |

## Design decisions reviewed

- [x] Step page design decisions — goal, scope, non-goals, the shell/regions, document model, navigation and
      contribution seams, project lifecycle, dirty/autosave/recovery, edit pipeline and undo/redo, validation
      surface, settings/theming/accessibility, shortcuts/menus/palette, dialogs/consoles, logging and crash
      handling, state ownership and threading, view-model/view contracts, folder layout, the four library
      changes, the settings format, the parallel-track interface, documentation, tests, performance, risks,
      sizing, and "Done when". — checked; see M-1–M-3 and n-4.
- [x] Data contracts and format changes — the new application-local `mocassin.settings` v1 (§17) and its
      deliberate exclusion from `contracts/manifest.json`; no change to `.mocprj`, `.msl`, `.msr`, `.mcs`, CLI
      reports, exit codes, diagnostics codes, or signatures. — checked; settings is app-local and not in the
      manifest; see m-5/m-6.
- [x] Architecture and module boundaries — `01-target-architecture.md` §3.2 (App/ViewModels/Views ownership and
      the dependency direction), §3.1, and the UI-thread row in §5.3; the "one writer per project", "no
      speculative service interfaces", and "no `Mocassin.Cli` reference" rules. — checked; see M-3 and m-9.
- [x] ADRs and open decisions — ADR 0003 (Avalonia/MVVM/DI, no new control dependency), ADR 0006 (3D untouched),
      ADR 0008/0011/0013 (unaffected), and the new resolved rows plus the Step 23 sizing gate in
      `05-open-decisions.md`. — checked; no new ADR is needed.
- [x] Quality strategy and test plan — `03-quality-strategy.md` §6 item 6, §8 tier row, and §11.1; the §20 test
      table with tiers, the real-object/fake boundary list, the screenshot policy, and the performance budgets
      in §21. — checked; see m-4.
- [x] Parity matrix and documentation plan — the Step 23 rows in
      `appendix-b-parity-matrix.md` §1/§9/§11 and the §19 documentation table; no row claims an unimplemented
      capability as done. — checked; no row claims a later-step capability.
- [x] Licensing and provenance — no new product dependency beyond the packages already pinned in
      `Directory.Packages.props`; the two test-only Avalonia headless packages are first-party, same-version,
      MIT-covered; no GPL code or data is copied; the original repository is used read-only. — checked; the
      headless packages are test-only.
- [x] Post-implementation audit plan — the implementation-status block, feature log, closeout evidence
      (`docs/planning/closeout/step-23.json`), `check_closeout_evidence.py`, the platform-gate evidence, and the
      audit request sequence. — checked; the closeout file is named, not pre-seeded.

### Step-specific decisions

- [x] **Composition root and dependency direction.** `AppHost`/`AppServiceRegistration` build the `IHost`; only
      `Program`/`App` resolve it; explicit registration only (no scanning, no locator); per-project objects are
      constructed by `ProjectSessionService`, not by DI scopes; `App.ViewModels` has no Avalonia reference;
      `ViewLocator` conventions and safeguards. — checked; see M-3 for the services that still need an
      interface/implementation split.
- [x] **Document model and tabs.** `DocumentViewModel`/`DocumentKey`/`DocumentCollectionViewModel`, project-scoped
      keys, one global tab strip, welcome document, the Project Overview and Project JSON documents, and the
      inspector `ContentControl` contract. — checked; consistent.
- [x] **Navigation and contribution seams.** `INavigationService` with bounded history,
      `INavigationTargetProvider` (one provider per domain), unknown-domain behaviour, project explorer plus
      `IExplorerNodeProvider`, and the inspector/menu/console/status seams — verify they are sufficient for
      Steps 24–28 and that no seam takes an Avalonia type. — checked; the seams are sufficient and framework-
      neutral; see m-8 for missing layout files.
- [x] **Project lifecycle.** `IProjectSessionService` over the concrete `ProjectStore`; typed outcomes and no
      prompting inside services; new via `ProjectTemplateFactory` + shared `ProjectNameRules`; save/save-as
      lock migration; close; recents; lock/read-only mode; external-change detection; multi-project workspace;
      window-close coordination. — checked; see m-10 (reload/recover), n-2, n-6.
- [x] **Dirty state, save point, autosave, and recovery.** The `MarkSaved()` no-argument decision and
      `LastWritten` separation, the four save-point consequences, the sidecar vs `recovery/` split, the
      per-session write serialisation, the retention rules (and the rule that sidecars are never swept), the
      unclean-exit session restore, and the crash dialog behaviour. — checked; the save-point proofs are sound;
      see m-11 for the crash write path.
- [x] **Edit pipeline and undo/redo.** Single `ProjectEditCoordinator` per project over `ProjectDocument`;
      no second stack; bounded depth; `ProjectChanged` as the only notification; rejected edits leave state
      unchanged; metadata editing as one undo entry per committed field. — checked; consistent with Step 01.
- [x] **Validation surface.** `ProjectValidationPipeline` usage with `ValidationOptions.Default`, 300 ms
      debounce, stale-result discard by document hash, status bar + Problems + navigation semantics, statistics
      on demand, and no invented diagnostics. — checked; see M-1 (load stage/diagnostics) and n-4.
- [x] **Settings, theming, density, accessibility.** `mocassin.settings` v1 field set and validation/migration
      rules, app-data root and override, additive-field rule with no schema bump, theme/density/font-scale/high
      contrast/reduce motion, WCAG 2.2 AA claims and the accessibility test coverage. — checked; see m-5, m-6,
      and M-3 (theme service placement).
- [x] **Shortcuts, menus, palette.** The gesture table, the `primary` modifier mapping, the "no dead menus"
      rule, the palette behaviour, and the deliberate absence of `Model`/`Build`/`Run`. — checked; see m-7
      (`Ctrl` vs `primary` and the gesture type).
- [x] **Dialogs, consoles, error boundaries.** In-shell overlay instead of `MsBox.Avalonia`, the
      `IDialogService`/`IStorageService` contracts, the Problems vs Application channel split, the entry cap, and
      the error/diagnostic presentation rule. — checked; see M-2 for feature ordering and m-10.
- [x] **Logging and crash handling.** Serilog sinks and retention mapping (with V-3 fallback), the three global
      handlers, the live-UI-thread vs dead-UI-thread crash paths, and `Help → Application diagnostics` not
      duplicating the CLI `doctor`. — checked; see m-11 and n-7.
- [x] **State ownership and threading.** The §13 ownership table, "no scientific logic in view models", UI-thread
      confinement of project edits, `IUiDispatcher`/`TimeProvider` boundaries, and the no-`Task.Delay` rule.
      — checked; see M-3 and m-11.
- [x] **Library changes.** The four additive changes in §16 (`ProjectDocument.MaxUndoDepth`,
      `ProjectDocument.CreateRecovered`, `ProjectStore.WriteAutosaveToPathAsync`, `ProjectNameRules` + CLI
      refactor) are actually necessary, additive, and behaviour-preserving; the CLI's observable behaviour and
      `CLI003`/3 messages stay identical; "no fifth change" is a real constraint. — checked; see m-3 (the
      message-pinning claim).
- [x] **Parallel-track interface.** §18's consumed/not-consumed tables, the rule that Step 23 references no
      `Compilation`/`Results`/`Cli`/solver API, and the statement of how Steps 24–28 plug in. — checked; no
      dependency on Steps 13–22.
- [x] **Sizing and split trigger.** §23's honest 3.75–4.5 pw estimate, the exceed of the four-week trigger, the
      23A/23B boundary, the recommendation, and the fact that the owner decision is still outstanding. — checked;
      see M-2 and m-1/m-2.

- [ ] **Complexity class and split decision (round 2).** §23's seven-dimension table, the High class and
      its normative justification, the proposed split after feature 4, the dependency-clean argument, the
      one-unit acceptance case and risk guard, and the fact that the owner decision is still outstanding.
      Verify no person-week figure is used as a gate and that the class is the highest dimension, not an
      average. — Added for round 2; it supersedes the person-week item above for the refrozen design.

## Dependency and interface checks

- [x] Step 01 is `Done` and its `ProjectStore`, `ProjectDocument`, `ProjectEditor`, `ProjectHasher`, and
      `ProjectJson` contracts are stable. Verify the save/lock/autosave API surface the design relies on,
      including `MarkSaved` overload semantics, `ProjectStoreOptions`, `ProjectFileLock`, and the canonical
      document hash including `modifiedUtc`. — checked; both `MarkSaved` overloads exist, the no-arg clean point
      is correct, `ProjectFileLock` exposes path/acquisition/diagnostics, and `ProjectCanonicalizer` includes
      `modifiedUtc`.
- [x] Step 03–06 editing helpers and the `Project` value model support the metadata edit and the single write
      path without a new API. Verify `ProjectEditor.Apply` covers project metadata and that validation rejects
      an invalid metadata edit. — checked; generic `Apply` + `ProjectValidator` reject an empty name and other
      metadata errors without a new helper.
- [x] Step 07 validation contract is consumed as specified: `ProjectValidationPipeline.Validate`,
      `ValidationOptions` defaults, `ValidationResult`, `ModelStatisticsService`, and the §8 GUI contract.
      Verify no second validator or diagnostic catalog is introduced. — checked; **M-1**: the design does not
      specify `ValidateOnLoad = false` or pass load diagnostics to `Validate`, so the Load stage is not consumed
      as Step 07 §8 requires.
- [x] Step 12's `ProjectTemplateFactory`/`ProjectTemplateKind` and the CLI's name/path rules are the only
      creation path, and the `ProjectNameRules` extraction keeps the CLI's `CLI003`/3 behaviour and messages
      unchanged. — checked; the kinds match (`blank`, `ceria`), but **m-3**: no existing test pins the message
      strings, contrary to the design's claim.
- [x] Steps 13–22 are not required: no design element depends on their types, packages, or behaviour, and the
      seams that Steps 24–28 will use are sufficient. — checked; §18 contains no solver/state/results API use
      and the 24–28 seams are named.
- [x] Format, versioning, and migration impact is confined to the application-local settings file; the project,
      package, results, and state formats and the contract manifest are untouched. — checked; see m-5/m-6 for
      the settings schema/atomicity gaps.

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable — checked; the non-goal list is
      unusually complete.
- [x] Design decisions are complete and internally consistent — checked; see M-1, M-2, M-3.
- [x] Required tests, property tests, golden fixtures, and performance targets are defined — checked; see m-4.
- [x] Documentation, status, parity, and audit updates are planned — checked; see n-1 (stale dashboard row).
- [x] Licensing, provenance, and dependency checks are resolved — checked; the dependency delta is MIT-compatible.
- [x] Risks and mitigations are realistic — checked.
- [x] Code layout and namespace plan follow the structure policy — checked; see M-3 and m-8.
- [x] No hidden scope, stale placeholders, or unowned decisions — checked; see M-2, n-1, n-4.
- [ ] Complexity class and split decision are recorded and justified per
      [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers) — added for round 2 by
      the 2026-11-20 template change; round 1 predates it.

## Evidence

Files reviewed:

- `AGENTS.md` §3, §8–§13; `docs/planning/reviews/README.md`; the request record at
  `docs/planning/reviews/step-23.md` (record HEAD `906db4e`, reviewed commit `9916166`).
- `docs/planning/steps/step-23-avalonia-shell-and-project-lifecycle.md` (all design sections, feature list,
  §16 library changes, §17 settings, §18 parallel track, §19–§23, tasks/acceptance/tests).
- `docs/planning/01-target-architecture.md` §3.1/§3.2/§5.3; `docs/planning/03-quality-strategy.md` §6/§8/§11.1
  and §13; `docs/planning/04-roadmap.md` §3/§3.1/§5; `docs/planning/05-open-decisions.md`;
  `docs/planning/appendix-b-parity-matrix.md` §1/§9/§11; `docs/development/code-structure.md` §2.3;
  `docs/planning/status.md` row 23; `docs/planning/closeout/README.md` and `_template.json`.
- ADR 0003 (`docs/planning/adr/0003-avalonia-mvvm-stack.md`) and ADR 0006
  (`docs/planning/adr/0006-3d-rendering.md`); `Directory.Packages.props`; `Mocassin.slnx`.
- Step 01/07/12 code: `src/dotnet/Mocassin.Core/Editing/ProjectDocument.cs`,
  `ProjectChange.cs`, `ProjectEditResult.cs`, `ProjectEditor.cs`; `src/dotnet/Mocassin.Core/Project/
  ProjectHasher.cs`, `ProjectCanonicalizer.cs`, `Project.cs`, `ProjectMetadata.cs`; `src/dotnet/Mocassin.Packages/
  Projects/ProjectStore.cs`, `ProjectStoreOptions.cs`, `ProjectStoreResults.cs`, `ProjectFileLock.cs`,
  `AtomicProjectFileWriter.cs`; `src/dotnet/Mocassin.Packages/Projects/Validation/ProjectValidationPipeline.cs`,
  `ValidationOptions.cs`, `ValidationResult.cs`, `DiagnosticCatalog.cs`, `ModelStatisticsService.cs`;
  `src/dotnet/Mocassin.Packages/Projects/Templates/ProjectTemplateFactory.cs`, `ProjectTemplate.cs`;
  `src/dotnet/Mocassin.Cli/NewProject/NewProjectCommand.cs`; `src/dotnet/Mocassin.Cli.Tests/NewProject/
  NewProjectCommandTests.cs`; `src/dotnet/Mocassin.Architecture.Tests/**`; `tools/structure-policy.json`.
- App skeleton: `src/dotnet/Mocassin.App/**`, `Mocassin.App.ViewModels/**`, `Mocassin.App.Views/**`,
  `Mocassin.App.Tests/**`, `Mocassin.Rendering/**`, and the project files/PublicAPI files.

Commands run (all in `/tmp/opencode/step-23-review` at `906db4e`):

- `git diff --name-status 9916166 906db4e` → only `A docs/planning/reviews/step-23.md`; the request commit is
  documentation-only and the reviewed commit is its parent.
- `git diff --name-status bd0d94e 9916166` → seven planning docs only; no code, test, configuration, contract,
  generated file, or status-dashboard edit.
- `./tools/verify.sh docs --require` → pass (math/link checks, Zensical strict build, DocFX, rustdoc; closeout
  gate completed with required checks enabled).
- `./tools/verify.sh policy --require` → pass (NuGet licence/vulnerability audit, ApiCompat, contract artifacts,
  projections, catalog, process consistency, tool tests, SPDX headers, secret scan, cargo-deny).
- `python tools/check_doc_math.py` → pass, 173 files scanned.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M-1 — The Step 07 GUI validation contract is not fully consumed: the design does not specify `ProjectStore` with `ValidateOnLoad = false` and calls `Validate(project)` without the load diagnostics.** | Step 23 §8 (“`Validate(project)` … does not let `ProjectStore` validate on load”); Step 07 §8 (“`ValidateFileAsync`, or … plus load diagnostics … does not let `ProjectStore` validate on load as well”); `ProjectStoreOptions.ValidateOnLoad` default `true`; `ProjectStore.LoadBytesCore` runs the validator when true; `ProjectValidationPipeline.Validate` takes `loadDiagnostics`, and `ValidateFileAsync` passes `loadResult.Diagnostics`. |
| **Major** | **M-2 — The eight-feature plan is not dependency-ordered, so the 23A/23B split at feature 5 is not clean and the owner cannot decide it as drawn.** | Feature table rows 3–7: feature 3 needs a settings dialog, feature 4 needs open/save-as/unsaved-changes prompts, feature 5 needs a recovery prompt/crash, feature 6 needs the Problems channel, while feature 7 delivers `IDialogService`, `IStorageService`, the Problems/Application channels, and the crash dialog. §23 calls features 1–4 “a clean 23A boundary” but also says a split leaves 23A “unusable … (no autosave/recovery/dialogs)”. |
| **Major** | **M-3 — The ViewModels folder layout places app-local file-I/O and Avalonia-touching services in the Avalonia-free project, contradicting §1/§14 and architecture §3.2.** | Step 23 §15 places `Settings/SettingsService.cs` (reads/writes `<app-data>/Mocassin.Settings.json`), `Settings/ThemeService.cs` (applies `Application.RequestedThemeVariant`/Fluent resource dictionaries), `Projects/ExternalChangeDetector.cs` (file stamps), and `Projects/RecoveryService.cs` (scans/deletes `recovery/`) in `Mocassin.App.ViewModels`; §1/§14 say that project has no Avalonia reference and is UI-framework-free; `01-target-architecture.md` §3.2 forbids “direct file I/O outside the library APIs”; `code-structure.md` §2.3 says the Avalonia implementations live in `Mocassin.App.Views` or `Mocassin.App`. |
| **Minor** | **m-1 — Sizing arithmetic and contingency allocation.** The per-feature estimates sum to 3.85 pw, not the stated 3.75 pw base; the split puts all 0.75 pw contingency on 23B although the Avalonia/platform risk is in 23A. | Step 23 §23 (`0.5+0.55+0.55+0.55+0.4+0.4+0.35+0.45+0.1 = 3.85`; “Base ≈3.75 pw … plus 0.75 pw contingency”; 23A ≈2.15, 23B ≈1.6 + contingency). |
| **Minor** | **m-2 — The 2.2 pw stop guard is not calibrated to the 2.15 pw feature 1–4 estimate.** It leaves 0.05 pw of headroom, so ordinary estimation noise triggers the stop/report. | Step 23 §23 (guard “if features 1–4 exceed 2.2 pw”); feature estimates. |
| **Minor** | **m-3 — The design says existing CLI tests pin the name/`CLI003` messages; they do not.** The tests pin codes, exit codes, and file preservation, but not the message strings, so the “byte-identical messages” requirement has no regression guard. | Step 23 §16 (“existing CLI tests pin the messages”); `NewProjectCommandTests.cs` invalid-name and existing-target tests assert only `CLI001`/`CLI003` and exit codes; `NewProjectCommand.TryNormalizeName` messages are untested. |
| **Minor** | **m-4 — Absolute wall-clock budgets are asserted in PR-fast tests, contrary to the quality strategy’s same-runner-class/no-absolute-cross-machine rule.** | Step 23 §21 (open/save/autosave “PR-fast test with a stopwatch”); `03-quality-strategy.md` §13 (“Never compare absolute timings across different CI machines”) and the tier table (performance is nightly/release). |
| **Minor** | **m-5 — The settings schema omits the picker’s last-directory state that `IStorageService` is required to remember.** | Step 23 §11 (“remembers the last directory per operation in settings”); §17 v1 schema has no `lastDirectory`/`lastOpenDirectory`/`lastSaveDirectory` field. |
| **Minor** | **m-6 — The settings write lacks the stated atomic discipline, a size cap, and a source-generated JSON context.** | Step 23 §17 (“same discipline as the project file” but “Written atomically (temp + rename)”); AGENTS §11 requires fsync; §17 has no max size; AGENTS §8 requires source-generated `System.Text.Json` contexts; no `AppSettingsJsonContext` is named. |
| **Minor** | **m-7 — The shortcut table uses literal `Ctrl+...` while the design declares a `primary` modifier that maps to Meta on macOS; `ShellCommand.Gesture` has no framework-neutral type.** | Step 23 §10 (“`primary` means Ctrl on Windows/Linux and Meta (`Cmd`) on macOS”, but the table lists `Ctrl+N`, `Ctrl+O`, …; only Quit uses `primary+Q`); `KeyGestureCatalog` lives in ViewModels. |
| **Minor** | **m-8 — The file layout omits several public types named in the design, and the PublicAPI task covers only Core/Packages.** | Step 23 §4 names `IInspectorContributor`, `ExplorerNode`, and `StatusBarItemViewModel`; §15 has no files for them and no `ConsoleService` or concrete `IStorageService`/`IUiDispatcher`; `Mocassin.App.ViewModels.csproj` and `Mocassin.App.Views.csproj` use `PublicApiAnalyzers`, but §16 mentions `PublicAPI.Unshipped.txt` only for Core/Packages. |
| **Minor** | **m-9 — The architecture test row is transitive-fragile.** `Mocassin.App` must not reference `Mocassin.Compilation`, but `Mocassin.App` → `Mocassin.Packages` → `Mocassin.Compilation`; a transitive check would fail. | Step 23 §20 architecture row; `Mocassin.App.csproj` references `Mocassin.Packages`; `Mocassin.Packages.csproj` references `Mocassin.Compilation`. |
| **Minor** | **m-10 — The lifecycle contract lacks reload/recover/discard operations.** | Step 23 §5 `IProjectSessionService` has `ScanRecoveryAsync` only; the external-change flow needs Reload and the recovery flow needs Recover/Discard. The already-open-path rule makes `OpenAsync(path)` activate rather than reload. |
| **Minor** | **m-11 — The crash-path “synchronous sidecar/recovery writes” conflict with the no-sync-over-async rule and the async-only `ProjectStore` API.** | Step 23 §6 crash bullet (“synchronous … writes”); §13 (“no sync-over-async”); `ProjectStore` exposes only async autosave/save; §16 adds no synchronous write. |
| **Note** | **n-1 — The status dashboard is stale.** | `docs/planning/status.md` row 23 says “Not started”/“Not requested”; the step page says design review `Ready for review`; the request records this as a step-agent follow-up. |
| **Note** | **n-2 — The `ProjectSessionResult` outcome list is incomplete for load failures.** | Step 23 §5 lists `Created`, `Opened`, `OpenedReadOnly`, `Saved`, `SaveFailed`, `UnsavedChanges`, `Locked`, `Canceled`, but no `NotFound`/`InvalidContent`/`Io` kind although `ProjectLoadResult.FailureKind` has them. |
| **Note** | **n-3 — `IInspectorContributor.CreateInspector(object selection)` is weakly typed.** | Step 23 §4; Steps 24/25 can pass arbitrary objects and the shell cannot validate the selection. |
| **Note** | **n-4 — The feature table does not explicitly allocate §8’s full validation surface.** | Step 23 §8 (status indicator, Problems filters/sort/help anchors, statistics on demand, navigation) vs the feature table, where these are split across features 2/6/7 without one owning feature; this also makes the 23A estimate harder to verify. |
| **Note** | **n-5 — “Verified in feature 1, §24” is a dangling reference for monitor fallback.** | Step 23 §2; §24 is the decisions table and its V-1…V-4 items do not include monitor fallback. |
| **Note** | **n-6 — Only the name rules are shared with the CLI, not the path/existence rules.** | Step 23 §16 extracts `ProjectNameRules`; the CLI’s `CLI002`/`CLI003` path/directory cases live in `NewProjectCommand`, so the GUI’s `CreateAsync` will re-implement them. |
| **Note** | **n-7 — The `TimeProvider` logging-timestamp claim overreaches.** | Step 23 §13 says `TimeProvider` is injected for “logging timestamps”; Serilog’s file-sink timestamps are not injectable (events can be enriched instead). |
| **Note** | **n-8 — “The audit can be read in one sitting” is a judgment.** | Step 23 §23; the step page is ~1,140 lines with eight features, which is a large audit surface. No action required if the owner accepts it. |

### Finding details

**M-1 — The Step 07 GUI validation contract is not fully consumed.**

Step 23 §8 says the shell “consumes `ProjectValidationPipeline` exactly as Step 07 §8 requires: `Validate(project)`
with `ValidationOptions.Default` … It does not … let `ProjectStore` validate on load.” Step 07 §8 permits two
forms: `ValidateFileAsync`, or the in-memory entry “plus load diagnostics”, and in both cases the store must not
validate on load as well. `ProjectStoreOptions.ValidateOnLoad` defaults to `true`, and `ProjectStore.LoadBytesCore`
runs `validator(project)` when it is true, so the composition must explicitly pass
`new ProjectStoreOptions { ValidateOnLoad = false }`; the design does not say this. The in-memory
`Validate(Project, ImmutableArray<Diagnostic> loadDiagnostics = default, …)` also takes the load diagnostics that
`ValidateFileAsync` forwards from `ProjectLoadResult.Diagnostics`; the design’s `Validate(project)` call omits
them. The result is either double validation on open (default store) or a `Load` stage that is skipped and
migration/load diagnostics that never reach the status bar or Problems. Fix: state the store options, seed the
first validation with `ProjectLoadResult.Diagnostics`, and add a test that a load/migration diagnostic appears in
Problems and the `Load` stage is not skipped.

**M-2 — The feature list is not dependency-ordered; the 23A/23B split is not clean.**

The feature table is the owner-approved work breakdown, and §23 uses features 1–4 as “exactly 23A”. But feature 3
includes the settings dialog, feature 4 includes new/open/save-as/close, recent files, and unsaved-changes
prompts, feature 5 includes the recovery prompt and crash dialog, and feature 6 includes the Problems/edit
diagnostics — while feature 7 is the feature that delivers `IDialogService`, `IStorageService`, the
Problems/Application channels, and the crash dialog (§11). §23’s own recommendation says a split leaves 23A
“unusable … (no autosave/recovery/dialogs)”, which concedes the boundary is not clean. The numbers imply an
implementation order; if the implementor reorders, the approved feature list and the split estimate no longer
describe the work. Fix: either move the dialog/picker contracts and host earlier (e.g., into feature 2/3), or state
that features 3–5 implement a minimal dialog/picker slice and feature 7 completes it, then re-cut the 23A/23B
boundary and estimates. Map §8’s validation surface to one owning feature at the same time (n-4).

**M-3 — File-I/O and Avalonia-touching services are placed in the Avalonia-free ViewModels project.**

§1/§14 say `Mocassin.App.ViewModels` has no Avalonia reference and is UI-framework-free;
`01-target-architecture.md` §3.2 says that project must not own “direct file I/O outside the library APIs”; and
`code-structure.md` §2.3 says dialogs, pickers, the UI dispatcher, and clocks are interfaces there and their
Avalonia implementations live in `Mocassin.App.Views` or `Mocassin.App`. Yet §15 places
`Settings/SettingsService.cs` (which reads/writes `<app-data>/Mocassin.Settings.json`),
`Settings/ThemeService.cs` (which applies `Application.RequestedThemeVariant` and the Fluent/accent/density
resource dictionaries), `Projects/ExternalChangeDetector.cs` (file stamps), and `Projects/RecoveryService.cs`
(scans/deletes `recovery/`) in `Mocassin.App.ViewModels`. That contradicts the architecture test/acceptance
criterion and the “no direct I/O” rule. Fix: keep the interfaces and pure state in ViewModels; put the
settings/recovery file stores and the Avalonia theme applier in `Mocassin.App`/`Mocassin.App.Views`, and list
those files in the layout. Alternatively, carve app-local settings/log/recovery I/O out of the architecture rule
explicitly and say where it lives.

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Re-review required — round 1 returned `Changes required` on the first freeze
`9916166a664f0a588e82eb8dbfc1097bc4949845` (M-1–M-3 Majors; m-1–m-11 and n-1–n-8 recorded for resolution). The
design was refrozen at `1b82fde744d02cfdb92cecbdad5a5165d62e8b9c` after the complexity-sizing rule landed; the
findings are resolved or explicitly dispositioned in `Resolution status` below, and the refrozen design awaits a
fresh round-2 review. Round-1 findings and verdict remain unchanged as history.

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

Follow-up owner: planner/step agent — resolve M-1–M-3 and either fix or explicitly accept the Minors before the
start gate.
Follow-up step: 23 — design re-review at the new freeze commit; earlier rounds stay in this record.

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

Round-1 reviewer text, findings, evidence, and verdict are unchanged. The design was corrected on
`docs/step-23-design` at the round-2 freeze `1b82fde744d02cfdb92cecbdad5a5165d62e8b9c`; this record update is a
documentation-only follow-up. No code, test, configuration, contract, or generated artifact changed.

| Finding | Status | Owner | Commit | Resolution |
| --- | --- | --- | --- | --- |
| **M-1** | Fixed | Planner | `1b82fde` | §5/§8/§18 now specify the single `ProjectStore` with `ProjectStoreOptions { ValidateOnLoad = false }`, the in-memory `Validate(project, loadDiagnostics: ProjectLoadResult.Diagnostics, ValidationOptions.Default, null, ct)` entry, and the consequences: no double validation, no skipped `Load` stage, load/migration diagnostics in the status bar and Problems. Feature 4/6 scope, the §20 test table, and the acceptance criteria carry a PR-fast test for the option, the populated `Load` stage, no duplicates, and that `ValidateFileAsync` is unused by the GUI. |
| **M-2** | Fixed | Planner | `1b82fde` | The feature list was reordered so every feature depends only on lower-numbered ones: feature 1 now delivers the dialog overlay, pickers, dispatcher, console host/Application channel, notifications, and progress; feature 2 the workspace; feature 3 settings/theme/a11y; feature 4 lifecycle; feature 5 edit/undo; feature 6 the single-owned validation surface; feature 7 dirty/autosave/recovery; feature 8 tests/docs/closeout. The 23A/23B boundary after feature 4 is dependency-clean and §23 argues it; the §23 recommendation no longer concedes an unusable 23A. `§8`'s surface is mapped to feature 6 (also resolves n-4). |
| **M-3** | Fixed | Planner | `1b82fde` | The app-local I/O and Avalonia-touching services moved out of the Avalonia-free project. §1 now has an explicit I/O-boundary table: ViewModels keeps `AppSettings`/validator/migrations/`AppSettingsJsonContext`/`IAppSettingsStore`/`IRecoveryStore`/`IProjectFileProbe`/`ThemeSelection`/`IThemeApplier`; `Mocassin.App` implements `AppSettingsStore`, `RecoveryStore`, `ProjectFileProbe`, `AppPaths`, log retention; `Mocassin.App.Views` implements `AvaloniaThemeApplier`, `AvaloniaStorageService`, `AvaloniaUiDispatcher`, key binding. §13/§14/§15, `01-target-architecture.md` §3.2, and `code-structure.md` §2.3 were updated with the same rule, and a source-token architecture test (no `System.IO`/`Environment`/`Application`/resource dictionaries in ViewModels) enforces it. |
| **m-1** | Superseded | Planner | `1b82fde` | Person-week arithmetic and contingency allocation are gone: the design no longer records a pw estimate, and §23 records the complexity class instead. No arithmetic remains to correct. |
| **m-2** | Superseded | Planner | `1b82fde` | The 2.2 pw stop guard is removed with the time rule. The one-unit branch of §23 has a trigger-based risk guard (harness not green, M-1 not demonstrable, M-3 unenforceable), not a time guard. |
| **m-3** | Fixed | Planner | `1b82fde` | The inaccurate "existing CLI tests pin the messages" claim is replaced: §16 states the existing tests pin codes/exit codes only, and the refactor adds new `Mocassin.Cli.Tests` cases that pin the exact message text of every rejected name/path case. A GUI test asserts the GUI shows the same message for the same input through the shared helper. |
| **m-4** | Fixed | Planner | `1b82fde` | §21 is rewritten: no absolute duration is asserted in a PR-fast test. Design budgets are recorded as non-gating evidence; PR-fast asserts functionality, ordering, and asynchrony; the only timing in a test is a generous hang guard. `03-quality-strategy.md` §11.1 states the same rule. |
| **m-5** | Fixed | Planner | `1b82fde` | §17 adds `paths.lastOpenDirectory`, `paths.lastSaveDirectory`, and `paths.lastFolderDirectory`; §11 and §5 require the picker to read and update them; the §20 settings test covers persistence and update. |
| **m-6** | Fixed | Planner | `1b82fde` | §9/§17 now specify the source-generated `AppSettingsJsonContext`, the temp-file + `Flush(flushToDisk: true)` + replace protocol, `MaxSettingsBytes = 4 MiB`, the `recentFiles` cap, and that a failed/oversized write leaves the previous file intact. An App-layer store makes it testable. |
| **m-7** | Fixed | Planner | `1b82fde` | §10 defines the framework-neutral `KeyGestureDescriptor(ModifierKeys, KeyName)` with a `Primary` modifier; the shortcut table uses `primary` uniformly; only `Mocassin.App.Views` converts to Avalonia `KeyGesture`/`KeyBinding` and renders the per-platform text. |
| **m-8** | Fixed | Planner | `1b82fde` | §15 now lists `IInspectorContributor`, `InspectorSelection`, `ExplorerNode`, `StatusBarItemViewModel`, `ConsoleService`, `IUiDispatcher`, `AppSettingsStore`, `RecoveryStore`, `ProjectFileProbe`, `AvaloniaThemeApplier`, `AvaloniaStorageService`, `AvaloniaUiDispatcher`, and the gesture types. All App-project types are `internal` with `InternalsVisibleTo("Mocassin.App.Tests")`, so `PublicAPI.*` for `Mocassin.App*` stays unchanged; §15/§16 state the rule. |
| **m-9** | Fixed | Planner | `1b82fde` | The §20 architecture row now checks **direct** project references read from the `.csproj` files, not the transitive assembly graph, and explicitly allows the expected `App → Packages → Compilation` edge. |
| **m-10** | Fixed | Planner | `1b82fde` | §5 now defines `ActivateAsync` (already-open path), `ReloadAsync` (fresh `ProjectDocument`, stacks discarded), `RecoverAsync`, and `DiscardRecoveryAsync`, and the outcome enum covers them; §6 and the §20 recovery test use the operations. |
| **m-11** | Fixed | Planner | `1b82fde` | The crash path is asynchronous: `CrashRecoveryWriter` starts the snapshot writes, awaits them with a 2 s budget, reports per-session success/timeout/failure, and never uses `.Result`/`GetAwaiter().GetResult()`. §13 repeats the no-sync-over-async rule for this path. |
| **n-1** | Deferred | Step agent | — | The status dashboard is step-agent-owned (AGENTS §3.8); the planner did not edit it. The step page and this record still flag it for the start gate. |
| **n-2** | Fixed | Planner | `1b82fde` | The `ProjectSessionOutcome` enum now includes `NotFound`, `InvalidContent`, and `IoFailure`, mapped from `ProjectLoadResult.FailureKind`, in addition to `Locked`/`UnsavedChanges`/`SaveFailed`/`Canceled`. |
| **n-3** | Fixed | Planner | `1b82fde` | `IInspectorContributor.CreateInspector(InspectorSelection)` replaces the `object selection` signature. `InspectorSelection` carries the document key, project id, validated `ModelPath` list, and domain tag; unclaimed tags render the empty state. |
| **n-4** | Fixed | Planner | `1b82fde` | The whole §8 surface (status indicator, Problems filters/sort/help anchors/related navigation/revalidate, edit diagnostics, on-demand statistics) is owned by feature 6; §8 and the feature table say so, and no part is orphaned across features. |
| **n-5** | Fixed | Planner | `1b82fde` | The dangling "verified in feature 1, §24" is replaced by the pure `WindowPlacementResolver` rule (intersection with a current screen, else primary), a PR-fast placement test, and verification item V-5 for the real multi-monitor check in the manual platform gate. |
| **n-6** | Fixed | Planner | `1b82fde` | The shared helper is now `ProjectPathRules` (name **and** target path: separators, `..`, length, reserved characters, existing target, directory creation), used by `File > New` and by `NewProjectCommand` (§16). |
| **n-7** | Fixed | Planner | `1b82fde` | `TimeProvider` is scoped to document/autosave/stamp/session/retention time; log record timestamps come from the Serilog sink, and tests assert level/scope/message rather than sink timestamps. |
| **n-8** | Superseded | Planner | `1b82fde` | The "audit can be read in one sitting" judgement is removed. The evidence-size dimension now rates the step **High** and drives the split recommendation in §23, so no inline claim is needed. |

**Record-integrity note (round 2).** This update changed only request-owned fields: the `Reviewed commit` cell,
this refreeze note, the Step summary row, the request checklists (two items appended for the new template), the
`Record state` line, and this section. The reviewer-authored `Evidence`, `Findings`, `Reviewer independence`,
`Verdict`, and `Review round 1` text is byte-identical to `94cfe46`. No global replacement was used: the
`Reviewed commit` cell was changed by matching its exact old cell string only. The extraction is reproducible
with `git show 94cfe46:docs/planning/reviews/step-23.md` and line-anchored section extraction; the SHA-256 values
are identical in both files:

| Section | Extent | SHA-256 |
| --- | --- | --- |
| Evidence, Findings, Finding details | `## Evidence` to `## Record state` | `74f4c21a42564c3238ce9051a23e15f3183e5ea47ad08c65b98eb93d42bc3047` |
| Reviewer independence and Verdict | `## Reviewer independence` to `## Human approval` | `7b16d7d90f8afdead99bd3781b386da7641ee15ae400254b3883b9c9f0778f0f` |
| Review round 1 | `## Review rounds` to EOF | `adedebdb4a0ba8a4420a44b40104d79ddc4761b93e7d5194aea146e2daf0daea` |

Findings and verdict above are round 1's and are not re-scored here.

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

I am a fresh context. I did not author, design, or implement the reviewed work and have no shared conversation
with the planner, implementor, or orchestrator. I reviewed the repository at record HEAD
`906db4e884ff90f4dba1fc42872ef958fecb6a02` and confirmed with `git diff --name-status 9916166 906db4e` that the
only delta from the frozen design commit is the added request record. The reviewed commit is
`9916166a664f0a588e82eb8dbfc1097bc4949845`, as the request states. `git diff --name-status bd0d94e 9916166`
contains seven planning/docs files only: `docs/development/code-structure.md`,
`docs/planning/01-target-architecture.md`, `docs/planning/03-quality-strategy.md`, `docs/planning/04-roadmap.md`,
`docs/planning/05-open-decisions.md`, `docs/planning/appendix-b-parity-matrix.md`, and the Step 23 page. No code,
test, configuration, contract, generated file, or status-dashboard edit is in the freeze.

#### Commands and results

- `./tools/verify.sh docs --require` — pass (math/link checks, strict Zensical build, DocFX, rustdoc; closeout
  gate completed).
- `./tools/verify.sh policy --require` — pass (NuGet licence/vulnerability audit, ApiCompat, contract artifacts,
  projections, catalog, process consistency, tool tests, SPDX headers, secret scan, cargo-deny).
- `python tools/check_doc_math.py` — pass, 173 files scanned.
- Read-only source inspection of the Step 01 `ProjectStore`/`ProjectDocument`/`ProjectHasher`/`ProjectCanonicalizer`
  APIs, the Step 07 validation pipeline/options, the Step 12 template factory and CLI `new` rules, the
  App/App.ViewModels/App.Views project files, the architecture tests, and the structure policy. No repository
  file was modified; the only write is this record.

#### Findings

M-1–M-3 are Majors and must be corrected before implementation:

- **M-1** — Step 07’s GUI contract is not consumed as written: the store’s `ValidateOnLoad = false` is not
  specified, and `Validate(project)` drops the load diagnostics. The result is double validation or a skipped
  `Load` stage and missing migration/load diagnostics.
- **M-2** — The feature list is not dependency-ordered; features 3–6 need the dialog/picker/Problems surfaces
  that feature 7 delivers. The 23A/23B split at feature 5 is therefore not clean, and the design’s own
  recommendation concedes 23A would have no dialogs.
- **M-3** — The ViewModels layout places app-local settings/recovery file I/O and the Avalonia theme applier in
  the Avalonia-free project, contradicting §1/§14, architecture §3.2, and the step’s own architecture test.

m-1–m-11 and n-1–n-8 are recorded in the `Findings` table above. The Minors are fixable in the design docs; the
Notes are observations for the planner/step agent.

#### Verdict

**Changes required.** The design is unusually complete, the parallel-track interface is sound, the save-point
analysis is correct, and both required policy/docs gates pass. It is not ready for implementation until M-1–M-3
are corrected and the Minors are either fixed or explicitly accepted by the human owner.
