# Domain language, glossary, and ADRs

This page is the task discipline for the project's vocabulary and for when a decision deserves an ADR. It keeps
names stable, shortens explanations, and stops future agents from re-litigating settled decisions.

Use it when naming a concept, resolving overloaded or conflicting vocabulary, or deciding whether a choice is
irreversible and surprising enough to need an ADR.

The glossary is a language reference, not a design document. The design lives in the step page, contracts, and
ADRs.

## 1. Two glossaries, one language

MOCASSIN keeps two files with different jobs:

| File | Audience | Holds |
| --- | --- | --- |
| [`GLOSSARY.md`](https://github.com/seb-eis/mocassin-2/blob/main/GLOSSARY.md) (repository root) | Contributors and agents | The working ubiquitous language: process and project terms in one or two sentences, with rejected synonyms under _Avoid_. |
| [`docs/glossary/`](https://github.com/seb-eis/mocassin-2/blob/main/docs/glossary/index.md) (published) | Users and readers of the site | The scientific glossary, notation, and units reference. |

No term is defined in both. A process term belongs in the root glossary; a scientific term, symbol, or unit belongs
in the published glossary. If a term has both a process and a scientific meaning, define the scientific meaning in
`docs/glossary/` and link to it from the root entry.

Rules for the root glossary:

- **Be opinionated.** Pick the best term and list rejected synonyms under `_Avoid_`.
- **Define what it is**, in one or two sentences. Do not describe implementation or behaviour.
- **Bold other glossary terms** when a definition refers to them.
- **Only project-specific terms.** General programming concepts such as timeouts, errors, or utilities do not
  belong.
- **Group by natural subheading** when the list grows; a flat list is fine for a small project.
- **Update it when the term is resolved**, not in a batch at the end.

## 2. Keeping language honest

While designing or reviewing:

- challenge a term that conflicts with the glossary;
- sharpen a vague or overloaded term and propose the canonical one;
- use the question tool when two terms are genuinely plausible and the owner should choose; otherwise state the
  assumption and proceed;
- test a definition with a concrete edge-case scenario;
- check the code against the definition and surface any contradiction.

The design agent updates the glossary during discovery and design. The dev updates it when implementation reveals a
better name. The review agent checks that the diff, tests, and docs use the glossary terms consistently.

## 3. When to write an ADR

ADRs already live in
[`docs/planning/adr/`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/adr/README.md), with the
template beside them. Offer an ADR only when all three are true:

1. **Hard to reverse** — changing the decision later has a meaningful cost.
2. **Surprising without context** — a future reader will wonder why it was done this way.
3. **A real trade-off** — there were genuine alternatives and a reason for the choice.

If any condition is missing, use a step-page note, an open decision, or a code comment instead. MOCASSIN's
non-negotiable decisions and format changes already require an ADR; that rule stays in
[`AGENTS.md` §4](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md) and `docs/planning/adr/`.

## 4. Short ADRs

An ADR may be one short paragraph. Record the context, decision, and why. The full template is available for
decisions that need compatibility, security, migration, or alternatives, but do not fill sections that add nothing.

Accepted ADRs are immutable. Supersede one with a new ADR and link both. Do not rewrite history to match a later
decision.

## 5. Durable output

- a resolved working term: `GLOSSARY.md`;
- a resolved scientific term or unit: `docs/glossary/`;
- an irreversible or surprising decision: an ADR;
- a still-open choice: [`docs/planning/05-open-decisions.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/05-open-decisions.md);
- scope, non-goals, and acceptance evidence: the step page.

## 6. Anti-patterns

- turning `GLOSSARY.md` into a specification or implementation notebook;
- defining the same term in both glossaries;
- adding general programming terms that every project shares;
- listing several synonyms as accepted terms instead of choosing one;
- writing an ADR for every small choice, or hiding a hard-to-reverse decision in chat;
- editing an accepted ADR instead of superseding it;
- letting code, tests, and docs use different names for the same concept.
