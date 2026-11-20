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
| Reviewed commit | `b9c56fe1351fd2f9d13d60d7ed5b989f3287fc12` |
| Docs branch | `docs/step-23-design` |
| Review date | 2026-11-20 |
| Reviewer | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh planning auditor |

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

**Refreeze note (round 2, 2026-11-20).** Round 1 returned `Changes required` on freeze `219f784` for M-1 and
M-2, with m-3 and Notes n-1–n-7. The design was refrozen at
`34d036dacbce3b7536549c3a4f9ea1f65dc2d5d5`. The refreeze states the exact closeout identities (`step-23a` /
`step-23b`) and the `step-NN[a-z]` tooling prerequisite (M-1), restores the normative `mocassin.settings` v1
shape in 23A §6.1 with 23B referencing it (M-2), fixes the malformed `05-open-decisions.md` links (m-3), and
addresses n-1–n-7. **Round-1 reviewer text — Evidence, Findings, Reviewer independence, Verdict, and the round-1
report — is byte-identical to `c65b1a1`**; only request-owned fields (this note, the `Reviewed commit` cell, the
`Record state` line, and the `Resolution status` section) changed. The round-1 findings and verdict remain
unchanged as history.

**Refreeze note (round 3, 2026-11-20).** Round 2 returned `Ready with notes` on freeze `34d036d` for m-4 and
n-8–n-11 (n-1 deferred). The owner asked for those notes to be fixed before the start gate, and the design was
refrozen at `b9c56fe1351fd2f9d13d60d7ed5b989f3287fc12`. The refreeze reserves `InvalidTargetPath` for the GUI and
keeps the CLI's empty-output-directory `CLI002`/3 behaviour and pinned test unchanged (m-4), scopes the §15 test
row to New (n-8), makes §6.1's `file-layout.md` reference future tense (n-9), uses the lowercase `23a`/`23b`
status-row ids matching the landed process checker (n-10), and states that every new type other than `App` is
internal while `App` is the only new public App type and `Program` is pre-existing (n-11). **Round-1 and round-2
reviewer text is byte-identical to `c65b1a1` and `8a6678b`**; only request-owned fields (this note, the
`Reviewed commit` cell, the `Record state` line, and the `Resolution status` section) changed.

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

- [x] Step page design decisions — goal, scope, non-goals, shell/regions, document model, navigation and
      contribution seams, project lifecycle, settings/theming/accessibility, dialogs/consoles, logging and crash
      boundary, state ownership, view-model/view contracts and visibility, folder layout, the one library change,
      the settings format, the 23B boundary, documentation, tests, performance, risks, and the class.
- [x] Data contracts and format changes — the application-local `mocassin.settings` v1 and its exclusion from
      `contracts/manifest.json`; no change to `.mocprj`, `.msl`, `.msr`, `.mcs`, CLI reports/exit codes,
      diagnostics codes, or signatures. — see M-2.
- [x] Architecture and module boundaries — `01-target-architecture.md` §3.2 (App/ViewModels/Views ownership, the
      I/O boundary, visibility), §3.1, and the UI-thread row in §5.3.
- [x] ADRs and open decisions — ADR 0003/0006/0008, the resolved rows in `05-open-decisions.md`, and the class
      acceptance gate.
- [x] Quality strategy and test plan — `03-quality-strategy.md` §6/§8/§11.1 and the page's §15 test table with
      tiers.
- [x] Parity matrix and documentation plan — the 23A rows and the §14 documentation column.
- [x] Licensing and provenance — no new product dependency beyond the pinned Avalonia/CommunityToolkit/Serilog/
      Microsoft.Extensions stack; two test-only first-party Avalonia headless packages; no GPL code or data.
- [x] Post-implementation audit plan — implementation-status block, feature log, `closeout/step-23a.json`, the
      platform gate, and the audit request. — see M-1.

### Step-specific decisions

- [x] **Split boundary.** 23A contains features 1–5 (host/boundaries, workspace, settings/theming/a11y,
      lifecycle, tests/docs/closeout) and no 23B task; 23B needs nothing from 23A beyond delivered code. Verify
      the feature table and §13 seam table against the 23B page. — checked; no 23B task.
- [x] **Composition and lifetimes.** `AppHost`/explicit registration; `Program`/`App` are the only public entry
      points; the single `ProjectStore` is registered with `ValidateOnLoad = false` and its load diagnostics are
      retained; 23A does not construct the validation pipeline; per-project objects are created by the session
      service, not by DI scopes. — store ValidateOnLoad=false; pipeline is 23B.
- [x] **Visibility (m-18).** Existing public `AppViewModelsModule`/`AppViewsModule` and `Program` stay public and
      baselined; 23A **adds** `public sealed class App : Application` and the `App.axaml(.cs)` files, which are
      not in an ApiCompat project and need no baseline; every other new type is `internal` with
      `InternalsVisibleTo`. Verify no `PublicAPI.*` or App baseline change is required. — m-18 resolved; App is added, baselines unchanged.
- [x] **I/O boundary and token test (m-13/n-15).** The exact token list, whole-identifier matching,
      `using static` detection, the permitted `IUiDispatcher`/`Dispatcher` and bare `Application`, the positive
      control, and the stated limitation (a tripwire, not a compiler guarantee) agree across §1/§15, `01` §3.2,
      `code-structure` §2.3, and `03` §11.1. — m-13/n-15 resolved; tripwire limitation stated.
- [x] **Document model and workspace.** Tab model, project-scoped keys, read-only Overview/JSON documents,
      unknown-domain notice, command catalog/palette, and the deferred dirty marker.
- [x] **Project lifecycle.** New via `ProjectPathRules`; open/activate/save/save-as/close/reload; recents;
      locks/read-only; external-change and deleted-file handling; no unsaved prompt in 23A (nothing is
      editable); multi-project workspace.
- [x] **Settings, theming, accessibility.** v1 schema (no `autosave`/`session`), write discipline, migrations,
      app-data layout, theme/density/font-scale/high-contrast/reduce-motion, last directories, log level, WCAG
      2.2 AA and its test coverage. — see M-2.
- [x] **Dialogs, consoles, logging, crash.** In-shell overlay, picker contract, Application channel only,
      notifications/progress, Serilog retention, the three handlers, and the crash dialog **without** a recovery
      action (n-14: no `IRecoveryWriter`/`IRecoveryStore` in 23A). — n-14 resolved.
- [x] **State ownership and threading.** The state table, no scientific logic in view models, UI-thread
      confinement, `IUiDispatcher`/`TimeProvider` boundaries, no `Task.Delay`.
- [x] **Library change.** `ProjectPathRules` + CLI refactor: additive, behaviour-preserving, CLI codes/exit
      codes/messages identical, new message-pinning tests, `PublicAPI.Unshipped.txt` entry, no baseline refresh. — see n-7.
- [x] **Parallel-track interface.** 23A consumes only 01/03–07/12; no `Compilation`/`Results`/`Cli`/solver API;
      the seams are sufficient for 23B and Steps 24–28, and 23B is the only in-flight successor.
- [x] **Complexity class.** The seven-dimension table, the High class and its normative justification, the
      Medium dimensions' evidence, and the statement that owner acceptance is required and no further split is
      proposed. — class High, owner acceptance outstanding (n-5).

## Dependency and interface checks

- [x] Step 01 is `Done` and its `ProjectStore`/`ProjectDocument`/`ProjectHasher`/`ProjectJson` contracts are
      stable. Verify `ProjectStoreOptions`, `ProjectLoadResult.Diagnostics`/`FailureKind`, `ProjectFileLock`, the
      save/lock/autosave API surface, and the canonical document hash including `modifiedUtc`.
- [x] Step 03–06 editing helpers and the `Project` value model support the read-only documents without a new
      API.
- [x] Step 07's validation contract is deferred correctly: 23A registers no pipeline and runs no validation; the
      store is configured `ValidateOnLoad = false` and the load diagnostics are retained for 23B. Verify this is
      consistent with Step 07 §8 and does not leave a gap. — ValidateOnLoad=false; no pipeline; diagnostics retained; m-14 resolved in 23B.
- [x] Step 12's `ProjectTemplateFactory`/`ProjectTemplateKind` and the CLI name/path rules are the only creation
      path, and the `ProjectPathRules` extraction keeps the CLI's `CLI001`/`CLI002`/`CLI003` codes, exit codes,
      file behaviour, and messages unchanged.
- [x] Step 23B is `Not started`/not design-reviewed, depends on 23A, and needs only 23A deliverables; no 23B
      item is hidden in 23A. — no hidden task; n-2.
- [x] Steps 13–22 are not required: no design element depends on their types, packages, or behaviour. — checked; no 13–22 use.
- [x] Format, versioning, and migration impact is confined to the application-local settings file; the project,
      package, results, and state formats and the contract manifest are untouched. — settings only; see M-2.

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable — checked; acceptance wording n-4.
- [x] Complexity class and split decision are recorded and justified per
      [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers)
- [x] Design decisions are complete and internally consistent — checked; M-1/M-2, m-3.
- [x] Required tests, property tests, golden fixtures, and performance targets are defined
- [x] Documentation, status, parity, and audit updates are planned — planned; M-1 and n-1.
- [x] Licensing, provenance, and dependency checks are resolved
- [x] Risks and mitigations are realistic
- [x] Code layout and namespace plan follow the structure policy
- [x] No hidden scope, stale placeholders, or unowned decisions — checked; n-2.

## Evidence

Reviewed the frozen design at `219f78445353218a772cbed3470c748523d0521d`; record HEAD
`7caa94d146aa5abdc1f92246686d205dd4917855` (the request's documentation-only follow-up that fills the
`Reviewed commit` cell). `git diff --name-status 219f784 7caa94d` is exactly
`M docs/planning/reviews/step-23a.md`; the worktree was clean before this record edit.

Files reviewed:

- `AGENTS.md` §3, §8–§12; `docs/planning/reviews/README.md`; `docs/planning/audits/README.md`;
  `docs/planning/closeout/README.md`, `_template.json`, and `tools/check_closeout_evidence.py` with its tests.
- The 23A request record, the 23A and 23B step pages (all sections), the superseded `step-23` record and page,
  and `04-roadmap.md` §3.1/§3/§5.
- `01-target-architecture.md` §3.2, `03-quality-strategy.md` §11.1, `05-open-decisions.md`,
  `code-structure.md` §2.3, `appendix-b-parity-matrix.md`, `status.md`, `docs/reference/file-layout.md`, and
  `06-toolchain-requirements.md`.
- Step 01 `ProjectStore`/`ProjectStoreOptions`/`ProjectLoadFailureKind`/`ProjectStoreResults`/`ProjectFileLock`,
  `ProjectDocument`, `ProjectHasher`, `ProjectJson`; Step 07 `ProjectValidationPipeline`; Step 12
  `ProjectTemplateFactory`/`ProjectTemplate` and `NewProjectCommand`/`NewProjectCommandTests`; the App project
  files, `Program`, `AppViewModelsModule`, `AppViewsModule`, `PublicAPI.*`, `tools/api-projects.txt`, the App API
  baselines, and `Directory.Packages.props`.

Commands run:

- `./tools/verify.sh docs --require` — pass; "Closeout gate completed with required checks enabled." (strict
  Zensical build, DocFX, rustdoc).
- `./tools/verify.sh policy --require` — pass; "Public API is compatible with the checked-in baseline";
  ApiCompat reports no breaking changes for `Mocassin.App.ViewModels`/`Mocassin.App.Views`; contract artifacts
  current (26 files); process consistency OK; 58 tool tests OK; SPDX headers present in 706 files; secret scan;
  cargo-deny advisories/bans/licenses/sources OK.
- `python tools/check_doc_math.py` — pass, 176 files scanned.
- `git status --porcelain` — empty before this record edit; `git diff --name-status 219f784 7caa94d` —
  `M docs/planning/reviews/step-23a.md` only.
- `git ls-files src/dotnet/Mocassin.App` — only `Mocassin.App.csproj` and `Program.cs`, so `App.axaml(.cs)` is
  added as the design says.
- `python` check of the closeout regex: `STEP_RE.match("step-23a")` is `False`; `step-23` is `True`.
- `grep`/`cat` inspection of `ProjectStoreOptions` (`ValidateOnLoad = true` by default), `ProjectLoadFailureKind`
  (`None`, `NotFound`, `TooLarge`, `Io`, `InvalidContent`), `ProjectStore.LoadBytesCore` (validator only when
  `ValidateOnLoad`), `ProjectValidationPipeline.Validate` (in-memory entry; `Load` stage `Skipped` with no
  diagnostics), `ProjectDocument.MarkSaved()`/`MarkSaved(Project)`, `ProjectHasher.ComputeDocument`
  (`ProjectCanonicalizer` includes `modifiedUtc`), `ProjectTemplateKind` (`Blank`, `Ceria`), and
  `NewProjectCommand`'s `CLI001`/`CLI002`/`CLI003` mapping.
- Read-only inspection of the `05-open-decisions.md` link defects: 13 occurrences of `...md))` on lines 43 and
  98–109.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M-1 — The 23A closeout plan cannot pass the closeout-evidence checker: `closeout/step-23a.json` is a `step-23a` identity but `tools/check_closeout_evidence.py` accepts only `^step-\d{2}$`.** The 23A feature-5 done-when, §20, implementation tasks, and acceptance criterion require `python tools/check_closeout_evidence.py` to pass; the 23B page repeats `closeout/step-23b.json`. `STEP_RE.match("step-23a")` and `"step-23b"` are false. The split must either define the closeout `step` value as `step-23` (and document the tag/audit distinction) or update `STEP_RE`, the closeout README/template example, and `tools/tests/test_closeout_evidence.py` with a letter-suffix positive case. | `tools/check_closeout_evidence.py:20,65-66`; `tools/tests/test_closeout_evidence.py` (`test_invalid_step_identifier` only tests `wp-03`); 23A page lines 52, 815-817, 843, 846, 867; 23B page lines 46, 352, 391; `docs/planning/closeout/README.md` says `step-NN`. The frozen acceptance "closeout evidence validates" is unattainable as written. |
| **Major** | **M-2 — The `mocassin.settings` v1 document schema was dropped in the split.** The 23A page §6 now gives the settings behavior and names fields in prose, but no longer defines the v1 `format`/`schemaVersion` document shape, key names/nesting, defaults, or a JSON example; the superseded unsplit design's §17 had the full v1 example. 23B §9 adds `autosave`/`session` to "the 23A v1 document" with no schema bump, so the base shape is a dependency. The request's data-contract item and feature 3's v1 model/validator/migrations/JSON context need the shape for implementation and the round-trip/migration tests. Add the v1 example/field table to the 23A page (or a normative design page linked from it) before implementation, or state that a checked-in `file-layout.md` schema is normative and must match. | 23A page §6 lines 422-454 (prose only; no JSON/field table); §14 line 683 defers the field table to implementation; 23B page §9 lines 227-239; `05-open-decisions.md` row 102; superseded `step-23` page §17 (git `4b5fa44`) for the lost example. |
| **Minor** | **m-3 — `05-open-decisions.md` has 13 malformed `...md))` links introduced by the split.** The rendered page shows a stray `)` after each 23A/23B link. The strict docs build passes because this is valid text, not a broken link. | `05-open-decisions.md` lines 43 and 98-109; `grep -c 'step-23[ab].*md))'` = 13; `git show 219f784 -- docs/planning/05-open-decisions.md` shows the single-`)` links replaced with `))`. |
| **Note** | **n-1 — `status.md` still lists only the superseded Step 23 row.** 23A §20 defers the `23A`/`23B` rows to the step agent at start. Record them before implementation so the dashboard does not point at the superseded page. | `status.md:73`; 23A page §20 lines 824-826; the split commit updated the README/roadmap/parity but not `status.md`. |
| **Note** | **n-2 — `ProjectSessionOutcome.UnsavedChanges` is present in 23A but never produced.** 23A has no editable document and no unsaved prompt; mark the value reserved for 23B or add it with 23B. It is a seam, not hidden 23B work. | 23A page §5 lines 373-379 and line 404 ("23A has no unsaved-changes prompt"); 23B page §4 adds the prompt. |
| **Note** | **n-3 — The §1 I/O-boundary table says `AppDataOverride` is "parsed `--data-dir`/`MOCASSIN_DATA_DIR`" in ViewModels while `Environment` is banned there.** §1's `AppStartupOptions` bullet puts the env read in App. Clarify that `AppDataOverride` is a pure value and `MOCASSIN_DATA_DIR` is read in `AppStartupOptions`/`AppPaths`. | 23A page §1 table line 201 and the `AppStartupOptions` bullet lines 156-159; §1 token ban lines 204-213. |
| **Note** | **n-4 — The acceptance criterion "an existing target is refused" is ambiguous after the Save-as clause.** §5 refuses an existing target for New and refuses another session's open path for Save As; a Save As to an existing on-disk file is the picker's overwrite-confirmed path. Scope the acceptance wording to New. | 23A page §5 lines 389-400 and acceptance line 855; test-plan row "New/open/save/close" line 703. |
| **Note** | **n-5 — The no-further-split rationale's wording is imprecise, though the conclusion is sound.** §18/roadmap say "host/workspace versus settings/lifecycle would leave ... settings with no consumer"; the settings/lifecycle half would lack the host, not a consumer. The conclusion (no clean split) still holds. | 23A page §18 lines 766-769; `04-roadmap.md` §3.1 lines 191-194. |
| **Note** | **n-6 — V-1's fallback adds an explicit `Avalonia.Skia` reference without saying whether it is test-only or production.** `Avalonia.Desktop` already brings Skia transitively; state the project and keep the "no new production package" claim and the test-only package boundary explicit. | 23A page §15 V-1 line 794; §18 line 753; §11 line 627. |
| **Note** | **n-7 — `ProjectPathRules.NormalizeName/ValidateTarget` has no signature or result shape in the design.** The page says the helper returns the CLI message; define whether it returns a result record with a reason enum/message, how `ValidateTarget` separates pure name rules from file-system checks, and how `NewProjectCommand` maps the result to `CLI001/2/3`. The one additive library API is otherwise scoped and the message-pinning plan is sound. | 23A page §12 lines 629-640; request record §12 lines 103-104; current `NewProjectCommand.TryNormalizeName` and path checks. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Re-review required — round 1 returned `Changes required` on freeze `219f784` (M-1, M-2, m-3,
n-1–n-7); round 2 returned `Ready with notes` on freeze `34d036d` (m-4, n-8–n-11; n-1 deferred). At the owner's
request, the remaining notes are fixed and the design is refrozen at
`b9c56fe1351fd2f9d13d60d7ed5b989f3287fc12`; the findings are resolved or explicitly dispositioned in
`Resolution status` below, and the refrozen design awaits a round-3 review. Earlier findings and verdicts remain
unchanged as history.

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

Follow-up owner: planner/step agent — resolve M-1 and M-2 (and the Minor/Notes), then re-freeze and request a re-review.
Follow-up step: 23A — design re-review at the next freeze commit; earlier rounds stay in this record.

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

Round-1 reviewer text is unchanged. The design was corrected on `docs/step-23-design` at freeze
`34d036dacbce3b7536549c3a4f9ea1f65dc2d5d5`; this record update is a documentation-only follow-up. No code, test,
configuration, contract, or generated artifact changed.

| Finding | Status | Owner | Commit | Resolution |
| --- | --- | --- | --- | --- |
| **M-1** | Fixed in design | Planner | `34d036d` | 23A §20 and 23B §18 now state the exact identity: evidence file `closeout/step-23a.json` / `step-23b.json`, `"step"` = `"step-23a"` / `"step-23b"`, tag `step-23a` / `step-23b`, audit record `audits/step-23a.md` / `step-23b.md`, and no `step-23` alias. They record that `tools/check_closeout_evidence.py` currently accepts only `^step-\d{2}$` and that a separate tooling change extending the checker, its tests, and the closeout README/template to `^step-\d{2}[a-z]$` (with a letter-suffix positive case) must land **before either closeout**. Feature 4/5 done-when and the acceptance criteria say "validates once the prerequisite has landed" instead of claiming it today. The design does not work around the checker. |
| **M-2** | Fixed in design | Planner | `34d036d` | 23A §6.1 is a new normative section: the full v1 JSON example, a field table with every key and nesting level (`format`, `schemaVersion`, `theme`, `accent`, `density`, `fontScale`, `highContrast`, `reduceMotion`, `culture`, `logging.level`, `ui.*`, `paths.*`, `recentFiles[]`, `layout.*`), defaults and ranges, the unknown-key/missing-key/wrong-type policy, the future-major refusal without overwrite, pure-function migrations, the 4 MiB cap and 10-entry recent list, the source-generated `AppSettingsJsonContext`, and the temp+fsync+replace protocol. It states the file is app-local and not in `contracts/manifest.json`; `file-layout.md` mirrors it. 23B §9 now references 23A §6.1 as the normative base and lists only its two additive objects (`autosave`, `session`) with types/defaults/ranges, no schema bump, and the `"problems"` channel value. |
| **m-3** | Fixed | Planner | `34d036d` | The 13 malformed `...md))` links (20 link instances on lines 43 and 98–109) in `05-open-decisions.md` are corrected to single-`)` links; `grep -c "md))"` is now 0. |
| **n-1** | Deferred | Step agent | — | `status.md` remains step-agent-owned per `AGENTS.md` §3.8; the planner did not edit it. 23A §20 states the required `23A`/`23B` row replacement and that the step agent performs it at start. |
| **n-2** | Fixed | Planner | `34d036d` | `ProjectSessionOutcome.UnsavedChanges` is marked in the enum and in §5 as reserved for 23B and never produced in 23A, so the result type does not change shape later without hidden 23B work. |
| **n-3** | Fixed | Planner | `34d036d` | §1's table now says `AppDataOverride` is a pure value; `AppStartupOptions` parses `--data-dir` and reads `MOCASSIN_DATA_DIR`, and `AppPaths` resolves the root — both in `Mocassin.App`, never in ViewModels. |
| **n-4** | Fixed | Planner | `34d036d` | §5 scopes "an existing target is refused" to **New only** and states that Save As to an existing file follows the picker's overwrite confirmation and the store's atomic replace. The acceptance criterion and test row are scoped the same way. |
| **n-5** | Fixed | Planner | `34d036d` | 23A §18 and `04-roadmap.md` §3.1 now say the settings/lifecycle half would be **without a host to run in**, not "without a consumer"; the no-further-split conclusion is unchanged. |
| **n-6** | Fixed | Planner | `34d036d` | V-1's fallback now adds `Avalonia.Skia` at the pinned version to **`Mocassin.App.Tests` only**, as a test-only dependency for the headless screenshot host; production keeps the already-pinned Avalonia packages and the "no new production package" claim stands. |
| **n-7** | Fixed | Planner | `34d036d` | §12 defines `ProjectPathFailure`, `ProjectNameResult`, `ProjectTargetResult`, `ProjectTargetState`, and `ProjectPathRules.NormalizeName/ValidateTarget/GetMessage`, with pure rules and one caller-supplied file-system fact. It maps `NormalizeName`/invalid-directory failures to `CLI001`/2, `ExistingTarget` (`File`) to `CLI003`/3, and `Directory`/creation failure to `CLI002`/3, and keeps the new CLI message-pinning tests. |

**Round-3 corrections to earlier rows.** The round-1 n-4 row's claim that the §15 test row was scoped like the
§5 rule was inaccurate; n-8 below corrects it. The round-1 n-7 mapping for `InvalidTargetPath` changed a CLI
behaviour; m-4 below corrects it. Those rows are kept as history; the rows below supersede the affected claims.

| Finding | Status | Owner | Commit | Resolution |
| --- | --- | --- | --- | --- |
| **m-4** | Fixed | Planner | `b9c56fe` | `ProjectPathFailure.InvalidTargetPath` is now documented as **GUI-only** (empty/invalid directory chosen in a picker; inline message and picker re-open). The CLI never produces it: it validates and normalises its output path before calling the helper, keeps the empty/invalid output directory as `CLI002` I/O with exit `3`, and `AnEmptyOutputDirectoryIsCli002AndExitThree` stays unchanged. The mapping table no longer maps `InvalidTargetPath` to `CLI001`/2; the CLI codes, exit codes, and messages are unchanged, and the new message-pinning tests cover only the failures the helper produces. §19's decision row records the GUI-only reservation. |
| **n-8** | Fixed | Planner | `b9c56fe` | The §15 "New/open/save/close" row now reads "**New** refuses an existing target, while Save As to an existing file follows the picker confirmation", matching §5 and the acceptance criterion. |
| **n-9** | Fixed | Planner | `b9c56fe` | §6.1 now says `docs/reference/file-layout.md` **will mirror** the section once the 23A documentation column lands (§14), and that at this freeze the page has no settings/app-data section. No present-tense content claim remains. |
| **n-10** | Fixed | Planner | `b9c56fe` | §20 (23A) and §18 (23B) now instruct the step agent to add the lowercase **`23a`**/**`23b`** status rows, matching `check_process_consistency.py`'s `\d{2}[a-z]?` parser, and state that page titles/prose keep `23A`/`23B`. No uppercase/lowercase mismatch is left in the dashboard instruction. |
| **n-11** | Fixed | Planner | `b9c56fe` | §1, §10, and feature 1's done-when now say every new type **other than `App`** is internal, `App` is the only new public App type, and `Program` is pre-existing. `code-structure.md` §2.3 uses the same wording. The no-ApiCompat/no-baseline substance is unchanged. |

The step agent fills any further status after the next verdict. Do not alter the reviewer-authored findings,
evidence, or verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

I am a fresh context. I did not author, design, or implement the reviewed work and have no shared conversation
with the planner, implementor, the earlier Step 23 reviewers, or the orchestrator. I reviewed the frozen design
at `219f78445353218a772cbed3470c748523d0521d` and the record HEAD `7caa94d146aa5abdc1f92246686d205dd4917855`
(the request's documentation-only follow-up that fills the `Reviewed commit` cell). `git diff --name-status
219f784 7caa94d` is exactly `M docs/planning/reviews/step-23a.md`; the worktree was clean before this record
edit, so the only repository file changed by this round is this record.

#### Commands and results

- `./tools/verify.sh docs --require` — pass; "Closeout gate completed with required checks enabled."
- `./tools/verify.sh policy --require` — pass; "Public API is compatible with the checked-in baseline";
  ApiCompat for both App assemblies, contract artifacts current (26 files), process consistency, 58 tool tests,
  SPDX, secret scan, and cargo-deny all pass.
- `python tools/check_doc_math.py` — pass, 176 files scanned.
- `git status --porcelain` — empty before this edit; `git diff --name-status 219f784 7caa94d` — the record only.
- `STEP_RE.match("step-23a")` / `"step-23b"` — `False`; `"step-23"` — `True`; see M-1.
- `grep -c "step-23[ab].*md))" docs/planning/05-open-decisions.md` — 13; see m-3.
- Read-only inspection of the Step 01/07/12 APIs and the App projects confirmed `ValidateOnLoad = true` by
  default, `TooLarge` in `ProjectLoadFailureKind`, the in-memory `Validate` signature and `Load`-stage
  semantics, `MarkSaved()`/`MarkSaved(Project)`, `ComputeDocument` including `modifiedUtc`, `Blank`/`Ceria`,
  and the `CLI001`/`CLI002`/`CLI003` mapping.

#### Findings

**M-1 (Major).** The 23A/23B closeout plan uses `closeout/step-23a.json` / `step-23b.json`, but
`tools/check_closeout_evidence.py` accepts only `^step-\d{2}$`, so the required evidence cannot validate. The
split must define the closeout `step` value as `step-23` (with the tag/audit distinction documented) or update
the checker, README/template, and tool tests. This is the same contract in both halves.

**M-2 (Major).** The `mocassin.settings` v1 schema was dropped in the split. The 23A page §6 describes the
fields and behavior but no longer defines the document shape, key names/nesting, defaults, or a JSON example;
the old unsplit §17 had it and 23B §9 adds `autosave`/`session` to the base document. Restore the v1
example/field table in the 23A design (or a normative linked page) before implementation.

**m-3 (Minor).** `05-open-decisions.md` has 13 `...md))` links from the split; the rendered page shows a stray
`)`. The strict docs build passes; fix the links.

**Notes.** n-1 status dashboard row; n-2 unused `UnsavedChanges`; n-3 `AppDataOverride`/`Environment` wording;
n-4 acceptance "existing target" ambiguity; n-5 no-further-split wording; n-6 V-1 `Avalonia.Skia` scope;
n-7 `ProjectPathRules` signature. Details and evidence are in the top-level `## Findings` table.

#### Verdict

**Changes required.** The split is dependency-clean, the Step 07 validation contract is correct, the visibility
and I/O-boundary claims match the repository, the public-API/ApiCompat claim is true, the class-High
justification is honest, and the five-feature/acceptance/test/documentation/closeout structure is present. The
step cannot start as frozen because M-1 makes the closeout gate unattainable and M-2 leaves the settings v1
document shape undefined. The human owner decides the next freeze; the earlier rounds stay in this record.

### Re-review round 2

#### Independence and frozen state

I am a fresh context. I did not author, design, or implement the reviewed work and have no shared conversation
with the planner, implementors, the earlier reviewers, or the orchestrator. I received only the frozen commit
`34d036dacbce3b7536549c3a4f9ea1f65dc2d5d5`, the record HEAD `3da2c9c`, the request record, repository links, and
the required commands/scope. No desired verdict or summary was supplied.

The design under review is `34d036dacbce3b7536549c3a4f9ea1f65dc2d5d5`. The record HEAD is `3da2c9c`; `git diff
--name-status 34d036d 3da2c9c` returns `M docs/planning/reviews/step-23a.md` only, and `git show --stat 3da2c9c`
is one file (+34/−7). The freeze `34d036d` changes four documentation files (`04-roadmap.md`,
`05-open-decisions.md`, `step-23a-workspace-and-project-lifecycle.md`,
`step-23b-editing-diagnostics-and-recovery.md`) and no code, test, configuration, contract, generated, or
status-dashboard file.

Round-1 reviewer text is byte-identical to `c65b1a1`. I extracted `## Evidence`..`## Record state`,
`## Reviewer independence`..`## Verdict`, `## Verdict`..`## Approval summary`, and the `### Review round 1` body
(to the start of the appended round, or to EOF in `c65b1a1`) from
`git show c65b1a1:docs/planning/reviews/step-23a.md` and from the record at HEAD and compared them: all four
extractions are identical. Only the request-owned `Reviewed commit` cell, the refreeze note, `Record state`, and
`Resolution status` changed between the commits. The `Reviewed commit` cell is exactly
`34d036dacbce3b7536549c3a4f9ea1f65dc2d5d5`.

I re-checked the request's design-decision and dependency checklists against the refrozen design; the existing
marks remain appropriate and the inline notes are unchanged.

#### Commands and results

- `./tools/verify.sh docs --require` — pass; the final line is "Closeout gate completed with required checks
  enabled." (strict Zensical build, DocFX, rustdoc).
- `./tools/verify.sh policy --require` — pass; "Public API is compatible with the checked-in baseline";
  ApiCompat for the checked App assemblies; contract artifacts current (26 files); process consistency OK; 58
  tool tests OK; SPDX headers in 706 source files; no secret patterns in 1115 tracked text files; cargo-deny
  advisories/bans/licenses/sources OK; final line "Closeout gate completed with required checks enabled."
- `python tools/check_doc_math.py` — pass: "Documentation math conventions OK (176 files scanned)."
- `git status --porcelain` — empty before this record edit.
- `git diff --name-status 34d036d 3da2c9c` — `M docs/planning/reviews/step-23a.md` only.
- At the freeze, `tools/check_closeout_evidence.py` has `STEP_RE = re.compile(r"^step-\d{2}$")`;
  `STEP_RE.match("step-23a")` is `False` and `STEP_RE.match("step-23")` is `True`. The design says exactly that,
  so it does not claim the checker works at the freeze.
- The prerequisite is real on `main`: merge `b2cebac` of `c7a2d89` ("tools: accept letter-suffixed step IDs")
  changes the checker to `STEP_RE = re.compile(r"^step-\d{2}[a-z]?$")`, adds positive `step-23a`/`step-23b`/
  `step-00z` and negative `step-23aa`/`step-023`/`step-23A`/`step-3a` cases to
  `tools/tests/test_closeout_evidence.py`, extends `tools/check_process_consistency.py` to
  `AUDIT_RE = ^docs/planning/audits/step-(\d{2}[a-z]?)\.md$` and `STEP_ID_RE = \d{2}[a-z]?` for status rows
  (with a `| 23a |` test), and updates `docs/planning/closeout/README.md`, `_template.json`, `tools/README.md`,
  and the `05-open-decisions.md` "Split step IDs" row. The merged commit is not an ancestor of the frozen design
  branch (`git merge-base --is-ancestor b2cebac 34d036d` is false), so the design's "prerequisite must land"
  wording remains correct; the step branch will be created from the reviewed `main` commit, which contains it.
- `grep -rn "md))" docs/planning/05-open-decisions.md` — no matches; `grep -c "step-23[ab].*md))"` — 0.
- Read-only inspection of the current CLI and tests: `NewProjectCommand.cs:108-131` maps an empty/invalid
  output path to `CliExitCode.Io` (3) and `CliDiagnosticFactory.Io` (CLI002), and
  `NewProjectCommandTests.AnEmptyOutputDirectoryIsCli002AndExitThree` pins exit 3/CLI002. `NewProjectCommand.cs`
  maps an existing target file to CLI003/3 and an existing directory to CLI002/3.
- Read-only inspection of Step 01/07/12 and the App projects confirms the design's dependency claims:
  `ProjectStoreOptions.ValidateOnLoad` defaults to `true`, `ProjectLoadFailureKind` has `None`/`NotFound`/
  `TooLarge`/`Io`/`InvalidContent`, `ProjectValidationPipeline.Validate` treats an in-memory `Load` as `Skipped`
  without load diagnostics and `Failed`/`Completed` with them, `ProjectDocument.MarkSaved()`/
  `MarkSaved(Project)` exist, `ProjectHasher.ComputeDocument` is canonical, `ProjectTemplateKind` is `Blank`/
  `Ceria`, `Mocassin.App` contains only `Mocassin.App.csproj` and `Program.cs`, `Mocassin.App` is absent from
  `tools/api-projects.txt`, and the App baselines/`PublicAPI.Shipped.txt` files are present.
- `docs/reference/file-layout.md` has no settings/app-data section at the freeze; 23A §14 line 801 schedules the
  update.
- `tools/check_process_consistency.py` on `main` uses `STEP_ID_RE = re.compile(r"\d{2}[a-z]?")` and
  `AUDIT_RE = re.compile(r"^docs/planning/audits/step-(\d{2}[a-z]?)\.md$")`; the added test uses a lowercase
  `| 23a |` status row.

#### Finding closure

| Finding | Round-1 severity | Round-2 status | Evidence |
| --- | --- | --- | --- |
| **M-1** | Major | **Closed** | 23A §20 and 23B §18 name `closeout/step-23a.json`/`step-23b.json`, `"step": "step-23a"`/`"step-23b"`, tag `step-23a`/`step-23b`, and `audits/step-23a.md`/`step-23b.md`, with no `step-23` alias. The design states the checker accepts only `^step-\d{2}$` at the freeze and that the `step-NN[a-z]` change must land before either closeout; it does not claim the checker works. The landed prerequisite is real (above). |
| **M-2** | Major | **Closed** | 23A §6.1 is a normative v1 shape: full JSON example; a field table covering `format`, `schemaVersion`, `theme`, `accent`, `density`, `fontScale`, `highContrast`, `reduceMotion`, `culture`, `logging.level`, `ui.*`, `paths.*`, `recentFiles[]`, and `layout.*` with defaults/ranges; unknown/missing/wrong-type/future-major/migration policy; 4 MiB cap and 10-entry recent list; source-generated `AppSettingsJsonContext`; temp+fsync+replace. 23B §9 cites it and lists only `autosave`/`session`. The file is app-local and absent from `contracts/manifest.json`. |
| **m-3** | Minor | **Closed** | No `...md))` remains in `05-open-decisions.md`; `grep -rn "md))"` is empty and the split links are single-`)`. |
| **n-1** | Note | **Deferred (accepted disposition)** | `status.md` still lists the unsplit `23` row; it is step-agent-owned per AGENTS §3.8 and 23A §20 records the `23A`/`23B` replacement at start. Not a design blocker; see n-10 for the row case. |
| **n-2** | Note | **Closed** | `ProjectSessionOutcome.UnsavedChanges` is marked "reserved for 23B; never produced in 23A" in the enum and §5. |
| **n-3** | Note | **Closed** | §1's table says `AppDataOverride` is a pure value; `AppStartupOptions` parses `--data-dir` and reads `MOCASSIN_DATA_DIR`; `AppPaths` resolves the root; both live in `Mocassin.App`, never ViewModels. |
| **n-4** | Note | **Substantively closed; residual n-8** | §5 and the acceptance criterion scope "an existing target is refused" to New and allow Save As to an existing file after picker confirmation. The §15 test row still says only "an existing target is refused" (n-8). |
| **n-5** | Note | **Closed** | 23A §18 and `04-roadmap.md` §3.1 now say the settings/lifecycle half would be "without a host to run in"; the no-further-split conclusion is unchanged. |
| **n-6** | Note | **Closed** | V-1's fallback explicitly adds `Avalonia.Skia` at the pinned version to `Mocassin.App.Tests` only; production keeps the pinned Avalonia packages and the "no new production package" claim stands. |
| **n-7** | Note | **Substantively closed; residual m-4** | §12 defines `ProjectPathFailure`, `ProjectNameResult`, `ProjectTargetResult`, `ProjectTargetState`, and `NormalizeName`/`ValidateTarget`/`GetMessage`, with pure rules and one caller-supplied file-system fact. The CLI mapping for `InvalidTargetPath` is wrong (m-4). |

#### New findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **m-4 — The `ProjectPathRules` CLI mapping changes the empty/invalid output-directory case and contradicts the "identical codes/exit codes/messages" invariant.** The design maps `ValidateTarget InvalidTargetPath` ("empty or syntactically invalid directory") to `CLI001` usage/exit 2, but the current CLI maps the same case to `CLI002`/exit 3, and `AnEmptyOutputDirectoryIsCli002AndExitThree` pins it. Either map `InvalidTargetPath` to `CLI002`/3 for the CLI (or keep the CLI's own path-parse error and reserve `InvalidTargetPath` for the GUI), or record an intentional behaviour change and update the pinned test. | 23A §12 lines 749-752; `src/dotnet/Mocassin.Cli/NewProject/NewProjectCommand.cs:108-131` (`CliExitCode.Io`, `CliDiagnosticFactory.Io`); `src/dotnet/Mocassin.Cli.Tests/NewProject/NewProjectCommandTests.cs:277`; `CliExitCode.Io = 3`. |
| **Note** | **n-8 — The §15 test row still says "an existing target is refused" without scoping it to New.** The §5 rule and acceptance criterion are correctly scoped, but the test row is not, and the n-4 resolution's claim that the test row is scoped the same way is inaccurate. Add "New refuses..." to the row so the test cannot contradict Save As's overwrite-confirmed path. | 23A §5 line 395; §15 line 821; acceptance line 985; `Resolution status` n-4. |
| **Note** | **n-9 — §6.1 says `docs/reference/file-layout.md` mirrors the settings section in the present tense, but the page has no settings/app-data section at the freeze.** §14 line 801 schedules the update in feature 5, so the normative shape (§6.1) is not missing; the sentence should say "will mirror" or the section should land with the page update. | 23A §6.1 lines 462-466; §14 line 801; `docs/reference/file-layout.md` (no settings content). |
| **Note** | **n-10 — The §20 dashboard instruction says the step agent writes `23A`/`23B` rows, while the landed `check_process_consistency.py` matches lowercase `23a`/`23b` status rows.** The closeout/decision records use `step-NN[a-z]` (lowercase). Use `23a`/`23b` in the status row or make the parser case-insensitive, otherwise the audit-record check will not map the split rows. | 23A §20 lines 952-954; `tools/check_process_consistency.py` on `main` lines 102-103/176; the added `| 23a |` test; `05-open-decisions.md` "Split step IDs". |
| **Note** | **n-11 — The visibility wording still says "every new type is internal" and "`App`/`Program` are the only new public App types", which conflicts with the public `App` exception and the pre-existing `Program`.** §1/§10 correctly say 23A adds `public sealed class App : Application` and that `Program` is pre-existing; the blanket sentences should say "every new type other than `App`" and "`App` is the only new public type; `Program` is pre-existing". The substance (no ApiCompat/baseline change) is correct. | 23A §1 visibility paragraph; §10 visibility bullet; feature 1 "Done when" row; `Mocassin.App` contains only the csproj and `Program.cs`; `tools/api-projects.txt` excludes `Mocassin.App`. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

#### Verdict

**Ready with notes.** Round 1's Majors are resolved in substance: M-1's closeout identity is consistent across
23A/23B and the `step-NN[a-z]` prerequisite is real on `main`, and M-2's `mocassin.settings` v1 shape is
complete, normative, and referenced by 23B. m-3 is fixed; n-2, n-3, n-5, and n-6 are closed; n-4 and n-7 are
substantively closed. The split remains dependency-clean, the Step 07 `ValidateOnLoad = false`/load-diagnostics
contract is correct, the I/O-boundary and public-API claims match the repository, the class-High justification
is honest, and the five-feature/acceptance/test/documentation/closeout structure is present with no 23B/24–28
scope.

The remaining material issue is the **Minor** m-4 (the `ProjectPathRules` CLI mapping) plus Notes n-8–n-11 and
the deferred n-1. None is a Blocker or Major. The human owner must explicitly accept these notes, or ask for the
small corrections, before the start gate; this review does not create the step branch, mark the step
`In progress`, or authorize implementation.
