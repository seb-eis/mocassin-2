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
| Reviewed commit | `4b5fa44c14bd5a3f145203eb81a1b8b5a5f71993` |
| Docs branch | `docs/step-23-design` |
| Review date | 2026-11-20 |
| Reviewer | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh planning auditor |

Step page: [`Step 23 — Avalonia Shell and Project Lifecycle`](../steps/step-23-avalonia-shell-and-project-lifecycle.md)

**Freeze note (round 1).** The first freeze was `9916166a664f0a588e82eb8dbfc1097bc4949845`; round 2 reviews
`1b82fde744d02cfdb92cecbdad5a5165d62e8b9c` (see the refreeze note below). The first freeze contains the rewritten
Step 23 page and the reconciled
[`01-target-architecture.md`](../01-target-architecture.md) §3.2, [`03-quality-strategy.md`](../03-quality-strategy.md)
§11.1, [`04-roadmap.md`](../04-roadmap.md) §3/§5, [`05-open-decisions.md`](../05-open-decisions.md),
[`appendix-b-parity-matrix.md`](../appendix-b-parity-matrix.md), and
[`docs/development/code-structure.md`](../../development/code-structure.md) §2.3. This request record is a
documentation-only follow-up so the freeze SHA can be written; no design content changed between the first
freeze and its request record. Neither freeze contains a code, test, configuration, contract, generated-file, or
status-dashboard edit.

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

**Refreeze note (round 3, 2026-11-20).** Round 2 (`36efc80`) is historical: it reviewed freeze `1b82fde`,
confirmed M-1–M-3 in substance, and returned `Changes required` on M-4 plus m-12–m-17 and n-9–n-13. This design
was refrozen at `4b5fa44c14bd5a3f145203eb81a1b8b5a5f71993`. The refreeze keeps the two existing public App module
markers and entry points, corrects the internal-visibility text (M-4), mirrors `ProjectLoadFailureKind` exactly
(m-12), replaces the I/O token list with an exact, positively controlled one (m-13), scopes the `Load`-stage claim
to load-diagnostic fixtures (m-14), finishes the `ProjectPathRules` rename (m-15), replaces the integrity values
with a reproducible check (m-16), and splits the 23A/23B documentation scope (m-17); n-9–n-13 are fixed. **Round-1
and round-2 reviewer text is byte-identical to `94cfe46` and `36efc80` respectively**; only request-owned fields
(this note, the `Reviewed commit` cell, the request checklists, `Record state`, and `Resolution status`) changed.
The earlier rounds' findings and verdicts remain intact as history.

**Parallel-track note.** The human owner started the GUI track in parallel with the solver step (Step 13) on
2026-11-20. The design deliberately consumes only the library surfaces of Steps 01–07 and 12. Reviewers should
check that §18 contains no assumption about Steps 13–22 and that the seams are sufficient for Steps 24–28.

**Known follow-up owned by the step agent.** `docs/planning/status.md` row 23 still shows
`Design review: Not requested`. The planner may not edit step status ([`AGENTS.md` §3.8](../../../AGENTS.md)), so
the step agent updates the dashboard cell to the current design-review state with this record when the start
gate is prepared. This is an acknowledged bookkeeping gap, not a design change.

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

- [x] **Complexity class and split decision (round 2).** §23's seven-dimension table, the High class and
      its normative justification, the proposed split after feature 4, the dependency-clean argument, the
      one-unit acceptance case and risk guard, and the fact that the owner decision is still outstanding.
      Verify no person-week figure is used as a gate and that the class is the highest dimension, not an
      average. — Added for round 2; it supersedes the person-week item above for the refrozen design.
      — Round 2 checked; see the complexity-class check in `Re-review round 2`.

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
- [x] Complexity class and split decision are recorded and justified per
      [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers) — added for round 2 by
      the 2026-11-20 template change; round 1 predates it.
      — Round 2 checked; the class is High (the highest dimension) and no pw figure is a gate.

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

**State:** Ready with notes — round 3 at record `f9c922e` (frozen design `4b5fa44`) closed M-4 in substance and
closed m-12, m-13, m-15, m-16, m-17, and n-9–n-13; m-14 remains partly closed (two duplicate unconditional
`Load`-stage sentences), and round 3 adds m-18 plus Notes n-14/n-15. The human owner must explicitly accept these
notes, the feature list, and the §23 split decision. Rounds 1 and 2 returned `Changes required`; their findings and
verdicts remain unchanged as history.

`Ready with notes` is a verdict; the human owner's acceptance is still required for the start gate.

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

**Round-2 corrections to the rows above.** The m-8 row's "all App types are `internal`" claim and the n-2 row's
outcome list were incomplete; the M-4 and m-12 rows below supersede them. The round-1 rows are kept as history
and are otherwise accurate.

| Finding | Status | Owner | Commit | Resolution |
| --- | --- | --- | --- | --- |
| **M-4** | Fixed | Planner | `4b5fa44` | Kept the existing public markers. §14/§15/§16 now say: every **new** App type is `internal` with `InternalsVisibleTo("Mocassin.App.Tests")`; the only public App types are the pre-existing `AppViewModelsModule`/`AppViewsModule` markers (in `PublicAPI.Shipped.txt`, baselined in `tests/contracts/api-baseline/net11.0/`, ApiCompat-checked) and the `Program`/`App` entry points (`Mocassin.App` is not in `tools/api-projects.txt`). Step 23 adds or removes no public App type, so `PublicAPI.*` and the two App baselines stay unchanged and `./tools/verify.sh policy` runs unmodified. `01-target-architecture.md` §3.2, `code-structure.md` §2.3, and `05-open-decisions.md` record the same rule; §15 lists the marker files as retained. No baseline refresh is proposed. |
| **m-12** | Fixed | Planner | `4b5fa44` | `ProjectSessionOutcome` now mirrors `ProjectLoadFailureKind` one-to-one: `NotFound`, `TooLarge` (the 64 MiB `MaxDocumentBytes` rejection), `Io` (OS/sharing failure), `InvalidContent`. The mapping is a pure switch with no silent fallback to `Io`, and §5 names each case. |
| **m-13** | Fixed | Planner | `4b5fa44` | The token test is exact and positively controlled. After stripping comments and string literals, `Mocassin.App.ViewModels/**/*.cs` must not contain `System.IO`, `File.`/`FileInfo`, `Directory.`/`DirectoryInfo`, `Path.Combine`/`Path.Get`, `FileStream`, `StreamReader`, `StreamWriter`, `Environment.`, `Avalonia`, `Dispatcher.UIThread`, `Application.Current`, or `ResourceDictionary`. `IUiDispatcher`/bare `Dispatcher` and bare `Application` are permitted and named; a positive control feeds a seeded violation and asserts it is reported. §1/§20, `01` §3.2, `code-structure` §2.3, and quality §11.1 say the same thing. |
| **m-14** | Fixed | Planner | `4b5fa44` | §8, §20, feature 4, and the acceptance criterion now state that the `Load` stage is `Completed`/`Failed` only when `ProjectLoadResult.Diagnostics` is non-empty, and correctly `Skipped` with count 0 for a clean current-version load (Step 07 §2). The test covers both fixtures and the no-duplicate rule. |
| **m-15** | Fixed | Planner | `4b5fa44` | `01-target-architecture.md` §3.2 and the parity-matrix "Create new project" row now say `ProjectPathRules`. No `ProjectNameRules` remains outside reviewer history. |
| **m-16** | Fixed | Planner | `4b5fa44` | The non-reproducing values were removed. The note below defines the exact extents, normalizes trailing newlines, and gives a copy-paste Python snippet with the four resulting hashes; the extracted sections diff empty against `94cfe46` (round 1) and `36efc80` (round 2). |
| **m-17** | Fixed | Planner | `4b5fa44` | §19 is now a 23A/23B column table scoped to each half's capabilities. `quickstart.md` and `first-project.md` are scoped to create/open/inspect/save/reopen, recents, locks, reload, templates, workspace, tabs, settings; validation, editing, undo, autosave, and recovery sections are listed as 23B completions. `projects.md`, `ui-architecture.md`, `troubleshooting/installation.md`, and `file-layout.md` are split the same way; feature 4/8 and §23 use the columns. |
| **n-9** | Fixed | Planner | `4b5fa44` | §14's two consecutive `Mocassin.App` bullets are merged into one; the `ViewLocator` safeguards and the I/O implementations are in the same bullet. |
| **n-10** | Fixed | Planner | `4b5fa44` | §1's lifetime list now registers one `ProjectStore` constructed with `ProjectStoreOptions { ValidateOnLoad = false }`, one `ProjectValidationPipeline` (documented stateless/thread-safe), and the settings/recovery/probe singletons; both library services are constructor-injected into `ProjectSessionService`, and the M-1 test resolves the registered store to assert its options. |
| **n-11** | Fixed | Planner | `4b5fa44` | §1's prose now states the direct `Mocassin.Contracts`, `Mocassin.Packages`, and `Mocassin.Results` references plus the transitive `Mocassin.Core`, matching the `.csproj`. |
| **n-12** | Fixed | Planner | `4b5fa44` | The internal rule is scoped to new types; `Program`/`App` are named entry-point exceptions and `Mocassin.App` is not an ApiCompat project. Same correction as M-4. |
| **n-13** | Fixed | Planner | `4b5fa44` | Feature 1 now uses an in-memory/default last-directory seam and the default log level; feature 3 wires both to the persisted settings model. Both remain inside 23A, so the split boundary is unchanged and the dependency is explicit. |

**Record-integrity note (rounds 1–2, corrected for m-16).** This update changed only request-owned fields: the
`Reviewed commit` cell, the refreeze notes, the Step summary row, the request checklists, the `Record state` line,
the round-1 freeze-note preamble/known-follow-up wording, and this `Resolution status` section (including the
round-2 additions). No global replacement was used: the `Reviewed commit` cell was changed by matching its exact
old cell string only.

The reviewer-owned sections are byte-identical to their source commits. The check is reproducible with this
snippet, run from the repository root; it extracts each extent from the source commit and from this file, anchors
on whole heading lines (so the heading strings quoted here cannot match), strips only trailing newlines (so the
separator before the next heading does not matter), hashes both, and asserts equality:

```python
import hashlib, subprocess
def sec(t, a, b=None):
    # line anchors, so the heading strings quoted in this note do not match
    i = t.index("\n" + a + "\n") + 1
    j = len(t) if b is None else t.index("\n" + b + "\n", i) + 1
    return t[i:j].rstrip("\n")
head = open("docs/planning/reviews/step-23.md", encoding="utf-8").read()
old1 = subprocess.check_output(["git", "show", "94cfe46:docs/planning/reviews/step-23.md"]).decode()
old2 = subprocess.check_output(["git", "show", "36efc80:docs/planning/reviews/step-23.md"]).decode()
checks = [
    ("evidence",        sec(old1, "## Evidence", "## Record state"),                    sec(head, "## Evidence", "## Record state")),
    ("independence",    sec(old1, "## Reviewer independence", "## Human approval"),      sec(head, "## Reviewer independence", "## Human approval")),
    ("round1",          sec(old1, "### Review round 1"),                                 sec(head, "### Review round 1", "### Re-review round 2")),
    ("round2",          sec(old2, "### Re-review round 2"),                              sec(head, "### Re-review round 2")),
]
for name, old, new in checks:
    print(name, hashlib.sha256(old.encode()).hexdigest(), old == new)
```

At the round-3 freeze `4b5fa44` the output is (all `True`):

| Extent | SHA-256 | Identical to |
| --- | --- | --- |
| `## Evidence` → `## Record state` | `cb66633b04c5b48de5f71bf6965cb27a509610e4041590e0a6996f4341fe2669` | `94cfe46` |
| `## Reviewer independence` → `## Human approval` | `6fefdb545c04e7a854f8cb8cf92c31ef821faa000ed01373a921cd8bde07cd3b` | `94cfe46` |
| `### Review round 1` → next round heading | `becc4ed4a5a77845a62ad8e07da189ab0720203bc6a9221ad5d4a88d5859c605` | `94cfe46` |
| `### Re-review round 2` → EOF | `101fff9b74de4c60bbfb10dac273314ccbbdc25fffa9a397dc1851c3828e2aa7` | `36efc80` |

The earlier round-2 note listed values produced with an unstated extent (they included the blank line before the
end heading and therefore did not reproduce); those values are removed, and the identity conclusion was and is
correct. A future round should compare its predecessor's section with the same snippet, ending that extent at the
new round heading.

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

### Re-review round 2

#### Independence and frozen state

I am a fresh context. I did not author, design, or implement the reviewed work and have no shared conversation
with the planner, implementor, round-1 reviewer, or orchestrator. I reviewed the repository at record HEAD
`562bf0e6b8c5cbe32357557efcf06ea26ee49f12` and the frozen design at
`1b82fde744d02cfdb92cecbdad5a5165d62e8b9c`. `git diff --name-status 1b82fde 562bf0e` reports exactly
`M docs/planning/reviews/step-23.md`; the freeze-to-head delta is the record only, and the request's
`Reviewed commit` cell is `1b82fde`. The branch is `docs/step-23-design` at `562bf0e`; round 1's record commit
is `94cfe46`. The worktree was clean before this record edit; no repository file outside this record was
modified.

#### Record integrity

I extracted the round-1 reviewer-owned sections from `git show 94cfe46:docs/planning/reviews/step-23.md` and
from the reviewed record at `562bf0e` (before this round-2 append; the `## Review rounds` extent is the
pre-append content) with line-anchored `awk` ranges and compared the bytes:

| Section | Extent used | 94cfe46 SHA-256 | 562bf0e SHA-256 | Result |
| --- | --- | --- | --- | --- |
| Evidence, Findings, Finding details | `## Evidence` to `## Record state` | `94f94652470bff170246f280c081ccfd25c9c9d0ca5502f7275fb22e9e9a896b` | same | byte-identical |
| Reviewer independence and Verdict | `## Reviewer independence` to `## Human approval` | `d911b8f4a073fd630f9e9240ce3779f727049e4b32a2fa573b33d2e03b84778c` | same | byte-identical |
| Review round 1 | `## Review rounds` to EOF | `36c496df3b1ec4f618b7e36232e92ee75322ef9f95b50cfefed931a65364b7d5` | same | byte-identical |

`diff` of each pair is empty, so the round-1 reviewer text is byte-identical to `94cfe46`. The
`Record-integrity note (round 2)` lists different values (`74f4c…`, `7b16d…`, `adede…`) for the same extents;
those values do not reproduce with the stated extents or with several reasonable variants, although the
identity conclusion itself is correct. This is recorded as m-16.

#### Commands and results

Run in `/tmp/opencode/step-23-review-r2` at `562bf0e` (the review worktree; no repository file changed apart
from this record):

- `./tools/verify.sh docs --require` — pass; the closeout gate completed with required checks enabled
  (math/link checks, strict Zensical build, DocFX, rustdoc).
- `./tools/verify.sh policy --require` — pass; the closeout gate completed with required checks enabled
  (NuGet licence/vulnerability audit, ApiCompat, contract artifacts, projections, catalog, process consistency,
  tool tests, SPDX headers, secret scan, cargo-deny).
- `python tools/check_doc_math.py` — pass, 173 files scanned.
- `git diff --name-status 1b82fde 562bf0e` — `M docs/planning/reviews/step-23.md` only.
- Read-only inspection of `AGENTS.md` §3, §8–§12; `docs/planning/reviews/README.md`; the Step 23 page (all
  sections); `04-roadmap.md` §3/§3.1/§5; `01-target-architecture.md` §3.2; `03-quality-strategy.md` §11.1;
  `05-open-decisions.md`; `code-structure.md` §2.3; `appendix-b-parity-matrix.md`; `status.md` row 23;
  `step-07-validation-and-diagnostics.md` §2/§8; ADR 0003/0006; `Directory.Packages.props`;
  `src/dotnet/Mocassin.Packages/Projects/{ProjectStore,ProjectStoreOptions,ProjectStoreResults,ProjectLoadFailureKind,ProjectFileLock}.cs`;
  `src/dotnet/Mocassin.Packages/Projects/Validation/ProjectValidationPipeline.cs`;
  `src/dotnet/Mocassin.Core/Editing/ProjectDocument.cs`;
  `src/dotnet/Mocassin.Core/Project/{Project,ProjectCanonicalizer}.cs`;
  `src/dotnet/Mocassin.Packages/Projects/Templates/ProjectTemplateFactory.cs`;
  `src/dotnet/Mocassin.Cli/NewProject/NewProjectCommand.cs` and
  `src/dotnet/Mocassin.Cli.Tests/NewProject/NewProjectCommandTests.cs`;
  `src/dotnet/Mocassin.App*` project files, `AppViewModelsModule.cs`, `AppViewsModule.cs`,
  `PublicAPI.Shipped.txt`, and `Program.cs`; `tools/api-projects.txt`, `tools/check-api-compat.sh`, and
  `tests/contracts/api-baseline/net11.0/Mocassin.App.*.dll`.

#### Finding closure — M-1, M-2, M-3

**M-1 is resolved in substance.** The refreeze specifies the single `ProjectStore` with
`ProjectStoreOptions { ValidateOnLoad = false }` (§5/§8/§18), the in-memory
`ProjectValidationPipeline.Validate(project, loadDiagnostics: session.LoadDiagnostics,
ValidationOptions.Default, compiledModel: null, cancellationToken)` call (§8), no `ValidateFileAsync` use by the
GUI, and a PR-fast test plus acceptance criterion (§20, acceptance criteria). I verified this against the real
code: `ProjectStoreOptions.ValidateOnLoad` defaults to `true`; `ProjectStore.LoadBytesCore` calls the validator
only when it is true; `ProjectLoadResult.Diagnostics` exists; and the real public `Validate` signature is
`(Project, ImmutableArray<Diagnostic> loadDiagnostics = default, ValidationOptions? options = null,
CompiledModel? compiledModel = null, CancellationToken cancellationToken = default)`, which matches the design
call. There is no double validation and no second validator. Residual **m-14**: the "`Load` stage is not
`Skipped`" wording is only true when `ProjectLoadResult.Diagnostics` is non-empty; a clean load correctly yields
`Skipped` per Step 07 §2 because `LoadBytesCore` returns empty diagnostics.

**M-2 is resolved for the code boundary.** The feature list is dependency-ordered: feature 1 delivers the
dialog overlay, pickers, dispatcher, console host/Application channel, notifications, and progress; feature 2
the workspace; feature 3 settings/theme/accessibility; feature 4 lifecycle; feature 5 edit/undo; feature 6 the
whole §8 validation surface; feature 7 dirty/autosave/recovery; feature 8 tests/docs/closeout. Every feature
uses only lower-numbered features plus the merged 01/03–07/12 libraries, and 23B (5–8) needs nothing from 23A
beyond its delivered code; 23A (1–4) contains no 23B code task. Residual **m-17** (23A's documentation set
includes pages whose §19 content covers 23B capabilities) and **n-13** (feature 1's picker last-directory state
and settings-sourced log level are wired against feature 3's settings model; clarify the in-memory/default
seam). Neither inverts the 23A/23B code boundary.

**M-3 is resolved for placement.** §1's I/O-boundary table, §13, §14, §15, `01-target-architecture.md` §3.2,
and `code-structure.md` §2.3 agree: view models keep contracts/state/pure logic and call only the shared
library APIs; `Mocassin.App` implements `AppSettingsStore`, `RecoveryStore`, `ProjectFileProbe`, `AppPaths`, and
log retention; `Mocassin.App.Views` implements `AvaloniaThemeApplier`, `AvaloniaStorageService`,
`AvaloniaUiDispatcher`, and key binding. The §15 lists place these consistently and no
`System.IO`/`Environment.SpecialFolder`/`Application`/resource-dictionary implementation remains in view
models. Residual **m-13**: the source-token test as specified would fail on the permitted `IUiDispatcher` (the
token `Dispatcher` occurs in its name and in every view model that uses it) and does not ban `Environment` /
`Application` as broadly as §1/§13/§14 and the architecture/code-structure pages state. Refine the tokens or
document the allow-list; otherwise the test cannot be both green and enforcing.

#### Closure of the round-1 Minors and Notes

| Item | Status | Round-2 check |
| --- | --- | --- |
| m-1 | Superseded | No person-week arithmetic or contingency remains; §23 is class/evidence/split only. |
| m-2 | Superseded | No pw stop guard; the one-unit guard is trigger-based (harness, M-1, M-3). |
| m-3 | Fixed | §16 now says the existing tests pin codes/exit codes only and adds new CLI message-text tests; `NewProjectCommandTests` still asserts only codes/exit codes. |
| m-4 | Fixed | §21 and quality §11.1 remove absolute PR-fast durations; budgets are non-gating evidence. |
| m-5 | Fixed | §17 has `paths.lastOpenDirectory`/`lastSaveDirectory`/`lastFolderDirectory`; §11 and §20 use them. |
| m-6 | Fixed | §9/§17 add `AppSettingsJsonContext`, temp + `Flush(flushToDisk: true)` + replace, `MaxSettingsBytes = 4 MiB`, the `recentFiles` cap, and the failed/oversized-write guarantee. |
| m-7 | Fixed | §10 defines `KeyGestureDescriptor` with `Primary`; the table uses `primary`; only Views binds Avalonia. |
| m-8 | Partly fixed | The named types are listed; the PublicAPI claim is superseded by M-4 (existing public markers). |
| m-9 | Fixed | §20 checks direct `.csproj` references and explicitly allows the expected `App → Packages → Compilation` edge. |
| m-10 | Fixed | §5 adds `ActivateAsync`, `ReloadAsync`, `RecoverAsync`, `DiscardRecoveryAsync` and outcomes; residual m-12 for `TooLarge`. |
| m-11 | Fixed | §6/§13 make the crash path asynchronous with a 2 s budget; no `.Result`/`GetAwaiter().GetResult()`. |
| n-1 | Deferred | `status.md` row 23 still says `Not started`/`Not requested`; the record and step page flag the step-agent follow-up. |
| n-2 | Partly fixed | The outcome enum adds `NotFound`/`InvalidContent`/`IoFailure`; `TooLarge` is still missing — m-12. |
| n-3 | Fixed | `IInspectorContributor.CreateInspector(InspectorSelection)` and the typed selection record. |
| n-4 | Fixed | Feature 6 owns the whole §8 surface; §8 and the feature table say so. |
| n-5 | Fixed | The dangling reference is replaced by `WindowPlacementResolver`, a PR-fast test, and V-5. |
| n-6 | Partly fixed | `ProjectPathRules` is the helper in §16/§5/§20, but architecture §3.2 and the parity matrix still say `ProjectNameRules` — m-15. |
| n-7 | Fixed | `TimeProvider` is scoped to document/autosave/stamp/session/retention; Serilog owns log timestamps. |
| n-8 | Superseded | The "one sitting" claim is gone; evidence size is a High dimension and drives the split. |

#### New findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M-4 — The "every App type internal / `PublicAPI.*` unchanged / no ApiCompat baseline refresh" claim contradicts the existing public App marker types and the checked-in App API baselines.** | Step 23 §15: "Every type in the three App projects is `internal` … Consequently `PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt` stay unchanged for `Mocassin.App*`, and the `PublicApiAnalyzers` references in those projects stay satisfied with empty lists." §14 repeats "Every type … is `internal`"; §16 says "Because every change is additive, no ApiCompat baseline refresh is expected"; §1 nevertheless shows `public static class Program` and `public sealed class App`. Repository: `src/dotnet/Mocassin.App.ViewModels/AppViewModelsModule.cs` declares `public static class AppViewModelsModule` and `PublicAPI.Shipped.txt` lists it; `src/dotnet/Mocassin.App.Views/AppViewsModule.cs` declares `public static class AppViewsModule` and its `PublicAPI.Shipped.txt` lists it; `tests/contracts/api-baseline/net11.0/Mocassin.App.ViewModels.dll` and `Mocassin.App.Views.dll` are checked in; `tools/api-projects.txt` lists both, and `./tools/verify.sh policy` runs ApiCompat for both. Making the markers internal/removing them must change the shipped lists and refresh the baseline (a deliberate, non-additive public-API change); keeping them public contradicts the "every type internal" rule. Fix: state the disposition (remove/replace the markers and record the intentional `PublicAPI`/ApiCompat update, or keep them as the documented public exceptions) and align §14/§15/§16 and the api-gate plan. |
| **Minor** | **m-12 — The `ProjectSessionOutcome` enum still does not cover `ProjectLoadFailureKind.TooLarge` (n-2 incomplete).** | Step 23 §5 says "the three load failures (`NotFound`, `InvalidContent`, `IoFailure`, mapped from `ProjectLoadResult.FailureKind`)"; `ProjectLoadFailureKind` has `None`, `NotFound`, `TooLarge`, `Io`, `InvalidContent`; `ProjectStore.LoadFromPathAsync` returns `TooLarge` for a file over `MaxDocumentBytes` (64 MiB) and `Io` for I/O failures. The outcome list and mapping must include all four or state the mapping explicitly. |
| **Minor** | **m-13 — The source-token architecture test as specified is neither green on the permitted code nor a complete enforcement of the stated rule.** | Step 23 §1/§20 scan for `System.IO`, `File.`, `Directory.`, `Environment.SpecialFolder`, `Avalonia`, `Dispatcher`, and `ResourceDictionary`. §15 places `Shell/IUiDispatcher.cs` in `Mocassin.App.ViewModels` and view models reference `IUiDispatcher`, so the `Dispatcher` token matches permitted code. The list bans only `Environment.SpecialFolder`, not `Environment`, and has no `Application` token, while §1/§13/§14, architecture §3.2, and code-structure §2.3 ban `Environment` and `Application`. Refine to e.g. `Dispatcher.UIThread`/`Avalonia.Application`/`Environment.` or state an allow-list for `IUiDispatcher`; otherwise the test cannot both pass and enforce the rule. |
| **Minor** | **m-14 — The "`Load` stage is not `Skipped`" requirement is true only for a load/migration fixture; a clean load is correctly `Skipped`.** | Step 23 §8 and §20 (and feature 4's done-when) say the `Load` stage is "populated … instead of being skipped" and "is not `Skipped`"; the acceptance criterion seeds it from `ProjectLoadResult.Diagnostics`. Step 07 §2 says the in-memory `Load` stage is `Skipped` with count 0 when the caller supplies no load diagnostics, and `Completed`/`Failed` when it does. `ProjectStore.LoadBytesCore` returns empty diagnostics for a clean current-version load; only migration/load diagnostics make the stage non-skipped. The M-1 fix (pass `ProjectLoadResult.Diagnostics` + `ValidateOnLoad = false`) is right; scope the assertion to a fixture with a load/migration diagnostic and state the clean-load behaviour. |
| **Minor** | **m-15 — The `ProjectNameRules` → `ProjectPathRules` rename is incomplete (n-6).** | Step 23 §16 and the resolution say the shared helper is `ProjectPathRules`; `01-target-architecture.md` §3.2 still says the CLI and GUI share `ProjectNameRules`, and `appendix-b-parity-matrix.md` row "Create new project" still says `ProjectNameRules`; `grep` finds no `ProjectNameRules` type in `src/dotnet`. |
| **Minor** | **m-16 — The record-integrity note's SHA-256 values do not reproduce.** | My independent extraction of the three stated extents gives `94f946…`, `d911b8…`, and `36c496…` (all identical between `94cfe46` and `562bf0e`); the note lists `74f4c…`, `7b16d…`, and `adede…`. The byte-identity conclusion is correct; the listed hashes should be corrected or removed. |
| **Minor** | **m-17 — The 23A documentation set names pages whose planned content covers 23B capabilities.** | §23 says 23A contains no task from 23B; the feature table's 23A documentation set includes `quickstart.md` and `first-project.md`, whose §19 content includes validate, save/recover, undo, autosave, and recovery (features 5–7, 23B). Under the split, either scope those pages to 23A or complete their 23B sections in 23B. |
| **Note** | **n-9 — §14 contains two consecutive `Mocassin.App` bullets that both enumerate `Program`, `App.axaml(.cs)`, `ViewLocator`, and the composition root.** | Step 23 §14 lines 858–865; the second bullet adds the `ViewLocator` safeguards. Merge them. |
| **Note** | **n-10 — The §1 singleton list omits `ProjectStore` (and does not say where `ProjectValidationPipeline` is constructed) although §5 relies on the single store.** | Step 23 §1 lifetime list vs §5 ("The single `ProjectStore` instance registered in the composition root") and §8 (pipeline use). Add the store to the registration/lifetime list so the M-1 test can resolve the configured store, and state the pipeline's lifetime. |
| **Note** | **n-11 — §1's prose omits the direct `Mocassin.Contracts` reference that the current `Mocassin.App.ViewModels.csproj` has.** | Step 23 §1 says the project "already references `Mocassin.Packages`, `Mocassin.Results`, and (transitively) `Mocassin.Core`"; `Mocassin.App.ViewModels.csproj` also references `Mocassin.Contracts` directly, and the dependency diagram includes Contracts. Align the prose. |
| **Note** | **n-12 — §1 shows public `Program`/`App` types while §15 says every App type is internal.** | Step 23 §1 code snippets vs §15 "Every type in the three App projects is `internal`." Scope the internal rule to the library projects or state the entry-point exceptions; this is the same cluster as M-4. |
| **Note** | **n-13 — Feature 1's picker last-directory state and settings-sourced log level depend on feature 3's settings model.** | Feature 1 scope ("`IStorageService` pickers with last-directory state"; logging with the settings minimum level) vs feature 3 (settings model/store). Clarify that feature 1 uses in-memory/default state and feature 3 wires persistence, or move the persistence detail to feature 3; both are inside 23A, so this is not a 23A/23B inversion. |

#### Complexity class check

Class **High** is honest: four of the seven §3.1 dimensions are High (cross-step interfaces, test-matrix
breadth, independent acceptance stories, evidence size), the class is the highest dimension rather than an
average, and §23 cites concrete evidence (five consuming GUI steps, a new application project plus a
headless/screenshot/accessibility harness, three independent acceptance stories, eight features, four library
changes, six documentation pages, and a ~1,400-line design page). No person-week figure is a gate: §23 says the
split decision is trigger-based, the §3 forecast is explicitly non-gating, and the one-unit guard is the
harness/M-1/M-3 trigger, not a time guard. The split after feature 4 is dependency-clean for the code, and the
one-unit case and risk guard are recorded; the owner decision is correctly still outstanding. Residual m-17 and
n-13 are documentation/ordering details, not a class problem.

#### Verdict

**Changes required.** The refreeze resolves the round-1 Majors M-1–M-3 in substance: the validation contract
matches the real `ProjectStore`/`ProjectValidationPipeline` signatures, the feature list is dependency-ordered
with a clean 23A/23B code boundary, and the I/O boundary is placed correctly. It is not ready for implementation
because of the new Major **M-4**: the "every App type internal / `PublicAPI.*` unchanged / no ApiCompat baseline
refresh" claim contradicts the existing public `AppViewModelsModule`/`AppViewsModule` types, their
`PublicAPI.Shipped.txt` entries, and the App API baselines that `./tools/verify.sh policy` checks. The Minor
items m-12–m-17 and Notes n-9–n-13 should be fixed or explicitly accepted together with M-4. The round-1
verdict remains as history; the owner decides the next freeze.

Follow-up owner: planner/step agent — resolve M-4 and the Minors/Notes, then re-freeze.
Follow-up step: 23 — design re-review at the next freeze commit; earlier rounds stay in this record.

### Re-review round 3

#### Independence and frozen state

I am a fresh context. I did not author, design, or implement the reviewed work and have no shared conversation with
the planner, implementor, the round-1 or round-2 reviewer, or the orchestrator. I reviewed the repository at record
HEAD `f9c922eb10444d0a66fa97bf88797c1d72b29cff` and the frozen design at
`4b5fa44c14bd5a3f145203eb81a1b8b5a5f71993`. `git diff --name-status 4b5fa44 f9c922e` is exactly
`M docs/planning/reviews/step-23.md`; `git diff --name-status 36efc80 4b5fa44` is the six design/docs files
(`docs/development/code-structure.md`, `docs/planning/01-target-architecture.md`,
`docs/planning/03-quality-strategy.md`, `docs/planning/05-open-decisions.md`,
`docs/planning/appendix-b-parity-matrix.md`, and the Step 23 page) and no code, test, configuration, contract,
generated file, PublicAPI list, or API baseline. The worktree was clean before this record edit, so the only
repository file changed by this round is this record.

#### Record integrity (m-16)

I ran the snippet in the `Resolution status` note verbatim from the worktree root. The four extents reproduce
exactly, and every comparison is `True`:

| Extent | SHA-256 | Identical to |
| --- | --- | --- |
| `## Evidence` → `## Record state` | `cb66633b04c5b48de5f71bf6965cb27a509610e4041590e0a6996f4341fe2669` | `94cfe46` |
| `## Reviewer independence` → `## Human approval` | `6fefdb545c04e7a854f8cb8cf92c31ef821faa000ed01373a921cd8bde07cd3b` | `94cfe46` |
| `### Review round 1` → the next round heading | `becc4ed4a5a77845a62ad8e07da189ab0720203bc6a9221ad5d4a88d5859c605` | `94cfe46` |
| `### Re-review round 2` → EOF (or `### Re-review round 3` after this append) | `101fff9b74de4c60bbfb10dac273314ccbbdc25fffa9a397dc1851c3828e2aa7` | `36efc80` |

The round-1 and round-2 reviewer-owned text is therefore byte-identical to `94cfe46` and `36efc80` respectively.
The earlier round-2 note's non-reproducing values are gone; the corrected note's extents, trailing-newline
normalization, and hashes all reproduce. m-16 is closed.

#### Commands and results

Run in `/tmp/opencode/step-23-review-r3` at `f9c922e` (the review worktree; no repository file changed apart from
this record):

- `./tools/verify.sh docs --require` — pass; the closeout gate completed with required checks enabled (strict
  Zensical build, DocFX, rustdoc).
- `./tools/verify.sh policy --require` — pass; "Public API is compatible with the checked-in baseline". ApiCompat
  reported "ran successfully without finding any breaking changes" for `Mocassin.App.ViewModels` and
  `Mocassin.App.Views`; contract artifacts current (26 files), projection checks, process consistency, 58 tool
  tests, SPDX headers, secret scan, and cargo-deny all passed.
- `python3 tools/check_doc_math.py` — pass, 173 files scanned.
- `git status --porcelain` — empty before this record edit.
- Read-only inspection of `AGENTS.md` §3/§8–§12; `docs/planning/reviews/README.md`; the full record; the Step 23
  page (all sections); `04-roadmap.md` §3.1; `01-target-architecture.md` §3.2; `03-quality-strategy.md` §11.1;
  `05-open-decisions.md`; `code-structure.md` §2.3; `appendix-b-parity-matrix.md`; `status.md` row 23;
  `src/dotnet/Mocassin.Packages/Projects/{ProjectLoadFailureKind,ProjectStoreOptions,ProjectStore}.cs`;
  `src/dotnet/Mocassin.Packages/Projects/Validation/ProjectValidationPipeline.cs`;
  `src/dotnet/Mocassin.Core/Editing/ProjectDocument.cs`;
  `src/dotnet/Mocassin.Packages/Projects/Templates/ProjectTemplateFactory.cs` and `ProjectTemplate.cs`;
  `src/dotnet/Mocassin.App*/**` project files, marker types, `PublicAPI.*`, and `Program.cs`;
  `tools/api-projects.txt`; the App API baselines; and the Step 01/07/12 tests and CLI rules.

#### Closure — M-4

**M-4 is resolved in substance.** The repository has `public static class AppViewModelsModule` and
`public static class AppViewsModule`; each appears in its project's `PublicAPI.Shipped.txt`, both
`tests/contracts/api-baseline/net11.0/Mocassin.App.ViewModels.dll` and `Mocassin.App.Views.dll` are checked in,
and `tools/api-projects.txt` lists both projects. `Mocassin.App` is not listed there, has no `PublicAPI.*` files,
and its only current source type is the public `Program`. The freeze touches none of these files, and
`./tools/verify.sh policy` passes ApiCompat for both App assemblies without a baseline refresh. The design now
scopes the internal rule to new types, names the markers and entry points as the public exceptions, and keeps
`PublicAPI.Shipped/Unshipped` and both baselines unchanged. No new public App type is added to an ApiCompat
project.

Residual wording (recorded as **m-18** below): `Mocassin.App/App.axaml(.cs)` does not exist at the freeze
(`git ls-files src/dotnet/Mocassin.App` returns only `Mocassin.App.csproj` and `Program.cs`), so `App` is added by
Step 23, not pre-existing. The design's "the ones that already existed" and "adds or removes no public type in
the App projects" are therefore inaccurate for `Mocassin.App`; the compatibility claim itself is still true
because `Mocassin.App` is not an ApiCompat project.

#### Closure — m-12

The real enum is `ProjectLoadFailureKind { None, NotFound, TooLarge, Io, InvalidContent }`, and
`ProjectStoreOptions.MaxDocumentBytes` defaults to `64L * 1024L * 1024L` (64 MiB). `ProjectStore.LoadFromPathAsync`
returns `TooLarge` for a document above that limit, `Io` for an OS/sharing failure, `NotFound` for a missing file,
and `InvalidContent` for parse/schema/migration failures. The refrozen `ProjectSessionOutcome` contains
`NotFound`, `TooLarge`, `Io`, and `InvalidContent` alongside the success/lock/save outcomes, and the design states
the mapping is a pure switch with no silent fallback to `Io`. The four failure kinds mirror the real enum exactly
(the success outcomes cover `None`). m-12 is closed.

#### Closure — m-13

The refrozen rule is an exact token list, not the old bare-word scan: after stripping `//` and `/* */` comments and
string literals, `Mocassin.App.ViewModels/**/*.cs` must not contain `System.IO`, `File.`/`FileInfo`,
`Directory.`/`DirectoryInfo`, `Path.Combine`/`Path.Get`, `FileStream`, `StreamReader`, `StreamWriter`,
`Environment.`, `Avalonia`, `Dispatcher.UIThread`, `Application.Current`, or `ResourceDictionary`. `IUiDispatcher`
and bare `Dispatcher`/`Application` identifiers are explicitly permitted, so the permitted ViewModels type
`IUiDispatcher` and every view model that injects it no longer false-positive. The list catches `System.IO`,
`Environment.SpecialFolder`/`Environment.CurrentDirectory`, `Application.Current`, and `ResourceDictionary`; the
positive control feeds a seeded violation through the same scanner and asserts it is reported. m-13 is closed for
the named tokens and permitted usages; the substring nature of `File.`/`Directory.` is a residual Note (n-15).

#### Closure — m-14

The real pipeline confirms the corrected semantics: `ProjectValidationPipeline.ValidateCore` adds
`StageExecution.Skipped(ValidationStage.Load)` when `loadDiagnostics` is empty, and adds `Completed` (or `Failed`
when an error is present) when it is non-empty. `ProjectStore.LoadBytesCore` returns empty diagnostics for a clean
current-version load, so a clean load is correctly `Skipped` with count 0. The refrozen §8, §20, feature 4, and
the acceptance criterion all state this, and the test covers both a load/migration fixture and a clean fixture.
**m-14 is only partly closed:** two duplicate unconditional sentences remain. The §5 store-configuration bullet
(lines 448–454) still says "the pipeline's `Load` stage is populated instead of skipped", and the §24 decisions
table (line 1305) still says "no skipped `Load` stage". Both are false for a clean load and contradict §8/§20 and
Step 07 §2. Severity **Minor**; correct or scope the two duplicate statements.

#### Closure — m-15, m-17, and n-9–n-13

| Item | Status | Round-3 check |
| --- | --- | --- |
| m-15 | Closed | `grep -rn "ProjectNameRules"` over the repository finds no occurrence outside this review record (reviewer history only); `01-target-architecture.md` §3.2 and the parity-matrix "Create new project" row now say `ProjectPathRules`. |
| m-17 | Closed | §19 is a 23A/23B column table. `installation.md` is scoped to 23A; `quickstart.md`, `first-project.md`, `projects.md`, `ui-architecture.md`, `troubleshooting/installation.md`, and `file-layout.md` name their 23A content and 23B completions. Feature 4 writes the 23A column; feature 8 writes the 23B column and navigation. No page ships a 23B capability as 23A. |
| n-9 | Closed | §14 has one merged `Mocassin.App` bullet with the entry points, `App.axaml(.cs)`, `ViewLocator` safeguards, composition root, and the I/O implementations (`AppSettingsStore`, `RecoveryStore`, `ProjectFileProbe`, `AppPaths`, log retention). |
| n-10 | Closed | §1 registers one `ProjectStore` constructed with `new ProjectStoreOptions { ValidateOnLoad = false }`, one stateless/thread-safe `ProjectValidationPipeline`, and the settings/recovery/probe singletons, and injects the store and pipeline into `ProjectSessionService`. The M-1 test can resolve the store because `ProjectStore.Options` is public. |
| n-11 | Closed | `Mocassin.App.ViewModels.csproj` directly references `Mocassin.Contracts`, `Mocassin.Packages`, and `Mocassin.Results`; `Mocassin.Packages` and `Mocassin.Results` reference `Mocassin.Core`, so the design's "direct Contracts/Packages/Results, transitive Core" matches the project file. |
| n-12 | Closed | The internal rule is scoped to new types; `Program` and `App` are named entry-point exceptions, and `Mocassin.App` is correctly described as not an ApiCompat project. The `App` "pre-existing" wording is m-18. |
| n-13 | Closed | Feature 1 uses an in-memory/default last-directory seam and the default log level; feature 3 wires both to the persisted settings model. Both remain inside 23A, so the split boundary is unchanged. |

#### Complexity, dependencies, scope, and parity

Class **High** still holds. Four of the seven §3.1 dimensions are High (cross-step interfaces, test-matrix
breadth, independent acceptance stories, evidence size), the class is the highest dimension rather than an average,
and §23 records concrete evidence (five consuming GUI steps, a new application project plus a headless/screenshot/
accessibility harness, three independent acceptance stories, eight features, four library changes, six
documentation pages, and a ~1,400-line design page). No person-week figure is used as a gate: the §3 forecast is
explicitly non-gating, §23's guard is the harness/M-1/M-3 trigger, and the step page says "no time waiver is
requested". The split after feature 4 is dependency-clean for the code (23A features 1–4; 23B features 5–8; 23B
needs only 23A's delivered surfaces), and the owner decision is correctly recorded as outstanding. m-17 and n-13
were the only documentation/ordering residuals; both are closed.

The dependencies are limited to the merged library surfaces of Steps 01, 03–07, and 12; those step pages show
`Done` in `status.md`. No Step 13–22 API is consumed: §18's consumed table contains only `ProjectStore`,
`ProjectDocument`, `ProjectEditor`, `ProjectTemplateFactory`, `ProjectValidationPipeline`, and related
Step 01–07/12 types. The two new packages are the first-party test-only `Avalonia.Headless` and
`Avalonia.Headless.XUnit` (same pinned version, MIT-covered), and the four library changes
(`ProjectDocument.MaxUndoDepth`, `ProjectDocument.CreateRecovered`, `ProjectStore.WriteAutosaveToPathAsync`, and
`ProjectPathRules.NormalizeName/ValidateTarget` plus the CLI refactor) are additive; `ProjectDocument` and
`ProjectStore` still have their current surface, and `ProjectTemplateKind` still has `Blank`/`Ceria`. The parity
rows name Step 23 truthfully as `Planned`/`Partial`, and the documentation plan creates the missing pages during
implementation rather than linking to absent files. No hidden Steps 24–28 implementation is present: the design
provides contribution seams, not editors, build UI, 3D, or run monitoring. `status.md` row 23 still says
`Not started`/`Not requested`; this is the known step-agent bookkeeping follow-up (n-1), not a design change.

#### New findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **m-18 — The "pre-existing `Program`/`App`" and "adds or removes no public type in the App projects" wording is inaccurate for `Mocassin.App`.** | `git ls-files src/dotnet/Mocassin.App` returns only `Mocassin.App.csproj` and `Program.cs`; there is no `App.axaml(.cs)` or `App` class at the freeze. Step 23 adds `public sealed class App : Application` (§1) and the `App.axaml(.cs)` files (§15). The compatibility claim is still true because `Mocassin.App` is not in `tools/api-projects.txt` and has no `PublicAPI`/baseline, but the "already existed" and "no public type added" statements should be corrected. |
| **Note** | **n-14 — `IRecoveryWriter` is named in feature 1 and §6 but is not defined in §1/§15.** | Feature 1 and §6 say feature 1 registers a null `IRecoveryWriter` until feature 7; §1 and §15 define `IRecoveryStore` (list/write/delete) and `CrashRecoveryWriter`, not `IRecoveryWriter`. Align the name or state that it is the write half of `IRecoveryStore`. |
| **Note** | **n-15 — The token list is a substring scan, not a complete lexical ban.** | §1/§20 ban `File.`/`Directory.`/`Environment.` but not the bare identifiers; `using static System.Environment;` or `using static System.IO.File;` can evade the scan, and a property access such as `LastOpenDirectory.Trim()` can match `Directory.`. The named permitted usages (`IUiDispatcher`/`Dispatcher`, bare `Application`) are safe; this is an observation about the guard's limits, not a false-positive on the design's permitted code. |

#### Verdict

**Ready with notes.** The round-2 Major M-4 is resolved in substance: the existing public App markers and the
`Program` entry point are documented exceptions, new types are internal, and `PublicAPI.Shipped/Unshipped` and both
App ApiCompat baselines are unchanged and pass `./tools/verify.sh policy`. m-12, m-13, m-15, m-16, m-17, and
n-9–n-13 are closed; the complexity class and split are sound and the owner decision remains outstanding. The
remaining findings are Minor/Note only: the m-14 residual (two duplicate unconditional `Load`-stage sentences),
m-18 (`App` is added, not pre-existing), n-14 (`IRecoveryWriter` naming), and n-15 (token-scan limits), plus the
known n-1 dashboard bookkeeping. The human owner must explicitly accept these notes and the start gate (including
the §23 split decision); if the owner requires every round-2 Minor to be fully closed, correct the two m-14
sentences and the m-18 wording and re-review that small diff.

Follow-up owner: planner/step agent — correct the m-14 duplicate sentences, the m-18 wording, and the
`IRecoveryWriter` name; the owner accepts the notes and the start gate.
Follow-up step: 23 — start gate after owner acceptance; earlier rounds stay in this record.

