# Step 23A Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-23a.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 23A — Workspace and Project Lifecycle |
| Request owner | Planner (design freeze); the step agent takes ownership of resolutions at the start gate |
| Orchestrator | Orchestrator agent (spawns the planning auditor) |
| Step agent (implementor) | Not assigned — created from the reviewed `main` commit after the start-gate approval |
| Review initiated by | Orchestrator, on request of the planner |
| Reviewed commit | — |
| Docs branch | `docs/step-23-design` |
| Review date | 2026-11-20 |
| Reviewer | — |

Step page: [`Step 23A — Workspace and Project Lifecycle`](../steps/step-23a-workspace-and-project-lifecycle.md)

**Freeze note.** The freeze is the commit that introduces this request (the split of Step 23 into 23A/23B and
the 23A design). This record is part of that commit so the branch has no dangling links; a documentation-only
follow-up fills the `Reviewed commit` cell with the freeze SHA before the orchestrator requests the review. The
freeze contains no code, test, configuration, contract, generated file, or status-dashboard edit.

**Split note.** Step 23 was class High with three independent acceptance stories and an audit surface too large
for one sitting. The human owner approved the split on 2026-11-20 into **23A** (this step) and **23B** (editing,
diagnostics, recovery). 23A contains no 23B task; 23B depends on 23A and defines its own design but is **not
design-reviewed and not started**. The superseded unsplit record is
[`step-23.md`](step-23.md), closed as superseded with its rounds unchanged.

> Orchestration: the orchestrator spawns a fresh planning auditor after the planner has frozen the design and
> completed this request, and relays the verdict and owner decisions. The planner and step agent update the
> design docs, apply fixes on the design docs or a maintenance branch, add resolutions without altering reviewer
> text, and ask the orchestrator for re-review until the auditor no longer identifies issues. The orchestrator
> does not edit the design or the review record. The human owner approves the final verdict and the start of
> implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | F. GUI (first half of the split Step 23) |
| Depends on | 01, 03, 04, 05, 06, 07, 12 — all `Done`, merged, and tagged. Not dependent on 02/08–11 and not on Steps 13–22 |
| Delivers | The Avalonia host and composition root, shell layout, document/navigation model, read-only project documents, project create/open/save/save-as/close/reload with recents, locks, read-only mode and external-change detection, versioned settings with theming/density/accessibility, dialog/picker/console/notification/progress surfaces, structured logging with a crash boundary, and the headless harness. No editing, undo/redo, validation surface, autosave, or recovery (23B) |
| Complexity and split | Class **High** on cross-step interfaces and test-matrix breadth (Medium on the other five dimensions). **Owner acceptance of the class is required at the start gate**; no further split is proposed (the page §18 states why). The split itself was approved by the owner on 2026-11-20; Step 24 depends on 23B |

## Design decisions reviewed

- [ ] Step page design decisions — goal, scope, non-goals, shell/regions, document model, navigation and
      contribution seams, project lifecycle, settings/theming/accessibility, dialogs/consoles, logging and crash
      boundary, state ownership, view-model/view contracts and visibility, folder layout, the one library change,
      the settings format, the 23B boundary, documentation, tests, performance, risks, and the class.
- [ ] Data contracts and format changes — the application-local `mocassin.settings` v1 and its exclusion from
      `contracts/manifest.json`; no change to `.mocprj`, `.msl`, `.msr`, `.mcs`, CLI reports/exit codes,
      diagnostics codes, or signatures.
- [ ] Architecture and module boundaries — `01-target-architecture.md` §3.2 (App/ViewModels/Views ownership, the
      I/O boundary, visibility), §3.1, and the UI-thread row in §5.3.
- [ ] ADRs and open decisions — ADR 0003/0006/0008, the resolved rows in `05-open-decisions.md`, and the class
      acceptance gate.
- [ ] Quality strategy and test plan — `03-quality-strategy.md` §6/§8/§11.1 and the page's §15 test table with
      tiers.
- [ ] Parity matrix and documentation plan — the 23A rows and the §14 documentation column.
- [ ] Licensing and provenance — no new product dependency beyond the pinned Avalonia/CommunityToolkit/Serilog/
      Microsoft.Extensions stack; two test-only first-party Avalonia headless packages; no GPL code or data.
- [ ] Post-implementation audit plan — implementation-status block, feature log, `closeout/step-23a.json`, the
      platform gate, and the audit request.

### Step-specific decisions

- [ ] **Split boundary.** 23A contains features 1–5 (host/boundaries, workspace, settings/theming/a11y,
      lifecycle, tests/docs/closeout) and no 23B task; 23B needs nothing from 23A beyond delivered code. Verify
      the feature table and §13 seam table against the 23B page.
- [ ] **Composition and lifetimes.** `AppHost`/explicit registration; `Program`/`App` are the only public entry
      points; the single `ProjectStore` is registered with `ValidateOnLoad = false` and its load diagnostics are
      retained; 23A does not construct the validation pipeline; per-project objects are created by the session
      service, not by DI scopes.
- [ ] **Visibility (m-18).** Existing public `AppViewModelsModule`/`AppViewsModule` and `Program` stay public and
      baselined; 23A **adds** `public sealed class App : Application` and the `App.axaml(.cs)` files, which are
      not in an ApiCompat project and need no baseline; every other new type is `internal` with
      `InternalsVisibleTo`. Verify no `PublicAPI.*` or App baseline change is required.
- [ ] **I/O boundary and token test (m-13/n-15).** The exact token list, whole-identifier matching,
      `using static` detection, the permitted `IUiDispatcher`/`Dispatcher` and bare `Application`, the positive
      control, and the stated limitation (a tripwire, not a compiler guarantee) agree across §1/§15, `01` §3.2,
      `code-structure` §2.3, and `03` §11.1.
- [ ] **Document model and workspace.** Tab model, project-scoped keys, read-only Overview/JSON documents,
      unknown-domain notice, command catalog/palette, and the deferred dirty marker.
- [ ] **Project lifecycle.** New via `ProjectPathRules`; open/activate/save/save-as/close/reload; recents;
      locks/read-only; external-change and deleted-file handling; no unsaved prompt in 23A (nothing is
      editable); multi-project workspace.
- [ ] **Settings, theming, accessibility.** v1 schema (no `autosave`/`session`), write discipline, migrations,
      app-data layout, theme/density/font-scale/high-contrast/reduce-motion, last directories, log level, WCAG
      2.2 AA and its test coverage.
- [ ] **Dialogs, consoles, logging, crash.** In-shell overlay, picker contract, Application channel only,
      notifications/progress, Serilog retention, the three handlers, and the crash dialog **without** a recovery
      action (n-14: no `IRecoveryWriter`/`IRecoveryStore` in 23A).
- [ ] **State ownership and threading.** The state table, no scientific logic in view models, UI-thread
      confinement, `IUiDispatcher`/`TimeProvider` boundaries, no `Task.Delay`.
- [ ] **Library change.** `ProjectPathRules` + CLI refactor: additive, behaviour-preserving, CLI codes/exit
      codes/messages identical, new message-pinning tests, `PublicAPI.Unshipped.txt` entry, no baseline refresh.
- [ ] **Parallel-track interface.** 23A consumes only 01/03–07/12; no `Compilation`/`Results`/`Cli`/solver API;
      the seams are sufficient for 23B and Steps 24–28, and 23B is the only in-flight successor.
- [ ] **Complexity class.** The seven-dimension table, the High class and its normative justification, the
      Medium dimensions' evidence, and the statement that owner acceptance is required and no further split is
      proposed.

## Dependency and interface checks

- [ ] Step 01 is `Done` and its `ProjectStore`/`ProjectDocument`/`ProjectHasher`/`ProjectJson` contracts are
      stable. Verify `ProjectStoreOptions`, `ProjectLoadResult.Diagnostics`/`FailureKind`, `ProjectFileLock`, the
      save/lock/autosave API surface, and the canonical document hash including `modifiedUtc`.
- [ ] Step 03–06 editing helpers and the `Project` value model support the read-only documents without a new
      API.
- [ ] Step 07's validation contract is deferred correctly: 23A registers no pipeline and runs no validation; the
      store is configured `ValidateOnLoad = false` and the load diagnostics are retained for 23B. Verify this is
      consistent with Step 07 §8 and does not leave a gap.
- [ ] Step 12's `ProjectTemplateFactory`/`ProjectTemplateKind` and the CLI name/path rules are the only creation
      path, and the `ProjectPathRules` extraction keeps the CLI's `CLI001`/`CLI002`/`CLI003` codes, exit codes,
      file behaviour, and messages unchanged.
- [ ] Step 23B is `Not started`/not design-reviewed, depends on 23A, and needs only 23A deliverables; no 23B
      item is hidden in 23A.
- [ ] Steps 13–22 are not required: no design element depends on their types, packages, or behaviour.
- [ ] Format, versioning, and migration impact is confined to the application-local settings file; the project,
      package, results, and state formats and the contract manifest are untouched.

## Standard design-review checklist

- [ ] Scope, non-goals, and acceptance criteria are explicit and testable
- [ ] Complexity class and split decision are recorded and justified per
      [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers)
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
