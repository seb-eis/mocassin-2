# Step 25 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 25 — Energy, Lattice, and Simulation Editors |
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
| Depends on | 24, 05, 06 |
| Delivers | Editors for interaction environments, clusters, site energies, doping/lattice definitions, and MMC/KMC simulations, with cost previews and live validation. |

Step page: [`Step 25 — Energy, Lattice, and Simulation Editors`](../steps/step-25-model-editors-energy-lattice-simulations.md)

## Step-specific acceptance checklist

- [ ] A user can define the ceria stable environment, migration-edge cluster, and site energies in the GUI.
- [ ] Filters are editable as a grid and reflected in the radial diagram.
- [ ] Cluster geometry shows offsets and rejects rings/duplicates inline.
- [ ] Site energies support bulk edit and CSV import/export.
- [ ] The cost preview matches the compiler counts for the ceria sample.
- [ ] A ceria reduction doping can be defined and previewed for charge neutrality.
- [ ] KMC/MMC simulations can be created with all settings and transitions.
- [ ] Zero-field/modulus mismatch and missing transitions are caught inline.
- [ ] Undo/redo works across all edits.
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
