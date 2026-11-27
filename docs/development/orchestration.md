# Orchestration and Role Delegation

This page is the normative home for the **lead** role: the single user-facing coordinator for all agent-assisted
work on MOCASSIN. The branch, gate, audit, and tag rules are in
[`AGENTS.md` §3](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md); the practical playbook is in
[`agent-workflow.md`](agent-workflow.md); model tiers and the free-first binding are in
[`model-economics.md`](model-economics.md). Review independence and one-file records are unchanged by this page.

## 1. One lead

Every engagement is coordinated by exactly one **lead agent**. The human owner talks only to the lead. The owner
does not spawn a design agent, dev, or review agent directly; they ask the lead, which spawns the right role.

The lead is not an additional implementation role. It routes work, keeps the handoff state coherent, and relays
decisions. The human owner remains the only approver of gates, waivers, merges, and tags.

```text
Human owner
    │
    └── lead ──┬── design (design docs, ADRs, request scope in step pages)
               ├── dev-free (features, fixes, closeout; free variant of the default tier, first choice)
               ├── dev (same role, paid default fallback)
               └── review (design review, step audit, cross-cutting review)
```

`dev-free` and `dev` are one dev role with two model bindings. There is no separate auditor role: design review
and audit both use the `review` agent, in a fresh context each time.

## 2. Responsibilities

- **Intake.** Clarify the request with the owner, identify the active step or the need for an out-of-band
  change, and surface the stop-and-ask triggers in [`agent-workflow.md`](agent-workflow.md) §8.
- **Route.** Spawn `design` for design work, `review` for a frozen design or a frozen closeout, and `dev-free`
  for approved features and fixes, falling back to the paid `dev` on a rate limit, a degraded model, or
  unavailability. Spawn one fresh context per assignment.
- **Assign the tier.** Use [`model-economics.md`](model-economics.md): the default tier for lead, design, dev,
  and review, with the dev's free variant first. A senior consultation is an escalation, not a standing tier.
  Never upgrade a role to a premium model on your own; a frontier upgrade is an owner decision.
- **Relay.** Pass requests, evidence, questions, and owner decisions between the roles and the owner. Relaying
  is not transcription: each role writes its own durable record, and the lead must not rewrite or replace it.
- **Track.** Keep the handoff card, active branch, frozen commit, and next action coherent. The repository
  remains the memory.
- **Discuss.** Serve as the owner's general discussion partner for questions, status, and planning conversations
  that do not require a specialised role.
- **Facilitate discovery.** Run a discovery/grilling round when the request is ambiguous, present the current
  frontier, and recommend an option with its consequence. Recommending is not deciding: the design agent or the
  owner records the decision in the step page, `GLOSSARY.md`, an ADR, or `docs/planning/05-open-decisions.md`,
  and the lead does not choose for the owner or change scope.
- **Route documentation.** Under the OpenCode binding the lead has no edit permission. Even small,
  non-normative documentation edits are routed to the dev; the process allowance in section 5 is exercised by
  the dev, not the lead.

## 3. Boundaries

The lead must not:

- implement code, tests, configuration, contracts, or generated artifacts;
- make design decisions, choose between plausible designs, or change scope; presenting a recommended option in a
  discovery round is not a decision and is allowed;
- act as design agent, dev, or review agent;
- write or edit a review, audit, or cross-cutting record, including request scope, evidence, findings,
  resolutions, or verdicts;
- approve a verdict, accept notes, waive a gate, merge, tag, push `main`, or delete a shared branch;
- spawn a generic general-purpose agent in place of a defined role agent;
- pass its own conversation history, assumptions, or preferred outcome to a review agent;
- promote itself or a role to a premium model.

For a merge, a tag, or a Blocker/Major waiver the lead assembles and presents the cumulative approval report from
[`AGENTS.md` §3.12](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md) and asks for the owner's decision.
After the owner approves a merge or tag, the dev or the owner performs it; the lead checks that the result is
recorded.

## 4. Context and independence

The lead is deliberately long-lived: it spans the active step and, while its context stays reliable, consecutive
steps and phases. That is the one exception to the fresh-context-per-step rule in
[`agent-workflow.md`](agent-workflow.md) §1. The role agents it spawns are fresh:

- spawn a **design** agent for a new step or amendment design;
- spawn a **review** agent only after the design docs and review request are frozen;
- spawn a **dev-free** (or fallback **dev**) for an approved feature list, a fix feature, or the closeout
  feature;
- spawn a **review** agent again only after the closeout feature, local gates, pre-audit gate, and closeout CI
  are complete;
- spawn a **review** agent for a phase-boundary cross-cutting review after the phase's last step is merged and
  tagged.

Do not carry a dev into a review, or a review agent into implementation. Give a review agent only the frozen
commit, the request record, repository links, and the required checks. A review agent must be able to state that
it had no shared conversation with the design agent, dev, or lead.

If the lead's context becomes unreliable, write the handoff card, start a fresh lead, and read the repository
before continuing. The outgoing and incoming leads must not both direct the same work.

## 5. Simple documentation

Under the OpenCode binding the lead has no edit permission; this section describes the process allowance, and
every edit it lists is routed to the dev.

The lead may route documentation edits directly only when the change is small, non-normative, and does not
commit the project to anything. Examples:

- typo, spelling, and formatting fixes;
- broken relative links and navigation or index entries;
- refreshing a handoff card or status note to reflect a decision that is already recorded elsewhere;
- adding a short note that links to an existing durable record.

It must route the following to a design agent or dev instead:

- design, scope, acceptance criteria, contracts, schemas, or public API documentation;
- process rules, gates, roles, profiles, or templates;
- ADRs, amendments, review or audit records, or waiver decisions;
- status changes that assert a gate passed, a feature is complete, or a step is done;
- any change that needs a new decision or a branch/merge gate.

Documentation-only commits use a `docs/<slug>` branch or the active step branch, `[skip ci]`, and are made by the
dev. The lead never commits directly to `main` and never commits process or design changes without the owning
role.

## 6. Spawning and handoff rules

- Spawn only the defined role agents: `design`, `dev-free`, `dev`, and `review`. Do not use an undifferentiated
  agent for role work. `dev-free` and `dev` are one role, not two.
- Assign the model tier from [`model-economics.md`](model-economics.md). Do not upgrade a role to a premium model
  because the task feels hard; escalation follows the triggers on that page.
- Prefer `dev-free`. When the spawn fails or the child reports a rate limit, a degraded model, or
  unavailability, re-spawn the same bounded assignment on `dev` and note the fallback. Do not switch the free
  binding to another model silently.
- Give each agent a bounded assignment: step or feature, branch, base commit, scope, non-goals, acceptance
  evidence, and stop-before triggers. The feature prompt in [`agent-workflow.md`](agent-workflow.md) §4 and the
  review spawn card in §15 are the templates.
- Do not ask for a preferred verdict, and do not re-spawn until the answer is convenient. A new review round
  needs a new commit.
- Do not edit a role agent's output. If the output is wrong, send the correction back to the role that owns it.
- Record the role, assignment, and handoff in the step page or handoff card.
- Ensure the request owner and the review agent are different contexts. The lead is the only spawner and must
  not be the review agent.
- Treat a failed, errored, or interrupted role-agent spawn as **possibly still running and partially applied**,
  not as a no-op. A spawn that ends in a harness error (`Session.AgentNotFoundError`, a timeout, or a dropped
  connection) may still have started the child, kept it alive, or run part of its assignment. Before
  re-spawning, confirm that the earlier context has actually stopped and inspect the branch tip, the worktree,
  and the working-tree status; never re-run a destructive or history-writing step (merge, squash, amend, reset,
  branch deletion) blindly. Two live contexts on one branch can produce duplicate or conflicting commits, so
  check for commits the previous context created before starting a replacement. Report the observed state in the
  handoff. The known cause and the version-scoped mitigation are in §9.

## 7. Anti-patterns

- The owner or a role agent spawning another role directly, bypassing the lead.
- The lead implementing, designing, reviewing, or approving to "save a step".
- The lead squashing a feature or creating the step commit itself.
- The lead summarising a review agent's evidence instead of letting the durable record stand.
- The lead carrying chat context into a fresh review agent.
- The lead treating a relayed message as durable evidence.
- One lead per role, or multiple leads directing the same work.
- Treating "lead", "dev", and "design agent" as synonyms in a record or template.
- The lead silently upgrading to a premium model.

## 8. Transition note

This policy was introduced after Step 10 feature 1 was implemented and squashed by the orchestrating agent
before an explicit coordinator role existed. That was the deviation that motivated this page. The Step 10
closeout record must note it as a process deviation in the step page. No retrospective reimplementation is
required, and the feature is not re-audited as if the new rule had applied; the Step 10 audit covers the commits
as they exist. The role rename to `lead`/`design`/`dev`/`review` applies from Step 15; earlier records keep the
names they used.

## 9. Harness binding

The repository binds the roles as OpenCode agents in `.opencode/agents/` (`lead`, `design`, `dev-free`, `dev`,
`review`) and sets the lead as the `default_agent` in `opencode.json`. `.opencode/commands/step-start.md` and
`step-closeout.md` are thin routers.

The binding enforces only what a permission list can enforce:

| Rule | Enforced how |
| --- | --- |
| The lead cannot edit any file | `edit "*"` denied |
| The lead can spawn only `design`, `dev-free`, `dev`, and `review` | `subagent "*"` denied, then those four allowed |
| The review agent can edit only `step-*.md` and `xc-*.md` records, and cannot spawn helpers | `edit "*"` denied, then the record paths allowed; `subagent "*"` denied |
| The design agent cannot edit `src/`, `tests/`, `tools/`, or `contracts/` | those `edit` paths denied |
| Each role binding runs on its assigned model tier | `model:` in each agent file, per [`model-economics.md`](model-economics.md) |

Everything else is procedural. Shell is unrestricted, so an agent can still write through a command; pushes,
merges, and tags are not restricted and rely on owner approval and branch protection; the review agent's
assigned-record rule is not enforceable; and the lead cannot make even a handoff-card or status edit, so those
are routed to the dev. A human can select another agent with the agent picker (shift+tab in the container) and
bypass the binding entirely. Another harness ports the same prompts; the binding is an adapter, not a second
rule set.

MOCASSIN does not import the upstream `.githooks`/`GATE_HISTORY` checks. Its controls are the existing local
gates (`tools/verify.sh`), branch protection, the review/audit gates, and owner approval.

### OpenCode 2.3.0 cold-start spawn race (2026-11-17)

On OpenCode 2.3.0 a fresh role-agent spawn can fail with
`Session.AgentNotFoundError: Agent not found: "<role>"` even though the definitions in `.opencode/agents/` are
valid and loaded; `opencode debug agents` lists all four agents. Durable evidence from
`~/.local/share/opencode/log/opencode.log` recorded 20 such failures, each 0.57–0.80 s after a fresh
`location services booted` line and 10–100 ms after the `agent.updated` event, with `ServerProcess.start` at the
bottom of the stack. This is a cold-start race between server boot and the session drain, not a permission or
definition error, and it can hit the parent (`lead`) as well as a child. A failed spawn is therefore not a clean
no-op: the child may have run part of the assignment, and its context may still be alive. In one engagement a
context whose spawn reported `AgentNotFoundError` resumed later and committed to the same branch while its
replacement was preparing a commit; the replacement found the earlier commit already at the branch tip, created
from the same working tree. Confirming that the previous context has stopped, and checking the branch tip for
commits it already created, is part of the §6 inspection rule.

Mitigation until the image ships a fixed release:

- warm the shared service before the first spawn with `opencode api get /api/info`; do not use
  `opencode debug agents` for this, because it may start a standalone instance instead of contacting the running
  service (see below);
- prefer one long-lived service or session over repeated cold starts.

Remove this note once a fixed OpenCode release is pinned in the image and the failure no longer reproduces. The
§6 inspection rule is harness-independent and stays.

### Persistent worktrees and container restarts

**Observed behaviour.** The container itself restarts between turns. The hostname and the service PID change, and
`/tmp` is wiped. Evidence: two consecutive incidents in which the hostname and the service PID changed and
`/tmp/opencode` came back empty. A restart kills running subagents and loses their completion reports, so an
agent's last message is not evidence that its work is durable.

Rules for every role:

- Worktrees live under `/home/node/worktrees/<name>`; scratch files live under `/home/node/tmp` or the
  repository's gitignored `.tmp`. `/tmp` is for disposable artifacts only and is never the sole copy of state or
  evidence.
- Commit and push each feature or record before leaving work idle. Never rely on uncommitted work surviving a turn
  boundary.
- After an unexpected error or restart, verify the branch tip, the worktree registration, the working-tree status,
  and any CI, merge, or tag state, then resume the same session. Never repeat a CI dispatch, merge, squash, tag,
  or branch deletion on assumption.
- Prune registrations for wiped worktree paths with `git worktree prune`.

**Service version check.** After a restart, compare `opencode --version` with the `version` reported by
`GET /api/info`. A mismatch has caused agent-resolution failures (`AgentNotFoundError`) and is fixed by restarting
the service while it is idle. Warm the shared service with `opencode api get /api/info`, not
`opencode debug agents`, which may start a standalone instance instead of contacting the running service.
