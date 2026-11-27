---
description: Run a retrospective on a step or session
agent: lead
---

Read `AGENTS.md` §3, `docs/development/retrospectives.md`, and
`docs/planning/retrospectives/_template.md`.

Run a retrospective for step `$1`. The argument is `<NN-slug>`; if it is empty, use the most recently closed step or
the current session. Read the durable evidence: the step page, review and audit records, closeout evidence, commits,
gate and CI results, and any handoff notes.

Present candidate improvements in order of severity, grouped by navigation, automated checks, standards, steering
files, tool economy, information access, and prototypes or research. Prefer a deterministic check over a longer
prose rule. Every candidate needs an owner and a next action; mark accepted no-ops explicitly.

Do not edit the retro record yourself. Give the content to the dev or design agent to commit from the template, then
route any process change through the normal `docs/<slug>` branch, `AGENTS.md` §3.15, and owner approval.
