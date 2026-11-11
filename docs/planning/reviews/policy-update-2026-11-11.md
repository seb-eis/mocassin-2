# Policy Update 2026-11-11 — Workflow and Tooling Adaptation

> Reviewer: write evidence, findings, and verdict directly into this record. You may edit only your own metadata
> (`Reviewer`, `Review date`, `Record state`), checklist completion marks and inline verification notes (not item
> text), `Evidence`, `Findings`, and `Verdict`. Commit only this file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration, or any
> other file.

## Request

| Field | Value |
| --- | --- |
| Review ID | `POLICY-2026-11-11` |
| Type | Process and workflow policy update |
| Target commit | `92a7ea372be21bc7c0cb557d4a799b174aeef9a9` |
| Request owner | Planner / implementor |
| Orchestrator (implementor) | Step agent |
| Review initiated by | Human owner |
| Reviewer | Fresh independent reviewer (subagent; read-only) |
| Review date | 2026-11-11 |

## Purpose

Verify that the approved policy update implements the selected workflow improvements, is internally consistent,
does not regress existing process, and is safe to merge as a documentation-and-tooling change.

This is a process review, not a step design review or implementation audit.

## Approved decisions

The owner selected:

1. JSON closeout evidence plus checker and the merge-tree rule.
2. Process consistency checker plus stricter local gates.
3. Records created on demand, with unused pre-seeded audit records removed.
4. `AGENTS.md` §3 as the normative home; other guides trimmed to links and checklists.
5. Monthly and phase-boundary drift checks.
6. A full incident and rollback page plus postmortem template.
7. A full agent-security threat model and checklist.
8. Template cleanups: record state vs verdict, independence statement, `In audit` status, review-invalidation rules.
9. Step sizing and split triggers in the roadmap and process guidance.
10. Release checklist deferred to Step 29/31.
11. Batch-only feature approval retained.
12. No adoption profiles, no steps-to-work-packages rename, no bootstrap tooling.

## Scope

- `AGENTS.md`
- `CONTRIBUTING.md`
- `docs/development/agent-workflow.md`, `contributing.md`, `code-structure.md`, `index.md`
- `docs/development/drift-checks.md`, `incident-and-rollback.md`, `agent-security.md`
- `docs/planning/README.md`, `status.md`, `04-roadmap.md`, `ci-policy.md`
- `docs/planning/audits/README.md`, `step-audit-template.md`
- `docs/planning/reviews/README.md`, `step-design-review-template.md`
- `docs/planning/reviews/cross-cutting/README.md`, `cross-cutting-review-template.md`, `xc-01`–`xc-03`
- `docs/planning/closeout/README.md`, `_template.json`
- `docs/planning/drift-checks/_template.md`
- `docs/planning/incidents/_template.md`
- `tools/verify.sh`, `tools/README.md`, `tools/check_closeout_evidence.py`, `tools/check_docs_links.py`,
  `tools/check_process_consistency.py`, `tools/__init__.py`, `tools/tests/`
- Removed unused audit records `docs/planning/audits/step-07.md` through `step-31.md`

## Request owner evidence

Commands run at the target commit:

```text
./tools/verify.sh all --require
python tools/check_process_consistency.py
python tools/check_docs_links.py
python -m unittest discover -s tools/tests -t . -q
zensical build --strict
```

Results:

- `./tools/verify.sh all --require` — pass, including .NET, API, Rust, Python, docs, contracts, process
  consistency, tool tests, SPDX, secret scan, and `cargo-deny`.
- `check_process_consistency` — `Process consistency: OK`.
- `check_docs_links` — `Documentation links OK (141 files scanned)`.
- Tool tests — 12 tests pass.
- `zensical build --strict` — no issues.

## Evidence

Review basis: frozen target `92a7ea372be21bc7c0cb557d4a799b174aeef9a9`, changed set
`git diff 987d74c..92a7ea3`, and review request commit `0f6363a` (which adds only this record). The worktree is
`/tmp/opencode/mocassin-policy-update` at `0f6363a86ae2d7552c44493ecbdc49d4c534a9d3`. I am a fresh context with
no shared conversation with the planner or implementor, and I did not author or implement this work.

Files reviewed:

- `AGENTS.md` and the changed sections of `CONTRIBUTING.md`.
- `docs/development/agent-workflow.md`, `agent-security.md`, `drift-checks.md`,
  `incident-and-rollback.md`, `contributing.md`, `code-structure.md`, and `index.md`.
- `docs/planning/README.md`, `status.md`, `04-roadmap.md`, and `ci-policy.md`.
- `docs/planning/reviews/README.md`, `step-design-review-template.md`, the three cross-cutting records, the
  cross-cutting README/template, and this record.
- `docs/planning/audits/README.md` and `step-audit-template.md`.
- `docs/planning/closeout/README.md` and `_template.json`; `docs/planning/drift-checks/_template.md`; and
  `docs/planning/incidents/_template.md`.
- `tools/verify.sh`, `tools/README.md`, `tools/check_closeout_evidence.py`, `tools/check_docs_links.py`,
  `tools/check_process_consistency.py`, and `tools/tests/`.
- The removal of `docs/planning/audits/step-07.md` through `step-31.md`, plus references to those paths.

Commands and results:

- `git diff --stat 987d74c..92a7ea3` — 61 files changed, 1665 insertions, 3817 deletions; only documentation,
  process records, and tooling/tests changed. No `src/`, `contracts/`, or runtime code changed.
- `git diff --check` and `git diff --check 987d74c..92a7ea3` — both exit 0.
- `python tools/check_process_consistency.py` — `Process consistency: OK` at `0f6363a` and at a detached
  worktree of `92a7ea3`.
- `python tools/check_docs_links.py` — `Documentation links OK (142 files scanned)` at `0f6363a`; `141 files
  scanned` at `92a7ea3`, matching the request owner's evidence.
- `python -m unittest discover -s tools/tests -t . -q` — `Ran 12 tests`, `OK` at `0f6363a` and `92a7ea3`.
- `./tools/verify.sh all --require` — exit 0 at `0f6363a`; .NET restore/build/test/format, API compatibility,
  Rust fmt/clippy/test, Python pytest/build, Zensical strict, DocFX, rustdoc, contract/symmetry drift, process
  consistency, tool tests, SPDX, secret scan, and `cargo-deny` all ran. `cargo-deny` is installed in this
  environment, so the missing-`cargo-deny` failure path was inspected in the script rather than executed.
- `zensical build --strict` — exit 0, `No issues found`.
- `bash -n tools/verify.sh` — syntax OK. `./tools/verify.sh --list` exits 0; an unknown argument exits 2.
- Reproduced finding F1: `python tools/check_closeout_evidence.py docs/planning/closeout/_template.json`
  raises an uncaught `subprocess.CalledProcessError` because the zero SHAs are format-valid but unresolved.
- Reproduced finding F2: in a scratch repository, a merge candidate that changes only `AGENTS.md` is treated as a
  code change and requires `merge_tree_ci`.
- Searched for links to the removed audit records. No Markdown link points at them; the remaining references are
  historical prose or `git show` evidence in `docs/planning/reviews/step-05.md`, which should not be rewritten.
- Checked the approved decisions against the files: closeout evidence/checker/merge-tree rule, consistency
  checker and stricter gates, on-demand records, drift checks, incident/rollback, agent security, template
  cleanups, step sizing, deferred release checklist, batch-only feature approval, and the excluded
  adoption/rename/bootstrap changes are present.

## Checklist

### A. Approved decisions

- [x] Closeout evidence JSON, checker, audit fields, and merge-tree rule are present and coherent. (F1/F2 are
  checker edge cases, not missing fields.)
- [x] Process consistency checker and `--require`/`--closeout` gates are present and runnable.
- [x] Review/audit records are on demand; unused pre-seeded audit records are removed; no broken links remain.
- [ ] `AGENTS.md` §3 is the normative home and other guides do not restate the same gates. (F4: one restatement
  and one verbatim duplication remain.)
- [x] Drift-check page, template, status table, and monthly/phase-boundary cadence are present.
- [x] Incident/rollback page and postmortem template are present and linked.
- [x] Agent-security page covers permissions, secrets, prompt injection, reviewer manipulation, and process bypass.
- [ ] Record-state vs verdict, independence statement, `In audit` status, and review-invalidation rules are
  consistent across templates and READMEs. (F3 notes the legacy status fields that sit beside `Record state`;
  F5 notes the `In audit` lifecycle mismatch.)
- [x] Step sizing/split triggers are in the roadmap and process guidance. (See F4 for the duplication concern.)
- [x] Release checklist is deferred without leaving a contradictory instruction.
- [x] Batch-only feature approval is retained; no per-feature approval gate remains.
- [x] No adoption profiles, terminology rename, or bootstrap tooling were introduced.

### B. Consistency and correctness

- [ ] Status and verdict vocabularies agree across process pages, templates, and records. (N3: design reviews
  use `Changes required`, while audits and cross-cutting reviews use `Fixes required` for the equivalent state;
  F5 records the related `Done`/`In audit` timing drift.)
- [x] `Re-review required` and `Re-audit required` are record states, not verdicts.
- [x] All relative links in the changed pages resolve, including planning pages.
- [ ] No stale terms, old file names, or obsolete approval wording remain in normative pages. (F4 and F5.)
- [x] Closeout evidence fields match the checker and the audit template. (Field names align; F1/F2 concern
  checker behaviour on edge cases.)
- [x] `tools/verify.sh` modes and flags are documented and behave as documented. (N2 notes the `--closeout`
  argument-order and `--list` edge cases.)
- [x] `--require` fails on a missing required closeout check; `--closeout` implies `all --require`. (Verified by
  inspection and by the installed `cargo-deny` path; `--closeout` is order-dependent, see N2.)
- [x] The new checkers have tests and the tests pass. (N1 notes that the process-consistency test is
  happy-path only.)
- [x] The change does not alter code, tests, contracts, or runtime behavior. (No `src/` or `contracts/` change;
  tool tests and `verify.sh` are in the approved scope.)
- [x] The removed audit records are genuinely unused and no step page or process page links to them.

### C. Exit criteria

- [x] The full local gate passes at the target commit. (Run at `0f6363a`, whose only delta from `92a7ea3` is
  this record.)
- [x] The documentation build and link check pass.
- [x] No Blocker or Major finding remains.
- [x] Minor/Note findings have owners and follow-ups, or are explicitly accepted. (Follow-up owner is the
  planner/implementor; see the Verdict section.)
- [ ] The human owner approves the final verdict and merge.

## Findings

| ID | Severity | Area | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- |
| F1 | Minor | Closeout checker robustness | `check_closeout_evidence.py` raises an uncaught `subprocess.CalledProcessError` when a format-valid SHA does not resolve (including the zero SHAs in its own `_template.json`), because the `audit..candidate` diff is run before the unresolved-SHA errors stop the check. The promise to report "does not resolve to a commit" is lost, and the tool prints a traceback. The gate still exits non-zero, so this is not a bypass. | `tools/check_closeout_evidence.py:90-107`; reproduced with `python tools/check_closeout_evidence.py docs/planning/closeout/_template.json` (traceback at line 95). `tools/tests/test_closeout_evidence.py` has no unresolved-SHA case. | Guard the diff and ancestry calls with `commit_exists`/`SHA_RE` checks, or return the collected errors before running git history commands; add a negative test using the template and a 40-hex non-existent SHA. |
| F2 | Minor | Closeout checker correctness | The documentation-only test uses `DOC_PREFIXES = ("docs/",)`, so a merge candidate that changes only root documentation (`AGENTS.md`, `README.md`, `CONTRIBUTING.md`) is treated as a code change and requires a `merge_tree_ci` run. This contradicts the closeout README's "non-documentation files" wording and can force an unnecessary closeout CI run in a cost-sensitive policy. | `tools/check_closeout_evidence.py:21,94-104`; reproduced in a scratch git repository: a candidate changing only `AGENTS.md` produced "merge_candidate changes files outside docs/" and "merge_tree_ci.run_id is required when the merge candidate changes code". `docs/planning/closeout/README.md` "The checker fails when". | Treat the root Markdown guides as documentation, or narrow the README wording to "files outside `docs/`" and document the policy explicitly. |
| F3 | Minor | Template consistency | The design-review, audit, and cross-cutting templates each retain an older status field (`Design review status`, `Audit status`, `Status`) and also add a separate `Record state` section with the same or overlapping vocabulary. Both fields are reviewer-editable and no checker compares them, so a record can show two different states without detection. This weakens the "record state vs verdict" cleanup. | `docs/planning/reviews/step-design-review-template.md:18,84-88`; `docs/planning/audits/step-audit-template.md:18,91-95`; `docs/planning/reviews/cross-cutting/cross-cutting-review-template.md:16,87-91`; write scopes in `docs/planning/reviews/README.md:121-124` and `docs/planning/audits/README.md:159-161`. | Keep one reviewer-owned state field (the `Record state`) and define the request status as implementor-owned, or remove the duplicate section; add a consistency check if both remain. |
| F4 | Minor | One normative home | The one-home rule is not fully applied. `docs/development/agent-workflow.md` §13 says "This playbook does not restate the gate sequences", then restates a six-step closeout sequence. The step-sizing bullets are duplicated verbatim in `AGENTS.md` §7.2 and `docs/planning/04-roadmap.md` §3.1, contradicting `AGENTS.md` §3.15 ("Summaries link to it; do not copy it into a second page that can drift"). | `docs/development/agent-workflow.md:280-315` (self-contradiction: line 282 and the sequence that follows); `AGENTS.md:785-798` and `docs/planning/04-roadmap.md:93-105` (identical five-bullet block); `AGENTS.md:553-555` (§3.15). | Replace the closeout sequence with a link to `AGENTS.md` §3.4 or `docs/planning/closeout/README.md`; choose one home for step sizing and link to it from the other page. |
| F5 | Minor | Status drift | The new `In audit` status is not reflected in `AGENTS.md` §7.1, which still says the closeout feature records `Done`, the closeout CI result, and the main merge reference. `docs/planning/status.md` now says the closeout feature records `In audit`, and `Done` only after the merge. In addition, `In audit` is defined as "a fresh completion audit is running" while the closeout feature sets it before the audit record reaches `Ready for audit`. | `AGENTS.md:774-778`; `docs/planning/status.md:14,26-30`; `docs/planning/audits/README.md:40-41,55`. | Update `AGENTS.md` §7.1 to the new `In audit`/`Done` sequence, and align the `In audit` definition with when the step actually enters that state. |
| N1 | Note | Test coverage | The process-consistency checker has one test that asserts the current repository is clean; there are no negative tests for a missing required file, a forbidden term, a mis-seeded audit record, or a state placed in a verdict list. A regression in the checker would not be caught. | `tools/tests/test_process_consistency.py` contains a single `test_repository_process_is_consistent`; the other checkers have positive and negative cases. | Add focused temporary-tree tests for each check. |
| N2 | Note | `verify.sh` CLI edges | `--closeout` is parsed as an alias for `all --require`, but argument order changes the result: `./tools/verify.sh --closeout docs` leaves the mode as `docs` and only enables `--require`. `--list` prints the full catalogue even when a mode is selected, although the usage text says "List the selected modes and checks". | `tools/verify.sh:50-64,72-87` (loop assigns in order; `--closeout` sets `mode="all"`); `--list` block prints all modes. | Reject a mode combined with `--closeout`, or make `--closeout` win regardless of order; make `--list` reflect the selected mode or reword the option. |
| N3 | Note | Vocabulary | Design reviews use `Changes required` while audits and cross-cutting reviews use `Fixes required` for the equivalent failure state. The dashboard documents the split per column, but no page states the mapping, so the "one status vocabulary" intent of the prior repository-policy review remains only partly met. | `docs/planning/reviews/README.md:44-55`; `docs/planning/audits/README.md:35-46`; `docs/planning/reviews/cross-cutting/README.md:112-123`; `docs/planning/status.md:45-46`. | State the mapping explicitly, or use one failure-state name across all three record families. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**.

## Record state

**State:** **Passed with notes**

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner or implementor.

## Verdict

- [ ] Passed
- [x] Passed with notes
- [ ] Fixes required
- [ ] Waived

The approved decision set is implemented and the full local and documentation gates pass at the target
commit. No Blocker or Major finding remains. Five Minor findings (F1–F5) and three Notes (N1–N3) are recorded
above; none blocks merge, but F1–F5 should be resolved or explicitly accepted before the next step closeout.
The gate is passed with notes because the closeout checker has two real edge-case defects, the templates keep
duplicate state fields, and the one-home/status wording is not yet fully consistent.

Follow-up owner: Planner / implementor
Follow-up step: Next process-maintenance change (`docs/policy-follow-up`)

## Remediation status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

## Human approval

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:
