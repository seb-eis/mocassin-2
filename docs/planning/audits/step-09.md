# Step 09 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-09.md`. You may edit only your own metadata (`Auditor`, `Audit date`,
> `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 09 — Parameterization Templates |
| Request owner | Step agent |
| Orchestrator (step agent) | General planner/orchestrator |
| Audit initiated by | Human owner |
| Audited commit | `20c73bcb3b1643320ad5acbdd6ff5c0d18e2ad5b` |
| Step branch | `step/09-parameterization-templates` |
| Step tag | — |
| Closeout CI run | [`43630049214`](https://github.com/seb-eis/mocassin-2/actions/runs/43630049214) — green at `20c73bcb3b1643320ad5acbdd6ff5c0d18e2ad5b` |
| Closeout CI head SHA | `20c73bcb3b1643320ad5acbdd6ff5c0d18e2ad5b` |
| Main SHA at freeze | `b81355e00946f7abee5c4a56be23d8e1f777743e` |
| Merge-tree CI | Not required; `main` `b81355e` is an ancestor of the audited commit |
| Closeout evidence | `docs/planning/closeout/step-09.json` |
| Platform CI | Not run (Ubuntu-only policy; no platform-specific code) |
| Audit date | — |
| Auditor | — |

Step page: [`Step 09 — Parameterization Templates`](../steps/step-09-parameterization-templates.md)

> Orchestration: the step agent (the orchestrator) freezes the step branch, requests the auditor, applies fixes
> autonomously on a new feature branch, reruns closeout CI, adds resolutions without altering reviewer text,
> and requests re-audit until the auditor no longer identifies issues. The human owner approves the final
> verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 08 |
| Delivers | Project-resident, sparse parameterization templates with stable compiled identities, a values-only parameterization signature, deterministic apply/defaulting, site-energy overrides, `PRM` validation, diff/rebase reports, completeness checks, golden templates/vectors, and documentation. |

## Step-specific acceptance checklist

- [ ] Creating a template from the ceria compiled model produces exactly the expected sets/rules.
- [ ] Applying the template to the same model succeeds and yields the same compiled values.
- [ ] A changed model produces a signature mismatch with a useful diff.
- [ ] Rebase copies unchanged values, defaults new ones, and reports removed ones without silent reuse.
- [ ] Chiral partners share one `EnergyId` and one value; independent A→B/B→A values are unrepresentable.
- [ ] Site energy overrides apply and can be reset to the authored model value.
- [ ] Missing/zero values produce warnings, not silent success.
- [ ] Template JSON is canonical and round-trips, including culture, `-0.0`, and additive missing arrays.
- [ ] The parameterization signature is deterministic, finalized in the manifest, and reproduced by the
      checked-in projection vector.
- [ ] Untrusted-template limits reject oversized counts/strings/values with `PRM` errors before allocation.
- [ ] Documentation written.

## Standard audit checklist

- [ ] Required tests from the step page pass.
- [ ] Public/protected XML documentation policy is followed.
- [ ] Documentation, status dashboard, feature log, and parity matrix are current.
- [ ] Closeout CI is green at the audited commit.
- [ ] Closeout evidence matches the audited commit and the merge candidate.
- [ ] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded.
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

- The design review is `Ready with notes` at `26d6a0e` (round 4), accepted by the human owner on 2026-11-14;
  see `docs/planning/reviews/step-09.md`. Owner decisions: single-step 3.4–4.4 person-week exception, absolute
  `frequency_hz` model payload, and the implicit default build path.
- Feature-level deviations recorded in the Step 09 page:
  - `PRM005` with typed units: negative/non-finite frequencies are rejected by the JSON unit converters;
    structural `PRM005` covers zero.
  - Editor missing-set references use the existing `PROJ010` code; there is no Core `PRM` code for that
    editor-only case.
  - `PRM014` also rejects duplicate group headers and empty transition IDs.
  - `ParameterizationTemplateDefaults.CreateDefault` and `MaxAppliedValueBytes` enforcement landed in feature 4.
  - `PRM001/002/008/012` were registered in feature 4; feature 5 completed the catalog and `Validate`.
  - `Apply` returns no `ParameterizedModel` for error diagnostics (`PRM001/004/005/013`); warnings still produce
    a model.
  - The §4 pair-table invariant was clarified: every table has at least one geometry; achiral descriptors
    agree; a chiral table has exactly one `GeometryId == EnergyId` representative plus reciprocal links.
  - `NearestGeometry` is pair-only, requires the same stable/unstable kind and particle pair, and excludes
    chiral entries; ambiguous matches copy nothing and are reported.
  - The design's `100 fm` comment was corrected: 100 fm is 0.001 Å, not 0.1 Å.
  - Ceria-KMC golden values are the implicit-default apply (the sample has no template); ceria and BaZrO₃ use
    their checked-in populated templates.
  - Benchmark numbers were measured in the development container and are order-of-magnitude evidence.
- Closeout: exactly one closeout CI dispatch (`43630049214`); no per-feature CI runs; Windows/macOS not run.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** Ready for audit

`Re-audit required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner or implementor.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
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

## Human approval

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:

## Audit rounds

Append each new round below. Never overwrite an earlier round.

### Audit round 1

#### Independence and frozen state

#### Commands and results

#### Findings

#### Verdict
