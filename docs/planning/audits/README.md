# Step Audits

This directory records the independent audit gate between finishing a step and merging it into `main`.
Audits are separate from the step agent's self-checks: the implementer may test and review its own work, but
a fresh, read-only audit context decides whether the step is ready for approval.

## 1. Roles

| Role | Responsibility |
| --- | --- |
| Step agent | Implement, self-check, fix findings, update status/docs/parity |
| Audit agent | Independent read-only review; no repository modifications |
| Human owner | Approve fixes, merge, and tag |

The audit agent must not edit the repository. If it starts fixing code, it becomes an implementer and a new
auditor is required for the final pass.

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
4. The step branch is frozen. Do not add commits while the audit is running.
5. A fresh audit agent reads:
   - `AGENTS.md`
   - the step page
   - the audit request
   - `docs/planning/status.md`
   - relevant ADRs, contracts, and quality strategy
6. The auditor runs the required builds/tests and reviews the diff and artifacts.
7. The auditor writes findings and a verdict into the audit request.
8. If fixes are required:
   - the step agent fixes them on a new feature branch;
   - squash-merges into the step branch;
   - pushes and waits for CI;
   - updates the audit request to `Re-audit required` with the new commit.
9. The auditor re-audits the new commit. The same auditor may continue if it remained read-only; otherwise
   start a fresh audit context.
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

Use [`step-audit-template.md`](step-audit-template.md) as the starting point. The audit request is the
durable handoff between the step agent and the audit agent; do not rely on chat history.
