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
| Status | Ready for review |
| Target commit | `92a7ea372be21bc7c0cb557d4a799b174aeef9a9` |
| Request owner | Planner / implementor |
| Orchestrator (implementor) | Step agent |
| Review initiated by | Human owner |
| Reviewer | — |
| Review date | — |

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

Files reviewed:

Commands run:

## Checklist

### A. Approved decisions

- [ ] Closeout evidence JSON, checker, audit fields, and merge-tree rule are present and coherent.
- [ ] Process consistency checker and `--require`/`--closeout` gates are present and runnable.
- [ ] Review/audit records are on demand; unused pre-seeded audit records are removed; no broken links remain.
- [ ] `AGENTS.md` §3 is the normative home and other guides do not restate the same gates.
- [ ] Drift-check page, template, status table, and monthly/phase-boundary cadence are present.
- [ ] Incident/rollback page and postmortem template are present and linked.
- [ ] Agent-security page covers permissions, secrets, prompt injection, reviewer manipulation, and process bypass.
- [ ] Record-state vs verdict, independence statement, `In audit` status, and review-invalidation rules are
  consistent across templates and READMEs.
- [ ] Step sizing/split triggers are in the roadmap and process guidance.
- [ ] Release checklist is deferred without leaving a contradictory instruction.
- [ ] Batch-only feature approval is retained; no per-feature approval gate remains.
- [ ] No adoption profiles, terminology rename, or bootstrap tooling were introduced.

### B. Consistency and correctness

- [ ] Status and verdict vocabularies agree across process pages, templates, and records.
- [ ] `Re-review required` and `Re-audit required` are record states, not verdicts.
- [ ] All relative links in the changed pages resolve, including planning pages.
- [ ] No stale terms, old file names, or obsolete approval wording remain in normative pages.
- [ ] Closeout evidence fields match the checker and the audit template.
- [ ] `tools/verify.sh` modes and flags are documented and behave as documented.
- [ ] `--require` fails on a missing required closeout check; `--closeout` implies `all --require`.
- [ ] The new checkers have tests and the tests pass.
- [ ] The change does not alter code, tests, contracts, or runtime behavior.
- [ ] The removed audit records are genuinely unused and no step page or process page links to them.

### C. Exit criteria

- [ ] The full local gate passes at the target commit.
- [ ] The documentation build and link check pass.
- [ ] No Blocker or Major finding remains.
- [ ] Minor/Note findings have owners and follow-ups, or are explicitly accepted.
- [ ] The human owner approves the final verdict and merge.

## Findings

| ID | Severity | Area | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**.

## Record state

**State:** Ready for review / In review / Fixes required / Re-review required / Passed / Passed with notes / Waived

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner or implementor.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Waived

Follow-up owner:
Follow-up step:

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
