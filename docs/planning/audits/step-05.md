# Step 05 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-05.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Audit
> status`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 05 — Energy Model |
| Request owner | Step agent |
| Audit initiated by | Planner / implementor / human owner |
| Audit status | Not requested |
| Audited commit | — |
| Step branch | — |
| Step tag | — |
| Closeout CI run | — |
| Platform CI | Not run (Ubuntu-only policy) |
| Audit date | — |
| Auditor | — |

Step page: [`Step 05 — Energy Model`](../steps/step-05-energy-model.md)

## Step summary

| Field | Value |
| --- | --- |
| Phase | B. Model definition |
| Depends on | 03, 04 |
| Delivers | Stable/unstable interaction environments, cutoff filters, multi-body clusters, and site energies, with validation and cost previews. |

## Step-specific acceptance checklist

- [ ] Ceria's stable environment and oxygen migration-edge cluster validate.
- [ ] A stable filter that removes a specific sublattice pair is applied symmetrically; an unstable filter is
      one-directional and keeps `SiteAId` equal to the environment site.
- [ ] Filter boundaries are inclusive and use the 1e-5 Å distance tolerance.
- [ ] A cluster with 2 or 8 offsets is accepted; 1 or 9 offsets are rejected (`ENM008`).
- [ ] Duplicate, center-equivalent, unknown/unstable/filtered-surrounding, ring/degenerate, and out-of-range
      clusters are rejected with paths (`ENM009`–`ENM011`).
- [ ] The cost preview reports pair counts and permutation counts that match a brute-force oracle, including
      the center-particle factor and `long` overflow-safe limits; the Step 08 cross-check is recorded as a
      Step 08 follow-up, not a Step 05 closeout claim.
- [ ] Chiral pair partners are detected, share one `EnergyId`, and expose `ChiralPartnerGeometryId`;
      independent A→B/B→A energy values are unrepresentable.
- [ ] The energy sections round-trip; the default `stableEnvironment` loads the 1.0 Å/no-filter default; empty
      sections are omitted; semantic arrays preserve order.
- [ ] Canonical collection ordering and `ProjectHasher.ComputeEnergy` are stable under array shuffles and
      change when energy data changes.
- [ ] The generated project schema accepts the energy sections, and fixtures cover empty and populated models.
- [ ] Every `ENM001`–`ENM021` code has a positive or negative test as applicable, including the ENM018
      Step 07 boundary.
- [ ] New files carry SPDX headers and introduce no new dependency.
- [ ] Public API files and the ApiCompat baseline are current.
- [ ] Documentation pages, navigation, index links, status dashboard, and parity matrix are updated.

## Standard audit checklist

- [ ] Required tests from the step page pass.
- [ ] Public/protected XML documentation policy is followed.
- [ ] Documentation, status dashboard, feature log, and parity matrix are current.
- [ ] Closeout CI is green at the audited commit.
- [ ] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code.
- [ ] Format/version/fixture changes are documented and tested.
- [ ] Security, dependency, licence, and secret-scan gates pass.
- [ ] Code structure policy passes; no new root-level or catch-all folders.
- [ ] No unrelated changes or unapproved scope.
- [ ] Branch, tag, and status hygiene are correct.

## Evidence

Commands run:

```text

```

## Known notes for the auditor

- Record known limitations, accepted deviations, or areas needing extra attention.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Re-audit required
- [ ] Waived

Follow-up owner:
Follow-up issue/step:

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Approval summary

The step agent completes this section before requesting human approval. It must cover the full history, not
only the latest round.

- [ ] Requested decision and frozen commit recorded
- [ ] Every review/audit round recorded (round, date, commit, reviewer, verdict)
- [ ] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status)
- [ ] Accepted notes and waivers recorded with rationale
- [ ] Residual risks, deferred items, and follow-up owners recorded
- [ ] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived
- [ ] Final CI and documentation-build results recorded

Round history:

| Round | Date | Commit | Reviewer | Verdict |
| --- | --- | --- | --- | --- |
| — | — | — | — | — |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Human approval

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:
