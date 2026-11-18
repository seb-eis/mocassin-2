# Step 12 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-12.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 12 — Build Orchestration and `mocassin` CLI |
| Request owner | Step agent (implementor) |
| Orchestrator | — |
| Step agent (implementor) | Step agent (implementor) |
| Audit initiated by | Orchestrator, on request of the step agent |
| Step branch | `step/12-build-orchestration-and-cli` |
| Closeout evidence | `docs/planning/closeout/step-12.json` |
| Audit date | 2026-11-18 |
| Auditor | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh context, no shared conversation with the planner, implementor, reviewers, or orchestrator |

The closeout evidence file holds the audited commit, closeout CI run and `head_sha`, `main` SHA at freeze, merge candidate,
merge-tree CI, platform CI, and tag. Do not restate them here: one source, one place.

Step page: [`Step 12 — Build Orchestration and mocassin CLI`](../steps/step-12-build-orchestration-and-cli.md)

> Orchestration: the orchestrator spawns a fresh auditor when the step agent has frozen the step branch and
> completed the audit request, and relays the verdict and owner decisions. The step agent applies fixes on a new
> feature branch, reruns closeout CI, adds resolutions without altering reviewer text, and asks the orchestrator
> for re-audit until the auditor no longer identifies issues. The orchestrator does not fix findings or edit the
> audit record. The human owner approves the final verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 10, 11 |
| Delivers | A headless `mocassin` CLI and the shared `Mocassin.Packages.Build` orchestration library: recipe/job/parameterization selection, job expansion and normalization, partial selections with stable full-expansion IDs, preview, progress and cancellation, atomic package publication, post-publication verification, and the `new`, `validate`, `compile`, `build`, `inspect`, `verify`, `doctor`, and `clean --dry-run` command surface with seven manifest-owned reports. |

The step page records deviations 1–16. The design review is
[`step-12`](../reviews/step-12.md) (`Ready with notes`, accepted by the human owner 2026-11-17 at `1b6af90`).

## Step-specific acceptance checklist

Copy the acceptance criteria from the step page, preserving their wording:

- [x] `mocassin validate samples/ceria/ceria.mocprj` exits 0 with the expected warnings; the human table and
      `validation-v1` report are unchanged from Step 07. — verified: exit 0, 0 errors / 0 warnings / 0 infos; the
      human table prints the `SEVERITY  CODE  MESSAGE` shape and the `ValidateCommandTests` regression tests pass.
- [x] `mocassin build samples/jobs/ceria-temperature-sweep.mocprj -o /tmp/ceria.msl` produces a package that
      `mocassin verify` accepts, using the implicit default parameterization (`parameterizations: []`), and
      writes `job_selection = "all"`. The ceria model-definition sample has no job template and is not a
      build sample. — verified: exit 0, `jobSelection = all`, 6 jobs, 98,304 bytes, `verification = passed`;
      `verify` exit 0 with 10 checks/0 diagnostics; `inspect` reports `job_selection = all`.
- [x] `--jobs 1-3` writes exactly jobs 1–3 with full-expansion IDs and `job_selection = "1-3"`; `--jobs 2,5`
      writes exactly those IDs with a gap and the same IDs in a full build. — verified: `--jobs 1-3` reports
      `1-3`/3 jobs; `--jobs 2,5` reports `2,5`/2 jobs; the partial rows match the full build's lattice signature,
      configuration, and seed for job IDs 2 and 5, and `inspect` reports `2,5`.
- [x] `--jobs` normalizes equivalently (`1,2,3` = `1-3`; a full selection = `all`) and rejects malformed,
      zero, descending, and out-of-range selections with exit 2. — verified: `1,2,3` reports `1-3`; `1-6` on the
      6-job sample reports `all`; `0`, `-1`, `3-1`, `99`, and `1,,2` each exit 2 with `CLI005` and no package.
      (The step page §8.2 "pre-validates before loading" sentence is not implemented; finding F-3.)
- [x] Build/job-template/parameterization selection auto-selects a single recipe, lists IDs on ambiguity,
      honours explicit overrides, and uses the implicit default when no template exists. — verified by the
      `BuildSelectionResolverTests` and the CLI error-path tests: single job template auto-selected; unknown IDs
      are `PROJ010`/1; wrong-kind and ambiguous selections are `CLI004`/2; `--job-template` and
      `--parameterization` overrides are reported; the sample reports `implicitDefault = true`.
- [x] `--force` atomically replaces an existing package; without it, an existing target exits 3 and the
      original file is byte-identical. — verified: existing target exit 3 with `CLI003` and an unchanged
      SHA-256; `--force` exit 0 with a new published package and the same target path.
- [x] Cancelling before publication leaves no temp file and preserves an existing package; cancelling after
      publication leaves the package and exits 6. — verified by SIGINT on a 10,000-job `--force` build: at 0.8 s
      exit 6, `CLI006`, `package = null`, existing package SHA-256 unchanged, no `*.tmp-*`; at 3.0/3.2 s (during
      verification) exit 6, `published = true`, `verification = skipped`, package retained. The pre-publication
      path has an automated real-writer test; the post-publication path has no automated test (finding F-6).
- [x] `--json` prints exactly one schema-valid report with `exitCode` for `validate` and the seven
      report-backed commands, including usage errors; help/version and reserved commands emit no report;
      `--json` leaves stderr JSONL-only; JSON-Lines events validate against the `events` contract; `--quiet`
      additionally suppresses stage/progress/log events. — verified: every report-backed command with a usage
      error printed one schema-shaped object and exit 2; `mocassin simulate --json` and an unknown command
      printed no stdout; build stderr was JSONL only; `--quiet --json` emitted only `diagnostic`,
      `build.started`, and one `build.finished`; the generated-schema tests pass.
- [x] `mocassin --help`, `mocassin <command> --help`, and `mocassin --version` exit 0; `mocassin --version`
      prints the assembly informational version and the runtime description from the CLI action, and
      `mocassin <command> --version` is `CLI001`/2; a parse error exits
      2 with `CLI001` and never exposes the library's default 1; `--jobs -1`, `--parallel -1`, and
      `-o -file` reach the handlers as values; `--` ends option parsing. — verified: root help/build help exit 0;
      root version printed `mocassin 1.0.0+886e1ac…` and `Runtime: .NET 11.0.0 (linux-x64)`; subcommand version
      exit 2 with `CLI001`; unknown option/command exit 2 with `CLI001`; `--jobs -1`/`--parallel -1`/`--parallel 0`
      are exit 2 with `CLI005`/`CLI001`; `validate -- -nodash.mocprj` reaches the handler and exits 3.
- [x] `@file` response files follow §5.3: quoting/whitespace and `#` comment rules, recursive references,
      cwd-relative paths, `@` literal and no escape, `@file` before `--` expanded and after `--` literal,
      `--input=@file` literal, `--input @file` token-expanded before arity, `--json` inside a file honored,
      and the §5.3 limits (1 MiB/4 MiB raw bytes, depth 8, 10,000 tokens contributed by response files,
      32 files) enforced. Missing, unreadable, invalid, or
      oversized files exit 2 with `CLI001`, not `CLI002`/3. A `--json` on the command line produces the
      command report for an expansion failure only when a command is recognised outside the failed file
      (`mocassin <command> @missing --json`); `mocassin @missing --json` has no recognised command and emits
      no report. — verified: all four limits and the missing/directory/invalid-UTF-8 cases exit 2 with
      `CLI001`; `@missing --json` emits no stdout; `build @missing --json` emits one build report. A file that
      successfully contributes a command before a nested reference fails still emits a report, contrary to the
      "outside the failed file" rule (finding F-4).
- [x] `./tools/verify.sh aot` passes: the smoke host publishes for `linux-x64` with `PublishAot=true` and
      `TrimMode=full` with zero IL2xxx/IL3xxx warnings, the separate positive control emits IL2026 and IL3050,
      and the native binary runs `--help`, `--version`, and one parse-error case that exits 2 through the
      host's `CLI001` remap. Missing clang/lld/ILCompiler is a hard failure with a clear message, never a
      skip. Feature 7 runs `./tools/verify.sh all --require` at Step 12 closeout and records the output; the
      real `Mocassin.Cli` publish result is recorded as evidence; dependency-owned warnings are attributed to
      Step 31. The closeout GitHub workflow does not run `verify.sh`, so this is a local gate. — verified:
      `all --require` exit 0; smoke log has zero `warning IL…`; positive control log has IL2026 ×2 and IL3050
      ×2; native parse error exits 2 with `CLI001`; real CLI publish failed with 62 = 31 × IL2026 + 31 × IL3050,
      all from first-party `Mocassin.Core/Project/ProjectJson.cs` (see finding F-5 on the "dependency-owned"
      wording; Step 31 ownership is explicit).
- [x] `build --preview` writes no package, reports `mode = "preview"`, projects `BuildResult.Preview`, and
      exits with the build table. The same payload is available to a library caller (Step 26) without a
      reload, recompile, or CLI reference. — verified: exit 0, `mode = "preview"`, `package = null`,
      `verification = skipped`, `preview.totalJobs`/collections/configs match the typed payload, no `.msl` is
      created; `preview` timing is `BuildPreview.Elapsed`; `BuildPreviewEqualityTests` exercises the library
      payload directly.
- [x] `clean --json` without `--dry-run` emits a `clean-v1` report with `dryRun = false`, `removedCount = 0`,
      and `CLI008`, and deletes nothing. — verified: exit 2, `dryRun = false`, `removedCount = 0`, `CLI008`,
      read-only listing; `Job*` folders and the package remained; `--results` emits a minimal report.
- [x] `new --template blank` and `new --template ceria` create valid projects with fresh identity and seed;
      `compile -o` writes a `compiled-inspection-v1` document; `inspect`/`verify` read a real package. —
      verified: both templates create UUIDv7 identities and distinct 32-byte seeds, validate exit 0, and the
      ceria template's canonical model hash equals `samples/ceria/ceria.mocprj`; `compile -o` wrote a 67,205-byte
      `compiled-inspection-v1` with the ten stages; `inspect`/`verify` succeeded read-only on a real package.
- [x] `doctor` exits 0 on CI; a missing solver is a warning. `clean --dry-run` lists only and never deletes. —
      verified: `doctor --json` exit 0, `healthy = true`, seven checks in order, `solver = warn`; `clean`
      dry-run listed `Job00001`/`Job00002` with sizes and `--older-than 3` filtered by timestamp, deleting
      nothing; the source-level no-delete assertion and the package hash stayed intact.
- [x] `--parallel` 1 and 4 produce the same signatures and `package-summary-v1`; output order is ascending
      `JobId`. — verified: both exit 0 with identical model/document/parameterization signatures, job/lattice
      counts, reuse factor, and inspect package summary apart from `createdUtc`; the fixed-clock pipeline test
      asserts identical package bytes and ascending job rows.
- [x] The CLI builds and verifies 10⁴ jobs without the GUI within the Step 11 budget (< 10 min, < 2 GB). —
      reproduced with the synthetic 20-configuration × 500-job ceria project: exit 0 in 4.02 s wall time,
      153,768 KB peak RSS, 9,601,024-byte package, 9,998 lattices, reuse 1.0002, `verification = passed`, 7
      warnings; standalone `verify` exit 0 in 1.26 s. Well inside the budget.
- [x] Documentation written (§18.1). — the CLI/user-guide/troubleshooting/diagnostics/dev/planning pages are
      present and the strict docs build passes; accuracy findings F-2, F-3, F-4, and F-5 remain.

## Standard audit checklist

- [x] Required tests from the step page pass. — `./tools/verify.sh all --require` exit 0: 1,756 .NET tests
      (App 1, Contracts 37, Symmetry 223, Results 42, Core 685, Compilation 96, Cli 171, Packages 500,
      Architecture 1), 58 contract/policy tool tests, Rust, Python, docs, and AOT.
- [x] Public/protected XML documentation policy is followed. — Public API/ApiCompat passes; new public Build,
      template, and Contracts.Cli types are XML-documented; the CLI types are internal.
- [x] Documentation, status dashboard, feature log, and parity matrix are current. — the step page, feature
      log, `status.md` row (`In audit`, closeout run), and parity rows were updated; documentation accuracy
      findings F-2/F-4/F-5 and test-plan findings F-6/F-7 are recorded separately.
- [x] Closeout CI is green at the audited commit. — GitHub API run `44370563073` (workflow `CI`,
      `workflow_dispatch`) conclusion `success`, `head_sha = 9e1997d270ae79d8ad2cd2357529a08e158401fb`;
      jobs: Detect changes, Contracts and policy, .NET (ubuntu-latest), and Public API compatibility success;
      Rust and Python skipped by path filters.
- [x] Closeout evidence matches the audited commit and the merge candidate. — `python3
      tools/check_closeout_evidence.py docs/planning/closeout/step-12.json` exit 0; `audit_commit` =
      `closeout_ci.head_sha` = `merge_candidate` = `9e1997d`; `main_sha_at_freeze` = `9d4d2ef`; the
      `9e1997d..886e1ac` delta is exactly the four documentation files.
- [x] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded. — `main` and
      `origin/main` are still `9d4d2ef2fdc484daf6da73c14773b1767ee74183` and are ancestors of the audited
      commit; no merge-tree run is required.
- [ ] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code. — **Not satisfied; finding F-1 (Major).** Step 12 adds platform-specific code
      (`CliSignalHandler` uses `System.Runtime.InteropServices`/`PosixSignalRegistration` and branches on
      `OperatingSystem.IsWindows()`; `ProcessSolverProbe` chooses `mocassin-sim.exe` on Windows) and the step
      page §24 itself says "Windows platform gate at closeout", but the closeout evidence records
      `platform_ci: "not run"` with the inaccurate rationale "no new platform-specific code beyond the Step 11
      native fsync path". No Windows/macOS run exists on the step branch.
- [x] Format/version/fixture changes are documented and tested. — the seven new report schemas and additive
      `build.stage` event keep `schemaVersion = 1`/`manifestVersion = 1`; generated artifacts are current;
      `events-v1.jsonl` is the new fixture; no `.msl` format change.
- [x] Security, dependency, licence, and secret-scan gates pass. — NuGet licence/vulnerability audit, cargo-deny
      (advisories/bans/licenses/sources), SPDX headers in 703 files, and the secret scan (1,103 files) all
      passed.
- [x] Code structure policy passes; no new root-level or catch-all folders. — `Mocassin.Architecture.Tests`
      passes; `Mocassin.Packages/Build` has 12+3 files and `BuildPipeline.cs` is 760 lines; CLI folder counts and
      depth are within policy.
- [x] No unrelated changes or unapproved scope. — `main..9e1997d` contains only the seven feature squash
      commits and their docs; no solver, results-store, GUI, or legacy-format work; the writer/core/compilation
      projects are untouched.
- [x] Branch, tag, and status hygiene are correct. — feature branches are deleted after squash; the step branch
      is pushed; no tag exists; no merge into `main`; status/dashboard show `In audit`; only documentation-only
      commits follow the audited commit.

## Evidence

Commands run:

```text
Repository state
$ git rev-parse HEAD 9e1997d main
  886e1ace631fd7c0fb6cc63499812954c044c350
  9e1997d270ae79d8ad2cd2357529a08e158401fb
  9d4d2ef2fdc484daf6da73c14773b1767ee74183
$ git diff --name-status 9e1997d..886e1ac
  A docs/planning/audits/step-12.md
  A docs/planning/closeout/step-12.json
  M docs/planning/status.md
  M docs/planning/steps/step-12-build-orchestration-and-cli.md
$ git merge-base --is-ancestor main HEAD; echo $?
  0
$ git log --oneline main..9e1997d
  9e1997d step-12: record the full local gate [skip ci]
  5031fe6 step-12: record the feature log, acceptance evidence, and status [skip ci]
  7d17c22 step-12: document the command surface and close the parameterization-warning gap
  a2c6178 step-12: add doctor and the clean dry-run
  85d8d41 step-12: add the read-only inspect and verify commands
  e7564c0 step-12: add the project templates, new, and compile commands
  af7431b step-12: wire build and preview to the pipeline and build-v1
  1dc9eeb step-12: add the build orchestration core and preview builder
  517ae27 step-12: add the CLI shell, parser, response files, and report contracts
$ git diff --name-only main..9e1997d -- docs/
  docs/development/design-decisions.md, docs/performance/index.md, docs/planning/02-data-contracts.md,
  docs/planning/appendix-b-parity-matrix.md, docs/planning/status.md,
  docs/planning/steps/step-12-build-orchestration-and-cli.md, docs/reference/cli.md,
  docs/reference/diagnostics.md, docs/reference/index.md, docs/troubleshooting/errors.md,
  docs/user-guide/building.md, docs/user-guide/parameterization.md

Required gates
$ ./tools/verify.sh all --require
  exit 0. 1,756 .NET tests passed: App 1, Contracts 37, Symmetry 223, Results 42, Core 685, Compilation 96,
  Cli 171, Packages 500, Architecture 1. ApiCompat compatible; 58 contract/policy tool tests; Rust tests;
  Python pytest 3 passed; Zensical strict/DocFX/rustdoc; SPDX 703 files; secret scan 1,103 files; cargo-deny;
  AOT passed. `dotnet format --verify-no-changes` is part of the mode and exited 0.
$ python3 tools/check_closeout_evidence.py docs/planning/closeout/step-12.json
  Closeout evidence: OK (exit 0)
$ curl -s https://api.github.com/repos/seb-eis/mocassin-2/actions/runs/44370563073
  workflow CI, event workflow_dispatch, status completed, conclusion success,
  head_sha 9e1997d270ae79d8ad2cd2357529a08e158401fb, head_branch step/12-build-orchestration-and-cli.
  Jobs: Detect changes success, Contracts and policy success, .NET (ubuntu-latest) success,
  Public API compatibility success, Rust skipped, Python skipped.
$ curl -s .../actions/runs?branch=step/12-build-orchestration-and-cli
  total_count 1 (the Ubuntu closeout run above); no Windows/macOS dispatch exists.

Acceptance runs (Release CLI src/dotnet/Mocassin.Cli/bin/Release/net11.0/Mocassin.Cli.dll)
$ mocassin validate samples/ceria/ceria.mocprj
  exit 0; 0 errors / 0 warnings / 0 infos; human table unchanged.
$ mocassin build samples/jobs/ceria-temperature-sweep.mocprj -o /tmp/opencode/audit-accept/ceria-sample.msl --json
  exit 0; jobSelection {expression: all, expandedCount: 6, selectedCount: 6}; 6 jobs; 98,304 bytes;
  verification passed; timings load/select/validate/compile/validateBuild/parameterize/expand/generate/
  publish/verify; stderr JSONL only.
$ mocassin verify /tmp/opencode/audit-accept/ceria-sample.msl --json
  exit 0; valid; 10 checks; 0 diagnostics; package.jobSelection "all".
$ mocassin build ... --jobs 1-3|2,5|1,2,3|1-6 --json
  exit 0; expressions "1-3"/"2,5"/"1-3"/"all"; 3/2/3/6 job rows; partial IDs 2 and 5 match the full build's
  lattice signature, configuration, and seed.
$ mocassin build ... --jobs 0|-1|3-1|99|1,,2 --json
  exit 2 each; CLI005; no package written; timings show the pipeline reached expand (finding F-3).
$ mocassin build ... -o <existing file> [no --force]; then --force
  exit 3 + CLI003 + unchanged SHA-256; --force exit 0 with a new published package.
$ SIGINT at 0.8 s / 3.0 s / 3.2 s into a 10,000-job --force build
  0.8 s: exit 6, CLI006, package null, existing package SHA-256 unchanged, no *.tmp-*.
  3.0/3.2 s (verification): exit 6, published true, verification skipped, package retained, no *.tmp-*.
$ mocassin build ... --preview --json; build ... --preview -o x.msl --json
  exit 0, mode preview, package null, verification skipped, preview.totalJobs 6, timings end with preview;
  exit 2 + CLI001 for -o with preview, no file created.
$ mocassin clean <pkg> --json; clean <pkg> --dir <jobs> --dry-run --json; clean <pkg> --results x --json
  no dry-run: exit 2, dryRun false, removedCount 0, CLI008, read-only listing.
  dry-run: exit 0, Job00001/Job00002 listed with sizes, --older-than 3 filtered to Job00002, nothing deleted.
  --results: exit 2, CLI008, minimal report.
$ mocassin doctor --json
  exit 0; healthy true; checks runtime/sqlite/write-permission/disk-space/symmetry-catalog/project-templates/
  solver; solver warn.
$ mocassin new audittest --template blank -o <dir> --json; new audiceria --template ceria ...
  exit 0; fresh UUIDv7 and distinct 32-byte seed; both validate exit 0; ceria model hash equals
  samples/ceria/ceria.mocprj; second create exit 3 + CLI003; directory collision exit 3 + CLI002.
$ mocassin compile <project> -o inspection.json --json
  exit 0; 67,205-byte compiled-inspection-v1; stages resolve, structure, environments, pairs, clusters, rules,
  encoding, indexing, signing, report.
$ mocassin inspect <pkg> --json; verify <pkg> --json
  exit 0 both; inspect package.jobSelection "2,5"; verify 10 checks.
$ corrupt a lattice payload; wrong-signature package; non-SQLite file; missing file
  corrupt: inspect exit 0 (tolerant), verify exit 1 PKG004.
  wrong signature: inspect exit 0, verify exit 1 PKG005.
  non-SQLite: inspect exit 1 CLI009; verify exit 1 PKG001.
  missing: inspect/verify/clean exit 3 CLI002.
$ mocassin build ... --parallel 1 vs 4 --json
  exit 0 both; identical model/document/parameterization signatures, job/lattice counts, reuse factor and
  inspect summary apart from createdUtc; fixed-clock pipeline test asserts identical bytes and ascending rows.
$ mocassin build ... --compact --json; verify
  exit 0; signatures unchanged; verify valid.
$ response files: @build.rsp, @missing.rsp, @self.rsp, 33 files, 10,001 tokens, 1 MiB+ file, --jobs @file,
  --jobs=@file, multi-token file, @partial.rsp (build + nested @missing) with --json
  expansion works; limits and missing/directory/invalid UTF-8 exit 2 CLI001; @missing --json has empty
  stdout; build @missing --json emits one build report; the partial-file case emits a build report even though
  the command token came from the failed file (finding F-4).
$ mocassin --help|--version|build --help|build --version|bogus --json|no args|simulate --json
  help/version exit 0 with the informational version and runtime description; subcommand version exit 2 CLI001;
  unknown/no command exit 2 CLI001 with no report; reserved simulate exit 2 CLI008 with no report.
$ 10,000-job synthetic ceria project (20 configs × 500 jobs)
  exit 0 in 4.02 s wall, 153,768 KB peak RSS (getrusage RUSAGE_CHILDREN), 9,601,024-byte package, 9,998
  lattices, reuse 1.0002, 7 warnings, verification passed; standalone verify exit 0 in 1.26 s, 10 checks.
$ AOT log artifacts/aot/mocassin-cli.log
  31 error IL2026 + 31 error IL3050, all Mocassin.Core/Project/ProjectJson.cs (reflection JSON); smoke log
  0 warning IL2xxx/IL3xxx; control log 2 × IL2026 + 2 × IL3050; native parse error exit 2 CLI001.
```

Files inspected beyond the step page and record: `AGENTS.md` §3/§7/§8–§16, `docs/planning/audits/README.md`,
`docs/planning/closeout/README.md`, `docs/planning/reviews/step-12.md` (rounds 1–5), `docs/planning/02-data-contracts.md`,
`docs/planning/03-quality-strategy.md`, `docs/planning/01-target-architecture.md`,
`docs/development/code-structure.md`, `docs/reference/cli.md`, `docs/reference/diagnostics.md`,
`docs/troubleshooting/errors.md`, `docs/user-guide/building.md`, `docs/performance/index.md`,
`docs/planning/appendix-b-parity-matrix.md`, `contracts/manifest.json` and generated schemas,
`tools/verify.sh`, `tests/aot/**`, `tests/contracts/events-v1.jsonl`, `tests/contracts/project-parameterization-warning.mocprj`,
`src/dotnet/Mocassin.Cli/**`, `src/dotnet/Mocassin.Packages/Build/**`, `src/dotnet/Mocassin.Contracts/Cli/**`,
`src/dotnet/Mocassin.Packages/Projects/Templates/**`, and the new Package/Cli test suites.

Evidence file: `docs/planning/closeout/step-12.json`

## Known notes for the auditor

- **Scope.** Seven features are squash-merged into `step/12-build-orchestration-and-cli`; the feature log on the
  step page records each branch, squash commit, local gates, and the absence of per-feature CI. The step adds
  the shared build pipeline, the command graph, seven report schemas, and the auxiliary commands; it does not
  add the solver, results collection, evaluation, GUI build UI, or full `clean`.
- **10⁴-job evidence.** The workload is a synthetic project derived from
  `samples/jobs/ceria-temperature-sweep.mocprj` with 20 configurations × `500` jobs = 10,000 jobs (four
  configurations × 2,500 would exceed `JOB008`, which caps a configuration at 1,000 jobs). It built and
  verified in 3.76 s wall time with 151,820 KB peak RSS and a 9,625,600-byte package; standalone `verify` was
  valid in 1.21 s. The step page has the exact command, the performance page has the method and stage timings.
- **Warnings-as-errors gap closed.** `tests/contracts/project-parameterization-warning.mocprj` is a clean
  buildable ceria variant with no validation or build-stage warnings and a deliberately unset explicit
  parameterization. `BuildParameterizationWarningTests` pins warning-free validation, exit 1 for validation
  warnings on the sample, exit 4 with `PRM003`/`PRM006` for the fixture under `--warnings-as-errors`, and a
  successful unflagged build and verify of the same fixture. The earlier scripted-pipeline mapping test remains.
- **`inspect` tolerance is intended.** `inspect` reports what it can read; corrupt payloads and signature
  mismatches are `verify` findings, and only fatal structure failures are `CLI009`/1. The CLI reference and
  data contracts state this reconciliation.
- **Doctor and clean deviations.** `doctor` treats free space below 100 MiB as `warn`, and a throwing check as
  a recorded `fail`; `clean` without `--dry-run` still performs the read-only listing and adds `CLI008`, and
  the reserved `--results` emits a minimal report. Both delete nothing. Deviations 14–16 record the reasons.
- **Report contracts.** `verify-v1` has one `diagnostics[]` array (the shared envelope) and a `checks[]` list;
  `compile-v1.stages[]` names the ten compiler stages including `report`; `doctor-v1` names the seven checks.
  The data contracts and CLI reference were reconciled to the implemented code in feature 7.
- **AOT and platform gates.** `aot` is part of `all --require`; the required smoke host publishes with zero
  trim warnings, the positive control emits IL2026/IL3050, and the native parse error exits 2. The real
  `Mocassin.Cli` publish emits 62 IL2026/IL3050 diagnostics from `Mocassin.Core` and is recorded as evidence,
  not a gate; Step 31 owns the AOT/single-file decision. No Windows/macOS run is planned for this step because
  it adds no new platform-specific code beyond the Step 11 native fsync path; platform CI is `not run`.
- **Closeout CI policy.** The closeout dispatch is the only step-branch CI run. The GitHub workflow does not run
  `tools/verify.sh`; the local `all --require` output is the gate evidence, and the closeout CI confirms the
  same tree through the repository's CI jobs. Rust and Python jobs may be skipped by their path filters because
  this step changes neither.
- **Docs-only closeout delta.** The closeout evidence, this audit request, the feature log, and the dashboard
  status are documentation-only commits after the audited code commit; they contain no code, test, contract,
  or configuration change. The closeout evidence binds the audited commit, the merge candidate, and the `main`
  SHA at freeze.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **F-1 (Major)** | Platform CI is not recorded even though Step 12 adds platform-specific code, and the recorded rationale is false. `CliSignalHandler` uses `System.Runtime.InteropServices`/`PosixSignalRegistration` and branches on `OperatingSystem.IsWindows()`; `ProcessSolverProbe` selects `mocassin-sim.exe` on Windows. Step page §24 says "Windows platform gate at closeout" and §18.2 says a platform gate is required when cancellation/process/path behaviour is touched, but the closeout evidence has `platform_ci = "not run"` justified by "no new platform-specific code beyond the Step 11 native fsync path", and the audit request repeats that claim. The GitHub API shows only run `44370563073` (Ubuntu) on the step branch; no Windows/macOS dispatch exists. The focused Windows job would also not run `Mocassin.Cli.Tests`, so the signal path would remain unexercised even after a gate. Fix: run the Windows platform gate at `9e1997d` and record it, or replace the false rationale with an owner-approved residual-risk statement covering the guarded `Ctrl+C`/`SIGTERM` paths and the untested Windows CLI tests. | `src/dotnet/Mocassin.Cli/CommandLine/CliSignalHandler.cs:3,37-48`; `src/dotnet/Mocassin.Cli/Doctor/ProcessSolverProbe.cs:18`; step page §18.2/§24 lines 1574/1989; `docs/planning/closeout/step-12.json` `platform_ci`; audit request Known notes; GitHub API `actions/runs?branch=step/12-build-orchestration-and-cli` total_count 1; `.github/workflows/ci.yml` Windows job scope. |
| **F-2 (Minor)** | `build -o` path behaviour does not match the documentation. The CLI reference says an explicit target is checked before any load and "no build work starts"; a directory target actually runs load → … → generate → publish before failing with `CLI002`/3. The step page deviation 11 and the troubleshooting table say a missing parent directory is `CLI002`/3 for build, but `build -o missing-dir/x.msl` creates the directory and publishes successfully (exit 0). `new -o ""` is `CLI001`/2 while the CLI reference lists an invalid output path as `CLI002`/3. Fix the pre-check/refusal or the documentation. | `build ... -o targetdir --json` → exit 3, timings include generate/publish; `build ... -o /tmp/.../nonexistent-dir/x.msl --json` → exit 0 and the package exists; `new foo -o "" --json` → exit 2 with `CLI001`; `docs/reference/cli.md:88-89,168,215-219`; `docs/troubleshooting/errors.md:91-93`; step page deviation 11. |
| **F-3 (Minor)** | The step page §8.2 says the CLI "pre-validates the selection syntax before loading for a fast error", but the CLI has no `JobSelection.Parse` call; malformed selections are detected only after load, validation, compile, build-stage validation, parameterize, and expansion. Exit 2/`CLI005` and the no-package guarantee still hold; only the promised fast path is absent and is not recorded as a deviation. | grep for `JobSelection.Parse` finds only `BuildPipeline.cs:404`; `build ... --jobs 1,,2 --json` timings `load,select,validate,compile,validateBuild,parameterize,expand` then `CLI005`; step page line 856. |
| **F-4 (Minor)** | Response-file expansion emits a command report when the only command token came from the file that failed to expand, contradicting the contract "a report is produced only when a command is recognised outside the failed file". `mocassin @partial.rsp --json`, where `partial.rsp` contains `build` followed by `@missing-inside.rsp`, writes a 651-byte `build-v1` report; the CLI reference, data contract, and step page §5.3 say this case is human usage with no report (only `mocassin <command> @missing --json` should report). Exit code remains 2. | `src/dotnet/Mocassin.Cli/CommandLine/CliApplication.cs:314-320,382-396` concatenates partial expansion tokens; `docs/reference/cli.md:68-70`; step page §5.3; manual run. |
| **F-5 (Minor)** | The closeout evidence and several summaries call the real-CLI AOT diagnostics "dependency-owned" (pointing at `Microsoft.Data.Sqlite`/`MessagePack`), but the actual 62 diagnostics are first-party: 31 × `IL2026` + 31 × `IL3050`, all in `Mocassin.Core/Project/ProjectJson.cs` reflection-based JSON. Step page deviation 4 and the AOT log are accurate; the summary wording in `step-12.json` and step page §5.2/§22/§23/§24 is not. Step 31 ownership is explicit. | `artifacts/aot/mocassin-cli.log`; `docs/planning/closeout/step-12.json` note 2; step page lines 358-364, 1886, 1958, 1981; deviation 4. |
| **F-6 (Minor)** | The step page test table promises cancellation "before/after publication" and the inspect-tolerance behaviour is a documented contract, but neither has an automated regression test. The pipeline test covers pre-publication cancellation with a real writer; no test drives cancellation during post-publication verification, and no test asserts that a corrupt payload or wrong signature still inspects successfully (only the verifier's `PKG` tests exist). Both behaviours are correct in manual reproduction. | `BuildPipelineStreamTests.CancellationBeforePublication…`; CLI tests only `PreCancelledBuildMapsToExitSix…`; grep for `BuildStatus.Cancelled`/tolerance; manual post-publication SIGINT and corrupt-payload/wrong-signature runs. |
| **F-7 (Minor)** | The step page "Exit codes" test row claims "every `BuildFailureKind`/status maps to the documented code, including … `PackageWrite` without an I/O cause, and `None`/`Failed`", but no test exercises those mapper branches (nor the CLI mapping of `JobExpansion`, `LatticeGeneration`, or `Preview`); the branches are code-inspected only. Exit 5 is never emitted in code, but that is also not asserted by a test. | `src/dotnet/Mocassin.Cli/Build/BuildExitMapper.cs:77-145`; grep of `BuildFailureKind.` in tests; step page §23 line 1945. |
| **F-8 (Minor)** | Step page §15 says `Mocassin.Cli -> Mocassin.Compilation` "becomes an explicit project reference for `compile`", but `Mocassin.Cli.csproj` has no such reference; `CompileCommand` compiles against `Mocassin.Compilation` types through the transitive `Mocassin.Packages -> Mocassin.Compilation` edge. Layering is not violated, but the stated dependency graph is not the built one. | `src/dotnet/Mocassin.Cli/Mocassin.Cli.csproj`; `src/dotnet/Mocassin.Cli/Compile/CompileCommand.cs:8-9`; step page §15 lines 1330-1331. |
| **N-1 (Note)** | ETA is computed by `Mocassin.Cli.Build.BuildEtaEstimator` (8–64 sample window) rather than by the library `ProgressEstimator` named in step page §10/§16.1; the library class only throttles. The CLI's advisory `etaS`/human ETA is correct, but a Step 26 library consumer must compute its own estimate. | `src/dotnet/Mocassin.Packages/Build/ProgressEstimator.cs`; `src/dotnet/Mocassin.Cli/Build/BuildEtaEstimator.cs`; step page §10/§16.1. |
| **N-2 (Note)** | The `log` JSON-Lines event is documented and present in `events-v1.jsonl`, but no production code emits `BuildLog`; `--quiet` therefore suppresses a channel that is currently unused. This is consistent with "forwarded human log" as future/advisory, but no producer or test asserts a real `log` line. | `src/dotnet/Mocassin.Cli/Build/BuildEventWriter.cs:73-75`; grep for `new BuildLog`; `tests/contracts/events-v1.jsonl`. |
| **N-3 (Note)** | The closeout evidence records `merge_candidate` as the audited code commit (`9e1997d`), not the branch tip (`886e1ac`), following the Step 11 N-2 convention that a commit cannot contain its own SHA. The `9e1997d..886e1ac` delta is exactly four documentation files and the checker passes, so the evidence is internally consistent; the actual later merge candidate also includes this record commit and the step agent's resolution commit. | `docs/planning/closeout/step-12.json`; `docs/planning/closeout/step-11.json` note; `git diff --name-status 9e1997d..886e1ac`. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** Fixes required

`Re-audit required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

Confirmed: I was spawned by the orchestrator for this audit only; I did not contribute to the Step 12 design,
review rounds, implementation, feature squashes, or closeout. I inspected the repository at the frozen commits
and ran the gates and acceptance commands myself.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [x] Fixes required
- [ ] Waived

Follow-up owner: Step agent (implementor) — resolve F-1 (Major) and the Minor/Note findings;
a fresh or continuing auditor re-audits the new commit.
Follow-up issue/step: Step 12 re-audit after the platform-gate/evidence fix and the documentation corrections.

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Approval summary

The step agent completes this section before a merge, tag, or Blocker/Major waiver request. It must cover the
full history, not only the latest round.

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

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:

## Audit rounds

Append each new round below. Never overwrite an earlier round.

### Audit round 1

#### Independence and frozen state

Fresh auditor: DeepSeek V4.1 Flash 1017 (opencode-go), spawned by the orchestrator; no shared conversation with
the planner, step agent/implementor, design reviewers, or orchestrator, and no part in the design or
implementation. The audit ran in a separate worktree at `886e1ace631fd7c0fb6cc63499812954c044c350`; the audited
code tree is `9e1997d270ae79d8ad2cd2357529a08e158401fb`, and `git diff --name-status 9e1997d..886e1ac` is exactly
`docs/planning/audits/step-12.md`, `docs/planning/closeout/step-12.json`, `docs/planning/status.md`, and the step
page. The working tree was clean before the record edit. `main`/`origin/main` were `9d4d2ef` and an ancestor of
the audited commit; no merge-tree run applied.

#### Commands and results

`./tools/verify.sh all --require` exited 0 on the frozen tree: 1,756 .NET tests, `dotnet format` clean, Public
API/ApiCompat compatible, 58 contract/policy tool tests, Rust tests, Python pytest, Zensical/DocFX/rustdoc strict,
SPDX/secret/licence/advisory checks, and AOT. `python3 tools/check_closeout_evidence.py
docs/planning/closeout/step-12.json` exited 0. GitHub API run `44370563073` is `success` at the audited
`head_sha`; Rust/Python were skipped by path filters. The acceptance commands and the 10,000-job workload were
re-run and reproduced (4.02 s, 153,768 KB, 9,601,024-byte package; verify 1.26 s). The full command/result list
is in `Evidence` above.

#### Findings

One Major (F-1, platform CI/absence rationale, which also leaves a standard audit checklist item unsatisfied),
seven Minor (F-2 path documentation vs behaviour, F-3 missing `--jobs` fast pre-validation, F-4 nested
response-file report rule, F-5 AOT "dependency-owned" wording, F-6 missing post-publication-cancellation and
inspect-tolerance tests, F-7 exit-code test-plan overclaim, F-8 missing explicit `Cli -> Compilation` reference),
and three Notes (N-1 ETA placement, N-2 unused `log` event, N-3 merge-candidate convention). The step's core
acceptance behaviour, exit-code table, read-only guarantees, package publication, 10,000-job budget, and AOT
gate all pass; the Major is a process/evidence gap, not a code failure.

#### Verdict

**Fixes required.** The step is not ready for merge while F-1 remains open. F-2–F-8 should be corrected or
explicitly accepted with the owner's decision; N-1–N-3 need no merge action beyond acknowledgement. The fixes are
documentation/evidence and test additions; no production code change is required for F-1 unless the owner
chooses to run the platform gate. A re-audit is required after the fixes, with a new commit and (if `main` moves)
a replacement closeout CI run.
