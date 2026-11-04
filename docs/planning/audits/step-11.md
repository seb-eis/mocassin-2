# Step 11 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 11 — Simulation Package and `.msl` Writer |
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
| Phase | C. Compilation |
| Depends on | 08, 09, 10 |
| Delivers | A versioned SQLite simulation package with content-addressed compiled models and lattices, version checks, atomic writes, and inspection tools. |

Step page: [`Step 11 — Simulation Package and .msl Writer`](../steps/step-11-simulation-package-and-msl-writer.md)

## Step-specific acceptance checklist

- [ ] A package built from the ceria sample opens in SQLite and contains the expected tables/rows.
- [ ] A published package opens read-only with no `-wal`/`-shm` files and has no results table.
- [ ] Every job references a valid model and lattice; signatures verify.
- [ ] Rebuilding the same project produces identical model/lattice signatures; stored compressed bytes may differ but the canonical projections must match.
- [ ] A cancelled build leaves no temp file and does not modify an existing package.
- [ ] A simulated disk-full/error leaves the previous package intact.
- [ ] `mocassin inspect` and `verify` report accurate information.
- [ ] 10⁴ small jobs build within the time/size budget.
- [ ] No EF Core or native structs are used.
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
