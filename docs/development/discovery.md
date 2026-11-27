# Discovery and grilling

This page is the task discipline for turning an ambiguous request into decisions the owner can approve. It is a
lightweight adaptation of a structured interview, sized for a solo developer or small team.

Discovery is not design and not implementation. Its output is a set of settled decisions, recorded in the step page
or open decisions, plus the questions that remain open.

## 1. When to run it

Run a discovery round before the design is frozen when any of these is true:

- the request has more than one reasonable interpretation;
- a public contract, format, dependency, or user-visible behaviour is affected;
- two or more plausible designs exist;
- the owner has not stated the non-goals or the acceptance evidence;
- the agent would need to guess at an unstated priority or constraint.

For a small, unambiguous change, skip the round and record why in the step page. Do not turn discovery into a gate
for typos or mechanical work; those are `None`-class changes under
[`design-proportionality.md`](design-proportionality.md).

The lead runs the round with the owner. The design agent records the resolved decisions in the step page, the
glossary, an ADR, or the open-decisions page. A role agent that needs a fact from the repository finds it itself;
it does not ask the owner for something the repository or tools can answer.

## 2. Work the decision tree

Treat the request as a tree of decisions. A decision belongs to the **frontier** when every decision it depends on
is already settled.

For each round:

1. ask the whole current frontier together;
2. number each question and give your recommended answer;
3. word each question so a simple "yes" accepts the recommendation;
4. wait for the owner's answers before asking the next round;
5. recompute the frontier from those answers.

Prefer the question tool when it is available: ask every current frontier question in one call, put the recommended
option first, mark it "(Recommended)", and let the owner type a different answer. Use numbered text questions only
when the tool is unavailable or the question needs a long free-form explanation.

Questions whose answers depend on another open question belong in a later round. Do not ask the owner to resolve a
fact the agent can look up.

## 3. Budget

- One round of roughly five to eight questions is usually enough.
- Run a second round only when the first answers genuinely unblock new decisions.
- Stop when the frontier is empty, or when the remaining uncertainty is recorded as an open decision with an owner.
- Do not use a long interview to avoid making a design decision.

The goal is shared understanding, not an exhaustive questionnaire.

## 4. Round format

This is the one home for the interview format. Use it as the basis for a `/grill` round; the command reads this
section and does not restate it.

```text
Step: NN — Title
Current understanding:
- ...

Already settled:
- ...

Round N — frontier questions:

Q1 — <question title>
<question body. State the options and the consequence of each.>
Recommended: <answer, so that "yes" accepts it>

---

Q2 — <question title>
<question body.>
Recommended: <answer>

...

Facts I checked myself:
- <file, command, or source> — <result>

Facts still needed:
- <only facts the owner uniquely holds, if any>
```

## 5. Recording the outcome

Record durable results, not a transcript:

- settled scope, non-goals, and acceptance evidence in the step page;
- resolved working terms in [`GLOSSARY.md`](https://github.com/seb-eis/mocassin-2/blob/main/GLOSSARY.md), following
  [`domain-language.md`](domain-language.md);
- irreversible or surprising decisions as an ADR;
- unresolved questions in
  [`docs/planning/05-open-decisions.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/05-open-decisions.md).

The design agent owns the design records. The lead may conduct the interview and recommend options, but it does not
write the design, decide for the owner, or approve it. Recommending is not deciding.

## 6. Anti-patterns

- asking twenty questions in one round, including ones that depend on unanswered questions;
- asking the owner for facts the agent can find in the repository;
- asking a vague question with no recommended answer;
- treating "the owner said yes" as a substitute for a testable acceptance criterion;
- recording every answer in chat instead of the durable records;
- running discovery after the design is frozen to justify a decision already made;
- the lead choosing an option on the owner's behalf.
