# Step 06 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-06.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Audit
> status`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 06 — Lattice and Simulation Models |
| Request owner | Step agent |
| Orchestrator (step agent) | Step 06 implementor |
| Audit initiated by | Human owner |
| Audit status | Passed with notes |
| Audited commit | `ea6293e` |
| Step branch | `step/06-lattice-and-simulation-models` |
| Step tag | — |
| Closeout CI run | [`43085605850`](https://github.com/seb-eis/mocassin-2/actions/runs/43085605850) — green at `ea6293e` |
| Platform CI | Not run (Ubuntu-only policy; no platform-specific code) |
| Audit date | 2026-11-11 |
| Auditor | Independent audit agent (fresh context; no shared conversation with the planner, implementor, or step agent) |

Step page: [`Step 06 — Lattice and Simulation Models`](../steps/step-06-lattice-and-simulation-models.md)

## Audit request summary

- Frozen code commit: `ea6293e` on `step/06-lattice-and-simulation-models`.
- Feature sequence: lattice model, persistence, and validation (`bc333f3`); simulation model and validation
  (`1c64c96`); persistence and contracts (`48e1f20`); queries and editing (`cfeb7b6`); samples and docs
  (`46ea087`); closeout (this commit).
- Local closeout gate: `./tools/verify.sh all` passed on the step branch; Zensical strict, DocFX, rustdoc,
  Rust, Python, contracts, API compatibility, and structure gates all passed.
- Closeout CI: one manual Ubuntu dispatch, run `43085605850`, green. Detect changes, .NET, Contracts and policy, and Public API compatibility passed; Rust and Python were path-filtered and skipped.
- Known deferrals: Step 10 owns fraction application, integer truncation, charge-compensation counts, the single
  supercell generator, the lattice/job-count preview service, and effective job counts. Step 11 owns the
  `.msl` writer and `SolverJobFlags` composition. Step 25 owns the GUI editors. Step 07 owns the consolidated
  diagnostic catalog and help pages.
- Accepted design-review residuals: R6-1 (Compilation/Packages carrier for the derived intent) is resolved by
  `SolverJobFlagsMapper` and its tests; R6-2 (data-contract deterministic wording) is corrected in this step;
  R6-3 (`ILatticePreviewService` placement) is Step 10; R6-4 is a process note only.

> Orchestration: the step agent (the orchestrator) freezes the step branch, requests the auditor, applies fixes
> autonomously on a new feature branch, reruns closeout CI, adds resolutions without altering reviewer text,
> and requests re-audit until the auditor no longer identifies issues. The human owner approves the final
> verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | B. Model definition |
| Depends on | 03, 04 |
| Delivers | Occupation exchanges, ordered dopings, and MMC/KMC simulation definitions with defaults and validation. |

## Step-specific acceptance checklist

- [x] The ceria reduction doping validates at the model level: both deltas are non-zero and opposite in sign,
      and the derived building block is exposed. — `OccupationExchangeTests.CeriaReductionHasOppositeNonZeroChargeDeltas`,
      `DopingTests`, and `LatticeModelTests.DefaultBuildingBlockUsesFirstSetParticleAndVoidOnUnstableSites`; the
      ceria-reduction sample also validates.
- [x] An isovalent doping without a dependent exchange validates. — `LatticeTestModels.IsovalentDoping` (Ce⁴⁺ →
      Zr⁴⁺, zero delta) is part of `ValidLatticeModelHasNoLatticeOrProjectDiagnostics`; `LAT008` requires a
      dependent only when compensation is enabled.
- [x] A dependent exchange with the wrong charge sign is rejected with `LAT009`. — `DopingTests` covers a
      same-sign pair and a zero delta.
- [x] Doping priorities are unique, in `0..9999`, and serialize in ascending order; duplicates are `LAT006`. —
      `DopingTests` covers duplicates/ranges and `LatticeModelTests.CanonicalModelSortsExchangesByIdAndDopingsByPriority`
      covers ascending serialization.
- [x] A KMC simulation cannot select an MMC transition, and vice versa (`SIM002`). — the validator mapping is
      symmetric (`SimulationModelValidator.cs:160–193`); the KMC→MMC direction is tested, but the MMC→KMC
      direction is not directly asserted (F1).
- [x] All original simulation settings are represented: temperature, target MCSP, write-out count, time limit,
      min success rate, job count, RNG seed, execution flags, KMC pre-run/field/normalization/max frequency,
      and MMC break tolerance/sample length/interval/result sample. — all fields exist on
      `Simulation`/`KineticSimulation`/`MetropolisSimulation`; `SimulationTests` checks defaults and round-trip.
- [x] New simulations use the documented defaults, and every hard range and recommended band has a diagnostic
      and a boundary test. — all `SIM004`–`SIM037` diagnostics exist and `SimulationBoundaryTests` covers the
      hard limits; exact recommended-band boundary acceptance is only partly exercised (F1).
- [x] The lattice/simulation sections round-trip through the project JSON context, the canonical model, save,
      and load; dopings preserve priority order and simulations preserve kind-specific settings and
      authored-order transition arrays. — `LatticeModelTests`, `SimulationTests`,
      `LatticeSimulationPersistenceTests`, and `ProjectStoreTests`.
- [x] Lattice/simulation edits change `ProjectHasher.Compute` and `ComputeDocument`; no section is dropped by
      canonicalization, empty arrays are omitted, and a pre-Step-06 project keeps its canonical hash. —
      `LatticeModelTests.LatticeEditsChangeModelAndDocumentHashes`,
      `LatticeSimulationPersistenceTests.PreStep06ProjectKeepsItsCanonicalModelAndDocumentHashes`, and the
      empty-array omission tests.
- [x] The generated project schema accepts the flat lattice/simulation sections and rejects unknown fields;
      fixtures cover empty and populated models. — `ProjectSchemaTests` covers all four fixtures plus the
      lattice/simulation samples and unknown-field rejection.
- [x] Public API files and the ApiCompat baseline are current. — `./tools/verify.sh api` passed all nine
      ApiCompat checks.
- [x] Documentation written. — strict docs build passes; the two user-guide pages, reference/development pages,
      navigation, indexes, status dashboard, and parity rows are updated.

## Standard audit checklist

- [x] Required tests from the step page pass. — the committed suite is green (Core 561, Symmetry 215, Packages 59,
      Architecture 1, plus the support suites); F1 records the missing PR-medium property row and asymmetric
      edge-case coverage.
- [x] Public/protected XML documentation policy is followed. — DocFX builds 229 managed-reference files with
      0 warnings/0 errors; the new public surface is in `PublicAPI.Unshipped.txt` and ApiCompat passes.
- [x] Documentation, status dashboard, feature log, and parity matrix are current. — Zensical strict passes and
      the step page, dashboard, feature log, parity rows, and published pages match the frozen state.
- [x] Closeout CI is green at the audited commit. — GitHub Actions run `43085605850` is
      `completed/success` at `ea6293e`; only the single step-06 closeout dispatch exists.
- [x] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code. — the diff has no P/Invoke, native interop, GUI, path/case/locking, or OS-conditional code; the
      Ubuntu-only policy applies.
- [x] Format/version/fixture changes are documented and tested. — additive project-v1 lattice/simulation fields;
      manifest status/notes, generated schema, descriptors, and the four fixtures are current; the pre-Step-06
      hash-stability test passes.
- [x] Security, dependency, licence, and secret-scan gates pass. — contract/policy gate: artifacts current,
      SPDX headers in 285 source files, no secret patterns in 616 tracked text files, cargo-deny and NuGet
      checks clean.
- [x] Code structure policy passes; no new root-level or catch-all folders. — `./tools/verify.sh structure`
      passes; the new `Lattice/`, `Simulations/`, and `Jobs/` concept folders are within the limits.
- [x] No unrelated changes or unapproved scope. — the 79-file diff is limited to Step 06 model, persistence,
      editing, contracts, API baseline, samples, tests, and documentation; no preview service, GUI, legacy
      import, or package writer was added.
- [x] Branch, tag, and status hygiene are correct. — branch `step/06-lattice-and-simulation-models`; no `step-06`
      tag and no main merge; the only step-branch CI run is the closeout dispatch; the record parent is the
      docs-only request commit `0304b40` (N2).

## Evidence

### Audit round 1

**Reviewer context and independence.** I am a fresh audit agent. I did not plan, implement, or review Step 06 and
have no shared conversation with the planner, implementor, or step agent. I read `AGENTS.md` (§3.8, §3.10,
§3.12, §3.13, §6, §8, §10–§13, §16), the audit README and template, this request, the Step 06 page, the Step 06
design-review record, `docs/planning/status.md`, `docs/planning/appendix-b-parity-matrix.md`,
`docs/planning/02-data-contracts.md`, `contracts/manifest.json`, `contracts/generated/schema/project-v1.schema.json`,
`contracts/generated/fixtures/descriptors.json`, the Step 03/04/07/10/11/25 dependency pages, the complete
`b8b54b7..ea6293e` diff, the source and tests, the samples, and the published pages. I did not rely on the
request summary for the verdict.

**Frozen state and worktree.** Worktree `/workspace/mocassin`, branch `step/06-lattice-and-simulation-models`,
`HEAD` `0304b40` (the docs-only audit request), `git status --short` empty. The audited code commit is
`ea6293efa2cc120ca57d87dd0b1b765c1f6976fa`; `git merge-base --is-ancestor ea6293e HEAD` exits 0.
`git diff --name-status ea6293e..0304b40` lists only `docs/planning/audits/step-06.md`, `docs/planning/status.md`,
and `docs/planning/steps/step-06-lattice-and-simulation-models.md`; no code, test, contract, or sample changed
after the freeze. The record commit parent is therefore the docs-only request commit `0304b40`, while the code
audited is frozen at `ea6293e` (N2).

**Commands and results (from `/workspace/mocassin`):**

```text
git status --short
# (no output: clean worktree)

git log --oneline -8
# 0304b40 docs: request step-06 independent audit [skip ci]
# ea6293e step-06: closeout status, audit request, and final checks
# 46ea087 step-06: add lattice and simulation samples and docs
# cfeb7b6 step-06: add lattice and simulation editing operations
# 48e1f20 step-06: persist lattice and simulation contracts
# 1c64c96 step-06: add simulation model and validation
# bc333f3 step-06: add lattice model, persistence, and validation
# b8b54b7 docs: record step-06 design approval and review [skip ci]

./tools/verify.sh dotnet
# EXIT:0. Build succeeded, 0 Warning(s), 0 Error(s); dotnet format --verify-no-changes clean.
# Tests: Core 561, Symmetry 215, Packages 59, Architecture 1, Contracts 1, Results 1, Compilation 1, App 1.

./tools/verify.sh contracts
# EXIT:0. Contract artifacts are current (17 files); space-group catalog current (274 settings);
# SPDX headers present in 285 source files; no secret patterns in 616 tracked text files;
# cargo-deny advisories/bans/licenses/sources ok.

./tools/verify.sh api
# EXIT:0. No vulnerable NuGet packages. ApiCompat passed for all nine projects; the checked-in baseline matches.

./tools/verify.sh structure
# EXIT:0. Mocassin.Architecture.Tests passed (1 test).

./tools/verify.sh docs
# EXIT:0. Zensical strict: no issues; DocFX: 230 models, 0 warnings, 0 errors; rustdoc built.

./tools/verify.sh rust
# EXIT:0. cargo fmt --check, clippy, tests, and doc-tests pass.

./tools/verify.sh python
# EXIT:0. pytest passes; sdist and wheel build successfully.

git status --short
# (no output after all gates: build output is ignored)

for f in samples/lattice/*.mocprj samples/simulations/*.mocprj; do
  dotnet run --project src/dotnet/Mocassin.Cli -c Release --no-build -- validate "$f"
done
# EXIT:0 for ceria-reduction, ceria-hydration, bzo-doping, ceria-kmc, and ceria-mmc.
```

**Closeout CI (GitHub API; token not printed).** Run `43085605850`: name `CI`, event `workflow_dispatch`,
branch `step/06-lattice-and-simulation-models`, `head_sha` `ea6293efa2cc120ca57d87dd0b1b765c1f6976fa`,
status `completed`, conclusion `success`. The recent-runs query shows no workflow run for the docs-only
`0304b40` and no per-feature step runs.

**Code and artifact review.** I inspected `ModelDefinition` and `ModelDefinitionJsonConverter`; the canonical
save/write/hash path (`ProjectJson.CanonicalizeModel` 95–146, `ProjectJsonConverter.Write` 325–380,
`ProjectHasher.Compute`/`ComputeDocument`); the lattice records, derived building block, query, validator, and
editing operations; the simulation records, `Kind`, `ExecutionFlags` converter, `SimulationCanonicalizer`,
`SimulationJsonConverter`, validator, and simulation editor operations; `SolverJobFlags` and
`SolverJobFlagsMapper`; the project validator/composition; the Core/Packages tests; the four contract fixtures,
manifest, schema, and descriptors; the five samples; `zensical.toml`; the user-guide, reference, and development
pages; `status.md`; and the parity rows. I traced each acceptance item to code and test evidence rather than the
feature log.

**Scope checks.** The diff is limited to Step 06 lattice/simulation model, persistence, editing, contracts,
API baseline, samples, tests, and documentation. There is no lattice/job-count preview, no fraction application
or truncation, no `.msl` writer, no GUI, and no legacy import. `Mocassin.Compilation` references Core and
Symmetry only; `Mocassin.Contracts` references Compilation; `Mocassin.Packages` references Compilation,
Contracts, and Core; `Mocassin.Results` references Core and Contracts. No new third-party dependency was added.

## Known notes for the auditor

- No lattice or job-count preview exists in Step 06 by design; Step 10 owns the single preview service and the
  shared supercell/doping generator.
- The simulation persisted shape is intentionally flat at the top level with a `settings` object. The generated
  schema cannot express a `oneOf` discriminator, so kind-specific fields are optional in the schema and the
  validator (`SIM023`) rejects a mismatched shape.
- Step 06 adds nullable support to `tools/contracts/generate.py` for the simulation settings that use `null`
  (`timeLimitSeconds`, `minSuccessRate`, `rngSeed`, `instruction`, `normalizationEnergyEv`,
  `maxAttemptFrequencyHz`). Existing generated artifacts are unchanged by the feature.
- Step 10 must cross-check the lattice preview against the generated lattice's doping report; Step 06 does not
  claim that cross-check.
- Windows and macOS were not run because Step 06 has no platform-specific code; this is the Ubuntu-only policy.
- `SolverJobFlags` composition is tested in `Mocassin.Packages.Tests`; the `.msl` writer itself is Step 11.

## Findings

### Audit round 1 findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **F1 — The reviewed test plan's PR-medium property row and some boundary/kind-coverage rows are not fully delivered.** No FsCheck `[Property]` test exists under `Mocassin.Core.Tests/Lattice` or `Mocassin.Core.Tests/Simulations`, so the required “Random valid doping/simulation models round-trip canonically and preserve order” sweep is absent; the only FsCheck property tests in `Mocassin.Core.Tests` are `Transitions/TransitionPropertyTests.cs` and `GenericMathTests.cs`. `SIM002` is asserted only KMC→MMC (`SimulationModelTests.Sim002WrongTransitionKindIsReported`, lines 36–42); the MMC→KMC direction is correct by code inspection (`SimulationModelValidator.cs:160–193`) but untested. The `SIM023` JSON test injects an MMC-only setting into a KMC simulation (`SimulationTests.PersistedKindShapeMismatchIsDetectedFromJson`, lines 285–299); the wrong-kind transition-array case is not asserted. Exact recommended-band boundary acceptance values (for example 100/5000 K, target 1000, job 5/100, field 1e4/1e9 V/m, break sample 10,000, interval 100, result sample 10,000, 10 transitions, 10 MCS/s) are only partly exercised; `SimulationBoundaryTests` mostly covers the hard limits. The behaviour is correct by inspection and the deterministic round-trip/order tests cover the core paths, but the required test rows are incomplete. Fix or explicitly accept. | Step page tests table lines 605–623; `src/dotnet/Mocassin.Core.Tests/Lattice/` and `Simulations/` contain no `[Property]`/FsCheck usage; `SimulationModelTests.cs:36–42, 247–254`; `SimulationTests.cs:285–299`; `SimulationBoundaryTests.cs:13–188`. |
| **Note** | **N1 — In-memory `Project.Dopings` is not sorted by priority even though its XML doc and `ILatticeQuery` say it is.** `Project.Dopings => Model.Dopings` and `ProjectEditor.AddDoping` append in call order; only `ProjectJson.CanonicalizeModel` sorts by `(Priority, Id)` before save/hash. The acceptance criterion and hash tests pass because serialization is canonical, but a same-snapshot consumer that reads `Dopings` before saving receives authored/insertion order. Align the doc or sort on read; Step 10 should not assume array order without sorting. | `src/dotnet/Mocassin.Core/Project/Project.cs:180–182`; `src/dotnet/Mocassin.Core/Model/Lattice/ILatticeQuery.cs:18–21`; `src/dotnet/Mocassin.Core/Editing/ProjectEditor.Lattice.cs:119–127`; `src/dotnet/Mocassin.Core/Project/ProjectJson.cs:139–144`. |
| **Note** | **N2 — Record-parent process note.** The audited code is frozen at `ea6293e`, but the record commit's parent is the docs-only audit-request commit `0304b40`; `git diff ea6293e..0304b40` changes only the audit record, status dashboard, and step-page status block. The frozen code and reviewed behavior are exactly `ea6293e`. | `git diff --name-status ea6293e..0304b40`; `git show -s --format='%H %P %s' 0304b40`. |
| **Note** | **N3 — The execution-flag rejection mechanism is described as `SIM035`, but the project reader rejects duplicate/derived/unknown flag names with `JsonException` before validation.** `ExecutionFlagsJsonConverter.Read` rejects duplicates, numeric names, `None`, combined values, and names outside the authored mask; `SIM035` only covers an in-memory `ExecutionFlags` value with a derived/compiler bit. The rejection is safe and tested both ways, but the user guide (“`LAT`/`SIM` validation rejects duplicate, derived, or unknown flags”) and the `SIM035` row overstate where the rejection occurs. Align the wording or carry these cases into the diagnostic path. | `ExecutionFlagsJsonConverter.cs:38–81`; `SimulationModelValidator.cs:309–316`; `SimulationTests.DuplicateExecutionFlagNamesAreRejected`/`DerivedExecutionFlagNamesAreRejected`; `docs/user-guide/simulations.md:101–103`; step page line 382. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Verdict

- [ ] Passed
- [x] Passed with notes
- [ ] Fixes required
- [ ] Re-audit required
- [ ] Waived

Follow-up owner: Step 06 agent (fix F1 or record explicit human acceptance of F1 and N1–N3); human owner for the
approval gate.
Follow-up issue/step: Step 06 — Lattice and Simulation Models. If the notes are accepted, proceed to the Step 06
merge approval with the §3.12 cumulative approval report. If F1 is fixed, use a new feature branch, squash into
the step branch, dispatch one new closeout CI run, and request a fresh re-audit.

**Verdict: Passed with notes.** I did not author or implement this step and have no shared conversation with the
planner, implementor, or step agent. No Blocker or Major finding remains. The lattice model, ordered dopings,
derived building block, simulation kinds/defaults/limits, canonical persistence and hashing, empty-array omission,
authored-order transition arrays, flat simulation DTO with `SIM023` shape rejection, `SolverJobFlags`
composition, contracts/schema/fixtures, samples, documentation, parity rows, and status dashboard satisfy the
frozen design. The full local gate passes (dotnet, api, structure, docs, contracts, rust, python) and the single
Ubuntu closeout CI run `43085605850` is green at `ea6293e`. F1 is Minor (incomplete PR-medium property and
asymmetric boundary/kind test coverage); N1–N3 are notes (in-memory doping order wording, the docs-only record
parent, and the flag-rejection wording). The human owner must explicitly accept F1 and N1–N3 or request fixes; a
fix commit requires a fresh re-audit. This audit is evidence, not merge authorization.

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| F1 | Accepted by human owner on 2026-11-11 as a test-coverage follow-up (Minor, no behavioural defect) | Step 06 agent / human owner | — |
| N1 | Accepted by human owner on 2026-11-11 with a wording follow-up for in-memory doping order | Step 06 agent / human owner | — |
| N2 | Acknowledged — process note; frozen code is `ea6293e` | Step 06 agent | — |
| N3 | Accepted by human owner on 2026-11-11; reader-level rejection is intentional and `SIM035` covers out-of-mask in-memory values | Step 06 agent / human owner | — |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Approval summary

The step agent completes this section before requesting human approval. It must cover the full history, not
only the latest round.

- [x] Requested decision and frozen commit recorded — accept the audit verdict `Passed with notes` for the
      frozen code commit `ea6293e`, then approve the Step 06 `--no-ff` merge into `main`. Tagging `step-06` is a
      separate post-merge step and requires a green automatic main CI run.
- [x] Every review/audit round recorded — six design-review rounds and one audit round are listed below; the
      full per-finding text is in `docs/planning/reviews/step-06.md` and the audit findings above.
- [x] Every finding recorded — the design-review groups M1–M7, m1–m7, R2-1–R2-8, R3-1–R3-3, R4-1–R4-4,
      R5-1–R5-6, and R6-1–R6-4 are resolved or explicitly accepted; audit F1 and N1–N3 are recorded above.
- [x] Accepted notes and waivers recorded — design residuals R6-1 (Step 10/11 carrier detail, now implemented
      by `SolverJobFlagsMapper`), R6-2 (deterministic wording, corrected in this step), R6-3 (Step 10 preview
      placement), and R6-4 (process note) were accepted by the human owner on 2026-11-10.
- [x] Residual risks, deferred items, and follow-up owners recorded — Step 10 owns fractions, truncation,
      compensation counts, the single generator/preview, and effective job counts; Step 11 owns the `.msl`
      writer; Step 25 owns the GUI editors; Step 07 owns the consolidated diagnostics. F1 and N1/N3 are
      accepted by the human owner on 2026-11-11 with follow-up wording/test-coverage ownership.
- [x] Explicit no-Blocker/no-Major statement — no Blocker or Major finding remains in the design review or the
      audit. F1 is Minor (test-coverage completeness); N1–N3 are Notes. The human owner is asked to accept them
      explicitly or request a fix/re-audit.
- [x] Final CI and documentation-build results recorded — `./tools/verify.sh all` passed on the step branch;
      Zensical strict, DocFX 0/0, rustdoc, Rust, Python, contracts, API compatibility, and structure gates
      passed; the single Ubuntu closeout CI run `43085605850` is green at `ea6293e`.

Round history:

| Round | Date | Commit | Reviewer | Verdict |
| --- | --- | --- | --- | --- |
| 1 | 2026-11-10 | `a64ecf3` | Independent planning auditor (fresh context) | Changes required |
| 2 | 2026-11-10 | `160b226` | Fresh independent re-reviewer | Ready with notes |
| 3 | 2026-11-10 | `5ae32b0` | Fresh independent re-reviewer | Ready with notes |
| 4 | 2026-11-10 | `fe4868a` | Fresh independent re-reviewer | Ready with notes |
| 5 | 2026-11-10 | `e587a6b` | Fresh independent re-reviewer | Ready with notes |
| 6 | 2026-11-10 | `6173e61` | Fresh independent re-reviewer | Ready with notes — accepted |
| A1 | 2026-11-11 | `ea6293e` | Independent audit agent (fresh context) | Passed with notes |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| M1–M7 | Major | Round-1 design findings: canonical save/hash path, incomplete settings/defaults, incomplete limits, Step 10-owned rules in Step 06, preview placement/API, and transition-order conflict | Design revision; per-finding detail in the review record | `160b226` | Resolved |
| m1–m7 | Minor | Round-1 design findings: polymorphism/flag invariants, layout names, query integration, manifest/fixtures, scope/audit coverage, test plan, rule consistency | Design revision; per-finding detail in the review record | `160b226` | Resolved |
| R2-1–R2-3 | Minor | Round-2 design findings: `SIM036`, `SIM037`, and Step 25 write-out setting | Small corrections | `5ae32b0` | Resolved |
| R2-4–R2-8 | Note | Round-2 notes: write-out-0 deviation, `.msl` default, max-frequency attribution, non-finite rules, record parent | Corrections/acknowledgement | `5ae32b0` | Resolved/acknowledged |
| R3-1 | Minor | `MinSuccessRate` upper hard bound | `SIM012` upper bound added | `fe4868a` | Resolved |
| R3-2–R3-3 | Note | `Deterministic` semantics and deviation placement | Corrected | `fe4868a` | Resolved |
| R4-1 | Minor | `SolverJobFlags` carrier and derived-bit table | Added; implemented by `SolverJobFlagsMapper` in Step 06 | `e587a6b`, `48e1f20` | Resolved |
| R4-2–R4-4 | Note | Deterministic consumer, normalization canonicalization, dashboard ownership | Corrected/acknowledged | `e587a6b` | Resolved/acknowledged |
| R5-1 | Minor | Empty-array omission and pre-Step-06 hash stability | Rule and fixture test added | `6173e61`, `48e1f20` | Resolved |
| R5-2 | Minor | Canonical `0`/`null` values | Canonicalizer and tests added | `6173e61`, `1c64c96` | Resolved |
| R5-3–R5-6 | Note | Deterministic wording, composition boundary, `SIM009` whitespace, process note | Corrected/acknowledged | `6173e61` | Resolved/acknowledged |
| R6-1 | Minor | Compilation-side carrier/`default_settings.flags` split | Accepted residual; `SolverJobFlagsMapper` and tests implement the composition boundary | `48e1f20` | Resolved/accepted |
| R6-2–R6-4 | Note | Data-contract deterministic wording, Step 10 preview placement, process note | Accepted; wording corrected in this step, placement is Step 10, process note acknowledged | `46ea087` | Resolved/accepted |
| F1 | Minor | Audit: PR-medium property sweep and asymmetric boundary/kind coverage | Accepted as a test-coverage follow-up; no behavioural defect found | — | Accepted 2026-11-11 |
| N1 | Note | Audit: in-memory `Project.Dopings` order wording | Accepted with a wording follow-up; canonical persistence sorts | — | Accepted 2026-11-11 |
| N2 | Note | Audit: record parent is the docs-only request commit | Acknowledged; frozen code is `ea6293e` | — | Acknowledged |
| N3 | Note | Audit: flag rejection occurs in the reader rather than only `SIM035` | Accepted; reader rejection is intentional and `SIM035` covers out-of-mask in-memory values | — | Accepted 2026-11-11 |

## Human approval

- [ ] Accepted: Passed
- [x] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by: Human owner
Date: 2026-11-11
