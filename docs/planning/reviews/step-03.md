# Step 03 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-03.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Design
> review status`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 03 — Particle and Structure Models |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Design review status | Ready for review |
| Reviewed commit | `1beb8da8751d3df55865e9f4a592b20ca17d33a1` (`1beb8da`) |
| Docs branch | `docs/step-03-design-fixes` |
| Review date | — |
| Reviewer | — |

Step page: [`Step 03 — Particle and Structure Models`](../steps/step-03-particle-and-structure-models.md)

### Pre-review corrections

The design was revised before the first review; this is not a review round and there are no reviewer findings
yet. The corrections are in the reviewed commit `1beb8da`:

- `ModelDefinition` is flat so the JSON shape is `model.particles` / `model.particleSets` / `model.structure`;
  `ParticleModel` is an in-memory grouping only.
- Added explicit JSON mapping rules for `Fractional3` arrays, camelCase enums, `[JsonIgnore]` on `Particle.IsVoid`,
  reserved IDs, and canonical array sorting.
- Added `ModelDefinition.Empty` / `StructureModel.Default` so a Step 01 `"model": {}` document still loads.
- Added `StructureModel.Name`, `STR012` (1000-reference-site limit), `STR013` (structure name), `PAR008`
  (symbol warning), and `PAR009` (duplicate set warning).
- Defined `PROJ034` ownership in `Mocassin.Packages.Projects.ProjectValidationService`; Core remains
  catalog-free.
- Corrected the unreachable-diagnostic cases: `PAR003` is documented as unused, `STR004` is a pre-trim range
  check, and `STR006` covers missing/default set IDs.
- Defined `TryResolve`/`TryCreatePreview`, preview cache keys, and deterministic preview evidence.
- Added the composed validation owner, typed editor operations, performance target, test tiers, property
  tests, parity-matrix task, and CLI/UI acceptance split.
- Recorded the deliberate validation deviations from the original.

## Step summary

| Field | Value |
| --- | --- |
| Phase | B. Model definition |
| Depends on | 02 |
| Delivers | Particle, particle-set, space-group, cell-parameter, cell-site, and dummy-position models with validation and a stable public API. |

## Design decisions reviewed

- [ ] Step page design decisions
- [ ] Data contracts and format changes
- [ ] Architecture and module boundaries
- [ ] ADRs and open decisions
- [ ] Quality strategy and test plan
- [ ] Parity matrix and documentation plan
- [ ] Licensing and provenance
- [ ] Post-implementation audit plan

## Dependency and interface checks

- [ ] Dependencies are `Done` and their contracts are stable
- [ ] Inputs, outputs, types, and formats are defined
- [ ] Versioning and migration behavior is defined
- [ ] No unresolved cross-step interface question remains

## Standard design-review checklist

- [ ] Scope, non-goals, and acceptance criteria are explicit and testable
- [ ] Design decisions are complete and internally consistent
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined
- [ ] Documentation, status, parity, and audit updates are planned
- [ ] Licensing, provenance, and dependency checks are resolved
- [ ] Risks and mitigations are realistic
- [ ] Code layout and namespace plan follow the structure policy
- [ ] No hidden scope, stale placeholders, or unowned decisions

## Evidence

Files reviewed:

Commands run:

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Re-review required
- [ ] Waived

Follow-up owner:
Follow-up step:

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

- [ ] Accepted: Ready for implementation
- [ ] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by:
Date:

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.
