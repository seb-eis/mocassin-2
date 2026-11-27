---
description: Grill an ambiguous step request with the owner before design
agent: lead
---

Read `AGENTS.md` §3, `docs/development/discovery.md`, and `docs/development/orchestration.md`.

Run one discovery round for step `$1`. The argument is `<NN-slug>`; if it is empty, use the active step or the
request in the current conversation. Ask only the current frontier, number the questions, give a recommended answer
for each, and wait for my answers before the next round.

Prefer the question tool for the round. Put the recommended option first and mark it "(Recommended)"; allow me to
type a different answer. Use numbered text only if the tool is unavailable or the question needs a long free-form
explanation.

Keep it to roughly five to eight questions unless the answers genuinely unblock more. Find facts yourself; ask me
only for decisions or facts I uniquely hold. Do not design, implement, or write the design records yourself.

After the round, hand the settled decisions to the design agent or dev to record in the step page, `GLOSSARY.md`, an
ADR, or `docs/planning/05-open-decisions.md`. Recommending is not deciding: do not choose for me or change scope.
