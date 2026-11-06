# Step 02 Design Review

> Planning-auditor handoff: complete this review in
> `.tmp/review/step-02-r1-dd1d103-design-review.md` instead of editing this tracked record. The step agent or
> scribe transcribes the findings and verdict verbatim into this file.

## Request

| Field | Value |
| --- | --- |
| Step | 02 — Symmetry and Geometry |
| Request owner | Step agent |
| Design review status | Ready for review |
| Reviewed commit | `dd1d103a4e74da71cfb50d4165b39f5eddc20754` (`dd1d103`) |
| Docs branch | `docs/step-02-design-review` |
| Review date | 2026-11-06 |
| Reviewer | — |

Step page: [`Step 02 — Symmetry and Geometry`](../steps/step-02-symmetry-and-geometry.md)

## Step summary

| Field | Value |
| --- | --- |
| Phase | A. Foundations |
| Depends on | 01 — Domain Kernel and Project Format |
| Delivers | A validated space-group catalog, exact symmetry operations, coordinate systems, P1 expansion, orbit/permutation reduction, and periodic geometry kernels |

## Design decisions reviewed

- [ ] Core/Symmetry boundary and ownership of symmetry types
- [ ] Catalog provenance, licensing, and the 274-setting compatibility subset
- [ ] Catalog generation, packaging, versioning, and checksum contract
- [ ] Structured space-group reference and project-format mapping
- [ ] Exact fractional grid and rational deduplication
- [ ] Cell geometry validation and the corrected cell-vector formula
- [ ] Canonical P1 order, `Vector4I` encoding, and linear index
- [ ] Orbit, stabilizer, pair, and cluster reduction semantics
- [ ] Periodic geometry, minimum image, and tolerance policy
- [ ] Caching, determinism, symmetry contract versioning, performance, and test oracles

### Decision summary

| Decision | Recorded in |
| --- | --- |
| Core/Symmetry boundary | Step 02 §4, Step 03 §2/§5, `01-target-architecture.md` |
| Catalog provenance and 274 settings | Step 02 §1, `NOTICE.md`, `AGENTS.md` §13 |
| Catalog generation and checksum | Step 02 §1, `05-open-decisions.md` |
| Structured space-group reference | Step 02 §1, Step 03 §2, `02-data-contracts.md` |
| Exact fractional grid | Step 02 §4, `02-data-contracts.md`, `glossary/notation.md` |
| Cell geometry | Step 02 §3, Step 03 §4, `02-data-contracts.md` |
| P1 order and linear index | Step 02 §4, `02-data-contracts.md` |
| Reduction semantics | Step 02 §5, Step 05, Step 08 |
| Periodic geometry | Step 02 §6, Step 08 |
| Contract, caches, and performance | Step 02 §7, Step 08, `02-data-contracts.md` |

## Dependency and interface checks

- [ ] Step 01 is `Done` and its project-format contracts are stable
- [ ] Inputs, outputs, types, and formats are defined for Step 03 and downstream steps
- [ ] Versioning and migration behavior is defined for the catalog and symmetry contract
- [ ] No unresolved cross-step interface question remains

## Standard design-review checklist

- [ ] Scope, non-goals, and acceptance criteria are explicit and testable
- [ ] Design decisions are complete and internally consistent
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined
- [ ] Documentation, status, parity, and audit updates are planned
- [ ] Licensing, provenance, and dependency checks are resolved
- [ ] Risks and mitigations are realistic
- [ ] No hidden scope, stale placeholders, or unowned decisions

## Evidence

Files reviewed:

- `AGENTS.md`
- `NOTICE.md`
- `docs/planning/01-target-architecture.md`
- `docs/planning/02-data-contracts.md`
- `docs/planning/03-quality-strategy.md`
- `docs/planning/05-open-decisions.md`
- `docs/planning/steps/step-02-symmetry-and-geometry.md`
- `docs/planning/steps/step-03-particle-and-structure-models.md`
- `docs/planning/steps/step-05-energy-model.md`
- `docs/planning/steps/step-08-model-compilation-pipeline.md`
- `docs/planning/audits/step-02.md`
- `docs/planning/reviews/README.md`
- `docs/glossary/notation.md`

Commands run:

```text

```

## Known notes for the auditor

- Step 02 is the first step to use the pre-implementation design review. Step 00 and Step 01 are recorded as
  `Waived — process introduced after completion`.
- No implementation code exists yet. This review is of the planning and contract documents only.
- The reviewed docs are frozen at `dd1d103a4e74da71cfb50d4165b39f5eddc20754`. This review record itself is
  metadata added after the freeze and does not change the design.
- `docs/reference/project-format.md` intentionally does not yet contain the structure/cell section; Step 03
  adds it when the feature lands.
- The original MOCASSIN SQLite database is a local, read-only oracle only. It is not a source, is not copied,
  and is not a CI dependency.
- Focus on cross-step consistency, especially Step 02 ↔ Step 03 ↔ Step 05 ↔ Step 08, the data contract, the
  `symmetry-1` contract version, and the compiled-payload metadata.

## Findings

| Severity | Finding | Evidence | Resolution |
| --- | --- | --- | --- |
| — | — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor fills this section in the
handoff report; the step agent or scribe copies it verbatim into this record and adds resolutions.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Re-review required
- [ ] Waived

Follow-up owner:
Follow-up step:
