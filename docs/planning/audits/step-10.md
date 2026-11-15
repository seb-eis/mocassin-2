# Step 10 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-10.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 10 — Job Templates and Supercell Generation |
| Request owner | Step agent (implementor) |
| Orchestrator | — |
| Step agent (implementor) | Step agent (implementor) |
| Audit initiated by | Orchestrator, on request of the step agent |
| Audited commit | `c10e6d7ff1d399afb48a0d9b24667c996995c92a` |
| Step branch | `step/10-job-templates` |
| Step tag | — |
| Closeout CI run | [`43704552037`](https://github.com/seb-eis/mocassin-2/actions/runs/43704552037) — green at `c10e6d7ff1d399afb48a0d9b24667c996995c92a` |
| Closeout CI head SHA | `c10e6d7ff1d399afb48a0d9b24667c996995c92a` |
| Main SHA at freeze | `ca9a98fe8c7756ccff7fb8bfa1e6fad7aef906a5` |
| Merge-tree CI | Not required; `main` `ca9a98f` is an ancestor of the audited commit |
| Closeout evidence | `docs/planning/closeout/step-10.json` |
| Platform CI | Not run (Ubuntu-only policy; no platform-specific code) |
| Audit date | 2026-11-15 |
| Auditor | Fresh independent completion auditor (OpenCode subagent; no shared conversation with the planner, step agent/implementor, or orchestrator) |

Step page: [`Step 10 — Job Templates and Supercell Generation`](../steps/step-10-job-templates-and-supercells.md)

> Orchestration: the orchestrator spawns a fresh auditor when the step agent has frozen the step branch and
> completed the audit request, and relays the verdict and owner decisions. The step agent applies fixes on a new
> feature branch, reruns closeout CI, adds resolutions without altering reviewer text, and asks the orchestrator
> for re-audit until the auditor no longer identifies issues. The orchestrator does not fix findings or edit the
> audit record. The human owner approves the final verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 06, 08, 09 |
| Delivers | Job template collections/configurations, deterministic job expansion, and doping-aware supercell generation with charge compensation. |

## Step-specific acceptance checklist

Copy the acceptance criteria from the step page, preserving their wording:

- [x] The ceria KMC sample expands to the expected number of jobs with deterministic IDs. (audit: `samples/jobs/ceria-temperature-sweep.mocprj` previews as 6 jobs across 2 collections and 4 configurations; `JobTemplateExpanderTests.ExpandsCollectionsConfigsAndDuplicatesInAuthoredOrder` and `ExpansionIsDeterministicAndSeedsAreUniquePerJob` pin 1-based IDs, authored order, and per-job seeds. The sample itself is previewed, not expanded with an explicit ID vector; the expansion path is the same.)
- [x] For a 10×10×10 ceria cell and 1 % Ce doping, the applied counts and vacancy compensation are correct. (audit: no exact 10×10×10 test exists (F-2); the real ceria sample exercises 6×6×6/8×8×8 at 1–3 % and `PreviewCommandTests.JsonPreviewReportsJobsDopingsAndOptimizers` asserts 1 % → 2 primary + 1 dependent at 864 positions; `DopingApplicationTests` covers the same truncation/compensation arithmetic at 10×1×1, including 10 % → 1+1 and fractional ratios.)
- [x] A requested fraction that cannot be realized is adjusted downward and reported. (audit: `DopingApplicationTests.TinyCellTruncatesToZeroAndWarnsJob011`, `TruncationReportsRequestedAppliedAndEffectiveFraction`, and the sample's 3 % → 14+7 preview with `JOB011` show the downward truncation and report.)
- [x] An impossible charge compensation fails with `JOB012`. (audit: `DopingApplicationTests.ImpossibleCompensationFailsWithJob012AndNoPartialLattice` and `LatticePreviewServiceTests.ImpossibleCompensationReturnsJob012AndNoPreview` assert the error and no lattice/preview.)
- [x] The lattice preview echoes the request, reports requested/applied counts, per-site remaining populations,
  total charge before/after, and neutrality, and matches the generated lattice's report for the same request. (audit: `LatticePreviewServiceTests.PreviewEchoesTheRequestAndDopingsAndMatchesTheGeneratedReport` compares every report field and `DopingInfo`; `RemainingPopulationsArePerSiteAndExcludeTheVoidParticle` checks ordering, counts, and void exclusion.)
- [x] The preview returns `null` plus diagnostics for an unknown doping, a non-finite or out-of-range
  fraction, an invalid supercell, or impossible compensation; it never returns a partial preview. (audit: `UnknownDopingReturnsJob006AndNoPreview`, the NaN/Inf/−0.01/1.01 theory, the invalid-cell theory, and `ImpossibleCompensationReturnsJob012AndNoPreview`; failed results are not cached.)
- [x] The job-count preview reports the default, effective, and total job counts without generating lattices. (audit: `JobCountPreviewReportsDefaultEffectiveAndWholeTemplateTotals`; `JobCountPreviewDoesNotGenerateLattices` passes an invalid `LatticeRequest(0,0,0)` and still returns complete counts, pinning the no-generation boundary.)
- [x] Same job descriptor + seed produces an identical lattice across runs and OSes, and the documented
  job-seed/lattice-seed/sampler golden vectors match. (audit: `GenerationIsDeterministicForTheSameDescriptor`; an independent Python reimplementation reproduced the job seed `884a9517…`, custom job seed `b6d2d161…`, lattice seed `7f601350…`, request hash `5719de5e…`, custom lattice seed `a497141d…`, and sampler `[1, 7, 2]` exactly; the sampler is pure SHA-256/Lemire/Fisher–Yates with no platform branch.)
- [x] Changing a simulation-only setting does not change the lattice for the same job. (audit: the shared generator takes only the lattice request, dopings, and seed, and the request hash excludes runtime settings; however, no explicit test mutates a setting and compares the lattice/seed (F-2).)
- [x] A same-site sequential doping applies to the particles remaining after earlier dopings. (audit: `SameSiteSequenceAppliesToTheRemainingParticles` asserts 10 → 5 A→B then 5 remaining A→C; `DopingsApplyInResolvedPriorityOrderRegardlessOfRequestOrder` covers priority ordering.)
- [x] A non-compensated charged lattice (including a mixed compensated/non-compensated config) is permitted
  with `JOB016` when `IsChargeNeutral` is false; an unrealizable/unbalanced compensated pair is `JOB012`.
  `IsChargeNeutral` uses `1e-6 × max(1, total_positions)` and never fails a pre-existing base charge by itself. (audit: `NonCompensatedChargedDopingWarnsJob016`, `MixedCompensatedAndNonCompensatedConfigWarnsJob016`, `PreExistingNonNeutralBaseIsReportedWithoutDiagnostics`, `CompensatedDopingOnChargedBaseIsNotMisclassified`, and `IsovalentDopingDoesNotWarnJob016`; the predicate is implemented against `particles.Length`.)
- [x] `validate` reports structural `JOB001`–`JOB008`, the `JOB009` reference check, `JOB015`, `JOB017`,
  `JOB018`, and `JOB020` without a compiled model; `JOB009`'s removes-all check and the other compiled-bound
  codes require the build stage. (audit: `JobTemplateValidatorTests` calls `ProjectValidator.Validate` for every structural code; `SelectionOptimizerResolverTests` and `DopingApplicationTests` cover the compiled-bound outcomes through expansion/generation. The documented `JobTemplateValidationStage` itself is absent (F-1), so the "build stage" wording is not literally met even though `build --preview` surfaces the diagnostics.)
- [x] The 99,999-job cap and the structural count caps fail with `JOB019`/`JOB020`. (audit: `JobTemplateValidatorTests` `TemplateAndCollectionCapsAreJob020`, `ConfigDopingAndOptimizerCapsAreJob020`, and `TotalJobsAboveHardLimitIsJob019`; `JobTemplateExpanderTests.ExpansionAboveTheHardJobLimitIsAJob019Diagnostic`.)
- [x] Optimizers remove only start options at every P1 image of the target site, never lattice particles, and
  clear dual-DOF only under the documented all-masks rule. (audit: `SelectionOptimizerResolverTests.MapsEveryP1PositionOfTheSiteAndSortsTheCompiledTargets`, `OneReducedEnvironmentKeepsDualDof`, `SomeReducedEnvironmentsKeepDualDof`, `EveryReducedEnvironmentClearsDualDof`, and `SupercellGeneratorTests.CompiledSelectionOptimizersNeverChangeTheLatticeParticles`; the original `JumpSelectionOptimizer.Run` equality accumulation was inspected and matches the all-reduced predicate.)
- [x] Documentation written (audit: the job-template, lattice, project-format, diagnostics, CLI, compilation-pipeline, and design-decision pages are updated; `./tools/verify.sh all --require` builds the site strictly with DocFX 0/0 and rustdoc clean. The stage documentation and status narrative are inaccurate — F-1/F-3.)

## Standard audit checklist

- [x] Required tests from the step page pass. (audit: `./tools/verify.sh all --require` exit 0 — .NET 1,394 tests across nine projects with 0 failures/skips, Rust fmt/clippy/tests, Python 3, docs strict, contracts/policy 31; focused runs: Packages Jobs 151, Core Jobs/Format/Schema 104, CLI preview/build 9. Three listed PR-medium rows (doping property, sampling property/uniformity, performance) and an explicit setting-invariance/10×10×10 test are absent — F-2.)
- [x] Public/protected XML documentation policy is followed. (audit: warnings-as-errors build 0/0; DocFX 376 files/377 models with 0 warnings/0 errors; PublicAPI.Unshipped.txt updated in Core and Packages; ApiCompat baseline refreshed and the gate is clean.)
- [x] Documentation, status dashboard, feature log, and parity matrix are current. (audit: the step page, feature log, parity rows, and dashboard row are updated; the `Active work` narrative is stale and silent on Step 10, and the feature-8 text assigns closeout to the orchestrator — F-3. The stage pages describe a missing type — F-1.)
- [x] Closeout CI is green at the audited commit. (audit: GitHub Actions run `43699356371` is `CI`/`workflow_dispatch`, `completed/success`, `head_sha` `f2a364dd5f96bca2f9a3df5588443f48cb10f61c`, `head_branch` `step/10-job-templates`; all six jobs green: Detect changes, Python, Contracts and policy, Rust, Public API compatibility, .NET. The branch has exactly one run.)
- [x] Closeout evidence matches the audited commit and the merge candidate. (audit: `python tools/check_closeout_evidence.py docs/planning/closeout/step-10.json` → `Closeout evidence: OK`; `audit_commit` = `closeout_ci.head_sha` = `merge_candidate` = `f2a364d`; `main_sha_at_freeze` `ca9a98f` is an ancestor; `git diff --name-status f2a364d..5bd4a37` is documentation-only.)
- [x] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded. (audit: `main` and `origin/main` are both still `ca9a98fe8c7756ccff7fb8bfa1e6fad7aef906a5`; no move and no merge-tree run required.)
- [x] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code. (audit: not run; accepted under the manual platform policy. The step adds managed C# JSON/records, SHA-256/SHA-256-counter sampling, and `System.Threading.Lock`; no P/Invoke, native/RID package, `System.Runtime.InteropServices`, GUI, installer, or `cfg(windows)`/`cfg(target_os)` branch. Cross-platform execution remains an accepted risk.)
- [x] Format/version/fixture changes are documented and tested. (audit: project `schemaVersion` stays 1 under the Step 03–09 additive pre-release rule; manifest `jobTemplates` fields, generated schema, descriptor fixture registry, and `project-job-templates-populated.mocprj` are updated; 18 contract artifacts current; `doping_info` is a pre-implementation `.msl` field with no published format. No `.msr`/`.mcs` change.)
- [x] Security, dependency, licence, and secret-scan gates pass. (audit: NuGet licence policy 51 packages, no vulnerable packages; SPDX headers in 522 files; no secret patterns in 876 tracked text files; cargo-deny advisories/bans/licenses/sources ok; no new dependency.)
- [x] Code structure policy passes; no new root-level or catch-all folders. (audit: `Mocassin.Packages/Jobs` has 20 direct `.cs` files at the 20-file limit plus the `Optimization` subfolder (3 files); `Mocassin.Core/Model/Jobs` has 9; `tools/verify.sh structure` and `Mocassin.Architecture.Tests` pass.)
- [x] No unrelated changes or unapproved scope. (audit: diff `ca9a98f..f2a364d` is 89 files/13,115 insertions; it is the Step 10 job-template model, persistence/validation, expansion/seeds, supercell/doping/preview/optimizer code and tests, the CLI preview path, sample/docs/contracts, plus two small test adjustments for the `JobTemplate` namespace move. No solver, package writer, GUI, or unrelated feature.)
- [x] Branch, tag, and status hygiene are correct. (audit: work is on `step/10-job-templates`, clean and pushed at `5bd4a37`; no `step-10` tag; no merge to `main`; feature branches are gone; exactly one closeout CI run and no per-feature runs. The status narrative is stale — F-3.)

## Evidence

Commands run:

```text
$ git status -sb
  ## step/10-job-templates...origin/step/10-job-templates
  (clean)

$ git log --oneline -4
  f2a364d docs: record step-10 feature 8 squash commit [skip ci]
  057a85c step-10: complete documentation, parity, and closeout
  cd1e830 step-10: add selection optimizers and build preview
  a95511e step-10: add shared lattice and job-count preview service

$ dotnet restore Mocassin.slnx
  exit 0

$ dotnet build Mocassin.slnx -c Release
  Build succeeded. 0 Warning(s), 0 Error(s); exit 0

$ ./tools/verify.sh all --require
  exit 0
  .NET: build 0 warnings; 1,394 tests passed across nine test projects
    (App 1, Results 1, Contracts 15, Cli 16, Symmetry 223, Core 683, Packages 358,
     Compilation 96, Architecture 1); dotnet format clean
  Public API and dependency gates: build succeeded; ApiCompat clean for nine projects;
    NuGet licence/vulnerability audit clean
  Rust: cargo fmt --check, clippy -D warnings, and workspace tests clean
  Python: 3 passed
  Documentation: math OK (157 files), links OK (160 files), Zensical strict "No issues found",
    DocFX 0 warnings/0 errors, rustdoc clean, site built in site/
  Contracts and policy: 31 tests OK; SPDX headers in 522 files; no secret patterns in
    874 tracked files; cargo-deny advisories/bans/licenses/sources ok
  Closeout gate completed with required checks enabled.

$ git push origin step/10-job-templates
  a146edb..f2a364d  step/10-job-templates -> step/10-job-templates

$ CI workflow_dispatch (platforms=ubuntu, force_all=true)
  run 43699356371, head_sha f2a364dd5f96bca2f9a3df5588443f48cb10f61c, completed/success

$ python tools/check_closeout_evidence.py docs/planning/closeout/step-10.json
  Closeout evidence: OK
```

### Independent audit evidence (round 1)

The block above is the request's pre-audit evidence. This subsection records the auditor's own commands and
results. The audit was read-only apart from this record.

```text
$ git rev-parse HEAD f2a364dd5f96bca2f9a3df5588443f48cb10f61c
  5bd4a37a0d1b983d042643fc50baa586d9b178e4
  f2a364dd5f96bca2f9a3df5588443f48cb10f61c

$ git diff --stat ca9a98f..f2a364d | tail -1
  89 files changed, 13115 insertions(+), 90 deletions(-)

$ git diff --name-status f2a364d..5bd4a37
  A docs/planning/audits/step-10.md
  A docs/planning/closeout/step-10.json
  M docs/planning/status.md
  M docs/planning/steps/step-10-job-templates-and-supercells.md

$ git merge-base --is-ancestor ca9a98f f2a364d; echo $?
  0

$ git rev-parse main origin/main
  ca9a98fe8c7756ccff7fb8bfa1e6fad7aef906a5
  ca9a98fe8c7756ccff7fb8bfa1e6fad7aef906a5

$ git tag -l 'step-10*'
  (empty)
$ git ls-remote --heads origin | grep -E 'step/10|feature/step-10'
  5bd4a37a0d1b983d042643fc50baa586d9b178e4	refs/heads/step/10-job-templates

$ python tools/check_closeout_evidence.py docs/planning/closeout/step-10.json
  Closeout evidence: OK (exit 0)

$ ./tools/verify.sh all --require
  exit 0; "Closeout gate completed with required checks enabled."
  .NET: build 0 warnings/0 errors; App 1, Results 1, Contracts 15, Symmetry 223,
        Core 683, Cli 16, Packages 358, Compilation 96, Architecture 1 — 1,394 passed,
        0 failed/skipped; dotnet format clean.
  API/dependencies: ApiCompat compatible; NuGet licence policy 51 packages; no vulnerable packages.
  Rust: cargo fmt/clippy -D warnings/workspace tests clean.
  Python: 3 passed.
  Documentation: math/links OK; Zensical strict "No issues found"; DocFX 0 warnings/0 errors;
        rustdoc clean; site built.
  Contracts/policy: 18 artifacts current; model projection 5 fixtures; parameterization
        projection 4 fixtures; space-group catalog current (274 settings); 31 tool tests;
        SPDX headers in 522 files; no secret patterns in 876 tracked text files;
        cargo-deny advisories/bans/licenses/sources ok.

$ dotnet test src/dotnet/Mocassin.Packages.Tests/Mocassin.Packages.Tests.csproj -c Release --no-build \
    --filter "FullyQualifiedName~Jobs"
  Passed! - Failed: 0, Passed: 151, Skipped: 0, Total: 151
$ dotnet test src/dotnet/Mocassin.Core.Tests/Mocassin.Core.Tests.csproj -c Release --no-build \
    --filter "FullyQualifiedName~Jobs|FullyQualifiedName~ProjectHasherLattice|FullyQualifiedName~ProjectFormat|FullyQualifiedName~ProjectSchema"
  Passed! - Failed: 0, Passed: 104, Skipped: 0, Total: 104
$ dotnet test src/dotnet/Mocassin.Cli.Tests/Mocassin.Cli.Tests.csproj -c Release --no-build \
    --filter "FullyQualifiedName~Preview|FullyQualifiedName~Build"
  Passed! - Failed: 0, Passed: 9, Skipped: 0, Total: 9

$ dotnet run --project src/dotnet/Mocassin.Cli -c Release --no-build -- \
    validate samples/jobs/ceria-temperature-sweep.mocprj
  0 error(s), 3 warning(s) (ENM017 ×2, ENM018); exit 0.
$ dotnet run --project src/dotnet/Mocassin.Cli -c Release --no-build -- \
    build samples/jobs/ceria-temperature-sweep.mocprj --preview
  exit 0; "Jobs: 6 across 2 collection(s) and 4 configuration(s)";
  1 % → requested 2, applied 2+1; 2 % → 10+5; 3 % → 14+7; 0 .msl files created.
$ dotnet run --project src/dotnet/Mocassin.Cli -c Release --no-build -- \
    validate tests/contracts/project-job-templates-populated.mocprj
  exit 1; JOB002, JOB006, JOB009 (the fixture is reference-only; see F-7).

$ GitHub Actions API GET /repos/seb-eis/mocassin-2/actions/runs/43699356371
  name=CI event=workflow_dispatch status=completed conclusion=success run_attempt=1
  head_branch=step/10-job-templates
  head_sha=f2a364dd5f96bca2f9a3df5588443f48cb10f61c
  created_at=2026-11-15T09:27:41Z updated_at=2026-11-15T09:31:06Z
  jobs (6/6 success): Detect changes, Python (3.13), Contracts and policy,
        Rust (ubuntu-latest), Public API compatibility, .NET (ubuntu-latest)
  branch run query: total_count=1 (only this run).

$ independent Python reimplementation of the written vectors (not copied from the page)
  job_seed       884a9517b16620af2257945215f5552c1f5af0580ed2c911456b1a29a4f54ce7  matches
  custom job      b6d2d1614b2b819445764523ef5613a47b7712a218e303d1a3d2cdb4f71a5c2e  matches
  lattice zero    7f601350f114b61ba560fa983218cfd41e41df83953072ff415eafa21adbf394  matches
  request hash    5719de5efea8b57ef6960229bf2248de0da3bb497c2f29507d1742157f878295  matches
  custom lattice  a497141d26d005eb5caa17025b3c1e3564f31f8d46518a7a36d311623be464d2  matches
  sampler         [1, 7, 2]                                                          matches

$ grep -rn "JobTemplateValidationStage" src/dotnet --include=*.cs | wc -l
  0
$ grep -rln "IValidationStage" src/dotnet --include=*.cs | grep -v Tests
  only src/dotnet/Mocassin.Packages/Projects/Validation/IValidationStage.cs (interface)
$ grep -rn "\.CompiledModel" src/dotnet --include=*.cs | grep -v Tests
  only documentation references; the ValidationContext property has no consumer.
$ grep -rn "RunBuildStage = true" src/dotnet --include=*.cs | grep -v Tests
  (empty); ProjectValidationPipeline.RunBuildStages constructs ValidationContext without CompiledModel.
```

Repository reading:

- `AGENTS.md` §3.8–§3.12, §4–§8, §10–§12, and §16; `docs/planning/audits/README.md` and the audit template;
  `docs/planning/reviews/step-10.md` (all five design rounds and resolutions); `docs/planning/status.md`;
  `docs/planning/closeout/step-10.json` and `README.md`; the full Step 10 page and its diff.
- ADR 0005, 0009, 0010, 0013, and 0014; `docs/planning/02-data-contracts.md` §2.4/§3.4;
  Steps 06, 07, 08, 09, 11, 12, 13, and 25.
- The complete implementation diff `ca9a98f..f2a364d`, all new Core/Packages/CLI code and tests, the sample,
  the generated manifest/schema/fixtures, and every published page the step changed.
- The original clone was read selectively for fidelity only (`JumpSelectionOptimizer.Run` and
  `JobDbModelBuilder.RunPostBuildOptimizers`); the original equality accumulation matches the documented
  all-reduced dual-DOF predicate. No original code or data was copied.

## Known notes for the auditor

- Feature 7 was squash-merged as `cd1e830`; feature 8 was squash-merged as `057a85c`; `f2a364d` records the
  feature 8 squash commit in the feature log. The feature branch
  `feature/step-10-job-templates/documentation-parity-and-closeout` was deleted locally and remotely after the
  squash.
- `main` `ca9a98fe8c7756ccff7fb8bfa1e6fad7aef906a5` was already an ancestor of the frozen commit; no merge-tree
  run was needed. Only documentation and audit-record commits follow the audited commit.
- The closeout CI run is the single Ubuntu dispatch for this step. No per-feature CI ran and no replacement
  closeout run was dispatched.
- Platform CI (Windows/macOS) was not run. Step 10 adds managed C# compilation/lattice code with no P/Invoke,
  native library, RID-specific package, `System.Runtime.InteropServices`, GUI project, installer, or
  `cfg(windows)`/`cfg(target_os)` branch; path and case handling is not touched.
- Known limitations recorded on the step page:
  - The GUI documentation describes the intended Step 25/26 editor surfaces as planned; no editor UI ships in
    Step 10.
  - `build-preview-v1` is an interim report shape until Step 12 formalises the build report.
  - The job-template guide links to the CLI reference page without a fragment because the repository link
    checker and Zensical derive different heading slugs for `mocassin build --preview` (the `--` token).
  - `build --preview` performs load, validation, compilation, expansion, and generation but never opens or
    writes a `.msl`; the full build command belongs to Step 12.
  - The selection dual-DOF rule, the `IsChargeNeutral` tolerance, `JOB016` semantics, and the 99,999-job cap are
    documented design decisions with tests, not silent corrections.
- An earlier DocFX run reported three `InvalidAssemblyReference` warnings for MessagePack against stale build
  outputs. A clean `dotnet restore` plus Release build cleared them; the recorded gates are green.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **F-1 — The documented `JobTemplateValidationStage` and the `ValidationContext.CompiledModel` build-stage path are not implemented.** The accepted design (§6–§7; design review R2-3/m9) and the step-page task list make the Packages `IValidationStage` the owner of the compiled-bound `JOB004` total-position, `JOB009` removes-all, `JOB010`–`JOB014`, `JOB016`, and `JOB019` checks, with Step 12 registering it. No class implements `IValidationStage`; `grep -rn "JobTemplateValidationStage" src/dotnet --include=*.cs` returns 0; `ProjectValidationPipeline.RunBuildStages` constructs `ValidationContext` without `CompiledModel`; no caller sets `RunBuildStage = true`; `PreviewCommand` uses `new ProjectValidationPipeline()` with no stage. The public `ValidationContext.CompiledModel` property is therefore dead, and `docs/development/design-decisions.md`, the step page §6/§7, and Step 12 describe a type that does not exist. The compiled-bound diagnostics are still reachable because `build --preview` runs expansion and generation, so this is an architecture/contract and Step 12 integration gap rather than a wrong lattice; but the step-page task marked [x] and the acceptance wording are not literally met. Implement the stage and pipeline wiring (or record an approved deviation and update the step page, design-decisions, Step 12, and the task list). | Step page §6–§7 and tasks (`docs/planning/steps/step-10-job-templates-and-supercells.md:841`, `:848-857`, `:1002`); `docs/development/design-decisions.md:293-305`; `docs/planning/steps/step-12-build-orchestration-and-cli.md:99`; `ValidationContext.cs:25`; `ProjectValidationPipeline.cs:242-270`; `PreviewCommand.cs:122`; `grep` output in Evidence. |
| **Minor** | **F-2 — Several test rows and two acceptance properties have no direct test.** The step test table lists PR-medium "Sampling" (uniform without replacement), "Doping property" (random dopings within bounds/deterministic), "Sampling property" (random requests never exceed candidates), and "Performance" (10⁴ jobs < 10 s; generation < 1 GiB with cancellation every job). No FsCheck/property test or timing/memory test exists in the Step 10 test folders; `LatticeSamplerTests` covers the golden vector, determinism, bounds, and candidate containment, not uniformity. The acceptance criteria "10×10×10 ceria + 1 % Ce doping" and "changing a simulation-only setting does not change the lattice" also have no exact test: the real sample exercises 6×6×6/8×8×8 at 1–3 % and synthetic tests use 10×1×1, while the generator/request-hash construction excludes settings but no regression test mutates a setting. There is also no CLI test that drives a compiled-bound `JOB` failure through `build --preview`. Existing tests pass; this is a completeness gap, not a correctness failure. | Step page Tests (`:1058`, `:1063-1064`, `:1074`) and acceptance (`:1011`, `:1021`); `LatticeSamplerTests.cs`; no `FsCheck`/`Stopwatch` in `Mocassin.Packages.Tests/Jobs`; `PreviewCommandTests.cs` covers success, usage, unknown template, and missing project only. |
| **Minor** | **F-3 — Status/process documentation is stale or assigns step-agent work to the orchestrator.** `docs/planning/status.md` "Active work" still says Step 09 "is in audit" and then records its completion, and it does not mention Step 10 at all, while the dashboard row is `In audit`. The feature-8 outcome says "The orchestrator owns those steps" for closeout CI dispatch, `docs/planning/closeout/step-10.json`, and the audit request; `AGENTS.md` §3.8/§3.10 assign closeout, evidence, and the audit request to the step agent, and the orchestrator only routes/spawns and must not run gates or edit records. | `docs/planning/status.md:102-117`; `docs/planning/steps/step-10-job-templates-and-supercells.md:409-410`; `AGENTS.md` §3.8 and §3.10. |
| **Minor** | **F-4 — The compensation search can report `JOB012` although a smaller pair is feasible when the dependent source overlaps the primary source.** `DopingApplier` computes `dependentCandidates.Count` before applying the primary and passes that count to `TryFindCompensatedPair`; after the primary it recomputes the dependent candidates and returns `JOB012` if fewer remain. A compensated doping whose primary source positions are also dependent source positions can therefore consume the dependent availability and fail instead of continuing the documented downward search. No test covers overlapping primary/dependent sources. Recompute the post-primary dependent availability inside the search (or document and test the overlap as unsupported). | `src/dotnet/Mocassin.Packages/Jobs/DopingApplier.cs:185-210` (pre-primary dependent count), `:224-255` (post-primary recheck and `JOB012`); design Step 10 §3 steps 2e/2g; no overlapping test in `DopingApplicationTests`. |
| **Note** | **F-5 — Extension-config canonicalization writes every JSON number as an IEEE-754 double.** `CanonicalJson.Write` uses `writer.WriteNumberValue(element.GetDouble())` for `JsonValueKind.Number`; an extension config integer above 2^53 is silently rounded on save/hash (for example `9007199254740993` → `9007199254740992`), and a JSON number outside the double range may throw during canonicalization. The extension schema belongs to Steps 27/28; define the number policy (preserve the raw token or restrict to doubles) and add a round-trip test before extensions are used. | `src/dotnet/Mocassin.Core/Project/JobTemplateCanonicalizer.cs` (`CanonicalJson.Write`, number case); `JobTemplatePersistenceTests` uses only small numbers. |
| **Note** | **F-6 — `JOB013`/`JOB019` are emitted by the Core structural validator although the accepted ownership table assigns them to the Packages build stage.** `JobTemplateValidator` accumulates total jobs and emits `JOB019`/`JOB013`; Core tests assert it. This gives earlier `validate` diagnostics and is functionally harmless, but it is an unrecorded ownership deviation (and duplicates the expander's `JOB019` message). Either record it as an intentional improvement or move the check to the stage once implemented. | `src/dotnet/Mocassin.Core/Project/JobTemplateValidator.cs:240-256`; step page ownership table `:839-842`; `JobTemplateValidatorTests.TotalJobsAboveWarningThresholdIsJob013`/`TotalJobsAboveHardLimitIsJob019`. |
| **Note** | **F-7 — The project schema/envelope tests filter every `JOB` diagnostic, so the new populated job-template fixture's structural errors are invisible to those tests.** `ProjectSchemaTests` and `ProjectFormatTests` now use `.Where(d => !d.Code.StartsWith("JOB"))`; running `validate tests/contracts/project-job-templates-populated.mocprj` returns `JOB002`, `JOB006`, and `JOB009` because the fixture references entities absent from its minimal model. The step page records the fixture as reference-only and `JobTemplateValidatorTests` covers the codes, but the broad filter would also hide a future invalid job template in any schema fixture. Prefer a fixture-specific exclusion or a valid populated fixture. | `src/dotnet/Mocassin.Core.Tests/ProjectSchemaTests.cs:322-329`; `src/dotnet/Mocassin.Core.Tests/ProjectFormatTests.cs:165-170`; command output in Evidence; step page feature-2 deviations `:82-83`. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** Ready for audit

`Ready for audit` and `Re-audit required` are record states, not verdicts.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator. I read the repository at `f2a364dd5f96bca2f9a3df5588443f48cb10f61c`
> (and the documentation-only request tip `5bd4a37`) directly rather than relying on a summary, and I edited and
> committed only this review record.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [x] Fixes required
- [ ] Waived

**Verdict: Fixes required.** One Major (F-1) and three Minor (F-2–F-4) remain; F-5–F-7 are Notes. The
deterministic seed/lattice/sampler vectors, sequential doping/truncation/compensation/report semantics,
preview/build equality, cache bounds, optimizer P1 mapping and dual-DOF rule, caps, sample, documentation,
and all local/CI gates are correct and were independently reproduced. The Major is the absent compiled-bound
`JobTemplateValidationStage`/`ValidationContext.CompiledModel` plumbing that the step page, design decisions,
and Step 12 describe as delivered; the compiled-bound diagnostics themselves are still reachable through
`build --preview` expansion/generation, so this is an architecture and cross-step integration failure rather
than a wrong lattice. Fix F-1 (or record an approved deviation and reconcile Step 12 and the design pages)
before merge; F-2–F-4 should be fixed or explicitly accepted. This record is evidence and does not authorize
the merge or tag.

Follow-up owner: Step agent (implementor)
Follow-up issue/step: Step 10 fix feature branch, then re-audit at the new frozen commit

## Resolution status

The step agent owns this section. The reviewer-authored findings, evidence, and verdict above are unchanged.

Round-1 fixes were committed on `feature/step-10-job-templates/fix-audit-round-1` at
`5f375152a7768c615eb7d2673ddbfbf14457b348` and squash-merged into `step/10-job-templates` as
`c10e6d7ff1d399afb48a0d9b24667c996995c92a` (`step-10: fix audit round 1 findings`). The replacement closeout CI
run [`43704552037`](https://github.com/seb-eis/mocassin-2/actions/runs/43704552037) is green at `c10e6d7` with
all six jobs successful; the earlier run `43699356371` at `f2a364d` remains process history. The fix feature
branch was deleted locally and remotely. The audit request is ready at `c10e6d7`; the orchestrator routes the
round-2 re-audit.

| Finding | Status | Owner | Fix commit | Resolution |
| --- | --- | --- | --- | --- |
| F-1 (Major) | Fixed | Step agent | `5f37515` | Added `Mocassin.Packages.Projects.Validation.JobTemplateValidationStage` and wired `ValidationContext.CompiledModel` through `ProjectValidationPipeline.Validate`/`ValidateFileAsync`. The stage owns total-position `JOB004`, large-supercell `JOB014`, job totals `JOB013`/`JOB019`, removes-all `JOB009`, and doping/truncation/compensation/neutrality `JOB010`–`JOB012`/`JOB016` through the shared `LatticePreviewService`. `mocassin build --preview` now compiles first, registers the stage with the compiled model, and sets `RunBuildStage = true`; build stages are skipped without either. Tests cover a real compiled model, skipping, each owned code, and a CLI compiled-bound failure. Design decisions, Step 10 §6–§7, and Step 12 now describe the implemented type and wiring. |
| F-2 (Minor) | Fixed | Step agent | `5f37515` | Added the missing sampling property/uniformity tests, the random-doping property and determinism test, the exact 10×10×10 ceria + 1 % Ce acceptance test, the explicit simulation-setting invariance test (same request hash, lattice seed, particles, and report), a 10⁴-job expansion smoke test declared `PR-medium`, and a CLI `build --preview` compiled-bound failure test. |
| F-3 (Minor) | Fixed | Step agent | `5f37515` | Corrected the feature-8 wording so closeout CI, closeout evidence, and the audit request are step-agent tasks and the orchestrator only routes and spawns. The `docs/planning/status.md` Active work and dashboard state are updated in the accompanying documentation commit. |
| F-4 (Minor) | Fixed | Step agent | `5f37515` | The compensation search now samples the maximal primary selection once and searches downward using the dependent availability that remains after the prefix, including positions that are both primary and dependent sources. `OverlappingPrimaryAndDependentSourcesReduceToAFeasiblePair` reproduces the former false `JOB012` and pins the 5+5 feasible pair. |
| F-5 (Note) | Fixed | Step agent | `5f37515` | Extension-config canonicalization now preserves each JSON number token verbatim instead of writing an IEEE-754 double. Integers above 2⁵³ and values outside the binary64 range round-trip exactly; `ExtensionConfigNumbersRoundTripWithoutBinary64Rounding` pins the behavior. `docs/reference/project-format.md` and Step 10 §8 record the policy for Steps 27/28. |
| F-6 (Note) | Fixed | Step agent | `5f37515` | `JOB013`/`JOB019` ownership now matches the accepted ownership table: the Core structural validator no longer accumulates job totals, and the compiled-bound stage emits both codes. `JobTemplateValidatorTests.JobTotalsAreDeferredToTheCompiledBoundBuildStage` pins the Core side; stage tests pin `JOB013`/`JOB019`. |
| F-7 (Note) | Fixed | Step agent | `5f37515` | Removed the broad `JOB` filter. The populated job-template fixture now asserts its exact expected structural codes (`JOB002`, `JOB006`, `JOB009`), every other schema fixture asserts no diagnostics, and the envelope fixture asserts only its expected `JOB001` stub code, so a future invalid job template cannot hide. |

All seven findings are addressed; none is accepted or deferred. No reviewer-authored finding, evidence, or
verdict text was changed.

## Approval summary

The step agent completes this section before the orchestrator presents the approval request. It must cover the
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

I am a fresh context. I did not author or implement Step 10 and I have no shared conversation with the planner,
step agent/implementor, or orchestrator. I received only the frozen commit, the audit request, repository links,
and the required commands/scope. I read the repository directly; the findings and verdict above are mine.

The audited code commit is `f2a364dd5f96bca2f9a3df5588443f48cb10f61c`. The request tip is
`5bd4a37a0d1b983d042643fc50baa586d9b178e4`; `git diff --name-status f2a364d..5bd4a37` contains only the audit
request, the closeout evidence, and the two status lines, so the record parent is the documented
documentation-only request commit, not the audited code commit. `main` and `origin/main` are still
`ca9a98fe8c7756ccff7fb8bfa1e6fad7aef906a5` and are ancestors of `f2a364d`; no merge-tree run is required.
The branch is clean and pushed, has no `step-10` tag, and has exactly one CI run.

#### Commands and results

The full command/output record is in `### Independent audit evidence (round 1)` under **Evidence**.
Summary: `check_closeout_evidence.py` → `Closeout evidence: OK`; `./tools/verify.sh all --require` → exit 0
(1,394 .NET tests, Rust fmt/clippy/tests, Python 3, docs strict/DocFX 0-0/rustdoc, 18 contract artifacts,
31 policy tests, SPDX 522, no secrets, cargo-deny ok); focused Packages Jobs 151, Core Jobs/Format/Schema 104,
CLI preview/build 9 all pass; the sample validates (0 errors) and previews six jobs with no `.msl`; the
GitHub Actions run `43699356371` is `CI`/`workflow_dispatch`, `completed/success`, `head_sha` `f2a364d`, with
all six jobs green and no other run on the branch. The six published golden values reproduce from an
independent Python implementation.

#### Findings

F-1 (Major): the documented `JobTemplateValidationStage`/`ValidationContext.CompiledModel` build-stage path is
absent — no `IValidationStage` implementation, no compiled-model plumbing, no `RunBuildStage = true` caller;
the compiled-bound codes still surface through `build --preview` expansion/generation. F-2 (Minor): listed
property/performance tests, an exact 10×10×10 1 % test, an explicit setting-invariance test, and a CLI
compiled-bound failure test are missing. F-3 (Minor): the status "Active work" narrative is stale and the
feature-8 text assigns closeout/evidence/audit-request work to the orchestrator instead of the step agent.
F-4 (Minor): the compensation search uses pre-primary dependent availability and can emit a false `JOB012`
when primary and dependent sources overlap. F-5 (Note): extension-config numbers are canonicalized through
`double`. F-6 (Note): Core emits `JOB013`/`JOB019` although the ownership table assigns them to the build stage.
F-7 (Note): the schema/envelope tests filter all `JOB` diagnostics, hiding the reference-only fixture's
structural errors.

#### Verdict

**Fixes required** at `f2a364dd5f96bca2f9a3df5588443f48cb10f61c`. No Blocker was found; one Major and three
Minor findings remain. The implementation's deterministic generation, doping/compensation semantics, preview
equality, optimizer rule, caps, sample, and gates are sound. The Major is a design/Step 12 integration gap
rather than a wrong lattice; the step page task list and design pages currently claim it is delivered.
