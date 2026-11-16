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
| Reviewed commit | `a6cf22aafbaa3ea3272bf6665bb7eac990b88e63` |
| Docs branch | `docs/step-12-design` |
| Review date | 2026-11-16 |
| Reviewer | — |

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

- [ ] Step page design decisions — goal, scope, non-goals, the pipeline and event stream, selection,
      job selection, output/atomicity, progress, errors/exit codes, JSON contracts, commands, cancellation,
      dependency direction, layout, contract artifacts, tests, documentation, and "Done when".
- [ ] Data contracts and format changes — `02-data-contracts.md` §3.2 (`job_selection` normal form), §5.1
      (command surface), §5.5 (`build.stage` and optional build-event fields), §5.7 (the seven report
      descriptors, `build-v1`, exit codes), and §6 (fixtures); the `reports`/`events` manifest additions and
      generated artifacts.
- [ ] Architecture and module boundaries — `01-target-architecture.md` §3/§3.1 (the `Mocassin.Packages.Build`
      home, the explicit `Cli -> Compilation` read-only edge, no `Packages -> Results` edge) and §7.3 (the
      corrected CLI exit codes).
- [ ] ADRs and open decisions — ADR 0009 (manifest and generated checks), ADR 0010 (signatures; no signature
      change), ADR 0011 (immutable package), and the new resolved/gated rows in `05-open-decisions.md`.
- [ ] Quality strategy and test plan — `03-quality-strategy.md` §5, §6, §8, §9.10, and §13; the Step 12 test
      table with tiers and the real-file/real-SQLite policy.
- [ ] Parity matrix and documentation plan — the Step 12 rows and the pages in `docs/reference`, `docs/user-guide`,
      `docs/getting-started`, `docs/troubleshooting`, `docs/development`, and this planning directory.
- [ ] Licensing and provenance — no new third-party package; `JsonSchema.Net` is only added to an existing
      test project; the embedded ceria template is MIT-authored project data and is freshness-checked.
- [ ] Post-implementation audit plan — implementation-status block, feature log, closeout evidence, and the
      audit request sequence.

### Step-specific decisions

- [ ] **Parser.** Hand-written, table-driven parser over `System.CommandLine`; one path, generated
      help/completions, no new dependency. Verify the option grammar, `--` handling, and usage-error reports.
- [ ] **Build orchestration home.** `Mocassin.Packages.Build`, consumed by CLI and GUI; `Mocassin.Packages`
      does not reference `Mocassin.Results`; post-publication verification is caller-composed. Verify no
      project-graph contradiction.
- [ ] **Pipeline order.** Load → select → validate stages 1–4 → compile → build-stage validation with the
      compiled model → parameterize → expand → generate/write → publish; the pre-compile validation gives
      model errors exit 1, and the post-compile call is required by `JobTemplateValidationStage`.
- [ ] **Event stream.** The in-process `BuildEvent` types, the bounded channel/backpressure, progress
      throttling, `build.started`/`build.stage`/`build.progress`/`build.finished` mapping, and the
      guarantee that `build.finished` is emitted once.
- [ ] **Selection.** `--template` = build template, `--job-template` = job template, `--parameterization`
      override; auto-selection, ambiguity, wrong-kind IDs, unknown IDs, implicit default, and output-name
      resolution.
- [ ] **Job selection.** Grammar, canonical normalization, full-expansion IDs, gaps, `all`, `inspect`
      visibility, and the writer’s existing `job_selection` metadata.
- [ ] **Output and atomicity.** `--force`, the writer’s temp/rename barrier, pre-publication failures,
      post-publication cancellation, `--compact` = quality 9 + VACUUM, and the `compile -o` replacement rule.
- [ ] **Progress/quiet.** Human progress on stderr, final summary on stdout, JSONL on stderr, ETA from a
      rolling window, and `--quiet` suppression without hiding diagnostics.
- [ ] **Error model.** The 0/1/2/3/4/5/6/10 table, exit 5 being reserved, the `--warnings-as-errors` scope,
      and the `CLI001`–`CLI009` descriptors in Contracts.
- [ ] **Build-result identity and failure mapping.** `BuildIdentity` supplies the report’s project/model/
      document hashes and signatures; `Io` is separate from `Load`; the documented table maps every
      `BuildResult` to an exit code; cancellation is a library status with `CLI006` composed by the CLI; and
      `build-v1` carries no package-size estimate. Verify the report fields and exit codes agree.
- [ ] **JSON reports.** The seven `reports` descriptors, the shared envelope, `build-v1` fields, the
      replacement of `build-preview-v1`, the usage-error nullability rule, and the one-object/one-exit-code
      guarantee.
- [ ] **Commands.** `new` (embedded templates, fresh identity, safe create), `compile` (`-o` inspection,
      `compile-v1`), `inspect`/`verify` wrappers, `doctor` checks and solver warning, `clean --dry-run`
      scope, completions, and reserved commands.
- [ ] **Cancellation.** Pre-publication temp cleanup, existing-package preservation, post-publication
      behaviour, signal handling, and the second-signal rule.
- [ ] **Layout.** `Mocassin.Packages/Build`, the CLI command-domain folders, `Mocassin.Contracts/Cli`, test
      folder mirrors, and the 20-file/800-line/depth limits.
- [ ] **Contract artifacts.** The `reports`/`events` manifest additions, `REPORT_CONSTANTS`, generated
      schemas/constants/descriptors, `events-v1.jsonl`, and the `verify.sh contracts` freshness check.
- [ ] **Sizing and feature list.** Seven features, 3.0–4.0 pw, the 2.25 pw contingency, and the owner
      decisions in §21.

## Dependency and interface checks

- [ ] Dependencies 10 and 11 are `Done` and their shipped contracts are stable — `JobTemplate`/`JobDescriptor`,
      `JobTemplateExpander`, `SupercellGenerator`, `GeneratedLattice`, `JobPackage`,
      `ISimulationPackageWriter`, `PackageBuildRequest`/`Options`/`WriteResult`,
      `IMslPackageInspector`, `IPackageVerifier`, `DiagnosticCatalog`, `ProjectValidationPipeline`,
      `IModelCompiler`, and `IParameterizationService`.
- [ ] Inputs, outputs, types, and formats are defined — the pipeline request/result/event types, the
      `build-v1`/`compile-v1`/`inspect-v1`/`verify-v1`/`doctor-v1`/`clean-v1`/`new-project-v1` payloads, the
      `job_selection` normal form, and the JSON-Lines event fields.
- [ ] Versioning and migration behavior is defined — report `schemaVersion = 1`, additive event changes,
      `build-preview-v1` retirement, no package/payload/signature change, and the Step 08/09/11 version
      domains.
- [ ] No unresolved cross-step interface question remains — Step 13’s reader/`job_selection`, Step 16’s job
      flags, Step 19/20/21/28 reserved commands, Step 23’s creation UI, Step 26’s build UI, and Step 30’s
      report registration are either defined or explicitly deferred.
- [ ] The CLI/GUI split is real — the GUI can call `Mocassin.Packages.Build` and the verifier without
      referencing `Mocassin.Cli`; the CLI owns only parsing, formatting, and exit-code policy.
- [ ] The diagnostics composition is acyclic — `CLI` descriptors live in Contracts, `Packages` composes them,
      the CLI constructs them, and `Results` keeps using the shared descriptors.
- [ ] `Mocassin.Packages` can build without `Mocassin.Results` — the writer, generator, parameterizer,
      expander, and compiler are in its reference closure; verification is caller-composed.
- [ ] The command surface matches the closed-step contracts — `build-preview-v1`, `--template`, `--compact`,
      `clean`, and the Step 11 writer options are reconciled.

## Standard design-review checklist

- [ ] Scope, non-goals, and acceptance criteria are explicit and testable
- [ ] Design decisions are complete and internally consistent
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined
- [ ] Documentation, status, parity, and audit updates are planned
- [ ] Licensing, provenance, and dependency checks are resolved
- [ ] Risks and mitigations are realistic
- [ ] Code layout and namespace plan follow the structure policy
- [ ] No hidden scope, stale placeholders, or unowned decisions

## Evidence

Files reviewed:

Commands run:

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Ready for review

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Waived

Follow-up owner:
Follow-up step:

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

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

#### Commands and results

#### Findings

#### Verdict
