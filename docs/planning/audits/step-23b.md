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
| Audit date | — |
| Auditor | — |

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

- [ ] Metadata editing works with undo/redo across the save point in both directions; a rejected structural edit
      changes nothing; a read-only session returns the app-level `ReadOnly` outcome with no diagnostic code.
- [ ] The validity indicator and Problems channel show code, severity, path, source, and help; double-click
      navigates through a registered provider and produces a notice otherwise; the retained load/migration
      result reaches Problems and a clean load leaves the `Load` stage `Skipped`; opening does not run a second
      validation or a statistics run; the help map matches the documented anchors.
- [ ] Dirty state is correct across save, undo, and redo; closing with unsaved changes prompts; Cancel closes
      nothing and leaves autosave running; Discard and discard-reload delete the sidecar/recovery file; a return
      to the save point or a clean close deletes the sidecar, except a deferred sidecar candidate, which
      survives until Recover/Discard and is cleared by the first accepted content change (a no-op same-hash
      edit does not clear it, n-12); a deferred `recovery/` candidate remains subject to the 7-day sweep.
- [ ] Autosave writes only dirty documents and deletes its file on save; save/autosave/close/reload/recovery do
      not interleave; a simulated crash leaves a `recovery/` snapshot; recovery restores a dirty document equal
      to the snapshot, updates `LoadDiagnostics`, and recomputes validation before the `Replaced` event; a
      timestamp-only snapshot is not offered; the crash path completes within its budget or reports the timeout.
- [ ] Recovery candidates include the sidecar source; the newest valid candidate wins; recovery files are swept
      after 7 days; sidecars are never swept; unclean-exit session restore works; missing project files are
      reported without an untitled session.
- [ ] The `autosave`/`session` settings fields round-trip without a schema bump and older documents still load.
- [ ] No unhandled exception kills the process silently, and the crash dialog reports recovery results; the
      dead-UI path is documented.
- [ ] `./tools/verify.sh all --require` passes, the strict docs build passes, and `closeout/step-23b.json` with
      `"step": "step-23b"` validates.
- [ ] Documentation written (§12), and no page claims a later step's capability or an untitled-project model.

## Standard audit checklist

- [ ] Required tests from the step page pass.
- [ ] Public/protected XML documentation policy is followed.
- [ ] Documentation, status dashboard, feature log, and parity matrix are current.
- [ ] Closeout CI is green at the audited commit.
- [ ] Closeout evidence matches the audited commit and the merge candidate.
- [ ] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded.
- [ ] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code.
- [ ] Format/version/fixture changes are documented and tested.
- [ ] Security, dependency, licence, and secret-scan gates pass.
- [ ] Code structure policy passes; no new root-level or catch-all folders.
- [ ] No unrelated changes or unapproved scope.
- [ ] Branch, tag, and status hygiene are correct.

## Evidence

Commands run on the audited tree (all exit 0 unless noted):

```text
./tools/verify.sh all --require
python tools/check_closeout_evidence.py docs/planning/closeout/step-23b.json
```

Closeout CI and platform gates:

- Ubuntu closeout `44929014836`, `head_sha` `eb120e17ce43a152c8905fc1f54528b69448b3e3`, green.
- Windows platform gate `44929276522`, same `head_sha`, green.
- macOS platform gate `44929582584`, same `head_sha`, green.

Evidence file: `docs/planning/closeout/step-23b.json`

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
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** Ready for audit

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Waived

Follow-up owner:
Follow-up issue/step:

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

#### Commands and results

#### Findings

#### Verdict
