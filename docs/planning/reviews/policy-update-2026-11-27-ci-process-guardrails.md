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
| Record path | `docs/planning/reviews/policy-update-2026-11-27-ci-process-guardrails.md` |
| Design class | **Full** |
| Design-class trigger | [`docs/development/design-proportionality.md`](../../development/design-proportionality.md) §1, “Policy-update reviews”: the change edits the **CI policy** and the **enforcement coverage of a gate** (which changes start the process/binding guardrails). |
| Branch | `fix/ci-process-checks` |
| Base commit | `be930c4e1d21bb96cde08bdb67ed20cb59631a20` (`origin/main` at the fix freeze) |
| Reviewed commit | `f00fd0fe1b8b529fe703f9a8fd43af87d0d59589` |
| Request tip | The record commit whose parent is `f00fd0f` (this commit; no rule change) |
| Request owner | Dev |
| Review initiated by | Lead |
| Review agent | — (not yet assigned) |
| Review date | — (not yet assigned) |
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
| `workflow_dispatch` with `force_all=true` | **runs** | Every area flag is forced true |
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

- [ ] **Spec** — The request states the concrete failure (guardrails added, no CI consumer, two green merges) and it
      is reproducible from the branch and the gate history.
- [ ] **Spec** — Full is the right class: the trigger names the CI policy and the gate's enforcement coverage, and
      the change is not argued down to Light.
- [ ] **Spec** — Scope and non-goals are explicit; the non-goals protect the cost policy (no new run, no widened
      job) and the isolated review boundary (no Step 15, no binding change).

### B. Job scope and cheapness

- [ ] **Spec** — The job runs exactly the four guardrails that lacked a consumer, and no other check moves into it.
- [ ] **Standards** — It installs no dependency, builds neither .NET nor Rust, has a timeout, and uses only the
      standard library.
- [ ] **Standards** — It is registered where the other Ubuntu-only jobs are, follows their checkout/setup style, and
      does not inherit a matrix.

### C. Trigger correctness

- [ ] **Spec** — Every path family the four checks read is covered, and the case arm, `ci-policy.md` §3, and
      `AGENTS.md` §3.13 agree.
- [ ] **Spec** — The four added families each have a stated reason tied to a check the job runs.
- [ ] **Standards** — No other job is widened: a contracts-only path still enables only `contracts`, and a code path
      never enables `process`.
- [ ] **Spec** — `ci.yml`, `force_all`, and `platforms=all` enable the job; `platforms=windows`/`macos` do not.

### D. Cost and budget

- [ ] **Spec** — The added cost is bounded and described, and the §5 budget-guard reading is recorded as an owner
      decision rather than assumed.
- [ ] **Standards** — No new trigger, runner, matrix, secret, or dependency is introduced, so §3.13's CI cost policy
      and the closeout-only rule still hold.

### E. Discovery consistency and tests

- [ ] **Standards** — The job's discovery command equals `tools/verify.sh`'s, and the `-t .` root is what the suite
      needs; a drift guard fails if they diverge.
- [ ] **Spec** — The filter proof covers a positive list, a `.opencode` path, a `docs/planning/status.md` path, and
      the negative `tools/contracts/**` case.
- [ ] **Standards** — The new tests fail on the pre-fix tree for the reasons claimed, and the drift guard is a
      divergence check, not a restatement of the current list.

### F. Documentation, evidence, and gates

- [ ] **Spec** — `ci-policy.md` §2–§3, `AGENTS.md` §3.13, `contributing.md`, and `status.md` agree with the workflow
      and with each other.
- [ ] **Standards** — `check_process_consistency.py`, `check_docs_links.py`, `check_doc_math.py`, and both
      `verify.sh --require` modes pass at the reviewed commit, and the recorded counts match the tree.
- [ ] **Spec** — No step page, audit, closeout, tag, code, contract, format, or generated artifact is touched.
- [ ] **Standards** — The record follows §9.1: request-owned fields, a reviewer-owned section, one verdict, one
      record commit with `[skip ci]`.

## Evidence

Review agent: — (to be filled by the review agent). Include the frozen and reviewed commits, the three-dot diff,
the commands above with their exact output, and the files read.

## Findings

| ID | Axis | Severity | Origin | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- | --- |
| | | | | | | |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. Origin: **design**, **implementation**,
**evidence**, or **documentation**. Axis: **Standards** or **Spec**.

## Record state

**State:** Ready for review

`Ready for review` means the design and the reviewed tree are frozen at `f00fd0f` and the request is complete. A
`Changes required` verdict returns the work to the dev for a fix commit and a fresh round; `Ready for
implementation` (or an owner-accepted `Ready with notes`) permits the merge. This record is a policy-update record:
`Re-review required` is a state, not a verdict.

## Verdict

Reviewer only. Tick one.

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Waived

Follow-up owner:
Follow-up change:

## Human approval

- [ ] Accepted: Ready for implementation
- [ ] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by:
Date:
