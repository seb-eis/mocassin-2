# Step 11 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-11.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 11 — Simulation Package and `.msl` Writer |
| Request owner | Planner (design freeze); the step agent takes ownership of resolutions at the start gate |
| Orchestrator | Orchestrator agent (spawns the planning auditor) |
| Step agent (implementor) | Not assigned — created from the reviewed commit after the start-gate approval |
| Review initiated by | Orchestrator, on request of the planner |
| Reviewed commit | `d8ade7a29b287ef96bda537f5cd3102d9caa1b41` |
| Docs branch | `docs/step-11-design` |
| Review date | — |
| Reviewer | — |

Step page: [`Step 11 — Simulation Package and .msl Writer`](../steps/step-11-simulation-package-and-msl-writer.md)

> Orchestration: the orchestrator spawns a fresh planning auditor after the planner has frozen the design and
> completed this request, and relays the verdict and owner decisions. The planner and step agent update the
> design docs, apply fixes on the design docs or a maintenance branch, add resolutions without altering reviewer
> text, and ask the orchestrator for re-review until the auditor no longer identifies issues. The orchestrator
> does not edit the design or the review record. The human owner approves the final verdict and the start of
> implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 08, 09, 10 |
| Delivers | The immutable `.msl` v1 package: schema, publication barrier, model/job/lattice payload DTOs and encoding, content-addressed lattices, streamed job insertion, a read-only inspector/verifier, and the checked-in contract fixtures |
| Sizing | 3.5–4.0 person-weeks, seven features; within the roadmap §3.1 four-week / roughly 6–8 feature trigger |

## Design decisions reviewed

- [ ] Step page design decisions — goal, scope, non-goals, dependency reconciliation, design sections 1–9,
      tasks, acceptance criteria, tests, risks, and "Done when"
- [ ] Data contracts and format changes — `02-data-contracts.md` §1.3, §3.2–§3.5; the pre-release deltas listed
      in Step 11 §6 and their fixture/generated-artifact consequences
- [ ] Architecture and module boundaries — `01-target-architecture.md` §3/§3.1; `Mocassin.Contracts`,
      `Mocassin.Packages`, and `Mocassin.Results` split for payload DTOs, writing, and read-only verification
- [ ] ADRs and open decisions — ADR 0004, 0009, 0010, 0011, 0013 and the new resolved rows in
      `05-open-decisions.md`
- [ ] Quality strategy and test plan — `03-quality-strategy.md` §9.9, §10, §13 and the Step 11 test table with
      tiers
- [ ] Parity matrix and documentation plan — `appendix-b-parity-matrix.md` package rows and the named
      reference/developer pages
- [ ] Licensing and provenance — no new dependency (MessagePack, ZstdSharp.Port, Microsoft.Data.Sqlite are
      already centrally versioned), no GPL-derived content, SPDX headers for new files
- [ ] Post-implementation audit plan — the implementation-status block, feature log, closeout evidence, and
      audit request sequence
- [ ] Feature list and sizing — seven features with distinct outcomes and a 3.5–4.0 pw estimate; whether the
      list and estimate match roadmap §3.1
- [ ] Package schema and version metadata — `application_id`, `user_version`, `schema_info` rows, pragmas,
      batch transactions, WAL→DELETE, fsync file and directory, atomic rename, no `.bak`
- [ ] Model payload mapping and signature — every §3.3 field's source, applied values versus compiled defaults,
      absolute `frequency_hz`, tracker definitions/mappings, `jump_count`/`jump_mapping` shape, and why
      `model_signature` cannot be re-encoded from the payload
- [ ] Lattice payload and signature finalization — the five-field projection, removal of `dopingInfo`, dedup
      semantics, and the ADR 0010 pre-release exception
- [ ] Job payload mapping — settings, `SolverJobFlagsMapper.Compose`, normalized Cartesian field direction,
      optimizers, `doping_info`, and the `{id, config_json}` extension encoding
- [ ] Read-only inspection and verification — bounded decoding, the `PKG` check list, the `PKG` descriptor home
      in `Mocassin.Contracts`, and the Step 12 CLI ownership boundary
- [ ] Contract manifest and generated artifacts — `formats.package.payloads`, removal of the undefined job
      columns, generator changes, regenerated schema/constants/descriptors, and fixture descriptors
- [ ] Cross-step interfaces with Steps 12, 13, 15, 16, 18, 19, and 20 — writer/verifier consumption, the Rust
      reader contract, the field vector, per-job frequency factors, tracker ownership, extension JSON bytes, and
      `.msr` binding

## Dependency and interface checks

- [ ] Dependencies 08, 09, and 10 are `Done` and their shipped contracts are stable — `CompiledModel`,
      `ParameterizedModel`/`ParameterizationValues`/`ParameterizationSignatureV1`, `JobDescriptor`,
      `ResolvedJobSettings`, `GeneratedLattice`, `DopingReport`, `CompiledOptimizers`, `SolverJobFlagsMapper`,
      and `SymmetryVersionInfo` exist as used by the design
- [ ] Inputs, outputs, types, and formats are defined — model/job/lattice payload fields, the denormalized job
      columns, the lattice signature projection, and the verification report
- [ ] Versioning and migration behavior is defined — package schema, payload, and compiled-model versions are
      independent domains; the pre-release finalizations and the current-only fixture policy are recorded
- [ ] No unresolved cross-step interface question remains — the Step 06 field/frequency text, Step 13's payload
      struct, Step 12's `inspect`/`verify` ownership, and the Step 12 build sample are reconciled
- [ ] The writer/verifier split matches the architecture — `Mocassin.Packages` writes and self-checks its own
      payloads, `Mocassin.Results` owns read-only `.msl` inspection/verification, and neither creates a circular
      project reference
- [ ] The `Mocassin.Packages` reference closure still allows the writer — `Compilation`, `Contracts`, and `Core`
      provide the compiled model, the applied values, the payload DTOs, the signature projections, and the cell
      vectors needed for the field conversion
- [ ] The contract fixtures can be produced without hand-authored binaries — the real writer generates the
      `.msl`, the canonical summary normalizes build metadata, and the extracted model payload is copied from
      the writer's bytes
- [ ] Step 12 can build the full pipeline from this API — model write, per-job lattice/descriptor pairing,
      completion result with the package SHA-256, and the verifier used before reporting success

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
> planner, implementor, or orchestrator.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Waived

Follow-up owner:
Follow-up step:

## Approval summary

The request owner (step agent or planner) completes this section before the orchestrator presents the approval
request. It must cover the full history, not only the latest round.

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
