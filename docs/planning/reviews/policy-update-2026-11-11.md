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
| Target commit | `e0abc5c3e9e75469260ccab09528a7d086b0dac2` (round 3; round 1 `92a7ea3`, round 2 `f1e7045`) |
| Request owner | Planner / implementor |
| Orchestrator (implementor) | Step agent |
| Review initiated by | Human owner |
| Reviewer | Fresh independent reviewer — round 3 (subagent; read-only) |
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

**State:** Passed (round 3)

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

Resolutions applied at `f1e7045feef8f83752941ff603503b40fb635410`; reviewer text above is unchanged.

| Finding | Status | Resolution | Commit |
| --- | --- | --- | --- |
| F1 | Fixed | Invalid or unresolved SHAs now return before git history commands; added a negative test. | `f1e7045` |
| F2 | Fixed | Root guides (`AGENTS.md`, `README.md`, `CONTRIBUTING.md`, `SECURITY.md`) are treated as documentation; added a root-docs test and clarified the closeout wording. | `f1e7045` |
| F3 | Fixed | Removed the duplicate `Design review status` / `Audit status` / `Status` request fields; `Record state` is the single state field in all three templates and the three XC records. | `f1e7045` |
| F4 | Fixed | Removed the restated closeout sequence from `agent-workflow.md`; `AGENTS.md` §7.2 now links to the roadmap sizing section instead of repeating it. | `f1e7045` |
| F5 | Fixed | `AGENTS.md` §7.1 now records `In audit` before `Done`; all `In audit` definitions cover the frozen audit-request state. | `e0abc5c` |
| N1 | Fixed | Added four focused negative tests for the process checker (six new tool tests; 18 total). | `f1e7045` |
| N2 | Fixed | `--closeout` now wins regardless of argument order; `--list` reports the selected mode. | `f1e7045` |
| N3 | Fixed | Added the `Changes required` / `Fixes required` vocabulary mapping to `AGENTS.md` §3.15. | `f1e7045` |

## Re-review request (round 2)

Round 2 target: `f1e7045feef8f83752941ff603503b40fb635410`.

Round 1 verdict: `Passed with notes` at `92a7ea3`. The Minor findings and Notes above have been resolved at the
round 2 target. The review request is otherwise unchanged.

### Round 2 evidence

I am a fresh context with no shared conversation with the planner or implementor. I did not author or
implement this work. Review basis: round 2 target `f1e7045feef8f83752941ff603503b40fb635410`, review request
commit `88caace270f7b3038e3b7122c035472f66f2c3dc` (which changes only this record), and the implementation set
`987d74c..f1e7045`. The worktree is `/tmp/opencode/mocassin-policy-update`; the working tree was clean before
my record edit.

Files reviewed: the full changed set `git diff 987d74c..f1e7045` (62 files: documentation, process records,
tooling, and tool tests only; no `src/`, `contracts/`, or runtime code), with a detailed read of the round 2
delta `92a7ea3..f1e7045`: `AGENTS.md`, `docs/development/agent-workflow.md`, `docs/planning/status.md`,
`docs/planning/04-roadmap.md`, `docs/planning/audits/README.md`, `docs/planning/audits/step-audit-template.md`,
`docs/planning/reviews/README.md`, `docs/planning/reviews/step-design-review-template.md`,
`docs/planning/reviews/cross-cutting/README.md`, the cross-cutting template and `xc-01`–`xc-03`,
`docs/planning/closeout/README.md`, `tools/check_closeout_evidence.py`, `tools/check_process_consistency.py`,
`tools/tests/test_closeout_evidence.py`, `tools/tests/test_process_consistency.py`, and `tools/verify.sh`.
I also read the round 1 findings and the remediation table, and compared the claims with the tree.

Commands and results at `88caace` (whose only delta from `f1e7045` is the request):

- `git diff --stat 987d74c..f1e7045` — 62 files changed, 1970 insertions, 3832 deletions; docs, process, and
  tooling only.
- `git diff --check` and `git diff --check 987d74c..f1e7045` — exit 0.
- `python tools/check_process_consistency.py` — `Process consistency: OK`.
- `python tools/check_docs_links.py` — `Documentation links OK (142 files scanned)`.
- `python -m unittest discover -s tools/tests -t . -q` — `Ran 18 tests`, `OK`. Verbose run: 7 closeout-checker
  tests, 6 docs-link tests, 5 process-consistency tests (the repository test plus four new negative tests).
- `./tools/verify.sh all --require` — exit 0. .NET restore/build/test/format, API compatibility, Rust
  fmt/clippy/test, Python pytest/build, Zensical strict, DocFX, rustdoc, contract/symmetry drift, process
  consistency, tool tests, SPDX, secret scan, and `cargo-deny` all ran; final line `Closeout gate completed
  with required checks enabled.`
- `zensical build --strict` — exit 0, `No issues found`.
- `bash -n tools/verify.sh` — syntax OK.
- Checked that the only round 2 record change in `f1e7045` is the removal of the old request `Status` field, and
  that `88caace` changes only this record.

Finding verification and reproductions:

- **F1 (fixed).** `python tools/check_closeout_evidence.py docs/planning/closeout/_template.json` exits 1 with
  per-field `does not resolve to a commit` messages and no traceback. `check_closeout_evidence.py:74-88`
  returns before any git history call once a SHA is format-invalid or unresolved. The new
  `test_unresolved_sha_reports_without_traceback` would fail on the old code.
- **F2 (fixed).** Reproduced in a scratch git repository: a candidate changing only `AGENTS.md` and `README.md`
  now yields `Closeout evidence: OK`; a candidate changing `code.py` still yields the two `merge_tree_ci`
  errors. The new `test_root_docs_candidate_is_documentation_only` covers this, and the closeout README now
  names the root guides explicitly.
- **F3 (fixed).** The request-level `Design review status`, `Audit status`, and `Status` fields are gone from
  the three templates and the three `xc-01`–`xc-03` records; `Record state` is the single reviewer-owned state
  field. The reviewer write scopes in both READMEs were updated to match. Older records such as `step-02`,
  `step-04`, and `step-05` still contain the historical field, which is correct history and was not rewritten.
- **F4 (fixed).** The restated closeout sequence was removed from `docs/development/agent-workflow.md`; it now
  links to `AGENTS.md` §3.4 and the closeout README. `AGENTS.md` §7.2 links to the roadmap sizing section
  instead of duplicating its bullets; `04-roadmap.md` §3.1 is now the single home. The added
  `#31-step-sizing-and-split-triggers` anchor resolves under the link check.
- **F5 (partially fixed).** `AGENTS.md` §7.1 now records `In audit` before `Done`, and `status.md` now defines
  `In audit` as "frozen at the audit request; pending or running". However, the normative audit-status table
  that `status.md` points to still defines `In audit` as "A fresh audit context is reviewing the frozen
  commit", so the lifecycle mismatch called out in F5 remains (see R1).
- **N1 (resolved).** Four new temporary-tree negative tests cover the forbidden term, a record state in a
  verdict list, a pre-seeded audit for a `Not started` step, and a missing `In audit` status; the checker now
  correctly reads the dashboard status column. Note R2 on the remediation-table wording.
- **N2 (resolved).** `./tools/verify.sh --list` prints `Mode: all`; `docs --list` prints `Mode: docs`;
  both `--closeout docs --list` and `docs --closeout --list` print `Mode: all`, so `--closeout` now wins
  regardless of argument order, and `--list` reflects the selected mode. The `docs --closeout docs` execution
  path was inspected rather than run in full to avoid a duplicate full gate, which had already passed.
- **N3 (resolved).** `AGENTS.md` §3.15 now states the `Changes required` / `Fixes required` mapping and that
  each family keeps its own vocabulary.

Regressions: none found. The `check_process_consistency` dashboard parser change now reads the `Status` column
(index 3), which matches the actual `status.md` table; the old greedy regex captured the last column. No code,
contracts, or runtime behaviour changed. I left the round 1 checklist marks untouched; the round 2 item-level
results are recorded above.

### Round 2 findings

| ID | Severity | Area | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- |
| R1 (F5 residual) | Minor | Status drift | The `In audit` status still has two conflicting definitions. `status.md` says the step is frozen at the audit request and the audit is "pending or running", but `audits/README.md` — the page `status.md` names as the source of the audit statuses — still says "A fresh audit context is reviewing the frozen commit". F5 asked for the definition to be aligned; only `status.md` and `AGENTS.md` were updated. | `docs/planning/audits/README.md:41`; `docs/planning/status.md:14,45`; `AGENTS.md:778`. | Change the `In audit` row in `docs/planning/audits/README.md` to the same "pending or running" wording, then this finding closes. |
| R2 | Note | Remediation record accuracy | The F5 resolution says "the `In audit` definition covers the frozen audit-request state", which is only true for `status.md`/`AGENTS.md`, and the N1 resolution says "six negative tests for the process checker"; the commit adds four process-checker tests (the six new tests comprise four process-checker and two closeout-checker tests). N1's requested scenarios themselves are covered. | Remediation table rows F5/N1; `tools/tests/test_process_consistency.py`; verbose unittest run. | Reword the two resolution rows: note that the audit README still needs the wording change, and say four process-checker tests / six new tool tests. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**.

### Round 2 verdict

- [ ] Passed
- [x] Passed with notes
- [ ] Fixes required
- [ ] Waived

Round 2 closes F1–F4 and N1–N3. The two closeout-checker defects are fixed and covered by tests, the templates
use a single `Record state`, the one-home duplication is removed, `AGENTS.md` §7.1 now records the
`In audit`/`Done` sequence, the process checker has negative tests, `--closeout` is order-independent, and the
vocabulary mapping is stated. No Blocker or Major finding remains and no regression was introduced.

F5 remains partly open: the `In audit` wording is still inconsistent in `docs/planning/audits/README.md` (R1,
Minor), and the remediation table overstates two resolutions (R2, Note). The verdict is `Passed with notes`
because these are non-blocking, the full local and documentation gates pass, and the owner may accept R1/R2 as
follow-ups before the next process-maintenance change. The record state is `Re-review required` only if the
implementor applies the R1/R2 wording fixes and wants them verified; otherwise the human owner can accept the
notes and close the review.

Follow-up owner: Planner / implementor
Follow-up step: Next process-maintenance change (`docs/policy-follow-up`)

## Re-review request (round 3)

Round 3 target: `e0abc5c3e9e75469260ccab09528a7d086b0dac2`.

Round 2 verdict: `Passed with notes` at `f1e7045`. R1 and R2 are resolved at the round 3 target:

- **R1** — `docs/planning/audits/README.md` now defines `In audit` as "frozen at the audit request; a fresh
  audit context is pending or reviewing the frozen commit", matching `status.md` and `AGENTS.md`.
- **R2** — the remediation table now says four process-checker negative tests (six new tool tests; 18 total)
  and records the round 3 commit for F5.

### Round 3 evidence

I am a fresh context with no shared conversation with the planner or implementor. I did not author or
implement this work. Review basis: round 3 target `e0abc5c3e9e75469260ccab09528a7d086b0dac2`, review request
commit `1b067fd038aab89ee61c99f34919a65766adb1d1` (which changes only this record), and the implementation
set `987d74c..e0abc5c`. The worktree is `/tmp/opencode/mocassin-policy-update`; the working tree was clean
before my record edit.

Files reviewed: the full changed set `git diff 987d74c..e0abc5c` (62 files: documentation, process records,
tooling, and tool tests only; no `src/`, `contracts/`, or runtime code), with a detailed read of the round 3
delta `f1e7045..e0abc5c`: `docs/planning/audits/README.md` and the two remediation rows in this record.
I also re-read `AGENTS.md`, `docs/planning/status.md`, `docs/planning/reviews/README.md`,
`docs/planning/audits/step-audit-template.md`, `docs/planning/closeout/README.md`,
`docs/development/agent-workflow.md`, `docs/development/drift-checks.md`,
`docs/development/agent-security.md`, `docs/development/incident-and-rollback.md`, the three review/audit
templates, `docs/planning/closeout/_template.json`, `tools/verify.sh`, the three checkers, and the tool tests.

Commands and results at `1b067fd` (whose only delta from `e0abc5c` is the round 3 request):

- `git diff --stat 987d74c..e0abc5c` — 62 files changed, 2091 insertions, 3833 deletions; docs, process, and
  tooling only. `git show --stat e0abc5c` — 2 files: `docs/planning/audits/README.md` (one status-row wording
  change) and this record (two remediation rows). `git show --stat 1b067fd` — this record only.
- `git diff --check` and `git diff --check 987d74c..e0abc5c` — both exit 0.
- `python tools/check_process_consistency.py` — `Process consistency: OK`.
- `python tools/check_docs_links.py` — `Documentation links OK (142 files scanned)`.
- `python -m unittest discover -s tools/tests -t . -q` — `Ran 18 tests`, `OK` (7 closeout-checker, 6
  docs-link, 5 process-consistency tests).
- `./tools/verify.sh all --require` — exit 0. `git diff --check`, .NET restore/build/test/format, API and
  dependency gates, Rust fmt/clippy/test, Python pytest/build, documentation/link check, DocFX, rustdoc,
  contract/symmetry drift, process consistency, tool tests, SPDX headers, secret scan, and `cargo-deny` all
  ran; final line `Closeout gate completed with required checks enabled.`
- `zensical build --strict` — exit 0, `No issues found`.
- `bash -n tools/verify.sh` — syntax OK; the `docs` and `policy` modes referenced by the process pages exist.

Finding verification and reproductions:

- **R1 (closed).** All `In audit` definitions now agree: `docs/planning/audits/README.md:41` says "The step
  branch is frozen at the audit request; a fresh audit context is pending or reviewing the frozen commit",
  `docs/planning/status.md:14` says "The step branch is frozen at the audit request; a fresh completion audit
  is pending or running", and `AGENTS.md:778` records `In audit` at the audit request and `Done` only after the
  merge. The step-audit template only lists the state, and the drift-check and planning README references use
  the same lifecycle. No conflicting definition remains.
- **R2 (closed).** The remediation table now records F5 as fixed at `e0abc5c` with all `In audit` definitions
  covering the frozen audit-request state, and N1 as four focused process-checker negative tests (six new tool
  tests; 18 total). The diff `92a7ea3..f1e7045` adds exactly six tool tests: two closeout-checker tests and
  four process-consistency tests; the current suite has 7 + 6 + 5 = 18. Both corrected claims match the tree.
- Re-checked the earlier resolutions against the tree: the closeout checker returns before git history on
  unresolved SHAs (`check_closeout_evidence.py:74-90`); root guides are treated as documentation
  (`DOC_FILES`); the three templates and `xc-01`–`xc-03` use `Record state`; `agent-workflow.md` links the
  gate sequences and sizing has one home; the `Changes required` / `Fixes required` mapping is in
  `AGENTS.md` §3.15. Historical records that still contain the old status fields were correctly not rewritten.

Regressions: none found. The round 3 change is a one-line normative wording alignment plus two correction
rows. No code, contracts, runtime behaviour, approved decision, link, or status vocabulary changed, and the
full local and documentation gates pass at the target commit. I left the round 1/2 checklist marks and all
earlier evidence, findings, and verdicts untouched; the round 3 results are recorded here.

### Round 3 findings

None. R1 (Minor) and R2 (Note) are closed, and re-verification found no regression, contradiction, stale
wording, broken link, or incorrect claim in the approved decision set.

### Round 3 verdict

- [x] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Waived

Round 3 closes R1 and R2. All `In audit` definitions agree on the frozen-audit-request lifecycle, and the
remediation table now accurately records the F5 and N1 resolutions. The full local gate, link check, tool
tests, and strict documentation build pass at the target commit. No Blocker, Major, Minor, or Note finding
remains from any round, and the approved decision set is implemented without regression. The human owner
still approves the final verdict and merge.

Follow-up owner: Planner / implementor
Follow-up step: None required

## Human approval

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:
