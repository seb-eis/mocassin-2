---
name: Domain Modeling
description: Sharpen project terminology, update the root GLOSSARY.md, and decide when a decision deserves an ADR. Use when naming a concept, resolving overloaded vocabulary, or recording an irreversible or surprising decision.
---

# Domain Modeling

Read [`docs/development/domain-language.md`](../../../docs/development/domain-language.md),
[`GLOSSARY.md`](../../../GLOSSARY.md), and the published glossary at `docs/glossary/index.md`.

Challenge terms that conflict with the glossary. Propose one canonical term for a vague or overloaded concept, and
test it with a concrete edge-case scenario. Use the question tool when two terms are genuinely plausible and the
owner should choose; put the recommended term first. Update `GLOSSARY.md` when the term is resolved, not later.

Keep the root glossary to project-specific process and working terms, one or two sentences each, with rejected
synonyms under `_Avoid_`. Scientific terms, notation, and units belong in `docs/glossary/`; no term is defined in
both. Do not turn either glossary into a spec or implementation notebook.

Offer an ADR only when the decision is hard to reverse, surprising without context, and the result of a real
trade-off. Use the ADR template in `docs/planning/adr/`. Never edit an accepted ADR; supersede it.
