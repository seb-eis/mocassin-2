# Step 18 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 18 — Tracking, Statistics, and State Writer |
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
| Phase | D. Solver |
| Depends on | 16, 17 |
| Delivers | Global/static/mobile movement tracking, jump histograms, statistics dumps, derived transport quantities, and the versioned MessagePack state writer/reader. |

Step page: [`Step 18 — Tracking, Statistics, and State Writer`](../steps/step-18-tracking-statistics-and-state-writer.md)

## Step-specific acceptance checklist

- [ ] A single-particle random walk produces the expected origin-based MSD, D, and tracker displacements.
- [ ] Per-species and total transport values use the tracked ensemble count, not total particles.
- [ ] Global trackers separate transition contributions correctly.
- [ ] Static trackers reproduce per-position flow for a simple lattice.
- [ ] Mobile trackers follow particle identity across a 3-site migration.
- [ ] Histograms bin values correctly, including under/overflow.
- [ ] MessagePack state files round-trip through the reader and pass semantic validation.
- [ ] Resume restores lattice, counters, trackers, histograms, RNG state, `K_fix`/`K_norm`/`RawMax`, goals, and time, and continues identically.
- [ ] Corrupt/truncated state files are rejected.
- [ ] Statistics output contains no NaN/Inf for zero-field MMC/KMC cases.
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
