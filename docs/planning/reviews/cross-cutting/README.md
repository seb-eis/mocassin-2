# Cross-Cutting Reviews

This directory records the planned post-Step-07 checkpoint reviews. They are **repository-wide** reviews, not
per-step design reviews or per-step audits.

The checkpoint has three independent reviews:

| ID | Review | Focus |
| --- | --- | --- |
| `XC-01` | [Repository policy and planning status audit](repository-policy-audit.md) | Agent instructions, workflows, CI, code structure, process records, status |
| `XC-02` | [Design foundations and model definition review](design-foundations-review.md) | Architecture, contracts, Steps 00–07 design, cross-step interfaces |
| `XC-03` | [Documentation status review](documentation-status-review.md) | Coverage, accuracy, navigation, API docs, published/internal boundary |

## Status

The records are currently **Draft for owner review**. The checklists and scopes are proposals for the human
owner to review before any reviewer is spawned.

After owner approval, each record is frozen at a commit and a fresh independent reviewer is spawned.

## Process

### Roles

| Role | Responsibility |
| --- | --- |
| Request owner | Planner/step agent; writes the review scope and checklists |
| Reviewer | Fresh independent context; writes evidence, findings, and verdict only |
| Human owner | Approves the checklists, final verdicts, and remediation plan |

### Reviewer rules

- Each review gets its own fresh reviewer. The three reviewers must not share context with each other or with
  the implementors of the reviewed work.
- The reviewer reads the repository at the frozen commit. A summary from the request owner is not evidence.
- The reviewer may check checklist boxes and add short inline verification notes. It may not change item text.
- The reviewer writes only its assigned record, and commits only that file with `[skip ci]`.
- The reviewer must not fix findings, merge, tag, push `main`, or delete branches.
- The reviewer records exact commands, results, and file references behind the verdict.

### Order

1. Freeze the repository after the Step 07 merge and `step-07` tag, plus a docs-only checkpoint commit.
2. Run `XC-01` first. It catches stale instructions, workflows, and status records that could mislead the other
   reviews.
3. Run `XC-02` and `XC-03` in parallel, or sequentially if the owner prefers.
4. The human owner reviews the findings and approves the remediation plan.
5. Remediate Blocker/Major findings on maintenance branches. Re-run the affected review with a fresh context.
6. Close the checkpoint when all three reviews are `Passed` or the owner accepts `Passed with notes`.

### Status values

| Status | Meaning |
| --- | --- |
| **Draft for owner review** | Scope/checklists proposed; no reviewer yet. |
| **Ready for review** | Scope frozen at a commit; reviewer may start. |
| **In review** | A fresh reviewer is working. |
| **Fixes required** | Blocker/Major findings remain. |
| **Re-review required** | Remediation applied; fresh review needed. |
| **Passed** | No Blocker/Major remains. |
| **Passed with notes** | Only Minor/Note findings remain; owner accepts them. |
| **Waived** | Owner explicitly accepts that the review is not required. |

### Non-goals and guardrails

- These reviews do not create a step branch, step tag, or release tag.
- They do not replace the Step 08 design review, step audits, or Step 29 parity validation.
- They do not authorize implementation of future steps.
- Cross-cutting findings are remediated through maintenance branches, ADRs, amendments, or step-page updates as
  appropriate. Existing tags are never moved.
- A review record commit that touches any other file is invalid; re-run the review with a fresh reviewer.

## Record template

Use [`cross-cutting-review-template.md`](cross-cutting-review-template.md) for a new review.
