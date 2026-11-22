# Step NN Design Review

> Review agent: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-NN.md`. You may edit only your own metadata (`Review agent`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | NN — Step title |
| Request owner | Dev |
| Lead | — |
| Dev | — |
| Review initiated by | Lead, on request of the design agent or dev |
| Design class | Full / Light |
| Reviewed commit | — |
| Docs branch | — |
| Review date | — |
| Review agent | — |

Step page: [`Step NN — Step title`](../steps/step-NN-short-title.md)

> Coordination: the lead spawns a fresh review agent after the dev has frozen the design and completed the
> review request, and relays the verdict and owner decisions. The design agent and dev update the design docs,
> apply fixes on the design docs or a maintenance branch, add resolutions without altering review text, and ask
> the lead for re-review until the review agent no longer identifies issues. The lead does not edit the design or
> the review record. The human owner approves the final verdict and the start of implementation.
>
> Design depth: a `Light` review answers the five design questions only and is one pass. A `Full` review uses the
> full checklist and stops when no Blocker or Major remains. Minor and Note findings are accepted or deferred and
> do not trigger another round. A third Full round needs an owner decision.

## Step summary

| Field | Value |
| --- | --- |
| Phase | — |
| Depends on | — |
| Delivers | — |

## Design decisions reviewed

- [ ] Step page design decisions
- [ ] Data contracts and format changes
- [ ] Architecture and module boundaries
- [ ] ADRs and open decisions
- [ ] Quality strategy and test plan
- [ ] Parity matrix and documentation plan
- [ ] Licensing and provenance
- [ ] Post-implementation audit plan

## Dependency and interface checks

- [ ] Dependencies are `Done` and their contracts are stable
- [ ] Inputs, outputs, types, and formats are defined
- [ ] Versioning and migration behavior is defined
- [ ] No unresolved cross-step interface question remains

## Standard design-review checklist

Use this full checklist for a `Full` step. A `Light` step answers the five design questions only.

- [ ] Design class (`Full` or `Light`) is recorded and matches the triggers
- [ ] Scope, non-goals, and acceptance criteria are explicit and testable
- [ ] Complexity class and split decision are recorded and justified per
      [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers)
- [ ] Design decisions are complete and internally consistent
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined
- [ ] Documentation, status, parity, and audit updates are planned
- [ ] Licensing, provenance, and dependency checks are resolved
- [ ] Risks and mitigations are realistic
- [ ] Code layout and namespace plan follow the structure policy
- [ ] No hidden scope, stale placeholders, or unowned decisions

## Evidence

Files reviewed:

Commands run:

## Findings

| Severity | Origin | Finding | Evidence |
| --- | --- | --- | --- |
| — | — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. Origin: **design**, **implementation**,
**evidence**, or **documentation**. The review agent writes the findings and verdict directly. The dev adds
resolutions in the separate `Resolution status` section without altering the review agent's findings, evidence,
or verdict.

## Record state

**State:** Not requested / Ready for review / In review / Changes required / Re-review required / Ready with notes / Ready for implementation / Waived

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> design agent, dev, or lead.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Waived

Follow-up owner:
Follow-up step:

## Approval summary

The request owner (dev or design agent) completes this section before the lead presents the approval
request. It must cover the full history, not only the latest round.

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

- [ ] Accepted: Ready for implementation
- [ ] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by:
Date:

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The dev fills this section after the verdict. Do not alter the review agent-authored findings, evidence, or
verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

#### Commands and results

#### Findings

#### Verdict
