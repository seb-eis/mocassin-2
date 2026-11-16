# Step 11 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-11.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 11 — Simulation Package and `.msl` Writer |
| Request owner | Step agent (implementor) |
| Orchestrator | — |
| Step agent (implementor) | Step agent (implementor) |
| Audit initiated by | Orchestrator, on request of the step agent |
| Step branch | `step/11-simulation-package-and-msl-writer` |
| Closeout evidence | `docs/planning/closeout/step-11.json` |
| Audit date | 2026-11-16 |
| Auditor | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh independent completion auditor (OpenCode subagent; no shared conversation with the planner, implementor, or orchestrator) |

The closeout evidence file holds the audited commit, closeout CI run and `head_sha`, `main` SHA at freeze, merge candidate,
merge-tree CI, platform CI, and tag. Do not restate them here: one source, one place.

Step page: [`Step 11 — Simulation Package and `.msl` Writer`](../steps/step-11-simulation-package-and-msl-writer.md)

> Orchestration: the orchestrator spawns a fresh auditor when the step agent has frozen the step branch and
> completed the audit request, and relays the verdict and owner decisions. The step agent applies fixes on a new
> feature branch, reruns closeout CI, adds resolutions without altering reviewer text, and asks the orchestrator
> for re-audit until the auditor no longer identifies issues. The orchestrator does not fix findings or edit the
> audit record. The human owner approves the final verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 08, 09, 10 |
| Delivers | A versioned SQLite simulation package containing one parameterized model, content-addressed initial lattices, and streamed job payloads, with a verified publication barrier, contract fixtures, and a read-only inspection/verification library for the build CLI. |

The step includes one integrated amendment:
[`step-11-amend-1`](../amendments/step-11-amend-1-dotnet-11-native-zstd.md) — .NET 11 toolchain and native
Zstandard — integrated as `00753e7` after a `Ready with notes` design review
([record](../reviews/step-11-amend-1.md)). The parent design review is
[`step-11`](../reviews/step-11.md) (`Ready with notes`, accepted 2026-11-15).

## Step-specific acceptance checklist

Copy the acceptance criteria from the step page, preserving their wording:

- [x] A package built from `samples/jobs/ceria-temperature-sweep.mocprj` (six jobs, `parameterizations: []`)
      uses the implicit default parameterization, opens read-only, and contains one model row, the expected
      job rows, and the expected lattice rows.
- [ ] A package built with an explicit in-test parameterization stores the applied pair/group/site/frequency
      values, not compiled defaults, and `models.signature` equals `ParameterizedModel.Signature`.
- [x] The published package has no `results` table, needs no `-wal`/`-shm` files, and reports
      `application_id = 0x4D4F4353` and `user_version = 1`.
- [x] Every job references a valid model and lattice; `job_id` is unique and ascending; a partial build leaves
      gaps exactly as recorded in `job_selection`.
- [x] `verify` re-encodes the parameterization signature from the payload's absolute `frequency_hz` values and
      re-encodes the lattice signature from the particle array and the referenced `model_signature`.
- [x] Every environment pair entry is one row per periodic image with its relative `offset`; the compiler emits
      one `CompiledPairLink` per image and the writer maps it one-to-one, Step 14's target expansion
      (`cell + offset` wrapped, `target_position`) reproduces the compiled minimum-image link for every pair
      table, and a multi-image shell (for example a six-image nearest-neighbour shell) is fully present.
- [ ] The writer asserts `tracker_order.Length` equals the representative direction's path-node count; the
      verifier proves the payload-checkable part (non-empty, non-sentinel values below `global_tracker_count`,
      length within the documented bound), and Step 13's reader loads it as a variable `u8[]` with `0xFF` for
      non-mobile nodes; Step 18's mobile-tracker permutation follows the same indices.
- [x] `PKG001`–`PKG010` are documented with severities and anchors, `PKG` descriptors are composed into
      `DiagnosticCatalog.Default`, and `Mocassin.Results` still references only `Core` and `Contracts`.
- [x] The checked-in `.msl` fixture's `package-summary-v1` golden matches a rebuild after normalizing
      `created_utc`, `app_version`, `generator`, size, and payload byte hashes (`PackageFixtureTests`).
- [x] Two jobs whose lattices are byte-identical share one `lattices` row; a different particle array does not.
- [x] Rebuilding the same project produces identical model/lattice signatures and an identical canonical
      summary; stored bytes may differ only in build metadata (`PackageFixtureTests.RebuildReproducesTheFixtureSummary`).
- [x] The job `flags` word equals `SolverJobFlagsMapper.Compose(...)` and contains exactly one kind bit.
- [x] The job `electric_field_direction` is a unit vector equal to the compiled cell transform of the authored
      fractional direction (tested on a non-orthogonal cell).
- [x] `extensions[].config_json` is byte-equal to the project canonical JSON for the same config, including a
      number token above 2^53.
- [x] A cancelled build leaves no temp file and does not modify an existing package.
- [x] A simulated disk-full/write error leaves the previous package intact.
- [x] `verify` detects a truncated payload, a corrupt Zstd frame, a wrong signature, a missing FK, a wrong
      payload-derived column, a stray `-wal`, and an injected `results` table, each with the expected `PKG` code.
- [x] 10⁴ small jobs build within the time/size budget (amended: 3.87 s, 10.05 MB, 118 MB peak RSS, 5,853 jobs/s; net10 reference: 4.03 s, equal size, 119 MB, 5,230 jobs/s).
- [x] No EF Core, no native structs, no custom binary header, and no results table.
- [x] Documentation written

## Standard audit checklist

- [x] Required tests from the step page pass. — Every committed test passes (1,519 .NET; Rust; Python; 58
      tool tests). Two declarative test-plan rows (property round-trip/signature stability and the nightly fuzz
      harness) have no corresponding test; recorded as F-4. The cross-step rows (Step 13/14/18 consumption) are
      deliberately owned by those steps.
- [x] Public/protected XML documentation policy is followed. — DocFX 0 warnings/0 errors and API analyzers are
      clean; the public writer comments still carry stale feature-3 text and a `NotSupportedException` tag that
      is no longer thrown (F-1, documentation content only).
- [x] Documentation, status dashboard, feature log, and parity matrix are current. — Step page, status row,
      feature log, and parity rows are current for `In audit`; the step-page final-gate counts are stale (N-3)
      and `AGENTS.md` §1 is pre-existing drift (N-4).
- [x] Closeout CI is green at the audited commit. — Run `43935118095`, `head_sha=e94d975…`, conclusion success;
      `.NET (ubuntu-latest)`, `Contracts and policy`, and `Public API compatibility` succeeded and Rust/Python
      were skipped by path filters, as recorded.
- [x] Closeout evidence matches the audited commit and the merge candidate. — `check_closeout_evidence.py` exit
      0; `audit_commit == closeout_ci.head_sha == merge_candidate == e94d975…`; `main_sha_at_freeze=bca6637…` is
      an ancestor. The request tip `4d1af4e` is a docs-only delta; note N-2 records that the checker does not
      itself inspect that later delta.
- [x] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded. — `main` and `origin/main`
      are still `bca66373c68485d08ac78c0259577887726879d1`; it has not moved, so no merge-tree run applies.
- [x] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code. — Windows run `43935661973` at `e94d975…` is green and its focused `Platform tests - package writer`
      and `Platform tests - results reader` steps passed; macOS is omitted because the Unix P/Invoke path is
      exercised on Ubuntu, as recorded.
- [x] Format/version/fixture changes are documented and tested. — `.msl`/payload versions stay 1; the manifest,
      generated schemas/descriptors, lattice projection vectors, model/parameterization vectors, `.msl` fixture,
      pre-amendment net10 fixture, and ApiCompat baseline are regenerated/tested; contract drift is clean.
- [x] Security, dependency, licence, and secret-scan gates pass. — NuGet licence policy 52 packages, no
      vulnerable packages, cargo-deny advisories/bans/licenses/sources ok, SPDX headers in 601 files, no secret
      patterns in 982 tracked text files; no new third-party codec (ZstdSharp removed; native zstd is BSD-3-Clause
      and recorded as a Step 31 packaging obligation).
- [x] Code structure policy passes; no new root-level or catch-all folders. — `Mocassin.Architecture.Tests`
      passed; the new `SimulationPackage`, `Payloads`, `Internal`, and `Packages` folders follow the policy and
      the 800-line/file-count limits.
- [x] No unrelated changes or unapproved scope. — The .NET 11 retarget/native Zstandard and the `AGENTS.md`
      §1/§8 wording are the owner-approved `step-11-amend-1`; the CI platform-gate fix came from `main`
      (`bca6637`) and is recorded. No unrecorded scope or waiver was found.
- [x] Branch, tag, and status hygiene are correct. — Branch is `step/11-simulation-package-and-msl-writer` at
      `4d1af4e`; no `step-11` tag locally or on `origin`; no merge to `main`; feature branches are gone; the
      working tree is clean.

## Evidence

Commands run:

```text
$ git status -sb; git status --porcelain=v1 | wc -l
  ## step/11-simulation-package-and-msl-writer...origin/step/11-simulation-package-and-msl-writer
  0

$ git rev-parse HEAD 4d1af4e e94d975 bca6637
  4d1af4eb7cfc4060b4692b703b166fb4202622ed
  4d1af4eb7cfc4060b4692b703b166fb4202622ed
  e94d975ffe953561a56bd7850f39e073e603b9ea
  bca66373c68485d08ac78c0259577887726879d1

$ git diff --name-status e94d975..4d1af4e
  A docs/planning/audits/step-11.md
  A docs/planning/closeout/step-11.json
  M docs/planning/status.md
  M docs/planning/steps/step-11-simulation-package-and-msl-writer.md
  (docs-only; no code, test, contract, or configuration file)

$ git merge-base --is-ancestor bca6637 e94d975; echo $?
  0
$ git ls-remote origin refs/heads/main; git rev-parse main origin/main
  bca66373c68485d08ac78c0259577887726879d1  refs/heads/main
  bca66373c68485d08ac78c0259577887726879d1
  bca66373c68485d08ac78c0259577887726879d1
$ git tag -l 'step-11*'; git ls-remote --tags origin | grep step-11
  (empty; no local or remote step-11 tag)

$ python tools/check_closeout_evidence.py docs/planning/closeout/step-11.json
  Closeout evidence: OK (exit 0)

$ DOTNET_ROOT=/tmp/opencode/dotnet11 PATH=/tmp/opencode/dotnet11:$PATH ./tools/verify.sh all --require
  exit 0; "Closeout gate completed with required checks enabled."
  .NET: Release build 0 warnings/0 errors; dotnet format clean;
        App 1, Contracts 37, Cli 17, Symmetry 223, Results 39, Core 685,
        Compilation 96, Packages 420, Architecture 1 — 1,519 passed, 0 failed/skipped.
  Public API and dependency gates: build 0 warnings/0 errors; ApiCompat compatible for all
        nine projects; NuGet licence policy OK (52 packages); no vulnerable packages.
  Rust: cargo fmt --check, clippy -D warnings, workspace tests clean.
  Python: 3 tests passed and the wheel/sdist built.
  Documentation: math OK (164 files), links OK (167 files), Zensical strict "No issues found";
        DocFX 0 warnings/0 errors; rustdoc clean; site built.
  Contracts and policy: 19 artifacts current; model 5 / parameterization 4 / lattice 2 projection
        fixtures; space-group catalog current (274 settings); process consistency OK; 58 tool tests;
        SPDX headers in 601 files; no secret patterns in 982 tracked text files;
        cargo-deny advisories/bans/licenses/sources ok.

$ /tmp/opencode/dotnet11/dotnet run --project src/dotnet/Mocassin.Benchmarks -c Release --no-build \
    -- --simulation-package-budget /tmp/opencode/audit-step11-budget.json
  exit 0; independent re-run of Part B on the audited tree:
  totalSeconds=3.8783007, packageBytes=10051584, modelPayloadBytes=1415,
  peakRssBytes=118321152, jobInsertionRate=6212.74, jobCount=10000, latticeCount=10000,
  integrityCheck=ok. The recorded evidence (3.8740823 s, 10,051,584 B, 118,263,808 B,
  5,853.18 jobs/s) is reproduced within run-to-run noise; the package SHA differs only in
  build metadata (informational version), as documented.

$ GitHub Actions API GET /repos/seb-eis/mocassin-2/actions/runs/43935118095
  name=CI event=workflow_dispatch status=completed conclusion=success run_attempt=1
  head_branch=step/11-simulation-package-and-msl-writer
  head_sha=e94d975ffe953561a56bd7850f39e073e603b9ea
  jobs: Detect changes success; .NET (ubuntu-latest) success (Restore/Build/Test with
        coverage/Format); Contracts and policy success; Public API compatibility success;
        Rust skipped; Python skipped.

$ GitHub Actions API GET /repos/seb-eis/mocassin-2/actions/runs/43935661973
  name=CI event=workflow_dispatch status=completed conclusion=success run_attempt=1
  head_branch=step/11-simulation-package-and-msl-writer
  head_sha=e94d975ffe953561a56bd7850f39e073e603b9ea
  jobs: Detect changes success; .NET (windows-latest) success with
        "Platform tests - package writer" success and "Platform tests - results reader" success;
        Test with coverage, Format, Contracts/policy, API, Rust, Python skipped.
  (No macOS run; the new Unix directory-fsync P/Invoke is exercised by the Ubuntu suite and the
   step records macOS as optional because the same Unix path runs on Ubuntu.)

$ GitHub Actions API GET /repos/seb-eis/mocassin-2/actions/runs/43932125294 (main push at bca6637)
  attempt 1: .NET (ubuntu-latest) failed at "Test with coverage"; every other job succeeded.
  attempt 2: all six jobs succeeded (Detect changes, .NET, Contracts and policy, API, Rust, Python).
  Consistent with the recorded shared-runner budget miss; no test, threshold, or workflow file changed.

$ git grep -n "ZstdSharp" -- '*.cs' '*.csproj' '*.props' '*.json' '*.sh' '*.yml'
  (no output; no source, project, props, workflow, or container reference)
$ git grep -n "ZstdSharp" -- .
  only planning/audit/amendment prose (removal and historical evidence), no build input.
$ cat global.json; grep -n TargetFramework Directory.Build.props
  sdk.version=11.0.100, rollForward=latestPatch, allowPrerelease=false; TargetFramework=net11.0.
$ grep -n "Microsoft.NET.Test.Sdk" Directory.Packages.props
  Version="18.12.0".
$ grep -n "11.0.100\|10.\\|dotnet --list-sdks" docs/opencode/mocassin.containerfile
  installs 11.0.100 only; exact SDK-count, pin assertion, and negative "10." grep are present.

$ python - <<'EOF' (fixture inspection, read-only)
  tests/contracts/packages/msl-v1/ceria-temperature-sweep.msl:
    size 98304, sha256 e8f615e1… (matches .sha256 and README), application_id 0x4d4f4353,
    user_version 1, journal_mode delete, tables schema_info/models/lattices/jobs + sqlite_stat1,
    no results table, no -wal/-shm; 1 model, 6 lattices, 6 jobs with ids 1..6.
  tests/contracts/packages/amend-1-net10/ceria-temperature-sweep.msl:
    sha256 053e4c8f… and summary 60c95e1b… match the README; the net11 compatibility test reads both.
  package-summary-v1.json hashes match their READMEs (1bc3a85e… amended, 60c95e1b… net10).

$ git grep -n "The map/encode path\|NotSupportedException\|before the payload encoder" \
    -- src/dotnet/Mocassin.Packages/SimulationPackage
  stale feature-3 remarks/exception tags in SimulationPackageWriter.cs:22-24,140-144,192-196 and
  the three Internal/*Row.cs summaries (F-1).
$ git grep -n "normalization_energy_ev" docs/planning/02-data-contracts.md
  1060: "null = no normalization; 0.0 canonicalizes to null"; JobPayloadMapper.cs:59 passes the
  resolved value through with no canonicalization (F-3).
$ git grep -n "FsCheck\|Arbitrary\|\[Property\]\|fuzz" -- \
    src/dotnet/Mocassin.Packages.Tests/SimulationPackage src/dotnet/Mocassin.Results.Tests/Packages
  (no output; no property-test or fuzz harness for the step test-plan rows; the JSON tests'
  "GetProperty" matches are unrelated — F-4).
$ sed -n '234,246p' docs/planning/steps/step-12-build-orchestration-and-cli.md
  Step 12 acceptance lists only the implicit default parameterization; no explicit-template package
  or applied-value/signature test (F-2).
```

Repository reading (direct, not via a summary):

- `AGENTS.md` §1, §3.4, §3.8–§3.13, §5, §8, §10–§12, §16; `docs/planning/audits/README.md` and the audit
  template; `docs/planning/closeout/README.md`; `docs/development/orchestration.md`.
- The full Step 11 page, `docs/planning/closeout/step-11.json`, `docs/planning/status.md`, the parent design
  review `docs/planning/reviews/step-11.md` (both rounds and resolution tables), the amendment
  `docs/planning/amendments/step-11-amend-1-dotnet-11-native-zstd.md` (§9.1/§9.4/§9.5 and the acceptance
  list), and the amendment review `docs/planning/reviews/step-11-amend-1.md` (both rounds).
- ADR 0009, 0010, 0011, and 0013; `docs/planning/02-data-contracts.md` §3.2–§3.5;
  `docs/reference/msl-format.md`, `docs/reference/diagnostics.md`, `docs/reference/file-layout.md`,
  `docs/user-guide/building.md`, `docs/development/architecture.md`, `docs/planning/06-toolchain-requirements.md`.
- The complete diff `bca6637..e94d975` (236 files) and the docs-only request delta `e94d975..4d1af4e`; all new
  Contracts/Packages/Results code and tests; the manifest/generated schemas, signature vectors, `.msl` and
  pre-amendment fixtures, benchmark evidence, containerfile, CI workflow, ApiCompat baseline, and docs.
- The CI run/job data via the GitHub Actions API (runs `43935118095`, `43935661973`, `43932125294`).

Evidence file: `docs/planning/closeout/step-11.json`

## Known notes for the auditor

- **Two unchecked acceptance criteria are deliberate.** The explicit in-test parameterization package is a
  Step 12 follow-up (the sample fixture uses the implicit default parameterization; the write-time re-encode is
  covered by model-payload tests against real `ParameterizationValues`). The Step 13/18 half of the
  `tracker_order` criterion needs the Rust reader and the mobile-tracker permutation, which do not exist yet;
  the writer assertion and the verifier's payload-checkable part are implemented and tested.
- **Amendment 1 is integrated** as `00753e7`: `global.json` and all projects on .NET 11, the in-box
  `System.IO.Compression` Zstandard codec replacing `ZstdSharp.Port`, and the pre-amendment net10 `.msl`
  fixture and codec goldens checked in. The amendment review record is linked above. The owner authorized the
  factual `.NET 11` wording change in `AGENTS.md` §1/§8 as part of that amendment.
- **Container image not built — accepted risk (owner, 2026-11-15).** There is no OCI/podman builder in the agent
  environment, so `docs/opencode/mocassin.containerfile` is not build-verified. It pins SDK 11.0.100 only and
  asserts the `global.json` pin. Closeout CI resolves the same pin through `actions/setup-dotnet`; the local
  gates ran on the pinned SDK under `/tmp/opencode/dotnet11`. This is recorded in the closeout evidence notes.
- **Part A small-payload gate refinement (owner-approved).** Below 8 KiB the codec comparison uses an absolute
  decode penalty of ≤ 0.15 µs, throughput ≥ 100 MB/s, and ≤ +10 % relative decode at or above 8 KiB. Part A
  passed 240/240 checks with 24/24 byte-identical frames; the rule and results are in amendment 1 §9.1.
- **Platform gate.** The step adds the solution's first platform-specific native code (Unix directory `fsync`
  interop; Windows skips the directory call and relies on file fsync plus atomic replace). The focused Windows
  platform run is green at the audited commit; it built the solution and ran `Mocassin.Packages.Tests` and
  `Mocassin.Results.Tests`. No macOS run is required because the Unix path is exercised on Ubuntu.
- **Closeout CI scope.** The Ubuntu closeout dispatch ran the path-filtered jobs for the changed paths; Rust and
  Python were skipped because this step changes neither. The full local `all --require` gate covers them and
  passed on the same tree.
- **Main-CI flake re-run.** The main CI run for the platform-gate merge `bca6637` failed attempt 1 only at
  `CompilationPerformanceTests.TenThousandPositionModelCompilesWithinTheBudget` (30.134 s against the documented
  `< 30 s` budget) on the shared runner. The owner accepted the miss as a flake and authorized a re-run; attempt
  2 was green. No test, threshold, or workflow file changed.
- **Merge-candidate bookkeeping.** The closeout evidence and this request are a documentation-only commit on top
  of the audited commit. A commit cannot contain its own SHA, so `merge_candidate` in the evidence records the
  audited commit; verify that the later delta changes only documentation.
- **Cleanup.** `fix/ci-platform-gate` and the superseded `docs/dotnet-11-container` branch were deleted locally
  and remotely, and the `mocassin-dotnet11` worktree was removed. No `step-11` tag and no step-to-main merge
  exist at the freeze.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **F-1 — The shipped public writer still carries stale feature-3 XML documentation and a `NotSupportedException` tag for methods that no longer throw it.** The class remarks say the "public payload-encoder methods are completed in Step 11 features 4 and 5; this feature exposes the schema and publication barrier"; `WriteModelAsync`/`WriteJobAsync` remarks say the map/encode path "lands in Step 11 feature 4/5" and both declare `<exception cref="NotSupportedException">`, although both methods map, encode, decode, and write the payload and never throw `NotSupportedException`. The three internal row records also say "before the payload encoder … exists". DocFX and the analyzers are clean because the tags are syntactically valid, but the generated API documentation is misleading. Remove/refresh the remarks and exception tags. | `src/dotnet/Mocassin.Packages/SimulationPackage/SimulationPackageWriter.cs:22-24` (class remarks), `:140-144` (`WriteModelAsync`), `:192-196` (`WriteJobAsync`); `Internal/PackageModelRow.cs:6`, `PackageLatticeRow.cs:6`, `PackageJobRow.cs:6`; `git grep` output in Evidence. |
| **Minor** | **F-2 — The deferred explicit-parameterization acceptance criterion names Step 12 as its owner, but Step 12 does not carry the item.** The criterion is honestly disclosed as unchecked with "follow-up owner: Step 12" and the request's known notes repeat the deferral, but Step 12's acceptance criteria list only the implicit-default sample (`parameterizations: []`) and contain no template-backed package, applied-value assertion, or `models.signature` assertion; its Tests table's "Templates" row covers auto-select/ambiguous/override only. The deferral therefore has no tracked closure point and could be dropped silently. Add the item to Step 12's acceptance criteria/tests (or add the end-to-end explicit-template package test now; the writer re-encode is already proven against real `ParameterizationValues` in the model-payload tests). | Step page `docs/planning/steps/step-11-simulation-package-and-msl-writer.md:1060-1066`; Step 12 acceptance `docs/planning/steps/step-12-build-orchestration-and-cli.md:234-246` and tests `:257-271`; request "Known notes for the auditor". |
| **Minor** | **F-3 — `normalization_energy_ev` is not canonicalized from `0.0` to `null` as the job-payload contract requires.** The data contract says `null` = no normalization and `0.0` canonicalizes to `null`, but `JobPayloadMapper.Map` passes the resolved nullable value straight through, and `JobSettings` deliberately lets an explicit zero override an inherited value; `JobSettingsResolver` uses null-coalescing, so an authored `normalizationEnergyEv: 0.0` survives into the payload. No test covers this field value. The default path stores `null`, so the common build is correct; an explicit-zero override produces a non-canonical payload that Step 16 may interpret differently. Normalize `0.0` to `null` in the mapper and add a regression test. | `docs/planning/02-data-contracts.md:1060`; `src/dotnet/Mocassin.Packages/SimulationPackage/Payloads/JobPayloadMapper.cs:59`; `src/dotnet/Mocassin.Core/Model/Jobs/JobSettings.cs:10`; `src/dotnet/Mocassin.Packages/Jobs/JobSettingsResolver.cs:89`. |
| **Minor** | **F-4 — Three step test-plan rows for property and fuzz coverage have no corresponding test.** The Tests table promises an arbitrary-model payload round-trip property, a signature-stability property (`-0.0`, shuffled map order, a different Zstd level), and a nightly corruption/fuzz harness. The committed round-trip/signature tests are example-based on the ceria model, and the corruption tests are fixed truncation/frame/guard cases; `grep` finds no FsCheck/arbitrary-model property and no fuzz harness in the Step 11 test folders. The existing tests are real-object and pass; this is a test-plan completeness gap, not a correctness failure. Add the property tests and the nightly fuzz harness, or record an explicit deferral. | Step page `:1144-1147`; `ModelPayloadEncodingTests` / `JobAndLatticePayloadEncodingTests` (example-based); `PackageVerifierTests`, `PackagePayloadDecoderTests`, `LatticeDecodeGuardTests` (fixed corruption cases); `git grep` output in Evidence. |
| **Minor** | **F-5 — Malformed `schema_info` values are reported as `PKG002` instead of `PKG003`.** The design maps `PKG002` to application id/user version/format version and `PKG003` to a missing or malformed required schema_info key. `ReadSchemaInfoAsync` returns `UnsupportedSchema` (`PKG002`) when `project_id` is not a GUID and when `created_utc` does not parse; these are malformed schema_info values and should be `MissingSchemaElement` (`PKG003`). `Pkg002UnsupportedVersion` only mutates `application_id`, so the path is untested. This affects diagnostic taxonomy, not package validity. Correct the codes and test both malformed values. | `src/dotnet/Mocassin.Results/Packages/Internal/PackageReader.cs:200-212`; design `docs/planning/steps/step-11-simulation-package-and-msl-writer.md` §5 PKG table; `PackageVerifierTests.Pkg002UnsupportedVersion` / `Pkg003MissingSchemaElement`. |
| **Note** | **N-1 — The containing-directory fsync happens before the atomic rename, so it does not make the rename durable.** The publication barrier flushes the file, flushes the directory, then calls `File.Move(..., overwrite: true)`. This is exactly the order recorded in data contracts §3.5 and design §2, so it is not an implementation deviation; but a crash immediately after the rename can still lose the new directory entry because the directory is not fsynced after the rename. A future contract/ADR correction should fsync the directory after the rename (optionally also before). The current ordering still preserves the no-truncation guarantee: recovery yields either the old or the new complete file. | `src/dotnet/Mocassin.Packages/SimulationPackage/SimulationPackageWriter.cs:291-293`; `docs/planning/02-data-contracts.md` §3.5 step 3; Step 11 §2 pipeline step 6. |
| **Note** | **N-2 — One closeout note overstates what the evidence checker validates.** The note says the post-audit documentation-only commit's delta is validated by `tools/check_closeout_evidence.py`, but the checker compares `audit_commit..merge_candidate`, which are both `e94d975…` and therefore yield an empty diff. The actual delta `e94d975..4d1af4e` was verified manually and is docs-only (audit record, closeout evidence, status dashboard, step page). Reword the note or record the request tip as the merge candidate in a future step. | `docs/planning/closeout/step-11.json` notes[3]; `tools/check_closeout_evidence.py` (`git diff --name-only audit..candidate`); Evidence `git diff --name-status e94d975..4d1af4e`. |
| **Note** | **N-3 — The step-page final-gate counts are slightly stale.** The feature-7/closeout text records 45 tool tests, 600 SPDX files, and 979 secret-scanned files; the audited tree runs 58 tool tests, 601 SPDX files, and 982 secret-scanned files. The increase is consistent with the `b44022a` main merge bringing `tools/tests/test_ci_platform_gate.py` and the CI-fix files; the gate is green. Refresh the counts in the next documentation commit. | Step page `:331-346`; audit gate output in Evidence. |
| **Note** | **N-4 — `AGENTS.md` §1 is pre-existing stale status text.** It still says "Steps 00–07 are complete … Step 08 — Model Compilation Pipeline is next", while Steps 08–10 are merged and Step 11 is `In audit`. This predates Step 11 (`git blame` attributes it to `81fba457`, 2026-11-12) and the status dashboard is correct; it is not a Step 11 scope failure, but it is the kind of top-level drift a future documentation change should correct. | `AGENTS.md:11-14`; `docs/planning/status.md:61`; `git blame -L 10,15 AGENTS.md`. |
| **Note** | **N-5 — The checked pair-expansion criterion bundles a Step 14 consumer test that does not exist in Step 11.** The compiler-emits-one-link-per-image and writer-one-to-one-mapping halves are directly tested (`PayloadPairLinksAreOneToOneWithTheCompiledPerImageLinks`, `MultiImageShellExpandsToEveryEquivalentImage`), but the "Step 14's target expansion reproduces the compiled minimum-image link for every pair table" half cannot run until Step 14 exists. The step page's implementation task states the expansion is a cross-step acceptance test, so this is an acceptance-wording observation rather than a missing Step 11 payload property. | Step page `:1073-1077` and implementation task `:1022-1023`; `ModelPayloadEncodingTests` (pair tests); no Step 14 code. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** Passed with notes — round 1 audited `e94d975ffe953561a56bd7850f39e073e603b9ea` (request tip
`4d1af4eb7cfc4060b4692b703b166fb4202622ed` is the docs-only record parent) and found no Blocker or Major.
Five Minor findings (F-1–F-5) and five Notes (N-1–N-5) remain and require explicit human-owner acceptance.
`Passed with notes` is a record state and the round-1 verdict; it does not authorize the merge or tag.

`Re-audit required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator. I received only the frozen commits (`e94d975…` audited, `4d1af4e…`
> request tip), this request record, and the required commands/scope; no desired verdict or summary was supplied.
> I read the repository, the diff, the durable records, and the CI run data directly rather than relying on a
> summary. The audit was read-only apart from this record; I edited and committed only this one file and did not
> merge, tag, push `main`, delete branches, or fix any finding.

## Verdict

- [ ] Passed
- [x] Passed with notes
- [ ] Fixes required
- [ ] Waived

**Verdict: Passed with notes** at `e94d975ffe953561a56bd7850f39e073e603b9ea` (request tip `4d1af4e`). No
Blocker or Major finding remains. The `.msl` v1 schema, payload DTOs, canonical signatures, content addressing,
atomic publication, no-results-table rule, and bounded read-only verifier match the data contracts and ADRs; the
.NET 11 retarget and in-box native Zstandard swap are complete with no `ZstdSharp` dependency, the net10
compatibility fixture is read and its canonical summary matches, the Part A small-payload gate is recorded, and
the Part B budget is independently reproduced. The audited tree's full local `all --require` gate is green, and
both the closeout CI and the focused Windows platform run are green at the audited commit. `main` is unchanged at
`bca6637`, so no merge-tree run applies, and there is no `step-11` tag or merge.

Five Minor findings remain: stale feature-3 XML documentation on the public writer (F-1), a deferred
explicit-parameterization criterion with no Step 12 closure point (F-2), missing `0.0` → `null` canonicalization
for `normalization_energy_ev` (F-3), missing property/fuzz test-plan rows (F-4), and malformed `schema_info`
values classified as `PKG002` instead of `PKG003` (F-5). Five Notes record the directory-fsync order, one
closeout-note overstatement, stale step-page gate counts, pre-existing `AGENTS.md` §1 drift, and the checked
pair-expansion criterion's Step 14 half. None affects a scientific value, format signature, package validity, or
acceptance criterion that is claimed complete. The human owner must explicitly accept the Minors and Notes before
merge; this record is evidence and does not authorize the merge or tag.

Follow-up owner: Step agent (implementor) for F-1–F-5 and N-2/N-3/N-5; orchestrator/next documentation change for
N-4; design owner for N-1.
Follow-up issue/step: Fix or explicitly accept the five Minors in a new feature branch (or record them as accepted
notes) before the merge request; F-2 should also be added to Step 12's acceptance criteria. Re-audit is required
only if code, tests, or configuration change.

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

I am a fresh context. I did not author or implement Step 11, the amendment, or the routed lattice-guard fix, and I
have no shared conversation with the planner, step agent/implementor, or orchestrator. I received only the frozen
commits, the audit request, and the required commands/scope; no desired verdict or summary was supplied. The audit
was read-only apart from this record.

The audited code commit is `e94d975ffe953561a56bd7850f39e073e603b9ea`; the request tip (record parent) is
`4d1af4eb7cfc4060b4692b703b166fb4202622ed`. `git diff --name-status e94d975..4d1af4e` contains only
`docs/planning/audits/step-11.md`, `docs/planning/closeout/step-11.json`, `docs/planning/status.md`, and the Step 11
page, so the executed code is the audited commit. The working tree is clean and the branch is pushed. `main` and
`origin/main` are still `bca66373c68485d08ac78c0259577887726879d1` and are ancestors of the audited commit; no
merge-tree run is required. There is no `step-11` tag locally or on `origin`, no merge to `main`, and no surviving
feature branch.

#### Commands and results

- `python tools/check_closeout_evidence.py docs/planning/closeout/step-11.json` → `Closeout evidence: OK` (exit 0).
- `DOTNET_ROOT=/tmp/opencode/dotnet11 PATH=/tmp/opencode/dotnet11:$PATH ./tools/verify.sh all --require` → exit 0;
  1,519 .NET tests passed, 0 warnings, ApiCompat clean for nine assemblies, 52 NuGet packages, Rust/Python/docs
  green, 19 contract artifacts current, 58 tool tests, 601 SPDX files, 982 secret-scanned files, cargo-deny clean.
- GitHub Actions API: closeout run `43935118095` is `success` at `e94d975…` (the .NET, contracts/policy, and API
  jobs succeeded; Rust/Python skipped by path filters); Windows run `43935661973` is `success` at `e94d975…` with
  the focused package-writer and results-reader steps successful. The main push run `43932125294` for `bca6637`
  failed attempt 1 only at `.NET` "Test with coverage" and was fully green in attempt 2, matching the recorded
  owner-accepted flake.
- Independent Part B re-run via `Mocassin.Benchmarks --simulation-package-budget`: 3.878 s, 10,051,584 bytes,
  1,415-byte model payload, 118,321,152-byte peak RSS, 6,212.74 jobs/s, `integrityCheck=ok`, 10,000 jobs/10,000
  lattices — the recorded budget evidence is reproduced.
- Fixture inspection: the `msl-v1` package is 98,304 bytes, SHA-256 `e8f615e1…`, `application_id=0x4d4f4353`,
  `user_version=1`, `journal_mode=delete`, one model/six lattices/six jobs, no `results` table and no
  `-wal`/`-shm`; the `amend-1-net10` package SHA-256 and canonical summary match their golden and are read by the
  net11 compatibility test.
- Focused greps confirmed: no `ZstdSharp` reference outside planning prose; `global.json`/TFM/container/CI on
  `11.0.100`/`net11.0`; the stale feature-3 writer comments (F-1); the missing `normalization_energy_ev`
  canonicalization (F-3); the absent property/fuzz tests (F-4); and the absence of a Step 12 explicit-template
  closure for F-2.

#### Findings

Five Minor findings (F-1–F-5) and five Notes (N-1–N-5) are recorded in the `Findings` table above. No Blocker or
Major was found. The Minors are documentation accuracy (F-1), a deferred-criterion traceability gap (F-2), an
edge-case payload canonicalization (F-3), test-plan completeness (F-4), and a diagnostic-code taxonomy error
(F-5). The Notes are the directory-fsync ordering, one closeout-note overstatement, stale gate counts,
pre-existing `AGENTS.md` §1 drift, and the Step 14 half of a checked acceptance criterion.

#### Verdict

**Passed with notes** at `e94d975ffe953561a56bd7850f39e073e603b9ea`. The Step 11 solution and the integrated
amendment produce a contract-conforming, reproducible, immutable `.msl` package with a bounded read-only verifier;
the two unchecked acceptance criteria are explicitly disclosed and owned by Steps 12 and 13/18 (with F-2 recording
that Step 12 does not yet list the first one); the full gate and both CI runs are green; and no Blocker or Major
remains. The human owner must accept the five Minors and five Notes before merge. This record is evidence and does
not authorize the merge or tag.
