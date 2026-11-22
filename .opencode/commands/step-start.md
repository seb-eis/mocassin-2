---
description: Start a step through the lead
agent: lead
---

Read `AGENTS.md` §3, `docs/development/orchestration.md`, and `docs/planning/README.md`.

Start step `$1`. The argument is `<NN-slug>`; use the zero-padded `NN` for record names and the full `NN-slug` for
the step branch and tag.

1. confirm the step is the lowest unblocked step and that its dependencies are done;
2. spawn `design` to complete or update the step page (including the design class) and create
   `docs/planning/reviews/step-NN.md` from the template (request fields, scope, checklist, status `Ready for
   review`, reviewed commit), committed with `[skip ci]`;
3. when the design is frozen, spawn `review` with the spawn card in `docs/development/agent-workflow.md`;
4. present the design-review verdict and stop for approval of the design and the complete feature-branch list.
   The cumulative approval report is for a merge, a tag, or a Blocker/Major waiver, not for this gate.

Do not design or implement anything yourself, do not edit any file, and do not create the step branch
(`step/NN-slug`) before I approve.
