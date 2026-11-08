# Step 03 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-03.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Audit
> status`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 03 — Particle and Structure Models |
| Request owner | Step agent |
| Audit initiated by | Step agent |
| Audit status | Not requested |
| Audited commit | — |
| Step branch | `step/03-particle-and-structure-models` |
| Step tag | — |
| Closeout CI run | — |
| Platform CI | Not run (Ubuntu-only policy) |
| Audit date | — |
| Auditor | — |

Step page: [`Step 03 — Particle and Structure Models`](../steps/step-03-particle-and-structure-models.md)

## Step summary

| Field | Value |
| --- | --- |
| Phase | B. Model definition |
| Depends on | 02 |
| Delivers | Particle, particle-set, space-group, cell-parameter, cell-site, and dummy-position models with validation and a stable public API. |

## Step-specific acceptance checklist

- [ ] A user can define ceria: 225, a=5.41 Å, name "Ceria", Ce at (0,0,0), O at (0.25,0.25,0.25), and an
      unstable O site at (0.5,0.25,0.25), using the public editing API.
- [ ] The preview shows the correct P1 orbit sizes for each site.
- [ ] Overlapping sites are rejected with a `CellSiteId` path to each offending site through the structured
      `OrbitOverlap` result; no message parsing is used.
- [ ] Crystal-system constraints are enforced without silently overwriting user input.
- [ ] The structure preview is deterministic and cached by
      `(structureHash, A, B, C, catalogDataVersion, catalogDataSha256, symmetryContractVersion)`;
      `structureHash` is the canonical structure-model hash.
- [ ] The ceria/BZO preview benchmark case and baseline are recorded; the target is mean < 100 ms on the Step
      02 benchmark runner class. The benchmark runs in Nightly/release, not as a PR-fast wall-clock assertion.
- [ ] A preview request whose `A*B*C*P` exceeds `int.MaxValue` returns `STR014` and no preview.
- [ ] `"model": {}` still loads as `ModelDefinition.Empty`, and the existing minimal/ceria fixtures round-trip.
- [ ] JSON round-trips use `[a,b,c]` fractional arrays, camelCase enum strings, and no `isVoid` field.
- [ ] A project with `symmetry` metadata parses in Core, is included in the document hash, and is compared by
      `ProjectValidationService`; a mismatch emits `PROJ034`.
- [ ] `ProjectStore.LoadBytesCore` returns Core, Symmetry, and `PROJ034` diagnostics through the composed
      validator.
- [ ] Shuffling authored particles, sets, sites, or dummies does not change the canonical model hash.
- [ ] All diagnostics are returned by the composed public validation API. CLI exposure is verified in Steps
      07/12; UI exposure is verified in Step 24.
- [ ] The generated project schema accepts the structure (including `name`), particles, and optional symmetry
      fields, and fixtures cover present, absent, default-empty, and mismatched metadata.
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

```

## Known notes for the auditor

- The frozen audit commit is the squashed closeout commit on
  `step/03-particle-and-structure-models`. The closeout CI run URL and audited commit are added by a
  documentation-only `[skip ci]` commit after the squash; CI is green at the same tree.
- The ceria sample keeps the unstable oxygen transition site in the shared oxygen set. A dedicated
  transition-state occupation set was deliberately not added; the original reference example has one, but the
  approved Step 03 feature set excludes transition semantics, which belong to Step 04. The user retracted an
  earlier misreading that the ceria transition position was missing.
- The barium zirconate proton reference position at `(0.734, 0.5, 0)` is a **stable** defect site. Its offset
  `0.234` is a representative structural value (≈0.98 Å on a 4.19 Å cell), not a fitted parameterization.
  Proton-hopping transitions are Step 04 work.
- The samples use one shared uncharged vacancy particle per model, with a symbol distinct from the host
  species. Ceria uses `Vacancy`/`Vc`; barium zirconate uses `Vacancy`/`V` for both the oxygen and proton
  sets. Additional vacancy particles are reserved for distinct charged vacancy states.
- `STR008` and `STR011` are removed from the design and documented as unused. `STR014` (index cap) and
  `STR015` (dummy name validation) were added. `PAR010`–`PAR012` were added.
- `StructurePreviewService.TryCreatePreview` uses the parameter name `structureModel` instead of the design's
  `structure` to satisfy CA1716. `IStructureQuery.Structure` uses a member-level CA1716 suppression.
- The generated project schema remains `schemaVersion: 1` because the Step 03 fields are additive and the
  model shape is pre-release.
- CLI and UI diagnostic exposure is intentionally verified in Steps 07/12 and 24, not in this step.
- `samples-and-docs` was amended before squash specifically for the vacancy-symbol, shared-vacancy, and
  stable-proton corrections. The audit should verify the samples, fixtures, and guide text agree.

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
