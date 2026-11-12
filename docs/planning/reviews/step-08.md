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
| Reviewed commit | `c676716e88bcdd5e8877938e21ad6950cc2986bb` (round 2; round 1 reviewed `36fbba58182b2e769a6d59b3228a60021344248d`) |
| Docs branch | `docs/step-08-design` |
| Review date | 2026-11-12 |
| Reviewer | Independent planning auditor (fresh context; no shared conversation with the planner or implementor) |

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

- [x] Step page design decisions — reviewed; the pipeline, API, diagnostics, and feature breakdown are coherent, but M1/M2/M5 affect the stated derivation, field convention, and sizing claim.
- [ ] Data contracts and format changes — reviewed; the existing `model` signature entry and the new `compiledModel` task conflict (M4), and the signature-scope wording differs from Step 09 (m6).
- [x] Architecture and module boundaries — reviewed; `Mocassin.Compilation` is pure and the documented folder layout follows `code-structure.md`; the internal resolver reference is m1.
- [x] ADRs and open decisions — reviewed; ADR 0010 domain separation is followed and the open-decision rows record the compiler/signature/cache/intent boundaries.
- [ ] Quality strategy and test plan — reviewed; the test families are strong, but the field-factor/dual-DOF tests encode M1/M2 and the cache row does not exercise simulation-transition edits (M3).
- [x] Parity matrix and documentation plan — reviewed; the Step 08 parity rows and the named documentation pages are present.
- [x] Licensing and provenance — reviewed; no new dependency or GPL-derived content is introduced, and the SPDX/PublicAPI task is recorded.
- [x] Post-implementation audit plan — reviewed; the `golden-models-and-closeout` feature records the audit request and closeout work.

## Dependency and interface checks

- [x] Dependencies are `Done` and their contracts are stable — Steps 02–07 are Done and their public preview/validation APIs are implemented; the internal-resolver and path-point details are m1/M6.
- [ ] Inputs, outputs, types, and formats are defined — mostly defined, but the field-factor units (M2), manifest signature identity (M4), and Step 09 `model.Signature` name (m5) are ambiguous.
- [x] Versioning and migration behavior is defined — `model_signature` is additive before the first released `.msl`; `format_version` stays `1`, and `models.signature` remains the Step 09 parameterization signature.
- [ ] No unresolved cross-step interface question remains — M3, M4, M6, m1, m2, m5, and m6 remain.

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable — scope/non-goals are explicit and the acceptance table is testable; the sizing claim is the exception (M5).
- [ ] Design decisions are complete and internally consistent — M1, M2, M3, M4, and M6 are material contradictions; m5/m6 are interface wording.
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined — families and targets are defined, but M2's formula and M1's derivation need tests that do not merely encode the design, and M3's cache test omits simulation-transition edits.
- [x] Documentation, status, parity, and audit updates are planned — the named pages, CMP docs, parity rows, status links, and closeout/audit task are present.
- [x] Licensing, provenance, and dependency checks are resolved — no new dependency, SPDX/PublicAPI tasks recorded, and the embedded catalog provenance is unchanged.
- [x] Risks and mitigations are realistic — the table covers the main failure modes; the missing risks for M1–M3 are captured by those findings rather than by a separate risk entry.
- [x] Code layout and namespace plan follow the structure policy — the `Compilation/{Compiler,Context,Structure,...}` layout is domain-based and within the documented limits.
- [ ] No hidden scope, stale placeholders, or unowned decisions — the `compiledModel` manifest task (M4), the benchmark project reference (n1), and the split decision (M5) are unowned/stale.

## Step 08-specific checks

- [x] `IModelCompiler`, `CompilationResult`, `CompilationOptions`, cancellation, parallelism, and the
      synchronous/async boundary are defined; exception policy separates user data from programmer errors. —
      API/exception policy is coherent and synchronous; the cancellation granularity is m4.
- [x] The compiler's validation boundary is explicit: it assumes a validated `Project`, does not consume
      `ValidationResult` or duplicate `ProjectValidationPipeline`, and the `IValidationStage`/`Build` owner is
      identified. — Confirmed: Step 12 owns the Build adapter and `mocassin validate` does not compile.
- [ ] Stages are reconciled with the actual Step 02/04/05 APIs: P1 expansion and encoders, radial search and
      periodic images, filters, `G0` reduction, chirality, cluster permutation canonicalization, transition
      path resolution, state codes, mass/charge conservation, and forward/inverse/dependent rules. — Mostly
      reconciled, but m1 (internal resolver), m2 (void-last preview order), m3 (Burnside/bound), and M6
      (point-level path resolution) remain.
- [ ] `CompiledModel` is reconciled with `02-data-contracts.md` §3.3 and the Step 11 `.msl` writer; persisted,
      JSON-inspection-only, and composed-later data are separated. — The persisted/inspection/composed split
      matches §3.3, but m5 (`model.Signature` vs `ModelSignature`) and m6 (signature scope) need reconciliation.
- [ ] Dense indexing, stable geometry/rule IDs, model signature, the Step 09 parameterization-signature
      boundary, and byte-identical determinism are defined. — Index rules and deterministic ordering are
      defined; M4 (manifest identity) and m6 (active-collection scope) remain.
- [ ] The derived solver intent (`UseDualDofCorrection`, `UsePrerun`, kind) is defined and Step 11 owns the
      `.msl` flag word. — `UsePrerun`/kind and the composition boundary are correct; M1 is a material divergence
      for `UseDualDofCorrection`.
- [x] Tracker mappings emitted by Step 08 and mutable tracker state owned by Step 18 are separated. —
      Confirmed: global/static definitions and `tracker_order` are Step 08; Step 18 owns values, mobile
      assignment, and `.mcs` output.
- [x] New compile diagnostic codes, their owner prefix, their `DiagnosticCatalog` composition, and their
      reference-docs path are defined. — `CMP001`–`CMP012`, `CompilationDiagnosticCatalog`, the composed
      catalog, and the docs-drift test are defined; m7, m8, and n2 are report/path/integrity details.
- [x] The `mocassin compile` ownership question is resolved explicitly (Step 08 minimal command vs library/test
      harness; Step 12 full CLI). — Confirmed: Step 08 library plus pure `CompiledModelJson`; Step 12 owns the
      command and file output.
- [ ] Cache keys, invalidation graph, lifetime, thread safety, and tests are defined. — Lifetime/thread safety
      are defined, but the simulation-transition invalidation path and missing simulation hash are M3.
- [x] Diagnostics/report fields, stage counts/timings, and realistic performance targets are defined. — Report
      fields and targets are concrete; m7 notes the missing by-reason rejection breakdown.
- [ ] The feature list has roughly 4–8 auditable features with the required branch names, all `Not started`;
      the sizing estimate and split trigger are recorded. — Eight required branches are present and all are
      `Not started`; 4–6 pw/eight features exceed the recorded split trigger (M5).
- [x] No production compiler code, step branch, or pre-seeded review verdict exists. — Confirmed:
      `Mocassin.Compilation` contains only `CompilationModule.cs`, the test project only has `SmokeTests.cs`,
      no `step/08...` branch exists, and this record contains no verdict before this round.

## Evidence

**Review round 1** — frozen design commit `36fbba58182b2e769a6d59b3228a60021344248d`; review-request/current
tip `91d74208a92de7eab1000d25de49c616c521b6ed`. I am a fresh context: I did not author or implement this
design, did not participate in its preparation, and used no handoff, summary, or prior conversation. The
worktree `/workspace/playground/step08-design` was clean at `91d7420`; all evidence is from repository
inspection at that state and from the frozen design content at `36fbba5`.

Files reviewed:

- `AGENTS.md` §3.8–§3.9, §3.15, §7.2, §8, §11, §13, §16; `docs/planning/reviews/README.md`; the review
  template; the Step 08 review record and Step 08 page.
- Steps 02–07 pages, especially Step 02 §4–§7 (P1, encoder, `G0`/`Gset`/`Gseq`, pair/cluster reduction,
  periodic images), Step 04 §2–§7 (state changes, chains, `GeneratesReverse`, path resolver), Step 05 §5–§7
  and §10 (`IEnergyGeometrySampler`, pair/cluster tables, `EnergyId`, preview limits), Step 06 §1–§7
  (simulations, `ExecutionFlags`, `SolverJobFlags`), and Step 07 §2–§7 (validation stages, Build boundary,
  statistics). Also Steps 09–12 and 14–18 for the signature, package, build, energy, KMC, and tracker
  boundaries.
- `docs/planning/reviews/step-07.md` (accepted standard and finding history).
- `docs/planning/02-data-contracts.md` §1.3, §2.3, §3.1–§3.3, §3.6, §5.1, §5.6, §6; `docs/development/architecture.md`,
  `design-decisions.md`, `compilation-pipeline.md`, `code-structure.md`, `testing-and-parity.md`;
  `docs/planning/03-quality-strategy.md` §9.3 and §13; `docs/planning/04-roadmap.md` §3.1;
  `docs/planning/05-open-decisions.md`; `docs/planning/appendix-b-parity-matrix.md`;
  `docs/planning/status.md`; `docs/reference/diagnostics.md`, `docs/reference/cli.md`.
- ADRs 0009, 0010, 0011, 0013, 0014; `NOTICE.md`; the SPDX/licence rules.
- Implemented APIs at the frozen commit: `Mocassin.Compilation/CompilationModule.cs` and both `PublicAPI` files;
  `Mocassin.Symmetry/Energy/{IEnergyGeometrySampler,EnergyGeometrySampler,EnergyPreviewService}.cs`,
  `Reduction/{PairReduction,ClusterReduction,PointGroup}.cs`, `Transitions/{TransitionPathResolver,TransitionPreviewService}.cs`,
  `Structure/{SpaceGroupReferenceResolver,StructurePreviewService}.cs`, `Geometry/{UnitCellVectorEncoder,PeriodicGeometry}.cs`,
  `Contracts/SymmetryVersionInfo.cs`, `Diagnostics/*`, `AssemblyInfo.cs`;
  `Mocassin.Core/Project/{Project,ProjectHasher,ProjectCanonicalizer}.cs`, `Diagnostics/{Diagnostic,DiagnosticBag,ModelPath,DiagnosticDescriptor}.cs`,
  `Model/Particles/Particle.cs`, `Model/Transitions/*`, `Model/Simulations/*`;
  `Mocassin.Packages/Projects/Validation/*` (`DiagnosticCatalog`, `IValidationStage`, `ValidationStage`,
  `ProjectValidationPipeline`), `Mocassin.Packages/Jobs/SolverJobFlagsMapper.cs`;
  `Mocassin.Contracts/Solver/SolverJobFlags.cs`; `Mocassin.Compilation.Tests/SmokeTests.cs`;
  `Mocassin.Packages.Tests/Projects/Validation/*`; `contracts/manifest.json`, `tools/contracts/generate.py`,
  `contracts/generated/signatures/model-v1.json`, and `contracts/generated/fixtures/descriptors.json`.
- Read-only original reference used only to check behaviour: `JobDbModelBuilder.SetSimulationJobInfoFlags`
  (dual-DOF), `JumpSelectionOptimizer`/`RunPostBuildOptimizers`, and the solver `INFO_FLG_DUALDOF` comment.

Commands run (from `/workspace/playground/step08-design`):

```sh
git status --short
# clean

python tools/check_process_consistency.py
# Process consistency: OK

python tools/check_docs_links.py
# Documentation links OK (149 files scanned).

zensical build --strict
# Build started
# No issues found
# Build finished in 0.30s

git diff --check
# no output; exit 0

python tools/contracts/generate.py --check
# Contract artifacts are current (18 files)

git diff --stat 36fbba5..HEAD
# pre-record-commit request state (the record commit below adds this review text):
# docs/planning/reviews/step-08.md                   | 187 +++++++++++++++++++++
# docs/planning/status.md                            |   8 +-
# docs/planning/steps/step-08-model-compilation-pipeline.md | 2 +-
# 3 files changed, 195 insertions(+), 2 deletions(-)

git diff --name-only 36fbba5..HEAD
# docs/planning/reviews/step-08.md
# docs/planning/status.md
# docs/planning/steps/step-08-model-compilation-pipeline.md

git branch -a
# * docs/step-08-design
# + main
#   remotes/origin/main

find src/dotnet/Mocassin.Compilation -type f -name '*.cs' -not -path '*/obj/*'
# src/dotnet/Mocassin.Compilation/CompilationModule.cs

find src/dotnet/Mocassin.Compilation.Tests -type f -name '*.cs' -not -path '*/obj/*'
# src/dotnet/Mocassin.Compilation.Tests/SmokeTests.cs
```

Targeted read-only checks:

```sh
grep -n "compiledModel\|signatures\|REQUIRED_SIGNATURES" tools/contracts/generate.py contracts/manifest.json
# `REQUIRED_SIGNATURES = {"model", "parameterization", "lattice"}`; the manifest already has `signatures.model`
# with the same `mocassin/model-signature/v1` prefix and a `model-v1` compiled-model fixture for Step 08.

grep -n "GetParticleOrder" src/dotnet/Mocassin.Symmetry/Transitions/TransitionPreviewService.cs
# lines 408-416: authored particles ascending, then `project.VoidParticle.Id` — void is last, not index 0.

grep -n "TryResolve\|FindMatches\|TryGetComponentShift" src/dotnet/Mocassin.Symmetry/Transitions/TransitionPathResolver.cs
# public `TryResolve(KineticTransition, ...)`; `FindMatches`/`TryGetComponentShift` are private.

grep -n "internal static class SpaceGroupReferenceResolver" src/dotnet/Mocassin.Symmetry/Structure/SpaceGroupReferenceResolver.cs
# line 16: internal; AssemblyInfo exposes only Mocassin.Symmetry.Tests to internals.

grep -n "MaxTupleCount\|InvalidOperationException" src/dotnet/Mocassin.Symmetry/Reduction/ClusterReduction.cs
# fixed `MaxTupleCount = 1_000_000`; `Reduce` always enumerates and has no Burnside or configurable bound.

grep -n "CancellationToken" src/dotnet/Mocassin.Symmetry/Energy/IEnergyGeometrySampler.cs \
  src/dotnet/Mocassin.Symmetry/Reduction/*.cs src/dotnet/Mocassin.Symmetry/Transitions/TransitionPathResolver.cs
# no matches: the sampler/reducers/path resolver are atomic calls without a token.

grep -n "SetSimulationJobInfoFlags\|JobFlags |= dofFlag\|UseDualDofCorrection" \
  /workspace/mocassin-original/src/ModelBuilder/ICon.Model.Translator/Database/EntityBuilder/Main/JobDbModelBuilder.cs \
  /workspace/mocassin-original/src/ModelBuilder/ICon.Model.Translator/Database/Optimization/JumpSelectionOptimizer.cs
# original sets `UseDualDofCorrection` for every KMC job; the optimizer may clear it.

git diff 36fbba5..HEAD -- docs/planning/status.md docs/planning/steps/step-08-model-compilation-pipeline.md
# status/step-page review-request links only; no design change after the freeze.
```

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M1 — `UseDualDofCorrection` is redefined without reconciling the original solver contract or recording a correction.** The design sets it only for KMC collections whose chain generates an automatic inverse (`Migration3`) and explicitly returns `false` for vehicle/complex mechanisms. The original job builder sets `UseDualDofCorrection` for every KMC job; the selection optimizer can clear it after mask optimization, and the solver comment defines it as "non-optimized with twice the actually existing degrees of freedom". Step 16 uses `dof = 2` when the flag is set, so the design changes the KMC time step for KMC mechanisms that are not `Migration3`. There is no rationale, reference, or test that distinguishes this from an accidental correction. | Step 08 page §9 lines 414–420 and acceptance lines 716–718; `docs/planning/steps/step-08-model-compilation-pipeline.md`; Step 16 line 153; original `JobDbModelBuilder.cs` lines 236–250; original `JumpSelectionOptimizer.cs` lines 38–45; solver `Constants.h` line 40. |
| **Major** | **M2 — The field-factor formula is dimensionally and mathematically inconsistent and cannot produce the stated single-charge result.** The page defines `pathVector = Σ_i movement_i` as Cartesian, but `movement_i` is defined as a fractional displacement. `pathUnit` is then dotted with `movement_i`; if `movement_i` is the fractional displacement, the result is not the claimed `q` for a single charge (the dot product is `|Δx|`), and mixing fractional movement with a Cartesian unit vector is not dimensionally valid. `rule.field_factor` is multiplied by `direction.field_projection` in Step 15, so the resulting field term is wrong or unit-dependent. The single-charge, vacancy, and collinear cases cannot validate the written formula as stated. | Step 08 page §8.1 lines 310–312 and §8.4 lines 361–376; Step 15 lines 100–112. |
| **Major** | **M3 — The cache invalidation graph reuses `Rules` after a simulation edit even though rules are generated only for transitions referenced by simulations.** The `Rules` stage processes "each kinetic or Metropolis transition referenced by at least one simulation"; the rules cache key is only "transition closure hashes + structure hash + particle order" and has no simulation/active-transition-set component. Adding a simulation that references a previously unused transition, or removing the last reference to one, therefore changes the required rule set while the graph says `Simulations/settings` rebuilds only `Encoding, Indexing, Signing` and reuses `Rules`. The result can be stale or missing rules, and `CMP009`/`CMP006` behaviour can change without a cache miss. There is also no named simulation-hash producer in `ProjectHasher`. | Step 08 page §4 line 299, §12 lines 583–595; `ProjectHasher` has `Compute`, `ComputeStructure`, `ComputeEnergy`, and `ComputeTransition`, but no simulation hash; `CMP009` rule. |
| **Major** | **M4 — The `compiledModel` manifest task conflicts with the existing `model` signature entry and the contract generator's exact required set.** The task says to add a `compiledModel` signed-projection entry. `contracts/manifest.json` already defines `signatures.model` with the same `mocassin/model-signature/v1` domain prefix, `ModelSignatureV1` projection type, a note that Step 08 finalizes its field-level types, and a `model-v1` compiled-model fixture for Step 08. `tools/contracts/generate.py` requires `signatures` to be exactly `{model, parameterization, lattice}` and emits `contracts/generated/signatures/model-v1.json`; adding a second entry would fail `--check` and duplicate the projection identity. | Step 08 page §11.3 line 528 and task line 705; `contracts/manifest.json` lines 1904–1912 and 2055–2061; `tools/contracts/generate.py` lines 30 and 222–224; `contracts/generated/signatures/model-v1.json` lines 1–10. |
| **Major** | **M5 — The step-sizing statement contradicts the roadmap split trigger.** The page records an eight-row feature list and a 4–6 person-week estimate but says this is "below the split trigger". `04-roadmap.md` §3.1 says to split before implementation when a step has more than roughly 6–8 features or is expected to take more than about four weeks for one implementor. The page's own internal "stop after 2.5 person-weeks for the first three features" trigger does not change the full-step estimate, and no owner waiver or split rationale is recorded. | Step 08 page §1 lines 85–89; `docs/planning/04-roadmap.md` §3.1 lines 93–105. |
| **Major** | **M6 — Direction expansion cannot use the named public resolver for the required transformed-point resolution.** The design says `Rules` uses the public `TransitionPathResolver`, does not duplicate the validator's path rules, and re-resolves every transformed path point to a canonical `(P, offset)`. The only public resolver operation is `TryResolve(KineticTransition, ...)`; `FindMatches` and `TryGetComponentShift` are private, and `TransitionPreviewService.ExpandPath` only transforms and trims. The compiler would have to duplicate the nearest-image/tie rule or construct synthetic transitions (which reject repeated positions), and neither approach is specified or tested. | Step 08 page §4 lines 212–213 and §8.1 lines 301–307; `TransitionPathResolver.cs` lines 88–100 and 165–182; `TransitionPreviewService.cs` lines 441–461. |
| **Minor** | **m1 — `SpaceGroupReferenceResolver` is internal to `Mocassin.Symmetry`, so `Mocassin.Compilation` cannot call it as written.** The structure stage names it directly. The public alternative is `StructurePreviewService.TryResolve` (or `SpaceGroupCatalog.GetSetting` plus a diagnostic wrapper), but the design and task list do not name it; `InternalsVisibleTo` currently covers only `Mocassin.Symmetry.Tests`. | Step 08 page §4 line 206; `SpaceGroupReferenceResolver.cs` line 16; `Mocassin.Symmetry/AssemblyInfo.cs` line 6; `Mocassin.Compilation.csproj` references only Core and Symmetry. |
| **Minor** | **m2 — The particle-index claim does not match `TransitionPreviewService.GetParticleOrder`.** The page says index 0 is void and that this "matches" `GetParticleOrder`, but the implemented order is authored particles ascending followed by `project.VoidParticle.Id` (void last). The compiled 0=void order is a deliberate difference from the preview/state-change matrix order, not a match; the mapping between `ParticleId` and compiled index must be stated and tested so state codes and Step 09/11 consumers cannot diverge. | Step 08 page §5 lines 220–222; `TransitionPreviewService.cs` lines 408–416; `ProjectHasher.cs` lines 131–134; Step 04 `StateChangeMatrices` particle-order rule. |
| **Minor** | **m3 — `ClusterReduction` has no Burnside output and no configurable tuple bound, so the cluster-limit wording is not implementable as written.** The page says not to enumerate the full product when Burnside/reducer output is available and to honor `MaxClusterTupleCount`. The implemented reducer always enumerates, computes no Burnside count, and throws only at its fixed `MaxTupleCount = 1_000_000`; the preview's `BurnsideCount` is private to `EnergyPreviewService`. The compiler can pre-estimate or catch the fixed exception, but cannot apply a lower configured threshold before enumeration. | Step 08 page §7 lines 278–280; `ClusterReduction.cs` lines 60 and 165–182; `EnergyPreviewService.cs` lines 337–353; `CompilationOptions.MaxClusterTupleCount`. |
| **Minor** | **m4 — The cancellation granularity is finer than the Step 02/04/05 APIs allow.** The page promises checks every 4096 inner enumeration steps and responsiveness within 250 ms. `IEnergyGeometrySampler.TrySamplePairs`, `PairReduction.Reduce`, `ClusterReduction.Reduce`, and `TransitionPathResolver.TryResolve` take no `CancellationToken`; a single large reduction or radial search is an atomic call. The design should scope cancellation to stage/top-level boundaries and around those calls, or add tokens to the Symmetry APIs. | Step 08 page §13.3 lines 652–661 and performance line 669; `IEnergyGeometrySampler.cs` lines 28–34; reduction/path resolver signatures. |
| **Minor** | **m5 — Step 09's signature comparison uses `model.Signature`, but the `CompiledModel` contract defines `ModelSignature`.** `ParameterizationService.Apply(CompiledModel model, ...)` says it verifies `template.ModelSignature == model.Signature`; the Step 08 record has `CompiledModel.ModelSignature` and no `Signature` member. The design should align the Step 09 dependency page or add the alias explicitly before implementation. | Step 08 page §10 lines 426–438 and §11.3 lines 513–519; Step 09 page §4 lines 108–115. |
| **Minor** | **m6 — The model-signature scope is described differently in the Step 08 design, Step 09, and the data contract.** The design includes "active-collection selection" and simulation encodings in the Step 08 projection; Step 09 calls the value "geometry/rules only"; data contract §3.3 describes geometry/rules/encoding and excludes runtime settings. It is not stated whether adding/removing a simulation (without changing geometry or rules) invalidates parameterization templates. Clarify the projection boundary and the template-invalidation rule. | Step 08 page §11.3 lines 521–525 and layout lines 533–548; Step 09 page lines 65–67; `02-data-contracts.md` lines 588–592 and 868–877. |
| **Minor** | **m7 — `CMP005` promises rejection counts by reason in the report, but the report shape only has a total rejected count.** The rule says `CMP005` includes "the rejection counts by reason in the message and the report"; `CompilationReport.Counts` lists "rejected rules" but no mass/charge/invalid-state breakdown. Add the breakdown or remove the report claim. | Step 08 page §8.2 line 329 and §13.1 lines 631–632; report contract lines 640–644. |
| **Minor** | **m8 — `CMP009` uses `transition` as its path domain, but the existing model paths distinguish `kineticTransition` and `metropolisTransition`.** The diagnostic table's path column and the per-code path test need the concrete domain for each transition kind; otherwise the docs-drift/path test cannot assert a stable `ModelPath`. | Step 08 page §13.1 line 616; existing transition paths in `TransitionPathResolver.cs` line 223 and `TransitionPathValidator`. |
| **Note** | **n1 — The benchmark task has no project-reference task.** The performance section says to run `Mocassin.Benchmarks/Compilation`, but `Mocassin.Benchmarks.csproj` currently references only Core and Symmetry and has no Compilation folder. Add the reference/feature task or record a different harness. | Step 08 page §13.4 line 670; `src/dotnet/Mocassin.Benchmarks/Mocassin.Benchmarks.csproj`. |
| **Note** | **n2 — The `CMP012` cache-integrity check has no defined integrity predicate.** The design says a cached stage that fails a "defensive integrity check" is recomputed, but does not say what is checked (shape, index bounds, hash, version) or how the test injects a bad entry. This is a future test/hardening detail, not a start blocker. | Step 08 page §12 lines 597–598; `ICompilationCache` API lines 134–141. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Re-review required

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner or implementor.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

**Verdict: Changes required.** No Blocker was found, but six Major findings remain: the `UseDualDofCorrection`
derivation (M1), the field-factor formula (M2), the simulation-transition cache invalidation gap (M3), the
`compiledModel` manifest conflict (M4), the step-sizing/split-trigger contradiction (M5), and the missing
public point-level path-resolution boundary (M6). Eight Minor findings and two Notes are recorded above. The
core architecture is sound: a pure synchronous staged compiler over the implemented Step 02/04/05/06 services,
an explicit Step 07 validation boundary, a separate compiled-model signature, a Step 18 tracker split, and a
documented `CMP` family fit the repository. The required repository checks pass. The design must be revised and
a fresh review round must verify the fixes before implementation starts. This record is evidence and does not
authorize the step.

Follow-up owner: Step agent / planner
Follow-up step: Step 08 design revision and fresh design review (round 2)

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

Round 1 findings were remediated on `docs/step-08-design` at
`c676716e88bcdd5e8877938e21ad6950cc2986bb`. M5 is resolved by an explicit human-owner waiver of the roadmap
§3.1 split trigger, recorded on the Step 08 page and in `status.md`; it is not a silent acceptance. A fresh
round 2 reviewer must re-check the changed design and the dependency pages (Steps 04, 09, 10, 11).

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| M1 | Resolved — `UseDualDofCorrection = Kind == Kmc` default matches the original job builder; Step 10 owns any documented optimizer override | Planner | `c676716` |
| M2 | Resolved — fractional movements converted to Cartesian; `fieldFactor = Σ q_i (cartesianMovement_i · pathVector)/|pathVector|²` reduces to `q` for one charge | Planner | `c676716` |
| M3 | Resolved — `ProjectHasher.ComputeSimulationSelection` added to the Rules key; invalidation graph distinguishes active-set and runtime-only simulation edits | Planner | `c676716` |
| M4 | Resolved — Step 08 finalizes the existing `signatures.model`/`ModelSignatureV1` entry; no second `compiledModel` entry | Planner | `c676716` |
| M5 | Waived by human owner on 2026-11-12 — 4–6 pw/eight features accepted as one step; waiver recorded on the Step 08 page, status, and this record | Human owner | `c676716` |
| M6 | Resolved — additive public `TransitionPathResolver.TryResolvePoint`/`TryResolvePath`; Step 04 page updated; no private nearest-image duplication | Planner | `c676716` |
| m1 | Resolved — use public `IStructurePreviewService.TryResolve`, not internal `SpaceGroupReferenceResolver` | Planner | `c676716` |
| m2 | Resolved — compiled 0=void order is explicitly not preview matrix order; particle-ID mapping and test added | Planner | `c676716` |
| m3 | Resolved — pre-check raw cluster count with saturating `long`, honor the reducer's fixed 1,000,000 ceiling, catch bounded failure as `CMP006` | Planner | `c676716` |
| m4 | Resolved — cancellation is stage/top-level/atomic-call scoped; the 250 ms target is qualified as between checks | Planner | `c676716` |
| m5 | Resolved — Step 09 now uses `model.ModelSignature`; Step 09 page updated | Planner | `c676716` |
| m6 | Resolved — signature scope explicitly includes simulation encoding and excludes runtime settings; Step 09 and data contract aligned | Planner | `c676716` |
| m7 | Resolved — report counts split `RejectedByMass`, `RejectedByCharge`, `RejectedByInvalidState` | Planner | `c676716` |
| m8 | Resolved — `CMP009` path domain is `kineticTransition` or `metropolisTransition` by kind | Planner | `c676716` |
| n1 | Addressed — benchmark/reference task added to the golden/closeout feature | Planner | `c676716` |
| n2 | Addressed — cache-integrity predicate and injected-entry test defined | Planner | `c676716` |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

I am a fresh context. I did not author or implement this design, did not participate in its preparation or any
discussion of its decisions, and used no handoff, summary, or prior conversation. I read the frozen repository
state at `36fbba58182b2e769a6d59b3228a60021344248d` and the request/current tip
`91d74208a92de7eab1000d25de49c616c521b6ed` directly.

The frozen-state check passes: `git diff --stat 36fbba5..HEAD` contains only
`docs/planning/reviews/step-08.md` (the review request record), `docs/planning/status.md` (the review-status
link), and `docs/planning/steps/step-08-model-compilation-pipeline.md` (the review link). No design, contract,
code, test, configuration, step-branch, or implementation commit was made after the freeze. All eight feature
rows are `Not started` with the required `feature/step-08-model-compilation-pipeline/<feature>` names, no
`step/08-model-compilation-pipeline` branch exists, and the compiler/test projects contain only the
`CompilationModule` marker and the smoke test.

#### Commands and results

All commands were run in `/workspace/playground/step08-design`:

```sh
git status --short
# clean

python tools/check_process_consistency.py
# Process consistency: OK

python tools/check_docs_links.py
# Documentation links OK (149 files scanned).

zensical build --strict
# Build started
# No issues found
# Build finished in 0.30s

git diff --check
# no output; exit 0

python tools/contracts/generate.py --check
# Contract artifacts are current (18 files)
```

The strict documentation build, process check, link check, whitespace check, and contract-freshness check all
pass. The manifest conflict in M4 is a design/tooling inconsistency, not a current artifact failure, because
the design has not yet added the conflicting entry. The exact `git diff --stat`, branch, and file-list evidence
is recorded in `Evidence` above.

#### Findings

Round 1 records six Major findings, eight Minor findings, and two Notes in the findings table:

- **Major:** M1 (`UseDualDofCorrection` divergence), M2 (field-factor formula/units), M3 (simulation-transition
  cache invalidation), M4 (`compiledModel` vs existing `model` manifest/signature), M5 (sizing vs split
  trigger), M6 (public point-level path resolution).
- **Minor:** m1 (internal `SpaceGroupReferenceResolver`), m2 (void-last preview particle order), m3
  (cluster/Burnside bound), m4 (cancellation API granularity), m5 (`model.Signature` naming in Step 09), m6
  (model-signature scope), m7 (`CMP005` report breakdown), m8 (`CMP009` path domain).
- **Notes:** n1 (benchmark project reference), n2 (`CMP012` integrity predicate).

No Blocker was found. The core architecture, dependency direction, validation boundary, indexing plan, tracker
split, `CMP` catalog plan, and documentation/parity/audit plan are sound; the Major findings are specific
correctness and contract contradictions that must be resolved before implementation.

#### Verdict

**Changes required.** The design is not `Ready for implementation` or `Ready with notes`: six Major findings
remain, including a likely KMC time-step change, a central field-term formula error, a cache invalidation gap,
a contract-tooling conflict, a sizing-gate contradiction, and an unimplementable path-resolution boundary. A
fresh design review (round 2) is required after the planning agent and step agent update the design docs and
add resolutions. This record does not itself authorize starting Step 08.
