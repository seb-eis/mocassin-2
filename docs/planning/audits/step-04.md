# Step 04 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-04.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Audit
> status`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 04 — Transition Model |
| Request owner | Step agent |
| Audit initiated by | Human owner |
| Audit status | Not requested |
| Audited commit | — |
| Step branch | `step/04-transition-model` |
| Step tag | — |
| Closeout CI run | — |
| Platform CI | Not run (Ubuntu-only policy) |
| Audit date | — |
| Auditor | — |

Step page: [`Step 04 — Transition Model`](../steps/step-04-transition-model.md)

## Step summary

| Field | Value |
| --- | --- |
| Phase | B. Model definition |
| Depends on | 03 |
| Delivers | State changes, state-change groups, abstract chains, and KMC/MMC transitions bound to reference geometry, with validation of transition semantics. |

## Step-specific acceptance checklist

- [ ] Ceria oxygen migration can be defined as a 3-site chain with `[O,V_O]`, `[O,∅]`, `[O,V_O]` and a
      stable-unstable-stable path whose endpoints are symmetry-equivalent O orbit members, not only the single
      authored O representative.
- [ ] A 2-site MMC exchange `[A,B]` can be defined and previewed, including a same-site exchange.
- [ ] A 5-site vehicle with five chain groups (`[A,V_A]`, `[A,V_A]`, `[AB,∅]`, `[B,V_B]`, `[B,V_B]`),
      a `D,S,S,D` chain, and an `S,S,U,S,S` reference path validates.
- [ ] A 5-site interstitialcy with an `S,U,S,U,S` path validates.
- [ ] Invalid paths (unstable endpoint, consecutive unstable, pattern-sequence mismatch, any repeated
      encoded position, unmatched point) are rejected with precise diagnostics.
- [ ] A complex transition without a reverse link warns `TRN018`; adding a symmetric reverse link clears it.
- [ ] `GeneratesReverse` set inconsistently with the pattern is rejected with `TRN024`.
- [ ] The preview enumerates the expected symmetry-equivalent paths and echoes `(A, B, C)` and the transition
      reference.
- [ ] An invalid transition returns `null` and diagnostics from `TryCreatePreview`; no partial preview is
      returned.
- [ ] `StateChangeMatrices` has the documented dimensions, particle order, binary encoding, and
      stable/unstable cardinality.
- [ ] The transition sections round-trip through save/load, the project JSON context, and the canonical model;
      semantic arrays preserve order and transition edits change the document hash.
- [ ] The generated project schema accepts the transition sections, and fixtures cover empty and populated
      models.
- [ ] Public API files and the ApiCompat baseline are current.
- [ ] Documentation written.

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
Pending closeout. The step agent records the exact `./tools/verify.sh all` output and closeout CI run here.
```

## Known notes for the auditor

- Empty transition sections are omitted from the written model so transition-free Step 01/03 projects keep
  their existing canonical model/document hashes. Non-empty sections are written and hashed; `schemaVersion`
  remains 1 under the additive-v1 precedent.
- `TransitionPreview` is derived data and is never persisted.
- The transition preview benchmark is a local-container measurement; the declared GitHub runner class is
  re-verified only by the manual Benchmark CI job.
- Platform CI is not run; Step 04 adds no platform-specific code.
- The transition-preview cache is bounded and scoped to the service; capacity breach is an explicit failure.

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
