# Step 09 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-09.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 09 — Parameterization Templates |
| Request owner | Step 09 planner |
| Orchestrator (step agent / planner) | General planner/orchestrator |
| Review initiated by | Planner |
| Reviewed commit | `1ee5a2a3f5b6e34b44a1881e46390700e962f6b3` |
| Docs branch | `docs/step-09-design` |
| Review date | 2026-11-14 |
| Reviewer | — |

Step page: [`Step 09 — Parameterization Templates`](../steps/step-09-parameterization-templates.md)

> Orchestration: the step agent or planner (the orchestrator) freezes the design, requests the reviewer, applies
> fixes autonomously on the design docs or a maintenance branch, adds resolutions without altering reviewer
> text, and requests re-review until the reviewer no longer identifies issues. The human owner approves the
> final verdict and the start of implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 08 |
| Delivers | Project-resident, sparse parameterization templates with stable compiled identities, a values-only parameterization signature, deterministic apply/defaulting, site-energy overrides, `PRM` validation, diff/rebase reports, and completeness checks. |

## Design decisions reviewed

- [ ] Step page design decisions
- [ ] Data contracts and format changes
- [ ] Architecture and module boundaries
- [ ] ADRs and open decisions
- [ ] Quality strategy and test plan
- [ ] Parity matrix and documentation plan
- [ ] Licensing and provenance
- [ ] Post-implementation audit plan
- [ ] Ownership: template/value records in Core, `ParameterizationValues` + signature in Contracts, factory/apply/validation/rebase in Packages, `Mocassin.Compilation` unchanged and parameterization-free.
- [ ] Persistence: templates are project-resident in `parameterizations`; `ParameterizationId` is UUIDv7; versioning is the project `schemaVersion`; canonical ordering and no separate template document.
- [ ] Sparse/default semantics: absence means unset; pair/group `0 eV`, rule `1e13 Hz`, site background from authored model values; explicit defaults are real entries; creation writes set headers only.
- [ ] Identity: pair `EnergyId`; group `ClusterEnergyId` + canonical occupation tuple; rule `RuleGeometryId` inside `(transitionKind, transitionId)`; site override `(siteId, particleId)`; geometry-change behavior and chiral one-value/symmetric-table behavior.
- [ ] Parameterization signature: values-only six-field projection, dense compiled order, 12-decimal quantization, excluded runtime settings, manifest finalization, and Step 11/13 consumption.
- [ ] Create/apply API: factory, `IParameterizationService`, `ParameterizedModel`, immutability, mismatch rejection, defaulting, and the site-energy background.
- [ ] Site-energy overrides: background mapping to `[position][particle]`, reset-to-model, Step 15 consumption, and invalid-target behavior.
- [ ] Validation: `PRM` family ownership, Core structural vs Packages compiled-bound checks, catalog composition, paths, no Step 09 `IValidationStage`, and the Step 12 integration.
- [ ] Diff/rebase: strategies, matching rules, report content, no silent reuse, and chiral-link changes.
- [ ] Completeness/quality metrics and warning vs error thresholds.
- [ ] Determinism, untrusted-template bounds, and performance targets/benchmark plan.
- [ ] Testing: acceptance mapping, real compiled models, golden/projection fixtures, property tests, freshness, and tiers.
- [ ] Feature list and sizing: seven features, dependencies, honest 3–4 person-week estimate below the four-week trigger, no roadmap split.
- [ ] Parity and documentation: Step 09 rows, published pages, navigation, and diagnostics anchors.

## Dependency and interface checks

- [ ] Dependencies are `Done` and their contracts are stable
- [ ] Inputs, outputs, types, and formats are defined
- [ ] Versioning and migration behavior is defined
- [ ] No unresolved cross-step interface question remains
- [ ] Step 08 is `Done`; the design binds to the implemented `CompiledModel`, `EnergyId`, `ClusterEnergyId`, `RuleGeometryId`, `CompiledJumpRule.FrequencyHz`, site stability, and position masks.
- [ ] The implemented `CompiledEnergyModel` has no site-energy background; Step 09 ownership of the `defect_background` is consistent with the data contract §3.3 and Step 11.
- [ ] `Mocassin.Contracts` references `Mocassin.Compilation`; `Mocassin.Compilation` has no Contracts/Packages/parameterization dependency.
- [ ] Step 10 receives `ParameterizedModel`; Step 11 maps `CompiledModel` + `ParameterizationValues`, writes both signatures, `frequency_factor`, and `defect_background`.
- [ ] Step 12 owns `compile`, `build`, the Build-stage adapter, and exit codes; no `mocassin parameterize` command is added.
- [ ] Step 13's Rust reader can re-encode the values-only parameterization projection from the payload (`model_signature` + value sections) without persisted rule geometry IDs.
- [ ] `contracts/manifest.json` `signatures.parameterization` finalization, generated descriptor, golden `.bin` vectors, and the Python checker are explicitly planned.
- [ ] `ProjectStore` remains the only project persistence owner; no separate template file or legacy format is introduced.
- [ ] Step 26 owns editor UI, bulk fill, and import/export; Step 09 exposes library operations only.
- [ ] `ValidationContext` has no `CompiledModel`, so the no-`IValidationStage` decision is justified and tested.

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

## Record state

**State:** Ready for review

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner or implementor.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
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

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

#### Commands and results

#### Findings

#### Verdict
