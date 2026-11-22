# Step NN Audit Request

> Review agent: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-NN.md`. You may edit only your own metadata (`Review agent`, `Audit date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | NN — Step title |
| Request owner | Dev (`dev-free`/`dev`) |
| Lead | — |
| Dev (`dev-free`/`dev`) | — |
| Audit initiated by | Lead, on request of the dev |
| Design class | Full / Light / None (with pre-authorized one-line record) |
| Pre-audit gate | Recorded in the step page — date / result |
| Step branch | — |
| Closeout evidence | `docs/planning/closeout/step-NN.json` — for a post-merge amendment (`step-NN-amend-M`), write `this record` and carry the audited commit, closeout CI, `main` SHA, merge commit, merge-tree CI, platform CI, and tag here instead |
| Audit date | — |
| Review agent | — |

The closeout evidence file holds the audited commit, closeout CI run and `head_sha`, `main` SHA at freeze, merge candidate,
merge-tree CI, platform CI, and tag. Do not restate them here: one source, one place. A post-merge amendment has
no evidence file; this record is the one source (see [`README.md`](README.md#31-post-merge-amendment-audits)).

Step page: [`Step NN — Step title`](../steps/step-NN-short-title.md)

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
| Phase | — |
| Depends on | — |
| Delivers | — |

## Step-specific acceptance checklist

Copy the acceptance criteria from the step page, preserving their wording:

- [ ] Acceptance criterion 1
- [ ] Acceptance criterion 2

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

```

Evidence file: `docs/planning/closeout/step-NN.json` (post-merge amendment: this record, per the request row)

## Known notes for the review agent

- Record known limitations, accepted deviations, or areas needing extra attention.

## Findings

| Severity | Origin | Finding | Evidence |
| --- | --- | --- | --- |
| — | — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. Origin: **design**, **implementation**,
**evidence**, or **documentation**. The review agent writes the findings and verdict directly. The dev adds
resolutions in the separate `Resolution status` section without altering the review agent's findings, evidence,
or verdict.

## Record state

**State:** Not requested / Ready for audit / In audit / Fixes required / Re-audit required / Passed / Passed with notes / Waived

`Re-audit required` is a record state, not a verdict.

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

The dev fills this section after the verdict. Do not alter the review agent-authored findings, evidence, or
verdict.

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
