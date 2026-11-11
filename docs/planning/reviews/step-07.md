# Step 07 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-07.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record
> state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 07 — Validation, Diagnostics, and Model Statistics |
| Request owner | Step agent |
| Orchestrator (step agent / planner) | Step agent / planner |
| Review initiated by | Planner |
| Reviewed commit | `dc73ee40954070b8e95bf0713f8df4526b208ba6` |
| Docs branch | `docs/step-07-design` |
| Review date | — |
| Reviewer | — |

Step page: [`Step 07 — Validation, Diagnostics, and Model Statistics`](../steps/step-07-validation-and-diagnostics.md)

> Orchestration: the step agent or planner (the orchestrator) freezes the design, requests the reviewer, applies
> fixes autonomously on the design docs or a maintenance branch, adds resolutions without altering reviewer
> text, and requests re-review until the reviewer no longer identifies issues. The human owner approves the
> final verdict and the start of implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | B. Model definition |
| Depends on | 03, 04, 05, 06 |
| Delivers | A load/model/cross-domain/feasibility validation pipeline, stable diagnostic catalog, model statistics, and the headless `mocassin validate` command. |

## Design decisions reviewed

- [ ] Step page design decisions
- [ ] Data contracts and format changes, including the `validation-v1` report in `02-data-contracts.md` §5.6
- [ ] Architecture and module boundaries, including the `Mocassin.Packages.Projects.Validation` placement
- [ ] ADRs and open decisions, including the resolved validation/statistics entries
- [ ] Quality strategy and test plan
- [ ] Parity matrix and documentation plan
- [ ] Licensing and provenance
- [ ] Post-implementation audit plan

## Dependency and interface checks

- [ ] Dependencies 03–06 are `Done` and their diagnostic/preview contracts are stable.
- [ ] The existing `ProjectValidationService` remains the model/domain composition; Step 07 does not re-emit
      domain diagnostics.
- [ ] `ProjectStore` load diagnostics and `ProjectValidationPipeline` stage `Load`/`Model` ownership are
      unambiguous, including `ValidateOnLoad`.
- [ ] Inputs, outputs, types, formats, and schema versions are defined, including `validation-v1`.
- [ ] Stage `Build` is an extension point owned by Steps 09/10/12, not implemented here.
- [ ] No unresolved cross-step interface question remains.

## Standard design-review checklist

- [ ] Scope, non-goals, and acceptance criteria are explicit and testable
- [ ] Design decisions are complete and internally consistent
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined
- [ ] Documentation, status, parity, and audit updates are planned
- [ ] Licensing, provenance, and dependency checks are resolved
- [ ] Risks and mitigations are realistic
- [ ] Code layout and namespace plan follow the structure policy
- [ ] No hidden scope, stale placeholders, or unowned decisions

## Step-specific checks

- [ ] The pipeline composes existing validators instead of duplicating `ProjectValidator`,
      `ProjectValidationService`, or Symmetry validation.
- [ ] `ValidationOptions`, `ValidationResult`, `DiagnosticSummary`, and stage results are sufficiently specified
      for the CLI and GUI to share them without reimplementing rules.
- [ ] `ProjectStoreOptions.ValidateOnLoad` avoids double validation and preserves existing store semantics.
- [ ] Stage order, skipping, cancellation, and error behavior are defined.
- [ ] `ENM018` and `SIM038`–`SIM042` have clear ownership, severity, promotion policy, and no cascade or
      duplicate with `TRN020`, `SIM003`, or earlier diagnostics.
- [ ] The diagnostic catalog covers Core/Symmetry/package codes, reserves `JOB` for Step 10 without stubs, and
      has a credible completeness/docs-drift test.
- [ ] `ModelStatistics` fields, sources, nullability, cell-count behavior, and rule-count advisory semantics are
      coherent with Steps 02/05/08/10/16.
- [ ] The `validation-v1` report is a stable external contract; its fields, ordering, deduplication, `valid`
      policy, and exit codes are testable.
- [ ] CLI loading, human output, JSON output, exit codes, and `--stats` behavior are specified.
- [ ] GUI navigation and statistics consumption are a contract only; no GUI is implemented in Step 07.
- [ ] Performance targets are realistic and distinguish validation from opt-in statistics.
- [ ] Validation never mutates the project and is deterministic.
- [ ] The feature list and split trigger fit one audit sitting.

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
