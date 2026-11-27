# Design proportionality

This page is the normative home for how much design a step needs and when its design review stops. The review
process, roles, and records are in [`docs/planning/reviews/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/reviews/README.md) and
[`docs/planning/audits/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/audits/README.md); this page decides the depth.

A **step** is MOCASSIN's work package. The 32-step model, `step/<NN>-<slug>` branches, `feature/...` branches,
and `step-NN` tags are unchanged. The design class is a property of a step, an amendment, or a policy-update
review, not a new unit of work.

Design is a cost: it consumes the default tier's tokens and the owner's review time. Its value is avoiding contract
rework. Spend it on contract risk, not evenly across every change.

## 1. Design class

Classify the step when it is created and record the class in its page. A step may move up a class at any time. It
may not move down after the design is frozen without an owner decision.

| Class | Use when any of these is true | Minimum artifact | Review depth |
| --- | --- | --- | --- |
| **Full** | public API or contract; `.mocprj`, `.msl`, `.msr`, or `.mcs` format, schema, or version; dependency; cross-package interface; data migration; security or concurrency; an irreversible decision; an architecture boundary | Design section with interfaces, error behaviour, security, migration, and acceptance tests; an ADR for each irreversible decision | Independent design review; stop when no Blocker or Major remains |
| **Light** | internal feature inside an existing boundary; no contract, format, dependency, or architecture change | One-page design section: scope, contracts touched, acceptance criteria with their tests, non-goals, risks | One review agent pass; Minor and Note findings are accepted and do not trigger a re-review |
| **None** | documentation, typo, mechanical refactor, test-only change, generated-file refresh | One line in the step page: what changes and why no design is needed | No design review; record the decision |

**Policy-update reviews.** A policy-update review is the design-review gate for a process, workflow, role, or
agent-prompt change that is not a step design. Its record is
`docs/planning/reviews/policy-<YYYY-MM-DD>-<slug>.md`. It uses the same classes. It is **Full** when it changes a
gate, a role, the agent binding, the CI policy, or a normative process rule; **Light** for a bounded internal
process edit inside an existing boundary; and **None** for a typo, link, or formatting-only change. The record
class and write scope are in
[`docs/planning/reviews/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/reviews/README.md)
§9.1; the class trigger is here.

Rules:

- If any Full trigger applies, the class is Full. Do not split a contract change across steps to avoid the class.
- If a Light step discovers a Full trigger during design, stop and reclassify before freezing. The design is not
  frozen and the step branch does not exist yet.
- **Standard-profile adaptation (owner decision, 2026-11-22):** under the `standard` profile, `None` is
  pre-authorized for documentation, typos, test-only changes, mechanical refactors, and generated-file refreshes.
  A `None` step needs the one-line record but **no per-item owner waiver**. This is a deliberate MOCASSIN
  adaptation of the upstream rule that a `None` class on a reviewing profile is a recorded waiver. The one-line
  record is the evidence; the owner does not have to approve each `None` classification.
- A class chosen after the outcome always matches the outcome. Record the class before the work.
- The class does not replace the complexity class (Low/Medium/High) and split decision in
  [`docs/planning/04-roadmap.md` §3.1](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/04-roadmap.md#31-step-sizing-and-split-triggers). Both are
  recorded: complexity class for sizing, design class for review depth.

Throwaway work that will never be committed is outside this page and the workflow: no class, no branch, and no
record. Use a free model and discard the result. Once the output will be committed, it is `None`-class work and
needs the one-line record.

## 2. Review depth and the stop rule

A design review answers five questions:

1. Are the contracts this step touches resolved, with version and migration?
2. Is every acceptance criterion testable, and does it name the test, command, or fixture that proves it?
3. Are the non-goals and irreversible decisions explicit?
4. Is the feature list complete, and does it fit one boundary?
5. Are the risks real, with an owner?

Nothing else is a design finding. Prose style, status and parity bookkeeping, code layout, and file limits are
mechanical checks. Route them to the pre-audit gate, not to another review round.

Stop rule:

- **Full:** stop when no Blocker or Major remains. Minor and Note findings are accepted or deferred; they do not
  justify another round.
- **Light:** one pass. If the review agent finds a Blocker or Major, the step was misclassified: reclassify it as
  Full and review it at that depth.
- **None:** no review agent pass. The one-line record and the local gates are the check.
- A third Full round needs an owner decision and a recorded reason. Two rounds should resolve the design; a third
  is a signal that the design question is still open, not that the review agent needs to look harder.

`Ready with notes` and `Passed with notes` remain available when the owner explicitly accepts the remaining Minor
or Note findings, but they are not required merely because Minor or Note findings exist. Under the stop rule, a
Full or Light review that has no Blocker or Major is complete; Minor and Note findings are recorded, accepted, or
deferred without a new round.

## 3. Pre-audit gate

Most audit findings are bookkeeping, not judgment. Before the audit request, the request owner (the dev) runs the
mechanical checks:

- Every ticked acceptance criterion names its evidence: a test, command, fixture, or CI run.
- The step page, status dashboard, feature log, and closeout evidence agree.
- File and folder limits, the link check, the secret scan, and the licence check pass.
- The design class and any waiver are recorded.
- The closeout evidence file is filled and passes `python tools/check_closeout_evidence.py`.

Record the result in the step page. An audit request without it is incomplete. This is where bookkeeping belongs;
the review agent's tokens go to judgment.

## 4. Metrics

Record the origin of each audit finding as **design**, **implementation**, **evidence**, or **documentation**. At
each phase boundary, compare the distribution with the design effort spent.

- If design-origin findings are rare, lower the design class of similar work.
- If implementation-origin findings dominate, invest in tests and gates, not in more design.
- If evidence- or documentation-origin findings dominate, fix the pre-audit gate, not the design review.
- The key measures are zero Blocker/Major at audit and zero escaped defects after merge. Minor and Note findings
  are expected and are not a design failure.

## 5. Anti-patterns

- a full design for a mechanical change;
- another review round for Minor or Note findings only;
- a `Light` class hiding a contract change;
- a design review checking prose, status, or layout instead of the five questions;
- using audit findings as a reason to add design rounds without checking their origin;
- recording the design class after the work, so it always matches what happened;
- treating the `None` pre-authorization as permission to skip the local gates or the one-line record.

## 6. Relation to other rules

- Model tiers: [`model-economics.md`](model-economics.md).
- Step sizing and split triggers: [`docs/planning/04-roadmap.md` §3.1](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/04-roadmap.md#31-step-sizing-and-split-triggers).
- Design-review records and statuses: [`docs/planning/reviews/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/reviews/README.md).
- Audit records and statuses: [`docs/planning/audits/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/audits/README.md).
- Adoption profile: [`AGENTS.md` §3.15](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md) and
  [`docs/planning/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/README.md).
