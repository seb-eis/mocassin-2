# AGENTS.md

Guidance for AI coding agents and human contributors working in this repository.

## 1. What this project is

This is a modern, cross-platform reimplementation of **MOCASSIN** (*Monte Carlo for Solid State Ionics*),
the fixed-lattice MMC/KMC simulation suite for defects and transport in crystalline solids originally developed
at Forschungszentrum Jülich / RWTH Aachen.

**Current state: planning only.** The repository contains the reimplementation plan, architecture, data
contracts, and parity matrix. There is no production .NET solution or Rust workspace yet. Do not claim that a
feature is implemented until code and tests exist.

Target stack:

| Component | Technology |
| --- | --- |
| Model builder, compilation, package writer, results API, CLI | C# / .NET 10 |
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
14. [`docs/development/agent-workflow.md`](docs/development/agent-workflow.md) — context, prompt, review, and
    evidence playbook for coding agents.
15. The step page you intend to work on, plus its dependencies.
16. [`docs/planning/appendix-b-parity-matrix.md`](docs/planning/appendix-b-parity-matrix.md) — what “done” means.
17. [`docs/planning/adr/0008-standalone-documentation-toolchain.md`](docs/planning/adr/0008-standalone-documentation-toolchain.md),
   [`docs/planning/adr/0009-contract-manifest-and-contracts-library.md`](docs/planning/adr/0009-contract-manifest-and-contracts-library.md),
   [`docs/planning/adr/0010-canonical-messagepack-signatures.md`](docs/planning/adr/0010-canonical-messagepack-signatures.md),
   [`docs/planning/adr/0011-immutable-package-and-results-store.md`](docs/planning/adr/0011-immutable-package-and-results-store.md),
   [`docs/planning/adr/0012-transport-and-msd-definitions.md`](docs/planning/adr/0012-transport-and-msd-definitions.md),
   [`docs/planning/adr/0013-format-compatibility-and-api-versioning.md`](docs/planning/adr/0013-format-compatibility-and-api-versioning.md),
   [`docs/planning/adr/0014-extension-abi-and-trust-boundary.md`](docs/planning/adr/0014-extension-abi-and-trust-boundary.md),
   and [`docs/planning/steps/step-30-standalone-documentation.md`](docs/planning/steps/step-30-standalone-documentation.md)
   — documentation toolchain, contract/format, and extension decisions.

The plan is the source of truth. If code and plan disagree, stop and reconcile them rather than silently
following one.

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
  it once. Individual features still require approval before their squash merge.
- Short-lived maintenance branches that are not part of a step use `fix/<slug>` or `docs/<slug>` from
  `main`; they are merged into `main` with `--no-ff` after approval.

### 3.2 Starting work

Before the first numbered item, complete the pre-implementation design review in section 3.9 and obtain a
`Ready for implementation` verdict. Do not create the step branch or write implementation code before that.

1. Propose the feature-branch list and get one approval for the step breakdown.
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
- **Stop and ask the user for explicit approval before every feature squash merge.** Report:
  - feature branch name and commit hash(es);
  - exact local test commands/results;
  - confirm that no per-feature CI run will be triggered;
  - a short summary of the diff;
  - any known limitations or follow-up work.
- Feature integration uses local squash merges after chat approval. Do not open a pull request unless the
  user asks for one.
- After approval:
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
  merges from `main`, and explicitly approved closeout changes.
- If the user rejects or abandons a feature, delete its local and remote branch after confirmation.

### 3.4 Step completion

1. Use a final closeout feature branch for:
   - step page status, deviations, and links;
   - parity-matrix updates;
   - documentation updates;
   - any final acceptance fixes.
2. Run the step's full acceptance criteria and local quality gates (`./tools/verify.sh all`) on the step branch.
3. Merge the current `main` into the step branch if needed so it has the current `ci.yml`. Dispatch the single
   closeout CI run on the step branch: `CI` workflow, Ubuntu, path-filtered. Record the run URL or ID in the
   audit record. Do not run CI after each feature; the closeout run is the only step-branch CI run.
4. If an emergent design change is required, integrate a step amendment (section 3.11) before the audit freeze.
   The audit then covers the combined solution. Re-run the closeout CI once after the final amendment is
   integrated.
5. Complete the independent audit in section 3.10. The step branch must be frozen at the audited commit; after
   a passing audit, the auditor's record commit and the step agent's resolution/status commit are the only
   permitted later commits.
6. Only after the audit passes, **stop and ask for explicit permission before merging the step into `main`.**
   The approval request must include the cumulative approval report in section 3.12.
7. After approval:
   ```sh
   git switch main
   git pull --ff-only
   git merge --no-ff step/00-foundation -m "step-00: repository, toolchain, and CI"
   git push origin main
   # wait for the automatic main CI run to pass
   git tag -a step-00 -m "step-00: repository, toolchain, and CI"
   git push origin step-00
   ```
8. Wait for the automatic `main` CI run before tagging. If it fails, fix forward or revert and re-run; never tag
   a red `main`.
9. Delete the step branch locally and remotely after the merge and tag.
10. Do not use fast-forward or squash for the step-to-main merge unless the user explicitly requests it.

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
- When a task or feature is finished, report the branch name, commit hash(es), test results, and ask for
  the required approval before any merge.
- If the user authorizes a merge, follow their instruction exactly; otherwise leave the branch unmerged.

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

The workflow has four roles. Each role edits only the artifacts it owns. There is no scribe and no
transcription step.

| Role | May edit | May commit | Must not edit |
| --- | --- | --- | --- |
| Planner (planning agent) | Design and planning docs; the review record's request, scope, checklists, and resolutions | Documentation-only commits on the design branch | Code, tests, configuration, audit or design verdicts, step status, merges, tags |
| Implementor (step agent) | Code, tests, configuration, contracts, step page, status dashboard, parity matrix; the audit/review record's request, scope, checklists, and resolutions | Feature-branch commits; documentation-only record and status commits | The reviewer's findings, evidence, or verdict text; merges and tags without approval |
| Reviewer (planning auditor or audit agent) | Only the assigned review or audit record: its own metadata, checklist completion marks (not item text), evidence, findings, and verdict | At most one documentation-only record commit per round, with `[skip ci]` | Everything else, including code, tests, configuration, design docs, step page, status dashboard, parity matrix, request scope, checklist item text, resolutions, merges, tags, and pushes to `main` |
| Human owner | Approve gates, merges, and tags | — | — |

Reviewer rules:

- A reviewer must be a fresh context that did not author or implement the reviewed work. It may be spawned by
  the planner, implementor, or human owner.
- The spawner must not be the reviewer. It provides only the frozen commit, the request record, repository
  links, and the required commands/scope. It must not supply a desired verdict or replace repository reading
  with a summary.
- The human owner approves the final verdict and gate. The spawner must not mark a verdict, waive findings,
  merge, tag, or push `main`. A re-review requires a new commit with fixes; do not re-spawn until a preferred
  verdict appears.
- The reviewer states in its record that it is a fresh context with no shared conversation with the
  planner/implementor.
- The reviewer writes findings and verdict directly into the durable record. No handoff file, transcription,
  or scribe is used.
- The reviewer appends a new round and must not alter earlier rounds or reviewer-authored text.
- The reviewer marks the request checklists to record what was actually reviewed. It may change checkbox
  marks and add short inline verification notes, but not the item text; the request owner owns the items.
- The reviewer records the exact commands, results, and files behind the verdict in the record's `Evidence`
  section, so the human owner can spot-check it.
- The reviewer must not fix findings. If it edits code, tests, configuration, or design docs, it becomes an
  implementor and a fresh reviewer is required.
- The reviewer's record commit must touch exactly one file: the review or audit record. The reviewed or audited
  commit is the parent of that commit. A record commit that touches any other file is invalid, and the verdict
  must be re-run by a fresh reviewer.
- The implementor may add resolutions and update status after the record commit, but must not change the
  reviewer's findings, evidence, or verdict. Resolutions belong in a separate section or table.
- The reviewer does not merge, tag, push `main`, or delete branches. The human owner still approves the merge.
- The reviewer works in a writable worktree of the branch under review. If no writable checkout is available,
  it writes the record and the step agent commits it unchanged.
- The record commit uses a documentation-only message such as
  `docs: record step-NN audit Passed [skip ci]`.
- Optional scratch notes may live under `.tmp/`. They are not gate artifacts and are never committed.

### 3.9 Pre-implementation design review

Every new step must pass an independent design review before the step branch is created or implementation
starts. This gate is separate from the post-implementation audit in section 3.10.

1. The planning agent and step agent finish the step's design decisions and update the step page, data
   contracts, architecture, quality strategy, open decisions, parity matrix, and related docs. They apply those
   edits directly.
2. The docs are frozen at a specific commit and the step agent creates or updates
   `docs/planning/reviews/step-NN.md` using
   [`docs/planning/reviews/step-design-review-template.md`](docs/planning/reviews/step-design-review-template.md).
   Set the design-review status to `Ready for review` and record the reviewed commit.
3. A fresh planning auditor with no implementation context reads the step page, design decisions, dependency
   contracts, architecture, data contracts, ADRs, quality strategy, open decisions, parity matrix, licensing
   records, and audit plan. The auditor must not have authored or implemented the design.
4. The auditor writes the evidence, findings, and verdict directly into
   `docs/planning/reviews/step-NN.md` and commits only that file with a documentation-only message and
   `[skip ci]`. The reviewed commit is the parent of the record commit. The auditor does not merge, tag, push
   `main`, or change any other file. If the auditor cannot commit, it writes the record and the step agent
   commits it unchanged.
5. The auditor's write scope is defined in section 3.8. It may edit only its own metadata, evidence, findings,
   and verdict. It must not edit the request scope, checklists, resolutions, design docs, step page, status
   dashboard, parity matrix, code, tests, or configuration.
6. If the verdict is `Changes required`, the planning agent and step agent update the design docs directly, the
   step agent adds the resolutions and marks the review `Re-review required`, and a fresh planning auditor
   re-reviews the new commit. The new round is appended; earlier rounds are not overwritten.
7. Only a `Ready for implementation` verdict permits the human owner to approve starting the step. The approval
   request must include the cumulative approval report in section 3.12, covering every review round, finding,
   resolution, accepted note, waiver, and residual risk. The step branch is then created from the reviewed
   `main` commit.
8. Step 00 and Step 01 are recorded as `Waived — process introduced after completion`; no retrospective
   review is required.
9. The post-implementation audit in section 3.10 remains mandatory.

Design-review statuses and finding severities are defined in
[`docs/planning/reviews/README.md`](docs/planning/reviews/README.md). Design-review records are planning
records and are excluded from the published site.

### 3.10 Step audit gate

The step agent's self-check is not an independent audit. Before a step is merged into `main`, a fresh,
out-of-context audit context must review the frozen step branch.

1. The step agent finishes all features and the closeout feature, updates the step page, status dashboard,
   parity matrix, and feature log, runs `tools/verify.sh all`, and gets the single closeout CI run green.
2. The step agent creates or updates `docs/planning/audits/step-NN.md` using
   [`docs/planning/audits/step-audit-template.md`](docs/planning/audits/step-audit-template.md), records the
   exact audited commit and the closeout CI run, and sets the status to `Ready for audit`.
3. The step branch is frozen at the audited commit. After a passing audit, two documentation-only commits may
   follow: the auditor's record commit and the step agent's resolution/status commit. Neither may change code,
   tests, configuration, or the auditor's text. If fixes are needed, the step agent uses a new feature branch
   and the new commit is re-audited.
4. A fresh audit agent reads `AGENTS.md`, the step page, the audit record, the status dashboard, and relevant
   ADRs/contracts, then runs the required builds/tests and reviews the diff. The auditor must not have
   implemented the step.
5. The auditor writes the evidence, findings, and verdict directly into
   `docs/planning/audits/step-NN.md` and commits only that file with a documentation-only message and
   `[skip ci]`. The audited commit is the parent of the record commit. The auditor does not merge, tag, push
   `main`, or change any other file. If the auditor cannot commit, it writes the record and the step agent
   commits it unchanged.
6. The auditor's write scope is defined in section 3.8. It may edit only its own metadata, evidence, findings,
   and verdict. It must not edit the scope, checklists, resolutions, code, tests, configuration, step page,
   status dashboard, or parity matrix.
7. If fixes are needed, the step agent adds the resolutions, fixes the findings on a new feature branch,
   squash-merges into the step branch, dispatches one new closeout CI run, and sets the audit status to
   `Re-audit required`.
8. The fresh auditor re-audits the new commit and appends a new round to the record. The same auditor may
   continue if it stayed within the reviewer write scope; otherwise start a fresh audit context. Keep every
   earlier round in the record.
9. Only a `Passed` audit permits the step-to-main approval request. The request must include the cumulative
   approval report in section 3.12, covering every audit and review round, every finding and resolution, every
   accepted note or waiver, and the residual risk. The audit result is evidence, not merge authorization; the
   human owner still approves the merge.
10. After merge, wait for the automatic `main` CI run. Tag `step-NN` only when it is green; record the result
    and delete the step branch. Never tag a red `main`.

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
   feature approval and squash process.
6. Update the parent step page, amendment page, audit request, and design review record with the combined
   commit.
7. Freeze the combined step branch. The audit in section 3.10 covers the parent step and every integrated
   amendment at that commit. Do not create a separate audit record or tag for an integrated amendment.
8. If the parent step is already merged or tagged, do not move the tag. Use a maintenance branch from `main`,
   audit the amendment separately, and create a new tag such as `step-NN.1` only with owner approval.

Amendment statuses and the amendment index are in
[`docs/planning/amendments/README.md`](docs/planning/amendments/README.md).

### 3.12 Approval report

Before the human owner approves any review verdict, audit verdict, waiver, merge, or tag, the agent requesting
approval must present a **cumulative approval summary** covering the entire history. The summary goes in the
approval request itself; linking the durable record is not sufficient.

The summary must contain:

1. **Requested decision** — the exact verdict, waiver, merge, or tag being requested, and the frozen commit.
2. **Final state** — branch, reviewed/audited commit, CI result, and documentation-build result.
3. **Every round** — for each design-review and audit round: round number, date, reviewed or audited commit,
   reviewer or auditor identity and spawner, and verdict.
4. **Every finding** — finding ID, severity, issue, evidence, resolution or fix, resolving commit, and current
   status. Include findings from every round, including those already fixed.
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

### 3.13 CI cost policy

The project uses the GitHub Free private-repository allowance of 2,000 billable minutes per month. CI is a
confirmation gate, not the development loop. The full rationale and cost data are in
[`docs/planning/ci-policy.md`](docs/planning/ci-policy.md).

| Run | Trigger | Runner | Scope |
| --- | --- | --- | --- |
| Main merge | automatic `push` to `main` | Ubuntu | Path-filtered jobs |
| Step closeout | one manual `workflow_dispatch` per step | Ubuntu | Path-filtered jobs |
| Platform gate | manual `workflow_dispatch` with `platforms` | Windows/macOS | On demand only |
| Documentation | manual `docs.yml` | Ubuntu | Full docs build |
| Benchmark | manual `benchmark.yml` | Ubuntu | Selected benchmark |
| Nightly | disabled until real suites exist | — | — |

Rules:

- There are no per-feature and no per-push step-branch CI runs. Validate locally with `tools/verify.sh`.
- The closeout run is the only step-branch CI run. The main merge runs once more after the `--no-ff` merge.
- Automatic CI is Ubuntu-only. Windows and macOS are manual. Promote Windows to automatic closeout/main when
  platform-specific code first appears; keep macOS manual until there is macOS-specific behaviour to validate.
- Path filters must ensure that only affected jobs run. A change to `.github/workflows/ci.yml` forces all jobs.
- Documentation is validated locally with `zensical build --strict` (and the other docs commands). The
  separate `docs.yml` workflow is manual until the documentation is mature enough to justify automatic runs.
- Keep Nightly disabled until real long-running suites exist. If it is re-enabled, run it weekly on Ubuntu.
- Target at most 300 billable minutes per month. If usage passes 50% of the allowance before mid-month, switch
  to closeout-only CI and ask the human owner before adding any run.
- Use `[skip ci]` for docs-only commits and merges.
- Never tag a red `main`; wait for the main merge CI before creating `step-NN`.

Platform-specific triggers: P/Invoke, `LibraryImport`, `System.Runtime.InteropServices`, native libraries or
RID-specific packages, path/case/locking logic, line-ending or encoding-sensitive handling, GUI projects, Rust
`cfg(windows)` or `cfg(target_os = "...")`, and installers or packaging.

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

These commands become valid as the corresponding projects are created. Do not run them expecting success
today.

```bash
# .NET
dotnet restore Mocassin.slnx
dotnet build Mocassin.slnx -c Release
dotnet test Mocassin.slnx -c Release
dotnet format Mocassin.slnx --verify-no-changes

# Rust
cargo fmt --manifest-path src/rust/Cargo.toml --all --check
cargo clippy --manifest-path src/rust/Cargo.toml --workspace -- -D warnings
cargo test --manifest-path src/rust/Cargo.toml --workspace
cargo build --manifest-path src/rust/Cargo.toml --workspace --release

# End-to-end (once the CLI exists)
dotnet run --project src/dotnet/Mocassin.Cli -- validate samples/ceria/ceria.mocprj
dotnet run --project src/dotnet/Mocassin.Cli -- build samples/ceria/ceria.mocprj -o /tmp/ceria.msl
cargo run --manifest-path src/rust/Cargo.toml -p mocassin-cli -- \
  --db /tmp/ceria.msl --job 1 --io /tmp/Job00001
dotnet run --project src/dotnet/Mocassin.Cli -- collect /tmp/ceria.msl --dir /tmp --results /tmp/ceria.msr

# Python analysis API (once the package exists)
python -m pytest src/python/tests
python -m build src/python

# Documentation (once the Zensical/DocFX setup exists)
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

1. Choose the lowest-numbered unblocked step unless the owner assigns another.
2. Check its **Depends on** line; do not start before dependencies are done.
3. Read the step’s **Original reference**, **Design**, **Acceptance criteria**, **Documentation**, **Tests**, and
   **Risks**.
4. Propose the feature-branch list for the step and get one approval. Then create/push the step branch if it
   does not exist, and create the feature branch as described in section 3.
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
11. Push the feature branch, report the result, and ask for approval before the squash merge into the step
    branch.
12. After approval, squash-merge, push the step branch, and delete the feature branch locally and remotely.
13. After all features and the closeout feature, update `docs/planning/audits/step-NN.md`, freeze the step
    branch, and request the independent audit described in section 3.10.
14. Only after a passed audit, ask for permission to merge the step into `main`, then merge with `--no-ff`,
    tag `step-NN`, push, and delete the step branch.

A step is not done because the code compiles. Its acceptance criteria, tests, docs, parity entries, and audit
must pass.

### 7.1 Status tracking

Each step page starts with an **Implementation status** block: status, step branch, step CI, main merge,
step tag, and a feature log. The top-level [`docs/planning/status.md`](docs/planning/status.md) dashboard is
the at-a-glance roll-up.

Update status as follows:

- **Step start:** after the feature list is approved, set the step to `In progress` and record the step branch.
- **Each feature:** update its feature row and the step-page checkboxes in the feature branch. Mark it
  `Awaiting approval` before asking for approval.
- **After squash:** the next feature or closeout feature records the squash commit and the local validation
  result. CI is not run per feature.
- **Step completion:** the closeout feature records `Done`, the closeout CI result, and the main merge
  reference. After the approved merge and a green main CI run, create and push the annotated `step-NN` tag.
- **Milestones:** create the matching `milestone-mN` tag when the milestone is reached.
- **Parity:** keep `appendix-b-parity-matrix.md` as the feature-level completeness source.

Do not create duplicate per-step checklist files. The step page is the detailed checklist; `status.md` is the
roll-up. Avoid status-only commits; CI runs only at closeout and on `main`. See
[`docs/development/agent-workflow.md`](docs/development/agent-workflow.md) for context handoff, prompt,
approval-report, and diff-review templates.

## 8. Coding conventions

### .NET

- .NET 10, nullable enabled, implicit usings, warnings as errors.
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
- **Math uses KaTeX.** Use `$$...$$` for display math and `\(...\)` for inline math; never use fenced
  `text` blocks for formulas. The single-dollar `$...$` delimiter is deliberately not enabled. Preview the
  rendered page before closing a documentation change.
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
- Do not modify `/workspace/mocassin-original` if it is present. It is a read-only reference clone.

## 15. Useful references in the original repository

If `/workspace/mocassin-original` is available, these are the most useful starting points. Read, do not copy.

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
- [ ] Any approval request includes the cumulative approval report in section 3.12: every round, every finding
      and resolution, accepted notes and waivers, residual risk, and the no-Blocker/no-Major gate statement.
- [ ] The CI cost policy in section 3.13 is followed: local validation, one closeout run, one main run, no
      per-feature CI, and platform runs only when required.
