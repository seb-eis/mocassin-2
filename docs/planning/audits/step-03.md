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
| Audit status | Fixes required |
| Audited commit | `5a06d67` |
| Step branch | `step/03-particle-and-structure-models` |
| Step tag | — |
| Closeout CI run | [`42511003132`](https://github.com/seb-eis/mocassin-2/actions/runs/42511003132) — green at `5a06d67` |
| Platform CI | Not run (Ubuntu-only policy) |
| Audit date | 2026-11-08 |
| Auditor | Independent out-of-context audit agent (fresh context; no shared conversation with the implementor or planner) |

Step page: [`Step 03 — Particle and Structure Models`](../steps/step-03-particle-and-structure-models.md)

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

- [x] Required tests from the step page pass. — the committed suite is green; F1 and F4 record missing coverage.
- [x] Public/protected XML documentation policy is followed. — DocFX builds with 0 warnings.
- [ ] Documentation, status dashboard, feature log, and parity matrix are current. — F3 records status/step-page drift.
- [x] Closeout CI is green at the audited commit. — run `42511003132` completed/success at `5a06d67`.
- [x] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code. — not run; no platform-specific code in Step 03, accepted under §3.13.
- [x] Format/version/fixture changes are documented and tested. — additive v1 fields; manifest/schema/fixtures current.
- [x] Security, dependency, licence, and secret-scan gates pass. — licence, vulnerability, SPDX, secret, and cargo-deny gates pass.
- [x] Code structure policy passes; no new root-level or catch-all folders. — Architecture test 1/1.
- [x] No unrelated changes or unapproved scope. — diff is limited to Step 03 code, tests, contracts, samples, and docs.
- [x] Branch, tag, and status hygiene are correct. — no `step-03` tag; branch at `3ed0436`; F3 records status drift.

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

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

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

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:
