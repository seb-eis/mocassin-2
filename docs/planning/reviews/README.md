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
| Planning agent (planner) | Facilitate decisions, write the review request, update design docs directly |
| Step agent (implementor) | Own the step page and review record request, fix design findings, add resolutions |
| Planning auditor (reviewer) | Fresh, out-of-context design review; writes evidence, findings, and verdict directly into the review record |
| Human owner | Approve the verdict and the start of implementation |

The planning auditor must not be the agent that authored the decisions or will implement the step. It may edit
only the review record's reviewer-owned metadata, evidence, findings, and verdict. It must not edit design docs,
the step page, status dashboard, parity matrix, request scope, checklists, resolutions, code, tests, or
configuration. The auditor writes directly into `docs/planning/reviews/step-NN.md`; there is no handoff file
and no transcription step.

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
   contracts, architecture, quality strategy, open decisions, parity matrix, and related docs. They apply those
   edits directly.
2. The docs are frozen at a specific commit.
3. The step agent creates or updates `docs/planning/reviews/step-NN.md` from the template:
   - status: `Ready for review`
   - reviewed commit
   - design decisions and scope
   - known limitations
4. A fresh planning auditor reads the step page, decisions, dependency contracts, architecture, data
   contracts, ADRs, quality strategy, open decisions, parity matrix, licensing records, and audit plan. The
   auditor must not have authored or implemented the design.
5. The planning auditor writes the evidence, findings, and verdict directly into
   `docs/planning/reviews/step-NN.md` and commits only that file with a documentation-only message and
   `[skip ci]`. The reviewed commit is the parent of the record commit. The auditor does not merge, tag, push
   `main`, or change any other file. If the auditor cannot commit, it writes the record and the step agent
   commits it unchanged.
6. If the verdict is `Changes required`, the planning agent and step agent update the design docs directly, the
   step agent adds the resolutions and marks the review `Re-review required`, and a fresh planning auditor
   re-reviews the new commit. The new round is appended; earlier rounds are not overwritten.
7. On `Ready for implementation`, the human owner approves the start. The step branch is created from the
   reviewed `main` commit.
8. The post-implementation audit in `docs/planning/audits/` remains mandatory and separate.

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

## 8. Reviewer write scope

- The reviewer may edit only `docs/planning/reviews/step-NN.md`.
- Within that file, the reviewer may edit its own metadata (`Reviewer`, `Review date`, `Design review status`),
  the `Evidence` section, the `Findings` section, and the `Verdict` section for the current round.
- The reviewer must not edit the request scope, checklists, known limitations, or resolutions owned by the
  step agent, and must not edit any other file.
- The reviewer may commit that one file with a documentation-only message and `[skip ci]`; it must not merge,
  tag, push `main`, or delete branches. If it cannot commit, it writes the record and the step agent commits it
  unchanged.
- The implementor adds resolutions and status updates after the record commit without altering the reviewer's
  findings, evidence, or verdict. That resolution/status commit is documentation-only and does not invalidate
  the review; the reviewed commit remains the parent of the reviewer's record commit.
- The record keeps each round: `### Review round 1`, `### Re-review round 2`, etc. Never overwrite an earlier
  round.
- A record commit that touches any other file is invalid; the verdict must be re-run by a fresh reviewer.
- Historical records keep the role names in force when they were written; do not rewrite them.
- The planning auditor must not approve its own report. The human owner approves the start.

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
