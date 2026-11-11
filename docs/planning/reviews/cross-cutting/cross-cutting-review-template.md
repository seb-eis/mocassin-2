# Cross-Cutting Review — <Title>

> Reviewer: write evidence, findings, and verdict directly into this record. You may edit only your own
> metadata and record state, checklist completion marks and inline verification notes (not item text), `Evidence`, `Findings`,
> and `Verdict`, and commit only this file with a documentation-only message and `[skip ci]`. Do not edit the
> scope, checklist item text, resolutions, code, tests, configuration, step pages, status dashboard, or any
> other file.

## Request

| Field | Value |
| --- | --- |
| Review ID | XC-NN |
| Checkpoint | Phase —<br>Steps — |
| Type | Repository-wide review |
| Target commit | — |
| Request owner | Step agent / planner |
| Orchestrator (implementor) | — |
| Review initiated by | Human owner |
| Reviewer | — |
| Review date | — |

> Orchestration: an implementor agent (the orchestrator) freezes the checkpoint, requests the reviewer, fixes
> findings on a maintenance branch, adds resolutions without altering reviewer text, and requests re-review
> until the reviewer no longer identifies issues. The human owner approves the final verdict and the phase gate.

## Purpose

What decision or checkpoint does this review support?

## Non-goals

- —

## Scope

- —

## Evidence

Files reviewed:

Commands run:

## Checklist

### A. Foundation

- [ ] Proposed check item.
- [ ] Proposed check item.

### B. Records and consistency

- [ ] Proposed check item.
- [ ] Proposed check item.

### C. Original design fidelity

Required for the **Architecture and design status** review; use it for any review that touches design.

- [ ] **Corrections:** the original issues this phase intended to correct are corrected, and the evidence
      shows the corrected behaviour. List each intended correction and its result.
- [ ] **Ported strengths:** the good/clever original design aspects this phase intended to keep are present
      and verified (algorithms, data layouts, caches, incremental updates, validation, performance shortcuts,
      workflow). State what was kept, adapted, or dropped.
- [ ] **Regressions:** no unintended regression over the original remains in capability, semantics,
      performance, memory, diagnostics, determinism, or maintainability. Any deliberate regression is recorded
      with rationale, owner, and follow-up.
- [ ] Each answer above is explicit and supported by code, tests, benchmarks, or a side-by-side design
      comparison, not only prose.

### D. Exit criteria

- [ ] No Blocker or Major finding remains, or every exception is explicitly waived by the human owner.
- [ ] Remediation owners and follow-ups are recorded.
- [ ] Human owner approves the verdict and the phase gate.

## Findings

| ID | Severity | Area | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**.

## Record state

**State:** Planned / Ready for review / In review / Fixes required / Re-review required / Passed / Passed with notes / Waived

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> request owner or the other reviewers.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Waived

Follow-up owner:
Follow-up step:

## Remediation status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

## Human approval

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:
