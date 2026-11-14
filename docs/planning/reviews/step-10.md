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
| Reviewed commit | `bb62e405f27199173cdabc256f6f6d43aa033da1` |
| Round 1 frozen commit | `c66b483f6ef064858d0e8082543d2fcec8386ed3` |
| Round 2 frozen commit | `2eb6a5715ea51f324a579ae8974f9e8526005fa9` |
| Round 3 frozen commit | `07f14f2e23feff9d9c1f7ff7c46ce4d074a86b43` |
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

`Ready with notes` was the round-3 review verdict; it is not a merge authorization. The round-4 re-review
target is `bb62e405f27199173cdabc256f6f6d43aa033da1`.

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

## Round 2 review

### Reviewer identity and scope

| Field | Value |
| --- | --- |
| Reviewer | Independent planning auditor (round 2; fresh context; no shared conversation with the planner, implementor, step agent, or orchestrator) |
| Review date | 2026-11-14 |
| Branch | `docs/step-10-design` |
| Round 1 frozen commit | `c66b483f6ef064858d0e8082543d2fcec8386ed3` |
| Round 2 revised design commit | `2eb6a5715ea51f324a579ae8974f9e8526005fa9` |
| Resolution/status commit | `19c076c95b0dbcb7cd8d064ed0ff6a10e364b46e` |
| Record parent | `19c076c95b0dbcb7cd8d064ed0ff6a10e364b46e` |
| Design delta reviewed | `git show --stat 2eb6a57`: 14 files, 332 insertions, 168 deletions |

> I am a fresh context for round 2. I did not author or implement the reviewed work and have no shared
> conversation with the planner, implementor, step agent, or orchestrator. I read the repository at
> `2eb6a5715ea51f324a579ae8974f9e8526005fa9` and the resolution/status delta at `19c076c95b0dbcb7cd8d064ed0ff6a10e364b46e`
> directly rather than relying on the resolution table, and I edited and committed only this review record.

The read-only original clone (`/workspace/mocassin-original`) is still absent from this environment. Original
behaviour claims were checked against the Step 06/08 design text, the Step 08 review record that quotes
`JumpSelectionOptimizer.Run` and `RunPostBuildOptimizers`, and the current compiled-model types. That is a
review limitation, not a finding.

### Evidence

Files read in this round:

- `AGENTS.md` §3.8–§3.9, §3.12, §8, §10–§12, and §16; `docs/planning/reviews/README.md` and the design-review
  template; the request record, round-1 findings, and step-agent resolutions in this file.
- `docs/planning/steps/step-10-job-templates-and-supercells.md` in full; the complete diff `c66b483..2eb6a57`
  and the isolated revision `2eb6a57`; `docs/planning/status.md`; `docs/planning/02-data-contracts.md` §1,
  §2.4, §3.3, §3.4; `docs/planning/01-target-architecture.md`; `docs/planning/03-quality-strategy.md` §9.4;
  `docs/planning/04-roadmap.md` §3.1; `docs/planning/05-open-decisions.md`;
  `docs/planning/appendix-b-parity-matrix.md`.
- ADR 0005 (statistical parity and RNG), 0009, 0010, 0011, 0013, and 0014; the relevant Step 06 compensation
  and doping rules; Steps 07, 08, 09, 11, 12, 13, 25, and 26; `docs/reference/project-format.md` and
  `docs/user-guide/lattice.md`; `docs/development/design-decisions.md`, `compilation-pipeline.md`, and
  `code-structure.md`.
- Current code/contracts used to verify the claimed navigation and types:
  `src/dotnet/Mocassin.Core/Project/ProjectHasher.cs`;
  `src/dotnet/Mocassin.Core/Project/ProjectMetadata.cs`;
  `src/dotnet/Mocassin.Compilation/Simulations/CompiledSimulation.cs`;
  `src/dotnet/Mocassin.Compilation/Structure/CompiledStructureModel.cs`;
  `src/dotnet/Mocassin.Compilation/Transitions/CompiledTransitionModel.cs`;
  `src/dotnet/Mocassin.Packages/Projects/Validation/ValidationContext.cs`;
  `src/dotnet/Mocassin.Packages/Projects/Validation/DiagnosticCatalog.cs`;
  `src/dotnet/Mocassin.Core/Model/Templates.cs`; `src/dotnet/Mocassin.Core/Ids/TypedIds.cs`.

Commands run and exact results (before this record was written):

```text
git status --short
# no output (exit 0)

git rev-parse HEAD
# 19c076c95b0dbcb7cd8d064ed0ff6a10e364b46e

git cat-file -t 2eb6a5715ea51f324a579ae8974f9e8526005fa9
# commit

git diff --stat c66b483 2eb6a57
# 16 files changed, 716 insertions(+), 170 deletions(-)
# Includes the round-1 request/record commits; the isolated design revision is:

git show --stat 2eb6a57
# 14 files changed, 332 insertions(+), 168 deletions(-)
# Step 10 421 lines, data contracts 27, quality strategy 11, architecture 2, open decisions 1,
# parity 2, Steps 06/09/11/12/13/25, project-format 4, user-guide/lattice 5

git show --stat 19c076c
# 3 files changed, 51 insertions(+), 6 deletions(-)
# docs/planning/reviews/step-10.md, docs/planning/status.md, Step 10 status line

python tools/check_docs_links.py
# Documentation links OK (158 files scanned). (exit 0)

python tools/check_doc_math.py
# Documentation math conventions OK (155 files scanned). (exit 0)

python tools/check_process_consistency.py
# Process consistency: OK (exit 0)

./tools/verify.sh docs --require
# git diff --check passed; docs math/links passed; Zensical strict "No issues found";
# DocFX "Build succeeded. 0 warning(s) 0 error(s)"; rustdoc completed;
# "Closeout gate completed with required checks enabled." (exit 0)

./tools/verify.sh policy --require
# git diff --check passed; build 0 warnings/0 errors; ApiCompat passed for every project;
# contract artifacts current (18 files); model projection OK (5 fixtures); parameterization
# projection OK (4 fixtures); space-group catalog current (274 settings, sha256
# 1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395); process consistency
# OK; 31 contract tests OK; SPDX headers in 470 files; no secret patterns in 817 tracked
# text files; advisories/bans/licenses/sources ok; "Closeout gate completed with required
# checks enabled." (exit 0)
```

Golden-vector check (reimplemented independently from the written specification, not copied from the page):

```python
master = bytes(range(32))
job = sha256(master + u32le(0) + u32le(0) + u32le(0) + b"mocassin/job/v1").digest()
lattice = sha256(b"mocassin/lattice/v1" + job + bytes(32)).digest()
# counter DRBG: block = sha256(b"mocassin/lattice-rng/v1" + lattice + u64le(i));
# x = le_u64(block[0:8]); Lemire NextBounded; Fisher-Yates for i = 0..k-1, n = 10, k = 3
```

```text
job_seed     884a9517b16620af2257945215f5552c1f5af0580ed2c911456b1a29a4f54ce7  (matches)
lattice_seed 7f601350f114b61ba560fa983218cfd41e41df83953072ff415eafa21adbf394  (matches)
selected     [1, 7, 2]                                                          (matches)
```

The three golden values are reproducible exactly as written in Step 10 §2 and are consistent with ADR 0005's
tuple order and Step 13's `derive_job_seed(master, collection, config, job, purpose)` byte order.

### Resolution verification

| Finding | Round-2 status | Verification |
| --- | --- | --- |
| M1 | Resolved | ADR 0005 tuple order is used (`master, collection, config, job, purpose`); the fields are fixed-width little-endian with a terminal UTF-8 purpose; the project `masterSeed` is required and validated as 32-byte lowercase hex; the golden vector matches. Step 13's byte order now matches. Notes R2-7 and R2-8 remain about the seed-text special case and the request-hash encoding, not about the job-seed contract. |
| M2 | Resolved | Step 10 §2 lines 170–213 list every hashed field and encoding, name the SHA-256 counter DRBG, Lemire `NextBounded`, and Fisher–Yates, and freeze job-seed, lattice-seed, and sampler vectors. The golden vectors reproduce. R2-8 notes the missing populated request-hash vector. |
| M3 | Resolved | A compensation-requested doping with `requested > 0` and no feasible `n_p` now emits `JOB012` and stops generation/preview; `available == 0` or `requested == 0` is explicitly not a `JOB012` failure. This matches `JOB012`, the acceptance criterion, and the preview failure rule (`null` + diagnostics). |
| M4 | Resolved | The ratio is `−Δq_p / Δq_d`, the search scans `n_p` downward with `1e-6`, compensated neutrality uses `1e-6 × max(1, total_positions)`, non-compensated charged lattices are permitted with `JOB016`, and Step 06 guarantees non-zero opposite-sign deltas for compensated dopings. R2-4 records the remaining mixed-compensation edge case. |
| M5 | Resolved | Step 10 §3 now applies every doping to the current lattice and states that a later `A→C` applies to the remaining `A` particles; the narrower rule is removed, matching Step 06 and the data contract. |
| M6 | Resolved | `DopingReport` is build/preview data; only the structured `DopingInfo` is persisted; `DopingApplication` has priority and available primary; `EffectiveFraction` is defined; the data contract §3.4 and Step 11 match. R2-6 notes the undefined textual `DopingId` encoding. |
| M7 | **Partially resolved** | The site→P1 mapping is now explicit and implementable. The dual-DOF rule remains unimplementable as written: it navigates from `CompiledSimulation.ActiveCollectionIndices` to `CompiledEnvironment`, but those indices address `CompiledTransitionModel.Collections` (jump collections), which do not own environments; environments belong to `CompiledSite`/`CompiledPosition`. The set of masks and whether the bit is cleared only at the optimizer's target site or at every active environment are undefined. This affects `UseDualDofCorrection` and therefore the KMC `dof` factor (R2-1, Major). |
| M8 | Resolved | Extensions now live inside `baseSettings`/`overrides`, a non-null array replaces the inherited list, ids are unique and bounded, configs are objects, and `JOB018` covers the extension content/bounds; data contract §2.4 matches. R2-2 records the new `JOB009`/`JOB017` duplicate-optimizer overlap. |
| M9 | Resolved | Step 25 now keys the compiled model by the project model/document hash, clears the preview on edit, and either recompiles with debounce/cancellation or disables the preview; the preview service never compiles. |
| m1 | Resolved | Step 06, data contract, `project-format.md`, and `user-guide/lattice.md` now say `JOB001`–`JOB020`. XC-02 remains a historical record from before this design. |
| m2 | Resolved | The documentation plan adds `docs/reference/diagnostics.md` and `docs/reference/cli.md`; both paths exist. |
| m3 | Resolved | The test table gains canonicalization/persistence, golden seeds/sampler, report round-trip, validation-stage, limits, tiers, and explicitly defers writer/reader integration to Steps 11/13 (nightly). |
| m4 | Resolved | Hard caps are defined and `JobId` is bounded to `1..99,999`; `JOB019`/`JOB020` cover overflow; the parity matrix records the retained original cap. |
| m5 | Resolved | The preview rejects non-finite fractions, treats invalid `(A,B,C)` as diagnostics, throws only for a null project/compiled model, and removes the duplicate job-count override. R2-9 notes the remaining `SimulationId` role in `TryCreateJobCountPreview`. |
| m6 | Resolved | Typed `JobCollectionId`/`JobConfigId`, immutable byte arrays, the `Mocassin.Core.Model.Jobs` move, and PublicAPI/ApiCompat work are in the design and task list. |
| m7 | Resolved | Structural `JOB` checks run through the Core `JobTemplateValidator` in `validate`; compiled-bound checks run in the Build stage; acceptance and tests pin the split. R2-3 records the remaining stage-name/ownership drift. |
| m8 | Resolved | `JOB016`–`JOB020` cover the missing conditions and the overloaded rules are narrowed. R2-2 records the one remaining code overlap. |
| m9 | Resolved | Step 11 owns only package-size limits; Step 12 now constructs `ValidationContext` with the compiled model and registers the Step 10 stage. |
| m10 | Resolved | Step 10 states the direct/transitive references; the architecture row and Step 09 consumption wording are corrected. |
| m11 | Resolved | `DopingInfo` is a versioned, structured, priority-ordered entry list that preserves zero-applied entries and is defined in the data contract. R2-6 notes the id encoding. |
| n1 | Accepted; no change needed | The eight-feature list is within the roadmap's "roughly 6–8" trigger and the step page records owner approval. Keeping the list is a legitimate owner decision. |
| n2 | Resolved | The cache is bounded to 8 entries / 256 MiB with LRU and a test; the policy is sufficient for the preview size. |
| n3 | Resolved | Step 10 owns expansion (`< 10 s`) and generation (`< 1 GiB`, cancellation); Step 11 owns the full package-build budget; the quality strategy records the split. |
| n4 | Resolved | The original 99,999-job cap is retained as `JOB019`; the parity matrix is updated and Step 26's guard wording is now consistent. |

### Findings

| ID | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| **R2-1** | **Major** | **M7's dual-DOF mask rule is still not implementable because the environment set and the clear scope are undefined.** Step 10 says "For each environment of the simulation's active KMC collections (`CompiledSimulation.ActiveCollectionIndices` → `CompiledEnvironment`), clear the optimizer particle bit from `AllowedParticleMask`." `ActiveCollectionIndices` indexes `CompiledTransitionModel.Collections`, whose entries are `CompiledJumpCollection` records with a `MobileParticleMask`; they do not own environments. Environments belong to sites and positions (`CompiledSite.EnvironmentIndex`, `CompiledPosition.EnvironmentIndex`). There is no documented collection→rule→start-position→environment navigation. It is also unclear whether the bit is cleared only in the target site's environment or in every environment reached by the active collections; the latter would remove valid start options at other sites and change the "every mask changed" predicate. The flag selects `dof = 2` versus `dof = 1` in Steps 14/16, so a wrong set changes the KMC time step. Define the exact environment set and the per-environment clear rule, and add a test where only some environments change. | Step 10 §5 lines 376–381 and §2 lines 141–144; `CompiledTransitionModel.cs` lines 20–31; `CompiledSimulation.cs` lines 100–108; `CompiledStructureModel.cs` lines 20–43; Step 14 line 196; Step 16 line 157; Step 08 §9 lines 464–466. |
| **R2-2** | **Minor** | **`JOB009` and `JOB017` both claim the duplicate-optimizer condition.** `JOB009` is "Optimizer reference invalid, duplicate target, or removes every usable start option"; `JOB017` is "Duplicate doping reference in a config, or duplicate `(site, particle)` optimizer". A duplicate `(site, particle)` optimizer therefore has two codes and two possible severities, although both are errors; the Core validator ownership row names both codes. Keep one code (or state precedence) and make the ownership table unambiguous. | Step 10 §6 lines 404, 412, and 421–422. |
| **R2-3** | **Minor** | **The build-stage type name and optimizer ownership still drift across pages.** Step 10 §6 names `JobTemplateBuildValidator` for the Packages `IValidationStage`, while §7 and Step 12 name `JobTemplateValidationStage`. The ownership table also assigns only `JOB010`–`JOB014`, `JOB016`, and `JOB019` to the Packages stage, but `JOB009`'s "removes every usable start option" is compiled-bound and is not assigned to either side explicitly. In addition, Step 08 still says it "resolves the simulation's authored optimizers to compiled `(position, particle)` pairs for the report", while optimizers are authored on job collections and Step 10 owns their expansion. Use one stage name and state which page owns optimizer resolution/reporting. | Step 10 §6 lines 419–422 and §7 lines 427–438; Step 12 line 99; Step 08 §9 lines 464–466; Step 10 §1 lines 78–98 and §5 lines 367–390. |
| **R2-4** | **Minor** | **The global neutrality rule makes a mixed compensated/non-compensated configuration fail with `JOB012`, although a non-compensated charged lattice is documented as permitted with `JOB016`.** Step 10 §3 says "if any doping requested compensation" the total charge must be within tolerance, else `JOB012`; only "if no doping requested compensation" is a non-neutral lattice permitted with `JOB016`. Step 06 allows a non-compensated primary exchange with any charge delta, including a charged one. A config with one compensated doping and one intentionally non-compensated charged doping therefore always fails `JOB012`, and the acceptance criterion's "non-compensated charged lattice is permitted" is false for that mixed case. State the precedence explicitly or make the neutrality check per-doping. | Step 10 §3 lines 252–255 and §6 line 411; Step 06 lines 173–180 and 187–190; Step 10 acceptance lines 597–598. |
| **R2-5** | **Minor** | **The revision changes a documented `.msl` field format while saying there is no package format change.** Step 10 §8 says "no package/results format changes in this step", but the same revision changes `doping_info` in the `.msl` job and lattice payloads from the legacy priority/fraction summary to the `v1:{dopingId}:...` entry list (data contract §3.4). The `.msl` is not implemented yet, so a version bump may not be needed, but the statement is inaccurate and the audit trail should record that this is a pre-implementation contract update. | Step 10 §8 lines 497–499; `02-data-contracts.md` §3.4 lines 1006 and 1016; Step 10 §4 lines 286–295. |
| **R2-6** | **Note** | **The `DopingInfo` `{dopingId}` textual encoding is not defined.** The format is declared machine-readable, but the design only defines the 16-byte RFC 4122 encoding for the lattice-request hash; it does not say whether the string uses the canonical hyphenated lowercase GUID form, a 32-hex form, or another encoding. Specify the exact invariant-culture id encoding so independent readers (results/analysis, GUI) parse the same string. | Step 10 §4 lines 289–293; §2 line 185; `02-data-contracts.md` §3.4 line 1006. |
| **R2-7** | **Note** | **The collection/simulation seed text rule has an unnecessary project-seed-dependent special case.** "A value that is exactly the project's 64-hex seed is decoded, not re-hashed" means the same collection `RngSeed` string is interpreted as bytes only while it equals the project seed, and as UTF-8 text after the project seed changes. That is deterministic but surprising and couples two independent settings. Prefer "any canonical 64-hex value is decoded; every other explicit seed is UTF-8 hashed", or state why the equality rule is required, and add a golden vector for the custom-seed path. | Step 10 §2 lines 149–153; `02-data-contracts.md` line 548; `ProjectMetadata.cs` lines 38–57. |
| **R2-8** | **Note** | **The lattice-request projection has no golden vector and its hex-to-byte decoding is only stated for one field.** The formula writes `structure_hash[32]`, but `ProjectHasher.ComputeStructure` returns a lowercase 64-character hex digest; the text explicitly says only `SpaceGroupDataSha256` is decoded from hex. Add a populated-model request-hash golden vector and state that every hex digest is decoded to 32 bytes before concatenation. | Step 10 §2 lines 170–195; `ProjectHasher.cs` lines 44–50. |
| **R2-9** | **Note** | **`TryCreateJobCountPreview` takes a `SimulationId` while computing totals across the whole template.** The signature is `(Project, JobTemplate, SimulationId)`, but the text says it resolves "the total jobs across the template's collections/configs", which can include collections bound to other simulations. State whether `simulationId` filters the template, selects the default job count, or is redundant. | Step 10 §4.1 lines 327–331 and 350–352; data contract §2.4 lines 680–696. |

No Blocker was found. R2-1 is a Major and must be fixed; R2-2–R2-5 are Minor and should be fixed or explicitly accepted; R2-6–R2-9 are Notes.

### Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

**Verdict: Changes required.** The revision resolves M1–M6, M8, and M9 in substance; the seed, sampler,
compensation, same-site, report, extension, and Step 25 contracts are now implementable, and the published
golden vectors reproduce exactly. M7 is only partially resolved: the P1 image mapping is fixed, but the
dual-DOF rule still cannot be implemented from the written environment navigation and clear scope (R2-1), and
that flag changes the KMC degree-of-freedom factor. Four Minor findings and four Notes remain. The required
repository gates pass. A focused round-3 revision and re-review of the dual-DOF rule (plus the Minor items) is
required before the step can start.

Follow-up owner: Step agent / planner
Follow-up step: Step 10 design revision round 3 (dual-DOF environment set, JOB009/JOB017, stage naming,
mixed compensation, `.msl` `doping_info` statement) and fresh review

## Round 2 resolutions

Design revision commit: `07f14f2e23feff9d9c1f7ff7c46ce4d074a86b43`. The round-2 reviewer text is unchanged; the
step agent owns this section.

| Finding | Resolution | Files |
| --- | --- | --- |
| R2-1 | The dual-DOF environment set is now every `CompiledStructureModel.Environments` entry. For each environment, the predicate is reduced when an optimizer targets a site owning a P1 position with that environment and the particle is allowed. No compiled mask is mutated. The all-reduced rule and one/some/all test are explicit. | Step 10 §5, Tests; Step 08 selection-optimizer wording |
| R2-2 | `JOB009` no longer claims duplicate optimizers; duplicates are only `JOB017`. `JOB009` is reference-invalid in Core and removes-all in the build stage. | Step 10 §6 |
| R2-3 | The stage is consistently `JobTemplateValidationStage`; the ownership table assigns compiled-bound `JOB009` to it, and Step 08 no longer claims to resolve authored optimizers. | Step 10 §6–§7; Step 08 |
| R2-4 | Compensated pairs are checked individually (`JOB012`). A final non-neutral lattice caused by an uncompensated charged doping — including a mixed compensated/non-compensated config — is `JOB016`, not `JOB012`. | Step 10 §3, §6, acceptance |
| R2-5 | The `.msl` `doping_info` format is recorded as a pre-implementation contract update; no published `.msl` exists and no schema bump is claimed. | Step 10 §8; data contract §3.4 |
| R2-6 | `{dopingId}` is the canonical lowercase hyphenated GUID (`ToString("D")`). | Step 10 §4; data contract §3.4 |
| R2-7 | Any 64-hex seed text is decoded; every other explicit seed is hashed. The project-seed-equality special case is removed, and a custom-seed golden vector is added. | Step 10 §2 |
| R2-8 | Every hex digest is explicitly decoded to 32 bytes; a populated synthetic request-hash and lattice-seed vector is added. | Step 10 §2 |
| R2-9 | `TryCreateJobCountPreview` is simulation-scoped: default and effective counts for that simulation's collections, whole-template total, no separate override. | Step 10 §4.1, Tests |

No earlier reviewer text, resolution table, or checklist item was altered.

## Round 3 review

### Reviewer identity and scope

| Field | Value |
| --- | --- |
| Reviewer | Independent planning auditor (round 3; fresh context; no shared conversation with the planner, implementor, step agent, or orchestrator) |
| Review date | 2026-11-14 |
| Branch | `docs/step-10-design` |
| Round 1 frozen commit | `c66b483f6ef064858d0e8082543d2fcec8386ed3` |
| Round 2 revised design commit | `2eb6a5715ea51f324a579ae8974f9e8526005fa9` |
| Round 3 revised design commit | `07f14f2e23feff9d9c1f7ff7c46ce4d074a86b43` |
| Round 3 resolution/status commit | `0a5667a5b1491a6f2bbe17bde9323f5edaec93ae` |
| Record parent | `0a5667a5b1491a6f2bbe17bde9323f5edaec93ae` |
| Design delta reviewed | `git show --stat 07f14f2`: 3 files, 68 insertions, 37 deletions |

> I am a fresh context for round 3. I did not author or implement the reviewed work and have no shared
> conversation with the planner, implementor, step agent, or orchestrator. I read the repository at
> `07f14f2e23feff9d9c1f7ff7c46ce4d074a86b43` and the resolution/status delta at
> `0a5667a5b1491a6f2bbe17bde9323f5edaec93ae` directly rather than relying on the resolution table, and I
> edited and committed only this review record.

The read-only original clone (`/workspace/mocassin-original`) is still absent from this environment. Original
behaviour claims were checked against the Step 08 review and audit text that quote
`JumpSelectionOptimizer.Run` and `RunPostBuildOptimizers`, the Step 06/08 design text, and the current compiled
types. That is a review limitation, not a finding.

### Evidence

Files read in this round:

- `AGENTS.md` §3.8–§3.9, §3.12, §8, §10–§12, and §16; `docs/planning/reviews/README.md` and the design-review
  template; this record's request, round-1/2 text, and round-2 resolutions.
- `docs/planning/steps/step-10-job-templates-and-supercells.md` in full; the complete diff
  `2eb6a57..07f14f2` and the isolated design commit `07f14f2`; `git show --stat 0a5667a`;
  `docs/planning/status.md`; `docs/planning/02-data-contracts.md` §2.4, §3.3, and §3.4;
  `docs/planning/01-target-architecture.md`; `docs/planning/03-quality-strategy.md` §9.4;
  `docs/planning/04-roadmap.md` §3.1; `docs/planning/05-open-decisions.md`;
  `docs/planning/appendix-b-parity-matrix.md`.
- ADR 0005 (statistical parity and RNG), 0009 (contract manifest), 0010 (canonical signatures), 0011 (immutable
  package), 0013 (format compatibility), and 0014 (extension ABI/trust boundary).
- Steps 06, 07, 08, 09, 11, 12, 13, 14, 16, 25, and 26; `docs/reference/project-format.md`,
  `docs/reference/diagnostics.md`, `docs/user-guide/lattice.md`; `docs/development/design-decisions.md`,
  `compilation-pipeline.md`, and `code-structure.md`.
- Current code/contracts used to verify the claimed navigation, types, and vectors:
  `src/dotnet/Mocassin.Compilation/Structure/CompiledStructureModel.cs`,
  `src/dotnet/Mocassin.Compilation/Structure/EnvironmentCompilationStage.cs`,
  `src/dotnet/Mocassin.Compilation/Compiler/CompiledModel.cs`,
  `src/dotnet/Mocassin.Compilation/Simulations/CompiledSimulation.cs`,
  `src/dotnet/Mocassin.Compilation/Transitions/CompiledTransitionModel.cs`,
  `src/dotnet/Mocassin.Core/Project/ProjectHasher.cs`,
  `src/dotnet/Mocassin.Core/Project/ProjectMetadata.cs`,
  `src/dotnet/Mocassin.Core/Ids/TypedIds.cs`,
  `src/dotnet/Mocassin.Core/Model/Particles/Particle.cs`,
  `src/dotnet/Mocassin.Core/Model/Lattice/Doping.cs`,
  `src/dotnet/Mocassin.Core/Model/Lattice/LatticeModelValidator.cs`,
  `src/dotnet/Mocassin.Packages/Projects/Validation/ValidationContext.cs`, and
  `src/dotnet/Mocassin.Packages/Projects/Validation/DiagnosticCatalog.cs`.

Commands run and exact results (before this record was written):

```text
git status --short
# no output (exit 0)

git rev-parse HEAD
# 0a5667a5b1491a6f2bbe17bde9323f5edaec93ae

git cat-file -t 07f14f2e23feff9d9c1f7ff7c46ce4d074a86b43
# commit

git diff --stat 2eb6a57 07f14f2
# 5 files changed, 301 insertions(+), 43 deletions(-)
# The five files include the round-2 record commit 563cd31 and the status line.
# The design-only delta in the reviewed commit is:

git show --stat 07f14f2
# 3 files changed, 68 insertions(+), 37 deletions(-)
# docs/planning/02-data-contracts.md | 2 +-
# docs/planning/steps/step-08-model-compilation-pipeline.md | 7 +-
# docs/planning/steps/step-10-job-templates-and-supercells.md | 96 ++++++++++++++--------

git show --stat 0a5667a
# 3 files changed, 34 insertions(+), 6 deletions(-)
# docs/planning/reviews/step-10.md, docs/planning/status.md, Step 10 status line.

python tools/check_docs_links.py
# Documentation links OK (158 files scanned). (exit 0)

python tools/check_doc_math.py
# Documentation math conventions OK (155 files scanned). (exit 0)

python tools/check_process_consistency.py
# Process consistency: OK (exit 0)

./tools/verify.sh docs --require
# git diff --check passed; Zensical (strict) "No issues found"; DocFX "Build succeeded.
# 0 warning(s) 0 error(s)"; rustdoc completed; "Documentation built in /workspace/site";
# "Closeout gate completed with required checks enabled." (exit 0)

./tools/verify.sh policy --require
# git diff --check passed; build 0 warnings/0 errors; "Public API is compatible with the
# checked-in baseline"; contract artifacts current (18 files); model projection OK
# (5 fixtures); parameterization projection OK (4 fixtures); space-group catalog current
# (274 settings, sha256 1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395);
# process consistency OK; 31 contract tests OK; SPDX headers present in 470 source files;
# no secret patterns in 817 tracked text files; advisories/bans/licenses/sources ok;
# "Closeout gate completed with required checks enabled." (exit 0)
```

The `2eb6a57..07f14f2` stat includes the round-2 reviewer record commit `563cd31`; the actual design revision
is the isolated `07f14f2` commit. The round-3 resolution/status commit `0a5667a` touches only the review record,
the status dashboard, and the Step 10 status line. No code, test, configuration, or design page changed after
`07f14f2`.

Golden-vector check, reimplemented independently from the written specification rather than copied from the
page:

```python
def u32(x): return struct.pack("<I", x)
def sha(b): return hashlib.sha256(b).digest()

master = sha(b"mocassin/seed-text/v1" + b"ceria-seed-1")
job_custom = sha(master + u32(0) + u32(0) + u32(0) + b"mocassin/job/v1")
job_base = sha(bytes(range(32)) + u32(0) + u32(0) + u32(0) + b"mocassin/job/v1")
lattice_zero = sha(b"mocassin/lattice/v1" + job_base + bytes(32))
request = sha(
    b"mocassin/lattice-request/v1"
    + bytes(range(0x00, 0x20)) + bytes(range(0x20, 0x40)) + bytes(range(0x40, 0x60))
    + u32(10) + u32(10) + u32(10) + u32(1)
    + bytes.fromhex("00112233445566778899aabbccddeeff")
    + struct.pack("<d", 0.01)
    + u32(len(b"2026.1")) + b"2026.1" + bytes(range(0x60, 0x80))
    + u32(len(b"symmetry-1")) + b"symmetry-1")
lattice_custom = sha(b"mocassin/lattice/v1" + job_custom + request)
```

```text
job_seed        884a9517b16620af2257945215f5552c1f5af0580ed2c911456b1a29a4f54ce7  (matches)
lattice_seed    7f601350f114b61ba560fa983218cfd41e41df83953072ff415eafa21adbf394  (matches)
sampler [1, 7, 2]                                                              (matches)
custom job_seed b6d2d1614b2b819445764523ef5613a47b7712a218e303d1a3d2cdb4f71a5c2e  (matches)
request_hash    5719de5efea8b57ef6960229bf2248de0da3bb497c2f29507d1742157f878295  (matches)
custom lattice  a497141d26d005eb5caa17025b3c1e3564f31f8d46518a7a36d311623be464d2  (matches)
```

The sampler check used the written counter DRBG (`SHA-256("mocassin/lattice-rng/v1" || lattice_seed ||
u64_le(counter))`, first eight bytes little-endian), Lemire rejection, and Fisher–Yates for `i = 0..k-1`,
`j = i + NextBounded(n - i)`. It reproduces `[1, 7, 2]` exactly.

### Resolution verification

| Finding | Round-3 status | Verification |
| --- | --- | --- |
| R2-1 (M7) | Resolved | The environment set is now **every `CompiledModel.Structure.Environments` entry** (`CompiledStructureModel.Environments`), and the public navigation exists: `CompiledSite.PositionIndices`, `CompiledPosition.EnvironmentIndex` and `AllowedParticleMask`, and `CompiledEnvironment.AllowedParticleMask`/`SiteIndex`. A shared stable environment (`SiteIndex = -1`; every stable site's positions have `EnvironmentIndex = 0`) is reduced when any optimizer targets a stable site and its particle is allowed at that position. A site-specific unstable environment (`SiteIndex >= 0`) is reduced only by an optimizer on its owning site. The predicate is implementable for both shapes. The all-reduced rule matches the Step 08 review quote of `JumpSelectionOptimizer.Run` (`isReducedMask \|= optimizedMask == originalMask`): keep the flag when there is no optimizer, when any environment is unchanged (including the mixed case), or when the bit is already clear; clear only when every environment is reduced. The compiled model is not mutated; the predicate is per job. The "three environments, one/some/all reduced" test is in the test table. Steps 14/16 still read the resulting `.msl` bit (`dof = 2`/`1`), and Step 08 now says it exposes compiled data without interpreting authored optimizers. Consistent. The original clone is absent, so the original-code check relies on the Step 08 review quote, not a fresh source read. |
| R2-2 | Resolved | `JOB009` is now "reference invalid or removes every usable start option"; `JOB017` owns duplicate `(site, particle)` optimizers. The ownership table assigns `JOB009`'s reference check to Core and its removes-all check to the Packages stage, and `JOB017` to Core. The explicit sentence "Duplicate `(site, particle)` optimizers are owned only by `JOB017`; `JOB009` does not duplicate that rule" removes the overlap. |
| R2-3 | Resolved | The stage is `JobTemplateValidationStage` in §6, §7, and Step 12; no `JobTemplateBuildValidator` remains in the repository. The ownership table names the compiled-bound `JOB009` half explicitly. Step 08 now says it "exposes the compiled site, position, environment, and selection masks; it does not interpret authored optimizers", and Step 10 owns expansion and the dual-DOF predicate. |
| R2-4 | Resolved for the asked case | Each applied compensated pair is checked individually at `1e-6 × max(1, n_p + n_d)`; a compensation-requested doping that cannot realize a pair is `JOB012`; `hasUncompensatedCharge` makes a mixed compensated/non-compensated config `JOB016` instead of `JOB012`; the final `JOB012` fallback applies only when no uncompensated charge was applied. The acceptance criterion and `JOB016` table row now include the mixed case. R3-1 records the adjacent pre-existing-base-lattice case that this revision does not classify. |
| R2-5 | Resolved | §8 now says `doping_info` is a pre-implementation `.msl` field defined by Step 10 and written by Step 11, with no schema-version bump because no `.msl` exists, and no `.msr`/`.mcs` change. Data contract §3.4 and Step 11 map the same field. |
| R2-6 | Resolved | `{dopingId}` is the canonical lowercase hyphenated GUID (`Guid.ToString("D")`) in Step 10 §4 and data contract §3.4. `DopingId.ToString()` already uses `Value.ToString("D", CultureInfo.InvariantCulture)`, so the planned encoding matches the current typed ID. |
| R2-7 | Resolved | The project-seed-equality special case is gone. Any 64-character lowercase hex seed text is decoded; every other explicit seed text is hashed with `SHA-256("mocassin/seed-text/v1" || UTF8(seedText))`. The custom seed `ceria-seed-1` vector reproduces `b6d2d161…` exactly. |
| R2-8 | Resolved | Every `[32]` input is stated to be a SHA-256 digest decoded from its canonical 64-character lowercase hex form, including `structure_hash`, `particle_hash`, `lattice_hash`, and `SpaceGroupDataSha256`; the version strings are UTF-8 with `u32_le` length prefixes. The populated synthetic vector reproduces request hash `5719de5e…` and, with the custom job seed, lattice seed `a497141d…` exactly. |
| R2-9 | Resolved | `TryCreateJobCountPreview` is explicitly simulation-scoped: `DefaultJobCount` is that simulation's `Simulation.JobCount`; `EffectiveJobCount` is the job total for the template collections bound to that simulation after collection/config overrides; `TotalJobs` is the whole-template total. There is no separate count-override parameter. The test row is updated. |

### Findings

| ID | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| **R3-1** | **Minor** | **The `JOB012` final-neutrality fallback misclassifies a pre-existing non-neutral base lattice as an unbalanced compensation failure.** `hasUncompensatedCharge` is set only by an *applied doping* with compensation off and a nonzero charge delta. If the generated base lattice is itself non-neutral — Step 06 does not require the first particle of each stable set to produce a neutral default, and `Particle.Charge` is arbitrary — then a config with no doping, or with only compensated/isovalent dopings, reaches the final check with `hasUncompensatedCharge = false` and a non-zero `TotalChargeAfter`. The design then emits `JOB012`, although no compensated pair was unbalanced and the acceptance criterion says a non-compensated charged lattice is permitted with `JOB016`. `JOB012`'s descriptor ("compensation pair cannot be realized, or an applied compensated pair is not charge-balanced") and `JOB016`'s descriptor ("because an uncompensated charged doping was applied") both fail to cover a charged base lattice. Define whether a non-neutral base lattice is a model error, a `JOB016` warning, or excluded by comparing the doping-induced change (`TotalChargeAfter - TotalChargeBefore`) rather than the absolute final total. | Step 10 §3 lines 262–270 and acceptance lines 627–628; `JOB012`/`JOB016` rows lines 432 and 436; Step 06 lines 177–183 (non-compensated charged exchange allowed; no base-neutrality invariant); `Particle.cs` line 25 (arbitrary `ChargeElementary`); `LatticeModelValidator.cs` lines 286–289 (only compensation sign checks, no total-charge check). |
| **R3-2** | **Note** | **The `hasUncompensatedCharge` parenthetical contradicts its own predicate.** The sentence says the flag is true for "an applied doping [that] has compensation off and a nonzero charge delta (including an isovalent/zero-delta doping, which does not count)". An isovalent doping has zero delta, so it cannot be an example of a nonzero delta; the intent is clearly "excluding". Reword to avoid an implementer setting the flag for an isovalent doping. | Step 10 §3 lines 264–265. |
| **R3-3** | **Note** | **The `validate` acceptance line names the whole `JOB009` code as structural, although the removes-all half is compiled-bound.** The acceptance criterion says `validate` reports structural `JOB001`–`JOB009`/`JOB015`/`JOB017`/`JOB018`/`JOB020` errors without a compiled model. The §6/§7 ownership split puts only `JOB009`'s reference check in Core; its removes-all check needs the compiled model and runs in the Packages `JobTemplateValidationStage`. Align the acceptance wording with the split (for example, "`JOB001`–`JOB008` and the `JOB009` reference check"). | Step 10 acceptance line 629; §6 ownership table lines 446–447; §7 lines 458–462. |

No Blocker or Major was found. R3-1 is a Minor edge case; R3-2 and R3-3 are Notes. They do not reopen the
architecture, the seed/sampler contract, the dual-DOF navigation, or the mixed compensated/non-compensated rule.

### Verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Waived

**Verdict: Ready with notes.** R2-1–R2-9 are resolved in substance: the dual-DOF environment set is explicit
and navigable from the public compiled types, the all-reduced rule matches the original accumulation, the
`JOB009`/`JOB017` ownership and stage naming are consistent, the mixed compensated/non-compensated case is
`JOB016`, the `.msl` `doping_info` change is recorded as a pre-implementation contract update, and the
`DopingId`, seed-text, hex-decoding, and `TryCreateJobCountPreview` Notes are addressed. All six published
golden values reproduce independently. One Minor (R3-1, the pre-existing non-neutral base lattice) and two
Notes (R3-2, R3-3) remain; the human owner must accept them explicitly or request a focused revision. The
required repository gates pass. This record is evidence and does not authorize the step.

Follow-up owner: Step agent / planner
Follow-up step: Step 10 implementation start after owner acceptance of R3-1–R3-3, or a focused round-4
revision for the base-lattice neutrality classification

## Round 3 resolutions

Design revision commit: `bb62e405f27199173cdabc256f6f6d43aa033da1`. The round-3 reviewer text is unchanged; the
step agent owns this section.

| Finding | Resolution | Files |
| --- | --- | --- |
| R3-1 | A non-neutral **base** lattice is now explicitly a model property: it is reported through `TotalChargeBefore`/`IsChargeNeutral` but never triggers `JOB012` or `JOB016` by itself. `JOB012` is only the unrealizable/unbalanced compensated-pair failure. `JOB016` requires an applied uncompensated charged doping. Step 06 has no base-neutrality invariant, so Step 10 does not invent one. | Step 10 §3, §6, Tests |
| R3-2 | The parenthetical now says isovalent/zero-delta dopings are **excluded** from `hasUncompensatedCharge`. | Step 10 §3 |
| R3-3 | The `validate` acceptance line now names `JOB001`–`JOB008`, the `JOB009` reference check, `JOB015`, `JOB017`, `JOB018`, and `JOB020`; the compiled-bound half of `JOB009` is explicitly build-stage-only. | Step 10 acceptance |

No earlier reviewer text, resolution, or checklist item was altered.

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
| 2 | 2026-11-14 | `2eb6a57` | Independent planning auditor (fresh context) | Changes required |
| 3 | 2026-11-14 | `07f14f2` | Independent planning auditor (fresh context) | Ready with notes |
| 4 | 2026-11-14 | `bb62e40` | Pending fresh auditor | Pending |

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
| R2-1 | Resolved in round-3 design revision | Step agent | `07f14f2` |
| R2-2 | Resolved in round-3 design revision | Step agent | `07f14f2` |
| R2-3 | Resolved in round-3 design revision | Step agent | `07f14f2` |
| R2-4 | Resolved in round-3 design revision | Step agent | `07f14f2` |
| R2-5 | Resolved in round-3 design revision | Step agent | `07f14f2` |
| R2-6–R2-9 | Resolved in round-3 design revision (see round-2 resolutions) | Step agent | `07f14f2` |
| R3-1 | Resolved in round-4 design revision | Step agent | `bb62e40` |
| R3-2 | Resolved in round-4 design revision | Step agent | `bb62e40` |
| R3-3 | Resolved in round-4 design revision | Step agent | `bb62e40` |
