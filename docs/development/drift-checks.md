# Drift Checks

Cross-cutting reviews are the full phase-end check. A **drift check** is the lightweight, frequent check that
keeps the phase-end review from becoming a rescue operation.

Run a drift check:

- monthly, or at least once per milestone;
- before each phase boundary;
- after a process, CI-policy, architecture, or status change;
- when someone notices stale instructions, broken links, or contradictory status.

A drift check should take about 15–30 minutes and does not require a fresh reviewer. Record the result in
[`docs/planning/status.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/status.md) and escalate to a full cross-cutting review when you find
Blocker/Major drift. Use [`docs/planning/drift-checks/_template.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/drift-checks/_template.md).

## Checklist

### Policy and process

- [ ] `AGENTS.md`, `docs/development/orchestration.md`, `docs/development/agent-workflow.md`, and
      `docs/development/contributing.md` agree on branches, approvals, gates, orchestrator routing, and reviewer
      roles.
- [ ] The CI workflow matches the run matrix and `tools/verify.sh`.
- [ ] No documented command, path, mode, or tag is stale.
- [ ] Status and verdict vocabularies agree across process, templates, and records.
- [ ] No retired branch, CI matrix, review name, or tool is still described as current.
- [ ] `Re-review required` and `Re-audit required` are record states, not verdicts.

### Status and records

- [ ] The dashboard matches tags, merges, step pages, reviews, audits, and amendments.
- [ ] No step is `In progress` without a branch or owner.
- [ ] No review or audit record is stuck in `Ready for ...` or `In ...` without an owner.
- [ ] Review and audit records are append-only; no earlier round was rewritten.
- [ ] No audit record is pre-seeded for a step that is still `Not started`.
- [ ] Closeout evidence exists for every step at or past `In audit`.

### Architecture and contracts

- [ ] The architecture page matches the actual module and dependency direction.
- [ ] Public APIs, formats, schemas, and compatibility promises match the current code.
- [ ] Generated artifacts and fixtures are current.
- [ ] ADRs and open decisions have owners and current status.
- [ ] No step has grown beyond its size or split trigger without a recorded decision.

### Documentation

- [ ] `./tools/verify.sh docs --require` passes, including the planning-doc link check.
- [ ] Tutorial and reference commands still run against a sample.
- [ ] No page claims a future feature is implemented.
- [ ] Navigation has no orphan pages; `docs/planning/` is excluded from the published site.
- [ ] Generated API docs build without missing public items.

### Security and operations

- [ ] Secret scan, licence headers, and dependency checks pass.
- [ ] Dependency alerts and security advisories have owners.
- [ ] CI actions are pinned and workflow permissions are minimal.
- [ ] Platform gaps are recorded with a review date, not left as permanent accepted risk.
- [ ] The incident and rollback path is still current.
- [ ] Agent access is still least-privilege; no token or credential was exposed.

## Record

Record the result in the drift-check table in [`docs/planning/status.md`](https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/status.md). A drift check
is not a gate; it is a maintenance signal. If it finds Blocker/Major drift, open a maintenance branch and, when
the drift is broad, start the relevant cross-cutting review.

## Escalation

| Finding | Action |
| --- | --- |
| Stale command, link, or status | Fix in a `docs/<slug>` branch. |
| Contradictory process rule | Decide the normative home, fix the summary, and run `tools/check_process_consistency.py`. |
| Architecture or contract drift | Open a maintenance branch; use an ADR or amendment if the design changed. |
| Repeated or broad drift | Start the next cross-cutting review early; record why the phase boundary was not sufficient. |
| Security or dependency finding | Follow [`incident-and-rollback.md`](incident-and-rollback.md) and remediate or rotate immediately. |
