# XC-02 — Architecture and Design Status

> Reviewer: write evidence, findings, and verdict directly into this record. You may edit only your own
> metadata, checklist completion marks and inline verification notes (not item text), `Evidence`, `Findings`,
> and `Verdict`, and commit only this file with `[skip ci]`. Do not edit the scope, checklist item text,
> resolutions, code, tests, configuration, step pages, status dashboard, or any other file.

## Request

| Field | Value |
| --- | --- |
| Review ID | XC-02 |
| Checkpoint | Phase B — Model definition (Steps 03–07) |
| Type | Repository-wide architecture and design-status review |
| Status | Planned — awaiting Step 07 completion |
| Target commit | After the Step 07 merge and `step-07` tag, plus a docs-only checkpoint commit |
| Request owner | Step agent / planner |
| Orchestrator (implementor) | — |
| Review initiated by | Human owner |
| Reviewer | — |
| Review date | — |

> Orchestration: an implementor agent requests the reviewer, fixes findings on a maintenance branch, adds
> resolutions without altering reviewer text, and requests re-review until the reviewer no longer identifies
> issues. The human owner approves the final verdict and the phase gate.

## Purpose

Verify that the architecture, contracts, and the completed phase's design are coherent, complete, and ready
for the next phase, including original-design fidelity: did we make the intended corrections to the original,
did we port the original's good/clever design aspects, and did we avoid introducing a regression? For the
Phase B checkpoint, this covers the Steps 00–07 model definition and the Step 08 compilation interface.

This is a design review, not a line-by-line code audit or scientific parity validation.

## Non-goals

- Re-running the Step 00–07 implementation audits line by line.
- Statistical parity validation of the solver (Step 29).
- Documentation prose review (XC-03) or repository process audit (XC-01).
- Approving Step 08 implementation; that requires its own design review.

## Scope

- `docs/planning/00-scope-and-principles.md`
- `docs/planning/01-target-architecture.md`
- `docs/planning/02-data-contracts.md`
- `docs/planning/03-quality-strategy.md`
- `docs/planning/04-roadmap.md`
- `docs/planning/05-open-decisions.md`
- `docs/planning/06-toolchain-requirements.md`
- `docs/planning/07-reference-build.md`
- Step pages 00–07 and their review/audit/amendment records
- ADRs, parity matrix, contracts/manifest and generated schemas
- Actual code, tests, project references, and public API baselines where needed to confirm design claims
- Step 08 design page and the interfaces it consumes
- Original reference build, inventory, and parity material needed to compare intended corrections, ported
  strengths, and regressions

## Evidence

Files reviewed:

Commands run:

The reviewer should inspect the actual code/tests where needed:

```sh
git status --short
git log --oneline --decorate -10
dotnet build Mocassin.slnx -c Release
dotnet test Mocassin.slnx -c Release
./tools/verify.sh structure
./tools/verify.sh api
./tools/verify.sh contracts
```

The reviewer may use targeted tests/greps rather than the full suite when a finding is specific.

## Checklist

### A. Foundations

- [ ] Scope, non-goals, and success criteria are explicit and internally consistent.
- [ ] Target architecture and project dependency direction match the project references.
- [ ] Core/Symmetry/Compilation/Contracts/Packages/Results/App boundaries are consistent and respected.
- [ ] Data contracts define the project, package, results, state, job, event, and CLI surfaces without
  contradictions.
- [ ] Schema versions, additive-v1 decisions, manifests, generated artifacts, and fixtures are consistent.
- [ ] Canonical JSON, hashing, typed IDs, units, diagnostics, and ordering rules are coherent.
- [ ] Quality strategy, test tiers, property/brute-force/golden tests, and performance targets cover the
  model-definition steps.
- [ ] Open decisions, gated decisions, and deferred items are current and have owners.
- [ ] ADRs are consistent with the current design and not contradicted by step pages.

### B. Model definition

- [ ] Particles and particle sets: void/empty semantics, IDs, limits, defaults, name/symbol rules.
- [ ] Structure: space group, cell, sites, dummies, stability, P1 preview, overlap validation, schema.
- [ ] Transitions: state changes, groups, chains, patterns/linkers, custom grammar, reverse links, path
  matching, transition kinds, conservation boundary.
- [ ] Reverse behavior distinguishes rule-level inverse generation from automatic inverse transition models and
  explicit reverse transition links.
- [ ] Energy: environments, filters, clusters, site energies, pair/chiral derivation, cost limits, preview vs
  compiled tables.
- [ ] Lattice/doping: occupation exchanges, priority order, default building block, charge compensation,
  truncation, preview.
- [ ] Simulations: KMC/MMC settings, defaults, transition references, job count, execution flags.
- [ ] Cross-domain invariants: site/particle/transition/exchange references, validation ownership, cascade
  behavior, empty-model behavior.
- [ ] Public API, typed IDs, namespaces, XML docs, and the ApiCompat baseline are coherent.
- [ ] Derived data is not persisted; compiled-model expectations for Step 08 are defined.

### C. Original design fidelity

Answer the three questions explicitly; a missing or unrecorded answer is a finding.

- [ ] **Corrections:** every original defect, limitation, or undesirable behaviour that this phase intended to
  correct is actually corrected. Each intended correction is traceable to a requirement, ADR, or step decision,
  and the evidence shows the corrected behaviour. No intended correction is silently retained.
- [ ] **Ported strengths:** the good and clever original design aspects that this phase intended to keep are
  carried into the new design and verified. This includes algorithms, data layouts, caches, incremental
  updates, validation strategies, performance shortcuts, and workflow ideas. The record states what was kept,
  adapted, or dropped, and why.
- [ ] **Regressions:** no unintended regression over the original remains in capability, semantics,
  performance, memory, diagnostics, determinism, or maintainability. Any deliberate regression is recorded as
  an explicit trade-off with rationale, owner, and follow-up.
- [ ] Reverse transition behavior matches the intended original semantics and the deviation is recorded.
- [ ] Chirality/pair counting, cluster counting, charge compensation, MCSP semantics, and original defects are
  resolved or explicitly documented.
- [ ] Deviations from the original are intentional, justified, and testable.
- [ ] A keep/adapt/drop inventory for original design aspects exists, and deliberate regressions have an owner
  and follow-up.
- [ ] The evidence is concrete: code, tests, benchmarks, or a side-by-side design comparison, not only prose.
- [ ] No hidden scope or unowned scientific assumption remains.

### D. Cross-step interfaces

- [ ] Step 08 compiled model, indices, signatures, and caches consume the Step 03–06 definitions coherently.
- [ ] Steps 09–12 parameterization, job expansion, package writer, and CLI contracts are compatible.
- [ ] No unresolved cross-step interface question remains.
- [ ] Step 08 design can start without reopening Steps 00–07 decisions.

### E. Evidence and exit criteria

- [ ] Contract/schema/fixtures are current.
- [ ] Property/brute-force/golden tests cover the model invariants claimed by the design.
- [ ] Public API baseline and build gates pass at the frozen commit.
- [ ] No Blocker or Major finding remains, or every exception is explicitly waived by the human owner.
- [ ] Minor/Note findings have owners and follow-ups.
- [ ] The human owner approves the verdict and remediation plan.

## Findings

| ID | Severity | Area | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Re-review required
- [ ] Waived

Follow-up owner:
Follow-up step:

## Remediation status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

## Human approval

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:
