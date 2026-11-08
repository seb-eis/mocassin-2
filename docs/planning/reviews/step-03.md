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
| Design review status | Changes required |
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
