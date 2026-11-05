# Step Audits

This directory records the independent audit gate between finishing a step and merging it into `main`.
Audits are separate from the step agent's self-checks: the implementer may test and review its own work, but
a fresh, read-only audit context decides whether the step is ready for approval.

## 1. Roles

| Role | Responsibility |
| --- | --- |
| Step agent | Implement, self-check, create/update the audit request, fix findings, update status/docs/parity |
| Audit agent | Independent review of the frozen commit; writes a gitignored audit report only |
| Human owner | Approve fixes, merge, and tag |

The audit request, its scope, and its acceptance checklist are owned by the step agent. The audit agent must
not change code, configuration, tests, scope, status, the step branch, or the tracked audit request. It writes
a report to a gitignored handoff file; the step agent or a neutral scribe transcribes the findings and verdict
into the durable audit request. If the auditor starts fixing code, it becomes an implementer and a new auditor
is required for the final pass.

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
7. The auditor writes a report to the gitignored handoff file
   `.tmp/audit/step-NN-rN-<short-sha>-audit.md` (or `...-reaudit.md` for a re-audit). The report uses the
   audit template and records the audited commit, CI run, commands run, findings, verdict, and follow-up.
   The auditor does not commit, push, or edit tracked files.
8. The step agent or a neutral scribe transcribes the auditor's findings and verdict verbatim into
   `docs/planning/audits/step-NN.md`, adds the audit metadata, and commits the documentation-only record.
   The auditor must not be the person who commits or approves it.
9. If fixes are required:
   - the step agent reads the handoff report and fixes the findings on a new feature branch;
   - squash-merges into the step branch;
   - pushes and waits for CI;
   - updates the audit request to `Re-audit required` with the new commit.
10. The auditor re-audits the new commit and writes a new handoff report. The same auditor may continue if it
    remained read-only; otherwise start a fresh audit context. Keep the earlier report in the durable audit
    request history.
11. On `Passed`, the human owner gives merge approval.
12. Merge the step branch into `main` with `--no-ff`, tag `step-NN`, push, and delete the step branch.
13. Delete the handoff file only after the durable audit record is committed.

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
that every step has an audit entry before implementation. The step agent owns its entry and updates the
request fields, scope, and acceptance checklist as the step progresses. The auditor completes the findings
and verdict in the handoff report.

The audit request is the durable record. The auditor's raw report lives in the gitignored `.tmp/audit/`
handoff directory; the step agent or scribe transcribes it into this file. Do not rely on chat history, and do
not commit the handoff file.

## 8. Audit handoff files

- Directory: `.tmp/audit/` (gitignored). If the repository is read-only, use `/tmp/opencode/audit/`.
- Naming: `step-NN-rN-<short-sha>-audit.md`; use `...-reaudit.md` for a re-audit.
- The auditor writes only this report and normal build/test artifacts.
- The implementor reads the report but does not edit it.
- The scribe or step agent transcribes findings and verdict verbatim, adds metadata and resolutions, and
  commits the durable record.
- The durable request keeps each round: `### Audit round 1`, `### Re-audit round 2`, etc. Never overwrite an
  earlier round.
- Delete the handoff only after the durable record is committed.
- The auditor must not commit or approve its own report.
