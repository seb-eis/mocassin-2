# Step 01 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 01 — Domain Kernel and Project Format |
| Request owner | Step agent |
| Audit status | **Not requested** — Step 01 is in progress |
| Audited commit | — |
| Step branch | `step/01-domain-kernel` |
| Step tag | — |
| CI run | — |
| Audit date | — |
| Auditor | — |

Step page: [`Step 01 — Domain Kernel and Project Format`](../steps/step-01-domain-kernel-and-project-format.md)

Do not start this audit until the status is changed to **Ready for audit** and the audited commit is filled in.

## Step summary

| Field | Value |
| --- | --- |
| Phase | A. Foundations |
| Depends on | 00 |
| Delivers | The immutable domain kernel, stable IDs, units, diagnostics, and the versioned JSON project file with canonical serialization and migrations. |

## Audit scope

- [ ] Domain kernel, immutable project aggregate, IDs, units, diagnostics, and editing primitives
- [ ] Versioned project JSON, canonical serialization, and canonical hash rules
- [ ] Schema migrations and future-version rejection
- [ ] Atomic save, `.bak`, lock file, stale-lock recovery, and autosave behavior
- [ ] Reference validation and diagnostic codes
- [ ] Project store behavior and round-trip tests
- [ ] Property tests for units, canonicalization, and edit invariants
- [ ] Public/protected XML documentation policy
- [ ] Documentation, status dashboard, feature log, and parity matrix
- [ ] CI result, scope hygiene, and no unapproved format changes

## Step-specific acceptance checklist

- [ ] A `Project` round-trips through JSON with no loss for all sample projects.
- [ ] Canonical serialization is byte-identical after reordering collections.
- [ ] Invalid references (unknown IDs, wrong domain IDs) produce diagnostics with paths.
- [ ] A future `schemaVersion` is rejected with a clear message.
- [ ] A v1 fixture loads and saves without changes.
- [ ] A failed save (simulated I/O error) leaves the original file intact.
- [ ] A lock file prevents concurrent edits and recovers from a stale lock.
- [ ] The JSON Schema validates all fixtures.
- [ ] Documentation written

## Standard audit checklist

- [ ] Required tests from the step page pass.
- [ ] Public/protected XML documentation policy is followed.
- [ ] Documentation, status dashboard, feature log, and parity matrix are current.
- [ ] CI is green at the audited commit.
- [ ] Format/version/fixture changes are documented and tested.
- [ ] Security, dependency, licence, and secret-scan gates pass.
- [ ] No unrelated changes or unapproved scope.
- [ ] Branch, tag, and status hygiene are correct.

## Evidence

Commands run:

```text

```

## Known notes for the auditor

- Step 01 was in progress when the audit structure was created.
- The step agent must freeze the step branch and update this file before the audit starts.
- The step must be re-audited after any fix commit; do not audit a moving branch.

## Findings

| Severity | Finding | Evidence | Resolution |
| --- | --- | --- | --- |
| — | — | — | — |

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Re-audit required
- [ ] Waived

Follow-up owner:
Follow-up issue/step:
