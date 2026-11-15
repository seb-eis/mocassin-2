# Orchestration and Role Delegation

This page is the normative home for the **orchestrator** role: the single user-facing coordinator for all
agent-assisted work on MOCASSIN. The branch, gate, audit, and tag rules are in
[`AGENTS.md` §3](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md); the practical playbook is in
[`agent-workflow.md`](agent-workflow.md). Reviewer independence and one-file records are unchanged by this page.

## 1. One orchestrator

Every engagement is coordinated by exactly one **orchestrator agent**. The human owner talks only to the
orchestrator. The owner does not spawn a planner, step agent, reviewer, or auditor directly; they ask the
orchestrator, which spawns the right role.

The orchestrator is not an additional implementation role. It routes work, keeps the handoff state coherent, and
relays decisions. The human owner remains the only approver of gates, waivers, merges, and tags.

```text
Human owner
    │
    └── orchestrator ──┬── planner (design, ADRs, request scope in step pages)
                       ├── planning auditor (design reviewer)
                       ├── step agent / implementor (features, closeout, resolutions)
                       ├── audit agent (step auditor)
                       └── cross-cutting reviewer (phase-end XC reviews)
```

## 2. Responsibilities

- **Intake.** Clarify the request with the owner, identify the active step or the need for an out-of-band
  change, and surface the stop-and-ask triggers in [`agent-workflow.md`](agent-workflow.md) §8.
- **Route.** Spawn the planner for design work, the planning auditor for a frozen design review, the step agent
  for approved features and fixes, the auditor for a frozen closeout, and the cross-cutting reviewer for a
  phase-boundary review. Spawn one fresh context per assignment.
- **Relay.** Pass requests, evidence, questions, and owner decisions between the roles and the owner. Relaying
  is not transcription: each role writes its own durable record, and the orchestrator must not rewrite or
  replace it.
- **Track.** Keep the handoff card, active branch, frozen commit, and next action coherent. The repository
  remains the memory.
- **Discuss.** Serve as the owner's general discussion partner for questions, status, and planning
  conversations that do not require a specialised role.
- **Simple documentation.** Make small, non-normative documentation edits directly, as defined in section 5.

## 3. Boundaries

The orchestrator must not:

- implement code, tests, configuration, contracts, or generated artifacts;
- make design decisions, choose between plausible designs, or change scope;
- act as planner, design reviewer, step agent/implementor, auditor, or cross-cutting reviewer;
- write or edit a review, audit, or cross-cutting record, including request scope, evidence, findings,
  resolutions, or verdicts;
- approve a verdict, accept notes, waive a gate, merge, tag, push `main`, or delete a shared branch;
- spawn a generic general-purpose agent in place of a defined role agent;
- pass its own conversation history, assumptions, or preferred outcome to a reviewer or auditor.

For a merge, a tag, or a Blocker/Major waiver the orchestrator presents the cumulative approval report from
[`AGENTS.md` §3.12](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md) and asks for the owner's
decision. After the owner approves a merge or tag, the step agent or the owner performs it; the orchestrator
checks that the result is recorded.

## 4. Context and independence

The orchestrator is deliberately long-lived: it spans the active step and, while its context stays reliable,
consecutive steps and phases. That is the one exception to the fresh-context-per-step rule in
[`agent-workflow.md`](agent-workflow.md) §1. The role agents it spawns are fresh:

- spawn a **planner** for a new step or amendment design;
- spawn a **planning auditor** only after the design docs and review request are frozen;
- spawn a **step agent** for an approved feature list, a fix feature, or the closeout feature;
- spawn an **auditor** only after the closeout feature, local gates, and closeout CI are complete;
- spawn a **cross-cutting reviewer** only after the phase's last step is merged and tagged.

Do not carry a step agent into a review, or a reviewer into implementation. Give a reviewer only the frozen
commit, the request record, repository links, and the required checks. A reviewer must be able to state that it
had no shared conversation with the planner, step agent, or orchestrator.

If the orchestrator's context becomes unreliable, write the handoff card, start a fresh orchestrator, and read
the repository before continuing. The outgoing and incoming orchestrators must not both direct the same work.

## 5. Simple documentation

The orchestrator may edit documentation directly only when the change is small, non-normative, and does not
commit the project to anything. Examples:

- typo, spelling, and formatting fixes;
- broken relative links and navigation or index entries;
- refreshing a handoff card or status note to reflect a decision that is already recorded elsewhere;
- adding a short note that links to an existing durable record.

It must route the following to the planner or step agent instead:

- design, scope, acceptance criteria, contracts, schemas, or public API documentation;
- process rules, gates, roles, templates, or CI policy;
- ADRs, amendments, review or audit records, or waiver decisions;
- status changes that assert a gate passed, a feature is complete, or a step is done;
- any change that needs a new decision or a branch/merge gate.

Documentation-only orchestrator commits use a `docs/<slug>` branch and `[skip ci]`. The orchestrator never
commits directly to `main` and never commits process or design changes without the owning role.

## 6. Spawning and handoff rules

- Spawn only the defined role agents: planner, planning auditor, step agent/implementor, auditor, and
  cross-cutting reviewer. Do not use an undifferentiated agent for role work.
- Give each agent a bounded assignment: step or feature, branch, base commit, scope, non-goals, acceptance
  evidence, and stop-before triggers. The feature prompt in [`agent-workflow.md`](agent-workflow.md) §4 and the
  reviewer spawn card in §15 are the templates.
- Do not ask for a preferred verdict, and do not re-spawn until the answer is convenient. A new review round
  needs a new commit.
- Do not edit a role agent's output. If the output is wrong, send the correction back to the role that owns it.
- Record the role, assignment, and handoff in the step page or handoff card.
- Ensure the request owner and the reviewer are different contexts. The orchestrator is the only spawner and
  must not be the reviewer.

## 7. Anti-patterns

- The owner or a role agent spawning another role directly, bypassing the orchestrator.
- The orchestrator implementing, designing, reviewing, or approving to "save a step".
- The orchestrator squashing a feature or creating the step commit itself.
- The orchestrator summarising a reviewer's evidence instead of letting the durable record stand.
- The orchestrator carrying chat context into a fresh reviewer.
- The orchestrator treating a relayed message as durable evidence.
- One orchestrator per role, or multiple orchestrators directing the same work.
- Treating "orchestrator", "step agent", and "planner" as synonyms in a record or template.

## 8. Transition note

This policy was introduced after Step 10 feature 1 was implemented and squashed by the orchestrating agent
before an explicit orchestrator role existed. That was the deviation that motivated this page. The Step 10
closeout record must note it as a process deviation in the step page. No retrospective reimplementation is
required, and the feature is not re-audited as if the new rule had applied; the Step 10 audit covers the commits
as they exist. Future steps follow this page from their first feature.

## 9. Harness binding

The repository binds the four roles as OpenCode agents in `.opencode/agents/` and sets the orchestrator as the
`default_agent` in `opencode.json`. `.opencode/commands/step-start.md` and `step-closeout.md` are thin routers.

The binding enforces only what a permission list can enforce:

| Rule | Enforced how |
| --- | --- |
| The orchestrator cannot edit any file | `edit "*"` denied |
| The orchestrator can spawn only the planner, implementor, and reviewer | `subagent "*"` denied, then the three allowed |
| The reviewer can edit only `step-*.md` and `xc-*.md` records, and cannot spawn helpers | `edit "*"` denied, then the record paths allowed; `subagent "*"` denied |
| The planner cannot edit `src/`, `tests/`, `tools/`, or `contracts/` | those `edit` paths denied |

Everything else is procedural. Shell is unrestricted, so an agent can still write through a command; pushes,
merges, and tags are not restricted and rely on owner approval and branch protection; the reviewer's assigned-record
rule is not enforceable; and the orchestrator cannot make even a handoff-card or status edit, so those are routed
to the step agent. A human can select another agent with the agent picker (shift+tab in the container) and bypass the binding
entirely. Another harness ports
the same four prompts; the binding is an adapter, not a second rule set.
