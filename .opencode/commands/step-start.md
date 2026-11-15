---
description: Start a step through the orchestrator
agent: orchestrator
---

Read `AGENTS.md` §3, `docs/development/orchestration.md`, and `docs/planning/README.md`.

Start step `$1`. The argument is `<NN-slug>`; use the zero-padded `NN` for record names and the full `NN-slug` for
the step branch and tag.

1. confirm the step is the lowest unblocked step and that its dependencies are done;
2. spawn the `planner` to complete or update the step page and create `docs/planning/reviews/step-NN.md` from the
   template (request fields, scope, checklist, status `Ready for review`, reviewed commit), committed with
   `[skip ci]`;
3. when the design is frozen, spawn the `reviewer` with the spawn card in `docs/development/agent-workflow.md`;
4. present the design-review verdict and stop for approval of the design and the complete feature-branch list.
   The cumulative approval report is for a merge, a tag, or a Blocker/Major waiver, not for this gate.

Do not design or implement anything yourself, and do not create the step branch (`step/NN-slug`) before I approve.
