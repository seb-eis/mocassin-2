# Step 10 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-10.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 10 — Job Templates and Supercell Generation |
| Request owner | Step 10 orchestrator |
| Orchestrator (step agent / planner) | General orchestrator |
| Review initiated by | Human owner |
| Reviewed commit | `c66b483f6ef064858d0e8082543d2fcec8386ed3` |
| Docs branch | `docs/step-10-design` |
| Review date | 2026-11-14 |
| Reviewer | — |

Step page: [`Step 10 — Job Templates and Supercell Generation`](../steps/step-10-job-templates-and-supercells.md)

> Orchestration: the step agent or planner (the orchestrator) freezes the design, requests the reviewer, applies
> fixes autonomously on the design docs or a maintenance branch, adds resolutions without altering reviewer
> text, and requests re-review until the reviewer no longer identifies issues. The human owner approves the
> final verdict and the start of implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 06, 08, 09 |
| Delivers | Job template collections/configurations, deterministic job expansion, doping-aware supercell generation with charge compensation, and the shared lattice/job-count preview used by the GUI and CLI |

## Design decisions reviewed

- [ ] Step page design decisions
- [ ] Data contracts and format changes
- [ ] Architecture and module boundaries
- [ ] ADRs and open decisions
- [ ] Quality strategy and test plan
- [ ] Parity matrix and documentation plan
- [ ] Licensing and provenance
- [ ] Post-implementation audit plan
- [ ] Feature list and sizing (eight features; 3–4 person-weeks)
- [ ] Job template model, persistence, canonical ordering, and `schemaVersion = 1` additive change
- [ ] Inheritable `JobSettings` fields, kind-specific overrides, job count, and extension handling
- [ ] Job expansion, deterministic IDs, master/project/job seed derivation, and domain separation
- [ ] Supercell generation, default occupations, deterministic sampling, size limits, and row-major layout
- [ ] Doping priority, truncation, charge-compensation ratios, `DopingReport`, and `DopingInfo`
- [ ] Preview service ownership in `Mocassin.Packages.Jobs`, cache keys, request echo, failure boundary, and
      preview/build equality
- [ ] Selection optimizers, P1 position mapping, `UseDualDofCorrection`, and start-option validation
- [ ] `ValidationContext.CompiledModel` extension, `JobTemplateValidationStage`, and `JOB001`–`JOB015`
      ownership
- [ ] `mocassin build --preview` boundary versus Step 12 package writing
- [ ] Cross-step interfaces with Steps 11, 12, 13, and 25

## Dependency and interface checks

- [ ] Dependencies are `Done` and their contracts are stable
- [ ] Inputs, outputs, types, and formats are defined
- [ ] Versioning and migration behavior is defined
- [ ] No unresolved cross-step interface question remains
- [ ] Step 06 lattice/doping definitions are sufficient for Step 10 and are not duplicated
- [ ] Step 08 `CompiledModel`, `CompiledRuntimeSettings`, `CompiledSolverIntent`, and particle/site tables are
      sufficient for expansion, generation, and optimizer application
- [ ] Step 09 `ParameterizedModel` and the parameterization-independent lattice hash are coherent
- [ ] Step 11 package writing consumes `JobDescriptor` and `GeneratedLattice` with the documented mapping
- [ ] Step 12 reuses the preview/expansion services and does not reimplement them
- [ ] Step 13's `.msl` reader can consume the documented lattice layout and optimizer encoding
- [ ] Step 25 consumes the preview service with a project and compiled model and does not reimplement doping

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
