# Step 24 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 24 — Particle, Structure, and Transition Editors |
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
| Phase | F. GUI |
| Depends on | 23, 03, 04 |
| Delivers | Production editors for particles/sets, crystal structure/sites, and the full transition model, with live validation, previews, and undo/redo. |

Step page: [`Step 24 — Particle, Structure, and Transition Editors`](../steps/step-24-model-editors-particle-structure-transitions.md)

## Step-specific acceptance checklist

- [ ] A user can create the ceria particles, sets, structure, and oxygen migration without the CLI.
- [ ] Drag-and-drop and keyboard-only flows both work for particle sets.
- [ ] Changing the space group previews the new orbit sizes before applying.
- [ ] Overlapping sites and invalid paths are blocked with inline errors.
- [ ] The chain builder supports all built-in patterns and custom linkers.
- [ ] KMC path editing shows stability colors and symmetry-expanded path count.
- [ ] Every diagnostic navigates to the offending field.
- [ ] Undo/redo works across all edits.
- [ ] Large grids/previews remain responsive.
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
