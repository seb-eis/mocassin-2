# Step 07 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-07.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record
> state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 07 — Validation, Diagnostics, and Model Statistics |
| Request owner | Step agent |
| Orchestrator (step agent / planner) | Step agent / planner |
| Review initiated by | Planner |
| Reviewed commit | `dc73ee40954070b8e95bf0713f8df4526b208ba6` |
| Docs branch | `docs/step-07-design` |
| Review date | 2026-11-11 |
| Reviewer | Independent planning auditor (fresh context, round 1; no shared conversation with the planner or implementor) |

Step page: [`Step 07 — Validation, Diagnostics, and Model Statistics`](../steps/step-07-validation-and-diagnostics.md)

> Orchestration: the step agent or planner (the orchestrator) freezes the design, requests the reviewer, applies
> fixes autonomously on the design docs or a maintenance branch, adds resolutions without altering reviewer
> text, and requests re-review until the reviewer no longer identifies issues. The human owner approves the
> final verdict and the start of implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | B. Model definition |
| Depends on | 03, 04, 05, 06 |
| Delivers | A load/model/cross-domain/feasibility validation pipeline, stable diagnostic catalog, model statistics, and the headless `mocassin validate` command. |

## Design decisions reviewed

- [x] Step page design decisions — reviewed in full; findings M1–M6 and m1–m9.
- [ ] Data contracts and format changes, including the `validation-v1` report in `02-data-contracts.md` §5.6 — report/load-failure contradiction (M1), hash format (m5), manifest tooling (M6).
- [x] Architecture and module boundaries, including the `Mocassin.Packages.Projects.Validation` placement — placement is coherent; the Build-stage contract is incomplete (M3).
- [x] ADRs and open decisions, including the resolved validation/statistics entries — both entries are recorded; no new ADR is required for this CLI report, but the manifest integration is unresolved (M6).
- [x] Quality strategy and test plan — §9.7 defines the right test families; cancellation and hint coverage are overstated (m4, n3).
- [ ] Parity matrix and documentation plan — matrix rows are present, but nav/index, PublicAPI, SPDX, and closeout/audit planning are missing (m6).
- [x] Licensing and provenance — no new dependency and no GPL-derived content; the missing SPDX task is folded into m6.
- [ ] Post-implementation audit plan — no closeout/audit task is recorded on the step page (m6).

## Dependency and interface checks

- [x] Dependencies 03–06 are `Done` and their diagnostic/preview contracts are stable — all four steps are Done/tagged; the preview APIs are the implemented `TryCreatePreview` contracts.
- [x] The existing `ProjectValidationService` remains the model/domain composition; Step 07 does not re-emit domain diagnostics — confirmed in the design and the implemented static composition.
- [ ] `ProjectStore` load diagnostics and `ProjectValidationPipeline` stage `Load`/`Model` ownership are unambiguous, including `ValidateOnLoad` — the pipeline cannot run or time `Load`, and double validation is not ruled out for the GUI (M2).
- [ ] Inputs, outputs, types, formats, and schema versions are defined, including `validation-v1` — the report cannot represent load/usage failures (M1), and its manifest/schema integration is unsupported by the current generator (M6).
- [ ] Stage `Build` is an extension point owned by Steps 09/10/12, not implemented here — the extension contract is incomplete and is not recorded in those step pages (M3).
- [ ] No unresolved cross-step interface question remains — M2, M3, and m9 remain.

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable — the deferred-scope section is the non-goals; the ceria warning set is not enumerated (n3).
- [ ] Design decisions are complete and internally consistent — M4, M5, m2, m8, and m9.
- [x] Required tests, property tests, golden fixtures, and performance targets are defined — the test table covers the main paths; tiers and per-code hint coverage need tightening (m4, n3).
- [ ] Documentation, status, parity, and audit updates are planned — the page list exists, but nav/index, PublicAPI, SPDX, closeout, and audit tasks are missing (m6).
- [x] Licensing, provenance, and dependency checks are resolved — no new dependency or GPL-derived content; SPDX task in m6.
- [x] Risks and mitigations are realistic — the risk table matches the known failure modes.
- [ ] Code layout and namespace plan follow the structure policy — namespaces are placed correctly, but there is no file layout for the large catalog and no 800-line/file split plan (m6).
- [ ] No hidden scope, stale placeholders, or unowned decisions — contract-generator, PublicAPI, docs-nav, and closeout work are hidden scope (M6, m6); `SymmetryDiagnosticCatalog` is described as existing but does not (m1).

## Step-specific checks

- [x] The pipeline composes existing validators instead of duplicating `ProjectValidator`, `ProjectValidationService`, or Symmetry validation — confirmed; the design keeps `ProjectValidationService` as stage 2.
- [ ] `ValidationOptions`, `ValidationResult`, `DiagnosticSummary`, and stage results are sufficiently specified for the CLI and GUI to share them without reimplementing rules — `ValidationContext` is undefined, `Load` cannot be represented, and statistics nullability is contradictory (M2, M3, M5).
- [ ] `ProjectStoreOptions.ValidateOnLoad` avoids double validation and preserves existing store semantics — only the CLI path is specified; GUI/pipeline consumers can still double-validate (M2).
- [ ] Stage order, skipping, cancellation, and error behavior are defined — skipping is defined for 1–4, but Build gating and load-stage behavior are not (M2, M3, m4, n2).
- [ ] `ENM018` and `SIM038`–`SIM042` have clear ownership, severity, promotion policy, and no cascade or duplicate with `TRN020`, `SIM003`, or earlier diagnostics — `SIM039` duplicates `SIM003`/`TRN022`, `SIM038`/`SIM040` overlap, and the `ENM018` boundary is not precise (M4, m8).
- [ ] The diagnostic catalog covers Core/Symmetry/package codes, reserves `JOB` for Step 10 without stubs, and has a credible completeness/docs-drift test — `JOB` is correctly reserved; the completeness test targets non-existent/internal types (m1).
- [ ] `ModelStatistics` fields, sources, nullability, cell-count behavior, and rule-count advisory semantics are coherent with Steps 02/05/08/10/16 — pair source is ambiguous, rule count is `int`, and nullability conflicts with the contract (M5, m2, m3).
- [ ] The `validation-v1` report is a stable external contract; its fields, ordering, deduplication, `valid` policy, and exit codes are testable — load/usage failures cannot be represented, the hash format is ambiguous, and the manifest integration is unsupported (M1, M6, m5).
- [ ] CLI loading, human output, JSON output, exit codes, and `--stats` behavior are specified — the load-failure and lock exit-code paths are not implementable as written (M1).
- [x] GUI navigation and statistics consumption are a contract only; no GUI is implemented in Step 07 — confirmed; panels/navigation remain Steps 24–25.
- [x] Performance targets are realistic and distinguish validation from opt-in statistics — targets are separated; the new budgets need a recorded rationale (n1).
- [x] Validation never mutates the project and is deterministic — the hash-before/after test and `DiagnosticBag` ordering support this.
- [x] The feature list and split trigger fit one audit sitting — six features plus closeout and 2–3 person-weeks are within the roadmap trigger; the size estimate is not recorded (n4).

## Evidence

**Review round 1** — frozen design commit `dc73ee40954070b8e95bf0713f8df4526b208ba6`; review request commit
`eadd6d9` only adds this record and the status/step-page links. I am a fresh context: I did not author or
implement this work and used no handoff, summary, or prior conversation. The worktree
`/workspace/playground/step07-design` was clean at `eadd6d9`; all evidence below is from repository inspection at
that state and the frozen design content.

Files reviewed:

- `AGENTS.md` §3.8–§3.13, §7.2, §8, §11, §16; `docs/planning/reviews/README.md`; the review template.
- `docs/planning/steps/step-07-validation-and-diagnostics.md`; Step 03–06 pages, and the Step 05/06 review and
  audit records for the prior standard and dependency boundaries.
- `docs/planning/02-data-contracts.md` §5.5–§5.6, `03-quality-strategy.md` §9.7, `05-open-decisions.md`,
  `appendix-b-parity-matrix.md`, `04-roadmap.md` §3.1, `status.md`.
- `docs/development/architecture.md`, `docs/development/design-decisions.md`,
  `docs/development/testing-and-parity.md`, `docs/development/code-structure.md`, `docs/reference/index.md`,
  `docs/reference/project-format.md`, `docs/troubleshooting/index.md`, `zensical.toml`.
- Implemented APIs at the frozen commit: `Mocassin.Packages/Projects/ProjectValidationService.cs`,
  `ProjectStore.cs`, `ProjectStoreOptions.cs`, `ProjectStoreResults.cs`; `Mocassin.Core/Editing/ProjectEditor.cs`;
  `Mocassin.Core/Diagnostics/Diagnostic.cs`, `DiagnosticBag.cs`, `ModelPath.cs`, all Core diagnostic-code classes;
  `Mocassin.Symmetry/Diagnostics/*`; `StructurePreview`/`IStructurePreviewService`,
  `EnergyCostPreview`/`ClusterCostPreview`/`IEnergyPreviewService`,
  `TransitionPreview`/`ITransitionPreviewService`; `Mocassin.Core/Project/ProjectHasher.cs`;
  `Mocassin.Cli/Program.cs`; `contracts/manifest.json`; `tools/contracts/generate.py`; `tools/verify.sh`.

Commands run (from `/workspace/playground/step07-design`):

```sh
git status --short
# clean

git log --oneline -5
# eadd6d9 docs: request step 07 design review [skip ci]
# dc73ee4 docs: prepare step 07 validation and diagnostics design [skip ci]

git diff --stat dc73ee4^..dc73ee4
# 9 files changed, 470 insertions(+), 166 deletions(-)

git diff --stat dc73ee4..eadd6d9
# docs/planning/reviews/step-07.md | 172 +++
# docs/planning/status.md          |   9 +-
# docs/planning/steps/step-07-validation-and-diagnostics.md | 2 +-
# (record/status links only)

python tools/check_process_consistency.py
# Process consistency: OK

python tools/check_docs_links.py
# Documentation links OK (143 files scanned).

zensical build --strict
# Build started; No issues found; Build finished in 0.27s

python tools/contracts/generate.py --check
# Contract artifacts are current (17 files)

./tools/verify.sh contracts
# Contract artifacts current; space-group catalog current; process consistency OK;
# 18 tests OK; SPDX headers present in 285 source files; no secrets; cargo-deny ok

./tools/verify.sh structure
# Passed: 1/1 (Mocassin.Architecture.Tests)
```

Targeted read-only checks:

```sh
grep -n "REQUIRED_FORMATS\|set(formats) ==" tools/contracts/generate.py
# lines 20-33 and 178: formats must be exactly the eight required ids

grep -rn "ValidateOnLoad" src/dotnet docs/planning
# only the Step 07 page; ProjectStoreOptions has no such property

grep -rn "SymmetryDiagnosticCatalog\|CoreDiagnosticCatalog" src/dotnet
# no matches: neither catalog type exists yet

grep -n "internal static class" src/dotnet/Mocassin.Symmetry/Diagnostics/*.cs
# both STR/SYM code classes are internal

grep -n "ExpectedRuleCount" src/dotnet/Mocassin.Symmetry/Transitions/TransitionPreview.cs
# int ExpectedRuleCount

grep -n "CancellationToken" src/dotnet/Mocassin.Symmetry/*/I*PreviewService.cs
# no matches

grep -n "Convert.ToHexStringLower" src/dotnet/Mocassin.Core/Project/ProjectHasher.cs
# lowercase hexadecimal, no sha256: prefix

grep -rn "ProjectValidationPipeline\|IValidationStage" docs/planning/steps/step-0[89]*.md \
  docs/planning/steps/step-1[012]*.md
# no matches: later steps do not record the extension point
```

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M1 — The CLI failure paths cannot produce the promised `validation-v1` report and exit codes.** `mocassin validate --json` must print one JSON report whose `projectId`, `modelHash`, and `documentHash` are required. The pipeline takes an already-loaded `Project`, and `ProjectStore` returns `Project? = null` for a missing, unreadable, malformed, or schema-invalid file. Malformed JSON is required to exit `1`, but no `Project` exists to supply those fields; usage (`2`) and I/O/lock (`3`) failures have the same problem. `ProjectStore` emits `PROJ002` for both a missing file and malformed JSON, so the CLI cannot map `3` versus `1` without an unspecified file-system pre-check. Exit `3` for “locked for editing” is not reachable on the read-only load path, which never acquires or checks the lock. | Step page lines 281–301 and 412; `02-data-contracts.md` lines 1292–1342; `ProjectStore.cs` lines 340–373 and 395–463; `ProjectStoreResults.cs` lines 12–38; `ProjectDiagnosticCodes.cs` (`InvalidDocument` covers missing file, BOM, UTF-8, JSON, and schema failures). |
| **Major** | **M2 — `Load` stage ownership, `Stages` reporting, and `ValidateOnLoad` double validation are unresolved.** The stage table says `Load` “Always” runs and `Stages` records per-stage elapsed time, but the pipeline receives a `Project` plus optional load diagnostics; it cannot load, fail, or time the load itself, and a direct GUI call has no load stage at all. Only the CLI path is told to set `ValidateOnLoad = false`; the GUI contract also calls the pipeline, so a GUI loading through the default store would validate the model twice. `ProjectStore` currently applies its validator unconditionally and `ProjectStoreOptions` has no such option. | Step page lines 124–169, 285–286, 303–316, 351; `ProjectStore.cs` lines 41 and 462; `ProjectStoreOptions.cs` lines 8–29. |
| **Major** | **M3 — The Build-stage extension point is not implementable as specified.** `IValidationStage.Validate` takes a `ValidationContext` that is never defined. There is no rule for when `additionalStages` run, how their declared `ValidationStage` interacts with the built-in stages (including a stage declaring `Model`), how Build skips after earlier errors, or how `validate` is prevented from running Build; “validate never runs Build” is only a caller convention. Steps 09/10/12 pages do not mention `IValidationStage` or `ProjectValidationPipeline`, so the assigned ownership is not recorded on the receiving side. | Step page lines 124–169, 153–154, 190, 342; `grep` over Steps 08–12 returns no match for either type; no `ValidationContext` declaration in the tree. |
| **Major** | **M4 — `SIM038`–`SIM040` overlap and cascade with existing model diagnostics, so the claimed no-duplication policy is not met.** `SIM039` (“a KMC simulation has no unstable site”) can only occur when the simulation has no kinetic transition (otherwise `TRN022` is an error) or its transition set is empty (`SIM003` warning); it therefore duplicates `SIM003`/`TRN022` rather than adding a distinct check. `SIM038` (“no mobile start particle” per simulation) and `SIM040` (“no mobile particle for any authored simulation”) are not distinguished, and neither is suppressed when the other or `SIM003` fires. | Step page lines 176–192 and 404–405; `step-04-transition-model.md` line 390 (`TRN022`); `step-06-lattice-and-simulation-models.md` line 350 (`SIM003`). |
| **Major** | **M5 — Statistics nullability contradicts the external contract and acceptance criteria.** The step page computes statistics when stages `Load`–`CrossDomain` completed without errors and says only Model errors omit them; `SIM041`/`SIM042` are Feasibility errors, so a model with those errors would still carry statistics. The data contract says statistics is `null` when “the model has errors”, and the acceptance/tests say “`--stats` … omitted on invalid models” and “`null` on invalid models”. A policy-promoted warning or a statistics-preview error can also make `valid` false while `Statistics` remains non-null, so `--stats` can change validity. | Step page lines 150–151, 166, 274–275, 376, 408; `02-data-contracts.md` lines 1332 and 1340–1341. |
| **Major** | **M6 — The `validation-v1` manifest/schema claim conflicts with the implemented contract tooling.** The design says the report is added to `contracts/manifest.json` as `validation-v1` with a generated JSON Schema. `tools/contracts/generate.py` requires `formats` to be exactly the eight binary/format ids (`REQUIRED_FORMATS`) and has no report/`validation` slot; adding a ninth format or a new top-level key would fail `validate_manifest` or be ignored, so `verify.sh contracts` would fail. The task list says only “Add the validation-v1 contract and generated schema/check” and does not mention generator, generated-artifact, or version-constant changes. | `tools/contracts/generate.py` lines 20–33 and 171–180; `tools/contracts/README.md` generated outputs; step page lines 360 and 374; `02-data-contracts.md` lines 1342–1343. |
| **Minor** | **m1 — The catalog completeness test is specified against types that do not exist or are internal.** `SymmetryDiagnosticCatalog` does not exist in the tree and cannot “become public”. `StructureDiagnosticCodes` and `SymmetryDiagnosticCodes` are `internal static class`, so a test cannot enumerate their “public code constants” from outside `Mocassin.Symmetry`; the test needs `InternalsVisibleTo`/reflection or the constants/catalog must be made public. The catalog also has to register `ProjectDiagnosticCodes` from `Mocassin.Core.Project`, not only `Mocassin.Core.Diagnostics`. | Step page lines 218–228; `Mocassin.Symmetry/Diagnostics/*.cs` line 12; `grep` for `SymmetryDiagnosticCatalog` returns no code. |
| **Minor** | **m2 — `EstimatedRuleCount` and `MaxEstimatedRuleCount` are `int` and sum `int` preview counts, contrary to the design's own overflow rationale and the Step 05 review's long-statistics fix.** `TransitionPreview.ExpectedRuleCount` is `int`; a sum over up to 100 transitions can overflow before the 1,000,000 budget check. Make the statistic and budget `long` (or accumulate in `long`) and state the boundary. | Step page lines 253–254, 261–262, 272–273; `TransitionPreview.cs` line 35; `docs/planning/reviews/step-05.md` m10. |
| **Minor** | **m3 — `EstimatedPairInteractionCount` has no defined source, and the report does not enumerate statistics fields.** `EnergyCostPreview` exposes both `PairImageCount` (sampled pair images) and `UniquePairInteractionCount` (distinct `EnergyId` tables); the design names neither. `validation-v1` says statistics uses “camelCase fields matching ModelStatistics” without listing them, so the schema and docs can drift. | Step page lines 251 and 267–273; `EnergyCostPreview.cs` lines 10–19; `02-data-contracts.md` line 1340. |
| **Minor** | **m4 — The cancellation claim is finer than the preview APIs permit.** The design says cancellation is checked “per transition/cluster in the statistics service”, but `IStructurePreviewService`, `IEnergyPreviewService`, and `ITransitionPreviewService` take no `CancellationToken`, and `ProjectValidationService.Validate` has none; cluster cost is computed in one `TryCreatePreview` call. Only between-stage and between-transition checks are implementable without changing those APIs. | Step page lines 327–328; preview interfaces; `ProjectValidationService.cs` lines 31–105. |
| **Minor** | **m5 — The `validation-v1` hash format is ambiguous and conflicts with the Step 01 hash API.** The example shows `"modelHash": "sha256:…"` and `"documentHash": "sha256:…"`, while `ProjectHasher.Compute`/`ComputeDocument` return lowercase hexadecimal without a prefix, and `reference/project-format.md` says all hashes return lowercase hexadecimal. Specify whether the CLI prefixes or the report uses the raw value. | `02-data-contracts.md` lines 1302–1303 and 1326–1327; `ProjectHasher.cs` lines 26–60; `docs/reference/project-format.md` lines 410–416. |
| **Minor** | **m6 — The implementation/documentation plan omits required gates and integration points.** New public types in `Mocassin.Packages`/`Mocassin.Symmetry` need `PublicAPI.Unshipped.txt`/ApiCompat work (Steps 03–06 each list it; Step 07 does not). There is no SPDX/licence task, no closeout/audit/status/parity task, and no plan to add `docs/reference/diagnostics.md`/`cli.md` to `zensical.toml` navigation and `docs/reference/index.md`; the catalog with roughly 100 descriptors also has no file-layout plan for the 800-line/file limit. | Step page lines 348–393; `docs/development/code-structure.md` limits; `zensical.toml` navigation; `docs/reference/index.md`; compare the Step 03–06 closeout and PublicAPI tasks. |
| **Minor** | **m7 — `ValidationOptions` cell counts are unvalidated while the preview services reject non-positive counts.** `ValidationOptions` accepts any `int` for `CellCountA/B/C`; `StructurePreviewService.TryCreatePreview` throws `ArgumentOutOfRangeException` for values `<= 0`. The pipeline's “does not throw; append diagnostics” contract needs either option validation or a documented exception boundary. | Step page lines 98–102 and 274–275; `StructurePreviewService.cs` lines 83–87. |
| **Minor** | **m8 — The `ENM018` boundary is not defined precisely enough to implement or test.** The design says a path leaves “the relevant stable or unstable environment cutoff” but does not say which environment/cutoff applies to each path position, whether the comparison is inclusive, or how a path crossing multiple environments is reported. Step 05 defines only the constant/help anchor. The test table's “fires only for resolved paths outside the relevant cutoff” cannot be written unambiguously. | Step page lines 178, 191–192, 404; Step 05 lines 314 and 331–332, 502–506. |
| **Minor** | **m9 — Two small internal inconsistencies.** `SIM042` says the budget is “configurable in ValidationOptions via a future settings record”, but `ValidationOptions` already declares `MaxEstimatedMemoryBytes`; and §2 lists the Build owners as Steps 09/10/12 while §4 and §11 also include Step 11. | Step page lines 101–102, 153–154, 183, 189–190, 342. |
| **Note** | **n1 — The new hard budgets have no recorded rationale.** 1,000,000 estimated rules and 2 GiB estimated memory are new policy limits with no reference in the roadmap, Step 08, or the original design. Record where they come from and how they relate to Step 08/11 limits. | Step page lines 101–102 and 182–183. |
| **Note** | **n2 — `ValidationStageStatus.Failed`, `DiagnosticCount`, and `Elapsed` semantics are undefined.** No stage ever sets `Failed`; per-stage counts after global deduplication need a rule; `Elapsed` for a caller-owned Load cannot be measured (see M2). | Step page lines 86–91, 108–112, 152. |
| **Note** | **n3 — Ceria's “documented warning set” and per-code hint/path coverage are not specified.** The acceptance criterion does not enumerate the expected warnings; the statement that existing Step 03–06 tests count for “severity, path, and hint” overstates current coverage (only two tests assert a `Hint`). | Step page lines 368–370 and 403; `grep -rn "Hint" src/dotnet/*Tests` returns two assertions. |
| **Note** | **n4 — No size estimate or split trigger is recorded on the step page as AGENTS §7.2 asks.** The seven-row feature list (six features plus closeout) and the roadmap's 2–3 person-week estimate are still within the 6–8-feature trigger, so this is a recording gap, not a split. | Step page lines 19–27; `04-roadmap.md` line 63 and §3.1. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Changes required

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner or implementor.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

**Verdict: Changes required.** Six Major findings (M1–M6) and nine Minor findings (m1–m9) remain, plus four
Notes (n1–n4). The core architecture is sound: a stage pipeline over the existing `ProjectValidationService`,
an immutable `ValidationResult`, a static catalog, and preview-backed opt-in statistics fit the implemented
Steps 03–06 APIs and the project boundaries. The repository checks pass. Before implementation, the design must
fix the external CLI/report contract for load and usage failures (M1), make the Load and Build stage boundaries
and `ValidateOnLoad` ownership explicit (M2, M3), remove the feasibility-diagnostic overlap/cascade (M4), choose
one statistics-nullability rule across the step page, data contract, and acceptance tests (M5), and reconcile the
`validation-v1` manifest/schema claim with `tools/contracts/generate.py` (M6). No Blocker was found.

Follow-up owner: Step agent / planner
Follow-up step: Step 07 design revision and fresh re-review (round 2)

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
