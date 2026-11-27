# AGENTS.md

Guidance for AI coding agents and human contributors working in this repository.

## 1. What this project is

This is a modern, cross-platform reimplementation of **MOCASSIN** (*Monte Carlo for Solid State Ionics*),
the fixed-lattice MMC/KMC simulation suite for defects and transport in crystalline solids originally developed
at Forschungszentrum Jülich / RWTH Aachen.

**Current state: Phase D is in progress.** Steps 00–14 are complete — repository/toolchain and CI; domain kernel
and project format; symmetry/geometry; the particle, structure, transition, energy, lattice, and simulation
models; validation, diagnostics, and model statistics; the Phase C compilation pipeline; the Rust solver
foundation and package reader; and simulation-context initialization. Step 23A (workspace and project lifecycle)
and Step 23B (editing, diagnostics, and recovery) are complete and tagged. The Phase C cross-cutting checkpoint
`XC-04` is accepted. Step 15 — Energy Engine — is next. Do not claim that a feature is implemented until code and
tests exist.

**Adoption profile: standard.** The repository follows the `standard` adoption profile for a solo developer or
small team. The profile and the gate matrix are in [`AGENTS.md` §3.15](#315-rule-homes-and-process-changes) and
[`docs/planning/README.md`](docs/planning/README.md).

**Orchestration:** every engagement is coordinated by one **lead** agent. The human owner interacts only with the
lead and does not spawn role agents directly. The lead spawns the **design** agent, the **dev** (`dev-free` first,
paid `dev` as the fallback), and the **review** agent; it relays their requests and the owner's decisions, handles
general discussion, and routes even small documentation edits to the dev. It does not design, implement, review,
or approve on its own. See [`docs/development/orchestration.md`](docs/development/orchestration.md).

Target stack:

| Component | Technology |
| --- | --- |
| Model builder, compilation, package writer, results API, CLI | C# / .NET 11 |
| Desktop GUI | Avalonia UI + CommunityToolkit.Mvvm, cross-platform |
| Solver | Rust (`mocassin-sim`) |
| Project format | Versioned UTF-8 JSON (`.mocprj`) |
| Simulation package | Immutable SQLite (`.msl`) with versioned MessagePack/Zstd payloads |
| Results store | SQLite (`.msr`) with collected states/hashes, bound to the package |
| Result state | MessagePack document (`.mcs`) |
| Python analysis API | `mocassin-analysis` package, read-only `.mcs`/`.msr` reader, NumPy, pytest |
| Documentation | Zensical + DocFX + rustdoc + mkdocstrings |
| Charts | LiveCharts2 (`LiveChartsCore.SkiaSharpView.Avalonia`) |

## 2. Read this first

Before changing anything, read these in order:

1. [`docs/planning/README.md`](docs/planning/README.md) — index and current step table.
2. [`docs/planning/00-scope-and-principles.md`](docs/planning/00-scope-and-principles.md) — scope, decisions,
   non-goals, success criteria.
3. [`docs/planning/01-target-architecture.md`](docs/planning/01-target-architecture.md) — repository layout,
   project boundaries, runtime architecture.
4. [`docs/planning/02-data-contracts.md`](docs/planning/02-data-contracts.md) — the canonical project/package/
   results/state/job-folder/event contracts.
5. [`docs/planning/03-quality-strategy.md`](docs/planning/03-quality-strategy.md) — test and parity strategy.
6. [`docs/planning/04-roadmap.md`](docs/planning/04-roadmap.md) — phases, dependencies, milestones.
7. [`docs/planning/05-open-decisions.md`](docs/planning/05-open-decisions.md) — resolved, gated, deferred, and
   still-open decisions.
8. [`docs/planning/06-toolchain-requirements.md`](docs/planning/06-toolchain-requirements.md) — required and
   optional tools by language and development phase.
9. [`docs/planning/07-reference-build.md`](docs/planning/07-reference-build.md) — verified original
   solver/model-builder build and reference-data recipe.
10. [`docs/planning/status.md`](docs/planning/status.md) — step and feature status dashboard.
11. [`docs/planning/audits/README.md`](docs/planning/audits/README.md) — independent step-audit process and
    approval gate.
12. [`docs/planning/reviews/README.md`](docs/planning/reviews/README.md) — design-review process and start gate.
13. [`docs/planning/amendments/README.md`](docs/planning/amendments/README.md) — amendment process for design
    changes that emerge during implementation.
14. [`docs/development/orchestration.md`](docs/development/orchestration.md) — lead role, delegation,
    spawning, and handoff rules.
15. [`docs/development/agent-workflow.md`](docs/development/agent-workflow.md) — context, prompt, review, and
    evidence playbook for coding agents.
16. [`docs/development/model-economics.md`](docs/development/model-economics.md) — model tiers, role assignment,
    free-first binding, escalation, and cost guardrails.
17. [`docs/development/design-proportionality.md`](docs/development/design-proportionality.md) — `Full`/`Light`/
    `None` design classes, the review stop rule, and the pre-audit gate.
18. [`docs/development/drift-checks.md`](docs/development/drift-checks.md) — lightweight monthly and
    phase-boundary checks between cross-cutting reviews.
19. [`docs/development/agent-security.md`](docs/development/agent-security.md) — agent threat model,
    permissions, secrets, and prompt injection.
20. [`docs/development/incident-and-rollback.md`](docs/development/incident-and-rollback.md) — rollback,
    hotfix, and postmortem rules.
21. The step page you intend to work on, plus its dependencies.
22. [`docs/planning/appendix-b-parity-matrix.md`](docs/planning/appendix-b-parity-matrix.md) — what “done” means.
23. [`docs/planning/adr/0008-standalone-documentation-toolchain.md`](docs/planning/adr/0008-standalone-documentation-toolchain.md),
   [`docs/planning/adr/0009-contract-manifest-and-contracts-library.md`](docs/planning/adr/0009-contract-manifest-and-contracts-library.md),
   [`docs/planning/adr/0010-canonical-messagepack-signatures.md`](docs/planning/adr/0010-canonical-messagepack-signatures.md),
   [`docs/planning/adr/0011-immutable-package-and-results-store.md`](docs/planning/adr/0011-immutable-package-and-results-store.md),
   [`docs/planning/adr/0012-transport-and-msd-definitions.md`](docs/planning/adr/0012-transport-and-msd-definitions.md),
   [`docs/planning/adr/0013-format-compatibility-and-api-versioning.md`](docs/planning/adr/0013-format-compatibility-and-api-versioning.md),
   [`docs/planning/adr/0014-extension-abi-and-trust-boundary.md`](docs/planning/adr/0014-extension-abi-and-trust-boundary.md),
   and [`docs/planning/steps/step-30-standalone-documentation.md`](docs/planning/steps/step-30-standalone-documentation.md)
   — documentation toolchain, contract/format, and extension decisions.
24. [`GLOSSARY.md`](GLOSSARY.md) — the working ubiquitous language for contributors and agents (process and
    project terms). Scientific terms, notation, and units are in [`docs/glossary/`](docs/glossary/index.md).

The task-level practices are indexed in [`docs/development/index.md`](docs/development/index.md): codebase design,
bug diagnosis, domain language, discovery, prototypes, research, review axes, TDD, and retrospectives. Each links
the normative repository rule it depends on.

The plan is the source of truth. If code and plan disagree, stop and reconcile them rather than silently
following one.

Design scratches live in [`docs/planning/drafts/`](docs/planning/drafts/). They are non-normative and are not an
implementation authorization. A draft becomes a decision only through an ADR, step-page update, amendment, or
reviewed design change.

## 3. Branch and merge rules (mandatory)

These rules override convenience. If you are unsure, stop and ask the user.

### 3.1 Branch model

- `main` is the integration/release branch. Never commit feature work directly to it.
- Create **one step branch** per roadmap step:
  - `step/<NN>-<slug>`, e.g. `step/00-foundation`.
- Create **one feature branch per deliverable** from the step branch:
  - `feature/step-<NN>-<slug>/<feature>`, e.g. `feature/step-00-foundation/dotnet-skeleton`.
  - Bug fixes inside a step: `feature/step-<NN>-<slug>/fix-<short-description>`.
  - Rationale: Git cannot store `step/00-foundation` and `step/00-foundation/<feature>` at the same time
    (directory/file ref conflict). The `feature/` prefix keeps the step grouping without that conflict.
- Use lowercase kebab-case. Keep the feature name short and specific.
- One step is active at a time unless the user explicitly starts parallel work.
- At the start of a step, propose the complete feature-branch list for that step and ask the user to approve
  it once. That approval covers every feature in the list; individual features are implemented and
  squash-merged without a further approval.
- Short-lived maintenance branches that are not part of a step use `fix/<slug>` or `docs/<slug>` from
  `main`; they are merged into `main` with `--no-ff` after approval.

### 3.2 Starting work

Before the first numbered item, complete the pre-implementation design review in section 3.9 and obtain a
`Ready for implementation` verdict. Do not create the step branch or write implementation code before that.

1. Propose the feature-branch list and get one approval for the step breakdown. This is the only
   feature-level approval; individual features are squash-merged without a further approval.
2. Create the step branch from current `main`:
   ```sh
   git switch main
   git pull --ff-only
   git switch -c step/00-foundation
   git push -u origin step/00-foundation
   ```
3. Create each feature branch from the step branch:
   ```sh
   git switch step/00-foundation
   git switch -c feature/step-00-foundation/dotnet-skeleton
   ```
4. All work commits for that feature belong on the feature branch.
5. Push every feature branch to `origin` when it is created. Push the step branch at step start, at closeout,
   and when a remote backup or handoff is needed; do not push it for every feature squash.
6. Feature branches are validated **locally** with `tools/verify.sh`; they do not trigger CI. Run the relevant
   local gates and report the exact commands and results.
7. CI is intentionally minimal. Automatic CI runs only on `main` after a step merge, on Ubuntu, with path
   filters so only affected jobs run. The step closeout run is a single manual `CI` dispatch. There are no
   per-feature or per-push step-branch runs. See section 3.13.
8. If `main` moves while a step is active, merge `main` into the step branch when needed. Do not merge it on
   every main change just to trigger validation. Do not rebase a shared step branch or rewrite its history.

### 3.3 Feature integration

- A feature branch is integrated into its step branch **only with a squash merge**.
- Feature squash merges do **not** require a separate approval. The step-start feature-list approval covers
  every feature in the step.
- After the local gates pass, squash-merge the feature and record in the step page:
  - feature branch name and squash commit;
  - exact local test commands/results;
  - confirmation that no per-feature CI run was triggered;
  - a short summary of the diff;
  - any known limitations or follow-up work.
- Feature integration uses local squash merges. Do not open a pull request unless the user asks for one.
- Then:
  ```sh
  git switch step/00-foundation
  git merge --squash feature/step-00-foundation/dotnet-skeleton
  git commit -m "step-00: add .NET solution skeleton"
  git branch -D feature/step-00-foundation/dotnet-skeleton
  git push origin --delete feature/step-00-foundation/dotnet-skeleton
  ```
- Feature integration is validated locally before the squash. Do not start the next feature until
  `tools/verify.sh` passes for the affected area. If it fails, fix it through another feature or
  `feature/step-00-foundation/fix-<short-description>` branch and squash it. Do not rely on CI to catch
  feature-level failures.
- Push the step branch at step start, at closeout, and when a remote backup or handoff is needed. A push does
  not trigger CI.
- Never merge a feature branch directly into `main`.
- Do not commit feature work directly to the step branch. The step branch receives only squash commits,
  merges from `main`, and closeout changes.
- If the user rejects or abandons a feature, delete its local and remote branch after confirmation.

### 3.4 Step completion

1. Use a final closeout feature branch for:
   - step page status, deviations, and links;
   - parity-matrix updates;
   - documentation updates;
   - any final acceptance fixes.
2. Run the step's full acceptance criteria and local quality gates (`./tools/verify.sh all --require`) on the
   step branch.
3. Merge the current `main` into the step branch so the audit covers the integration tree, not only the step
   branch in isolation. Dispatch the single closeout CI run on the step branch: `CI` workflow, Ubuntu,
   path-filtered. Record the run URL or ID and the full `head_sha` in the audit record and closeout evidence. Do
   not run CI after each feature; the closeout run is the only step-branch CI run.
4. If an emergent design change is required, integrate a step amendment (section 3.11) before the audit freeze.
   The audit then covers the combined solution. Re-run the closeout CI once after the final amendment is
   integrated.
5. Fill `docs/planning/closeout/step-NN.json` from [`_template.json`](docs/planning/closeout/_template.json) and run
   `python tools/check_closeout_evidence.py docs/planning/closeout/step-NN.json`. The evidence must bind the
   audited commit, the closeout CI `head_sha`, the `main` SHA at freeze, and the merge candidate. If `main` moves
   after the freeze, apply the **merge-tree rule**: merge the new `main`, run the full local gate, dispatch a
   replacement closeout CI run, and re-audit; or record an equivalent merge-tree CI run for the final merge
   candidate. Never merge an untested combination.
6. Complete the independent audit in section 3.10. The step branch must be frozen at the audited commit; after
   a passing audit, the review agent's record commit and the dev's resolution/status commit are the only
   permitted later commits.
7. Only after the audit passes, **stop and ask for explicit permission before merging the step into `main`.**
   The approval request must include the cumulative approval report in section 3.12.
8. After approval:
   ```sh
   git switch main
   git pull --ff-only
   git merge --no-ff step/00-foundation -m "step-00: repository, toolchain, and CI"
   git push origin main
   # wait for the automatic main CI run to pass
   git tag -a step-00 -m "step-00: repository, toolchain, and CI"
   git push origin step-00
   ```
9. Wait for the automatic `main` CI run before tagging. If it fails, fix forward or revert and re-run; never tag
   a red `main`.
10. Delete the step branch locally and remotely after the merge and tag.
11. Do not use fast-forward or squash for the step-to-main merge unless the user explicitly requests it.

### 3.5 Out-of-step changes

Urgent fixes and planning/documentation corrections that are not part of the active step use a short-lived
maintenance branch from `main`:

```sh
git switch main
git pull --ff-only
git switch -c fix/<short-description>    # or docs/<short-description>
```

After implementation and tests:

1. Push the branch and report the result.
2. Ask for explicit approval.
3. Merge into `main` with `--no-ff`, push, and delete the branch locally and remotely.
4. Merge the updated `main` into the active step branch before continuing step work.

### 3.6 General rules

- Do not push to `main` without explicit permission for that push.
- Do not force-push, rewrite published history, or delete another contributor's branch.
- Do not open a pull request unless the user asks.
- When a task or feature is finished, report the branch name, commit hash(es), and test results. A feature
  inside an approved step is squash-merged without a separate approval; a maintenance branch or a step-to-main
  merge still needs the approval required by its gate.
- If the user authorizes a merge, follow their instruction exactly; otherwise leave the branch unmerged.
- Follow the [agent security](docs/development/agent-security.md) rules for permissions, secrets, prompt
  injection, and high-risk operations.
- If a bad change reaches `main` or a release, follow the
  [incident and rollback](docs/development/incident-and-rollback.md) rules.

### 3.7 Git tags

Tags are immutable checkpoints for completed steps, roadmap milestones, and releases. They do not replace the
mutable status dashboard.

| Tag | Created when | Points to |
| --- | --- | --- |
| `planning-complete` | The implementation plan is accepted and frozen | The final planning commit on `main` |
| `development-ready` | Development workflow and agent playbook are accepted | The final development-readiness commit on `main` |
| `step-<NN>` | A step is merged into `main` | The step's `--no-ff` merge commit |
| `milestone-m<N>` | A roadmap milestone is reached | The commit that satisfies the milestone |
| `v<major>.<minor>.<patch>` | A release or release candidate is published | The release commit |

Rules:

- Tag only commits already on `main`.
- Create tags only after the corresponding step/milestone/release approval. Tag creation and push are part of
  that approval; never create or push a tag without it.
- Use annotated tags (`git tag -a`). Sign with `-s` when a signing key is configured.
- Never move or delete a published tag. Use a suffixed tag such as `step-00.1` for a follow-up checkpoint.
- Do not create tags automatically in CI, and do not tag feature branches or step branches.
- Step and milestone tags are internal history markers, not package versions. Release builds may use
  `git describe --tags --match 'v*'` for versioning.
- `step-NN`, `milestone-mN`, and release tags are required. `planning-complete` and `development-ready` are
  optional one-time markers; do not create them retroactively.

Tag message template:

```text
step-00: repository, toolchain, and CI

merge: <merge-commit-sha>
date: <ISO-8601 UTC>
ci: <run-url-or-id>
features: <comma-separated feature names>
status: docs/planning/status.md#step-00
```

Milestone tags use the same fields with `milestone-m0` and the milestone name. Release tags are created only
after the Step 31 release gates pass.

### 3.8 Agent roles and write scopes

The workflow has four standing roles, one escalation role, and one owner. Each role edits only the artifacts it
owns. There is no scribe and no transcription step.

| Role | May edit | May commit | Must not edit |
| --- | --- | --- | --- |
| Lead | Nothing. Handoff cards and status notes are routed to the dev. | Documentation-only `docs/<slug>` commits with `[skip ci]` are routed to the dev, not made by the lead. | Code, tests, configuration, contracts, generated artifacts, design or process rules, ADRs, step pages, review/audit/cross-cutting records, verdicts, waivers, merges, tags, `main`; any file at all under the harness binding |
| Design agent | Design and planning docs; the review record's request, scope, checklists, and resolutions | Documentation-only commits on the design branch | Code, tests, configuration, contracts, audit or design verdicts, step status, merges, tags |
| Dev (`dev-free` or `dev`) | Code, tests, configuration, contracts, step page, status dashboard, parity matrix; the audit/review record's request, scope, checklists, and resolutions | Feature-branch commits; documentation-only record and status commits | The review agent's findings, evidence, or verdict text; merges and tags without approval |
| Review agent (design review or audit) | Only the assigned review or audit record: its own metadata, checklist completion marks (not item text), evidence, findings, and verdict | At most one documentation-only record commit per round, with `[skip ci]` | Everything else, including code, tests, configuration, design docs, step page, status dashboard, parity matrix, request scope, checklist item text, resolutions, merges, tags, and pushes to `main` |
| Senior consultant (escalation only) | Only the assigned senior record under `docs/planning/reviews/senior/`: its own metadata, assignment check, answer or decline, evidence, and limitations | At most one documentation-only record commit per consultation, with `[skip ci]` | Everything else, including code, tests, configuration, design docs, the parent record, findings, verdicts, waivers, merges, tags, and pushes to `main` |
| Human owner | Approve gates, merges, and tags | — | — |

Orchestration rule: the lead is the human owner's only direct interaction point and spawns every role agent. The
owner does not spawn role agents directly, and role agents do not spawn each other. The lead never designs,
implements, reviews, approves, merges, or tags on its own. `dev-free` and `dev` are one dev role with two model
bindings; the lead spawns `dev-free` first and re-spawns the same assignment on `dev` only on a rate limit, a
degraded model, or unavailability. The normative page is
[`docs/development/orchestration.md`](docs/development/orchestration.md).

Review-agent rules:

- A review agent must be a fresh context that did not author or implement the reviewed work. It is spawned by the
  lead, which relays the request from the design agent or dev.
- The lead is the only spawner and must not be the review agent. It provides only the frozen commit, the request
  record, repository links, and the required commands/scope. It must not supply a desired verdict or replace
  repository reading with a summary.
- The human owner approves the final verdict and gate. The lead must not mark a verdict, waive findings, merge,
  tag, or push `main`. A re-review requires a new commit with fixes; do not re-spawn until a preferred verdict
  appears.
- The review agent states in its record that it is a fresh context with no shared conversation with the design
  agent, dev, or lead.
- The review agent writes findings and verdict directly into the durable record. No handoff file, transcription,
  or scribe is used.
- The review agent appends a new round and must not alter earlier rounds or review-authored text.
- The review agent marks the request checklists to record what was actually reviewed. It may change checkbox
  marks and add short inline verification notes, but not the item text; the request owner owns the items.
- The review agent records the exact commands, results, and files behind the verdict in the record's `Evidence`
  section, so the human owner can spot-check it.
- The review agent must not fix findings. If it edits code, tests, configuration, or design docs, it becomes a dev
  and a fresh review agent is required.
- The review agent's record commit must touch exactly one file: the review or audit record. The reviewed or
  audited commit is the parent of that commit. A record commit that touches any other file is invalid, and the
  verdict must be re-run by a fresh review agent.
- The dev may add resolutions and update status after the record commit, but must not change the review agent's
  findings, evidence, or verdict. Resolutions belong in a separate section or table.
- The review agent does not merge, tag, push `main`, or delete branches. The human owner still approves the merge.
- The review agent works in a writable worktree of the branch under review. If no writable checkout is available,
  it writes the record and the dev commits it unchanged.
- The record commit uses a documentation-only message such as
  `docs: record step-NN audit Passed [skip ci]`.
- Optional scratch notes may live under `.tmp/`. They are not gate artifacts and are never committed.

**Harness binding.** The six agent bindings are defined in `.opencode/agents/` (`lead`, `design`, `dev-free`,
`dev`, `review`, `senior`), and `opencode.json` makes the lead the `default_agent`. `.opencode/commands/step-start.md`,
`step-closeout.md`, `grill.md`, `handoff.md`, and `retro.md` are thin routers. The binding enforces five rules only:
the lead cannot edit and can spawn only `design`, `dev-free`, `dev`, `review`, and `senior`; the review agent can
edit only `step-*.md`, `policy-*.md`, and `xc-*.md` records and cannot spawn; the senior consultant can edit only
its assigned record under `docs/planning/reviews/senior/` and cannot spawn; the design agent cannot edit code areas.
Shell, pushes, merges, and tags are procedural, so owner approval and branch protection remain the real controls.
See [`docs/development/orchestration.md`](docs/development/orchestration.md) §9 for the full statement, and
[`docs/development/model-economics.md`](docs/development/model-economics.md) for the model tiers.



### 3.9 Pre-implementation design review

Every new step must pass a design review before the step branch is created or implementation starts, except a
step classified `None`. This gate is separate from the post-implementation audit in section 3.10. The design
agent and dev own the design work, the review request, and the resolutions; the lead routes the request and
spawns the review agent; the human owner approves the verdict and start gate.

The **design class** in [`docs/development/design-proportionality.md`](docs/development/design-proportionality.md)
sets the depth and the stop rule:

- **Full** — public API/contract, `.mocprj`/`.msl`/`.msr`/`.mcs` format or schema/version, dependency,
  cross-package interface, data migration, security/concurrency, an irreversible decision, or an architecture
  boundary. Independent review; stop when no Blocker or Major remains.
- **Light** — an internal feature inside an existing boundary. One review-agent pass; Minor and Note findings are
  accepted and do not trigger a re-review. If a Blocker or Major appears, the step is reclassified Full.
- **None** — documentation, typo, test-only change, mechanical refactor, or generated-file refresh. No design
  review; a one-line record in the step page. Under the `standard` profile, `None` is pre-authorized for these
  categories and needs no per-item owner waiver.

A Full review stops when no Blocker or Major remains. Minor and Note findings are recorded and accepted or
deferred; they do not justify another round. A third Full round needs an owner decision and a recorded reason.
Before the audit request, the dev runs the pre-audit gate in
[`docs/development/design-proportionality.md`](docs/development/design-proportionality.md) §3: bookkeeping is
checked there, not in another design-review round.

1. The design agent and dev finish the step's design decisions and update the step page, data contracts,
   architecture, quality strategy, open decisions, parity matrix, and related docs. They apply those edits
   directly. The step page records the design class.
2. The docs are frozen at a specific commit and the dev creates or updates
   `docs/planning/reviews/step-NN.md` using
   [`docs/planning/reviews/step-design-review-template.md`](docs/planning/reviews/step-design-review-template.md).
   Set the design-review status to `Ready for review` and record the reviewed commit and design class.
3. The dev asks the lead to spawn a fresh review agent with no implementation context. The review agent reads the
   step page, design decisions, dependency contracts, architecture, data contracts, ADRs, quality strategy, open
   decisions, parity matrix, licensing records, and audit plan. It must not have authored or implemented the
   design. For a Light step it answers the five design questions only; for a Full step it also checks the full
   checklist.
4. The review agent writes the evidence, findings, and verdict directly into
   `docs/planning/reviews/step-NN.md` and commits only that file with a documentation-only message and
   `[skip ci]`. The reviewed commit is the parent of the record commit. It does not merge, tag, push `main`, or
   change any other file. If it cannot commit, it writes the record and the dev commits it unchanged.
5. The review agent's write scope is defined in section 3.8. It may edit only its own metadata, evidence,
   findings, and verdict. It must not edit the request scope, checklists, resolutions, design docs, step page,
   status dashboard, parity matrix, code, tests, or configuration. It labels every finding on the **Spec** or
   **Standards** review axis per
   [`docs/development/review-axes.md`](docs/development/review-axes.md); one record carries one verdict.
6. If the verdict is `Changes required`, the design agent and dev update the design docs directly, the dev adds
   the resolutions and marks the review `Re-review required`, and the lead spawns a fresh review agent for the
   new commit. The new round is appended; earlier rounds are not overwritten. Do not open a new round for Minor
   or Note findings alone.

   After two completed standard rounds of the same design review each ended blocked and at least one Blocker or
   Major is still open, the lead may request **one** senior focused consultation before a third standard round.
   The same escalation is available to a step audit, a cross-cutting review, and a policy-update review. The
   trigger, request contract, senior rules, and advisory effect are in
   [`docs/development/senior-consultation.md`](docs/development/senior-consultation.md). The answer is advisory,
   the human owner decides, and a fresh standard round at the new commit remains the gate; a senior consultation
   is not a round and not a verdict.
7. Only a `Ready for implementation` verdict, or a `Ready with notes` verdict the owner accepts, permits the human
   owner to approve starting the step. The lead presents the verdict and the feature list and stops for approval;
   it does not approve them. The cumulative report in section 3.12 is for a merge, a tag, or a Blocker/Major
   waiver, not for this start gate. The step branch is then created from the reviewed `main` commit.
8. A `None` step records the one-line design decision in the step page, skips the review record, and proceeds
   under the normal branch, local-gate, and audit rules. The audit still covers it.
9. Step 00 and Step 01 are recorded as `Waived — process introduced after completion`; no retrospective review is
   required.
10. The post-implementation audit in section 3.10 remains mandatory.

Design-review statuses and finding severities are defined in
[`docs/planning/reviews/README.md`](docs/planning/reviews/README.md). Design-review records are planning
records and are excluded from the published site.

### 3.10 Step audit gate

The dev's self-check is not an independent audit. Before a step is merged into `main`, a fresh, out-of-context
review agent must review the frozen step branch. The dev owns the closeout, the audit request, and the
resolutions; the lead spawns the review agent and relays the verdict; the human owner approves the merge and tag.

1. The dev finishes all features and the closeout feature, updates the step page, status dashboard, parity
   matrix, and feature log, merges current `main`, runs `tools/verify.sh all --require`, and gets the single
   closeout CI run green.
2. The dev runs the **pre-audit gate** in
   [`docs/development/design-proportionality.md`](docs/development/design-proportionality.md) §3: map every
   ticked acceptance criterion to its evidence, confirm that the step page, status dashboard, feature log, and
   closeout evidence agree, confirm the structure/link/secret/licence checks, and record the design class and
   any waiver. Record the result in the step page. An audit request without this is incomplete.
3. The dev creates `docs/planning/audits/step-NN.md` on demand from
   [`docs/planning/audits/step-audit-template.md`](docs/planning/audits/step-audit-template.md), links the
   closeout evidence file (which holds the audited commit, closeout CI run and `head_sha`, `main` SHA at freeze,
   and merge candidate), and sets the status to `Ready for audit`. Do not pre-seed audit records for steps that
   have not started.
4. The step branch is frozen at the audited commit. If `main` moves after the freeze, apply the merge-tree rule
   in section 3.4 and the closeout evidence must show that the audited commit, closeout CI, and merge candidate
   remain consistent. After a passing audit, two documentation-only commits may follow: the review agent's
   record commit and the dev's resolution/status commit. Neither may change code, tests, configuration, or the
   review agent's text. If fixes are needed, the dev uses a new feature branch and the new commit is re-audited.
5. The dev asks the lead to spawn a fresh review agent. It reads `AGENTS.md`, the step page, the audit record,
   the status dashboard, and relevant ADRs/contracts, then runs the required builds/tests and reviews the diff.
   It must not have implemented the step. Design and audit use the same `review` agent and model tier.
6. The review agent writes the evidence, findings, and verdict directly into
   `docs/planning/audits/step-NN.md` and commits only that file with a documentation-only message and
   `[skip ci]`. The audited commit is the parent of the record commit. It does not merge, tag, push `main`, or
   change any other file. If it cannot commit, it writes the record and the dev commits it unchanged.
7. The review agent's write scope is defined in section 3.8. It may edit only its own metadata, evidence,
   findings, and verdict. It must not edit the scope, checklists, resolutions, code, tests, configuration, step
   page, status dashboard, or parity matrix. It labels every finding on the **Spec** or **Standards** review axis
   per [`docs/development/review-axes.md`](docs/development/review-axes.md); one record carries one verdict.
8. If fixes are needed, the dev adds the resolutions, fixes the findings on a new feature branch, squash-merges
   into the step branch, merges current `main`, updates the closeout evidence, dispatches one new closeout CI
   run, and sets the audit status to `Re-audit required`.
9. The lead spawns a fresh review agent for the new commit; it re-audits and appends a new round to the record.
   The same review agent may continue if it stayed within the review write scope; otherwise start a fresh
   context. Keep every earlier round in the record.

   After two completed standard audit rounds each ended blocked and at least one Blocker or Major is still open,
   the lead may request **one** senior focused consultation before a third standard round. The same escalation is
   available to a design review, a cross-cutting review, and a policy-update review. The trigger, request
   contract, senior rules, and advisory effect are in
   [`docs/development/senior-consultation.md`](docs/development/senior-consultation.md). The answer is advisory,
   the human owner decides, and a fresh standard audit round at the new commit remains the gate; a senior
   consultation is not an audit round and not a verdict.
10. Only a `Passed` audit, or a `Passed with notes` audit the owner accepts, permits the step-to-main approval
    request. The lead presents the request with the cumulative approval report in section 3.12, covering every
    audit and review round, every finding and resolution, every accepted note or waiver, and the residual risk,
    but does not approve it. The audit result is evidence, not merge authorization; the human owner still
    approves the merge.
11. After merge, wait for the automatic `main` CI run. Tag `step-NN` only when it is green; record the result
    and delete the step branch. Never tag a red `main`.

The audit record records the origin of each finding as **design**, **implementation**, **evidence**, or
**documentation**, so the phase-boundary metric in
[`docs/development/design-proportionality.md`](docs/development/design-proportionality.md) §4 can compare the
distribution with the design effort spent.

Platform CI (Windows and macOS) is manual and runs only when the step first touches platform-specific code.
When it is not run, record that as an accepted risk in the audit record. See section 3.13 and
[`docs/planning/ci-policy.md`](docs/planning/ci-policy.md).

The audited solution includes every amendment integrated before the freeze. A post-merge amendment needs its
own audit and tag under section 3.11 and must never move the existing `step-NN` tag.

Audit status values and finding severities are defined in
[`docs/planning/audits/README.md`](docs/planning/audits/README.md). Audit reports are planning records and are
excluded from the published site.

### 3.11 Step amendments

An amendment captures a design change that emerges during implementation without reopening a frozen step. It
is planned, reviewed, integrated into the parent step branch, and audited as part of the parent step.

1. Record it as `step-NN-amend-M` using
   [`docs/planning/amendments/step-amendment-template.md`](docs/planning/amendments/step-amendment-template.md).
2. Keep the parent step active. Do not add the amendment to a frozen or already-audited step branch.
3. Run a focused design review under section 3.9 and record it in
   `docs/planning/reviews/step-NN-amend-M.md`. The parent review record links to it.
4. Implement on `feature/step-NN-<step-slug>/amend-M-<slug>` from the parent step branch. A large amendment may
   use `step/NN-amend-M-<slug>` with the normal feature rules.
5. Integrate the amendment into the parent step branch before the parent audit freeze, using the normal
   feature squash process.
6. Update the parent step page, amendment page, audit request, and design review record with the combined
   commit.
7. Freeze the combined step branch. The audit in section 3.10 covers the parent step and every integrated
   amendment at that commit. Do not create a separate audit record or tag for an integrated amendment.
8. If the parent step is already merged or tagged, do not move the tag. Use a maintenance branch from `main`,
   audit the amendment separately, and create a new tag such as `step-NN.1` only with owner approval.

Amendment statuses and the amendment index are in
[`docs/planning/amendments/README.md`](docs/planning/amendments/README.md).

### 3.12 Approval report

Before the human owner approves a merge, a tag, or a waiver of a Blocker or Major finding, the agent requesting
approval must present a **cumulative approval summary** covering the entire history. The lead assembles it from
the durable records and relays it; it does not approve it. Accepting a design-review or audit verdict on its own
does not need the summary: the record's round table is the reference. Feature squash merges are not approval
requests; their evidence is recorded in the step page. The summary goes in the approval request itself; linking
the durable record is not sufficient.

Use this order. It matches the upstream approval-report template where that reduces cost; the MOCASSIN
additions are the step and closeout fields.

```text
Decision requested:
Branch / frozen commit:
Final verdict:

Final state:
  - closeout CI:
  - main CI:
  - docs build:
  - policy/security gate:

Rounds:
  1. <date> — <reviewed/audited commit> — <review agent, lead> — <verdict>
  2. ...

Findings and resolutions:
  ID | Severity | Origin | Issue | Evidence | Resolution / fix commit | Status
  ...

Accepted notes and waivers:
  - <ID or note> — accepted by <owner> — rationale — follow-up

Residual risk:
  - <open note / deferred item / known limitation> — owner — target

Diff summary:
Tests and commands:
Docs/status updated:

Gate statement:
  No Blocker or Major finding remains unclosed.
  OR
  The owner is asked to waive: <exact list>.

Approval requested for:
  <verdict / waiver / merge / tag>
```

The summary must contain:

1. **Requested decision** — the exact verdict, waiver, merge, or tag being requested, and the frozen commit.
2. **Final state** — branch, reviewed/audited commit, closeout and `main` CI results, documentation-build result,
   and policy/security gate result.
3. **Every round** — for each design-review and audit round: round number, date, reviewed or audited commit,
   review agent identity and lead, and verdict.
4. **Every finding** — finding ID, severity, origin (design, implementation, evidence, or documentation), issue,
   evidence, resolution or fix, resolving commit, and current status. Include findings from every round,
   including those already fixed.
5. **Accepted notes and waivers** — each note or waiver, who accepted it, and its rationale or follow-up.
6. **Residual risk** — open notes, deferred items, known limitations, and follow-up owners.
7. **Gate statement** — an explicit statement that no Blocker or Major finding remains unclosed, or the complete
   list of exceptions the human owner is being asked to waive.

Rules:

- Never omit earlier rounds or findings because they were fixed; the fixed history is the point.
- Never report only the latest round or only the findings that remain open.
- Do not rewrite earlier records to fit the summary; derive the summary from the durable record.
- If a round is incomplete or lacks a verdict or resolution, say so instead of omitting it.
- The summary is evidence for the human owner, not an approval. The human owner still decides.
- A merge/tag request always includes it. A verdict acceptance, a feature squash, or the design-review start gate
  does not.



### 3.13 CI cost policy

The project uses the GitHub Free private-repository allowance of 2,000 billable minutes per month. CI is a
confirmation gate, not the development loop. The full rationale and cost data are in
[`docs/planning/ci-policy.md`](docs/planning/ci-policy.md).

| Run | Trigger | Runner | Scope |
| --- | --- | --- | --- |
| Main merge | automatic `push` to `main` | Ubuntu | Path-filtered jobs, including process/binding checks |
| Step closeout | one manual `workflow_dispatch` per step | Ubuntu | Path-filtered jobs, including process/binding checks |
| Platform gate | manual `workflow_dispatch` with `platforms` | Windows/macOS | On demand only |
| Documentation | manual `docs.yml` | Ubuntu | Full docs build |
| Benchmark | manual `benchmark.yml` | Ubuntu | Selected benchmark |
| Nightly | manual placeholder; no schedule until real suites exist | — | — |

Rules:

- There are no per-feature and no per-push step-branch CI runs. Validate locally with `tools/verify.sh`.
- The closeout run is the only step-branch CI run. The main merge runs once more after the `--no-ff` merge.
- Automatic CI is Ubuntu-only. Windows and macOS are manual. Promote Windows to automatic closeout/main when
  platform-specific code first appears; keep macOS manual until there is macOS-specific behaviour to validate.
- Path filters must ensure that only affected jobs run. A change to `.github/workflows/ci.yml` forces all jobs.
- The `Process and binding checks` job runs when process-relevant paths change: `.opencode/**`, `AGENTS.md`,
  `GLOSSARY.md`, `opencode.json`, `docs/development/**`, `docs/planning/reviews/**`, `docs/planning/audits/**`,
  `docs/planning/README.md`, `docs/planning/04-roadmap.md`, `docs/planning/ci-policy.md`,
  `docs/planning/status.md`, `docs/planning/maintenance/**`, `docs/planning/drift-checks/**`,
  `docs/planning/closeout/**`, `docs/planning/incidents/**`, `docs/planning/research/**`,
  `docs/planning/prototypes/**`, `docs/planning/retrospectives/**`, `zensical.toml`,
  `tools/check_process_consistency.py`, `tools/check_closeout_evidence.py`, `tools/check_docs_links.py`,
  `tools/check_doc_math.py`, `tools/verify.sh`, and `tools/tests/**`. A `ci.yml` change enables it together with
  every other job. It is Ubuntu-only, installs no dependencies, builds neither .NET nor Rust, and runs the
  process checker, the documentation link and math checks, and the `tools/tests` suite with the same discovery
  root as `tools/verify.sh`.
- Documentation is validated locally with `zensical build --strict` (and the other docs commands). The
  separate `docs.yml` workflow is manual until the documentation is mature enough to justify automatic runs.
- Keep Nightly manual-only; do not add a schedule until real long-running suites exist. If a schedule is later introduced, run it weekly on Ubuntu.
- Target at most 300 billable minutes per month. If usage passes 50% of the allowance before mid-month, switch
  to closeout-only CI and ask the human owner before adding any run or job.
- Use `[skip ci]` for docs-only commits and merges.
- Never tag a red `main`; wait for the main merge CI before creating `step-NN`.

Platform-specific triggers: P/Invoke, `LibraryImport`, `System.Runtime.InteropServices`, native libraries or
RID-specific packages, path/case/locking logic, line-ending or encoding-sensitive handling, GUI projects, Rust
`cfg(windows)` or `cfg(target_os = "...")`, and installers or packaging.

### 3.14 Cross-cutting phase reviews

After the last step of each phase is merged and tagged, the phase closes with **one combined repository-wide
review** with three sections:

1. **Policies and planning** — agent instructions, workflows, CI, code structure, process records, and
   phase status.
2. **Architecture and design status** — architecture, contracts, design status, cross-step interfaces, and
   original-design fidelity: the intended corrections to the original, the good/clever original design aspects
   that were ported, and any regression over the original.
3. **Documentation status** — coverage, accuracy, navigation, API docs, and the publication boundary.

The owner may still ask for the three separate reviews when a phase is unusually risky; that is an explicit
choice, recorded in the checkpoint.

The architecture and design review must explicitly answer the original-design fidelity questions and record
concrete evidence. A missing intended correction, a lost original strength, or an unrecorded regression is a
finding.

This checkpoint is a **quality gate**. The next phase's first step design review may not start until the
combined verdict is `Passed` or the human owner accepts `Passed with notes`, or the owner waives the review with a
recorded rationale.

1. Freeze the checkpoint after the phase's last step is merged and tagged. Record the checkpoint commit in each
   review.
2. The lead coordinates each review: it requests the frozen request and record from the design agent or dev,
   spawns a fresh independent review agent, and relays findings and decisions between the review agent, the
   remediating role, and the owner. The design agent or dev prepares the request and remediates findings on a
   maintenance branch, adds resolutions without altering review text, records the new commit, and asks the lead
   for re-review. The lead must not fix findings, request a particular verdict, or edit review evidence,
   findings, or verdict.
3. The review agent is a fresh context, reads the repository at the frozen commit, writes only its assigned
   record, may check checklists and add inline notes, and commits only that file with `[skip ci]`. It must not fix
   findings, merge, tag, push `main`, or delete branches.
4. Repeat until the review agent no longer identifies Blocker/Major issues. Use a fresh review agent for the new
   round unless the same one stayed read-only and outside the remediation.
5. The human owner approves each final verdict and the phase gate.

Run the lightweight [drift checks](docs/development/drift-checks.md) at each milestone, or monthly when the
project has no milestone; event-driven checks after a process, CI, architecture, or status change still apply.
They catch stale instructions, status drift, and broken links before the checkpoint. Create cross-cutting review
records at the phase boundary, not for distant phases; the Phase B records are historical.

Review records live under [`docs/planning/reviews/cross-cutting/`](docs/planning/reviews/cross-cutting/).
Review IDs are globally sequential (`XC-NN`) and are never reused. From Phase C, each phase gets **one** combined
review ID; the three-review set stays available when the owner asks for it. The registry and status values are in
the cross-cutting README. Phase A is waived because the process was introduced after it completed, and the Phase
B records are historical.

Findings are remediated through maintenance branches, ADRs, amendments, or step-page updates. Never move an
existing tag. A cross-cutting review never creates a step branch or step tag, and it does not replace step
design reviews, step audits, or Step 29 parity validation.

### 3.15 Rule homes and process changes

Each rule has one normative home. Summaries link to it; do not copy it into a second page that can drift.

| Topic | Normative home |
| --- | --- |
| Branch model, feature integration, step completion, tags, roles, approval report | This section, `AGENTS.md` §3 |
| Adoption profile and gate matrix | This section, `AGENTS.md` §3.15, and `docs/planning/README.md` |
| Lead role and delegation | `docs/development/orchestration.md` |
| Model tiers, role assignment, free-first binding, escalation | `docs/development/model-economics.md` |
| Design classes, the policy-update class trigger, review stop rule, pre-audit gate, finding-origin metric | `docs/development/design-proportionality.md` |
| Senior escalation trigger, request contract, senior rules, effect | `docs/development/senior-consultation.md` |
| Review axes (Standards and Spec) | `docs/development/review-axes.md` |
| Task disciplines (codebase design, bug diagnosis, domain language, discovery, prototypes, research, TDD, retrospectives) | `docs/development/index.md` and each discipline page |
| Agent output contract | §17 |
| Policy-update review records | `docs/planning/reviews/README.md` §9.1 |
| Design review and audit records | `docs/planning/reviews/README.md` and `docs/planning/audits/README.md` |
| Cross-cutting phase reviews | `docs/planning/reviews/cross-cutting/README.md` |
| Step sizing, complexity class, and split triggers | `docs/planning/04-roadmap.md` §3.1 |
| Drift checks | `docs/development/drift-checks.md` |
| Agent security | `docs/development/agent-security.md` |
| Incident and rollback | `docs/development/incident-and-rollback.md` |
| CI cost and gates | §3.13 and `docs/planning/ci-policy.md` |
| OpenCode agent and command binding | `docs/development/orchestration.md` §9 |
| Testing and quality | `docs/planning/03-quality-strategy.md` |
| Code structure | `docs/development/code-structure.md` |
| Closeout evidence | `docs/planning/closeout/README.md` |

Design reviews use `Changes required`; audits and cross-cutting reviews use `Fixes required` for the equivalent
failure state. Each family keeps its own vocabulary; the dashboard columns use the family's terms.

**Effective from Step 11.** The combined phase review, the reduced approval-report scope, the canonical
closeout evidence, the optional planning tags, and the reduced scheduled drift check apply from Step 11 onwards.
Records, pages, evidence files, and tags for Steps 00–10 are historical and are not rewritten.

**Effective from Step 15.** The `lead`/`design`/`dev-free`/`dev`/`review` role names, the `standard` adoption
profile, model economics, the `Full`/`Light`/`None` design classes, the review stop rule, the pre-audit gate, and
the `None` pre-authorization apply from Step 15 and to any amendment started after 2026-11-22. Steps 00–14, their
reviews, audits, closeouts, and tags are historical and are not rewritten; historical records keep the role names
in force when they were written. The `Design class` field is added to Step 15 onward and to new amendment pages,
not backfilled into completed step pages.

**Effective from 2026-11-27.** The task disciplines, review axes, the `senior` escalation role, the agent output
contract in §17, and the `default`/`senior` tier names apply from the 2026-11-27 policy update. Historical records,
including the earlier tier wording in `docs/planning/maintenance/` and the `status.md` note that records it, are
not rewritten; they keep the vocabulary in force when they were written.

Changing the process:

1. Open a `docs/<slug>` branch from `main`.
2. Explain the problem and the proposed rule.
3. Update the normative page and every dependent template or summary in the same change.
4. Run `./tools/verify.sh docs --require` and `./tools/verify.sh policy --require`.
5. Get owner approval. Do not silently change the process in the middle of a step; if a mid-step change is
   unavoidable, record it as an amendment-level decision and update the process after the step closes.

## 4. Non-negotiable decisions

These are recorded decisions (see [`docs/planning/adr/`](docs/planning/adr/)). Do not reverse them without an
explicit new ADR and owner approval.

1. **Clean break on legacy formats.** No reader/writer for the original `.mocprj`, `.msl`, or `.mcs` formats.
   Do not add “just a small compatibility shim” without a new decision.
2. **MIT licence for the new implementation.** The original project remains GPL-3.0-or-later. Do not copy
   original source code, documentation, or database content into MIT-licensed files. See `NOTICE.md` and
   [ADR 0007](docs/planning/adr/0007-mit-license.md).
3. **Rust solver process boundary.** The solver consumes only the compiled `.msl` package and CLI arguments.
   It never reads project JSON and never writes `.msl`/`.msr`. .NET never calls solver code in-process for production
   runs.
4. **Statistical parity, not bit-exact trajectories.** Use ChaCha12 and SHA-256-derived seeds. Do not reintroduce
   PCG32 trajectory compatibility.
5. **Avalonia redesign, not a WPF port.** Preserve scientific concepts and workflows; do not port WPF controls,
   service-locator patterns, or `System.Windows.*` types.
6. **LiveCharts2 for charts.** Do not use ScottPlot.
7. **Phased full parity.** A working KMC/MMC core and model editors come before 3D, MMCFE, advanced analytics,
   and HPC polish. Do not build later-phase features while a blocking earlier step is unfinished.
8. **No silent scientific corrections.** If the original behaviour is ambiguous or wrong, document the chosen
   interpretation in the step page and add a test. Do not “fix” physics without evidence.
9. **Documentation toolchain.** Narrative docs use Zensical; .NET API uses DocFX; Rust API uses rustdoc;
   Python API uses mkdocstrings; publish one site with `/api/dotnet/`, `/api/rust/`, and `/api/python/`.
   `docs/planning/` is not published.
10. **Python API is read-only and minimal.** `mocassin-analysis` reads `.mcs` files and provides basic
    multi-file helpers; it does not write simulations or duplicate the full .NET evaluation framework.

## 5. Repository layout

Current repository:

```text
mocassin/
├── AGENTS.md
├── opencode.json           # default agent: the lead
├── .opencode/              # role agents and the two step commands
├── README.md
├── LICENSE                 # MIT
├── NOTICE.md               # original GPL attribution + MIT boundary
├── zensical.toml           # target: Zensical site config
├── docs/                   # target: published narrative docs (Zensical)
│   ├── planning/           # internal plan, 32 step pages, ADRs, appendices
│   ├── user-guide/
│   ├── physics/
│   ├── tutorials/
│   ├── reference/          # CLI, formats, diagnostics, extension ABI
│   ├── development/        # architecture, code structure, design decisions
│   ├── performance/
│   ├── troubleshooting/
│   ├── migration/
│   ├── examples/
│   ├── glossary/
│   └── api/                # landing + placeholders; generated API copied to site/api at build
├── src/dotnet/             # target: .NET solution
├── src/rust/               # target: Rust workspace
├── src/python/             # target: `mocassin-analysis` read-only .mcs reader
├── contracts/              # target: manifest.json + generated contract checks
├── samples/                # target: ceria, barium zirconate, vehicle, interstitialcy
├── tests/contracts/        # target: cross-language .msl/.msr/.mcs fixtures
├── tests/reference/        # target: statistical parity fixtures/reports
└── tools/hpc/              # target: SLURM/PBS helpers
```

Target .NET dependency direction:

```text
Core <- Symmetry <- Compilation <- Contracts <- Packages <- Cli
                                         └──► Results
App -> App.ViewModels -> Contracts/Packages/Results
App -> App.Views -> App.ViewModels
App -> Rendering
```

Target Rust workspace:

```text
mocassin-cli -> mocassin-solver -> mocassin-state
                    ├── mocassin-rng
                    ├── mocassin-msl
                    └── mocassin-types
mocassin-extension
```

Rules:

- `Mocassin.Core` has no file I/O, SQLite, Avalonia, EF Core, or UI dependency. Project persistence lives in
  `Mocassin.Packages.Projects`.
- `Mocassin.Compilation` is pure and has no I/O.
- `Mocassin.Contracts` owns versioned package/results/state DTOs, job-folder/solver/event contracts, and
  schema constants; it must not depend on `Mocassin.Packages` or `Mocassin.Results`.
- `Mocassin.Packages` owns project JSON, supercells, and immutable `.msl` writing; it depends on
  `Mocassin.Contracts`.
- `Mocassin.Results` owns `.msl`/`.msr`/`.mcs` reading, collection, and evaluation, not simulation or package
  writing. It depends on `Mocassin.Contracts`, not `Mocassin.Packages`.
- `contracts/manifest.json` is the single source for package/results/state/job-folder/event field names, types,
  and versions; generated checks and fixtures must be current.
- View models never reference views or controls.
- Scientific algorithms never live in view models or views.
- The Rust solver core is a library first; the CLI is a thin wrapper.

## 6. Build and test commands

The dedicated local development image is
[`docs/opencode/mocassin.containerfile`](docs/opencode/mocassin.containerfile). It depends only on the
`opencode:base` image from
[`seb-eis/opencode-container`](https://github.com/seb-eis/opencode-container) and does not modify that
repository. The same tools can also be installed directly for development without opencode.

Run the normal local gate with `./tools/verify.sh all`, or the mode for the area you changed. The commands
below are the underlying gates. End-to-end commands that depend on a later step are annotated with that step.

```bash
# .NET
dotnet restore Mocassin.slnx
dotnet build Mocassin.slnx -c Release
dotnet test Mocassin.slnx -c Release
dotnet format Mocassin.slnx --verify-no-changes

# Rust
cargo fmt --manifest-path src/rust/Cargo.toml --all --check
cargo clippy --manifest-path src/rust/Cargo.toml --workspace --all-targets -- -D warnings
cargo test --manifest-path src/rust/Cargo.toml --workspace
cargo build --manifest-path src/rust/Cargo.toml --workspace --release

# End-to-end (validate is available; build/collect/solver commands land in later steps)
dotnet run --project src/dotnet/Mocassin.Cli -- validate samples/ceria/ceria.mocprj
dotnet run --project src/dotnet/Mocassin.Cli -- build samples/ceria/ceria.mocprj -o /tmp/ceria.msl
cargo run --manifest-path src/rust/Cargo.toml -p mocassin-cli -- \
  --db /tmp/ceria.msl --job 1 --io /tmp/Job00001
dotnet run --project src/dotnet/Mocassin.Cli -- collect /tmp/ceria.msl --dir /tmp --results /tmp/ceria.msr

# Python analysis API (package and tests exist; full analysis API lands in Step 22)
python -m pytest src/python/tests
python -m build src/python

# Documentation
zensical build --strict
dotnet tool restore
dotnet docfx tools/docfx/docfx.json -o artifacts/docfx
cargo doc --manifest-path src/rust/Cargo.toml --workspace --no-deps
```

Quality gates for any change:

- No new compiler/clippy warnings; warnings are errors.
- `dotnet format` and `cargo fmt --check` clean.
- Tests added/updated for the change.
- Contract fixtures, canonical signature vectors, and previous-major compatibility fixtures updated if a format
  changed.
- Performance benchmarks run if the change touches a hot path.
- Step page, parity matrix, and relevant documentation pages updated.
- Documentation builds strictly and API docs have no missing public items.

## 7. Working on a step

The 32 steps are in [`docs/planning/steps/`](docs/planning/steps/). Use them as work packages, not suggestions.

The lead routes the work and spawns the role agents; the dev performs the numbered implementation steps below.
The lead does not implement a feature, write the step page, squash commits, run the gates, or edit records. It
presents approval requests and relays decisions; the human owner approves.

1. Choose the lowest-numbered unblocked step unless the owner assigns another.
2. Check its **Depends on** line; do not start before dependencies are done.
3. Read the step’s **Original reference**, **Design**, **Acceptance criteria**, **Documentation**, **Tests**, and
   **Risks**.
4. Propose the feature-branch list for the step and get one approval. That approval covers all features in
   the step. Then create/push the step branch if it does not exist, and create the feature branch as described
   in section 3.
5. Implement only that feature’s scope. Resist adding later-phase features to the same branch.
6. Add the tests named in the step, plus tests for every bug you find.
7. Update the step page and documentation as part of the feature, or in the final closeout feature:
   - complete its **Documentation** section and write/update the named pages;
   - mark tasks/acceptance criteria (including “Documentation written”) as done;
   - record deviations and decisions;
   - link the implementation/PR/commit.
8. Update the parity matrix status in `appendix-b-parity-matrix.md`.
9. Run the quality gates.
10. Commit with an imperative message, e.g. `model: add particle and particle-set validation`.
11. Push the feature branch and report the result. After the local gates pass, squash-merge it into the step
    branch without waiting for a per-feature approval.
12. Delete the feature branch locally and remotely. Push the step branch only at step start, at closeout, or
    when a remote backup or handoff is needed.
13. After all features and the closeout feature, run the pre-audit gate, update
    `docs/planning/audits/step-NN.md`, freeze the step branch, and ask the lead to request the independent audit
    described in section 3.10.
14. Only after a passed audit, the lead presents the merge request. After owner approval, merge with
    `--no-ff`, tag `step-NN`, push, and delete the step branch.

A step is not done because the code compiles. Its acceptance criteria, tests, docs, parity entries, and audit
must pass.

### 7.1 Status tracking

Each step page starts with an **Implementation status** block: status, step branch, step CI, main merge,
step tag, and a feature log. The top-level [`docs/planning/status.md`](docs/planning/status.md) dashboard is
the at-a-glance roll-up.

Update status as follows:

- **Step start:** after the feature list is approved, set the step to `In progress` and record the step branch.
- **Each feature:** update its feature row and the step-page checkboxes in the feature branch. Mark it
  `Squashed` or `Done` after the squash; no per-feature approval is required.
- **After squash:** the next feature or closeout feature records the squash commit and the local validation
  result. CI is not run per feature.
- **Step completion:** the closeout feature records `In audit`, the closeout CI result, the closeout evidence,
  and the audit request. After the audit passes and the step is merged, set `Done`, record the main merge
  reference, and create and push the annotated `step-NN` tag after a green main CI run.
- **Milestones:** create the matching `milestone-mN` tag when the milestone is reached.
- **Parity:** keep `appendix-b-parity-matrix.md` as the feature-level completeness source.

Do not create duplicate per-step checklist files. The step page is the detailed checklist; `status.md` is the
roll-up. Avoid status-only commits; CI runs only at closeout and on `main`. See
[`docs/development/agent-workflow.md`](docs/development/agent-workflow.md) for context handoff, prompt,
approval-report, and diff-review templates.

### 7.2 Step sizing and split triggers

A step must be small enough that a fresh review agent can read its diff, tests, and evidence in one sitting. Sizing
is judged by the **complexity class** (Low / Medium / High) and the split triggers in
[`docs/planning/04-roadmap.md` §3.1](docs/planning/04-roadmap.md#31-step-sizing-and-split-triggers); the
person-week figures in §3 are non-gating forecasts for scheduling. Record the class and a one-paragraph
justification on the step page at design time. Split before implementation, not during the audit.

## 8. Coding conventions

### Code structure

- The project root is a lobby, not a home. Source projects keep only entry points and project metadata at the
  root (`AssemblyInfo.cs`, `GlobalUsings.cs`, `Usings.cs`, `<Project>Module.cs`, `Program.cs`, framework app
  files, `PublicAPI.*.txt`, and `README.md`). Everything else lives in a domain folder.
- Group by domain concept, not by type kind. No `Utils/`, `Utilities/`, `Helpers/`, `Common/`, `Shared/`,
  `Misc/`, `Models/`, `Interfaces/`, `Implementations/`, `Abstractions/`, or `Extensions/` folders.
  `Internal/` is allowed only inside a named concept folder.
- Limits: at most 20 direct `.cs` files per folder, folder depth 4, and 800 lines per file. Subfolders reset
  the file count.
- File names match the primary type or a permitted concept file (`*Dtos.cs`, `*Ids.cs`, `*Values.cs`,
  `*Templates.cs`, `*Extensions.cs`, and the other suffixes in `tools/structure-policy.json`).
- Files use the namespace that matches their folder. Step 02's `Mocassin.Symmetry` namespaces were aligned in a
  post-step maintenance update; there is no namespace baseline.
- Test projects may keep up to 20 files at the root; new tests still belong in the folder matching the code
  under test once folders exist.
- The policy is enforced by `Mocassin.Architecture.Tests` and `tools/verify.sh structure`. See
  [`docs/development/code-structure.md`](docs/development/code-structure.md) for the full rules and examples.

### .NET

- .NET 11, nullable enabled, implicit usings, warnings as errors.
- Prefer immutable records/`ImmutableArray<T>` for model data; mutable only inside editors/builders.
- Use typed IDs (`ParticleId`, `CellSiteId`, …) in the model; dense integer indices only in compiled data.
- Use explicit unit types (`LengthAngstrom`, `EnergyEv`, `TemperatureKelvin`, …); no bare `double` at API
  boundaries where units matter.
- Use generic math (`INumber<T>`) for internal numeric/vector kernels, not to collapse semantic public types.
  Keep `Fractional3`, `Cartesian3`, and `Vector4I` distinct; do not replace them with `Vector3<double>`.
- Use `Diagnostic`/`DiagnosticBag` for user errors; exceptions for programmer errors, I/O, and contract
  violations.
- Use `System.Text.Json` with source-generated contexts. No Newtonsoft.Json in new code.
- Prefer `FrozenDictionary`/`FrozenSet` for compiled lookup maps, `TimeProvider` for time, `System.Threading.Lock`
  for mutual exclusion, `TensorPrimitives` for large numeric reductions, and `[GeneratedRegex]`/`SearchValues`
  for parsing. Do not introduce SIMD/generic-math complexity without a measured need.
- No EF Core for `.msl`; use `Microsoft.Data.Sqlite` with explicit SQL.
- Async all the way; accept `CancellationToken`; no sync-over-async.
- `Microsoft.Extensions.*` for DI, configuration, logging. No service locator in view models.
- Avalonia: CommunityToolkit.Mvvm, `ViewLocator`, `DocumentViewModel`, explicit Add/Remove commands.
- Charts: LiveCharts2 only.
- Public APIs need XML docs.

### Rust

- Stable Rust, edition 2024 unless the workspace pins otherwise.
- `#![forbid(unsafe_op_in_unsafe_fn)]`; no `unsafe` without a `// SAFETY:` comment and a test.
- Use `thiserror` at crate boundaries; no `anyhow` in library public APIs.
- Use `clap` for CLI, `tracing` for logs, `serde`/`rmp-serde` for payloads, `rusqlite` (bundled) for `.msl`.
- Hot path: flat arrays, `u32`/`u8` indices, CSR link structures, no `HashMap`, no allocation per cycle.
- All randomness comes from the injected `Rng`; never use `thread_rng` or time-based seeds.
- Energies are `f64`; never `f32`.
- Particle masks are `u64`; never `1 << id` on a 32-bit integer.
- Use MessagePack (`MessagePack-CSharp` / `rmp-serde`) for `.mcs`; do not introduce a custom binary header,
  section table, alignment scheme, or native struct layout.

## 9. Documentation conventions

- **Narrative docs use Zensical.** Content lives in `docs/`; `docs/planning/` is internal and excluded from
  the published site via `exclude_docs`.
- **API references are generated, not hand-written:**
  - .NET via **DocFX** under `/api/dotnet/`;
  - Rust via **rustdoc** (`cargo doc`) under `/api/rust/`;
  - Python via **mkdocstrings** under `/api/python/`.
- One published site: Zensical owns `/`, DocFX/rustdoc outputs are copied into `site/api/dotnet` and
  `site/api/rust`; Python API docs are rendered natively at `/api/python/`.
- Required site sections: user guide, physics explanation, developer architecture guide, tutorials, CLI and
  format reference, troubleshooting/FAQ, performance/HPC, contributing, migration/clean-break, glossary/
  notation/units, examples/publications, and the API landing page.
- **Math delimiters depend on the audience.**
  - Published pages under `docs/` (except `docs/planning/`) render with KaTeX through
    `docs/javascripts/katex.js`: use `$$...$$` for display and `\(...\)` for inline math. The single-dollar
    `$...$` delimiter is deliberately not enabled. GitHub and IDE previews do not run the initializer, so
    preview with `zensical serve` or the built `site/`.
  - Planning records under `docs/planning/` are read outside the Zensical build: use `$...$` for inline and
    `$$...$$` for display, and do not use `\(...\)` or `\[...\]`.
  - Never use fenced `text` blocks for formulas. `python tools/check_doc_math.py` reports delimiter warnings
    (non-fatal by default).
- **Diagrams use Mermaid fenced blocks** with the `mermaid` language tag, enabled through Zensical SuperFences
  custom fences.
- **Docs-as-code:** Step 00 creates the Zensical skeleton, navigation, build scripts, and Docs CI job. Every
  feature step updates its documentation pages and keeps the strict build green. Step 30 completes coverage
  and cross-cutting content; Step 31 packages the site with the release.
- Docs quality gates:
  - `zensical build --strict` (no broken links/anchors);
  - DocFX build with no missing public API docs;
  - `cargo doc --workspace --no-deps` with missing-docs denied;
  - tutorial commands executed against `samples/`;
  - generated HTML and `site/` never committed.
- Do not duplicate API reference content in Markdown; link to the generated API pages instead.
- In-app help links point to stable documentation anchors.

## 10. Scientific invariants

Read [`docs/planning/02-data-contracts.md`](docs/planning/02-data-contracts.md) and the relevant step pages.
The most important invariants:

- **P1 position order is a contract.** Use exact rational symmetry operations and the canonical order from
  Step 02. Never let database order, hash iteration, or tolerance dedup renumber `P`.
- **Space-group operations are exact.** Integer linear part + rational translation denominator 24. Validate
  identity, inverses, closure, determinant ±1.
- **Cell geometry uses the standard crystallographic matrix.** The legacy missing-`c` bug must not return.
- **Energy units:** model and tables in eV; solver converts to kT once per job. Transport output uses SI.
- **Lattice energy:** pair contributions use the documented 0.5 factor; cluster contributions are counted at
  their center. Test the convention.
- **KMC:** uniform direction selection, rejection acceptance, `K_norm = K_fix * K_auto`,
  `dt = dof * K_norm / (nu_max * selectable_count)`. Start-unstable advances state but not time/MCS;
  end-unstable advances time but not state.
- **MMC:** Metropolis acceptance, no time advance, partner cell selected uniformly over the supercell.
- **Nernst–Einstein:** compute per species `D_σ,s = σ_s * k_B * T / (n_s * (z_s e)^2)` and define the total
  with `Σ n_s (z_s e)^2`. Use tracked-ensemble counts, not total particles. The legacy `u * z * kT` formula is
  wrong; do not reintroduce it. Report per-species and total values (ADR 0012).
- **Unit cells:** `A*B*C`, never `A+B+C`.
- **Tracking:** global per `(transition, particle)`, mobile per particle, static per `(cell, position,
  particle)`. Displacements are fractional until output conversion.
- **Detailed balance and conservation:** every accepted step must preserve mass and charge; add a test.

When the original behaviour is ambiguous, inspect the original source as a **reference only**, document the
interpretation, and encode it in a test. Do not copy GPL code.

## 11. Data and format rules

- `docs/planning/02-data-contracts.md` is the human-readable contract; `contracts/manifest.json` is the
  machine-readable source for field names, types, versions, and generated checks.
- Any format change requires:
  1. an ADR or explicit decision note;
  2. a version bump (`schemaVersion`, `format_version`, or `.mcs` version);
  3. updated contract fixtures in `tests/contracts/`;
  4. cross-language round-trip tests (.NET writer ↔ Rust reader; Rust writer ↔ .NET reader);
  5. canonical signature-projection vectors for signed types;
  6. current + previous major fixtures for `.msl`/`.msr`; current-only for `.mcs`;
  7. updated docs.
- Keep `tests/contracts/**` fixtures tracked. The `.gitignore` exceptions for `.mcs`/`.msl`/`.msr`/logs and
  `Job*/` folders are required; do not remove them.
- Never “fix” a format by silently reinterpreting existing fields.
- Write files atomically (temp file + fsync + rename). Never truncate a valid file on failure.
- Treat project/package/state input as untrusted: bound counts, validate indices, reject NaN/Inf where invalid,
  cap allocations, fuzz parsers.
- `.msl`: one shared compiled model per signature, content-addressed lattices, deterministic job IDs, **no
  results table**. It is immutable after publication and requires no `-wal`/`-shm` files.
- `.msr`: the results store; one collector at a time, bound to the package signature, current + previous major
  read support.
- `.mcs`: MessagePack map with `format = "mocassin.mcs"` and `formatVersion`; `lattice` as `bin`; optional
  KMC tracker/histogram fields. No custom header/section table/CRC. Integrity comes from the `.msr` SHA-256;
  enforce the < 4 GiB size assumption.
- Model/parameterization/lattice signatures use the canonical MessagePack projection (ADR 0010), not the
  stored compressed bytes.
- `.msl`/`.msr` support current + previous major; `.mcs` is current-major only (ADR 0013).
- Job folders and `solver.json` are contracts: `run.mcs`, `prerun.mcs`, `stdout.log`, `stderr.log`,
  `solver.json`, optional `mmcfe.db`. Do not invent undocumented files or paths.

## 12. Testing expectations

Follow the testing trophy in
[`docs/planning/03-quality-strategy.md`](docs/planning/03-quality-strategy.md): invest most effort in contract/
acceptance, property, and toy-integration tests; keep unit tests for exact formulas and boundaries.

- **Prefer real objects and real data.** Use actual domain objects, real temp directories, real SQLite files,
  and `.msl`/`.msr`/`.mcs` files produced by the real writers. Use generated toy data for breadth.
- **“No mocks/fakes” means “mocks/fakes only where they are truly needed and beneficial.”** Test doubles are
  allowed at true process/OS/UI/network/time boundaries and for failure injection. They are not allowed for
  domain objects, compilers, symmetry, energy/evaluation code, package writers/readers, or SQLite.
- Do not assert private call counts or collaborator interactions; assert observable state, files, diagnostics,
  exit codes, or public API results.
- Property-based tests for geometry, symmetry, permutation reduction, compilation, supercells, package/state
  round-trips, and evaluation formulas.
- Golden tests for compiled models, `.msl` payloads, `.msr` stores, `.mcs` documents, job folders, and CLI output,
  generated by the real
  writers and checked for freshness in CI.
- Contract tests across .NET, Rust, and Python for every format, backed by `contracts/manifest.json`.
- Statistical tests for physics: detailed balance, residence time, MSD/diffusion, conductivity, field sign,
  conservation.
- Statistical parity against reference observables on ceria, barium zirconate, and a complex mechanism.
- UI view-model tests use real project documents; only dialogs, pickers, the solver launcher, and time are
  faked. Avalonia headless/screenshot tests cover rendering and accessibility.
- Fuzz project JSON, `.msl`, `.msr`, `.mcs` MessagePack documents, job-folder/event inputs, and extension config
  parsers.
- Run `pytest` for the Python reader/aggregation package and cross-check fixtures against .NET and Rust.
- Declare each test’s tier (PR-fast, PR-medium, nightly, release). PR-fast tests must not depend on nightly
  assets.
- Coverage is a diagnostic, not a gate; do not add call-count tests to raise a percentage.

A bug fix without a regression test is incomplete. A scientific change without a physics/parity test is
incomplete.

## 13. Licensing rules

- New source files: `SPDX-License-Identifier: MIT`.
- Do not copy original MOCASSIN source, documentation text, or database content into MIT files.
- Any GPL-derived file must be isolated, marked `SPDX-License-Identifier: GPL-3.0-or-later`, and documented in
  `NOTICE.md`.
- The space-group catalog must be generated from spglib, pinned to an exact release and commit, not from the
  original SQLite database. Record the source, version, checksum, and BSD-3-Clause attribution in `NOTICE.md`
  and the generated catalog provenance. If that changes, review provenance and licensing first.
- The generated space-group catalog is committed and embedded; normal builds must not require spglib, Python,
  or network access. Regeneration uses the pinned dev-only tool.
- Dependencies must be MIT-compatible. GPL/AGPL dependencies require explicit review.
- Keep the original authors’ citation guidance in the README/NOTICE.

## 14. What not to do

- Do not add legacy `.mocprj`/`.msl`/`.mcs` import/export.
- Do not copy original GPL code or data into the MIT codebase.
- Do not use ScottPlot; use LiveCharts2.
- Do not introduce EF Core, Newtonsoft.Json, WPF, `System.Windows.*`, or a service locator.
- Do not let Rust read project JSON or write `.msl`/`.msr`.
- Do not use raw native structs/images, unaligned `Unsafe.AsRef`, or pinned whole-file buffers.
- Do not introduce a custom `.mcs` header/section table/CRC; use the MessagePack schema.
- Do not modify a published `.msl`; collect into `.msr` and rebuild for new packages.
- Do not use PCG32/`string.GetHashCode()` for reproducibility; use ChaCha12/SHA-256 for seeds and content
  signatures, and SHA-256 for `.mcs`/release integrity. Do not add a second hash algorithm without an ADR.
- Do not change canonical P1 order, energy conventions, or time-step formulas without an ADR and tests.
- Do not use `A+B+C` for unit-cell counts.
- Do not put scientific logic in view models.
- Do not make `Mocassin.Results` depend on `Mocassin.Packages`; use `Mocassin.Contracts`.
- Do not hand-edit generated contract artifacts; update `contracts/manifest.json` and regenerate.
- Do not hand-write API reference pages; generate them with DocFX/rustdoc.
- Do not publish `docs/planning/` as part of the standalone site.
- Do not optimize before correctness is proven by tests.
- Do not mark a step/parity item “done” without passing acceptance criteria.
- Do not work directly on `main` or `master`; always create a step branch and a feature branch.
- Do not commit feature work directly to a step branch; use a feature branch and squash merge.
- Do not merge a feature branch directly into `main`; merge it into its step branch.
- Do not merge a step branch into `main` without explicit user permission and a `--no-ff` merge.
- Do not use fast-forward or squash for the step-to-main merge unless the user explicitly requests it.
- Do not add CI triggers for feature branches; use local gates and run CI after squashing into the step branch.
- Do not bypass the lead to spawn a role agent directly; the owner asks the lead, and role agents
  do not spawn each other.
- Do not let the lead design, implement, review, approve, merge, or tag instead of delegating.
- Do not modify or commit `/workspace/mocassin-original`. It is the gitignored, read-only GPL-3.0 reference
  clone of `https://github.com/seb-eis/mocassin` at the verified commit.

## 15. Useful references in the original repository

The read-only reference clone is at `/workspace/mocassin-original` (gitignored; never commit it). These are
the most useful starting points. Read, do not copy.

- Project/model semantics: `src/ModelBuilder/ICon.Model/`
- Symmetry and geometry: `src/ModelBuilder/ICon.Framework.Symmetry/`
- Compilation/translation: `src/ModelBuilder/ICon.Model.Translator/`
- Evaluation formulas: `src/ModelBuilder/Mocassin.Tools.Evaluation/`
- Solver algorithms: `src/McSolver/Libraries/Simulator/`
- User guide: `docs/userguide-md/`
- Example project: `docs/project-examples/User-Guide-Project.mocprj` (legacy format; do not import)

The analysis notes used to create this plan are not part of the repository. If they are unavailable, re-derive
from the original and the plan’s “Original reference” sections.

## 16. Definition of done for a change

- [ ] The step’s acceptance criteria are met.
- [ ] Unit/property/golden/contract tests pass.
- [ ] Critical interfaces have an acceptance test using real objects/files.
- [ ] Any test double is justified by a process/OS/UI/network/time boundary or failure injection.
- [ ] No new warnings; formatters and linters clean.
- [ ] Format versions/fixtures updated if applicable.
- [ ] Step page updated with outcome and deviations.
- [ ] Parity matrix updated.
- [ ] No legacy/GPL/unsafe shortcuts introduced.
- [ ] Performance benchmark run for hot-path changes.
- [ ] Commit message is imperative and scoped.
- [ ] The change can be explained in terms of the plan, not just the code.
- [ ] Code follows the structure policy in section 8; `tools/verify.sh structure` passes.
- [ ] Public API changes update `PublicAPI.Unshipped.txt` and, when intentional, the ApiCompat baseline;
      `./tools/verify.sh api` passes.
- [ ] A merge, tag, or Blocker/Major waiver request includes the cumulative approval report in section 3.12: every round, every finding
      and resolution, accepted notes and waivers, residual risk, and the no-Blocker/no-Major gate statement.
- [ ] The CI cost policy in section 3.13 is followed: local validation, one closeout run, one main run, no
      per-feature CI, and platform runs only when required.
- [ ] The applicable task discipline from `docs/development/index.md` was used, and its durable output is in the
      named home rather than only in chat.
- [ ] Every review or audit finding is labelled with its review axis, `Spec` or `Standards`.
- [ ] The completion report follows the agent output contract in section 17.

## 17. Agent output contract

Every role uses the same everyday report. It is the single home for the fields; the cumulative approval report in
section 3.12 is separate and is required only for a merge, a tag, or a Blocker/Major waiver.

```text
Step / feature:
Branch:
Commits:
Commands run and results:
CI run (if any):
Docs/status/parity updated:
Known limitations and risks:
Approval requested for:
```

Rules:

- Do not ask for approval from prose alone; provide the diff and the evidence.
- For a merge, a tag, or a Blocker/Major waiver, also provide the cumulative approval report from section 3.12.
- A review or senior agent reports the record path, the verdict or answer, and the exact commands and evidence
  behind it, instead of a narrative.
- The lead reports the role, assignment, and handoff state; it does not restate another role's evidence in place of
  the durable record.
