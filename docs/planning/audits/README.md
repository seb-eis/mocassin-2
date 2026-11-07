# Step Audits

This directory records the independent audit gate between finishing a step and merging it into `main`.
Audits are separate from the step agent's self-checks: the implementer may test and review its own work, but
a fresh, out-of-context audit context decides whether the step is ready for approval.

## 1. Roles

| Role | Responsibility |
| --- | --- |
| Step agent (implementor) | Implement, self-check, create/update the audit record's request fields, fix findings, add resolutions, update status/docs/parity |
| Audit agent (reviewer) | Independent review of the frozen commit; writes evidence, findings, and verdict directly into the audit record |
| Human owner | Approve fixes, merge, and tag |

The audit request, its scope, and its acceptance checklist are owned by the step agent. The audit agent may
edit only the audit record's reviewer-owned metadata, evidence, findings, and verdict. It must not change code,
configuration, tests, scope, checklists, resolutions, status dashboard, step page, parity matrix, or the step
branch. If the auditor starts fixing code, it becomes an implementer and a new auditor is required for the final
pass. The reviewer writes directly into `docs/planning/audits/step-NN.md`; there is no handoff file and no
transcription step.

## 2. Audit status values

| Status | Meaning |
| --- | --- |
| **Not requested** | No audit request yet. |
| **Ready for audit** | Step branch is frozen at a specific commit and the request is complete. |
| **In audit** | A fresh audit context is reviewing the frozen commit. |
| **Fixes required** | The audit found blocking or material issues. |
| **Re-audit required** | Fixes were applied and the new commit needs verification. |
| **Passed** | The audit found no blocking issues at the recorded commit. |
| **Waived** | The human owner explicitly accepted a documented exception. |

## 3. Required sequence

1. The step agent finishes all features, the closeout feature, documentation, status, and parity updates.
2. The step branch is pushed and its CI is green.
3. The step agent creates or updates `docs/planning/audits/step-NN.md`:
   - status: `Ready for audit`
   - exact audited commit
   - CI run
   - scope and acceptance criteria
   - known limitations
4. The step branch is frozen at the audited commit. Only the reviewer's record commit and the step agent's
   resolution/status commit may follow; neither may change code, tests, configuration, or the reviewer's text.
5. A fresh audit agent reads:
   - `AGENTS.md`
   - the step page
   - the audit request
   - `docs/planning/status.md`
   - relevant ADRs, contracts, and quality strategy
6. The auditor runs the required builds/tests and reviews the diff and artifacts.
7. The auditor writes the evidence, findings, and verdict directly into
   `docs/planning/audits/step-NN.md` and commits only that file with a documentation-only message and
   `[skip ci]`. The audited commit is the parent of the record commit. The auditor does not merge, tag, push
   `main`, or change any other file. If the auditor cannot commit, it writes the record and the step agent
   commits it unchanged.
8. If fixes are required:
   - the step agent adds the resolutions to the record;
   - the step agent fixes the findings on a new feature branch;
   - squash-merges into the step branch;
   - pushes and waits for CI;
   - updates the audit record to `Re-audit required` with the new commit.
9. The auditor re-audits the new commit and appends a new round to the record. The same auditor may continue if
   it stayed within the reviewer write scope; otherwise start a fresh audit context. Keep every earlier round
   in the record.
10. On `Passed`, the human owner gives merge approval.
11. Merge the step branch into `main` with `--no-ff`, tag `step-NN`, push, and delete the step branch.

## 4. What the audit must cover

- Step acceptance criteria and “Done when”.
- Required tests, property tests, contract tests, and documentation.
- Public/protected XML documentation policy.
- Status dashboard, step page, parity matrix, and feature log accuracy.
- CI configuration and actual CI result.
- Format/version changes, fixtures, and migration behavior.
- Security, dependency, licence, and secret-scan gates.
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

Use [`step-audit-template.md`](step-audit-template.md) as the starting point. All 32 files are pre-seeded so
that every step has an audit entry before implementation. The step agent owns the request fields, scope,
checklists, and resolutions. The auditor writes the evidence, findings, and verdict directly into this file.

The audit record is the durable record. Do not rely on chat history, and do not use a separate handoff file.

## 8. Reviewer write scope

- The reviewer may edit only `docs/planning/audits/step-NN.md`.
- Within that file, the reviewer may edit its own metadata (`Auditor`, `Audit date`, `Audit status`), the
  `Evidence` section, the `Findings` section, and the `Verdict` section for the current round.
- The reviewer must not edit the request scope, checklists, known notes, or resolutions owned by the step
  agent, and must not edit any other file.
- The reviewer may commit that one file with a documentation-only message and `[skip ci]`; it must not merge,
  tag, push `main`, or delete branches. If it cannot commit, it writes the record and the step agent commits it
  unchanged.
- The implementor adds resolutions and status updates after the record commit without altering the reviewer's
  findings, evidence, or verdict. That resolution/status commit is documentation-only and does not invalidate
  the audit; the audited commit remains the parent of the reviewer's record commit.
- The record keeps each round: `### Audit round 1`, `### Re-audit round 2`, etc. Never overwrite an earlier
  round.
- A record commit that touches any other file is invalid; the verdict must be re-run by a fresh reviewer.
- The auditor must not approve its own report. The human owner approves the merge and tag.
