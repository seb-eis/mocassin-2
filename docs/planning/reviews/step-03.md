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
| Design review status | Ready with notes — accepted |
| Reviewed commit | `1beb8da8751d3df55865e9f4a592b20ca17d33a1` (`1beb8da`) |
| Docs branch | `docs/step-03-design-fixes` |
| Review date | 2026-11-08 |
| Reviewer | Independent planning auditor (fresh context) |

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

- [x] Step page design decisions — reviewed the frozen Step 03 page; model, preview, validation, and public API decisions are mostly clear, with gaps M1, M3, M5, M6 and minor m1/m4.
- [x] Data contracts and format changes — `02-data-contracts.md` and `docs/reference/project-format.md` are mostly aligned; the `symmetry` metadata path is incomplete (M1) and `particleIds` canonicalization is still ambiguous in one paragraph (m7).
- [x] Architecture and module boundaries — Core/Symmetry/Packages ownership is sound; the composed loader wiring is missing (M2).
- [x] ADRs and open decisions — no new ADR is required; `05-open-decisions.md` records the project symmetry-metadata decision.
- [x] Quality strategy and test plan — the test matrix is extensive; the performance criterion is not benchmark-backed (M4) and several delivered rules lack a test row (m5).
- [x] Parity matrix and documentation plan — user-guide, physics, architecture, project-format, and parity updates are planned; the parity update is correctly deferred to implementation.
- [x] Licensing and provenance — no new third-party dependency; the original is used read-only; the MIT/SPDX convention applies to new files (n3).
- [x] Post-implementation audit plan — the standard frozen-branch audit applies; no special audit plan is required.

## Dependency and interface checks

- [x] Dependencies are `Done` and their contracts are stable — Step 01 and Step 02 are Done/tagged, and the Step 02 catalog/geometry/encoder contracts are available.
- [x] Inputs, outputs, types, and formats are defined — mostly; the metadata type (M1), `STR007` overlap interface (M3), and index-cap code (M5) are missing.
- [x] Versioning and migration behavior is defined — the fields are additive and `schemaVersion` stays 1; the `symmetry` metadata mapping still needs definition (M1).
- [ ] No unresolved cross-step interface question remains — **not satisfied**: M1/M2 (metadata and loader), M3 (`STR007` enrichment), and M5 (index-cap diagnostic) remain open.

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable — scope and non-goals are clear; the performance criterion (M4) and the "void index is 0" test (m3) are not testable as written.
- [x] Design decisions are complete and internally consistent — mostly; M6, m1, m4, and m7 are inconsistencies.
- [x] Required tests, property tests, golden fixtures, and performance targets are defined — the test strategy is extensive; M4 and m5 are the gaps.
- [x] Documentation, status, parity, and audit updates are planned — yes; the step page, status, docs pages, and parity task are present.
- [x] Licensing, provenance, and dependency checks are resolved — no new dependency and no GPL copy; n3 records the SPDX convention.
- [x] Risks and mitigations are realistic — mostly; the "unreachable diagnostics" mitigation misses M6 and m1.
- [x] Code layout and namespace plan follow the structure policy — yes; n2 notes a duplicate class name for clarity.
- [ ] No hidden scope, stale placeholders, or unowned decisions — **not fully satisfied**: M1/M2/M5 and m2 are unowned details.

### Review round 1

Reviewer: independent planning auditor, fresh context with no shared conversation with the planner or
implementor; did not author or implement this design. The design content is frozen at `1beb8da`; `HEAD`
`b5802bc` only adds the review-request and status metadata.

## Evidence

Files reviewed:

- `AGENTS.md` §§3.8, 3.9, 3.10, 3.12, 10–13, 16; `docs/planning/reviews/README.md`;
  `docs/planning/reviews/step-design-review-template.md`; `docs/planning/reviews/step-03.md`.
- Step pages: `step-03-particle-and-structure-models.md` (frozen `1beb8da`),
  `step-02-symmetry-and-geometry.md`, `step-04-transition-model.md`,
  `step-07-validation-and-diagnostics.md`, `step-08-model-compilation-pipeline.md`.
- Planning contracts: `01-target-architecture.md`, `02-data-contracts.md`, `03-quality-strategy.md`,
  `05-open-decisions.md`, `appendix-b-parity-matrix.md`, `status.md`.
- Public docs: `docs/reference/project-format.md`, `docs/glossary/notation.md`,
  `docs/development/code-structure.md`.
- Contracts: `contracts/manifest.json`, `contracts/generated/schema/project-v1.schema.json`.
- Existing code: `ModelDefinition`, `Project`, `ProjectJson`, `ProjectValidator`, `ProjectDiagnosticCodes`,
  `ProjectHasher`, `ProjectCanonicalizer`, `ProjectEditor`, `ProjectEditResult`, `Diagnostic`, `ModelPath`,
  `DiagnosticBag`, `Fractional3`, `TypedIds`, `UnitValues`, `UnitCellVectorEncoder`, `CellGeometry`,
  `ExpandedSite`, `SiteOrbitInfo`, `StructureDiagnosticCodes`, `SymmetryVersionInfo`, `SpaceGroupCatalog`,
  `SymmetryCache`, and `ProjectStore`; tests `ProjectFormatTests`, `ProjectPropertyTests`, and
  `ProjectSchemaTests`; `samples/` and `tests/contracts/` fixtures; `Mocassin.Benchmarks` and
  `tests/reference/bench/baselines.json`.
- Original reference (read-only, no copy): `ICon.Model` particle/structure validators, `Particle`,
  `CellSite`, `StructureInfo`, and the particle/structure model settings.

Commands run (read-only):

```text
git status --short                         -> clean
git log --oneline -5                       -> b5802bc, 1beb8da, fb49111, 17875e1, 2a54e4f
git diff --stat 41d77fd..1beb8da           -> 7 files, 462 insertions(+), 128 deletions(-)
git diff --stat 1beb8da..HEAD              -> only docs/planning/reviews/step-03.md and docs/planning/status.md
git diff --check; range checks             -> clean
zensical build --strict                    -> No issues found
python tools/contracts/generate.py --check -> Contract artifacts are current (17 files)
python catalog inspection                  -> 274 settings; maximum 192 operations
grep/read source                           -> no `symmetry` member in Project or ProjectDto; ProjectStore line 437
```

The planning pages are excluded from the published site, so `zensical build --strict` validates the published
pages (including `docs/reference/project-format.md`) but not planning links. No implementation build or test
was run because no Step 03 implementation exists. `git diff --stat 1beb8da..HEAD` confirms that the only
post-freeze changes are the review-request metadata and the status update.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M1 — `symmetry` metadata has no Core model or JSON mapping.** The design says Core parses the optional envelope metadata and `ProjectValidationService` compares it, but no Core type/property or `ProjectDto` field is specified. `Project` has no `symmetry` member; `ProjectDto` has no field, and `UnmappedMemberHandling.Disallow` means a project containing `symmetry` is rejected. `PROJ034`, the matching fixtures, and the comparison cannot be implemented as written. | Step 03 lines 262–266, 343–346, 445–446, 468, 515; `02-data-contracts.md` lines 114–120, 139; `src/dotnet/Mocassin.Core/Project/Project.cs` lines 26–84; `ProjectJson.cs` lines 91, 237–282; `ProjectDiagnosticCodes.cs` lines 13–63. |
| **Major** | **M2 — The composed validation is not wired into the loader.** `ProjectValidationService` is called the single composition point, but `ProjectStore.LoadBytesCore` still calls Core `ProjectValidator.Validate(project)`. Unless `ProjectStore` is changed to use `ProjectValidationService`, `PROJ034` and all Symmetry diagnostics are never emitted when a project is loaded, contradicting the contract and the acceptance criterion. | Step 03 lines 343–346, 404, 437; `02-data-contracts.md` lines 116–120; `src/dotnet/Mocassin.Packages/Projects/ProjectStore.cs` line 437; `01-target-architecture.md` line 142. |
| **Major** | **M3 — `STR007` enrichment with a `CellSiteId` path has no structured Step 02 interface.** The acceptance requires overlapping sites rejected with a `CellSiteId` path, and the design says the validator reuses the encoder's `STR007` result and "enriches" it. `UnitCellVectorEncoder.TryCreate` accepts only `IEnumerable<Fractional3>`, emits `STR007` with reference indices in the message, and aborts before returning orbit data; `Diagnostic.Path` is null. No API exposes the overlapping pair, so the implementer must parse message text or reimplement orbit comparison, contradicting the design. | Step 03 lines 274–276, 434–435, 458, 505; `src/dotnet/Mocassin.Symmetry/Geometry/UnitCellVectorEncoder.cs` lines 89–206 (STR007 at 142–147); `ExpandedSite.cs` lines 15–20; `Diagnostic.cs`/`ModelPath.cs`. |
| **Major** | **M4 — The performance acceptance criterion is not measurable as specified.** The design sets "ceria/BZO preview < 100 ms" on the Step 02 runner class and lists it as a PR-fast test, but adds no preview benchmark method, fixture, command, or baseline; `Mocassin.Benchmarks` only has `P1Expansion20Cubed` and `Matrix3` benchmarks, and `baselines.json` only records P1. A wall-clock PR-fast assertion is flaky and conflicts with the quality strategy's benchmark tiers. | Step 03 lines 214–216, 461, 517; `03-quality-strategy.md` lines 15, 34, 157–165, 380–405; `src/dotnet/Mocassin.Benchmarks/Program.cs` lines 29–101; `tests/reference/bench/baselines.json`. |
| **Major** | **M5 — The signed 32-bit index-cap rejection has no diagnostic code.** The preview must reject `A*B*C*P > int.MaxValue` with a structured diagnostic, and the test asserts that, but the validation table defines only `STR009` (warning above 10,000 positions) and no hard-cap code. `UnitCellVectorEncoder.GetLinearIndex` throws `ArgumentOutOfRangeException`; `IsLatticeSizeSupported` returns a bool. An uncoded error cannot satisfy the preview/reject contract or the Step 07 per-code test. | Step 03 lines 210–213, 217–219, 227–250, 510; `UnitCellVectorEncoder.cs` lines 298–322, 406–411; `StructureDiagnosticCodes.cs` lines 17–32. |
| **Major** | **M6 — `STR008` is unreachable as written.** The warning fires only when a site orbit is "> 192 positions", but the largest space-group order in the embedded catalog is 192 (Fd-3c, 192 operations); an orbit cannot exceed the group order. The code can never fire, so the risk "every code has a positive and negative test" and the Step 07 per-code acceptance cannot be met. Change the threshold/meaning or remove the code. | Step 03 line 244 and line 534; `src/dotnet/Mocassin.Symmetry/Data/space-groups.json` (274 settings; maximum 192 operations); `step-07` line 184. |
| **Minor** | **m1 — `STR011` is redundant or undefined.** A resolved particle set that contains at least one valid particle can occupy the site; empty or void sets are `PAR005`, unknown members are `PAR006`, and a missing/default set ID is `STR006`. No distinct "cannot be occupied" case is defined, so `STR011` is either unreachable or a duplicate; define the case or remove the code. | Step 03 lines 175–176, 242, 247; `step-07` line 78; `PAR005`/`PAR006` table rows. |
| **Minor** | **m2 — Reserved-ID reuse has no diagnostic code.** The design forbids authored `ParticleId.Void`/`ParticleSetId.Empty` and the test asserts they "cannot be authored", but the `PAR` table has no reserved-ID rule and `ProjectDiagnosticCodes` has no code for it (`PROJ003` is duplicate IDs). Assign a code/path or state that the editor rejects it as a programmer error. | Step 03 lines 84–85, 227–250, 496; `ProjectDiagnosticCodes.cs` lines 13–63; `PAR` table. |
| **Minor** | **m3 — "void index is 0" is a Step 08 compiled-index test, not a Step 03 test.** `Particle`/`ParticleModel` have no index; `ParticleId.Void` is a GUID. The Step 03 test matrix cannot assert a compiled index. Move the assertion to Step 08 or expose a model-level contract. | Step 03 lines 108–109, 496; `step-08` lines 99, 111; `appendix-b` line 35. |
| **Minor** | **m4 — `ParticleSet` order wording is contradictory.** Line 90 says "`ParticleSet` order inside `ModelDefinition` is not semantic"; line 119 says "`ParticleSet` order is semantic: the first particle is the default occupation". Only `ParticleSet.ParticleIds` order is semantic; say so consistently. | Step 03 lines 90, 119; `02-data-contracts.md` lines 161–163. |
| **Minor** | **m5 — The test matrix omits several delivered rules.** There is no test row for dummy positions/`STR010`, `PAR001`, `PAR004`, `STR005`, `STR006`, `STR009`, or `STR011`; the risk table claims every code has a positive and negative test. Add the rows or explicitly defer them to Step 07. | Step 03 tests lines 490–518 and risk line 534; `step-07` line 184. |
| **Minor** | **m6 — `ParticleSet.Name` and `CellDummyPosition.Name` (and duplicate set members) are unvalidated.** `PAR001` covers particle names; `STR005` covers site names; `STR013` covers the structure name. Empty/duplicate set or dummy names, and duplicate `ParticleIds` within one set, have no rule or test. | Step 03 lines 102–105, 153–156, 229–250; test matrix. |
| **Minor** | **m7 — The project-format canonicalization paragraph does not list `particleIds` as semantic.** The new Step 03 section correctly says `particleIds` order is semantic, but the earlier "Arrays are sorted by `id` ... semantic orders include ..." sentence still omits it. Add it to the list or state the list is non-exhaustive. | `docs/reference/project-format.md` lines 69–71 vs lines 130–131; `02-data-contracts.md` lines 135, 161–163. |
| **Minor** | **m8 — The preview cache key's `structureHash` and version-injection test seam are undefined.** The cache is keyed by `(structureHash, A, B, C, catalogDataVersion, symmetryContractVersion)`, but no hash computation is specified (`ProjectHasher.Compute` hashes the model, not the structure), and the preview service has no defined way to vary catalog/contract versions; `SymmetryCache` is internal and `SpaceGroupCatalog.Default` is process-wide. | Step 03 lines 214–216, 507; `SymmetryCache.cs` lines 20–42, 186–190; `ProjectHasher.cs` lines 21–26. |
| **Note** | **n1 — The structure-name default/provenance is slightly inconsistent.** The design uses `New Structure` and says the name is "preserved from the original `IStructureInfo`"; the original model default is `Unnamed` (`StructureInfo.CreateDefault`), while the original UI data uses `New Structure`. Record this as a deliberate UI-default choice or align the wording. | Step 03 lines 65, 108, 164; `src/ModelBuilder/ICon.Model/Structures/Modeling/ModelParameters/StructureInfo/StructureInfo.cs` line 26; `src/ModelBuilder/Mocassin.UI.Xml/StructureModel/StructureInfoData.cs` line 19. |
| **Note** | **n2 — Two different `StructureDiagnosticCodes` classes are planned.** Core gets `Diagnostics/StructureDiagnosticCodes.cs` and Symmetry keeps `Diagnostics/StructureDiagnosticCodes.cs`. Different assemblies/namespaces make this legal, but it invites confusion; use distinct names or document the split. | Step 03 lines 383–385, 392–394; existing `src/dotnet/Mocassin.Symmetry/Diagnostics/StructureDiagnosticCodes.cs`. |
| **Note** | **n3 — New files must carry `SPDX-License-Identifier: MIT` and no new third-party dependency is introduced.** The design does not mention licensing, but the existing policy requires the header and the work is clean-room; no action beyond the implementation convention. | `AGENTS.md` §13; Step 03 code layout; no new dependency in the design. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Re-review required
- [ ] Waived

**Changes required.** There are no Blockers, but six Major findings (M1–M6) must be corrected before the
human owner can approve implementation. The most material are the incomplete `symmetry`/`PROJ034` path (M1,
M2), the missing structured `STR007` overlap interface (M3), the unmeasurable performance criterion (M4), the
uncoded index-cap rejection (M5), and the unreachable `STR008` rule (M6).

Follow-up owner: Step agent / planning agent
Follow-up step: Update the Step 03 design and affected docs, set the review record to `Re-review required`,
and request a fresh review of the new commit.

## Approval summary

The step agent completes this section before requesting human approval. It must cover the full history, not
only the latest round.

- [x] Requested decision and frozen commit recorded — accept `Ready with notes` at design commit `3096aa8`; round-2 notes applied in `a299046`
- [x] Every review/audit round recorded (round, date, commit, reviewer, verdict)
- [x] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status)
- [x] Accepted notes and waivers recorded with rationale — round-2 notes R2-1–R2-6 are resolved in `a299046`; explicit owner acceptance is pending
- [x] Residual risks, deferred items, and follow-up owners recorded — see the note table and the step-page risks
- [x] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived — no Blocker/Major; three Minor findings and three Notes from round 2 are resolved or accepted as notes
- [x] Final CI and documentation-build results recorded — `zensical build --strict` passes; `python tools/contracts/generate.py --check` reports 17 current files; no CI run for this documentation-only branch

Round history:

| Round | Date | Commit | Reviewer | Verdict |
| --- | --- | --- | --- | --- |
| 1 | 2026-11-08 | `1beb8da` | Independent planning auditor (fresh context) | Changes required |
| 2 | 2026-11-08 | `3096aa8` | Independent planning auditor (fresh context, round 2) | Ready with notes |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| M1–M6, m1–m8, n1–n3 | Major/Minor/Note | See round 1 | See `Resolution status` | `3096aa8` | Resolved |
| R2-1–R2-6 | Minor/Note | See round 2 | See `Note fixes after round 2` | `a299046` | Resolved — awaiting owner acceptance |

## Human approval

- [ ] Accepted: Ready for implementation
- [x] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by: Human owner
Date: 2026-11-08

The human owner accepted the `Ready with notes` verdict at design commit `3096aa8` and the round-2 note
resolutions in `a299046`, and authorized the Step 03 start gate. The complete feature-branch list still
requires its own approval before the step branch is created.

## Resolution status

Fix round 1: `3096aa8` — design and contract corrections; this section is owned by the planner/step agent and
does not alter the reviewer's findings, evidence, or verdict.

| Finding | Status | Resolution |
| --- | --- | --- |
| M1 | Resolved | Added `ProjectSymmetryMetadata`, `Project.Symmetry`, `ProjectDto.Symmetry`, JSON/canonical-document support, and model/document hash separation. |
| M2 | Resolved | `ProjectStore.LoadBytesCore` now uses `ProjectValidationService` by default; the loader test is required. |
| M3 | Resolved | Added `StructureSiteReference`/`SiteOrbitOverlap` and the additive `UnitCellVectorEncoder.TryCreate` overload with `CellSiteId` paths. |
| M4 | Resolved | Added `StructurePreviewCeria`/`StructurePreviewBariumZirconate` benchmark cases, command, baseline, and PR-medium/release tier; removed the PR-fast wall-clock assertion. |
| M5 | Resolved | Added `STR014` for the signed 32-bit supercell cap. |
| M6 | Resolved | Removed `STR008`; documented that a single-site orbit cannot exceed the catalog maximum of 192. |
| m1 | Resolved | Removed `STR011`; documented it as redundant with `PAR005`, `PAR006`, and `STR006`. |
| m2 | Resolved | Added `PAR010` for reserved-ID reuse. |
| m3 | Resolved | Changed the Step 03 test to the reserved `ParticleId.Void`/`Find` contract; compiled index 0 remains Step 08. |
| m4 | Resolved | Distinguished `ParticleSet.ParticleIds` order from the non-semantic `ParticleSets` array order. |
| m5 | Resolved | Added test rows for `PAR001`, `PAR004`, `STR005`, `STR006`, `STR009`, and `STR010`. |
| m6 | Resolved | Added `PAR011` (set names), `PAR012` (duplicate members), and `STR015` (dummy names). |
| m7 | Resolved | Added particle IDs to the semantic-order list in `docs/reference/project-format.md`. |
| m8 | Resolved | Defined `ProjectHasher.ComputeStructure` and catalog/contract version injection for the preview cache key. |
| n1 | Resolved | Recorded `New Structure` as the deliberate project/UI default and noted the original `Unnamed` model default. |
| n2 | Resolved | Renamed the Core diagnostic class `CoreStructureDiagnosticCodes`. |
| n3 | Resolved | Added the SPDX/licensing implementation task. |

### Note fixes after round 2

Commit `a299046` applies the round-2 notes without changing the reviewed design commit `3096aa8`.

| Finding | Status | Resolution |
| --- | --- | --- |
| R2-1 | Resolved — awaiting owner acceptance | Moved the benchmark to `Mocassin.Benchmarks/Structure/StructurePreviewBenchmarks.cs`, inside the structure-policy folder limits. |
| R2-2 | Resolved — awaiting owner acceptance | Changed the benchmark tier to Nightly/release and added the structure-preview target to `03-quality-strategy.md` §13. |
| R2-3 | Resolved — awaiting owner acceptance | Made `ProjectHasher.ComputeStructure` explicitly public, added it to the PublicAPI/ApiCompat task, and updated `design-decisions.md` and `project-format.md` to describe three hashes. |
| R2-4 | Resolved — awaiting owner acceptance | Changed the risk wording to “every used code”. |
| R2-5 | Resolved — awaiting owner acceptance | Added `ProjectDiagnosticCodes.SymmetryMetadataMismatch` (`PROJ034`) to the Core code layout. |
| R2-6 | Resolved — awaiting owner acceptance | Renamed the record to `OrbitOverlap`; the diagnostic constant remains `StructureDiagnosticCodes.SiteOrbitOverlap`. |

## Re-review round 2

### Re-review request

| Field | Value |
| --- | --- |
| Step | 03 — Particle and Structure Models |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Design review status | Ready with notes |
| Reviewed commit | `3096aa82c37a1f5e9fd6bf9ef83576fe691cb6ff` (`3096aa8`) |
| Docs branch | `docs/step-03-design-fixes` |
| Review date | 2026-11-08 |
| Reviewer | Independent planning auditor (fresh context, round 2) |

Step page: [`Step 03 — Particle and Structure Models`](../steps/step-03-particle-and-structure-models.md)

The design content for round 2 is frozen at `3096aa8`. The delta from the round-1 commit `1beb8da` is the fix
round recorded above; the request and status changes are metadata only. Verify the delta yourself with
`git diff --stat 1beb8da..3096aa8`.

Reviewer: append your independence statement, evidence, findings, and verdict for this round below. Keep the
round-1 text unchanged.

### Re-review evidence

**Independence.** I am a fresh, independent planning auditor for round 2. I did not author or implement the
Step 03 design, the fix round, or any implementation; I have no shared conversation with the planner or
implementor. I reviewed the design content frozen at `3096aa8` and treated `a90057a` only as request,
resolution-status, and status metadata.

Files reviewed:

- `AGENTS.md` §§3.8, 3.9, 3.10, 3.12, 10–13, 16; `docs/planning/reviews/README.md`;
  `docs/planning/reviews/step-design-review-template.md`; this record.
- Step 03 at `3096aa8`, plus `step-02-symmetry-and-geometry.md`,
  `step-04-transition-model.md`, `step-07-validation-and-diagnostics.md`, and
  `step-08-model-compilation-pipeline.md`.
- Planning contracts: `01-target-architecture.md`, `02-data-contracts.md`, `03-quality-strategy.md`,
  `05-open-decisions.md`, `appendix-b-parity-matrix.md`, `status.md`.
- Public docs: `docs/reference/project-format.md`, `docs/glossary/notation.md`,
  `docs/development/design-decisions.md`, `docs/development/code-structure.md`.
- Contracts: `contracts/manifest.json`, `contracts/generated/schema/project-v1.schema.json`.
- Existing code: `Project`, `ProjectJson`/`ProjectDto`, `ProjectHasher`, `ProjectCanonicalizer`,
  `ProjectValidator`, `ProjectEditor`, `ProjectStore`, `Diagnostic`, `ModelPath`, `TypedIds`,
  `UnitCellVectorEncoder`, `StructureDiagnosticCodes`, `SymmetryVersionInfo`, `SymmetryCache`,
  `SpaceGroupCatalog`, `Mocassin.Benchmarks/Program.cs`, `tests/reference/bench/baselines.json`,
  `tools/structure-policy.json`, and `Mocassin.Architecture.Tests/StructureChecks`.
- Original reference (read-only, no copy): `StructureInfo.CreateDefault` and `StructureInfoData` for the
  `Unnamed`/`New Structure` default.

Commands run (read-only):

```text
git status --short                         -> clean
git log --oneline -5                       -> a90057a, 3096aa8, a3a330c, b5802bc, 1beb8da
git diff --stat 1beb8da..3096aa8           -> 6 files, 300 insertions(+), 97 deletions(-)
git diff --stat 3096aa8..HEAD              -> reviews/step-03.md, status.md, and one metadata line in the step page
git diff 3096aa8..HEAD -- step page        -> only the Design review link changes
git diff --check                           -> clean
zensical build --strict                    -> No issues found
python tools/contracts/generate.py --check -> Contract artifacts are current (17 files)
python catalog inspection                  -> 274 settings; maximum 192 operations
```

The post-freeze delta at `a90057a` is metadata only: the round-2 request, the resolution-status table, the
status dashboard, and the step-page design-review link. No implementation build or test was run because no
Step 03 implementation exists. I did not modify code, tests, or any file other than this record.

### Re-review checklist

Marks and notes apply to round 2 only; item text is unchanged.

**Design decisions reviewed**

- [x] Step page design decisions — reviewed the frozen page; M1–M6, m1–m8, and n1–n3 are addressed; R2-1 and R2-2 are new small inconsistencies.
- [x] Data contracts and format changes — the `symmetry` mapping, `PAR010`–`PAR012`, `STR014`/`STR015`, and semantic-order rules are aligned; R2-3 notes the hash documentation.
- [x] Architecture and module boundaries — the Core/Symmetry/Packages split is sound; `ProjectValidationService` is the composition point and `ProjectStore` calls it.
- [x] ADRs and open decisions — no new ADR is required; the project symmetry-metadata decision is recorded.
- [x] Quality strategy and test plan — the benchmark and the missing test rows are now present; R2-2 notes the tier wording.
- [x] Parity matrix and documentation plan — the parity rows are planned and the documentation plan is present except R2-3.
- [x] Licensing and provenance — n3 is resolved; no new third-party dependency is introduced.
- [x] Post-implementation audit plan — the standard frozen-branch audit applies.

**Dependency and interface checks**

- [x] Dependencies are `Done` and their contracts are stable — Steps 01 and 02 are Done/tagged and the Step 02 catalog/encoder types are available.
- [x] Inputs, outputs, types, and formats are defined — `ProjectSymmetryMetadata`, `StructureSiteReference`, `SiteOrbitOverlap`, `STR014`, and the benchmark are specified.
- [x] Versioning and migration behavior is defined — the fields are additive and `schemaVersion` remains 1.
- [x] No unresolved cross-step interface question remains — M1/M2/M3/M5 are resolved; R2-5 is a constant-placement detail, not an interface gap.

**Standard design-review checklist**

- [x] Scope, non-goals, and acceptance criteria are explicit and testable — the performance criterion is now benchmark-backed; R2-2 is a tier-wording issue.
- [x] Design decisions are complete and internally consistent — the substantive design is consistent; R2-1 and R2-3 are small inconsistencies.
- [x] Required tests, property tests, golden fixtures, and performance targets are defined — yes; R2-2 affects the declared tier, not the target.
- [x] Documentation, status, parity, and audit updates are planned — yes; R2-3 and R2-4 are small gaps.
- [x] Licensing, provenance, and dependency checks are resolved — n3 records the SPDX convention and no new dependency.
- [x] Risks and mitigations are realistic — yes; R2-4 is a wording issue.
- [ ] Code layout and namespace plan follow the structure policy — **not satisfied as written**: R2-1 places the benchmark file at the project root.
- [x] No hidden scope, stale placeholders, or unowned decisions — no hidden scope; R2-5 notes the `PROJ034` constant location.

### Re-review findings

#### Round-1 resolution verification

| Finding | Severity | Status | Verification |
| --- | --- | --- | --- |
| M1 | Major | Resolved | `ProjectSymmetryMetadata`, `Project.Symmetry`, and `ProjectDto.Symmetry` are specified at Step 03 lines 92–109 and 504–505; the model/document hash split is at line 106; the `PROJ034` comparison is at lines 107–109 and 320–324; the data contract is at `02-data-contracts.md` 124–127; the test is at Step 03 611. The existing `Project` has no `symmetry` member and `ProjectDto` has no field (`Project.cs` 12–84; `ProjectJson.cs` 237–282), so the added type and property are the required change. |
| M2 | Major | Resolved | `ProjectStore.LoadBytesCore` is explicitly changed to `ProjectValidationService.Validate` by default at Step 03 411–413 and 522–523; `ProjectStore` accepts an injected validator for tests; the test is at 610 and the acceptance criterion at 557. The existing call is `ProjectStore.cs` line 437. |
| M3 | Major | Resolved | `StructureSiteReference` and `SiteOrbitOverlap` plus the additive `TryCreate` overload are specified at Step 03 252–257, 394–400, 409–410, and 517–518; `Diagnostic.Path` plus `Related` (`Diagnostic.cs` 22–39) can carry both `CellSiteId` paths; the test is at 601 and the acceptance criterion at 545–546. |
| M4 | Major | Resolved | The benchmark cases, fixture, command, baseline, and tier are specified at Step 03 263–271, 527, 550–551, and 616; the PR-fast wall-clock assertion is removed at 582–583. `Mocassin.Benchmarks/Program.cs` 29–101 currently has only `P1Expansion20Cubed`/`Matrix3`, and `baselines.json` records only P1, so this is a real addition. See R2-2. |
| M5 | Major | Resolved | `STR014` is defined at Step 03 line 299 and in the ownership text at 318–319 and 328–329; it is emitted by the preview service before allocation (250–251); the test is at 607 and the acceptance criterion at 552. `UnitCellVectorEncoder.IsLatticeSizeSupported` exists at `UnitCellVectorEncoder.cs` 406–411. |
| M6 | Major | Resolved | `STR008` is removed from the validation table and documented as unused at Step 03 310–312; the catalog maximum is 192 operations (`space-groups.json`: 274 settings, maximum 192). |
| m1 | Minor | Resolved | `STR011` is removed and documented as unused at Step 03 310–312; the table no longer lists it. |
| m2 | Minor | Resolved | `PAR010` is added at Step 03 131–133, 285, and 308–309; `02-data-contracts.md` 206–207; `project-format.md` 129; the test is at 592. `ParticleId.Void` and `ParticleSetId.Empty` already exist in `TypedIds.cs` 43 and 67. |
| m3 | Minor | Resolved | The Step 03 test now asserts `Find(ParticleId.Void)` and the reserved-ID contract (Step 03 127–129 and 592); compiled index 0 remains Step 08 (`step-08` 99 and 111). |
| m4 | Minor | Resolved | `ParticleSets` array order is non-semantic and `ParticleSet.ParticleIds` order is semantic at Step 03 90 and 142–143; `project-format.md` 130 agrees; no contradictory wording remains. |
| m5 | Minor | Resolved | The tests now cover `PAR001` (591), `PAR004`/`STR009` (608), and `STR005`/`STR006`/`STR010`/`STR015` (602); `STR011` is removed. |
| m6 | Minor | Resolved | `PAR011`/`PAR012` are at Step 03 145–146 and 286–287; `STR015` is at 211 and 300; the tests are at 594 and 602. |
| m7 | Minor | Resolved | `particle IDs in a particle set` is in the semantic-order list at `docs/reference/project-format.md` 69–71 and 130–131; the Step 03 section agrees at 142–143. |
| m8 | Minor | Resolved | `ProjectHasher.ComputeStructure` and the cache key including `catalogDataSha256` are at Step 03 240–247; `StructurePreviewService` accepts a `SymmetryVersionInfo`, whose three version values are defined at `SymmetryVersionInfo.cs` 13–31. |
| n1 | Note | Resolved | `New Structure` is recorded as the deliberate project/UI default with the original `Unnamed` model default at Step 03 189–191; the original sources confirm `Unnamed` (`StructureInfo.cs` 26) and `New Structure` (`StructureInfoData.cs` 19). |
| n2 | Note | Resolved | The Core class is `CoreStructureDiagnosticCodes` at Step 03 330–331 and 458; Symmetry keeps `StructureDiagnosticCodes` at 469. |
| n3 | Note | Resolved | The SPDX/no-new-dependency task is at Step 03 535 and follows `AGENTS.md` §13. |

No round-1 finding remains open. The fixes are present in the frozen design and are consistent with the
existing type signatures; the new findings below are separate issues introduced or left by the fix round.

#### New findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **R2-1 — The new benchmark file is placed at the project root, contradicting the design's own code-layout rule and the structure policy.** Step 03 says "no new root-level files" (lines 430–431), but the layout puts `Mocassin.Benchmarks/StructurePreviewBenchmarks.cs` at the project root (lines 484–485). `tools/structure-policy.json` only permits `Program.cs` and the named support files at a source-project root, and `Mocassin.Architecture.Tests/StructureChecks/StructureChecker.cs` 70–82 rejects any other root file; `Mocassin.Benchmarks` is not a test project. | Step 03 430–431, 484–485; `tools/structure-policy.json` `sourceRootFilePatterns`; `src/dotnet/Mocassin.Architecture.Tests/StructureChecks/StructureChecker.cs` 70–82; `src/dotnet/Mocassin.Benchmarks/Program.cs` is the existing allowed root file. |
| **Minor** | **R2-2 — The benchmark tier conflicts with the quality strategy, and the new performance target is not added to the performance-target table.** Step 03 calls the preview benchmark "PR-medium/release" (lines 270–271, 550–551, 582–583, and 616), while `03-quality-strategy.md` places benchmarks in Nightly/release (line 34) and lists only model variants/fixtures/manifest drift for PR-medium (lines 157–159); the < 100 ms target is not in §13 (lines 380–405). | Step 03 270–271, 550–551, 582–583, 616; `03-quality-strategy.md` 34, 157–159, 380–405. |
| **Minor** | **R2-3 — `ComputeStructure` makes the hash documentation incomplete.** Step 03 adds `ProjectHasher.ComputeStructure` (lines 240, 460, and 514), but `docs/development/design-decisions.md` 52/57 still describes "two hashes" and `docs/reference/project-format.md` 208–212 describes only `Compute` and `ComputeDocument`; the documentation plan (Step 03 569–578) does not name those pages. State whether the method is public (then update the docs and `PublicAPI.Unshipped.txt`) or internal. | Step 03 240, 460, 514, 569–578; `docs/development/design-decisions.md` 52–61; `docs/reference/project-format.md` 208–212. |
| **Note** | **R2-4 — The risk-table mitigation still says every code has a positive and negative test while three codes are documented as unused.** | Step 03 line 633; lines 303–312. |
| **Note** | **R2-5 — The location of the `PROJ034` code constant is not specified.** The design says Packages emits it (Step 03 109, 301, and 322), but the code layout adds only `ProjectValidationService.cs`; `ProjectDiagnosticCodes` currently ends at `PROJ033` (`ProjectDiagnosticCodes.cs` 13–63). | Step 03 109, 301, 322, 480–482; `ProjectDiagnosticCodes.cs` 13–63. |
| **Note** | **R2-6 — The new `SiteOrbitOverlap` record shares its name with the existing `StructureDiagnosticCodes.SiteOrbitOverlap` constant.** Different namespaces make this legal, but it is the same clarity problem n2 addressed; rename the record or document the split. | Step 03 397–400; `src/dotnet/Mocassin.Symmetry/Diagnostics/StructureDiagnosticCodes.cs` 27. |

### Re-review verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Re-review required
- [ ] Waived

**Ready with notes.** No Blocker or Major finding remains. All six round-1 Majors (M1–M6) and every
minor/note (m1–m8, n1–n3) are resolved in the frozen design. The fixes are implementable against the
existing types: `Project`/`ProjectDto` can gain the optional metadata, `ProjectStore.LoadBytesCore` can call
the composed validator, the encoder can gain the structured overload, `STR014`/`PAR010`–`PAR012`/`STR015`
are defined, and the benchmark/test plan is present. Three Minor findings and three Notes remain: R2-1
(benchmark file at the project root), R2-2 (benchmark tier wording), R2-3 (stale "two hashes" docs), and
Notes R2-4–R2-6. The human owner must explicitly accept these before implementation; R2-1 should be corrected
in the design, or handled by the implementer before `tools/verify.sh structure` runs.

Follow-up owner: Step agent / planning agent
Follow-up step: Correct R2-1, R2-2, and R2-3 or record explicit acceptance; then request human approval for
`Ready with notes`.
