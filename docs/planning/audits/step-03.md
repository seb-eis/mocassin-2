# Step 03 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-03.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Audit
> status`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 03 — Particle and Structure Models |
| Request owner | Step agent |
| Audit initiated by | Step agent |
| Audit status | Passed with notes |
| Audited commit | `b6a97ec` |
| Step branch | `step/03-particle-and-structure-models` |
| Step tag | — |
| Closeout CI run | [`42514332091`](https://github.com/seb-eis/mocassin-2/actions/runs/42514332091) — green at `b6a97ec` |
| Platform CI | Not run (Ubuntu-only policy) |
| Audit date | 2026-11-08 |
| Auditor | Independent out-of-context audit agent (fresh context; no shared conversation with the implementor or planner) |

Step page: [`Step 03 — Particle and Structure Models`](../steps/step-03-particle-and-structure-models.md)

## Re-audit request

Round 1 returned `Fixes required` at `5a06d67`. F1–F5 were fixed on
`feature/step-03-particle-and-structure-models/fix-audit-findings` and squashed as `b6a97ec`; F6 is accepted
as a note with the benchmark evidence explicitly labelled. The replacement closeout CI run
[`42514332091`](https://github.com/seb-eis/mocassin-2/actions/runs/42514332091) is green at `b6a97ec`.
The step branch is refrozen at `b6a97ec`; a fresh re-audit of the complete step is requested. Earlier
reviewer-authored findings, evidence, and verdict are preserved unchanged below.

## Step summary

| Field | Value |
| --- | --- |
| Phase | B. Model definition |
| Depends on | 02 |
| Delivers | Particle, particle-set, space-group, cell-parameter, cell-site, and dummy-position models with validation and a stable public API. |

## Step-specific acceptance checklist

- [x] A user can define ceria: 225, a=5.41 Å, name "Ceria", Ce at (0,0,0), O at (0.25,0.25,0.25), and an
      unstable O site at (0.5,0.25,0.25), using the public editing API.
- [x] The preview shows the correct P1 orbit sizes for each site.
- [x] Overlapping sites are rejected with a `CellSiteId` path to each offending site through the structured
      `OrbitOverlap` result; no message parsing is used.
- [x] Crystal-system constraints are enforced without silently overwriting user input.
- [x] The structure preview is deterministic and cached by
      `(structureHash, A, B, C, catalogDataVersion, catalogDataSha256, symmetryContractVersion)`;
      `structureHash` is the canonical structure-model hash.
- [x] The ceria/BZO preview benchmark case and baseline are recorded; the target is mean < 100 ms on the Step
      02 benchmark runner class. The benchmark runs in Nightly/release, not as a PR-fast wall-clock assertion.
- [x] A preview request whose `A*B*C*P` exceeds `int.MaxValue` returns `STR014` and no preview.
- [x] `"model": {}` still loads as `ModelDefinition.Empty`, and the existing minimal/ceria fixtures round-trip.
- [x] JSON round-trips use `[a,b,c]` fractional arrays, camelCase enum strings, and no `isVoid` field.
- [x] A project with `symmetry` metadata parses in Core, is included in the document hash, and is compared by
      `ProjectValidationService`; a mismatch emits `PROJ034`.
- [x] `ProjectStore.LoadBytesCore` returns Core, Symmetry, and `PROJ034` diagnostics through the composed
      validator.
- [x] Shuffling authored particles, sets, sites, or dummies does not change the canonical model hash.
- [x] All diagnostics are returned by the composed public validation API. CLI exposure is verified in Steps
      07/12; UI exposure is verified in Step 24.
- [x] The generated project schema accepts the structure (including `name`), particles, and optional symmetry
      fields, and fixtures cover present, absent, default-empty, and mismatched metadata.
- [x] Public API files and the ApiCompat baseline are current.
- [x] Documentation written.

## Standard audit checklist

- [x] Required tests from the step page pass. — the committed suite is green; F1 and F4 record missing coverage. — round 2: F1/F4 coverage added; Core 266/266, Symmetry 153/153, Packages 41/41, Architecture 1/1, support 1/1 each, 0 failed/skipped.
- [x] Public/protected XML documentation policy is followed. — DocFX builds with 0 warnings.
- [x] Documentation, status dashboard, feature log, and parity matrix are current. — F3 records status/step-page drift; round 2: F3 corrected at `b6a97ec`/`2bdacd2`, and the Step 03 parity rows and feature log are current.
- [x] Closeout CI is green at the audited commit. — run `42511003132` completed/success at `5a06d67`. — round 2: run `42514332091` completed/success at `b6a97ec`.
- [x] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code. — not run; no platform-specific code in Step 03, accepted under §3.13.
- [x] Format/version/fixture changes are documented and tested. — additive v1 fields; manifest/schema/fixtures current.
- [x] Security, dependency, licence, and secret-scan gates pass. — licence, vulnerability, SPDX, secret, and cargo-deny gates pass.
- [x] Code structure policy passes; no new root-level or catch-all folders. — Architecture test 1/1.
- [x] No unrelated changes or unapproved scope. — diff is limited to Step 03 code, tests, contracts, samples, and docs.
- [x] Branch, tag, and status hygiene are correct. — no `step-03` tag; branch at `3ed0436`; F3 records status drift. — round 2: branch head `2bdacd2` is documentation-only on the `b6a97ec` tree; no `step-03` tag.

## Evidence

**Independence.** I am a fresh, independent, out-of-context audit agent. I did not author or implement
Step 03 and have no shared conversation with the implementor or planner. I reviewed the frozen tree at
`5a06d67`; `3ed0436` is the documentation-only audit-request commit described below.

**Frozen revision and tree.**

```text
git status --porcelain=v1
(no output; clean working tree)

git branch --show-current
step/03-particle-and-structure-models

git rev-parse HEAD
3ed04369e366f194c0714ecd1aa37462f309c77c

git rev-parse 5a06d67
5a06d67d3eaf8a572db14e66340bdfa754c7c67d

git log -1 --format='%H %s' 5a06d67
5a06d67d3eaf8a572db14e66340bdfa754c7c67d step-03: add closeout acceptance tests and status

git log -1 --format='%H %s' HEAD
3ed04369e366f194c0714ecd1aa37462f309c77c docs: request step-03 audit [skip ci]

git diff --name-status 5a06d67..3ed0436
M docs/planning/audits/step-03.md
M docs/planning/status.md
M docs/planning/steps/step-03-particle-and-structure-models.md

git diff --stat 5a06d67..HEAD -- . ':(exclude)docs/planning'
(no output; the code, tests, and configuration tree is identical)

git diff --check main..5a06d67
exit 0

git tag --list
development-ready
planning-complete
step-00
step-01
step-02
(no step-03 tag; correct before merge)

git branch -vv
* step/03-particle-and-structure-models 3ed0436 [origin/step/03-particle-and-structure-models]
(up to date with the remote)
```

**Local gates.** `./tools/verify.sh all` exited 0.

```text
dotnet build Mocassin.slnx -c Release --no-restore
Build succeeded. 0 Warning(s), 0 Error(s).

dotnet test Mocassin.slnx -c Release --no-build
Passed: Mocassin.Core.Tests 263/263
Passed: Mocassin.Symmetry.Tests 152/152
Passed: Mocassin.Packages.Tests 41/41
Passed: Mocassin.Architecture.Tests 1/1
Passed: Mocassin.App.Tests, Mocassin.Contracts.Tests, Mocassin.Results.Tests, Mocassin.Compilation.Tests 1/1 each
Failed: 0, Skipped: 0

dotnet format Mocassin.slnx --verify-no-changes --no-restore
exit 0 (workspace-loading warning printed; no formatting changes)

NuGet licence policy OK (47 packages); no vulnerable packages
ApiCompat: 9 assemblies compatible with the checked-in baseline
Rust: cargo fmt, clippy, and workspace tests pass
Python: pytest and package build pass
Docs: zensical build --strict "No issues found"; DocFX 0 warnings / 0 errors; rustdoc built
Contracts: artifacts current (17 files); catalog current (274 settings, sha256 1f3bf67d...)
SPDX headers present in 175 source files; no secret patterns in 466 tracked text files
cargo-deny: advisories ok, bans ok, licenses ok, sources ok
```

**Closeout CI.** GitHub API query for run `42511003132`:

```text
RUN CI completed success 5a06d67d3eaf8a572db14e66340bdfa754c7c67d step/03-particle-and-structure-models workflow_dispatch
JOB Detect changes completed success
JOB Contracts and policy completed success
JOB Public API compatibility completed success
JOB .NET (ubuntu-latest) completed success
JOB Rust (${{ matrix.os }}) completed skipped
JOB Python (${{ matrix.python-version }}) completed skipped
```

The Rust/Python skips are path-filtered. A query for `head_sha=3ed0436` returned no workflow runs, as
expected for the `[skip ci]` documentation-only commit. Platform CI was not run; Step 03 has no
platform-specific code, so the absence is accepted under `AGENTS.md` §3.13.

**Targeted acceptance checks.** A scratch console probe under `/tmp/opencode/step03check` (not committed)
loaded the real samples and fixtures through `ProjectJson.Deserialize` and
`ProjectValidationService.Validate`:

```text
samples/ceria/ceria.mocprj: []
samples/barium-zirconate/barium-zirconate.mocprj: []
tests/contracts/project-structure.mocprj: []
tests/contracts/project-symmetry.mocprj: []
tests/contracts/project-symmetry-mismatch.mocprj: [PROJ034, PROJ034, PROJ034]

bad dummy (CellDummyPosition with NaN coordinate): THREW System.ArgumentException
ParticleSet.ParticleIds = default via with: core and composed validation THREW System.NullReferenceException
```

The benchmark command
`dotnet run -c Release --project src/dotnet/Mocassin.Benchmarks -- --filter "*StructurePreview*" --job Dry`
exited 0 and validated both fixtures: `StructurePreviewCeria` 595.1 µs and
`StructurePreviewBariumZirconate` 516.5 µs in the cold-start Dry job. The recorded baseline is
15.786 µs / 6.653 µs on the local container; the manual `benchmark.yml` job exists for runner-class
verification.

**Inspection.** I read `AGENTS.md` §§3.8, 3.10, 3.12, and 12, the audit README, the step page, the design
review record, the data contracts, the project-format/architecture/symmetry/design-decision docs, the parity
matrix, the status dashboard, and the relevant quality strategy/ADR context; I reviewed the full Step 03 diff
against `main`, the Core/Symmetry/Packages implementation and tests, the contracts/schema/fixtures, the
samples, and the benchmark evidence. I did not rely on an implementor summary and did not modify any file
other than this record.

### Re-audit round 2 evidence

**Independence and preservation.** I am a separate fresh, independent, out-of-context audit agent from the
round-1 auditor. I did not author or implement Step 03 and have no shared conversation with the implementor,
planner, or round-1 auditor. Round 1's reviewer-authored evidence, findings, and verdict above are preserved
unchanged; the round-2 evidence, findings, and verdict are appended, not substituted.

**Frozen revision and tree identity (`b6a97ec` → `2bdacd2`).**

```text
git status --porcelain=v1
(no output; clean working tree)

git branch --show-current
step/03-particle-and-structure-models

git rev-parse HEAD
2bdacd29a511d76ab5e6bf9653adbab18def12f3

git rev-parse b6a97ec
b6a97ecce67c561c0eca60a751fe37a14fa3e024

git log -3 --format='%H %s'
2bdacd29a511d76ab5e6bf9653adbab18def12f3 docs: record step-03 audit fixes and request re-audit [skip ci]
b6a97ecce67c561c0eca60a751fe37a14fa3e024 step-03: resolve audit findings
b9ae1f63e87b8820740494e4b8bf478ddf28fec2 docs: record step-03 audit Fixes required [skip ci]

git diff --name-status b6a97ec..2bdacd2
M docs/planning/audits/step-03.md
M docs/planning/status.md
M docs/planning/steps/step-03-particle-and-structure-models.md

git diff --stat b6a97ec..2bdacd2 -- . ':(exclude)docs/planning'
(no output)

git diff --stat b6a97ec..2bdacd2 -- src tests contracts tools samples .github
(no output)

git diff --check b6a97ec..2bdacd2
exit 0
```

The code, tests, configuration, contracts, samples, and workflow tree at `2bdacd2` is identical to
`b6a97ec`; the only differences are the audit record, the status dashboard, and the step page. The audited
code tree is therefore `b6a97ec`, reached through the documentation-only re-audit request `2bdacd2`.

**Full diff and complete step history.** `git log --oneline main..b6a97ec` shows nine Step 03 squash commits:
`3974596`, `dd642d6`, `7c4e748`, `67e39e1`, `8a75bc6`, `18940a1`, `9039b6f`, `5a06d67`, and the fix squash
`b6a97ec`, plus the two documentation-only audit-request/record commits `3ed0436` and `b9ae1f6`; `2bdacd2`
is the re-audit request on top. The accepted design merge is `6bffcf2`. `git diff --stat main..b6a97ec` is
108 files, 8710 insertions(+),
176 deletions(-): Core particle/structure models, editing, validation, project/envelope hashing; Symmetry
preview/validator/encoder/cache; Packages composition; contract manifest/schema/fixtures; samples; benchmarks
and baselines; public API and API baseline; user/development/reference docs. There are no Rust, Python,
workflow, or unrelated project changes. `git diff --stat b9ae1f6..b6a97ec` is 13 files, 250 insertions(+),
31 deletions(-), all tied to F1–F6 and their documentation. `git diff --check main..b6a97ec` exits 0.
`git tag --list` contains `development-ready`, `planning-complete`, `step-00`, `step-01`, `step-02` and no
`step-03`.

**Round-1 finding verification.**

| Finding | Round-2 status | Independent evidence |
| --- | --- | --- |
| **F1 (Major)** — non-finite dummy throws in `TryCreatePreview` | **Verified fixed** | `StructurePreviewService.TryCreatePreview` now normalizes dummy positions, rejects non-finite dummies with `STR010`, and returns `null` before computing the structure hash. The new `NonFiniteDummyCoordinateReturnsDiagnosticsAndDoesNotThrow` test passes. A scratch probe supplied `Fractional3(double.NaN, 0, 0)` and produced `preview=null diagnostics=[STR010]` with no exception. A non-finite site control produced `preview=null diagnostics=[SYM001]` with no exception. `LengthAngstrom`/`AngleDegrees` reject non-finite values at construction, so the remaining hash inputs cannot be non-finite through the public unit types. |
| **F2 (Minor)** — default `ParticleIds` throws instead of `PAR005` | **Verified fixed** | `ParticleSet.ParticleIds` now normalizes through a backing field on both construction and `init`, and `ParticleModelValidator.ValidateSetMembers` also guards `IsDefault`. The probe produced `IsDefault=False Length=0`, Core `[PAR005]`, and composed `[PAR005]` with no exception. `DefaultParticleIdsThroughWithExpressionNormalizeToEmpty` and `SetWithDefaultParticleIdsThroughWithExpressionProducesPar005` pass. |
| **F3 (Minor)** — documentation/status drift | **Verified fixed** | The step page now lists Core ownership as `PAR001`, `PAR002`, `PAR005`–`PAR012` and records `PAR004` as emitted by `ProjectValidationService`; the `ST008` typo is corrected; the deviations list records the `PAR004` split; the project-format `model` row describes the Step 03 sections; and the status dashboard Active work text and Step 03 row describe the audit state. `ProjectValidationService.cs:151` emits `PAR004`; the probe shows Core `[]` and composed `[PAR004]`. |
| **F4 (Minor)** — missing random model round-trip property | **Verified fixed** | `Mocassin.Core.Tests/Model/ModelPropertyTests.cs` adds `RandomParticleAndStructureModelsRoundTripCanonically` (100 cases) covering random particles, sets, sites, stabilities, and dummies; it asserts canonical JSON equality and structure-hash equality after a round-trip. The targeted run passed in 145 ms. |
| **F5 (Minor)** — BZO benchmark fixture omitted H | **Verified fixed** | The benchmark fixture now includes the stable H interstitial at `(0.734, 0.5, 0)` and expects 17 P1 positions with orbits `[1, 1, 3, 12]`; `baselines.json` and the raw result were regenerated consistently. The sample BZO probe expands to `P1=17 orbits=[1, 1, 3, 12]` with the last position `(0.734, 0.5, 0)`. The benchmark `--job Dry` run exited 0 and `GlobalSetup` validated both fixtures. |
| **F6 (Note)** — runner-class evidence label | **Accepted note remains** | `structurePreview.evidenceStatus` in `baselines.json` now says the evidence is local-container evidence and the declared GitHub runner class is re-verified through the manual Benchmark CI job; the raw result repeats the qualification. The recorded means (16.460 µs and 9.976 µs) remain far below the 100 ms target. No merge block. |

**Local gates.** `./tools/verify.sh all` exited 0. Observed results:

```text
dotnet build Mocassin.slnx -c Release --no-restore
Build succeeded. 0 Warning(s), 0 Error(s).

dotnet test Mocassin.slnx -c Release --no-build
Core 266/266; Symmetry 153/153; Packages 41/41; Architecture 1/1;
Contracts/Compilation/Results/App 1/1 each; Failed: 0, Skipped: 0.

dotnet format Mocassin.slnx --verify-no-changes --no-restore
exit 0

NuGet licence policy OK; no vulnerable packages
ApiCompat: 9 assemblies compatible with the checked-in baseline
Rust: cargo fmt, clippy (all targets, -D warnings), and workspace tests pass
Python: pytest 3/3 and package build pass
Docs: zensical build --strict "No issues found"; DocFX 0 warnings / 0 errors; rustdoc built
Contracts: artifacts current (17 files); catalog current (274 settings, sha256 1f3bf67d...)
SPDX headers present in 176 source files; no secret patterns in 467 tracked text files
cargo-deny: advisories ok, bans ok, licenses ok, sources ok
```

**Targeted re-audit probe.** A scratch console project at `/tmp/opencode/reaudit-r2` (not committed) loaded
the real samples and fixtures through `ProjectJson.Deserialize` and `ProjectValidationService.Validate`:

```text
ceria: []
bzo: []
project-structure: []
project-symmetry: []
project-symmetry-partial: []
project-symmetry-mismatch: [PROJ034, PROJ034, PROJ034]

F1 dummy NaN: preview=null diagnostics=[STR010]
F1 site +Inf: preview=null diagnostics=[SYM001]
F1 dummy NaN + site +Inf: preview=null diagnostics=[STR010]

F2 with-default: ParticleIds.IsDefault=False, Length=0; core [PAR005]; composed [PAR005]

F5 BZO: P1=17, orbits=[1, 1, 3, 12], last=(0.734, 0.5, 0)

shuffle: ProjectHasher.Compute equal=True; ComputeStructure equal=True
structure name edit changes structure hash: True
PAR004: core [], composed [PAR004]

"model": {} -> particles=0, sets=0, structure='New Structure', sites=0
ModelDefinition.Empty equal=True; canonical round-trip=True
```

**Benchmark fixture and baseline.** `dotnet run -c Release --project src/dotnet/Mocassin.Benchmarks --
--filter "*StructurePreview*" --job Dry` exited 0. `GlobalSetup` validated ceria at 36 P1 positions with
orbits `[4, 8, 24]` and barium zirconate at 17 P1 positions with orbits `[1, 1, 3, 12]`. The cold-start Dry
means were 617.1 µs (ceria, 36.63 KB) and 716.6 µs (BZO, 25.2 KB). The committed baseline is the warmed
DefaultJob evidence (16.460 µs / 9.976 µs), which is internally consistent with the raw result and the
recorded fixture counts.

**Closeout CI.** GitHub API query for run `42514332091`:

```text
RUN CI completed success b6a97ecce67c561c0eca60a751fe37a14fa3e024 step/03-particle-and-structure-models workflow_dispatch
JOB Detect changes completed success
JOB Public API compatibility completed success
JOB Contracts and policy completed success
JOB .NET (ubuntu-latest) completed success
JOB Python (${{ matrix.python-version }}) completed skipped
JOB Rust (${{ matrix.os }}) completed skipped
```

The Rust/Python skips are path-filtered and correct: Step 03 changes no Rust or Python files. A query for
`head_sha=2bdacd2` returned no workflow runs, as expected for the `[skip ci]` documentation-only commit.
Platform CI was not run; Step 03 has no platform-specific code (no P/Invoke, interop, RID-specific packages,
path/case/locking logic, GUI, or Rust platform code), so the absence is accepted under `AGENTS.md` §3.13.

**Standard checklist re-verification (round 2).**

| Standard checklist item | Round-2 result |
| --- | --- |
| Required tests from the step page pass | Pass — full suite green; F1/F4 regression coverage added |
| Public/protected XML documentation policy | Pass — DocFX 0 warnings / 0 errors |
| Documentation, status dashboard, feature log, parity matrix | Pass — F3 corrected; Step 03 section-2 parity rows and feature log current |
| Closeout CI green at the audited commit | Pass — `42514332091` success at `b6a97ec` |
| Windows/macOS runs recorded or absence accepted | Pass — not run; no platform-specific code, accepted under §3.13 |
| Format/version/fixture changes documented and tested | Pass — additive v1 fields; manifest/schema/fixtures current; 17 contract artifacts |
| Security, dependency, licence, secret-scan gates | Pass — licence/vulnerability, ApiCompat, SPDX 176, secrets 467, cargo-deny all pass |
| Code structure policy | Pass — Architecture 1/1 |
| No unrelated changes or unapproved scope | Pass — fix commit limited to F1–F6 and docs; full step diff limited to the step |
| Branch, tag, and status hygiene | Pass — branch `step/03-particle-and-structure-models`; no `step-03` tag; docs-only head |

**Inspection (round 2).** I read `AGENTS.md` §§3.8, 3.10, 3.12, and 12, the audit README, the round-1 record,
the re-audit request and resolutions, the step page, `status.md`, the Step 03 design review record, the data
contracts, the project-format/architecture/symmetry/design-decision docs, the parity matrix, the quality
strategy and relevant ADR context. I reviewed the full `main..b6a97ec` diff, the `b9ae1f6..b6a97ec` fix commit,
the new regression/property tests, the benchmark fixtures/baseline/raw result, the contract fixtures/schema,
the samples, and the CI evidence. I did not rely on an implementor summary and did not modify any file other
than this record.

**Observation (non-finding).** The cross-cutting section 9 row in `appendix-b-parity-matrix.md` for
`Unit/property/contract tests` still says `Planned | 03`; it predates Step 03, is outside the Step 03 parity
task (which covered the section-2 rows), and is a project-wide quality row for later steps. It does not
affect this verdict.

## Known notes for the auditor

- The frozen audit commit is the squashed closeout commit on
  `step/03-particle-and-structure-models`. The closeout CI run URL and audited commit are added by a
  documentation-only `[skip ci]` commit after the squash; CI is green at the same tree.
- The ceria sample keeps the unstable oxygen transition site in the shared oxygen set. A dedicated
  transition-state occupation set was deliberately not added; the original reference example has one, but the
  approved Step 03 feature set excludes transition semantics, which belong to Step 04. The user retracted an
  earlier misreading that the ceria transition position was missing.
- The barium zirconate proton reference position at `(0.734, 0.5, 0)` is a **stable** defect site. Its offset
  `0.234` is a representative structural value (≈0.98 Å on a 4.19 Å cell), not a fitted parameterization.
  Proton-hopping transitions are Step 04 work.
- The samples use one shared uncharged vacancy particle per model, with a symbol distinct from the host
  species. Ceria uses `Vacancy`/`Vc`; barium zirconate uses `Vacancy`/`V` for both the oxygen and proton
  sets. Additional vacancy particles are reserved for distinct charged vacancy states.
- `STR008` and `STR011` are removed from the design and documented as unused. `STR014` (index cap) and
  `STR015` (dummy name validation) were added. `PAR010`–`PAR012` were added.
- `StructurePreviewService.TryCreatePreview` uses the parameter name `structureModel` instead of the design's
  `structure` to satisfy CA1716. `IStructureQuery.Structure` uses a member-level CA1716 suppression.
- The generated project schema remains `schemaVersion: 1` because the Step 03 fields are additive and the
  model shape is pre-release.
- CLI and UI diagnostic exposure is intentionally verified in Steps 07/12 and 24, not in this step.
- `samples-and-docs` was amended before squash specifically for the vacancy-symbol, shared-vacancy, and
  stable-proton corrections. The audit should verify the samples, fixtures, and guide text agree.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **F1 — `StructurePreviewService.TryCreatePreview` throws for a non-finite dummy position instead of returning diagnostics.** The design says the preview service never throws for invalid user data and returns diagnostics for a non-finite coordinate; the Tests row requires invalid input to return diagnostics and no partial preview. `TryCreatePreview` computes `ProjectHasher.ComputeStructure` before validation whenever all *site* coordinates are finite, and that hash serializes `DummyPositions`; a `CellDummyPosition` with `Fractional3(double.NaN, 0, 0)` (publicly constructible) makes `Utf8JsonWriter.WriteNumberValue` throw `ArgumentException`. No preview or diagnostics are returned. The committed `NonFiniteCoordinateReturnsDiagnosticsAndDoesNotThrow` test only exercises a non-finite *site*. | Step page design §4 line 273 and Tests row line 637; `StructurePreviewService.cs` lines 90-101; `Fractional3JsonConverter.cs` `Write`; `StructurePreviewTests.cs` lines 133-145; scratch probe `/tmp/opencode/step03check` (not committed) reproduced the throw. |
| **Minor** | **F2 — Validation throws `NullReferenceException` for `ParticleSet.ParticleIds = default` instead of emitting `PAR005`.** The constructor normalizes default `ParticleIds` to empty, but `ParticleIds` is an `init` property, so `with { ParticleIds = default }` bypasses that normalization. `ParticleModelValidator.ValidateSetMembers` reads `memberIds.Length` without an `IsDefault` guard; `ProjectValidator.Validate` and `ProjectValidationService.Validate` then throw instead of returning the required `PAR005` empty-set error, contrary to the design's no-throw validation rule. | `ParticleSet.cs` constructor and `init` property; `ParticleModelValidator.cs` lines 169-170; `ParticleSetTests.DefaultParticleIdsNormalizeToEmpty` (constructor only, line 23); scratch probe `/tmp/opencode/step03check` (not committed). |
| **Minor** | **F3 — Documentation and status drift.** The step page still says Core emits `PAR001`–`PAR012`, but `PAR004` is emitted in `Mocassin.Packages.Projects.ProjectValidationService` and the architecture/design-decision pages document that composition ownership; the step page's deviations list does not record the change. The status dashboard's Active work section still says “No step branch or implementation exists yet” at `5a06d67`/`3ed0436`. The step page also has a `ST008` typo, and the project-format envelope table still describes `model` as an empty Step 01 object. | Step page lines 38 and 352-357; `ProjectValidationService.cs` line 151; `docs/development/architecture.md` Step 03 section; `docs/development/design-decisions.md` validation-ownership section; `docs/planning/status.md` lines 82-87; `docs/reference/project-format.md` line 44. |
| **Minor** | **F4 — A required property test is missing.** The step page Tests row requires random particle/structure models to round-trip canonically. The committed property tests cover random overlap detection, random validators, and random project names, while the model round-trip test uses one fixed model; no test generates random particle/structure models and round-trips canonical JSON. | Step page Tests row line 648; `StructurePropertyTests.cs`; `ParticleModelValidatorTests.cs` and `StructureModelValidatorTests.cs`; `ProjectPropertyTests.cs`; `ModelEnvelopeTests.cs` fixed `ModelRoundTripsThroughProjectJson`. |
| **Minor** | **F5 — The BZO benchmark fixture is not the recorded sample structure.** The design says the benchmark uses the sample ceria and barium zirconate structures. `StructurePreviewBenchmarks.CreateBariumZirconate` has Ba, Zr, and O only and expects 5 P1 positions with orbit sizes `[1,1,3]`; the sample BZO additionally has the stable H interstitial site and expands to 17 positions with `[1,1,3,12]`. The baseline records 5, so the benchmark under-represents the sample workload even though the target is met. | Step page line 292; `StructurePreviewBenchmarks.cs` lines 25, 41-44, and 120-131; `tests/reference/bench/baselines.json` `structurePreview.fixtures.bariumZirconate`; `samples/barium-zirconate/barium-zirconate.mocprj` lines 144-150; `SampleStructureTests.cs` lines 50-65. |
| **Note** | **F6 — The benchmark baseline is local-container evidence, not the declared runner class.** `baselines.json` labels the runner class `linux-x64-github-ubuntu-latest-dotnet10` but records the local AMD Ryzen environment; the raw result says the runner class is re-verified through the manual benchmark job on release candidates. The target margin is very large (recorded microseconds vs 100 ms), so this is an evidence limitation, not a target failure. | `tests/reference/bench/baselines.json` `structurePreview.runnerClass` and `recorded.environment`; `tests/reference/bench/structure-preview-2026-11-08.txt`; `.github/workflows/benchmark.yml`; Step 02 audit m2 precedent. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

### Re-audit round 2 findings

F1–F5 are verified closed. F6 remains an accepted Note. I identified no new Blocker, Major, or Minor
finding at `b6a97ec`.

| Finding | Original severity | Round-2 status | Verification |
| --- | --- | --- | --- |
| F1 — non-finite dummy preview throw | Major | **Closed — fixed** | `STR010` + `null` preview; regression test passes |
| F2 — default `ParticleIds` validation throw | Minor | **Closed — fixed** | Normalization + validator guard; `PAR005`; two tests pass |
| F3 — documentation/status drift | Minor | **Closed — fixed** | Step page, status dashboard, project-format, and `PAR004` ownership corrected |
| F4 — missing random model round-trip property | Minor | **Closed — fixed** | `ModelPropertyTests` property passes (100 cases) |
| F5 — BZO benchmark fixture omitted H | Minor | **Closed — fixed** | 17 P1 / `[1,1,3,12]`; baseline/raw/sample consistent; Dry run validates |
| F6 — runner-class evidence label | Note | **Accepted note remains** | `evidenceStatus` records local-container evidence and manual re-verification; target met by a wide margin |

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [x] Fixes required
- [ ] Re-audit required
- [ ] Waived

**Fixes required.** The frozen tree builds cleanly; the committed Core, Symmetry, Packages, Architecture, and
support test suites pass (263/263, 152/152, 41/41, 1/1, and 1/1 each); the strict documentation, DocFX,
rustdoc, contract, catalogue, API, licence, vulnerability, secret, and structure gates pass; and the closeout
CI run is green at `5a06d67`. The Step-specific acceptance behaviour is substantiated by the committed tests
and by independent sample/fixture validation. However, F1 is a Major contract violation in the public
`TryCreatePreview` API: a non-finite dummy position throws instead of returning diagnostics and a null
preview. F2–F5 should be corrected or explicitly accepted; F6 is a note. There is no Blocker.

Follow-up owner: Step agent / implementor; human owner for merge and tag approval only after a passing
re-audit.

Follow-up issue/step: Fix F1 on a new feature branch and add a regression test for a non-finite dummy
position; address or record acceptance for F2–F5; update the status/step-page/benchmark evidence; then set
the audit record to `Re-audit required` and request a fresh audit of the new commit. Do not merge
`step/03-particle-and-structure-models` into `main` until a passing audit is recorded.

### Re-audit round 2 verdict

- [ ] Passed
- [x] Passed with notes
- [ ] Fixes required
- [ ] Re-audit required
- [ ] Waived

**Passed with notes.** The audited code tree is `b6a97ec`; `2bdacd2` is a documentation-only re-audit
request whose code, test, configuration, contract, sample, and workflow tree is identical. The full Step 03
diff and the nine-commit step history were reviewed, not only the fix commit. `./tools/verify.sh all` passes;
the .NET solution builds with 0 warnings, the test suites are green (Core 266/266, Symmetry 153/153, Packages
41/41, Architecture 1/1, support 1/1 each), and the documentation, DocFX, rustdoc, contract, catalogue,
licence, vulnerability, secret, ApiCompat, structure, cargo-deny, Rust, and Python gates pass. The closeout
CI run `42514332091` is green at `b6a97ec`; no CI run was created for the `[skip ci]` docs commit `2bdacd2`.

The round-1 Major finding F1 is closed: a non-finite dummy position now returns `STR010` and no preview, and
the regression test passes. F2–F5 are closed with targeted regression/property coverage and regenerated
benchmark evidence. F6 remains an accepted Note: the baseline is explicitly labelled local-container
evidence with the runner class re-verified through the manual Benchmark CI job, and the measured means are
far below the 100 ms target. I found no new Blocker, Major, or Minor finding.

Residual risk: F6's runner-class evidence is still local-container evidence until the manual benchmark job
runs on a release candidate; the target margin is very large, so the risk is low. Step 03 has no
platform-specific code, so Windows/macOS CI remains not run under the Ubuntu-only policy.

Gate statement: no Blocker or Major finding remains unclosed. The only remaining item is the accepted Note
F6. Follow-up owner: step agent / implementor for the resolution and approval-summary commit; human owner for
the final verdict, merge, and tag approval.

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| F1 — non-finite dummy preview throw | Fixed — `TryCreatePreview` returns `STR010` and no preview; regression test added | Step agent | `b6a97ec` |
| F2 — default `ParticleIds` validation throw | Fixed — `init` normalization plus validator guard; emits `PAR005`; tests added | Step agent | `b6a97ec` |
| F3 — documentation/status drift | Fixed — diagnostic ownership, `STR008` typo, status dashboard, and project-format rows corrected | Step agent | `b6a97ec` |
| F4 — missing random model round-trip property | Fixed — `ModelPropertyTests` added | Step agent | `b6a97ec` |
| F5 — BZO benchmark fixture omitted H | Fixed — fixture uses 17 P1 positions / `[1,1,3,12]`; baseline and raw result regenerated | Step agent | `b6a97ec` |
| F6 — runner-class evidence label | Accepted note — `evidenceStatus` added; local-container evidence explicit; target class re-verified manually | Human owner (fix approval) | `b6a97ec` |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Approval summary

The step agent completes this section before requesting human approval. It must cover the full history, not
only the latest round.

- [x] Requested decision and frozen commit recorded
- [x] Every review/audit round recorded (round, date, commit, reviewer, verdict)
- [x] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status)
- [x] Accepted notes and waivers recorded with rationale
- [x] Residual risks, deferred items, and follow-up owners recorded
- [x] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived
- [x] Final CI and documentation-build results recorded

Round history:

| Round | Date | Commit | Reviewer | Verdict |
| --- | --- | --- | --- | --- |
| Design review 1 | 2026-11-08 | `1beb8da` | Independent planning auditor (fresh context) | Changes required |
| Design review 2 | 2026-11-08 | `3096aa8` | Independent planning auditor (fresh context) | Ready with notes |
| Design notes accepted | 2026-11-08 | `a299046` | Human owner | Accepted |
| Audit round 1 | 2026-11-08 | `5a06d67` | Fresh out-of-context audit agent | Fixes required |
| Audit round 2 | 2026-11-08 | `b6a97ec` | Fresh out-of-context re-audit agent | Passed with notes |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| M1 | Major | Symmetry metadata had no Core model/JSON mapping | Envelope metadata, `Project.Symmetry`, hash split | `a299046` / `7c4e748` | Closed |
| M2 | Major | Composed validation not wired into the loader | `ProjectStore.LoadBytesCore` uses `ProjectValidationService` | `a299046` / `18940a1` | Closed |
| M3 | Major | `STR007` overlap lacked a structured site path | `StructureSiteReference` and `OrbitOverlap` encoder overload | `a299046` / `67e39e1` | Closed |
| M4 | Major | Performance criterion was unmeasurable | Preview benchmark, fixture, baseline, tier | `a299046` / `67e39e1` | Closed |
| M5 | Major | Index-cap rejection had no diagnostic code | Added `STR014` | `a299046` / `67e39e1` | Closed |
| M6 | Major | `STR008` was unreachable | Removed and documented as unused | `a299046` | Closed |
| m1 | Minor | `STR011` redundant/undefined | Removed and documented | `a299046` | Closed |
| m2 | Minor | Reserved-ID reuse had no code | Added `PAR010` | `a299046` | Closed |
| m3 | Minor | Void-index test belonged to Step 08 | Replaced with reserved-ID/`Find` contract | `a299046` | Closed |
| m4 | Minor | Particle-set order wording contradictory | Distinguished array order from `ParticleIds` order | `a299046` | Closed |
| m5 | Minor | Test matrix omissions | Added the missing `PAR`/`STR` rows | `a299046` | Closed |
| m6 | Minor | Set/dummy names and duplicate members unvalidated | Added `PAR011`, `PAR012`, `STR015` | `a299046` | Closed |
| m7 | Minor | `particleIds` missing from semantic-order list | Updated project-format documentation | `a299046` | Closed |
| m8 | Minor | Preview cache hash/version seam undefined | Added `ComputeStructure` and version injection | `a299046` / `7c4e748` | Closed |
| n1 | Note | Structure default provenance | Recorded deliberate `New Structure` default | `a299046` | Closed |
| n2 | Note | Core diagnostic class naming | Renamed to `CoreStructureDiagnosticCodes` | `a299046` | Closed |
| n3 | Note | SPDX/licensing task missing | Added implementation task | `a299046` | Closed |
| R2-1 | Minor | Benchmark file at project root | Moved under `Mocassin.Benchmarks/Structure/` | `a299046` / `67e39e1` | Closed |
| R2-2 | Minor | Benchmark tier/perf table | Nightly/release tier and target documented | `a299046` / `67e39e1` | Closed |
| R2-3 | Minor | Hash documentation incomplete | Public `ComputeStructure` and three-hash docs | `a299046` / `7c4e748` | Closed |
| R2-4 | Note | Risk wording | “Every used code” wording | `a299046` | Closed |
| R2-5 | Note | `PROJ034` constant location | Added to `ProjectDiagnosticCodes` | `a299046` / `7c4e748` | Closed |
| R2-6 | Note | `OrbitOverlap` naming | Record renamed; diagnostic constant unchanged | `a299046` / `67e39e1` | Closed |
| F1 | Major | Non-finite dummy preview threw | `STR010` and no preview; regression test | `b6a97ec` | Closed |
| F2 | Minor | Default `ParticleIds` validation threw | `init` normalization, validator guard, `PAR005` tests | `b6a97ec` | Closed |
| F3 | Minor | Documentation/status drift | Corrected step page, status dashboard, project format | `b6a97ec` | Closed |
| F4 | Minor | Missing random model round-trip property | Added `ModelPropertyTests` | `b6a97ec` | Closed |
| F5 | Minor | BZO benchmark omitted H interstitial | Fixture now 17 P1 / `[1,1,3,12]`; baseline regenerated | `b6a97ec` | Closed |
| F6 | Note | Runner-class evidence label | `evidenceStatus` added; local evidence explicit; manual re-verification | `b6a97ec` | Accepted note |

**Accepted notes and waivers.** Design review “Ready with notes” was accepted at `3096aa8`; the round-2 note
resolutions were accepted at `a299046`. Audit Note F6 is accepted: the structure-preview baseline is labelled
local-container evidence and the declared GitHub runner class is re-verified through the manual Benchmark CI
job on release candidates. The target margin is very large.

**Residual risks.** F6 runner-class evidence remains local until a release-candidate manual benchmark run.
Windows/macOS CI is not run because Step 03 has no platform-specific code, accepted under the Ubuntu-only
policy. Sample P-order goldens depend on the proton offset and `symmetry-1` canonical order. The ceria
transition site shares the oxygen set; transition-state occupation belongs to Step 04. BZO remains
structural only. CLI/UI diagnostic exposure is verified in Steps 07/12 and 24. The proton offset is
representative geometry, not a fitted parameter. `TryCreatePreview` uses `structureModel` for CA1716.

**Gate statement.** No Blocker or Major finding remains unclosed. The only remaining item is accepted
Note F6. Final CI: closeout run [`42514332091`](https://github.com/seb-eis/mocassin-2/actions/runs/42514332091)
green at `b6a97ec`; local `./tools/verify.sh all` and the strict documentation, DocFX, rustdoc, contract,
catalogue, API, licence, vulnerability, secret, structure, Rust, and Python gates pass.

## Human approval

- [ ] Accepted: Passed
- [x] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by: Human owner
Date: 2026-11-08
