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
| Audit date | — |
| Review agent | — |

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

- [ ] The layout is used as recorded in ADR 0003: the table selects, the splitter resizes the detail pane, and the
      detail pane edits. No new grid/control dependency is added.
- [ ] A user can author the ceria particles, sets, structure, and sites **from an empty project**
      (`samples/blank`) without the CLI, and the authored values match the ceria sample. (The oxygen migration is
      24B's criterion; 24A does not claim it.)
- [ ] Drag-and-drop and keyboard-only flows both work for particle-set membership and produce the same order.
- [ ] Changing the space group previews the new orbit sizes and overlaps **before** applying, and cancelling
      changes nothing.
- [ ] Overlapping sites are shown inline and cannot be applied; the rejection changes nothing.
- [ ] The P1 preview shows positions with their canonical `P` index and orbit-member index, matches the engine's
      orbit/position data, and renders no generating-operation label (M-1).
- [ ] Every particle/structure diagnostic navigates to the offending field by keyboard and by click.
- [ ] Undo/redo works across every 24A edit, and the save point stays correct.
- [ ] `Tab` reaches the detail pane from the selection surface after `Home`/`End`; the surface is one tab stop.
- [ ] The site table stays aligned and virtualizes exactly 1,000 sites; a preview of more than 1,000 P1 positions
      is bounded and cancellable.
- [ ] Read-only sessions disable every edit path and show the 23B banner, proved by `EditorReadOnlyTests`.
- [ ] Documentation written (the §12 pages).

## Standard audit checklist

- [ ] The design class is recorded; `Full`/`Light` review evidence is present, or `None` has the one-line record
- [ ] The pre-audit gate result is recorded in the step page and matches this request
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

Commands run:

```text
./tools/verify.sh all --require                                  # exit 0 at the local audited tree 9d0cfd7
./tools/verify.sh structure                                      # 9/9 green
./tools/verify.sh docs                                           # strict build green
./tools/verify.sh policy                                         # exit 0
python tools/check_closeout_evidence.py docs/planning/closeout/step-24a.json  # Closeout evidence: OK
```

Closeout CI: Ubuntu run `45477848106` at `5dd70e652c2885d2b439b4b495c0cd3f7e79b742`, green.
Platform gates: Windows run `45478516289`, macOS run `45479315065`, both green at the same commit.
Evidence file: `docs/planning/closeout/step-24a.json`.

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
| — | — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. Origin: **design**, **implementation**, **evidence**, or **documentation**. The review agent writes the findings and verdict directly. The dev adds resolutions in the separate `Resolution status` section without altering the review agent's findings, evidence, or verdict.

## Record state

**State:** Ready for audit

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> design agent, dev, or lead.

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

#### Commands and results

#### Findings

#### Verdict
