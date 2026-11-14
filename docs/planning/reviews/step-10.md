# Step 10 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-10.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 10 — Job Templates and Supercell Generation |
| Request owner | Step 10 orchestrator |
| Orchestrator (step agent / planner) | General orchestrator |
| Review initiated by | Human owner |
| Reviewed commit | `2eb6a5715ea51f324a579ae8974f9e8526005fa9` |
| Round 1 frozen commit | `c66b483f6ef064858d0e8082543d2fcec8386ed3` |
| Docs branch | `docs/step-10-design` |
| Review date | 2026-11-14 |
| Reviewer | Independent planning auditor (round 1; fresh context; no shared conversation with the planner, implementor, step agent, or orchestrator) |

Step page: [`Step 10 — Job Templates and Supercell Generation`](../steps/step-10-job-templates-and-supercells.md)

> Orchestration: the step agent or planner (the orchestrator) freezes the design, requests the reviewer, applies
> fixes autonomously on the design docs or a maintenance branch, adds resolutions without altering reviewer
> text, and requests re-review until the reviewer no longer identifies issues. The human owner approves the
> final verdict and the start of implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 06, 08, 09 |
| Delivers | Job template collections/configurations, deterministic job expansion, doping-aware supercell generation with charge compensation, and the shared lattice/job-count preview used by the GUI and CLI |

## Design decisions reviewed

- [x] Step page design decisions — reviewed the full page, the original-reference mapping, and the feature list;
      the overall design is coherent, but M1–M9 are material gaps in derivation, generation, report,
      optimizer, extension, and preview decisions.
- [x] Data contracts and format changes — reviewed `02-data-contracts.md` §2.1–§2.5 and §3.3, the project
      manifest fields, and the generated-schema story. The additive `schemaVersion = 1` decision matches the
      Step 03–09 pre-release precedent; M6 and M8 show the report and extension contracts are not yet
      internally consistent, and m1 shows the `JOB` ownership statement is stale in four pages.
- [x] Architecture and module boundaries — reviewed `01-target-architecture.md` and the project references.
      Putting the records in `Mocassin.Core.Model.Jobs` and generation/preview in `Mocassin.Packages.Jobs` is
      consistent with `Packages -> Compilation`; M9 and m10 identify the remaining compile-path and wording
      gaps.
- [x] ADRs and open decisions — reviewed ADR 0005, 0009, 0010, 0011, 0013, and 0014 and
      `05-open-decisions.md`. The `schemaVersion = 1` pre-release exception is consistent with ADR 0009 as
      applied by Steps 03–09; M1 conflicts with ADR 0005's seed tuple, and no open-decision row yet records the
      job/lattice seed and sampling contract.
- [x] Quality strategy and test plan — reviewed `03-quality-strategy.md` §9.4 and §13 and the Step 10 test
      table. The property-test and preview/build-equality intentions are right, but m3 records the missing
      persistence/canonicalization and golden-seed/lattice coverage and the test that depends on Steps 11/13.
- [x] Parity matrix and documentation plan — reviewed the Step 10 parity rows and the named pages. The
      parity rows are correctly still `Planned`; the documentation plan omits `docs/reference/diagnostics.md`
      and the CLI reference (m2).
- [x] Licensing and provenance — reviewed the dependency and SPDX requirements. No new dependency or
      GPL-derived content is introduced as written; if M2 is resolved with a .NET ChaCha12 implementation, the
      dependency and licensing decision must be recorded before implementation.
- [x] Post-implementation audit plan — reviewed the Implementation-status block and feature 8. The step is
      correctly `Not started` with no audit record; the standard closeout/audit gate applies.
- [x] Feature list and sizing (eight features; 3–4 person-weeks) — reviewed against
      `04-roadmap.md` §3.1. Eight features is at the upper end of the "roughly 6–8" trigger and 3–4 pw is at,
      not beyond, the four-week trigger; n1 recommends an honest estimate or the page's own 4+5 / 6+7 merges.
- [x] Job template model, persistence, canonical ordering, and `schemaVersion = 1` additive change —
      reviewed the records, the canonical ordering rules, and the manifest/schema task. The ordering is
      mostly specified; M8 is the extension-shape contradiction and m6 records the typed-ID/PublicAPI and
      mutable-array details.
- [x] Inheritable `JobSettings` fields, kind-specific overrides, job count, and extension handling —
      reviewed the inheritance order and the `CompiledRuntimeSettings` source. The order and nullable
      inheritance are clear; M8 makes extension inheritance ambiguous, m5 notes the duplicated
      `JobCountPreview` parameter, and m8 records the JOB015/JOB009 overloads.
- [x] Job expansion, deterministic IDs, master/project/job seed derivation, and domain separation —
      reviewed the ordering, 1-based IDs, and the formulas. Expansion order and IDs are clear; M1 shows the
      seed contract conflicts with ADR 0005/Step 13 and omits byte encoding; M2 leaves the sampling RNG
      unspecified; m4 records the missing hard job bound.
- [x] Supercell generation, default occupations, deterministic sampling, size limits, and row-major layout —
      reviewed the algorithm and the data-contract layout. Default occupations, `A × B × C`, and the row-major
      formula are correct; M2/M3/M4/M5 are the generation and doping defects; m5 records the NaN boundary.
- [x] Doping priority, truncation, charge-compensation ratios, `DopingReport`, and `DopingInfo` —
      reviewed the algorithm, report, and summary. M3 (impossible compensation), M4 (fractional-charge
      arithmetic), M5 (same-site rule), and M6 (report persistence/effective fraction) are material; m11
      records the priority/empty-summary ambiguity.
- [x] Preview service ownership in `Mocassin.Packages.Jobs`, cache keys, request echo, failure boundary, and
      preview/build equality — reviewed the interface, cache key, and equality test. Ownership and the
      request echo are right; M9 is the missing Step 25 compile/refresh path, m5 is the failure-boundary
      ambiguity, and n2 notes that "bounded" has no eviction policy.
- [x] Selection optimizers, P1 position mapping, `UseDualDofCorrection`, and start-option validation —
      reviewed Step 08's `CompiledSite.PositionIndices`, Step 14's `(position, particle)` removal, and the
      `.msl` payload. M7 is the one-to-many site→P1 mapping and "every environment selection mask" gap.
- [x] `ValidationContext.CompiledModel` extension, `JobTemplateValidationStage`, and `JOB001`–`JOB015`
      ownership — reviewed the Step 07 pipeline and the Packages reference direction. The additive
      `CompiledModel?` is safe and the single Build-stage owner is coherent; M3/M4 affect JOB012, m1 the
      ownership wording, m7 the `validate` gap, and m8 the overloaded codes.
- [x] `mocassin build --preview` boundary versus Step 12 package writing — reviewed Step 12's updated CLI.
      The no-write boundary is explicit and reuses Step 12's full build; m9 records that Step 12 does not name
      the JobTemplateValidationStage registration.
- [x] Cross-step interfaces with Steps 11, 12, 13, and 25 — reviewed the updated Step 11/12/25 texts and Step
      13's RNG API. Step 11's `GeneratedLattice` mapping is updated; M1 conflicts with Step 13's
      `derive_job_seed`, M6 shows the report is not mapped, M9 is the Step 25 compile path, and m9/m10 record
      the remaining wording gaps.

## Dependency and interface checks

- [x] Dependencies are `Done` and their contracts are stable — Steps 06, 08, and 09 are `Done` in
      `status.md`; their public model/compiled/parameterization contracts are present. No dependency page is
      reopened by this design.
- [x] Inputs, outputs, types, and formats are defined — the main records and layout are defined, but M1 (seed
      bytes), M2 (sampling algorithm), M6 (report persistence), M7 (optimizer P1 mapping), and M8 (extension
      shape) leave material inputs or outputs ambiguous.
- [x] Versioning and migration behavior is defined — the project stays `schemaVersion = 1` under the Step
      03–09 pre-release additive precedent; `.msl` is not implemented yet, and Step 11/13 own the package
      mapping. No migration is needed; M1/M2 still need versioned golden vectors because the lattice seed
      stream is a reproducibility contract.
- [x] No unresolved cross-step interface question remains — not satisfied: M1 (ADR 0005 vs Step 10 vs Step
      13), M6 (report not mapped by Step 11), M9 (Step 25 compile path), and m9 (Step 12 stage registration)
      remain open.
- [x] Step 06 lattice/doping definitions are sufficient for Step 10 and are not duplicated — yes: Step 06
      owns `Doping`, priorities, exchange shape, and charge signs; Step 10 owns fractions and counts. M5
      shows Step 10's same-site rule is narrower than Step 06's "particles remaining" model, and m11 shows
      `DopingInfo` cannot identify a doping by its `DopingId`.
- [x] Step 08 `CompiledModel`, `CompiledRuntimeSettings`, `CompiledSolverIntent`, and particle/site tables are
      sufficient for expansion, generation, and optimizer application — yes: `CompiledRuntimeSettings`
      supplies all settings, `CompiledSimulation.Intent` supplies dual-DOF/pre-run, and
      `CompiledSite.PositionIndices` supplies the P1 mapping. M7 shows the optimizer mapping/rule is not yet
      written, and M2 shows the sampling RNG is not named.
- [x] Step 09 `ParameterizedModel` and the parameterization-independent lattice hash are coherent — the
      lattice excludes parameterization values, consistent with Step 09. Step 09 says Step 10 "receives
      `ParameterizedModel`", while Step 10's preview/expansion APIs take `Project` + `CompiledModel`;
      m10 records the wording mismatch, which is resolvable by taking the compiled model out of the
      parameterized model.
- [x] Step 11 package writing consumes `JobDescriptor` and `GeneratedLattice` with the documented mapping —
      yes, Step 11 now maps `GeneratedLattice` to `InitialLattice` and uses the Step 10 §10 layout. M6 is the
      exception: `GeneratedLattice.Report` is not mapped and the design's "report is stored" claim is false.
      m9 records the overlapping "package/supercell limits" stage wording.
- [x] Step 12 reuses the preview/expansion services and does not reimplement them — yes; Step 12 adds
      `--preview` only as the no-write path and keeps `build`/`--jobs`/`--parallel`. m9 records that Step 12
      does not name the `JobTemplateValidationStage` registration that Step 10's design assigns to it.
- [x] Step 13's `.msl` reader can consume the documented lattice layout and optimizer encoding — the
      row-major layout and `(position_id, particle_id)` payload are documented. M1 is a direct conflict:
      Step 13 defines `derive_job_seed(master: [u8;32], purpose, collection, config, job)` and a
      `JobPackage.seed`, while Step 10 defines a different textual formula and does not define the bytes.
      M7 means the optimizer encoding is also not fully defined.
- [x] Step 25 consumes the preview service with a project and compiled model and does not reimplement doping —
      yes in principle; Step 25 calls `TryCreatePreview` and `JobCountPreview`. M9 records that Step 25 has no
      documented way to obtain or refresh the required `CompiledModel` in the lattice editor, and that a
      preview never compiles.

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable — scope and non-goals are clear;
      most acceptance criteria are testable. M3 makes the impossible-compensation acceptance criterion
      contradict the algorithm, and M2 means the across-OS lattice criterion has no specified RNG or golden
      vector.
- [x] Design decisions are complete and internally consistent — not satisfied: M1–M9 are material gaps or
      contradictions in seed derivation, sampling, compensation, same-site doping, report persistence,
      optimizer mapping, extensions, and the preview compile path.
- [x] Required tests, property tests, golden fixtures, and performance targets are defined — property and
      preview/build tests are strong, but m3 records the missing project-JSON canonicalization/round-trip
      tests, golden seed/lattice vectors, tier labels, and the "Job acceptance" row that needs the Step 11
      writer/Step 13 reader.
- [x] Documentation, status, parity, and audit updates are planned — step page, `project-format.md`,
      `compilation-pipeline.md`, user guides, parity, and closeout are planned. m2 records the missing
      `docs/reference/diagnostics.md` and CLI-reference updates; the diagnostics page is catalog-checked and
      will fail the docs-drift test if JOB codes are added without it.
- [x] Licensing, provenance, and dependency checks are resolved — no new dependency or GPL-derived file is
      proposed as written. If M2 is resolved with a ChaCha12 .NET implementation, that dependency/licence
      decision must be recorded; otherwise no licensing blocker.
- [x] Risks and mitigations are realistic — the table covers charge rounding, multiple dopings, preview
      divergence, cache staleness, non-determinism, memory, and seed collisions. It does not yet name the
      seed-contract cross-language risk (M1), the unspecified sampling RNG (M2), the optimizer P1 mapping
      (M7), or the GUI compile path (M9); those are the findings, not a reason to add rows before they are
      fixed.
- [x] Code layout and namespace plan follow the structure policy — `Mocassin.Core.Model.Jobs` and
      `Mocassin.Packages.Jobs` are domain-concept folders, the existing `Packages/Jobs` folder already
      exists, and the count is far below the 20-file limit. m6 records the raw `Guid` IDs, mutable `byte[]`
      fields, namespace move, and missing `PublicAPI.Unshipped.txt`/ApiCompat task.
- [x] No hidden scope, stale placeholders, or unowned decisions — no hidden scope was found; the eight
      features match the stated deliverable. m1 (JOB001–JOB003 ownership), m8 (unowned extension/duplicate-ID
      diagnostics), m9 (Step 11 stage overlap), and n4 (original cap replacement) are stale or unowned
      contract details to resolve before implementation.

## Evidence

Files reviewed:

- `AGENTS.md` §3.8–§3.9, §3.12, §8, §10, §11, §12, and §16; `docs/planning/reviews/README.md` and the
  design-review template; the request record itself.
- `docs/planning/steps/step-10-job-templates-and-supercells.md` in full, including the committed diff against
  `c66b483^`; `docs/planning/status.md`; `docs/planning/02-data-contracts.md` §1, §2.1–§2.5, and §3.3;
  `docs/planning/01-target-architecture.md`; `docs/planning/03-quality-strategy.md` §9.4, §10, and §13;
  `docs/planning/04-roadmap.md` §3.1 and the step table; `docs/planning/05-open-decisions.md`;
  `docs/planning/appendix-b-parity-matrix.md`.
- ADR 0005 (seed/RNG policy), 0009 (contract manifest), 0010 (canonical signatures), 0011 (immutable package),
  0013 (format compatibility), and 0014 (extension ABI/trust boundary).
- Dependency and downstream pages: Steps 03, 04, 05, 06, 07, 08, 09, 11, 12, 13, 14, 25, and 26, focusing on
  the job, lattice, doping, compiled-model, validation, seed, package, and preview contracts.
- `docs/reference/project-format.md`; `docs/reference/diagnostics.md`;
  `docs/development/design-decisions.md`; `docs/development/compilation-pipeline.md`;
  `docs/development/code-structure.md`.
- Code/contracts at the frozen commit: `src/dotnet/Mocassin.Compilation/Compiler/CompiledModel.cs`;
  `src/dotnet/Mocassin.Compilation/Simulations/CompiledSimulation.cs`;
  `src/dotnet/Mocassin.Compilation/Structure/CompiledStructureModel.cs`;
  `src/dotnet/Mocassin.Compilation/Simulations/EncodingCompilationStage.cs`;
  `src/dotnet/Mocassin.Core/Model/Particles/Particle.cs`;
  `src/dotnet/Mocassin.Core/Model/Lattice/Doping.cs`;
  `src/dotnet/Mocassin.Core/Model/Lattice/LatticeModelValidator.cs`;
  `src/dotnet/Mocassin.Core/Model/Simulations/Simulation.cs`;
  `src/dotnet/Mocassin.Core/Model/Templates.cs`;
  `src/dotnet/Mocassin.Core/Project/ProjectHasher.cs`;
  `src/dotnet/Mocassin.Core/Project/ProjectJson.cs`;
  `src/dotnet/Mocassin.Core/Project/ProjectValidator.cs`;
  `src/dotnet/Mocassin.Packages/Projects/Validation/ValidationContext.cs`;
  `src/dotnet/Mocassin.Packages/Jobs/SolverJobFlagsMapper.cs`; the `Mocassin.Packages`,
  `Mocassin.Compilation`, and `Mocassin.Contracts` project files; `contracts/manifest.json`.

The read-only original clone (`/workspace/mocassin-original`) is not present in this environment. Original
behaviour claims were therefore checked against the Step 06/08 "Original reference" text, the Step 08 review
and audit records that quote the original `JumpSelectionOptimizer`/job-builder line numbers, and the design
pages, not against the original source itself. That is a review limitation, not a finding.

Commands run:

```text
git rev-parse HEAD
# 42dab141616c34661747b341cb5942e3a04852bc (the request commit; parent is c66b483)

git status --short
# no output

git diff --stat c66b483^ c66b483
# docs/development/design-decisions.md | 10 +-
# docs/planning/02-data-contracts.md | 35 +++--
# docs/planning/steps/step-07-validation-and-diagnostics.md | 8 +-
# docs/planning/steps/step-10-job-templates-and-supercells.md | 173 +++++++++---
# docs/planning/steps/step-11-simulation-package-and-msl-writer.md | 9 +-
# docs/planning/steps/step-12-build-orchestration-and-cli.md | 6 +-
# docs/planning/steps/step-25-model-editors-energy-lattice-simulations.md | 6 +-
# docs/reference/project-format.md | 2 +-
# 8 files changed, 209 insertions(+), 40 deletions(-)

git diff --stat c66b483 HEAD
# At review start, before this record was written:
# docs/planning/reviews/step-10.md | 163
# docs/planning/status.md | 2 +-
# docs/planning/steps/step-10-job-templates-and-supercells.md | 2 +-
# 3 files changed, 165 insertions(+), 2 deletions(-)
# Only the review request/status commits follow the frozen design.

python tools/check_docs_links.py
# Documentation links OK (158 files scanned).

python tools/check_doc_math.py
# Documentation math conventions OK (155 files scanned).

python tools/check_process_consistency.py
# Process consistency: OK

./tools/verify.sh docs --require
# git diff --check passed; docs math/links passed; Zensical strict build "No issues found";
# DocFX "Build succeeded. 0 warning(s) 0 error(s)"; rustdoc completed;
# "Closeout gate completed with required checks enabled."

./tools/verify.sh policy --require
# git diff --check passed; build 0 warnings/0 errors; ApiCompat passed for all projects;
# contract artifacts current (18 files); model and parameterization projection checks passed;
# space-group catalog current (274 settings); Process consistency OK; 31 contract tests OK;
# SPDX headers present (470 files); no secret patterns; licence/vulnerability checks OK;
# "Closeout gate completed with required checks enabled."
```

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M1 — The deterministic job/lattice seed contract is not a single, implementable specification.** Step 10 defines `seed = SHA-256("mocassin/job/v1" concatenation master_seed concatenation "job" concatenation collection_index_u32 concatenation config_index_u32 concatenation job_index_u32)` and takes `master_seed` from a collection `RngSeed` string, a simulation `RngSeed` string, or the project `masterSeed`. ADR 0005 orders the tuple as `(master_seed, collection_index, config_index, job_index, purpose)`, while Step 10 puts the purpose second; Step 13 exposes `derive_job_seed(master: [u8;32], purpose, collection, config, job)` and reads a `JobPackage.seed` from the package. The design does not define the byte encoding of an arbitrary `RngSeed` string, length framing, or the endianness of the `u32` fields; the project `masterSeed` is required by the manifest but described as optional in the architecture, and "if absent, derive it ... and store it" would silently add project state during a build. No golden seed vectors are planned. As written, the same textual formula cannot be reproduced independently by .NET and Rust, and the acceptance criterion for cross-OS identity cannot be tested. | Step 10 lines 141–169; ADR 0005 lines 23–27; Step 13 lines 92 and 143–144; Step 06 lines 70–82; `02-data-contracts.md` lines 145 and 547; `01-target-architecture.md` lines 215–222; `contracts/manifest.json` project `masterSeed` field. |
| **Major** | **M2 — The lattice-generation RNG, draw order, and lattice-request projection are unspecified.** Step 10 says only that sampling uses "a deterministic Fisher–Yates shuffle or reservoir sampling" over a candidate list sorted by linear position. It does not name the PRNG or stream, the number/order of draws, or which of the two algorithms is normative. ADR 0005 specifies ChaCha12 for the solver and separate lattice/simulation streams, but the .NET lattice generator has no specified implementation or stream construction. `lattice_request_hash` is described only as a hash of a "canonical structure/particle/doping subset", the catalog version, `A/B/C`, and ordered fractions, with no field list, field order, or encoding. Under this design, "same job descriptor + seed produces an identical lattice across runs and OSes" and any golden lattice/seed vector cannot be implemented or independently checked. | Step 10 lines 151–168 and 205–207; ADR 0005 lines 23–27; `03-quality-strategy.md` line 261; Step 13 line 172. |
| **Major** | **M3 — The impossible-compensation path is a warning plus zero, contradicting JOB012 and the acceptance criterion.** Algorithm step 2e says that if no feasible `n_p > 0` exists, the doping contributes 0 and a warning is recorded. If the initial lattice is neutral, a zero primary and zero dependent application leave it neutral, so the §4 rule "the build fails if charge neutrality cannot be achieved and compensation was requested" and the `JOB012` error never fire for the unfulfilled compensation. The step page then makes "an impossible charge compensation fails with `JOB012`" an acceptance criterion. The design must either emit `JOB012` directly when a compensation-requested doping cannot realize any pair, or define a different, reachable failure predicate. | Step 10 lines 187–193, 232, 327, and 467; the report/neutrality text lines 198–203. |
| **Major** | **M4 — Charge-ratio and neutrality arithmetic is undefined for fractional charge deltas and non-compensated charged dopings.** Step 06 allows arbitrary `double` particle charges and a primary exchange with any charge delta when `ChargeCompensation` is false. Step 10 reduces `−Δq_p / Δq_d` to "the smallest integer pair `(p, d)`" without a rationalization rule, tolerance, or fallback for non-integer ratios, and step 3 validates "total charge is neutral within tolerance (or the configured tolerance policy)" although no tolerance value or policy field exists and a non-compensated charged doping is explicitly allowed. The tests only exercise ceria's integer charges. This is a scientific-correctness gap and a silent change from the original's `1e-6` dependent-count reduction. | Step 10 lines 187–199 and 216–221; Step 06 lines 57–59 and 173–180; `Particle.cs` line 25; `LatticeModelValidator.cs` lines 288–310. |
| **Major** | **M5 — The same-site doping rule contradicts the documented "remaining particles" sequence and the original behaviour.** Step 10 says: if a doping targets the same site as a higher-priority doping, the source particle "must be the result of the earlier doping"; otherwise it contributes 0. Step 06 and the data contract say each fraction applies to the particles remaining after higher-priority dopings, and the original reference says the same. A later `A→C` doping after a partial `A→B` doping would therefore be forced to 0 even though `A` remains, which is not the documented sequential rule. No rationale, deviation, or test records this scientific correction (AGENTS.md §4.8 forbids silent corrections). | Step 10 lines 63 and 208–209; Step 06 lines 60–61; `02-data-contracts.md` line 465. |
| **Major** | **M6 — `DopingReport` is claimed to be stored, but the package carries only a summary string, and `EffectiveFraction` is undefined.** Step 10 §4 says "the report is stored in the job metadata (`DopingInfo`)"; `DopingInfo` is a string, Step 11 maps only `{format_version, lattice_size, position_count, lattice, doping_info}`, and Step 10 §10 says `DopingInfo` is display-only with the authoritative counts in `DopingReport` and the package lattice signature. The package payload has no application array or requested/applied counts, so the report is lost after the build. `DopingApplication` has no priority although the summary string is keyed by `priority`, and `EffectiveFraction` has no definition (applied/available vs applied/total positions). | Step 10 lines 216–231 and 426–428; Step 11 lines 120–122; `02-data-contracts.md` lines 1006 and 1016. |
| **Major** | **M7 — The selection-optimizer site-to-P1 mapping and the dual-DOF clear rule are underspecified.** The project stores `SelectionOptimizer(CellSiteId, ParticleId)`; each `CompiledSite` owns multiple `PositionIndices`, while the `.msl` payload stores one `(position_id, particle_id)` per entry. Step 08 says it resolves optimizers to compiled `(position, particle)` pairs, and Step 14 removes specific `(position, particle)` combinations. The design does not say whether one site optimizer expands to every P1 position of the site, one canonical position, or the site's environment mask. It also does not define which "every environment selection mask" must be reduced before `UseDualDofCorrection` is cleared, or the no-optimizer/mixed case in implementable terms. A multi-position site can therefore under- or over-remove start options and set the wrong degree-of-freedom value. | Step 10 lines 97, 141–144, and 300–310; `02-data-contracts.md` lines 718–720 and 1003; `CompiledStructureModel.cs` lines 25–33; Step 08 lines 464–466; Step 14 line 159. |
| **Major** | **M8 — Extension persistence and inheritance contradict the C# model and the JSON contract.** `JobSettings` has `Extensions` and the text says extensions "inherit/override like other settings" and are sorted by `Id`; `JobConfig` has no `Extensions`, and the data-contract JSON example places `extensions` as a sibling of `overrides` at configuration level. The manifest/schema update cannot define both shapes. Step 10 says it validates a non-empty unique `id` but provides no `JOB` diagnostic for an empty/duplicate id, and ADR 0014's "bounded UTF-8 JSON object" has no size bound in this design. | Step 10 lines 88–93, 381, and 392–400; `02-data-contracts.md` lines 693–704 and 721–724; ADR 0014 lines 25–26. |
| **Major** | **M9 — The preview requires a `CompiledModel`, but Step 25 has no documented way to obtain or refresh one.** Step 10 defines `TryCreatePreview(Project, CompiledModel, ...)`, says a preview "never triggers a compilation", and says Step 25 consumes it "with the current project and compiled model". Step 25's lattice editor is a model editor; its only compile-preview button is in the energy-editor section, and no Step 23/25 contract says the lattice editor compiles on demand, disables preview until a build, or invalidates a preview after a model edit. A user editing dopings before a build cannot preview, or previews a stale compiled model. | Step 10 lines 258–263, 274–276, and 297; Step 25 lines 88 and 125–128. |
| **Minor** | **m1 — `JOB001`–`JOB003` ownership conflicts with four other normative pages.** Step 10 assigns `JOB001`–`JOB015` to the Step 10 stage and claims a single owner, while Step 06, the data contract, `project-format.md`, and `XC-02` all say Step 10 owns only `JOB004`–`JOB015`. The codes can still be implemented, but the ownership statement is stale and the audit/parity trace will be ambiguous. | Step 10 lines 314–340; Step 06 line 333; `02-data-contracts.md` line 473; `docs/reference/project-format.md` line 311; `docs/planning/reviews/cross-cutting/xc-02-architecture-and-design.md` line 238. |
| **Minor** | **m2 — The documentation plan omits the generated diagnostics reference and the CLI reference.** Step 10 adds `JOB001`–`JOB015` but the documentation plan names only the user guides, project format, and compilation-pipeline pages. `docs/reference/diagnostics.md` says it is generated from the same descriptors and that a documentation-drift test fails when it disagrees; adding JOB codes without updating it will fail that test. `build --preview` also needs a CLI-reference entry. | Step 10 lines 478–487; `docs/reference/diagnostics.md` lines 1–5; Step 12 lines 44–70. |
| **Minor** | **m3 — The test plan omits persistence/canonicalization and golden seed/lattice coverage, and one row depends on later steps.** The table has expansion, inheritance, supercell, doping, charge, preview, cache, optimizer, edge, and performance rows, but no project-JSON round-trip/canonical-order/unknown-field test for the new nested template, no golden seed or lattice vector, and no test-tier labels. The "Job acceptance" row says the lattice is written and read back through the real writer/reader, but Step 11's writer and Step 13's reader do not exist during Step 10. | Step 10 lines 491–510 and 495; `03-quality-strategy.md` lines 90, 113, and 480; AGENTS.md §12 (tier declarations). |
| **Minor** | **m4 — The design has no hard resource bound on job-template counts or total jobs, and `JobId` is an `int`.** `JOB013` only warns above 10,000 jobs; there is no cap on collections/configs per template, and `JobId` is a 32-bit `int`. The parity matrix records "no hard cap" as a replacement decision, but AGENTS.md §11 requires untrusted project input to bound counts and cap allocations. A malformed project can force unbounded expansion before Step 11's streaming writer helps. | Step 10 lines 111 and 314–330; `appendix-b-parity-matrix.md` line 149; AGENTS.md §11. |
| **Minor** | **m5 — The preview failure boundary is ambiguous, misses non-finite fractions, and `JobCountPreview` duplicates the collection override.** The page says unknown dopings, fractions outside `[0,1]`, invalid `(A,B,C)`, and impossible compensation return `null` plus diagnostics, then says non-positive `(A,B,C)` is a programmer error; it does not say how `NaN`/infinity fractions are rejected (`NaN` passes a naive range check). `TryCreateJobCountPreview` takes both a `JobTemplate` and an `int? jobCountPerConfiguration`, but the template/collection already carries `JobCountPerConfig`; the override precedence is undefined. | Step 10 lines 265–270 and 286–288; Step 10 line 320; `DopingValue` line 96; AGENTS.md §11 (reject NaN/Inf). |
| **Minor** | **m6 — The model/API conventions need tightening.** `JobCollection` and `JobConfig` use raw `Guid` IDs rather than typed IDs such as `JobCollectionId`/`JobConfigId`; `JobDescriptor.Seed` and `GeneratedLattice.Particles` are mutable `byte[]` in otherwise immutable records; the existing `JobTemplate` moves from `Mocassin.Core.Model` to `Mocassin.Core.Model.Jobs`; and the task list has no `PublicAPI.Unshipped.txt`/ApiCompat entry for the new public surface. | Step 10 lines 80, 89, 125, 131, 396–403; AGENTS.md §8 (typed IDs, immutable records) and §16 (PublicAPI gate); current `Mocassin.Core/Model/Templates.cs`. |
| **Minor** | **m7 — `mocassin validate` will not report structural job-template errors.** Step 10 places all `JOB001`–`JOB015` checks in the `Build` stage and says `validate` keeps `RunBuildStage = false`; `JOB001`–`JOB009` do not need a compiled model (only total positions and optimizer/compensation checks do). A project with an empty template, unknown simulation, duplicate names, or a duplicate optimizer therefore validates clean and fails only on build. This may be intended, but the page should state it and the acceptance/tests should pin the intended `validate` behaviour. | Step 10 lines 332–350; Step 07 lines 188–190 and 210–212. |
| **Minor** | **m8 — Several `JOB` rules are overloaded and some new validation conditions have no code.** `JOB015` is labelled "Inherited settings missing a required value" but is also used for a kind-inapplicable override; `JOB009` is labelled "Optimizer reference invalid" but is also used for removing every start option, with the design leaving "warning/error" undecided. Duplicate doping IDs in a config, duplicate `(site, particle)` optimizers, and empty/duplicate extension IDs have no `JOB` code despite being validated or rejected. | Step 10 lines 309, 325–330, 338–350, and 392–395. |
| **Minor** | **m9 — Build-stage ownership overlaps between Steps 10 and 11, and Step 12's registration is missing.** Step 10 claims the single `Build`-stage owner for `JOB001`–`JOB015`; Step 11 says its own `Build` stage handles "package/supercell limits". Step 12's page adds `--preview` but does not state that it constructs `ValidationContext` with `CompiledModel` and registers `JobTemplateValidationStage`, which Step 10 and `design-decisions.md` assign to it. | Step 10 line 340; Step 11 lines 184–186; Step 12 lines 44–70; `docs/development/design-decisions.md` lines 272–273. |
| **Minor** | **m10 — Cross-step wording still drifts on ownership and the parameterization input.** Step 10 says `Mocassin.Packages` "already references `Core`, `Symmetry`, `Compilation`, and `Contracts`", but the project file directly references only `Compilation`, `Contracts`, and `Core` (Symmetry is transitive). The architecture table says `Mocassin.Packages` owns "job templates" while Step 10 moves the records to `Mocassin.Core.Model.Jobs`. Step 09 says Step 10 "receives `ParameterizedModel`" as the model half of expansion, while Step 10's preview/expansion APIs take `Project` + `CompiledModel`. | Step 10 lines 260, 274–278, and 295–297; `src/dotnet/Mocassin.Packages/Mocassin.Packages.csproj`; `01-target-architecture.md` line 148; Step 09 lines 1144–1151. |
| **Minor** | **m11 — `DopingInfo` cannot identify a doping and treats "no applied doping" ambiguously.** The summary format is `{priority}@{effectiveFraction:R}`, but `DopingApplication` carries `DopingId`, not `priority`; a reader must re-resolve priority from the model. The rule "no applied doping yields the empty string" does not distinguish a template with no doping from a template whose dopings all applied zero counts, which loses information for build reports. | Step 10 lines 216–228 and 426–428; `02-data-contracts.md` line 1006. |
| **Note** | **n1 — The eight-feature list and 3–4 pw estimate are at the split-trigger boundary.** The roadmap trigger is more than roughly 6–8 features or more than about four weeks; eight features and a four-week upper bound are at, not beyond, the limit. The page already permits merging 4+5 and 6+7: doing so before approval would reduce the audit surface and make the estimate more credible. | Step 10 lines 20–34 and 440–445; `04-roadmap.md` §3.1. |
| **Note** | **n2 — The preview cache is "bounded" without a bound or eviction policy.** The key is well specified and the cache is declared thread-safe, but no maximum entry count/byte budget, eviction order, or test for the bound is given. A 500,000-position lattice preview can make this material. | Step 10 lines 293–294 and 211. |
| **Note** | **n3 — The performance target covers expansion only, not the build the quality strategy budgets.** The Step 10 target is "10⁴ small jobs expand in < 10 s"; `03-quality-strategy.md` budgets a 10⁴-job build at < 10 min and < 2 GB peak RSS. Step 10 should state which of those it owns and add a memory/cancellation target for generation. | Step 10 line 510; `03-quality-strategy.md` lines 480–481. |
| **Note** | **n4 — The replacement of the original 99,999-job cap is not recorded as a deviation in the step page.** The original reference notes the cap; the new design warns at 10,000 and Step 26 still mentions a "99,999-job guard", while the parity matrix records "no hard cap". This is a recorded product decision but not a step-page deviation, so the audit trail is split across pages. | Step 10 lines 65 and 328; `appendix-b-parity-matrix.md` line 149; Step 26 line 36. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Re-review required

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context for round 1. I did not author or implement the reviewed work and have no shared
> conversation with the planner, implementor, step agent, or orchestrator. I read the repository at
> `c66b483f6ef064858d0e8082543d2fcec8386ed3` directly rather than relying on a summary, and I edited and
> committed only this review record.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

**Verdict: Changes required.** No Blocker was found, but nine Major findings remain: the seed derivation
contract (M1), the unspecified lattice RNG/sampling projection (M2), the impossible-compensation path versus
`JOB012` (M3), fractional-charge/neutrality arithmetic (M4), the same-site doping rule (M5), the
`DopingReport`/`DopingInfo` persistence contradiction (M6), the selection-optimizer P1 mapping and dual-DOF
rule (M7), the extension shape/inheritance contradiction (M8), and the Step 25 preview compile path (M9).
Eleven Minor findings and four Notes are recorded above. The core architecture is sound: job/template records
belong in Core, generation and preview belong in `Packages.Jobs`, `Mocassin.Compilation` stays pure, the
Step 07 `Build` stage is the right extension point, the row-major lattice layout is clear, and the
preview/build equality principle is correct. The required repository checks pass. The design must be revised
and a fresh review round must verify the fixes before implementation starts. This record is evidence and does
not authorize the step.

Follow-up owner: Step agent / planner
Follow-up step: Step 10 design revision and fresh design review (round 2)

## Round 1 resolutions

Design revision commit: `2eb6a5715ea51f324a579ae8974f9e8526005fa9`. The reviewer text above is unchanged; the
step agent owns this section.

| Finding | Resolution | Files |
| --- | --- | --- |
| M1 | Seed derivation now follows ADR 0005's tuple order with fixed-width `u32_le` fields and a terminal purpose; master-seed text normalization and a job-seed golden vector are defined. The project seed is required; no build-time state is added. | Step 10 §2; Step 13 §4; ADR 0005; `05-open-decisions.md` |
| M2 | The lattice request projection now lists every hashed field and encoding (structure, particles/sets, lattice/dopings, A/B/C, fractions, catalog/contract provenance). The build-side SHA-256 counter DRBG, Lemire bounded draws, Fisher–Yates sampling, and golden seed/sampler vectors are normative. | Step 10 §2; `05-open-decisions.md`; `03-quality-strategy.md` §9.4 |
| M3 | A compensation-requested doping that cannot realize a positive pair now emits `JOB012` and stops generation/preview; zero requested/available is explicitly not a failure. | Step 10 §3, §4, acceptance |
| M4 | The ratio search scans `n_p` downward with the named `1e-6` tolerance, validates compensated neutrality at `1e-6 × max(1, total_positions)`, and permits non-compensated charged lattices with `JOB016`; particle charges are included in the request hash. | Step 10 §2–§3 |
| M5 | All same-site dopings now apply sequentially to the current remaining particles; the narrower "must be the earlier result" rule is removed. | Step 10 §3 |
| M6 | `DopingReport` is explicitly build/preview data; only the structured `DopingInfo` is persisted. `DopingApplication` gains priority and available count, and `EffectiveFraction` is defined. | Step 10 §3–§4, §10; data contract §3.4; Step 11 §4 |
| M7 | Optimizers expand to every P1 image of the site, sorted by `(position_id, particle_id)`; dual-DOF is cleared only under the explicit all-active-environment-masks rule, with no-optimizer/mixed behavior stated. | Step 10 §5 |
| M8 | Extensions live inside `baseSettings`/`overrides`; a non-null array replaces the inherited list. Unique ids, object configs, 16-entry/256-KiB bounds, and `JOB018` are defined. | Step 10 §8; data contract §2.4 |
| M9 | Step 25 now owns a compiled-model cache keyed by model/document hash, invalidates on edits, and either recompiles with debounce/cancellation or disables preview. The preview service never compiles. | Step 10 §4.1; Step 25 §2 |
| m1 | Ownership is now `JOB001`–`JOB020`; Step 06, the data contract, project-format, and the lattice guide are updated. XC-02 remains a historical record from before this design. | Step 06; data contract; `project-format.md`; `user-guide/lattice.md` |
| m2 | The documentation plan adds `docs/reference/diagnostics.md` and `docs/reference/cli.md`. | Step 10 Documentation |
| m3 | The test plan adds canonicalization/persistence, golden seeds/sampler, report round-trip, validation-stage, limits, tiers, and moves writer/reader integration to the later steps. | Step 10 Tests |
| m4 | Hard caps are defined (`JOB019`/`JOB020`), with `JobId` bounded to 1..99,999. | Step 10 §8; parity matrix |
| m5 | The preview rejects non-finite fractions, treats all invalid `(A,B,C)` as diagnostics, throws only for null project/model, and removes the duplicate job-count override parameter. | Step 10 §4.1 |
| m6 | Typed `JobCollectionId`/`JobConfigId`, immutable byte arrays, the `Model.Jobs` move, and PublicAPI/ApiCompat work are added to the design and tasks. | Step 10 §1, §8, tasks |
| m7 | Structural `JOB` checks now run through Core `JobTemplateValidator` in `validate`; only compiled-bound checks need the Build stage. Acceptance and tests pin this. | Step 10 §6–§7; Step 07 |
| m8 | `JOB016`–`JOB020` now cover non-neutral non-compensated lattices, duplicates, extension bounds, the 99,999 cap, and structural caps; overloaded rules are narrowed. | Step 10 §6 |
| m9 | Step 11 owns only package limits; Step 12 explicitly constructs `ValidationContext` with the compiled model and registers `JobTemplateValidationStage`. | Step 11; Step 12 |
| m10 | Step 10 now states Packages' direct references and transitive Symmetry; the architecture table and Step 09 consumption wording are corrected. | Step 10 §4.1; architecture; Step 09 |
| m11 | `DopingInfo` is now `v1:{dopingId}:{priority}:{requested}:{appliedPrimary}:{appliedDependent}:{effectiveFraction:R}` entries, preserving zero-applied entries. | Step 10 §4; data contract §3.4 |
| n1 | The eight-feature list is retained; it is within the "roughly 6–8" trigger and the owner approved it. | Step 10 §12 |
| n2 | The preview cache is bounded to 8 entries / 256 MiB with LRU eviction and a test. | Step 10 §4.1 |
| n3 | Step 10 now owns expansion `< 10 s`, generation `< 1 GiB` with cancellation; Step 11 owns the full package-build budget. | Step 10 Tests; `03-quality-strategy.md` §9.4 |
| n4 | The original 99,999-job cap is retained as `JOB019`; the parity matrix records it as implemented, not replaced. | parity matrix; Step 10 §8 |

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
| 1 | 2026-11-14 | `c66b483` | Independent planning auditor (fresh context) | Changes required |
| 2 | 2026-11-14 | `2eb6a57` | Pending fresh auditor | Pending |

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
| M1 | Resolved in design revision | Step agent | `2eb6a57` |
| M2 | Resolved in design revision | Step agent | `2eb6a57` |
| M3 | Resolved in design revision | Step agent | `2eb6a57` |
| M4 | Resolved in design revision | Step agent | `2eb6a57` |
| M5 | Resolved in design revision | Step agent | `2eb6a57` |
| M6 | Resolved in design revision | Step agent | `2eb6a57` |
| M7 | Resolved in design revision | Step agent | `2eb6a57` |
| M8 | Resolved in design revision | Step agent | `2eb6a57` |
| M9 | Resolved in design revision | Step agent | `2eb6a57` |
| m1–m11 | Resolved in design revision (see table above) | Step agent | `2eb6a57` |
| n1 | Accepted; no change (eight features retained) | Step agent | `2eb6a57` |
| n2–n4 | Resolved in design revision (see table above) | Step agent | `2eb6a57` |
