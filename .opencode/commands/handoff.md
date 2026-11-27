---
description: Build a durable handoff for the next context
agent: lead
---

Read `AGENTS.md` §3, `docs/development/agent-workflow.md`, and `docs/development/orchestration.md`.

Build a handoff for step `$1`. The argument is `<NN-slug>`; if it is empty, use the active step and the next action
in the conversation. Read the durable records first; do not reconstruct state from chat alone.

Use the context handoff card in `docs/development/agent-workflow.md` §2 and add one line naming the applicable task
discipline or skill from `docs/development/index.md`. Include the exact branch, commits, gate and CI results, open
blockers, next action, and the records the next context must read. Reference existing records by path instead of
duplicating them. Redact secrets and personal data.

Do not write the card yourself. Give it to the dev or design agent to record in the step page or a durable status
note. If the next action is a merge, tag, or push to `main`, state that it needs owner approval.
