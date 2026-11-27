# Working with Coding Agents

This page is the practical playbook for guiding coding agents on MOCASSIN. The mandatory rules live in
[`AGENTS.md`](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md); if this page and `AGENTS.md`
disagree, `AGENTS.md` wins.

Use this page to prepare work, review agent output, and keep contexts small enough to stay reliable.

## 1. Context policy

- Start a **fresh context for each roadmap step**.
- Reuse that context across the step's feature branches.
- Start a fresh context at the next step boundary.
- Refresh mid-step if context compression loses important constraints, or if the agent begins mixing old and
  current decisions.
- Do not carry a review agent into the implementation context, or a dev into the review context.
- The **lead** is the long-lived coordination context and the owner's only direct interface. The role agents it
  spawns (`design`, `dev-free`/`dev`, `review`, and `senior` on escalation only) are the fresh contexts.
- Do not carry the lead's chat history into a review agent. Pass the spawn card, the frozen commit, and
  repository links only.
- The model tier for each role is in [`model-economics.md`](model-economics.md). The dev starts on `dev-free`;
  the lead falls back to `dev` only on a rate limit, a degraded model, or unavailability.

The repository is the memory, not the conversation. At the start of a step, read:

1. [`AGENTS.md`](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md)
2. [`docs/planning/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/README.md)
3. [`docs/planning/status.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/status.md)
4. The step page and its dependencies
5. Relevant ADRs, data contracts, and reference docs
6. [`drift-checks.md`](drift-checks.md) and [`agent-security.md`](agent-security.md) when the work touches process,
   CI, credentials, or cross-step status
7. [`incident-and-rollback.md`](incident-and-rollback.md) when a bad change has reached `main` or a release

Verify the starting state:

```sh
git switch main
git pull --ff-only
git describe --tags
git status --short
```

### Agent roles and write scopes

The normative matrix is in [`AGENTS.md`](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md) §3.8 and the
orchestration rules are in [`orchestration.md`](orchestration.md). In short: the lead is the owner's only direct
interface and spawns every role agent; the design agent and dev edit their own artifacts directly; the review
agent may edit only its assigned review or audit record; the human owner approves gates. There is no scribe and
no transcription step.

| Role | Scope | Not |
| --- | --- | --- |
| Lead | Intake, routing, spawning, model-tier assignment, handoffs, relaying, discussion | Design, code, tests, configuration, records, verdicts, gates, merges, tags; any edit |
| Design agent | Design and planning docs; request scope in the review record | Code, tests, configuration, verdicts, step status, merges, tags |
| Dev (`dev-free`/`dev`) | Code, tests, configuration, contracts, step page, status, parity; request and resolutions in records | Review findings, evidence, or verdict text; merges and tags without approval |
| Review agent (design review or audit) | Only the assigned record: own metadata, checklist marks, evidence, findings, verdict | Everything else, including code, design docs, request scope, resolutions, merges, tags |
| Human owner | Approval of gates, waivers, merges, and tags | — |

The lead routes documentation edits to the dev, including typos, links and navigation entries, handoff cards, and
status notes that restate an already-recorded decision. Process rules, design, contracts, acceptance criteria,
records, and status that asserts a gate passed belong to the owning role.

## 2. Context handoff card

At a context boundary, record this in the step page or status discussion:

```text
Main commit/tag:
Step:
Step branch:
Approved feature list:
Current feature:
Feature branch:
Last local gates:
Last closeout CI:
Open blockers:
Next action:
Applicable discipline/skill:
```

The role ending its context writes the card; the dev records it and the lead relays it. The next context should
not need the previous chat history. The discipline line names the task practice from
[`index.md`](index.md) that fits the next action, if one does.

## 3. Step start checklist

Before implementation:

- [ ] The step is the lowest unblocked step, or the owner assigned it.
- [ ] Dependencies are complete.
- [ ] The design class is recorded: `Full`, `Light`, or `None` per
      [`design-proportionality.md`](design-proportionality.md).
- [ ] For `Full` and `Light`, the design review is `Ready for implementation` or an owner-accepted
      `Ready with notes`. For `None`, the one-line design record is present.
- [ ] The step page, acceptance criteria, tests, and risks were read.
- [ ] The step page records its complexity class and split decision per
      `docs/planning/04-roadmap.md` §3.1 (internal planning), or the split is recorded with the owner
      acceptance for a High step.
- [ ] A complete feature-branch list was proposed and approved once.
- [ ] The step branch was created from current `main`.
- [ ] The step was marked `In progress` in the step page and dashboard.
- [ ] Relevant ADRs and data contracts were read.
- [ ] The original reference clone is treated as read-only.

## 4. Feature start prompt template

Use this as the basis for each feature request:

```text
Step: 00 — Repository, Toolchain, and CI
Feature: .NET solution skeleton
Branch: feature/step-00-foundation/dotnet-skeleton
Base: step/00-foundation, with current main merged in
Design class: Full / Light / None

Read:
- AGENTS.md
- docs/planning/status.md
- docs/planning/steps/step-00-repository-toolchain-and-ci.md
- relevant ADRs and contracts

Scope:
- ...

Directory plan:
- New folders:
- New files:
- Moved files:
- New namespaces:

Non-goals:
- ...

Acceptance evidence:
- exact build/test commands and output
- files changed
- docs/status updates
- known limitations

Stop before:
- any format/API decision not covered by the plan
- adding a new dependency
- merging into `main` or starting a new step
```

## 5. Evidence and approval report

The everyday output contract is [`AGENTS.md` §17](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md):
step/feature, branch, commits, commands and results, CI, docs/status/parity, known limitations, and approval
requested. Do not duplicate it here.

A merge, a tag, or a Blocker/Major waiver is not ready for approval until the requesting agent can report the
full picture, including every earlier round. A verdict acceptance uses the record's round table instead:

```text
Decision requested:
Branch/frozen commit:
Final verdict:
Final state: closeout CI, main CI, docs build, policy/security gate
Rounds:
  1. date, commit, review agent, lead, verdict
  2. ...
Findings and resolutions:
  ID, severity, origin (design/implementation/evidence/documentation), issue, evidence, resolution, fixing commit, status
Accepted notes and waivers:
Residual risks and follow-up owners:
Diff summary:
Tests/commands:
CI:
Docs build:
Structure/policy check:
Docs/status updated:
Gate statement (no Blocker/Major remains, or the exact exceptions being waived):
Approval requested for:
```

Rules:

- Include every review and audit round, every finding, and every resolution, including findings already fixed.
- Name the code commit that fixed each finding and the documentation commit that resolved each design note.
- Report accepted notes and waivers explicitly, with who accepted them and why.
- State residual risk, deferred items, and follow-up owners.
- Never report only the latest round or only the findings that remain open.
- If a round is incomplete or lacks a verdict or resolution, say so; do not omit it.
- The report is the basis for the human decision. The human owner still approves verdicts, waivers, merges, and
  tags.

Review the diff itself:

```sh
git diff step/00-foundation...feature/step-00-foundation/<feature>
```

Do not review or accept a change from prose alone.

## 6. Diff review checklist

- Does the change stay inside the approved feature scope?
- Are non-goals still out of scope?
- Are public APIs, formats, or scientific semantics changed?
- If yes, is there an ADR/version bump/fixture update?
- Are tests exercising observable behaviour rather than internal calls?
- Were real objects/files used where practical?
- Are new dependencies justified, pinned, and licence-checked?
- Are generated contract artifacts updated by the generator, not hand-edited?
- Are step page, feature log, dashboard, and parity matrix current?
- Are there unrelated formatting or refactor changes?

## 7. Testing expectations

Follow the testing trophy in
[`docs/planning/03-quality-strategy.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/03-quality-strategy.md):

- highest effort on contract/acceptance, property, and toy-integration tests;
- real objects, real files, and real writers where practical;
- generated toy data for breadth;
- mocks/fakes only at true process/OS/UI/network/time boundaries or for failure injection;
- no mocking domain/compiler/solver/energy/evaluation internals;
- no call-count or internal-order assertions;
- coverage is a diagnostic, not a target.

Ask: **what user-visible failure does this test catch?**

## 8. Stop-and-ask triggers

Stop before proceeding when:

- a format, API, or physics convention is ambiguous;
- two or more plausible designs exist;
- a public contract or generated schema would change;
- a new dependency is required;
- the original behaviour is unclear;
- the change would cross from one step into another;
- tests reveal a contradiction in the plan;
- the design class would need to move down after the design is frozen;
- the next action would need a third Full review round without an owner decision, or a senior consultation when
  its trigger is not met;
- a role-agent spawn fails, errors, or is interrupted — stop, confirm the earlier context has stopped, and
  verify the branch tip, worktree, and working-tree status before re-spawning; the failed spawn may be partially
  applied or still live and writing ([`orchestration.md`](orchestration.md) §6 and §9);
- an agent errors unexpectedly, or the container or OpenCode service restarts — stop, verify the branch tip,
  worktree, working-tree status, and any CI, merge, or tag state; use `/home/node/worktrees` and `/home/node/tmp`,
  and resume the same session instead of repeating an irreversible step ([`orchestration.md`](orchestration.md)
  §9);
- the working tree is on `main` or a step branch with feature work;
- the next action would merge, tag, push `main`, or delete a shared branch.

Guessing is more expensive than asking. Record the question and the answer in the step page or the review/audit
record.

## 9. Status and tags

Keep status current in the same feature branch:

- `docs/planning/status.md` dashboard;
- step-page implementation status and feature log;
- step-page task/acceptance checkboxes;
- parity matrix when applicable.

After a step is approved and merged:

- create an annotated `step-NN` tag on the merge commit;
- create the matching `milestone-mN` tag when a roadmap milestone is reached;
- never move or delete a published tag;
- use `step-NN.1` for a follow-up checkpoint.

See [`AGENTS.md`](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md) section 3.7 for the normative
rules.

## 10. CI policy

CI is intentionally minimal and local-first. The normative rules are in `AGENTS.md` §3.13 and the cost model
is in [`docs/planning/ci-policy.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/ci-policy.md).

Automatic CI runs only on `main` after a step merge, on Ubuntu, with path filters so only affected jobs run.
The step closeout run is a single manual `CI` dispatch on the step branch. There are **no per-feature and no
per-push step-branch CI runs**.

| Run | Trigger | Runner |
| --- | --- | --- |
| Main merge | automatic `push` to `main` | Ubuntu |
| Step closeout | one manual `workflow_dispatch` per step | Ubuntu |
| Platform gate | manual `workflow_dispatch` with `platforms` | Windows/macOS |
| Documentation | manual `docs.yml` | Ubuntu |
| Benchmark | manual `benchmark.yml` | Ubuntu |

Rules:

- Validate every feature locally with `./tools/verify.sh <area>`; run `./tools/verify.sh all --require` before
  closeout. A skipped required gate is not a pass; `cargo-deny` must be installed for a closeout run.
- Do not push a step branch just to trigger CI.
- Windows and macOS stay manual until platform-specific code appears. Promote Windows first; keep macOS manual
  until there is macOS-specific behaviour to validate.
- Documentation is built locally with `zensical build --strict` until the site is mature enough to justify
  automatic docs CI.
- Keep Nightly manual-only; do not add a schedule until real long-running suites exist.
- Wait for the automatic `main` run before tagging `step-NN`. Never tag a red `main`.
- Do not add CI triggers for feature branches or per-push checkpoint runs just to feel safe. Add a trigger only
  when it catches a class of regression that local gates cannot.

## 11. Reference data

When generating reference data:

- use [`docs/planning/07-reference-build.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/07-reference-build.md);
- keep the gitignored `/workspace/mocassin-original` reference clone read-only;
- record the original commit, compiler, package pins, seeds, and hashes;
- treat the original as an oracle, not as source to copy.

## 12. Anti-patterns

Reject or stop on:

- a long-lived feature branch that keeps absorbing unrelated changes;
- an agent implementing outside the approved step scope, or bypassing the audit gate;
- “builds successfully” with no command output;
- mock-heavy tests that assert handling rather than behaviour;
- coverage gaming;
- silent changes to energy conventions, P1 order, unit-cell counts, seeds, or formats;
- hand-edited generated contract files;
- new dependencies without justification;
- direct commits to `main` or the step branch;
- moving/deleting published tags;
- rewriting shared history or force-pushing;
- copying GPL code or data into the MIT implementation;
- a scribe transcription step or a review agent editing anything outside its assigned record;
- bypassing the lead to spawn a role agent directly, or letting the lead design, implement, review, approve,
  merge, or tag;
- a review/audit record commit that touches code, tests, configuration, status, or design docs;
- pre-seeding review, audit, or cross-cutting records for work that has not started;
- skipping a gate without a recorded waiver and a follow-up owner;
- a new review round for Minor or Note findings alone;
- a senior consultation before two completed blocked standard rounds, as a third standard round, or as a verdict;
- using the senior consultant as a standing reviewer or a general upgrade;
- using a premium model as a default or upgrading to it without the owner.

## 13. Gate references

This playbook does not restate the gate sequences. The normative homes are:

- design classes and the pre-audit gate: [`design-proportionality.md`](design-proportionality.md);
- task disciplines: [`index.md`](index.md) and each discipline page;
- review axes: [`review-axes.md`](review-axes.md);
- senior escalation: [`senior-consultation.md`](senior-consultation.md);
- agent output contract:
  [`AGENTS.md` §17](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md);
- model tiers and free-first binding: [`model-economics.md`](model-economics.md);
- design review: [`AGENTS.md` §3.9](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md) and
  [`docs/planning/reviews/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/reviews/README.md);
- step audit: [`AGENTS.md` §3.10](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md) and
  [`docs/planning/audits/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/audits/README.md);
- closeout evidence and the merge-tree rule:
  [`docs/planning/closeout/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/closeout/README.md);
- amendments: [`AGENTS.md` §3.11](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md) and
  [`docs/planning/amendments/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/amendments/README.md);
- cross-cutting phase reviews:
  [`docs/planning/reviews/cross-cutting/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/reviews/cross-cutting/README.md);
- drift checks: [`drift-checks.md`](drift-checks.md);
- incident and rollback: [`incident-and-rollback.md`](incident-and-rollback.md);
- agent security: [`agent-security.md`](agent-security.md).

The closeout sequence, merge-tree rule, and approval rules are in `AGENTS.md` §3.4 and
[`docs/planning/closeout/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/closeout/README.md).
The review and audit records are the durable evidence; this page is only the operating playbook.

## 14. Changing the process

Process changes are normal documentation changes, but they have a wider blast radius than project docs.

1. Open a `docs/<slug>` branch from `main`.
2. Explain the problem and the proposed rule change in the branch or pull request.
3. Update the normative page and every template or summary that depends on it in the same change.
4. Run `./tools/verify.sh docs --require` and `./tools/verify.sh policy --require`.
5. Get owner approval. Do not change the process silently in the middle of a step; if a mid-step change is
   unavoidable, record it as an amendment-level decision and update the process after the step closes.

Do not add a second copy of a rule to make it easier to find. Add a link from the new location to the normative
page. `tools/check_process_consistency.py` is the mechanical backstop.

## 15. Review agent spawn card

Use this when the lead spawns a design review, audit, cross-cutting review, or policy-update review agent on
behalf of the request owner. Replace the bracketed values and do not add a desired verdict or a summary that
replaces reading the repository.

```text
You are a fresh, read-only review agent for [a design review / a step audit / a cross-cutting review / a
policy-update review]. You did not author or implement this work and have no shared conversation with the design
agent, dev, or lead.

Frozen commit: [full commit]
Request record: [docs/planning/reviews/step-NN.md, docs/planning/audits/step-NN.md, or
docs/planning/reviews/policy-<date>-<slug>.md]
Design class: [Full / Light]
Repository: /workspace/mocassin

Read AGENTS.md, the request record, the step page, and the relevant design/contract docs. Do not rely on my
summary; inspect the repository at the frozen commit yourself. Pin the base and reviewed commits and use a
three-dot diff.

Run the required builds/tests and review the diff. Label every finding on the Spec or Standards review axis per
docs/development/review-axes.md; do not merge or re-rank the axes. Write evidence, findings, verdict, and
checklist completion marks directly into the request record. You may check checklist boxes but not change item
text. Do not edit any other file, merge, tag, push main, or delete branches. For a Full review, stop when no
Blocker or Major remains; for a Light review, one pass is the whole review. Do not open a new round for Minor or
Note findings alone.

If you find issues, report them; do not fix them. The human owner approves the final verdict and gate.
State in the record that you are a fresh context with no shared conversation with the design agent, dev, lead,
or other review agents.
```

Rules for the lead as spawner:

- Do not ask for a specific verdict or re-spawn until a preferred verdict appears.
- Do not edit the review agent's findings, evidence, or verdict.
- Do not mark the review/audit accepted, waived, merged, or tagged.
- Fixes require a new commit and a new review round, made by the dev or design agent.

### Senior consultation spawn card

Use this card only after two completed standard rounds of the same design review, audit, cross-cutting review, or
policy-update review each ended blocked and at least one Blocker or Major is still open. The full trigger, request
contract, and senior rules are in [`senior-consultation.md`](senior-consultation.md); do not restate them in the
card.

The request owner creates the senior record from
`docs/planning/reviews/senior/_template.md`. Replace the bracketed values. Do not add a preferred answer, a
competing narrative, the lead's history, or a request to inspect the whole repository.

```text
You are the senior consultant for one bounded question. This is an escalation after two completed blocked
standard rounds, not a normal review round. Do not issue a gate verdict.

Frozen commit: [full commit hash]
Parent record: [docs/planning/reviews/step-NN.md, docs/planning/audits/step-NN.md, or the policy/XC record]
Senior record: [docs/planning/reviews/senior/step-NN-M.md, xc-NN-M.md, or policy-<date>-<slug>-M.md]
Step page or checkpoint: [path]
Standard rounds escalated: [round X and round Y, with verdicts]
Open Blocker/Major findings: [finding IDs and severities]
Approved inspection scope: [exact files, diff range, commands, and evidence]
Out of scope: [everything else]

Question to answer:
[one answerable question]

Why this question is unresolved:
[the contradictory evidence or missing judgment, without advocating an answer]

Read only the assignment, the frozen commit, the parent record, and the paths above, plus the minimum direct
context needed to answer. Do not scan the repository or open unrelated files. Do not guess.

If the question, frozen commit, scope, or evidence is incomplete, ambiguous, contradictory, or too broad to
answer without a repository-wide scan, stop before inspecting anything. Write `Declined` in the senior record,
state the exact missing information and the clarification you need, and report the same refusal to the lead. Do
not answer a different question.

If the assignment is clear, answer only the question, cite the evidence, state assumptions and limitations, and
write the answer into the senior record. Edit no other file. Commit only the senior record with `[skip ci]`.
Do not fix findings, approve or waive anything, spawn another agent, merge, tag, push main, or delete branches.
State in the record that you are a fresh context with no access to the lead's history, the owner's preferred
answer, or another reviewer's reasoning.
```

Rules for the lead:

- Check the trigger first: two completed blocked standard rounds of the same gate, at least one Blocker or Major
  still open in the second round, and one answerable question.
- Do not request a senior consultation for a first round, for Minor or Note findings only, or to avoid a standard
  round.
- Provide the frozen commit, the parent record, the finding IDs, and an exact bounded inspection scope.
- Do not pass the lead's conversation history, a preferred answer, the owner's preferred outcome, or a request to
  "review the repository".
- If the senior declines, relay the missing information to the owner or request owner. Do not re-spawn with the
  same vague assignment.
- The answer is advisory. The owner decides, and a fresh standard round at the new commit remains the gate.
