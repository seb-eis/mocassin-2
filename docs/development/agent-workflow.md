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
- Do not carry a reviewer into the implementation context, or an implementor into the review context.
- The **orchestrator** is the long-lived coordination context and the owner’s only direct interface. The role
  agents it spawns (planner, step agent/implementor, planning auditor, auditor) are the fresh contexts.
- Do not carry the orchestrator’s chat history into a reviewer. Pass the spawn card, the frozen commit, and
  repository links only.

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
orchestration rules are in [`orchestration.md`](orchestration.md). In short: the orchestrator is the owner’s
only direct interface and spawns every role agent; the planner and step agent edit their own artifacts
directly; the reviewer may edit only its assigned review or audit record; the human owner approves gates. There
is no scribe and no transcription step.

| Role | Scope | Not |
| --- | --- | --- |
| Orchestrator | Intake, routing, spawning, handoffs, relaying, discussion, small non-normative docs fixes | Design, code, tests, configuration, records, verdicts, gates, merges, tags |
| Planner | Design and planning docs; request scope in the review record | Code, tests, configuration, verdicts, step status, merges, tags |
| Step agent (implementor) | Code, tests, configuration, contracts, step page, status, parity; request and resolutions in records | Reviewer findings, evidence, or verdict text; merges and tags without approval |
| Reviewer (planning auditor or auditor) | Only the assigned record: own metadata, checklist marks, evidence, findings, verdict | Everything else, including code, design docs, request scope, resolutions, merges, tags |
| Human owner | Approval of gates, waivers, merges, and tags | — |

The orchestrator may edit documentation directly only for typos, links and navigation entries, handoff cards,
and status notes that restate an already-recorded decision. Process rules, design, contracts, acceptance
criteria, records, and status that asserts a gate passed belong to the owning role.

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
```

The role ending its context writes the card; the orchestrator relays it and the next context should not need the
previous chat history.

## 3. Step start checklist

Before implementation:

- [ ] The step is the lowest unblocked step, or the owner assigned it.
- [ ] Dependencies are complete.
- [ ] The pre-implementation design review is `Ready for implementation`.
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

A merge, a tag, or a Blocker/Major waiver is not ready for approval until the requesting agent can report the
full picture, including every earlier round. A verdict acceptance uses the record's round table instead:

```text
Decision requested:
Branch/frozen commit:
Final verdict:
Rounds:
  1. date, commit, reviewer/auditor, verdict
  2. ...
Findings and resolutions:
  ID, severity, issue, evidence, resolution, fixing commit, status
Accepted notes and waivers:
Residual risks and follow-up owners:
Diff summary:
Tests/commands:
CI:
Docs build:
Structure check:
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
- a role-agent spawn fails, errors, or is interrupted — stop, confirm the earlier context has stopped, and
  verify the branch tip, worktree, and working-tree status before re-spawning; the failed spawn may be partially
  applied or still live and writing ([`orchestration.md`](orchestration.md) §6 and §9);
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
- a scribe transcription step or a reviewer editing anything outside its assigned record;
- bypassing the orchestrator to spawn a role agent directly, or letting the orchestrator design, implement,
  review, approve, merge, or tag;
- a review/audit record commit that touches code, tests, configuration, status, or design docs;
- pre-seeding review, audit, or cross-cutting records for work that has not started;
- skipping a gate without a recorded waiver and a follow-up owner.

## 13. Gate references

This playbook does not restate the gate sequences. The normative homes are:

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

## 15. Reviewer spawn card

Use this when the orchestrator spawns a design reviewer or auditor on behalf of the request owner. Replace the
bracketed values and do not add a desired verdict or a summary that replaces reading the repository.

```text
You are a fresh, read-only [design reviewer / auditor]. You did not author or implement this work and have no
shared conversation with the planner, implementor, or orchestrator.

Frozen commit: [full commit]
Request record: [docs/planning/reviews/step-NN.md or docs/planning/audits/step-NN.md]
Repository: /workspace/mocassin

Read AGENTS.md, the request record, the step page, and the relevant design/contract docs. Do not rely on my
summary; inspect the repository at the frozen commit yourself.

Run the required builds/tests and review the diff. Write evidence, findings, verdict, and checklist completion
marks directly into the request record. You may check checklist boxes but not change item text. Do not edit any
other file, merge, tag, push main, or delete branches.

If you find issues, report them; do not fix them. The human owner approves the final verdict and gate.
State in the record that you are a fresh context with no shared conversation with the planner, implementor,
orchestrator, or other cross-cutting reviewers.
```

Rules for the orchestrator as spawner:

- Do not ask for a specific verdict or re-spawn until a preferred verdict appears.
- Do not edit the reviewer's findings, evidence, or verdict.
- Do not mark the review/audit accepted, waived, merged, or tagged.
- Fixes require a new commit and a new review round, made by the step agent or planner.
