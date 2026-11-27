---
name: Grilling
description: Turn an ambiguous request into settled decisions by interviewing the owner in frontier rounds, each question with a recommended answer. Use before a design is frozen whenever scope, behaviour, a contract, or priorities are unclear.
---

# Grilling

Read [`docs/development/discovery.md`](../../../docs/development/discovery.md); its section 4 holds the one home for
the round format.

Run the current frontier as one round for the active step: number each question, state the options and consequence,
and give a recommended answer so "yes" accepts it. Wait for the owner's answers before asking the next round.

Prefer the question tool: ask all current frontier questions in one call, put the recommended option first and mark
it "(Recommended)", and allow a typed answer. Fall back to numbered text only when the tool is unavailable or the
question needs a long free-form explanation.

Find facts yourself. Ask the owner only for decisions or facts that only they hold. One round of roughly five to
eight questions is usually enough; stop when the frontier is empty or the remaining uncertainty is recorded as an
open decision.

Record settled scope and acceptance evidence in the step page, resolved terms in `GLOSSARY.md`, and irreversible or
surprising choices as an ADR. The lead may run the interview and recommend an option; it must not write the design,
decide for the owner, or approve it.
