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
| Reviewed commit | `26d6a0ecff543ac65eb60d60d9f9c893e00f32bc` |
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

**State:** Ready with notes

> Round-2 state is `Ready with notes`: no Blocker or Major remains; R2-1..R2-3 are Minor and R2-4..R2-6 are
> Notes, all requiring explicit human-owner acceptance before implementation starts. Round-1 findings, evidence,
> and verdict are unchanged below.
>
> Round-3 state is `Ready with notes`: no Blocker or Major remains; one Minor (R3-1) and the orchestrator-owned
> dashboard update (R2-5) require explicit human-owner acceptance before implementation starts. Rounds 1–2
> reviewer text is unchanged.
>
> Round-4 state is `Ready with notes`: R3-1 is resolved and no design finding remains open; the only outstanding
> item is the orchestrator-owned dashboard update (R2-5), which the human owner must accept explicitly.
> Rounds 1–3 reviewer text is unchanged.

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

- [x] Requested decision and frozen commit recorded: accept the round-4 `Ready with notes` verdict for design
  freeze `26d6a0e`, merge `docs/step-09-design` into `main`, and start Step 09 with the approved seven-feature
  list. The single-step 3.4–4.4 person-week exception and the M1/M2 format decisions were approved by the
  human owner on 2026-11-14.
- [x] Every review/audit round recorded (round, date, commit, reviewer, verdict).
- [x] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status).
- [x] Accepted notes and waivers recorded with rationale.
- [x] Residual risks, deferred items, and follow-up owners recorded.
- [x] Explicit no-Blocker/no-Major statement: no Blocker and no Major remains open after round 4.
- [x] Final CI and documentation-build results recorded: `docs --require` and `policy --require` pass at the
  design freeze and at the record tip; no CI run was used because every commit is documentation-only.

Round history:

| Round | Date | Commit | Reviewer | Verdict |
| --- | --- | --- | --- | --- |
| 1 | 2026-11-14 | `1ee5a2a` | Fresh design reviewer A | Changes required (M1–M3 Major; m1–m17, n1–n4) |
| 2 | 2026-11-14 | `53bbfd5` | Fresh design reviewer B | Ready with notes (R2-1–R2-6) |
| 3 | 2026-11-14 | `b5db289` | Fresh design reviewer C | Ready with notes (R3-1) |
| 4 | 2026-11-14 | `26d6a0e` | Fresh design reviewer D | Ready with notes (no new findings; R3-1 resolved) |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| M1 | Major | Parameterization signature not reproducible from the `.msl` payload | Store each rule's absolute `frequency_hz`; contract and Steps 08/11/13/16 updated | `53bbfd5` | Resolved |
| M2 | Major | No unparameterized/default build path | Implicit default template for `parameterizations: []`; Step 11/12 acceptance updated | `53bbfd5` | Resolved |
| M3 | Major | Project-format manifest/schema not updated | Feature 1 owns manifest fields, schema regeneration, sample fixtures, and `ProjectSchemaTests` | planned (feature 1) | Resolved as planned work |
| M4 | Minor | Sizing claim contradicted the four-week trigger | 3.4–4.4 person-weeks recorded; single-step exception approved 2026-11-14 | `b5db289` | Owner decision recorded |
| M5 | Minor | Chiral acceptance named a non-existent fixture | Real barium-zirconate model plus generated P1/P4mm link-change fixture; 24 chiral geometries (12 reciprocal pairs) | `53bbfd5` | Resolved |
| m1–m17 | Minor | Consistency, API-binding, fixture, and validation findings | All resolved; m4 closed by R2-2 | `53bbfd5`, `b5db289` | Resolved |
| n1–n4 | Note | Naming, unit-type, closeout-evidence, and API-change notes | All resolved | `53bbfd5`, `b5db289` | Resolved |
| R2-1 | Minor | Parity row still assigned the per-job factor to Step 11 | Step 11 writes absolute `frequency_hz`; Step 16 derives the factor | `b5db289` | Resolved |
| R2-2 | Minor | `PRM006` catalogue row used the superseded rule | Explicit-entries rule; explicit defaults suppress it | `b5db289` | Resolved |
| R2-3 | Minor | Chiral count wording conflated geometries and pairs | "24 chiral geometries (12 reciprocal pairs)" consistently | `b5db289` | Resolved |
| R2-4 | Note | Single-step option had no roadmap bookkeeping | Owner exception recorded in roadmap §3.1 and the Step 09 row | `b5db289` | Resolved |
| R2-5 | Note | Dashboard still reported the design review as `Not requested` | Orchestrator updates the dashboard at acceptance | this commit | Resolved |
| R2-6 | Note | Step 13 acceptance less explicit than the Step 09 claim | Explicit re-encoding acceptance bullet added | `b5db289` | Resolved |
| R3-1 | Minor | No ADR-0010-scoped authorization for the parameterization signature finalization | ADR 0010 pre-release finalization now covers both signatures | `26d6a0e` | Resolved |

Accepted notes and waivers:

- The human owner accepted the single-step 3.4–4.4 person-week exception to roadmap §3.1 on 2026-11-14; the
  09A/09B split is recorded as considered and rejected.
- No Blocker or Major is waived; none remains.

Residual risks and follow-ups:

- The 4.4 person-week upper bound makes Step 09 a larger audit sitting; the audit must still be completable in
  one sitting, and each feature stays independently auditable.
- The absolute `frequency_hz` model payload is a pre-release `.msl` change authorized by ADR 0010; no released
  format exists yet.
- Projects without a template build with the implicit default; explicit empty templates warn.
- Cross-language Rust reproduction is deferred to Step 13's acceptance, which now states it explicitly.
- The design branch is not yet merged to `main`; the step branch is created from the reviewed `main` commit.

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
- **M5:** the chiral test uses the real barium-zirconate model (24 chiral geometries, i.e. 12 reciprocal pairs); a generated
  P1/P4mm project fixture covers the link-change case.

| Finding | Status | Resolution | Commit |
| --- | --- | --- | --- |
| M1 | Resolved | Model payload stores absolute `frequency_hz`; no factor × maximum reconstruction; Steps 08/11/13/16 and data contract updated. | `53bbfd5` |
| M2 | Resolved | Nullable template + implicit default path; samples with no templates build; `PRM003`/`PRM006` behavior defined; Steps 11/12 acceptance updated. | `53bbfd5` |
| M3 | Resolved | Manifest project fields, generated schema, sample fixtures, and `ProjectSchemaTests.fixturePaths` assigned to feature 1. | `53bbfd5` |
| M4 | Accepted — owner decision required | 17–22 days = 3.4–4.4 pw recorded; upper bound exceeds the four-week trigger; 09A/09B split option and owner decision recorded in §1. | `53bbfd5` |
| M5 | Resolved | Real BaZrO₃ chiral model (24 chiral geometries, i.e. 12 reciprocal pairs) plus generated P1/P4mm link-change fixture and golden entry. | `53bbfd5` |
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

### Round 2 resolution (2026-11-14)

**Owner decisions recorded durably:**

- **R2-4 / M4:** Step 09 proceeds as a single step with a recorded exception. The honest estimate is
  3.4–4.4 person-weeks; the upper bound exceeds the four-week split trigger. The 09A/09B split was considered
  and rejected. Recorded in the Step 09 page “Owner decisions (2026-11-14)”, §1, and
  `docs/planning/04-roadmap.md` §3.1 plus the Step 09 estimate row.
- **M1:** the absolute `frequency_hz` model-payload design is confirmed (pre-release `.msl` change; Step 11 not
  implemented). Recorded in the Step 09 owner decisions and §6.1.
- **M2:** the implicit default build path for projects with `parameterizations: []` is confirmed. Recorded in
  the Step 09 owner decisions and §5.1.
- **Feature list:** the seven-feature list is approved 2026-11-14. The step remains `Not started`; no step
  branch was created.

| Item | Status | Resolution | Commit |
| --- | --- | --- | --- |
| R2-1 | Resolved | Parity row now reads “Step 11 writes absolute `frequency_hz`; Step 16 derives the per-job factor”. | `b5db289` |
| R2-2 | Resolved | `PRM006` row now reads “No explicit entries at all (`HasExplicitValues == false`) for an explicit template; suppressed for the implicit default”; explicit defaults suppress it. | `b5db289` |
| R2-3 | Resolved | Step 09, quality strategy, and the M5 resolution use “24 chiral geometries (12 reciprocal pairs)”. | `b5db289` |
| R2-4 | Owner decision recorded | Single step approved with a 3.4–4.4 pw exception; 09A/09B considered and rejected; roadmap §3.1 and the Step 09 row updated. | `b5db289` |
| R2-5 | Orchestrator-owned | `docs/planning/status.md` is outside the planner write scope; the orchestrator updates the dashboard design-review column at acceptance/step start. No edit made here. | — |
| R2-6 | Resolved | Step 13 acceptance now explicitly requires exact re-encoding of the parameterization signature from `model_signature`, the value tables, and absolute `jump_rules[].frequency_hz`. | `b5db289` |

The round-2 reviewer text and boxes are unchanged.

### Round 3 resolution (2026-11-14)

- **R3-1:** resolved by extending ADR 0010's pre-release exception from `signatures.model` to both signed
  projections and adding the parameterization finalization basis. No released `.msl` exists; the
  `parameterization-v1` entry is finalized while unreleased and is closed with golden vectors and the Python
  checker in Step 09 feature 2. The Step 09 §6.1, data contract §1.3/§3.3, and `05-open-decisions` reference
  the same ADR home without duplicating the rule.
- **R2-5:** remains orchestrator-owned; `docs/planning/status.md` was not edited by the planner.

| Item | Status | Resolution | Commit |
| --- | --- | --- | --- |
| R3-1 | Resolved | ADR 0010 pre-release signature-layout exception now covers `signatures.parameterization`; Step 09 §6.1, data contract §1.3/§3.3, and `05-open-decisions` reference it. No released `.msl`; feature 2 closes it with golden vectors and the Python checker. | `26d6a0e` |

The round-3 reviewer text and boxes are unchanged.

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

### Review round 2

#### Independence and frozen state

I am a fresh context with no shared conversation with the planner, implementor, step agent, or orchestrator. I
did not author or implement the reviewed work, did not write round 1, and received only the frozen design
commit, the request record, repository links, and the required commands/scope; no desired verdict or summary
was supplied. All evidence below is from reading `/workspace/playground/step09-design` at the request tip
`c8359e7803ba645602b1d65c1afac8e8dbdbab9f` and the frozen design at
`53bbfd58a6038aa932d0e20bac91f757546c5f2e`.

Frozen-state checks:

- `git rev-parse HEAD` — `c8359e7803ba645602b1d65c1afac8e8dbdbab9f`.
- `git diff --name-only 53bbfd5..c8359e7` — exactly `docs/planning/reviews/step-09.md` and
  `docs/planning/steps/step-09-parameterization-templates.md`. The step-page delta is one `Design review`
  status line; the record delta is the planner's `Resolution status` plus a request-owned review-metadata and
  checklist item update.
- `git diff 57a9ddb..c8359e7 -- docs/planning/reviews/step-09.md` — no hunk in the round-1 `Findings`,
  `Evidence`, `## Reviewer independence`, `## Verdict`, or `### Review round 1` text. The planner did not alter
  reviewer-authored findings, evidence, or verdict; only the reviewed-commit metadata, one request-owned
  checklist item, the `Record state`, and the resolutions changed.
- `git status --short` — clean before this record; no step or feature branch exists; `docs/planning/audits/step-09.md`
  is absent.

I re-read every checklist item above against `53bbfd5`; all marks remain `[x]` and no item text was changed.
The round-1 notes are unchanged. Round-2 residuals are recorded only in this appended section.

#### Commands and results

Run in `/workspace/playground/step09-design`; full logs are in `/tmp/opencode/step09-review2/` (scratch, not
committed).

```sh
./tools/verify.sh docs --require
# exit 0; "Documentation links OK (153 files scanned).";
# Zensical strict: "No issues found";
# DocFX: "Applying templates to 302 model(s)..." then "Build succeeded." / "0 warning(s)" / "0 error(s)";
# rustdoc generated for every workspace crate;
# "Closeout gate completed with required checks enabled."

./tools/verify.sh policy --require
# exit 0; "NuGet licence policy OK (51 packages)."; no vulnerable packages;
# ApiCompat: "Public API is compatible with the checked-in baseline" (the only notes are unresolved
# MessagePack.dll references for Mocassin.Compilation/Mocassin.Packages, not breaking-change findings);
# "Contract artifacts are current (18 files)"; "Model projection check OK (5 fixtures).";
# "space-group catalog is current: 274 settings ..."; "Process consistency: OK";
# "Ran 18 tests ... OK"; "SPDX headers present in 411 source files";
# "No secret patterns found in 739 tracked text files";
# "advisories ok, bans ok, licenses ok, sources ok";
# "Closeout gate completed with required checks enabled."
```

Repository probes: `grep`/`sed` over the frozen design pages, data contract, manifest and generated schema,
contract tools, compiler/validator/editor code, tests, samples, roadmap, status dashboard, and parity matrix.
No code probe was needed: the round-1 factor×maximum probe is obsoleted by the absolute `frequency_hz` design,
and the remaining questions are document-consistency questions verified against the tree.

#### Resolution verification

| Item | Round-2 status | Reproduced evidence |
| --- | --- | --- |
| M1 | Resolved in the contract; two residual documentation gaps (R2-1, R2-6). | `02-data-contracts.md` lines 924 and 934–938 now persist `jump_rules[].frequency_hz` as the absolute Step 09 value and state that the per-job factor is derived, not stored; line 102 pins projection values to compiled table/rule order. Step 09 lines 765–767 and 811–817 sign the absolute values and forbid factor × maximum; Step 11 lines 111–119 says `verify` re-encodes from the payload; Step 13 lines 74–84 and 125–127 define `frequency_hz` and the exact re-encoding; Step 16 lines 38–41 and 125–128 derive the runtime factor. No model-level `frequency_factor` field remains. |
| M2 | Resolved. | Step 09 lines 543–545, 615, and 689–719 define nullable templates, `ParameterizationTemplateDefaults.CreateDefault`, the reserved empty ID, `PRM003`/`PRM006` suppression for the implicit default, and the explicit-empty case. Step 11 lines 215–216 and Step 12 lines 65–68 and 213–215 update acceptance and selection. `samples/{ceria,barium-zirconate,simulations,transitions,lattice}/*.mocprj` all contain `"parameterizations": []`. |
| M3 | Resolved as a planned, owned feature-1 change. | Step 09 lines 416–435 assign the `contracts/manifest.json` project `itemFields` update, `generate.py` regeneration, new `samples/parameterizations/*.mocprj` fixtures, `ProjectJsonContext`, and `ProjectSchemaTests.fixturePaths` to feature 1 (line 1184). The current manifest (lines 896–914) still has only `id`, `name`, `modelSignature`, and `createdUtc`; the generated schema still has `additionalProperties: false`; `ProjectSchemaTests` lines 13–35 has a hardcoded list. The design now closes exactly that gap. |
| M4 | Accepted — owner decision required (R2-4). | Step 09 lines 92–109 records 17–22 days = 3.4–4.4 pw, the 09A/09B split option, the single-step option, and the mandatory owner choice; lines 1192–1194 sum the feature estimates. Roadmap line 65 still lists 1–3 pw and §3.1 requires a roadmap update when a step exceeds the trigger. |
| M5 | Resolved in substance; count wording is wrong (R2-3). | Step 09 line 1125 compiles the real `samples/barium-zirconate/barium-zirconate.mocprj` and uses a generated P1/P4mm link-change fixture; lines 1150–1158 add both to the golden list. The sample compiles (`Mocassin.Compilation.Tests/Compiler/GoldenModelTests.cs` line 54) and the symmetry oracle asserts 24 chiral previews (`EnergyPreviewTests.cs` lines 258–283). |
| m1 | Resolved. | `docs/reference/diagnostics.md` contains no `PRM` token today; Step 09 lines 850–854 and 1209–1211 forbid a `PRM007` token and keep retirement in planning/design pages. `DiagnosticDocumentationTests` lines 16–24 still enforces exact set equality. |
| m2 | Resolved. | `tools/contracts/check_model_projection.py` line 94 uses `FIXTURES.glob("*.bin")`, which does not descend; Step 09 lines 1162–1166 place parameterization fixtures in `projections/parameterization/` and wire a separate checker; data contract line 111 records the same rule. |
| m3 | Resolved. | `tools/contracts/generate.py` lines 254–258 validate the `fixtures.signatureVectors` object and lines 566–575 regenerate `signature-vectors.json`; Step 09 lines 1159–1161 and 1185 require manifest vectors plus regeneration, with no hand edit. |
| m4 | Partially resolved — the `PRM006` table row is stale (R2-2). | Step 09 lines 647–651, 1031, and 1127 now define `PRM006` as `HasExplicitValues == false` and state that explicit `0 eV`/`1e13 Hz` suppresses it; line 832 still says “No explicit value differs from its default”, which fires for an explicit default. |
| m5 | Resolved. | Step 09 lines 485–515 make `ParameterizationFactory.Create` take `Project` and `CompiledModel` and state that its summary equals `Describe(project, model, null)`; the factory no longer has a project-less summary. |
| m6 | Resolved. | Step 09 lines 982–990 limit `NearestGeometry` to unique pair descriptors with a bucket tolerance; groups, rules, and site overrides are exact-ID only, so a changed group geometry defaults. |
| m7 | Resolved. | Step 09 line 1070 states 4,096 `64²` cells including void and at most `63² = 3,969` non-void entries, matching `ParticleModel.MaxCustomParticles = 63` and compiled particle index 0 = void. |
| m8 | Resolved. | Step 09 lines 234, 244, 249, and 258 pin `[property: JsonPropertyName("energyEv")]`/`("frequencyHz")` on every persisted entry record, matching the existing `SiteEnergyEntry.cs` line 16 and the data-contract JSON. |
| m9 | Resolved. | Step 09 lines 604–611, 668–671, and the `PRM008` row (line 834) separate a caller contract violation from stale authored background data (`PRM008` Warning) and a structural signature mismatch (`PRM001` Error). |
| m10 | Resolved. | Step 08 lines 509–513 remove the compiled site-energy background and lines 672–676 mark the site-energy-only row as no compiled-content change; the correction note at lines 35–38 assigns the table to Step 09/11. |
| m11 | Resolved. | Step 10 lines 28–32 name `Mocassin.Packages.Parameterization.ParameterizedModel` as the input and exclude parameterization values from job/lattice payloads. |
| m12 | Resolved. | Step 26 lines 91–94 make import/export a valid `.mocprj` project fragment with the existing project version/template shape, explicitly rejecting a separate format or ADR. |
| m13 | Resolved. | Step 12 adds `--warnings-as-errors` to the `build` synopsis (line 53), defines validation-stage exit 1 versus promoted parameterization exit 4 (lines 170–173), and updates acceptance/documentation (lines 213–215 and 241). |
| m14 | Resolved. | Step 09 lines 579–583 state `Validate` returns diagnostics only and builds lookup maps but no dense tables/signature; lines 862–864 put the diff on `Apply`'s result and the mismatch summary in the `PRM001` message. |
| m15 | Resolved. | Step 09 lines 452–467 explicitly say the current `ProjectEditor` has no template methods, add `Add`/`Update`/`Remove` signatures in feature 1, and add the pure `ParameterizationTemplateEditor` operations. The implemented editor has no parameterization methods. |
| m16 | Resolved. | Step 09 lines 177–181 and 411–414 make `ProjectJson`/`ProjectCanonicalizer` write-time normalization the authoritative guarantee, so `with` cannot bypass ordering or signed-zero normalization; data contract lines 655–660 records the same. |
| m17 | Resolved. | Step 09 lines 904–964 replace the free-form diff key with `ParameterizationValueKey(Kind, SetId, TransitionKind, TransitionId, EntryId, SiteId, ParticleId)` and specify the canonical entry encodings; reports and tests can compare records, not delimited strings. |
| n1 | Resolved. | Step 09 uses `Mocassin.Packages.Parameterization` (lines 136, 575, and the feature rows) and `ParameterizationSignatureV1` in `Mocassin.Contracts.Parameterization` (line 737); no plural `Parameterizations` namespace remains in the design pages. |
| n2 | Resolved. | Step 09 line 830 uses `EnergyLimits.MinEnergy`/`MaxEnergy` (`EnergyLimits.cs` lines 65/70), lines 594–601 use `FrequencyHz` thresholds, and lines 960–962 document `TemplateValue` as a display-only union whose unit is selected by `Key.Kind`. |
| n3 | Resolved. | Step 09 lines 114–117 and the feature-7 row (line 1190) add `docs/planning/closeout/step-09.json` and `python tools/check_closeout_evidence.py` to the closeout scope. |
| n4 | Resolved. | Step 09 lines 144–146 and 784–795 record the public `CanonicalProjectionWriter`/`Quantize` change with `PublicAPI` and ApiCompat evidence and no parameterization semantics or dependency edge; `Mocassin.Compilation` still references only Core and Symmetry. |

#### Round 2 findings

| ID | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| R2-1 | **Minor** | **M1 residual: the parity matrix still assigns the per-job factor to Step 11.** The row says “Absolute Hz per `RuleGeometryId`; factor written by Step 11”, which contradicts the M1 resolution: Step 11 writes the absolute `frequency_hz`; Step 16 derives the per-job pre-check/normalization factor. This is the only remaining `factor` statement outside the Step 16 derivation text. Fix the row to “Step 11 writes absolute `frequency_hz`; Step 16 derives the per-job factor.” | `appendix-b-parity-matrix.md` line 131; contrast `02-data-contracts.md` lines 934–938, Step 11 lines 111–118, Step 16 lines 38–41/125–128, and Step 09 lines 765–767. |
| R2-2 | **Minor** | **m4 residual: the `PRM006` catalogue row still uses the superseded rule.** The table says `PRM006` fires when “No explicit value differs from its default”. Under that wording an explicit `0 eV` or `1e13 Hz` is equal to its default and fires, while §5, §9, and §11.1 say an explicit default is an explicit entry and suppresses `PRM006`. Reword the row to “No explicit entries at all (`HasExplicitValues == false`) for an explicit template; suppressed for the implicit default.” | Step 09 line 832 versus lines 647–651, 1031, and 1127; quality strategy line 361. |
| R2-3 | **Minor** | **M5 residual: “24 reciprocal chiral pairs” is 24 chiral directed geometries, i.e. 12 reciprocal pairs.** The compiled counter divides chiral geometries by two, and the symmetry oracle asserts 24 chiral previews; each preview has a reciprocal partner. The acceptance assertion “compiled chiral geometry count is 24” is correct, but the “pairs” wording in the resolution and two design pages can be read as 24 shared `EnergyId`s, pair tables, or template entries. State “24 chiral geometries (12 reciprocal pairs)” consistently. | `Mocassin.Compilation/Compiler/CompilationCounts.cs` line 88 (`ChiralPairs = ...IsChiral) / 2`); `EnergyPreviewTests.cs` lines 258–283 (`Assert.Equal(24, chiral.Length)`); Step 09 line 1125; quality strategy line 362; `Resolution status` table, M5. |
| R2-4 | Note | **M4 follow-up: the single-step option has no roadmap bookkeeping.** The step page records 3.4–4.4 pw and the split option, and the owner must choose. If the owner accepts the single-step 4.4 pw option, roadmap line 65 still says 1–3 pw and §3.1 says a step that exceeds four weeks is split and the roadmap updated; the accepted exception should be recorded there (or the step-page estimate explicitly made the source of record) before implementation. | Step 09 lines 92–109 and 1192–1194; `04-roadmap.md` line 65 and §3.1 lines 93–105. |
| R2-5 | Note | **Planning-status lag: the dashboard still reports the design review as `Not requested`.** The step page and this record say a re-review is pending at `53bbfd5`. This is a status-only inconsistency; the planner/step agent should update the dashboard (or record that the design-review column is updated only at acceptance). It does not affect the design or the gate. | `status.md` line 59 row 09 (design review “Not requested”); Step 09 page line 19 (“Re-review required at `53bbfd...`”). |
| R2-6 | Note | **M1 follow-up: Step 13’s acceptance list is less explicit than Step 09 claims.** Step 09 says Rust reproduction is “recorded as a Step 13 acceptance item”, but Step 13’s acceptance list only has the generic “Signature mismatches are detected”; the exact absolute-`frequency_hz` re-encoding rule is in the reader’s verify bullet and test table. Add an explicit Step 13 acceptance bullet (or soften the Step 09 claim) when Step 13 is designed. | Step 09 lines 1171–1173; Step 13 lines 125–127, 231–239, and 252–259. |

No Blocker was found. Three Minor findings and three Notes remain; none changes the core API, the project/package
signatures, or the step split. The design remains implementable after the wording fixes.

Residual implementation risks to keep visible:

- The `PRM` catalog and `docs/reference/diagnostics.md` must be updated in the same feature that registers a
  code; the docs-drift test compares the catalog and page exactly. Feature 1 adds structural `PRM` emissions and
  feature 5 owns the full catalog/docs, so feature 1 must not register a code that it leaves undocumented.
- Any new `tests/contracts/` project fixture (including the generated chiral-link P1/P4mm fixture) should be
  added to `contracts/manifest.json` `fixtures.items` as well as `ProjectSchemaTests.fixturePaths`, so the
  manifest remains the fixture registry as well as the schema source.
- The M4 owner decision (09A/09B split versus single-step 4.4 pw) remains a prerequisite to implementation and is
  not a review verdict.

#### Verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Waived

**Ready with notes.** No Blocker or Major remains. M1–M3 and M5 are substantively resolved; M4 is recorded as an
owner decision; m1–m3, m5–m17, and n1–n4 are resolved as claimed; m4 is only partially resolved. The required
`docs` and `policy` gates pass, the frozen design delta contains only the review record and one step-page status
line, the planner did not alter the round-1 findings/evidence/verdict, and no implementation code or step branch
exists. R2-1..R2-3 should be corrected in the design branch and R2-4..R2-6 recorded before the owner accepts;
under `docs/planning/reviews/README.md`, `Ready with notes` requires the human owner to accept the remaining
Minor/Note findings explicitly. This record is evidence and does not authorize starting Step 09.

### Review round 3

#### Independence and frozen state

I am a fresh context with no shared conversation with the planner, implementor, step agent, or orchestrator. I
did not author or implement the reviewed work and did not write rounds 1–2. I received only the frozen design
commit, the request record, the repository links, and the required commands/scope; no desired verdict or summary
was supplied. All evidence below is from reading `/workspace/playground/step09-design` at the request tip
`0c503122c15070f8056687e566672528cb2b7c49` and the frozen design at
`b5db2892aae77954e9ae55d0bb2c66f6e8a14925`.

Frozen-state checks:

- `git rev-parse HEAD` — `0c503122c15070f8056687e566672528cb2b7c49`.
- `git diff --name-status b5db289..0c50312` — exactly `M docs/planning/reviews/step-09.md` and
  `M docs/planning/steps/step-09-parameterization-templates.md` (31 insertions, 5 deletions).
  The step-page hunk is one `Design review` status line (`Re-review required at b5db289…`). The record hunk is
  the reviewed-commit metadata, the `Record state`, two M5 resolution lines, and the appended
  `### Round 2 resolution` block. This is a documentation-only request/status delta, recorded per
  `reviews/README.md` §8.1.
- `git show --stat b5db2892aae77954e9ae55d0bb2c66f6e8a14925` — five docs files
  (`03-quality-strategy.md`, `04-roadmap.md`, `appendix-b-parity-matrix.md`,
  `steps/step-09-parameterization-templates.md`, `steps/step-13-rust-solver-foundation-and-package-reader.md`)
  and **no** review record. Rounds 1–2 reviewer findings, evidence, and verdict text are therefore untouched by
  the fix commit; the request commit's record diff contains no hunk in the round-1/round-2 `Findings`,
  `Evidence`, `## Reviewer independence`, `## Verdict`, or `### Review round` text.
- `git status --short` — clean before this record. `git branch -a` — only `docs/step-09-design`, `main`,
  `origin/docs/step-09-design`, `origin/main`; no step or feature branch. `git worktree list` —
  `/workspace/mocassin` (main) and this worktree. `docs/planning/audits/step-09.md` is absent.

I re-read every checklist item above against `b5db289`; all marks remain `[x]` and no item text was changed.
The round-1 and round-2 sections are unchanged; round-3 residuals are recorded only in this appended section.

#### Commands and results

Run in `/workspace/playground/step09-design`; full logs are in `/tmp/opencode/step09-review3/` (scratch, not
committed).

```sh
./tools/verify.sh docs --require
# exit 0; "Documentation links OK (153 files scanned).";
# Zensical strict: "No issues found";
# DocFX: "Applying templates to 302 model(s)..." then "Build succeeded." / "0 warning(s)" / "0 error(s)";
# rustdoc generated for every workspace crate;
# "Closeout gate completed with required checks enabled."

./tools/verify.sh policy --require
# exit 0; "NuGet licence policy OK (51 packages)."; "no vulnerable packages" for every solution project;
# ApiCompat: "Public API is compatible with the checked-in baseline" (only unresolved MessagePack.dll
# reference notes for Mocassin.Compilation/Mocassin.Packages, not breaking-change findings);
# "Contract artifacts are current (18 files)"; "Model projection check OK (5 fixtures).";
# "space-group catalog is current: 274 settings, sha256 1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395";
# "Process consistency: OK"; "Ran 18 tests ... OK";
# "SPDX headers present in 411 source files"; "No secret patterns found in 739 tracked text files";
# "advisories ok, bans ok, licenses ok, sources ok";
# "Closeout gate completed with required checks enabled."
```

Repository probes: `grep`/`sed` over the frozen design pages, record, data contract and ADR 0010, manifest and
generated schema, contract tools, roadmap, status dashboard, parity matrix, Steps 08/10–13/16/26, the
implemented compiler/symmetry code, the compiler and symmetry tests, and the samples. No code probe was needed
for the frequency issue: the absolute `frequency_hz` design removes the round-1 factor × maximum reconstruction,
so round 3 is a document-consistency and decision-record check.

#### Round-2 and owner-decision verification

| Item | Round-3 status | Reproduced evidence |
| --- | --- | --- |
| R2-1 | Resolved. | `appendix-b-parity-matrix.md` line 131 now reads “Step 11 writes absolute `frequency_hz`; Step 16 derives the per-job factor”. Consistent with `02-data-contracts.md` lines 924 and 934–938 (`jump_rules[].frequency_hz` is the Step 09 applied absolute value; the factor is derived, not persisted), Step 11 lines 114–119 and 219–220, Step 16 lines 39–41 and 125–128, and Step 09 lines 765–767 and 792–795. A repository grep for `frequency_factor` finds only the Step 16 derivation and Step 11's explicit “no relative `frequency_factor` is stored in the model payload”. |
| R2-2 | Resolved. | Step 09 line 848: “No explicit entries at all (`HasExplicitValues == false`) for an explicit template; suppressed for the implicit default. An explicit `0 eV`/`1e13 Hz` suppresses it.” Matches §5 lines 658–662, §9 lines 1040 and 1047, §11.1 line 1143, and `03-quality-strategy.md` line 361. A grep for “differs from its default” finds only the historical round-1/round-2 reviewer text, no design page. |
| R2-3 | Resolved and consistent. | Step 09 line 1141 and `03-quality-strategy.md` line 362 both read “24 chiral geometries, i.e. 12 reciprocal pairs”; the M5 resolution (record lines 225 and 234) uses the same wording. Reproduced from code: `Mocassin.Compilation/Compiler/CompilationCounts.cs` line 88 computes `ChiralPairs = …Count(IsChiral) / 2`, and `Mocassin.Symmetry.Tests/Energy/EnergyPreviewTests.cs` lines 258–283 (`BariumZirconateHighSymmetryPairsAreReciprocal`) asserts exactly 24 chiral previews whose partners are reciprocal and share the `EnergyId`. So 24 directed chiral geometries are 12 reciprocal pairs; the acceptance assertion “compiled chiral geometry count is 24” is correct. |
| R2-4 | Recorded as an owner decision. | Step 09 lines 25–27 and 104–120 record the single-step approval, the honest 3.4–4.4 pw estimate, the upper-bound trigger breach, and the 09A/09B split (features 1–4 vs 5–7) as considered and rejected. `04-roadmap.md` line 65 carries the row exception and §3.1 lines 107–113 records the same exception, its Step 09-only scope, and the re-split rule. The step page remains `Status: Not started` (line 13). |
| R2-5 | Verified; correctly assigned to the orchestrator. | `docs/planning/status.md` is unchanged by this review cycle (last touched by `81d1f32`, Step 08 closeout); row 09 (line 59) still reports design review `Not requested` while the step page line 20 reports `Re-review required at b5db289…`. The resolution assigns the dashboard update to the orchestrator at acceptance/step start, which is the correct write scope: AGENTS §3.8 forbids the planner from editing step status and forbids the reviewer from editing any other file. Accepted as an outstanding orchestrator/step-agent follow-up; no design impact. |
| R2-6 | Resolved. | Step 13's acceptance list (lines 234–237) now requires the reader to “re-encode the Step 09 parameterization signature exactly from `model_signature`, the pair/group/site tables, and the absolute `jump_rules[].frequency_hz` values; a changed frequency is detected without any factor × maximum reconstruction”. Matches Step 11 acceptance lines 219–220 and Step 09 lines 1188–1189. |
| M1 owner decision | Recorded; step not started. | Step 09 lines 28–29 and 787–789 confirm the absolute `frequency_hz` model-payload design as a pre-release `.msl` change; data contract §3.3 lines 934–938 is the authoritative field rule. No model-level `frequency_factor` remains. |
| M2 owner decision | Recorded; step not started. | Step 09 lines 704–705 confirm the implicit default build (`parameterizations: []`), with the full path at §5.1 lines 707–732. `samples/ceria/ceria.mocprj`, `samples/simulations/ceria-kmc.mocprj`, and `samples/barium-zirconate/barium-zirconate.mocprj` all contain `"parameterizations": []`; Step 11 lines 211–219 and Step 12 lines 65–68/213–215 build through the default. |
| Seven-feature list | Approved, without marking the step in progress. | Step 09 line 14 and lines 31–42 record the 2026-11-14 approval of the seven features; the status block stays `Not started` (line 13) and no step branch exists (`git branch -a`). Estimates at lines 1208–1210 sum to 17–22 days. |

#### Findings

| ID | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| R3-1 | **Minor** | **The `signatures.parameterization` finalization has no ADR-0010-scoped pre-release statement.** Step 09 finalizes the existing `skeleton` projection into the six-field typed `ParameterizationSignatureV1` layout (`status: final`, new `projectionType`, the two placeholder `bin` pair fields replaced), but ADR 0010 records its pre-release layout-finalization exception only for `signatures.model` / `ModelSignatureV1` / `model-v1`, and the data contract repeats only the model exception. ADR 0010 rule 1 makes manifest field-order/type changes format-breaking, and AGENTS §11 requires “an ADR or explicit decision note” for a format change. Record the same explicit pre-release basis for the parameterization layout before feature 2 implements it — either extend ADR 0010's “Pre-release model layout finalization” section to cover `signatures.parameterization`, or add a one-line decision note in Step 09 §6.1 / data contract §1.3 stating that the projection is finalized while the `.msl` format is unreleased and no package or fixture is affected. This is carried forward from the round-1 “ADRs and open decisions” and round-2 “Versioning and migration” inline notes; it implies no API, projection, or design change and does not block implementation. | ADR 0010 lines 48–56 (exception names only `signatures.model`; “Any later field or type change after the first released `.msl` requires a version bump and a new ADR”); Step 09 lines 794–799 (finalization) and 787–789 (pre-release statement scoped to the frequency payload); `02-data-contracts.md` lines 87 (order/type change is format-breaking), 109–112 (finalization documented, no decision-note basis), and 837–838 (model-only exception); `05-open-decisions.md` line 59 records the signature decision but not the versioning basis; AGENTS §11. |

No Blocker and no Major was found. R2-1..R2-4 and R2-6 are resolved as reproduced above; R2-5 is verified and
correctly assigned to the orchestrator; the M1, M2, M4, and seven-feature owner decisions are recorded without
starting the step. Only R3-1 (Minor) and the R2-5 dashboard follow-up remain for explicit human-owner
acceptance.

Residual/administrative notes (not findings):

- The `Approval summary` section is still empty; the step agent must complete the cumulative report required by
  AGENTS §3.12 before the human owner is asked to accept this verdict.
- The dashboard `Design review` cell for Step 09 must be updated by the orchestrator/step agent at
  acceptance/step start (the reviewer and planner cannot edit `status.md`).
- The round-2 residual implementation risks (catalog/docs atomicity, `fixtures.items` registration for new
  `tests/contracts/` fixtures, and the M4 owner decision as a prerequisite) remain visible and unchanged.

#### Verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Waived

**Ready with notes.** No Blocker or Major remains. The round-2 fixes are reproduced from the tree: R2-1 (parity
row), R2-2 (`PRM006` catalogue row), R2-3 (“24 chiral geometries, i.e. 12 reciprocal pairs”, confirmed against
`CompilationCounts` and the symmetry oracle), R2-4 (single-step exception in the step page and roadmap §3.1 with
the 09A/09B split recorded as considered and rejected), R2-5 (status dashboard unchanged and the update
correctly assigned to the orchestrator), and R2-6 (Step 13 acceptance requires the exact re-encoding). The M1,
M2, and M4 owner decisions and the approved seven-feature list are recorded with the step still `Not started`.
The required `docs` and `policy` gates pass, the frozen-design delta is documentation-only, no implementation
code or step branch exists, and the planner did not alter the round-1/round-2 findings, evidence, or verdict.
One Minor (R3-1) and the R2-5 dashboard follow-up require explicit human-owner acceptance under
`docs/planning/reviews/README.md` before implementation starts. This record is evidence and does not authorize
starting Step 09.

### Review round 4

#### Independence and frozen state

I am a fresh context with no shared conversation with the planner, implementor, step agent, or orchestrator. I
did not author or implement the reviewed work, did not write rounds 1–3, and received only the frozen design
commit, the request record, repository links, and the required commands/scope; no desired verdict or summary
was supplied. All evidence below is from reading `/workspace/playground/step09-design` at the request tip
`dc51d2d5cfa2c282b425185079e42504fe966e61` and the frozen design at
`26d6a0ecff543ac65eb60d60d9f9c893e00f32bc`.

Frozen-state checks:

- `git rev-parse HEAD` — `dc51d2d5cfa2c282b425185079e42504fe966e61`.
- `git diff --name-status 26d6a0e..dc51d2d` — exactly `M docs/planning/reviews/step-09.md` and
  `M docs/planning/steps/step-09-parameterization-templates.md` (2 files, +18/−3). The step-page hunk is the
  single `Design review:` status line. This is the documentation-only request/status delta allowed by
  `reviews/README.md` §8.1.
- `git show --stat 26d6a0e` — four design docs (`02-data-contracts.md`, `05-open-decisions.md`,
  `adr/0010-canonical-messagepack-signatures.md`, `steps/step-09-parameterization-templates.md`; 27 insertions,
  12 deletions) and **no** review record. The commit's parent is `534218b` (the round-3 record commit), and
  `git diff 534218b..26d6a0e -- docs/planning/reviews/step-09.md` is empty, so the R3-1 fix could not have
  altered rounds 1–3.
- `git diff 26d6a0e..dc51d2d -- docs/planning/reviews/step-09.md` — three hunks only: the reviewed-commit
  metadata row, the `Record state` value, and an appended `### Round 3 resolution` block. No hunk touches the
  request scope, checklist item text, `## Evidence`, `## Findings`, `## Reviewer independence`, `## Verdict`, or
  `### Review round 1/2/3`; a grep of the diff for checklist lines returns nothing.
- `git status --short` — clean before this record. `git branch -a` — only `docs/step-09-design`, `main`,
  `origin/docs/step-09-design`, `origin/main`; no step or feature branch. `docs/planning/audits/step-09.md` is
  absent. `git log -3 -- docs/planning/status.md` — last touched by `81d1f32` (Step 08 closeout).

I re-read every checklist item above against `26d6a0e`; all marks remain `[x]` and no item text was changed.
The round-1/2/3 sections are unchanged; round-4 residuals are recorded only in this appended section.

#### Commands and results

Run in `/workspace/playground/step09-design`; full logs are in `/tmp/opencode/step09-review4/` (scratch, not
committed).

```sh
./tools/verify.sh docs --require
# exit 0; "Documentation links OK (153 files scanned).";
# Zensical (strict): "No issues found";
# DocFX: "Applying templates to 302 model(s)..." then "Build succeeded." / "0 warning(s)" / "0 error(s)";
# rustdoc generated for every workspace crate;
# "Closeout gate completed with required checks enabled."

./tools/verify.sh policy --require
# exit 0; "NuGet licence policy OK (51 packages)."; no vulnerable packages for any solution project;
# ApiCompat: "Public API is compatible with the checked-in baseline";
# "Contract artifacts are current (18 files)"; "Model projection check OK (5 fixtures).";
# "space-group catalog is current: 274 settings, sha256 1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395";
# "Process consistency: OK"; "Ran 18 tests in 0.427s" (OK);
# "SPDX headers present in 411 source files"; "No secret patterns found in 739 tracked text files";
# "advisories ok, bans ok, licenses ok, sources ok";
# "Closeout gate completed with required checks enabled."
```

Repository probes: `grep`/`sed` over the ADR, data contract §1.3/§3.3, open decisions, Step 09 §6.1 and the
feature table, the manifest and generated descriptors, the projections fixture tree, the tags, and the status
dashboard.

#### R3-1 verification (reproduced, not taken from the resolution table)

| Claim | Reproduced evidence |
| --- | --- |
| The pre-release finalization is authorized by a normative, ADR-0010-scoped decision. | `adr/0010-canonical-messagepack-signatures.md` lines 48–61, section “Pre-release signature layout finalization”: it names both `signatures.model` / `ModelSignatureV1` / `model-v1` (Step 08) and `signatures.parameterization` / `ParameterizationSignatureV1` / `parameterization-v1` (Step 09) as finalized once while the `.msl` format is unreleased; it calls this “an explicit pre-release exception to rule 1's format-breaking-change warning”, states that `status = "final"` means “finalized for the first release candidate, not released”, and requires a version bump and a new ADR for any later field/type change after the first released `.msl`. The parameterization half names the replacement of the Step 00 skeleton fields, the generated descriptor, and closure with golden vectors plus the Python checker in Step 09 feature 2 (`parameterization-signature`). |
| The rule has one normative home and the other pages reference it. | ADR 0010 is the only page that states the exception as a decision. `02-data-contracts.md` §1.3 lines 109–114 says the finalization is authorized by “ADR 0010's pre-release signature-layout exception”; §3.3 lines 839–842 says “ADR 0010 records the pre-release signature-layout exception for both” and that neither `status` value is a release marker; Step 09 §6.1 lines 800–804 says “ADR 0010's pre-release signature-layout exception covers this finalization (extended from the model layout to both signed projections)”; `05-open-decisions.md` line 59 says the layout finalization “is covered by ADR 0010's signature-layout exception”. No page presents a competing rule or a different basis, and a grep for “new ADR”/“decision note” finds only the post-release future requirement in the ADR and Step 09. |
| The text is consistent with the data contract and Step 09. | The six-field typed projection (`formatVersion`, `modelSignature`, `pairTables`, `groupSets`, `siteEnergies`, `transitionRules`) and the domain prefix are identical in `02-data-contracts.md` §1.3 and Step 09 §6.1. The feature name `parameterization-signature` matches the approved feature table (Step 09 line 37) and the feature-2 scope (line 1206: finalize the manifest entry, generated descriptor, golden vectors, manifest fixture vectors, and the Python checker). `contracts/manifest.json` still has `signatures.parameterization.status = "skeleton"` with the `stablePairs`/`unstablePairs` placeholder `bin` fields, matching the ADR's “replaces the Step 00 skeleton fields” description and leaving the change to feature 2. |
| The text is consistent with AGENTS §11. | Item 1 (ADR or explicit decision note): ADR 0010, amended in this commit. Item 2 (version bump): deferred only by the recorded pre-release exception, with the post-release bump-plus-ADR rule stated; this matches the accepted `model-v1` precedent in `reviews/step-08-amend-1.md` (R3-10) and `02-data-contracts.md` §3.3. Item 5 (canonical signature-projection vectors): planned for the signed type in feature 2 and recorded in the manifest fixture section with `generate.py` regeneration. Items 3–4/6 (fixtures and cross-language vectors): unchanged, and no parameterization projection fixture exists yet (`tests/contracts/projections/` contains model fixtures only). Item 7 (docs): updated in the same commit. No released `.msl` exists (`git tag` lists no `v*` tag; no `.msl` is tracked), so the exception's premise holds. |
| The design is otherwise unchanged and still coherent. | `git diff 534218b..26d6a0e` touches only the four authorization/versioning statements above. The signed values, the absolute `jump_rules[].frequency_hz` rule, the 12-decimal quantization, the exclusion list, and the Step 11/13/16 consumption rules are unchanged from `b5db289`, and the round-3 `docs`/`policy` gates still pass on this tree. |

R2-5 remains correctly assigned: `docs/planning/status.md` row 09 still reads design review `Not requested`, the
file was last changed by `81d1f32` (Step 08 closeout), and neither the planner nor this reviewer edited it. The
dashboard update belongs to the orchestrator/step agent at acceptance or step start under AGENTS §3.8.

#### Findings

No new finding. R3-1 is resolved: the authorization now exists in its normative ADR home, the data contract and
Step 09 references agree, and the pre-release premise (no released `.msl`) holds. No earlier-round finding,
evidence item, independence statement, verdict, or checklist item was altered.

Residual/administrative notes (not findings, unchanged from round 3 unless stated):

- The `Approval summary` section is still empty; the step agent must complete the cumulative report required by
  AGENTS §3.12 before the human owner is asked to accept this verdict.
- R2-5: the dashboard `Design review` cell for Step 09 must be updated by the orchestrator/step agent at
  acceptance/step start; the reviewer and planner cannot edit `status.md`.
- The round-2 residual implementation risks (catalog/docs atomicity, `fixtures.items` registration for new
  `tests/contracts/` fixtures, and the M4 owner decision as a prerequisite) remain visible and unchanged.
- The round-1 checklist inline note about ADR 0010 covering only `signatures.model` is historical text from the
  frozen round-1 review; it is preserved unaltered and is superseded by this round's reproduced evidence.

#### Verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Waived

**Ready with notes.** No Blocker or Major remains and R3-1 is verified resolved from the tree: ADR 0010 now
carries the pre-release signature-layout exception for `signatures.parameterization`, and the data contract
§1.3/§3.3, open decisions, and Step 09 §6.1 reference that single normative home consistently with AGENTS §11.
The frozen-design delta contains only the R3-1 docs change, the required `docs` and `policy` gates pass, the
planner did not alter rounds 1–3 or any checklist item text, no implementation code or step branch exists, and
R2-5 is still correctly assigned to the orchestrator. The only open item is the administrative R2-5 dashboard
update, which the human owner must accept explicitly under `docs/planning/reviews/README.md` before
implementation starts. This record is evidence and does not authorize starting Step 09.
