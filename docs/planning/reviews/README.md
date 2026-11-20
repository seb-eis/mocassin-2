# Pre-Implementation Design Reviews

This directory records the independent design-review gate that runs before a step is implemented. It is
separate from the post-implementation audit in `docs/planning/audits/`.

> Historical note: records written before the direct-write policy may reference `.tmp/review/*` handoff files
> that no longer exist. Treat those references as historical and do not rewrite the record. New reviews write
> evidence, findings, and verdict directly into the durable record.

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
| Orchestrator (spawner) | Provides the frozen request and spawns a fresh reviewer on behalf of the request owner; relays the verdict; does not review or approve |
| Human owner | Approve the verdict and the start of implementation |

The planning auditor must not be the agent that authored the decisions or will implement the step. It may edit
only the review record's reviewer-owned metadata, evidence, findings, and verdict. It must not edit design docs,
the step page, status dashboard, parity matrix, request scope, checklists, resolutions, code, tests, or
configuration. The auditor writes directly into `docs/planning/reviews/step-NN.md`; there is no handoff file
and no transcription step.

### 2.1 Reviewer spawning

The orchestrator is the only spawner. The planner or step agent asks the orchestrator for a review; the owner
does not spawn a reviewer directly. The spawner must not be the reviewer.

- The reviewer is a fresh context with no shared conversation with the planner, implementor, or orchestrator.
- The orchestrator provides only the frozen commit, the review request, repository links, and the required
  commands/scope. It must not supply a desired verdict, pass on its conversation history, or substitute a
  summary for reading the repository.
- The reviewer writes findings and verdict directly into the record. The orchestrator and request owner do not
  edit them.
- The human owner approves the final verdict and the start gate. The orchestrator must not mark a verdict, waive
  findings, merge, tag, or push `main`.
- A re-review requires a new commit with fixes, made by the planner or step agent. Do not re-spawn reviewers
  until a preferred verdict appears.

## 3. Status values

| Status | Meaning |
| --- | --- |
| **Not requested** | No design review yet. |
| **Ready for review** | Design decisions and docs are frozen at a specific commit. |
| **In review** | A fresh planning auditor is reviewing the frozen docs. |
| **Changes required** | Blocking or material design issues were found. |
| **Re-review required** | Docs were updated and need another review. |
| **Ready with notes** | Only minor/note findings remain; the human owner explicitly accepts them. |
| **Ready for implementation** | No blocking design issues remain; the human owner may approve the start. |
| **Waived** | The human owner explicitly accepted that no review is required. |

## 4. Required sequence

1. The planning agent and step agent finish the step's design decisions and update the step page, data
   contracts, architecture, quality strategy, open decisions, parity matrix, and related docs. They apply those
   edits directly.
2. The docs are frozen at a specific commit.
3. The step agent creates or updates `docs/planning/reviews/step-NN.md` from the template:
   - record state: `Ready for review`
   - reviewed commit
   - design decisions and scope
   - known limitations
4. The step agent asks the orchestrator to spawn a fresh planning auditor. The auditor reads the step page,
   decisions, dependency contracts, architecture, data contracts, ADRs, quality strategy, open decisions, parity
   matrix, licensing records, and audit plan. The
   auditor must not have authored or implemented the design.
5. The planning auditor writes the evidence, findings, and verdict directly into
   `docs/planning/reviews/step-NN.md` and commits only that file with a documentation-only message and
   `[skip ci]`. The reviewed commit is the parent of the record commit. The auditor does not merge, tag, push
   `main`, or change any other file. If the auditor cannot commit, it writes the record and the step agent
   commits it unchanged.
6. If the verdict is `Changes required`, the planning agent and step agent update the design docs directly, the
   step agent adds the resolutions and marks the review `Re-review required`, and a fresh planning auditor
   re-reviews the new commit. The new round is appended; earlier rounds are not overwritten.
7. On `Ready for implementation`, the human owner approves the start. The orchestrator presents the verdict and
   the feature list; the cumulative approval report is for a merge, tag, or Blocker/Major waiver, not for this
   gate. The step branch is created from the reviewed `main` commit.
8. The post-implementation audit in `docs/planning/audits/` remains mandatory and separate.

## 5. What the review must cover

- Step scope, non-goals, and testable acceptance criteria.
- Complexity class and split decision, recorded and justified per
  [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers).
- All design decisions are recorded and internally consistent.
- Cross-step interfaces, types, formats, and data versions are defined.
- Dependencies are complete and their contracts are stable.
- Format, versioning, and migration impacts are addressed.
- Test strategy, property tests, golden fixtures, performance targets, and CI gates are defined.
- Documentation, status dashboard, parity matrix, and post-implementation audit updates are planned.
- Licensing, provenance, and dependency issues are resolved.
- Code layout and namespace plan follow `docs/development/code-structure.md`.
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
- **Waived** — the human owner records why no review is required.

`Re-review required` is a record state, not a verdict; it means the docs changed after a `Changes required`
verdict and a fresh review is pending.

## 8. Reviewer write scope

- The reviewer may edit only `docs/planning/reviews/step-NN.md`.
- Within that file, the reviewer may edit its own metadata (`Reviewer`, `Review date`) and the `Record state` field, the three checklist sections (checkbox marks and short inline verification notes
  only; not item text), the `Evidence` section, the `Findings` section, and the `Verdict` section for the current
  round.
- The reviewer must not edit the request scope, checklist item text, known limitations, or resolutions owned by
  the step agent, and must not edit any other file.
- Checklist marks record what the reviewer actually reviewed; the step agent owns the checklist item text.
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
- The reviewer records that it is a fresh context with no shared conversation with the planner or implementor.

### 8.1 What invalidates a review

- The record commit touches any file other than the record. Re-run the review with a fresh reviewer.
- The reviewer edits code, tests, configuration, or design docs. It has become an implementor; a fresh reviewer
  is required.
- The docs branch moves after the frozen commit and before the record commit, except for documented
  documentation-only request/status commits. Record the delta as a note or re-review.
- An earlier round is overwritten. Rounds are append-only.

## 9. Review record files

Create one file per step when its design review starts. Do not pre-seed review records for steps that have not
started; use the current template at review time.

`docs/planning/reviews/step-00.md`  
`docs/planning/reviews/step-01.md`  
`docs/planning/reviews/step-02.md`  
...

Step 00 and Step 01 are recorded as `Waived — process introduced after completion`. No retrospective design
review is performed.

The review record keeps each round: `### Review round 1`, `### Re-review round 2`, etc. Never overwrite an
earlier round.

## 10. Step amendment reviews

- Amendment reviews use the same statuses, severity, verdict, and reviewer write scope as a normal design
  review.
- Record: `docs/planning/reviews/step-NN-amend-M.md`.
- Scope: the amendment delta and its impact on the parent step, not a full re-review of the parent design.
- The parent review record links to the amendment review record.
- The reviewer still edits only the review record and commits only that file.
- The parent audit covers the combined solution; an integrated amendment does not get a separate audit or tag.

## 11. Approval report

When a merge, a tag, or a Blocker/Major waiver is presented to the human owner, the requesting agent must include
the cumulative approval report required by [`AGENTS.md` §3.12](../../../AGENTS.md). A verdict acceptance on its
own does not need it. The report must cover every review round, every finding and its resolution, accepted notes
and waivers, residual risk, and the no-Blocker/no-Major gate statement. Reporting only the latest round or only
open findings is not acceptable. The record's `Approval summary` section is the durable source for that report.

## 12. Cross-cutting phase reviews

After the last step of each phase is merged and tagged, the phase closes with one combined repository-wide
review with three sections:

- **policies and planning** — agent instructions, workflows, CI, code structure, process records, phase status;
- **architecture and design status** — architecture, contracts, design status, cross-step interfaces, and
  original-design fidelity (intended corrections, ported original strengths, regressions);
- **documentation status** — coverage, accuracy, navigation, API docs, publication boundary.

The owner may ask for three separate reviews when a phase is unusually risky; that is an explicit choice. The
next phase's first step design review may not start until the combined verdict is accepted by the human owner, or
the review is explicitly waived with a recorded rationale.

The architecture and design status review must explicitly answer whether the phase made the corrections to the
original that it intended, ported the good/clever original design aspects, and introduced a regression. The
reviewer records each answer with code, test, benchmark, or side-by-side design evidence; an unrecorded
regression, a missing intended correction, or a lost original strength is a finding.

### 12.1 Numbering

Review IDs are globally sequential and are never reused. From Phase C, each phase gets one combined ID:

| Phase | Steps | Reviews |
| --- | --- | --- |
| B. Model definition | 03–07 | `XC-01`, `XC-02`, `XC-03` (historical, three separate) |
| C. Compilation and build | 08–12 | `XC-04` (combined) |
| D. Solver | 13–19 | `XC-05` |
| E. Results | 20–22 | `XC-06` |
| F. GUI | 23–28 | `XC-07` |
| G. Release and documentation | 29–31 | `XC-08` |

Phase A is waived because the process was introduced after it completed. Run the lightweight
[drift checks](../../development/drift-checks.md) at each milestone, or monthly when there is none; event-driven
checks still apply. Create cross-cutting review records at the phase boundary; the Phase B records are
historical. The registry and records are in [`cross-cutting/`](cross-cutting/README.md).

### 12.2 Orchestration

Each review is coordinated by the orchestrator, not by the reviewer:

1. The planner or step agent freezes the request and target commit.
2. The orchestrator spawns a fresh independent reviewer with the frozen commit, record, links, and scope; it does
   not request a verdict or substitute a summary for repository reading.
3. The reviewer writes only its record and commits only that file with `[skip ci]`.
4. If findings remain, the planner or step agent fixes the repository state on a maintenance branch, updates the
   appropriate design/process/docs artifact, adds resolutions without altering reviewer text, records the new
   commit, and asks the orchestrator for re-review.
5. Repeat until the reviewer no longer identifies Blocker/Major issues. Use a fresh reviewer for the new round
   unless the same reviewer stayed read-only and outside the remediation.
6. The human owner approves the final verdict and the phase gate.

The orchestrator must not fix findings, edit reviewer text, or approve the verdict. The reviewer must not fix
findings, merge, tag, push `main`, or delete branches. A record commit that touches any other file is invalid.
