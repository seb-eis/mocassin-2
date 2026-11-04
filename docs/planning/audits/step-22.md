# Step 22 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 22 — Python Analysis API |
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
| Phase | E. Results |
| Depends on | 18, 20 |
| Delivers | A small, read-only Python package for `.mcs`/`.msr` files with basic multi-file evaluations, pytest coverage, and mkdocstrings-generated API docs inside the Zensical site. |

Step page: [`Step 22 — Python Analysis API`](../steps/step-22-python-analysis-api.md)

## Step-specific acceptance checklist

- [ ] `read_mcs` reads KMC and MMC fixtures and returns the expected metadata, lattice, counters, trackers, and histograms.
- [ ] `read_msr` reads results/status metadata and extracts `.mcs` blobs with package binding checks.
- [ ] The Python reader matches the .NET reader on the same fixture.
- [ ] Single-file helpers match hand calculations and .NET evaluation values within tolerance, including per-species/total transport and origin-based MSD.
- [ ] Multi-file filtering/grouping/aggregation works and reports excluded files.
- [ ] Zero-field and `z == 0` cases return `None`/`nan`, never `Infinity`/`NaN` silently.
- [ ] Malformed/unsupported files raise typed errors with path and field context.
- [ ] `pytest` passes on Python 3.11–3.13 with coverage ≥ 85 %.
- [ ] `mkdocstrings` renders the API under `/api/python/` in the Zensical site.
- [ ] The wheel/sdist builds and installs in a clean environment.
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
