# Working with Coding Agents

This page is the practical playbook for guiding coding agents on MOCASSIN. The mandatory rules live in
[`AGENTS.md`](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md); if this page and `AGENTS.md`
disagree, `AGENTS.md` wins.

Use this page to prepare work, review agent output, and keep contexts small enough to stay reliable.

## 1. Context policy

- Start a **fresh context for each roadmap step**.
- Reuse that context across the step’s feature branches.
- Start a fresh context at the next step boundary.
- Refresh mid-step if context compression loses important constraints, or if the agent begins mixing old and
  current decisions.

The repository is the memory, not the conversation. At the start of a step, read:

1. [`AGENTS.md`](https://github.com/seb-eis/mocassin-2/blob/main/AGENTS.md)
2. [`docs/planning/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/README.md)
3. [`docs/planning/status.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/status.md)
4. The step page and its dependencies
5. Relevant ADRs, data contracts, and reference docs

Verify the starting state:

```sh
git switch main
git pull --ff-only
git describe --tags
git status --short
```

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
Last step-branch CI:
Open blockers:
Next action:
```

The next context should not need the previous chat history.

## 3. Step start checklist

Before implementation:

- [ ] The step is the lowest unblocked step, or the owner assigned it.
- [ ] Dependencies are complete.
- [ ] The step page, acceptance criteria, tests, and risks were read.
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

Read:
- AGENTS.md
- docs/planning/status.md
- docs/planning/steps/step-00-repository-toolchain-and-ci.md
- relevant ADRs and contracts

Scope:
- ...

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
- merging into the step branch
```

## 5. Evidence and approval report

A feature is not ready for approval until the agent can report:

```text
Branch:
Commits:
Diff summary:
Tests/commands:
CI:
Docs/status updated:
Risks:
Approval requested for:
```

Review the diff itself:

```sh
git diff step/00-foundation...feature/step-00-foundation/<feature>
```

Do not approve from prose alone.

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
- the working tree is on `main` or a step branch with feature work;
- the next action would merge, tag, push `main`, or delete a shared branch.

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

Feature branches are validated locally. CI runs on:

- integration branches matching `step/*`;
- `main` after a step merge.

Do not add CI triggers for feature branches just to feel safe. Add a trigger only when it catches a class of
regression that local gates cannot.

## 11. Reference data

When generating reference data:

- use [`docs/planning/07-reference-build.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/07-reference-build.md);
- keep `/workspace/mocassin-original` read-only;
- record the original commit, compiler, package pins, seeds, and hashes;
- treat the original as an oracle, not as source to copy.

## 12. Anti-patterns

Reject or stop on:

- a long-lived feature branch that keeps absorbing unrelated changes;
- an agent chaining multiple features without approval;
- “builds successfully” with no command output;
- mock-heavy tests that assert handling rather than behaviour;
- coverage gaming;
- silent changes to energy conventions, P1 order, unit-cell counts, seeds, or formats;
- hand-edited generated contract files;
- new dependencies without justification;
- direct commits to `main` or the step branch;
- moving/deleting published tags;
- rewriting shared history or force-pushing;
- copying GPL code or data into the MIT implementation.

## 13. Step completion audit

The step agent's self-check is not the independent audit.

Before merging a completed step:

1. Finish the closeout feature and get step-branch CI green.
2. Update `docs/planning/audits/step-NN.md` from the template with the exact commit, CI run, scope, and known
   limitations; set its status to `Ready for audit`.
3. Freeze the step branch.
4. Let a fresh read-only audit agent review it. The auditor must not modify the repository.
5. If findings require fixes, the step agent fixes them on a new feature branch, squash-merges into the step
   branch, pushes, waits for CI, and marks the audit `Re-audit required`.
6. Re-audit the new commit. Only a `Passed` audit permits the step-to-main approval request.
7. The human owner still approves the merge and tag.

Use [`docs/planning/audits/README.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/audits/README.md)
for status values, severities, and the report format. Audit reports are durable planning records; do not rely on chat history.

## 14. Review gates

| Gate | Approval |
| --- | --- |
| Step feature list | Once, at step start |
| Each feature squash | Before every squash into the step branch |
| Step completion audit | Fresh read-only audit after the step is frozen |
| Step-to-main merge | Before the `--no-ff` merge, only after a passed audit |
| Tag creation/push | Included in the step/milestone/release approval |
| Release | After Step 31 gates |

The goal is not to slow the agent down. It is to keep every merge explainable, reproducible, and reversible.
