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
| Reviewed commit | `9916166a664f0a588e82eb8dbfc1097bc4949845` |
| Docs branch | `docs/step-23-design` |
| Review date | 2026-11-20 |
| Reviewer | — |

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
| Sizing | Design estimate **3.75–4.5 pw**, eight features. The upper bound exceeds the four-week §3.1 trigger, so §23 requests an owner decision: a recorded single-step exception, or a 23A/23B split at feature 5. **No owner approval is recorded yet.** Guard: stop and report before feature 5 if features 1–4 exceed 2.2 pw |

## Design decisions reviewed

- [ ] Step page design decisions — goal, scope, non-goals, the shell/regions, document model, navigation and
      contribution seams, project lifecycle, dirty/autosave/recovery, edit pipeline and undo/redo, validation
      surface, settings/theming/accessibility, shortcuts/menus/palette, dialogs/consoles, logging and crash
      handling, state ownership and threading, view-model/view contracts, folder layout, the four library
      changes, the settings format, the parallel-track interface, documentation, tests, performance, risks,
      sizing, and "Done when".
- [ ] Data contracts and format changes — the new application-local `mocassin.settings` v1 (§17) and its
      deliberate exclusion from `contracts/manifest.json`; no change to `.mocprj`, `.msl`, `.msr`, `.mcs`, CLI
      reports, exit codes, diagnostics codes, or signatures.
- [ ] Architecture and module boundaries — `01-target-architecture.md` §3.2 (App/ViewModels/Views ownership and
      the dependency direction), §3.1, and the UI-thread row in §5.3; the "one writer per project", "no
      speculative service interfaces", and "no `Mocassin.Cli` reference" rules.
- [ ] ADRs and open decisions — ADR 0003 (Avalonia/MVVM/DI, no new control dependency), ADR 0006 (3D untouched),
      ADR 0008/0011/0013 (unaffected), and the new resolved rows plus the Step 23 sizing gate in
      `05-open-decisions.md`.
- [ ] Quality strategy and test plan — `03-quality-strategy.md` §6 item 6, §8 tier row, and §11.1; the §20 test
      table with tiers, the real-object/fake boundary list, the screenshot policy, and the performance budgets
      in §21.
- [ ] Parity matrix and documentation plan — the Step 23 rows in
      `appendix-b-parity-matrix.md` §1/§9/§11 and the §19 documentation table; no row claims an unimplemented
      capability as done.
- [ ] Licensing and provenance — no new product dependency beyond the packages already pinned in
      `Directory.Packages.props`; the two test-only Avalonia headless packages are first-party, same-version,
      MIT-covered; no GPL code or data is copied; the original repository is used read-only.
- [ ] Post-implementation audit plan — the implementation-status block, feature log, closeout evidence
      (`docs/planning/closeout/step-23.json`), `check_closeout_evidence.py`, the platform-gate evidence, and the
      audit request sequence.

### Step-specific decisions

- [ ] **Composition root and dependency direction.** `AppHost`/`AppServiceRegistration` build the `IHost`; only
      `Program`/`App` resolve it; explicit registration only (no scanning, no locator); per-project objects are
      constructed by `ProjectSessionService`, not by DI scopes; `App.ViewModels` has no Avalonia reference;
      `ViewLocator` conventions and safeguards.
- [ ] **Document model and tabs.** `DocumentViewModel`/`DocumentKey`/`DocumentCollectionViewModel`, project-scoped
      keys, one global tab strip, welcome document, the Project Overview and Project JSON documents, and the
      inspector `ContentControl` contract.
- [ ] **Navigation and contribution seams.** `INavigationService` with bounded history,
      `INavigationTargetProvider` (one provider per domain), unknown-domain behaviour, project explorer plus
      `IExplorerNodeProvider`, and the inspector/menu/console/status seams — verify they are sufficient for
      Steps 24–28 and that no seam takes an Avalonia type.
- [ ] **Project lifecycle.** `IProjectSessionService` over the concrete `ProjectStore`; typed outcomes and no
      prompting inside services; new via `ProjectTemplateFactory` + shared `ProjectNameRules`; save/save-as
      lock migration; close; recents; lock/read-only mode; external-change detection; multi-project workspace;
      window-close coordination.
- [ ] **Dirty state, save point, autosave, and recovery.** The `MarkSaved()` no-argument decision and
      `LastWritten` separation, the four save-point consequences, the sidecar vs `recovery/` split, the
      per-session write serialisation, the retention rules (and the rule that sidecars are never swept), the
      unclean-exit session restore, and the crash dialog behaviour.
- [ ] **Edit pipeline and undo/redo.** Single `ProjectEditCoordinator` per project over `ProjectDocument`;
      no second stack; bounded depth; `ProjectChanged` as the only notification; rejected edits leave state
      unchanged; metadata editing as one undo entry per committed field.
- [ ] **Validation surface.** `ProjectValidationPipeline` usage with `ValidationOptions.Default`, 300 ms
      debounce, stale-result discard by document hash, status bar + Problems + navigation semantics, statistics
      on demand, and no invented diagnostics.
- [ ] **Settings, theming, density, accessibility.** `mocassin.settings` v1 field set and validation/migration
      rules, app-data root and override, additive-field rule with no schema bump, theme/density/font-scale/high
      contrast/reduce motion, WCAG 2.2 AA claims and the accessibility test coverage.
- [ ] **Shortcuts, menus, palette.** The gesture table, the `primary` modifier mapping, the "no dead menus"
      rule, the palette behaviour, and the deliberate absence of `Model`/`Build`/`Run`.
- [ ] **Dialogs, consoles, error boundaries.** In-shell overlay instead of `MsBox.Avalonia`, the
      `IDialogService`/`IStorageService` contracts, the Problems vs Application channel split, the entry cap, and
      the error/diagnostic presentation rule.
- [ ] **Logging and crash handling.** Serilog sinks and retention mapping (with V-3 fallback), the three global
      handlers, the live-UI-thread vs dead-UI-thread crash paths, and `Help → Application diagnostics` not
      duplicating the CLI `doctor`.
- [ ] **State ownership and threading.** The §13 ownership table, "no scientific logic in view models", UI-thread
      confinement of project edits, `IUiDispatcher`/`TimeProvider` boundaries, and the no-`Task.Delay` rule.
- [ ] **Library changes.** The four additive changes in §16 (`ProjectDocument.MaxUndoDepth`,
      `ProjectDocument.CreateRecovered`, `ProjectStore.WriteAutosaveToPathAsync`, `ProjectNameRules` + CLI
      refactor) are actually necessary, additive, and behaviour-preserving; the CLI's observable behaviour and
      `CLI003`/3 messages stay identical; "no fifth change" is a real constraint.
- [ ] **Parallel-track interface.** §18's consumed/not-consumed tables, the rule that Step 23 references no
      `Compilation`/`Results`/`Cli`/solver API, and the statement of how Steps 24–28 plug in.
- [ ] **Sizing and split trigger.** §23's honest 3.75–4.5 pw estimate, the exceed of the four-week trigger, the
      23A/23B boundary, the recommendation, and the fact that the owner decision is still outstanding.

## Dependency and interface checks

- [ ] Step 01 is `Done` and its `ProjectStore`, `ProjectDocument`, `ProjectEditor`, `ProjectHasher`, and
      `ProjectJson` contracts are stable. Verify the save/lock/autosave API surface the design relies on,
      including `MarkSaved` overload semantics, `ProjectStoreOptions`, `ProjectFileLock`, and the canonical
      document hash including `modifiedUtc`.
- [ ] Step 03–06 editing helpers and the `Project` value model support the metadata edit and the single write
      path without a new API. Verify `ProjectEditor.Apply` covers project metadata and that validation rejects
      an invalid metadata edit.
- [ ] Step 07 validation contract is consumed as specified: `ProjectValidationPipeline.Validate`,
      `ValidationOptions` defaults, `ValidationResult`, `ModelStatisticsService`, and the §8 GUI contract.
      Verify no second validator or diagnostic catalog is introduced.
- [ ] Step 12's `ProjectTemplateFactory`/`ProjectTemplateKind` and the CLI's name/path rules are the only
      creation path, and the `ProjectNameRules` extraction keeps the CLI's `CLI003`/3 behaviour and messages
      unchanged.
- [ ] Steps 13–22 are not required: no design element depends on their types, packages, or behaviour, and the
      seams that Steps 24–28 will use are sufficient.
- [ ] Format, versioning, and migration impact is confined to the application-local settings file; the project,
      package, results, and state formats and the contract manifest are untouched.

## Standard design-review checklist

- [ ] Scope, non-goals, and acceptance criteria are explicit and testable
- [ ] Design decisions are complete and internally consistent
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined
- [ ] Documentation, status, parity, and audit updates are planned
- [ ] Licensing, provenance, and dependency checks are resolved
- [ ] Risks and mitigations are realistic
- [ ] Code layout and namespace plan follow the structure policy
- [ ] No hidden scope, stale placeholders, or unowned decisions

## Evidence

Files reviewed:

Commands run:

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Ready for review

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Waived

Follow-up owner:
Follow-up step:

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

#### Commands and results

#### Findings

#### Verdict
