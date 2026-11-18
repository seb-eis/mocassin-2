# Cross-Cutting Review — Phase C (XC-04)

> Reviewer: write evidence, findings, and verdict directly into this record. You may edit only your own
> metadata and record state, checklist completion marks and inline verification notes (not item text),
> `Evidence`, `Findings`, and `Verdict`, and commit only this file with a documentation-only message and
> `[skip ci]`. Do not edit the scope, checklist item text, resolutions, code, tests, configuration, step pages,
> status dashboard, or any other file.

## Request

| Field | Value |
| --- | --- |
| Review ID | XC-04 |
| Checkpoint | Phase C — Compilation and build (Steps 08–12) |
| Type | Repository-wide combined review: policies and planning, architecture and design status (including original-design fidelity), and documentation status |
| Target commit | `2127f0f88bcc2f43e3cf5fe7f7d321e28e0069e1` (`main`; Phase C checkpoint hygiene: status correction, Phase C drift check, `milestone-m4`/`milestone-m5` backfill, and self-building docs gate) |
| Branch | `docs/xc-04-phase-c` (docs-only; committed with `[skip ci]`; not pushed, merged, or tagged by the planner) |
| Request commit | The docs-only commits on `docs/xc-04-phase-c` (the initial request, the merge of `main`, and the re-freeze edit); `git diff --stat 2127f0f..HEAD` shows only this record plus the two status restatements in the review registry and dashboard |
| Request owner | Planner (request, scope, and checklist); the Phase C step agent owns remediation and resolutions |
| Orchestrator (implementor) | OpenCode Phase C orchestrating agent |
| Review initiated by | Human owner |
| Reviewer | Fresh independent reviewer (DeepSeek V4.1 Flash via `opencode-go`); no Phase C implementation, design, audit, or review context and no shared conversation with the planner, implementors, reviewers, auditors, or orchestrator |
| Review date | 2026-11-18 |

> Orchestration: the orchestrator freezes nothing itself; it spawns the reviewer, relays findings and decisions,
> and does not fix findings or edit reviewer text. The request owner fixes findings on a maintenance branch and
> adds resolutions without altering reviewer text. The human owner approves the final verdict and the phase gate.
> A record commit that touches any other file is invalid (see `AGENTS.md` §3.14 and this directory's README §7).

## Purpose

Provide the single combined cross-cutting quality gate for Phase C, which covers the compilation and build
chain delivered by Steps 08–12. The review must establish, with concrete evidence, that:

1. the repository's agent instructions, write scopes, orchestration, CI policy, code-structure enforcement, and
   process records are internally consistent and accurate for the completed phase;
2. the architecture, contracts, and cross-step interfaces 08 → 09 → 10 → 11 → 12 are coherent, and the
   phase's design status and open decisions are current; and
3. the published and developer documentation covers the implemented Phase C surface, is accurate and
   navigable, keeps the `docs/planning/` publication boundary, and matches implemented behaviour.

The architecture and design section is also the Phase C **original-design fidelity gate**: it must explicitly
answer the intended-corrections, ported-strengths, and regressions questions with code, test, benchmark, or
side-by-side design evidence, not prose alone.

This review is the gate that must be `Passed` (or accepted as `Passed with notes`, or explicitly waived by the
human owner with a recorded rationale) before the Step 13 design review may start.

## Non-goals

- Re-running the Step 08–12 implementation audits line by line, or re-opening their accepted notes.
- Statistical parity validation of the solver (Step 29), or any Phase D/E/F/G work.
- Reviewing Phase A/B records for correctness beyond what Phase C depends on and what the Phase B checkpoint
  already accepted.
- Reviewing or approving Step 13 design or implementation.
- Writing, fixing, or remediating anything: the reviewer reports only, and remediation happens on a later
  maintenance branch through a separate commit and, if needed, a re-review round.
- Replacing step design reviews, step audits, or Step 29 parity validation.

## Scope

In scope, at the frozen commit:

- **Policies and planning:** `AGENTS.md` (especially §§1, 3.1–3.15, 5, 6, 8, 9, 13, 14, 16), `.opencode/agents/`,
  `opencode.json`, `.opencode/commands/`, `docs/development/orchestration.md`,
  `docs/development/agent-workflow.md`, `docs/development/contributing.md`, `CONTRIBUTING.md`,
  `docs/development/code-structure.md`, `tools/structure-policy.json`,
  `src/dotnet/Mocassin.Architecture.Tests/`, `docs/planning/ci-policy.md`, `.github/workflows/*.yml`,
  `tools/verify.sh`, `docs/planning/status.md`, `docs/planning/README.md`, `docs/planning/04-roadmap.md`,
  `docs/planning/05-open-decisions.md`, `docs/planning/06-toolchain-requirements.md`, ADRs 0009–0015,
  `docs/planning/appendix-b-parity-matrix.md`, and the Phase C step pages, design reviews, audits, amendments,
  closeout evidence, and the cross-cutting registry/template.
- **Architecture and design:** `docs/planning/00-scope-and-principles.md`, `01-target-architecture.md`,
  `02-data-contracts.md`, `03-quality-strategy.md`, `04-roadmap.md`, `05-open-decisions.md`, ADRs 0009–0015,
  `contracts/manifest.json` and every generated artifact under `contracts/generated/`,
  `tests/contracts/`, the project graph in `Mocassin.slnx` and every `*.csproj`, all Phase C production code
  under `src/dotnet/Mocassin.Compilation`, `Mocassin.Contracts`, `Mocassin.Packages`, `Mocassin.Results`,
  `Mocassin.Cli`, and the Phase C test suites and performance evidence.
- **Documentation:** the whole published tree `docs/` excluding `docs/planning/` — in particular
  `docs/reference/cli.md`, `msl-format.md`, `project-format.md`, `diagnostics.md`, `file-layout.md`,
  `docs/user-guide/building.md`, `parameterization.md`, `job-templates.md`,
  `docs/development/compilation-pipeline.md`, `architecture.md`, `design-decisions.md`,
  `testing-and-parity.md`, `docs/performance/index.md`, `docs/troubleshooting/errors.md`,
  `docs/getting-started/`, `docs/index.md`, and `docs/development/index.md` — plus `zensical.toml`,
  `tools/docs/`, `tools/check_docs_links.py`, `tools/check_doc_math.py`, `tools/docfx/docfx.json`, and the
  generated DocFX/rustdoc/mkdocstrings output.

Boundaries:

- **Phase C only (Steps 08–12), not Step 12 alone.** A review that only inspects the CLI and the final step
  does not satisfy this request. Steps 08, 09, 10, and 11 are equally in scope, including their cross-step
  hand-offs.
- **Not Phase B and not Phase D.** Phase B findings are historical and were accepted at `84781f8`/`e48d9ca`;
  do not re-litigate them, but do verify that Phase C did not contradict, regress, or silently depend on an
  unresolved Phase B item. Phase D assets (solver, `mocassin-sim`, `.mcs` writer, HPC) are out of scope; where
  Phase C only designs a later-phase behaviour, verify the design and ownership, not the later implementation.
- **Not a step re-audit.** Use the audit records as evidence and verify their claims, but do not duplicate
  their line-by-line scope.
- **Documentation, process, and design status only.** No code changes, no findings remediation, no verdict
  drafting by the request owner.

## Required evidence sources and commands

The reviewer must inspect the repository at `2127f0f88bcc2f43e3cf5fe7f7d321e28e0069e1` directly. A summary
from the orchestrator, the request owner, or this page is not evidence. The request commits add only this
record and the two status restatements; `git diff --stat 2127f0f..HEAD` must show no Phase C code, test,
contract, or configuration change. If it does, stop and report it.

### Sources the reviewer must inspect

| Area | Sources |
| --- | --- |
| Phase C status and records | `docs/planning/status.md`; step pages `step-08`–`step-12`; `docs/planning/reviews/step-08.md`–`step-12.md`; `docs/planning/reviews/step-11-amend-1.md`; `docs/planning/audits/step-08.md`–`step-12.md`; `docs/planning/closeout/step-08.json`–`step-12.json`; `docs/planning/amendments/step-11-amend-1-dotnet-11-native-zstd.md`; `docs/planning/drift-checks/2026-11-18-phase-c.md` |
| Decisions and contracts | ADRs `0009`–`0015`; `docs/planning/02-data-contracts.md` §§1–3.5, §5.1, §5.6–§5.7, §6; `docs/planning/05-open-decisions.md`; `contracts/manifest.json`; `contracts/generated/**`; `tests/contracts/**` |
| Architecture | `docs/planning/01-target-architecture.md`; `docs/development/architecture.md`; `docs/development/compilation-pipeline.md`; `docs/development/design-decisions.md`; `docs/development/code-structure.md`; every `*.csproj` |
| Process and CI | `AGENTS.md` §3; `docs/development/orchestration.md` (especially §§6–9 and the OpenCode cold-start note); `docs/development/agent-workflow.md`; `docs/planning/ci-policy.md`; `.github/workflows/ci.yml`, `docs.yml`, `benchmark.yml`, `nightly.yml`; `.opencode/agents/`; `opencode.json`; `tools/verify.sh`; `tools/structure-policy.json`; `src/dotnet/Mocassin.Architecture.Tests/` |
| Documentation | All published pages under `docs/` except `docs/planning/`; `zensical.toml`; `tools/docs/`; `tools/check_docs_links.py`; `tools/check_doc_math.py`; generated `site/`, `artifacts/docfx/`, `target/doc/` |
| Parity and original design | `docs/planning/appendix-a-original-inventory.md`; `docs/planning/appendix-b-parity-matrix.md` §§5–6, 10–11; the read-only original clone at `/workspace/mocassin-original` (`src/ModelBuilder/ICon.Model/`, `src/ModelBuilder/ICon.Model.Translator/`, `src/ModelBuilder/ICon.Framework.Symmetry/`, the SQLite/package-writing code, and the model-builder workflow). Read, never copy. |

### Commands the reviewer must run and record

Record the exact command, exit code, and the relevant output line(s) in `Evidence`. Do not replace a failed
command with a narrative.

| # | Command | What it establishes |
| --- | --- | --- |
| 1 | `git status --short`; `git log --oneline --decorate -30`; `git tag --list`; `git for-each-ref refs/tags --format='%(refname:short) %(objecttype)'` | Frozen state, clean tree, annotated Phase C tags |
| 2 | `git rev-list --parents -n1 step-08^{}` … `step-12^{}`; `git merge-base --is-ancestor <tag>^{} main` | Every Phase C tag is an annotated two-parent `--no-ff` merge on `main` |
| 3 | `git branch -a`; `git ls-remote --heads origin` | No stale Phase C step/feature branches locally or remotely |
| 4 | `git diff --stat 2127f0f..HEAD` | The request commits are docs-only and scoped to this checkpoint |
| 5 | `./tools/verify.sh all --require` | Full Phase C gate: .NET build/test/format, API/ApiCompat, Rust, Python, docs, contracts/policy, and the required AOT gate |
| 6 | `./tools/verify.sh docs --require` | `check_doc_math.py`, `check_docs_links.py`, strict Zensical, DocFX, and rustdoc; the stage builds the solution in Release when the DocFX inputs are absent, so no manual pre-build is required |
| 7 | `./tools/verify.sh policy --require` | NuGet licence/vulnerability policy, ApiCompat, manifest/symmetry/projection drift, process consistency, tool tests, SPDX, secrets, `cargo-deny` |
| 8 | `python tools/check_docs_links.py`; `python tools/check_doc_math.py` | Link and math-delimiter checks with file counts |
| 9 | `./tools/docs/build-docs.sh` | Full documentation build; then confirm `site/planning` is absent and generated API pages exist |
| 10 | `python tools/check_closeout_evidence.py docs/planning/closeout/step-08.json` … `step-12.json` | Every Phase C closeout file binds audit commit, closeout CI, `main` SHA at freeze, and merge candidate |
| 11 | `python tools/check_process_consistency.py`; `python tools/contracts/generate.py --check`; `python -m unittest discover -s tools/tests -t . -q` | Process/status vocabulary, generated contract artifacts, and tool self-tests |
| 12 | `dotnet run --project src/dotnet/Mocassin.Cli -c Release -- validate samples/ceria/ceria.mocprj` | Phase C end-to-end validation path |
| 13 | `dotnet run … -- compile samples/ceria/ceria.mocprj -o /tmp/xc04/compiled.json` (and `--json`) | Compilation report and `compiled-inspection-v1` |
| 14 | `dotnet run … -- build samples/jobs/ceria-temperature-sweep.mocprj -o /tmp/xc04/ceria.msl --json`, then `verify`, `inspect`, and `build --preview` | Package write, post-publication verification, and inspection; preview writes nothing |
| 15 | `dotnet run … -- build … --jobs 1-3 --json`; a `@file` response-file case; `doctor`; `clean --dry-run`; `new` into a temp directory | Selection normal form, response-file contract, doctor, non-destructive clean, project creation |
| 16 | 10⁴-job workload from Step 12 §22 (synthetic copy of `samples/jobs/ceria-temperature-sweep.mocprj`, 20 configurations × 500 jobs) if the machine allows | Reproduces the recorded 3.76 s / 151,820 KB / 9,625,600-byte package evidence; record any deviation |
| 17 | `git diff --name-status 07851d8..HEAD` for Step 12 and the equivalent docs-only checks for Steps 08–11 | Post-audit deltas are documentation-only, matching each closeout note |

Performance and AOT notes:

- The 10⁴-job run is nightly/release tier in the step page. Reproduce it if feasible; if not, record that the
  recorded numbers were checked against the step page, closeout evidence, and `docs/performance/index.md`, and
  state that the run was not repeated.
- `verify.sh all` includes `aot`, which requires clang/lld/ILCompiler. It is a required local gate from Step 12
  onward and is fail-not-skip. Record the exact result, including the known real-CLI publish diagnostics
  (62 first-party trim warnings) that are recorded as evidence, not as a gate.
- Windows/macOS are manual gates. The reviewer cannot reproduce the Windows platform run locally; verify the
  recorded run IDs and `head_sha` against the closeout evidence and step pages, and record the macOS absence as
  the accepted risk it is, not as a passing result.

## Evidence

### Frozen state and worktree

- Reviewed code at `2127f0f88bcc2f43e3cf5fe7f7d321e28e0069e1` (`main`). The record branch `docs/xc-04-phase-c`
  is `a4988a224b8d56f6d7287822994c5428976d74de`; `git diff --stat 2127f0f..HEAD` shows only
  `xc-04-phase-c.md` (new), `reviews/cross-cutting/README.md` (status cell plus the `XC-04` row), and
  `status.md` (three status cells). No code, test, contract, or configuration file differs from the frozen
  commit.
- Working trees used: `/tmp/opencode/xc-04-review` (branch tip) for all code and gate runs, and
  `/tmp/opencode/xc04-docs` (detached at `2127f0f`, never built) solely to prove the docs gate self-builds.
- Reviewed tree clean (`git status --short` empty) before and after the runs; my only edit is this record.

### Commands run and results

| # | Command | Result |
| --- | --- | --- |
| 1 | `git log --oneline --decorate -15`; `git tag --list`; `git for-each-ref refs/tags --format='%(refname:short) %(objecttype)'` | `main` is `2127f0f`. `step-00`–`step-12`, `milestone-m0`–`milestone-m5`, `planning-complete`, `development-ready` are all `tag` objects (annotated), none lightweight. |
| 2 | `git rev-list --parents -n1 step-08^{}` … `step-12^{}`; `git merge-base --is-ancestor <tag>^{} main` | `step-08` `f91fc63` parents `884ffe7`/`4834cbb`; `step-09` `ef2f335` parents `b81355e`/`8335c70`; `step-10` `945dddf` parents `ca9a98f`/`88a44fe`; `step-11` `84ceb76` parents `bca6637`/`fac659c`; `step-12` `ec2e04a` parents `9d4d2ef`/`650e299`. Every one is a two-parent `--no-ff` merge and an ancestor of `main`. |
| 3 | `git cat-file -p refs/tags/step-08` … `milestone-m5` | Step tags carry `merge`/`date`/`ci`/`features`/`status`; `milestone-m4` → `f91fc63` and `milestone-m5` → `ec2e04a` carry `merge`/`date`/`created`/`ci`/`features`/`status`, match roadmap M4/M5, and were added without moving `step-08`/`step-12`. |
| 4 | `git branch -a`; `git ls-remote --heads origin` | Remote heads are only `main` and `docs/xc-04-phase-c`. No `step/*` or `feature/*` branch survives, local or remote. Local residue: `audit/step-12-round-1`, `review/step-12-design-round-1/2/5`, `docs/step-11-design` (review/design record branches, not step or feature branches). |
| 5 | `./tools/verify.sh all --require` | Exit 0. .NET: 1,785 tests passed, 0 failed (App 1, Contracts 37, Symmetry 223, Results 42, Core 685, Cli 200, Packages 500, Compilation 96, Architecture 1); `dotnet format` clean; NuGet licence/vulnerability and ApiCompat pass. Rust fmt/clippy/tests and Python (3 passed) pass. Docs: math 169 files, links 172 files, Zensical strict “No issues found”, DocFX with `--warningsAsErrors`, rustdoc. Contracts: 26 generated artifacts current, 5/4/2 projection fixtures OK, space-group catalog current (274 settings), process consistency OK, 58 tool tests, SPDX 706 files, secret scan 1,108 files, `cargo-deny` OK. AOT: gate passed, real CLI publish recorded 62 IL2xxx/IL3xxx diagnostics as evidence. |
| 6 | `./tools/verify.sh docs --require` from a clean detached worktree with no Release output | Exit 0. Ran `== .NET Release build (DocFX input) ==`, so the gate self-builds (drift PC-07 / `XC-03` DOC-08 fixed). Math 168 files, links 171 files, Zensical strict clean, DocFX clean, rustdoc clean. |
| 7 | `./tools/verify.sh policy --require` | Exit 0. Licence, vulnerability, ApiCompat, manifest/projection/symmetry drift, process consistency, 58 tool tests, SPDX, secrets, `cargo-deny` all pass. |
| 8 | `python tools/check_docs_links.py`; `python tools/check_doc_math.py`; `python tools/check_process_consistency.py` | All exit 0; links 172 files, math 169 files, `Process consistency: OK`. |
| 9 | `./tools/docs/build-docs.sh` (inside the docs gate) | Zensical strict, DocFX, rustdoc complete; `site/planning` absent; `site/api/dotnet/api/Mocassin.Core.CoreModule.html` and `site/api/rust/mocassin_types/index.html` exist; `site/sitemap.xml` has no planning entry. |
| 10 | `python tools/check_closeout_evidence.py docs/planning/closeout/step-{08..12}.json` | `Closeout evidence: OK`, exit 0 for all five. |
| 11 | `python tools/contracts/generate.py --check`; `python -m unittest discover -s tools/tests -t . -q` | Artifacts current (26 files); 58 tests OK. |
| 12 | `dotnet run --project src/dotnet/Mocassin.Cli -c Release --no-build -- validate samples/ceria/ceria.mocprj` | Exit 0; 0 errors / 0 warnings, `Result: valid`. |
| 13 | `compile samples/ceria/ceria.mocprj -o <tmp>/compiled.json --json` | Exit 0; `compile-v1` with ten stages `resolve, structure, environments, pairs, clusters, rules, encoding, indexing, signing, report`; `compiled-inspection-v1` document written with `modelSignature`, catalog versions. |
| 14 | `build samples/jobs/ceria-temperature-sweep.mocprj -o <tmp>/ceria.msl --json`, then `verify`, `inspect`, and `build --preview` | Build exit 0, 6 jobs, 7 warnings, package 98,304 bytes; `verify` valid with 10 checks; `inspect` reports `jobSelection = all`, lattice reuse 1; preview exit 0 with `package = null` and a populated preview, and `git status --short` stayed clean (nothing written). |
| 15 | `build … --jobs 1-3 --json`; `@<file>` response file; `doctor --json`; `clean … --dry-run --json`; `new scratchproj -o <tmp> --json` | Selection normalizes to `1-3`, expanded 6 / selected 3; response file expands `validate … --json` and returns a report; `doctor` healthy with `solver:warn` in the fixed seven-check order; `clean` dry run lists 0 folders and 0 removals; `new` creates `scratchproj.mocprj`. |
| 16 | 10⁴-job reproduction (20 configurations × 500 jobs, mixed 6³/8³ cells) | Exit 0; wall 3.56 s, child peak RSS 147,908 KB, `jobSelection` all (10,000/10,000), 9,998 lattices, reuse 1.0002, package 9,609,216 bytes, verification passed. The recorded evidence is 3.76 s / 151,820 KB / 9,625,600 bytes; my synthetic project is not byte-identical (different config IDs/names), so time and memory agree and package size differs by 0.17 %. The budget (< 10 min, < 2 GB) is met with wide margin. |
| 17 | `git diff --name-status <audit>..<merge>` for 08–12 | Every post-audit delta is documentation-only. Step 08 `28e741f..f91fc63`: amendments/audit/closeout/status/step page. Step 09 `916bae1..ef2f335`: audit/closeout/status/step page. Step 10 `c10e6d7..945dddf`: `design-decisions.md`, `02-data-contracts.md`, audit/closeout/status/step page, `reference/project-format.md`. Step 11 `4197166..84ceb76`: audit/closeout/status/step page. Step 12 `07851d8..ec2e04a`: `02-data-contracts.md`, `03-quality-strategy.md`, audit/closeout/status/step page, `docs/troubleshooting/errors.md` (the F‑9/N‑4 corrections recorded in audit round 2). |
| 18 | `git diff --stat 2127f0f..HEAD` | 3 files, +489/−5: this record, the cross-cutting registry status, and two dashboard status cells. No Phase C code/test/contract/configuration change. |

### Files and areas read

- Process: `AGENTS.md` §§1, 3.1–3.15, 5, 6, 8–14; `.opencode/agents/{orchestrator,planner,implementor,reviewer}.md`;
  `opencode.json`; `.opencode/commands/`; `docs/development/{orchestration,agent-workflow,contributing,code-structure}.md`;
  `CONTRIBUTING.md`; `tools/verify.sh`; `tools/structure-policy.json`; `.github/workflows/{ci,docs,benchmark,nightly}.yml`;
  `docs/planning/{ci-policy,status,README,04-roadmap,05-open-decisions,06-toolchain-requirements}.md`;
  `docs/planning/appendix-b-parity-matrix.md`; the Phase C step pages, design reviews, audits, amendment,
  closeout files, and `docs/planning/drift-checks/2026-11-18-phase-c.md`.
- Architecture: `00-scope-and-principles.md`, `01-target-architecture.md`, `02-data-contracts.md`
  §1–§3.5/§5.1/§5.6–§5.7/§6, `03-quality-strategy.md`, ADRs 0009–0015, `contracts/manifest.json`,
  `contracts/generated/`, `tests/contracts/`, `Mocassin.slnx`, every `src/dotnet/**/*.csproj`, and the Phase C
  production code in `Mocassin.Compilation`, `Mocassin.Contracts`, `Mocassin.Packages`, `Mocassin.Results`, and
  `Mocassin.Cli` plus their test projects (specific types/tests cited below). The read-only original at
  `/workspace/mocassin-original` was inspected for `ICon.Model/Energies/Manager/**` (energy cache),
  `Energies/Modeling/Logic/PairInteractionFinder`, and the chiral pair-interaction model; nothing was copied.
- Documentation: all published pages under `docs/` outside `docs/planning/`, `zensical.toml`, `tools/docs/`,
  `tools/check_docs_links.py`, `tools/check_doc_math.py`, `tools/docfx/docfx.json`, the built `site/`, and the
  generated `site/api/dotnet` / `site/api/rust` trees.

### Independent spot checks (code and documentation)

- Dependency graph from the actual project files: `Compilation` → `Core`, `Symmetry`; `Contracts` →
  `Compilation` (plus the generated `ContractVersions.g.cs`); `Packages` → `Compilation`, `Contracts`, `Core`;
  `Results` → `Core`, `Contracts` (not `Packages`); `Cli` → `Compilation`, `Packages`, `Results`;
  `App.ViewModels` → `Contracts`, `Packages`, `Results`; `App.Views` → `App.ViewModels`; `App` → view models,
  views, rendering, contracts, packages, results. No production `File`/`System.IO`/`Sqlite` use exists in
  `Mocassin.Core` or `Mocassin.Compilation` (only generated `GlobalUsings` mention `System.IO`).
- `tools/verify.sh` structure: `run_docs` builds Release only when DocFX inputs are missing and only when the
  `all` stage has not already built; `run_aot` is fail-not-skip and records the real CLI publish as evidence.
- Seeds: `Mocassin.Packages/Jobs/JobSeedDeriver.cs:21-101` implements the documented
  `SHA-256("mocassin/seed-text/v1" || UTF8(text))` and `SHA-256(master || u32_le collection || u32_le config ||
  u32_le job || UTF8("mocassin/job/v1"))`; `docs/reference/project-format.md:465-472` matches;
  `JobSeedDeriverTests.JobSeedMatchesDocumentedGoldenVector`, `IndicesUseFixedLittleEndianFields`, and
  `SeedTextHashingIsDomainSeparatedFromPlainSha256` pin it.
- Immutability/atomicity: `Mocassin.Packages/SimulationPackage/SimulationPackageWriter.cs:21-22,278-290` writes
  WAL during the build, switches to `journal_mode=DELETE`, hashes, fsyncs the file and directory, and replaces
  atomically; `SimulationPackagePublicationTests` asserts `-wal`/`-shm` absent and preserves the previous target
  on pre-publication cancellation; `SimulationPackageSchemaTests` asserts the published pragmas.
- Compiler: `CompilationEndToEndTests` (`CacheInvalidationTests`, `SimulationSelectionHashTests`,
  `RuntimeSettingsDoNotChangeTheModelSignature`, `ParticleChargeEditMissesTheRulesCacheAndChangesTheModel`);
  `CompilationDiagnosticEmissionTests.ConflictingRuleOutcomesEmitCmp004`; `CompilationPerformanceTests`
  (`< 1 s` ceria, 10,000 positions `< 30 s`/`< 2 GiB`); `ClusterReduction` 6-bit codes and 9-site ceiling;
  `CompiledEnergyModel.EnergyId` is the shared chiral identity.
- Package/CLI: `Mocassin.Cli/Build/BuildCommand.cs:27-35` consumes `IBuildPipeline` (`Mocassin.Packages.Build`),
  so CLI and the future GUI share one pipeline; `BuildCommandFailureTests` covers CLI002/3/4/5/6/7/8/9 and
  preservation; `CliResponseFiles.cs:25-28` has 1 MiB/4 MiB, depth 8, 10,000-token and 32-file limits.
- Documentation accuracy: `docs/reference/cli.md` matches the live `--help` graph, options, response-file rules,
  exit codes, `build-v1`/`inspect-v1`/`verify-v1`/`doctor-v1`/`clean-v1`; `docs/reference/diagnostics.md` lists
  CMP001–CMP012, CLI001–CLI009, JOB001–JOB020, PKG001–PKG010, PRM001–PRM006/008–PRM016 with the drift test
  `DiagnosticDocumentationTests`; the retired `PRM007` is intentionally absent. `docs/reference/msl-format.md`
  and `SimulationPackageSchema.cs:42-61` agree on `schema_info`/`models`/`lattices`/`jobs`.
- Navigation: a script diff of `docs/**/*.md` (excluding `docs/planning/`) against `zensical.toml` nav leaves
  only `docs/api/dotnet/index.md` and `docs/api/rust/index.md`, which are documented pre-generated-output
  placeholders that the build replaces; there is no dangling nav entry.

### Not run, and why

- Windows and macOS CI were not run: this reviewer has no runner access. The recorded runs were checked against
  the step pages, closeout evidence, and tag messages (`Windows` closeout/gates `43944396192` at `4197166` and
  `44378270028` at `07851d8`; `macOS` never run, accepted as N‑5/drift). This is an accepted platform gap, not a
  passing result.
- The PowerShell docs build (`tools/docs/build-docs.ps1`) was not executed on Linux. Its input check mirrors the
  bash script line for line (`tools/docs/build-docs.ps1:18-42`); the Windows execution remains unverified and is
  recorded as a platform gap (drift record “Notes for XC-04”).
- The `Benchmark` workflow was not dispatched; the Step 09 benchmark numbers were checked against the performance
  page and the benchmark suite source, not re-measured.
- The 10⁴-job run was an approximate reproduction (see table row 16), not the exact recorded project.
- I did not access the GitHub API; run identities, `head_sha` values, and `[skip ci]` usage were verified from the
  durable step pages, closeout JSON, audit records, tag messages, and workflow definitions, which agree.

### Original-design fidelity detail

**C1 — intended corrections.** Trace = parity matrix §11 row / step page defect table. Evidence is the test or
code that demonstrates the corrected behaviour; all were verified present at the frozen commit.

| Intended correction | Trace | Corrected evidence | Status |
| --- | --- | --- | --- |
| Randomized `string.GetHashCode` model hash → canonical SHA-256 | §11; ADR 0010 | `ModelSignatureProjection`, `ProjectHasher`, `RuntimeSettingsDoNotChangeTheModelSignature`, projection vectors | Corrected |
| Whole-cache invalidation after one edit → content-hash stage caches | §11; Step 08 §13 | `CacheInvalidationTests.{CallerCacheReusesStructureRulesAndSigningKeys, ParticleChargeEditMissesTheRulesCacheAndChangesTheModel, EnergyEnvironmentEditReusesStructureAndRulesButMissesEncoding, ParticleSetMembershipEditMissesTheStructureCache}` | Corrected |
| O(R²) inverse/dependent pairing → hash lookup/dedup | §11 | `TransitionCompilationStage.Rules` + `TransitionRuleHelpers` dedup key; `ExplicitReverseCompilationTests`; `ConflictingRuleOutcomesEmitCmp004` | Corrected (lookup/dedup); dependent *instances* remain the accepted m1 gap (C2/C3) |
| Runtime ambiguity → compile-time `CMP004` | §5/§11 | `CompilationDiagnosticEmissionTests.ConflictingRuleOutcomesEmitCmp004`; `docs/reference/diagnostics.md` CMP004 | Corrected |
| Uncounted dependent rules → explicit counts | §11 | `CompilationCounts`/report `forward/inverse/dependent` counts; step page §13 table | Corrected (counts exist and report 0 when none are needed) |
| Non-deterministic/shared lattice RNG → domain-separated seeds | §11; ADR 0005 | `JobSeedDeriver` + golden vectors; `DifferentIndicesProduceDifferentSeeds`; `docs/reference/project-format.md:465-472` | Corrected |
| Uninitialized positions → explicit void initialization | §11 | `SupercellGeneratorTests.DefaultOccupationsUseTheFirstSetParticleAndTheVoidForUnstableSites` | Corrected |
| Inverted/double-applied optimizer masks | §11 | `SelectionOptimizerResolverTests` (dual-DOF kept/cleared cases, `JOB009`, deterministic, MMC ignores optimizers) | Corrected |
| Discarded base lattice config | §11 | `JobSettingsResolverTests` + explicit simulation → collection → config inheritance; `02-data-contracts.md` §2.4 | Corrected |
| MMC frequency NaN | §11 | `CompiledJumpRule.DefaultFrequencyHz` is a finite `1e13`; `ParameterizationEntryWalker.ValidateFrequency` rejects non-finite/non-positive values (`PRM005`); KMC and MMC simulations/rules are distinct kinds (`CompiledSimulation`, `SimulationPayloadMapper`) | Corrected (finite values, kind separation) |
| Reflection/service-locator model layer → immutable domain + explicit compiler | §11 | `Mocassin.Core` immutable records; `IModelCompiler`/`ModelCompiler`; `Architecture.Tests` structure test | Corrected |

No intended correction is silently left in place. The only residual is the dependent-instance gap that Step 08
audit note m1 already records.

**C2 — ported strengths.** “Implemented and verified” means code + test at this commit; “design only” names the
owning step and is not claimed as ported.

| Original strength | Kept / adapted / dropped | Evidence |
| --- | --- | --- |
| Auto-sampled pair interactions with a shared chiral energy table | Kept and adapted | `PairCompilationStage` reuses the Step 05 sampler; `CompiledEnergyModel.EnergyId` is the shared chiral identity; `StructureAndPairCompilationTests`/`PairPreviewCrossCheckTests`; chiral template fixture `project-chiral-p1.mocprj` |
| Canonical P1 ordering and dense compiled indices | Kept | `CompilationContext`/`CompiledStructureModel`; `RuntimeSettingsDoNotChangeTheModelSignature`; Step 02 canonical order reused |
| Pair/cluster shell reduction and 6-bit occupation codes | Kept | `ClusterReduction` (9 sites, `0..63`), `CompiledClusterTable`, `ClusterCompilationTests`; canonical shell order in `PairCompilationStage` |
| Per-environment energy cache with path-local `S2` deltas | Adapted in Phase C; engine design only | Step 08 replaces the model-builder energy cache with content-hash stage caches (`StageCache`, `Caching/*`). The runtime per-environment cache and dynamic `S2` are designed in `step-15-energy-engine.md:119-170` and `step-14-simulation-context-initialization.md:177-182`; not claimed as ported. |
| Static `S2` corrections | Design only | `step-15-energy-engine.md:138-142,218`; Step 08 explicitly stores `null`/NaN and uses dynamic S2 (step page §8.3); owned by Step 15 |
| Selection pools and selection optimizers | Partly kept, rest scheduled | P1 image expansion + dual-DOF + `JOB009` in Step 10 (`SelectionOptimizerResolver`, tests); `.msl` encoding in Step 11; runtime pool in Step 14 (`step-14-simulation-context-initialization.md:223-224`); UI in Step 26. Parity row says Partial until 14 — honest. |
| Pre-run normalization and the KMC time-step formula | Design only | `step-16-kmc-engine.md` and parity §7 rows `Planned` 16; not claimed as ported |
| CSR neighbour/jump links | Adapted now, CSR design later | Step 08 emits dense `jump_count`/`jump_mapping` and pair links; CSR `env_offsets`/`env_links`/`jump status` designed in `step-13-rust-solver-foundation-and-package-reader.md:231-233` and `step-14-simulation-context-initialization.md:147-177` |
| Incremental lattice-energy updates | Design only | `step-15-energy-engine.md:170`; owned by Step 15 |
| Model-builder workflow: build recipe, preview, stage/result reporting, log-console diagnostics | Kept and improved | Step 12 `--template`/`--job-template`/`--parameterization` recipe, `--preview` (verified writes nothing), ten `build.stage` events, `build-v1`/`doctor-v1`, `doctor` (seven checks, live healthy), structured diagnostics instead of a log console |

**C3 — regressions.** Capability: the Phase C surface is broader than the original model builder's compilation
and packaging path (structured reports, preview, doctor, immutable verified package, deterministic seeds). The
one capability gap is dependent/hidden rule *instances* (C2, audit m1): recorded and accepted, but the parity
row still names the completed Step 08, so the follow-up owner is not durable — XC04-F3. Semantics: supercell,
doping, truncation, compensation, and rule generation have real-data acceptance/property tests
(`CeriaDopingAcceptanceTests`, `DopingApplicationTests`, `SupercellGeneratorTests`, `TransitionCompilationTests`),
and `.msl` content is queryable through the shared inspector (live `inspect`/`verify`). Performance/memory:
Phase C has its own budgets; no regression claim is made against the original build path because they are not
the same workload, and no benchmark is presented as a comparison. Diagnostics and determinism are improvements
(structured `CMP`/`PRM`/`JOB`/`PKG`/`CLI`, canonical SHA-256). No unrecorded regression was found; the accepted
residuals (dependent instances, macOS gate, AOT publish diagnostics, Step 11 N‑1 fsync ordering, nightly fuzz
deferral) are recorded with owners. Maintainability: the dependency direction, purity tests, and structure
policy hold.

**C4 — evidence quality.** The tables above cite test names, file paths, and live command results; no row rests
on prose alone. The original was inspected at
`/workspace/mocassin-original/src/ModelBuilder/ICon.Model/Energies/Manager/` (`EnergyModelCache`,
`EnergyCacheManager`), `Energies/Modeling/Logic/PairInteractionFinder/PairInteractionFinder.cs`, and the chiral
`StablePairInteraction` model; the comparison is by design, not code copying.

**C5 — no silent loss or addition.** No intended correction is missing. No intended strength is silently
absent: the dependent-rule instances are absent but openly recorded (audit m1) and are the only gap; their
parity status/owner is XC04-F3. No regression was introduced without a record. No Phase D/E/F feature was
silently implemented.


## Checklist

Instructions for the reviewer:

- Tick an item only after checking it against the repository, and add a short inline note naming the file,
  command, or test that supports the tick. Do not change item text.
- "Evidence" means a file reference, a test name or run, a golden fixture, a benchmark result, a command
  output, or a side-by-side comparison. A statement in a planning page is a claim to verify, not evidence.
- Where an item cannot be checked, write why in `Evidence` and leave it unticked rather than ticking it
  conditionally.
- All three sections must be reviewed and their checklists completed before a combined verdict is written.

### A. Policies and planning

- [x] A1. `AGENTS.md` §1 current-state text, §3.14 phase-gate description, and §3.15 rule-home table match the
      repository and the process actually used for Phase C (combined review, frozen commit, fresh reviewer,
      one record commit).
      - Verified: `AGENTS.md:11-16` states Phase C complete/`XC-04` in progress/Step 13 next, matching
        `status.md` and the tags. §3.14 describes one combined review from Phase C; §3.15 names the normative
        homes used here. This review used the frozen commit, a fresh reviewer, and one record commit.
- [ ] A2. `docs/planning/status.md` (dashboard rows 08–12, Active work, phase-checkpoint table, drift-check
      table) matches the tags, merge commits, CI runs, audit/review verdicts, and step pages at the frozen
      commit; the Phase C row and the XC-04 row do not claim an outcome.
      - Not ticked: one dashboard cell is wrong. `status.md:58` (Step 08, `Current feature`) still reads
        `step-08` where every other completed row uses `—`; everything else (rows 08–12, Active work,
        phase-checkpoint table, drift-check table, no claimed verdict) agrees with tags/records.
        See XC04-F1.
- [ ] A3. Step pages 08–12 record the correct status, step branch, step CI, main merge, tag, design-review and
      audit verdicts with round/commit references, feature logs, acceptance boxes, and deviations; no step
      page claims a feature that has no code/test behind it.
      - Not ticked: `step-08-model-compilation-pipeline.md:784-840` leaves all eight implementation-task boxes
        and all 16 acceptance criteria unticked although the page and dashboard say `Done` (steps 09, 10, 12
        are fully ticked; step 11's one open box carries explicit Step 13/18 owners). Status, CI, tag, review,
        audit, feature log, and deviations are otherwise correct. See XC04-F2.
- [x] A4. Design reviews and audits for 08–12 (and Step 11 amendment 1) are append-only: every round is
      present, the round table and finding/resolution tables are complete, reviewer text was not rewritten,
      and the human-approval sections are signed wherever a verdict is recorded as accepted.
      - Verified: `audits/step-08.md` has four rounds and a round table; `step-09.md` two; `step-10.md` two;
        `step-11.md` two; `step-12.md` two (round 2 explicitly records that the earlier sections are
        byte-identical). Every audit has `Human approval` signed; reviews 08–12 have `Review rounds` and
        signed approvals. Resolutions live in separate sections.
- [ ] A5. Closeout evidence for 08–12 passes `check_closeout_evidence.py` and is consistent with the step
      pages and audit records: audited commit, closeout CI run and `head_sha`, `main` SHA at freeze, merge
      candidate, platform CI, tag, and notes all agree.
      - Not ticked: all five files pass the checker, and the audit/CI/main/candidate values agree, but
        `closeout/step-11.json` has `"tag": ""` although the annotated `step-11` tag exists on `84ceb76`
        (`closeout/README.md` requires the tag field “when it exists”), and `closeout/step-12.json:22` still
        says “No tag exists and no merge into main has been performed” after the tag/merge note was added.
        See XC04-F5.
- [x] A6. Phase C Git history follows the branch model: one step branch per step, feature work squash-merged
      into the step branch, step merged with `--no-ff`, annotated tag on `main` only after green main CI, and
      step/feature branches deleted; no per-feature CI run is claimed.
      - Verified: all five step tags are annotated and point at two-parent merges; their messages carry the
        green main CI run; no `step/*`/`feature/*` ref remains locally or on `origin`; step pages record
        “Not run (no per-feature CI)” for every feature.
- [x] A7. The two Step 12 process deviations are recorded with date, context, owner, rationale, and
      no-action/closure: the planner's status-cell edit before a step agent existed (§3.8 write scope), and the
      direct session question to the owner during feature 1 instead of routing through the orchestrator
      (`AGENTS.md` §3.8/§7). Check whether the records overstate, understate, or omit any related action.
      - Verified: `step-12-build-orchestration-and-cli.md:73-88` records both with date, context, owner, and
        “no action required”, plus the interim CLI008 placeholders and the AOT/publish deviation. It does not
        overstate: the status-cell edit and the direct question are the only process deviations claimed, and
        `orchestration.md:131-137` separately records the earlier Step 10 feature-1 squash.
- [x] A8. The `OpenCode 2.3.0 cold-start spawn race` note in `docs/development/orchestration.md` §9 is
      version-scoped and states symptoms, durable log evidence, the branch-tip/previous-context inspection
      rule, the mitigation, and the removal condition; the role definitions in `.opencode/agents/` and
      `opencode.json` match the orchestration page's binding table.
      - Verified: `orchestration.md:163-185` names OpenCode 2.3.0, `Session.AgentNotFoundError`, the log
        evidence, the branch-tip/previous-context rule, the registry-preload mitigation, and the removal
        condition. `.opencode/agents/*.md` and `opencode.json` match §9's table (orchestrator denied edit and
        limited to three subagents; reviewer limited to `step-*.md`/`xc-*.md`; planner denied code paths).
- [x] A9. `docs/development/orchestration.md`, `agent-workflow.md`, `contributing.md`, `CONTRIBUTING.md`, and
      the review/audit/amendment READMEs agree with `AGENTS.md` §3.8–§3.12 on role scopes, reviewer
      independence, checklist ownership, and the approval gate, and with the record shapes actually produced
      in Phase C.
      - Verified by reading all six documents against the Phase C records: role scopes, independence, checklist
        ownership, `[skip ci]` record commits, and the approval gate agree. One already-accepted mismatch
        (`XC01-F9`: the contributing CI table omits the space-group drift check) remains open and is carried as
        XC04-N1.
- [x] A10. CI policy and cost: `.github/workflows/*.yml` match `ci-policy.md` §2 and §3.13 (automatic
      `main`-only Ubuntu, one closeout dispatch per step, manual Windows/macOS/docs/benchmark/nightly, path
      filters, `[skip ci]` usage), and the Phase C run record shows no per-feature or per-push step-branch
      run.
      - Verified: `ci.yml:27-45` triggers on `push: main` and `workflow_dispatch` only; `docs.yml`,
        `benchmark.yml`, and `nightly.yml` are manual; `nightly.yml` has no schedule; permissions are
        `contents: read`; concurrency is per-ref. Every step page records one closeout dispatch (plus a
        replacement/re-run where stated) and no per-feature CI.
- [x] A11. Code-structure policy is enforced and current: `tools/structure-policy.json`,
      `Mocassin.Architecture.Tests`, `docs/development/code-structure.md`, and `AGENTS.md` §8 agree on folder
      depth, file counts, forbidden folders, file-name suffixes, and namespaces; Phase C projects comply and
      `verify.sh structure` passes.
      - Verified: `verify.sh all` ran `Mocassin.Architecture.Tests` (1 passed); the Phase C folders
        (`Compilation/{Compiler,Caching,Context,Diagnostics,Energy,Indexing,Signatures,Simulations,Structure,Transitions}`,
        `Packages/{Build,Jobs,Parameterization,Projects,SimulationPackage}`) comply with the depth/file/suffix
        rules and namespaces match folders.
- [x] A12. Local gate parity: `tools/verify.sh` modes (`all`, `docs`, `policy`, `contracts`, `aot`) cover the
      Phase C CI jobs and the commands documented in `AGENTS.md` §6 and `contributing.md`; the required `aot`
      gate is correctly described as local and as part of `all` from Step 12.
      - Verified: `verify.sh` modes and the `all` order (`dotnet`, `api`, `rust`, `python`, `docs`, `contracts`,
        `aot`) map to the CI jobs plus the local AOT gate; `docs` self-builds when needed; `aot` is
        fail-not-skip and is described as local in `step-12`/`docs/performance`.
- [x] A13. ADRs 0009–0015 are `Accepted` with current status/date/deciders and no contradiction with the
      implemented contracts; `05-open-decisions.md` marks every resolved decision as resolved and keeps only
      genuinely gated/deferred items; `04-roadmap.md` records the Step 09 and Step 12 sizing exceptions and
      the Step 12 parser change consistently with the step pages.
      - Verified: all seven ADRs are `Status: Accepted` (0015 dated 2026-11-17 with the owner decision);
        `04-roadmap.md:107-125` records both sizing exceptions; `05-open-decisions.md` has no Phase C item
        claiming resolved while open; ADR 0015 matches the `System.CommandLine` 3.0.0 pins and the no-completion
        decision in `step-12`.
- [ ] A14. `appendix-b-parity-matrix.md` Phase C rows (`§5 Compilation pipeline`, `§6 Simulations and
      templates`, `§10 Quality`, `§11 original defects`) match implemented code/tests: no row marked `Done`
      lacks evidence, and every `Partial`/`Planned`/deferred row names its owning step or accepted deferral.
      - Not ticked: `§3` rows `Reverse rule generation (2/3-site)`, `No reverse for complex mechanisms`, and
        `Dependent/hidden rules` are still `Planned` with Step `08`, although the first two are implemented
        (`ExplicitReverseCompilationTests`, `TransitionCompilationStage`) and Step 08 is complete; the third is
        the accepted audit m1 gap but has no future owning step in the row. `§5` keeps the matching `Partial`
        row without an owner. See XC04-F3.
- [x] A15. Milestone and tag policy is applied: the roadmap's M4/M5 milestones and their tag status are
      consistent with §3.7 and the roadmap's "tagged when reached" rule, or the deferral is recorded with an
      owner.
      - Verified: annotated `milestone-m4` (`f91fc63`) and `milestone-m5` (`ec2e04a`) match the roadmap's
        “after step 08/12” definition and carry the required fields; `status.md` and `04-roadmap.md` record
        the 2026-11-18 backfill; no step tag moved.
- [x] A16. The Phase C boundary has the required drift check (or a recorded waiver/deferral) and its findings
      are disposed of in a durable record; the phase gate does not rest only on step-level audits.
      - Verified: `drift-checks/2026-11-18-phase-c.md` exists, was checked at `dcbf499`, records PC‑01–PC‑07
        with owners/actions, and the fixes are visible at `2127f0f` (AGENTS/README/index/development index,
        roadmap §2, dashboard Active work, milestone tags, docs self-build). Its two “Notes for XC-04” are
        covered here (PowerShell/Windows gap) or accepted (history retained).
- [x] A17. Every process claim needed for the verdict is checkable from durable files alone; no part of the
      phase's evidence depends on chat history or on the orchestrator's summary.
      - Verified: tags, merges, CI run IDs, closeout JSON, audit/review rounds, deviations, drift dispositions,
        and gate output are all durable files or reproducible commands. The run IDs themselves are not
        re-queried here; the closeout checker and the step pages bind them to commits.

### B. Architecture and design status

- [x] B1. The dependency graph for Phase C matches `AGENTS.md` §5 and `01-target-architecture.md` §3: `Core`
      ← `Symmetry` ← `Compilation` ← `Contracts` ← `Packages` ← `Cli`, `Results` depends on `Contracts` (not
      `Packages`), `Packages.Build` owns orchestration, and the CLI references only the projects it uses.
      Check the actual `*.csproj` references, not only the diagram.
      - Verified against every `.csproj`: `Compilation → Core, Symmetry`; `Contracts → Compilation`;
        `Packages → Compilation, Contracts, Core`; `Results → Core, Contracts`; `Cli → Compilation, Packages,
        Results`; `Packages/Build` holds `BuildPipeline`/`IBuildPipeline`; the CLI has no reference to
        `Symmetry`/`Contracts` directly. App layering also matches the documented target.
- [x] B2. Purity and I/O boundaries hold: `Core` and `Compilation` have no file I/O, SQLite, UI, or solver
      dependency; `.mprj` persistence is in `Packages`; package writing is in `Packages`; package
      reading/inspection/verification is in `Results`; the Rust solver boundary is untouched.
      - Verified: no `System.IO`/`Microsoft.Data.Sqlite`/file API use in `Mocassin.Core` or
        `Mocassin.Compilation` production sources; `Packages/Projects` owns project JSON/locking/autosave;
        `Packages/SimulationPackage` owns writing; `Results/Packages` owns inspection/verification; the Rust
        workspace still only reads `.msl`-shaped data (no project JSON) and Phase C did not add solver code.
- [ ] B3. `contracts/manifest.json` is the single machine-readable source for Phase C formats/reports:
      generated schemas, constants, and projection vectors are current (`generate.py --check`, model/
      parameterization/lattice projection checks), and the manifest `status`/`notes` convention accounts for
      the Phase C schema additions without silent gaps.
      - Not ticked: all generated artifacts and the three projection checks pass, but the top-level `status`
        is `step-12` while `notes` narrates only Steps 06, 07, 10, and 12. Step 09 finalized
        `signatures.parameterization` and Step 11 added the `.msl` payload/schema, and neither is narrated;
        this is the Phase B `XC01-F6` convention regressing. See XC04-F4.
- [x] B4. Interface 08 → 09: the compiled model's dense indices, `EnergyId`/`GeometryId`/rule identities,
      geometry/rules signature, tracker definitions, and site-energy boundary match the Step 09 consumer; the
      parameterization signature covers model + applied values and excludes runtime settings; `Compilation`
      remains parameterization-free.
      - Verified: `ParameterizationSignatureV1.Compute(modelSignature, ParameterizationValues)` is the
        six-field projection documented in `02-data-contracts.md` §1.3/§3.3; `CompilationEndToEndTests.RuntimeSettingsDoNotChangeTheModelSignature`
        and the parameterization projection vectors pin exclusion of runtime settings; Step 08 explicitly moves
        authored site energies to Step 09 (`step-08` implementation note; Step 09 applies them).
- [x] B5. Interface 09 → 10: template identities and sparse defaults, canonical JSON, `PRM` structural vs
      compiled-bound validation, and the `Apply`/`Validate` contract feed job expansion and build-stage
      validation without a second validation or apply implementation.
      - Verified: `Mocassin.Packages/Parameterization` exposes the single apply/validate service consumed by
        `BuildPipeline`; `PRM003`/`PRM006` warning path is covered end to end by
        `tests/contracts/project-parameterization-warning.mocprj` and `BuildParameterizationWarningTests`
        (warning-free build; `--warnings-as-errors` exits 4); structural `PRM` codes stay in Core validation.
- [x] B6. Interface 10 → 11: job expansion and selection normal form, per-job/lattice seed derivation
      (ADR 0005 tuple order, SHA-256 endianness), doping truncation/compensation results, deterministic
      lattice generation, and the job payload fields match the writer and the data contract.
      - Verified: `JobSeedDeriver`/`LatticeSeedDeriver` implement the documented SHA-256 construction with
        fixed little-endian indices and are pinned by golden-vector tests; `SupercellGeneratorTests` covers
        deterministic generation, row-major cell order, void initialization, and JOB004/JOB006/JOB014; the
        rendered selection `1-3` and `all` were observed live and the package records it in `job_selection`.
- [x] B7. Interface 11 → 12: the writer's publication barrier and immutability, the single `Results` reader
      reused by `inspect`/`verify`, the `PKG` descriptors, `build-v1` and the other command reports, the exit
      code map, and reserved commands match `02-data-contracts.md` §3 and §5 and the CLI implementation.
      - Verified: `inspect`/`verify` call the `Results` inspector/verifier (no second reader); a live build
        verified with 10 checks; `simulate` exits 2 with `CLI008`; `BuildCommandFailureTests` covers the
        CLI002/3/4/5/6/7/8/9 map; the report schemas are the seven manifest descriptors.
- [x] B8. End-to-end 08 → 12: one build pipeline (load → select → validate → compile → build-stage validate →
      parameterize → expand → generate → publish → verify) is used by the CLI and is the same library the
      future GUI consumes; diagnostic ownership (`CMP`, `PRM`, `JOB`, `PKG`, `CLI`) is unambiguous and
      non-duplicated.
      - Verified: `Mocassin.Cli/Build/BuildCommand.cs:27-35` constructs `Mocassin.Packages.Build.BuildPipeline`;
        the live `compile-v1` stage order and `build.stage` events match the pipeline; `DiagnosticDocumentationTests`
        and the catalog composition keep `CMP`/`PRM`/`JOB`/`PKG`/`CLI` in their owning assemblies.
- [x] B9. Determinism and identity: canonical projections, quantized signatures, SHA-256 content IDs,
      content-addressed lattices, and stable ordering are implemented and tested; the compiled,
      parameterization, lattice, and package-summary signatures are distinct and documented; no
      hash-iteration, culture, clock, or path dependence can change a published byte or signable value.
      - Verified: domain prefixes `mocassin/model-signature/v1` (model), `mocassin/parameterization-signature/v1`,
        `mocassin/lattice-signature/v1` and the package summary are documented distinct in
        `02-data-contracts.md:115-128` and `design-decisions.md:121-125`; `RuntimeSettingsDoNotChange*`,
        `CacheInvalidationTests`, `JobSeedDeriverTests.DerivationIsDeterministic`, lattice
        deduplication/reuse tests, and invariant-culture projection writers cover the claim. Live rebuilds
        produced identical lattice reuse/signature shapes.
- [x] B10. Immutability, atomicity, and failure behaviour: `.msl` is immutable after publication, requires no
      WAL sidecars, is written through the documented atomic path, and a failed or cancelled build never
      leaves a valid-looking package; the post-publication verification result and exit mapping are
      consistent.
      - Verified: writer switches to `journal_mode=DELETE`, hashes, fsyncs file and directory, and replaces
        atomically; `SimulationPackagePublicationTests` asserts no `-wal`/`-shm`, target preservation on
        cancellation, and no temp file; CLI tests assert verification failure leaves the published package and
        exits 1, cancellation exits 6 with `published=false`. The known N‑1 directory-fsync-before-rename
        ordering remains the accepted Step 11 note.
- [x] B11. Phase C input boundaries are treated as untrusted where the design says so: bounded counts and
      allocations, index/NaN/chirality/duplicate validation, package parsing and payload limits, response-file
      limits, and path handling; the corresponding tests exist or the gap is an explicitly accepted item.
      - Verified: response files enforce 1 MiB/4 MiB/depth 8/10,000 tokens/32 files (`CliResponseFiles.cs:25-28`);
        package decoding has `LatticePayloadBoundTests`, `LatticeDecodeGuardTests`, `PayloadPropertyTests`, and
        `PackageVerifierTests`; JOB004/JOB013/JOB019 hard caps and ENM/CMP limits are tested; nightly `.msl`
        fuzzing is explicitly deferred to Step 29 with an owner.
- [ ] B12. Design status is honest: no Phase C design decision was implemented differently from its frozen
      page without a recorded deviation, amendment, or ADR; `05-open-decisions.md` and the parity matrix match
      the code; no Phase D/E/F feature is silently implemented or claimed as complete.
      - Not ticked: design deviations are recorded and no later-phase feature is claimed, but the parity
        matrix does not match the code for the reverse/dependent rule rows (see A14/XC04-F3).
- [x] B13. Phase C performance and memory evidence is reproducible and correctly tiered: compile/cache,
      package write/verify, and the 10⁴-job workload have recorded commands and results; deferred targets name
      an owner and a later step; no benchmark is used to claim a target it did not measure.
      - Verified: `CompilationPerformanceTests` enforces ceria `< 1 s` and 10,000 positions `< 30 s`/`< 2 GiB`;
        Step 09 benchmark numbers and Step 12/Step 11 package numbers are recorded with runner/method on
        `docs/performance/index.md`; my 10⁴ reproduction (3.56 s, 147,908 KB) matches the recorded budget; the
        Step 10 `< 10 s`/`< 1 GiB` target deferral is recorded as R2‑1 with Step 29/release ownership.

### C. Original-design fidelity

Required for the combined review. Answer all three questions explicitly and cite concrete evidence for every
row. Compare against the read-only original at `/workspace/mocassin-original`; do not copy its code.

- [x] C1. **Intended corrections.** Build a table of every Phase C correction the plan intended (at minimum
      the Phase C rows of `appendix-b-parity-matrix.md` §11 and each step page's original-defect table):
      correction, trace to a requirement/ADR/step decision, corrected evidence (test name, code, fixture, or
      command), and status (corrected / partially corrected / not corrected / not applicable). Cover at least:
      randomized `string.GetHashCode` model hash → canonical SHA-256; whole-cache invalidation → content-hash
      stage caches; O(R²) inverse/dependent pairing → hash lookup/dedup; runtime ambiguity → compile-time
      `CMP004`; uncounted dependent rules → explicit counts; non-deterministic/shared lattice RNG →
      domain-separated seeds; uninitialized positions → explicit void initialization; inverted/double-applied
      optimizer masks; discarded base lattice config; MMC frequency NaN; and the reflection/service-locator
      model layer → immutable domain + explicit compiler. No intended correction may remain silently in place;
      a partial correction must state the residual.
      - Verified: the C1 table under “Original-design fidelity detail” covers all eleven corrections with
        trace and evidence. Ten are corrected; the O(R²)/dependent row is corrected for lookup/dedup and its
        dependent-instance residual is the recorded m1 gap.
- [x] C2. **Ported strengths.** Build a table of the original's good/clever aspects that Phase C intended to
      keep, and state **kept / adapted / dropped** with the reason and evidence. Cover at least: auto-sampled
      pair interactions with a shared chiral energy table; canonical P1 ordering and dense compiled indices;
      pair/cluster shell reduction and 6-bit occupation codes; the per-environment energy cache with
      path-local `S2` deltas; static `S2` corrections; selection pools and selection optimizers; pre-run
      normalization and the KMC time-step formula; CSR neighbour/jump links; incremental lattice-energy
      updates; and the original model-builder workflow ideas (build recipe, preview, stage/result reporting,
      log-console diagnostics). Distinguish "implemented and verified" from "design only for a later phase";
      a design-only strength must name its owning step and must not be claimed as ported.
      - Verified: the C2 table states kept/adapted/design-only per row. Pair/cluster/chiral tables, P1 order,
        6-bit codes, selection optimizers (Phase C half), and the build workflow are implemented and tested;
        the per-environment energy cache, static S2, pre-run/time-step, CSR runtime links, and incremental
        updates are design-only for Steps 13–16 and are not claimed as ported.
- [x] C3. **Regressions.** Compare Phase C capability, semantics, performance, memory, diagnostics,
      determinism, and maintainability against the original with concrete evidence. Check in particular:
      rule generation coverage (reverse/dependent/complex), energy table identity and chiral sharing,
      supercell/doping semantics, `.msl` content and queryability, build workflow ergonomics, error
      reporting, and determinism. A deliberate regression is acceptable only with a recorded rationale,
      owner, and follow-up; an unrecorded regression is a finding.
      - Verified: the C3 paragraph compares each area with tests/commands. Reverse/explicit/complex rule
        generation is tested; dependent instances are the recorded m1 gap (owner missing — XC04-F3); chiral
        sharing, supercell/doping, `.msl` queryability, workflows, diagnostics, and determinism are equal or
        improved. No unrecorded regression found.
- [x] C4. **Evidence quality.** Every answer above cites a file:line, test name/run, golden fixture,
      benchmark result, or a side-by-side table; prose assertions, planning-page claims, or audit text alone
      do not satisfy the requirement.
      - Verified: C1/C2 rows cite test class/method names, source paths, and live command results; the
        original-side inspection is named in C4.
- [x] C5. **No silent loss or silent addition.** The record states explicitly whether any intended Phase C
      correction is missing, any intended strength is absent, or any regression was introduced without a
      record, and names the exact item. If none, say so explicitly.
      - Verified: C5 states that no intended correction is missing, the dependent-rule instances are the only
        openly recorded absent strength, no regression is unrecorded, and no later-phase feature is silently
        added. The status/ownership defect for that item is XC04-F3.

### D. Documentation status

- [x] D1. Coverage: every implemented Phase C surface has a published or developer page — CLI commands,
      options, exit codes and reports; `.mocprj` build templates and parameterization; job templates;
      `.msl` format and inspection; compilation pipeline; diagnostics; building workflow; performance; and
      troubleshooting. Name any implemented surface with no page.
      - Verified: `reference/cli.md` (all commands/options/reports/exit codes/reserved commands),
        `user-guide/{building,parameterization,job-templates,lattice}.md`, `reference/{msl-format,project-format,diagnostics,transitions,energy}.md`,
        `development/{compilation-pipeline,architecture,design-decisions,testing-and-parity}.md`,
        `performance/index.md`, `troubleshooting/errors.md`. No implemented Phase C surface lacks a page.
- [x] D2. CLI accuracy: `docs/reference/cli.md` matches the implemented command graph, option names and
      semantics (`--template` vs `--job-template`, `--jobs`, `--parameterization`, `--force`, `--parallel`,
      `--compact`, `--warnings-as-errors`, `--quiet`, `--json`), response-file syntax/limits, help/version
      behaviour, reserved commands, exit codes, and report schemas.
      - Verified against live `--help`, `validate`, `compile`, `build`/`--preview`/`--jobs`/`--json`,
        `inspect`, `verify`, `doctor`, `clean --dry-run`, `new`, a response file, and `simulate` (exit 2,
        `CLI008`). Option semantics and report kinds match; `cli.md:127-139` matches `BuildCommandFailureTests`
        for CLI004/CLI005. Response-file constants match `CliResponseFiles.cs:25-28`.
- [x] D3. Format accuracy: `docs/reference/msl-format.md`, `project-format.md`, and the data-contract pages
      match the Step 11 writer/inspector and the Step 12 reports — tables, payloads, signatures, publication
      barrier, read-only guarantees, and the canonical package summary.
      - Verified: `msl-format.md:17-30,130,143,173` matches `SimulationPackageSchema.cs:42-61` and the writer's
        `journal_mode=DELETE`/fsync/replace barrier; the live package's 10 verification checks and canonical
        summary match `verify-v1`; seed/lattice/parameterization projections match §1/§2 of the data contracts.
- [x] D4. Pipeline accuracy: `docs/development/compilation-pipeline.md`, `architecture.md`,
      `design-decisions.md`, and `testing-and-parity.md` match the implemented assemblies, stage order,
      ownership boundaries, and test tiers; no page describes a second pipeline, a stale type name, or an
      abandoned design.
      - Verified: the page's ten-stage order matches `compile-v1` live output and `CompilationPipeline`;
        ownership (Step 08 compiler, Step 09 values, Step 10 jobs, Step 11 package, Step 12 orchestration)
        matches the projects and the code; no second pipeline exists.
- [x] D5. Navigation: `zensical.toml` lists every published Phase C page and generated API target, has no
      dangling nav entry, and the strict build passes; new pages are reachable from the section indexes.
      - Verified: strict Zensical build clean; a nav-vs-tree diff leaves only the two documented API
        pre-generation placeholders (`docs/api/dotnet/index.md`, `docs/api/rust/index.md`), which the build
        replaces with generated content; section indexes link the Phase C pages.
- [x] D6. API docs: DocFX builds with no missing public-item documentation for the new Phase C public types;
      rustdoc remains clean for the untouched workspace; the Python API placeholder is still explicitly
      owned by Step 22 and not presented as delivered.
      - Verified: DocFX ran with `--warningsAsErrors` and passed; rustdoc clean; `docs/api/python/index.md`
        says “Step 22 replaces this placeholder” and `docs/api/index.md` marks it as a placeholder.
- [x] D7. Publication boundary: the built site contains no `planning/` directory, the search index and
      sitemap contain no planning records, and this cross-cutting record is not published.
      - Verified: `site/planning` absent; `site/sitemap.xml` has no planning entry; `search.json` contains
        only prose references to `docs/planning` on published pages, no planning record; this record is under
        `docs/planning/` and excluded.
- [x] D8. Links and math: `check_docs_links.py` and `check_doc_math.py` pass with the recorded file counts;
      internal anchors resolve; published pages use `$$…$$`/`\(…\)` per `AGENTS.md` §9 and planning pages use
      `$…$`/`$$…$$`.
      - Verified: links 171 files in the clean frozen worktree / 172 with this record, math 168/169, both
        exit 0; the strict Zensical build (which validates anchors) is clean; no `\(`/`\[` in published pages
        found.
- [x] D9. Examples and claims: documented sample commands run against `samples/`; JSON examples are
      schema-valid or explicitly illustrative; diagnostic codes/titles/severities on the diagnostics page
      match the composed catalog; performance numbers on the performance page match the recorded evidence
      and are not rounded into a stronger claim.
      - Verified: the getting-started/CLI sample commands were run (validate/build/verify/inspect/doctor);
        diagnostics page is generated-drift tested (`DiagnosticDocumentationTests`, and the PRM/JOB catalog
        tests); performance numbers match the step/closeout records, including the toolchain note and the
        “not repeated” caveat.
- [x] D10. Status surfaces: `README.md`, `docs/index.md`, `docs/development/index.md`, `docs/planning/README.md`,
      and the step pages describe Phase C completion and Step 13 next consistently with `status.md`; no page
      still advertises an earlier phase as current or a completed command as future work.
      - Verified: all five say Phase C complete/Steps 00–12 tagged/`XC-04` in progress/Step 13 next;
        `docs/planning/README.md:150` says complete through Step 12. No published page advertises Phase B or
        a completed command as future work.
- [x] D11. No stale placeholder vocabulary on Phase C pages: no `TODO`, `FIXME`, "coming soon", "not
      implemented", or future-tense claim for behaviour that now exists, and no page promises behaviour that
      the code does not have.
      - Verified: a scan of `docs/` outside `docs/planning/` found only accurate future-tense statements
        (solver arrives later, Python API Step 22, Rust/DocFX pre-build placeholders). No TODO/FIXME/coming
        soon or false assertion of implemented behaviour.

### E. Exit criteria

- [x] E1. All three subject sections (policies, architecture/fidelity, documentation) and their checklists
      have been reviewed and completed; the verdict states the combined result and, if the sections differ,
      which section drives the verdict.
      - Verified: A, B, C, and D are completed with notes; the verdict is combined and identifies policies
        and status/records as the sections with the open findings; architecture/fidelity and documentation
        otherwise pass.
- [x] E2. No Blocker or Major finding remains, or every exception is explicitly waived by the human owner
      with a recorded rationale.
      - Verified: no Blocker or Major finding is recorded (five Minor, two Note). The accepted residuals
        (dependent-rule instances, macOS gate, AOT publish diagnostics, Step 11 N‑1, nightly fuzz deferral)
        remain the already-accepted items named in the scope notes.
- [x] E3. Every Minor/Note finding has an owner and a follow-up (step, maintenance branch, ADR, or accepted
      risk), and the remediation table is filled in.
      - Verified: the Remediation status table assigns owner and follow-up for XC04-F1–F5 and XC04-N1/N2.
- [ ] E4. The human owner approves the verdict and the Phase C gate; the Step 13 design review does not start
      before that approval.
      - Not ticked: owner approval is pending by design; this reviewer does not approve the gate.

## Findings

| ID | Severity | Area | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- |
| XC04-F1 | Minor | Status dashboard | The Step 08 dashboard row still has `step-08` in the `Current feature` column, although the step is `Done` and every other completed row uses `—`. A reader can misread it as an active feature. | `docs/planning/status.md:58`; adjacent rows 00–07/09–12 use `—`; tags/step page say Done. | Set the cell to `—` on the next documentation maintenance commit. Owner: orchestrator/step agent. |
| XC04-F2 | Minor | Step page records | `step-08-model-compilation-pipeline.md` leaves all eight implementation-task boxes and all acceptance criteria unticked although the step is `Done`, merged, tagged, and audited. The step is the only Phase C page in this state. | `docs/planning/steps/step-08-model-compilation-pipeline.md:785-840`; the step-09, step-10, and step-12 pages are fully ticked, and `step-11:1143-1148` explicitly annotates its one open box. | Tick the boxes (or mark the section historical with a pointer to the audit evidence) on a docs-only maintenance commit. Owner: step agent/planner. |
| XC04-F3 | Minor | Parity matrix and follow-up ownership | Three Phase C parity rows are stale: `Reverse rule generation (2/3-site)` and `No reverse for complex mechanisms` are `Planned` under Step `08` although both are implemented and tested, and `Dependent/hidden rules` is `Planned` under Step `08` although Step 08 is complete and the accepted audit m1 gap has no future owning step. `§5`'s `Forward/inverse/dependent rules` row is `Partial` with the same missing owner. | `docs/planning/appendix-b-parity-matrix.md:71-73,109`; `ExplicitReverseCompilationTests`, `TransitionCompilationStage`; `audits/step-08.md:275,336` (m1 “Deferred”); no Phase D row names dependent rules. | Set the two reverse rows to `Done` with their tests; assign the dependent-instance item to a future step (or record it as an explicit accepted deferral with owner) and correct the `§5` row. Owner: planner/Step 16 design. |
| XC04-F4 | Minor | Contracts manifest metadata | `contracts/manifest.json` has top-level `"status": "step-12"` but its `notes` narrate only Steps 06, 07, 10, and 12. Step 09 (parameterization signature finalized) and Step 11 (`.msl` payload/schema) are not narrated, so the Phase C additions have a silent gap in the human-readable convention; this is the Phase B XC01-F6 issue recurring. Generated artifacts are current and all projection checks pass. | `contracts/manifest.json:5-6`; `git log -- contracts/manifest.json` shows Step 09 (`9a11a38`, `20c73bc`) and Step 11 (`eb8513d`, `9b754af`, `bdafaa4`) changes; `generate.py --check` OK (26 files). | Either define the field as “last project/report schema-changing step” in `tools/contracts/README.md` and add a Step 11 note, or add the missing notes. Owner: contracts/planner. |
| XC04-F5 | Minor | Closeout evidence | `docs/planning/closeout/step-11.json` has `"tag": ""` although the annotated `step-11` tag exists on `84ceb76`, and `step-12.json:22` retains “No tag exists and no merge into main has been performed” after a later note records the merge and tag. The checker passes because it does not validate `tag` or note text. | `docs/planning/closeout/step-11.json:15`; `closeout/README.md` (“`tag`: The step tag, when it exists”); `closeout/step-12.json:22` vs `dcbf499` note; `check_closeout_evidence.py` required fields exclude `tag`. | Set `"tag": "step-11"` and replace the stale Step 12 sentence with the post-merge state; optionally extend the checker to warn when `tag` is empty for a merged step. Owner: step agent. |
| XC04-N1 | Note | Policy follow-ups | Four Phase B cross-cutting follow-ups remain open and were not closed by Phase C: XC01-F4 (no `Approval summary` section in the cross-cutting template/records, including this one), XC01-F7 (workflow actions still pinned to mutable major tags while `agent-security.md` asks for SHA pins where supported), XC01-F8 (no Dependabot/named alert owner), and XC01-F9 (local `verify.sh contracts` runs process consistency/tool tests that the CI contracts job does not, and `contributing.md`'s CI table omits catalog drift). | `xc-01-policies-and-planning.md:265-270,311-319`; `.github/workflows/*.yml` `uses:` lines; `docs/development/agent-security.md:53`; `docs/development/contributing.md:100-109`. | Keep as owner-accepted follow-ups or close them with the named owners; no Phase C regression, but they should not disappear from the approval summary. |
| XC04-N2 | Note | Platform verification | The Windows docs path (`build-docs.ps1`) and the macOS gate were not executed; the PowerShell edit mirrors the bash input check line for line, and macOS absence is the accepted Step 12 N‑5 risk. This is a platform gap, not a passing result. | `tools/docs/build-docs.ps1:18-42`; `drift-checks/2026-11-18-phase-c.md` “Notes for XC-04”; `audits/step-12.md` N‑5. | Run the PowerShell path on a Windows run and record macOS when platform behaviour changes; owner: documentation/CI owner. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. A missing intended correction, a lost original
strength, or an unrecorded regression is at least a Major finding.

No Blocker or Major finding is recorded. The dependent-rule *instances* are an accepted Step 08 audit item (m1)
and are not re-opened here; this review only records that its parity status/ownership is not durable (XC04-F3),
consistent with the non-goal that forbids re-auditing accepted notes.

## Scope notes

Context for the reviewer, not findings and not a verdict. Each note names something already visible in the
durable record at the frozen commit; the reviewer decides whether it is acceptable, a finding, or out of
scope. This list is not exhaustive and must not limit the review.

- **S-1 — Phase C status text (remediated before the freeze).** At the previous freeze `dcbf499`, `AGENTS.md`
  §1, `README.md`, `docs/index.md`, and `docs/development/index.md` still described Phase B as complete and
  Step 08 as next. The checkpoint-hygiene branch fixed all four at `2127f0f`: `AGENTS.md` §1 now records Phase
  C complete (Steps 00–12, `step-08`–`step-12` tagged), `XC-04` in progress, and Step 13 next after the
  checkpoint gate; the three public pages say the same. Evidence: drift record
  [`2026-11-18-phase-c.md`](../../drift-checks/2026-11-18-phase-c.md) PC-01/PC-02, fixed on the maintenance
  branch. Residual: the reviewer rechecks the merged `main` text against the tags and dashboard.
- **S-2 — Phase C drift check (remediated before the freeze).** At `dcbf499` the drift-check table contained
  only the `2026-11-12` Phase B record. The Phase C drift check now exists at
  [`2026-11-18-phase-c.md`](../../drift-checks/2026-11-18-phase-c.md), checked at `dcbf499` and recording two
  Major (PC-01, PC-02) and five Minor findings (PC-03–PC-07), all fixed on the maintenance branch before this
  freeze. Residual: the reviewer confirms the record is present, its findings are disposed of, and its
  "Notes for XC-04" are addressed; the drift record itself is not a gate and does not replace this review.
- **S-3 — Milestones M4 and M5 (remediated before the freeze).** At `dcbf499` only `milestone-m0`–`m3` were
  tagged. Annotated `milestone-m4` → `f91fc63` (Step 08 merge) and `milestone-m5` → `ec2e04a` (Step 12 merge)
  were created and pushed on 2026-11-18; the drift record PC-06 and the roadmap/status pages record the
  backfill. Residual: the reviewer verifies both tags are annotated, point at the correct merges, and did not
  move a step tag.
- **S-4 — Step 08 accepted residuals.** The audit records accepted items including m1 (equivalent-start orbit
  reduction/dependent rules unimplemented; the parity matrix keeps "Dependent/hidden rules" at `Planned` and
  "Forward/inverse/dependent rules" at `Partial`), R3-1 (descriptor-binding/cluster-fixture coverage), R3-2
  (no mixed KMC+MMC tracker-scope test), R3-3 (10k evidence narrower than the criterion and a live-heap delta
  rather than peak managed memory), R3-7 (pairs/clusters not cached), and R4-1/R4-2 (combined pair+cluster
  limit check after materialization; no chiral 3-site brute-force oracle).
- **S-5 — Step 11 accepted residuals.** Step 11 N-1 (directory fsync precedes the atomic rename) is recorded as a
  future contract/ADR correction; the container image was not built and that risk was accepted; nightly
  `.msl` fuzz is deferred to Step 29.
- **S-6 — Step 12 AOT and platform scope.** The real `Mocassin.Cli` Native AOT publish emits 62 first-party
  `Mocassin.Core` trim diagnostics and is recorded as evidence, not a gate; only `linux-x64` AOT is
  exercised; the macOS platform gate was not run (N-5, accepted risk); Step 31 owns the AOT/single-file
  decision.
- **S-7 — Documentation gate self-build (remediated before the freeze).** At the previous freeze the docs gate
  failed on a tree without a Release build because DocFX could not resolve the dependency assemblies (`XC-03`
  DOC-08). At `2127f0f`, `tools/verify.sh docs` builds the solution in Release when the DocFX inputs are
  absent, `verify.sh all` reuses the .NET stage instead of building twice, and `tools/docs/build-docs.sh` /
  `.ps1` build when the inputs are missing (drift record PC-07). Residual: the drift record notes that the
  PowerShell path was updated for parity but not executed on Linux; the reviewer verifies it on a Windows run
  or records the platform gap. No manual pre-build is required for the bash docs gate.
- **S-8 — Process deviations around orchestration.** The two Step 12 deviations are recorded in the step page
  (planner status edit before a step agent existed; direct owner question during feature 1);
  `docs/development/orchestration.md` §8 also records the earlier Step 10 feature-1 squash by the
  orchestrating agent before the role existed. The OpenCode 2.3.0 cold-start spawn race is documented in
  `orchestration.md` §9 with a mitigation and removal condition.
- **S-9 — Open XC-01 follow-ups touch Phase C gates.** XC01-F4 (cross-cutting template/records have no
  `Approval summary` section), XC01-F7 (workflow actions pinned to mutable tags), XC01-F8 (no
  `.github/dependabot.yml`/named alert owner), and XC01-F9 (local `verify.sh contracts` vs CI contracts job
  split) were accepted with follow-ups at the Phase B checkpoint and are not obviously closed at this
  commit. This record follows the existing template and has no `Approval summary` section.
- **S-10 — Closeout merge-candidate convention.** Steps 11 and 12 record `merge_candidate` equal to the
  audited code commit with a manually verified documentation-only tail (Step 11 N-3 convention); the checker
  does not validate that tail.
- **S-11 — Manifest status/notes convention.** `contracts/manifest.json` has `"status": "step-12"`, but its
  `notes` narrate Steps 06, 07, 10, and 12 only. The reviewer should check whether Phase C's 08/09/11 schema
  additions are covered by the notes convention or whether the field is "last schema-changing step" only.
- **S-12 — Step 12 report/command surface contract surface area.** Seven new command-report schemas, a
  `build.stage` event, and the `CLI001`–`CLI009` descriptors landed in one step. Check the manifest,
  generated schemas, CLI reference, diagnostics page, and tests agree; this is a large contract surface for
  one step even though every individual item has an owner.
- **S-13 — Step 10 performance deferral.** R2-1 records that the `< 10 s` / `< 1 GiB` generation targets are
  deferred to Step 29/release performance work; Step 10 claims only the 30 s expansion smoke bound. The Step
  12 10⁴-job run is separate evidence and passed its budget.

## Record state

**State:** Passed with notes — accepted by the human owner 2026-11-18

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> request owner or the other reviewers.

I was spawned by another session solely as the independent XC-04 reviewer. I did not design, implement, audit,
review, or coordinate any Phase C work, and I received no summary or preferred verdict: I read the repository at
`2127f0f` and ran the gates myself. My only repository edit is this record file.

## Verdict

- [ ] Passed
- [x] Passed with notes
- [ ] Fixes required
- [ ] Waived

Combined result: the policies/planning, architecture/original-design-fidelity, and documentation sections all
pass on substance. Policies and records drive the verdict: five Minor findings are status/record hygiene
(dashboard cell, Step 08 page boxes, stale parity rows/owner, manifest notes, closeout `tag`/note text), plus
two carried-over Notes (XC-01 follow-ups; platform gaps). No Blocker or Major finding remains, no intended
correction is missing, no original strength is silently lost, and no regression is unrecorded. Phase D may start
after the human owner accepts this verdict and the Phase C gate.

Follow-up owner: planner/step agent (XC04-F1–F5); security/CI owner (XC04-N1); documentation/CI owner (XC04-N2)
Follow-up step: Phase C maintenance/branch docs fix before or alongside the Step 13 design work; XC04-F3 also
feeds the Step 16 design

## Remediation status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| XC04-F1 | Fixed at `73bb608` — Step 08 `Current feature` set to `—` | Orchestrator/step agent | `73bb6083b27bfb1af2d0460fc464b64dee31f89e` |
| XC04-F2 | Fixed at `73bb608` — all Step 08 implementation-task and acceptance boxes ticked with evidence; m1 residual annotated and owned | Step agent/planner | `73bb6083b27bfb1af2d0460fc464b64dee31f89e` |
| XC04-F3 | Fixed at `73bb608` — reverse rows `Done` with tests; dependent-rule residual reassigned from Step 16 (not charged with compiler dependent rules) to the Step 29 §9 release gate, which owns parity-matrix closure; `§5` row updated | Planner / Step 29 release gate | `73bb6083b27bfb1af2d0460fc464b64dee31f89e` |
| XC04-F4 | Fixed at `73bb608` — Step 09 and Step 11 notes added to `contracts/manifest.json`; 26 generated artifacts checked current | Contracts/planner | `73bb6083b27bfb1af2d0460fc464b64dee31f89e` |
| XC04-F5 | Fixed at `73bb608` — `step-11.tag = "step-11"`; `step-12` note records merge `ec2e04a` and tag `step-12`; checker passes | Step agent | `73bb6083b27bfb1af2d0460fc464b64dee31f89e` |
| XC04-N1 | Accepted follow-up (carried from XC-01) | Security/tooling owner / planner | — |
| XC04-N2 | Accepted platform gap | Documentation/CI owner | — |

## Human approval

- [ ] Accepted: Passed
- [x] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by: Sebastian Eisele (human owner)
Date: 2026-11-18

Owner acceptance: the human owner accepted the reviewer's `Passed with notes` verdict on 2026-11-18 without
changing the findings, evidence, or verdict text. **XC04-F1–F5** are accepted and assigned to a documentation
maintenance branch that must be merged before the Phase C gate opens to Step 13. **XC04-N1** and **XC04-N2**
are accepted notes with their existing owners (security/tooling owner and planner for N1; documentation/CI
owner for N2); no additional action is required for the phase gate.
