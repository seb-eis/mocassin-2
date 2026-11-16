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
| Reviewed commit | `05b7297172f9f87453b3ffc23d06801035da7a2c` |
| Docs branch | `docs/step-12-design` |
| Review date | 2026-11-16 |
| Reviewer | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh context, no shared conversation |

Step page: [`Step 12 — Build Orchestration and mocassin CLI`](../steps/step-12-build-orchestration-and-cli.md)

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
| Delivers | A headless `mocassin` CLI and the shared `Mocassin.Packages.Build` orchestration: create/validate/compile/build/preview/inspect/verify/doctor/clean-dry-run, build-template and parameterization selection, partial `--jobs` builds with `job_selection` metadata, atomic replacement, progress/cancellation, JSON reports and JSON-Lines events, `CLI` diagnostics, and the `build-v1` report that supersedes `build-preview-v1` |
| Sizing | 3.0–4.0 person-weeks, seven features; roadmap §3.1 says 1–3 pw. The request presents the delta and a 2.25 pw contingency, and asks the owner to accept the estimate without a split |

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
- [x] Licensing and provenance — no new third-party package; `JsonSchema.Net` is only added to an existing
      test project; the embedded ceria template is MIT-authored project data and is freshness-checked.
      — Reviewed; `JsonSchema.Net` 9.6.0 is centrally versioned, MIT-compatible, and already accepted by the
      licence policy; the embedded templates are repository-authored JSON.
- [x] Post-implementation audit plan — implementation-status block, feature log, closeout evidence, and the
      audit request sequence. — Reviewed; the standard Step 11/12 sequence is present and the closeout is a
      dedicated feature.

### Step-specific decisions

- [x] **Parser.** Hand-written, table-driven parser over `System.CommandLine`; one path, generated
      help/completions, no new dependency. Verify the option grammar, `--` handling, and usage-error reports.
      — Reviewed; the command table and grammar are defined; n-8 records the ambiguous `-`-value rule.
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
      scope, completions, and reserved commands. — Reviewed; m-5, m-9, n-3, n-4.
- [x] **Cancellation.** Pre-publication temp cleanup, existing-package preservation, post-publication
      behaviour, signal handling, and the second-signal rule. — Reviewed; consistent with the Step 11 writer;
      m-4 records that validation does not receive the token.
- [x] **Layout.** `Mocassin.Packages/Build`, the CLI command-domain folders, `Mocassin.Contracts/Cli`, test
      folder mirrors, and the 20-file/800-line/depth limits. — Reviewed; the proposed folders satisfy
      `tools/structure-policy.json`; n-9 records the missing template factory/templates public API.
- [x] **Contract artifacts.** The `reports`/`events` manifest additions, `REPORT_CONSTANTS`, generated
      schemas/constants/descriptors, `events-v1.jsonl`, and the `verify.sh contracts` freshness check.
      — Reviewed; `generate.py` supports additive report/event descriptors and pins `manifestVersion = 1`.
- [x] **Sizing and feature list.** Seven features, 3.0–4.0 pw, the 2.25 pw contingency, and the owner
      decisions in §21. — Reviewed; m-7 records the missing decision-register row and the stale roadmap row.

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

**State:** Re-review required — round 1 audited the design freeze
`80b3f7fd99f0140b1dad5650851d1aaa3c4ac0b8`; the request tip
`0d466e8d7f78b1da19e56130d570140105bc429f` adds only that record. Round 1 found one Major and nine Minor
findings and no Blocker. The step agent resolved them at `05b7297172f9f87453b3ffc23d06801035da7a2c`; a fresh
reviewer re-reviews that commit.

`Re-review required` is a record state, not a verdict.

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
- [ ] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by:
Date:

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
