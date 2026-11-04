# Step 03 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 03 — Particle and Structure Models |
| Request owner | Step agent |
| Audit status | Not requested |
| Audited commit | — |
| Step branch | — |
| Step tag | — |
| CI run | — |
| Audit date | — |
| Auditor | — |

## Step summary

| Field | Value |
| --- | --- |
| Phase | B. Model definition |
| Depends on | 02 |
| Delivers | Particle, particle-set, space-group, cell-parameter, cell-site, and dummy-position models with validation and a stable public API. |

Step page: [`Step 03 — Particle and Structure Models`](../steps/step-03-particle-and-structure-models.md)

## Step-specific acceptance checklist

- [ ] A user can define ceria: 225, a=5.41 Å, Ce at (0,0,0), O at (0.25,0.25,0.25), and an unstable O site at (0.5,0.25,0.25).
- [ ] The preview shows the correct P1 orbit sizes for each site.
- [ ] Overlapping sites are rejected with a path to the offending site.
- [ ] Crystal-system constraints are enforced without silently overwriting user input.
- [ ] The structure preview is deterministic and cached by content hash.
- [ ] All diagnostics are reachable from the CLI and UI.
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
