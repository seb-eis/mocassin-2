# Step 24A Audit Request

> Review agent: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-24a.md`. You may edit only your own metadata (`Review agent`, `Audit date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 24A — Shared Editor Framework, Particles, and Structure |
| Request owner | Dev (`dev-free`/`dev`) |
| Lead | — |
| Dev (`dev-free`/`dev`) | `dev` (paid fallback after the 2026-11-23 `dev-free` free-limit event) |
| Audit initiated by | Lead, on request of the dev |
| Design class | Full — recorded in the step page §16; no waiver |
| Pre-audit gate | Recorded in the step page, 2026-11-24: local `all --require` green; closeout/platform CI green; acceptance/evidence map; parity rows mapped; structure/link/secret/licence checks green |
| Step branch | `step/24a-model-editors-particles-and-structure` |
| Closeout evidence | [`docs/planning/closeout/step-24a.json`](../closeout/step-24a.json) |
| Audit date | 2026-11-24 |
| Review agent | DeepSeek V4.1 Flash 1017 (fresh audit context; `opencode-go/deepseek-v4.1-flash`) |

The closeout evidence file holds the audited commit, closeout CI run and `head_sha`, `main` SHA at freeze, merge
candidate, merge-tree CI, platform CI, and tag. Do not restate them here: one source, one place.

Step page: [`Step 24A — Shared Editor Framework, Particles, and Structure`](../steps/step-24a-model-editors-particles-and-structure.md)

> Coordination: the lead spawns a fresh review agent when the dev has frozen the step branch and completed the
> audit request, and relays the verdict and owner decisions. The dev applies fixes on a new feature branch,
> reruns closeout CI, adds resolutions without altering review agent text, and asks the lead for re-audit until
> the review agent no longer identifies issues. The lead does not fix findings or edit the audit record. The
> human owner approves the final verdict, merge, and tag.
>
> Stop rule: the pre-audit gate has already checked the bookkeeping. The review agent spends its pass on judgment
> and stops at no Blocker or Major. Minor and Note findings are accepted or deferred; they do not justify another
> audit round by themselves.

## Step summary

| Field | Value |
| --- | --- |
| Phase | F. GUI |
| Depends on | 23B (workspace, editing, diagnostics, recovery) and the merged surfaces of 02–06 (read-only usage) |
| Delivers | The ADR 0003 shared editor framework; particle/set editors; space-group and cell-parameter forms; cell-site table and P1 preview; dummy-positions document; structure summary; closeout evidence and audit request. Transition editors are 24B |

## Step-specific acceptance checklist

Copy of the step page acceptance criteria; the reviewer marks these. The evidence map is in the step page
"Pre-audit gate and closeout record" section.

- [x] The layout is used as recorded in ADR 0003: the table selects, the splitter resizes the detail pane, and the
      detail pane edits. No new grid/control dependency is added. — `ModelTableSurfaceTests`,
      `EditorIntegrationTests` (named splitter, detail host), `ModelEditorScreenshotTests`; no new package in
      `Directory.Packages.props`. The editor-splitter drag itself is not directly tested (F-5).
- [x] A user can author the ceria particles, sets, structure, and sites **from an empty project**
      (`samples/blank`) without the CLI, and the authored values match the ceria sample. (The oxygen migration is
      24B's criterion; 24A does not claim it.) — `ModelEditorAcceptanceTests.CeriaParticlesAndSetsAreAuthoredFromBlankAndFullyUndone`;
      values compared by name/value and fully undone.
- [x] Drag-and-drop and keyboard-only flows both work for particle-set membership and produce the same order. —
      `ParticleSetEditorTests.DragAndKeyboardPathsProduceTheSameOrder`.
- [x] Changing the space group previews the new orbit sizes and overlaps **before** applying, and cancelling
      changes nothing. — `SpaceGroupPreviewTests` (preview, overlap, cancel, one undo entry).
- [ ] Overlapping sites are shown inline and cannot be applied; the rejection changes nothing. — The test
      `CellSiteEditorTests.AnOverlappingCandidateIsShownAndRefused` passes only with the synchronous editor; the
      production debounced path can commit the overlap before validation runs. See F-1.
- [x] The P1 preview shows positions with their canonical `P` index and orbit-member index, matches the engine's
      orbit/position data, and renders no generating-operation label (M-1). —
      `CellSiteEditorTests.TheTableAndP1PreviewMatchTheEngineData` (index equality, no operation property); the
      view has no operation label.
- [x] Every particle/structure diagnostic navigates to the offending field by keyboard and by click. —
      `ModelEditorFrameworkTests.ADiagnosticNavigatesIntoAnEditorAndRequestsTheField`,
      `EditorIntegrationTests` (five domains, stale id), and `CellSiteEditorTests.OpeningADependentRequestsShellNavigation`;
      the request's known note accepts this representative coverage, not an exhaustive fixture.
- [x] Undo/redo works across every 24A edit, and the save point stays correct. — Editor undo tests across
      particles/sets/structure/sites/dummies, `EditorRefreshTests`, `ProjectDocumentApplyOverloadTests`, plus the
      23B coordinator save-point tests and dirty assertions.
- [x] `Tab` reaches the detail pane from the selection surface after `Home`/`End`; the surface is one tab stop. —
      `ModelTableSurfaceTests.HomeOrEndThenTabReachesTheDetailPane` (rows not tab stops).
- [x] The site table stays aligned and virtualizes exactly 1,000 sites; a preview of more than 1,000 P1 positions
      is bounded and cancellable. — `CellSiteEditorTests.OneThousandSitesVirtualizeAndTheLastRowIsSelectable`,
      `CellSiteEditorTests.APreviewOfMoreThanOneThousandPositionsIsVirtualized`, `StructurePreviewSchedulerTests`.
- [x] Read-only sessions disable every edit path and show the 23B banner, proved by `EditorReadOnlyTests`. — The
      distributed tests prove the disabled commands/state (`ModelEditorFrameworkTests` plus the per-domain
      `ReadOnly*` tests) and the view binds the banner to `IsReadOnly`; no test class named `EditorReadOnlyTests`
      exists and no test asserts the rendered banner. See F-5.
- [x] Documentation written (the §12 pages). — `particles.md`, `structure.md`, `ui-architecture.md`,
      `ui-interaction-contract.md`, `errors.md`, `tutorials/ceria-model.md`; strict docs build green.

## Standard audit checklist

- [x] The design class is recorded; `Full`/`Light` review evidence is present, or `None` has the one-line record
      — **Full**, step page §16; design-review rounds 1–2 at `b98a56f`/`b75efea`, round 2 `Ready with notes`,
      owner-accepted 2026-11-23.
- [x] The pre-audit gate result is recorded in the step page and matches this request — recorded 2026-11-24 in
      the step page; see F-6 for the stale dashboard narrative.
- [x] Required tests from the step page pass. — 451 App tests (122 in the editor namespace), 696 Core, all
      solution/Rust/Python suites green; the named `EditorReadOnlyTests` row is absent (F-5).
- [x] Public/protected XML documentation policy is followed. — Release build 0 warnings; the new public Core
      overload is documented and listed in `PublicAPI.Unshipped.txt`.
- [x] Documentation, status dashboard, feature log, and parity matrix are current. — Pages and parity rows are
      current; the status table row is `In audit`, but its “Active work” paragraph is stale (F-6).
- [x] Closeout CI is green at the audited commit. — Ubuntu `45477848106` at `5dd70e6`, recorded in the closeout
      evidence; not independently re-queried because `gh`/a GitHub token is unavailable in this environment.
- [x] Closeout evidence matches the audited commit and the merge candidate. —
      `python tools/check_closeout_evidence.py docs/planning/closeout/step-24a.json` → `Closeout evidence: OK`.
- [x] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded. — `main` is still
      `3e904ed`; `git merge-base --is-ancestor 3e904ed 5dd70e6` is true, so no merge-tree run applies.
- [x] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code. — Windows `45478516289` and macOS `45479315065` at `5dd70e6`, both green in the closeout evidence.
- [x] Format/version/fixture changes are documented and tested. — No `.mocprj`/`.msl`/`.msr`/`.mcs`, manifest,
      diagnostic-code, or CLI change; the additive app-local `editorFraction` reads a default when absent and
      is covered by `EditorFractionSettingsTests`/`ShellLayoutTests`.
- [x] Security, dependency, licence, and secret-scan gates pass. — `all --require`: SPDX in 1,205 files, no
      secret patterns in 1,750 tracked files, NuGet licence/vulnerability gate, cargo-deny green.
- [x] Code structure policy passes; no new root-level or catch-all folders. — `./tools/verify.sh structure` 9/9.
- [x] No unrelated changes or unapproved scope. — `3e904ed..5dd70e6` is 145 files, all 24A editors/docs/closeout;
      no package, contract, or format change.
- [x] Branch, tag, and status hygiene are correct. — Branch clean at the frozen tip, no `step-24a` tag, no
      `main` merge; the status-row hygiene issue is F-6.

## Evidence

**Frozen state reviewed.** Audited commit `5dd70e652c2885d2b439b4b495c0cd3f7e79b742`; frozen branch tip
`bdeaee7f8f4c2c95160bd5d3e66ec6026c23c940`. `git diff --name-status 5dd70e6..bdeaee7` is exactly four
documentation files (`docs/planning/audits/step-24a.md`, `closeout/step-24a.json`, `status.md`, the 24A step
page), so the code/test/configuration tree under audit is `5dd70e6`. `main` is `3e904ed` and is an ancestor of the
audited commit; it did not move. The closeout evidence was completed in that documentation-only delta after the
CI runs; the final file binds `audit_commit` and `closeout_ci.head_sha` to `5dd70e6`.

I am a fresh audit context. I did not author or implement the step and have no shared conversation with the design
agent, dev, lead, or other review agents.

**Commands run in this worktree** (all exit 0 unless noted):

```text
./tools/verify.sh all --require                                    # exit 0, full closeout gate
./tools/verify.sh structure                                        # 9/9 Architecture tests
./tools/check-api-compat.sh                                        # baseline compatible
python tools/check_closeout_evidence.py docs/planning/closeout/step-24a.json  # Closeout evidence: OK
dotnet test src/dotnet/Mocassin.App.Tests/... --filter "FullyQualifiedName~...Editors"  # 122 passed
dotnet test src/dotnet/Mocassin.Core.Tests/... --filter "FullyQualifiedName~ProjectDocumentApplyOverloadTests"  # 7 passed
git diff --check 3e904ed..5dd70e6                                  # clean
git merge-base --is-ancestor 3e904ed 5dd70e6                        # true
git diff --stat 3e904ed..5dd70e6                                    # 145 files, +14,675/−58
```

`all --require` results: .NET build succeeded (0 warnings/0 errors), `dotnet format` clean; solution suites green —
Contracts 38, Symmetry 223, Results 91, Core 696, Cli 203, Compilation 99, Packages 536, Architecture 9,
**App 451** (all 0 failed); Rust fmt/clippy/tests green; Python 3 passed and package build green; strict docs build
“No issues found” plus DocFX and rustdoc; contracts/policy current (26 artifacts, 62 process tests, 274-setting
catalog, SPDX 1,205 files, 1,750 tracked files secret-scanned, cargo-deny ok); AOT gate passed with the documented
62 IL diagnostics recorded as evidence; `Closeout gate completed with required checks enabled.` (`EXIT:0`).
The gate ran at `bdeaee7`; the delta from `5dd70e6` is documentation-only as verified above.

**Production-path probe (F-1).** I built a scratch probe outside the repository in `/home/node/tmp/step24a-race`
(not committed) using the real `SitesEditorViewModel` with a `ManualTimeProvider` and the inline dispatcher. On the
ceria sample, selecting `O transition` and changing the three fractional fields to `0.25, 0.25, 0.25` left
`HasOverlap=false`, `CanApply=true`, `ApplyCommand.CanExecute=true` **before the 250 ms clock advance**; executing
`ApplyCommand` then committed the site (`undo=1`, position `0.25, 0.25, 0.25`) and the structure subsequently
reported `STR007`. After advancing the clock the preview correctly reported the overlap and disabled Apply. This
reproduces the gate gap described in F-1.

**CI evidence.** Closeout Ubuntu run `45477848106` at `5dd70e6` is recorded green in
`docs/planning/closeout/step-24a.json`; Windows `45478516289` and macOS `45479315065` are recorded green at the
same commit. `gh` and a GitHub token are unavailable here, so I did not independently re-query the run objects; the
closeout evidence file is the recorded source and its required fields pass the checker. `main` CI is not applicable
before merge; no tag exists.

**Files read.** `AGENTS.md` §3.8/§3.9/§3.10/§3.13/§3.15; `docs/planning/audits/README.md`;
`docs/planning/reviews/README.md`; `docs/development/design-proportionality.md`; the audit request; the 24A step
page; the step-24a design-review record (both rounds); `docs/planning/status.md`; `docs/planning/closeout/step-24a.json`;
`docs/planning/appendix-b-parity-matrix.md`; ADR 0003; `01-target-architecture.md` §3.2; `02-data-contracts.md`;
`03-quality-strategy.md` §11.1; the step diff `3e904ed..5dd70e6`; the Core `ProjectDocument`/`PublicAPI.Unshipped.txt`
and `ProjectDocumentApplyOverloadTests`; `ProjectEditCoordinator`, `BulkEditComposer`, `ModelEditorViewModel`,
`ModelEditorDocumentViewModel`, the editor view models/views for particles, sets, structure, sites, and dummies;
`ModelUsageIndex`; `NumericExpression`; `ModelTableSurface`; `StructurePreviewScheduler`; `AppServiceRegistration`/
`AppHost`; the named editor tests; `samples/ceria/ceria.mocprj` and `samples/blank/blank.mocprj`; the user-guide,
tutorial, architecture, interaction-contract, and troubleshooting docs changed by the step.

**Evidence file:** `docs/planning/closeout/step-24a.json` (`Closeout evidence: OK`).

## Known notes for the review agent

- The divergence point is `5dd70e6`: the audited commit includes the closeout evidence, feature-6 records, and the
  step page. The earlier local `all --require` run was at `9d0cfd7`; the delta to `5dd70e6` is documentation-only.
- Limitation: criterion 7 is evidenced by the navigation seam plus representative diagnostic/provider tests, not
  an exhaustive fixture for every diagnostic code.
- Limitation: the screenshot baselines were generated on Linux; the Windows/macOS platform gates cover the
  focused keyboard/focus/drag behaviour but not a pixel comparison.
- The large-P1 `STR009` warning is wired and displayed; the checked-in samples stay below the 10,000-position
  budget, so no synthetic over-budget fixture was added.
- No code, test, contract, or configuration change exists after the audited commit; the evidence/request/status
  commits are documentation-only.

## Findings

| Severity | Origin | Finding | Evidence |
| --- | --- | --- | --- |
| **Major** | **implementation** | **F-1 — The production site form can commit an overlapping candidate before the debounced validation runs.** `CellSiteDetailViewModel.CanApply` is derived from the *last* `candidateDiagnostics` (line 373), while a field change only starts a 250 ms `StructurePreviewScheduler` computation (lines 597–620; scheduler delay lines 43–58). The production `SitesEditorViewModel` is created with the real clock/dispatcher by `SitesEditorViewModelFactory` (lines 43–44), so `CanApply`/`ApplyCommand.CanExecute` remain true during the debounce window. `ApplyCore` then calls `TryBuildSite` and `AddCellSite`/`UpdateCellSite` (lines 399–404) without running `IStructureValidator`; Core's `StructureModelValidator` and `ProjectEditor.UpdateCellSite` do not check overlap (only the Symmetry `StructureValidator` emits `STR007`), so the invalid site is written. The acceptance test `CellSiteEditorTests.AnOverlappingCandidateIsShownAndRefused` uses a scheduler-free editor (`CreateEditor`), so it never exercises the production timing. **Reproduced** with a scratch probe: after editing to `0.25,0.25,0.25`, `HasOverlap=false`/`CanApply=true` and `ApplyCommand` committed the site (`undo=1`); a subsequent validation reported `STR007`. This violates acceptance criterion 5. Fix by validating the candidate synchronously in `ApplyCore`/`Apply`, or by tracking a pending preview and keeping Apply disabled until the current candidate has been validated; add a fake-clock regression test that applies before advancing the clock. | `CellSiteDetailViewModel.cs:373,399–404,597–620`; `StructurePreviewScheduler.cs:43–58`; `SitesEditorViewModel.cs:69–71,114–115,127`; `SitesEditorViewModelFactory.cs:43–44`; `CellSiteEditorTests.AnOverlappingCandidateIsShownAndRefused` (scheduler-free `CreateEditor`); `ProjectEditor.UpdateCellSite` and `StructureModelValidator` (no overlap check); scratch probe output recorded in `Evidence` |
| **Minor** | **implementation** | **F-2 — The site table does not enable multi-select, so the designed bulk-delete path is unreachable.** Step page §3 says multi-select is enabled “for particles, sets, and sites”; `ParticlesEditorViewModel` and `ParticleSetsEditorViewModel` set `Table.IsMultiSelectEnabled = true`, but `SitesEditorViewModel` never does. `SitesEditorViewModel.RemoveManyCore` is therefore dead code, and the site table stays single-select (`ModelTableViewModel.IsMultiSelectEnabled` defaults false). | `SitesEditorViewModel.cs:65–73,142–155`; `ParticlesEditorViewModel.cs:47–48`; `ParticleSetsEditorViewModel.cs:41–42`; `ModelTableViewModel.cs` (`IsMultiSelectEnabled` default); step page §3 |
| **Minor** | **implementation** | **F-3 — Blocked-delete dependents are text-only for particles and sets; the “Show usages” navigation is not wired.** The design says a blocked delete “lists dependents from the usage index and offers Show usages (navigates to the dependent domain)”. `TryRemoveSelected` populates `SelectedUsages` and a message, but `ModelEditorDocumentView` binds only `BlockedDeleteMessage`; `SelectedUsages`, `ShowUsagesCommand`, and `OpenUsageCommand` have no view binding outside the site detail's own dependent rows. Particles/sets users can see names in one sentence but cannot navigate to the blocking object. | `ModelEditorViewModel.cs:70,180,229,417–447`; `ModelEditorDocumentView.axaml:29–37`; step page §5 (“Delete is blocked … offers Show usages”); sites detail already has `OpenDependentCommand`/Go buttons |
| **Minor** | **implementation** | **F-4 — Editor gesture/focus contract gaps: `Delete` removes without confirmation; multi-field forms have no `Enter` apply; the structure form declares no initial focus.** The step page §7 and `ui-interaction-contract.md` §8 rule 8 say `Delete` removes after confirmation, and §7 says `Enter` applies the focused field or form. `ModelEditorDocumentView` executes `RemoveCommand` directly on `Delete` (no dialog/confirmation), and the site/cell/dummy/space-group views expose only Apply buttons (no `Enter`/default mapping); `StructureEditorDocumentView` has no initial-focus declaration. These are contract deviations, not acceptance-criterion failures. | `ModelEditorDocumentView.axaml.cs:106–110`; step page §7 (lines 451–454); `ui-interaction-contract.md:345–352`; `CellSiteDetailView.axaml`, `CellParametersView.axaml`, `DummyPositionDetailView.axaml`, `SpaceGroupEditorView.axaml` (Apply buttons only); `StructureEditorDocumentView.axaml.cs` (no focus hook) |
| **Minor** | **evidence** | **F-5 — Several §13 test-plan rows and the criterion-11 evidence are not delivered as named.** Criterion 11 names `EditorReadOnlyTests`, and §13 names `EditorAccessibilityTests` and `EditorPerformanceTests`; none exists as a class. The distributed read-only tests (plus `ModelEditorFrameworkTests.ReadOnly…`) prove disabled commands/state, and both document views bind the banner to `IsReadOnly`, but no test asserts the rendered 23B banner. The non-gating `EditorPerformanceEvidence` (1,000-row build/select and preview latency) is not recorded in the step page; the 100-vs-1,000 comparison is not tested; screenshot baselines cover dark/high-contrast only for particles/sites (sets/structure/dummies are light-only); the editor splitter drag is not directly tested. The pre-audit gate should have reconciled the test plan with the delivered classes. | Step page §13 (lines 585–590) and criterion 11 (line 774); `grep` finds no `EditorReadOnlyTests`/`EditorAccessibilityTests`/`EditorPerformanceTests` in `src/dotnet`; `ModelEditorFrameworkTests.ReadOnlySession…`, `ParticleEditorTests.ReadOnly…`, `ParticleSetEditorTests.ReadOnly…`, `CellParameterTests.ReadOnly…`, `CellSiteEditorTests.ReadOnly…`, `DummyPositionTests.ReadOnly…`; `ModelEditorScreenshotTests.cs:40–54`; `ModelEditorDocumentView.axaml:42–49` and `StructureEditorDocumentView.axaml` banner binding |
| **Minor** | **documentation** | **F-6 — The status dashboard's “Active work” narrative is stale and contradicts the table.** The 24A row correctly says `In audit` with the closeout evidence, but the paragraph still says the 24A design review round 1 returned `Changes required`, the record is `Re-review required`, and “Neither half has a step branch or a start-gate approval; the start gates are pending”. The design was accepted 2026-11-23, the step branch exists, and the step is in audit. This fails the “status dashboard is current” audit check. | `docs/planning/status.md:91–103` vs the 24A row at line 82 and the step page `Implementation status`; step page/status/closeout all show `In audit` |
| **Note** | **documentation** | **F-7 — The new ceria tutorial uses a different vacancy symbol from the sample it reproduces.** The tutorial says “Add `Vacancy` with symbol `V`”, while `samples/ceria/ceria.mocprj` uses symbol `Vc`; a user following the tutorial to match the sample gets a different value. Cosmetic, but user-facing. | `docs/tutorials/ceria-model.md:14`; `samples/ceria/ceria.mocprj` (`particles[2].symbol = "Vc"`) |
| **Minor** | **implementation** | **F-8 — `SitesEditorViewModel` never disposes its `StructurePreviewScheduler`.** The editor creates the scheduler in its constructor (lines 69–71) and passes it to every site detail, but it has no `OnDisposed` override; `ModelEditorViewModel.Dispose` only calls the virtual `OnDisposed` hook (lines 488–518). A pending 250 ms timer therefore survives editor/document close and can post `ApplyPreview` onto a stale detail. `StructurePreviewSchedulerTests` proves `Dispose` cancels the timer, but no editor test disposes the editor and advances the clock. Fix by disposing the scheduler in a `SitesEditorViewModel.OnDisposed` override. | `SitesEditorViewModel.cs:69–71,114–115,127` (no `OnDisposed`); `ModelEditorViewModel.cs:488–518`; `StructurePreviewScheduler.cs:66–78`; `StructurePreviewSchedulerTests.DisposingTheSchedulerCancelsThePendingComputation` (direct scheduler only) |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. Origin: **design**, **implementation**, **evidence**, or **documentation**. The review agent writes the findings and verdict directly. The dev adds resolutions in the separate `Resolution status` section without altering the review agent's findings, evidence, or verdict.

## Record state

**State:** Fixes required — audit round 1 (see `Audit rounds`); the dev owns the resolution/fix commit and a fresh re-audit.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> design agent, dev, or lead.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [x] Fixes required
- [ ] Waived

**Audit round 1 verdict: Fixes required.** One Major (F-1) and six Minor findings remain; F-7 is a Note. F-1 is an
acceptance-criterion failure on the production path and must be fixed before merge. The six Minor findings are
localized implementation/evidence/documentation issues; the dev should fix or explicitly record an owner-accepted
deferral for each in the resolution commit. After the fix on a new feature branch, a new closeout CI run, and a
re-audit request, a fresh/continuing review agent appends round 2. The audit result is evidence, not merge
authorization; the human owner still approves the merge and tag.

Follow-up owner: Dev (`dev-free`/`dev`) for F-1…F-8; the human owner accepts any remaining Minor/Note deferral.
Follow-up issue/step: Step 24A re-audit after the F-1 fix and a new closeout CI run.

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The dev fills this section after the verdict. Do not alter the review agent-authored findings, evidence, or verdict.

## Approval summary

The dev completes this section before a merge, tag, or Blocker/Major waiver request. It must cover the
full history, not only the latest round.

- [ ] Requested decision and frozen commit recorded
- [ ] Every review/audit round recorded (round, date, commit, review agent, verdict)
- [ ] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status)
- [ ] Accepted notes and waivers recorded with rationale
- [ ] Residual risks, deferred items, and follow-up owners recorded
- [ ] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived
- [ ] Final CI and documentation-build results recorded

Round history:

| Round | Date | Commit | Review agent | Verdict |
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

I am a fresh audit context. I did not author or implement Step 24A and have no shared conversation with the design
agent, the dev, the lead, or any other review agent. I was spawned by the lead with the audited commit, the audit
request, and the repository; no desired verdict was supplied. My model is DeepSeek V4.1 Flash 1017
(`opencode-go/deepseek-v4.1-flash`).

I audited `5dd70e652c2885d2b439b4b495c0cd3f7e79b742` on
`step/24a-model-editors-particles-and-structure`. The frozen branch tip is
`bdeaee7f8f4c2c95160bd5d3e66ec6026c23c940`; `git diff --name-status 5dd70e6..bdeaee7` is exactly four
documentation files (audit request, closeout evidence, status dashboard, 24A step page), so the code/test tree is
the audited tree. `main` is `3e904ed21a5004dcf41cfd9060adaaf33a1967b7` and is an ancestor of the audited commit;
it did not move, so no merge-tree run applies. The closeout evidence was completed in the documentation-only delta
after the CI runs; it binds `audit_commit` and `closeout_ci.head_sha` to `5dd70e6`.

#### Commands and results

Run in `/home/node/.local/share/opencode/worktree/c2378f/step-24a` at the frozen tip:

- `./tools/verify.sh all --require` → **exit 0**. App 451 tests (122 in the editor namespace), Core 696, all other
  solution suites green, 0 failed; .NET build 0 warnings/0 errors and `dotnet format` clean; Rust fmt/clippy/tests
  green; Python tests/build green; strict docs build “No issues found” with DocFX and rustdoc; contracts/policy
  green (26 artifacts, 62 process tests, 274-setting catalog, SPDX 1,205 files, 1,750 files secret-scanned,
  cargo-deny); AOT gate passed with the documented 62 IL diagnostics as evidence; `Closeout gate completed with
  required checks enabled.`
- `./tools/verify.sh structure` → **exit 0**, 9/9 Architecture tests.
- `./tools/check-api-compat.sh` → **exit 0**, “Public API is compatible with the checked-in baseline” (non-fatal
  reference-resolution warnings during the standalone invocation; the full `all` run also passes).
- `python tools/check_closeout_evidence.py docs/planning/closeout/step-24a.json` → `Closeout evidence: OK`.
- Targeted `dotnet test` filters: `Mocassin.App.Tests` editor namespace **122 passed**, Core
  `ProjectDocumentApplyOverloadTests` **7 passed**.
- `git diff --check 3e904ed..5dd70e6` clean; `git merge-base --is-ancestor 3e904ed 5dd70e6` true; diff stat 145
  files, +14,675/−58.
- Production-path probe (scratch, not committed): with the real `SitesEditorViewModel` + `ManualTimeProvider` +
  inline dispatcher, editing the ceria `O transition` site to `0.25,0.25,0.25` left `HasOverlap=false`,
  `CanApply=true`, and `ApplyCommand.CanExecute=true` before the 250 ms advance; the Apply committed the site
  (undo entry 1), and validation then reported `STR007`. See F-1.

Closeout CI: Ubuntu `45477848106` at `5dd70e6`, recorded green in `docs/planning/closeout/step-24a.json`;
Windows `45478516289` and macOS `45479315065` recorded green at the same commit. `gh`/a GitHub token is unavailable
in this environment, so the run objects were not independently re-queried; the closeout evidence is the recorded
source and its required fields validate.

#### Findings

No Blocker. One Major, six Minor, and one Note remain:

- **F-1 (Major, implementation)** — the production debounced site form can commit an overlapping candidate before
  `IStructureValidator` runs; this violates acceptance criterion 5. Reproduced with a scratch probe. Fix before
  merge.
- **F-2 (Minor, implementation)** — the site table does not enable multi-select; `RemoveManyCore` is unreachable,
  contrary to step page §3.
- **F-3 (Minor, implementation)** — blocked-delete dependents are text-only for particles and sets; the designed
  “Show usages”/dependent navigation is not wired outside the site detail.
- **F-4 (Minor, implementation)** — `Delete` removes without confirmation and the multi-field forms lack the
  designed `Enter`/initial-focus behaviour (step page §7, interaction contract §8).
- **F-5 (Minor, evidence)** — §13/criterion-11 test rows named `EditorReadOnlyTests`, `EditorAccessibilityTests`,
  and `EditorPerformanceTests` do not exist; the banner, non-gating performance evidence, 100-vs-1,000 comparison,
  screenshot theme breadth, and editor-splitter drag are not directly proven.
- **F-6 (Minor, documentation)** — the status dashboard “Active work” paragraph is stale (still says
  `Changes required`/`Re-review required`/no step branch) while the table says `In audit`.
- **F-7 (Note, documentation)** — the ceria tutorial uses vacancy symbol `V` while the sample uses `Vc`.
- **F-8 (Minor, implementation)** — `SitesEditorViewModel` creates a `StructurePreviewScheduler` but never disposes
  it; a pending timer can fire after the editor/document closes.

The findings table above records severity, origin, evidence, and the fix direction.

#### Verdict

**Fixes required.** The step is substantially complete: the ADR 0003 surface is implemented, the acceptance test
authors ceria from blank, the P1 indices match the engine, the Core overload and coordinator path are sound, the
documentation and gates are green, and the closeout/platform CI evidence is recorded. It cannot merge with F-1
open because the production path can write an overlapping structure, which the step explicitly forbids. F-2…F-7 are
minor/notable and should be fixed or owner-accepted in the resolution commit. A new commit with the F-1 fix and a
new closeout CI run is required before round 2.
