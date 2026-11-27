# Retrospectives

This page is the task discipline for improving the **agent environment** after a step or a painful session. A retro
is not a blame review and not a replacement for the step audit. It looks for changes that make future runs cheaper,
clearer, and less error-prone.

## 1. When to run one

- after a step closes, especially if it took more than one attempt;
- after a session where the agent was slow, lost context, or repeated a mistake;
- at a phase boundary, alongside the cross-cutting review;
- whenever the owner says "that should have been easier".

A small project does not need a retro after every typo. Record why one was skipped if the owner expected it.

## 2. Inputs

Read the durable evidence, not a reconstructed chat summary:

- the step page, review and audit records, and closeout evidence;
- the commits and diffs;
- the local gate and CI results;
- the handoff notes when available.

## 3. What to look for

| Area | Question |
| --- | --- |
| Navigation | Did the agent take too long to find the right files or hidden dependencies? Is a pointer missing? |
| Automated checks | Could a lint, type, test, structure, or hook check have caught the mistake? Is an existing check unwired or broken? |
| Standards | Did the review agent miss a rule? Is the rule mechanical enough to become a check, or is it a genuine judgement call? |
| Steering files | Is `AGENTS.md` or a process page too large, stale, duplicated, or full of no-ops? |
| Tool economy | Did the agent make expensive or redundant calls that a command or pointer could replace? |
| Information access | Was a crucial log, service, or artifact unavailable? Can it be exposed read-only? |
| Prototypes and research | Did a throwaway probe or research note answer a question? Should it be recorded or deleted? |

Prefer a deterministic check over a longer prose rule. Reserve prose for judgement calls that no check can make.

## 4. Output

Create `docs/planning/retrospectives/<YYYY-MM-DD>-<slug>.md` from the
[template](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/retrospectives/_template.md).

Each candidate improvement must name:

- the observed problem, with evidence;
- the change proposed;
- the owner and the step page or process page that will carry it;
- the expected effect;
- whether it is a check, a pointer, a process change, or an accepted no-op;
- the next action.

Do not make a retro a wish list. A finding without an owner and a next action is an observation.

## 5. Who does what

- The lead runs the retro with the owner and proposes candidates.
- The dev or design agent writes the record and the resulting process change.
- A process change still follows the normal `docs/<slug>` branch, the process rules in
  [`AGENTS.md` §3.15](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md), and owner approval.
- The review agent does not fix retro findings.
- Link the retro from the step page or the status dashboard.

## 6. Anti-patterns

- blaming a person or a model instead of changing the environment;
- adding another rule when a check would be cheaper;
- writing a long retro that no one reads;
- recording a finding without an owner or next action;
- changing `AGENTS.md` without removing the stale line it replaces;
- treating a retro as a substitute for the step audit.
