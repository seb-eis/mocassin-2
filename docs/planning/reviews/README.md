# Pre-Implementation Design Reviews

This directory records the independent design-review gate that runs before a step is implemented. It is
separate from the post-implementation audit in `docs/planning/audits/`.

## 1. Purpose

The design review checks that a step is ready to implement: the decisions are complete and consistent, the
cross-step interfaces are defined, and the test, documentation, licensing, and audit plans are adequate. It
does not review implementation code, because none exists yet.

## 2. Roles

| Role | Responsibility |
| --- | --- |
| Planning agent | Facilitate decisions, write the review request, update design docs |
| Step agent | Own the step page and review record, fix design findings |
| Planning auditor | Fresh, read-only, out-of-context design review; writes a gitignored report only |
| Scribe | Transcribe findings and verdict verbatim; apply approved documentation edits |
| Human owner | Approve the verdict and the start of implementation |

The planning auditor must not be the agent that authored the decisions or will implement the step. It must not
edit tracked files, commit, push, or change a branch.

## 3. Status values

| Status | Meaning |
| --- | --- |
| **Not requested** | No design review yet. |
| **Ready for review** | Design decisions and docs are frozen at a specific commit. |
| **In review** | A fresh planning auditor is reviewing the frozen docs. |
| **Changes required** | Blocking or material design issues were found. |
| **Re-review required** | Docs were updated and need another review. |
| **Ready for implementation** | No blocking design issues remain; the human owner may approve the start. |
| **Waived** | The human owner explicitly accepted that no review is required. |

## 4. Required sequence

1. The planning agent and step agent finish the step's design decisions and update the step page, data
   contracts, architecture, quality strategy, open decisions, parity matrix, and related docs.
2. The docs are frozen at a specific commit.
3. The step agent creates or updates `docs/planning/reviews/step-NN.md` from the template:
   - status: `Ready for review`
   - reviewed commit
   - design decisions and scope
   - known limitations
4. A fresh planning auditor reads the step page, decisions, dependency contracts, architecture, data
   contracts, ADRs, quality strategy, open decisions, parity matrix, licensing records, and audit plan.
5. The planning auditor writes a report to the gitignored handoff file
   `.tmp/review/step-NN-rN-<short-sha>-design-review.md` (or `...-re-review.md`). The report records the
   reviewed commit, files reviewed, findings, verdict, and follow-up. The auditor does not commit, push, or
   edit tracked files.
6. The step agent or a neutral scribe transcribes the findings and verdict verbatim into
   `docs/planning/reviews/step-NN.md`, adds the metadata, and commits the documentation-only record. The
   planning auditor must not commit or approve it.
7. If the verdict is `Changes required`, the planning agent/scribe updates the design docs, the step agent
   marks the review `Re-review required`, and a fresh planning auditor re-reviews the new commit.
8. On `Ready for implementation`, the human owner approves the start. The step branch is created from the
   reviewed `main` commit.
9. The post-implementation audit in `docs/planning/audits/` remains mandatory and separate.

## 5. What the review must cover

- Step scope, non-goals, and testable acceptance criteria.
- All design decisions are recorded and internally consistent.
- Cross-step interfaces, types, formats, and data versions are defined.
- Dependencies are complete and their contracts are stable.
- Format, versioning, and migration impacts are addressed.
- Test strategy, property tests, golden fixtures, performance targets, and CI gates are defined.
- Documentation, status dashboard, parity matrix, and post-implementation audit updates are planned.
- Licensing, provenance, and dependency issues are resolved.
- Risks and mitigations are realistic.
- No hidden scope, stale placeholders, or unowned decisions.

## 6. Finding severity

| Severity | Meaning |
| --- | --- |
| **Blocker** | The step cannot start until the design is corrected. |
| **Major** | Material design gap or contradiction; correct before implementation. |
| **Minor** | Small inconsistency or missing detail; fix or explicitly accept. |
| **Note** | Observation or future improvement; no start block. |

## 7. Verdict

- **Ready for implementation** — no blockers or majors remain.
- **Changes required** — blockers or majors remain.
- **Ready with notes** — only minor/note findings remain; the human owner accepts them explicitly.
- **Re-review required** — docs were updated after a `Changes required` verdict.
- **Waived** — the human owner records why no review is required.

## 8. Handoff files

- Directory: `.tmp/review/` (gitignored). If the repository is read-only, use `/tmp/opencode/review/`.
- Naming: `step-NN-rN-<short-sha>-design-review.md`; use `...-re-review.md` for a re-review.
- The planning auditor writes only this report and normal read-only commands.
- The implementor and step agent read the report but do not edit it.
- The scribe or step agent transcribes findings and verdict verbatim and commits the durable record.
- Delete the handoff only after the durable record is committed.
- The planning auditor must not commit or approve its own report.

## 9. Review record files

Create one file per step when its design review starts:

`docs/planning/reviews/step-00.md`  
`docs/planning/reviews/step-01.md`  
`docs/planning/reviews/step-02.md`  
...

Step 00 and Step 01 are recorded as `Waived — process introduced after completion`. No retrospective design
review is performed.

The review record keeps each round: `### Review round 1`, `### Re-review round 2`, etc. Never overwrite an
earlier round.
