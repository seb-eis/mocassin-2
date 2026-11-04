# Step 01 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 01 — Domain Kernel and Project Format |
| Audit status | **Pending — Step 01 in progress** |
| Audited commit | — |
| Step branch | `step/01-domain-kernel` |
| Step tag | — |
| CI run | — |
| Audit date | — |
| Auditor | — |

Do not start this audit until the status is changed to **Ready for audit** and the audited commit is filled in.

## Scope

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
