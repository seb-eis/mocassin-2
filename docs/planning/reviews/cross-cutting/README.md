# Cross-Cutting Phase Reviews

This directory records the cross-cutting quality gate that runs after the steps of each phase are complete.
It is a repository-wide review, not a per-step design review or per-step audit.

Each phase checkpoint has three independent reviews:

| Review | Focus | Order |
| --- | --- | --- |
| **Policies and planning** | Agent instructions, workflows, CI, code structure, process records, phase status | Starts first |
| **Architecture and design status** | Architecture, contracts, design status, cross-step interfaces | After the policies review |
| **Documentation status** | Coverage, accuracy, navigation, API docs, publication boundary | After the policies review, in parallel with architecture and design |

The next phase's first step design review may not start until all three reviews are `Passed` or the human
owner accepts `Passed with notes`, or the owner explicitly waives a review with a recorded rationale.

## 1. Phase checkpoints and numbering

Review IDs are globally sequential and are never reused. Each phase gets three new IDs.

| Phase | Steps | Reviews | Status |
| --- | --- | --- | --- |
| A. Foundations | 00–02 | Waived — process introduced after completion | — |
| B. Model definition | 03–07 | `XC-01` policies and planning, `XC-02` architecture and design, `XC-03` documentation | Planned — awaiting Step 07 completion |
| C. Compilation and build | 08–12 | `XC-04` policies and planning, `XC-05` architecture and design, `XC-06` documentation | Planned |
| D. Solver | 13–19 | `XC-07` policies and planning, `XC-08` architecture and design, `XC-09` documentation | Planned |
| E. Results | 20–22 | `XC-10` policies and planning, `XC-11` architecture and design, `XC-12` documentation | Planned |
| F. GUI | 23–28 | `XC-13` policies and planning, `XC-14` architecture and design, `XC-15` documentation | Planned |
| G. Release and documentation | 29–31 | `XC-16` policies and planning, `XC-17` architecture and design, `XC-18` documentation | Planned |

The first checkpoint records:

| ID | Record | Status |
| --- | --- | --- |
| `XC-01` | [`xc-01-policies-and-planning.md`](xc-01-policies-and-planning.md) | Planned — awaiting Step 07 completion |
| `XC-02` | [`xc-02-architecture-and-design.md`](xc-02-architecture-and-design.md) | Planned — awaiting Step 07 completion |
| `XC-03` | [`xc-03-documentation-status.md`](xc-03-documentation-status.md) | Planned — awaiting Step 07 completion |

`XC-01` starts first. Once it is underway, `XC-02` and `XC-03` may run in parallel.

## 2. Roles

| Role | Responsibility |
| --- | --- |
| Implementor orchestrator | Freezes the request, spawns the reviewer, fixes findings, adds resolutions, requests re-review |
| Reviewer | Fresh independent context; writes evidence, findings, and verdict only |
| Human owner | Approves the checklists, final verdicts, and phase gate |

The orchestrator must be an implementor/step agent, not the reviewer. It may be the same agent for all three
reviews in a checkpoint, but the three reviewers must be separate fresh contexts.

## 3. Orchestration

Each review uses the same round-based process as design reviews and implementation:

1. The orchestrator prepares the request, scope, checklist, and target commit.
2. A fresh reviewer is spawned. The spawner provides the frozen commit, record, repository links, and required
   commands/scope. It must not provide a desired verdict or a summary that substitutes for reading the
   repository.
3. The reviewer writes evidence, findings, verdict, and checklist marks into its record and commits only that
   file with `[skip ci]`.
4. If the reviewer reports findings, the orchestrator fixes the repository state on a maintenance branch,
   updates the appropriate design/process/docs artifact, adds resolutions to the record without altering
   reviewer text, sets the status to `Re-review required`, and records the new commit.
5. A fresh reviewer re-reviews the new commit. The same reviewer may continue only if it stayed read-only and
   did not participate in the remediation; otherwise use a fresh context. Append a new round.
6. Repeat until the reviewer no longer identifies Blocker/Major issues. The human owner approves the final
   verdict and the phase gate.

## 4. Reviewer rules

- The reviewer is a fresh context with no shared conversation with the orchestrator or implementors.
- The reviewer reads the repository at the frozen commit. A summary from the orchestrator is not evidence.
- The reviewer may check checklist boxes and add short inline verification notes. It may not change item text.
- The reviewer writes only its assigned record and commits only that file with `[skip ci]`.
- The reviewer must not fix findings, merge, tag, push `main`, or delete branches.
- The reviewer must not be asked for a particular verdict and is not re-spawned until a preferred verdict
  appears.
- The reviewer records exact commands, results, and file references behind the verdict.

## 5. Status values

| Status | Meaning |
| --- | --- |
| **Planned** | Scope approved; waiting for the phase's last step to complete and the checkpoint to freeze. |
| **Ready for review** | Scope frozen at a commit; reviewer may start. |
| **In review** | A fresh reviewer is working. |
| **Fixes required** | Blocker/Major findings remain. |
| **Re-review required** | Remediation applied; fresh review needed. |
| **Passed** | No Blocker/Major remains. |
| **Passed with notes** | Only Minor/Note findings remain; owner accepts them. |
| **Waived** | Owner explicitly accepts that the review is not required, with a recorded rationale. |

## 6. Phase gate

- Freeze the checkpoint after the last step of the phase is merged and tagged.
- Run the policies and planning review first; then run the architecture and design, and documentation reviews in parallel.
- The next phase's first step design review may not start until all three verdicts are accepted by the human
  owner.
- A review may be waived only by the human owner, with the reason recorded in its record.
- The gate does not replace step design reviews, step audits, or Step 29 parity validation.
- Do not create a step branch, step tag, or release tag for a cross-cutting review.
- Remediate findings through maintenance branches, ADRs, amendments, or step-page updates. Never move an
  existing tag.
- A record commit that touches any other file is invalid; re-run the review with a fresh reviewer.

## 7. Record template

Use [`cross-cutting-review-template.md`](cross-cutting-review-template.md) for a new review.
