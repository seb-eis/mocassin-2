# Step 12 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-12.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 12 — Build Orchestration and `mocassin` CLI |
| Request owner | Planner (design freeze); the step agent takes ownership of resolutions at the start gate |
| Orchestrator | Orchestrator agent (spawns the planning auditor) |
| Step agent (implementor) | Not assigned — created from the reviewed commit after the start-gate approval |
| Review initiated by | Orchestrator, on request of the planner |
| Reviewed commit | `1b6af90c72a0c4ff77d50fd266e80738bd8490fb` |
| Docs branch | `docs/step-12-design` |
| Review date | 2026-11-16 |
| Reviewer | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh context, no shared conversation |

Step page: [`Step 12 — Build Orchestration and mocassin CLI`](../steps/step-12-build-orchestration-and-cli.md)

**Design change since round 2 (2026-11-17).** The human owner replaced the round-2 hand-written-parser
decision with `System.CommandLine` 3.0.0 after an AOT/full-trim verification showed the stated AOT/trimming
risk was wrong. On 2026-11-17 the owner also confirmed the revised 2.75–3.75 pw sizing, removed
`mocassin completion` from Step 12, confirmed the minimal AOT smoke host as feature 1's gate (the real CLI
publish is evidence, not a gate), added bounded `@file` response files as a Step 12 contract, and requested
[ADR 0015](../adr/0015-cli-parser-system-commandline.md). Round 3 returned `Ready with notes` at `d211584`
(reviewed `6b15f9d`); the step agent resolved R3-1–R3-8 and the two open round-2 Notes (R2-2, R2-4). Round 4
returned `Ready with notes` at `f548d26` (reviewed `d6c5402`); the step agent resolved R4-1–R4-7 in the new
freeze `1b6af90c72a0c4ff77d50fd266e80738bd8490fb`. The owner also confirmed the AOT policy: the gate is
required, clang/lld/ILCompiler are Tier 1, and `verify.sh all --require` is a local closeout gate because the
closeout CI does not run `verify.sh`. The freeze rewrites §5.2, adds §5.3 (CLI-owned response-file tokenization)
and §5.4 (required AOT gate), rewrites feature 1, the command surface, layout, tests, risks, and acceptance
criteria; maps library parse errors (default exit 1) to `CLI001`/2; specifies root-only `--version`; and adds
the linux-x64 `verify.sh aot` gate. `validation-v1`, the exit-code table,
`--json`/JSON-Lines, the seven reports, `--jobs`/`job_selection`, atomic `--force`, and cancellation are
unchanged. Rounds 1–4, their findings, and the R2-1–R2-4 notes are unchanged below; the `Reviewed commit`
cell and this note point the request at the new freeze. Round 5 should review the new freeze, not re-open the
accepted features 2–7 unless the parser/response-file/AOT change touched them.

> Orchestration: the orchestrator spawns a fresh planning auditor after the planner has frozen the design and
> completed this request, and relays the verdict and owner decisions. The planner and step agent update the
> design docs, apply fixes on the design docs or a maintenance branch, add resolutions without altering reviewer
> text, and ask the orchestrator for re-review until the auditor no longer identifies issues. The orchestrator
> does not edit the design or the review record. The human owner approves the final verdict and the start of
> implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 10, 11 (both `Done`, merged and tagged) |
| Delivers | A headless `mocassin` CLI and the shared `Mocassin.Packages.Build` orchestration: create/validate/compile/build/preview/inspect/verify/doctor/clean-dry-run, build-template and parameterization selection, partial `--jobs` builds with `job_selection` metadata, atomic replacement, progress/cancellation, bounded `@file` response files, JSON reports and JSON-Lines events, `CLI` diagnostics, and the `build-v1` report that supersedes `build-preview-v1` |
| Sizing | 2.75–3.75 person-weeks (owner-approved 2026-11-17; was 3.0–4.0), seven features; roadmap §3.1 says 1–3 pw. The request presents the delta, the approval, and a 2.25 pw contingency, and records the no-split decision |

## Design decisions reviewed

- [x] Step page design decisions — goal, scope, non-goals, the pipeline and event stream, selection,
      job selection, output/atomicity, progress, errors/exit codes, JSON contracts, commands, cancellation,
      dependency direction, layout, contract artifacts, tests, documentation, and "Done when". — Reviewed;
      the page is detailed and largely internally consistent; M-1 and m-1–m-9 record the gaps.
- [x] Data contracts and format changes — `02-data-contracts.md` §3.2 (`job_selection` normal form), §5.1
      (command surface), §5.5 (`build.stage` and optional build-event fields), §5.7 (the seven report
      descriptors, `build-v1`, exit codes), and §6 (fixtures); the `reports`/`events` manifest additions and
      generated artifacts. — Reviewed; the additions are additive under `manifestVersion = 1` and match the
      generator's field/event shapes; m-2, m-5, m-6, n-2, n-6.
- [x] Architecture and module boundaries — `01-target-architecture.md` §3/§3.1 (the `Mocassin.Packages.Build`
      home, the explicit `Cli -> Compilation` read-only edge, no `Packages -> Results` edge) and §7.3 (the
      corrected CLI exit codes). — Reviewed; the project graph and the one-reader rule are consistent;
      `Mocassin.Packages.csproj` has no `Results` edge and `Mocassin.Cli.csproj` already references `Results`.
- [x] ADRs and open decisions — ADR 0009 (manifest and generated checks), ADR 0010 (signatures; no signature
      change), ADR 0011 (immutable package), and the new resolved/gated rows in `05-open-decisions.md`.
      — Reviewed; only the `--template` start-gate row is registered; m-7 (sizing) and n-7 (baseline wording).
- [x] Quality strategy and test plan — `03-quality-strategy.md` §5, §6, §8, §9.10, and §13; the Step 12 test
      table with tiers and the real-file/real-SQLite policy. — Reviewed; the tier split is sound; m-9 records
      the missing `doctor` injection seams.
- [x] Parity matrix and documentation plan — the Step 12 rows and the pages in `docs/reference`, `docs/user-guide`,
      `docs/getting-started`, `docs/troubleshooting`, `docs/development`, and this planning directory.
      — Reviewed; the parity transitions are stated as intended and do not mark unimplemented rows `Done`; n-3,
      n-4, n-9.
- [x] Licensing and provenance — `System.CommandLine` 3.0.0 is a new CLI-only MIT dependency with an empty
      `net10.0` transitive graph and a linux-x64 AOT smoke check; `JsonSchema.Net` is only added to an existing
      test project; the embedded ceria template is MIT-authored project data and is freshness-checked.
      — Reviewed; `JsonSchema.Net` 9.6.0 is centrally versioned, MIT-compatible, and already accepted by the
      licence policy; the embedded templates are repository-authored JSON.
- [x] Post-implementation audit plan — implementation-status block, feature log, closeout evidence, and the
      audit request sequence. — Reviewed; the standard Step 11/12 sequence is present and the closeout is a
      dedicated feature.

### Step-specific decisions

- [x] **Parser.** `System.CommandLine` 3.0.0 (MIT, pinned, CLI-only; ADR 0015); typed command graph and
      `ParseResult` binding, parse errors mapped to `CLI001`/2, help/version 0, no `completion` command, AOT
      smoke check. Verify the library's tokenization (arity, `--opt=value`, negative values, `--`) and the
      usage-error reports. — Reviewed; the command table and grammar are defined; n-8 records the ambiguous
      `-`-value rule.
- [x] **Response files.** `@file` is part of the Step 12 contract: documented syntax (UTF-8, double-quote
      grouping, `#` comments only at line start, recursion, cwd-relative paths), CLI-owned limits
      (1 MiB/4 MiB raw-byte/depth 8/10,000 tokens/32 files), `--`/`--json` interaction, and `CLI001`/2 for
      missing/unreadable/invalid/oversized files. Feature 1 implements and tests it; ADR 0015 records it.
      Verify the step page §5.3, data contracts §5.1, `cli.md`, tests, and acceptance agree. — Reviewed in
      round 3; see R3-1, R3-3, R3-6, and R3-7.
- [x] **Build orchestration home.** `Mocassin.Packages.Build`, consumed by CLI and GUI; `Mocassin.Packages`
      does not reference `Mocassin.Results`; post-publication verification is caller-composed. Verify no
      project-graph contradiction. — Reviewed; the graph is acyclic and the verification split is sound.
- [x] **Pipeline order.** Load → select → validate stages 1–4 → compile → build-stage validation with the
      compiled model → parameterize → expand → generate/write → publish; the pre-compile validation gives
      model errors exit 1, and the post-compile call is required by `JobTemplateValidationStage`.
      — Reviewed; m-3 (flag plumbing), m-4 (named arguments/token), m-8 (stage registration).
- [x] **Event stream.** The in-process `BuildEvent` types, the bounded channel/backpressure, progress
      throttling, `build.started`/`build.stage`/`build.progress`/`build.finished` mapping, and the
      guarantee that `build.finished` is emitted once. — Reviewed; n-1 records the `build.started` ordering
      surprise.
- [x] **Selection.** `--template` = build template, `--job-template` = job template, `--parameterization`
      override; auto-selection, ambiguity, wrong-kind IDs, unknown IDs, implicit default, and output-name
      resolution. — Reviewed; the three documents agree and the wrong-kind/unknown-ID split is defined.
- [x] **Job selection.** Grammar, canonical normalization, full-expansion IDs, gaps, `all`, `inspect`
      visibility, and the writer’s existing `job_selection` metadata. — Reviewed; matches §3.2 and
      `PackageBuildRequest.JobSelection`; partial packages verify because the verifier requires ascending,
      unique, not contiguous IDs.
- [x] **Output and atomicity.** `--force`, the writer’s temp/rename barrier, pre-publication failures,
      post-publication cancellation, `--compact` = quality 9 + VACUUM, and the `compile -o` replacement rule.
      — Reviewed; matches the Step 11 writer and the accepted Step 11 audit N-1 ordering.
- [x] **Progress/quiet.** Human progress on stderr, final summary on stdout, JSONL on stderr, ETA from a
      rolling window, and `--quiet` suppression without hiding diagnostics. — Reviewed; m-6 records the
      `--json`/human-stderr ambiguity.
- [x] **Error model.** The 0/1/2/3/4/5/6/10 table, exit 5 being reserved, the `--warnings-as-errors` scope,
      and the `CLI001`–`CLI009` descriptors in Contracts. — Reviewed; m-2 (uncovered failure rows) and m-3.
- [x] **Build-result identity and failure mapping.** `BuildIdentity` supplies the report’s project/model/
      document hashes and signatures; `Io` is separate from `Load`; the documented table maps every
      `BuildResult` to an exit code; cancellation is a library status with `CLI006` composed by the CLI; and
      `build-v1` carries no package-size estimate. Verify the report fields and exit codes agree.
      — Reviewed; identity fields and the no-estimate rule agree; m-2 shows the table does not cover every
      `BuildResult`/diagnostic.
- [x] **JSON reports.** The seven `reports` descriptors, the shared envelope, `build-v1` fields, the
      replacement of `build-preview-v1`, the usage-error nullability rule, and the one-object/one-exit-code
      guarantee. — Reviewed; m-5 (reserved commands) and n-2.
- [x] **Commands.** `new` (embedded templates, fresh identity, safe create), `compile` (`-o` inspection,
      `compile-v1`), `inspect`/`verify` wrappers, `doctor` checks and solver warning, `clean --dry-run`
      scope, and reserved commands; no `completion` command in Step 12. — Reviewed; m-5, m-9, n-3, n-4.
- [x] **Cancellation.** Pre-publication temp cleanup, existing-package preservation, post-publication
      behaviour, signal handling, and the second-signal rule. — Reviewed; consistent with the Step 11 writer;
      m-4 records that validation does not receive the token.
- [x] **Layout.** `Mocassin.Packages/Build`, the CLI command-domain folders, `Mocassin.Contracts/Cli`, test
      folder mirrors, and the 20-file/800-line/depth limits. — Reviewed; the proposed folders satisfy
      `tools/structure-policy.json`; n-9 records the missing template factory/templates public API.
- [x] **Contract artifacts.** The `reports`/`events` manifest additions, `REPORT_CONSTANTS`, generated
      schemas/constants/descriptors, `events-v1.jsonl`, and the `verify.sh contracts` freshness check.
      — Reviewed; `generate.py` supports additive report/event descriptors and pins `manifestVersion = 1`.
- [x] **Sizing and feature list.** Seven features, 2.75–3.75 pw, the 2.25 pw contingency, and the owner
      decisions in §21. — Reviewed; m-7 records the missing decision-register row and the stale roadmap row.
      Round 3 re-checked the current design at 2.75–3.75 pw; the item text records the round-1 figure (R3-5).

## Dependency and interface checks

- [x] Dependencies 10 and 11 are `Done` and their shipped contracts are stable — `JobTemplate`/`JobDescriptor`,
      `JobTemplateExpander`, `SupercellGenerator`, `GeneratedLattice`, `JobPackage`,
      `ISimulationPackageWriter`, `PackageBuildRequest`/`Options`/`WriteResult`,
      `IMslPackageInspector`, `IPackageVerifier`, `DiagnosticCatalog`, `ProjectValidationPipeline`,
      `IModelCompiler`, and `IParameterizationService`. — Verified in `src/dotnet`; `step-10` and `step-11` are
      tagged and merged; the named types and members exist as used.
- [x] Inputs, outputs, types, and formats are defined — the pipeline request/result/event types, the
      `build-v1`/`compile-v1`/`inspect-v1`/`verify-v1`/`doctor-v1`/`clean-v1`/`new-project-v1` payloads, the
      `job_selection` normal form, and the JSON-Lines event fields. — Mostly defined; M-1 leaves the
      `build-v1.preview` producer open and m-1 leaves `BuildOptions` unreachable.
- [x] Versioning and migration behavior is defined — report `schemaVersion = 1`, additive event changes,
      `build-preview-v1` retirement, no package/payload/signature change, and the Step 08/09/11 version
      domains. — Reviewed; additive under `manifestVersion = 1`, no `.msl`/signature change.
- [x] No unresolved cross-step interface question remains — Step 13’s reader/`job_selection`, Step 16’s job
      flags, Step 19/20/21/28 reserved commands, Step 23’s creation UI, Step 26’s build UI, and Step 30’s
      report registration are either defined or explicitly deferred. — One remains: the preview payload path
      shared by the CLI and Step 26 (M-1); reserved-command reports are also open (m-5).
- [x] The CLI/GUI split is real — the GUI can call `Mocassin.Packages.Build` and the verifier without
      referencing `Mocassin.Cli`; the CLI owns only parsing, formatting, and exit-code policy. — Sound, with
      the preview exception (M-1).
- [x] The diagnostics composition is acyclic — `CLI` descriptors live in Contracts, `Packages` composes them,
      the CLI constructs them, and `Results` keeps using the shared descriptors. — Verified; `Mocassin.Packages`
      references `Contracts`, and the docs-drift test's `CLI` regex accepts the three-letter prefix.
- [x] `Mocassin.Packages` can build without `Mocassin.Results` — the writer, generator, parameterizer,
      expander, and compiler are in its reference closure; verification is caller-composed. — Verified in
      `Mocassin.Packages.csproj`.
- [x] The command surface matches the closed-step contracts — `build-preview-v1`, `--template`, `--compact`,
      `clean`, and the Step 11 writer options are reconciled. — Reconciled; the `--template` change is the
      owner-gated §21 decision.

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable — reviewed; clear, with m-5/m-8
      affecting two testable guarantees.
- [x] Design decisions are complete and internally consistent — not satisfied; M-1 is a contradiction and
      m-1–m-9 are gaps.
- [x] Required tests, property tests, golden fixtures, and performance targets are defined — reviewed; the
      tiers and performance targets are present; m-9 (doctor injection seams).
- [x] Documentation, status, parity, and audit updates are planned — reviewed; the pages and parity
      transitions are named and mostly written; n-2–n-4, n-9.
- [x] Licensing, provenance, and dependency checks are resolved — reviewed; no new production dependency and
      the added test dependency is already licence-approved.
- [x] Risks and mitigations are realistic — mostly; the risk table omits the preview data-path risk (M-1) and
      the `--warnings-as-errors` plumbing risk (m-3).
- [x] Code layout and namespace plan follow the structure policy — reviewed; the folders, namespaces, file
      counts, and multi-type file names satisfy `tools/structure-policy.json`; n-9.
- [x] No hidden scope, stale placeholders, or unowned decisions — not fully; m-7 leaves the sizing decision
      unregistered and n-9 leaves a public factory outside the layout/public-API plan.

## Evidence

Files reviewed:

- Process and records: `AGENTS.md` §3, §8–§12; `docs/planning/reviews/README.md` (especially §4, §5, §8,
  §8.1); `docs/planning/audits/step-10.md` (accepted note R2-5) and `docs/planning/audits/step-11.md`
  (accepted notes N-1/N-4/N-6); `docs/planning/status.md`; `docs/planning/reviews/step-12.md`.
- Design delta `main..80b3f7fd99f0140b1dad5650851d1aaa3c4ac0b8`: the Step 12 page
  (`docs/planning/steps/step-12-build-orchestration-and-cli.md`, +1247/−215),
  `02-data-contracts.md` §3.2/§5.1/§5.5/§5.7/§6, `01-target-architecture.md` §3/§3.1/§7.3,
  `03-quality-strategy.md` §5/§6/§8/§9.10/§13, `04-roadmap.md` §3.1, `05-open-decisions.md`,
  `appendix-b-parity-matrix.md`, `docs/development/code-structure.md`, `docs/development/design-decisions.md`,
  `docs/reference/cli.md`, `docs/reference/diagnostics.md`, `docs/user-guide/building.md`,
  `docs/troubleshooting/errors.md`, `docs/getting-started/index.md`.
- Contract and tooling: `contracts/manifest.json` (`reports`, `formats.events`, `manifestVersion`);
  `tools/contracts/generate.py` (`REPORT_CONSTANTS`, `schema_for_format`, event schema,
  `manifestVersion == 1`); `tools/structure-policy.json`;
  `src/dotnet/Mocassin.Packages.Tests/Projects/Validation/DiagnosticDocumentationTests.cs`.
- Code and project graph inspected to test the design's interface claims:
  `Mocassin.Packages/Projects/Validation/{ProjectValidationPipeline.cs,ValidationOptions.cs,JobTemplateValidationStage.cs,DiagnosticCatalog.cs}`;
  `Mocassin.Packages/Projects/{ProjectStore.cs,ProjectStoreOptions.cs,ProjectStoreResults.cs,ProjectLoadFailureKind.cs}`;
  `Mocassin.Packages/Parameterization/{IParameterizationService.cs,ParameterizationOptions.cs}`;
  `Mocassin.Packages/Jobs/{JobTemplateExpander.cs,SupercellGenerator.cs,ILatticePreviewService.cs,LatticePreview.cs,JobDescriptor.cs}`;
  `Mocassin.Packages/SimulationPackage/{ISimulationPackageWriter.cs,SimulationPackageWriter.cs,PackageBuildOptions.cs,PackageBuildRequest.cs,PackageWriteResult.cs}`;
  `Mocassin.Compilation/Compiler/{IModelCompiler.cs,CompilationOptions.cs,CompilationResult.cs,CompilationReport.cs,CompilationCounts.cs}`;
  `Mocassin.Compilation/Signatures/CompiledModelJson.cs`;
  `Mocassin.Results/Packages/{IMslPackageInspector.cs,IPackageVerifier.cs,PackageInspection.cs}` and
  `Internal/PackageReader.cs` and `Internal/PackageVerifierEngine.cs` (ascending, non-contiguous job IDs;
  `job_selection` handling) and `Internal/PackageSummaryBuilder.cs` (no size in the canonical summary);
  `Mocassin.Cli/Build/{PreviewCommand.cs,PreviewReportDtos.cs}`; all four `*.csproj` files;
  `Directory.Packages.props`; `samples/blank/blank.mocprj`; `samples/ceria/ceria.mocprj`;
  `samples/jobs/ceria-temperature-sweep.mocprj` (`parameterizations: []`, `buildTemplates: []`, one job
  template).

Commands run:

- `git rev-parse HEAD` → `0d466e8d7f78b1da19e56130d570140105bc429f`; the reviewed design freeze is
  `80b3f7fd99f0140b1dad5650851d1aaa3c4ac0b8`.
- `git diff --name-status 80b3f7fd99f0140b1dad5650851d1aaa3c4ac0b8 0d466e8d7f78b1da19e56130d570140105bc429f`
  → `M docs/planning/reviews/step-12.md` only. The request tip adds no design change on top of the freeze.
- `git diff --stat main..80b3f7fd99f0140b1dad5650851d1aaa3c4ac0b8` → 14 files, +2394/−322; the step page
  is +1247/−215 and `02-data-contracts.md` +234/−20.
- `./tools/verify.sh docs --require` → exit 0. Documentation math OK (165 files); links OK (168 files);
  Zensical strict build `No issues found`; DocFX `Build succeeded` 0 warnings / 0 errors; rustdoc OK.
  (The first run, before any Release build, reported DocFX "No .NET API project detected"; the re-run after
  `policy` built the Release assemblies is the 0/0 result recorded here.)
- `./tools/verify.sh policy --require` → exit 0. ApiCompat clean for 9 assemblies; NuGet licence/vulnerability
  policy OK; contract artifacts current (19 files); model/parameterization/lattice projections OK (5/4/2
  fixtures); space-group catalog current (274 settings, sha256 `1f3bf67d…`); process consistency OK; 58 Python
  tool tests OK; SPDX headers in 603 source files; no secret patterns in 985 tracked text files; cargo-deny
  advisories/bans/licenses/sources OK.
- `python tools/check_doc_math.py` → `Documentation math conventions OK (165 files scanned).`
- Additional read-only inspections: parsed `contracts/manifest.json` and `tools/contracts/generate.py` to confirm
  the additive `reports`/`events` surface and the `manifestVersion` pin; counted the proposed folder/file
  layout against `tools/structure-policy.json`; checked the `samples/jobs` template shape; traced
  `ProjectValidationPipeline.RunBuildStages`, `PreviewCommand`, `PackageVerifierEngine`, and
  `PackageSummaryBuilder`. No repository file was modified apart from this record.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M-1 — The `build --preview` data path cannot be produced from the declared public surface, and the design contradicts itself about who evaluates the preview.** | Step page `:218-223`, `:286-295`, `:371-377`, `:673`, `:687`, `:960`; `02-data-contracts.md:1802`; `ILatticePreviewService.cs:36-40`; `PreviewCommand.cs:122-131`. |
| **Minor** | **m-1 — `BuildOptions` has no injection point.** The record exists but no method or constructor consumes it, so the documented test-tunable capacity and injected clock/interval cannot be set. | Step page `:241-246`, `:218-223`, `:323-327`, `:383-388`. |
| **Minor** | **m-2 — The failure-kind → exit-code table is incomplete and conflicts with the all-or-nothing sentence.** `Failure = PackageWrite` without an `IOException`/`UnauthorizedAccessException`, and a pipeline-side `CLI005` job-selection failure, have no row; the request's "maps every `BuildResult`" claim is overstated. | Step page `:552-568`; request `step-12.md:88-91`; `SimulationPackageWriter.cs` throws `SqliteException`/`InvalidOperationException` paths; `BuildRequest.JobSelection` `:232`. |
| **Minor** | **m-3 — `--warnings-as-errors` is described but not plumbed into the documented validation/parameterization calls.** `ValidationOptions.WarningsAsErrors` exists; the stage snippets omit it, and `ParameterizationOptions` has no equivalent flag. | Step page `:237`, `:336-343`, `:790-792`; `ValidationOptions.cs:23`; `ParameterizationOptions.cs`; `IParameterizationService.cs:56-65`. |
| **Minor** | **m-4 — The `Validate` calls are positional and drop the cancellation token, contradicting the accepted Step 10 audit note R2-5.** | Step page `:336-343`, `:368`; `ProjectValidationPipeline.cs:87-93`; `audits/step-10.md:485`, `:538`, `:678`. |
| **Minor** | **m-5 — The "one report per recognised command" guarantee has no shape for reserved commands, and `clean --json` without `--dry-run` cannot use `clean-v1`.** | Step page `:176-178`, `:618-626`, `:648`, `:720-722`, `:855-859`, `:1259-1260`; `completion` has no `--json` (`:153`, `:849-853`). |
| **Minor** | **m-6 — `--json` does not state that human progress is suppressed on stderr.** Human progress and JSONL both target stderr; only `--quiet` is documented as a suppression. | Step page `:508`, `:534-535`, `:726`; `docs/reference/cli.md:8-10`. |
| **Minor** | **m-7 — The 3.0–4.0 pw sizing decision has no gated row in the decision register, and the roadmap row still says 1–3 pw.** | Step page `:56-77`; `04-roadmap.md:68`, `:104-112`; `05-open-decisions.md:95`, `:99`. |
| **Minor** | **m-8 — The step page never states that the build pipeline must be constructed with `JobTemplateValidationStage`.** `RunBuildStage = true` on a default pipeline is a no-op; `design-decisions.md` says "reuses the same pipeline wiring and appends its own Build stages", but Step 12 adds no stage. | Step page `:336-343`, `:356`; `ProjectValidationPipeline.cs:285`; `PreviewCommand.cs:130-131`; `design-decisions.md:315`. |
| **Minor** | **m-9 — The `doctor` test plan injects runtime/SQLite/permission/disk/catalog/template failures, but only `ISolverProbe` has a documented seam.** | Step page `:833`, `:1290`, `:977-986`. |
| **Note** | **n-1 — `build.started` is emitted after expansion, so `build.stage` events for load/select/validate/compile/validateBuild/parameterize/expand precede an event named "started".** | Step page `:365`, `:732`, `:739`; `02-data-contracts.md:1597`. |
| **Note** | **n-2 — The `CLI` code table is duplicated in `cli.md` and will also live in `diagnostics.md`, the drift-checked home.** | `docs/reference/cli.md:357-369`; `DiagnosticDocumentationTests.cs:14-33`. |
| **Note** | **n-3 — `cli.md` says "Every command ... with `--json`, exactly one report", but `completion`, `--help`, and `--version` have no `--json`; the `validate` human-output section was deleted while the step page says the human table is unchanged.** | `docs/reference/cli.md:8`; step page `:1243-1244`; design diff removing the "Human output" section. |
| **Note** | **n-4 — The `JOB008` troubleshooting row treats the 99,999 total-job cap as the per-configuration cap.** | `docs/troubleshooting/errors.md:84`; `docs/reference/diagnostics.md:372-374`. |
| **Note** | **n-5 — `BuildOptions.TimeProvider` is non-null but the text says null means `TimeProvider.System`; the null branch is unreachable without `null!`.** | Step page `:244`, `:325-326`. |
| **Note** | **n-6 — The events fixture is said to be accepted by "the GUI event parser", which does not exist until Step 26.** | `02-data-contracts.md:1871`. |
| **Note** | **n-7 — The public-surface note says additive API is added to "the ApiCompat baseline"; the baseline is the release-facing break check and does not need a refresh for additive API.** | Step page `:213-214`, `:1008-1011`; `AGENTS.md` §16; `tools/check-api-compat.sh`. |
| **Note** | **n-8 — The rule for a value that starts with `-` does not define "option token" versus a negative or `-`-prefixed value.** | Step page `:171-173`, `:1274`. |
| **Note** | **n-9 — `ProjectTemplateFactory`/`Mocassin.Packages.Projects.Templates` is added but absent from the §6.1 public surface and §16 layout/public-API plan.** | Step page `:764-765`, `:904-925`, `:1002-1011`, `:1157`. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

### Finding details

**M-1 — The `build --preview` data path cannot be produced from the declared public surface, and the design
contradicts itself about who evaluates the preview.**

- The step page says "`--preview` runs stages 1–7, evaluates the selected configurations through
  `ILatticePreviewService`, and stops" (step page `:371-372`), then says "`--preview`'s configuration
  evaluation ... [is] caller-composed" (`:375-377`), and §12.2 repeats that `preview` is "caller-composed"
  (`:687`, `:1203`). The data contract also calls `preview` a caller-composed timing (`02-data-contracts.md`
  `:1800`).
- The declared interface exposes only `BuildAsync`/`BuildToCompletionAsync` (`:218-223`); `BuildResult` carries
  `Identity`, `Selection`, `JobSelection`, `Package`, `Timings`, and `Diagnostics` (`:286-295`) but no preview
  payload and no `Project`/`CompiledModel`.
- `build-v1.preview` requires the Step 10 payload (`02-data-contracts.md:1802`; step page `:673`):
  `templateId`, `templateName`, `totalJobs`, and the collections/configs/optimizers/dopings/charges.
  `ILatticePreviewService.TryCreatePreview` requires `Project` + `CompiledModel`
  (`src/dotnet/Mocassin.Packages/Jobs/ILatticePreviewService.cs:36-40`), neither of which `BuildResult`
  returns. `BuildRequest.Preview` therefore either has no observable preview output, or the caller must reload
  and recompile, duplicating the load/compile work and breaking the single-load/identity/timing design. The CLI
  `BuildPreviewData.cs` (step page `:960`) is a DTO, not a producer.
- The current `PreviewCommand` loads, validates, compiles, expands, and registers
  `JobTemplateValidationStage` itself (`src/dotnet/Mocassin.Cli/Build/PreviewCommand.cs:122-131`). Step 12's
  "the GUI build UI ... consumes the same library pipeline ... so there is no second build implementation"
  (step page `:46-49`) cannot hold without a library preview result.
- Required: add the preview payload to `BuildResult` (or a typed `BuildPreview`/`IBuildPipeline.PreviewAsync`),
  keep it out of `BuildStage`, state exactly which side evaluates it, and make the `build-v1.preview` producer
  testable. Because Step 26 consumes the same pipeline, this must be part of the public library surface, not a
  CLI-only helper.

**m-1 — `BuildOptions` has no injection point.**

`BuildOptions` defines `ProgressInterval`, `TimeProvider`, and `EventChannelCapacity`
(step page `:241-246`), and the page relies on them: "tests may lower" the capacity (`:326`), "using the
injected `TimeProvider`" (`:383-384`), and progress throttling to `BuildOptions.ProgressInterval` (`:533`). But
`IBuildPipeline` (`:218-223`) takes only `BuildRequest`, and the page lists `BuildPipeline.cs` (`:909`) without a
constructor or DI registration. Either add `BuildOptions Options { get; init; }` to `BuildRequest`, declare a
pipeline constructor, or state the `Microsoft.Extensions.DependencyInjection` registration; otherwise the record
is unreachable and the tests cannot configure it.

**m-2 — The failure-kind → exit-code table is incomplete and conflicts with the all-or-nothing sentence.**

The table (`:558-568`) has rows for `Succeeded`, `Cancelled`, `Io`, `PackageWrite` *caused by*
`IOException`/`UnauthorizedAccessException`, `Load`, `TemplateSelection` with `CLI004`/`PROJ010`,
`Validation`/`JobExpansion`/`LatticeGeneration`, `Compilation`/`Parameterization`, and `Internal`. It has no
row for a `PackageWrite` failure with another cause (the writer can surface `SqliteException`/`InvalidOperationException`
paths; see `SimulationPackageWriter.cs`), and no row for a pipeline-side job-selection failure (`CLI005`) — yet
`BuildRequest.JobSelection` is a public string (`:232`) and the request claims the table "maps every
`BuildResult` to an exit code" (request `:88-91`). The all-or-nothing bullet says "exits 1 (or 3 for a
package-write failure)" (`:552-553`), while the table gives 3 only for the IO-caused case. Add a row for every
`BuildFailureKind`/diagnostic combination the pipeline can return, or state that non-IO writer failures are
`Internal`/10.

**m-3 — `--warnings-as-errors` is described but not plumbed into the documented validation/parameterization
calls.**

`BuildRequest.WarningsAsErrors` exists (`:237`) and §13.3 says it promotes validation warnings (including `JOB`
structural warnings) to exit 1 and parameterization warnings to exit 4 (`:790-792`). The stage snippets build
`new ValidationOptions { RunBuildStage = false/true }` (`:338`, `:342`) and never set
`ValidationOptions.WarningsAsErrors`, although that property exists (`ValidationOptions.cs:23`); the
parameterization call is described as `IParameterizationService.Validate, then Apply` (`:344`) with no promotion
step, and `ParameterizationOptions` has no warning flag. State how the request flag reaches each call (or that
the pipeline post-filters diagnostics) and that only validation/parameterization warnings are promoted, not
`CMP`/`PKG` warnings.

**m-4 — The `Validate` calls are positional and drop the cancellation token, contradicting accepted Step 10
audit note R2-5.**

The stage-3/5 calls are `Validate(project, loadResult.Diagnostics, new ValidationOptions { ... },
compiledModel)` (`:336-343`). `ProjectValidationPipeline.Validate` has the optional `CompiledModel` before the
`CancellationToken` (`ProjectValidationPipeline.cs:87-93`). The Step 10 audit's accepted R2-5 resolution says
"Step 12 and external consumers use named arguments" (`docs/planning/audits/step-10.md:485`, `:538`, `:678`).
The design does not use named arguments and never passes the build token, so the "cancellation is checked at
every stage boundary" claim (`:368`) omits validation. Use `compiledModel:`/`cancellationToken:` (or reorder in
a future major) and pass the token, or record why validation is intentionally non-cancellable.

**m-5 — The "one report per recognised command" guarantee has no shape for reserved commands, and
`clean --json` without `--dry-run` cannot use `clean-v1`.**

The page promises a report for every recognised command and every outcome (`:176-178`, `:648`) and an
acceptance test says "exactly one schema-valid report with `exitCode` for every recognised command and outcome"
(`:1259-1260`). The seven report ids (`:618-626`) have no descriptor for `simulate`, `collect`, `evaluate`,
`export`, or full `clean`, which the parser recognises and exits 2 with `CLI008` (`:855-859`); `completion` has
no `--json` at all (`:153`, `:849-853`); `clean --json` without `--dry-run` cannot use `clean-v1`, whose
`dryRun` is always true and `removedCount` always 0 (`:720-722`). Define the report for a reserved
command/option (a shared usage-report kind or an explicit exemption) and the `--json` behaviour for
`completion`, `--help`, and `--version`.

**m-6 — `--json` does not state that human progress is suppressed on stderr.**

"Human mode writes stage/progress lines to stderr" (`:508`) and "`--json` emits JSON-Lines events on stderr"
(`:726`). The only documented suppression is `--quiet` (`:179-181`, `:534-535`). With `--json` and without
`--quiet`, stderr can therefore contain both human lines and JSONL, which breaks a JSONL consumer. State that
`--json` switches stderr to JSONL only (or that human lines are suppressed whenever `--json` is set), and
assert it in the event/report tests.

**m-7 — The 3.0–4.0 pw sizing decision has no gated row in the decision register, and the roadmap row still
says 1–3 pw.**

The step page estimates 3.0–4.0 pw and asks the owner to accept it without a split (`:56-77`). The roadmap row
still says 1–3 pw (`04-roadmap.md:68`), and §3.1 says to record the estimate and split trigger in the step page
and update the roadmap if a step grows (`:104-112`). Step 11's precedent records the exception/split decision
as a gated row (`05-open-decisions.md:95`), but the Step 12 register adds only the `--template` row (`:99`), not
sizing, the ceria template asset, or the report-registration decision. Add the sizing (and, if still needed,
the other two) gated rows, and update the roadmap row after owner acceptance.

**m-8 — The step page never states that the build pipeline must be constructed with
`JobTemplateValidationStage`.**

The stage-5 snippet (`:340-343`) shows `ProjectValidationPipeline.Validate(... RunBuildStage = true,
compiledModel)` but no `new ProjectValidationPipeline(additionalStages: [new JobTemplateValidationStage(...)])`.
`RunBuildStages` returns immediately when `buildStages.Length == 0` (`ProjectValidationPipeline.cs:285`), so
`RunBuildStage = true` on a default pipeline is a no-op and the compiled-bound `JOB` checks silently do not
run. `PreviewCommand.cs:130-131` shows the required construction. `design-decisions.md:315` says Step 12
"reuses the same pipeline wiring and appends its own Build stages", but Step 12 adds no Build stage. State the
construction or DI registration in the step page and correct the "appends its own Build stages" sentence.

**m-9 — The `doctor` test plan injects runtime/SQLite/permission/disk/catalog/template failures, but only
`ISolverProbe` has a documented seam.**

§13.6 says "the solver probe is the only process boundary; it uses an `ISolverProbe` seam" (`:833`), while the
PR-fast test table requires "injected runtime/SQLite/permission/disk/catalog/template failures" (`:1290`). The
permission and disk checks in particular cannot be failed deterministically without a seam or an
environment-control strategy. Define the check seams (for example constructor-injected probes/delegates) or
adjust the test plan to what is actually testable.

**n-1 — `build.started` ordering.** The page emits `BuildStarted` after expansion (`:365`) and the event table
says `build.started` is "Build began ... known after expansion" (`02-data-contracts.md:1597`), so the stage
events for load/select/validate/compile/validateBuild/parameterize/expand precede an event named "started"
(`:739`). Document that consumers must accept `build.stage` before `build.started`, or emit a start marker
before load with a nullable/absent total.

**n-2 — Duplicate `CLI` table.** `docs/reference/cli.md:357-369` repeats all nine `CLI` rows, while
`docs/reference/diagnostics.md` is the drift-checked home (`DiagnosticDocumentationTests.cs:14-33`). The two
tables can drift silently; link to the diagnostics page or extend the drift test to the CLI page.

**n-3 — `cli.md` overbroad sentence and removed validate output.** `docs/reference/cli.md:8` says "Every
command has ... with `--json`, exactly one machine-readable report", but `completion`, `--help`, and `--version`
do not accept `--json`. The same page's "Human output" section for `validate` was deleted in the design
diff, while the step page says the human table is unchanged from Step 07. Restore the stability statement or
record that the human format is now documented only in the acceptance tests.

**n-4 — `JOB008` cap.** `docs/troubleshooting/errors.md:84` says the `JOB004`/`JOB008`/`JOB019` caps are
500,000 positions and 99,999 jobs; `JOB008` is the 1..1000 per-configuration job count
(`docs/reference/diagnostics.md:372-374`). Name the per-configuration cap.

**n-5 — `TimeProvider` null semantics.** `BuildOptions.TimeProvider` is declared non-null with a
`TimeProvider.System` default (`:244`), while the text says null means `TimeProvider.System` (`:325-326`). The
null branch is unreachable without `null!`; make the property `TimeProvider?` or delete the null sentence.

**n-6 — GUI parser tense.** `02-data-contracts.md:1871` says `events-v1.jsonl` "is accepted by the GUI event
parser" in the present tense, but the GUI parser arrives in Step 26. Phrase it as a future/Step 26 acceptance.

**n-7 — ApiCompat baseline wording.** The public-surface note says new API is added to "the ApiCompat
baseline" (`:213-214`) and §16.3 says the Contracts baseline "is refreshed" (`:1008-1011`). Additive API does
not require a baseline refresh, and `AGENTS.md` §16 says "when intentional". State that `PublicAPI.Unshipped.txt`
is updated and the baseline only if it is intentionally rebaselined.

**n-8 — `-`-prefixed value rule.** "A value that starts with `-` is still a value when the option requires
one; only a following option token starts a new option" (`:171-173`) does not define an "option token" versus a
negative number or path. Specify the exact tokenisation (for example, the next token is always the value, or
only a known option name terminates) and cover `--jobs -1`, `--parallel -1`, and a path beginning with `-`.

**n-9 — Template factory outside the layout/public-API plan.** Feature 4 adds
`Mocassin.Packages.Projects.Templates` and the public `ProjectTemplateFactory` (`:764-765`, `:1157`), but §6.1's
public surface and §16's layout list only the `Build` namespace and the Contracts `Cli` files. Add the folder,
its file list, and the `PublicAPI.Unshipped.txt` obligation so the structure policy and the API gate cover it.

## Record state

**State:** Ready with notes — round 5 reviewed `1b6af90c72a0c4ff77d50fd266e80738bd8490fb` and returned
`Ready with notes`; R4-1–R4-7 are closed, and R5-1 (Minor) plus R5-2/R5-3 (Notes) are recorded in round 5 below
for the human owner's acceptance. Rounds 1–4 and their reviewer text remain unchanged above; round 5 is
appended below.

`Ready with notes` is a verdict awaiting explicit human-owner acceptance, not a merge or start authorization.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

I received only the frozen commit
`80b3f7fd99f0140b1dad5650851d1aaa3c4ac0b8`, the review request record, repository links, and the required
commands/scope. No desired verdict or summary was supplied. I read the repository, the design delta, the
dependency audits, and the code types directly; the review was read-only apart from this record, and I committed
only this one file.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

Follow-up owner: Planner and step agent (design corrections on `docs/step-12-design`); a fresh planning
auditor re-reviews the new commit.
Follow-up step: Step 12 design re-review (round 2).

## Approval summary

The request owner (step agent or planner) completes this section before the orchestrator presents the approval
request. It must cover the full history, not only the latest round.

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
- [x] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by: Human owner
Date: 2026-11-17

Accepted verdict: **Ready with notes** for the design frozen at
`1b6af90c72a0c4ff77d50fd266e80738bd8490fb`. Acceptance is not implementation authorization: the step branch is
not created, the step is not marked `In progress`, and implementation start awaits a separate owner approval.
The R5-1/R5-2/R5-3 dispositions and their owners are recorded in the round-5 resolutions below.

## Resolution status

The step agent owns this section. The round-1 reviewer text above is unchanged. All design fixes are docs-only
and are in `05b7297172f9f87453b3ffc23d06801035da7a2c`; no code, test, configuration, or contract file changed.

| Finding | Status | Owner | Commit | Resolution |
| --- | --- | --- | --- | --- |
| M-1 (Major) | Fixed | Planner | `05b7297` | The pipeline now owns the preview data path: `BuildResult.Preview` carries a typed `BuildPreview` produced after expansion by the shared `BuildPreviewBuilder` over the injected `ILatticePreviewService`; the CLI and Step 26 map it without reload, recompile, or a second evaluation. `BuildPreview.Elapsed` feeds the `preview` timing. A separate `PreviewAsync` was rejected because it would create two orchestration paths for the same work. Step page §6.1/§6.2/§12.2, §16.1, §23; data contract §5.7.3. |
| m-1 (Minor) | Fixed | Planner | `05b7297` | `BuildRequest.Options` is the only injection point for `ProgressInterval`, `TimeProvider`, and the test-tunable `EventChannelCapacity`; the pipeline reads it per call and has no options constructor or ambient configuration. Step page §6.1/§6.3/§19/§23. |
| m-2 (Minor) | Fixed | Planner | `05b7297` | The exit mapping now covers every failure kind/status combination: `JobSelection` (`CLI005`) → 2, `Preview` → 1, `PackageWrite` with a `PKG` error → 1 and without an I/O/`PKG` cause → 10, `None`/`Failed` → 10. Writer I/O is classified as `Io`; load `NotFound`/`TooLarge`/`Io` → `Io`, `InvalidContent` → `Load`. Step page §11.1/§23. |
| m-3 (Minor) | Fixed | Planner | `05b7297` | Both validation calls set `ValidationOptions.WarningsAsErrors = request.WarningsAsErrors`; because `ParameterizationOptions` has no warning flag, the pipeline inspects the returned parameterization diagnostics and fails with `Failure = Parameterization` before the writer opens. Only validation/parameterization warnings are promoted; `CMP`/`PKG` stay warnings. Step page §6.2/§13.3/§20. |
| m-4 (Minor) | Fixed | Planner | `05b7297` | Both `Validate` calls use `loadDiagnostics:`, `options:`, `compiledModel:`, and `cancellationToken:` and pass the build token, following Step 10 audit R2-5. Step page §6.2/§14. |
| m-5 (Minor) | Fixed | Planner | `05b7297` | The one-report guarantee is limited to `validate` and the seven report-backed commands. `completion`, help/version, and reserved commands do not accept `--json` and emit no report; `clean --json` without `--dry-run` emits `clean-v1` with `dryRun = false`, `removedCount = 0`, and `CLI008`, and deletes nothing. Step page §5.1/§12.1/§12.4/§13.7/§13.9/§22; data contract §5.7.2/§5.7.4. |
| m-6 (Minor) | Fixed | Planner | `05b7297` | `--json` now makes stderr JSON Lines only and suppresses human progress; `--quiet` additionally suppresses stage/progress/log events. Step page §5.1/§10/§12.5; `cli.md`; data contract §5.5. |
| m-7 (Minor) | Fixed | Planner | `05b7297` | The owner accepted 3.0–4.0 pw, seven features, no split, with the 2.25 pw contingency. `04-roadmap.md` §3/§3.1 records the Step 12 exception; `05-open-decisions.md` adds the resolved sizing, `--template`, ceria-asset, and report-registration rows; step page §2/§21 record the decisions. |
| m-8 (Minor) | Fixed | Planner | `05b7297` | The pipeline now constructs `ProjectValidationPipeline(additionalStages: [new JobTemplateValidationStage(previewService)])` and shares the preview service; a default pipeline with `RunBuildStage = true` is documented as a silent no-op. `design-decisions.md` no longer claims Step 12 adds a Build stage. Step page §6.2/§16.1/§20. |
| m-9 (Minor) | Fixed | Planner | `05b7297` | Every doctor check implements the internal `IDoctorCheck` seam; `DoctorCommand` accepts an injected check list, production uses the seven real checks, and tests inject pass/warn/fail outcomes. The solver process boundary keeps `ISolverProbe`. Step page §13.6/§16.2/§18.2/§23. |
| n-1 (Note) | Fixed | Planner | `05b7297` | Documented that `build.stage` events for load/select/validate/compile/validateBuild/parameterize/expand precede `build.started`, which means “job generation begins”; consumers must accept stage events before it. Step page §12.5; data contract §5.5. |
| n-2 (Note) | Fixed | Planner | `05b7297` | The duplicate `CLI` table in `cli.md` is replaced by a link to the drift-checked diagnostics reference; feature 1 adds the rows there with the Contracts descriptors. Step page §18.1. |
| n-3 (Note) | Fixed | Planner | `05b7297` | The overbroad `cli.md` sentence now scopes `--json` to `validate` and the seven report-backed commands; the `validate` human-output section is restored with the Step 07 stability statement. |
| n-4 (Note) | Fixed | Planner | `05b7297` | The troubleshooting row separates `JOB004` (500,000 positions), `JOB008` (1–1000 per-configuration jobs), and `JOB019` (99,999 total jobs). |
| n-5 (Note) | Fixed | Planner | `05b7297` | `BuildOptions.TimeProvider` is non-null with a `TimeProvider.System` default; the unreachable null sentence is removed. Step page §6.1/§6.3. |
| n-6 (Note) | Fixed | Planner | `05b7297` | The data-contract fixture text now says the Step 26 GUI parser will accept `events-v1.jsonl`; the CLI schema validator accepts it now. Data contract §6. |
| n-7 (Note) | Fixed | Planner | `05b7297` | Additive API updates `PublicAPI.Unshipped.txt`; the ApiCompat baseline is refreshed only for an intentional rebaseline. Step page §6.1/§16.3. |
| n-8 (Note) | Fixed | Planner | `05b7297` | The parser rule now consumes the next token unconditionally for a value-taking option; `--jobs -1`, `--parallel -1`, and `-o -file` are specified and tested. Step page §5.1/§23; `cli.md`. |
| n-9 (Note) | Fixed | Planner | `05b7297` | `Mocassin.Packages/Projects/Templates/` is added to the layout plan with its files, embedded assets, namespace, public-API obligation, and test folder. Step page §13.1/§16.5; `code-structure.md`. |

No finding is deliberately left open. The four owner decisions are recorded in step page §21 and the resolved
rows in `05-open-decisions.md`; they are not re-opened in round 2.

### Round 5 resolutions — human-owner decisions (2026-11-17)

The human owner accepted the round-5 verdict **`Ready with notes`** for the design frozen at
`1b6af90c72a0c4ff77d50fd266e80738bd8490fb`. The reviewer's round-5 text, evidence, findings, and verdict are
unchanged.

| Finding | Disposition | Owner / follow-up |
| --- | --- | --- |
| R5-1 (Minor) | **Accepted as a Minor owned by feature 1.** The positive-control project opts out of warnings-as-errors with a per-project setting (`TreatWarningsAsErrors=false` or `WarningsNotAsErrors` for IL2026/IL3050), so the `aot` gate asserts IL2026 and IL3050 as **warnings**; the zero-warning host keeps the repository's strict policy. Feature 1 documents the exact setting when it starts. Step page §5.4 and the feature-1 task now state the approach. | Feature 1 (implementor) |
| R5-2 (Note) | **Accepted as a Note.** The exact clang/ILCompiler version is the observed apt/NuGet resolution, not a containerfile pin. The `06` §9 manual checklist checks it; `verify.sh aot` hard-fails on absence, not on a version mismatch. Step page §5.4, `06-toolchain-requirements.md`, `05-open-decisions.md`, and ADR 0015 now state this. | Step agent/planner (recorded); feature 1 keeps the absence check |
| R5-3 (Note) | **Accepted as a Note and confirmed as intended policy.** `aot` stays inside `verify.sh all`, so **every step closeout from Step 12 onward requires the Tier 1 toolchain** (clang/lld/ILCompiler). The consequence is stated in `06-toolchain-requirements.md` §3.2/§5/§8, step page §5.4, `03-quality-strategy.md`, `ci-policy.md`, `05-open-decisions.md`, and ADR 0015. | Step agent/planner (recorded); all future step closeouts |

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

I am a fresh context with no shared conversation with the planner, implementor, or orchestrator. I did not
author or implement any part of the Step 12 design. I received only the frozen commit
`80b3f7fd99f0140b1dad5650851d1aaa3c4ac0b8`, the request record, and the required commands/scope.

The reviewed commit is the design freeze. The request tip
`0d466e8d7f78b1da19e56130d570140105bc429f` is one record-only line on top of it:
`git diff --name-status 80b3f7fd99f0140b1dad5650851d1aaa3c4ac0b8 0d466e8d7f78b1da19e56130d570140105bc429f`
returns `M docs/planning/reviews/step-12.md` only. No code or design doc moved between the freeze and the
request tip, so the delta is a documentation-only request commit and does not need a re-freeze.

#### Commands and results

- `./tools/verify.sh docs --require` → exit 0. Math OK (165 files), links OK (168 files), Zensical strict
  `No issues found`, DocFX 0 warnings/0 errors, rustdoc OK.
- `./tools/verify.sh policy --require` → exit 0. ApiCompat clean for 9 assemblies; NuGet licence/vulnerability
  policy OK; 19 contract artifacts current; 5/4/2 projection fixtures OK; space-group catalog 274 settings
  current; process consistency OK; 58 tool tests OK; 603 SPDX files; 985 secret-scanned files; cargo-deny OK.
- `python tools/check_doc_math.py` → `Documentation math conventions OK (165 files scanned).`
- Interface inspection confirmed the Step 10/11 types and the project graph; the failing claims are design
  gaps, not build failures.

#### Findings

- **M-1 (Major)** — the preview data path is missing and the pipeline/caller boundary is contradictory.
- **m-1 … m-9 (Minor)** — `BuildOptions` injection, incomplete failure→exit mapping, `--warnings-as-errors`
  plumbing, positional `Validate`/missing token (R2-5), reserved-command reports, `--json` stderr mixing,
  sizing decision register/roadmap, `JobTemplateValidationStage` registration, `doctor` injection seams.
- **n-1 … n-9 (Note)** — event ordering, duplicated `CLI` table, overbroad CLI-reference sentence and removed
  validate output, `JOB008` cap, `TimeProvider` null semantics, GUI-parser tense, ApiCompat baseline wording,
  `-`-value rule, and the template factory outside the layout/public-API plan.

No Blocker was found. The request's four owner questions are decidable on the current text; only the sizing
question lacks the decision-register row the Step 11 precedent uses.

#### Verdict

**Changes required.** One Major (M-1) and nine Minor findings remain. The design cannot proceed to the start
gate until the preview data path is designed (and its `BuildResult`/`IBuildPipeline` surface fixed) and the
Minors are either resolved or explicitly accepted. M-1 is a cross-step interface gap that Step 26 shares, so it
is not an implementation detail for feature 3.

### Re-review round 2

#### Independence and frozen state

I am a fresh context. I did not author or implement any part of the Step 12 design and have no shared
conversation with the planner, implementor, orchestrator, or the round-1 reviewer. I received only the frozen
commit `05b7297172f9f87453b3ffc23d06801035da7a2c`, the record HEAD
`7a70ec36617be221234bbbded06cbb782776e3d0`, the request record, and the required commands/scope. No desired
verdict or summary was supplied. I read the repository at the frozen commit, the full round-1 record, the
design/contract/architecture/quality/roadmap/decision pages, and the `src/` and `contracts/` types the design
cites; the review was read-only apart from this record.

The design under review is `05b7297172f9f87453b3ffc23d06801035da7a2c`. The record HEAD
`7a70ec36617be221234bbbded06cbb782776e3d0` is the step agent's documentation-only resolution/status commit on
top of it: `git diff --name-status 05b7297 7a70ec3` returns `M docs/planning/reviews/step-12.md` only. The
resolution commit itself is documentation-only: `git show --stat 05b7297` changes 12 documentation files
(+516/−221) and no code, test, configuration, or contract file. The round-1 `Findings`, `Finding details`,
`Evidence`, and `### Review round 1` text is unchanged; the only pre-round-2 edits to the record are the request
`Reviewed commit` cell, the `Record state` marker, and the `Resolution status` table.

#### Commands and results

- `git rev-parse HEAD` (review worktree) → `05b7297172f9f87453b3ffc23d06801035da7a2c`; the record worktree was
  then switched to `7a70ec36617be221234bbbded06cbb782776e3d0`.
- `git diff --name-status 80b3f7f 05b7297` → 12 documentation files and the record only; no `src/`,
  `contracts/`, `tests/`, `tools/`, or configuration file. `git diff --name-status 05b7297 7a70ec3` →
  `M docs/planning/reviews/step-12.md` only.
- `./tools/verify.sh docs --require` → first run exit 255: in a fresh worktree with no Release assemblies
  DocFX reported “No .NET API project detected” (2 warnings). After `policy` built the Release assemblies, the
  re-run → exit 0: math OK (165 files); links OK (168 files); Zensical strict `No issues found`; DocFX
  `Build succeeded` 0 warnings / 0 errors; rustdoc OK.
- `./tools/verify.sh policy --require` → exit 0. ApiCompat clean for 9 assemblies; NuGet licence/vulnerability
  policy OK; contract artifacts current (19 files); model/parameterization/lattice projections OK
  (5/4/2 fixtures); space-group catalog current (274 settings, sha256
  `1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395`); process consistency OK; 58 Python tool
  tests OK; SPDX headers in 603 source files; no secret patterns in 985 tracked text files; cargo-deny
  advisories/bans/licenses/sources OK; final line `Closeout gate completed with required checks enabled.`
- `python tools/check_doc_math.py` → `Documentation math conventions OK (165 files scanned).`
- Interface re-checks against the frozen tree (read-only):
  `ILatticePreviewService.TryCreatePreview(Project, CompiledModel, LatticeRequest, ImmutableArray<DopingValue>,
  out ...)`; `LatticePreview.Report` exposes `TotalPositions`, `TotalChargeBefore`, `TotalChargeAfter`, and
  `IsChargeNeutral`; `ProjectStore.LoadAsync` and `IModelCompiler.Compile`;
  `ProjectValidationPipeline.Validate(project, loadDiagnostics, options, compiledModel, cancellationToken)`;
  `ValidationOptions.WarningsAsErrors`; `ParameterizationOptions` has no warning flag;
  `IParameterizationService.Validate` returns `ImmutableArray<Diagnostic>`; `SelectionOptimizerResolver.Resolve`;
  `RunBuildStages` returns when `buildStages.Length == 0`; `Mocassin.Packages.csproj` has no `Results` edge;
  `Mocassin.Cli.csproj` references `Results` and reaches `Compilation` transitively; both `Mocassin.Packages`
  and `Mocassin.Cli` declare `InternalsVisibleTo` for their test assemblies; `DiagnosticCatalog.Default`
  composes descriptor sets and the docs-drift regex `\b[A-Z]{3,4}\d{3}\b` accepts `CLI001`;
  `tools/contracts/generate.py` pins `manifestVersion == 1` and maps each `reports` entry through
  `REPORT_CONSTANTS`; the structure checker counts only `*.cs` files.

#### Re-check of the request checklists

I re-read every item in the three request checklists and re-checked it against the frozen docs and code. The
round-1 marks and inline notes remain accurate as history; the items whose notes cited M-1, m-1–m-9, and
n-1–n-9 are now satisfied by the resolution commit. I did not change any item text.

#### M-1 closure

M-1 is closed. The preview data path is now producible from the declared surface with one load and one compile:

- `IBuildPipeline.BuildAsync`/`BuildToCompletionAsync` still take only `BuildRequest`, and `BuildResult` now
  carries `BuildPreview? Preview` (step page §6.1). `BuildPreview` and its collection/config/optimizer/doping
  records are public and match the payload `build-v1.preview` already required.
- The pipeline owns the evaluation: §6.2 places “Preview … BuildPreviewBuilder over the selected descriptors”
  after expansion and `--jobs` filtering and before `Generate+Write`, and stores the typed result in
  `BuildResult.Preview`. §6.1/§20 state it is evaluated inside the pipeline; §12.2 and the data contract
  §5.7.3 say `preview` is a direct projection of `BuildResult.Preview`, that the `preview` timing comes from
  `BuildPreview.Elapsed`, and that the CLI does not reload, recompile, or call the service again. §16.1 places
  the only mapping implementation in `Internal/BuildPreviewBuilder.cs`; the CLI’s `BuildPreviewMapper` projects
  the payload.
- The inputs are in hand at that point: `ProjectStore.LoadAsync` returns the project (stage 1) and
  `IModelCompiler.Compile` returns `CompilationResult.Model` (stage 4);
  `ILatticePreviewService.TryCreatePreview` takes exactly `Project` + `CompiledModel` + the job’s lattice
  request/dopings, and `SelectionOptimizerResolver.Resolve` supplies the optimizers.
  `JobTemplateValidationStage` already has a constructor overload taking the shared `ILatticePreviewService`, so
  the shared-instance claim is consistent with the existing type.
- The GUI-reuse claim is now consistent: §1 and §25 say Step 26 receives the same typed `BuildPreview` from the
  same library pipeline, and the data contract, `cli.md`, and `building.md` use the same wording. The former
  “caller-composed preview” statements are gone; only `verify` remains caller-composed, and §6.2/§12.2/§20 say
  so explicitly.

No second preview implementation remains in the design. `BuildPreviewBuilder` is internal to
`Mocassin.Packages.Build`, so the CLI and GUI consume only `BuildResult.Preview`.

#### Closure of round-1 findings

| Finding | Round-2 status | Evidence verified in the frozen docs/code |
| --- | --- | --- |
| M-1 (Major) | **Closed** | Step page §6.1 `BuildResult.Preview`/`BuildPreview` types and the “single preview output” bullet; §6.2 preview stage; §12.2 `preview` projection and timing; §16.1 `BuildPreviewBuilder`; §20/§25 GUI reuse; data contract §5.7.3. `ILatticePreviewService.cs:36-40` needs `Project`+`CompiledModel`, both available to the pipeline. |
| m-1 (Minor) | **Closed** | `BuildRequest.Options` exists (§6.1), the pipeline reads it per call (§6.3), §20 records it as the only injection point, and §23 has the “Options injection” test. `Mocassin.Packages` has `InternalsVisibleTo("Mocassin.Packages.Tests")` for the internal test constructor. |
| m-2 (Minor) | **Closed** | §11.1 table now has rows for `JobSelection`, `Preview`, `PackageWrite` with/without a `PKG` cause, and `None`/`Failed`; the classification paragraph maps load kinds and writer exceptions. §23 “Exit codes” pins every `BuildFailureKind`/status. |
| m-3 (Minor) | **Closed** | §6.2 sets `ValidationOptions.WarningsAsErrors` on both calls; §13.3 post-filters parameterization diagnostics to `Failure = Parameterization`. `ValidationOptions.WarningsAsErrors` exists; `ParameterizationOptions` has no warning flag; `IParameterizationService.Validate` returns diagnostics. |
| m-4 (Minor) | **Closed** | §6.2 uses `loadDiagnostics:`, `options:`, `compiledModel:`, `cancellationToken:` and passes the token; the signature is `(project, loadDiagnostics, options, compiledModel, cancellationToken)`. Aligns with accepted Step 10 R2-5. |
| m-5 (Minor) | **Closed; residual Note R2-1** | §5.1 limits the one-report guarantee to `validate` and the seven report-backed commands; §12.1/§12.4/§13.7/§13.9/§22 and data contract §5.7.2/§5.7.4 define the reserved-command and `clean --json` behaviour. The closing sentence of data contract §5.7.6 still says “All commands…”, recorded as R2-1. |
| m-6 (Minor) | **Closed** | §5.1/§10/§12.5, `cli.md`, and data contract §5.5 all state that `--json` makes stderr JSON Lines only and suppresses human progress; `--quiet` adds stage/progress/log suppression. |
| m-7 (Minor) | **Closed** | Roadmap §3 row and §3.1 exception; `05-open-decisions.md` resolved rows for sizing/`--template`/ceria/report registration; step page §2/§21. No split; the 2.25 pw contingency is retained. |
| m-8 (Minor) | **Closed** | §6.2 constructs `ProjectValidationPipeline(additionalStages: [new JobTemplateValidationStage(previewService)])` and states the registration is mandatory; §20 repeats it; `design-decisions.md` no longer claims Step 12 adds a Build stage; the `RunBuildStages` no-op is verified. |
| m-9 (Minor) | **Closed** | §13.6/§16.2/§18.2/§23 define the internal `IDoctorCheck`/`DoctorCheckResult` seam and injectable check list; `Mocassin.Cli` has `InternalsVisibleTo("Mocassin.Cli.Tests")`; `ISolverProbe` remains the only process boundary. |
| n-1 (Note) | **Closed** | §6.2/§12.5 and data contract §5.5 state that stage events precede `build.started` and define it as “job generation begins”; §23 “Events” asserts the ordering. |
| n-2 (Note) | **Closed; editorial Note R2-2** | `cli.md` now links to `diagnostics.md` instead of duplicating the table; §18.1 names the drift-checked page (but lists it twice, R2-2). |
| n-3 (Note) | **Closed** | `cli.md` scopes `--json` to `validate` and the seven report-backed commands and restores the `validate` “Human output” section with the Step 07 stability statement. |
| n-4 (Note) | **Closed** | `errors.md` separates `JOB004` (500,000 positions), `JOB008` (1–1000 per-configuration jobs), and `JOB019` (99,999 total jobs). |
| n-5 (Note) | **Closed** | `BuildOptions.TimeProvider` is non-null with a `TimeProvider.System` default; the “null means `TimeProvider.System`” sentence is gone from §6.1/§6.3. |
| n-6 (Note) | **Closed** | Data contract §6 and step page §12.5 say the CLI validator accepts the fixture now and the Step 26 GUI parser will accept it when it exists. |
| n-7 (Note) | **Closed** | §6.1/§16.3 say `PublicAPI.Unshipped.txt` is updated and the ApiCompat baseline only for an intentional rebaseline. |
| n-8 (Note) | **Closed** | §5.1 defines the unconditional next-token rule and the `--jobs -1`/`--parallel -1`/`-o -file` cases; `cli.md` repeats it; §23 “Parser/help” tests it. |
| n-9 (Note) | **Closed** | §16.5 adds `Mocassin.Packages/Projects/Templates/` with `ProjectTemplate`/`ProjectTemplateFactory`/`Assets`, the namespace, the `PublicAPI.Unshipped.txt` obligation, and the test folder; `code-structure.md` repeats it; the structure checker counts only `.cs` files and the depth is 3. |

No round-1 finding is left open. The four owner decisions are recorded as decided in step page §21 and the
`05-open-decisions.md` resolved rows: 3.0–4.0 pw with seven features, no split, the 2.25 pw contingency, and
the roadmap/register exception; `--template` as the build-template id with `--job-template` for job templates;
the ceria starter with the model-hash freshness test and `blank` retained; and `compiled-inspection-v1`/
`package-summary-v1` kept as canonical projections with no new CLI report ids. I found no silent re-scoping of
any of the four.

#### New findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Note** | **R2-1 — The data contract’s closing report-scope sentence still claims every recognised command prints a report with `--json`, contradicting the new m-5 scope.** | `docs/planning/02-data-contracts.md:1864-1865`; contrast `:1786-1793` (§5.7.2), step page §5.1/§13.9, and `cli.md:8`. |
| **Note** | **R2-2 — §18.1 lists `docs/reference/diagnostics.md` twice.** | Step page `:1235`, `:1257-1258`. |
| **Note** | **R2-3 — The implementation-status sizing cross-reference says “`04-roadmap.md` §3.1 and §2”, but the exception is in roadmap §3/§3.1 and §2 is the step page’s own sizing section.** | Step page `:15-16`; contrast `:74-76`, `:1253`, `:1449-1451`; `04-roadmap.md:68`, `:114-123`. |
| **Note** | **R2-4 — §15 says the shared `BuildPreviewBuilder` comes “from `Mocassin.Packages.Jobs`/`Projects.Validation`”, while §16.1 places it in `Mocassin.Packages/Build/Internal/`.** | Step page `:1038-1040`; contrast `:1061`, `:1067`, `:1072-1073`. |

**R2-1.** The m-5 resolution correctly limits the one-report guarantee to `validate` and the seven
report-backed commands in the step page, `cli.md`, and data contract §5.7.2/§5.7.4. However, the last sentence
of data contract §5.7.6 still reads “All commands print exactly one report object with `--json`, including
usage errors, once the command name is recognised.” `completion`, `--help`, `--version`, and the reserved
commands are recognised commands that do not accept `--json` and emit no report. Limit the sentence to
`validate` and the seven report-backed commands (or say “every command that accepts `--json`”). No start block;
the operative sections are correct.

**R2-2.** The feature-1 documentation list in §18.1 contains a `docs/reference/diagnostics.md` bullet at
`:1235` (“`CLI001`–`CLI009` rows and headings, included in the docs-drift test”) and a second at `:1257-1258`
(the link/row addition). Remove one so the plan has one owner bullet for the page. No start block.

**R2-3.** The implementation-status block says “`04-roadmap.md` §3.1 and §2 record it.” The roadmap exception is
in §3 (step-estimate row) and §3.1 (recorded-exception paragraph); roadmap §2 is the phase overview. The same
page correctly says “`04-roadmap.md` §3 and §3.1” at §2, §18.1, and §20. Use the same reference (or name the
step page §2 explicitly). No start block.

**R2-4.** §15 says `Mocassin.Packages.Build` uses “the shared `BuildPreviewBuilder` from
`Mocassin.Packages.Jobs`/`Projects.Validation`”. §16.1 lists `Internal/BuildPreviewBuilder.cs` under
`Mocassin.Packages/Build/`. The builder is in the same assembly either way, so this is a placement/ownership
wording issue, not a dependency question; name `Build` for the builder and `Jobs`/`Projects.Validation` for the
service and stage. No start block.

#### Verdict

**Ready with notes.** M-1 is closed: the preview payload is produced once, inside the pipeline, from the single
`ProjectStore.LoadAsync` and the single `IModelCompiler.Compile`, and is returned as the public typed
`BuildResult.Preview` that both the CLI and Step 26 project. All nine Minor findings and all nine round-1 Notes
are resolved in the frozen documents, and the four owner decisions are recorded as decided without re-scoping.
No Blocker or Major remains. Four editorial/consistency Notes (R2-1–R2-4) remain; none affects a cross-step
interface, a testable guarantee, or the start gate. The human owner may accept them as notes or ask for a small
documentation-only cleanup before the start gate.

Follow-up owner: Step agent/planner for the four documentation-only Notes; the human owner approves the verdict
and the start gate.

### Review round 3

#### Independence and frozen state

I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
planner, implementor, orchestrator, or the round-1/round-2 reviewers. I received only the frozen commit
`6b15f9d1630aa5d86830d76fab9a561308e8d4e3`, the record HEAD
`2e18144d89534734e3929b1ccc2bf7ceb1c867e4`, the request record, repository links, and the required
commands/scope. No desired verdict or summary was supplied. I read the repository at the frozen commit, the full
record including rounds 1–2, every document the freeze changes, and the resolved `System.CommandLine` 3.0.0
assembly; the review was read-only apart from this record.

The design under review is `6b15f9d`. The record HEAD `2e18144` is two documentation-only commits on top of it:
`git diff --name-status 6b15f9d 2e18144` returns `M docs/planning/reviews/step-12.md`,
`M docs/planning/status.md`, and `M docs/planning/steps/step-12-build-orchestration-and-cli.md`. The non-record
hunks change only the status row's freeze pointer (from the intermediate `8af67f8` to `6b15f9d`) and the
step-page `Design review` bullet; no design text moved between the freeze and the record HEAD.

The design delta `05b7297..6b15f9d` (round-2 freeze to round-3 freeze) is 15 documentation files: the Step 12
page, `02-data-contracts.md`, `reference/cli.md`, `03-quality-strategy.md`, `01-target-architecture.md`,
`design-decisions.md`, `05-open-decisions.md`, `04-roadmap.md`, `code-structure.md`, `status.md`, the Step 07
page, the record, the new ADR 0015, and two process pages (`agent-workflow.md`, `orchestration.md`) that came
from the merged `main`. No `src/`, `tests/`, `contracts/`, `tools/`, or configuration file changed. Rounds 1–2,
the findings table, the finding details, and the round-2 text are unchanged; the record diff only updates the
request top/step summary, `Record state`, the resolution table, and appends round 2.

#### Commands and results

- `./tools/verify.sh policy --require` → exit 0. ApiCompat clean for 9 assemblies; NuGet licence/vulnerability
  policy OK; contract artifacts current (19 files); model/parameterization/lattice projections OK (5/4/2
  fixtures); space-group catalog current (274 settings, sha256
  `1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395`); process consistency OK; 58 Python tool
  tests OK; SPDX headers in 603 source files; no secret patterns in 986 tracked text files; cargo-deny
  advisories/bans/licenses/sources OK; final line `Closeout gate completed with required checks enabled.`
- `./tools/verify.sh docs --require` → exit 0. Math OK (166 files); links OK (169 files); Zensical strict
  `No issues found`; DocFX `Build succeeded.` (0 warnings / 0 errors); rustdoc OK; site built.
- `python tools/check_doc_math.py` → `Documentation math conventions OK (166 files scanned).`
- Read-only probe of the resolved package (a throwaway console app in `/tmp/opencode`, referencing
  `System.CommandLine` 3.0.0 `lib/net10.0/System.CommandLine.dll`; no repository file was created or changed):
  - Assembly version `3.0.0.0`. `ParserConfiguration` has `EnablePosixBundling` (default `true`) and
    `ResponseFileTokenReplacer` of type `TryReplaceToken` (`bool Invoke(string, out IReadOnlyList<string>?,
    out string?)`). `System.CommandLine.Parsing.StringExtensions` (with `Tokenize`, `TryReadResponseFile`, and
    `SplitLine`) is `NotPublic`, so the tokenizer is not callable by the CLI.
  - Default parse errors return 1; `--help` and `--version` return 0; `run --version` is
    `Unrecognized command or argument '--version'.`; the default version action prints only the version string.
  - `@rsp` expands before `--`; `-- @rsp` leaves `@rsp` literal; a `--` inside a response file stops expansion
    for the tokens after it (with a custom replacer, the replacer is not called for a later `@token`); `@` alone
    is literal; `@@name` calls the replacer with `@name`; `--input=@file` is literal; a nested `@file` is
    resolved against the working directory, not the containing file; the replacer is called recursively for
    `@` tokens returned by a previous replacement.
  - Tokenizer examples match §5.3: `a"b c"d` becomes `ab cd`; `"a""b"` becomes `a` and `b`; `a""b` becomes
    `ab`; `""` is an empty argument; `x"y z` becomes `xy z`; a token starting with an unterminated quote is
    discarded.
  - `--jobs -1`, `--parallel -1`, and `-o -file` bind as values; with `EnablePosixBundling = false`, `-abc` is
    one unrecognized token, not a bundle.
- Diff and closure checks: `git diff --name-status 05b7297 6b15f9d` (above); `git diff 6b15f9d 2e18144` (above);
  `git diff 05b7297 6b15f9d -- docs/planning/reviews/step-12.md` adds round 2 and changes only the request,
  record state, and resolution sections. R2-1–R2-4 were re-checked against the frozen text (below).

#### Consistency of the changed decisions

The parser choice and version agree across the step page §5.2/§15/§16.2/§20, ADR 0015, `02-data-contracts.md`
§5.1/§5.7.6, `reference/cli.md`, `03-quality-strategy.md` §9.10, `01-target-architecture.md` §2/§7.3/ADR table,
`design-decisions.md`, `05-open-decisions.md`, `04-roadmap.md`, `code-structure.md`, and the Step 07 page:
`System.CommandLine` 3.0.0, MIT, centrally pinned, referenced only by `Mocassin.Cli`, no transitive package for
`net10.0+`, library-supplied command-line tokenization/arity/help/version, and CLI-owned command graph, typed
binding, `CLI001` wording, and exit-code policy.

The parse-error remap agrees in every place it appears: `ParseResult.Errors` → `CLI001`/2, help/version 0, and
the library's default 1 never exposed (step page §5.1/§5.2/§11.1/§22, data contract §5.1/§5.7.6, `cli.md`,
ADR 0015 decision 3, `01-target-architecture.md` §7.3, `03-quality-strategy.md` §9.10, `design-decisions.md`).
The exit table is unchanged apart from row 2 naming parse and response-file errors.

The AOT gate scope agrees: the minimal `tests/aot/` smoke host with a positive control is the gate; only
`linux-x64` is proven; the real CLI publish is recorded as evidence and dependency-owned SQLite/MessagePack
warnings are attributed to Step 31; Windows/macOS AOT, the `netstandard2.0` asset, and the AOT/single-file
decision remain Step 31 gates (step page §5.2/§18.2/§22, ADR 0015, quality §9.10, `05-open-decisions.md` AOT
row, `01-target-architecture.md`).

The sizing agrees on 2.75–3.75 pw, seven features, no split, and the 2.25 pw contingency (step page §2/§21,
roadmap §3/§3.1, `05-open-decisions.md`, request step summary). The old 3.0–4.0 figure survives only where
R3-5 records it.

The preserved contracts are untouched by this delta: `validation-v1` (feature 1 keeps it byte-compatible; no
schema edit), the exit table's 0/1/2/3/4/5/6/10 semantics, `--json`/JSON Lines, the seven `reports` ids,
`--jobs`/`job_selection`, atomic `--force`, and cancellation. The only data-contract edits are the §5.1 parser
and response-file bullets, the §5.7.2 `completion` removal, and the §5.7.6 report-scope/parse-error paragraph.

#### `@file` contract

The contract is specific and mostly implementable and testable. The recursive expansion, cwd-relative path
resolution, the `@`/`@@`/no-escape rules, the `--` boundaries, the `--input=@file` versus `--input @file`
split, the `--json` rules, the five limits, and the `CLI001`/2 error class are all pinned by acceptance
criteria and test rows; the probe above confirms the library's tokenizer and `--` behaviour that §5.3
describes. Four details need correction before implementation: the reader/tokenizer ownership (R3-1), the
literal `@`-leading option-value sentence (R3-3), and the published-reference and error-report precision
(R3-6, R3-7).

#### Closure of the round-2 notes

| Note | Round-3 status | Evidence |
| --- | --- | --- |
| R2-1 (data-contract report scope) | **Closed** | `02-data-contracts.md:1884-1888` now limits the guarantee to `validate` and every command that accepts `--json`, and excludes help/version/reserved commands; matches §5.7.2, `reference/cli.md:389-392`, and step page §5.1/§13.9. |
| R2-2 (duplicate `diagnostics.md` bullet) | **Open** | Step page `:1382` and `:1406` still contain one `docs/reference/diagnostics.md` bullet each. |
| R2-3 (roadmap cross-reference) | **Closed** | Step page `:21-23` now cites `04-roadmap.md` §3/§3.1 and `05-open-decisions.md`; no §2 reference. |
| R2-4 (`BuildPreviewBuilder` placement) | **Open** | Step page `:1177` still says the builder comes from `Mocassin.Packages.Jobs`/`Projects.Validation`; §16.1 `:1208`/`:1213` place it in `Mocassin.Packages/Build/Internal/`. |

R2-1 is genuinely closed; R2-2 and R2-4 are documentation-only Notes and were already Notes in round 2, not
start blockers.

#### Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **R3-1 — The bounded-reader story names `ResponseFileTokenReplacer`, but that hook cannot tokenize file content or observe nesting depth; the design says the library supplies tokenization while the CLI reader must reimplement it.** | Step page `:257-261` ("bounded `ResponseFileTokenReplacer`"), `:285-288` (library token-level expansion; CLI-owned contract), `:300-310` (quoting rules and depth 8), `:1227` (`CliResponseFiles` reader + syntax); ADR 0015 `:26`, `:28-31`, `:39`; `05-open-decisions.md:75`; `design-decisions.md:406`; `02-data-contracts.md:1460`; probe: `StringExtensions`/`Tokenize`/`TryReadResponseFile` are `NotPublic`, and `TryReplaceToken` receives one token and no depth. |
| **Minor** | **R3-2 — Two documents still describe `completion` as an existing command, contradicting its removal.** | `reference/cli.md:10` ("`completion`, `--help`, `--version` ... do not accept `--json`"); `design-decisions.md:459` ("`completion`, help/version, and reserved commands ..."); contrast `cli.md:363-370`, step page `:214`/`:1117-1131`, data contract `:1479-1482`, `05-open-decisions.md:77`. |
| **Minor** | **R3-3 — §5.3 says a literal option value beginning with `@` cannot be written before `--`, while the same section makes `--input=@file` literal; the first sentence is false.** | Step page `:292-293`; contrast `:319-321`; probe: `--input=@rsp1.rsp` binds the literal value `@rsp1.rsp`. |
| **Minor** | **R3-4 — `--version` is said to be a library action and to print the runtime description, but the library's default action prints only the version and is root-only; the design does not say the CLI customizes it.** | Step page `:190` (library actions; "after a command they are command help"), `:213` (assembly informational version and runtime description); probe: default `--version` printed `1.0.0`; `run --version` was `Unrecognized command or argument '--version'.` |
| **Note** | **R3-5 — The old 3.0–4.0 pw figure survives in the step page's documentation plan and in two round-1 request items.** | Step page `:1400`; record `:149` (sizing) and `:137` ("completions"); contrast step page `:21`/`:66`/`:88`, roadmap `:68`/`:115-123`, `05-open-decisions.md:83`. |
| **Note** | **R3-6 — The `CLI001` descriptor text does not name parse or response-file errors, and the expansion-failure report guarantee is unqualified although it needs a recognised command.** | Step page `:837` (descriptor examples), `:769`/`:812-815` (response-file errors are `CLI001`), `:1701-1702` ("a `--json` on the command line still produces the command report"); data contract `:1884-1888` ("once the command name is recognised"); `cli.md:56-59`; `mocassin @missing --json` has no recognised command. |
| **Note** | **R3-7 — The published `cli.md` response-file section omits quoting/line-ending rules that §5.3 and the test plan pin, although the data contract calls both pages the full contract; the 4 MiB wording also differs.** | `cli.md:39-59` (no empty-quote, concatenation/`"a""b"`, or CR/CRLF rule; "4 MiB total"); step page `:297-310` (`""`, `a"b c"d`, `"a""b"`, LF/CRLF/CR, "4 MiB across the command line"); data contract `:1477-1478`. |
| **Note** | **R3-8 — The AOT smoke check is assigned to `Nightly/release`, but the project has no scheduled nightly and the design names no hook that runs it after Step 12.** | Step page `:1416`, `:1739`, `:1762`; `03-quality-strategy.md:511-513`; `AGENTS.md` §3.13 (Nightly is manual-only); the Step 12 acceptance criterion `:1703-1706` runs it at closeout. |

#### Finding details

**R3-1.** The design correctly says the library's default reader is replaced, and the probe confirms that a
custom `ResponseFileTokenReplacer` is invoked for `@file` tokens before `--`, that the library recurses into
replacement tokens, and that a `--` inside an expanded file stops later expansion. What the public hook cannot
do is (a) tokenize the file content — `StringExtensions.Tokenize`/`TryReadResponseFile`/`SplitLine` are
internal — and (b) know the current nesting depth — `TryReplaceToken(string token, out IReadOnlyList<string>
replacementTokens, out string? errorMessage)` carries no depth or position, and the library decides when to
recurse. Therefore the depth-8 limit and the "self-reference fails at the depth bound" risk mitigation require
`CliResponseFiles` to recurse and flatten itself, which means it must implement the quoting/whitespace rules (a
second tokenizer) in addition to replacing the reader. State that explicitly: either the CLI owns
response-file tokenization and recursive expansion and the library only parses the resulting token list, or the
CLI accepts library-driven recursion and bounds files/tokens/bytes, not depth. The current text says both "the
library supplies tokenization" and "the CLI owns the syntax", and names only the replacer.

**R3-2.** The removal is recorded in ADR 0015 decision 5, step page §13.8, data contract §5.1, and
`05-open-decisions.md` (CLI shell completion row), so the decision itself is consistent. The two stale
sentences are in the published CLI reference and the developer decision page; both still name `completion` as
a command that "does not accept `--json`". Replace them with the help/version/reserved list.

**R3-3.** The first bullet says a literal option value beginning with `@` "cannot be written before `--`; use a
positional argument after `--`, or avoid the leading `@`." That contradicts the later `--input=@file` rule:
`--name=@value` is a token that does not start with `@`, so it is literal (probe: `--input=@rsp1.rsp` bound
`@rsp1.rsp`). Change the sentence to allow `--name=@value` before `--`, or drop it.

**R3-4.** The library's built-in `--version` action prints the version string only and is added to the root
command; it is not inherited by subcommands (`run --version` is an unrecognized argument and exit 1 before the
CLI remap). The step page promises the assembly informational version and the runtime description, and says
help/version after a command are "command help". Either state that feature 1 replaces the root `VersionOption`
action with a CLI-owned action that adds the runtime description and that `--version` after a command is a
`CLI001`/2 parse error, or narrow the promised output.

**R3-5.** Step page `:1400` is part of the current documentation plan and should say 2.75–3.75 pw (or "both
figures"). The request's round-1 checklist items at `:149` and `:137` are request-owned text; add an inline
note rather than editing the item, or accept them as history. My notes above mark the response-file item and
the sizing item, but the item text is unchanged.

**R3-6.** `CLI001`'s descriptor text lists "unknown command or option, missing value, conflicting options,
invalid option combination"; the new classes (library parse errors, response-file missing/unreadable/invalid/
oversized) are `CLI001` per §11.1 and the data contract but are not named in the descriptor that
`docs/reference/diagnostics.md` will publish. Separately, the acceptance sentence says a `--json` on the
command line "still produces the command report for an expansion failure"; that holds only when a command name
is recognised outside the failed file. Limit it to `mocassin <command> @missing --json` (as the data contract
already does).

**R3-7.** §5.3 is detailed enough to implement and test; `cli.md` is the user-facing page and says less. The
data contract calls both "the full contract", so a reader cannot tell whether the omitted cases are
intentional. Either add the omitted quoting/line-ending rules to `cli.md` or name §5.3 as normative and
`cli.md` as a summary. Also align "4 MiB total" with "4 MiB across the command line" and say whether the limit
counts raw file bytes or expanded tokens.

**R3-8.** The acceptance criterion makes the smoke check a Step 12 closeout activity, so it will run once. The
risk table and test tier say "nightly/release", but `AGENTS.md` §3.13 keeps Nightly manual and there is no
scheduled run; after Step 12 the dependency claim has no recurring guard. Name the command that runs it (for
example a `tools/verify.sh` target invoked at closeout and before a `System.CommandLine` version change) or
record it as a manual Step 31 gate, and keep the "recorded evidence, not a gate" distinction for the real CLI
publish.

#### Verdict

**Ready with notes.** No Blocker or Major remains. The parser/version, the parse-error → `CLI001`/2 remap, the
AOT gate scope, and the sizing agree across the step page, the data contract, `cli.md`, the quality strategy,
the architecture page, the decision pages, the roadmap, the structure page, and ADR 0015; the preserved
contracts (`validation-v1`, the exit table, `--json`/JSON Lines, the seven reports, `--jobs`, atomic `--force`,
cancellation) are unchanged; and R2-1 is genuinely closed by the data-contract edit. Four Minor findings
(R3-1–R3-4) and four Notes (R3-5–R3-8) remain, plus the two still-open round-2 Notes R2-2 and R2-4. All are
documentation/precision issues; none blocks the start gate if the human owner accepts them. The human owner may
accept the notes or ask for a small documentation-only cleanup before the start gate.

Follow-up owner: step agent/planner for the documentation-only Minors and Notes; the human owner approves the
verdict and the start gate.

#### Round 3 resolutions

The step agent/planner fixed every round-3 finding and the two still-open round-2 Notes in a documentation-only
change. The reviewer's round-3 text, evidence, findings, verdict, and the earlier rounds are unchanged. The
new freeze is `d6c54029d487b2e87aaf6b62a7da3cf232ffd680`.

| Finding | Status | Resolution |
| --- | --- | --- |
| R3-1 (Minor) | Fixed | The CLI now owns response-file tokenization and recursive expansion. `CliResponseFiles` tokenizes with `SplitCommandLine`-equivalent semantics, flattens nested references itself, and enforces depth/token/file/byte bounds; `CliApplication` parses the flattened list with `ParserConfiguration.ResponseFileTokenReplacer = null`. The step page §5.3 states why the public hook cannot be used (internal tokenizer, no depth signal) and requires golden tokenizer tests. ADR 0015, decision 4, `design-decisions.md`, `05-open-decisions.md`, and `02-data-contracts.md` §5.1 agree. |
| R3-4 (Minor) | Fixed | `--version` is specified as root-only and CLI-owned: the library's default root `VersionOption` action is replaced by a CLI action that prints the assembly informational version and runtime description; `mocassin <command> --version` is `CLI001`/2. Step page §5.1/§5.2/§19/§22/§23, `reference/cli.md`, and ADR 0015 decision 2–3. |
| R3-2 (Minor) | Fixed | Removed the remaining `completion` mentions from `docs/reference/cli.md` (report-scope sentence) and `docs/development/design-decisions.md` (report-guarantee sentence). The only remaining mentions are the explicit removal statements. |
| R3-3 (Minor) | Fixed | Step page §5.3 now says `--name=@value` is the before-`--` form of a literal `@`-leading option value; the space-separated `--name @value` form is the response-file reference. `reference/cli.md` repeats it. |
| R3-5 (Note) | Fixed | Step page §18.1 now says 2.75–3.75 pw; the two round-1 request checklist items (commands no longer list completions; sizing says 2.75–3.75 pw) were corrected without touching reviewer-authored text. |
| R3-6 (Note) | Fixed | The `CLI001` descriptor now names library parse errors and response-file missing/unreadable/invalid/oversized errors. Step page §5.3/§11.1/§22 and `reference/cli.md` now say a report is produced only when a command is recognised outside the failed file; `mocassin @missing --json` emits no report. `05-open-decisions.md` repeats the `CLI001` scope. |
| R3-7 (Note) | Fixed | `reference/cli.md` now carries the §5.3 quoting, concatenation, empty-quote, line-ending, and `#` rules. Both pages use “4 MiB of raw response-file bytes across the command line”; the per-file limit is raw bytes, and the 10,000-token limit is separate. |
| R3-8 (Note) | Fixed | The AOT smoke host runs through a new `verify.sh aot` mode included in `verify.sh all`, so it runs at every step closeout and the Step 31 release gate, and on a `System.CommandLine` version-change branch. It is no longer labelled nightly; no scheduled run is claimed. Step page §5.2/§18.2/§19/§22/§23/§24, `03-quality-strategy.md`, and ADR 0015. |
| R2-2 (Note) | Fixed | The duplicate `docs/reference/diagnostics.md` bullet was merged into one §18.1 bullet with the drift-test and link wording. |
| R2-4 (Note) | Fixed | Step page §15 now says `BuildPreviewBuilder` comes from `Mocassin.Packages.Build.Internal`, while `ILatticePreviewService`/`JobTemplateValidationStage` come from `Jobs`/`Projects.Validation`; §16.1 already places the builder in `Build/Internal`. |

### Review round 4

#### Independence and frozen state

I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
planner, implementor, orchestrator, or the round-1/2/3 reviewers. I received only the frozen commit
`d6c54029d487b2e87aaf6b62a7da3cf232ffd680`, the record HEAD
`778824f81c1c11f1ec68eeeeec199c7eb896d002`, the request record, repository links, and the required
commands/scope. No desired verdict or summary was supplied. I read the repository at the frozen commit, the full
record including rounds 1–3, every document the freeze changes, and the resolved `System.CommandLine` 3.0.0
assembly; the review was read-only apart from this record.

The design under review is `d6c5402`. The record HEAD `778824f` is three documentation-only commits on top of
it: `git diff --name-status d6c5402 778824f` returns `M docs/planning/reviews/step-12.md`,
`M docs/planning/status.md`, and `M docs/planning/steps/step-12-build-orchestration-and-cli.md`; the step-page
hunk changes only the `Design review` status bullet (2 lines), and the record/status hunks point at the freeze
and the pending round. No design text moved between the freeze and the record HEAD. The design delta
`6b15f9d..d6c5402` is nine documentation files (record, step page, data contract, quality strategy, open
decisions, design decisions, ADR 0015, `reference/cli.md`, status); no `src/`, `tests/`, `contracts/`, `tools/`,
or configuration file changed.

Rounds 1–3 and the R2/R3 reviewer text are unchanged. `git diff d211584 d6c5402 -- docs/planning/reviews/step-12.md`
changes only the request `Reviewed commit`/note, the two request-owned checklist items R3-5 corrected, the
`Record state` line, and the appended `Round 3 resolutions`; the round-1–3 `Findings`, `Finding details`,
`Evidence`, and verdict text are byte-identical.

#### Commands and results

- `./tools/verify.sh policy --require` → exit 0. ApiCompat found no breaking changes for 9 assemblies (its usual
  unresolved `MessagePack`/`Microsoft.Data.Sqlite` reference notices are not failures); NuGet
  licence/vulnerability policy OK; contract artifacts current (19 files); model/parameterization/lattice
  projections OK (5/4/2 fixtures); space-group catalog current (274 settings, sha256
  `1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395`); process consistency OK; 58 Python tool
  tests OK; SPDX headers in 603 source files; no secret patterns in 986 tracked text files; cargo-deny OK;
  final line `Closeout gate completed with required checks enabled.`
- `./tools/verify.sh docs --require` → exit 0. Math OK (166 files); links OK (169 files); Zensical strict
  `No issues found`; DocFX `Build succeeded` with 0 warnings / 0 errors; rustdoc OK; site built.
- `python tools/check_doc_math.py` → `Documentation math conventions OK (166 files scanned).`
- Read-only probe of `System.CommandLine` 3.0.0 from the local NuGet cache (throwaway projects in
  `/tmp/opencode`; no repository file was created or changed):
  - `CommandLineParser.SplitCommandLine(string)` is `public static`; `Tokenize`/`TryReadResponseFile`/`SplitLine`
    on `System.CommandLine.Parsing.StringExtensions` are non-public.
  - `SplitCommandLine` returns the §5.3 golden cases: `a"b c"d` → `ab cd`; `"a""b"` → `a` + `b`; `a""b` → `ab`;
    `""` → one empty token; `x"y z` → `xy z`; a leading unterminated quote is discarded; `abc "unterminated`
    → `abc`; `'a b'` → `'a` + `b'`; `a\b` → `a\b`.
  - `ParserConfiguration.ResponseFileTokenReplacer` defaults to a non-null delegate and can be set to `null`;
    with `null`, `@rf/a.rsp`, `@` and `@@rf/a.rsp` are left literal with no exception. With the default, `@file`
    expands recursively; `-- @file` is literal; `@rf/a.rsp` after `--` is literal; `--input=@file` is literal;
    `--input @file` expands into multiple tokens; a `#` line is ignored; a nested reference resolves against the
    cwd; a self-reference stack-overflows the library (confirming the depth bound must be CLI-owned).
  - `VersionOption` is a public `Option` with a settable `Action`; the default root `--version` prints `1.0.0`
    and exits 0; `run --version` is `Unrecognized command or argument '--version'.`; `--bogus` exits 1. The
    design's CLI action replacement is therefore implementable, and the CLI remap is required for exit 2.
  - `dotnet publish -c Release -r linux-x64 -p:PublishAot=true -p:TrimMode=full -p:TrimmerSingleWarn=false` on
    a minimal `System.CommandLine` host → exit 0; clang/lld and `Microsoft.DotNet.ILCompiler` 11.0.0 are present
    in the image. The native binary prints `1.0.0` for the default `--version`, shows help with exit 0, and
    returns 1 for a parse error.
- Diff/closure checks: `git diff --name-status d6c5402 778824f` (above); `git diff d211584 d6c5402 -- record`
  (above); `git diff --name-status 6b15f9d d6c5402` (nine documentation files). R2-2 and R2-4 were re-checked
  against the frozen text: §18.1 has one `docs/reference/diagnostics.md` bullet, and §15 names
  `Mocassin.Packages.Build.Internal` for the builder.

#### Consistency of the changed decisions

**R3-1 is closed.** The operative documents agree that the CLI owns response-file tokenization and recursive
expansion: step page §5.2/§5.3/§16.2, data contract §5.1, `reference/cli.md`, `design-decisions.md`,
`05-open-decisions.md`, and ADR 0015 decision 4 all say `CliResponseFiles` pre-expands the raw argument list and
`CliApplication` parses the flat list with `ParserConfiguration.ResponseFileTokenReplacer = null`; the library
default is never the contract. The bounds are CLI-enforced (1 MiB raw per file, 4 MiB raw response-file bytes
across the command line, depth 8, 10,000 response-file tokens, 32 files), the risk table records the
self-reference/depth guarantee, and the golden cases match the library's observable `SplitCommandLine` semantics
(listed above). One implementor nuance: `CommandLineParser.SplitCommandLine` is public, so `CliResponseFiles`
may call it rather than write a second tokenizer; the design's "helpers internal" sentence is true of
`SplitLine`/`Tokenize` and of the hook's inability to see depth, but the public method is the exact tokenizer the
golden cases pin. This is an implementation choice, not an open design question.

**R3-4 is only partially closed (R4-1).** The operative sections are correct: step page §5.1/§5.2,
`reference/cli.md`, and ADR 0015 decisions 2–3 say the CLI replaces the library's root `VersionOption` action,
prints the assembly informational version and runtime description, keeps `--version` root-only, and maps
`mocassin <command> --version` to `CLI001`/2. The probe confirms the replacement is implementable and the
library default is 1/root-only. However, several summaries still say the library owns/supplies help/version:
`03-quality-strategy.md:453` ("supplies ... help/version"), `05-open-decisions.md:75` ("library owns ...
help/version"), `design-decisions.md:406` (same), `02-data-contracts.md:1461` ("help/version actions"; it also
does not record the root-only/`CLI001` rule), step page §11.1 `:842` ("Help and version are library actions"),
and ADR 0015:42 ("help/version are not reimplemented"). They contradict the step page/ADR decision and could lead
an implementor to keep the default.

**R3-6 is only partially closed (R4-2).** The `CLI001` descriptor now names library parse errors and response
file missing/unreadable/invalid/oversized errors in step page §11.2, data contract §5.1/§5.7.6,
`reference/cli.md`, and `05-open-decisions.md`. The report rule is stated consistently: step page
§5.1/§11.1/§22, data contract §5.7.6 ("once the command name is recognised"), and `reference/cli.md` exclude
`--help`/`--version`/reserved commands and the `mocassin @missing --json` case; no report is produced without a
recognised command. But step page §11.2 `:878` still says "`System.CommandLine` parse errors are the only source
of `CLI001`", contradicting the response-file `CLI001` class in the same table and §5.3.

**R3-8 is only partially closed (R4-3, R4-4).** The design names feature 1 as the owner of the
`verify.sh aot` mode and includes it in `verify.sh all`; the AOT publish is feasible in this image. But the
required gate is not fully specified. `tools/verify.sh` has no `aot` mode yet (feature 1 adds it) and `--require`
currently gates only `cargo-deny`; `06-toolchain-requirements.md:68` still marks `clang`/`lld`/`llvm` Optional,
Tier 2, "Steps 19, 29, 31" and `zlib1g-dev` Recommended for Steps 29/31; the design does not say whether a
missing clang/ILCompiler/network fails `verify.sh all --require` or skips; and the closeout CI job does not
invoke `tools/verify.sh` (it runs restore/build/test/format), so the gate is local-only and the closeout CI
cannot confirm it. The smoke-test row also expects the native binary to "report a parse error with exit 2",
while the minimal host's library default is 1; the design does not say the host replicates the CLI remap, and
the positive control cannot share the zero-warning publish without a separate project/configuration.

**No stale design statement remains.** No document still specifies a hand-written parser, a `completion`
command, the 3.0–4.0 pw figure as the current estimate, or an unenforceable response-file hook. The remaining
"hand-written parser" mentions are historical ("replaced the round-2 hand-written-parser decision", "there is
no hand-written parser", "deletes the Step 07/10 hand-written parsers"); `completion` appears only in the
explicit removal statements (§13.8, `reference/cli.md:372-377`, `design-decisions.md:420`,
`05-open-decisions.md:77`, ADR 0015 decision 5); the 3.0–4.0 pw figure survives only as the historical decision
record (§2/§20/§21, roadmap §3/§3.1, `05-open-decisions.md:83`), while the current estimate is 2.75–3.75 pw;
and the response-file hook is disabled, not used as the contract.

#### Closure of the round-3 findings and round-2 notes

| Finding | Round-4 status | Evidence |
| --- | --- | --- |
| R3-1 (Minor) | **Closed** | Step page §5.2/§5.3/§16.2; data contract §5.1; `reference/cli.md`; `design-decisions.md`; `05-open-decisions.md`; ADR 0015 decision 4. CLI pre-expansion, hook disabled, CLI-owned bounds, golden cases match `SplitCommandLine`. |
| R3-2 (Minor) | **Closed** | No stale `completion` command remains; `reference/cli.md:372-377` and `design-decisions.md:420` are explicit removal statements, and §13.8/data contract/`05-open-decisions.md` agree. |
| R3-3 (Minor) | **Closed** | Step page §5.3 and `reference/cli.md` both make `--name=@value` the literal before-`--` form and `--name @value` the response-file reference; the probe confirms `--input=@file` is literal. |
| R3-4 (Minor) | **Partially closed — R4-1** | Operative step page §5.1/§5.2, `reference/cli.md`, and ADR 0015 decisions 2–3 are correct; the summaries listed in R4-1 still say library-owned help/version, and step page §11.1 still says version is a library action. |
| R3-5 (Note) | **Closed** | Step page §18.1 now says 2.75–3.75 pw; the old 3.0–4.0 figure survives only as the historical decision record in §2/§20/§21, the roadmap, and `05-open-decisions.md`. |
| R3-6 (Note) | **Partially closed — R4-2** | Descriptor and report rule agree in step page §11.2/§5.3, data contract §5.1/§5.7.6, `reference/cli.md`, and `05-open-decisions.md`; step page §11.2 `:878` still says parse errors are the only source of `CLI001`. |
| R3-7 (Note) | **Closed** | `reference/cli.md` carries the quoting/concatenation/empty-quote/line-ending/`#` rules and the 4 MiB wording; residual 10,000-token scope wording is R4-5. |
| R3-8 (Note) | **Partially closed — R4-3, R4-4** | Feature 1 owns `verify.sh aot` in `verify.sh all`; the required gate's toolchain/absence/CI policy and the smoke-host exit-2/positive-control details are not specified. |
| R2-2 (Note) | **Closed** | Step page §18.1 has one `docs/reference/diagnostics.md` bullet with the drift-test and link wording. |
| R2-4 (Note) | **Closed** | Step page §15 names `Mocassin.Packages.Build.Internal` for the builder; §16.1 places it in `Build/Internal`. |

#### Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **R4-1 — R3-4 residual: the library-default help/version wording survives in four summaries and one step-page sentence.** | `03-quality-strategy.md:453`; `05-open-decisions.md:75`; `design-decisions.md:406`; `02-data-contracts.md:1461`; step page §11.1 `:842`; ADR 0015:42. Contrast step page §5.1/§5.2, `reference/cli.md:21-23`, ADR 0015 decisions 2–3. |
| **Minor** | **R4-2 — R3-6 residual: step page §11.2 says parse errors are the "only source" of `CLI001`, contradicting the response-file `CLI001` scope in the same table and §5.3.** | Step page `:878`; contrast `:844` and `:865`; data contract §5.1/§5.7.6; `reference/cli.md:49-61`; `05-open-decisions.md:80`. |
| **Minor** | **R4-3 — R3-8 residual: the required `verify.sh aot` gate has no documented toolchain, absence, or CI policy; `verify.sh all --require` can fail on a documented Tier 1 environment or skip, and the closeout CI does not run `tools/verify.sh`.** | Step page §5.2/§19/§22/§23/§24; `tools/verify.sh` (no `aot` mode; `--require` gates only `cargo-deny`); `06-toolchain-requirements.md:68` (clang/lld/llvm Optional, Tier 2, Steps 19/29/31; zlib1g-dev Recommended Steps 29/31); `.github/workflows/ci.yml` (dotnet job: restore/build/test/format only); `ci-policy.md:53`. |
| **Minor** | **R4-4 — R3-8 residual: the AOT smoke test expects the native minimal host to report a parse error with exit 2 (the CLI remap), while the library default is 1; the positive control's separation from the zero-warning publish is unstated.** | Step page §23 `:1781`; §5.2 `:268-277`; the AOT probe prints `1.0.0` for `--version` and returns 1 for `--bogus`; `03-quality-strategy.md:467-470`. |
| **Note** | **R4-5 — The 10,000-token limit's scope is worded differently across the contract pages.** | Step page §5.3 `:334` ("10,000 tokens contributed by response files"); `reference/cli.md:60` and ADR 0015:32 ("10,000 expanded tokens"); data contract `:1475`, `05-open-decisions.md:76`, and `design-decisions.md:416` ("10,000 tokens"). |
| **Note** | **R4-6 — The unterminated-quote "keep" rule is not pinned to an exact token, so the golden test is ambiguous for text before an unterminated quote.** | Step page §5.3 `:326-329`; §23 `:1767` ("unterminated-quote discard/keep rule"); observed `x"y z` → `xy z`, `"x y` → discarded. |
| **Note** | **R4-7 — The step-page status line still says "a round-3 design re-review is pending" after round 3 is complete.** | Step page `:12-14`; contrast the `Design review` bullet `:15-19` (round 4 reviews `d6c5402`). |

#### Finding details

**R4-1.** The decision is clear in the operative sections, but the decision register and developer summary still
say the library owns help/version (`05-open-decisions.md:75`, `design-decisions.md:406`), the quality strategy
says the library supplies help/version (`03-quality-strategy.md:453`), the data contract says it supplies
"help/version actions" and never records the root-only/`CLI001` rule (`02-data-contracts.md:1461`), step page
§11.1 says "Help and version are library actions" (`:842`), and ADR 0015's positive consequence says
"help/version are not reimplemented" (`:42`) although decision 2 makes the CLI supply the root version action.
An implementor reading the register could leave the library default, print `1.0.0` instead of the informational
version/runtime description, and fail the step-page §22 acceptance test. Change the summaries to "the library
supplies help; the CLI supplies the root `--version` action" (and the data contract to record the root-only
`CLI001`/2 rule), or narrow ADR 0015's consequence. Minor; docs-only.

**R4-2.** `CLI001` is defined as "library parse error, ... or a response file that is missing, unreadable,
invalid, or over its size/nesting/token limit" in step page §11.2 `:865`, and §5.3/data contract/`reference/cli.md`
make response-file failures `CLI001`/2. The following sentence, "`System.CommandLine` parse errors are the only
source of `CLI001`" (`:878`), is false. Change it to name both sources (or delete "only"). Minor; docs-only.

**R4-3.** The design's closeout gate is `./tools/verify.sh all --require`, and feature 1 adds `aot` to `all`.
The current `verify.sh` has no `aot` mode; its `--require` branch only fails when `cargo-deny` is absent, and
the other checks run unconditionally. NativeAOT needs clang/lld (and the ILCompiler package/network on a fresh
cache); `06-toolchain-requirements.md:68` still calls clang/lld/llvm Optional and Tier 2 for Steps 19/29/31.
The design does not say what `aot` does when those are absent. If it runs unconditionally, `all --require` fails
on a documented Tier 1 environment; if it skips, the required gate is silently skipped, which `ci-policy.md:53`
forbids. The closeout CI job does not invoke `tools/verify.sh`, so the aot gate is local-only and the closeout
CI cannot confirm it. Make the NativeAOT prerequisites required for Step 12 (and update
`06-toolchain-requirements.md`/the image), define fail-not-skip with `--require`, and state that the closeout CI
does not run the local gate (or add an aot job). Minor, but a finding because it can make the required gate
unrunnable or silently skipped.

**R4-4.** The smoke host is described as "minimal" and only references `System.CommandLine`; the probe shows the
library's default parse error exits 1, while the AOT test row expects exit 2 (`:1781`). To satisfy the row, the
host must include the `CLI001`/2 remap (a mini `CliApplication`) or the test must expect 1 and leave the remap to
`Mocassin.Cli.Tests`. Separately, the check asserts zero IL2xxx/IL3xxx warnings and also requires the positive
control to emit IL2026/IL3050; those cannot be the same publish, so the design should name the separate
positive-control project/configuration. Minor; test-plan precision.

**R4-5.** The step page's "10,000 tokens contributed by response files" is the clear scope. `reference/cli.md`
and ADR 0015 say "10,000 expanded tokens", which could include command-line tokens, and the data contract/
decision pages say "10,000 tokens". Pick one phrase (response-file tokens) in all six places. Note.

**R4-6.** §5.3 says "a token that had unquoted text before the quote is kept without the quote" and §23 says
"unterminated-quote discard/keep rule", but neither pins whether the rest of the line after the opening quote is
kept. The library's observed output is `x"y z` → `xy z` and `"x y` → discarded. Add those two golden cases so an
independent tokenizer cannot choose `x` only or treat the quote as an error. Note.

**R4-7.** The implementation-status bullet at `:12-14` still says "a round-3 design re-review is pending" while
the `Design review` bullet at `:15-19` says round 4 reviews `d6c5402`. Update the status line after round 4.
Note.

#### Verdict

**Ready with notes.** No Blocker or Major remains. R3-1, R3-2, R3-3, R3-5, R3-7, R2-2, and R2-4 are closed;
the R3-1 CLI-owned response-file design is internally consistent, enforces the specified bounds, and its golden
tokenizer cases match the library's observed `SplitCommandLine` behaviour. R3-4, R3-6, and R3-8 are only
partially resolved: their operative sections are correct, but the R4-1–R4-4 Minors and R4-5–R4-7 Notes remain.
None affects a cross-step interface or the start gate if the human owner accepts the notes; the owner may also
require the small documentation-only cleanup (and the R4-3 toolchain/CI statement) before the start gate.

Follow-up owner: step agent/planner for the documentation-only findings and the AOT gate statement; the human
owner approves the verdict and the start gate.

#### Round 4 resolutions

The step agent/planner fixed every round-4 finding in a documentation-only change. The reviewer's round-4
text, evidence, findings, verdict, and earlier rounds are unchanged. The new freeze is
`1b6af90c72a0c4ff77d50fd266e80738bd8490fb`.

| Finding | Status | Resolution |
| --- | --- | --- |
| R4-1 (Minor) | Fixed | Aligned the six stale help/version summaries: `03-quality-strategy.md` §9.10, `05-open-decisions.md` parser row, `design-decisions.md`, `02-data-contracts.md` §5.1, step page §11.1/§5.2/§16.2/§20, and ADR 0015 now say the library supplies help and the CLI supplies the root-only `--version` action; subcommand `--version` is `CLI001`/2. |
| R4-2 (Minor) | Fixed | Step page §11.2 now names both `CLI001` sources: library parse errors and response-file missing/unreadable/invalid/over-limit failures. |
| R4-3 (Minor) | Fixed | Step page §5.4 specifies the required local `verify.sh aot` gate: zero-warning host publish, separate positive-control publish, native `CLI001`/2 parse-error case, hard failure with a clear message when clang/lld/ILCompiler is absent (never a skip), and `all --require` treating it as required. `06-toolchain-requirements.md` makes clang/lld/llvm 19.1.7, ILCompiler 11.0.0, and zlib1g-dev Tier 1; feature 1 owns the mode/hosts and feature 7 runs `all --require` at closeout. `ci-policy.md`, `03-quality-strategy.md`, ADR 0015, `05-open-decisions.md`, and the step page state that `verify.sh` is local and the closeout GitHub workflow does not run it. |
| R4-4 (Minor) | Fixed | The smoke host includes a mini `CliApplication` that maps `ParseResult.Errors` to `CLI001`/2, so the native parse-error case exits 2. The zero-warning host and the positive control are separate projects/publishes; both are required, and the control must emit IL2026 and IL3050. Step page §5.4/§22/§23 and `03-quality-strategy.md`. |
| R4-5 (Note) | Fixed | All contract pages now use “10,000 tokens contributed by response files”: step page §5.3, `reference/cli.md`, ADR 0015, `02-data-contracts.md` §5.1, `05-open-decisions.md`, `design-decisions.md`, and `03-quality-strategy.md`. |
| R4-6 (Note) | Fixed | Step page §5.3 pins `x"y z` → `xy z` and `"x y` → no token; §23 and `reference/cli.md` carry the same exact cases. |
| R4-7 (Note) | Fixed | Step page status block now records round 4 `Ready with notes` and the round-5 freeze, not a pending round-3 review. |

### Review round 5

#### Independence and frozen state

I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
planner, implementor, orchestrator, or the round-1–4 reviewers. I received only the frozen commit
`1b6af90c72a0c4ff77d50fd266e80738bd8490fb`, the record HEAD
`908e497d2882536f2cbf170a606b2f92aa89592a`, the request record, repository links, and the required
commands/scope. No desired verdict or summary was supplied. I read the repository at the frozen commit, the full
record including rounds 1–4, every document the freeze changes, `tools/verify.sh`, `.github/workflows/ci.yml`,
`docs/opencode/mocassin.containerfile`, `Directory.Build.props`, and the `System.CommandLine` 3.0.0 and
`Microsoft.DotNet.ILCompiler` 11.0.0 packages; the review was read-only apart from this record, and I committed
only this file.

The design under review is `1b6af90`. The freeze commit changes 11 documentation files (+162/−95) and no
`src/`, `tests/`, `contracts/`, `tools/`, or configuration file:
`git diff --name-only 1b6af90^ 1b6af90`. The record HEAD `908e497` is documentation/status-only on top of it:
`git diff --name-status 1b6af90 908e497` returns `M docs/planning/reviews/step-12.md`,
`M docs/planning/status.md`, and `M docs/planning/steps/step-12-build-orchestration-and-cli.md`; the step-page
hunk changes only the `Design review` pointer (4 lines), and the status dashboard updates the round pointer
(2 lines). No design text moved between the freeze and the record HEAD. Rounds 1–4 and the R2/R3/R4 reviewer text
are unchanged; the post-freeze record changes are the appended `Round 4 resolutions` and the
`Record state`/request pointer.

#### Commands and results

- `./tools/verify.sh policy --require` → exit 0. ApiCompat reported no breaking changes for 9 assemblies (its
  unresolved `MessagePack`/`Microsoft.Data.Sqlite` notices are not failures); NuGet licence/vulnerability policy
  OK; contract artifacts current (19 files); model/parameterization/lattice projections OK (5/4/2 fixtures);
  space-group catalog current (274 settings, sha256
  `1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395`); process consistency OK; 58 Python tool
  tests OK; SPDX headers in 603 source files; no secret patterns in 986 tracked text files; cargo-deny
  advisories/bans/licenses/sources OK; final line `Closeout gate completed with required checks enabled.`
- `./tools/verify.sh docs --require` → exit 0. Math OK (166 files); links OK (169 files); Zensical strict
  `No issues found`; DocFX `Build succeeded` with 0 warnings / 0 errors; rustdoc OK; site built.
- `python tools/check_doc_math.py` → `Documentation math conventions OK (166 files scanned).` (exit 0).
- Toolchain/image probes: `clang --version` → `Debian clang version 19.1.7 (3+b1)`; `ld.lld --version` →
  `Debian LLD 19.1.7`; `llvm-config --version` → `19.1.7`; `dpkg` shows `clang-19`, `lld-19`, `llvm-19`
  `1:19.1.7-3+b1`, and `zlib1g-dev 1:1.3.dfsg+really1.3.1-1+deb13u1`.
  `docs/opencode/mocassin.containerfile:55,57` installs `clang lld llvm` and `zlib1g-dev` from apt
  **without an explicit version pin**; it does not install `Microsoft.DotNet.ILCompiler`. The cache contains
  `microsoft.dotnet.ilcompiler/11.0.0` and `runtime.linux-x64.microsoft.dotnet.ilcompiler/11.0.0`, restored by
  the first AOT publish. So the design's "image already installs clang/lld/llvm 19.1.7" and
  "ILCompiler 11.0.0 restored per publish" are true in this image; the 19.1.7 pin itself is Debian's, not the
  containerfile's (R5-2).
- `System.CommandLine` 3.0.0 probe (throwaway project in `/tmp/opencode/step12-r5/probe`, no repository file
  created): `CommandLineParser.SplitCommandLine` returns `x"y z` → `xy z`, `"x y` → no token, `a"b c"d` →
  `ab cd`, `a""b` → `ab`, `"a""b"` → `a` + `b`, `""` → one empty token. The default parse error is
  `Unrecognized command or argument '--bogus'.` with exit **1**; root `--version` is present and exits 0; a
  subcommand `--version` is an error and no subcommand `VersionOption` exists;
  `ParserConfiguration.ResponseFileTokenReplacer` defaults non-null and can be set to `null`. These match
  §5.2/§5.3.
- AOT probes (throwaway projects under `/tmp/opencode/step12-r5`, no repository file created):
  - A minimal `System.CommandLine` host with a mini `CliApplication` mapping `ParseResult.Errors` to
    `CLI001` published with `PublishAot=true`, `TrimMode=full`, `TrimmerSingleWarn=false`,
    `SuppressTrimAnalysisWarnings=false`, `SelfContained=true`, and `TreatWarningsAsErrors=true` → exit 0 with
    zero IL2xxx/IL3xxx warnings; the native binary exits 0 for `--help` and `--version` and **2** for
    `--bogus` with a `CLI001` line. The host/remap design is feasible.
  - `JsonSerializer.Serialize(new object())` published with the same AOT settings (no warnings-as-errors
    override) → exit 0 and emits both `warning IL2026` and `warning IL3050`; with
    `-p:TreatWarningsAsErrors=true` → exit 1 with `Trim analysis error IL2026` and `AOT analysis error
    IL3050`. This is R5-1.
- Script/workflow inspection: `tools/verify.sh` has no `aot` mode or `run_aot` function; its `--require` flag is
  read only in the cargo-deny branch (`tools/verify.sh:171-178`); the `all` case runs `dotnet`, `api`, `rust`,
  `python`, `docs`, and `contracts` (`tools/verify.sh:182-188`). Adding `aot` to `all` therefore makes plain
  `all` run it as well as `all --require`; the design says `aot` is fail-not-skip without `--require`, so it is
  consistent, but "`all --require` treats it as required" is redundant (R5-3). `ci.yml` has no `verify.sh`
  reference; its `dotnet` job runs restore/build/test/format (`ci.yml:207-237`), so the design's local-gate
  statement is true.
- Diff/closure checks: `git diff --name-status 1b6af90 908e497` (above); `git diff --name-only 1b6af90^
  1b6af90` (11 docs); `git diff --name-only d6c5402 1b6af90` (docs only). R4-1–R4-7 were re-checked against the
  frozen text and the probes below.

#### Closure of R4-1–R4-7

| Finding | Round-5 status | Evidence verified in the frozen docs/repository |
| --- | --- | --- |
| R4-1 (Minor) | **Closed** | The six stale summaries now say the library supplies help and the CLI supplies the root-only `--version` action: `03-quality-strategy.md:453`; `05-open-decisions.md:75`; `design-decisions.md:406`; `02-data-contracts.md:1460-1464` (also records root-only and subcommand `CLI001`/2); step page `:875` ("Help is the library action … the CLI root `--version` action returns 0; a subcommand `--version` is `CLI001`/2"); ADR 0015 `:26-27,42`. No "library owns/supplies help/version" sentence remains. |
| R4-2 (Minor) | **Closed** | Step page §11.2 `:912-914` now says "`System.CommandLine` parse errors **and response-file failures** (missing, unreadable, invalid, or over-limit) are the sources of `CLI001`", agreeing with the response-file row in the same table and §5.3. The "only source" sentence is gone (no other occurrence in the frozen docs). |
| R4-3 (Minor) | **Closed; R5-2/R5-3 are new notes** | `06-toolchain-requirements.md:68,114` mark clang/lld/llvm **Required**, `:70` zlib1g-dev **Required**, and `:196,204-205` Tier 1 with the closeout rationale; no other document treats them as optional (grep over `docs/`). The image installs clang/lld/llvm 19.1.7 and zlib1g-dev; ILCompiler 11.0.0 is restored per publish. Step page §5.4 `:357-386` defines `verify.sh aot`: separate zero-warning host and positive-control publishes, native `CLI001`/2, fail-not-skip, feature 1 adds the mode/hosts, feature 7 runs `all --require`; `ci-policy.md:53-56` and `03-quality-strategy.md:464-473,517` repeat the local/CI distinction. `tools/verify.sh` currently has no `aot` and gates only cargo-deny with `--require`; adding `aot` to `all` makes it unconditional, which the design states. `ci.yml` does not run `verify.sh`; confirmed. |
| R4-4 (Minor) | **Closed; R5-1 is a new Minor** | Step page §5.4 `:366-373` and §23 `:1825` require the separate positive control and a native host whose mini `CliApplication` maps `ParseResult.Errors` to `CLI001`/2; the probe confirms the library default is 1 and a mini host can return 2, and the control emits IL2026/IL3050. The remaining warning-policy gap is R5-1. |
| R4-5 (Note) | **Closed** | One phrase, "10,000 tokens contributed by response files", is used in step page §5.3 `:336` and §23 `:1811`, `reference/cli.md:60-61`, ADR 0015 `:31-32`, `02-data-contracts.md:1475-1476`, `05-open-decisions.md:76`, `design-decisions.md:419`, and `03-quality-strategy.md:467`. No "expanded tokens" remains. |
| R4-6 (Note) | **Closed** | Step page §5.3 `:325-330` pins `"x y` → no token and `x"y z` → `xy z`; §23 `:1811` includes both in the golden-test list; `reference/cli.md:55-57` repeats both. The probe confirms `SplitCommandLine` produces those exact results. |
| R4-7 (Note) | **Closed** | The step-page status block at the freeze says round 4 returned `Ready with notes` and the final fixes are frozen for round 5; its `Design review` bullet records rounds 2–4 and "round 5 pending" (`docs/planning/steps/step-12-build-orchestration-and-cli.md:12-19`). The status dashboard row at the record HEAD says "round 4 `Ready with notes` … round 5 pending" (`docs/planning/status.md:62` at `908e497`); no text still says round 3 or 4 is pending. (The frozen `status.md` still had the round-4-pending wording; the metadata-only `908e497` corrects it, which the record HEAD permits.) |

#### New findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **R5-1 — The positive-control publish cannot emit the required IL2026/IL3050 *warnings* under the repository-wide `TreatWarningsAsErrors=true`; they become errors and `dotnet publish` exits 1.** | `Directory.Build.props:7`; step page §5.4 `:366-370` and §23 `:1825`; probe: same AOT publish of `JsonSerializer.Serialize(new object())` exits 0 with `warning IL2026`/`warning IL3050` when warnings are not errors, and exits 1 with `Trim analysis error IL2026`/`AOT analysis error IL3050` under `-p:TreatWarningsAsErrors=true`. |
| **Note** | **R5-2 — The 19.1.7 toolchain pin is not in the containerfile; it is the current Debian apt resolution. ILCompiler is a NuGet restore, not an image install. The exact version is checked only by the manual §9 checklist, not by the containerfile build or the `aot` gate (which hard-fails on absence only).** | `docs/opencode/mocassin.containerfile:55,57`; `dpkg`/`clang`/`ld.lld` probes (19.1.7); `06-toolchain-requirements.md:68,70,114,196,204-205,207,219-232`; step page §5.4 `:374-378`. |
| **Note** | **R5-3 — `aot` is in `verify.sh all`, the standard closeout command for every step, so the Tier 1 toolchain is required at every step closeout from Step 12; the toolchain tables say "Step 12 closeout, Steps 19, 29, 31" and the step page names only Step 12 and Step 31. The every-step consequence is implicit, not stated.** | `tools/verify.sh:182-188`; `AGENTS.md` §3.4; `06-toolchain-requirements.md:68,70,114`; step page §5.4 `:382-385`; `03-quality-strategy.md:517`. |

**R5-1.** The zero-warning host and the positive control correctly have opposite warning policies: the host must
be clean, while the control must produce warnings. The repository sets `TreatWarningsAsErrors=true`
(`Directory.Build.props:7`), and `tests/aot/PositiveControl` is under the repository root, so it inherits that
property. The probe shows the same call produces `warning IL2026` and `warning IL3050` with exit 0 when
warnings are not errors, but produces `Trim analysis error IL2026` and `AOT analysis error IL3050` and exits 1
with `-p:TreatWarningsAsErrors=true`. The design does not state that the control opts out (for example
`-p:TreatWarningsAsErrors=false` or `<WarningsNotAsErrors>$(WarningsNotAsErrors);IL2026;IL3050</WarningsNotAsErrors>`
in the control project). As written, the required `verify.sh aot` positive-control publish fails and the check
cannot observe the warnings it requires. Fix the project/command warning policy in §5.4/§23 (and the control
csproj) while keeping the host strict. Minor; docs/test-plan precision, not a start blocker.

**R5-2.** The current image genuinely has `clang`/`lld`/`llvm` 19.1.7 and `zlib1g-dev`; the containerfile
installs them from apt without a version pin, and `Microsoft.DotNet.ILCompiler` 11.0.0 is restored by the first
AOT publish (the cache has `microsoft.dotnet.ilcompiler/11.0.0`). A rebuild against a different apt snapshot
could change the compiler version, and `verify.sh aot` as specified fails only on **absence**, not a version
mismatch; only the manual `06` §9 checklist asserts 19.1.7. Note; pin/assert the version in the containerfile
or the `aot` gate if reproducibility at 19.1.7 is required.

**R5-3.** Because `aot` joins `verify.sh all`, and `all --require` is the closeout gate for every step
(`AGENTS.md` §3.4), the AOT toolchain is needed at every step closeout from Step 12 onward, not only the
"Step 12 closeout, Steps 19, 29, 31" entries. The design should say that explicitly (or list "every step
closeout from Step 12" in the toolchain tables). The owner's "keep the gate required" decision is otherwise
recorded consistently in `05-open-decisions.md` (CLI AOT closeout gate row), `06`, `ci-policy.md`,
`03-quality-strategy.md`, ADR 0015, and step page §5.4.

#### Verdict

**Ready with notes.** No Blocker or Major remains. R4-1–R4-7 are closed: the six help/version summaries, the
`CLI001` source sentence, the toolchain/Tier 1 table, the separate positive control, the single 10,000-token
wording, the two pinned tokenizer cases, and the status text all match the frozen design and the repository.
The required `verify.sh aot` contract is implementable and honest about `--require`, feature ownership, and the
closeout CI (which does not run `verify.sh`); the current script gates only cargo-deny and has no `aot` mode,
and feature 1 adds it. One Minor (R5-1: the positive-control warnings become errors under the repo's
warnings-as-errors policy) and two Notes (R5-2: 19.1.7 is not pinned in the containerfile; R5-3: `aot` in `all`
makes the toolchain required at every step closeout) remain. None affects a cross-step interface or the start
gate if the human owner accepts them; the owner may also ask for the small docs-only R5-1/R5-2 clarifications
before the start gate.

Follow-up owner: step agent/planner for the R5-1 Minor and the R5-2/R5-3 Notes; the human owner approves the
verdict and the start gate.
