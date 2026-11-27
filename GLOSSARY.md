# Glossary

This is the working **ubiquitous language** for MOCASSIN contributors and agents: short, opinionated definitions of
process and working terms. It is not a specification, design note, or scratch pad. Read it before discussing
behaviour or naming things, and update it when a term is resolved rather than in a batch at the end of the work.

The published scientific glossary, notation, and units reference is [`docs/glossary/`](docs/glossary/index.md). No
term is defined in both: scientific, notation, and unit terms belong there; process and working-language terms
belong here.

## Process terms

| Term | Definition | _Avoid_ |
| --- | --- | --- |
| **Step** | MOCASSIN's unit of planned work, numbered `NN`, with a page in `docs/planning/steps/`, a `step/<NN>-<slug>` branch, and a `step-NN` tag. | work package, WP, phase task |
| **Feature** | One bounded deliverable inside a step, implemented on a `feature/step-<NN>-<slug>/<feature>` branch and squash-merged into the step branch. | sub-task, ticket |
| **Design class** | The depth of design and review a step, amendment, or policy-update review needs: `Full`, `Light`, or `None`. | review size, design level |
| **Review axis** | The label that says which question a finding answers: **Spec** (does it implement the frozen request?) or **Standards** (does it follow the repository's documented standards?). | category, dimension |
| **Record state** | A process position in a review, audit, or consultation record, such as `Ready for review` or `Re-review required`. It is not a verdict. | status, verdict |
| **Senior consultation** | One advisory answer to one question, requested after two completed standard rounds of the same gate still leave a Blocker or Major. It is not a round and not a verdict. | senior review, escalation review, third round |
| **Discipline** | A short task-level practice page under `docs/development/` with a matching skill trigger, such as TDD, bug diagnosis, or review axes. | methodology, guideline |
| **Prototype** | Throwaway code built to answer one design question, recorded under `docs/planning/prototypes/` and then deleted or parked. | spike, POC, experiment branch |
| **Research note** | A cited answer to one bounded question from primary sources, recorded under `docs/planning/research/`. | investigation, reading log |
| **Retrospective** | A record of how the agent environment should change after a step or session, under `docs/planning/retrospectives/`. | postmortem, lessons learned |
| **Handoff** | The durable card that gives the next context the branch, commits, gate results, blockers, and next action without the previous chat history. | summary, context dump |

## Language rules

- **Be opinionated.** Pick the best term and list rejected synonyms under _Avoid_.
- Define **what the term is** in one or two sentences, not how it is implemented.
- Add only project-specific terms; general programming words such as timeout, error, or utility do not belong.
- Update the glossary when the term is resolved. The design agent owns terms settled in design; the dev updates a
  term that implementation reveals to be wrong.
- Never turn this file into a specification. A rule lives in its normative page; a definition lives here.
