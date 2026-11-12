# Step 08 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-08.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record
> state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 08 — Model Compilation Pipeline |
| Request owner | Design planner (Step 08) |
| Orchestrator (step agent / planner) | Design planner |
| Review initiated by | Planner |
| Reviewed commit | `36fbba58182b2e769a6d59b3228a60021344248d` |
| Docs branch | `docs/step-08-design` |
| Review date | 2026-11-12 |
| Reviewer | — (fresh out-of-context planning auditor) |

Step page: [`Step 08 — Model Compilation Pipeline`](../steps/step-08-model-compilation-pipeline.md)

> Orchestration: the step agent or planner (the orchestrator) freezes the design, requests the reviewer, applies
> fixes autonomously on the design docs or a maintenance branch, adds resolutions without altering reviewer
> text, and requests re-review until the reviewer no longer identifies issues. The human owner approves the
> final verdict and the start of implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 02, 03, 04, 05, 06, 07 |
| Delivers | A pure, synchronous, staged compiler from a validated `Project` to a deterministic `CompiledModel` with P1 structure, pair/cluster tables, transition rules, simulation encoding, dense indices, signatures, caches, and `CMP` diagnostics. Step 08 is a library plus a canonical JSON inspection projection; Step 12 owns the CLI. |

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

## Step 08-specific checks

- [ ] `IModelCompiler`, `CompilationResult`, `CompilationOptions`, cancellation, parallelism, and the
      synchronous/async boundary are defined; exception policy separates user data from programmer errors.
- [ ] The compiler's validation boundary is explicit: it assumes a validated `Project`, does not consume
      `ValidationResult` or duplicate `ProjectValidationPipeline`, and the `IValidationStage`/`Build` owner is
      identified.
- [ ] Stages are reconciled with the actual Step 02/04/05 APIs: P1 expansion and encoders, radial search and
      periodic images, filters, `G0` reduction, chirality, cluster permutation canonicalization, transition
      path resolution, state codes, mass/charge conservation, and forward/inverse/dependent rules.
- [ ] `CompiledModel` is reconciled with `02-data-contracts.md` §3.3 and the Step 11 `.msl` writer; persisted,
      JSON-inspection-only, and composed-later data are separated.
- [ ] Dense indexing, stable geometry/rule IDs, model signature, the Step 09 parameterization-signature
      boundary, and byte-identical determinism are defined.
- [ ] The derived solver intent (`UseDualDofCorrection`, `UsePrerun`, kind) is defined and Step 11 owns the
      `.msl` flag word.
- [ ] Tracker mappings emitted by Step 08 and mutable tracker state owned by Step 18 are separated.
- [ ] New compile diagnostic codes, their owner prefix, their `DiagnosticCatalog` composition, and their
      reference-docs path are defined.
- [ ] The `mocassin compile` ownership question is resolved explicitly (Step 08 minimal command vs library/test
      harness; Step 12 full CLI).
- [ ] Cache keys, invalidation graph, lifetime, thread safety, and tests are defined.
- [ ] Diagnostics/report fields, stage counts/timings, and realistic performance targets are defined.
- [ ] The feature list has roughly 4–8 auditable features with the required branch names, all `Not started`;
      the sizing estimate and split trigger are recorded.
- [ ] No production compiler code, step branch, or pre-seeded review verdict exists.

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
