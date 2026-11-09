# Step 04 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-04.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Audit
> status`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 04 — Transition Model |
| Request owner | Step agent |
| Audit initiated by | Human owner |
| Audit status | Re-audit required |
| Audited commit | `8c44709` |
| Step branch | `step/04-transition-model` |
| Step tag | — |
| Closeout CI run | [`42695864895`](https://github.com/seb-eis/mocassin-2/actions/runs/42695864895) — green at `8c44709` |
| Platform CI | Not run (Ubuntu-only policy) |
| Audit date | 2026-11-09 |
| Auditor | Independent audit agent (fresh context; no shared conversation with the planner or implementor) |

Step page: [`Step 04 — Transition Model`](../steps/step-04-transition-model.md)

## Re-audit request

Round 1 returned **Passed with notes** at code commit `288dd37`. The step agent fixed F1–F4 and N1/N4 on
`feature/step-04-transition-model/fix-audit-findings`, squashed the result as `ebefc96`, ran
`./tools/verify.sh all`, and dispatched one replacement closeout CI run (green at `ebefc96`). N2 (local
benchmark evidence) and N3 (docs-only record-parent delta) remain accepted process notes. A fresh re-audit of
the complete step at `ebefc96` is requested; the reviewer should append a new round and preserve round 1
unchanged.

### Round 3 re-audit request

Round 2 returned **Passed with notes** at code commit `ebefc96` and recorded F5–F7 (Minor) plus N5 (Note). The
step agent fixed all four on `feature/step-04-transition-model/fix-reaudit-findings`, squashed the result as
`8c44709`, ran `./tools/verify.sh all`, and dispatched one replacement closeout CI run (green at `8c44709`).
N2 (local benchmark evidence) and N3 (docs-only record-parent delta) remain accepted process notes. A fresh
re-audit of the complete step at `8c44709` is requested; append a new round and preserve rounds 1–2 unchanged.

## Step summary

| Field | Value |
| --- | --- |
| Phase | B. Model definition |
| Depends on | 03 |
| Delivers | State changes, state-change groups, abstract chains, and KMC/MMC transitions bound to reference geometry, with validation of transition semantics. |

## Step-specific acceptance checklist

- [x] Ceria oxygen migration can be defined as a 3-site chain with `[O,V_O]`, `[O,∅]`, `[O,V_O]` and a
      stable-unstable-stable path whose endpoints are symmetry-equivalent O orbit members, not only the single
      authored O representative. — `samples/transitions/ceria-kmc.mocprj` validates; endpoint `[0.25,0.25,0.75]`
      resolves to the O 8c orbit of the authored `[0.25,0.25,0.25]` site; `TransitionSampleTests` covers it.
- [x] A 2-site MMC exchange `[A,B]` can be defined and previewed, including a same-site exchange. — the
      `ceria-mmc` sample uses `SiteA == SiteB`; `ValidMetropolisPreviewSupportsSameSiteExchange` covers it.
- [x] A 5-site vehicle with five chain groups (`[A,V_A]`, `[A,V_A]`, `[AB,∅]`, `[B,V_B]`, `[B,V_B]`),
      a `D,S,S,D` chain, and an `S,S,U,S,S` reference path validates. — `vehicle5.mocprj` uses five group slots
      (shared group IDs) with `D,S,S,D` and an `S,S,U,S,S` path; the sample test validates it.
- [x] A 5-site interstitialcy with an `S,U,S,U,S` path validates. — `interstitialcy5.mocprj` validates.
- [x] Invalid paths (unstable endpoint, consecutive unstable, pattern-sequence mismatch, any repeated
      encoded position, unmatched point) are rejected with precise diagnostics. — covered by `TransitionPathTests`
      and `TransitionValidationTests`; TRN020 ambiguity `Related` detail is F3.
- [x] A complex transition without a reverse link warns `TRN018`; adding a symmetric reverse link clears it. —
      `ComplexTransitionWithoutReverseLinkWarns` and `SymmetricComplexReverseLinksClearTheWarning`.
- [x] `GeneratesReverse` set inconsistently with the pattern is rejected with `TRN024`. —
      `GeneratesReverseMismatchIsReported`.
- [x] The preview enumerates the expected symmetry-equivalent paths and echoes `(A, B, C)` and the transition
      reference. — ceria-kmc preview expands to 96 paths, ceria-mmc to 8 pairs; tests assert the reference and
      `(A,B,C)` echo. The brute-force comparison test row is part of F1.
- [x] An invalid transition returns `null` and diagnostics from `TryCreatePreview`; no partial preview is
      returned. — path/site invalid cases return null with diagnostics; F2/F4 record Core-only definition and
      default-array boundary gaps.
- [x] `StateChangeMatrices` has the documented dimensions, particle order, binary encoding, and
      stable/unstable cardinality. — `ValidKineticPreviewDescribesPathMatricesAndCounts` asserts 3 matrices,
      16 entries, particle order, binary encoding, and unstable cardinality.
- [x] The transition sections round-trip through save/load, the project JSON context, and the canonical model;
      semantic arrays preserve order and transition edits change the document hash. — `TransitionPersistenceTests`
      and `TransitionModelTests` cover save/load, canonical round-trip, semantic order, and hash changes.
- [x] The generated project schema accepts the transition sections, and fixtures cover empty and populated
      models. — `ProjectSchemaTests` includes `project-minimal` plus `project-transitions-minimal` and
      `project-transitions-populated`; unknown-transition-field rejection is part of F1.
- [x] Public API files and the ApiCompat baseline are current. — `./tools/verify.sh api` passes all nine
      ApiCompat checks; `PublicAPI.Unshipped.txt` contains the new surface.
- [x] Documentation written. — user-guide, physics, development, and reference pages are present and linked
      from the navigation and section indexes; the strict docs gate passes.

## Standard audit checklist

- [x] Required tests from the step page pass. — the committed suite is green; F1 records missing
      property/cache/brute-force/schema test rows. — audited on the full local suite and closeout CI.
- [x] Public/protected XML documentation policy is followed. — DocFX builds 190 files with 0 warnings/0 errors;
      all new public types have XML docs; ApiCompat passes.
- [x] Documentation, status dashboard, feature log, and parity matrix are current. — step, audit status,
      dashboard, feature log, parity rows, and published pages match the closeout state; N1 is a parity-status
      semantics note, not a stale-state defect.
- [x] Closeout CI is green at the audited commit. — run `42690361556` is `completed/success` at `288dd37`;
      jobs .NET, Contracts and policy, and Public API compatibility passed; Rust/Python were skipped by path
      filters after the local `verify.sh all` pass.
- [x] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code. — Step 04 adds no P/Invoke, native, GUI, path/case/locking, or OS-specific code; the Ubuntu-only
      policy applies.
- [x] Format/version/fixture changes are documented and tested. — additive project-v1 fields; manifest
      status/notes, generated schema, descriptors, and both transition fixtures are current; empty sections are
      omitted to preserve Step 01/03 hashes.
- [x] Security, dependency, licence, and secret-scan gates pass. — NuGet licence/vulnerability policy OK for 47
      packages; SPDX headers present (215 files); no secret patterns (521 files); cargo-deny advisories/bans/
      licenses/sources OK.
- [x] Code structure policy passes; no new root-level or catch-all folders. — `./tools/verify.sh structure`
      passes; new `Transitions/` concept folders are within the file-count/depth limits.
- [x] No unrelated changes or unapproved scope. — the 87-file diff is limited to Step 04 transition code,
      tests, contracts, samples, benchmarks, and docs; no Step 05/06/08, GUI, rule-generation, or legacy-import
      work.
- [x] Branch, tag, and status hygiene are correct. — branch `step/04-transition-model`; no `step-04` tag and no
      main merge; the only step-branch CI run is the closeout run; no per-feature runs. Record parent is the
      docs-only `8869f3d`; audited code commit is `288dd37` (N3).

## Evidence

### Step-agent evidence (request)

Commands run:

```text
./tools/verify.sh all
  .NET: build succeeded; Core 336 passed, Symmetry 181 passed, Packages 42 passed,
        Architecture 1 passed, support suites passed; dotnet format clean
  Public API: all ApiCompat checks passed; no vulnerable packages
  Rust: fmt, clippy, and tests passed
  Python: 3 pytest tests passed; package built
  Documentation: Zensical strict, DocFX, and rustdoc passed
  Contracts: artifacts current (17 files), catalog current, SPDX headers present,
             no secrets, cargo-deny advisories/bans/licenses/sources ok

Closeout CI: run 42690361556, workflow_dispatch on step/04-transition-model,
Ubuntu, completed/success at 288dd37.
```

### Independent audit evidence (round 1)

**Independence and scope.** I am a fresh audit agent. I did not plan, implement, or review Step 04 and have
no shared conversation with the planner or implementor. I read `AGENTS.md` (§3.8, §3.10, §3.12, §4, §5, §7,
§8, §9, §10, §11, §12, §16), `docs/planning/audits/README.md`, the audit template, this request, the step
page, the frozen design at `a3d8641`, the design review, the transition section of
`docs/planning/02-data-contracts.md`, `docs/planning/03-quality-strategy.md`, `docs/development/code-structure.md`,
ADRs 0009/0010/0013, the status dashboard, the parity matrix, the contract manifest/generated schema/fixtures,
the samples, the public API files, and the full Step 04 implementation and test diff. I did not rely on the
step-agent evidence or the request summary for the verdict.

**Frozen state.** Worktree `/workspace/mocassin`, branch `step/04-transition-model`, `HEAD` `8869f3d`. The
audited code commit is `288dd37` (`288dd37b3927e9b00bfec382b30c6a98ce07808c`). `git status --short` is empty.
`git diff 288dd37..8869f3d --stat` touches only `docs/planning/audits/step-04.md` (23 lines),
`docs/planning/status.md` (15 lines), and `docs/planning/steps/step-04-transition-model.md` (8 lines); the
delta is closeout status and audit-request metadata only. The record commit parent is therefore the docs-only
request commit `8869f3d`, while the code audited is frozen at `288dd37` (N3).

**Commands and results (from `/workspace/mocassin`):**

```text
git status --short
# (no output; clean worktree)
git log --oneline --decorate -12
# 8869f3d (HEAD -> step/04-transition-model, origin/step/04-transition-model) docs: record step-04 closeout and request audit [skip ci]
# 288dd37 step-04: prepare closeout and audit request
# ec04f9d step-04: add transition samples and user documentation
# ... (nine Step 04 squash commits)
# 34eff24 (main) docs: record step-04 approved feature list [skip ci]
git diff 34eff24..288dd37 --stat
# 87 files changed, 9504 insertions(+), 91 deletions(-); all files are Step 04 code, tests, contracts,
# samples, benchmark evidence, or documentation.
git diff 288dd37..8869f3d --stat
# docs/planning/audits/step-04.md                 | 23 +++++++++++++++++-----
# docs/planning/status.md                         | 15 ++++++-------
# docs/planning/steps/step-04-transition-model.md |  8 ++++----
./tools/verify.sh dotnet
# EXIT 0; build succeeded with 0 warnings/0 errors; dotnet format clean; Core 336/336,
# Symmetry 181/181, Packages 42/42, Architecture 1/1, App/Compilation/Contracts/Results 1/1 each.
./tools/verify.sh structure
# EXIT 0; Architecture 1/1 passed.
./tools/verify.sh api
# EXIT 0; NuGet licence policy OK (47 packages); no vulnerable packages; ApiCompat passes Core,
# Symmetry, Compilation, Contracts, Packages, Results, App.ViewModels, App.Views, and Rendering;
# "Public API is compatible with the checked-in baseline."
./tools/verify.sh contracts
# EXIT 0; "Contract artifacts are current (17 files)"; space-group catalog current (274 settings,
# sha256 1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395); SPDX headers present
# in 215 source files; no secret patterns in 521 tracked text files; cargo-deny advisories/bans/
# licenses/sources ok.
./tools/verify.sh docs
# EXIT 0; Zensical strict "No issues found"; DocFX 190 files with 0 warnings/0 errors; rustdoc built.
./tools/verify.sh all
# EXIT 0; all of the above plus Rust fmt/clippy/tests and Python 3 passed plus package build.
```

**Closeout CI (GitHub API; no token printed).** Run `42690361556`: name `CI`, event `workflow_dispatch`,
branch `step/04-transition-model`, `head_sha` `288dd37b3927e9b00bfec382b30c6a98ce07808c`, status
`completed`, conclusion `success`. Jobs: `Detect changes`, `.NET (ubuntu-latest)`, `Contracts and policy`, and
`Public API compatibility` all `success`; `Python` and `Rust` were `skipped` by the path filters. The
recent-runs query shows no workflow run for the docs-only `8869f3d` and no per-feature step runs, matching the
CI cost policy.

**Targeted probes (scratch project under `/tmp/opencode/step04probe`, not committed).** All five transition
samples validate with empty Core and composed diagnostics, and every preview is non-null: `ceria-kmc` 96
expanded paths/expected rules, `ceria-mmc` 8 expanded site pairs, and `interstitialcy5`, `vehicle5`, and
`polaron-hop` 1 each. A mixed-role group (one stable-role pair plus one `[D, ∅]` pair) produced `TRN025` from
`ProjectValidator`/`ProjectValidationService`, but `TransitionPreviewService.TryCreatePreview` still returned a
preview with no diagnostics (F2). A duplicate-site structure produced `TRN020` with an empty `Related` list
(F3). A project with `Model.Particles = default` or `Model.Chains = default` made `TryCreatePreview` throw
`InvalidOperationException`/`NullReferenceException`, while `ProjectValidationService.Validate` returned
normally (F4).

**Files reviewed.** `AGENTS.md`; `docs/planning/audits/README.md`; `docs/planning/audits/step-audit-template.md`;
`docs/planning/audits/step-04.md`; `docs/planning/steps/step-04-transition-model.md` at `a3d8641` and HEAD;
`docs/planning/reviews/step-04.md`; `docs/planning/02-data-contracts.md` (model envelope and transitions section);
`docs/planning/03-quality-strategy.md`; `docs/development/code-structure.md`; `docs/development/compilation-pipeline.md`;
ADRs 0009, 0010, 0013; `docs/planning/status.md`; `docs/planning/appendix-b-parity-matrix.md`;
`docs/reference/transitions.md`, `project-format.md`, `index.md`; `docs/user-guide/transitions.md`, `index.md`;
`docs/physics/kmc.md`, `mmc.md`, `monte-carlo.md`, `index.md`; `contracts/manifest.json`,
`contracts/generated/schema/project-v1.schema.json`, `contracts/generated/fixtures/descriptors.json`;
`tests/contracts/project-transitions-minimal.mocprj`, `project-transitions-populated.mocprj`,
`project-ceria.mocprj`; `samples/transitions/*.mocprj`, `samples/ceria/ceria.mocprj`; `zensical.toml`;
all Step 04 source under `Mocassin.Core/Model/Transitions`, `Mocassin.Core/Diagnostics`,
`Mocassin.Core/Editing/ProjectEditor.Transitions.*`, `Mocassin.Core/Project`, `Mocassin.Symmetry/Transitions`,
and `Mocassin.Packages/Projects`; the corresponding Core/Symmetry/Packages tests; the benchmark and
`tests/reference/bench/baselines.json`; and the three `PublicAPI.Unshipped.txt` files.

**Scope checks.** I checked the step-specific acceptance criteria and the standard checklist; transition
envelope, persistence, canonicalization/hash, queries, editing, definition/path validation, preview service
and cache, benchmark baseline, contract schema/fixtures, samples, docs, parity rows, and status dashboard;
non-goals (no Step 05/06/08 work, no GUI, no rule generation, no legacy import); public API/XML docs/ApiCompat;
fixture/schema round-trips; additive-v1 hash behavior; and that `TransitionPreview` is never persisted. The
findings record the result.

## Known notes for the auditor

- Empty transition sections are omitted from the written model so transition-free Step 01/03 projects keep
  their existing canonical model/document hashes. Non-empty sections are written and hashed; `schemaVersion`
  remains 1 under the additive-v1 precedent.
- `TransitionPreview` is derived data and is never persisted.
- The transition preview benchmark is a local-container measurement; the declared GitHub runner class is
  re-verified only by the manual Benchmark CI job.
- Platform CI is not run; Step 04 adds no platform-specific code.
- The transition-preview cache is bounded and scoped to the service; capacity breach is an explicit failure.
- The audit request/status update is documentation-only; the code audited by the closeout run is frozen at
  `288dd37`.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **F1 — Several required Tests-table rows are not implemented.** The frozen step page requires property tests for random transition models and random reference paths against a brute-force site/stability oracle, preview cache-key separation for structure hash, transition hash, and symmetry catalog/contract versions, a symmetry-expanded path count compared with brute force and non-trivial MMC pair expansion, and a schema test that rejects unknown transition fields. No `[Property]`/FsCheck test exists in the transition test folders; `ModelPropertyTests` generates only particles/sets/sites; the only preview-cache test covers `(A,B,C)` and a hit; the P1 preview test asserts a single expanded path rather than a brute-force count; and `ProjectSchemaTests` has no unknown-transition-field case. The deterministic tests for the same behaviours pass, and the cache key includes every required component by inspection. | Step page lines 710-711, 716, 720; `Mocassin.Core.Tests/Transitions/*`; `Mocassin.Symmetry.Tests/Transitions/*`; `Mocassin.Core.Tests/Model/ModelPropertyTests.cs`; `Mocassin.Symmetry.Tests/Transitions/TransitionPreviewTests.cs` 108-125; `Mocassin.Core.Tests/ProjectSchemaTests.cs`. |
| **Minor** | **F2 — The preview returns a preview for a Core-invalid mixed-role group, contrary to the documented invalid-transition boundary.** `TransitionPreviewService.TryCreatePreview` runs the Symmetry path validator but not Core's `TransitionModelValidator`. `TransitionPathValidator.GetGroupStabilityRole` returns `null` for a group mixing stable-role and unstable-role pairs, so it emits no `TRN012`; `TryBuildMatrices` only requires a stable position to have at least one entry. A ceria-kmc variant with such a group at a stable position produced `TRN025` from both `ProjectValidator` and `ProjectValidationService`, but `TryCreatePreview` returned a non-null preview with no diagnostics. The published preview contract says an invalid transition returns `null` and diagnostics. | Scratch probe (not committed); `Mocassin.Symmetry/Transitions/TransitionPreviewService.cs` 88-93, 336-361; `Mocassin.Symmetry/Transitions/TransitionPathValidator.cs` 284-308; `Mocassin.Core/Model/Transitions/TransitionModelValidator.cs` 200-208; `docs/reference/transitions.md` 126-132. |
| **Minor** | **F3 — Ambiguous-path `TRN020` does not put the competing P indices in `Related` as designed.** The design says an ambiguous match is `TRN020` with the competing P indices in `Related`; `TransitionPathResolver` puts them in the message text only, while `PathError` sets `Related` to a single `referencePathPosition` entry. A duplicate-site structure produced `TRN020` with an empty `Related` list. The message remains actionable, so this is a diagnostic-navigation defect, not a missed rejection. | `Mocassin.Symmetry/Transitions/TransitionPathResolver.cs` 119-131, 198-205; step page lines 335-337; scratch probe (not committed). |
| **Minor** | **F4 — The preview throws for in-memory models with default `ImmutableArray` collections instead of returning diagnostics.** The preview contract says it never throws for invalid model data. A project whose `Model.Particles` or `Model.Chains` is `default` (possible through the public `init` properties/`with`) makes `TryCreatePreview` throw `InvalidOperationException` or `NullReferenceException` from `GetParticleOrder`/`ProjectHasher.ComputeTransition`; the Core and composed validators tolerate the same models. This is the same class as Step 03 F2; it is not reachable from persisted JSON, which normalizes missing sections to empty arrays. | Scratch probe (not committed); `Mocassin.Symmetry/Transitions/TransitionPreviewService.cs` 370-374; `Mocassin.Core/Project/ProjectHasher.cs` 66-84; `Mocassin.Core/Model/Transitions/TransitionModelValidator.cs` `IsDefault` guards; Step 03 audit F2 precedent. |
| **Note** | **N1 — Parity status semantics.** The mechanism rows are marked `Implemented` while their notes place rules in Steps 08/16/17 and the reference-path editor UI in Step 24; the matrix legend defines `Partial` as “model/API part implemented; UI or later-step integration is still pending.” The notes disambiguate the scope, and the `Transition editor` row remains `Planned` at Step 24; this is a status-wording observation with no code or acceptance impact. | `docs/planning/appendix-b-parity-matrix.md` lines 3-10 and 58-68. |
| **Note** | **N2 — Benchmark evidence is local-container.** `TransitionPreviewCeria` records 19.75 µs in `tests/reference/bench/baselines.json` and explicitly labels the evidence as local-container; the declared GitHub runner class is re-verified only by the manual Benchmark CI job. The 100 ms target is met by a wide margin, so this is an accepted evidence limitation. | `tests/reference/bench/baselines.json` `transitionPreview`; `tests/reference/bench/transition-preview-2026-11-09.txt`; CI policy. |
| **Note** | **N3 — Record parent is the docs-only request commit.** The audited code commit is `288dd37`, but the record commit's parent is `8869f3d` because the closeout/status and audit-request update sits between them. `git diff 288dd37..8869f3d` is documentation-only, so the audited code is unchanged; this is the same process note as the Step 04 design rounds and is not a finding about the implementation. | `git diff --stat 288dd37..8869f3d`; `git diff 288dd37..8869f3d -- docs/planning`. |
| **Note** | **N4 — MMC chain groups are not checked for stable role.** Step page §3 says a stable position must use a stable-role group (TRN012), but the rule table scopes TRN012 to kinetic path positions and `TransitionPathValidator` only checks group role for kinetic positions. A ceria-mmc variant whose Metropolis2 chain references an `[O, ∅]` group at both stable sites produces no Core or composed diagnostics and a preview matrix with `[O, void]` at both positions. The original `MetropolisTransitionValidator` also checked only the connector pattern, so this is a design-ambiguity observation for a future clarification, not a clear deviation from the frozen rule table. | Scratch probe (not committed); step page lines 223-226 and 380-381; `Mocassin.Symmetry/Transitions/TransitionPathValidator.cs` 227-282; `Mocassin.Core/Model/Transitions/TransitionModelValidator.cs` 126-154. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Verdict

- [ ] Passed
- [x] Passed with notes
- [ ] Fixes required
- [ ] Re-audit required
- [ ] Waived

Follow-up owner: Step 04 agent (fix F1–F4 or record explicit human acceptance of the notes); human owner for the
approval gate.
Follow-up issue/step: Step 04 — Transition Model. If fixes are made, use a new feature branch, squash into the step
branch, dispatch one new closeout CI run, and request a fresh re-audit. If the notes are accepted, proceed to the
Step 04 merge approval with the §3.12 cumulative approval report.

**Verdict: Passed with notes.** No Blocker or Major finding remains. The transition envelope and persistence,
canonical hashing, definition and resolved-path validation, queries and editing, preview service and cache,
benchmark baseline, contract schema/fixtures, samples, documentation, parity rows, and status dashboard satisfy
the frozen design; the full local gate and the closeout CI are green at `288dd37`; and no Step 05/06/08, GUI, rule
generation, legacy import, or preview persistence was added. F1–F4 are Minor: they are test-coverage,
preview-failure-boundary, diagnostic-navigation, and in-memory-robustness gaps, not observed failures of a
validated user workflow. N1–N4 are notes: parity-status wording, local-container benchmark evidence, the
docs-only record parent, and the MMC group-role ambiguity. The human owner must explicitly accept F1–F4 and
N1–N4 or request fixes; a fix commit requires a fresh re-audit. The audit result is evidence, not merge
authorization.

### Re-audit round 2

#### Independence and frozen state (round 2)

I am a fresh re-audit agent. I did not plan, implement, or review Step 04 and have no shared conversation with
the planner or implementor. I read `AGENTS.md` (§3.8, §3.10, §3.12, §3.13, §4, §5), the audit README and
template, this record's round 1 and re-audit request, the step page, the Step 04 design review, the frozen
design at `a3d8641`, the transition contracts, quality strategy, code-structure policy, ADRs 0009/0010/0013,
the status dashboard, the parity matrix, the manifest/schema/fixtures, the samples, the public API files, and
the complete round-2 diff. I did not rely on the step-agent resolutions or the request summary for the verdict.

Worktree `/workspace/mocassin`, branch `step/04-transition-model`, `HEAD` `90d95d2`. The audited code commit is
`ebefc96` (`ebefc9655a90d3ef7c6b4261d2377f4341b511e2`); `git status --short` is empty. `git diff --name-only
ebefc96..90d95d2` lists only `docs/planning/audits/step-04.md`, `docs/planning/status.md`, and
`docs/planning/steps/step-04-transition-model.md`, so the HEAD delta is documentation-only. The record commit
parent is therefore the docs-only re-audit-request commit `90d95d2`, while the audited code is frozen at
`ebefc96` (the same process note as N3).

`git diff --stat 288dd37..ebefc96` is 15 files, 637 insertions(+), 85 deletions(-); `git show --stat ebefc96`
is 12 files, 422 insertions(+), 31 deletions(-). The code/test changes are limited to the Core definition
validator, project query and hasher guards, the Symmetry path resolver and preview service, the corresponding
Core/Symmetry tests, and the parity rows. The `InternalsVisibleTo("Mocassin.Symmetry")` entry in Core is the
only visibility change; it lets Symmetry call the internal Core definition validator and does not change the
`Core <- Symmetry` dependency direction. No Step 05/06/08, GUI, rule-generation, legacy-import, schema,
fixture, sample, or preview-persistence change was introduced.

#### Commands and results (round 2, from `/workspace/mocassin`)

```text
git status --short
# (no output; clean worktree)
git log --oneline -7
# 90d95d2 docs: request step-04 re-audit after fixes [skip ci]
# ebefc96 step-04: address audit findings
# 5547f6d docs: record step-04 audit passed with notes [skip ci]
# b2382b3 docs: record step-04 audit Passed with notes [skip ci]
# 8869f3d docs: record step-04 closeout and request audit [skip ci]
# 288dd37 step-04: prepare closeout and audit request
# ec04f9d step-04: add transition samples and user documentation
git diff --name-only ebefc96..90d95d2
# docs/planning/audits/step-04.md
# docs/planning/status.md
# docs/planning/steps/step-04-transition-model.md
git diff --stat 288dd37..ebefc96
# 15 files changed, 637 insertions(+), 85 deletions(-)
git show --stat ebefc96
# 12 files changed, 422 insertions(+), 31 deletions(-)
./tools/verify.sh dotnet
# EXIT 0; build succeeded with 0 warnings/0 errors; Core 339/339, Symmetry 185/185,
# Packages 42/42, Architecture 1/1, App/Compilation/Contracts/Results 1/1 each; dotnet format clean.
./tools/verify.sh structure
# EXIT 0; Mocassin.Architecture.Tests 1/1 passed.
./tools/verify.sh all
# EXIT 0; includes:
#   API: NuGet licence policy OK (47 packages); no vulnerable packages; all nine ApiCompat
#        checks pass; "Public API is compatible with the checked-in baseline."
#   Rust: fmt, clippy, and tests pass.
#   Python: 3 tests pass; sdist and wheel build.
#   Docs: Zensical strict "No issues found"; DocFX 190 files with 0 warnings/0 errors; rustdoc built.
#   Contracts: artifacts current (17 files); catalogue current (274 settings, sha256
#        1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395);
#        SPDX headers present in 217 source files; no secret patterns in 523 tracked text files;
#        cargo-deny advisories/bans/licenses/sources ok.
```

**Closeout CI (GitHub API; no token printed).** Run `42693957630`: name `CI`, event
`workflow_dispatch`, branch `step/04-transition-model`, `head_sha`
`ebefc9655a90d3ef7c6b4261d2377f4341b511e2`, status `completed`, conclusion `success`. Jobs `Detect changes`,
`.NET (ubuntu-latest)`, `Public API compatibility`, and `Contracts and policy` are `success`; `Python` and
`Rust` are `skipped` by the path filters. The recent-runs query shows no workflow run for the docs-only
`90d95d2` and no per-feature step runs, matching the CI cost policy.

**Scratch probe.** A read-only console probe under `/tmp/opencode/step04probe2` (not committed) loaded the
real samples and mutated in-memory models to check the round-1 failure boundaries. It confirmed:
mixed-role group -> `null` preview + `TRN025`; ambiguous path -> `TRN020` with `Related` entries
`referencePathPosition=0`, `candidatePosition=p1`, and `candidatePosition=p2`; top-level default arrays ->
`null` preview + `TRN023`; unstable-role Metropolis group -> `TRN012` from Core and composed validation and
`null` preview + `TRN012`; cache keys differ for structure-name, state-change-name, and version changes;
ceria-kmc expands to 96 paths and ceria-mmc to 8 pairs. It also exposed F6 and N5 below.

#### Round-1 finding verification (round 2)

| Finding | Result |
| --- | --- |
| F1 | **Partially fixed.** The Core ordering property test, the random-path brute-force oracle, the cache-key separation test, and the unknown-transition-field schema test are present and pass. The Preview row's symmetry-expanded path-count-vs-brute-force assertion and non-trivial MMC pair-expansion assertion are still absent (F5). The Core property test randomizes the collection order of one fixed model rather than generating random models, but it does exercise canonical round-trip and hash stability. |
| F2 | **Fixed.** A mixed-role group referenced by the transition now returns `null` with `TRN025` from `TryCreatePreview` (unit test and independent probe). N5 records that the Core validation is project-wide rather than closure-only. |
| F3 | **Fixed in code.** An ambiguous path now emits `TRN020` with the competing `P` indices in `Related` (`candidatePosition=p1`, `candidatePosition=p2`) alongside the path position. No regression test was added (F7). |
| F4 | **Partially fixed.** Default top-level model collections (`Particles`, `Chains`, etc.) now return `null` + diagnostics instead of throwing. Nested default arrays (`TransitionChain.GroupIds`, `StateChangeGroup.StateChangeIds`) still throw from `ProjectHasher.ComputeTransition` (F6). |
| N1 | **Fixed.** The transition mechanism and reference-path editing rows now use `Partial` where later-step rules or the Step 24 editor UI remain; the `Transition editor` row remains `Planned` at Step 24. |
| N2 | **Still an accepted note.** The benchmark baseline is unchanged and remains local-container evidence; round 2 produced no GitHub runner-class measurement. The manual Benchmark CI job remains the re-verification path. |
| N3 | **Still an accepted note.** The audited code is `ebefc96`; the later `90d95d2` record parent is documentation-only. |
| N4 | **Fixed.** A Metropolis chain with an unstable-role group now produces `TRN012` from Core and composed validation, and `TryCreatePreview` returns `null` + `TRN012` (unit test and independent probe). |

#### Findings (round 2)

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **F5 — F1 is only partially closed: the required Preview test row still lacks the symmetry-expanded path count compared with brute force and the non-trivial MMC pair-expansion assertion.** The step page requires `Symmetry-expanded path count matches brute force; MMC pair expansion; (A,B,C) echo`; the sample test asserts only that previews are non-null with no diagnostics, and the preview tests assert only `Assert.Single(preview.ExpandedPaths)` on the small toy model. No test asserts the ceria-kmc 96-path or ceria-mmc 8-pair counts, so the acceptance count claim is behaviourally correct but unguarded against regression. | Step page `Tests` row at line 710; `Mocassin.Symmetry.Tests/Transitions/TransitionSampleTests.cs` (non-null/empty assertions only); `TransitionPreviewTests.cs` lines 38 and 71; scratch probe prints `ceria-kmc 96` and `ceria-mmc 8`; `grep -rn "ExpandedPaths" src/dotnet/*Tests` shows no count assertion beyond `Assert.Single`. |
| **Minor** | **F6 — F4 is only partially closed: nested default `ImmutableArray` members still make `TryCreatePreview` throw.** A chain with `GroupIds = default` or a group with `StateChangeIds = default`, both reachable through the public `init` properties/`with`, throws `InvalidOperationException` before the preview can return diagnostics. The failure is in the cache-key path, not in the newly added top-level guards. This is the same in-memory-only class as F4 and is not reachable from persisted JSON. | `ProjectHasher.ComputeTransition` lines 95 and 101 (`SelectMany` over the nested arrays); `TransitionPreviewService.CreateCacheKey` line 491 called at `TryCreatePreview` line 75; scratch probe stack trace `ImmutableArray<T>.ThrowInvalidOperationIfNotInitialized` -> `ProjectHasher.ComputeTransition` -> `TransitionPreviewService.CreateCacheKey` -> `TryCreatePreview`. |
| **Minor** | **F7 — The F3 fix has no regression test.** The diagnostic now carries the competing `P` indices, but no test asserts the `TRN020` `Related` contents, so a future refactor could silently remove the navigation detail. The existing TRN020 tests assert only the code. `AGENTS.md` §12 requires a regression test for a bug fix. | `grep -rn "Related" src/dotnet/Mocassin.Symmetry.Tests src/dotnet/Mocassin.Core.Tests` finds no TRN020 Related assertion; `TransitionPathResolver.cs` lines 126-133 and 202-220; independent probe verified `referencePathPosition` and `candidatePosition=p1/p2`. |
| **Note** | **N5 — Preview definition validation is project-wide, not closure-only.** The resolution describes Core validation on the "reduced closure", but `BuildReducedModel` passes every state change, group, and chain and `TransitionModelValidator.Validate` validates them all. A valid ceria-kmc transition preview therefore returns `null` with `TRN001` when an unrelated state change references an unknown particle, and with `TRN005` when an unrelated empty group exists. This fails closed and is consistent with the "invalid user data" failure boundary; it is only broader than the resolution wording. | `TransitionPreviewService.BuildReducedModel` lines 252-282; `TransitionModelValidator.Validate` validates all state changes/groups; scratch probe `F2c`/`F2d` outputs `null` + `TRN001`/`TRN005` for otherwise valid transitions. |

#### Verdict (round 2)

- [ ] Passed
- [x] Passed with notes
- [ ] Fixes required
- [ ] Re-audit required
- [ ] Waived

Follow-up owner: Step 04 agent for F5–F7 and N5, or explicit human acceptance; human owner for the approval
gate.
Follow-up issue/step: Step 04 — Transition Model. F5–F7 are Minor and N5 is a Note; no Blocker or Major remains.
The human owner must explicitly accept them or request a fix commit. A fix commit requires a new feature
branch, squash integration, one replacement closeout CI run, and a fresh re-audit.

**Verdict (round 2): Passed with notes.** F2, F3 (in code), N1, and N4 are verified fixed; F1 and F4 are
partially fixed, with the remaining test-coverage and nested-default-array gaps recorded as F5 and F6. The
full local gate and the replacement closeout CI are green at `ebefc96`; the `90d95d2` parent delta is
documentation-only; the round-2 diff introduces no unrelated change or design deviation. No Blocker or Major
finding remains, and no Step 05/06/08, GUI, rule generation, legacy import, or preview persistence was added.
The audit result is evidence, not merge authorization.

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| F1 | Fixed — property tests, brute-force path oracle, cache-key separation, schema rejection added | Step agent | `ebefc96` |
| F2 | Fixed — preview now runs Core definition validation on the reduced closure | Step agent | `ebefc96` |
| F3 | Fixed — ambiguous TRN020 now lists competing P indices in `Related` | Step agent | `ebefc96` |
| F4 | Fixed — preview and Project queries guard default `ImmutableArray` collections | Step agent | `ebefc96` |
| N1 | Fixed — transition parity rows use `Partial` where rules/UI remain | Step agent | `ebefc96` |
| N2 | Accepted note — local-container benchmark evidence; manual Benchmark CI re-verifies the runner class | Step agent | — |
| N3 | Accepted note — closeout CI is green at the code commit; later commits are documentation-only | Step agent | — |
| N4 | Fixed — Metropolis chains now reject unstable-role groups with TRN012 | Step agent | `ebefc96` |
| F5 | Fixed — ceria expanded-path and MMC pair counts now asserted against a brute-force count | Step agent | `8c44709` |
| F6 | Fixed — nested default chain/group arrays are guarded in the transition closure hash | Step agent | `8c44709` |
| F7 | Fixed — ambiguous TRN020 now has a regression test for `Related` candidate paths | Step agent | `8c44709` |
| N5 | Fixed — preview definition validation now uses only the transition closure | Step agent | `8c44709` |

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
