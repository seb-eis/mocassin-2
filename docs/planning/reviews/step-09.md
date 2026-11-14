# Step 09 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-09.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 09 — Parameterization Templates |
| Request owner | Step 09 planner |
| Orchestrator (step agent / planner) | General planner/orchestrator |
| Review initiated by | Planner |
| Reviewed commit | `53bbfd58a6038aa932d0e20bac91f757546c5f2e` |
| Docs branch | `docs/step-09-design` |
| Review date | 2026-11-14 |
| Reviewer | Independent planning auditor (fresh context; no shared conversation with the planner, implementor, step agent, or orchestrator) |

Step page: [`Step 09 — Parameterization Templates`](../steps/step-09-parameterization-templates.md)

> Orchestration: the step agent or planner (the orchestrator) freezes the design, requests the reviewer, applies
> fixes autonomously on the design docs or a maintenance branch, adds resolutions without altering reviewer
> text, and requests re-review until the reviewer no longer identifies issues. The human owner approves the
> final verdict and the start of implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 08 |
| Delivers | Project-resident, sparse parameterization templates with stable compiled identities, a values-only parameterization signature, deterministic apply/defaulting, site-energy overrides, `PRM` validation, diff/rebase reports, and completeness checks. |

## Design decisions reviewed

- [x] Step page design decisions — reviewed; scope, API, persistence, identity, validation, rebase, and tests are coherent in outline, but M1 (signature reproduction), M2 (no-template build), M3 (project-format contract), M4 (sizing arithmetic), and M5 (chiral fixture) require changes.
- [x] Data contracts and format changes — reviewed; the template JSON shape and the `signatures.parameterization` finalization are defined, but the project-format manifest/schema is not updated (M3), the signature-vector source is not planned (m3), and the `energyEv`/`frequencyHz` JSON names are not bound by the record snippets (m8).
- [x] Architecture and module boundaries — reviewed; Core/Contracts/Packages ownership, `Contracts -> Compilation`, and the parameterization-free `Compilation` rule hold; the planned public `CanonicalProjectionWriter` change is a Compilation API change, not parameterization semantics (see n4).
- [x] ADRs and open decisions — reviewed; ADR 0010 domain separation and quantization are followed, but ADR 0010's pre-release finalization exception covers only `signatures.model`; finalizing `signatures.parameterization` needs the same explicit decision-note statement (see the note below and the Versioning checklist item).
- [x] Quality strategy and test plan — reviewed; property/golden/limits tests are strong, but the chiral fixture is misnamed/absent (M5), the model-projection checker glob conflicts (m2), and the signature-vector manifest source is missing (m3).
- [x] Parity matrix and documentation plan — reviewed; the §6 rows and named pages/nav are present; Step 26's expected versioned template exchange is not reconciled (m12).
- [x] Licensing and provenance — reviewed; no new dependency or GPL-derived content is introduced, and the MIT/SPDX obligation is recorded.
- [x] Post-implementation audit plan — reviewed; the closeout feature records the feature log, parity, docs, `verify.sh all`, closeout CI, and the audit request; `docs/planning/closeout/step-09.json` is not named (n3).
- [x] Ownership: template/value records in Core, `ParameterizationValues` + signature in Contracts, factory/apply/validation/rebase in Packages, `Mocassin.Compilation` unchanged and parameterization-free. — Verified against the project references and the implemented compiler; the only Compilation change is the planned public canonical writer, which is not parameterization semantics (n4).
- [x] Persistence: templates are project-resident in `parameterizations`; `ParameterizationId` is UUIDv7; versioning is the project `schemaVersion`; canonical ordering and no separate template document. — Verified in `Templates.cs`, `ProjectJson`, and the empty sample arrays; the project-format manifest entry is the gap in M3.
- [x] Sparse/default semantics: absence means unset; pair/group `0 eV`, rule `1e13 Hz`, site background from authored model values; explicit defaults are real entries; creation writes set headers only. — Coherent with the implemented compiler defaults; `PRM006`/intentional-zero wording conflicts (m4) and the factory summary has no project (m5).
- [x] Identity: pair `EnergyId`; group `ClusterEnergyId` + canonical occupation tuple; rule `RuleGeometryId` inside `(transitionKind, transitionId)`; site override `(siteId, particleId)`; geometry-change behavior and chiral one-value/symmetric-table behavior. — Verified against `EnergyGeometrySampler`, `ClusterCompilationStage`, and `TransitionRuleHelpers`; group nearest matching lacks a geometry criterion (m6).
- [x] Parameterization signature: values-only six-field projection, dense compiled order, 12-decimal quantization, excluded runtime settings, manifest finalization, and Step 11/13 consumption. — Layout is specified, but the `.msl` transition payload cannot reproduce the absolute rule frequencies (M1); signature-vector source (m3).
- [x] Create/apply API: factory, `IParameterizationService`, `ParameterizedModel`, immutability, mismatch rejection, defaulting, and the site-energy background. — API shape is coherent; a null template rejects the no-template build (M2) and the factory summary has no project (m5).
- [x] Site-energy overrides: background mapping to `[position][particle]`, reset-to-model, Step 15 consumption, and invalid-target behavior. — Mapping/reset semantics are coherent; the `PRM008` warning/severity boundary is ambiguous (m9) and the Step 08 page still describes the removed background (m10).
- [x] Validation: `PRM` family ownership, Core structural vs Packages compiled-bound checks, catalog composition, paths, no Step 09 `IValidationStage`, and the Step 12 integration. — Split is justified and `ValidationContext` has no compiled model; `PRM001` diff is unavailable from `Validate` (m14) and the `PRM007` doc plan conflicts with the docs-drift test (m1).
- [x] Diff/rebase: strategies, matching rules, report content, no silent reuse, and chiral-link changes. — Strategies/no-silent-reuse are defined; group nearest matching has no geometry criterion (m6) and the composite rule key encoding is unspecified (m17).
- [x] Completeness/quality metrics and warning vs error thresholds. — Concrete thresholds; `PRM006` and intentional zero values contradict (m4).
- [x] Determinism, untrusted-template bounds, and performance targets/benchmark plan. — Determinism/limits/targets are concrete; the pair-entry limit parenthetical is arithmetically wrong (m7).
- [x] Testing: acceptance mapping, real compiled models, golden/projection fixtures, property tests, freshness, and tiers. — Real models/goldens/property tests are named; the checker wiring (m2), vector source (m3), and chiral fixture (M5) need changes.
- [x] Feature list and sizing: seven features, dependencies, honest 3.4–4.4 person-week estimate, recorded 09A/09B split option, and owner decision required before start. — Seven features/dependencies are coherent; the day estimates sum to 17–22 days (3.4–4.4 pw), so the exact "below four weeks" claim is not supported (M4).
- [x] Parity and documentation: Step 09 rows, published pages, navigation, and diagnostics anchors. — Rows/pages/nav are planned; the `PRM007` anchor conflict (m1) and the Step 26 exchange expectation (m12) remain.

## Dependency and interface checks

- [x] Dependencies are `Done` and their contracts are stable — Step 08 is `Done` (`status.md`; merge `f91fc63`, tag `step-08`), and its `CompiledModel`/energy/transition/structure APIs are implemented and referenced correctly by the design.
- [x] Inputs, outputs, types, and formats are defined — template records, `ParameterizationValues`, `ParameterizedModel`, the six projection fields, and the payload mapping are defined; M1 (frequency), M3 (project manifest), and m8 (JSON names) remain.
- [x] Versioning and migration behavior is defined — project `schemaVersion` stays 1 because no fixture/sample populates the stub; ADR 0010's model-only pre-release finalization does not explicitly cover the parameterization layout, so the design should state that basis explicitly.
- [x] No unresolved cross-step interface question remains — M1 (Step 11/13 reproduction), M2 (Step 11/12 no-template build), m11 (Step 10), m12 (Step 26), and m13 (Step 12 warnings) are open; the rest of the cross-step contracts are aligned.
- [x] Step 08 is `Done`; the design binds to the implemented `CompiledModel`, `EnergyId`, `ClusterEnergyId`, `RuleGeometryId`, `CompiledJumpRule.FrequencyHz`, site stability, and position masks. — Verified member-by-member in `Mocassin.Compilation`; names and types match.
- [x] The implemented `CompiledEnergyModel` has no site-energy background; Step 09 ownership of the `defect_background` is consistent with the data contract §3.3 and Step 11. — Verified there is no `SiteEnergies`/`defect` member in `Mocassin.Compilation`; Step 11's page now assigns the table to Step 09. The Step 08 page still has stale §10/§12 text (m10).
- [x] `Mocassin.Contracts` references `Mocassin.Compilation`; `Mocassin.Compilation` has no Contracts/Packages/parameterization dependency. — Verified in the `.csproj` files; the planned public writer is a visibility/API change only.
- [x] Step 10 receives `ParameterizedModel`; Step 11 maps `CompiledModel` + `ParameterizationValues`, writes both signatures, `frequency_factor`, and `defect_background`. — Step 11 is aligned; Step 10's page is not updated (m11), and the absolute-frequency part is M1.
- [x] Step 12 owns `compile`, `build`, the Build-stage adapter, and exit codes; no `mocassin parameterize` command is added. — Verified exit 4 and the validate/apply call; the no-template build path (M2) and build-level warnings-as-errors (m13) are open.
- [x] Step 13's Rust reader can re-encode the values-only parameterization projection from the payload (`model_signature` + value sections) without persisted rule geometry IDs. — Not as specified: the payload stores relative `frequency_factor` but the projection signs absolute Hz (M1).
- [x] `contracts/manifest.json` `signatures.parameterization` finalization, generated descriptor, golden `.bin` vectors, and the Python checker are explicitly planned. — The signature entry/descriptor/checker are planned; the project-format manifest (M3) and the `fixtures.signatureVectors` source (m3) are not.
- [x] `ProjectStore` remains the only project persistence owner; no separate template file or legacy format is introduced. — Verified `ProjectStore`/`ProjectJson` ownership and the atomic writer; no sidecar is planned.
- [x] Step 26 owns editor UI, bulk fill, and import/export; Step 09 exposes library operations only. — Verified the library-only scope; Step 26's "JSON template exchange with schema version" expectation needs reconciliation (m12).
- [x] `ValidationContext` has no `CompiledModel`, so the no-`IValidationStage` decision is justified and tested. — Verified `ValidationContext` has Project/Options/Catalog/PreviousStages only; the test inventory's stale-template test covers the boundary indirectly, but no test explicitly asserts the absence of a Step 09 stage.

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable — Explicit and mostly testable; M2 (no-template build), M4 (sizing), and M5 (chiral fixture) weaken the acceptance set.
- [x] Design decisions are complete and internally consistent — Material contradictions remain: M1 (signed absolute Hz vs stored factor), M2 (null template vs sample builds), M3 (template fields vs project schema), plus m4–m6/m8/m9/m17.
- [x] Required tests, property tests, golden fixtures, and performance targets are defined — Strong families and targets; the chiral fixture (M5), projection checker (m2), and signature-vector source (m3) need changes.
- [x] Documentation, status, parity, and audit updates are planned — Pages, parity rows, nav, and the closeout/audit request are present; the closeout evidence file is unnamed (n3), and the Step 26 exchange is unaligned (m12).
- [x] Licensing, provenance, and dependency checks are resolved — No new dependency/GPL content; `verify.sh policy` passes the licence/vulnerability/ApiCompat gates.
- [x] Risks and mitigations are realistic — Main scientific/identity risks are covered; the table omits the frequency-reproduction (M1) and no-template-build (M2) risks and the project-schema drift (M3).
- [x] Code layout and namespace plan follow the structure policy — `Mocassin.Core/Model` + `Editing`, `Mocassin.Contracts/Parameterization`, `Mocassin.Packages/Parameterizations`, and the benchmark folder use permitted concept/primary-type files and stay within the folder/depth limits.
- [x] No hidden scope, stale placeholders, or unowned decisions — No step branch or implementation exists; however M3, m10–m13, m15, and m17 are unowned/stale cross-step details that must be closed before implementation.

## Evidence

Files reviewed:

- `AGENTS.md` §3.8–3.9 (reviewer scope, design-review gate), §3.10–3.15, §5 (dependency direction), §6, §7.2, §8 (code structure), §9, §10, §11 (format-change rules), §12, §13, §16; `docs/planning/reviews/README.md` and the request record.
- Steps 05, 08, 09 (the reviewed design), 10, 11, 12, 13 and 26 pages; the frozen design delta `1ee5a2a^..1ee5a2a` and the request delta `1ee5a2a..22fd839`.
- `docs/planning/01-target-architecture.md` §3; `02-data-contracts.md` §1.1–§1.3, §2.3, §3.2–§3.4; `03-quality-strategy.md` §9 and §9.8; `04-roadmap.md` §3.1; `05-open-decisions.md`; `appendix-b-parity-matrix.md` §6; `status.md`.
- ADRs 0009, 0010, 0011, 0013; `docs/development/architecture.md`, `compilation-pipeline.md`, `design-decisions.md`, `code-structure.md`.
- `contracts/manifest.json`; `contracts/generated/signatures/{model,parameterization,lattice}-v1.json`; `contracts/generated/schema/project-v1.schema.json`; `tools/contracts/generate.py`, `check_model_projection.py`, `README.md`; `tests/contracts/projections/*`, `signature-vectors.json`, `project-*.mocprj`.
- Implemented code: `Mocassin.Core/Model/Templates.cs`, `Ids/TypedIds.cs`, `Project/{Project,ProjectJson,ProjectValidator,ProjectCanonicalizer,ProjectHasher,ProjectMigrations,ProjectStoreOptions}.cs`, `Diagnostics/{CoreDiagnosticCatalog,DiagnosticSeverity,ModelPath}.cs`, `Editing/ProjectEditor*.cs`; `Mocassin.Compilation/Compiler/{CompiledModel,CompilationOptions}.cs`, `Energy/{CompiledEnergyModel,CompiledClusterTable,PairCompilationStage,ClusterCompilationStage}.cs`, `Structure/{CompiledStructureModel,CompiledParticleTable}.cs`, `Transitions/{CompiledTransitionModel,TransitionRuleHelpers}.cs`, `Signatures/{CanonicalProjectionWriter,ModelSignatureProjection}.cs`; `Mocassin.Symmetry/Energy/EnergyGeometrySampler.cs`; `Mocassin.Contracts/Mocassin.Contracts.csproj`; `Mocassin.Packages/Projects/Validation/{ValidationContext,DiagnosticCatalog,ProjectValidationService}.cs`.
- Tests/read-only reference: `Mocassin.Core.Tests/Diagnostics/CoreDiagnosticCatalogTests.cs`, `ProjectSchemaTests.cs`; `Mocassin.Packages.Tests/Projects/Validation/DiagnosticDocumentationTests.cs`; `Mocassin.Symmetry.Tests/Energy/EnergyPreviewTests.cs`; `samples/{ceria,simulations,barium-zirconate}`; `docs/reference/diagnostics.md`, `energy.md`, `project-format.md`; `docs/user-guide/energy.md`; `zensical.toml`; `.github/workflows/benchmark.yml`; `/workspace/mocassin-original` `StablePairInteraction.cs` and `ParticleInteractionPair.cs` (read-only, for the chiral/stable-pair semantics only).

Commands run:

- `./tools/verify.sh docs --require` — **passed**. `git diff --check` clean; documentation links OK (153 files); Zensical strict build: "No issues found" (0.30 s); DocFX: "Build succeeded", 0 warnings, 0 errors (301 files); rustdoc generated; "Closeout gate completed with required checks enabled."
- `./tools/verify.sh policy --require` — **passed**. NuGet licence policy OK (51 packages); no vulnerable packages; ApiCompat found no breaking changes for all baselines (it printed unresolved `MessagePack.dll` reference notes for `Mocassin.Compilation` and `Mocassin.Packages`, then "Public API is compatible with the checked-in baseline"); contract artifacts current (18 files); model projection check OK (5 fixtures); space-group catalog current (274 settings, sha256 `1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395`); process consistency OK; 18 tool tests OK; SPDX headers in 411 files; no secrets in 739 files; cargo-deny advisories/bans/licenses/sources ok; "Closeout gate completed with required checks enabled."
- `git diff --stat 1ee5a2a^..1ee5a2a` — 13 files changed (the design); `git diff --stat 1ee5a2a..22fd839` — 2 files (`docs/planning/reviews/step-09.md` and one step-page status line). The reviewed design is `1ee5a2a`; the request commit `22fd839` is a documentation-only request/status delta, recorded here per `reviews/README.md` §8.1.
- `git status --short` — clean before this record; `git worktree list` — `/workspace/mocassin` (main) and this worktree; `git branch -a` — only `docs/step-09-design`, `main`, `origin/main` (no step/feature branch).
- Repository probes: `grep`/`sed` over the compiler, Core project, manifest/schema, checker, tests, and samples; a Python round-trip probe under `/tmp/opencode/step09-review` (not committed) showing 6,012/100,000 random absolute frequencies do not survive a factor×max round-trip at 12-decimal quantization (e.g. `16413554695562.09 Hz` → factor `0.021447147469625793` → `16413554695562.092`).

## Findings

| ID | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| M1 | **Major** | The parameterization signature cannot be reproduced from the `.msl` payload as specified. The signed `transitionRules` field is the effective **absolute** frequency in hertz, but the `.msl` `transitions.jump_rules` contract stores only a relative `frequency_factor`; `max_attempt_frequency_hz` is a nullable **job** setting and is excluded from the model signature. No model-level absolute frequency or normalization is persisted, so Step 11's `verify` and Step 13 cannot re-encode the six-field projection from the payload as ADR 0010 requires. A factor×max reconstruction is not exact (probe: 6,012/100,000 random values differ after 12-decimal quantization). Fix by persisting the applied absolute rule frequency in the model payload (and updating data contract §3.3/Step 11), or by defining a documented model-level normalization that is itself signed. | Step 09 §6.1 (`transitionRules` absolute Hz), §6.2 (Step 13 re-encode), §5 Step 11 consumption; `02-data-contracts.md` lines 917 (`frequency_factor`), 927–929, 977 (`max_attempt_frequency_hz` in the job payload); ADR 0010 lines 45–46; Step 13 lines 113 and 219; `/tmp/opencode/step09-review` round-trip probe. |
| M2 | **Major** | No unparameterized/default build path is defined. `IParameterizationService.Apply` takes a non-null `ParameterizationTemplate`, and §5 rejects null with a programmer exception; `ParameterizationFactory.Create` is the only producer. The ceria, ceria-kmc, and BaZrO3 samples all have `parameterizations: []`, yet Step 11's acceptance builds a package from ceria and Step 12's acceptance runs `mocassin build samples/ceria`; Step 12's page requires "exactly one usable template" otherwise. Define the implicit empty template/default-value path (and its summary/`PRM003`/`PRM006` behavior) or explicitly make a template mandatory and update the Step 11/12 acceptance criteria. | Step 09 §5 lines 478–485, 549–553; `samples/ceria/ceria.mocprj`, `samples/simulations/ceria-kmc.mocprj`, `samples/barium-zirconate/barium-zirconate.mocprj` (`parameterizations: []`); Step 11 acceptance line 209; Step 12 lines 65 and 207. |
| M3 | **Major** | The project-format contract is not updated for the extended template. `contracts/manifest.json` `formats.project.fields.parameterizations.itemFields` still lists only `id`, `name`, `modelSignature`, and `createdUtc` with `itemAdditionalProperties: false`, and the generated `project-v1.schema.json` therefore rejects `stablePairSets`, `unstablePairSets`, `groupSets`, `siteEnergyOverrides`, and `transitionRuleSets`. The design adds those fields to the on-disk template but never plans the manifest project-format update, regeneration, or adding the new `samples/parameterizations/*.mocprj` fixtures to the hardcoded `ProjectSchemaTests.fixturePaths`. Without this, the new fixtures either fail the schema contract or are untested, and the manifest stops being the single source. | `contracts/manifest.json` project/parameterizations descriptor; `contracts/generated/schema/project-v1.schema.json` (`additionalProperties: false`); `src/dotnet/Mocassin.Core.Tests/ProjectSchemaTests.cs` fixture list; Step 09 §3.2/§3.5 and feature 1. |
| M4 | Minor | The sizing arithmetic does not support the "below four weeks" claim. The feature estimates are 3–4 + 3 + 2 + 3–4 + 2–3 + 2–3 + 2–3 = 17–22 days, i.e. 3.4–4.4 person-weeks at five days/week, while §1 says "3–4 person-weeks ... below the four-week split trigger". Adjust the day estimates or record the 4.4 pw upper bound and the split decision so roadmap §3.1 is auditable. | Step 09 §1 lines 92–98 and feature table lines 1058–1059. |
| M5 | Minor | The chiral acceptance test names a fixture that does not exist as a compiled project. §11.1 says "Compile a real chiral model (P4mm/P1 fixture ... the Symmetry P1 chiral fixture is the oracle)", but the only chiral oracle is `EnergyPreviewTests.P4mmMultiImageOrbitLinksReciprocalChiralPair`, which builds an in-memory `ModelDefinition` (private `CreateModel`), and there is no P1 chiral project in `samples/` or `tests/contracts/`. BaZrO3 is a real project with 24 chiral pairs (`BariumZirconateHighSymmetryPairsAreReciprocal`); name it (or add a checked-in P4mm/P1 project) and add it to the golden list. | Step 09 line 1003; `Mocassin.Symmetry.Tests/Energy/EnergyPreviewTests.cs` lines 203–255 and 260–285; no chiral sample/fixture. |
| m1 | Minor | `PRM007` retired documentation conflicts with the exact docs-drift test. The design plans `PRM007` rows/anchors and a retired note in `docs/reference/diagnostics.md` while keeping the code unregistered. `DiagnosticDocumentationTests.ReferencePageMatchesTheCatalogExactly` collects every `\b[A-Z]{3,4}\d{3}\b` token on the page and asserts set equality with `DiagnosticCatalog.Default`; any `PRM007` token on that page makes the documented set larger and fails. Document the retirement elsewhere (or change the test in a separately owned task) and do not add a PRM007 token to the reference page. | Step 09 lines 744–746; `DiagnosticDocumentationTests.cs` lines 16–24 and 38; `docs/reference/diagnostics.md` currently has no PRM tokens. |
| m2 | Minor | Projection-fixture glob conflict. `tools/contracts/check_model_projection.py` re-encodes every `tests/contracts/projections/*.bin` with the `model-v1` descriptor and expects its metadata `descriptor == "model-v1"`. Adding `parameterization-*.bin/.json` to that folder (§11.2) makes the model checker fail (descriptor mismatch/canonical bytes differ) before the new checker runs. Put parameterization fixtures in a subdirectory or filter the model checker in the same feature and record the `verify.sh contracts` change. | `check_model_projection.py` lines 23 and 94–119; Step 09 §11.2 lines 1026–1036. |
| m3 | Minor | The signature-vector source is not planned. `tests/contracts/signature-vectors.json` is generated by `tools/contracts/generate.py` from `contracts/manifest.json` `fixtures.signatureVectors.vectors`, and `generate.py --check` requires byte equality. The design adds "entries" to the file but does not say to add the parameterization vectors to the manifest fixture section and regenerate; a hand-edit fails the gate. | `generate.py` lines 33–34, 560–575, 618–630; Step 09 line 1032. |
| m4 | Minor | `PRM006` and intentional zero values contradict. §5/§9 say `PRM006` warns when "no explicit value differs from its default" (summary `AllValuesDefault`), but §11.1 says "zero explicit values do not trigger `PRM006` if they are intentional". An explicit `0 eV` is a real entry equal to the default, so the stated rule fires. Define `PRM006` as "no explicit entries" (or exclude explicit defaults) and make the test and summary field agree. | Step 09 lines 271–273, 582, 726, 902, 1005. |
| m5 | Minor | Factory summary vs project background. `ParameterizationFactory.Create(CompiledModel, options)` returns a summary with "effective-default ranges" and site metrics but has no `Project`; `Describe(Project, CompiledModel, ...)` does. The claim that the two summaries are identical cannot hold for site-energy effective values. Either pass the project/background to `Create` or define the factory summary as excluding site-background effective ranges. | Step 09 lines 430–452, 494–498, 890–902. |
| m6 | Minor | `NearestGeometry` group matching has no geometry acceptance criterion. The strategy text requires "the same logical descriptor and acceptable distance", but the group bullet matches only `ClusterId`, centre site, and the canonical occupation tuple; a cluster whose offsets changed (new `ClusterEnergyId`) would be copied into without a geometry check. Add an explicit unchanged-offset/hash or tolerance criterion, as the pair rule has. | Step 09 lines 860–870 and 875–879. |
| m7 | Minor | Pair-entry limit arithmetic is wrong. `ParameterizationLimits` says 4,096 entries per set with the parenthetical "63² = 3,969 fits", but Step 03 allows 63 custom particles plus the implicit void, so a full table is 64² = 4,096. Clarify the maximum `nParticles` including void and the exact bound. | Step 09 line 948; Step 03 line 62/169; `ParticleModel.MaxCustomParticles = 63`; `CompiledParticleTable` index 0 is void. |
| m8 | Minor | JSON names `energyEv` and `frequencyHz` are not bound by the record snippets. The sample JSON uses `energyEv`/`frequencyHz`, and the existing `SiteEnergyEntry` needs `[property: JsonPropertyName("energyEv")]`; the `PairEnergyEntry`, `GroupEnergyEntry`, `SiteEnergyOverride`, and `RuleFrequencyEntry` snippets declare `EnergyEv Energy`/`FrequencyHz Frequency` with no attribute, so a literal implementation emits `energy`/`frequency` and disagrees with the data contract, fixtures, and generated schema. Add the attributes or rename the properties. | Step 09 lines 213–240 vs lines 345, 361, 368; `SiteEnergyEntry.cs`. |
| m9 | Minor | `PRM008` warning vs "programmer error" boundary. §5 says a mismatched project/model pair is a caller/programmer error, not user data, but §7.1 makes an authored background entry that does not map a `PRM008` **Warning** that is ignored. A programmer error should fail loudly (exception/`PRM001`-class error); a legitimate stale project should be described as user data and targeted by `PRM001`. Clarify the boundary and severity. | Step 09 lines 543–547, 599–601, 728, §5 step 5. |
| m10 | Minor | The Step 08 page still states the removed site-energy background. The new correction note (lines 35–38) says the implemented `CompiledEnergyModel` has pair/cluster tables only, but §10 line 510 still lists "defect/site-energy background" in `CompiledEnergyModel` and §12 line 670 still has the "Site-energy values only" invalidation row rebuilding Environments/Pairs/Clusters/Encoding. Update or mark those rows superseded. | Step 08 lines 35–38, 510, 670. |
| m11 | Minor | Step 10 is a dependent step but is not updated for `ParameterizedModel`. The design says Step 10 receives `ParameterizedModel`, but Step 10's page does not name the type or its input contract; only Steps 08/11/12/13 were edited. Record the input type in Step 10's page (or state that Step 10 keeps `CompiledModel` and reads values separately). | Step 09 lines 607–609; Step 10 line 25; no `ParameterizedModel` in Step 10. |
| m12 | Minor | Step 26 expects a versioned template exchange, but the design defers a separate document. Step 26's page plans "JSON template exchange with schema version and signature"; the design says a standalone template file is out of scope and would need its own version and an ADR. Reconcile: either Step 26 exports project fragments/embedded templates, or Step 26 owns the versioned envelope and an ADR. | Step 26 line 91; Step 09 lines 172–173 and 411. |
| m13 | Minor | Step 12's build surface has no `--warnings-as-errors`. The design says `PRM003`/`PRM006` warnings can be promoted by `--warnings-as-errors`/`WarningsAsErrors` in Step 12, and the test plan calls promotion "Step 12's policy test"; Step 12's `build` synopsis offers the flag only on `validate`. Add it to Step 12's build contract or drop the claim. | Step 09 lines 583 and 1005; Step 12 line 44 vs lines 45–54. |
| m14 | Minor | `PRM001` diff and "non-allocating" `Validate`. §7.1 says `PRM001` carries the `ParameterizationDiff` in the result, but `Validate` returns only `ImmutableArray<Diagnostic>` (the diff exists only on `ParameterizationResult`); §5 calls `Validate` a "non-allocating preflight" although it must build lookup maps and diagnostics. State that `Validate` reports the mismatch without the diff (or return a result record), and soften "non-allocating" to "no dense tables/signature". | Step 09 lines 487–492, 519–522, 754–755. |
| m15 | Minor | `ProjectEditor` template methods are described as existing. §3.7 presents `ProjectEditor.AddParameterizationTemplate`/`Update...`/`Remove...` with "the existing ProjectValidator"; the implemented `ProjectEditor` has no parameterization methods (only particles, structure, energy, lattice, simulations, transitions). Mark them as added in feature 1 and give their signatures so the design does not read as binding to a non-existent API. | Step 09 lines 403–405; `ProjectEditor*.cs`; feature 1 line 1050. |
| m16 | Minor | Canonicalization can be bypassed by `with`. §3.1 says the record constructor sorts sets/entries and normalizes `-0.0` so authoring order cannot change the document hash; `ParameterizationTemplate` exposes `init` properties, and `with` (the natural implementation of the editor operations) bypasses the constructor. Canonicalize in `ProjectJson`/`ProjectCanonicalizer` at write time (as the model path does) or make the arrays private/validated on set. | Step 09 lines 162–164, 194–202, 407–410; `ProjectJson` canonicalizes the model, not templates. |
| m17 | Minor | Diff/rebase composite key encoding is unspecified. `ParameterizationDiffEntry.Key` is a `string`, but rule sets are identified by `(TransitionKind, TransitionId)`; the design does not say how the kind and ID are encoded in one string or how a site-override key differs from a pair key. Specify the encoding (or add a structured key record) so reports and tests are reproducible. | Step 09 lines 804–843 and 840–843. |
| n1 | Note | Naming: the manifest `projectionType` is `ParameterizationSignatureV1` while the C# type is `ParameterizationSignature`, and the namespaces alternate `Mocassin.Contracts.Parameterization` and `Mocassin.Packages.Parameterizations`. Align names before the public API is frozen. | Step 09 lines 633–641 and 122–123; manifest. |
| n2 | Note | Limits/unit note: `PRM004` uses a literal `-100..100 eV` although `EnergyLimits.MinEnergy`/`MaxEnergy` already define that range; `ParameterizationQualityOptions` and `ParameterizationDiffEntry.TemplateValue` carry bare `double` Hz/eV values. Reuse `EnergyLimits` and the unit types where the API boundary matters. | Step 09 lines 533–540, 567, 804–809; `EnergyLimits.cs`. |
| n3 | Note | Audit-evidence note: the closeout plan names the feature log, parity, docs, `verify.sh all`, closeout CI, and audit request but not `docs/planning/closeout/step-09.json`; AGENTS §3.4 requires that file. Add it to the closeout feature scope. | Step 09 lines 100–104; `docs/planning/closeout/_template.json`. |
| n4 | Note | Checklist wording note: the request item says "`Mocassin.Compilation` unchanged", while §6 makes `CanonicalProjectionWriter` public and adds a public `Quantize`; the compiler stays parameterization-free, but the API change is real. Record it in the completion evidence. | Step 09 lines 679–685; `CanonicalProjectionWriter.cs`. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Re-review required

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, step agent, or orchestrator. I received only the frozen commit, the request record,
> repository links, and the required commands/scope; no desired verdict or summary was supplied. All evidence is
> from reading the repository at `22fd839` and the frozen design at `1ee5a2a`.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

Follow-up owner: Step 09 planner/step agent
Follow-up step: Step 09 design re-review (round 2)

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

Resolved by the Step 09 planner on 2026-11-14; design freeze
`53bbfd58a6038aa932d0e20bac91f757546c5f2e`. The reviewer-authored findings, evidence, and verdict above are
unchanged.

Summary of the substantive choices:

- **M1:** the model payload now stores each rule's absolute `frequency_hz`; `frequency_factor` is a Step 16
  per-job derivation, not a model field. Step 11 `verify` and Step 13 re-encode the projection from the stored
  absolute values exactly. Updated: data contract §3.3, Steps 08/09/11/13/16, quality strategy.
- **M2:** a nullable template selects the implicit default (`ParameterizationTemplateDefaults.CreateDefault`);
  projects with `parameterizations: []` build with defaults. The implicit default suppresses `PRM003`/`PRM006`;
  an explicit empty template emits both. Step 11/12 acceptance and selection rules updated.
- **M3:** feature 1 owns the `contracts/manifest.json` project-field update, schema regeneration, new
  `samples/parameterizations/*.mocprj` fixtures, and the `ProjectSchemaTests.fixturePaths` change.
- **M4:** the honest estimate is 17–22 days (3.4–4.4 pw); the upper bound exceeds the four-week split trigger.
  The step page records a 09A/09B split option and requires the human owner to choose before step start.
- **M5:** the chiral test uses the real barium-zirconate model (24 reciprocal chiral pairs); a generated
  P1/P4mm project fixture covers the link-change case.

| Finding | Status | Resolution | Commit |
| --- | --- | --- | --- |
| M1 | Resolved | Model payload stores absolute `frequency_hz`; no factor × maximum reconstruction; Steps 08/11/13/16 and data contract updated. | `53bbfd5` |
| M2 | Resolved | Nullable template + implicit default path; samples with no templates build; `PRM003`/`PRM006` behavior defined; Steps 11/12 acceptance updated. | `53bbfd5` |
| M3 | Resolved | Manifest project fields, generated schema, sample fixtures, and `ProjectSchemaTests.fixturePaths` assigned to feature 1. | `53bbfd5` |
| M4 | Accepted — owner decision required | 17–22 days = 3.4–4.4 pw recorded; upper bound exceeds the four-week trigger; 09A/09B split option and owner decision recorded in §1. | `53bbfd5` |
| M5 | Resolved | Real BaZrO₃ chiral model (24 pairs) plus generated P1/P4mm link-change fixture and golden entry. | `53bbfd5` |
| m1 | Resolved | No `PRM007` token in `docs/reference/diagnostics.md`; retirement documented only in planning/design pages. | `53bbfd5` |
| m2 | Resolved | Parameterization projections moved to `tests/contracts/projections/parameterization/`; new checker reads the subdirectory; model glob unchanged. | `53bbfd5` |
| m3 | Resolved | Parameterization vectors added to `contracts/manifest.json` fixtures and regenerated by `tools/contracts/generate.py`; no hand-edited `signature-vectors.json`. | `53bbfd5` |
| m4 | Resolved | `PRM006` now means no explicit entries; explicit `0 eV`/`1e13 Hz` suppresses it; implicit default suppresses both `PRM003`/`PRM006`. | `53bbfd5` |
| m5 | Resolved | `ParameterizationFactory.Create` takes `Project` and returns the same summary as `Describe`; project/model pairing documented. | `53bbfd5` |
| m6 | Resolved | `NearestGeometry` is pair-only; groups/rules/site overrides require exact IDs, so changed group geometry defaults. | `53bbfd5` |
| m7 | Resolved | Pair-table bound clarified: 64 compiled particles including void (64² cells), at most 63² non-void template entries. | `53bbfd5` |
| m8 | Resolved | `[property: JsonPropertyName("energyEv")]`/`("frequencyHz")` pinned on the template entry records. | `53bbfd5` |
| m9 | Resolved | Mismatched project/model is a caller contract violation; unmapped authored background is `PRM008` Warning (stale user data); structural signature mismatch is `PRM001` Error. | `53bbfd5` |
| m10 | Resolved | Step 08 §10 no longer lists a site-energy background; the site-energy invalidation row is marked as no compiled-content change, with Step 09 ownership. | `53bbfd5` |
| m11 | Resolved | Step 10 names `Mocassin.Packages.Parameterization.ParameterizedModel` as its input and excludes parameterization values from job/lattice payloads. | `53bbfd5` |
| m12 | Resolved | Step 26 exchanges a valid `.mocprj` project fragment containing selected `parameterizations`; no separate format/version. | `53bbfd5` |
| m13 | Resolved | Step 12 `build` gains `--warnings-as-errors`; validation warnings exit 1, promoted parameterization warnings exit 4. | `53bbfd5` |
| m14 | Resolved | `Validate` returns diagnostics only (no diff), builds lookup maps but no dense tables/signature; `Apply` returns the diff on `PRM001`. | `53bbfd5` |
| m15 | Resolved | `ProjectEditor` template methods are explicitly added by feature 1 with exact signatures; the current editor has none. | `53bbfd5` |
| m16 | Resolved | Canonical ordering/signed-zero normalization is a `ProjectJson`/`ProjectCanonicalizer` write-time guarantee; `with` cannot bypass it. | `53bbfd5` |
| m17 | Resolved | Structured `ParameterizationValueKey` replaces free-form strings; pair/group/rule/site key fields and canonical entry encodings are specified. | `53bbfd5` |
| n1 | Resolved | Namespace aligned to `Mocassin.Packages.Parameterization`; Contracts signature type named `ParameterizationSignatureV1` to match the manifest projection type. | `53bbfd5` |
| n2 | Resolved | `PRM004` references `EnergyLimits`; quality thresholds use `FrequencyHz`; `TemplateValue` is documented as a display-only union with unit by kind. | `53bbfd5` |
| n3 | Resolved | `docs/planning/closeout/step-09.json` and `check_closeout_evidence.py` added to feature 7/closeout scope. | `53bbfd5` |
| n4 | Resolved | The Compilation change is recorded as a public visibility/`PublicAPI`/ApiCompat change for `CanonicalProjectionWriter`/`Quantize`; no parameterization semantics or dependency edge. | `53bbfd5` |

The step agent/planner fills resolutions after the verdict without altering the reviewer's findings, evidence,
or verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

Fresh planning auditor with no shared conversation with the planner, implementor, step agent, or orchestrator. Reviewed the frozen design at `1ee5a2a3f5b6e34b44a1881e46390700e962f6b3` and the repository at the request tip `22fd839`; the delta `1ee5a2a..22fd839` is the request record plus one step-page status line, so it is a documentation-only request/status delta. The worktree was clean before this record; no step/feature branch exists and `docs/planning/audits/step-09.md` is absent.

#### Commands and results

`./tools/verify.sh docs --require` — passed (links OK for 153 files, Zensical strict clean, DocFX 0 warnings/0 errors, rustdoc generated). `./tools/verify.sh policy --require` — passed (licence policy 51 packages, no vulnerable packages, ApiCompat compatible, contract artifacts current, model projections OK for 5 fixtures, space-group catalog current, process consistency OK, 18 tool tests, SPDX 411, no secrets 739, cargo-deny ok). The ApiCompat run printed unresolved `MessagePack.dll` reference notes for `Mocassin.Compilation`/`Mocassin.Packages` but reported no breaking changes. See `Evidence` for repository probes.

#### Findings

Three Major findings (M1 signature reproduction from the `.msl` payload; M2 no unparameterized/default build path; M3 project-format manifest/schema not updated), two Minor (M4 sizing arithmetic, M5 chiral fixture), seventeen Minor (m1–m17), and four Notes (n1–n4); full statements and evidence are in the `Findings` table above. No Blocker was found.

#### Verdict

**Changes required** — M1–M3 must be corrected in the design docs, and M4–M5 and m1–m17/n1–n4 should be resolved or explicitly accepted, before a `Ready for implementation` verdict can be considered.
