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
| Design review status | Re-review required |
| Reviewed commit | `e587a6b` |
| Docs branch | `docs/step-06-design-revision` |
| Review date | 2026-11-10 |
| Reviewer | — (round-5 reviewer to fill) |

Step page: [`Step 06 — Lattice and Simulation Models`](../steps/step-06-lattice-and-simulation-models.md)

Round 1 was reviewed at `a64ecf3`, round 2 at `160b226`, round 3 at `5ae32b0`, and round 4 at `fe4868a`; all
reviewer text is unchanged. The round-4 notes were corrected in `e587a6b`, and the round-5 request is appended
at the end of this record.

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
| 1 | 2026-11-10 | `a64ecf3` | Independent planning auditor (fresh context) | Changes required |
| 2 | 2026-11-10 | `160b226` | Fresh independent re-reviewer | Ready with notes |
| 3 | 2026-11-10 | `5ae32b0` | Fresh independent re-reviewer | Ready with notes |
| 4 | 2026-11-10 | `fe4868a` | Fresh independent re-reviewer | Ready with notes |
| 5 | 2026-11-10 | `e587a6b` | Awaiting fresh reviewer | — |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| M1–M7 | Major | See round 1 | Design revision; see `Resolution status` | `160b226` | Resolved |
| m1–m7 | Minor | See round 1 | Design revision; see `Resolution status` | `160b226` | Resolved |
| n1–n3 | Note | See round 1 | Acknowledged/incorporated; see `Resolution status` | `160b226` | Closed |
| R2-1–R2-3 | Minor | See round 2 | Small corrections; see `Resolution status` | `5ae32b0` | Resolved |
| R2-4–R2-8 | Note | See round 2 | Corrections/acknowledgement; see `Resolution status` | `5ae32b0` | Resolved/acknowledged |
| R3-1 | Minor | See round 3 | Min-success-rate upper bound added | `fe4868a` | Resolved |
| R3-2–R3-3 | Note | See round 3 | Flag semantics and deviation placement corrected | `fe4868a` | Resolved |
| R4-1 | Minor | See round 4 | `SolverJobFlags` carrier and derived-bit table added | `e587a6b` | Resolved — awaiting round 5 |
| R4-2–R4-4 | Note | See round 4 | Deterministic consumer, normalization canonicalization, dashboard acknowledgement | `e587a6b` | Resolved/acknowledged — awaiting round 5 |

## Human approval

- [ ] Accepted: Ready for implementation
- [ ] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by:
Date:

## Resolution status

Design revision: `160b226` — Step 06 design, data-contract, Step 10/11/25 dependency, and audit-plan
corrections. This section is owned by the planner/step agent and does not alter the reviewer's findings,
evidence, or verdict.

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| M1 | Resolved — awaiting re-review. The canonical `ProjectJson.CanonicalizeModel`/write/save/hash path is now an explicit contract and task, with save/load and document-hash tests. | Planner | `160b226` |
| M2 | Resolved — awaiting re-review. Added `WriteOutCount`; corrected the defaults to the original user-facing values (write-out 100, P1Y, KMC pre-run 100, field 1e7 V/m `(1,0,0)`, no normalization, MMC samples 1000/100/200); recorded the derived-seed deviation. | Planner | `160b226` |
| M3 | Resolved — awaiting re-review. Added hard ranges and recommended bands for every original simulation setting, expanded `SIM004`–`SIM035`, and made the pre-run disable state representable (`0`). | Planner | `160b226` |
| M4 | Resolved — awaiting re-review. Removed the Step 10-owned conditions from Step 06; Step 10 owns `JOB004`–`JOB015`, and Step 07 owns build-time non-empty feasibility. | Planner | `160b226` |
| M5 | Resolved — awaiting re-review. Moved the lattice/job-count preview to Step 10 over the single shared generator; Step 06 defines no preview service. | Planner | `160b226` |
| M6 | Resolved — awaiting re-review. Redesigned the preview API in Step 10: request/doping echo, `DopingReport`, per-site populations, `Try*` failure boundary, cache, and `JobCountPreview`. | Planner | `160b226` |
| M7 | Resolved — awaiting re-review. Conformed to Step 04: transition arrays preserve authored order and are never re-sorted; Step 04 is unchanged. | Planner | `160b226` |
| m1 | Resolved — awaiting re-review. `Kind` is derived from the concrete record; `ExecutionFlags` is a `[Flags]` enum with a sorted camelCase-array/u32 contract; duplicate references and kind/array mismatches are validated. | Planner | `160b226` |
| m2 | Resolved — awaiting re-review. Removed the nonexistent `ProjectJsonContext.cs`, added `ProjectJson`/`ProjectHasher`, kept `ProjectSchema` unchanged for v1, and planned partial `ProjectEditor.Lattice.cs`/`ProjectEditor.Simulations.cs`. | Planner | `160b226` |
| m3 | Resolved — awaiting re-review. Added `ILatticeQuery`/`ISimulationQuery` to `IProjectQuery`; moved job-count preview to Step 10. | Planner | `160b226` |
| m4 | Resolved — awaiting re-review. Added manifest status/notes, four fixture IDs, descriptor/schema refresh, the flat-simulation shape, and the additive-v1/ADR 0009 reconciliation. | Planner | `160b226` |
| m5 | Resolved — awaiting re-review. Added non-goals, corrected the acceptance criteria, added parity/status/closeout/audit/SPDX and index/nav/samples tasks, removed the duplicate project-format entry, and planned the audit-checklist update at closeout. The status dashboard remains implementor-owned. | Planner | `160b226` |
| m6 | Resolved — awaiting re-review. Added test tiers, per-code coverage, save/load/document-hash, property, kind-array, and defaults/limits tests; preview tests moved to Step 10. | Planner | `160b226` |
| m7 | Resolved — awaiting re-review. Made compensation/dependent, duplicate-name, `SIM003`, priority-range, and seed/compensation severity rules consistent, with recorded deviations. | Planner | `160b226` |
| n1 | Acknowledged. The round-1 parent is the docs-only request/freeze commit; the design content reviewed is exactly `a64ecf3`. The revision is a new commit on `docs/step-06-design-revision` because another agent is concurrently editing cross-cutting review docs on `main`'s worktree. | Planner | `160b226` |
| n2 | Resolved — awaiting re-review. Transition lists preserve authored order; no in-memory sort key is needed. | Planner | `160b226` |
| n3 | Acknowledged; incorporated in Step 10. `Mocassin.Compilation` already references `Mocassin.Symmetry`, so the shared preview can consume P1 multiplicity without a new dependency. | Planner | `160b226` |
| R2-1 | Resolved — awaiting round 3. Added `SIM036` warning for 11–100 transitions (recommended 1–10). | Planner | `5ae32b0` |
| R2-2 | Resolved — awaiting round 3. Added `SIM037` warning for a positive `MinSuccessRate` below 10 MCS/s. | Planner | `5ae32b0` |
| R2-3 | Resolved — awaiting round 3. Added write-out count to Step 25's simulation-editor common settings. | Planner | `5ae32b0` |
| R2-4 | Resolved — awaiting round 3. Recorded the write-out-0 final-state-only mode as a deliberate warning-band deviation. | Planner | `5ae32b0` |
| R2-5 | Resolved — awaiting round 3. Aligned the `.msl` `write_out_count` SQL default to 100 and documented that the writer always sets it from the resolved payload. | Planner | `5ae32b0` |
| R2-6 | Resolved — awaiting round 3. Corrected the original-reference attribution: max attempt frequency is the KMC job-configuration base frequency, with an optional simulation-level default. | Planner | `5ae32b0` |
| R2-7 | Resolved — awaiting round 3. `SIM004`, `SIM007`, and `SIM016` now reject non-finite values explicitly, with a general NaN/Inf rule. | Planner | `5ae32b0` |
| R2-8 | Acknowledged. The round-3 record commit will also have the docs-only request commit as its parent; the design content is frozen at `5ae32b0`. | Planner | `5ae32b0` |
| R3-1 | Resolved — awaiting round 4. `SIM012` now rejects a min success rate above `int.MaxValue` as well as negative/non-finite values. | Planner | `fe4868a` |
| R3-2 | Resolved — awaiting round 4. Defined `Deterministic` semantics and mapped the derived/compiler-owned flags (`KmcSimulation`/`MmcSimulation`, `UsePrerun`, `UseDualDofCorrection`) that are not user-authored; documented the new `.msl` bit values. | Planner | `fe4868a` |
| R3-3 | Resolved — awaiting round 4. Moved the write-out-0 deviation into the simulation section's deviation list. | Planner | `fe4868a` |
| R4-1 | Resolved — awaiting round 5. Named the `SolverJobFlags` carrier and bit table, including `useDualDofCorrection` bit 4, `usePrerun` bit 5, and the kind bits; Steps 08/11/14/16 now consume the `.msl` flag. | Planner | `e587a6b` |
| R4-2 | Resolved — awaiting round 5. `Deterministic` now disables Step 16's `UseFrequencyPrecheck`; the bit table is in the `.msl` payload contract. | Planner | `e587a6b` |
| R4-3 | Resolved — awaiting round 5. `NormalizationEnergyEv = 0.0` is accepted and canonicalizes to `null`; the test plan covers it. | Planner | `e587a6b` |
| R4-4 | Acknowledged. `docs/planning/status.md` is implementor-owned and will be updated at step start/closeout; it is outside the reviewer and planner write scope. | Planner | `e587a6b` |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Re-review request (round 2)

| Field | Value |
| --- | --- |
| Step | 06 — Lattice and Simulation Models |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Design review status | Re-review required |
| Reviewed commit | `160b226` |
| Docs branch | `docs/step-06-design-revision` |
| Review date | 2026-11-10 |
| Reviewer | Independent planning auditor (fresh context, round 2; no shared conversation with the planner or implementor) |

Design revision: `160b22636bae8afce7fa077336fcbc6e0bed8755`.

The revision delta from `a64ecf3` is five planning files: the Step 06 page, `02-data-contracts.md`, and the
Step 10, Step 11, and Step 25 dependency pages. The round-1 request record and status dashboard are unchanged
except for the review-record resolution/status text above. The current `main` worktree contains another agent's
uncommitted cross-cutting review edits, so the revision is frozen on the `docs/step-06-design-revision` branch;
the design content is identical to the working-tree revision at `160b226`.

Reviewer: append your independence statement, evidence, findings, and verdict for round 2 below. Keep the
round-1 text unchanged.

### Re-review round 2

#### Reviewer context and independence

I am a fresh planning auditor for round 2. I did not author or implement the Step 06 design, the revision, or any
part of the implementation, and I have no shared conversation with the planner, the implementor, or the round-1
reviewer. I read the frozen design, the dependent step pages, the data contracts, the ADRs, the manifest/schema
tooling, and the original implementation as read-only reference myself; I did not rely on the resolution table or
the spawn summary for the verdict.

#### Evidence

**Frozen state and worktree.** Worktree `/tmp/opencode/step06-design`, branch `docs/step-06-design-revision`.
At review start `HEAD` was `dc67746`; the design revision is `160b226` and the re-review request is `dc67746`,
whose only parent is `160b226`. `git show --name-status dc67746` changes only
`docs/planning/reviews/step-06.md` and the one-line Step 06 design-review link; the design content is identical
at `160b226`. The round-1 design content is at `a64ecf3`; the design revision delta from the round-1 base is the
five files listed below. While this review was written, the shared `/workspace/mocassin` worktree contained
another agent's uncommitted cross-cutting review edits; I did not touch it.

**Commands run (from `/tmp/opencode/step06-design`):**

```sh
git status --short
# (no output: clean worktree)

git log --oneline --decorate -6
# dc67746 (HEAD -> docs/step-06-design-revision) docs: request step-06 design re-review [skip ci]
# 160b226 docs: revise step-06 lattice and simulation design after review [skip ci]
# befdf7e (main) docs: record step-06 design review Changes required [skip ci]
# ea825d2 docs: add post-step-07 review checkpoint plan [skip ci]
# 56f41b2 docs: plan post-step-07 repository reviews [skip ci]
# 2c7ce02 docs: freeze step-06 design for review [skip ci]

git show --stat 160b226
# commit 160b22636bae8afce7fa077336fcbc6e0bed8755
# docs: revise step-06 lattice and simulation design after review [skip ci]
#  docs/planning/02-data-contracts.md                 | 112 +++-
#  .../steps/step-06-lattice-and-simulation-models.md | 613 ++++++++++++++-------
#  .../steps/step-10-job-templates-and-supercells.md  |  79 ++-
#  .../step-11-simulation-package-and-msl-writer.md   |   2 +-
#  ...-25-model-editors-energy-lattice-simulations.md |   5 +-
#  5 files changed, 565 insertions(+), 246 deletions(-)

git diff --stat a64ecf3..160b226 -- docs/planning
# 14 files, 1549 insertions(+), 258 deletions(-); five design files plus nine docs-only
# main commits between a64ecf3 and befdf7e (cross-cutting review plans, the review record,
# and status.md). `git diff --stat befdf7e..160b226` shows only the five design files above.

git diff a64ecf3..160b226 -- docs/planning
# Full diff inspected. The Step 06 design delta is the five files above; the other nine files
# are the docs-only commits 56f41b2/ea825d2 (cross-cutting review plans), 2c7ce02 (freeze),
# 6285472 (round-1 request), befdf7e (round-1 record), and status.md. No code or contract
# artifact changed.

git diff --check
# (no output, exit 0)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.25s
# exit 0

python3 tools/contracts/generate.py --check
# Contract artifacts are current (17 files)
# exit 0

./tools/verify.sh docs
# First run in the fresh worktree exited 255: DocFX found no built Mocassin.*.dll files
# ("No .NET API project detected"). This is a missing build-output condition, not a docs failure.
# After `dotnet build Mocassin.slnx -c Release` (Build succeeded, 0 Warning(s), 0 Error(s)):
# ==> Zensical (strict): No issues found
# ==> DocFX: Build succeeded
# ==> rustdoc: Documentation built
# exit 0

git status --short
# (no output: build output is ignored)
```

**Additional read-only checks.**

```sh
git show -s --format='%H %P %s' 160b226 dc67746
# 160b226... befdf7e... docs: revise step-06 lattice and simulation design after review [skip ci]
# dc67746... 160b226... docs: request step-06 design re-review [skip ci]

git show --name-status dc67746
# M docs/planning/reviews/step-06.md
# M docs/planning/steps/step-06-lattice-and-simulation-models.md   # one-line design-review link

git merge-base --is-ancestor a64ecf3 HEAD
# exit 0

git diff --stat a64ecf3..160b226 -- \
  docs/planning/steps/step-06-lattice-and-simulation-models.md \
  docs/planning/02-data-contracts.md \
  docs/planning/steps/step-10-job-templates-and-supercells.md \
  docs/planning/steps/step-11-simulation-package-and-msl-writer.md \
  docs/planning/steps/step-25-model-editors-energy-lattice-simulations.md
# 5 files changed, 565 insertions(+), 246 deletions(-)
```

**Source and contract checks.** I verified the canonical persistence/hash path in
`src/dotnet/Mocassin.Core/Project/ProjectJson.cs` (`SerializeCanonicalModel` 81–84, `CanonicalizeModel` 93–133,
`ProjectJsonConverter.Write` 313 and 360), `ProjectHasher.cs` 26–29 and 58,
`ProjectEditor.cs` 57–58, `IProjectQuery.cs`, `TransitionRef.cs`, `ModelDefinition.cs` (empty defaults),
`TypedIds.cs` 237–285 (`OccupationExchangeId`, `DopingId`, `SimulationId`), and
`Mocassin.Compilation.csproj` 16–17 (`Mocassin.Core` and `Mocassin.Symmetry` references). I checked
`contracts/manifest.json` (`status: step-05`, fixture list, project model schema) and
`tools/contracts/generate.py` (no `oneOf`/`anyOf`/`discriminator` support, generated descriptors/schema). I
checked the original settings and validators read-only in `/workspace/mocassin-original`:
`MocassinSimulationSettings.cs` (`InitAsDefault`, `TransitionCount`, `TerminationSuccessRate`,
`WriteCallCount`, `PreRunMcsp`, `Normalization`, `ElectricField`), `ValueSetting.cs` (constructor and
`SetValues` sorting), `SimulationBaseValidator.cs`, `KineticSimulationValidator.cs`,
`MetropolisSimulationValidator.cs`, `SimulationBaseData.cs`, `KineticSimulationData.cs`,
`MetropolisSimulationData.cs`, `ISimulation.cs`, `IKineticSimulation.cs`, and `SimulationFlags.cs`.

**Cross-step checks.** I read Step 03, Step 04 (authored-order rule 110–117 and the `IProjectQuery` boundary),
Step 05, Step 07 (feasibility 50, 92, 135), Step 08 (simulation encoding), Step 10 (job templates, JOB001–JOB015,
and the new preview API), Step 11 (write-out metadata), Step 16 (rate abort), Step 25 (editor/preview), ADR 0005,
ADR 0009, `03-quality-strategy.md`, `appendix-b-parity-matrix.md`, `status.md`, and `audits/step-06.md`.

**Files reviewed.** `AGENTS.md` §3.8–§3.13 and §8/§10/§11; `docs/planning/reviews/README.md`; the Step 06 page
at `160b226` (all sections); `docs/planning/02-data-contracts.md` (model envelope, lattice/doping, simulations,
job payload, `.msl` tables); Step 03, 04, 05, 07, 08, 10, 11, 16, and 25 pages; ADR 0005 and 0009;
`contracts/manifest.json`; `tools/contracts/generate.py`; `contracts/generated/schema/project-v1.schema.json`;
`contracts/generated/fixtures/descriptors.json`; the Core project/query/hash/editor sources listed above; the
original simulation and validator sources listed above; `docs/user-guide/`, `docs/reference/project-format.md`,
`docs/development/architecture.md`, `docs/development/code-structure.md`,
`docs/development/compilation-pipeline.md`, `zensical.toml`, `tools/verify.sh`,
`tools/structure-policy.json`, and `tools/api-projects.txt`.

**Scope checks.** I reviewed the full requested scope: `ModelDefinition` lattice/simulation sections and
JSON/schema round-trip; polymorphic simulation persistence and kind-specific transition arrays; doping
priority/order/serialization, the derived building block, and charge-compensation semantics;
`SimulationKind`/`TransitionKind` compatibility, defaults, and limits; `ILatticeQuery`/`ISimulationQuery` and
their `IProjectQuery` integration, including the preview API now owned by Step 10; LAT001–LAT011 and
SIM001–SIM035 ownership/validation; Step 03/04 dependencies; manifest/schema/fixture tasks; PublicAPI/ApiCompat
tasks; code layout; documentation/navigation and test plans; and the remaining cross-step interface questions
across Step 06, `02-data-contracts.md`, Step 10, Step 11, and Step 25. The findings record the result.

#### Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **R2-1 — The recommended-band coverage is still incomplete for the transition count.** The original `TransitionCount` is `new ValueSetting<int>("Transitions per Simulation", 1, 0, 10, 100)`; `ValueSetting.SetValues` sorts the four values to `0,1,10,100`, so the original hard range is 0–100 and the recommended band is 1–10. `SIM003` correctly warns at 0 and `SIM021` errors above 100, but no code warns for 11–100. The step page says the original recommended bands remain warnings, and the acceptance/test table claim every recommended band has a diagnostic and a boundary test; both are false for this band. | Step 06 page 69–76, 320, 338–339, 356–358, 529–530, 571; original `ICon.Model/ModelProject/Manager/Settings/Modules/MocassinSimulationSettings.cs` (`TransitionCount`); original `ValueSetting.cs` (`ValueSetting` constructor and `SetValues`); original `KineticSimulationValidator.cs` and `MetropolisSimulationValidator.cs`. |
| **Minor** | **R2-2 — The recommended-band coverage is still incomplete for `MinSuccessRate`.** The original `TerminationSuccessRate` is `new ValueSetting<double>("Lower Termination Success Rate", 0, 10, int.MaxValue, int.MaxValue)`; after sorting, the hard range is 0–`int.MaxValue` and the recommended lower bound is 10. `SIM012` only errors negative/non-finite. The default null/0 disabled state is fine, but a positive value below 10 is buildable, outside the original recommended band, and has no warning. The same acceptance/test claim is false for this band. | Step 06 page 75–76, 255, 329, 529–530, 571; original `MocassinSimulationSettings.cs` (`TerminationSuccessRate`); original `SimulationBaseValidator.cs` (`AddTerminationLimitValidations`). |
| **Minor** | **R2-3 — Step 25's simulation editor common-settings list omits `WriteOutCount`.** Step 06 adds the field and its acceptance requires every original setting; Step 25's original-reference summary names "block count" and its acceptance says "all settings", but the "Common settings" list does not include write-out count. The dependent editor page should list it. | Step 06 page 201, 252–253, 526–528; Step 25 page 36, 134–136, 194. |
| **Note** | **R2-4 — `SIM028` exempts 0 from the write-out recommended band without a recorded deviation.** The original `WriteCallCount` recommended band is 5–100 and would warn at 0; the design says "0 is allowed" because 0 writes only the final state. This is a sensible final-state-only mode, but it is not in the "Deviations from the original" list. Record it if intentional. | Step 06 page 181–187, 328, 345; original `MocassinSimulationSettings.cs` (`WriteCallCount`); original `SimulationBaseValidator.cs` (`AddCounterValidations`). |
| **Note** | **R2-5 — The `.msl` `jobs.write_out_count` default is 0 while the model default is 100.** Step 11 says the denormalized columns must match the job payload, so the writer must always set the column; document the SQL default as a fallback or align it to 100 to avoid a silent final-state-only job if a writer omits it. | `02-data-contracts.md` 689; Step 06 page 201, 252–253; Step 11 page 123–127. |
| **Note** | **R2-6 — The original-reference line attributes max attempt frequency to `IKineticSimulation`.** The original `IKineticSimulation` has pre-run, field vector/magnitude, normalization, and transitions only; max attempt frequency is a job-config setting in Step 10's original reference. The optional simulation-level default is reasonable, but the original-reference attribution should be corrected. | Step 06 page 62–65; original `IKineticSimulation.cs`; original `MocassinSimulationSettings.cs`; Step 10 page 36–45. |
| **Note** | **R2-7 — The range rules for `TemperatureK`, `BreakTolerance`, and `ElectricFieldDirection` are inequality checks.** A NaN value compares false and would not be rejected by a simple "outside range" test. Steps 03–05 explicitly reject non-finite values; add non-finite checks to `SIM004`, `SIM016`, and `SIM007`, or state that the validators treat NaN/Inf as errors. | Step 06 page 321, 324, 333; Step 03/05 non-finite validation conventions. |
| **Note** | **R2-8 — Record parent and frozen delta (process note).** The record commit's parent will be `dc67746`, not the frozen design commit `160b226`, because the docs-only re-review request sits between them. `dc67746` touches only the review record and the one-line Step 06 design-review link; the design content is identical at `160b226`. This is the same process pattern as n1, not a design finding. | Commands above; `git show -s --format='%H %P %s'`; `git show --name-status dc67746`. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. No Blocker or Major finding remains in round 2.

#### Round-2 checklist

- [x] M1 — canonical persistence/hash wiring: `CanonicalizeModel`, `Write`, `SaveAsync`, `Compute`,
  `ComputeDocument`, and `ProjectEditor.Apply` are explicit contracts and tasks; save/load and document-hash
  tests are planned.
- [x] M2 — original settings/defaults: `WriteOutCount`, P1Y, KMC pre-run 100, field 1e7 `(1,0,0)`, normalization
  null, MMC samples 1000/100/200, and the derived-seed deviation are recorded.
- [x] M3 — ranges/recommended bands/pre-run disable: `SIM004`–`SIM035` and the `0` pre-run disable state are
  present; residual coverage is R2-1/R2-2.
- [x] M4 — LAT/SIM versus Step 07/Step 10 ownership and the JOB code handoff: LAT010/LAT011/SIM010/SIM011 are
  repurposed; Step 10 owns JOB004–JOB015; Step 07 owns build-time non-empty feasibility.
- [x] M5/M6 — preview ownership and public API: Step 06 exposes no preview; Step 10 owns
  `ILatticePreviewService`, `LatticePreview`, `JobCountPreview`, request/doping echo, `DopingReport`, per-site
  populations, the `Try*` failure boundary, the cache, and the Symmetry/multiplicity path.
- [x] M7 — transition-array ordering: Step 06 now preserves authored order and never re-sorts; Step 04 is
  unchanged and the data contract follows.
- [x] m1–m7 — kind/flags, code layout, `IProjectQuery`, manifest/schema/fixtures/ADR 0009, scope/planning/audit
  coverage, tests, and validation semantics re-checked; residual details are R2-3–R2-7.
- [x] n1–n3 — record parent/frozen delta, in-memory sort key, and `Compilation`→`Symmetry` dependency re-checked;
  n2 is resolved by authored order and n3 is confirmed by `Mocassin.Compilation.csproj`.
- [x] `ModelDefinition` lattice/simulation sections and JSON/schema round-trip: flat model sections, converter,
  schema, fixtures, and the additive-v1/ADR 0009 reconciliation are defined.
- [x] Polymorphic simulation persistence and kind-specific arrays: flat DTO, `kind`, exact array selection,
  authored order, duplicate rejection, and `SIM023` mismatch are defined.
- [x] Doping priority/order/serialization, derived building block, and charge-compensation semantics: unique
  `0..9999` priorities, ascending serialization, derived building block, and sign/zero compensation validation
  are defined; ratio/truncation belongs to Step 10.
- [x] `SimulationKind`/`TransitionKind` compatibility, defaults, and limits: exact mapping, `SIM002`, defaults,
  hard ranges, and recommended bands are defined; residual is R2-1/R2-2.
- [x] `ILatticeQuery`/`ISimulationQuery` and `IProjectQuery`; confirm the preview API is owned by Step 10:
  confirmed.
- [x] LAT001–LAT011 and SIM001–SIM035 ownership/validation: complete code list, Core validators, Step 07/10
  handoff, and positive/negative test plan; residual is R2-1/R2-2.
- [x] Step 03/04 dependencies, manifest/schema/fixture tasks, PublicAPI/ApiCompat, code layout,
  documentation/navigation and test plans, and cross-step interfaces: checked; residual is R2-3/R2-6.
- [x] Internal consistency across Step 06, `02-data-contracts.md`, Step 10, Step 11, and Step 25: checked;
  residual is R2-3/R2-5.

#### Verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Waived

No Blocker or Major remains. M1–M7 are resolved by `160b226`; m1–m7 and n1–n3 are addressed or resolved. The
remaining items are three Minor findings (R2-1, R2-2, R2-3) and five Notes (R2-4–R2-8). Under
`docs/planning/reviews/README.md` §7, `Ready with notes` requires the human owner to accept the Minor/Note
findings explicitly; alternatively the planner/step agent can make the small corrections and request a new
round. The start gate remains the human owner's decision.

Follow-up owner: Human owner (accept R2-1–R2-3 or request a small docs correction); planner/Step 06 agent
(apply the accepted corrections).
Follow-up step: Step 06 — Lattice and Simulation Models; the design revision `160b226` is otherwise ready for
implementation.

## Re-review request (round 3)

| Field | Value |
| --- | --- |
| Step | 06 — Lattice and Simulation Models |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Design review status | Re-review required |
| Reviewed commit | `5ae32b0` |
| Docs branch | `docs/step-06-design-revision` |
| Review date | 2026-11-10 |
| Reviewer | Independent planning auditor (fresh context, round 3; no shared conversation with the planner or implementor) |

Design revision: `5ae32b0e5768c48629f1faff54e8aa0d62949be6`.

The revision delta from `160b226` is four planning files: the Step 06 page, `02-data-contracts.md`, Step 11,
and Step 25. It adds `SIM036`/`SIM037`, the write-out-0 deviation, non-finite checks for `SIM004`/`SIM007`/
`SIM016`, the corrected max-attempt-frequency attribution, the Step 25 write-out field, and the `.msl`
write-out-count writer/default note. The round-1 and round-2 reviewer text is unchanged.

Reviewer: append your independence statement, evidence, findings, and verdict for round 3 below. Keep the
round-1 and round-2 text unchanged.

### Re-review round 3

#### Reviewer context and independence

I am a fresh planning auditor for round 3. I did not author or implement the Step 06 design, the revision, or
any part of the implementation, and I have no shared conversation with the planner, the implementor, the
round-1 reviewer, or the round-2 reviewer. I read the frozen design, the dependent step pages, the data
contracts, the ADRs, the manifest/schema tooling, the current Core sources, and the original implementation as
read-only reference myself; I did not rely on the resolution table or any summary for the verdict.

#### Evidence

**Frozen state and worktree.** Worktree `/workspace/playground/step06-design`, branch
`docs/step-06-design-revision`. At review start `HEAD` was `2296bc9`; the frozen design revision is `5ae32b0`
and the round-3 request is `2296bc9`, whose only parent is `5ae32b0`. `git show --name-status 2296bc9` changes
only `docs/planning/reviews/step-06.md` and one line of the Step 06 page (the design-review link `160b226` →
`5ae32b0`); `git diff --name-only 5ae32b0..2296bc9` outside those two files is empty. The design content is
identical at `5ae32b0`. The worktree was clean before and after the builds.

**Commands run (from `/workspace/playground/step06-design`):**

```sh
git status --short
# (no output: clean worktree)

git log --oneline --decorate -6
# 2296bc9 (HEAD -> docs/step-06-design-revision) docs: request step-06 round-3 design re-review [skip ci]
# 5ae32b0 docs: address step-06 re-review notes [skip ci]
# f47f0e8 docs: record step-06 design re-review Ready with notes [skip ci]
# dc67746 docs: request step-06 design re-review [skip ci]
# 160b226 docs: revise step-06 lattice and simulation design after review [skip ci]
# befdf7e (main) docs: record step-06 design review Changes required [skip ci]

git show --stat 5ae32b0
# commit 5ae32b0e5768c48629f1faff54e8aa0d62949be6
# docs: address step-06 re-review notes [skip ci]
#  docs/planning/02-data-contracts.md                 |  4 ++-
#  .../steps/step-06-lattice-and-simulation-models.md | 38 +++++++++++++---------
#  .../step-11-simulation-package-and-msl-writer.md   |  4 +++
#  ...-25-model-editors-energy-lattice-simulations.md |  4 +--
#  4 files changed, 32 insertions(+), 18 deletions(-)

git diff --stat f47f0e8..5ae32b0 -- docs/planning
# same four files; 32 insertions(+), 18 deletions(-)

git diff f47f0e8..5ae32b0 -- docs/planning
# Full diff inspected. It adds SIM036/SIM037, the write-out-0 deviation, the non-finite
# wording for SIM004/SIM007/SIM016 and the general NaN/Inf rule, the corrected
# maximum-attempt-frequency attribution, the Step 25 write-out field, and the
# `.msl` write_out_count DEFAULT 100/writer note. No code or contract artifact changed.

git diff --check
# (no output, exit 0)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.25s
# exit 0

python3 tools/contracts/generate.py --check
# Contract artifacts are current (17 files)
# exit 0

./tools/verify.sh docs
# After `dotnet build Mocassin.slnx -c Release` (Build succeeded, 0 Warning(s), 0 Error(s)):
# ==> Zensical (strict): No issues found
# ==> DocFX: Build succeeded. 0 warning(s), 0 error(s)
# ==> rustdoc: Documentation built in /workspace/playground/step06-design/site
# exit 0

git status --short
# (no output: generated site/target/build output is ignored)
```

**Additional read-only checks.**

```sh
git show -s --format='%H %P %s' 5ae32b0 2296bc9 f47f0e8 160b226
# 5ae32b0... f47f0e8... docs: address step-06 re-review notes [skip ci]
# 2296bc9... 5ae32b0... docs: request step-06 round-3 design re-review [skip ci]
# f47f0e8... dc67746... docs: record step-06 design re-review Ready with notes [skip ci]
# 160b226... befdf7e... docs: revise step-06 lattice and simulation design after review [skip ci]

git show --name-status 2296bc9
# M docs/planning/reviews/step-06.md
# M docs/planning/steps/step-06-lattice-and-simulation-models.md   # one-line design-review link

git diff --stat 5ae32b0..2296bc9
#  docs/planning/reviews/step-06.md                   | 52 ++++++++++++++++++----
#  .../steps/step-06-lattice-and-simulation-models.md |  2 +-

git diff --name-only 5ae32b0..2296bc9 -- docs/planning \
  ':!docs/planning/reviews/step-06.md' \
  ':!docs/planning/steps/step-06-lattice-and-simulation-models.md'
# (no output: no other design doc changed)

git merge-base --is-ancestor 5ae32b0 HEAD
# exit 0
```

**Source and contract checks.** I re-checked the canonical path in `ProjectJson.cs`
(`SerializeCanonicalModel` 81–84, `CanonicalizeModel` 93–133, `ProjectJsonConverter.Write` 313 and 360), the
nested `ProjectJsonContext` at 483, `ProjectHasher.cs` 26–29 and `ComputeDocument`, `ProjectEditor.cs` 37 and
57–58 (document-hash no-op guard; file is 506 lines), `ProjectStore.SaveAsync`
(`Mocassin.Packages/Projects/ProjectStore.cs` 130), `IProjectQuery.cs` (currently `IParticleQuery,
IStructureQuery, ITransitionQuery, IEnergyQuery`), `ProjectSchema.cs` (version constants only),
`ModelDefinition.cs` (empty defaults for the Step 03–05 sections), `TransitionRef.cs`, and
`Ids/TypedIds.cs` 237–285 (`OccupationExchangeId`, `DopingId`, `SimulationId`). The new sections are not yet in
the manifest: `contracts/manifest.json` still has `status: step-05` and no lattice/simulation model fields;
that is the planned Step 06 task. `tools/contracts/generate.py` has no `oneOf`/`anyOf`/`discriminator` support,
and the generated `project-v1.schema.json` sets `additionalProperties: false` at the root and model, so the
flat simulation shape and the unknown-field acceptance are consistent with the generator.

**Original reference checked read-only (`/workspace/mocassin-original`).**
`MocassinSimulationSettings.InitAsDefault` (`MocassinSimulationSettings.cs` 141–160) defines `Temperature`
`0.1,100,5000,10000`, `TargetMcsp` `1,1,1000,1,000,000`, `JobCount` `1,5,100,1000`, `WriteCallCount`
`0,5,100,1000`, `PreRunMcsp` `-1,0,1000,1,000,000`, `ElectricField` `0,1e4,1e9,double.MaxValue`, `Normalization`
`0,-1,10,double.MaxValue`, `BreakTolerance` `0,0,0.1,1`, `BreakSampleLength` `0,0,10,000,100,000`,
`BreakSampleInterval` `1,1,100,1000`, `ResultSampleLength` `0,0,10,000,1,000,000`, `TerminationSuccessRate`
`0,10,int.MaxValue,int.MaxValue`, and `TransitionCount` `1,0,10,100`; `ValueSetting.SetValues`
(`ValueSetting.cs` 170–176) sorts the four values, and `ParseUpper`/`ParseLower` (144–166) apply the sorted
min/lower-warning/upper-warning/max. `IKineticSimulation.cs` (10–36) has pre-run, field vector/magnitude,
normalization, and transitions, but no max attempt frequency; that is the Step 10 KMC job-configuration
`BaseFrequency`. `SimulationBaseData.cs` 16–20, `KineticSimulationData.cs` 17–20, and
`MetropolisSimulationData.cs` 16–19 confirm the UI defaults (job 12, target MCSP 1000, temperature 1000,
write-out 100, P1Y, KMC pre-run 100, field 10e6 V/m `(1,0,0)`, normalization 0, MMC 0/1000/100/200).
`SimulationFlags.cs` 10–56 defines the original execution flags (`KmcSimulation`, `MmcSimulation`, `UsePrerun`,
`NoSaving`, `UseDualDofCorrection`, `NoJumpLogging`, `UseFastExp`); the user-facing overwrite flags (61–79) are
`NoSaving`, `NoJumpLogging`, `UseFastExp`. `DopingValidator.cs` makes wrong-sign counter-doping a critical
warning, not an error; `Doping.cs` documents `Priority` as a simultaneous-doping group ID.

**Cross-step checks.** I read Step 03, Step 04 (authored-order rule 110–117 and the same-snapshot query
boundary), Step 05, Step 07 (feasibility 50–58 and 130–137), Step 08 (simulation encoding), Step 10 (job
templates, JOB004–JOB015, and the preview API 204–256), Step 11 (metadata columns 120–134), Step 16 (MCS/s
abort deviation 55 and 190–200), Step 25 (common/KMC/MMC settings 128–170 and acceptance 192–196), ADR 0005 and
ADR 0009, `03-quality-strategy.md` §9.4, `appendix-b-parity-matrix.md`, `status.md`, and `audits/step-06.md`.

**Status/process observations.** `docs/planning/status.md` 53 and 82 still say the Step 06 design is
`Ready for review at a64ecf3`, while the step page and round-3 request say `5ae32b0`. The status dashboard is
implementor-owned and outside the reviewer write scope; the step page's closeout task includes the status
update. I did not edit it and do not treat it as a design finding.

**Scope checks.** I re-checked the full requested scope: `ModelDefinition` lattice/simulation sections and
JSON/schema round-trip; polymorphic simulation persistence and kind-specific transition arrays; doping
priority/order/serialization, the derived building block, and charge-compensation semantics;
`SimulationKind`/`TransitionKind` compatibility, defaults, limits, and diagnostic coverage;
`ILatticeQuery`/`ISimulationQuery` and `IProjectQuery` integration, including the preview API now owned by
Step 10; LAT001–LAT011 and SIM001–SIM037 ownership/validation; Step 03/04 dependencies; manifest/schema/fixture
tasks; PublicAPI/ApiCompat; code layout; documentation/navigation and test plans; and the remaining cross-step
interfaces across Step 06, `02-data-contracts.md`, Step 10, Step 11, and Step 25. The findings record the
result.

#### Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **R3-1 — `MinSuccessRate` does not enforce the upper hard bound stated for it.** The original `TerminationSuccessRate` is `ValueSetting<double>("Lower Termination Success Rate", 0, 10, int.MaxValue, int.MaxValue)`; `SetValues` sorts it to hard 0–`int.MaxValue`, recommended 10–`int.MaxValue`, and `ParseUpper` treats a value above `int.MaxValue` as a restriction violation. The step page still describes the hard range as 0–`int.MaxValue` (line 77), but `SIM012` only rejects negative/non-finite values and `SIM037` only warns for positive values below 10; a finite value above `int.MaxValue` is accepted. The acceptance/test lines 537/579 therefore overstate the coverage for this bound. Add the upper-bound check to `SIM012` (or a dedicated code), or state explicitly that the field is intentionally unbounded above and remove the `int.MaxValue` hard-range claim. This is a small limit-coverage gap, not a blocker. | Step 06 page 77, 333, 358, 537, 579; original `MocassinSimulationSettings.cs` 158 and `ValueSetting.cs` 144–176. |
| **Note** | **R3-2 — `ExecutionFlags.Deterministic` has no defined semantics and no original counterpart.** The design defines `ExecutionFlags` as `NoSaving`, `NoJumpLogging`, `UseFastExp`, and `Deterministic` and persists the camelCase array/`u32` bitmask, but it does not say what `Deterministic` changes; the data contract and Step 25 repeat the name without a definition. The original `SimulationExecutionFlags` has `KmcSimulation`, `MmcSimulation`, `UsePrerun`, `NoSaving`, `UseDualDofCorrection`, `NoJumpLogging`, and `UseFastExp`; the user-facing overwrite flags are only `NoSaving`, `NoJumpLogging`, and `UseFastExp`. The design replaces the kind flags with `SimulationKind` and the pre-run flag with `PreRunMcsp > 0`, which is reasonable, but `Deterministic` is new and the design does not define its behavior or say how the original internal `UseDualDofCorrection`/`UsePrerun` flags are represented. Define the flag's behavior or drop it and correct `appendix-a-original-inventory.md` 112. This is a pre-existing detail, not introduced by `5ae32b0`. | Step 06 page 233–241, 275–276; `02-data-contracts.md` 516–517; Step 25 page 137; `appendix-a-original-inventory.md` 112; original `SimulationFlags.cs` 10–79. |
| **Note** | **R3-3 — The write-out-0 deviation is recorded under the lattice section.** The revision records the deviation at Step 06 189–190, but the list begins at 182 under section 2 “Lattice model”; section 3 “Simulation model” starts at 194 and has no consolidated deviations list. The content is present and satisfies R2-4, but a reader looking in the simulation section will not find it. Move it to the simulation section or a shared deviations subsection. | Step 06 page 182–190, 349; the simulation section starts at 194. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. No Blocker or Major finding remains in round 3.

#### Round-3 checklist

- [x] R2-1 — `SIM036` warns for 11–100 transitions and the recommended 1–10 band is documented (Step 06 76,
  357, 579); the original `TransitionCount` sorts to hard 0–100 with recommended 1–10; `SIM021` errors above 100
  and `SIM003` warns at 0. The acceptance/test claim now holds for this band.
- [x] R2-2 — `SIM037` warns for a positive `MinSuccessRate` below 10 MCS/s (Step 06 77, 358); the original
  `TerminationSuccessRate` sorts to hard 0–`int.MaxValue` with recommended ≥10; 0 remains the documented
  disabled state. Residual upper bound is R3-1.
- [x] R2-3 — Step 25's simulation-editor “Common settings” now lists write-out count (Step 25 134–136), matching
  Step 06 204/255 and the Step 11 metadata column.
- [x] R2-4 — The write-out-0 final-state-only deviation is recorded (Step 06 189–190) and `SIM028` exempts 0
  (349); placement is R3-3.
- [x] R2-5 — `.msl` `write_out_count` SQL default is 100 (data contract 689), with the “writer always sets the
  denormalized columns; defaults are fallbacks” rule (715–716) and the Step 11 note (131–133); 0 remains a valid
  resolved value.
- [x] R2-6 — Maximum attempt frequency is attributed to Step 10's KMC job configuration, with an optional
  simulation-level default (Step 06 62–64, 269–271; Step 10 40); the original `IKineticSimulation` has no such
  member.
- [x] R2-7 — `SIM004`, `SIM007`, and `SIM016` now reject non-finite values (325, 328, 337), and the general rule
  checks NaN/Inf before comparisons and requires three finite field-direction components (365–366).
- [x] R2-8 — The record commit's parent is the docs-only request `2296bc9`, whose parent is `5ae32b0`; the
  request changes only the review record and the one-line design-review link, so the design content reviewed is
  exactly `5ae32b0`. This is the same process pattern as n1/R2-8.
- [x] `ModelDefinition` lattice/simulation sections and JSON/schema round-trip: flat sections, converter,
  canonicalization, save/hash, additive-v1/ADR 0009 reconciliation, and manifest/schema/fixture tasks are
  defined (Step 06 89–130, 461–466, 487–508, 539–545).
- [x] Polymorphic simulation persistence and kind-specific transition arrays: abstract record with derived
  `Kind`, flat DTO, exact kind-specific array/settings, authored order, duplicate rejection, and `SIM023`
  mismatch (Step 06 196–252; data contract 453–522).
- [x] Doping priority/order/serialization, derived building block, and charge-compensation semantics: unique
  `0..9999` priorities, ascending serialization, derived building block, sign/zero compensation, and Step 10
  ratio/truncation ownership (Step 06 155–192; data contract 412–450).
- [x] `SimulationKind`/`TransitionKind` compatibility, defaults, limits, and diagnostic coverage: exact mapping,
  `SIM002`, documented defaults, hard ranges, and recommended bands (Step 06 196–375); residual is R3-1.
- [x] `ILatticeQuery`/`ISimulationQuery` and `IProjectQuery`; preview ownership is Step 10: present
  (Step 06 390–427; Step 10 204–256).
- [x] LAT001–LAT011 and SIM001–SIM037 ownership/validation: complete code list, Core validators, Step 07/10
  handoff, and positive/negative tests (Step 06 298–376, 571–590); residual is R3-1/R3-2.
- [x] Step 03/04 dependencies, manifest/schema/fixture tasks, PublicAPI/ApiCompat, code layout,
  documentation/navigation, test plans, and cross-step interfaces: checked; the new code stays inside the
  structure policy, `ProjectSchema` is unchanged for v1, and the four fixtures follow the existing naming.
- [x] Internal consistency across Step 06, `02-data-contracts.md`, Step 10, Step 11, and Step 25: checked;
  residual is R3-2/R3-3.

#### Verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Waived

No Blocker or Major remains. R2-1–R2-7 are resolved by `5ae32b0`; R2-8 is a process note. R3-1 is a new Minor
(the `MinSuccessRate` upper hard bound); R3-2 and R3-3 are Notes. Under `docs/planning/reviews/README.md` §7,
`Ready with notes` requires the human owner to accept the Minor/Note findings explicitly; alternatively the
planner/step agent can make the small corrections and request a new round. The start gate remains the human
owner's decision.

Follow-up owner: Human owner (accept R3-1–R3-3 or request a small docs correction); planner/Step 06 agent
(apply the accepted corrections).
Follow-up step: Step 06 — Lattice and Simulation Models; the design revision `5ae32b0` is otherwise ready for
implementation.

## Re-review request (round 4)

| Field | Value |
| --- | --- |
| Step | 06 — Lattice and Simulation Models |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Design review status | Re-review required |
| Reviewed commit | `fe4868a` |
| Docs branch | `docs/step-06-design-revision` |
| Review date | 2026-11-10 |
| Reviewer | Independent planning auditor (fresh context, round 4; no shared conversation with the planner or implementor) |

Design revision: `fe4868a63a3e1a2da6234f84218e0f9f38d562c2`.

The revision delta from `5ae32b0` is two planning files: the Step 06 page and `02-data-contracts.md`. It adds
the `MinSuccessRate` upper bound to `SIM012`, defines `Deterministic` and the derived/compiler-owned execution
flags, documents the new `.msl` flag bit values, and moves the write-out-0 deviation into the simulation
section. The round-1, round-2, and round-3 reviewer text is unchanged.

Reviewer: append your independence statement, evidence, findings, and verdict for round 4 below. Keep the
earlier rounds unchanged.

### Re-review round 4

#### Reviewer context and independence

I am a fresh planning auditor for round 4. I did not author or implement the Step 06 design, the revision, or
any part of the implementation, and I have no shared conversation with the planner, the implementor, the
round-1 reviewer, the round-2 reviewer, or the round-3 reviewer. I read the frozen design, the step page, the
data contracts, the dependent step pages, the ADRs, the manifest/schema tooling, the current Core sources, and
the original implementation as read-only reference myself; I did not rely on the resolution table or the spawn
summary for the verdict.

#### Evidence

**Frozen state and worktree.** Worktree `/workspace/playground/step06-design`, branch
`docs/step-06-design-revision`. At review start `HEAD` was `201d126`; the frozen design revision is `fe4868a`
and the round-4 request is `201d126`, whose only parent is `fe4868a`. `git show --name-status 201d126` changes
only `docs/planning/reviews/step-06.md` and `docs/planning/steps/step-06-lattice-and-simulation-models.md`; the
step-page change is one line (the design-review link `5ae32b0` → `fe4868a`). `git diff --name-only
fe4868a..HEAD` shows only those two files, so the design content is identical at `fe4868a`. The worktree was
clean before and after the builds.

**Commands run (from `/workspace/playground/step06-design`):**

```sh
git status --short
# (no output: clean worktree)

git log --oneline --decorate -6
# 201d126 (HEAD -> docs/step-06-design-revision) docs: request step-06 round-4 design re-review [skip ci]
# fe4868a docs: close step-06 round-3 design notes [skip ci]
# 3c3d879 docs: record step-06 round-3 design review Ready with notes [skip ci]
# 2296bc9 docs: request step-06 round-3 design re-review [skip ci]
# 5ae32b0 docs: address step-06 re-review notes [skip ci]
# f47f0e8 docs: record step-06 design re-review Ready with notes [skip ci]

git show --stat fe4868a
# commit fe4868a63a3e1a2da6234f84218e0f9f38d562c2
# docs: close step-06 round-3 design notes [skip ci]
#  docs/planning/02-data-contracts.md                 |  4 ++++
#  .../steps/step-06-lattice-and-simulation-models.md | 16 ++++++++++++----
#  2 files changed, 16 insertions(+), 4 deletions(-)

git diff --stat 3c3d879..fe4868a -- docs/planning
#  docs/planning/02-data-contracts.md                 |  4 ++++
#  .../steps/step-06-lattice-and-simulation-models.md | 16 ++++++++++++----
#  2 files changed, 16 insertions(+), 4 deletions(-)

git diff 3c3d879..fe4868a -- docs/planning
# Full diff inspected. Data contract 516-521: the deterministic/derived-flag text and
# the .msl bit values None=0, NoSaving=1, NoJumpLogging=2, UseFastExp=4, Deterministic=8.
# Step 06 182-188: the write-out-0 bullet is removed from the lattice deviation list;
# Step 06 282-284: the simulation deviation list now carries it; Step 06 273-281 adds
# the per-flag semantics, the derived-flag mapping, and the Deterministic definition;
# Step 06 341: SIM012 now includes "above `int.MaxValue`". No other planning file
# changed, and no code or contract artifact changed.

git diff --check
# (no output, exit 0)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.23s
# exit 0

python3 tools/contracts/generate.py --check
# Contract artifacts are current (17 files)
# exit 0

./tools/verify.sh docs
# DocFX loaded the existing src/dotnet/*/bin/Release assemblies; this docs-only review
# changed no source.
# ==> Zensical (strict): No issues found
# ==> DocFX: Build succeeded. 0 warning(s), 0 error(s)
# ==> rustdoc: Documentation built in /workspace/playground/step06-design/site
# exit 0

git status --short
# (no output: generated site/target/build output is ignored)
```

**Additional read-only checks.**

```sh
git show -s --format='%H %P %s' 201d126 fe4868a 3c3d879
# 201d126... fe4868a... docs: request step-06 round-4 design re-review [skip ci]
# fe4868a... 3c3d879... docs: close step-06 round-3 design notes [skip ci]
# 3c3d879... 2296bc9... docs: record step-06 round-3 design review Ready with notes [skip ci]

git show --name-status 201d126
# M docs/planning/reviews/step-06.md
# M docs/planning/steps/step-06-lattice-and-simulation-models.md   # one-line design-review link

git diff --stat fe4868a..HEAD
#  docs/planning/reviews/step-06.md                   | 45 ++++++++++++++++++----
#  .../steps/step-06-lattice-and-simulation-models.md |  2 +-

git diff --name-only 3c3d879..fe4868a
# docs/planning/02-data-contracts.md
# docs/planning/steps/step-06-lattice-and-simulation-models.md

git merge-base --is-ancestor fe4868a HEAD
# exit 0
```

**Source and contract checks.** I re-checked the canonical path in `ProjectJson.cs`
(`SerializeCanonicalModel` 81–84, `CanonicalizeModel` 93–133, `ProjectJsonConverter.Write` 313 and 360, the
nested `ProjectJsonContext` 483), `ProjectHasher.cs` 26–29 and `ComputeDocument`, `ProjectEditor.cs` 37 and
57–58 (document-hash no-op guard; file is 506 lines), the existing partials `ProjectEditor.Energy.cs` and
`ProjectEditor.Transitions.*.cs`, `ProjectStore.SaveAsync` (`Mocassin.Packages/Projects/ProjectStore.cs` 130),
`IProjectQuery.cs` (currently `IParticleQuery, IStructureQuery, ITransitionQuery, IEnergyQuery`),
`ProjectSchema.cs` (version constants only), `ModelDefinition.cs` (empty defaults for the Step 03–05 sections),
`TransitionRef.cs`, `Ids/TypedIds.cs` 237–285 (`OccupationExchangeId`, `DopingId`, `SimulationId`), and
`Mocassin.Compilation.csproj` (Core and Symmetry references). `ProjectValidationService` exists in
`Mocassin.Packages/Projects`. The new sections are not yet in the manifest: `contracts/manifest.json` still has
`status: step-05` and no lattice/simulation model fields; that is the planned Step 06 task.
`tools/contracts/generate.py` has no `oneOf`/`anyOf`/`discriminator` support, and the generated
`project-v1.schema.json` sets `additionalProperties: false` at the root and model, so the flat simulation shape
and unknown-field rejection are consistent with the generator.

**Original reference checked read-only (`/workspace/mocassin-original`).**
`SimulationFlags.cs` 10–56 defines `KmcSimulation=1`, `MmcSimulation=2`, `UsePrerun=4`, `NoSaving=8`,
`UseDualDofCorrection=16`, `NoJumpLogging=32`, and `UseFastExp=64`; the user-facing overwrite flags
(`SimulationExecutionOverwriteFlags` 61–79) are only `NoSaving`, `NoJumpLogging`, and `UseFastExp`.
`JobDbModelBuilder.SetSimulationJobInfoFlags` (236–258) derives `KmcSimulation`/`MmcSimulation` from the
simulation type, derives `UsePrerun` from the pre-run value, and sets `UseDualDofCorrection` for KMC;
`JumpSelectionOptimizer` (45) can clear it, and `RunPostBuildOptimizers` (304–313) applies the optimizer flags
to `JobFlags`. `MocassinSimulationSettings.InitAsDefault` defines `TerminationSuccessRate`
`0,10,int.MaxValue,int.MaxValue` and `TransitionCount` `1,0,10,100`; `ValueSetting` sorts the four values and
`ParseUpper`/`ParseLower` apply the sorted bounds. `SimulationBaseData.cs`, `KineticSimulationData.cs`, and
`MetropolisSimulationData.cs` confirm the UI defaults (job 12, target MCSP 1000, temperature 1000, write-out
100, P1Y, KMC pre-run 100, field 10e6 V/m `(1,0,0)`, normalization 0, MMC 0/1000/100/200).
`DopingValidator.cs` makes wrong-sign counter-doping a critical warning, not an error; `Doping.cs` documents
`Priority` as a simultaneous-doping group ID.

**Cross-step checks.** I read Step 03, Step 04 (authored-order rule 110–117 and the `IProjectQuery` boundary),
Step 05, Step 07 (feasibility 93 and 135), Step 08 (compiled simulation shape 74–80), Step 10 (job settings
36–45, preview API 204–256, JOB001–JOB015), Step 11 (metadata columns 118–134), Step 14 (context flags and
`dof` 195–196), Step 16 (`UseFrequencyPrecheck` 126, `dof` 152, `NoSaving` 181/190), Step 18 (state flags and
`prerun.mcs` phase 185/213–217), Step 25 (common settings 134–137), ADR 0005 (determinism guarantees), ADR 0009
(version bump), `appendix-a-original-inventory.md` 112, `appendix-b-parity-matrix.md`, `status.md`, and
`audits/step-06.md`.

**Files reviewed.** `AGENTS.md` §3.8–§3.13, §8, §10–§12; `docs/planning/reviews/README.md`; the Step 06 page at
`fe4868a` (all sections); `docs/planning/02-data-contracts.md` (model envelope, lattice/doping, simulations,
`.msl` jobs/payload, compiled model); the Step 03, 04, 05, 07, 08, 10, 11, 14, 16, 18, and 25 pages; ADR 0005
and ADR 0009; `contracts/manifest.json`; `tools/contracts/generate.py`;
`contracts/generated/schema/project-v1.schema.json`; `contracts/generated/fixtures/descriptors.json`; the Core
project/query/hash/editor sources listed above; the original simulation and validator sources listed above;
`docs/user-guide/`, `docs/reference/project-format.md`, `docs/reference/diagnostics.md`,
`docs/development/architecture.md`, `docs/development/code-structure.md`,
`docs/development/compilation-pipeline.md`, `zensical.toml`, `tools/verify.sh`, `tools/structure-policy.json`,
and `tools/api-projects.txt`.

**Scope checks.** I reviewed the full requested scope: `ModelDefinition` lattice/simulation sections and the
JSON/schema round-trip, including the canonical save/hash path; polymorphic simulation persistence and the
kind-specific transition arrays; doping priority/order/serialization, the derived building block, and
charge-compensation semantics; `SimulationKind`/`TransitionKind` compatibility, defaults, limits, and
diagnostic coverage; `ILatticeQuery`/`ISimulationQuery` and `IProjectQuery` integration, with the preview owned
by Step 10; LAT001–LAT011 and SIM001–SIM037 ownership and validation; Step 03/04 dependencies;
manifest/schema/fixture tasks; PublicAPI/ApiCompat; code layout; documentation/navigation and test plans; and
the remaining cross-step interfaces across Step 06, `02-data-contracts.md`, Step 10, Step 11, Step 25, and the
solver steps that consume execution flags. The findings record the result.

**Status/process observations.** `docs/planning/status.md` 53 and 82 still say the Step 06 design is
`Ready for review at a64ecf3`, while the step page and this request say `fe4868a`. The status dashboard is
implementor-owned and outside the reviewer write scope; the step page's closeout task includes the status
update. This is a process note (R4-4), not a design finding. I did not edit it.

#### Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **R4-1 — The `UseDualDofCorrection` mapping is not carried into the `.msl`/solver contract that consumes it.** The revision says `UseDualDofCorrection` is a compiler/optimizer-derived KMC time-stepping flag and not user-authored (Step 06 280–281), and the data contract lists only `None`, `NoSaving`, `NoJumpLogging`, `UseFastExp`, and `Deterministic` for the `.msl` bitmask (516–521). Step 14 still branches on `dof = 2` if the simulation flag `DualDof` is set (195–196), and Step 16 repeats the rule (152). The compiler-owned bit or field that carries `DualDof` is not named in the compiled-model shape (Step 08 74–80), the compiled `default_settings` (data contract 809), or the `.msl` job payload (`flags` u32, data contract 835). The original sets this bit in `JobFlags` in `JobDbModelBuilder.SetSimulationJobInfoFlags` 236–258, and the post-build optimizer can clear it (`JumpSelectionOptimizer` 45; `RunPostBuildOptimizers` 304–313). Without a named carrier the solver cannot decide the `dof` factor, so the KMC time-step remains ambiguous. Fix: define the compiler-owned bit/field (for example `UseDualDofCorrection` in the compiled simulation default settings and the job-payload bitmask), document it in the `.msl` contract, and update Step 14/16, or state explicitly how the solver derives `DualDof` from the compiled model. | Step 06 page 280–281; `02-data-contracts.md` 516–521, 809, 835; Step 14 page 195–196; Step 16 page 152; Step 08 page 74–80; original `JobDbModelBuilder.cs` 236–258 and `JumpSelectionOptimizer.cs` 45; `appendix-a-original-inventory.md` 112. |
| **Note** | **R4-2 — `Deterministic` is defined abstractly, but no consuming step maps it to concrete behavior, and `UseFrequencyPrecheck` is exactly the kind of optional shortcut it describes.** The revision defines `Deterministic` as “requests reproducible execution and disables optional performance shortcuts that would change the RNG stream or block ordering” and documents the `.msl` bit 8 (Step 06 278–279; data contract 516–521). Step 16 126 defines an optional `UseFrequencyPrecheck` that “changes trajectory but not the long-time distribution” and is enabled by default; no step says whether `Deterministic` disables it. ADR 0005 already guarantees identical state for the same package/binary/platform/seed, so the flag's relationship to that guarantee needs one sentence. The `.msl` job payload table (data contract 835) still shows only `flags | u32`, so a solver implementor must infer the bit table from the project section. Fix: assign the concrete shortcuts to Steps 16/17/18 (or state that `Deterministic` is reserved and changes no default behavior), reconcile the wording with ADR 0005's determinism guarantee, and place the bit table beside the `.msl` payload. | Step 06 page 273–281; `02-data-contracts.md` 516–521, 835; Step 16 page 126; ADR 0005 `Determinism guarantees`; Step 25 page 137; `appendix-a-original-inventory.md` 112. |
| **Note** | **R4-3 — `NormalizationEnergyEv = 0.0` is accepted but neither defined nor canonicalized.** The design says `null` means no normalization and “a positive value enables fixed normalization” (Step 06 265–266), and the deviation note says the original −1/0 values both meant no normalization and the new model uses `null` for that state (187–188). `SIM014` rejects only negative/non-finite values (343), so `0.0` is valid. A persisted `0.0` then round-trips and changes the model hash even though it is physically the same as `null` (`K_fix = exp(0)^-1 = 1`, Step 14 190), and the data contract example and text only name `null` (480, 522–523). Fix: reject `0.0` in `SIM014`, or define it as equivalent to `null` and canonicalize it on save; add the boundary test. | Step 06 page 187–188, 265–266, 343; `02-data-contracts.md` 480, 522–523; Step 14 page 190; `SIM014`. |
| **Note** | **R4-4 — The status dashboard still names `a64ecf3` (process note).** `docs/planning/status.md` 53 and 82 still say the Step 06 design is `Ready for review at a64ecf3`, while the step page (16) and this request (861) say `fe4868a`. The dashboard is implementor-owned and outside the reviewer write scope; the step page's closeout task plans the update. This is the same process pattern as round 3, not a design finding. | `docs/planning/status.md` 53, 82; Step 06 page 16; round-4 request 861. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. No Blocker or Major finding remains in round 4.

#### Round-4 checklist

- [x] R3-1 — `SIM012` now rejects negative, non-finite, and above `int.MaxValue`; the hard upper bound matches
  the sorted original `TerminationSuccessRate` 0–`int.MaxValue`, and `SIM037` still covers the recommended
  ≥10 band. Resolved; the acceptance/test lines 545–546/587 now have a diagnostic and boundary test for this
  bound.
- [x] R3-2 — `Deterministic` semantics, derived/compiler-owned flags, and `.msl` bit values are documented
  (Step 06 273–281; data contract 516–521); the mapping is consistent for `KmcSimulation`/`MmcSimulation` and
  `UsePrerun`, but the `UseDualDofCorrection` consumer is R4-1 and the concrete `Deterministic` shortcuts are
  R4-2.
- [x] R3-3 — The write-out-0 deviation is now in the simulation section's deviation list (Step 06 282–284); the
  lattice deviation list no longer carries it (182–188). Resolved.
- [x] `ModelDefinition` lattice/simulation sections and JSON/schema round-trip: flat sections,
  `ModelDefinitionJsonConverter`, nested `ProjectJsonContext`, `CanonicalizeModel`, `Write`/`SaveAsync`,
  `Compute`/`ComputeDocument`, additive-v1/ADR 0009, and manifest/schema/fixture tasks are defined (Step 06
  89–130, 493–553; data contract 412–451).
- [x] Polymorphic simulation persistence and kind-specific transition arrays: abstract record with derived
  `Kind`, flat DTO, exact kind-specific array/settings, authored order, duplicate rejection, and `SIM023`
  mismatch (Step 06 196–252; data contract 453–522).
- [x] Doping priority/order/serialization, derived building block, and charge-compensation semantics: unique
  `0..9999` priorities, ascending serialization, derived building block, sign/zero compensation, and Step 10
  ratio/truncation ownership (Step 06 155–192; data contract 412–450).
- [x] `SimulationKind`/`TransitionKind` compatibility, defaults, limits, and diagnostic coverage: exact mapping,
  `SIM002`, defaults, hard ranges, and recommended bands (Step 06 196–375); residual is R4-1/R4-2.
- [x] `ILatticeQuery`/`ISimulationQuery` and `IProjectQuery`; preview ownership is Step 10: present (Step 06
  396–435; Step 10 204–256).
- [x] LAT001–LAT011 and SIM001–SIM037 ownership/validation: contiguous code list, Core validators, Step 07/10
  handoff, and positive/negative tests (Step 06 306–384, 574–598); residual is R4-1/R4-2.
- [x] Step 03/04 dependencies, manifest/schema/fixture tasks, PublicAPI/ApiCompat, code layout,
  documentation/navigation, test plans, and cross-step interfaces: checked; the new code stays inside the
  structure policy, `ProjectSchema` is unchanged for v1, and the four fixtures follow the existing naming;
  residual is R4-1/R4-3.
- [x] Internal consistency across Step 06, `02-data-contracts.md`, Step 10, Step 11, and Step 25: checked;
  residual is R4-1/R4-2 for the solver flag consumers; the status dashboard is R4-4.

#### Verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Waived

No Blocker or Major remains. R3-1–R3-3 are resolved by `fe4868a`; R4-1 is a Minor and R4-2–R4-4 are Notes.
Under `docs/planning/reviews/README.md` §7, `Ready with notes` requires the human owner to accept the
Minor/Note findings explicitly, or the planner/Step 06 agent can make the small corrections and request a new
round. The start gate remains the human owner's decision.

Follow-up owner: Human owner (accept R4-1–R4-4 or request a small docs correction); planner/Step 06 agent
(apply the accepted corrections).
Follow-up step: Step 06 — Lattice and Simulation Models; the design revision `fe4868a` is otherwise ready for
implementation.

## Re-review request (round 5)

| Field | Value |
| --- | --- |
| Step | 06 — Lattice and Simulation Models |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Design review status | Re-review required |
| Reviewed commit | `e587a6b` |
| Docs branch | `docs/step-06-design-revision` |
| Review date | — |
| Reviewer | — |

Design revision: `e587a6b5a096c595da11b2c1f6cf671c5da5f1ba`.

The revision delta from `fe4868a` is six planning files: the Step 06 page, `02-data-contracts.md`, and Steps
08, 11, 14, and 16. It adds the `SolverJobFlags` carrier and derived-bit table (including
`useDualDofCorrection`), ties `Deterministic` to Step 16's `UseFrequencyPrecheck`, canonicalizes
`NormalizationEnergyEv = 0.0` to `null`, and acknowledges the implementor-owned status dashboard. The round-1
to round-4 reviewer text is unchanged.

Reviewer: append your independence statement, evidence, findings, and verdict for round 5 below. Keep the
earlier rounds unchanged.
