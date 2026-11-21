# Step Audits

This directory records the independent audit gate between finishing a step and merging it into `main`.
Audits are separate from the step agent's self-checks: the implementer may test and review its own work, but
a fresh, out-of-context audit context decides whether the step is ready for approval.

## 1. Roles

| Role | Responsibility |
| --- | --- |
| Step agent (implementor) | Implement, self-check, create/update the audit record's request fields, fix findings, add resolutions, update status/docs/parity |
| Audit agent (reviewer) | Independent review of the frozen commit; writes evidence, findings, and verdict directly into the audit record |
| Orchestrator (spawner) | Provides the frozen audit request and spawns a fresh auditor on behalf of the step agent; relays the verdict; does not review or approve |
| Human owner | Approve fixes, merge, and tag |

The audit request, its scope, and its acceptance checklist are owned by the step agent. The audit agent may
edit only the audit record's reviewer-owned metadata, evidence, findings, and verdict. It must not change code,
configuration, tests, scope, checklists, resolutions, status dashboard, step page, parity matrix, or the step
branch. If the auditor starts fixing code, it becomes an implementer and a new auditor is required for the final
pass. The reviewer writes directly into `docs/planning/audits/step-NN.md`; there is no handoff file and no
transcription step.

### 1.1 Auditor spawning

The orchestrator is the only spawner. The step agent asks the orchestrator for an audit; the owner does not spawn
an auditor directly. The spawner must not be the auditor.

- The auditor is a fresh context with no shared conversation with the implementor, planner, or orchestrator.
- The orchestrator provides only the frozen commit, the audit request, repository links, and the required
  commands/scope. It must not supply a desired verdict, pass on its conversation history, or substitute a
  summary for reading the repository.
- The auditor writes findings and verdict directly into the record. The orchestrator and step agent do not edit
  them.
- The human owner approves the final verdict and the merge/tag gate. The orchestrator must not mark a verdict,
  waive findings, merge, tag, or push `main`.
- A re-audit requires a new commit with fixes, made by the step agent. Do not re-spawn auditors until a
  preferred verdict appears.

## 2. Audit status values

| Status | Meaning |
| --- | --- |
| **Not requested** | No audit request yet. |
| **Ready for audit** | Step branch is frozen at a specific commit and the request is complete. |
| **In audit** | The step branch is frozen at the audit request; a fresh audit context is pending or reviewing the frozen commit. |
| **Fixes required** | The audit found blocking or material issues. |
| **Re-audit required** | Fixes were applied and the new commit needs verification. |
| **Passed** | The audit found no blocking issues at the recorded commit. |
| **Passed with notes** | Only minor/note findings remain; the human owner explicitly accepts them. |
| **Waived** | The human owner explicitly accepted a documented exception. |

## 3. Required sequence

1. The step agent finishes all features, the closeout feature, documentation, status, and parity updates,
   merges current `main` into the step branch, and runs `./tools/verify.sh all --require`.
2. The step branch is pushed and one closeout CI run is green. Do not run CI per feature; the closeout run is
   the only step-branch CI run.
3. The step agent creates `docs/planning/audits/step-NN.md` on demand from the current template:
   - record state: `Ready for audit`
   - a link to the closeout evidence file, which holds the audited commit, closeout CI run ID and `head_sha`,
     `main` SHA at freeze, merge candidate, merge-tree CI, platform CI, and tag
   - scope and acceptance criteria
   - known limitations
4. Fill `docs/planning/closeout/step-NN.json` from `_template.json` and run
   `python tools/check_closeout_evidence.py docs/planning/closeout/step-NN.json`. The evidence must bind the
   audited commit, the closeout CI `head_sha`, the `main` SHA at freeze, and the merge candidate.
5. The step branch is frozen at the audited commit. After a passing audit, only the reviewer's record commit
   and the step agent's resolution/status commit may follow; neither may change code, tests, configuration, or
   the reviewer's text. If fixes are needed, the step agent uses a new feature branch and the new commit is
   re-audited.
6. If `main` moves after the freeze, apply the **merge-tree rule** in
   [`../closeout/README.md`](../closeout/README.md): merge the new `main`, run the full local gate, dispatch a
   replacement closeout CI run, and re-audit; or record an equivalent merge-tree CI run for the final merge
   candidate. Never merge an untested combination.
7. A fresh audit agent reads:
   - `AGENTS.md`
   - the step page
   - the audit request
   - the closeout evidence
   - `docs/planning/status.md`
   - relevant ADRs, contracts, and quality strategy
8. The auditor runs the required builds/tests and reviews the diff and artifacts.
9. The auditor writes the evidence, findings, and verdict directly into
   `docs/planning/audits/step-NN.md` and commits only that file with a documentation-only message and
   `[skip ci]`. The audited commit is the parent of the record commit. The auditor does not merge, tag, push
   `main`, or change any other file. If the auditor cannot commit, it writes the record and the step agent
   commits it unchanged.
10. If fixes are required:
    - the step agent adds the resolutions to the record;
    - the step agent fixes the findings on a new feature branch;
    - squash-merges into the step branch, merges current `main`, and updates the evidence;
    - dispatches one new closeout CI run and waits for it;
    - updates the audit record to `Re-audit required` with the new commit.
11. The auditor re-audits the new commit and appends a new round to the record. The same auditor may continue if
    it stayed within the reviewer write scope; otherwise start a fresh audit context. Keep every earlier round
    in the record.
12. On `Passed`, the human owner gives merge approval. The approval request includes the cumulative approval
    report in section 10, covering every audit and review round, finding, resolution, note, waiver, and residual
    risk. A verdict acceptance on its own does not need it.
13. Merge the step branch into `main` with `--no-ff`, wait for the automatic `main` CI run, tag `step-NN` only
    when it is green, push, and delete the step branch.

### 3.1 Post-merge amendment audits

A **post-merge amendment** (`step-NN-amend-M`, integrated and tagged after the parent step is merged) is not a
step and has no step branch closeout. Its audit follows the same independent-auditor rules, with this evidence
rule:

- The audit record `docs/planning/audits/step-NN-amend-M.md` is the **single evidence source** for the audited
  commit, the closeout CI run and `head_sha`, the `main` SHA at freeze, the merge commit, the merge-tree CI (if
  required), the platform CI status, and the suffixed tag (`step-NN.M`). Do not create or rewrite a
  `closeout/step-NN.json` for the amendment; the parent step's evidence file is historical and stays unchanged.
- `tools/check_closeout_evidence.py` accepts `step-NN` and `step-NN[a-z]` only. It is **not run** for a
  post-merge amendment; the audit record carries the fields instead. Extending the checker to `step-NN.M` is a
  separate future process/tooling change, not a requirement of an amendment audit.
- The amendment is audited at its maintenance-branch freeze commit, merged with `--no-ff`, and tagged only after
  the automatic `main` CI run is green. The parent `step-NN` tag is never moved.
- The amendment audit uses the same status vocabulary (`Ready for audit`, `Fixes required`,
  `Re-audit required`, `Passed`, `Passed with notes`, `Waived`) and the same finding severities as a step audit.

This rule applies from `step-11-amend-2` (A2-07) and was **owner-approved 2026-11-21**. See also
[`../closeout/README.md`](../closeout/README.md) and the [audit template](step-audit-template.md).

CI is intentionally minimal: automatic CI runs only on `main`, on Ubuntu, with path filters. Windows and macOS
are manual and only required when platform-specific code appears. Documentation uses its own manual workflow.
See [`AGENTS.md` §3.13](../../../AGENTS.md) and [`docs/planning/ci-policy.md`](../ci-policy.md).

## 4. What the audit must cover

- Step acceptance criteria and “Done when”.
- Required tests, property tests, contract tests, and documentation.
- Public/protected XML documentation policy.
- Status dashboard, step page, parity matrix, and feature log accuracy.
- CI configuration and actual CI result.
- The closeout CI `head_sha` equals the audited commit, and the closeout evidence file is current (for a
  post-merge amendment, the audit record is the evidence source).
- If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded.
- Format/version changes, fixtures, and migration behavior.
- Security, dependency, licence, and secret-scan gates.
- Code structure and folder ownership; the structure test passes.
- No unrelated changes, stale placeholders, or unapproved scope.
- Branch, merge, tag, and status hygiene.

## 5. Finding severity

| Severity | Meaning |
| --- | --- |
| **Blocker** | Correctness, safety, contract, or acceptance failure; cannot merge. |
| **Major** | Material quality, documentation, or maintainability issue; fix before merge. |
| **Minor** | Small defect or hygiene issue; fix or explicitly accept. |
| **Note** | Observation or future improvement; no merge block. |

## 6. Verdict

- **Passed** — no blockers or majors remain.
- **Fixes required** — blockers or majors remain.
- **Passed with notes** — only minor/note findings remain; human owner accepts them explicitly.
- **Waived** — human owner records a temporary exception and its follow-up.

## 7. Audit request files

Create one file per step:

```text
docs/planning/audits/step-00.md
docs/planning/audits/step-01.md
...
```

Create `docs/planning/audits/step-NN.md` on demand when the step reaches `Ready for audit`, using
[`step-audit-template.md`](step-audit-template.md). Do not pre-seed records for steps that have not started;
templates rot faster than they are used. The step agent owns the request fields, scope, checklists, and
resolutions. The auditor writes the evidence, findings, and verdict directly into this file.

The audit record is the durable record. Do not rely on chat history, and do not use a separate handoff file.
The closeout evidence is a separate machine-checkable file; see
[`../closeout/README.md`](../closeout/README.md).

## 8. Reviewer write scope

- The reviewer may edit only `docs/planning/audits/step-NN.md`.
- Within that file, the reviewer may edit its own metadata (`Auditor`, `Audit date`) and the `Record state` field, the `Standard audit checklist` completion marks and inline verification notes (not item
  text), the `Evidence` section, the `Findings` section, and the `Verdict` section for the current round.
- The reviewer must not edit the request scope, checklist item text, known notes, or resolutions owned by the
  step agent, and must not edit any other file.
- Checklist marks record what the reviewer actually audited; the step agent owns the checklist item text.
- The reviewer may commit that one file with a documentation-only message and `[skip ci]`; it must not merge,
  tag, push `main`, or delete branches. If it cannot commit, it writes the record and the step agent commits it
  unchanged.
- The implementor adds resolutions and status updates after the record commit without altering the reviewer's
  findings, evidence, or verdict. That resolution/status commit is documentation-only and does not invalidate
  the audit; the audited commit remains the parent of the reviewer's record commit.
- The record keeps each round: `### Audit round 1`, `### Re-audit round 2`, etc. Never overwrite an earlier
  round.
- A record commit that touches any other file is invalid; the verdict must be re-run by a fresh reviewer.
- The audit is invalidated when the audited branch moves after the freeze except for documented
  documentation-only request/resolution commits, when the reviewer edits code or design docs, or when an earlier
  round is overwritten. Re-run the audit with a fresh reviewer.
- `Re-audit required` is a record state, not a verdict. Keep it in the record state field, not in the verdict
  checklist.
- The auditor must not approve its own report. The human owner approves the merge and tag.
- The auditor records that it is a fresh context with no shared conversation with the implementor, planner, or
  orchestrator.

## 9. Step amendments

- An amendment integrated before the parent audit freeze is audited as part of the parent step at the combined
  commit. The audit request lists the amendment IDs and the combined commit; no separate audit record or tag is
  created.
- A post-merge amendment needs its own maintenance audit and a new tag with owner approval. Never move or
  delete the existing `step-NN` tag.
- The amendment page links to the parent audit record. The auditor still edits only the audit record and
  commits only that file.

## 10. Approval report

When an audit verdict is presented to the human owner for merge or tag approval, the requesting agent must include
the cumulative approval report required by [`AGENTS.md` §3.12](../../../AGENTS.md). It must cover every audit and
review round, every finding and its resolution, accepted notes and waivers, residual risk, and the
no-Blocker/no-Major gate statement. Reporting only the latest round or only open findings is not acceptable. The
record's `Approval summary` section is the durable source for that report.
