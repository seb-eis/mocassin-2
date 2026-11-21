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
| Reviewer | — |

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

- [ ] Step page design decisions — goal, scope, non-goals, the 23A seam map, the edit pipeline and
      `ProjectChanged` consumers, save-point semantics, validation surface and retained-result contract, dirty
      markers and prompts, autosave, recovery/crash handling, settings additions, the two library changes, the
      23A seam-edit list, documentation, tests, performance, risks, and the class.
- [ ] Data contracts and format changes — the application-local `mocassin.settings` v1 `autosave`/`session`
      additions with no schema bump; the app-local `recovery/` manifest; no change to `.mocprj`, `.msl`, `.msr`,
      `.mcs`, CLI reports/exit codes, diagnostic codes, or signatures.
- [ ] Architecture and module boundaries — `01-target-architecture.md` §3.2 (App/ViewModels/Views ownership,
      the I/O boundary, one writer per project, visibility), §3.1, and the UI-thread row in §5.3.
- [ ] ADRs and open decisions — ADR 0003/0006/0008, the resolved 23B rows in `05-open-decisions.md`, and the
      class acceptance gate.
- [ ] Quality strategy and test plan — `03-quality-strategy.md` §6/§8/§11.1 and the page's §13 test table with
      tiers, including the `Load`-stage fixtures and the no-real-sleep rule.
- [ ] Parity matrix and documentation plan — the 23B rows and the §12 documentation column.
- [ ] Licensing and provenance — no new product or test package; the 23A ANGLE exception is unchanged; no GPL
      code or data.
- [ ] Post-implementation audit plan — implementation-status block, feature log, `closeout/step-23b.json`, the
      platform gate, and the audit request. — verify the landed `step-NN[a-z]` checker.

### Step-specific decisions

- [ ] **Split boundary.** 23B contains features 1–4 (edit, validation, dirty/autosave/recovery, tests/docs/
      closeout) and no 23A task; 23A contains no 23B task. Verify the feature table and §11 seam-edit list
      against the 23A page and delivered code.
- [ ] **Retained validation result.** 23A performs exactly one internal load-validation pass and stores it as
      `ProjectSession.Validation`; 23B starts from that result and does not run the pipeline again on open.
      Verify this is consistent with the 23A feature-4 deviation and Step 07 §8, and that the clean/migration
      `Load`-stage fixtures still hold.
- [ ] **Edit validation boundary.** `ProjectEditor.Apply` uses the structural `ProjectValidator`, not the full
      pipeline; a structurally valid but cross-domain-invalid edit is accepted and reported by the validation
      surface. Verify the design does not claim that a successful `Apply` means a fully valid model.
- [ ] **Save point.** The save calls `MarkSaved()` (no-arg) and does not install the store's returned
      `modifiedUtc`; the four save-point consequences and the in-memory timestamp display are documented. Verify
      the redo-after-save trace and that the 23A `MarkSaved(savedProject)` call is changed.
- [ ] **Undo depth.** `MaxUndoDepth` is init-only, default `0` (unbounded, preserving Step 01); the GUI sets
      200; negative values are rejected. Verify the property is additive, the GUI construction sets 200, and
      the boundary/unbounded cases are tested.
- [ ] **No untitled-project model.** 23A's New writes the file immediately; 23B introduces no untitled session
      and uses `recovery/` only for crash snapshots/manifest-backed fallbacks. Verify this reconciles the
      superseded unsplit wording and the parity/open-decision rows.
- [ ] **Recovery sources.** The sidecar is primary; `recovery/` is the location-independent crash/fallback
      snapshot with a manifest. `IRecoveryStore` + `CrashRecoveryWriter` are the types; no `IRecoveryWriter` and
      no new `ProjectStore` overload. Verify `WriteAutosaveAsync` is sufficient for the recovery base path.
- [ ] **Crash path.** Async writes with a 2 s budget, per-session results, no sync-over-async; the dead-UI path
      writes no recovery files and is recorded as a limitation. Verify the dialog/handler flow is implementable
      with the 23A crash boundary.
- [ ] **Settings additions.** `autosave`/`session` are additive, capped/normalized, and need no schema bump; a
      23A document still loads. Verify the source-generated context and validator coverage.
- [ ] **Shell seams.** The console channel selector, the settings dialog's autosave control, the dirty marker,
      and the project-provider metadata mapping are bounded seam fills, not a shell redesign. Verify no region,
      command-model, or lifecycle change is hidden.
- [ ] **Complexity class.** The seven-dimension table, the High class and its normative justification, the
      one-unit case, the no-further-split conclusion, and the guard. Verify owner acceptance is required and the
      class is the highest dimension, not an average.

## Dependency and interface checks

- [ ] Step 23A is `Done`, merged, and tagged; `ProjectSession`/`ProjectDocument`/`ProjectSessionService`/
      `LoadDiagnostics`/`Validation`, the document model, navigation/explorer/console/status seams, settings,
      crash boundary, and harness are stable. Verify the actual signatures the design relies on.
- [ ] Step 01's `ProjectDocument` (`Apply`/`Undo`/`Redo`/`MarkSaved`/`IsAutosaveDue`/`MarkAutosaved`) and
      `ProjectStore` (`WriteAutosaveAsync`/`DeleteAutosave`/`LoadAutosaveAsync`/`GetAutosavePath`) support the
      design without a second stack or a second autosave API.
- [ ] Step 07's validation contract is consumed correctly: `ProjectValidationPipeline.Validate` signature,
      `ValidationOptions.Default`, `ValidationResult.DocumentHash`, `Load`-stage semantics, and the retained
      23A result. Verify no second validator, catalog, or duplicate validation.
- [ ] Steps 03–06 and 12 are `Done` and their editing/template APIs support the metadata edit and project path
      behaviour without a new API.
- [ ] Steps 13–22 are not required: no design element depends on their types, packages, or behaviour.
- [ ] Format, versioning, and migration impact is confined to the app-local settings document and the app-local
      recovery manifest; the project, package, results, and state formats and the contract manifest are
      untouched.
- [ ] The `step-NN[a-z]` closeout-evidence checker and process parser support `step-23b` on `main`; the design
      does not claim an unattainable closeout gate.

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
