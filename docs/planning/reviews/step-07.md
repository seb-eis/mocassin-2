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
| Reviewed commit | `60437ca07219368dca745b4c9e9f056a543bde1a` (round 3; round 2 reviewed `4ddd571cacb5e714f246278005531b8ee4a73b2e`; round 1 reviewed `dc73ee40954070b8e95bf0713f8df4526b208ba6`) |
| Docs branch | `docs/step-07-design` |
| Review date | 2026-11-11 |
| Reviewer | Independent planning auditor (fresh context, round 3; rounds 1 and 2 were separate fresh contexts; no shared conversation with the planner or implementor) |

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

### Re-review round 2 — Evidence

**Review round 2** — frozen design commit `4ddd571cacb5e714f246278005531b8ee4a73b2e`; HEAD
`3f576a156a92525098a26922810e6067f5f63224` adds only the round-2 request, resolutions, and status. I am a
fresh context: I did not author or implement this work, I did not participate in round 1, and I used no
handoff, summary, or prior conversation. The worktree `/workspace/playground/step07-design` was clean at
`3f576a1`; all round-2 evidence is from repository inspection at that state and from the frozen design
content at `4ddd571`.

Files reviewed in round 2:

- `AGENTS.md` §3.8–§3.13, §3.15, §7.2, §8, §13; `docs/planning/reviews/README.md`; the review template.
- `docs/planning/reviews/step-07.md` round-1 findings, evidence, verdict, and `Resolution status`.
- `docs/planning/steps/step-07-validation-and-diagnostics.md`; Steps 03–06 pages; the Build-stage owner pages
  09, 10, 11, and 12.
- `docs/planning/02-data-contracts.md` §5.5–§5.6 and §6, `03-quality-strategy.md` §9.7,
  `04-roadmap.md` §3.1, `05-open-decisions.md`, `appendix-b-parity-matrix.md`, `status.md`.
- `docs/development/architecture.md`, `docs/development/design-decisions.md`,
  `docs/development/testing-and-parity.md`, `docs/development/code-structure.md`, `zensical.toml`,
  `docs/reference/index.md`, `docs/reference/project-format.md`, `docs/troubleshooting/index.md`.
- Implemented APIs: `Mocassin.Packages/Projects/ProjectStore.cs`, `ProjectStoreOptions.cs`,
  `ProjectStoreResults.cs`, `ProjectValidationService.cs`; `Mocassin.Core/Project/ProjectHasher.cs`,
  `Mocassin.Core/Project/ProjectDiagnosticCodes.cs`, Core diagnostic-code classes;
  `Mocassin.Symmetry/Diagnostics/*`, `Mocassin.Symmetry/Structure/StructurePreview.cs`,
  `IStructurePreviewService.cs`, `Mocassin.Symmetry/Energy/EnergyCostPreview.cs`,
  `ClusterCostPreview.cs`, `IEnergyPreviewService.cs`, `Mocassin.Symmetry/Transitions/TransitionPreview.cs`,
  `TransitionPreviewService.cs`, `TransitionPathResolver.cs`, `ITransitionPreviewService.cs`,
  `Mocassin.Core/Model/Transitions/ResolvedTransitionPath.cs`; `src/dotnet/Mocassin.Cli/Program.cs`;
  `contracts/manifest.json`; `tools/contracts/generate.py`; `tools/verify.sh`.

Commands run (from `/workspace/playground/step07-design`):

```sh
git status --short
# clean

git log --oneline -5
# 3f576a1 docs: record step 07 review resolutions and status [skip ci]
# 4ddd571 docs: revise step 07 validation design for review findings [skip ci]
# e1e1475 docs: record step 07 design review changes required [skip ci]
# eadd6d9 docs: request step 07 design review [skip ci]
# dc73ee4 docs: prepare step 07 validation and diagnostics design [skip ci]

git diff --stat dc73ee4^..dc73ee4
# 9 files changed, 470 insertions(+), 166 deletions(-)

git diff --stat 4ddd571^..4ddd571
# 10 files changed, 277 insertions(+), 126 deletions(-)

git diff --stat e1e1475..3f576a1
# 12 files changed, 307 insertions(+), 134 deletions(-)

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
# 18 tests OK; SPDX headers present; no secrets; cargo-deny ok
```

Targeted read-only checks:

```sh
grep -rn "SIM042" docs src tests tools
# only this review record (round-1 and round-2 text); no live design/code reference

grep -rn "ValidateOnLoad\|ProjectLoadFailureKind" src/dotnet
# no matches: both are still design-only additions

grep -n "set(formats) == REQUIRED_FORMATS" tools/contracts/generate.py
# line 178: formats must be exactly the eight required ids

find contracts/generated -maxdepth 2 -type f | sort
# schemas are emitted under contracts/generated/schema/<id>-v<version>.schema.json

grep -rn "class .*DiagnosticCodes" src/dotnet/Mocassin.Core/Diagnostics \
  src/dotnet/Mocassin.Core/Project
# all Core code classes are public

grep -rn "InternalsVisibleTo" src/dotnet/Mocassin.Core src/dotnet/Mocassin.Symmetry
# Core.Tests and Symmetry.Tests are present

grep -n "ExpectedRuleCount\|UniquePairInteractionCount\|EstimatedMemoryBytes" \
  src/dotnet/Mocassin.Symmetry/Transitions/TransitionPreview.cs \
  src/dotnet/Mocassin.Symmetry/Energy/EnergyCostPreview.cs
# ExpectedRuleCount is int; UniquePairInteractionCount and EstimatedMemoryBytes exist

grep -n "CancellationToken" src/dotnet/Mocassin.Symmetry/*/I*PreviewService.cs
# no matches

grep -n "Convert.ToHexStringLower" src/dotnet/Mocassin.Core/Project/ProjectHasher.cs
# lowercase hexadecimal, no sha256: prefix

grep -rn "ProjectValidationPipeline\|IValidationStage" docs/planning/steps/step-0[89]*.md \
  docs/planning/steps/step-1[012]*.md
# Steps 09-12 now record the Build-stage integration point
```

Resolution verification against the design at `4ddd571`:

| Finding | Round-2 verification |
| --- | --- |
| M1 | Partially closed. Nullable identity/hashes, the single-report rule, and the load-failure kind are present, and the unreachable edit-lock claim is gone. The pipeline API still does not propagate the kind to the CLI (R2-M1). |
| M2 | Closed. `ValidateFileAsync` owns `Load`, uses `ValidateOnLoad = false`, rejects a validating store, and the in-memory entry reports `Load` as `Skipped`; stage status/count/elapsed semantics are defined (residual wording in R2-m2). |
| M3 | Closed. `ValidationContext`, `IValidationStage` restricted to `Build`, `RunBuildStage` gating, and the integration text in Steps 09–12 are all present. |
| M4 | Closed for the duplicate `KMC-no-unstable` code and cascade. `SIM038`/`SIM039` are distinguished, `SIM039` suppresses `SIM038`, and `SIM042` is gone; suppression with per-simulation `SIM003` is still worded ambiguously (R2-m4). |
| M5 | Closed. The "requested, loaded, no `Model` error, previews succeeded" rule is consistent across the step page §2/§3/§6, contract §5.6, acceptance criteria, test table, quality strategy §9.7, and `testing-and-parity.md`; feasibility/budget errors may coexist with statistics and `--stats` does not change `valid`. |
| M6 | Closed. The report is registered under a top-level `reports` object, not `formats`; generator/README/version-constant/schema/freshness tasks are recorded. The generated-schema path is inconsistent with the existing `schema/` convention (R2-m5). |
| m1 | Closed. `SymmetryDiagnosticCatalog` is new public, the internal constants are read inside their assembly, `InternalsVisibleTo` exists, Core `ProjectDiagnosticCodes` are registered, and descriptors are split by domain. |
| m2 | Closed. `EstimatedRuleCount` and `MaxEstimatedRuleCount` are `long`; the preview remains `int` and the sum is accumulated in `long`. |
| m3 | Closed. Pair interactions use `UniquePairInteractionCount`; all 17 statistics fields are enumerated in the contract and match `ModelStatistics`. |
| m4 | Closed. Cancellation is claimed only between stages/transitions/simulations and around atomic preview calls. |
| m5 | Closed. Hashes are raw lowercase hexadecimal, matching `ProjectHasher`. |
| m6 | Closed. PublicAPI/ApiCompat, SPDX, closeout/status/parity/audit, navigation/index, and descriptor file-layout tasks are present. |
| m7 | Closed. `CellCountA/B/C < 1` is a programmer error and throws `ArgumentOutOfRangeException`. |
| m8 | Closed. The hop-by-hop minimum-image rule, per-position cutoff selection, strict comparison, first hop, invalid-cutoff skip, and path-error suppression are specified. |
| m9 | Closed. Budgets live in `ValidationOptions`; Steps 09–12 are used throughout. |
| n1 | Accepted. The budgets are recorded as initial advisory guardrails tuned by Steps 08/11. |
| n2 | Accepted. `Completed`/`Failed`/`Skipped`, post-dedup counts, and the in-memory `Load` elapsed rule are defined, subject to R2-m2/R2-m3. |
| n3 | Accepted. The acceptance criterion now references the fixture's recorded warning set and a Step 07 per-code hint test; Step 03–06 tests count for severity only. |
| n4 | Accepted. A `Sizing` section records seven rows, 2–3 person-weeks, and below the split trigger. |

### Re-review round 3 — Evidence

**Review round 3** — frozen design commit `60437ca07219368dca745b4c9e9f056a543bde1a`; HEAD
`59418839e17c09adf8eb402083da44f0a58cab33` adds only the round-3 request, resolutions, and status. I am a
fresh context: I did not author or implement this work, I did not participate in rounds 1 or 2, and I used no
handoff, summary, or prior conversation. The worktree `/workspace/playground/step07-design` was clean at
`5941883`; all round-3 evidence is from repository inspection at that state and from the frozen design content
at `60437ca`.

Files reviewed in round 3:

- `AGENTS.md` §3.8–§3.13, §3.15, §7.2, §8, §13; `docs/planning/reviews/README.md`; the review template.
- `docs/planning/reviews/step-07.md` round-1 and round-2 findings, evidence, verdicts, `Resolution status`, and
  `Round 3 resolutions`.
- `docs/planning/steps/step-07-validation-and-diagnostics.md`; Steps 03–06 pages; the Build-stage owner pages
  09, 10, 11, and 12.
- `docs/planning/02-data-contracts.md` §5.5–§5.6 and §6, `03-quality-strategy.md` §9.7,
  `04-roadmap.md` §3.1, `05-open-decisions.md`, `appendix-b-parity-matrix.md`, `status.md`.
- `docs/development/architecture.md`, `docs/development/design-decisions.md`,
  `docs/development/testing-and-parity.md`, `docs/development/code-structure.md`, `zensical.toml`,
  `docs/reference/index.md`, `docs/reference/project-format.md`, `docs/troubleshooting/index.md`.
- Implemented APIs: `Mocassin.Packages/Projects/ProjectStore.cs`, `ProjectStoreOptions.cs`,
  `ProjectStoreResults.cs`, `ProjectValidationService.cs`; `Mocassin.Core/Project/ProjectHasher.cs`,
  `Mocassin.Core/Project/ProjectDiagnosticCodes.cs`, Core diagnostic-code classes;
  `Mocassin.Symmetry/Diagnostics/*`, `Mocassin.Symmetry/Structure/StructurePreview.cs`,
  `Mocassin.Symmetry/Energy/EnergyCostPreview.cs`, `Mocassin.Symmetry/Transitions/TransitionPreview.cs`,
  `TransitionPathValidator.cs`, `TransitionPathResolver.cs`; `Mocassin.Core/Diagnostics/ModelPath.cs`;
  `src/dotnet/Mocassin.Cli/Program.cs`; `contracts/manifest.json`; `tools/contracts/generate.py`;
  `contracts/generated/`; `tools/verify.sh`.

Commands run (from `/workspace/playground/step07-design`):

```sh
git status --short
# clean

git log --oneline -6
# 5941883 docs: record step 07 round 2 resolutions and status [skip ci]
# 60437ca docs: close step 07 round 2 review findings [skip ci]
# dfd7717 docs: record step 07 design re-review changes required [skip ci]
# 3f576a1 docs: record step 07 review resolutions and status [skip ci]
# 4ddd571 docs: revise step 07 validation design for review findings [skip ci]
# e1e1475 docs: record step 07 design review changes required [skip ci]

git diff --stat dc73ee4^..dc73ee4
# 9 files changed, 470 insertions(+), 166 deletions(-)

git diff --stat 4ddd571^..4ddd571
# 10 files changed, 277 insertions(+), 126 deletions(-)

git diff --stat 60437ca^..60437ca
# docs/planning/02-data-contracts.md                          | 11 ++--
# docs/planning/steps/step-07-validation-and-diagnostics.md   | 59 +++++++++++++---------
# 2 files changed, 42 insertions(+), 28 deletions(-)

git diff --stat 60437ca..HEAD
# docs/planning/reviews/step-07.md                            | 20 ++++++++++++++++++--
# docs/planning/status.md                                     |  7 ++++---
# docs/planning/steps/step-07-validation-and-diagnostics.md   |  2 +-
# (round-3 request, resolutions, and status only; no design change)

python tools/check_process_consistency.py
# Process consistency: OK

python tools/check_docs_links.py
# Documentation links OK (143 files scanned).

zensical build --strict
# Build started; No issues found; Build finished in 0.26s

python tools/contracts/generate.py --check
# Contract artifacts are current (17 files)

./tools/verify.sh contracts
# Contract artifacts current; space-group catalog current; process consistency OK;
# 18 tests OK; SPDX headers present in 285 source files; no secrets; cargo-deny ok
```

Targeted read-only checks:

```sh
grep -rn "ProjectLoadFailureKind" src/dotnet
# no matches: the load-failure kind and ValidateOnLoad are still design-only additions

grep -n "set(formats) == REQUIRED_FORMATS\|def build_artifacts\|GENERATED_DIR / \"schema\"" \
  tools/contracts/generate.py
# line 178: formats must be exactly the eight required ids
# build_artifacts emits formats under contracts/generated/schema/<id>-v<version>.schema.json

find contracts/generated/schema -maxdepth 1 -type f | sort
# events-v1, jobFolder-v1, mmcfe-v1, package-v1, project-v1, results-v1, solver-v1, state-v1

grep -n "Convert.ToHexStringLower" src/dotnet/Mocassin.Core/Project/ProjectHasher.cs
# lowercase hexadecimal, no sha256: prefix

grep -n "SIM003" docs/planning/steps/step-06-lattice-and-simulation-models.md
# line 350: Simulation has no transitions

grep -n "KMC start pool" docs/planning/steps/step-10-job-templates-and-supercells.md
# line 47: Selection optimizers remove a particle at a reference site from the KMC start pool.

grep -n "\"kineticTransition\"" src/dotnet/Mocassin.Symmetry/Transitions/TransitionPathValidator.cs
# existing path domain is kineticTransition with the transition ID

grep -rn "in-memory.*Skipped\|Load.*Skipped\|reports \`Load\` as skipped" \
  docs/planning docs/development --include=*.md
# step-07 page lines 140, 180, 213, 520; 03-quality-strategy line 305; design-decisions line 81
```

Resolution verification against the design at `60437ca`:

| Finding | Round-3 verification |
| --- | --- |
| R2-M1 | Closed. `ValidationResult` now declares `ProjectLoadFailureKind? LoadFailure`; the design states `ValidateFileAsync` copies the kind from `ProjectLoadResult`, and the CLI maps `NotFound`/`TooLarge`/`Io` to `3` and `InvalidContent` to `1`. This removes the round-2 gap where only `Stages` and the shared `PROJ002` diagnostic were visible to the CLI. The enum and `ValidateOnLoad` remain planned additions, consistent with the task list. |
| R2-m1 | Closed. The `Load` row now lists I/O diagnostics for OS sharing failures only and states that the read-only path never checks the editing lock. |
| R2-m2 | Partially closed. §2 now distinguishes no caller diagnostics (`Skipped`/0/zero elapsed) from caller diagnostics (`Failed` or `Completed`, surviving count, zero elapsed). The same rule is not propagated to the API comment, §3, the test table, or the two design summaries (R3-m1). |
| R2-m3 | Closed. Statistics-preview diagnostics are attributed to the `Feasibility` stage result, which is consistent with the opt-in budget/statistics work, the stage-count sum rule, and the rule that `Feasibility` runs whenever statistics can be computed. |
| R2-m4 | Closed. §4 now gives an explicit evaluation order, states that `SIM003` only covers its own simulation, and records the mixed empty/transition-bearing case where `SIM039` still fires. The `SIM038`/`SIM039` scopes and suppression remain non-overlapping. |
| R2-m5 | Closed. The schema path is now `contracts/generated/schema/validation-v1.schema.json`, matching the existing `<id>-v<version>.schema.json` convention; the `reports.validation` descriptor shape and `ValidationReportVersion` home are stated. The generator task covers validating `reports`, emitting the schema, and updating the README/constant. |
| R2-m6 | Closed. `ENM018` now specifies `Path.Domain = "kineticTransition"`, `Path.ObjectId` = transition ID, and `Path.Property = "referencePath[i]"`, matching `TransitionPathValidator`'s existing paths. |
| R2-m7 | Closed. `SIM038`/`SIM039` are KMC-only; the design states MMC transitions have no start pool, and the test row excludes MMC. The KMC start pool is confirmed in Step 10. |

Round-1 Major findings M1–M6 remain closed: M2 is supported by the file/in-memory entry split and
`ValidateOnLoad`; M3 by `ValidationContext`, the `Build`-only stage restriction, `RunBuildStage`, and the
Steps 09–12 integration text; M4 by the single `SIM038`–`SIM041` set; M5 by the statistics rule repeated across
the step page, contract, acceptance criteria, tests, quality strategy, and `testing-and-parity.md`; M6 by the
`reports` section and generator tasks. Round-2 Minor findings m1–m9 and Notes n1–n4 remain closed or accepted as
recorded in round 2.

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

### Re-review round 2 — Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **R2-M1 — The structured load-failure kind is not propagated to the CLI, so the M1 exit-code mapping still cannot be implemented through the prescribed entry point.** The design adds `ProjectLoadFailureKind` to `ProjectLoadResult` and says the CLI maps `NotFound`/`TooLarge`/`Io` to exit `3` and `InvalidContent` to exit `1`. But the CLI is specified to call `ProjectValidationPipeline.ValidateFileAsync`, whose declared return type is `ValidationResult`; `ValidationResult` has `ProjectId?`, hashes, `IsValid`, `Summary`, `Diagnostics`, `Statistics`, and `Stages`, but no failure kind and no `exitCode`. `Stages` only distinguishes `Failed` from `Skipped`, and both parse and I/O failures emit the same `PROJ002` diagnostic, so the CLI cannot tell `1` from `3` after the pipeline returns. Propagating the kind (on `ValidationResult` or a dedicated load-failure outcome) or changing the file-entry signature must be part of the design. | Step page §2 (`ValidationResult`), §3 (`Load` stage), §7 (CLI calls `ValidateFileAsync`; kind mapping); `ProjectStoreResults.cs` lines 12–38; `ProjectStore.cs` lines 340–373 and 395–463; `ProjectDiagnosticCodes.InvalidDocument` (`PROJ002`) covers missing file, BOM, UTF-8, JSON, schema, and migration failures. |
| **Minor** | **R2-m1 — The `Load` stage responsibility row still mentions lock diagnostics although the read-only path never checks the editing lock.** The CLI section correctly says the path never acquires the lock and an OS sharing denial surfaces as `Io`, but the stage table still lists "lock/I/O diagnostics" as a `Load` responsibility. The design should either drop the lock wording or state that it means an OS-level sharing failure only. | Step page §3 `Load` row; §7 read-only lock paragraph; `ProjectStore.LoadAsync` has no lock-file check. |
| **Minor** | **R2-m2 — The in-memory entry's `Load` status is `Skipped` even when the caller supplies load diagnostics, which is ambiguous for error diagnostics and for the stage-count sum rule.** `Validate(project, loadDiagnostics, ...)` is the in-memory entry; §2 says `Load` is reported as `Skipped` with `TimeSpan.Zero`, while §3 says load errors stop the pipeline and stage counts sum to `Diagnostics.Length`. The design does not say whether caller-supplied load errors make the skipped stage carry a non-zero `DiagnosticCount` or whether they are attributed to `Model`. | Step page §2 (`Validate` in-memory entry and `Stages` bullet), §3 (load errors stop the pipeline; counts sum to `Diagnostics.Length`). |
| **Minor** | **R2-m3 — Statistics-preview failure diagnostics have no owning stage, which conflicts with the "stage counts sum to `Diagnostics.Length`" rule.** §6 says a failed statistics preview appends its diagnostics to the result, but statistics is not one of the five `ValidationStage` values and the result has no statistics stage result. Unless those diagnostics are only duplicates of an earlier preview failure (and are deduplicated), the stage counts cannot sum to the final diagnostics length. | Step page §2 (`Stages` bullet), §6 (failed preview diagnostics appended; statistics are not a stage), `ValidationStage` enum. |
| **Minor** | **R2-m4 — `SIM039`'s suppression relation to per-simulation `SIM003` is ambiguous for mixed models.** The design says "Neither is emitted when `SIM003` already reports the simulation's empty transition set". `SIM003` is per simulation, while `SIM039` is model-wide. If one simulation has no transitions and another has transitions but no mobile start particle, it is unclear whether `SIM003` suppresses the model-wide `SIM039`, leaving the second simulation without the buildability warning. State the exact evaluation order and the mixed-model case. | Step page §4 `SIM038`/`SIM039` rows and suppression bullets; Step 06 `SIM003` definition ("Simulation has no transitions"). |
| **Minor** | **R2-m5 — The generated validation schema path/naming does not follow the existing generator convention.** The design and task say `contracts/generated/validation-v1.schema.json`, while every existing generated schema is `contracts/generated/schema/<id>-v<version>.schema.json`. The freshness check works either way, but the path should be consistent or the exception should be stated; the `reports` descriptor shape and its `schemaVersion`/version-constant home are also not specified at file level. | Step page task list; `02-data-contracts.md` §5.6; `tools/contracts/generate.py` `build_artifacts`; `contracts/generated/schema/` listing. |
| **Minor** | **R2-m6 — `ENM018` specifies the property path but not the diagnostic domain/object identity.** The rule says to emit at the first offending hop with `Path.Property = "referencePath[i]"`, but it does not say whether `Path.Domain` is `kineticTransition` and `Path.ObjectId` is the transition ID. The per-code emission test requires an expected path. | Step page §4 `ENM018` boundary; test table `Cross-domain`; existing `TransitionPathValidator` paths use `kineticTransition`/transition ID. |
| **Minor** | **R2-m7 — `SIM038`/`SIM039` use "mobile start particle" without saying whether MMC simulations are included.** The term comes from the KMC start pool, but the rule is written for "a simulation" and the model can author KMC and MMC simulations. State the simulation-kind scope; otherwise the feasibility test boundary is ambiguous. | Step page §4 `SIM038`/`SIM039`; Step 10 "KMC start pool"; Step 06 simulation kinds. |

### Re-review round 3 — Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **R3-m1 — The R2-m2 fix is applied only to the §2 stage bullet; five other statements still say the in-memory `Load` stage is always `Skipped`.** §2 now says a caller-supplied in-memory entry reports `Load` as `Failed` (if any caller diagnostic is an `Error`) or `Completed`, with the surviving count and zero elapsed. The `Validate` code comment still says `Load` "is reported as Skipped"; §3 still says the in-memory `Load` "is reported as `Skipped`"; the `Pipeline order` test row still expects `in-memory Load is Skipped`; and `03-quality-strategy.md` and `design-decisions.md` still state the in-memory entry reports `Load` as skipped. A test written from the test table would contradict the §2 API rule, and the two summaries would misdescribe the stage status for callers that pass load diagnostics. | Step page §2 `Validate` comment and `Stages` bullet (lines 140, 180–182), §3 consumers bullet (line 213), Tests `Pipeline order` row (line 520); `docs/planning/03-quality-strategy.md` line 305; `docs/development/design-decisions.md` line 81. |

## Record state

**State:** Ready with notes

> Round-1 state was `Re-review required`; round 2 re-review returns `Changes required` (see the round-2
> verdict below).
>
> Round 3 re-review returns `Ready with notes` (see the round-3 verdict below). One Minor residual finding
> remains; no Blocker or Major remains, and the human owner must explicitly accept the Minor.

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

### Re-review round 2 — Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

**Round 2 verdict: Changes required.** One Major finding remains: the structured
`ProjectLoadFailureKind` is added to `ProjectLoadResult`, but the CLI consumes `ValidateFileAsync`, which
returns `ValidationResult` without the kind, so the promised parse-vs-I/O exit-code mapping (`1` vs `3`) is
still not implementable through the designed entry point (R2-M1). No Blocker was found.

The round-1 Major findings M2–M6 are closed against the design text and the implemented Steps 03–06 APIs.
M1 is materially improved (nullable identity/hashes, one report for every invocation, `ProjectLoadFailureKind`,
and removal of the unreachable edit-lock claim) but is not fully closed because of R2-M1. Minor findings
m1–m9 and Notes n1–n4 are closed or accepted as recorded. Seven new Minor findings remain (R2-m1–R2-m7);
they are correctness/clarity gaps, not start blockers beyond R2-M1. The repository checks and the strict
documentation build pass.

Because a Major remains, the design is not `Ready for implementation` or `Ready with notes`. The human owner
approves the final verdict and the start gate; this record does not authorize starting Step 07.

Follow-up owner: Step agent / planner
Follow-up step: Step 07 design revision for R2-M1 (and optionally R2-m1–R2-m7), then a fresh re-review (round 3)

### Re-review round 3 — Verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Waived

**Round 3 verdict: Ready with notes.** No Blocker or Major finding remains. R2-M1 is closed: the structured
load-failure kind is now carried on `ValidationResult.LoadFailure`, `ValidateFileAsync` copies it from
`ProjectLoadResult`, and the CLI can map parse failures (`1`) versus I/O failures (`3`) without inspecting
`PROJ002`. R2-m1 and R2-m3–R2-m7 are closed against the design text and the implemented Steps 03–06 APIs. The
earlier Major findings M1–M6 remain closed.

One Minor finding remains: R3-m1, the residual R2-m2 wording in five places that still calls the in-memory
`Load` stage unconditionally `Skipped` while §2 now defines `Failed`/`Completed` when caller diagnostics are
supplied. It is a documentation/test-expectation inconsistency, not an architectural gap; the §2 API rule is
implementable and unambiguous. The repository checks and the strict documentation build pass. Because a Minor
remains, this is not `Ready for implementation`; the human owner must explicitly accept the Minor under
`Ready with notes` before the start gate. This record does not itself authorize starting Step 07.

Follow-up owner: Step agent / planner
Follow-up step: Correct the five stale in-memory `Load` statements (R3-m1) when convenient; no re-review is
required if the human owner accepts `Ready with notes`.

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
| M1 — CLI/report failure paths | Resolved: one `--json` report for every invocation; nullable identity/hashes; `ProjectLoadFailureKind` maps parse (`1`) vs I/O (`3`); edit-lock claim removed | Step agent / planner | `4ddd571` |
| M2 — Load ownership/`ValidateOnLoad` | Resolved: `ValidateFileAsync` owns `Load`, sets `ValidateOnLoad = false`, rejects double-validation stores; in-memory `Load` is `Skipped`; stage status/count/elapsed defined | Step agent / planner | `4ddd571` |
| M3 — Build extension point | Resolved: `ValidationContext` defined; `IValidationStage` restricted to `Build`; `RunBuildStage` gates it; Steps 09–12 record the integration point | Step agent / planner | `4ddd571` |
| M4 — `SIM038`–`SIM040` overlap | Resolved: duplicate KMC-no-unstable code removed; `SIM038` per simulation, `SIM039` model-wide with suppression, no emission when `SIM003`/transition errors fire; codes renumbered `SIM038`–`SIM041` | Step agent / planner | `4ddd571` |
| M5 — statistics nullability | Resolved: one rule (requested, loaded, no `Model` error, previews succeeded); independent of policy and feasibility/budget errors; `--stats` never changes `valid`; contract, tests, and quality docs aligned | Step agent / planner | `4ddd571` |
| M6 — manifest/schema tooling | Resolved: `reports.validation` in the manifest (not `formats`), generator/README/version-constant tasks, generated `validation-v1.schema.json`, freshness check | Step agent / planner | `4ddd571` |
| m1 — catalog completeness types | Resolved: `SymmetryDiagnosticCatalog` is new public; internal constants read inside their assembly; assembly-local tests; Core project codes registered; descriptor file split | Step agent / planner | `4ddd571` |
| m2 — rule-count overflow | Resolved: `long` statistic and budget, `long` accumulation | Step agent / planner | `4ddd571` |
| m3 — pair source/field list | Resolved: `EstimatedPairInteractionCount = UniquePairInteractionCount`; all report fields enumerated | Step agent / planner | `4ddd571` |
| m4 — cancellation granularity | Resolved: between stages/transitions/simulations and around preview calls; atomic preview calls documented | Step agent / planner | `4ddd571` |
| m5 — hash format | Resolved: raw lowercase hex without a `sha256:` prefix, matching `ProjectHasher` | Step agent / planner | `4ddd571` |
| m6 — gates/integration tasks | Resolved: PublicAPI/ApiCompat, SPDX, closeout/status/parity/audit, navigation/index, and catalog file-layout tasks added | Step agent / planner | `4ddd571` |
| m7 — cell-count validation | Resolved: `< 1` is a programmer error (`ArgumentOutOfRangeException`) | Step agent / planner | `4ddd571` |
| m8 — `ENM018` boundary | Resolved: hop-by-hop minimum-image distance, per-position cutoff selection, strict comparison, first hop only, invalid-cutoff and path-error suppression | Step agent / planner | `4ddd571` |
| m9 — internal inconsistencies | Resolved: budget lives in `ValidationOptions`; build owners are Steps 09–12 throughout | Step agent / planner | `4ddd571` |
| n1 — budget rationale | Recorded as initial guardrails tuned by Steps 08/11 | Step agent / planner | `4ddd571` |
| n2 — stage-status semantics | Defined `Completed`/`Failed`/`Skipped`, post-dedup counts, and `Load` elapsed behavior | Step agent / planner | `4ddd571` |
| n3 — ceria/hint coverage | Acceptance now references the fixture's recorded warning set and a Step 07 per-code hint test; Step 03–06 tests counted for severity only | Step agent / planner | `4ddd571` |
| n4 — sizing record | Added a `Sizing` section: seven rows, 2–3 person-weeks, below the split trigger | Step agent / planner | `4ddd571` |

Round 2 request: the design fixes above are at `4ddd571`. A fresh re-review should verify M1–M6 and the minor/note
resolutions against that commit; reviewer-authored round 1 text is unchanged.

### Round 3 resolutions

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| R2-M1 — load-failure kind not propagated | Resolved: `ValidationResult.LoadFailure` carries `ProjectLoadFailureKind?`; `ValidateFileAsync` copies it from `ProjectLoadResult`; the CLI maps it to `1`/`3` | Step agent / planner | `60437ca` |
| R2-m1 — `Load` row lock wording | Resolved: row now says OS sharing/I/O only and that the read-only path never checks the edit lock | Step agent / planner | `60437ca` |
| R2-m2 — in-memory `Load` status/count | Resolved: no caller diagnostics → `Skipped`/0; caller diagnostics → `Failed` or `Completed` with surviving count and zero elapsed | Step agent / planner | `60437ca` |
| R2-m3 — statistics-preview diagnostics | Resolved: attributed to the `Feasibility` stage result, so counts sum to `Diagnostics.Length` | Step agent / planner | `60437ca` |
| R2-m4 — mixed `SIM003`/`SIM039` | Resolved: explicit evaluation order; `SIM003` suppresses only its own simulation, never the model-wide `SIM039` | Step agent / planner | `60437ca` |
| R2-m5 — generated schema path/shape | Resolved: `contracts/generated/schema/validation-v1.schema.json`, descriptor shape and `ValidationReportVersion` home specified | Step agent / planner | `60437ca` |
| R2-m6 — `ENM018` path identity | Resolved: domain `kineticTransition`, transition ID as object, `referencePath[i]` property | Step agent / planner | `60437ca` |
| R2-m7 — `SIM038`/`SIM039` kind scope | Resolved: both are KMC-only; MMC has no start pool | Step agent / planner | `60437ca` |

Round 3 request: the fixes are at `60437ca`. A fresh re-review should verify R2-M1 and R2-m1–R2-m7 against that
commit; all reviewer-authored round 1 and round 2 text is unchanged.

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.
