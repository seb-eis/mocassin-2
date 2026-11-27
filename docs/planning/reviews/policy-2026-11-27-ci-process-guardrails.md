# Policy Update 2026-11-27 — CI Process and Binding Guardrails

> Review agent: write evidence, findings, and verdict directly into this record. You may edit only your own
> metadata (`Review agent`, `Review date`), `Record state`, the review checklist marks and short inline
> verification notes (not item text), `Evidence`, `Findings`, `Verdict`, and `Human approval`'s reviewer-side
> fields. Commit only this file with a documentation-only message and `[skip ci]`. Do not edit the request,
> scope, non-goals, decision tables, design decisions, evidence supplied by the request owner, or any other file.

## Request

| Field | Value |
| --- | --- |
| Review ID | `POLICY-2026-11-27-CI` |
| Type | Process policy update — CI gate and enforcement coverage |
| Record path | `docs/planning/reviews/policy-2026-11-27-ci-process-guardrails.md` |
| Design class | **Full** |
| Design-class trigger | [`docs/development/design-proportionality.md`](../../development/design-proportionality.md) §1, “Policy-update reviews”: the change edits the **CI policy** and the **enforcement coverage of a gate** (which changes start the process/binding guardrails). |
| Branch | `fix/ci-process-checks` |
| Base commit | `be930c4e1d21bb96cde08bdb67ed20cb59631a20` (`origin/main` at the fix freeze) |
| Reviewed commit | `f00fd0fe1b8b529fe703f9a8fd43af87d0d59589` |
| Request tip | The record commit whose parent is `f00fd0f` (this commit; no rule change) |
| Request owner | Dev |
| Review initiated by | Lead |
| Review agent | DeepSeek V4.1 Flash 1017 (`opencode-go/deepseek-v4.1-flash`), fresh independent context |
| Review date | 2026-11-27 |
| Authority | Lead assignment of the CI guardrail change, 2026-11-27; owner decision recorded under “Owner decisions” |

This is a **design review of the change at the reviewed commit**. The branch already carries the implementation,
so the reviewed commit is both the design and the implemented tree: the review asks whether the design that is
now in `f00fd0f` is complete, consistent, and safe, and it judges the branch diff as the evidence. The record
commit adds this request only; the reviewed commit is its parent.

The record path follows the existing policy record
[`policy-update-2026-11-27-workflow-disciplines.md`](policy-update-2026-11-27-workflow-disciplines.md) and matches
the review binding's `docs/planning/reviews/policy-*.md` allow rule. The shorter `policy-<YYYY-MM-DD>-<slug>.md`
form in `docs/planning/reviews/README.md` §9.1 is the class pattern; this record uses the `policy-update-` prefix
that the first policy record established.

## Purpose

The 2026-11-27 workflow adoption added four guardrails — `tools/check_process_consistency.py`,
`tools/check_docs_links.py`, `tools/check_doc_math.py`, and the `tools/tests` suite — but `.github/workflows/ci.yml`
never ran them. Two adoption merges therefore went green without exercising the new checks, and nothing in the
pipeline detected the gap. The change under review closes that gap with one cheap Ubuntu-only job, records the job
in the CI policy and the contributing page, and pins the wiring with tests that execute the shipped filter.

The first commit (`80cffd6`) added the job. The fix commit (`f00fd0f`) repairs two defects found while reviewing
the first commit against `tools/verify.sh` and the checker's own required-file list:

1. the path filter missed `docs/planning/ci-policy.md`, `docs/planning/status.md`,
   `docs/planning/maintenance/**`, and `docs/planning/drift-checks/**`, so edits to the normative CI/status pages
   and to the drift-check template did not start the checker that validates them; and
2. the job ran `python -m unittest discover -s tools/tests` while `tools/verify.sh` runs
   `python -m unittest discover -s tools/tests -t .`, so CI and the local gate did not discover the same set.

## Scope

- The `Process and binding checks` job in `.github/workflows/ci.yml`: its steps, runner, timeout, and job-level
  condition.
- The `process` area flag in the change-detection step: the initial value, the case-arm paths, and the derivation
  rules (`ci.yml` → all jobs including `process`; `force_all`/`all` → every area).
- The cheapness constraints: standard library only, no dependency install, no .NET or Rust build, Ubuntu only,
  path-filtered, `timeout-minutes: 10`.
- The path-filter coverage fix and the discovery-command fix in `f00fd0f`.
- The documentation updates that make the policy true: `docs/planning/ci-policy.md` §2–§3, `AGENTS.md` §3.13,
  `docs/development/contributing.md` (job table), and the `docs/planning/status.md` note.
- The tests that pin the wiring: `tools/tests/test_ci_process_job.py` (new), and the `process` row in
  `tools/tests/test_ci_platform_gate.py`.

## Non-goals

- No per-feature and no per-push step-branch CI run. The closeout dispatch and the automatic `main` run remain the
  only step-branch/main runs; this change adds a job inside them, not a new trigger.
- No widening of any existing job: `.NET`, `Rust`, `Python`, `Contracts and policy`, `Public API compatibility`, and
  the focused Windows/macOS gates keep their scope, paths, and commands.
- No change to what the checks themselves validate. `check_process_consistency.py`, `check_docs_links.py`,
  `check_doc_math.py`, and the existing `tools/tests` cases are not edited, relaxed, or extended by this change.
- No new tool, dependency, workflow file, runner, matrix, or secret; no change to `docs.yml`, `benchmark.yml`,
  the platform matrix, or the concurrency group.
- No code, contract, format, schema, API, or runtime change; no change to `src/`, `tests/`, `contracts/`, samples,
  or generated artifacts.
- No change to Step 15, its design branch, or the temporary `dev-free` binding.
- No historical-record rewrite; the record is added, not amended over another one.

## Owner decisions

1. **One standard review pass before merge.** The owner accepts a single Full round; if the round returns
   `Changes required`, the dev fixes and a fresh round runs.
2. **Stop rule.** The review stops when no Blocker or Major remains. Minor and Note findings are recorded and
   accepted or deferred by the owner; they do not justify another round.
3. **Budget-guard interpretation.** `docs/planning/ci-policy.md` §5 says “add no new run without owner approval”
   while the 50% guard is active. This change adds **no new run or trigger**: it adds one job to the existing
   closeout and `main` runs. The owner approved the added job on that reading. The reviewer should say so
   explicitly if it reads §5 as forbidding this.

## Design decisions

### D1. One job, four steps

`process` runs, in order, `python tools/check_process_consistency.py`, `python tools/check_docs_links.py`,
`python tools/check_doc_math.py`, and `python -m unittest discover -s tools/tests -t .`. These are exactly the
process/documentation guardrails that previously had no CI consumer. Licence, secret, contract, API, and
dependency checks stay in `Contracts and policy`, whose paths are unchanged.

### D2. Cheapness

The job uses `actions/checkout` and `actions/setup-python` (3.13) only. It installs nothing (the checkers and the
test suite import only the standard library), builds neither .NET nor Rust, runs on `ubuntu-latest` with a
`10`-minute timeout, and has no matrix. The expected wall time is seconds to low tens of seconds, so the billable
cost is one short Ubuntu job inside a run that is already dispatched.

### D3. Trigger paths

The job runs when the changed-path set touches any of:

| Family | Why the guardrails read it |
| --- | --- |
| `.opencode/**` | Agent definitions, commands, and skills that the binding test and the process checker validate |
| `AGENTS.md`, `GLOSSARY.md`, `opencode.json` | The normative process contract, the ubiquitous language, and the binding registration |
| `docs/development/**` | The process, discipline, and review pages the checkers and link gate cover |
| `docs/planning/reviews/**`, `docs/planning/audits/**` | Review and audit records; the findings-column and status-vocabulary checks read them |
| `docs/planning/ci-policy.md`, `docs/planning/status.md` | The normative CI page and the status dashboard that this job and the process checker describe |
| `docs/planning/maintenance/**`, `docs/planning/drift-checks/**` | `check_process_consistency.py` requires and validates the drift-check template and the required-file list; maintenance records are the process-change home |
| `zensical.toml` | Documentation build configuration and navigation |
| `tools/check_process_consistency.py`, `tools/check_docs_links.py`, `tools/check_doc_math.py` | The checks themselves |
| `tools/tests/**` | The tool suite the job runs |

The case arm keeps `process` independent of the other areas: a process path sets `process=true` and nothing else.
A `tools/contracts/**` change still sets only `contracts`; a `src/**` change never sets `process`.

### D4. Condition and derivation

`if: needs.changes.outputs.process == 'true' && (github.event_name == 'push' || inputs.platforms == 'ubuntu' ||
inputs.platforms == 'all')`. This is the same job-level condition as the other Ubuntu-only jobs, so
`platforms=windows` and `platforms=macos` never start it, even when a process path changed. A `ci.yml` change sets
`ci=true`, which sets `all=true` and `process=true`; `force_all=true` does the same.

### D5. Path-filter coverage fix

`docs/planning/ci-policy.md` and `docs/planning/status.md` are the normative pages for this job and for the status
vocabulary the process checker enforces. `docs/planning/maintenance/**` is where process changes are recorded, and
`docs/planning/drift-checks/**` holds the drift-check template that `check_process_consistency.py` lists as
required. All four families are added to the case arm and to both normative filter lists (`ci-policy.md` §3 and
`AGENTS.md` §3.13). No catch-all `docs/planning/**` glob is used, so unrelated planning records stay out.

### D6. Discovery consistency fix

The job runs `python -m unittest discover -s tools/tests -t .`, matching `tools/verify.sh` line 180. The `-t .`
top-level directory is what lets the suite import `tools.tests.*` and keeps discovery identical between the local
gate and CI; a test asserts the two commands stay equal.

### D7. Documentation homes

`docs/planning/ci-policy.md` §2 (run table) and §3 (path-filter table and the job's cheapness paragraph),
`AGENTS.md` §3.13, `docs/development/contributing.md` (job table), and the `docs/planning/status.md` note. The
filter list has two homes by design — `ci-policy.md` §3 is the normative table and `AGENTS.md` §3.13 is the summary
the agents read — and a test compares both against the workflow arm so they cannot drift.

### D8. Drift guards in the tests

`tools/tests/test_ci_process_job.py` executes the shipped filter snippet (not a copy) over sample path lists,
asserts the job block's commands and cheapness, and adds three guards: every documented filter token matches the
workflow arm (normalising `**` to `*`, because the `case` glob treats them alike), the job's discovery command
equals the `tools/verify.sh` command, and the `process` output is exported by the `changes` job.

## Decision table: what runs and when

| Event | Process job | Why |
| --- | --- | --- |
| `push` to `main`, process paths changed | **runs** | Automatic main merge run, path-filtered |
| `push` to `main`, only code paths changed | skipped | `process=false` |
| `workflow_dispatch`, `platforms=ubuntu` (closeout), process paths changed | **runs** | Closeout run, path-filtered |
| `workflow_dispatch`, `platforms=windows` or `macos` | skipped | Ubuntu-only condition; verified by the platform-gate test |
| `workflow_dispatch`, `platforms=all` | **runs** if `process=true` | Ubuntu path is part of `all` |
| `workflow_dispatch` with `force_all=true` (`platforms=ubuntu` or `all`) | **runs** | Every area flag is forced true; Windows/macOS still skip the Ubuntu-only job |
| Any event, `.github/workflows/ci.yml` changed | **runs** | `ci=true` → `all=true` and `process=true` |
| Docs-only commit with `[skip ci]` | no run at all | Existing policy; accepted residual |

## Risks

| Risk | Severity | Owner | Mitigation |
| --- | --- | --- | --- |
| The job grows into a second full gate (installs, builds, or extra checks) | Major | Dev | Cheapness constraints in D2, the `tools/tests` cheapness assertions, a 10-minute timeout, and the non-goal “no widening” |
| Because `docs/planning/status.md` changes in nearly every step closeout, the job runs on most closeout runs, so the “cheap” job is effectively always-on for closeouts | Minor | Lead; human owner | Accepted as the point of the change: the guardrails should run at closeout. Cost is one short Ubuntu job with no installs. If the budget tightens, the first lever is to drop `status.md` from the filter and rely on the pre-audit gate |
| A new process tool (for example another `tools/check_*.py`) is added without adding it to the filter | Minor | Dev | The filter is a short explicit list; adding a checker is itself a `tools/`-adjacent process edit that a reviewer and the drift guard see. Recorded as a known limitation, not silently covered |
| The documented filter lists drift from the case arm again | Minor | Dev | `test_documented_filter_lists_match_the_workflow_arm` fails on divergence in either direction |
| CI and the local gate discover different test sets | Minor | Dev | `test_tool_test_discovery_matches_the_verify_script` compares the job with `tools/verify.sh` |
| The drift guard is coupled to the prose layout (a table row prefix and a bullet prefix) and would fail on a reflow | Note | Dev | The extractors use the stable row/bullet prefixes; a reflow fails the test loudly instead of passing silently, and the fix is one line in the test |
| The process job duplicates a check that also runs in `Contracts and policy` for code changes | Note | Dev | Duplication is bounded to the process paths, where `Contracts and policy` also runs; the process job adds no install and no code build |
| `[skip ci]` docs-only merges bypass the guardrails | Minor | Lead; human owner | Existing policy; the closeout pre-audit gate and `tools/verify.sh policy --require` run the same checks locally |

## Verification plan

At the reviewed commit, with the record present:

| Command | Purpose |
| --- | --- |
| `python -m unittest discover -s tools/tests -t .` | The whole tool suite, including the new filter proof |
| `python tools/check_process_consistency.py` | Process checker (required-file, integration, findings, tier vocabulary) |
| `python tools/check_docs_links.py` | Documentation links, including this record |
| `python tools/check_doc_math.py` | Documentation math delimiters |
| `./tools/verify.sh docs --require` | Strict Zensical, DocFX, rustdoc |
| `./tools/verify.sh policy --require` | Release build, contract/symmetry drift, tool suite, SPDX, secrets, `cargo-deny` |
| Red proof | The new tests fail at `80cffd6` with the fix applied only to the test file |

## Request-owner evidence

Recorded by the dev on `fix/ci-process-checks` at `f00fd0fe1b8b529fe703f9a8fd43af87d0d59589`, worktree
`/home/node/worktrees/ci-process-checks`.

**Branch state.** `git rev-parse HEAD` → `f00fd0fe1b8b529fe703f9a8fd43af87d0d59589`; local and
`origin/fix/ci-process-checks` are equal; `git status --porcelain` empty before this record was written.
`git log --oneline origin/main..HEAD` → `f00fd0f ci: cover the normative process pages and mirror the local test
discovery`, `80cffd6 ci: run the process and binding guardrails`.

**Branch diff.** `git diff --stat origin/main...HEAD` → 7 files, +367/−17:

| File | Change |
| --- | --- |
| `.github/workflows/ci.yml` | `process` output, case arm, job, `-t .` discovery |
| `AGENTS.md` | §3.13 filter list and discovery wording |
| `docs/development/contributing.md` | Job table row |
| `docs/planning/ci-policy.md` | §2–§3 job, filter table, cheapness paragraph |
| `docs/planning/status.md` | Adoption note |
| `tools/tests/test_ci_platform_gate.py` | `process` job registration |
| `tools/tests/test_ci_process_job.py` | New filter proof, job assertions, two drift guards |

**Filter proof** (the shipped snippet over single-path lists):

| Path | Result |
| --- | --- |
| `.opencode/agents/dev-free.md` | `process=true`, no other area |
| `.opencode/skills/tdd/SKILL.md` | `process=true`, no other area |
| `docs/planning/status.md` | `process=true`, no other area |
| `docs/planning/ci-policy.md` | `process=true`, no other area |
| `docs/planning/maintenance/2026-11-27-temporary-free-dev-binding.md` | `process=true`, no other area |
| `docs/planning/drift-checks/2026-11-18-phase-c.md` | `process=true`, no other area |
| `tools/contracts/manifest.json` | `process=false`, `contracts=true` |
| `tools/check-license-headers.py` | `process=false`, `contracts=true` |
| `src/dotnet/Mocassin.Core/Particle.cs` | `process=false` (`dotnet`, `contracts`, `api`) |
| `docs/planning/steps/step-15-energy-engine.md` | `process=false`, no area |
| `docs/user-guide/index.md` | `process=false`, no area |

**Red proof.** A detached worktree at `80cffd6` with only the new `tools/tests/test_ci_process_job.py` copied in:
`python -m unittest tools.tests.test_ci_process_job` → `Ran 9 tests`, `FAILED (failures=6)`. The failures are the
four added paths (`process=false` expected `true`), the discovery command, and the job-block command assertion.
`test_documented_filter_lists_match_the_workflow_arm` **passed** there by design: at `80cffd6` the workflow and
both documents agreed on the smaller list, so the guard detects divergence, not absence. The worktree was removed
with `git worktree remove --force`.

**Gates at the reviewed commit** (with this record present and staged):

| Command | Result |
| --- | --- |
| `python -m unittest discover -s tools/tests -t .` | `Ran 77 tests … OK` |
| `python tools/check_process_consistency.py` | `Process consistency: OK` |
| `python tools/check_docs_links.py` | `Documentation links OK (238 files scanned).` |
| `python tools/check_doc_math.py` | `Documentation math conventions OK (234 files scanned).` |
| `./tools/verify.sh docs --require` | exit 0; strict Zensical, DocFX, rustdoc; `Documentation built in …/site` |
| `./tools/verify.sh policy --require` | exit 0; `Contract artifacts are current (26 files)`; 274 space-group settings; `Process consistency: OK`; `Ran 77 tests … OK`; `SPDX headers present in 1211 source files`; `No secret patterns found in 1790 tracked text files`; `advisories ok, bans ok, licenses ok, sources ok` |

**Not run.** No CI run was dispatched for this branch: there are no per-feature or per-push runs, and this is a
maintenance branch, not a step closeout. The automatic `main` run after an approved merge is the CI confirmation.

## Design-review checklist

Design-time checks. Mark what the design and the reviewed tree actually satisfy; add short inline notes only.
Axis labels follow [`docs/development/review-axes.md`](../../development/review-axes.md): **Spec** = the change
implements the requested CI coverage; **Standards** = it follows the repository's documented engineering and CI
standards.

### A. Problem, class, and scope

- [x] **Spec** — The request states the concrete failure (guardrails added, no CI consumer, two green merges) and it
      is reproducible from the branch and the gate history.
      _Verified: `git show 8e9afef:.github/workflows/ci.yml` and `git show be930c4:.github/workflows/ci.yml` contain
      no `process:` job; the CI run history itself is not in the clone._
- [x] **Spec** — Full is the right class: the trigger names the CI policy and the gate's enforcement coverage, and
      the change is not argued down to Light.
      _Verified against design-proportionality §1: the change edits the CI policy and a gate's enforcement
      coverage, both Full triggers._
- [x] **Spec** — Scope and non-goals are explicit; the non-goals protect the cost policy (no new run, no widened
      job) and the isolated review boundary (no Step 15, no binding change).
      _Verified: `git diff --name-only` has 7 files; none is a Step 15 page, a binding, or another job._

### B. Job scope and cheapness

- [x] **Spec** — The job runs exactly the four guardrails that lacked a consumer, and no other check moves into it.
      _Verified: the four steps match D1; `check_doc_math.py` runs non-strict, exactly as `tools/verify.sh` runs it._
- [x] **Standards** — It installs no dependency, builds neither .NET nor Rust, has a timeout, and uses only the
      standard library.
      _Verified: `timeout-minutes: 10`; checkout plus `setup-python` only; the `tools/tests` imports are the
      standard library plus local `tools.*` modules; the suite ran in under one second._
- [x] **Standards** — It is registered where the other Ubuntu-only jobs are, follows their checkout/setup style, and
      does not inherit a matrix.
      _Verified: `needs: changes`, the same `if:` expression as the other Ubuntu-only jobs, `ubuntu-latest`, no
      `strategy`/matrix._

### C. Trigger correctness

- [ ] **Spec** — Every path family the four checks read is covered, and the case arm, `ci-policy.md` §3, and
      `AGENTS.md` §3.13 agree.
      _Not met: the three filter homes agree, but several files that `check_process_consistency.py` validates are
      not in the arm — see F-1._
      _Round 2: F-1 is closed — all 64 process-checker inputs match the live arm and the derived guard fails on an
      uncovered required file. The docs link/math half is still partial (123 of 238 Markdown files outside the
      arm); see F-7. The item stays open only for that Minor._
- [x] **Spec** — The four added families each have a stated reason tied to a check the job runs.
      _Verified: D3 ties `ci-policy.md`/`status.md` to the status vocabulary and `maintenance`/`drift-checks` to the
      checker's template and required-file checks; all four are in the arm._
- [x] **Standards** — No other job is widened: a contracts-only path still enables only `contracts`, and a code path
      never enables `process`.
      _Verified with the shipped filter and by `test_contract_tooling_does_not_enable_the_process_job` and
      `test_code_changes_do_not_enable_the_process_job`._
      _Round 2: re-run over the 64 checker paths; none enables another area._
- [x] **Spec** — `ci.yml`, `force_all`, and `platforms=all` enable the job; `platforms=windows`/`macos` do not.
      _Verified against the condition and the platform-gate evaluator; `force_all` with `windows`/`macos` is still
      skipped because the `platforms` clause is false (F-4)._
      _Round 2: the decision-table row now states the platforms qualification (F-4 closed)._

### D. Cost and budget

- [x] **Spec** — The added cost is bounded and described, and the §5 budget-guard reading is recorded as an owner
      decision rather than assumed.
      _Verified: Owner decision 3; see question 1 in Evidence and F-5._
- [x] **Standards** — No new trigger, runner, matrix, secret, or dependency is introduced, so §3.13's CI cost policy
      and the closeout-only rule still hold.
      _Verified: `on:` is unchanged, the job sits inside the existing closeout/main runs, and it has no
      matrix/secret/dependency._

### E. Discovery consistency and tests

- [x] **Standards** — The job's discovery command equals `tools/verify.sh`'s, and the `-t .` root is what the suite
      needs; a drift guard fails if they diverge.
      _Verified: `tools/verify.sh:180` and the job both contain `python -m unittest discover -s tools/tests -t .`;
      the red proof fails the guard at `80cffd6`._
- [x] **Spec** — The filter proof covers a positive list, a `.opencode` path, a `docs/planning/status.md` path, and
      the negative `tools/contracts/**` case.
      _Verified: 16 positive families, `.opencode/agents/dev-free.md`, `docs/planning/status.md`, and the
      `tools/contracts/manifest.json` / `tools/check-license-headers.py` negatives._
      _Round 2: `PROCESS_PATHS` now includes the new homes; all 64 checker paths return `process=true` with every
      other area false._
- [x] **Standards** — The new tests fail on the pre-fix tree for the reasons claimed, and the drift guard is a
      divergence check, not a restatement of the current list.
      _Verified: six failures at `80cffd6` with only the new test file copied in; the documented-list test passed
      there because the workflow and both prose homes agreed on the smaller list, as the request says._
      _Round 2: the new derived guard fails on an injected uncovered `REQUIRED_FILES` entry (red reproduction in
      Evidence), and the 78-test suite is green at the fix commit._

### F. Documentation, evidence, and gates

- [x] **Spec** — `ci-policy.md` §2–§3, `AGENTS.md` §3.13, `contributing.md`, and `status.md` agree with the workflow
      and with each other.
      _Verified: the two normative filter lists equal the case arm after `**`→`*` normalisation; the job-table row
      and the status note match. The stale workflow header comment is F-2._
      _Round 2: the header now lists process/binding (F-2 closed); the three homes are still equal (25 tokens
      each)._
- [x] **Standards** — `check_process_consistency.py`, `check_docs_links.py`, `check_doc_math.py`, and both
      `verify.sh --require` modes pass at the reviewed commit, and the recorded counts match the tree.
      _Verified: exact outputs in Evidence; 238 link files, 234 math files, 77 tests, 1211 SPDX files, 1790 tracked
      text files._
      _Round 2: 78 tests, 238 link files, 234 math files; both `verify.sh --require` modes exit 0; the counts were
      re-verified at the fix commit._
- [x] **Spec** — No step page, audit, closeout, tag, code, contract, format, or generated artifact is touched.
      _Verified: the 7-file `git diff --name-only` contains only the workflow, three docs, the status note, and two
      test files._
- [x] **Standards** — The record follows §9.1: request-owned fields, a reviewer-owned section, one verdict, one
      record commit with `[skip ci]`.
      _Verified shape; the filename pattern is a separate §9.1 deviation (F-3). This record commit touches only this
      file._
      _Round 2: the filename now follows `policy-<date>-<slug>.md` (F-3 closed); the round-2 record commit touches
      only this file._

## Evidence

### Review round 1 — 2026-11-27

> **Reviewer independence.** I am a fresh, independent review context. I did not author or implement this change
> and have no shared conversation with the dev, design agent, lead, or any other review agent. The lead supplied
> only the frozen commit, the request record, the repository, and the required commands; no desired verdict,
> summary, or conversation history was provided. My model is DeepSeek V4.1 Flash 1017
> (`opencode-go/deepseek-v4.1-flash`).

**Pinned point.** Base `be930c4e1d21bb96cde08bdb67ed20cb59631a20` (`origin/main`); reviewed tree
`f00fd0fe1b8b529fe703f9a8fd43af87d0d59589`; request commit
`061f761d0621c7bf60bd658c610bbfe35afaa738` (the subject record commit, documentation-only). This round's record
commit has parent `061f761`; the reviewed delta is the three-dot diff `be930c4...f00fd0f` (7 files, +367/−17),
and the only later change is the request text that already sits in `061f761`.

| Check | Command | Result |
| --- | --- | --- |
| Refs | `git rev-parse HEAD` / `git rev-parse origin/main` | `061f761…` / `be930c4…` |
| Diff | `git diff --stat be930c4...f00fd0f` | 7 files, +367/−17 |
| Diff names | `git diff --name-only be930c4...f00fd0f` | `.github/workflows/ci.yml`, `AGENTS.md`, `docs/development/contributing.md`, `docs/planning/ci-policy.md`, `docs/planning/status.md`, `tools/tests/test_ci_platform_gate.py`, `tools/tests/test_ci_process_job.py` |
| Whitespace | `git diff --check be930c4...f00fd0f` | exit 0, no output |
| Tool suite | `python -m unittest discover -s tools/tests -t .` | `Ran 77 tests in 0.869s` — `OK` |
| Process checker | `python tools/check_process_consistency.py` | `Process consistency: OK` |
| Links | `python tools/check_docs_links.py` | `Documentation links OK (238 files scanned).` |
| Math | `python tools/check_doc_math.py` | `Documentation math conventions OK (234 files scanned).` |
| Docs gate | `./tools/verify.sh docs --require` | exit 0 — `Documentation math conventions OK (234 files scanned).`; `Documentation links OK (238 files scanned).`; `No issues found`; DocFX 0 warnings / 0 errors; rustdoc for 7 crates; `Documentation built in …/site`; `Closeout gate completed with required checks enabled.` |
| Policy gate | `./tools/verify.sh policy --require` | exit 0 — `Public API is compatible with the checked-in baseline.`; `Contract artifacts are current (26 files)`; `space-group catalog is current: 274 settings …`; `Process consistency: OK`; `Ran 77 tests … OK`; `SPDX headers present in 1211 source files`; `No secret patterns found in 1790 tracked text files`; `advisories ok, bans ok, licenses ok, sources ok`; `Closeout gate completed with required checks enabled.` |

**Filter and condition.** I ran the shipped filter by importing the test helper, which extracts the live `run: |`
body from `ci.yml` and executes it: 16 positive families (`.opencode/*`, `AGENTS.md`, `GLOSSARY.md`,
`opencode.json`, `docs/development/*`, `docs/planning/reviews/*`, `docs/planning/audits/*`,
`docs/planning/ci-policy.md`, `docs/planning/status.md`, `docs/planning/maintenance/*`,
`docs/planning/drift-checks/*`, `zensical.toml`, the three `tools/check_*.py`, `tools/tests/*`) give
`process=true` and every other area `false`; `tools/contracts/manifest.json` and
`tools/check-license-headers.py` give `contracts=true`, `process=false`; `src/dotnet`, `src/rust`, and `src/python`
code paths give `process=false`; `.github/workflows/ci.yml` sets all six area flags true. The documented lists in
`ci-policy.md` §3 and `AGENTS.md` §3.13 are token-for-token equal to the case arm after the test's `**`→`*`
normalisation (verified by printing all three sets). Bash `case` treats `*` as matching `/`, so the documented
`**` forms and the workflow `*` forms are equivalent; I confirmed this with a direct `case` probe. The process
job's `if:` is character-for-character the same expression as `rust`, `python`, `contracts`, and `api-compat`, and
`test_ci_platform_gate.py` evaluates it for push/ubuntu/all/windows/macos.

**Red proof reproduced.** `git archive 80cffd6 | tar -x -C /home/node/tmp/redproof`, copied only
`tools/tests/test_ci_process_job.py` from `f00fd0f`, then `python -m unittest tools.tests.test_ci_process_job -v`:
`Ran 9 tests`, `FAILED (failures=6)`. The failures were the four paths added by `f00fd0f`
(`docs/planning/ci-policy.md`, `docs/planning/status.md`, `docs/planning/maintenance/…`,
`docs/planning/drift-checks/…`), the job-block discovery command, and
`test_tool_test_discovery_matches_the_verify_script`. `test_documented_filter_lists_match_the_workflow_arm` passed
at `80cffd6`, as the request says, because the workflow and both prose homes then agreed on the smaller list.
The scratch tree was `/home/node/tmp/redproof`; the repository was not modified.

**Coverage gap reproduced (F-1).** `REQUIRED_FILES`, `REQUIRED_INTEGRATION`, and `NORMATIVE_FILES` in
`tools/check_process_consistency.py` list files that no `process` glob matches: `docs/planning/README.md`,
`docs/planning/04-roadmap.md` (integration text `split trigger`), `docs/planning/closeout/README.md`,
`docs/planning/closeout/_template.json`, `docs/planning/incidents/_template.md`,
`docs/planning/research/README.md` and `_template.md`, `docs/planning/prototypes/README.md` and `_template.md`,
`docs/planning/retrospectives/README.md` and `_template.md`, `tools/check_closeout_evidence.py`, and
`tools/verify.sh`. `detect(["docs/planning/closeout/_template.json"])` returns every area `false`, including
`process`. In a scratch copy of `f00fd0f` at `/home/node/tmp/gapproof`, replacing `"merge_tree_ci"` in that
template made `python tools/check_process_consistency.py` exit 1 with
`closeout template is missing field: "merge_tree_ci"`. The real tree was not modified.

**Question 1 — does `ci-policy.md` §5's budget guard permit the new job?** Yes, on the recorded reading. §5 says
"switch to closeout-only CI and ask the owner before adding any run"; this change adds no trigger and no run — it
adds one short Ubuntu job inside the existing closeout and `main` runs, and it does not widen any other job. The
owner recorded that reading in Owner decision 3. The wording does not explicitly address a job that increases the
minutes of an existing run, so a future policy edit should say "run or job" if the intent is spend-based (F-5).
No owner waiver is missing for this change.

**Question 2 — is one combined Full pass adequate, or is a separate post-implementation verification required?**
Adequate as approved. `reviews/README.md` §9.1 describes a `## Post-implementation verification` pass at the
implemented commit; here the reviewed commit is both the design and the implementation (the branch already carries
the code), so one independent pass answers both the five design questions and the implemented-state questions
against the same tree. The owner explicitly accepted one standard pass (Owner decision 1), and this round is that
pass. A short verification section would make the record shape match §9.1 exactly; that is F-6 (Note), not a
reason for another round.

**Question 3 — does the record filename satisfy §9.1?** Not strictly. Both normative homes
(`design-proportionality.md` §1 and `reviews/README.md` §9.1) state `policy-<YYYY-MM-DD>-<slug>.md`; this record is
`policy-update-2026-11-27-ci-process-guardrails.md`, following the earlier record's `policy-update-` prefix. The
binding's `policy-*.md` glob accepts both, so there is no gate failure, but the pattern is not met (F-3).

**Not run.** No CI run was dispatched for this branch: per the CI policy there are no per-feature or per-push
runs, and this is a maintenance branch, not a step closeout. The automatic `main` run after an approved merge is
the CI confirmation. The "two adoption merges went green" CI history is not available in the clone; what is
reproducible is that `8e9afef` and `be930c4` touched process paths while their `ci.yml` had no `process:` job.

**Files read.** `AGENTS.md` §3.8–§3.13 and §17; `docs/planning/reviews/README.md` §2.2, §5–§9.1, §11;
`docs/development/design-proportionality.md` §1–§2; `docs/development/review-axes.md`;
`docs/planning/ci-policy.md` §2–§6; `.github/workflows/ci.yml` (full); `tools/verify.sh` (dispatch and discovery
line); `tools/check_process_consistency.py`; `tools/check_docs_links.py`; `tools/check_doc_math.py`;
`tools/tests/test_ci_process_job.py`; `tools/tests/test_ci_platform_gate.py`;
`docs/development/contributing.md`; `docs/planning/status.md`; the prior policy record's post-implementation
verification section; `.opencode/agents/review.md`.

### Re-review round 2 — 2026-11-27

> **Reviewer independence.** I am a fresh, independent review context for this round. I did not author or
> implement the change, and I have no shared conversation with the dev, design agent, lead, or the round-1 review
> agent. The lead supplied only the reviewed commit, the request tip, this record, the branch/worktree, and the
> required commands; no desired verdict, summary, or conversation history was supplied. My model is DeepSeek
> V4.1 Flash 1017 (`opencode-go/deepseek-v4.1-flash`).

**Pinned point.** Branch `fix/ci-process-checks`; base `be930c4e1d21bb96cde08bdb67ed20cb59631a20`; reviewed commit
`06a861b6c123b9cb796309a9a7332cc237a08734`; request tip `ee4327d91e32ee5e817f403531de515ac67886ea`. The record
commit's parent is the request tip. `git diff --stat 06a861b..ee4327d` is one file, `38 insertions(+), 3
deletions(-)`, and touches only this record (rename, `Record path`, the F-4 decision-table row, `Record state`,
and the appended Dev resolutions). That is a documented documentation-only request commit under
[`README.md`](README.md) §8.1, so the code tree under review is still `06a861b`; the delta is recorded here
rather than treated as a new design change.

**Round-1 immutability.** `git diff --find-renames 3838d7c..HEAD -- docs/planning/reviews/` shows only the request
commit's hunks plus the append. The F-1–F-6 rows, their evidence, the round-1 Evidence text, every checklist item
text, and the round-1 verdict block are unchanged. The old filename survives only in round-1 historical prose
(the old-name evidence and the F-3 row) and in the F-3 resolution.

| Check | Command | Result |
| --- | --- | --- |
| Tool suite | `python -m unittest discover -s tools/tests -t .` | `Ran 78 tests in 0.784s` — `OK` |
| Process checker | `python tools/check_process_consistency.py` | `Process consistency: OK` |
| Links | `python tools/check_docs_links.py` | `Documentation links OK (238 files scanned).` |
| Math | `python tools/check_doc_math.py` | `Documentation math conventions OK (234 files scanned).` |
| Docs gate | `./tools/verify.sh docs --require` | exit 0 — strict Zensical `No issues found`; DocFX `0 warning(s), 0 error(s)`; rustdoc documented 7 crates; `Closeout gate completed with required checks enabled.` |
| Policy gate | `./tools/verify.sh policy --require` | exit 0 — API compatible; `Contract artifacts are current (26 files)`; 274 space-group settings; `Process consistency: OK`; `Ran 78 tests … OK`; SPDX headers in 1211 files; no secrets in 1790 files; `advisories ok, bans ok, licenses ok, sources ok` |

**F-1 (Major) — closed.** The `process` arm now carries 25 patterns, including `docs/planning/README.md`,
`docs/planning/04-roadmap.md`, the closeout/incident/research/prototype/retrospective homes,
`tools/check_closeout_evidence.py`, and `tools/verify.sh`. The guard
`test_every_process_checker_input_matches_the_workflow_filter` imports `REQUIRED_FILES`, `NORMATIVE_FILES`, and
`VERDICT_FILES` from `tools/check_process_consistency.py`, unions the two gate scripts, and matches every path
against the live case arm. I executed the shipped Bash filter over all 64 paths: every one returns `process=true`
and every other area `false`; `docs/planning/closeout/_template.json` (the round-1 failure) now returns
`process=true`. **Red reproduction:** in a disposable `git archive` of `HEAD` at `/home/node/tmp/r2red`, I
injected `"docs/planning/future-required-file.md"` into `REQUIRED_FILES` and ran the single guard: `Ran 1 test`,
`FAILED (failures=1)`, with `docs/planning/future-required-file.md is validated by the process checks but no
process filter matches it`. The scratch tree was removed; the repository was not modified.

**F-2 (Minor) — closed.** `.github/workflows/ci.yml:19` now reads "The Rust, Python, contracts, process/binding,
and API-compatibility jobs are Ubuntu-only by job condition".

**F-3 (Minor) — closed.** The record is `policy-2026-11-27-ci-process-guardrails.md`, `Record path` is updated,
and `grep -rn policy-update-2026-11-27-ci-process-guardrails --include='*.md'` finds only round-1 historical
text and the resolution; no Markdown link targets the old filename.

**F-4 (Note) — closed.** The decision-table row reads "`workflow_dispatch` with `force_all=true`
(`platforms=ubuntu` or `all`)" and states that Windows/macOS still skip the Ubuntu-only job.

**F-5 (Note) — closed.** `docs/planning/ci-policy.md` §5 (lines 202 and 208) and `AGENTS.md` §3.13 (line 655) now
say "run or job"; the owner decision and the unchanged `on:` block still support the no-new-trigger reading.

**F-6 (Note) — accepted.** No separate post-implementation section is added. The combined Full pass and the
reviewed commit are stated in the record; no action and no further round.

**Regression sweep.** The arm enables only `process` for process paths: the 64-path execution above left
`dotnet`, `rust`, `python`, `contracts`, and `api` false everywhere. `tools/contracts/manifest.json`,
`tools/check-license-headers.py`, and `src/dotnet/Mocassin.Core/Particle.cs` remain `process=false`. The three
filter homes are equal as token sets (25 each) after the documented `**`→`*` normalisation. The job block keeps
the four commands, `ubuntu-latest`, `timeout-minutes: 10`, no matrix, and no dependency install.

**Docs-link coverage (F-7).** `check_docs_links.py` scans the five root guides and every `docs/**/*.md` (238
files). Matching that file set against the live arm matches 115 and leaves 123 outside, including `README.md`,
`CONTRIBUTING.md`, `SECURITY.md`, `docs/user-guide/**` (13 files), `docs/planning/steps/**`, and the ADRs. A link
break in those files still fails the checker without starting the process job. This is the same failure mode as
F-1 but outside the process-checker inputs; the published docs have their own gate (`verify.sh docs`, manual
`docs.yml`), and the job is deliberately process-scoped, so it is recorded as F-7 (Minor), not a Major.

**Merge tree (F-8).** `origin/main` is `895b20b33e8d747c6875bfa603c9b891da1dd4ad` at review time, not the
`be930c4` recorded as the base. Main advanced with the step-15 design commits, including
`docs/planning/04-roadmap.md` and `docs/development/solver-architecture.md`. `git merge-tree --write-tree
origin/main HEAD` produced tree `f61365dbcb36b0ff90bd0e0a2aeb37c81f16dced`; in an archive of that tree
`check_process_consistency.py`, `check_docs_links.py` (239 files), `check_doc_math.py` (235 files), and the
78-test suite all pass. The merge candidate must still be rechecked if main moves again before merge (F-8).

**Round-2 request metadata (F-9).** The Request table still pins the round-1 reviewed commit (`f00fd0f`) and
request tip, and its naming paragraph still says the record uses the `policy-update-` prefix. The round-2 fixed
point and the rename are recorded only in the Dev resolutions block. The review used the lead-supplied
`06a861b`/`ee4327d` and that block, so the gate is not ambiguous; the table is stale (F-9, Note).

**Guard completeness (F-10).** The derived guard imports the three tuples. `REQUIRED_INTEGRATION` and
`TIER_VOCABULARY_PAGES` are not imported; every current member is already covered by those tuples or the
`docs/development/*` glob, but a future checker input outside both would not trip the guard. Note only; the
current tree is fully covered.

**Files read.** `AGENTS.md` §3.9–§3.13 and §17; `docs/planning/reviews/README.md` §2.1–§9.1;
`docs/development/design-proportionality.md` §1–§2; `docs/development/review-axes.md`;
`docs/planning/ci-policy.md` §2–§5; `.github/workflows/ci.yml` (full); `tools/verify.sh`;
`tools/check_process_consistency.py`; `tools/check_docs_links.py`; `tools/check_doc_math.py`;
`tools/tests/test_ci_process_job.py`; `tools/tests/test_ci_platform_gate.py`;
`docs/development/contributing.md`; `docs/planning/status.md`; the round-1 record text and the Dev resolutions;
`.opencode/agents/review.md`.

## Findings

| ID | Axis | Severity | Origin | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- | --- |
| F-1 | Spec | **Major** | **design** | **The `process` filter still omits normative process files that the same four guardrails validate, so an edit to them can break `check_process_consistency.py` without starting the job — the failure mode this change exists to prevent.** Checklist C claims "Every path family the four checks read is covered", but D5's rationale ("`check_process_consistency.py` validates the maintenance and drift-check templates") applies equally to the closeout, research, prototype, retrospective, and incident templates, the planning index/roadmap, `tools/check_closeout_evidence.py`, and `tools/verify.sh`. The job would be skipped while the checker would fail. | The checker's `REQUIRED_FILES`/`REQUIRED_INTEGRATION`/`NORMATIVE_FILES` list `docs/planning/README.md`, `docs/planning/04-roadmap.md` (`split trigger`), `docs/planning/closeout/README.md`, `docs/planning/closeout/_template.json`, `docs/planning/incidents/_template.md`, `docs/planning/{research,prototypes,retrospectives}/README.md` and `_template.md`, `tools/check_closeout_evidence.py`, and `tools/verify.sh`; none match a `process` glob. Running the shipped filter over `docs/planning/closeout/_template.json` gives `process=false`; in a scratch copy at `f00fd0f`, replacing `"merge_tree_ci"` in that template made the checker exit 1 with `closeout template is missing field: "merge_tree_ci"`. | Add the omitted families to the case arm and to both normative lists (or a bounded `docs/planning/*` set that includes these homes), add their sample paths to `PROCESS_PATHS`, and update the checklist text; alternatively record the residual gap as an accepted risk and drop the "every path family" claim. |
| F-2 | Standards | Minor | documentation | The `ci.yml` header comment still says "The Rust, Python, contracts, and API-compatibility jobs are Ubuntu-only by job condition" and omits the new process job, although the earlier paragraph of the same comment block was updated to mention "the process/binding checks". | `.github/workflows/ci.yml` lines 18–21 (post-diff); the updated paragraph at lines 7–9. | Add process/binding to the sentence so the workflow header matches the job set. |
| F-3 | Standards | Minor | documentation | The record filename does not follow the normative `policy-<YYYY-MM-DD>-<slug>.md` pattern; it uses the `policy-update-` prefix of the earlier record. The binding's `policy-*.md` glob accepts both, so this is a process-conformance issue, not a gate failure. | `design-proportionality.md` §1 ("Its record is `docs/planning/reviews/policy-<YYYY-MM-DD>-<slug>.md`") and `reviews/README.md` §9.1 (same pattern); the record path and its justification at lines 33–37. | Rename to `policy-2026-11-27-ci-process-guardrails.md` before merge (the record is new and not linked from a normative page), or record an explicit owner decision that amends the pattern or the precedent. |
| F-4 | Spec | Note | documentation | The "Decision table: what runs and when" row for "`workflow_dispatch` with `force_all=true`" says the process job runs without qualifying `platforms`. With `platforms=windows` or `macos`, `force_all` sets `process=true` but the job-level condition still skips the job; D4 states this, but the table reads as unconditional. | Record decision-table row (line 175); D4; `ci.yml` line 412. | Qualify the row with "(`platforms=ubuntu` or `all`)". |
| F-5 | Standards | Note | documentation | `ci-policy.md` §5 says "add no new run without owner approval"; this change adds no trigger or run, only a job inside the existing closeout/main runs, and the owner approved it on that reading. The wording does not explicitly cover a new job that increases the billable minutes of an existing run. | `ci-policy.md` §5; Owner decision 3; unchanged `on:` block in `ci.yml`; Evidence question 1. | In a later policy edit, say "run or job" if the guard is meant to cover added jobs. |
| F-6 | Standards | Note | design | `reviews/README.md` §9.1 describes a `## Post-implementation verification` section for a policy update; this record has none. The owner approved one combined Full pass and the reviewed commit is both the design and the implementation, so a separate pass is not required, but the record shape does not state that in a verification section. | `reviews/README.md` §9.1 lines 232–243; Owner decision 1; the record preamble at lines 28–31; Evidence question 2. | Optional: add a short `## Post-implementation verification` section recording the combined-pass decision; it does not change this round's verdict or require another round. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. Origin: **design**, **implementation**,
**evidence**, or **documentation**. Axis: **Standards** or **Spec**.

### Re-review round 2 findings — 2026-11-27

| ID | Axis | Severity | Origin | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- | --- |
| F-7 | Spec | **Minor** | design | The job and the request say the change closes the gap for the four guardrails, but the `process` filter covers only 115 of the 238 Markdown files `check_docs_links.py` scans. A link or math break in `README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `docs/user-guide/**`, `docs/planning/steps/**`, or an ADR still fails the checker without starting the job. The `check_process_consistency.py` half of round-1 F-1 is fully closed. | Live-arm match over the checker's file set: 115 matched, 123 unmatched (75 under `docs/planning/`, 13 under `docs/user-guide/`, plus the root guides and published docs). `detect(["README.md"])`, `detect(["docs/user-guide/index.md"])`, and `detect(["docs/planning/adr/0011-immutable-package-and-results-store.md"])` all return `process=false`. | Either qualify the "closes that gap" and "every path family the four checks read" claims to the process-checker inputs and process homes, or add the remaining root guides and docs homes to the arm and both lists. If deferred, the owner accepts the residual; the published-docs gate is `verify.sh docs` and the manual `docs.yml`. |
| F-8 | Standards | Note | evidence | The request records `be930c4` as `origin/main`, but `origin/main` is `895b20b` at review time; the branch has not merged the new main. The step-15 design commits touch `docs/planning/04-roadmap.md` and `docs/development/solver-architecture.md`, both process-filtered. The merge candidate was checked, but a later main move would stale it again. | `git rev-parse origin/main` → `895b20b…`; `git log --oneline be930c4..origin/main` lists the step-15 records; `git merge-tree --write-tree origin/main HEAD` → `f61365db…`, and the four checks pass on that tree. | Recheck the merge tree against current `main` immediately before merge; no rebase is needed. This is the §3.4 merge-tree rule applied to a maintenance branch, not a design defect. |
| F-9 | Standards | Note | documentation | The Request table still pins the round-1 reviewed commit (`f00fd0f`) and request tip, and its naming paragraph still says the record uses the `policy-update-` prefix. The round-2 fixed point and the rename are recorded only in the Dev resolutions block. | Record lines 20–21 and 33–37 are unchanged round-1 text; the round-2 commit is named only at lines 505–506. The lead-supplied reviewed commit and the resolution block made the gate unambiguous. | Add a round-2 request line or header with `06a861b`/`ee4327d` and mark the old naming rationale as superseded; a bookkeeping note, not a re-review trigger. |
| F-10 | Standards | Note | design | The derived coverage guard imports `REQUIRED_FILES`, `NORMATIVE_FILES`, and `VERDICT_FILES`; `REQUIRED_INTEGRATION` and `TIER_VOCABULARY_PAGES` are not imported. Every current member is covered by those tuples or the `docs/development/*` glob, but a future checker input outside both would not trip the guard. | `tools/tests/test_ci_process_job.py` lines 68–75; `tools/check_process_consistency.py` tuples. All 64 current checker paths return `process=true`; `model-economics.md` and `design-proportionality.md` are covered by `docs/development/*`. | When the checker grows, union the remaining tuples (or every file the checker reads) into the guard; not a defect in the current tree. |

## Record state

**State:** Ready for implementation — round 2 complete at `06a861b` (request tip `ee4327d`); F-1 is closed and
no Blocker or Major remains. F-7 (Minor) and F-8–F-10 (Notes) are recorded for owner acceptance or deferral.

`Ready for review` means the design and the reviewed tree are frozen at `f00fd0f` and the request is complete. A
`Changes required` verdict returns the work to the dev for a fix commit and a fresh round; `Ready for
implementation` (or an owner-accepted `Ready with notes`) permits the merge. This record is a policy-update record:
`Re-review required` is a state, not a verdict.

## Verdict

Reviewer only. Tick one.

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

Follow-up owner: Dev
Follow-up change: F-1 (Major, Spec) — add the omitted normative process paths to the `process` arm, both normative filter lists, and the filter proof. F-2/F-3 are Minor and can ride with the fix; F-4–F-6 are Notes for owner acceptance or deferral.

### Re-review round 2 verdict — 2026-11-27

Reviewer only. Tick one.

- [x] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Waived

No Blocker or Major remains. Round-1 F-1 is closed; F-2–F-5 are closed; F-6 is accepted. The remaining F-7
(Minor, Spec) and F-8–F-10 (Notes, Standards) are non-blocking under the Full stop rule and do not justify another
round. Follow-up owner: Dev for the F-7 wording-or-paths choice and F-9 metadata; Lead/Dev to recheck the merge
tree against current `main` immediately before merge (F-8).

## Human approval

- [ ] Accepted: Ready for implementation
- [ ] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by:
Date:

## Dev resolutions — round 1

Dev-owned resolution and evidence section. The review agent's round-1 findings, evidence, verdict, and checklist
text above are unchanged.

**Fix commit (round-2 reviewed tree):** `06a861b6c123b9cb796309a9a7332cc237a08734`.
**Request tip:** this documentation-only record commit; its parent is the fix commit.

| Finding | Resolution |
| --- | --- |
| F-1 | Added bounded `process` patterns for the planning index and roadmap; closeout, incident, research, prototype, and retrospective homes; `tools/check_closeout_evidence.py`; and `tools/verify.sh`. The workflow, `ci-policy.md` §3, and `AGENTS.md` §3.13 now agree. `test_every_process_checker_input_matches_the_workflow_filter` imports `REQUIRED_FILES`, `NORMATIVE_FILES`, and `VERDICT_FILES` from the checker and requires every exported path, plus the two gate scripts, to match a live workflow pattern. Representative paths also exercise the shipped Bash filter. The checker was not weakened. |
| F-2 | Added `process/binding` to the Ubuntu-only job list in the `ci.yml` header comment. |
| F-3 | Renamed this record with `git mv` to `policy-2026-11-27-ci-process-guardrails.md` and updated its `Record path` field. No other Markdown link targeted the old name. The original naming rationale and the reviewer's old-name evidence remain as historical round-1 text, as requested. |
| F-4 | Qualified the `force_all=true` decision-table row with `platforms=ubuntu` or `all` and stated that Windows/macOS still skip the Ubuntu-only process job. |
| F-5 | Chose the wording fix: `ci-policy.md` §5 and `AGENTS.md` §3.13 now require owner approval before adding a new "run or job" while the budget guard is active. |
| F-6 | Accepted. The approved combined Full pass reviews the design and implemented tree at one commit, so no separate post-implementation verification section is added. |

### Resolution evidence

| Check | Result |
| --- | --- |
| Pre-fix red reproduction | `detect(["docs/planning/closeout/_template.json"])` returned `process=false`; assertion exited 1. |
| New regression before the fix | `Ran 1 test`; `FAILED (failures=13)` for the 13 omitted checker/gate paths. |
| Filtered coverage after the fix | 73 distinct checker/process paths each returned `process=true` and every other area flag `false`; `tools/contracts/manifest.json`, `tools/check-license-headers.py`, and `src/dotnet/Mocassin.Core/Particle.cs` remained `process=false`. |
| Negative proof at `06a861b` | In a disposable `git archive` of the fix commit, added `docs/planning/future-required-file.md` to `REQUIRED_FILES`; the new test failed only for that path: `Ran 1 test`, `FAILED (failures=1)`, exit 1 as expected. The scratch tree was removed. |
| `python -m unittest discover -s tools/tests -t .` | `Ran 78 tests in 0.886s` — `OK`. |
| `python tools/check_process_consistency.py` | `Process consistency: OK`. |
| `python tools/check_docs_links.py` | `Documentation links OK (238 files scanned).` |
| `python tools/check_doc_math.py` | `Documentation math conventions OK (234 files scanned).` |
| `./tools/verify.sh docs --require` | Exit 0; .NET build 0 warnings/errors; math and link checks passed; strict Zensical `No issues found`; DocFX 0 warnings/errors; rustdoc documented 7 crates; site built; required closeout gate completed. |
| `./tools/verify.sh policy --require` | Exit 0; public API compatible; 26 contract artifacts current; 274 space-group settings current; process checker and 78 tool tests passed; SPDX headers in 1211 source files; no secret patterns in 1790 tracked text files; `cargo deny` advisories/bans/licences/sources OK; required closeout gate completed. |

No CI run was dispatched. This remains a maintenance branch with local validation only; no merge, tag, or `main`
push is part of this resolution.

## Dev resolutions — F-7 follow-up

Dev-owned resolution and evidence for round-2 finding F-7. The review agent's findings, evidence, verdict, and
checklist text above are unchanged.

**Fix commit:** `97b6c48e9dd640c46bcd9b5b264c5dea7d166b62` on `fix/ci-process-filter`; owner approved the merge on
2026-11-27.

F-7 is resolved. The `process` arm now covers every tracked Markdown file the documentation checks read: the root
guides (`README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `AGENTS.md`, `GLOSSARY.md`) plus `docs/**`, with the
pre-existing explicit process paths retained. `test_every_documentation_markdown_file_matches_the_process_filter`
derives the file set from the documentation link checker's scan and requires every file to match a live filter
pattern.

Red proof at `97b6c48`: removing `docs/**` left 122 paths uncovered, including step pages, ADRs, and a temporary
`docs/planning/steps/zz-f7-red-proof.md`; removing the `README.md` token made the shipped-filter proof fail with
`process=false`. The owner accepted the cost: documentation changes now start the job, which installs the single
pinned `msgpack==1.2.4` wheel and takes about one billable minute, with no .NET or Rust build.
