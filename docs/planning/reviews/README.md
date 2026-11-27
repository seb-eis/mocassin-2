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
| Design agent | Facilitate decisions, write the review request, update design docs directly |
| Dev (`dev-free`/`dev`) | Own the step page and review record request, fix design findings, add resolutions |
| Review agent | Fresh, out-of-context design review; writes evidence, findings, and verdict directly into the review record |
| Lead (spawner) | Provides the frozen request and spawns a fresh review agent on behalf of the request owner; relays the verdict; does not review or approve |
| Human owner | Approve the verdict and the start of implementation |

The review agent must not be the agent that authored the decisions or will implement the step. It may edit
only the review record's review-owned metadata, evidence, findings, and verdict. It must not edit design docs,
the step page, status dashboard, parity matrix, request scope, checklists, resolutions, code, tests, or
configuration. It writes directly into `docs/planning/reviews/step-NN.md`; there is no handoff file and no
transcription step.

### 2.1 Review agent spawning

The lead is the only spawner. The design agent or dev asks the lead for a review; the owner does not spawn a
review agent directly. The spawner must not be the review agent.

- The review agent is a fresh context with no shared conversation with the design agent, dev, or lead.
- The lead provides only the frozen commit, the review request, repository links, and the required
  commands/scope. It must not supply a desired verdict, pass on its conversation history, or substitute a
  summary for reading the repository.
- The review agent writes findings and verdict directly into the record. The lead and request owner do not
  edit them.
- The human owner approves the final verdict and the start gate. The lead must not mark a verdict, waive
  findings, merge, tag, or push `main`.
- A re-review requires a new commit with fixes, made by the design agent or dev. Do not re-spawn review agents
  until a preferred verdict appears.

### 2.2 Design class and review depth

The step page records a **design class** per
[`../../development/design-proportionality.md`](../../development/design-proportionality.md):

- **Full** — public API/contract, `.mocprj`/`.msl`/`.msr`/`.mcs` format or schema/version, dependency,
  cross-package interface, data migration, security/concurrency, an irreversible decision, or an architecture
  boundary. Full review checklist; stop when no Blocker or Major remains.
- **Light** — an internal feature inside an existing boundary. One pass answering the five design questions;
  Minor and Note findings are accepted and do not trigger a re-review. A Blocker or Major means the step was
  misclassified and moves to Full.
- **None** — documentation, typo, test-only change, mechanical refactor, or generated-file refresh. No design
  review; a one-line record in the step page. Under `standard`, `None` is pre-authorized for these categories
  and needs no per-item owner waiver.

A Full review does not open another round for Minor or Note findings alone. A third Full round needs an owner
decision and a recorded reason. Before the audit request, the dev runs the pre-audit gate in
[`../../development/design-proportionality.md`](../../development/design-proportionality.md) §3; bookkeeping
findings belong there, not in another review round.

## 3. Status values

| Status | Meaning |
| --- | --- |
| **Not requested** | No design review yet. |
| **Ready for review** | Design decisions and docs are frozen at a specific commit. |
| **In review** | A fresh review agent is reviewing the frozen docs. |
| **Changes required** | Blocking or material design issues were found. |
| **Re-review required** | Docs were updated and need another review. |
| **Ready with notes** | Only minor/note findings remain; the human owner explicitly accepts them. |
| **Ready for implementation** | No blocking design issues remain; the human owner may approve the start. |
| **Waived** | The human owner explicitly accepted that no review is required. |

## 4. Required sequence

1. The design agent and dev finish the step's design decisions and update the step page, data
   contracts, architecture, quality strategy, open decisions, parity matrix, and related docs. They apply those
   edits directly. The step page records the design class.
2. The docs are frozen at a specific commit.
3. The dev creates or updates `docs/planning/reviews/step-NN.md` from the template:
   - record state: `Ready for review`
   - reviewed commit
   - design class (`Full` or `Light`)
   - design decisions and scope
   - known limitations
4. The dev asks the lead to spawn a fresh review agent. The review agent reads the step page,
   decisions, dependency contracts, architecture, data contracts, ADRs, quality strategy, open decisions, parity
   matrix, licensing records, and audit plan. It must not have authored or implemented the design. A `Light`
   review answers the five design questions; a `Full` review also checks the full checklist.
5. The review agent writes the evidence, findings, and verdict directly into
   `docs/planning/reviews/step-NN.md` and commits only that file with a documentation-only message and
   `[skip ci]`. The reviewed commit is the parent of the record commit. It does not merge, tag, push `main`, or
   change any other file. If it cannot commit, it writes the record and the dev commits it unchanged.
6. If the verdict is `Changes required`, the design agent and dev update the design docs directly, the dev adds
   the resolutions and marks the review `Re-review required`, and a fresh review agent re-reviews the new commit.
   The new round is appended; earlier rounds are not overwritten. Do not open a new round for Minor or Note
   findings alone.
7. On `Ready for implementation`, or an owner-accepted `Ready with notes`, the human owner approves the start.
   The lead presents the verdict and the feature list; the cumulative approval report is for a merge, tag, or
   Blocker/Major waiver, not for this gate. The step branch is created from the reviewed `main` commit.
8. A `None` step records the one-line design decision in the step page, skips the review record, and proceeds
   under the normal branch, local-gate, and audit rules.
9. The post-implementation audit in `docs/planning/audits/` remains mandatory and separate.

## 5. What the review must cover

A design review answers five questions:

1. Are the contracts this step touches resolved, with version and migration?
2. Is every acceptance criterion testable, and does it name the test, command, or fixture that proves it?
3. Are the non-goals and irreversible decisions explicit?
4. Is the feature list complete, and does it fit one boundary?
5. Are the risks real, with an owner?

A `Light` review uses the five questions only. A `Full` review also checks the full-depth list:

- Step scope, non-goals, and testable acceptance criteria.
- Complexity class and split decision, recorded and justified per
  [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers).
- The design class is recorded and consistent with the triggers.
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

Prose style, status bookkeeping, code layout, and file limits are not design findings. Route them to the
pre-audit gate, not to another review round.

## 6. Finding severity

| Severity | Meaning |
| --- | --- |
| **Blocker** | The step cannot start until the design is corrected. |
| **Major** | Material design gap or contradiction; correct before implementation. |
| **Minor** | Small inconsistency or missing detail; fix or explicitly accept. |
| **Note** | Observation or future improvement; no start block. |

Record each finding's **origin** as design, implementation, evidence, or documentation. At a phase boundary the
distribution feeds the metric in
[`../../development/design-proportionality.md`](../../development/design-proportionality.md) §4: design-origin
findings are the only ones that justify more design effort.

## 7. Verdict

- **Ready for implementation** — no blockers or majors remain.
- **Changes required** — blockers or majors remain.
- **Ready with notes** — only minor/note findings remain; the human owner accepts them explicitly.
- **Waived** — the human owner records why no review is required.

A Full review stops at `Ready for implementation` when no Blocker or Major remains. Minor and Note findings are
accepted or deferred and do not trigger another round. A Light review is one pass; if it finds a Blocker or Major,
the step is reclassified Full. A third Full round needs an owner decision and a recorded reason.

`Re-review required` is a record state, not a verdict; it means the docs changed after a `Changes required`
verdict and a fresh review is pending.

## 8. Review agent write scope

- The review agent may edit only `docs/planning/reviews/step-NN.md`.
- Within that file, the review agent may edit its own metadata (`Review agent`, `Review date`) and the `Record state` field, the three checklist sections (checkbox marks and short inline verification notes
  only; not item text), the `Evidence` section, the `Findings` section, and the `Verdict` section for the current
  round.
- The review agent must not edit the request scope, checklist item text, known limitations, or resolutions owned by
  the dev, and must not edit any other file.
- Checklist marks record what the review agent actually reviewed; the dev owns the checklist item text.
- The review agent may commit that one file with a documentation-only message and `[skip ci]`; it must not merge,
  tag, push `main`, or delete branches. If it cannot commit, it writes the record and the dev commits it
  unchanged.
- The dev adds resolutions and status updates after the record commit without altering the review agent's
  findings, evidence, or verdict. That resolution/status commit is documentation-only and does not invalidate
  the review; the reviewed commit remains the parent of the review agent's record commit.
- The record keeps each round: `### Review round 1`, `### Re-review round 2`, etc. Never overwrite an earlier
  round.
- A record commit that touches any other file is invalid; the verdict must be re-run by a fresh review agent.
- Historical records keep the role names in force when they were written; do not rewrite them.
- The review agent must not approve its own report. The human owner approves the start.
- The review agent records that it is a fresh context with no shared conversation with the design agent or dev.

### 8.1 What invalidates a review

- The record commit touches any file other than the record. Re-run the review with a fresh review agent.
- The review agent edits code, tests, configuration, or design docs. It has become a dev; a fresh review agent
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

### 9.1 Policy-update reviews

A **policy-update review** is the design-review gate for a process, workflow, role, or agent-prompt change that
is not a step design. It uses the same independence, evidence, finding-severity, and verdict rules as a step
design review, but its record is `docs/planning/reviews/policy-<YYYY-MM-DD>-<slug>.md`.

- The record owner (design agent or dev) writes the request fields, scope, non-goals, decision and mapping
  tables, checklist, and resolutions. The review agent writes its own metadata, `Record state`, checklist
  marks, `Evidence`, `Findings`, `Verdict`, and, for the post-implementation verification, the separate
  verification section.
- The review agent may edit only the assigned policy record and commits only that file with `[skip ci]`. The
  binding's `docs/planning/reviews/policy-*.md` allow rule exists for this record class; it does not permit
  editing any other file.
- A policy update uses the classes in
  [`../../development/design-proportionality.md`](../../development/design-proportionality.md) §1. Record the
  class in the request and justify it; the trigger is not repeated here. A `Full` policy update uses the full
  checklist and stops when no Blocker or Major remains.
- Historical policy records are not rewritten. A later policy update supersedes the affected rule in the
  normative pages and records the supersession in its own record.
- A policy-update review may use the senior consultation after two completed standard rounds still leave a
  Blocker or Major. The trigger and request contract are in
  [`../../development/senior-consultation.md`](../../development/senior-consultation.md); the senior record is
  `docs/planning/reviews/senior/policy-<date>-<slug>-M.md` and the senior answer is advisory.
- The post-implementation verification pass is appended to the same record under
  `## Post-implementation verification`; it is not a second design round and does not change the design
  verdict.

## 10. Step amendment reviews

- Amendment reviews use the same statuses, severity, verdict, and review agent write scope as a normal design
  review.
- Record: `docs/planning/reviews/step-NN-amend-M.md`.
- Scope: the amendment delta and its impact on the parent step, not a full re-review of the parent design.
- The parent review record links to the amendment review record.
- The review agent still edits only the review record and commits only that file.
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
review agent records each answer with code, test, benchmark, or side-by-side design evidence; an unrecorded
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

### 12.2 Coordination

Each review is coordinated by the lead, not by the review agent:

1. The design agent or dev freezes the request and target commit.
2. The lead spawns a fresh independent review agent with the frozen commit, record, links, and scope; it does
   not request a verdict or substitute a summary for repository reading.
3. The review agent writes only its record and commits only that file with `[skip ci]`.
4. If findings remain, the design agent or dev fixes the repository state on a maintenance branch, updates the
   appropriate design/process/docs artifact, adds resolutions without altering review agent text, records the new
   commit, and asks the lead for re-review.
5. Repeat until the review agent no longer identifies Blocker/Major issues. Use a fresh review agent for the new round
   unless the same review agent stayed read-only and outside the remediation.
6. The human owner approves the final verdict and the phase gate.

The lead must not fix findings, edit review agent text, or approve the verdict. The review agent must not fix
findings, merge, tag, push `main`, or delete branches. A record commit that touches any other file is invalid.
