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
| Target commit | `81fba457d6d4714b4fe56d9c7084a738815b365e` (Phase B checkpoint: drift remediation and record; parent `38a22d2`) |
| Request owner | Step agent / planner |
| Orchestrator (implementor) | OpenCode Phase B orchestrating agent |
| Review initiated by | Human owner |
| Reviewer | Fresh independent reviewer (OpenCode subagent; no Phase B implementation or review context) |
| Review date | 2026-11-12 |

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

### Frozen state and worktree

- Reviewed repository state at `81fba457d6d4714b4fe56d9c7084a738815b365e` (parent `38a22d2`).
  The assigned worktree `/tmp/opencode/mocassin-xc-02` was at `2fe7b9180b0768147e9f6d81f29aa7f5bcef0aa1`;
  `git diff --stat 81fba45..HEAD` shows only the five docs/status files in the request commit
  (`xc-01`, `xc-02`, `xc-03`, the cross-cutting README, and `status.md`), so the code, contracts, and
  design pages reviewed are exactly the frozen checkpoint. `git status --short` was clean before this
  record edit.
- `git log --oneline --decorate -15` shows `38a22d2` on `main`, tag `step-07` at `3ed558a`, and the
  docs-only checkpoint chain `81fba45` → `2fe7b91`; no code commit follows the Step 07 merge.
- `git worktree list` shows the canonical checkout at `38a22d2` and the review/phase-B worktrees; no
  unexpected active code branch was changed.

### Files and areas read

- `AGENTS.md` §§1–16; `docs/planning/00-scope-and-principles.md`, `01-target-architecture.md`,
  `02-data-contracts.md`, `03-quality-strategy.md`, `04-roadmap.md`, `05-open-decisions.md`,
  `06-toolchain-requirements.md`, `07-reference-build.md`, `status.md`, `appendix-a-original-inventory.md`,
  `appendix-b-parity-matrix.md`, and the Phase B drift record.
- Step pages 00–07, Step 08, and the Step 03–07 design-review/audit/amendment records; ADRs 0001–0014;
  `docs/development/architecture.md`, `compilation-pipeline.md`, `design-decisions.md`,
  `testing-and-parity.md`; `contracts/manifest.json` and generated schemas/descriptors/constants;
  `tests/contracts/` fixtures and the checked-in API baseline.
- Project graph (`Mocassin.slnx`, every `*.csproj`), the Phase B public source under `Mocassin.Core`,
  `Mocassin.Symmetry`, `Mocassin.Compilation`, `Mocassin.Contracts`, and `Mocassin.Packages`, the
  architecture test, and the Phase B test suites.
- Original reference (read-only, no copying): `src/McSolver/Libraries/Simulator/**` for the static S2,
  jump-link CSR, selection/normalization, and environment-energy behaviour; `src/ModelBuilder/ICon.Model/**`
  for transition, energy, lattice, and simulation semantics; `SpaceGroupService` for the original chiral
  pair predicate.

### Required commands and results

| Command | Result |
| --- | --- |
| `git status --short` (worktree, before record edit) | empty |
| `git log --oneline --decorate -15` | `main`/`origin/main` at `38a22d2`; `step-07` tag at `3ed558a`; checkpoint `81fba45`; request `2fe7b91`; no unexpected code commit |
| `dotnet build Mocassin.slnx -c Release` | **Build succeeded**, 0 warnings, 0 errors (all 21 projects) |
| `dotnet test Mocassin.slnx -c Release --no-build` | **898 passed**, 0 failed: Core 564, Symmetry 217, Packages 105, Cli 7, App 1, Compilation 1, Contracts 1, Results 1, Architecture 1 |
| `./tools/verify.sh structure` | passed (`Mocassin.Architecture.Tests`, 1/1) |
| `./tools/verify.sh api` | NuGet licence/vulnerability policy OK (47 packages); ApiCompat OK for Core, Symmetry, Compilation, Contracts, Packages, Results, App.ViewModels, App.Views, Rendering |
| `./tools/verify.sh contracts` | `Contract artifacts are current (18 files)`; space-group catalog current (274 settings, sha256 `1f3bf67…e3395`); `Process consistency: OK`; tool tests 18/18; SPDX 321 files; secret scan 661 files; `cargo-deny` advisories/bans/licenses/sources OK |
| `python3 tools/contracts/generate.py --check` | `Contract artifacts are current (18 files)` |
| Targeted probe (citable evidence for F-01) | Read `samples/ceria/ceria.mocprj`, called `EnergyPreviewService.TryCreatePairPreview`, and compared each preview shell with `PairReduction.Reduce` for the same centre P. Output: `PairReduction chiral shells=0 achiral shells=36; sampler IsChiral=2; mismatches=2`; the two mismatches are the Ce–O pair `centerP=0 → partnerP=9` (and reverse, bucket `234260`). The sampler reports `IsChiral=true` and a `ChiralPartnerGeometryId` where the stabilizer/orbit rule is achiral. Probe compiled against the reviewed Core/Symmetry projects in `/tmp/opencode/xc02probe`; no repository file was changed. |
| Targeted greps | Diagnostic codes/catalog coverage, `GeneratesReverse`, reverse links, `EnergyId`/`GeometryId`, `ComputeEnergy`/`ComputeTransition`, structure preview cache keys, `ProjectValidationPipeline`/catalog tests, typed IDs, and stale status/hash wording; results are cited below. |

### Original-design fidelity — corrections

| Intended correction (trace) | Evidence that it is corrected |
| --- | --- |
| Exact space-group data and the corrupt group 47 (Appendix B §10; Step 02) | Embedded spglib catalog, 230 groups/274 settings, checksum, `SpaceGroupCatalogTests.Group47IsTheCorrectedPmmmSetting` and `SpaceGroupCatalogCorruptionTests`; `verify.sh contracts` reports the catalog current. |
| Real crystallographic cell matrix including the missing `c` (Appendix B §10; Step 02) | `CellGeometryTests.TriclinicCellHasTheExpectedCVectorComponents` and `TriclinicRegressionMatchesIndependentVectorsAnglesAndVolume`; `CellGeometry.TryCreate` rejects inconsistent cells. |
| Exact P1 order and no tolerance-based hashing (Step 02 contract; `AGENTS.md` §10 P1 rule) | `UnitCellVectorEncoder` uses the `24 × 10^12` grid; `UnitCellVectorEncoderTests`, `BruteForceReductionTests`, and sample P-order tests; P is sorted by exact fractional order. |
| Strict reference positions, duplicate-name errors, 63/100/1000 limits (Step 03 recorded deviations) | `StructureModelValidatorTests` (`STR004`, `STR005`, `STR006`, `STR012`, `STR013`, `STR015`), `ParticleModelValidator` tests, and `ParticleSetTests`; strict `[0,1)`/trim tolerance and reserved IDs are explicit. |
| Empty-list indexing, disabled self-exchange, ambiguous end-state selection, conservation only at rule generation, and unbuildable-warning severity (Step 04 original-defect table) | `TransitionValidationTests` (self/unknown/asymmetric/cross-kind reverse links, complex reverse warning), `TransitionChainTests` (`GeneratesReverse` mismatch, custom grammar), `TransitionPathTests` (ambiguous match, stability sequence, repeated encoding), and Symmetry path tests; `KineticSimulation`/`MetropolisTransition` keep `SiteA == SiteB` valid. |
| Explicit chirality and cluster-count conventions; energy warning→error policy; site-energy range (Step 05) | `EnergyPreviewTests` (chiral P1 case, Burnside count, limits), `EnergyGeometryTests`, `EnergyDiagnosticCodeTests`, `ClusterTests`; warning/error table is implemented. **Caveat: the public preview's chiral metadata is wrong for achiral high-symmetry pairs (F-01), although `PairReduction` itself implements the orbit rule.** |
| Unique doping priority, wrong-sign compensation error, derived building block, normalization null, MCSP semantics (Step 06) | `DopingTests` (`LAT006`, `LAT009`, priority/shape), `LatticeSimulationPersistenceTests`, `SimulationModelTests` (all `SIM` codes/defaults), `ProjectEditorLatticeSimulationTests`; Step 14 design defines `target_mcsp * mobile_count` and the original "steps per particle" label is preserved. |
| Reflection-conflict mutation, no catalog, double validation, no mutation (Step 07) | `ProjectValidationPipeline` stages, `DiagnosticCatalog` completeness/docs tests, `ProjectValidationPipelineTests`/`ValidationRobustnessTests`, and the no-mutation hash tests; `ValidateFileAsync` disables store validation-on-load. |

No intended Phase B correction was found silently retained. The one corrected-behaviour defect found is F-01 (the preview bypasses the already-correct orbit primitive).

### Original-design fidelity — ported strengths (keep / adapt / drop)

| Original aspect | New design | Status and why |
| --- | --- | --- |
| Immutable model + explicit compiler instead of reflection manager/service-locator/event cache | `Mocassin.Core` immutable records, `Project` snapshot, `IProjectQuery`; `Mocassin.Compilation` pure compiler (Step 08 design) | **Kept/adapted.** Steps 03–06 implement the immutable model; Step 08 design keeps the explicit pipeline. Evidence: project files/`Project.cs`; no reflection or service locator in Phase B code. |
| Exact symmetry, canonical P1 order, pair/cluster shells, chiral links | `Mocassin.Symmetry` catalog/encoder/`PairReduction`/`ClusterReduction`; `PairShell.ChiralPartnerIndex` | **Kept.** Implemented and tested in Step 02/05; `PairReductionTests` and `BruteForceReductionTests`. The public energy preview does not use the chiral link correctly (F-01); the primitive is present. |
| Auto-sampled pair interactions, stable geometry identity, shared chiral energy table | `IEnergyGeometrySampler`, `GeometryId`/`EnergyId`, `PairShell`; Step 08/09 consume `EnergyId` | **Kept/adapted.** Implemented in Step 05; the identity includes `centerP`/`partnerP` to avoid shell collisions. The chiral linkage metadata is the F-01 defect. |
| Diagnostics as a pipeline, stable codes, no silent mutation | Step 07 `ProjectValidationPipeline`, `DiagnosticCatalog`, stage results, sorted/deduplicated diagnostics | **Kept/improved.** Implemented and tested; replaces reflection-discovered validators and conflict mutation. |
| Per-environment energy cache with path-local S2 deltas | Step 14 environment energy vectors + links; Step 15 `environment_energy`, pair/cluster delta propagation, dynamic S2 backup/restore | **Kept/adapted at design level.** The C solver's per-environment energy array becomes a Rust environment-energy vector with path-local deltas; implementation belongs to Phase D. |
| Static S2 corrections | Step 08 rule `static_correction` (NaN = dynamic); Step 14/15 compute and validate static corrections; dynamic is the v1 default | **Kept, deliberately staged.** Step 15 risk/tasks record “start with dynamic S2; add static corrections only after correctness is proven”, with a compiler/engine task and dynamic/static equivalence tests. This is a recorded performance trade-off, not an unrecorded loss. |
| Selection pool and selection optimizers | Step 14 `DirectionPool`/`SelectionPool`; Step 10 `SelectionOptimizer` removes `(site, particle)` start options | **Kept.** Designed for Steps 10/14; no Phase B implementation claimed. |
| Pre-run normalization and KMC time step | Step 06 `PreRunMcsp`/normalization fields; Step 14 §8 `K_fix * K_auto`, `dt = dof * K_norm / (ν_max * selectable)`; Step 16 owns execution | **Kept.** Model fields and solver design preserve the original semantics; the `-1` sentinel became `0`/`null` with a recorded deviation. |
| CSR neighbour links / jump status | Step 14 `link_offsets`/`links`/`cluster_links`; jump-status CSR with the original 56-link cap and dynamic fallback | **Kept.** Designed in Phase D step pages. |
| Incremental lattice-energy updates | Step 15 updates lattice energy by accepted ΔE and periodically reconciles against a full sum | **Kept.** Designed with a debug consistency test. |
| Legacy formats, WPF/SharpDX, MSI, multi-project file, PCG32 trajectories | New formats/UI/packaging and ChaCha12 statistical parity | **Dropped deliberately.** ADR 0001, 0003, 0005, 0007; Appendix A §8 and Appendix B §10 record the replacements. No unrecorded drop was found. |

No original strength intended for Phase B is missing from the model-definition design. The solver-side strengths above are not implemented at this checkpoint and are therefore not marked “verified in code”; their design exists and Phase D/XC-08 must verify them.

### Original-design fidelity — regressions

- **Capability/semantics:** no unintended loss found in Steps 03–07. Deliberate deviations are recorded in the step pages (strict validation, unique priorities, `null` normalization, 0 pre-run sentinel, additive v1, statistics budgets).
- **Determinism:** canonical sorting, exact-grid P1, SHA-256 content IDs, and no hash-iteration ordering are implemented; tests pass. No regression found.
- **Diagnostics:** severity policy is stricter and the catalog is complete; no loss found.
- **Performance/memory:** Phase B preview benchmarks are recorded (structure/transition/energy pages); no Phase B benchmark regression was measured. Solver performance is deferred to Step 29, not claimed here.
- **Maintainability:** immutable model and explicit compiler improve the original reflection/manager graph; no regression found.
- **F-01 is a correctness defect, not a deliberate regression.**

### Drift-check assessment (architecture/contracts)

- **D8 (Minor) is confirmed open.** `contracts/manifest.json:5-6` still has `"status": "step-06"` and Step 06-only `notes`, although `reports.validation` was added at `contracts/manifest.json:2183` by Step 07 (`0ca9bd0`) and the generated `validation-v1.schema.json` is current. `generate.py --check` does not compare `status`/`notes`, so the gate passes while the human-readable status is stale. This record carries it as F-02.
- **D9 is closed.** `docs/planning/README.md:80` now says “`.msl` v1 writer”; the checkpoint commit `81fba45` made that change. No `v2` premise remains in the step index.
- **D1/D2/D3 are closed on the privileged surfaces** (`AGENTS.md` §1 and the root/public status pages now say Phase B complete and Step 08 next; the dashboard’s active-work paragraph and Step 07 row are corrected). The planning index’s bottom status table was missed and is F-03.
- D4–D7 and D10–D15 are process/security/status/documentation items outside this review’s architecture/contracts remit; they are not re-scored here.

### Limitations

- Step 08 is **not implemented** and its design review has not started; this record reviews the design and the interfaces it consumes, not Step 08 code or its approval.
- Windows/macOS CI was not run; Step 07 records no platform-specific code and the policy makes those manual.
- Statistical parity (Step 29) was not run; no parity claim is made.
- `./tools/verify.sh all --require` and the docs-only gates were not rerun; the required build, test, structure, API, and contract gates above were run directly. Documentation prose/coverage belongs to XC-03.

## Checklist

### A. Foundations

- [x] Scope, non-goals, and success criteria are explicit and internally consistent.
  - Reviewed: `00-scope-and-principles.md` §§2–9; non-goals match the Phase B model-definition scope; deferred
    solver/GUI/analytics items are named. No contradiction found.
- [x] Target architecture and project dependency direction match the project references.
  - Verified every `*.csproj`: Core is BCL-only; Symmetry→Core; Compilation→Core+Symmetry; Contracts→Compilation;
    Packages→Core+Compilation+Contracts; Results→Core+Contracts; Cli→Packages+Results. The only nuance is that
    Packages consumes Symmetry types through Compilation's transitive reference rather than an explicit
    `ProjectReference` (F-05, Note); the direction itself is correct.
- [x] Core/Symmetry/Compilation/Contracts/Packages/Results/App boundaries are consistent and respected.
  - Verified the project graph, `Mocassin.Architecture.Tests` structure test, and source namespaces. Core has no
    Symmetry/IO/UI reference; the Compilation project is still a pure module (Step 08 not implemented); Packages
    is the composition point; Results does not reference Packages. No boundary violation found.
- [x] Data contracts define the project, package, results, state, job, event, and CLI surfaces without
  contradictions.
  - Reviewed `02-data-contracts.md` §§1–5, `contracts/manifest.json`, and generated schemas. The implemented
    project and validation-report surfaces match. Package/results/state/job/event fields remain design-level
    placeholders for Steps 11/18/20 as intended; no Phase B contradiction was found.
- [x] Schema versions, additive-v1 decisions, manifests, generated artifacts, and fixtures are consistent.
  - `generate.py --check` reports 18 current files; the generated project and validation schemas match the
    manifest. Steps 03–07 record the pre-release additive-v1 exemption and reconcile it with ADR 0009. The
    manifest's human-readable `status`/`notes` are stale (F-02, Minor), but the machine-readable descriptors,
    schemas, constants, and fixtures are current.
- [x] Canonical JSON, hashing, typed IDs, units, diagnostics, and ordering rules are coherent.
  - Verified `ProjectJson`/`ProjectCanonicalizer`, `ProjectHasher` (`Compute`, `ComputeDocument`,
    `ComputeStructure`, `ComputeEnergy`, `ComputeTransition`), typed IDs, unit structs, `Diagnostic`/`ModelPath`,
    and the canonical ordering rules; property/hash tests pass.
- [x] Quality strategy, test tiers, property/brute-force/golden tests, and performance targets cover the
  model-definition steps.
  - Reviewed `03-quality-strategy.md` §§9.1–9.7 and §10. The suite contains 77 PR-fast and 6 PR-medium-tiered
    tests, property tests (`ModelPropertyTests`, `StructurePropertyTests`, `TransitionPropertyTests`,
    `TransitionPathPropertyTests`, `ProjectPropertyTests`), brute-force reduction/geometry tests, sample/golden
    fixtures, and recorded preview benchmarks. F-01 is a missing achiral preview regression test.
- [x] Open decisions, gated decisions, and deferred items are current and have owners.
  - `05-open-decisions.md` records resolved, gated (3D, AOT, updater, remote deploy, `.msr` tuning), and deferred
    scope with named steps/gates. The milestone-tag ownership gap is a process item (D6, XC-01), not an open
    architectural decision.
- [x] ADRs are consistent with the current design and not contradicted by step pages.
  - Reviewed ADRs 0001–0014. ADR 0001/0003/0005/0007/0009–0014 agree with the step pages; the additive-v1
    exemption is explicitly reconciled in Steps 03–07. No ADR contradiction changes a Phase B decision. The
    manifest status wording is covered by F-02.

### B. Model definition

- [x] Particles and particle sets: void/empty semantics, IDs, limits, defaults, name/symbol rules.
  - Verified `Particle`/`ParticleSet`/`ParticleModel`, reserved `ParticleId.Void`/`ParticleSetId.Empty`, the
    63-custom/100-set/1000-site limits, default first-set occupation, and `PAR001`–`PAR012` tests. No defect
    found.
- [x] Structure: space group, cell, sites, dummies, stability, P1 preview, overlap validation, schema.
  - Verified `StructureModel`, `SpaceGroupRef`, `CellParameters`, `CellSite`, `CellDummyPosition`, preview/cache,
    exact-grid overlap (`OrbitOverlap`/`STR007`), crystal-system validation, and generated project schema;
    `StructureModelValidatorTests`/`StructurePreviewTests` pass.
- [x] Transitions: state changes, groups, chains, patterns/linkers, custom grammar, reverse links, path
    matching, transition kinds, conservation boundary.
  - Verified `StateChange`/`StateChangeGroup`/`TransitionChain`/`KineticTransition`/`MetropolisTransition`,
    built-in and custom pattern validation, canonical path matching (`PathMatchTolerance`), `TRN001`–`TRN028`,
    and the Step 08 conservation boundary; tests pass.
- [x] Reverse behavior distinguishes rule-level inverse generation from automatic inverse transition models and
  explicit reverse transition links.
  - Step 04 makes `GeneratesReverse` true only for `Metropolis2`/`Migration3`, leaves complex mechanisms
    unidirectional, and uses `ReverseTransitionId` for explicit links; `TRN018`/`TRN024`/`TRN027` tests cover
    the distinction.
- [x] Energy: environments, filters, clusters, site energies, pair/chiral derivation, cost limits, preview vs
  compiled tables.
  - Verified `StableEnvironment`/`UnstableEnvironment`/filters, cluster geometry/limits, sparse site energies,
    `EnergyGeometrySampler`, `GeometryId`/`EnergyId` including `centerP`/`partnerP`, Burnside cost preview, and
    the Step 08 compiled-table design. **Chirality preview metadata is incorrect for achiral high-symmetry
    pairs (F-01, Major).**
- [x] Lattice/doping: occupation exchanges, priority order, default building block, charge compensation,
    truncation, preview.
  - Step 06 implements the model, unique `0..9999` priorities, derived building block, and compensation
    shape/sign (`LAT001`–`LAT011`). Truncation/compensation counts and the single preview are explicitly
    Step 10 (`JOB004`–`JOB015`); no duplicate generator exists. Matches the recorded division.
- [x] Simulations: KMC/MMC settings, defaults, transition references, job count, execution flags.
  - Verified all Step 06 settings/defaults/ranges (`SIM001`–`SIM037`), kind-compatible `TransitionRef`
    collections, authored order, `ExecutionFlags`, and the `.msl` `SolverJobFlags` boundary; tests pass.
- [x] Cross-domain invariants: site/particle/transition/exchange references, validation ownership, cascade
  behavior, empty-model behavior.
  - Verified `ProjectValidationService` composition, Core/Symmetry/Step 07 ownership, dependent-removal paths,
    `ModelDefinition.Empty`/missing-section defaults, and empty-model suppression; tests pass.
- [x] Public API, typed IDs, namespaces, XML docs, and the ApiCompat baseline are coherent.
  - `./tools/verify.sh api` passes for all nine checked assemblies; `PublicAPI.Unshipped.txt` and the baseline
    are current; typed IDs/namespaces follow the structure policy.
- [x] Derived data is not persisted; compiled-model expectations for Step 08 are defined.
  - Previews/caches/statistics/transition matrices are non-persisted; Step 08 defines `CompiledModel`, dense
    indices, signatures, and cache keys. No derived preview has a persisted byte format.

### C. Original design fidelity

Answer the three questions explicitly; a missing or unrecorded answer is a finding.

- [x] **Corrections:** every original defect, limitation, or undesirable behaviour that this phase intended to
  correct is actually corrected. Each intended correction is traceable to a requirement, ADR, or step decision,
  and the evidence shows the corrected behaviour. No intended correction is silently retained.
  - The corrections table in `Evidence` maps each intended correction to code/tests (group 47, triclinic `c`,
    exact P1, strict positions/names/limits, transition defects, energy severity/chirality, lattice/simulation
    semantics, validation non-mutation). No intended correction was found silently retained. F-01 is a
    corrected-behaviour defect in the public preview, not a retained original defect.
- [x] **Ported strengths:** the good and clever original design aspects that this phase intended to keep are
  carried into the new design and verified. This includes algorithms, data layouts, caches, incremental
  updates, validation strategies, performance shortcuts, and workflow ideas. The record states what was kept,
  adapted, or dropped, and why.
  - The keep/adapt/drop table in `Evidence` records the immutable model/compiler, exact symmetry/P1/reduction,
    auto-sampled pairs and stable identities, diagnostics pipeline, per-environment energy vectors/deltas,
    static S2 (staged), selection pool/optimizers, pre-run normalization, CSR links, and incremental lattice
    energy. Phase B code verifies the model/symmetry/energy/validation strengths; solver strengths are design-
    level only until Phase D, and no Phase B claim exceeds that.
- [ ] **Regressions:** no unintended regression over the original remains in capability, semantics,
  performance, memory, diagnostics, determinism, or maintainability. Any deliberate regression is recorded as
  an explicit trade-off with rationale, owner, and follow-up.
  - **Not satisfied because of F-01.** The original/`PairReduction` orbit rule marks ceria's Ce–O directions
    achiral; the public preview marks them chiral and exposes a chiral partner ID. This is a semantics/ported-
    strength defect, not a recorded trade-off. Other deliberate regressions (PCG32→ChaCha12, WPF→Avalonia,
    legacy formats→clean break, dynamic S2 default) are recorded with ADR/step rationale and follow-ups.
- [x] Reverse transition behavior matches the intended original semantics and the deviation is recorded.
  - Step 04 records the rule-level versus explicit-link distinction, the required `GeneratesReverse` values,
    the complex-mechanism warning, and the original-defect corrections; tests cover the reverse links and the
    vehicle/interstitialcy unidirectional behaviour. No unexpected inverse generation was found.
- [ ] Chirality/pair counting, cluster counting, charge compensation, MCSP semantics, and original defects are
  resolved or explicitly documented.
  - Cluster counting (center factor, 6-bit/9-site, Burnside), charge compensation (Step 06 sign/zero, Step 10
    exact ratio/truncation), MCSP (`target_mcsp * mobile_count`), and original defects are documented and
    tested. **The chirality part is not fully resolved in the public energy preview (F-01); the `PairReduction`
    primitive itself is correct.**
- [x] Deviations from the original are intentional, justified, and testable.
  - Step 03–07 deviation tables (strict validation, duplicate sets, warnings→errors, unique priorities, null
    normalization, 0 pre-run sentinel, additive v1, statistics guardrails) name the rationale and each has a
    boundary/negative test or a Step 10/16 follow-up.
- [x] A keep/adapt/drop inventory for original design aspects exists, and deliberate regressions have an owner
  and follow-up.
  - The inventory is the `Evidence` keep/adapt/drop table; Appendix A §8 and Appendix B §10 supply the
    original-to-new mapping and fixed-defect list. Deliberate solver trade-offs name Step 14/15/16 ownership
    and dynamic/static S2 tests; deferred scope is in `05-open-decisions.md`. A durable standalone inventory
    page would still be useful for later phases (Note in F-03/F-06 follow-up).
- [x] The evidence is concrete: code, tests, benchmarks, or a side-by-side design comparison, not only prose.
  - Evidence cites source symbols, test names, generated contract checks, the original `SpaceGroupService`
    chiral predicate and solver files, and the targeted ceria probe output. The F-01 probe is a side-by-side
    comparison of `PairReduction` and the preview.
- [x] No hidden scope or unowned scientific assumption remains.
  - Reviewed the step pages, open decisions, and diagnostics tables. Remaining assumptions are named (chiral
    orbit definition, static S2 staging, statistics guardrails, Step 10 truncation). F-01 is an owned defect,
    not a hidden assumption.

### D. Cross-step interfaces

- [x] Step 08 compiled model, indices, signatures, and caches consume the Step 03–06 definitions coherently.
  - Step 08 uses `Project`, `IProjectQuery`, `IEnergyGeometrySampler`, `ProjectHasher.ComputeEnergy`/
    `ComputeTransition`/`ComputeStructure`, `PairInteractionPreview` (`GeometryId`/`EnergyId`), `TransitionPreview`,
    `ModelDefinition` sections, and `SolverJobFlags`. All exist at the frozen commit except the compiler itself.
    F-01 must be fixed so the sampler's chiral metadata is safe for Step 08 to consume.
- [x] Steps 09–12 parameterization, job expansion, package writer, and CLI contracts are compatible.
  - Step 09 uses the shared `EnergyId` and retires `PRM007`; Step 10 uses `JobDescriptor`, `LatticeRequest`,
    `SelectionOptimizer`, `SolverJobFlags`; Step 11/12 consume the Step 08 `CompiledModel` and Step 07
    `ValidationResult`. No contradictory field/type was found.
- [x] No unresolved cross-step interface question remains.
  - Reviewed Step 08's index/signature/cache/rule-encoding questions and the Step 09/10/11 boundaries. The
    only open item is the F-01 defect, not an undecided interface contract.
- [x] Step 08 design can start without reopening Steps 00–07 decisions.
  - Yes, subject to the F-01 remediation; Step 08 still needs its own independent design review and must not
    consume known-wrong chiral preview metadata.

### E. Evidence and exit criteria

- [x] Contract/schema/fixtures are current.
  - `generate.py --check` and `verify.sh contracts` pass (18 current artifacts; 274-setting catalog). The
    project and validation-report schemas match the manifest; later package/results/state/job/event fixtures are
    correctly marked planned. The manifest `status`/`notes` drift is F-02 (Minor).
- [x] Property/brute-force/golden tests cover the model invariants claimed by the design.
  - The suites cover canonical hashing/round-trips, exact P1/reduction, brute-force pair/cluster oracles, filter
    boundaries, permutation limits, transition paths, diagnostics, composition, and samples. The missing
    achiral preview test is part of F-01.
- [x] Public API baseline and build gates pass at the frozen commit.
  - Build 0 warnings/0 errors; 898 tests pass; structure, API, and contract gates pass; ApiCompat passes all
    nine checked assemblies.
- [ ] No Blocker or Major finding remains, or every exception is explicitly waived by the human owner.
  - **Not satisfied: F-01 is a Major finding.** No waiver is recorded in this review.
- [x] Minor/Note findings have owners and follow-ups.
  - F-02–F-06 name the responsible area/owner and a concrete follow-up in the Findings table; the human owner
    still approves the remediation plan and any waiver.
- [ ] The human owner approves the verdict and remediation plan.
  - Awaiting the human owner; the reviewer does not approve, merge, or waive.

## Findings

| ID | Severity | Area | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- |
| F-01 | **Major** | Energy pair identity / Step 08–09 interface | The public energy preview mislabels achiral high-symmetry pairs as chiral. `EnergyGeometrySampler` pairs directions by an inverse `(site, P, bucket)` key instead of the centre-stabilizer orbit, so it sets `IsChiral = true` and a `ChiralPartnerGeometryId` for directions that `PairReduction`/the Step 05 rule call achiral. On the canonical ceria sample the reduction reports 36 achiral shells and 0 chiral shells, while the preview reports 2 chiral pairs (Ce–O `centerP=0 ↔ partnerP=9`, bucket `234260`). This contradicts Step 05 §5 (“inverse direction is not in the same `G0` orbit”; achiral `EnergyId == GeometryId`) and the Step 08 design that consumes the sampler for pair geometry. Step 09 would receive a false chiral partner index even though the shared `EnergyId` currently prevents independent values. This is a ported-strength/semantics defect, not a recorded trade-off. | `src/dotnet/Mocassin.Symmetry/Energy/EnergyGeometrySampler.cs:104-176` (inverse-key match and unconditional `isChiral = true`); `src/dotnet/Mocassin.Symmetry/Reduction/PairReduction.cs:104-132` (correct stabilizer/orbit check); `PairInteractionPreview.cs:17-18`; Step 05 `docs/planning/steps/step-05-energy-model.md:228-245,633`; Step 08 `docs/planning/steps/step-08-model-compilation-pipeline.md:134-137,151-158`; ceria probe output `PairReduction chiral shells=0 achiral shells=36; sampler IsChiral=2; mismatches=2`. | Derive `IsChiral`/`ChiralPartnerGeometryId` from `PairShell.ChiralPartnerIndex` (or perform the same stabilizer orbit test) and set `EnergyId = GeometryId` for achiral pairs; add an achiral high-symmetry (ceria Ce–O or equivalent) preview test alongside the existing P1 chiral test; make Step 08 compile from the reduction result and cross-check the preview. Re-run Step 05/07 tests and re-review this record; Step 08 still needs its own design review. |
| F-02 | Minor | Contracts / manifest status | The contract manifest’s human-readable status is still `step-06` with Step 06-only notes, although Step 07 added `reports.validation`/`validation-v1` and the generated schema is current. The generator only freshness-checks artifacts, so the drift remains invisible to the gate. Drift-check D8 is confirmed open. | `contracts/manifest.json:5-6`; `contracts/manifest.json:2183`; `python3 tools/contracts/generate.py --check` → `Contract artifacts are current (18 files)`; `docs/planning/drift-checks/2026-11-12-phase-b.md:78` (D8); `tests/contracts/README.md` still describes only Step 01/03 project fixtures while Step 04–07 fixtures exist. | Set `status` to `step-07`, add the Step 07 validation/report note, refresh the fixture README, and consider making `generate.py --check` compare `status`/`notes` or define the field explicitly as “last schema-changing step”. Owner: implementor/planner. |
| F-03 | Minor | Design/phase status | The planning index’s bottom status table still says “`.NET model builder — Not started — Step 03 next`” and leaves the Rust solver, Avalonia GUI, and parity validation “Not started”, contradicting the Phase B checkpoint, the `step-03`–`step-07` tags, `status.md`, and `AGENTS.md` §1. The drift-check remediation updated `AGENTS.md`, `README.md`, `docs/index.md`, and `docs/development/index.md` but missed this mandatory-first-read planning index. | `docs/planning/README.md:140-151` (especially line 148) versus `docs/planning/status.md:57-58`, `AGENTS.md:11-14`, and tags `step-03`–`step-07`; drift-check D5/D2 surface list `docs/planning/drift-checks/2026-11-12-phase-b.md:72`; prior Step 02 policy-review fix `docs/planning/reviews/repository-policy-review-2026-11-08.md:218`. | Update the table to Phase B complete / Step 08 next and make this surface part of the status-consistency check so the Step 02-style regression cannot recur. Owner: orchestrator/planner (XC-01/XC-03 may also record it). |
| F-04 | Minor | Design decisions / cache-key documentation | `docs/development/design-decisions.md` still states “Three SHA-256 hashes are exposed” and lists only `Compute`, `ComputeDocument`, and `ComputeStructure`, although Step 04/05 added public `ComputeTransition` and `ComputeEnergy`; the Step 08 cache-key design depends on the latter two. A Step 08 implementor reading the design-decision page gets an incomplete cache-key inventory. | `docs/development/design-decisions.md:104-115`; `src/dotnet/Mocassin.Core/Project/ProjectHasher.cs:43,72,97`; Step 05 §1 `docs/planning/steps/step-05-energy-model.md:101-105`; Step 08 §9 `docs/planning/steps/step-08-model-compilation-pipeline.md:282-287`. | Extend the page to all five public hash entry points (or link to the cache-key section) and state which projection each one covers. Owner: planner/documentation; XC-03 may also record it. |
| F-05 | Note | Project graph clarity | `Mocassin.Packages` consumes `Mocassin.Symmetry` types directly (`ProjectValidationService`, `ModelStatisticsService`, `FeasibilityValidator`, `CrossDomainValidator`) but its `.csproj` references only Compilation, Contracts, and Core; Symmetry is available only transitively through Compilation. The direction is not forbidden, but the dependency is implicit and fragile if Compilation ever drops or scopes its Symmetry reference. | `src/dotnet/Mocassin.Packages/Mocassin.Packages.csproj:15-19`; `src/dotnet/Mocassin.Packages/Projects/ProjectValidationService.cs:8-12`; `src/dotnet/Mocassin.Packages/Projects/Validation/ModelStatisticsService.cs:7-9`; `docs/planning/01-target-architecture.md:127-131`; `docs/development/architecture.md:8-20`. | Add an explicit `Mocassin.Symmetry` `ProjectReference` (or document the intentional transitive use) and update both dependency diagrams; consider an architecture test for the declared project graph. Owner: implementor/planner. |
| F-06 | Note | Architecture documentation status | The developer architecture page is still Step 03-era: it says `Mocassin.Core.Model` will later gain transition/energy/lattice/simulation sections and documents only the Step 03 particle/structure layer. Phase B’s transition, energy, lattice/simulation, and validation architecture is documented in the step pages and `compilation-pipeline.md`, but not in the architecture overview a new developer would read first. | `docs/development/architecture.md:35,120-147`; implemented sections in `Mocassin.Core/Model/ModelDefinition.cs:65-139`; `docs/development/compilation-pipeline.md:96-174`; Step 07 architecture summary in `docs/development/design-decisions.md:67-86`. | Fold the Phase B sections into `architecture.md` or add clear links to `compilation-pipeline.md`; this overlaps XC-03 and Step 30, so the documentation owner should own the fix. |

## Record state

**State:** Re-review required — round 3 (R3-F1 Major, R3-F2 Minor)

`Re-review required` is a record state, not a verdict. Round 2 was performed and still requires fixes.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work, did not participate in any Phase B
> implementation, audit, or design review, and have no shared conversation with the request owner, the other
> Phase B reviewers, or the orchestrator. I read the repository at the frozen commit and the original reference
> read-only; I wrote only this record.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [x] Fixes required
- [ ] Waived

Follow-up owner: Phase B orchestrator/implementor for F-01; planner/documentation owners for F-02–F-06.
Follow-up step: Fix F-01 on a maintenance branch and re-review XC-02; the Step 08 design review remains separate
and must not consume the known-wrong chiral preview metadata.

## Remediation status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| F-01 | Fixed | Phase B orchestrator | `8d61e71` (fix and ceria regression test), `4a98ad8` (checkpoint integration), `ddf192e` (final checkpoint) |
| F-02 | Fixed | Phase B orchestrator | `62e1072`, `ddf192e` — manifest status/notes and fixture README updated |
| F-03 | Fixed | Phase B orchestrator | `62e1072` — planning-index status table now says Phase B complete / Step 08 next |
| F-04 | Fixed | Phase B orchestrator | `62e1072` — design-decisions page lists all five public hash entry points |
| F-05 | Accepted note — follow-up | Implementor/planner | — — add an explicit `Mocassin.Symmetry` reference or document the intentional transitive use |
| F-06 | Accepted note — follow-up | Documentation owner | — — fold the Phase B sections into `architecture.md` or link the compilation-pipeline page |

The F-01 correction makes chirality a symmetric pair property: `PairShell.IsChiral` records the inverse orbit
test, and the sampler requires both directed centre orbits to see the inverse as distinct before linking a
chiral partner. Achiral pairs keep `EnergyId == GeometryId`. The Step 05/08 pages and physics/reference docs
state the same rule.

## Round 2 request

| Field | Value |
| --- | --- |
| Target commit | `ddf192e7e6fc9c3f51a2d9924913c01fc20ca600` (Phase B checkpoint after F-01 and F-02–F-04 remediation; parent of this request commit) |
| Requested focus | Re-review F-01 and its regression test; confirm no Blocker/Major remains; append a new round without altering round 1 |
| Reviewer | — (fresh independent reviewer; fill name and date) |
| Review date | — |

The orchestrator fixed F-01 on `fix/energy-pair-chirality` and integrated it into the checkpoint. F-05 and F-06
are recorded as accepted notes with Step 08/30 follow-ups; they are not Blocker/Major findings.

## Human approval

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:

## Round 2

| Field | Value |
| --- | --- |
| Reviewer | Fresh independent reviewer (OpenCode subagent), round 2 |
| Review date | 2026-11-12 |
| Reviewed commit | `ddf192e7e6fc9c3f51a2d9924913c01fc20ca600` (Phase B checkpoint after F-01 and F-02–F-04 remediation) |
| Worktree | `/tmp/opencode/mocassin-xc-02-r2`, branch `docs/xc-02-re-review`, HEAD `e88b7134d184f337f0a4957dd8dab998876e14a3` |
| Request commit delta | `git diff --stat ddf192e..HEAD` = five documentation files only (`cross-cutting/README.md`, `xc-01`, `xc-02`, `xc-03`, `status.md`); no code, tests, contracts, schemas, or design pages changed after the frozen checkpoint |
| Record state result | `Fixes required` |

### Independence

I am a fresh context. I did not author or implement the Phase B work or the round-1 review, did not take part in
the F-01/F-02–F-04 remediation, and have no shared conversation with the round-1 reviewer, the orchestrator, the
Phase B implementors, or the other reviewers. I read the repository and the original reference read-only and
wrote only this record. The remediation table, round-1 request, evidence, findings, checkboxes, and verdict text
above were not changed; only `Record state` was updated to the round-2 result and this section was appended.

### Frozen state

- `git status --short` (before this record edit): empty.
- `git log --oneline --decorate -15`: `e88b713` (HEAD, request), `ddf192e` (frozen checkpoint), `9ece806`,
  `374da61`, `415fe28`, `62e1072`, `4a98ad8` (F-01 integration), `8d61e71` (F-01 fix), `f77e8ad`, `2b8f84c`
  (round-1 record), `08dd8df`, `2fe7b91`, `81fba45`, `38a22d2` (`main`/`origin/main`), `3ed558a` (`step-07`).
- `git worktree list`: canonical `/workspace/mocassin` at `38a22d2`; review worktrees at their expected
  branches. No branch was changed, merged, tagged, pushed, or deleted.

### Commands run and results

| Command | Result |
| --- | --- |
| `git status --short` | empty before the record edit |
| `git log --oneline --decorate -15` | as listed above; no code commit after `ddf192e` |
| `dotnet build Mocassin.slnx -c Release` | **Build succeeded**, 0 warnings, 0 errors (all 21 projects) |
| `dotnet test Mocassin.slnx -c Release --no-build` | **899 passed**, 0 failed: Core 564, Symmetry 218, Packages 105, Cli 7, App 1, Compilation 1, Contracts 1, Results 1, Architecture 1 |
| `dotnet test src/dotnet/Mocassin.Symmetry.Tests/... --filter FullyQualifiedName~EnergyPreviewTests` | **13 passed**, 0 failed (includes the new `CeriaHighSymmetryPairsAreAchiral`) |
| `dotnet test src/dotnet/Mocassin.Symmetry.Tests/... --filter FullyQualifiedName~ReductionTests` | **21 passed**, 0 failed (includes the stronger P1 chirality assertions) |
| `./tools/verify.sh structure` | passed: `Mocassin.Architecture.Tests` 1/1 |
| `./tools/verify.sh api` | NuGet licence/vulnerability policy OK; ApiCompat OK for all nine checked assemblies (`Core`, `Symmetry`, `Compilation`, `Contracts`, `Packages`, `Results`, `App.ViewModels`, `App.Views`, `Rendering`) |
| `./tools/verify.sh contracts` | `Contract artifacts are current (18 files)`; catalog current (274 settings, sha256 `1f3bf67…e3395`); process consistency OK; tool tests 18/18; SPDX 345 files; secret scan 661 files; `cargo-deny` advisories/bans/licenses/sources OK |
| `python3 tools/contracts/generate.py --check` | `Contract artifacts are current (18 files)` |
| Targeted probe (outside the repository, `/tmp/opencode/xc02-r2-probe`, not committed) | Ceria: 36 previews, 0 chiral, 36 achiral, 0 chiral/null-partner, 0 achiral/partner, 0 achiral `EnergyId != GeometryId`; reduction: 8 orbit-chiral and 16 achiral shells, 0 linked same-centre partners. P1 collision fixture: 4 previews, 2 asymmetric partner links, 1 declared chiral pair with different `EnergyId`, 2 distinct `EnergyId` values over 4 directions (details in R2-F1). |

### Files reviewed

- `AGENTS.md` §§1, 3, 4, 5, 8, 10, 11, 12, 16; `docs/planning/reviews/cross-cutting/README.md` §4; the full
  XC-02 record; `docs/planning/reviews/README.md` §6; `docs/planning/drift-checks/2026-11-12-phase-b.md`.
- Step 05 (`step-05-energy-model.md`) and Step 08 (`step-08-model-compilation-pipeline.md`) pages;
  `docs/physics/energies.md`; `docs/reference/energy.md`; `docs/development/design-decisions.md`;
  `docs/development/compilation-pipeline.md`; `docs/development/architecture.md`;
  `contracts/manifest.json`; `tests/contracts/README.md`; `docs/planning/README.md`; `docs/planning/status.md`.
- `src/dotnet/Mocassin.Symmetry/Reduction/PairReduction.cs`,
  `src/dotnet/Mocassin.Symmetry/Energy/EnergyGeometrySampler.cs`,
  `src/dotnet/Mocassin.Symmetry/Energy/PairInteractionPreview.cs`,
  `src/dotnet/Mocassin.Symmetry.Tests/Energy/EnergyPreviewTests.cs`,
  `src/dotnet/Mocassin.Symmetry.Tests/ReductionTests.cs`,
  `src/dotnet/Mocassin.Symmetry/PublicAPI.Unshipped.txt`,
  `src/dotnet/Mocassin.Core/Project/ProjectHasher.cs`,
  `src/dotnet/Mocassin.Packages/Mocassin.Packages.csproj`.
- Original reference read-only: `SpaceGroupService.CheckInteractionGeometryIsChiralPair` /
  `GetUnitCellP1PathExtension` and `PairInteractionFinder` (for the original chiral-pair semantics).

### F-01 — fixed

The correction is present in both the reduction primitive and the sampler, not only in prose.

- `PairShell.IsChiral` is computed as `!IsSameOrbit(stabilizer, inverse, displacement)` from the centre
  stabilizer (`PairReduction.cs:107-118`); `ChiralPartnerIndex` is only linked for chiral shells.
- `EnergyGeometrySampler` now requires `AllowsChirality` and `Shell.IsChiral` on **both** directed candidates
  before setting `IsChiral`/`ChiralPartnerGeometryId`, and leaves `EnergyId = GeometryId` otherwise
  (`EnergyGeometrySampler.cs:160-191`). Unstable–stable candidates pass `AllowsChirality: false`.
- The ceria reproduction now reports no chiral pairs: 36 achiral previews, no `ChiralPartnerGeometryId`, and
  `EnergyId == GeometryId` for every preview. This matches the Step 05/08 and `docs/physics/energies.md` rule.
  The P1 chiral-pair test still passes and asserts `shell.IsChiral` and linked partners; the new ceria test
  passes.
- `docs/physics/energies.md`, Step 05 §5, and Step 08 §4 all state the same both-centre orbit rule. The
  reference page states the `EnergyId` consequence but not the test definition; see R2-F2.

F-01 is **Fixed**. The broader chiral-pair identity path still has a separate defect, R2-F1.

### F-02–F-04 — fixed

- **F-02 Fixed.** `contracts/manifest.json` has `"status": "step-07"` and notes that record both the Step 06
  lattice/simulation additions and the Step 07 additive `reports.validation`/`validation-v1` report; the
  `reports.validation` entry exists and the generated artifacts are current (`generate.py --check`). The
  `tests/contracts/README.md` claim now covers Step 03–07 fixtures. The fix is accurate, not merely present.
- **F-03 Fixed.** `docs/planning/README.md` now says “.NET model builder — Partial — model definition complete
  through Step 07; Step 08 next” and adds the Step 07 validation/diagnostics/statistics row. This agrees with
  `AGENTS.md` §1 (“Phase B is complete … Step 08 … is next”) and `status.md`.
- **F-04 Fixed.** `docs/development/design-decisions.md` now says “Five SHA-256 hashes are exposed” and lists
  `Compute`, `ComputeDocument`, `ComputeStructure`, `ComputeEnergy`, and `ComputeTransition` with their
  projections. These are exactly the five public static entry points in `ProjectHasher.cs`.

### F-05 / F-06 — accepted notes remain

- **F-05 still a Note.** `Mocassin.Packages.csproj` still references only Compilation, Contracts, and Core;
  `ProjectValidationService`, `ModelStatisticsService`, `FeasibilityValidator`, and `CrossDomainValidator`
  still consume `Mocassin.Symmetry` namespaces through Compilation's transitive reference. The direction is
  allowed and the build/architecture gate passes, but the dependency remains implicit. This is not a
  Blocker/Major; the recorded follow-up stands.
- **F-06 still a Note.** `docs/development/architecture.md` remains Step 03-era (it still says later steps add
  transition/energy/lattice/simulation sections and documents only particles/structure); the Phase B
  architecture is in `compilation-pipeline.md`. This is a documentation-status item with the recorded
  follow-up; it is not a Blocker/Major.

### Findings (round 2)

| ID | Severity | Area | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- |
| R2-F1 | **Major** | Energy pair identity / Step 08–09 interface | The F-01 fix makes ceria correct, but the sampler still matches a forward candidate to its inverse by a `DirectedKey` that omits the periodic offset. When two images of the same partner P share a distance bucket but have different cell offsets, both forward directions select the same reverse candidate (the lexicographically smallest geometry) instead of their true inverses. The preview then declares a chiral pair whose partner is not the inverse, and the two directions can keep different `EnergyId` values. This contradicts the Step 05 §5 / Step 08 §4 / `reference/energy.md` contract that a chiral pair links both directions to one shared `EnergyId`, and Step 08 uses the same `IEnergyGeometrySampler` kernel for compiled tables. | Independent probe with a valid P1 model: cubic 10 Å cell, stable sites A=(0,0,0) and B=(0.5,0.1,0.2), stable cutoff 6.0 Å. `TryCreatePairPreview` returns 4 directed pairs at 5.4772 Å and one warning (`ENM002`, advisory cutoff > half the smallest cell dimension; no error). Output: `083a…` partner `97ab…` energy `083a…`; `3cca…` partner `97ab…` energy `3cca…`; `97ab…` partner `083a…` energy `083a…`; `c7da…` partner `083a…` energy `083a…`. Two partner links are non-reciprocal, `3cca…` and its declared partner `97ab…` have different `EnergyId` values, and 4 directions collapse to 2 distinct `EnergyId` values. The true inverse of `3cca…` is the other reverse image (`c7da…`), not `97ab…`. Code: `EnergyGeometrySampler.cs:104-155` (`DirectedKey` has no offset) and `:165-180` (partner chosen by key); test gap: `EnergyPreviewTests` only covers ceria (no linked chiral partner) and the P1 test has no equal-distance same-P images. | Include the negated periodic offset (and the true inverse partner P) in the inverse key, or derive the exact inverse geometry from `PairShell`/the displacement rather than the bucket key; require reciprocal `ChiralPartnerGeometryId` and equal `EnergyId` for every declared chiral pair; add a P1 regression test with a half-cell-coordinate site and a cutoff above half the smallest cell dimension. If such period-ambiguous models are intentionally out of contract, make that an error or state it in Step 05/08 instead of leaving `ENM002` advisory. |
| R2-F2 | Note | Documentation / physics | The remediation note says the “physics/reference docs state the same rule”, but `docs/reference/energy.md` only states the `EnergyId`/`ChiralPartnerGeometryId` consequences; it never defines the both-centre orbit test. The rule is correctly stated in `docs/physics/energies.md`, Step 05, and Step 08. | `docs/reference/energy.md:63-74`; `docs/physics/energies.md:53-63`; Step 05 §5; Step 08 §4. | Add one sentence to the reference “Identities” section or link the physics page; no behaviour change. |
| R2-F3 | Note | Public API XML docs | `PairShell.ChiralPartnerIndex` is documented as “the linked inverse shell index, or null when not chiral”, but after the F-01 change a shell can be `IsChiral = true` with a null index when its inverse is not present in the same-centre reduction (ceria has 8 such shells). | `src/dotnet/Mocassin.Symmetry/Reduction/PairReduction.cs:17`; ceria probe reduction count `chiral=8 achiral=16 linked=0`. | Say “null when no inverse shell is present in this reduction”; the `IsChiral` remarks already explain the orbit property. |

Carry-forward status: **F-01 Fixed**, **F-02 Fixed**, **F-03 Fixed**, **F-04 Fixed**, **F-05 Accepted note**
(unchanged), **F-06 Accepted note** (unchanged). No round-1 finding is reopened except that the F-01 area
contains the new R2-F1 defect.

### Round 2 checklist

- [x] F-01 is implemented, not only described: both-centre orbit gate, `EnergyId == GeometryId` for achiral
  pairs, and the ceria case reproduced.
- [x] The P1 chiral-pair test still reports chiral partners, and the new ceria test runs in the suite.
- [x] Step 05/08 and `docs/physics/energies.md` state the same rule; the reference-page gap is R2-F2.
- [x] F-02, F-03, and F-04 remediations are accurate.
- [x] F-05 and F-06 remain Notes, not Blocker/Major findings.
- [x] Build, full test suite, targeted tests, structure, API, contracts, and generated-artifact checks pass at
  the frozen checkpoint.
- [ ] No Blocker or Major finding remains: **R2-F1 is Major**.
- [ ] The human owner approves the verdict and remediation plan.

### Round 2 verdict

- [ ] Passed
- [ ] Passed with notes
- [x] Fixes required
- [ ] Waived

Follow-up owner: Phase B orchestrator/implementor (R2-F1); documentation owner (R2-F2, R2-F3).

Follow-up step: Fix R2-F1 on a maintenance branch and re-review this record in round 3. F-01–F-04 are closed;
F-05/F-06 remain accepted notes. Step 08 must not consume the chiral-pair metadata as authoritative until
R2-F1 is fixed or the period-ambiguous case is explicitly rejected.

## Round 2 remediation

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| R2-F1 | Fixed | Phase B orchestrator | `47c5927` — the reverse-pair key now includes the negated periodic offset; equal-distance images link to their true inverse |
| R2-F2 | Fixed | Documentation owner | `47c5927` — `docs/reference/energy.md` now states the both-centre orbit rule |
| R2-F3 | Fixed | Implementer | `47c5927` — `PairShell.ChiralPartnerIndex` XML docs now distinguish the link from the `IsChiral` orbit property |

The independent probe for the P1 equal-distance fixture now reports 0 asymmetric partner links and 0 chiral
pairs with differing `EnergyId`; the ceria probe remains 36 achiral previews with `EnergyId == GeometryId`. The
new regression test is `EnergyPreviewTests.PeriodicImagesAtEqualDistanceLinkToTheirTrueInverse`. Full build
(900 tests), format, structure, API, contracts, and docs gates pass at `47c5927`.

## Round 3 request

| Field | Value |
| --- | --- |
| Target commit | `47c5927aba4c12d7e2dacbcaaad11c1db9e688c4` (Phase B checkpoint after R2-F1 and R2-F2/R2-F3 remediation) |
| Requested focus | Re-review R2-F1, R2-F2, and R2-F3; confirm no Blocker/Major remains; append a new round without altering rounds 1–2 |
| Reviewer | — (fresh independent reviewer; fill name and date) |
| Review date | — |

The orchestrator fixed R2-F1 on the checkpoint branch and added the equal-distance regression test. R2-F2 and
R2-F3 are documentation/XML-doc corrections. No round-1 or round-2 finding text was changed.

## Round 3

| Field | Value |
| --- | --- |
| Reviewer | Fresh independent reviewer (OpenCode subagent), round 3 |
| Review date | 2026-11-12 |
| Reviewed commit | `47c5927aba4c12d7e2dacbcaaad11c1db9e688c4` (Phase B checkpoint after R2-F1 and R2-F2/R2-F3 remediation) |
| Worktree | `/tmp/opencode/mocassin-xc-02-r3`, branch `docs/xc-02-r3`, HEAD `59ad7d716e2dede5b1ece2986ac54d7190d25df0` |
| Reviewed-tree delta | `git diff --stat 47c5927..HEAD` = five documentation files only; `git diff --stat 47c5927..HEAD -- src tests contracts tools .github` is empty, so the code, tests, contracts, and design pages reviewed are exactly the frozen checkpoint |
| Record state result | `Fixes required` |

### Independence

I am a fresh context. I did not author or implement the Phase B work, any earlier XC-02 round, or the
R2-F1/R2-F2/R2-F3 remediation, and I have no shared conversation with the round-1/round-2 reviewers, the
orchestrator, the Phase B implementors, or the other reviewers. I read the repository at the frozen commit and
the original reference read-only, and wrote only this record. Rounds 1–2, the request text, the checklists, and
the orchestrator's remediation tables were not changed; only `Record state` and this appended section were
written.

### Frozen state

- `git status --short` (before this record edit): empty.
- `git log --oneline --decorate -15`: `59ad7d7` (HEAD, request), `0650146` (round-2 record merge),
  `47c5927` (frozen code/design checkpoint), `c52f728` (round-2 record), `e88b713`, `ddf192e`, `9ece806`,
  `374da61`, `415fe28`, `62e1072`, `4a98ad8`, `8d61e71`, `f77e8ad`, `2b8f84c`, `08dd8df`; `main`/`origin/main`
  at `38a22d2`, `step-07` at `3ed558a`. No code, test, contract, or design commit follows `47c5927`.
- `git worktree list`: canonical `/workspace/mocassin` at `38a22d2`; review worktrees at their expected
  branches. No branch was changed, merged, tagged, pushed, or deleted.

### Commands run and results

| Command | Result |
| --- | --- |
| `git status --short` | empty before the record edit |
| `git log --oneline --decorate -15` | as listed in Frozen state; no code commit after `47c5927` |
| `dotnet build Mocassin.slnx -c Release` | **Build succeeded**, 0 warnings, 0 errors (all 21 projects) |
| `dotnet test Mocassin.slnx -c Release --no-build` | **900 passed**, 0 failed: Core 564, Symmetry 219, Packages 105, Cli 7, App 1, Compilation 1, Contracts 1, Results 1, Architecture 1 |
| `dotnet test src/dotnet/Mocassin.Symmetry.Tests/Mocassin.Symmetry.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~EnergyPreviewTests\|FullyQualifiedName~ReductionTests"` | **35 passed**, 0 failed |
| `dotnet test ... --filter "FullyQualifiedName~ChiralDirectionsShareEnergyIdAndPartnerGeometry\|FullyQualifiedName~CeriaHighSymmetryPairsAreAchiral\|FullyQualifiedName~PeriodicImagesAtEqualDistanceLinkToTheirTrueInverse"` | **3 passed**, 0 failed (the new equal-distance test, the ceria test, and the P1 chiral test) |
| `./tools/verify.sh structure` | passed: `Mocassin.Architecture.Tests` 1/1 |
| `./tools/verify.sh api` | NuGet licence/vulnerability policy OK (47 packages); ApiCompat OK for Core, Symmetry, Compilation, Contracts, Packages, Results, App.ViewModels, App.Views, Rendering; `Public API is compatible with the checked-in baseline.` |
| `./tools/verify.sh contracts` | `Contract artifacts are current (18 files)`; catalog current (274 settings, sha256 `1f3bf67…e3395`); `Process consistency: OK`; tool tests 18/18; SPDX 345 files; secret scan 661 files; `cargo-deny` advisories/bans/licenses/sources OK |
| `python3 tools/contracts/generate.py --check` | `Contract artifacts are current (18 files)` |
| `dotnet format Mocassin.slnx --verify-no-changes` | exit 0 (workspace-loading warnings only; no formatting change) |
| Independent probe (outside the repository at `/tmp/opencode/xc02-r3-review-probe`, not committed) | P1 equal-distance fixture: 4 previews, 4 chiral, 2 reciprocal pairs, 0 asymmetric links, 0 differing `EnergyId`; ceria: 36 previews, 0 chiral, every achiral preview keeps `EnergyId == GeometryId`; P4mm both-special-position fixture: 2 previews, 0 chiral, while both shells are orbit-chiral and the two orbits are exact negations (1 chiral pair missed); barium zirconate sample: 51 previews, 24 chiral, 0 diagnostics; 36 directed geometries have an orbit-membership inverse with both shells chiral, but only 24 are declared chiral, leaving 12 orbit-chiral directed geometries unlinked (all H–H; the orbit-membership inverse is not one-to-one for these cycles) |

The probe reconstructs stable candidates with `PairReduction.Reduce`, recomputes each `GeometryId` from the
documented projection, and compares the preview with the orbit-membership inverse (the negated representative
contained in a reverse shell's `EquivalentImages`), requiring both shells to be orbit-chiral. It is scratch
evidence only and is not part of the repository.

### Files reviewed

- `AGENTS.md` §§1, 3, 4, 5, 8, 10, 11, 12, 16; `docs/planning/reviews/cross-cutting/README.md` §4;
  `docs/planning/reviews/README.md` §6; the full XC-02 record; `docs/planning/drift-checks/2026-11-12-phase-b.md`.
- Step 05 (`step-05-energy-model.md`), Step 08 (`step-08-model-compilation-pipeline.md`),
  `docs/physics/energies.md`, `docs/reference/energy.md`, `docs/development/design-decisions.md`,
  `docs/development/compilation-pipeline.md`, `docs/development/architecture.md`, `contracts/manifest.json`,
  `tests/contracts/README.md`, `docs/planning/README.md`, `docs/planning/status.md`.
- `src/dotnet/Mocassin.Symmetry/Energy/EnergyGeometrySampler.cs`,
  `src/dotnet/Mocassin.Symmetry/Reduction/PairReduction.cs`,
  `src/dotnet/Mocassin.Symmetry/Energy/PairInteractionPreview.cs`,
  `src/dotnet/Mocassin.Symmetry.Tests/Energy/EnergyPreviewTests.cs`,
  `src/dotnet/Mocassin.Symmetry.Tests/ReductionTests.cs`,
  `src/dotnet/Mocassin.Symmetry/PublicAPI.Unshipped.txt`, `src/dotnet/Mocassin.Packages/Mocassin.Packages.csproj`.
- Original reference read-only, no copying: `SpaceGroupService.CheckInteractionGeometryIsChiralPair` /
  `GetUnitCellP1PathExtension` and `PairInteractionFinder.FindAndAssignChiralPartners` (the original
  ordered-path/orbit comparison and its reciprocal `SetChiralPartner`).

### R2-F1 — declared-pair requirement fixed, but the path still misses chiral links

- The periodic offset is now part of the identity: `DirectedKey` carries `OffsetA`/`OffsetB`/`OffsetC`
  (`EnergyGeometrySampler.cs:336-344`); the forward key uses the shell representative's offsets and the inverse
  key negates them (`:110-141`).
- For the P1 equal-distance regression fixture the declared pairs are correct: 4 previews, 4 chiral, two
  reciprocal pairs sharing one `EnergyId`, 0 asymmetric links, 0 differing `EnergyId`. The new
  `PeriodicImagesAtEqualDistanceLinkToTheirTrueInverse` passes and reproduces R2-F1's case.
- Ceria remains correct: 36 previews, 0 chiral, every achiral preview keeps `EnergyId == GeometryId`; the ceria
  regression test passes.
- **The fix is representative-exact, not orbit-exact.** The inverse key is built from
  `candidate.Shell.Representative`; the reverse candidate is found only when its own canonical representative
  is exactly the negated image (`EnergyGeometrySampler.cs:128-163`). When the inverse of the forward
  representative is an equivalent image of the reverse shell but not its representative, the lookup fails and
  the pair is silently reported achiral even though both `PairShell.IsChiral` flags are true. This is R3-F1.
- The remediation table's P1/ceria claims are accurate for those fixtures; the barium-zirconate sample was not
  probed by the remediation evidence.

### R2-F2 / R2-F3 — fixed

- **R2-F2 Fixed.** `docs/reference/energy.md` now states the both-centre orbit rule and the achiral
  `EnergyId == GeometryId` consequence, in agreement with `docs/physics/energies.md`, Step 05 §5, and Step 08
  §4. The rule is stated, not only the identifier consequences.
- **R2-F3 Fixed.** `PairShell.ChiralPartnerIndex` (`PairReduction.cs:17-19`) now says the link is present "when
  the inverse shell is present in this centre reduction, or `null` when no inverse shell is present. Use
  `IsChiral` for the orbit property"; the `IsChiral` remarks (`:27-35`) state that it is an orbit property
  independent of the link. This matches the ceria case (8 orbit-chiral shells with no same-centre partner).

### F-01–F-04 remain fixed; F-05/F-06 remain accepted notes

- **F-01 Fixed.** Ceria: 0 chiral previews, no `ChiralPartnerGeometryId`, every `EnergyId == GeometryId`; the
  both-centre gate and shared-`EnergyId` logic are present (`EnergyGeometrySampler.cs:173-188`); the P1 chiral
  test still asserts reciprocal partners and equal `EnergyId`.
- **F-02 Fixed.** `contracts/manifest.json:5-6` is `status: step-07` with Step 06/07 notes;
  `generate.py --check` and `verify.sh contracts` pass; `tests/contracts/README.md` covers the Step 03–07
  fixtures.
- **F-03 Fixed.** `docs/planning/README.md` says the model builder is complete through Step 07 with Step 08
  next, and lists Step 07.
- **F-04 Fixed.** `docs/development/design-decisions.md` lists all five public hash entry points
  (`Compute`, `ComputeDocument`, `ComputeStructure`, `ComputeEnergy`, `ComputeTransition`).
- **F-05 remains a Note.** `Mocassin.Packages.csproj` still references only Compilation, Contracts, and Core;
  the `Mocassin.Symmetry` use remains transitive through Compilation. The direction is allowed and the build
  and architecture gates pass; the recorded follow-up stands.
- **F-06 remains a Note.** `docs/development/architecture.md` is still Step 03-era; the Phase B architecture
  remains in `compilation-pipeline.md`. This is the recorded documentation follow-up, not a Blocker/Major.

### Findings (round 3)

| ID | Severity | Area | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- |
| R3-F1 | **Major** | Energy pair identity / Step 08–09 interface | Chiral linking is still incomplete for multi-image orbits. The forward key uses the shell representative and the inverse key requires the reverse shell's canonical representative to be the exact negation; when the inverse of the forward representative is an equivalent image of the reverse shell but not its representative, the lookup fails and the pair is silently reported achiral. In a P4mm (#99) fixture with A=(0,0,0) (1a), B=(1/2,1/2,0.2) (1b), a=5 Å, c=10 Å, cutoff 4.5 Å, the forward orbit `[(P1,-1,-1,0),(P1,-1,0,0),(P1,0,-1,0),(P1,0,0,0)]` and reverse orbit `[(P0,0,0,0),(P0,0,1,0),(P0,1,0,0),(P0,1,1,0)]` are exact negations, both shells have `IsChiral=true`, yet the preview returns 2 entries, both `IsChiral=false`, `EnergyId==GeometryId`, no partner. The shipped barium-zirconate sample (cutoff 2.0 Å, no diagnostics, no ENM002) has 51 previews and 24 chiral directions, but 12 orbit-chiral directed geometries have no declared link (all H–H; 36 directed geometries have an orbit-membership inverse with both shells chiral); the preview already links other H–H pairs, so the omission is not a consistent same-site exclusion. This contradicts the Step 05 §5 / Step 08 §4 / `reference/energy.md` rule that both directed centres must see the inverse as distinct and a chiral pair shares one `EnergyId`; Step 08 uses the same sampler for compiled tables and Step 09 stores one value per `EnergyId`. For the BZ cycle the orbit-membership inverse is not one-to-one, so the design also needs an explicit rule for such reduced geometries rather than silently dropping them. | `EnergyGeometrySampler.cs:110-141` (key from `Shell.Representative`; inverse requires exact negation), `:173-188` (both-centre gate); `PairReduction.cs:111,123-149`; `EnergyPreviewTests.cs:148-200` (test only checks pairs already declared chiral); probe results in the command table. The original predicate is ordered-path based for different-site pairs and does not have this representative-equality limitation. | Link the inverse by orbit membership (`EquivalentImages` or the displacement), not representative equality; require every orbit-chiral inverse to be linked, or explicitly revise the Step 05/08/reference contract and reject/warn when the reduced inverse relation is not one-to-one; add P4mm and barium-zirconate regression coverage; re-run Step 05/07 tests and the Step 08 design review before Step 08 consumes the metadata as authoritative. |
| R3-F2 | Minor | Energy regression tests | `PeriodicImagesAtEqualDistanceLinkToTheirTrueInverse` asserts only properties of previews already marked `IsChiral`; it has no expected chiral count or partner identity and does not cover multi-image orbits, so it would pass while R3-F1 loses links. | `EnergyPreviewTests.cs:148-200`; the P4mm and barium-zirconate probe results in R3-F1. | Assert the expected chiral count and partner geometry IDs for the fixture and add a high-symmetry multi-image regression (P4mm or the barium-zirconate H–H pair). |

Carry-forward: **F-01–F-04 Fixed**, **F-05/F-06 Accepted notes** (unchanged), **R2-F1 Fixed for its stated
equal-distance/declared-pair failure mode**, **R2-F2/R2-F3 Fixed**. No round-1 or round-2 finding is reopened;
R3-F1 is a remaining defect in the same path that the P1 fixture does not exercise.

The P4mm fixture carries the same advisory `ENM002` (cutoff above half the smallest cell dimension) as the
R2-F1 P1 fixture. `ENM002` is a warning, not an error, and the Step 05/08 periodic-offset rule is explicitly
meant to cover these images; the barium-zirconate sample, which has no diagnostic, shows the same class of
silent omission.

### Original-design fidelity (round 3)

The three questions remain answered as in round 1, with the R3-F1 exception:

- **Corrections:** F-01–F-04 corrections remain in place; no intended correction was found silently retained.
- **Ported strengths:** exact symmetry/P1 order, centre-stabilizer reduction, stable `GeometryId`/`EnergyId`,
  and the auto-sampled pair preview are present. The chiral-pair linking — a ported original strength — is
  incomplete for multi-image orbits (R3-F1). The original predicate compares ordered-path extensions for different-site pairs rather than
  representatives (it guards on the reference-site pair), so the P4mm case is a regression over the original
  behaviour, not only a new-model gap. The new design does not state a same-site exclusion, and the preview
  already links some same-site H–H pairs in barium zirconate.
- **Regressions:** R3-F1 is an unintended semantics/step-interface regression, not a recorded trade-off. The
  other deliberate regressions (PCG32→ChaCha12, WPF→Avalonia, legacy formats, dynamic-S2 default) remain
  recorded with owners and follow-ups.

### Round 3 checklist

- [x] R2-F1's stated equal-distance P1 failure mode is fixed: declared chiral pairs are reciprocal and share
  one `EnergyId`; achiral pairs keep `EnergyId == GeometryId`.
- [x] R2-F2 and R2-F3 are fixed.
- [x] F-01–F-04 remain fixed; F-05/F-06 remain accepted notes.
- [x] Build, full test suite, targeted tests, format, structure, API, contracts, and generated-artifact checks
  pass at the frozen checkpoint.
- [ ] No Blocker or Major remains: **R3-F1 is Major**.
- [ ] R3-F2's regression-test gap is addressed.
- [ ] The human owner approves the verdict and remediation plan.

### Round 3 verdict

- [ ] Passed
- [ ] Passed with notes
- [x] Fixes required
- [ ] Waived

Follow-up owner: Phase B orchestrator/implementor (R3-F1); test owner (R3-F2).

Follow-up step: Fix the chiral-pair linking on a maintenance branch and re-review in round 4. The fix must
link the inverse by orbit membership, or explicitly revise the Step 05 §5 / Step 08 §4 / physics and reference
contract for reduced geometries whose inverse is not one-to-one (including same-site multi-image pairs), and
add P4mm and barium-zirconate regression coverage. Step 08 must not consume the chiral metadata as
authoritative until R3-F1 is fixed or the limitation is explicitly recorded.

## Round 3 remediation

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| R3-F1 | Fixed | Phase B orchestrator | `84781f8` — the inverse is indexed over every equivalent shell image and only reciprocal orbit-inverse matches are linked |
| R3-F2 | Fixed | Phase B orchestrator | `84781f8` — P4mm and barium-zirconate regression tests assert reciprocal partners and shared `EnergyId` |

The sampler now indexes all `PairShell.EquivalentImages`, so a forward shell finds its inverse even when the
inverse is a non-representative image of the reverse shell. A link is recorded only when each direction is in
the other's orbit-match set; the barium-zirconate H–H cycle therefore keeps 24 reciprocal chiral directions
(12 pairs) and leaves the 12 one-way orbit matches achiral, matching the original reciprocal pairing. The
reciprocal-only rule is stated in Step 05 §5, Step 08 §4, `docs/physics/energies.md`, and
`docs/reference/energy.md`. The P4mm fixture now links its 2 directions (was 0), ceria remains 0 chiral, and
the P1 equal-distance fixture remains 2 reciprocal pairs.

## Round 4 request

| Field | Value |
| --- | --- |
| Target commit | `84781f8a4ea3ef64fedaee59d513e26de604ee2c` (Phase B checkpoint after R3-F1 and R3-F2 remediation) |
| Requested focus | Re-review R3-F1 and R3-F2, the reciprocal-only rule, and the P4mm/barium-zirconate evidence; confirm no Blocker/Major remains; append a new round without altering rounds 1–3 |
| Reviewer | — (fresh independent reviewer; fill name and date) |
| Review date | — |

The orchestrator fixed R3-F1 on the checkpoint branch. The design rule is explicit: a non-reciprocal
orbit-inverse match is not a chiral pair and is left achiral. R3-F2 is covered by the new P4mm and
barium-zirconate tests. No earlier round text was changed.
