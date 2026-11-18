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
| Reviewer | — (fresh independent reviewer to be assigned by the orchestrator; no Phase C implementation, design, or review context) |
| Review date | — |

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

_To be completed by the reviewer: repository state, files and areas reviewed, commands run with results, and
the not-run items with reasons. The evidence must be enough for the human owner to spot-check the verdict._

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

- [ ] A1. `AGENTS.md` §1 current-state text, §3.14 phase-gate description, and §3.15 rule-home table match the
      repository and the process actually used for Phase C (combined review, frozen commit, fresh reviewer,
      one record commit).
- [ ] A2. `docs/planning/status.md` (dashboard rows 08–12, Active work, phase-checkpoint table, drift-check
      table) matches the tags, merge commits, CI runs, audit/review verdicts, and step pages at the frozen
      commit; the Phase C row and the XC-04 row do not claim an outcome.
- [ ] A3. Step pages 08–12 record the correct status, step branch, step CI, main merge, tag, design-review and
      audit verdicts with round/commit references, feature logs, acceptance boxes, and deviations; no step
      page claims a feature that has no code/test behind it.
- [ ] A4. Design reviews and audits for 08–12 (and Step 11 amendment 1) are append-only: every round is
      present, the round table and finding/resolution tables are complete, reviewer text was not rewritten,
      and the human-approval sections are signed wherever a verdict is recorded as accepted.
- [ ] A5. Closeout evidence for 08–12 passes `check_closeout_evidence.py` and is consistent with the step
      pages and audit records: audited commit, closeout CI run and `head_sha`, `main` SHA at freeze, merge
      candidate, platform CI, tag, and notes all agree.
- [ ] A6. Phase C Git history follows the branch model: one step branch per step, feature work squash-merged
      into the step branch, step merged with `--no-ff`, annotated tag on `main` only after green main CI, and
      step/feature branches deleted; no per-feature CI run is claimed.
- [ ] A7. The two Step 12 process deviations are recorded with date, context, owner, rationale, and
      no-action/closure: the planner's status-cell edit before a step agent existed (§3.8 write scope), and the
      direct session question to the owner during feature 1 instead of routing through the orchestrator
      (`AGENTS.md` §3.8/§7). Check whether the records overstate, understate, or omit any related action.
- [ ] A8. The `OpenCode 2.3.0 cold-start spawn race` note in `docs/development/orchestration.md` §9 is
      version-scoped and states symptoms, durable log evidence, the branch-tip/previous-context inspection
      rule, the mitigation, and the removal condition; the role definitions in `.opencode/agents/` and
      `opencode.json` match the orchestration page's binding table.
- [ ] A9. `docs/development/orchestration.md`, `agent-workflow.md`, `contributing.md`, `CONTRIBUTING.md`, and
      the review/audit/amendment READMEs agree with `AGENTS.md` §3.8–§3.12 on role scopes, reviewer
      independence, checklist ownership, and the approval gate, and with the record shapes actually produced
      in Phase C.
- [ ] A10. CI policy and cost: `.github/workflows/*.yml` match `ci-policy.md` §2 and §3.13 (automatic
      `main`-only Ubuntu, one closeout dispatch per step, manual Windows/macOS/docs/benchmark/nightly, path
      filters, `[skip ci]` usage), and the Phase C run record shows no per-feature or per-push step-branch
      run.
- [ ] A11. Code-structure policy is enforced and current: `tools/structure-policy.json`,
      `Mocassin.Architecture.Tests`, `docs/development/code-structure.md`, and `AGENTS.md` §8 agree on folder
      depth, file counts, forbidden folders, file-name suffixes, and namespaces; Phase C projects comply and
      `verify.sh structure` passes.
- [ ] A12. Local gate parity: `tools/verify.sh` modes (`all`, `docs`, `policy`, `contracts`, `aot`) cover the
      Phase C CI jobs and the commands documented in `AGENTS.md` §6 and `contributing.md`; the required `aot`
      gate is correctly described as local and as part of `all` from Step 12.
- [ ] A13. ADRs 0009–0015 are `Accepted` with current status/date/deciders and no contradiction with the
      implemented contracts; `05-open-decisions.md` marks every resolved decision as resolved and keeps only
      genuinely gated/deferred items; `04-roadmap.md` records the Step 09 and Step 12 sizing exceptions and
      the Step 12 parser change consistently with the step pages.
- [ ] A14. `appendix-b-parity-matrix.md` Phase C rows (`§5 Compilation pipeline`, `§6 Simulations and
      templates`, `§10 Quality`, `§11 original defects`) match implemented code/tests: no row marked `Done`
      lacks evidence, and every `Partial`/`Planned`/deferred row names its owning step or accepted deferral.
- [ ] A15. Milestone and tag policy is applied: the roadmap's M4/M5 milestones and their tag status are
      consistent with §3.7 and the roadmap's "tagged when reached" rule, or the deferral is recorded with an
      owner.
- [ ] A16. The Phase C boundary has the required drift check (or a recorded waiver/deferral) and its findings
      are disposed of in a durable record; the phase gate does not rest only on step-level audits.
- [ ] A17. Every process claim needed for the verdict is checkable from durable files alone; no part of the
      phase's evidence depends on chat history or on the orchestrator's summary.

### B. Architecture and design status

- [ ] B1. The dependency graph for Phase C matches `AGENTS.md` §5 and `01-target-architecture.md` §3: `Core`
      ← `Symmetry` ← `Compilation` ← `Contracts` ← `Packages` ← `Cli`, `Results` depends on `Contracts` (not
      `Packages`), `Packages.Build` owns orchestration, and the CLI references only the projects it uses.
      Check the actual `*.csproj` references, not only the diagram.
- [ ] B2. Purity and I/O boundaries hold: `Core` and `Compilation` have no file I/O, SQLite, UI, or solver
      dependency; `.mprj` persistence is in `Packages`; package writing is in `Packages`; package
      reading/inspection/verification is in `Results`; the Rust solver boundary is untouched.
- [ ] B3. `contracts/manifest.json` is the single machine-readable source for Phase C formats/reports:
      generated schemas, constants, and projection vectors are current (`generate.py --check`, model/
      parameterization/lattice projection checks), and the manifest `status`/`notes` convention accounts for
      the Phase C schema additions without silent gaps.
- [ ] B4. Interface 08 → 09: the compiled model's dense indices, `EnergyId`/`GeometryId`/rule identities,
      geometry/rules signature, tracker definitions, and site-energy boundary match the Step 09 consumer; the
      parameterization signature covers model + applied values and excludes runtime settings; `Compilation`
      remains parameterization-free.
- [ ] B5. Interface 09 → 10: template identities and sparse defaults, canonical JSON, `PRM` structural vs
      compiled-bound validation, and the `Apply`/`Validate` contract feed job expansion and build-stage
      validation without a second validation or apply implementation.
- [ ] B6. Interface 10 → 11: job expansion and selection normal form, per-job/lattice seed derivation
      (ADR 0005 tuple order, SHA-256 endianness), doping truncation/compensation results, deterministic
      lattice generation, and the job payload fields match the writer and the data contract.
- [ ] B7. Interface 11 → 12: the writer's publication barrier and immutability, the single `Results` reader
      reused by `inspect`/`verify`, the `PKG` descriptors, `build-v1` and the other command reports, the exit
      code map, and reserved commands match `02-data-contracts.md` §3 and §5 and the CLI implementation.
- [ ] B8. End-to-end 08 → 12: one build pipeline (load → select → validate → compile → build-stage validate →
      parameterize → expand → generate → publish → verify) is used by the CLI and is the same library the
      future GUI consumes; diagnostic ownership (`CMP`, `PRM`, `JOB`, `PKG`, `CLI`) is unambiguous and
      non-duplicated.
- [ ] B9. Determinism and identity: canonical projections, quantized signatures, SHA-256 content IDs,
      content-addressed lattices, and stable ordering are implemented and tested; the compiled,
      parameterization, lattice, and package-summary signatures are distinct and documented; no
      hash-iteration, culture, clock, or path dependence can change a published byte or signable value.
- [ ] B10. Immutability, atomicity, and failure behaviour: `.msl` is immutable after publication, requires no
      WAL sidecars, is written through the documented atomic path, and a failed or cancelled build never
      leaves a valid-looking package; the post-publication verification result and exit mapping are
      consistent.
- [ ] B11. Phase C input boundaries are treated as untrusted where the design says so: bounded counts and
      allocations, index/NaN/chirality/duplicate validation, package parsing and payload limits, response-file
      limits, and path handling; the corresponding tests exist or the gap is an explicitly accepted item.
- [ ] B12. Design status is honest: no Phase C design decision was implemented differently from its frozen
      page without a recorded deviation, amendment, or ADR; `05-open-decisions.md` and the parity matrix match
      the code; no Phase D/E/F feature is silently implemented or claimed as complete.
- [ ] B13. Phase C performance and memory evidence is reproducible and correctly tiered: compile/cache,
      package write/verify, and the 10⁴-job workload have recorded commands and results; deferred targets name
      an owner and a later step; no benchmark is used to claim a target it did not measure.

### C. Original-design fidelity

Required for the combined review. Answer all three questions explicitly and cite concrete evidence for every
row. Compare against the read-only original at `/workspace/mocassin-original`; do not copy its code.

- [ ] C1. **Intended corrections.** Build a table of every Phase C correction the plan intended (at minimum
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
- [ ] C2. **Ported strengths.** Build a table of the original's good/clever aspects that Phase C intended to
      keep, and state **kept / adapted / dropped** with the reason and evidence. Cover at least: auto-sampled
      pair interactions with a shared chiral energy table; canonical P1 ordering and dense compiled indices;
      pair/cluster shell reduction and 6-bit occupation codes; the per-environment energy cache with
      path-local `S2` deltas; static `S2` corrections; selection pools and selection optimizers; pre-run
      normalization and the KMC time-step formula; CSR neighbour/jump links; incremental lattice-energy
      updates; and the original model-builder workflow ideas (build recipe, preview, stage/result reporting,
      log-console diagnostics). Distinguish "implemented and verified" from "design only for a later phase";
      a design-only strength must name its owning step and must not be claimed as ported.
- [ ] C3. **Regressions.** Compare Phase C capability, semantics, performance, memory, diagnostics,
      determinism, and maintainability against the original with concrete evidence. Check in particular:
      rule generation coverage (reverse/dependent/complex), energy table identity and chiral sharing,
      supercell/doping semantics, `.msl` content and queryability, build workflow ergonomics, error
      reporting, and determinism. A deliberate regression is acceptable only with a recorded rationale,
      owner, and follow-up; an unrecorded regression is a finding.
- [ ] C4. **Evidence quality.** Every answer above cites a file:line, test name/run, golden fixture,
      benchmark result, or a side-by-side table; prose assertions, planning-page claims, or audit text alone
      do not satisfy the requirement.
- [ ] C5. **No silent loss or silent addition.** The record states explicitly whether any intended Phase C
      correction is missing, any intended strength is absent, or any regression was introduced without a
      record, and names the exact item. If none, say so explicitly.

### D. Documentation status

- [ ] D1. Coverage: every implemented Phase C surface has a published or developer page — CLI commands,
      options, exit codes and reports; `.mocprj` build templates and parameterization; job templates;
      `.msl` format and inspection; compilation pipeline; diagnostics; building workflow; performance; and
      troubleshooting. Name any implemented surface with no page.
- [ ] D2. CLI accuracy: `docs/reference/cli.md` matches the implemented command graph, option names and
      semantics (`--template` vs `--job-template`, `--jobs`, `--parameterization`, `--force`, `--parallel`,
      `--compact`, `--warnings-as-errors`, `--quiet`, `--json`), response-file syntax/limits, help/version
      behaviour, reserved commands, exit codes, and report schemas.
- [ ] D3. Format accuracy: `docs/reference/msl-format.md`, `project-format.md`, and the data-contract pages
      match the Step 11 writer/inspector and the Step 12 reports — tables, payloads, signatures, publication
      barrier, read-only guarantees, and the canonical package summary.
- [ ] D4. Pipeline accuracy: `docs/development/compilation-pipeline.md`, `architecture.md`,
      `design-decisions.md`, and `testing-and-parity.md` match the implemented assemblies, stage order,
      ownership boundaries, and test tiers; no page describes a second pipeline, a stale type name, or an
      abandoned design.
- [ ] D5. Navigation: `zensical.toml` lists every published Phase C page and generated API target, has no
      dangling nav entry, and the strict build passes; new pages are reachable from the section indexes.
- [ ] D6. API docs: DocFX builds with no missing public-item documentation for the new Phase C public types;
      rustdoc remains clean for the untouched workspace; the Python API placeholder is still explicitly
      owned by Step 22 and not presented as delivered.
- [ ] D7. Publication boundary: the built site contains no `planning/` directory, the search index and
      sitemap contain no planning records, and this cross-cutting record is not published.
- [ ] D8. Links and math: `check_docs_links.py` and `check_doc_math.py` pass with the recorded file counts;
      internal anchors resolve; published pages use `$$…$$`/`\(…\)` per `AGENTS.md` §9 and planning pages use
      `$…$`/`$$…$$`.
- [ ] D9. Examples and claims: documented sample commands run against `samples/`; JSON examples are
      schema-valid or explicitly illustrative; diagnostic codes/titles/severities on the diagnostics page
      match the composed catalog; performance numbers on the performance page match the recorded evidence
      and are not rounded into a stronger claim.
- [ ] D10. Status surfaces: `README.md`, `docs/index.md`, `docs/development/index.md`, `docs/planning/README.md`,
      and the step pages describe Phase C completion and Step 13 next consistently with `status.md`; no page
      still advertises an earlier phase as current or a completed command as future work.
- [ ] D11. No stale placeholder vocabulary on Phase C pages: no `TODO`, `FIXME`, "coming soon", "not
      implemented", or future-tense claim for behaviour that now exists, and no page promises behaviour that
      the code does not have.

### E. Exit criteria

- [ ] E1. All three subject sections (policies, architecture/fidelity, documentation) and their checklists
      have been reviewed and completed; the verdict states the combined result and, if the sections differ,
      which section drives the verdict.
- [ ] E2. No Blocker or Major finding remains, or every exception is explicitly waived by the human owner
      with a recorded rationale.
- [ ] E3. Every Minor/Note finding has an owner and a follow-up (step, maintenance branch, ADR, or accepted
      risk), and the remediation table is filled in.
- [ ] E4. The human owner approves the verdict and the Phase C gate; the Step 13 design review does not start
      before that approval.

## Findings

_To be completed by the reviewer. Use the table format and severity values below; do not pre-populate it._

| ID | Severity | Area | Finding | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. A missing intended correction, a lost original
strength, or an unrecorded regression is at least a Major finding.

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

**State:** Ready for review

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> request owner or the other reviewers.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Waived

Follow-up owner:
Follow-up step:

## Remediation status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

## Human approval

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:
