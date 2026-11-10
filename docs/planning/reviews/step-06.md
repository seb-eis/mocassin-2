# Step 06 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-06.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Design
> review status`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 06 — Lattice and Simulation Models |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Design review status | Changes required |
| Reviewed commit | `a64ecf3` |
| Docs branch | `main` |
| Review date | 2026-11-10 |
| Reviewer | Independent planning auditor (fresh context; no shared conversation with the planner or implementor) |

Step page: [`Step 06 — Lattice and Simulation Models`](../steps/step-06-lattice-and-simulation-models.md)

## Step summary

| Field | Value |
| --- | --- |
| Phase | B. Model definition |
| Depends on | 03, 04 |
| Delivers | Occupation exchanges, ordered dopings, and MMC/KMC simulation definitions with defaults and validation. |

## Design decisions reviewed

- [x] Step page design decisions — reviewed in full; findings M1–M7 and m1–m7.
- [x] Data contracts and format changes — reviewed `02-data-contracts.md` §2.2 Lattice/doping, Simulations, Transitions; gaps in M1, M7, m1, m4.
- [x] Architecture and module boundaries — reviewed Core/Compilation/Symmetry ownership; gaps in M5, M6, m3.
- [x] ADRs and open decisions — reviewed ADR 0009 and the additive-v1 precedent; reconciliation gap in m4.
- [x] Quality strategy and test plan — reviewed `03-quality-strategy.md` §9.4; test-plan gap in m6.
- [x] Parity matrix and documentation plan — reviewed `appendix-b-parity-matrix.md` §4/§5; planning gaps in m5.
- [x] Licensing and provenance — no new dependency or GPL-derived content; the missing SPDX task is m5.
- [x] Post-implementation audit plan — reviewed `audits/step-06.md`; incomplete step-specific checklist in m5.

## Dependency and interface checks

- [x] Dependencies are `Done` and their contracts are stable — Steps 03 and 04 are Done/tagged; Step 04's authored-order statement is the instability recorded in M7.
- [ ] Inputs, outputs, types, and formats are defined — partial: canonical persistence/hash (M1), simulation settings (M2/M3), preview types (M5/M6), and transition ordering (M7) are unresolved.
- [x] Versioning and migration behavior is defined — additive project-v1; ADR 0009 reconciliation is m4.
- [ ] No unresolved cross-step interface question remains — M4, M5, M6, M7, and m3 remain.

## Standard design-review checklist

- [ ] Scope, non-goals, and acceptance criteria are explicit and testable — there is no non-goals section, and the “applied in ascending order” criterion is Step 10 work (m5).
- [ ] Design decisions are complete and internally consistent — M1–M7, m1, and m7.
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined — m6.
- [ ] Documentation, status, parity, and audit updates are planned — m5.
- [x] Licensing, provenance, and dependency checks are resolved — no new dependency or GPL copying; the missing SPDX task is m5.
- [ ] Risks and mitigations are realistic — the risk table omits canonical data loss (M1), defaults/limits (M2/M3), preview/compiler divergence (M5) and cross-step ordering (M7).
- [ ] Code layout and namespace plan follow the structure policy — m2.
- [ ] No hidden scope, stale placeholders, or unowned decisions — M4 and m2/m5/m7.

## Evidence

### Review round 1

**Reviewer context.** I am a fresh planning auditor. I did not author or implement this design and have no
shared conversation with the planner or implementor. I inspected the repository, the frozen design, the
dependent step pages, and the original implementation as read-only reference myself; I did not rely on the
spawn summary.

**Frozen state and worktree.** Worktree `/workspace/mocassin`, branch `main`. At the start of the review
`HEAD` was `2c7ce02`. The request names `a64ecf3` as the frozen design commit. `git diff
a64ecf3..2c7ce02 -- docs/planning` changes only `docs/planning/reviews/step-06.md` (the request record) and
`docs/planning/status.md` (the freeze/status update), so the reviewed design content is identical at
`a64ecf3` and at the review start commit. `git diff --name-status a64ecf3..2c7ce02 -- docs/planning` shows
`A docs/planning/reviews/step-06.md` and `M docs/planning/status.md`. While the review was being written,
another agent added `56f41b2` and `ea825d2` to `main`; those commits are docs-only cross-cutting review plans
and do not touch the Step 06 design or its data contracts. This is a process note (n1), not a design finding.

**Commands run (from `/workspace/mocassin`):**

```sh
git status --short
# (no output: clean worktree)

git log --oneline --decorate -6
# 2c7ce02 (HEAD -> main, origin/main) docs: freeze step-06 design for review [skip ci]
# 6285472 docs: request step-06 design review [skip ci]
# a64ecf3 docs: prepare step-06 design for review
# e2ed1c6 docs: record step-05 audit human approval [skip ci]
# 99074e2 docs: mark step-05 complete and record merge [skip ci]
# c11a364 (tag: step-05) step-05: energy model

git show --stat a64ecf3
# commit a64ecf3d348ffac4ae882d95908e6b447986401e
# docs: prepare step-06 design for review
#  docs/planning/02-data-contracts.md                 |   7 +-
#  .../steps/step-06-lattice-and-simulation-models.md | 183 +++++++++++++++++++--
#  2 files changed, 179 insertions(+), 11 deletions(-)

git diff --stat a64ecf3..HEAD -- docs/planning
#  docs/planning/reviews/step-06.md | 131 +++++++++++++++++++++++++++++++++++++++
#  docs/planning/status.md          |  19 +++---
#  2 files changed, 138 insertions(+), 12 deletions(-)

git diff --check
# (no output, exit 0)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.26s
# exit 0

python3 tools/contracts/generate.py --check
# Contract artifacts are current (17 files)
# exit 0

./tools/verify.sh docs
# == Documentation ==
# ==> Zensical (strict)
# Build started
# No issues found
# Build finished in 0.26s
# ==> DocFX
# ...
# Build succeeded.
#     0 warning(s)
#     0 error(s)
# ==> rustdoc
# ...
# Documentation built in /workspace/mocassin/site
# exit 0

git status --short
# (no output: build output is ignored)
```

**Additional read-only checks:**

```sh
git show -s --format='%H %P %s' 2c7ce02 6285472 a64ecf3
# 2c7ce02... 99074e2... 6285472... docs: freeze step-06 design for review [skip ci]
# 6285472... a64ecf3... docs: request step-06 design review [skip ci]
# a64ecf3... e2ed1c6... docs: prepare step-06 design for review

find src/dotnet/Mocassin.Core/Project -maxdepth 1 -name 'ProjectJsonContext.cs'
# (no output: there is no standalone file)

grep -n "class ProjectJsonContext" src/dotnet/Mocassin.Core/Project/ProjectJson.cs
# 410:internal sealed partial class ProjectJsonContext : JsonSerializerContext;

grep -n "CanonicalizeModel\|Model = ProjectJson\|public override void Write" \
  src/dotnet/Mocassin.Core/Project/ProjectJson.cs
# 84,93,313,360

grep -n "SerializeCanonicalModel\|Compute(Project" \
  src/dotnet/Mocassin.Core/Project/ProjectHasher.cs
# 26: public static string Compute(Project project)
# 29: var bytes = ProjectJson.SerializeCanonicalModel(project.Model);

python3 - <<'PY'
import json
m=json.load(open('contracts/manifest.json'))
# Searched project model fields:
# occupationExchanges -> MISSING
# dopings -> MISSING
# simulations -> MISSING
# kineticTransitionIds -> MISSING
# metropolisTransitionIds -> MISSING
# executionFlags -> MISSING
PY

grep -n "oneOf\|anyOf\|discriminator" tools/contracts/generate.py
# (no output: the schema generator has no tagged-union support)

grep -n "simulation transition lists\|every other collection\|never re-sorts" \
  docs/planning/steps/step-04-transition-model.md
# 114, 115, 117

grep -n "A simulation's transition set is non-empty\|no transitions" \
  docs/planning/steps/step-07-validation-and-diagnostics.md
# 51, 135

grep -n "same supercell generator\|DopingReport\|JOB004\|JOB014" \
  docs/planning/steps/step-10-job-templates-and-supercells.md \
  docs/planning/steps/step-25-model-editors-energy-lattice-simulations.md
# Step 25 121; Step 10 185, 180, 233

wc -l src/dotnet/Mocassin.Core/Editing/ProjectEditor.cs
# 506 src/dotnet/Mocassin.Core/Editing/ProjectEditor.cs
```

**Original reference inspected (read-only, `/workspace/mocassin-original`).** `SimulationBaseData.cs`,
`KineticSimulationData.cs`, and `MetropolisSimulationData.cs` (UI defaults: job count 12, target MCSP 1000,
temperature 1000, write-out count 100, time limit P1Y, KMC pre-run 100, field 10e6 V/m along (1,0,0),
normalization 0, MMC break tolerance 0, break sample length 1000, interval 100, result sample 200);
`MocassinSimulationSettings.InitAsDefault` (temperature 0.1–10000, target MCSP 1–1,000,000, job count 1–1000,
pre-run -1–1,000,000, transition count 0–100, field, normalization, break and result sample ranges, seed
length); `DopingValidator.cs` (wrong-sign compensation is a warning, not an error); `Doping.cs` (`Priority`
is a simultaneous-doping group ID); and `FastDopedByteLatticeSource.cs` (per-reference-site populations).

**Files reviewed.** `AGENTS.md` §3.8–§3.13 and §8/§10/§11/§12; `docs/planning/reviews/README.md` and
`step-design-review-template.md`; `docs/planning/steps/step-06-lattice-and-simulation-models.md` at
`a64ecf3`; `docs/planning/02-data-contracts.md` (model envelope, lattice/doping, simulations, transitions,
job payload); `docs/planning/steps/step-03-particle-and-structure-models.md`,
`step-04-transition-model.md`, `step-05-energy-model.md`, `step-07-validation-and-diagnostics.md`,
`step-08-model-compilation-pipeline.md`, `step-10-job-templates-and-supercells.md`,
`step-11-simulation-package-and-msl-writer.md`, and
`step-25-model-editors-energy-lattice-simulations.md`; `docs/planning/03-quality-strategy.md`;
`docs/planning/appendix-a-original-inventory.md`; `docs/planning/appendix-b-parity-matrix.md`;
`docs/planning/status.md`; `docs/planning/audits/step-06.md`;
`docs/planning/adr/0009-contract-manifest-and-contracts-library.md`; `contracts/manifest.json`;
`tools/contracts/generate.py`; `contracts/generated/schema/project-v1.schema.json`;
`tests/contracts/` fixtures; `src/dotnet/Mocassin.Core/Model/ModelDefinition.cs`,
`ModelDefinitionJsonConverter.cs`, `Model/Transitions/TransitionRef.cs`, `TransitionKind.cs`,
`Model/Structure/CellSite.cs`, `Model/Particles/ParticleSet.cs`, `Ids/TypedIds.cs`,
`Project/Project.cs`, `Project/IProjectQuery.cs`, `Project/ProjectJson.cs`, `Project/ProjectHasher.cs`,
`Project/ProjectSchema.cs`, `Project/ProjectValidator.cs`, `Project/ProjectMigrations.cs`,
`Editing/ProjectEditor.cs`, `Diagnostics/*`; `src/dotnet/Mocassin.Compilation/Mocassin.Compilation.csproj`
and `CompilationModule.cs`; `src/dotnet/Mocassin.Symmetry/Structure/IStructurePreviewService.cs`,
`Structure/StructurePreview.cs`, `Geometry/UnitCellVectorEncoder.cs`, `Geometry/SiteOrbitInfo.cs`,
`Energy/IEnergyPreviewService.cs`, `Transitions/ITransitionPreviewService.cs`; `docs/user-guide/` and
`docs/reference/project-format.md`; `docs/development/architecture.md`, `code-structure.md`,
`compilation-pipeline.md`; `zensical.toml`; `tools/verify.sh`, `tools/structure-policy.json`, and
`tools/api-projects.txt`.

**Scope checks.** I reviewed all requested areas: model lattice/simulation sections and JSON/schema
round-trip; polymorphic simulation persistence and the kind-specific transition arrays; doping priority
ordering, uniqueness, and deterministic serialization; the derived default building block and
charge-compensation/truncation semantics; SimulationKind/TransitionKind compatibility; simulation defaults
and limits; `ILatticeQuery`, `ISimulationQuery`, `LatticePreview`, `SimulationJobPreview`, and
`ILatticePreviewService`; LAT001–LAT011 and SIM001–SIM011 ownership; Step 03/04 dependencies; manifest/schema
and fixture tasks; PublicAPI/ApiCompat tasks; code layout; documentation/navigation and test plans; and
unresolved cross-step interfaces. The findings record the result.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M1 — The lattice and simulation sections are not wired into the canonical save/hash path.** The design names `ModelDefinitionJsonConverter`, `ProjectJsonContext`, `ProjectSchema`, and `ProjectValidator` and defines serialization order, but never mentions `ProjectJson.CanonicalizeModel`, `ProjectJsonConverter.Write`, `ProjectStore.SaveAsync`, or `ProjectHasher.Compute`. `CanonicalizeModel` currently rebuilds a `ModelDefinition` containing only the Step 03–05 arrays; `ProjectJsonConverter.Write` replaces the model with that projection and `ProjectHasher.Compute` hashes it. Unless the new sections are added there, they are dropped on every save/serialize and absent from the model/document hashes, so the round-trip acceptance cannot pass. Step 04 and Step 05 make this an explicit contract requirement; Step 06 has no equivalent text or task. | Step 06 page 66–80 and 290–343; `src/dotnet/Mocassin.Core/Project/ProjectJson.cs` 93–133 and 360; `ProjectHasher.cs` 26–29; Step 04 page 101–104; Step 05 page 92–102. |
| **Major** | **M2 — Original simulation settings and defaults are incomplete or contradicted.** The step page's own original-reference list includes a write-out count (line 39), but the `Simulation` record (129–141), the persisted settings example (data contract 443–472), and the Step 11 job payload have no write-out count. The documented defaults (176–181) also disagree with the original user-facing defaults: the original defaults are job count 12, target MCSP 1000, temperature 1000 K, write-out count 100, time limit P1Y, KMC pre-run 100, field 1e7 V/m along (1,0,0), normalization 0, MMC break tolerance 0, break sample length 1000, interval 100, result sample 200; the design says result sample 100 and “sample length/interval derived from MCSP”, and it omits the write-out, time-limit, field, and normalization defaults. The original reference also says empty seeds become a GUID (line 46), while the design derives them from the project/job seed (line 172); the parity appendix says derived, so line 46 is stale. Acceptance “All original simulation settings are represented and validated” (line 369) therefore fails. | Step 06 page 39–46, 129–141, 172, 176–181, 369; `02-data-contracts.md` 443–472; Step 11 page 127; original `Mocassin.UI.Xml/SimulationModel/SimulationBaseData.cs`, `KineticSimulationData.cs`, `MetropolisSimulationData.cs`; `appendix-a-original-inventory.md` 113. |
| **Major** | **M3 — The simulation limits and validation table cover only a fraction of the settings they claim to validate.** The table (195–218) validates temperature, target MCSP, job count, field direction, and zero pre-run only. The original settings define ranges and warning bands for field modulus, normalization energy, pre-run (min -1), write-out count, break tolerance, break sample length/interval, result sample, lower success rate, transition count, and seed length; none of the MMC ranges and none of the field/normalization/success-rate/seed limits has a Step 06 code or default. `SIM008` says pre-run zero is invalid “unless explicitly disabled”, but `PreRunMcsp` is a non-nullable `long` with no disable flag or sentinel, so the exception is not representable. This leaves acceptance line 369 unimplementable as written. | Step 06 page 164–173 and 195–218; original `MocassinSimulationSettings.InitAsDefault` (`BreakTolerance`, `BreakSampleLength`, `BreakSampleInterval`, `ResultSampleLength`, `ElectricField`, `Normalization`, `PreRunMcsp`, `WriteCallCount`, `TerminationSuccessRate`, `TransitionCount`, `Seeding`); `03-quality-strategy.md` §9.4. |
| **Major** | **M4 — LAT010, LAT011, SIM010, and SIM011 use inputs and semantics that Step 06 does not own and that conflict with Step 10.** `Doping` has no fraction or supercell; fractions and supercells are Step 10 job-template data. LAT010 (“cannot be realized exactly in common supercells”) and LAT011 (“incompatible order”) therefore cannot be evaluated by the Core lattice validator. Step 10 owns the same conditions as JOB010/JOB011 and its algorithm explicitly permits a same-site sequence when the source matches, otherwise applying zero with a diagnostic (Step 10 154–170); making LAT011 an error would reject models Step 10 accepts. SIM010 needs supercell dimensions and makes >500,000 a warning, while Step 10 JOB004 makes >500,000 an error and JOB014 warns above 100,000. SIM011 (“very large”) has no threshold and overlaps JOB013. Step 25 says the preview uses the Step 10 generator, so a second Step 06 implementation is the divergence risk Step 25 already names. | Step 06 page 93–124 and 206–207, 217–218; Step 10 page 44–45, 154–170, 180, 229–233; Step 25 page 112–121 and 228; `02-data-contracts.md` 416–441. |
| **Major** | **M5 — The lattice preview is placed in Step 06 but depends on Step 10's truncation/compensation algorithm and on Symmetry, both of which the design excludes.** The preview promises requested vs actual replaced counts, dependent counts and ratios, remaining particles, total charge, and affected sites (119–124). Actual counts require the Step 10 floor/reduction algorithm and the P1 site multiplicity for A/B/C; Step 10 defines that algorithm (154–170), and `Mocassin.Symmetry` owns P1 expansion and orbit multiplicity. The design instead says `ILatticePreviewService` is in `Mocassin.Compilation` and “does not ... use Symmetry” (282), although `Mocassin.Compilation` already references `Mocassin.Symmetry`. Step 25 explicitly says the preview uses the same supercell generator as Step 10 and lists divergence as a risk. Either the preview belongs to Step 10 or Step 06 must define the exact shared algorithm and consume Symmetry; the current split cannot produce an accurate preview. | Step 06 page 119–124 and 266–283; Step 10 page 154–170; Step 25 page 112–121 and 228; `src/dotnet/Mocassin.Symmetry/Structure/StructurePreview.cs`, `Geometry/UnitCellVectorEncoder.cs`, `Geometry/SiteOrbitInfo.cs`; `src/dotnet/Mocassin.Compilation/Mocassin.Compilation.csproj`. |
| **Major** | **M6 — The preview public API cannot express the preview the step page and Step 25 require.** `LatticePreview` carries only an array of `DopingPreview`, a total `RemainingParticleCount`, and `ChargeNeutral` (256–259); it has no per-site remaining counts and no total charge before/after, both required by Step 06 122–124 and Step 25 118–119. `CreatePreview` returns a non-null record with no diagnostics, while the Step 03/04/05 previews use `TryCreatePreview(..., out diagnostics)`, return null for invalid user data, and never throw; there is no result for unknown doping IDs, fractions outside [0,1], or invalid A/B/C. The fraction input is a tuple, while Step 10 defines `DopingValue(DopingId, double)`; `DopingPreview` also duplicates Step 10's `DopingApplication`/`DopingReport`. `CreateJobPreview` takes one simulation and an override but reports `TotalJobs`, which depends on Step 10 collections/configs, not one simulation. The preview also does not echo A/B/C as `StructurePreview` does. | Step 06 page 247–283; Step 25 page 112–121; Step 10 page 74–77 and 185–200; `src/dotnet/Mocassin.Symmetry/Structure/IStructurePreviewService.cs`; Step 03 page 246–275. |
| **Major** | **M7 — Step 04 and Step 06 disagree on whether simulation transition lists are semantic.** Step 04 says “simulation transition lists — authored order (Step 06)” and that every other collection is sorted by `id` and a semantic array is never re-sorted (Step 04 110–117). Step 06 says the kind-specific arrays are “sorted deterministically” and “transition assignment order is not semantic; the solver resolves by rule lookup” (74–75); the data contract follows Step 06 (440). The two rules produce different canonical hashes and different persisted order for the same model. One of the two design pages must be changed; Step 06 cannot be implemented against both. | Step 04 page 110–117; Step 06 page 72–75; `02-data-contracts.md` 440; `ProjectJson.CanonicalizeModel` 93–133. |
| **Minor** | **m1 — Polymorphic persistence leaves type/kind and flag invariants unstated.** `Simulation` is an abstract record with a settable `SimulationKind Kind` and a `TransitionIds` array (129–141); `KineticSimulation` and `MetropolisSimulation` can be constructed with the wrong `Kind`, and no SIM code rejects that mismatch even though the persisted `kind` selects the DTO. `ExecutionFlags` (139) is not defined as a `[Flags]` enum or a set; the data contract persists an array (462) and the job payload uses a `u32` bitmask, while the design says the arrays are “sorted” (74). There is also no rule for duplicate `TransitionRef` entries or for a persisted simulation that has both or neither kind-specific array. | Step 06 page 69–75 and 129–141; `02-data-contracts.md` 440–472; Step 11 job payload; existing `TransitionRef.cs` and `TransitionKind.cs`. |
| **Minor** | **m2 — The code-layout and task list names a nonexistent file and misses the files M1 requires.** `ProjectJsonContext.cs` is listed (314) and named in the task (342), but `ProjectJsonContext` is a nested internal type in `Project/ProjectJson.cs`; Step 04 explicitly says no standalone file exists or is added (Step 04 99–100). `ProjectSchema.cs` contains only the version constants and needs no change while `schemaVersion` stays 1, yet the task says to update it. `ProjectEditor.cs` is already 506 lines and the design says to add lattice/simulation operations without the partial split used by the energy/transition editors. `ProjectJson.cs` and `ProjectHasher.cs` are absent from the layout even though M1 requires them. | Step 06 page 290–343; `src/dotnet/Mocassin.Core/Project/ProjectJson.cs` 410; `ProjectSchema.cs` 1–14; `ProjectEditor.cs` 506 lines; Step 04 page 99–100; Step 05 page 544–545. |
| **Minor** | **m3 — `ILatticeQuery`/`ISimulationQuery` are not integrated with the established query boundary, and the preview service mixes two concerns.** `IProjectQuery` is currently `IParticleQuery, IStructureQuery, ITransitionQuery, IEnergyQuery`; Step 06 says `Project` implements the two new queries but does not add them to `IProjectQuery`. Consumers that take one snapshot (compile, preview, Step 25) therefore need extra interfaces and the same-snapshot guarantee is not explicit. `ILatticePreviewService` also combines lattice preview and job-count preview; the latter belongs to Step 10's job expansion. | `src/dotnet/Mocassin.Core/Project/IProjectQuery.cs`; Step 06 page 241–245 and 266–283; Step 04 page 620. |
| **Minor** | **m4 — Manifest, schema, versioning, and fixture tasks are incomplete, and the additive-v1 exception is not reconciled.** ADR 0009 says a manifest change requires a version bump; the design keeps `schemaVersion` 1 under the Step 03–05 precedent but does not state that reconciliation, and does not update the manifest `status`/`notes`, name the new fixture IDs in `fixtures`, or update `contracts/generated/fixtures/descriptors.json`. The generator has no `oneOf`/discriminator support, so the “polymorphic” simulation shape can only be a flat object with optional kind-specific fields; the design does not state which fields are required or rejected for the other kind. The `ProjectSchema` task is a no-op while the version stays 1. | Step 06 page 339–359; `contracts/manifest.json` `status: step-05` and notes; `tools/contracts/generate.py` (no oneOf/discriminator); `contracts/generated/schema/project-v1.schema.json`; ADR 0009 Rules; `02-data-contracts.md` 439–441. |
| **Minor** | **m5 — Scope, planning, and audit coverage are incomplete.** The step page has no Non-goals section (Steps 03/04/05 each have one). Acceptance line 367 says dopings are “applied in ascending order”, which is Step 10 lattice generation, not Step 06; line 369 is already false per M2/M3. The task list has no parity-matrix update, status/closeout/audit task, or SPDX/licence task, and no update to `docs/user-guide/index.md`, `docs/reference/index.md`, `docs/development/index.md`, `samples/README.md`, or the audit record. The documentation section lists `docs/reference/project-format.md` twice (383 and 385). The audit request's step-specific checklist (`audits/step-06.md` 38–44) omits the round-trip, schema, and PublicAPI acceptance items from the step page (366–375). | Step 06 page 361–387; Step 03/04/05 non-goals sections; `docs/planning/audits/step-06.md` 36–44; `docs/user-guide/index.md`; `zensical.toml`; parity matrix §4. |
| **Minor** | **m6 — The test plan lacks tiers, per-code coverage, and key regression/property/performance cases.** The tests table (389–402) has no PR-fast/PR-medium/nightly tiers and no benchmark or performance target for the preview, unlike Steps 03–05. It has no positive/negative test row for LAT001–LAT011 or SIM001–SIM011, no save/load/document-hash test showing the new sections survive `CanonicalizeModel` and change `ProjectHasher.Compute`, no property test for random valid doping sequences, and no test for empty/absent/populated kind-specific simulation arrays. The acceptance “applied in ascending order” is a Step 10 test, not a Step 06 test. | Step 06 page 389–402; Step 03 test plan 619–653; Step 04 tests 700–720; Step 05 tests 673–700; `03-quality-strategy.md` §§5, 9.4. |
| **Minor** | **m7 — Several validation rules are internally inconsistent or diverge from the original without a deviation note.** Line 113 says a false `ChargeCompensation` means `DependentExchangeId` “must be null or ignored”, while LAT008 (204) makes a dependent exchange without compensation an error and the data contract says `dependentExchangeId` “may be omitted” when compensation is false. Line 183 says simulation names are unique, but SIM009 (216) only warns. SIM003 (210) makes a simulation with no transitions an error, contradicting the original `TransitionCount` minimum 0 and Step 07's feasibility stage; a newly created simulation has no transition assignment in the documented defaults. The original rejects duplicate names as errors and emits a warning (not an error) for wrong-sign compensation; no deviation note explains the changes. `Priority` has no range or negative-value rule, and the original `Priority` is a simultaneous-doping group ID, while Step 06 requires unique values. | Step 06 page 112–118, 183, 204, 210, 216; `02-data-contracts.md` 433–435; original `MocassinSimulationSettings.InitAsDefault` (`TransitionCount` min 0), `DopingValidator.cs`, `Doping.cs`; Step 07 page 51 and 135. |
| **Note** | **n1 — Record parent and frozen delta (process note).** At review start `HEAD` was `2c7ce02`, a merge of `99074e2` and `6285472`; `6285472`'s parent is the frozen design commit `a64ecf3`. `git diff a64ecf3..2c7ce02 -- docs/planning` changes only `docs/planning/reviews/step-06.md` (the request record) and `docs/planning/status.md`; no design doc changed. While the review was being written, another agent added `56f41b2` and `ea825d2` to `main` (docs-only cross-cutting review plans; no Step 06 design or data-contract change). The record commit's parent is therefore `ea825d2`, not `a64ecf3`, so the §3.9 sentence “the reviewed commit is the parent of the record commit” is not literally met. The design content reviewed is still exactly `a64ecf3`. This is a process note for the human owner, not a design finding. | Commands and output above; `git show -s --format='%H %P %s'`; `git diff --stat 2c7ce02..ea825d2 -- docs/planning`. |
| **Note** | **n2 — The in-memory transition-list sort key is unstated.** `TransitionRef` is `(TransitionKind Kind, Guid Id)`; the design says the persisted arrays are sorted but does not say whether the in-memory `TransitionIds` array is sorted by `(Kind, Id)`, by `Id`, or preserved. The answer must match M7 and the canonicalization rule. | Step 06 page 74 and 141; `Model/Transitions/TransitionRef.cs`. |
| **Note** | **n3 — `Mocassin.Compilation` already depends on `Mocassin.Symmetry`.** The no-Symmetry constraint in M5 is self-imposed, not an architecture rule; `Mocassin.Compilation.csproj` references `Mocassin.Symmetry`. A Symmetry-side or Compilation-side preview that consumes `StructurePreview` would not violate the dependency direction. | `src/dotnet/Mocassin.Compilation/Mocassin.Compilation.csproj`; `docs/development/architecture.md` dependency direction. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Re-review required
- [ ] Waived

Follow-up owner: Planner and Step 06 agent (design revision and finding resolution)
Follow-up step: Step 06 — Lattice and Simulation Models; a fresh re-review is required after the design commit

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
