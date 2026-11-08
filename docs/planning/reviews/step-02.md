# Step 02 Design Review

> Durable review record. The planning auditor's findings and verdicts are transcribed verbatim from
> `.tmp/review/step-02-r1-dd1d103-design-review.md` and
> `.tmp/review/step-02-r2-b698496-design-review.md`; both handoffs were deleted after this record was
> committed.

## Request

| Field | Value |
| --- | --- |
| Step | 02 — Symmetry and Geometry |
| Request owner | Step agent |
| Design review status | Ready with notes |
| Reviewed commit | `b698496d4098064a232016e4d6784167cf1374d9` (`b698496`) |
| Docs branch | `docs/step-02-design-review` |
| Review date | 2026-11-07 |
| Reviewer | Independent planning auditor |
| Report | `.tmp/review/step-02-r2-b698496-design-review.md` |

Step page: [`Step 02 — Symmetry and Geometry`](../steps/step-02-symmetry-and-geometry.md)

The original design was frozen at `dd1d103`. Round 2 is frozen at `b698496`, which includes the review fixes.
The later commit `848c9a7` adds only `docs/planning/reviews/step-02.md` (the review request metadata) and does
not change the design; that was verified with `git show --stat 848c9a7`.

### Review rounds

| Round | Status | Reviewed commit | Date | Report |
| --- | --- | --- | --- | --- |
| 1 | Changes required | `dd1d103` | 2026-11-06 | `.tmp/review/step-02-r1-dd1d103-design-review.md` |
| 2 | Ready with notes | `b698496` | 2026-11-07 | `.tmp/review/step-02-r2-b698496-design-review.md` |

## Step 02 amendments

| Amendment | Review record | Status |
| --- | --- | --- |
| `step-02-amend-1` — public generic math | [`step-02-amend-1.md`](step-02-amend-1.md) | Ready for implementation |
| `step-02-amend-2` — KaTeX documentation math | [`step-02-amend-2.md`](step-02-amend-2.md) | Ready for implementation |

The parent audit covers these amendments at the combined frozen commit.

### Implementation integration

| Amendment | Branch | Integrated commit | Status |
| --- | --- | --- | --- |
| `step-02-amend-1` — public generic math | `feature/step-02-symmetry-and-geometry/amend-1-public-math` | `cd796b0` | Integrated |
| `step-02-amend-2` — KaTeX documentation math | `feature/step-02-symmetry-and-geometry/amend-2-docs-math` | `d6eac6f` | Integrated |

Combined solution commit: `d6eac6f`. The combined solution is ready for the parent audit.

## Step summary

| Field | Value |
| --- | --- |
| Phase | A. Foundations |
| Depends on | 01 — Domain Kernel and Project Format |
| Delivers | Validated space-group catalog, exact symmetry operations, coordinate systems, P1 expansion, orbit/permutation reduction, and periodic geometry kernels |

## Design decisions reviewed

- [x] Core/Symmetry boundary and ownership of symmetry types — mostly consistent; wording and one Step 03 task need correction (m1, m11, n4).
- [x] Catalog provenance, licensing, and the 274-setting compatibility subset — licensing is resolved; the monoclinic selection rule is factually wrong (M1); exact spglib pin is still a placeholder (n1).
- [x] Catalog generation, packaging, versioning, and checksum contract — packaging/checksum are well defined; operation ordering is not specified (n2); version/migration policy is missing (M4).
- [x] Structured space-group reference and project-format mapping — decision is sound, but the persisted enum wire names do not match the C# enums (M5) and `uniqueAxisA` is exposed while unsupported (M7).
- [x] Exact fractional grid and rational deduplication — sound; conversion midpoint and non-finite/underflow behavior need definition (m5).
- [x] Cell geometry validation and the corrected cell-vector formula — sound; tolerance-name duplication (m2) and a conditioning note (n5).
- [x] Canonical P1 order, `Vector4I` encoding, and linear index — sound; the supercell-size owner for the index-cap check is ambiguous (m11).
- [x] Orbit, stabilizer, pair, and cluster reduction semantics — mathematically sound; pair/interaction canonical ordering conflicts across documents (M6); cluster “ring” is undefined (m4); cache-key text is inconsistent (m3).
- [x] Periodic geometry, minimum image, and tolerance policy — sound; duplicate tolerance naming (m2).
- [x] Caching, determinism, symmetry contract versioning, performance, and test oracles — contract versioning is sound; benchmark fixture/runner is undefined (M3); sample-based golden tests depend on later steps (M2); status is stale (m7).

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

- [x] Step 01 is `Done` and its project-format contracts are stable — Step 01 merge/tag and main CI are recorded; the structure/cell section is intentionally added in Step 03.
- [x] Inputs, outputs, types, and formats are defined for Step 03 and downstream steps — mostly yes; the Step 02 ↔ Step 03 enum/JSON mapping (M5), unique-axis-A scope (M7), and Step 03 JSON details (m9) need correction.
- [x] Versioning and migration behavior is defined for the catalog and symmetry contract — version values and signature inclusion are defined, but project-level mismatch/migration behavior is not (M4).
- [ ] No unresolved cross-step interface question remains — **not fully satisfied**: sample availability (M2), pair ordering (M6), and the Step 03 JSON mapping (m9) remain open.

## Evidence

Files reviewed:

- `AGENTS.md` (especially §3.8, §3.9, §10, §11, §13, §14)
- `docs/development/agent-workflow.md` (§13)
- `docs/planning/reviews/README.md`
- `docs/planning/reviews/step-design-review-template.md`
- `docs/planning/reviews/step-02.md` (review request and decision summary)
- `docs/planning/steps/step-02-symmetry-and-geometry.md` (frozen design)
- `docs/planning/steps/step-03-particle-and-structure-models.md`
- `docs/planning/steps/step-05-energy-model.md`
- `docs/planning/steps/step-08-model-compilation-pipeline.md`
- `docs/planning/01-target-architecture.md`
- `docs/planning/02-data-contracts.md`
- `docs/planning/03-quality-strategy.md`
- `docs/planning/05-open-decisions.md`
- `docs/planning/appendix-b-parity-matrix.md`
- `docs/planning/status.md`
- `docs/planning/audits/step-02.md`
- `docs/glossary/notation.md`
- `NOTICE.md`
- `docs/reference/project-format.md` (checked for the intentionally absent structure/cell section)
- `.tmp/review/step-02-r1-dd1d103-request.md` (auditor request)

Commands run (read-only):

```text
git status --porcelain=v1
git log --oneline -8
git diff --stat main...HEAD
git diff main...HEAD -- docs/planning/01-target-architecture.md \
  docs/planning/02-data-contracts.md docs/planning/03-quality-strategy.md \
  docs/planning/05-open-decisions.md docs/planning/steps/step-02-symmetry-and-geometry.md \
  docs/planning/steps/step-03-particle-and-structure-models.md \
  docs/planning/steps/step-05-energy-model.md \
  docs/planning/steps/step-08-model-compilation-pipeline.md
git show --stat 848c9a7
grep -rn "catalog version|catalogVersion|space_group_data_version|..." docs/planning docs/glossary
grep -rn "GeometricEqualityTolerance|VectorEqualityTolerance|DistanceEqualityTolerance" docs
grep -rn "\"stable\"|SiteStability|stable:" docs/planning docs/reference docs/development
wc -l <reviewed files>
```

I also read the upstream spglib `src/spg_database.c` (read-only reference, not added as a dependency) to verify
the `choice` strings used by the 274-setting selection. No build or test commands were run: the repository is
planning-only and no Step 02 implementation exists.

## Findings

| Severity | Finding | Evidence | Resolution |
| --- | --- | --- | --- |
| **Major** | **M1 — The monoclinic part of the 274-setting selection rule is wrong.** The table says `choice = "b1"` and `"c1"` for all 13 monoclinic groups. spglib uses `b`/`c` (and `a`) for primitive monoclinic groups 3, 4, 6, 10, and 11; only the cells with cell choices (5, 7, 8, 9, 12, 13, 14, 15) use `b1`/`c1`. A literal implementation of the table fails for five groups and the generator assertion `274 settings` will not pass. | Step 02 §1 “274-setting selection”, lines 98–101; spglib `src/spg_database.c` entries for groups 3, 4, 6, 10, 11 (choices `b`, `c`, `a`) vs groups 5, 7–9, 12–15 (choices `b1`, `c1`, …). | Select the standard unique-axis-B and unique-axis-C entry per group (`b` or `b1`; `c` or `c1`), or select by Hall number. Add a generator assertion for 26 monoclinic settings and for each group’s two selected settings. The total 274 can still hold. |
| **Major** | **M2 — Sample-based golden and acceptance criteria depend on samples delivered by later steps.** Step 02 depends only on Step 01, but it requires golden P indices for ceria, barium zirconate, and the vehicle/interstitialcy sample; those structures/mechanisms are added in Steps 03–05. At Step 02 the sample projects do not exist, so “P1 expansion of every sample site” and “P1 golden indices … freeze the expected P order” are not executable as written. | Step 02 lines 529–533, 609, 616; Step 03 line 235 (“Add sample definitions for ceria and barium zirconate”); Step 02 “Depends on: 01”; Step 05 energy models and Step 04 transitions. | Define minimal Step 02-local test structures for P-order/pair/cluster golden tests, or explicitly move the full sample golden tests to Step 03/08 and keep Step 02 to synthetic fixtures. Update the acceptance criteria accordingly. |
| **Major** | **M3 — The 20×20×20 performance fixture and runner class are undefined, so the performance acceptance criterion is not testable.** The design says “Define the 20×20×20 benchmark concretely: cell, P count, site count, runner class, and baseline”, but that is an open task, not a recorded fixture. The acceptance criterion and quality-strategy target both refer to “the defined CI runner class”/“defined fixture”. | Step 02 §7.5 lines 552–560; Step 02 acceptance line 613; `03-quality-strategy.md` line 390. | Record the concrete fixture (cell, Pcount, site count), CI runner class, baseline, and benchmark command in the frozen design. Make the acceptance criterion refer to that exact fixture and runner. |
| **Major** | **M4 — No project-level policy exists for reopening a project under a changed catalog or symmetry contract.** The project stores only `(number, axis, origin)` and explicitly does not persist the catalog data version or `SymmetryContractVersion`; only the compiled payload records them. If the embedded catalog or the canonical-order contract changes after an application update, an existing project’s P1 positions and preview can change silently, while parameterizations are only invalidated later at build/signature time. This conflicts with the “P1 position order is a contract” and “no silent scientific corrections” rules. | Step 02 lines 140–142 (“project file stores only the setting identity; it does not persist the catalog version”), 506–519 (“The project file does not store the contract version”); `02-data-contracts.md` line 559 (compiled signature only); `01-target-architecture.md` §7.6 (no catalog/contract version row). | Persist `spaceGroupDataVersion` and `symmetryContractVersion` in the project (or a sidecar) and define a mismatch warning/migration policy, or explicitly guarantee that catalog/contract updates never change the operations or P order for existing settings. Add a test that opens a project under a different catalog/contract version and asserts the chosen behavior. |
| **Major** | **M5 — Persisted enum wire names do not match the C# enum member names.** The Step 02 sketch uses `AxisSetting.UniqueA/B/C` and `OriginChoice.One/Two` (comments), while the project contract requires `uniqueAxisA/B/C` and `originChoice1/2`. The glossary says enums use camelCase strings, so a default `JsonStringEnumConverter` would emit `uniqueA`, `uniqueB`, `uniqueC`, `one`, and `two`, not the contract values. No custom converter or explicit `JsonStringEnumMemberName` mapping is specified. | Step 02 lines 56–57 and 108–109; `02-data-contracts.md` lines 198–201; `docs/glossary/notation.md` line 133; Step 03 line 278 expects `(number, axis, origin)` round-trip. | Rename the enum members to produce the contract strings (`UniqueAxisA`, `UniqueAxisB`, `UniqueAxisC`, `OriginChoice1`, `OriginChoice2`) or specify an explicit JSON converter/mapping. Add a project round-trip test for every enum value. |
| **Major** | **M6 — Pair-table / pair-interaction canonical ordering conflicts across documents.** Step 02 sorts pair candidates by `(distanceBucket, targetP, cellA, cellB, cellC)` and assigns shell representatives in that order. Step 08 says `PairInteractionIndex` is “stable geometry ID hash order, then distance, then site indices”. The data contract says “Pair tables are in canonical shell order”. Hash order and discovery/shell order are not the same ordering. | Step 02 line 355; Step 08 §3 line 116 and §4 lines 136–140; `02-data-contracts.md` line 591. | Define one canonical pair-table/interaction order and use it in Step 02, Step 08, and the data contract. If the stable geometry ID is intended to define the order, say so and sort shells by that ID; otherwise use the shell discovery order everywhere. Add a golden test that asserts the pair table order. |
| **Major** | **M7 — `uniqueAxisA` is exposed in the v1 project format and tested in Step 03, but the 274 catalog does not contain unique axis A.** Step 02 explicitly says `AxisSetting.UniqueA` remains in the enum for future extension but is not populated by the 274 compatibility catalog, and that adding unique axis A requires a project-format decision. The project contract nevertheless lists `uniqueAxisA` as a valid `axis` value, and Step 03’s tests require “unique-axis A/B/C” crystal-system cases. A project with `uniqueAxisA` is valid JSON but cannot resolve to a catalog setting (`STR001`), and the Step 03 test cannot pass through the composed validator. | Step 02 lines 105–109; `02-data-contracts.md` line 200; Step 03 line 268 (test “unique-axis A/B/C”), line 174 (unknown enum string is `PROJ002`, so `uniqueAxisA` is not malformed). | Either remove `uniqueAxisA` from the v1 project format and the Step 03 test, or add unique-axis-A settings to the catalog (which changes the 274 count, the selection rule, and the catalog/contract version). Keep the enum value only if the format deliberately accepts an unsupported setting and documents the `STR001` behavior. |
| **Minor** | **m1 — `CrystalSystem` is described as “persisted” Core vocabulary, but it is catalog-derived and is not a field of `SpaceGroupRef` or `CellParameters`.** The architecture table also calls it “persisted symmetry vocabulary”. This is inconsistent with the data contract, which says the crystal system is derived from the catalog and never persisted. | Step 02 lines 62–64 and 221–224; `01-target-architecture.md` line 141; `02-data-contracts.md` line 202. | Describe `CrystalSystem` as a Core vocabulary type that is catalog-derived and not serialized; reserve “persisted vocabulary” for `AxisSetting` and `OriginChoice`. |
| **Minor** | **m2 — Two names are used for the same 1e-6 Å tolerance.** Step 02 §3 defines `GeometricEqualityTolerance = 1e-6 Å` for Cartesian distance/vector equality; §6 defines `VectorEqualityTolerance = 1e-6 Å` for Cartesian vector/position equality. Either these are duplicates or one scope is undefined. | Step 02 lines 202 and 458. | Keep one constant with one documented scope, or define both scopes explicitly and test the distinction. |
| **Minor** | **m3 — The prose cache keys in §5 differ from the authoritative cache table in §7.1.** §5 says pair shells are cached by `(settingId, centerP, cutoff, cellParametersHash)` and clusters by `(settingId, centerP, clusterOffsetsHash, allowedOccupationHash)`. §7.1 adds `catalogDataVersion`, uses `settingKey`, and uses `cutoffBucket` not raw `cutoff`. | Step 02 lines 363, 376 vs 489–492. | Align §5 with the §7.1 table; use the integer `cutoffBucket` and the catalog version in every key. |
| **Minor** | **m4 — The cluster “ring” validation has no operational definition.** Step 05 rejects a cluster that “contains a ring” and lists `ENM011` (“ring/degenerate geometry”), but a cluster is defined as a center plus a set of offsets with no edge/graph model. The acceptance criterion “Duplicate/ring/out-of-range clusters are rejected” cannot be tested without a definition of ring. | Step 05 lines 93–94 and 163; Step 05 acceptance line 197; Step 05 test table line 216. | Define the cluster connectivity/ring criterion (or the intended lattice-path condition) and a matching brute-force test, or remove `ENM011` and the ring acceptance item. This is a Step 05 follow-up, not a Step 02 blocker. |
| **Minor** | **m5 — The exact-grid conversion rule leaves midpoint rounding and non-finite/underflow behavior unspecified.** The design says values are “rounded with a documented midpoint rule” but never names the rule. It also does not say what happens for `NaN`/`Inf`, or for `double` values below `decimal`’s ~1e-28 range, which cannot be parsed to `decimal` and multiplied by `D`. The grid tests cover ordinary values but not these cases. | Step 02 lines 286–293 and 671–674; `Fractional3` is a public `double` type; Step 03 `STR004` only checks `[0,1)` after trim. | Name the midpoint rule (e.g., `MidpointRounding.AwayFromZero`), reject non-finite coordinates with a structured diagnostic, and define underflow-to-zero (or another explicit policy). Add tests for `NaN`, `+Inf`, `-Inf`, and very small subnormal values. |
| **Minor** | **m6 — The version-bump rule for grid/canonical-order changes is ambiguous.** §4 says changing the grid denominator or conversion rule requires a “catalog/canonical-order version bump”; §7.3 says any covered rule change bumps `SymmetryContractVersion`. A grid change does not necessarily change the catalog settings payload, so “catalog version” is the wrong bump. | Step 02 lines 298–299 vs 506–519. | State explicitly that a grid or canonical-P1-order change bumps `symmetry-N` (and updates golden P-index fixtures/signatures); bump `catalog-N` only when the catalog payload, selection rule, or encoding changes. |
| **Minor** | **m7 — The status dashboard and step page still report `Design review: Not requested` while the review record is `Ready for review`.** The review record was committed after the frozen design, but the status fields were not updated. | `docs/planning/status.md` line 47; Step 02 line 16; `docs/planning/reviews/step-02.md` line 13. | Set the step page and dashboard design-review status to `Ready for review`/`In review` while the review is open, then `Changes required` (or the final verdict) after transcription. |
| **Minor** | **m8 — CI platform documentation conflicts.** The quality-strategy PR pipeline lists `ubuntu-latest`, `windows-latest`, and `macos-latest`; `AGENTS.md` and Step 02 say step branches run Linux and Windows, and macOS runs only on `main`, manual dispatch, or nightly. | `03-quality-strategy.md` line 357; `AGENTS.md` §3.2; Step 02 line 504. | Align the quality-strategy matrix description with the actual CI policy (feature/step branches Linux+Windows; macOS on main/nightly/manual), or update the policy if macOS on PRs is intended. |
| **Minor** | **m9 — Several Step 03 project-JSON details remain inconsistent with the Step 03 domain model.** (a) `particleIds` order is semantic (“first is default”) but is not in the data contract’s list of semantic arrays, so canonicalization would sort it by ID. (b) The structure example uses `"stable": true` while `CellSite` uses a `SiteStability` enum with no stated JSON mapping. (c) `CellDummyPosition` uses a raw `Guid` while the glossary says every user-created object has a typed ID. (d) `StructurePreview` rejects `A*B*C*P > int.MaxValue` but has no `A/B/C` supercell fields. | `02-data-contracts.md` lines 14–20 and 164–170; Step 03 lines 95, 98, 107, 141; glossary line 10. | Resolve in Step 03 before implementing the structure section: add `particleIds` to the semantic-array list; specify the `stable` JSON representation or change it to an enum; use a typed dummy-position ID; and give the encoder/compiler the supercell dimensions used for the index-cap check. These are Step 03 follow-ups, not Step 02 blockers. |
| **Note** | **n1 — The exact spglib release and commit are still placeholders.** NOTICE says the pin is recorded in the generated catalog provenance; `requirements.txt` says `spglib==<version>`; the data-contract example uses `spglib-2.8.0`. The 274-setting counts and the checksum depend on the exact release. | Step 02 line 127; `NOTICE.md` lines 30–32; `02-data-contracts.md` line 551. | Pin the exact spglib release and commit in the frozen design (or in `Data/README.md` before implementation), record it in `NOTICE.md`, and make the generator’s `--check` assert the version. |
| **Note** | **n2 — Operation order within a catalog setting is not specified, but it affects the catalog checksum and therefore the model signature.** Settings are sorted by `(number, axis, origin)`, but the operation array order is not defined. A generator reordering operations would change the checksum/model signature without changing the group. | Step 02 lines 130–134 and 112–114. | Define a canonical operation sort (e.g., linear matrix row-major, then translation numerator) so the checksum is stable across spglib iteration order. |
| **Note** | **n3 — `SpaceGroupSetting.DataVersion` duplicates the catalog root `dataVersion`.** Each setting carries the same version string, while the root metadata already has `dataVersion`. | Step 02 lines 58–59 and 129. | Keep the version at the catalog level, or document why it is repeated per setting. |
| **Note** | **n4 — Some public API types are omitted from the public/internal boundary list.** `PointGroup` exposes `SitePermutation`; Step 03 exposes `ExpandedSite` and `SiteOrbitInfo`. They are not in the Step 02 public list, but they must be public to compile and need XML docs/ApiCompat coverage. | Step 02 lines 330–337 and 561–567; Step 03 lines 127–140. | Add the exposed types to the public API list and cover them with XML documentation and `ApiCompat` baselines. |
| **Note** | **n5 — `CellDeterminantTolerance = 1e-12` is a determinant-only near-degeneracy check and may not bound conditioning.** A cell with `det G` above 1e-12 can still have an ill-conditioned `M⁻¹` for very skewed triclinic cells. The design also warns about skewed minimum-image searches. | Step 02 lines 175–182, 201, 462; §6 minimum-image tests. | Consider a relative determinant or condition-number bound for very skewed cells, or document why the 1e-12 cutoff is sufficient and add a stress test with a high-condition-number triclinic cell. |
| **Note** | **n6 — `FractionalTrimTolerance = 1e-10` is much larger than one grid step (`1/(24×10^12) ≈ 4.2e-14`).** Boundary snapping can move a point by many grid steps and can merge near-boundary inputs before overlap detection. This is probably intended, but the interaction with canonical P order should be explicit. | Step 02 lines 203, 254–256, 459. | Document that boundary snapping is a user-input convenience and add a near-boundary P-order/overlap test. |
| **Note** | **n7 — Half-open box queries and boundary-image ownership should be tested explicitly.** `GetPositionsInBox` says boundary points belong to exactly one image and periodic duplicates are returned with offsets. The exact tie behavior for points on `min`/`max` is not specified. | Step 02 lines 468–470. | Add an exact-boundary test for `GetPositionsInBox` and state whether a point equal to `max` is excluded and which image owns it after periodic wrapping. |

## Checklist assessment

| Check | Assessment |
| --- | --- |
| Scope, non-goals, and acceptance criteria are explicit and testable | Mostly explicit. The sample-based criteria (M2) and the 20×20×20 benchmark (M3) are not testable as written. |
| Design decisions are complete and internally consistent | Mostly strong. M1, M5, M6, M7 are direct inconsistencies; m1–m3 are smaller inconsistencies. |
| Required tests, property tests, golden fixtures, and performance targets are defined | Extensive test plan. Golden fixture inputs (M2) and the benchmark fixture/runner (M3) are missing. |
| Documentation, status, parity, and audit updates are planned | Documentation and parity are planned; the status dashboard is stale (m7). |
| Licensing, provenance, and dependency checks are resolved | Licensing boundary is clear (spglib BSD-3-Clause; original GPL DB not copied). Exact spglib pin is still a placeholder (n1). |
| Risks and mitigations are realistic | Yes; the risk table is comprehensive. The catalog-version migration risk (M4) is missing. |
| No hidden scope, stale placeholders, or unowned decisions | The sample dependency (M2) is hidden scope; the benchmark definition (M3) and spglib pin (n1) are unowned placeholders. |

## Fix round 1

Date: 2026-11-07

| Commit | Scope |
| --- | --- |
| `558ab2f` | Step 02 page, status, quality strategy, glossary |
| `4cb7e86` | Data contract, Step 03, Step 08, open decisions |
| `ddf49c6` | Unique-axis wording cleanup |
| `b698496` | Integration head for re-review |

Summary: corrected the monoclinic selection rule; moved full sample golden models to Step 03/08 and added
Step 02-local fixtures; defined the concrete `P1Expansion20Cubed` benchmark; added project symmetry metadata
and `PROJ034`; aligned enum wire names and removed the per-setting `DataVersion`; reserved and rejected
`uniqueAxisA`; standardized the canonical pair order; fixed grid input, version-bump, trim, cache-key,
box-boundary, condition-limit, and public-type details.

## Resolution status

| Finding | Status | Resolution |
| --- | --- | --- |
| M1 | Fixed — awaiting re-review | Corrected monoclinic selection in `558ab2f`; asserts 26 settings and two per group. |
| M2 | Fixed — awaiting re-review | Step 02-local synthetic fixtures; full sample goldens moved to Step 03/08. |
| M3 | Fixed — awaiting re-review | Concrete `P1Expansion20Cubed` fixture, runner, baseline, and command. |
| M4 | Fixed — awaiting re-review | Optional project `symmetry` metadata and `PROJ034`; tests added in Step 03. |
| M5 | Fixed — awaiting re-review | Enum names aligned to wire strings; per-setting `DataVersion` removed. |
| M6 | Fixed — awaiting re-review | Canonical pair order `(distanceBucket, targetP, cellA, cellB, cellC)` in Step 02/08/data contract. |
| M7 | Fixed — awaiting re-review | `uniqueAxisA` reserved and rejected in v1; B/C tests aligned. |
| m1, m2, m3, m5, m6, m7, m8 | Fixed — awaiting re-review | Addressed in `558ab2f` / `4cb7e86`; m7 in `ddf49c6`. |
| m4 | Deferred to Step 05 design review | Cluster ring definition is a Step 05 concern. |
| m9 | Deferred to Step 03 design review | Step 03 JSON details remain Step 03 work. |
| n1–n7 | Addressed or documented | spglib 2.8.0 pin, operation sort, `DataVersion` removal, public types, condition limit, trim note, box rule. |

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Re-review required
- [ ] Waived

**Changes required.** There are no blockers, but seven major findings (M1–M7) must be corrected before the
human owner can approve implementation. The most urgent are the factual monoclinic selection error (M1), the
unresolved sample/benchmark acceptance inputs (M2, M3), the missing project-level catalog/contract mismatch
policy (M4), the persisted enum wire-name mismatch (M5), and the pair-order contradiction (M6).

Follow-up owner: Step agent / planning agent

Follow-up step: Update the Step 02 design (and the affected data-contract/Step 03/Step 05/Step 08 text), set the
review record to `Re-review required`, and request a fresh re-review of the new commit.

## Re-review round 2

### Re-review request

| Field | Value |
| --- | --- |
| Step | 02 — Symmetry and Geometry |
| Request owner | Step agent |
| Design review status | Ready with notes |
| Reviewed commit | `b698496d4098064a232016e4d6784167cf1374d9` (`b698496`) |
| Docs branch | `docs/step-02-design-review` |
| Review date | 2026-11-07 |
| Reviewer | Independent planning auditor |
| Report | `.tmp/review/step-02-r2-b698496-design-review.md` |

Step page: [`Step 02 — Symmetry and Geometry`](../steps/step-02-symmetry-and-geometry.md)

The design content is frozen at `b698496`. `HEAD` is `ad8d36e`; the only later changes are
`docs/planning/reviews/step-02.md` (the fix-round record), `docs/planning/status.md`, and the Step 02
design-review status line, all metadata. Verified with `git diff --stat b698496 HEAD`.

### Step summary

| Field | Value |
| --- | --- |
| Phase | A. Foundations |
| Depends on | 01 — Domain Kernel and Project Format |
| Delivers | Validated space-group catalog, exact symmetry operations, coordinate systems, P1 expansion, orbit/permutation reduction, and periodic geometry kernels |

### Round-1 finding verification

| Finding | Status | Verification |
| --- | --- | --- |
| **M1** — monoclinic selection rule | **Fixed** | Step 02 now selects `b` or `b1` for unique axis B and `c` or `c1` for unique axis C; it explicitly lists primitive groups 3, 4, 6, 10, and 11 as using `b`/`c`, and cell-choice groups 5, 7–9, and 12–15 as using `b1`/`c1`. The generator asserts 26 monoclinic settings and two per group. This matches the spglib `spg_database.c` evidence from round 1. |
| **M2** — sample-based goldens | **Fixed** | Step 02 now uses Step 02-local synthetic fixtures (P1 triclinic, P2 unique-axis B/C, Pmmm general/special, Fm-3m special, R-3m hexagonal general) for P-order/pair/cluster goldens; full ceria/BZO/vehicle goldens are explicitly moved to Step 03/08. Acceptance and test tables now say “synthetic fixture”. Step 03 adds the ceria/BZO sample P-order test; Step 08 owns the compiled sample goldens. |
| **M3** — 20×20×20 benchmark | **Fixed** | Step 02 §7.5 now records: 64 deterministic P1 positions in a cubic `a=5.41 Å` cell, `20×20×20 = 512,000` positions, `ubuntu-latest` 4 vCPU/.NET 10 Release, BenchmarkDotNet `P1Expansion20Cubed`, mean < 1 s, baseline class `linux-x64-github-ubuntu-latest-dotnet10`, and the exact command. The quality-strategy target and the acceptance/test rows are aligned. |
| **M4** — project catalog/contract mismatch | **Fixed** | The project envelope now supports optional `symmetry` metadata (`spaceGroupDataVersion`, `spaceGroupDataSha256`, `symmetryContractVersion`); a mismatch with the embedded values emits `PROJ034` and does not rewrite the project; the compiled payload remains authoritative. Step 03, quality strategy, glossary, and open decisions define the matching/missing/mismatch behavior. See `r5` for the remaining schema-versioning note. |
| **M5** — enum wire names | **Fixed** | `AxisSetting` now uses `None, UniqueAxisA, UniqueAxisB, UniqueAxisC, Hexagonal, Rhombohedral`; `OriginChoice` uses `None, OriginChoice1, OriginChoice2`, so the default camelCase serializer emits the contract strings. The per-setting `DataVersion` was removed; the catalog root and compiled payload carry it. Data contract, glossary, and Step 02 agree. |
| **M6** — pair order | **Fixed** | Step 02, Step 08, and the data contract now use the same canonical shell order `(distanceBucket, targetP, cellA, cellB, cellC)`, then site indices. Stable geometry IDs are explicitly content hashes for parameterization matching, not table ordering. Step 08 adds a pair-order test. See `r6` for a small completeness note in Step 02. |
| **M7** — `uniqueAxisA` scope | **Fixed** | `uniqueAxisA` is reserved in the enum and rejected in v1 with `STR001`; the data contract’s `axis` values exclude it; Step 03 tests were changed from unique-axis A/B/C to B/C with an explicit A rejection. See `r2` for a remaining parser/test wording ambiguity. |

**All seven majors are substantively fixed. No blocker or major finding remains.**

### Design decisions reviewed (round 2)

- [x] Core/Symmetry boundary and ownership of symmetry types — fixed in Step 02; one stale architecture wording remains (`r1`).
- [x] Catalog provenance, licensing, and the 274-setting compatibility subset — fixed; the release is pinned to spglib 2.8.0 and the exact commit is recorded at generation (`r7` is a note).
- [x] Catalog generation, packaging, versioning, and checksum contract — fixed; operation ordering and `dataVersion` placement are now explicit.
- [x] Structured space-group reference and project-format mapping — fixed; `uniqueAxisA` is reserved/rejected; one wording ambiguity remains (`r2`).
- [x] Exact fractional grid and rational deduplication — fixed; midpoint rounding, non-finite rejection, underflow, trim, and boundary tests are defined.
- [x] Cell geometry validation and the corrected cell-vector formula — fixed; `CellConditionLimit` was added, but it is not yet mirrored in the data contract/Step 03 `STR003` text (`r3`).
- [x] Canonical P1 order, `Vector4I` encoding, and linear index — fixed; the synthetic goldens and benchmark fixture are now concrete.
- [x] Orbit, stabilizer, pair, and cluster reduction semantics — fixed; pair order is canonical; the Step 05 cluster-ring issue remains deferred to Step 05 (as agreed in round 1).
- [x] Periodic geometry, minimum image, and tolerance policy — fixed; one 1e-6 Å tolerance name, half-open box rule, and tests.
- [x] Caching, determinism, symmetry contract versioning, performance, and test oracles — fixed; cache keys, `symmetry-N`/`catalog-N` bump rules, benchmark, and status are aligned.
- [x] Documentation, status, parity, and audit updates — status now says `Re-review required`; the step-page `Last updated` date is stale (`r4`).

### Dependency and interface checks (round 2)

- [x] Step 01 is `Done` and its project-format contracts are stable — still true; the optional project `symmetry` metadata is an additive envelope property (`r5`).
- [x] Inputs, outputs, types, and formats are defined for Step 03 and downstream steps — yes; the previous enum/format contradiction is resolved. The Step 03 `uniqueAxisA` test wording (`r2`) and the missing condition-limit text (`r3`) remain minor.
- [x] Versioning and migration behavior is defined for the catalog and symmetry contract — yes: optional project metadata, `PROJ034`, compiled payload authoritative; the project-schema versioning note is `r5`.
- [x] No unresolved cross-step interface question remains — the majors are resolved; the residual items are editorial/schema-fixture notes, not blocking interface questions.

### Re-review evidence

Files reviewed:

- `AGENTS.md` (§3.8, §3.9)
- `docs/planning/reviews/README.md`
- `docs/planning/reviews/step-design-review-template.md`
- `docs/planning/reviews/step-02.md` (round 1 findings, fix round, resolution status, re-review request)
- `docs/planning/steps/step-02-symmetry-and-geometry.md` (fixed design)
- `docs/planning/steps/step-03-particle-and-structure-models.md`
- `docs/planning/steps/step-05-energy-model.md`
- `docs/planning/steps/step-08-model-compilation-pipeline.md`
- `docs/planning/01-target-architecture.md`
- `docs/planning/02-data-contracts.md`
- `docs/planning/03-quality-strategy.md`
- `docs/planning/05-open-decisions.md`
- `docs/planning/status.md`
- `docs/glossary/notation.md`
- `NOTICE.md`
- `docs/planning/audits/step-02.md`
- `.tmp/review/step-02-r2-b698496-request.md`

Commands run (read-only):

```text
git status --porcelain=v1
git log --oneline -8
git log --oneline dd1d103..b698496
git diff --stat dd1d103 b698496
git diff --stat b698496 HEAD
git diff dd1d103 b698496 -- docs/planning/steps/step-02-symmetry-and-geometry.md
git diff dd1d103 b698496 -- docs/planning/02-data-contracts.md \
  docs/planning/steps/step-03-particle-and-structure-models.md \
  docs/planning/steps/step-08-model-compilation-pipeline.md
git diff dd1d103 b698496 -- docs/planning/03-quality-strategy.md \
  docs/glossary/notation.md docs/planning/05-open-decisions.md
git diff b698496 HEAD -- docs/planning/steps/step-02-symmetry-and-geometry.md docs/planning/status.md
grep -n "CellConditionLimit|condition number" <docs>
grep -n "CrystalSystem|persisted symmetry vocabulary" <docs>
grep -n "uniqueAxisA|unique axis A|every enum value|STR001|PROJ002" <docs>
grep -n "Pair tables are in canonical|PairInteractionIndex|pair-shell order" <docs>
```

The round-1 spglib `src/spg_database.c` verification was reused for M1; the fixed rule now matches the
`b`/`c` vs `b1`/`c1` split. No build or test commands were run: the repository is planning-only and no Step 02
implementation exists.

### Re-review findings

| Severity | Finding | Evidence | Resolution |
| --- | --- | --- | --- |
| **Minor** | **r1 — The architecture table still calls `CrystalSystem` “persisted symmetry vocabulary”.** Step 02 and the data contract now correctly describe it as catalog-derived and never serialized, but `01-target-architecture.md` was not updated in the fix round. | `01-target-architecture.md` lines 121–122 and 141; Step 02 lines 84–86 and 255; `02-data-contracts.md` line 213. | Change the architecture wording to “persisted vocabulary (`AxisSetting`, `OriginChoice`); `CrystalSystem` is catalog-derived vocabulary”. Documentation-only. |
| **Minor** | **r2 — The `uniqueAxisA` parser/validator path and one Step 03 test wording remain ambiguous.** The data contract excludes `uniqueAxisA` from the allowed `axis` values but says it is rejected with `STR001`; Step 03 says an *unknown* enum string is `PROJ002`; the Step 03 test says `(number, axis, origin)` “round-trips for every enum value” and also says `uniqueAxisA` is rejected. It is not clear whether the parser accepts the reserved enum member and the validator emits `STR001`, or the schema/parser rejects it as `PROJ002`. | `02-data-contracts.md` lines 210–211; Step 03 lines 178, 272, 282; Step 02 lines 80 and 136; glossary line 142. | State one path explicitly. Recommended: the JSON parser accepts the reserved `uniqueAxisA` enum value and the validator emits `STR001`; the manifest/schema marks it reserved rather than silently listing it as supported. Change the test wording to “every supported enum value” and keep a separate `uniqueAxisA` rejection test. |
| **Minor** | **r3 — `CellConditionLimit` is missing from the data-contract cell constraints and the Step 03 `STR003` description.** The fix defines `CellConditionLimit = 1e12` and a test in Step 02 and the glossary, but the canonical project-format contract and the Step 03 diagnostic table do not mention the condition-number rejection. A cell that violates the condition limit would be rejected with `STR003`, but the contract text only lists angles, Gram determinant, and crystal-system constraints. | Step 02 line 236; glossary line 58; `02-data-contracts.md` lines 201–204; Step 03 `STR003` line 159. | Add the condition-number limit to the data-contract cell constraints and to the `STR003` row (and a Step 03 test/acceptance item). Documentation-only. |
| **Minor** | **r4 — The Step 02 `Last updated` date is stale.** The status fields were updated to `Re-review required`, but the date still says 2026-11-06 while the fix and re-review are dated 2026-11-07. | Step 02 line 17; review record fix-round date 2026-11-07. | Update `Last updated` to 2026-11-07 (or the final status-change date). |
| **Note** | **r5 — The optional project `symmetry` metadata is a new envelope property on the already-completed project schema v1.** The data contract makes it optional and advisory, included in the document hash but not the model hash; the compiled payload is authoritative. Because Step 01’s v1 reader rejects unknown properties, the manifest/generated schema and fixture coverage should make clear whether this is an additive v1 change or requires a schema-version bump/reader tolerance for older builds. | `02-data-contracts.md` lines 97–116 and 132; Step 01 project schema `schemaVersion: 1`; Step 03 `PROJ034` row. | Confirm the manifest/generated project schema includes `symmetry`, add a project fixture with and without it, and state whether `schemaVersion` remains 1 or is bumped. No design change needed if this is an intentional additive v1 field. |
| **Note** | **r6 — Step 02 states the shell key but not the pair-table outer site-pair order.** Step 08 and the data contract say “canonical shell order …, then site indices”; Step 02 only says the shell order is the canonical pair-table order. | Step 02 line 405; Step 08 line 116; `02-data-contracts.md` line 602. | Add “then center/partner site indices” to Step 02 or explicitly reference the Step 08/data-contract ordering. Documentation-only. |
| **Note** | **r7 — The exact spglib commit is recorded at generation, not in the frozen design.** The release is now pinned to `spglib==2.8.0`, and the step says the exact commit is recorded in `Data/README.md` and asserted by the generator; `NOTICE.md` repeats that policy. | Step 02 lines 95–97 and 151; `NOTICE.md` lines 30–32. | Ensure `Data/README.md` records the exact commit and the generator `--check` asserts the 2.8.0 pin. No design change required. |
| **Note** | **r8 — The diagnostic catalogue reference does not mention `PROJ034`.** Step 03 adds `PROJ034`, and the data-contract/glossary describe the warning, but the glossary’s diagnostic summary still says Step 01 defines `PROJ001`–`PROJ033` and the catalogue is finalized in Step 07. | Step 03 lines 168–171; glossary lines 138–139. | Mention `PROJ034` (project loader) in the diagnostic catalogue summary and the Step 07 finalization list. Documentation-only. |

### Resolution status (round 2)

| Finding | Status | Owner |
| --- | --- | --- |
| r1 | Fixed — `d4d2b42` | Architecture wording separates persisted vocabulary from catalog-derived `CrystalSystem`. |
| r2 | Fixed — `d4d2b42` | Parser accepts reserved `uniqueAxisA`; validator rejects with `STR001`; supported-enum test wording fixed. |
| r3 | Fixed — `d4d2b42` | `CellConditionLimit` added to the data contract and Step 03 `STR003`. |
| r4 | Resolved — 2026-11-07 | Step 02 `Last updated` updated. |
| r5 | Accepted — additive v1 | Optional `symmetry` metadata is an additive v1 field; fixtures added to Step 03. |
| r6 | Fixed — `d4d2b42` | Step 02 pair order now includes center/partner site indices. |
| r7 | Confirmed | Exact spglib commit recorded at generation; generator asserts the 2.8.0 pin. |
| r8 | Fixed — `d4d2b42` | Glossary diagnostic summary includes `PROJ034`. |

### Re-review verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Re-review required
- [ ] Waived

**Ready with notes.** No blockers or major findings remain; all seven round-1 majors are fixed. The remaining
items are four minor documentation/test-wording issues (`r1`–`r4`) and four notes (`r5`–`r8`). The human owner
should explicitly accept these notes; the step may then proceed after the usual step-start approvals. The
minor items should be applied in the Step 02/03 documentation or the closeout feature; they do not require a
new design round.

Follow-up owner: Step agent / planning agent (documentation-only corrections) and human owner (explicit
acceptance of the notes).

Follow-up step: Apply `r1`–`r4` and `r5`–`r8` in the Step 02/03 docs, record the human owner’s acceptance, and
start Step 02 under the normal branch and feature-approval rules.

## Human owner acceptance

Date: 2026-11-07

The human owner accepted the **Ready with notes** verdict and the remaining r5–r8 notes. Minor fixes r1–r3,
r5, r6, and r8 were applied in `d4d2b42`; r4 was resolved earlier; r7 is confirmed by the `Data/README.md`
and generator task.

The design review gate is complete. Step 02 may proceed under the normal branch and feature-approval rules.
