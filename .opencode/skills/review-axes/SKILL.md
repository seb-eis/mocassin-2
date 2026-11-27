---
name: Review Axes
description: Review a step audit along two separate axes, Standards and Spec, and label findings without merging them. Use when auditing a frozen implementation, reviewing a branch or work-in-progress diff, or judging a design against its spec.
---

# Review Axes

Read [`docs/development/review-axes.md`](../../../docs/development/review-axes.md) and the gate rules in
`docs/planning/reviews/README.md`, `docs/planning/audits/README.md`, and
`docs/planning/reviews/cross-cutting/README.md`.

Pin the base and reviewed commits and use a three-dot diff. For a **step audit**, review the **Spec** axis against
the frozen step page, feature request, and acceptance criteria, then the **Standards** axis against the repository's
documented standards and, only where nothing is documented, the smell baseline.

For a **design review**, review the Spec axis plus the five design questions in
`docs/development/design-proportionality.md`. Do not spend a design round on code layout, prose, or bookkeeping.

For a **cross-cutting review**, follow its repository-wide checklist and use the axes only where they fit.

Label every finding with its axis; do not merge, rerank, or choose one overall winner. Keep one gate verdict
reflecting the worst outcome across the axes. Use one review context and two passes; do not spawn parallel review
sub-agents by default.

If there is no spec, report it as a finding and continue. Do not invent one. Do not fix findings; write them in the
assigned record.
