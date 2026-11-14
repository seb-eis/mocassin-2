# Step 08 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-08.md`. You may edit only your own metadata (`Auditor`, `Audit date`,
> `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 08 — Model Compilation Pipeline |
| Request owner | Step agent |
| Orchestrator (step agent) | Step 08 implementor |
| Audit initiated by | Human owner |
| Audited commit | `ba8e889cb51041352c876681801b12f455b2f7ee` |
| Step branch | `step/08-model-compilation-pipeline` |
| Step tag | — |
| Closeout CI run | [`43533462127`](https://github.com/seb-eis/mocassin-2/actions/runs/43533462127) — green at `ba8e889cb51041352c876681801b12f455b2f7ee` |
| Closeout CI head SHA | `ba8e889cb51041352c876681801b12f455b2f7ee` |
| Main SHA at freeze | `884ffe78f8b8f2a9fa6093afcce94856a6094c73` |
| Merge-tree CI | Not required; `main` 884ffe7 was merged before the freeze |
| Closeout evidence | `docs/planning/closeout/step-08.json` |
| Platform CI | Not run (Ubuntu-only policy; no platform-specific code) |
| Audit date | 2026-11-13 |
| Auditor | Fresh independent audit context (DeepSeek V4.1 Flash 1017, OpenCode audit subagent); no shared conversation with the planner or implementor |

Step page: [`Step 08 — Model Compilation Pipeline`](../steps/step-08-model-compilation-pipeline.md)

> Orchestration: the step agent (the orchestrator) freezes the step branch, requests the auditor, applies fixes
> autonomously on a new feature branch, reruns closeout CI, adds resolutions without altering reviewer text,
> and requests re-audit until the auditor no longer identifies issues. The human owner approves the final
> verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | Steps 02–07 |
| Delivers | A pure, staged compiler from a validated `Project` to a deterministic, densely indexed `CompiledModel` with structure, environments, pair/cluster tables, transition rules, simulation encoding, signatures, caches, diagnostics, and JSON inspection. |

## Step-specific acceptance checklist

- [x] Ceria compiles to the expected P1 positions, pair sets/tables, cluster sets, and oxygen migration rules. (audit: covered by structure/pair/cluster/rule tests, the ceria KMC golden signature, and the pinned inspection-JSON hash)
- [x] A 2-site MMC model compiles forward and inverse exchange rules. (audit: `MetropolisExchangeCompilesForwardAndInverseRulesWithoutState1` and the ceria-MMC golden)
- [ ] A 3-site migration compiles forward and inverse rules, including a chiral case, and keeps the KMC dual-DOF default. (audit: forward/inverse and dual-DOF are covered for ceria; no explicit chiral rule/migration case is asserted anywhere)
- [x] A 5-site vehicle compiles unidirectional rules only, with no automatic inverse; as a KMC simulation it keeps the dual-DOF default unless a documented Step 10 optimizer rule clears it. (audit: unidirectional/no-inverse is pinned; dual-DOF is pinned for KMC generally, not for the vehicle specifically. The explicit reverse transition cannot be compiled alongside the forward one — finding A1)
- [ ] Rule generation enforces unstable positions void in S0/S2, stable positions void in S1, and the documented mass/charge conservation rules; rejected counts are reported. (audit: state roles and conservation are enforced; `RejectedByMass/Charge/InvalidState` are never populated — finding A4)
- [ ] Charge transport vectors give `qΔx` for a single charge, the correct vacancy sign, zero for an identical-ion exchange, and non-zero for a distinct-charge exchange or a non-collinear multi-site path. (audit: only the interstitialcy/vacancy pin (-4.0) and the neutral vehicle pin (0.0) exist; the other named cases are untested — finding A9)
- [ ] Rule ambiguity is detected as `CMP004` with both conflicting transitions/rules. (audit: detection logic exists, but no emission test demonstrates it — finding A8)
- [x] Every compiled direction/rule is reachable through `jump_count`/`jump_mapping`; every selectable `(position, particle, relative slot)` has at most one rule for its `state0`. (audit: uniqueness follows from the `(collection, startP, state0)` rule store; a scratch harness confirmed every ceria direction is mapped, but there is no dedicated assertion)
- [ ] Tracker definitions/mappings are emitted for KMC and absent for MMC; a 3-site migration's `tracker_order` follows particle identity. (audit: 3-site `tracker_order` and KMC emission are pinned; MMC collections still emit tracker definitions and rule tracker orders, and KMC simulations receive trackers for non-active/MMC collections — finding A6)
- [x] `CompiledModel.ModelSignature` is stable across recompilation, excludes runtime settings and parameterization values, and changes exactly when compiled geometry/rules/encoding changes. (audit: stability and runtime-setting exclusion are tested; coverage is adequate. The projection byte format itself is finding A2)
- [ ] Compiling the same project twice yields byte-identical `CompiledModelJson` and signature on Ubuntu, Windows, and macOS. (audit: Ubuntu is verified; Windows/macOS were not run. Accepted per the manual platform policy and recorded as a note)
- [ ] Stage caches reuse unaffected stages after a single-domain edit and never change the output. (audit: fails — a caller cache returns stale rules and a different signature after a particle edit, and a successful stale compile where a fresh compile fails `CMP005`; pairs/clusters/indexing are not cached — finding A3)
- [ ] Ceria compiles in `< 1 s`; a 10k-position model compiles in `< 30 s` with `< 2 GiB` managed memory. (audit: ceria measured ~1 ms; the guard is `< 2 s`, and no 10k-position or memory evidence exists — finding A7)
- [x] Cancellation returns no partial model and leaves the project unchanged. (audit: cancelled-token and between-stage cancellation tests pass; project immutability is asserted for a successful compile. A mid-stage/mid-Symmetry cancellation run is not exercised, which the design accepts)
- [ ] Every `CMP` code has a catalog descriptor, documentation anchor, and emission test. (audit: catalog and anchors are complete and tested; only `CMP006`, `CMP009`, and `CMP010` have emission tests — finding A8)
- [x] Documentation written. (audit: the named pages are present and describe the implemented pipeline; the post-freeze request commit breaks the docs link gate — finding m4)

## Standard audit checklist

- [ ] Required tests from the step page pass. (audit: all 970 existing tests pass, but several tests named in the step page's Tests table are absent: brute-force pair/cluster oracles, charge-transport cases, cache invalidation after a one-domain edit, 10k performance/memory, and per-code CMP emission)
- [x] Public/protected XML documentation policy is followed. (audit: documentation file generated with warnings-as-errors; `PublicAPI.Unshipped.txt` updated; ApiCompat clean)
- [x] Documentation, status dashboard, feature log, and parity matrix are current. (audit: updated, though the parity matrix marks the stage cache `Done` despite A3, and the request commit's step-page link is broken)
- [x] Closeout CI is green at the audited commit. (audit: run 43520011439 is `workflow_dispatch`, `success`, `head_sha` = `ebc0e46a052d4fb9d12dd0111f905be13ec51849`; contracts/.NET/ApiCompat jobs green, Python/Rust skipped by path filters)
- [x] Closeout evidence matches the audited commit and the merge candidate. (audit: `check_closeout_evidence.py` reports OK; `main` 884ffe7 is an ancestor of the audited commit; no merge-tree run required)
- [x] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded. (audit: `main` did not move after the freeze; the merge-tree field is correctly empty)
- [x] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific code. (audit: not run; accepted under the manual platform policy. The cross-platform determinism claim is therefore unverified — note N2)
- [ ] Format/version/fixture changes are documented and tested. (audit: the model-signature projection does not match ADR 0010/the manifest, and only the C# projection is pinned — finding A2)
- [x] Security, dependency, licence, and secret-scan gates pass. (audit: `contracts` mode: 403 SPDX headers, no secret patterns in 722 tracked text files, advisories/bans/licenses/sources ok, NuGet licence/vulnerability audit clean)
- [x] Code structure policy passes; no new root-level or catch-all folders. (audit: `structure` mode passes; `Mocassin.Compilation` is split by concept and stays within the file/folder limits)
- [x] No unrelated changes or unapproved scope. (audit: the diff is the compiler, tests, contracts/fixtures, the additive `TransitionPathResolver` API, the documented sample/validator correction, and the required docs/status updates)
- [x] Branch, tag, and status hygiene are correct. (audit: work is on `step/08-model-compilation-pipeline`; no step tag; no per-feature CI; only documentation commits follow the freeze)

## Evidence

Commands run:

```text
$ ./tools/verify.sh dotnet
  PASS. Release build and test of Mocassin.slnx; 970 tests, 0 failed
  (Compilation 66, Core 565, Symmetry 223, Packages 105, Cli 7,
   App 1, Results 1, Contracts 1, Architecture 1). dotnet format clean.

$ ./tools/verify.sh structure
  PASS. Mocassin.Architecture.Tests 1/1.

$ ./tools/verify.sh api
  PASS. NuGet licence/vulnerability audit clean; ApiCompat clean for
  Compilation, Contracts, Packages, Results, App.ViewModels, App.Views,
  Rendering.

$ ./tools/verify.sh contracts
  PASS. Contract artifacts current (18 files); space-group catalog current
  (274 settings, sha256 1f3bf67d...); process consistency OK; 18 tool tests
  OK; 403 SPDX headers; no secret patterns in 722 tracked text files;
  advisories/bans/licenses/sources ok.

$ ./tools/verify.sh docs
  FAIL at branch tip 911408b:
  docs/planning/steps/step-08-model-compilation-pipeline.md:17:
  missing link target: audits/step-08.md
  PASS at the audited commit ebc0e46 (re-run in a temporary detached
  worktree: 2 DocFX warnings, 0 errors, no broken links).

$ python tools/check_closeout_evidence.py docs/planning/closeout/step-08.json
  Closeout evidence: OK

$ git diff --stat main...HEAD
  94 files changed, 8967 insertions(+), 115 deletions(-)

$ GitHub API: actions/runs/43520011439
  CI, workflow_dispatch, completed/success, head_sha ebc0e46..., branch
  step/08-model-compilation-pipeline; .NET/Contracts/ApiCompat jobs green,
  Python/Rust skipped by path filters.
```

Reviewed in addition to the commands: `AGENTS.md`; the step page; the closeout evidence; this request;
`docs/development/compilation-pipeline.md`, `architecture.md`, `design-decisions.md`;
`docs/planning/02-data-contracts.md` §3.3; ADR 0010; the full compiler diff under
`src/dotnet/Mocassin.Compilation`; the compilation tests; the additive `TransitionPathResolver` API and its
tests; the `TransitionModelValidator` spectator change; the corrected `vehicle5`/`interstitialcy5` samples;
the contract manifest/fixtures; the status dashboard and parity-matrix changes.

Reproduction evidence for A1 and A3 used a scratch console harness outside the repository
(`/tmp/opencode/probe`, project references to `Mocassin.Compilation` and `Mocassin.Packages`; not committed,
not part of the audited tree). It loaded the real samples through the parse-only store and applied in-memory
record edits:

- A1: a `KineticSimulation` referencing both `vehicle5` kinetic transitions (`...a51`, `...a52`) gives
  `CMP005` for `...a52` with `(mass 0, charge 0, invalid state 0)`; the analogous interstitialcy pair
  (`...b51`, `...b52`) gives `CMP005` for `...b52`. Referencing only the first transition succeeds, which is
  the only shape the tests use.
- A3: compile ceria-kmc with a caller-owned cache, then recompile an in-memory copy whose `O2-` particle
  charge is changed. The cached result keeps the old `chargeTransportVector` (`5` vs the fresh `2.5`) and a
  different signature (`09271109...` vs `405574b8...`), so `CompiledModelJson` differs from a fresh compile.
  Swapping a state-change donor/acceptor under the same cache returns a successful compile with empty
  diagnostics, while a fresh compile fails with `CMP005`.
- A4: ceria-kmc reports `RejectedByMass/Charge/InvalidState = 0/0/0`, although the two state changes per
  stable node produce two mass-invalid combinations per forward direction (only the O/vacancy-balanced pair is
  accepted), so real rejections are silently dropped from the report.
- A6: a mixed project with one KMC and one MMC simulation produces global trackers
  `c0p1, c0p2, c1p1, c1p2`; the KMC simulation declares all four, including the two MMC-collection trackers,
  and the MMC rules carry `trackerOrder = [1, 0]`.
- A7: ceria-kmc compiles in ~1 ms (Release, warm) — far inside the 1 s budget — but no 10k-position model is
  constructed anywhere.

A2 evidence: `ModelSignatureProjection.ComputeProjection` writes a custom `BinaryWriter` stream. `WriteScalar`
for the format version and lengths resolves to the 4-byte `int` overload; `WriteText` uses
`BinaryWriter.Write(string)` (7-bit length prefix); particle/structure/energy/transition/simulation sections
are written inline. `contracts/generated/signatures/model-v1.json` and `contracts/manifest.json` declare
`formatVersion` as `uint64` and those sections as `bin`, and ADR 0010 §"Projection rules" 2–3 require
MessagePack arrays/str/bin and fixed-width `uint64`/`int64`. The only pinned evidence is the C# hex in
`GoldenModelTests` and `tests/contracts/signature-vectors.json`; no cross-language or manifest-driven vector
exists.

## Known notes for the auditor

- The `vehicle5` and `interstitialcy5` samples originally repeated the same donor/acceptor direction on both
  sides of the complex chain. They were corrected to alternating groups; `interstitialcy5` uses an explicit
  spectator self state change at its interior stable centre. `TRN002` now permits a self state change only at
  an interior stable position with unstable neighbours.
- The charge transport vector is computed from charge-weighted endpoint moments, equivalent to the
  particle-identity sum for balanced counts and covering charge-state changes. Movement follows species flow in
  path order, so a chemically unchanged spectator site still moves physically.
- No `vehicle6` sample or golden exists; the acceptance target is the 5-site vehicle. Windows/macOS golden runs
  were not performed (manual platform policy).
- Performance is guarded by a local ceria `< 2 s` test; no 10k-position or benchmark-project evidence is
  recorded in this implementation.
- Closeout CI run `43519214591` covered the earlier `ec10960` commit; run `43520011439` is the replacement
  closeout run for the audited commit.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Blocker** | **A1 — Directions are deduplicated across collections, so an explicit reverse transition cannot be compiled with its forward partner.** `TransitionCompilationStage.AddDraft` searches the shared `drafts` list with a key built only from the relative path and the inverse flag; the collection/transition identity is absent. A simulation that references both the corrected vehicle (or interstitialcy) forward transition and its authored `reverseTransitionId` loses every direction of the second collection, and compilation fails with `CMP005` ("produced no valid rules", with zero candidates). This contradicts design §8.3 ("An explicit reverse transition generates its own rules") and the step's vehicle acceptance item. | `TransitionCompilationStage.AddDraft`/`TryBuildDirections`; `TransitionRuleHelpers.DirectionKey`. Scratch harness: `vehicle5` with `a51`+`a52` → `CMP005 ...a52`; `interstitialcy5` with `b51`+`b52` → `CMP005 ...b52`. Tests only reference `KineticTransitions[0]`. |
| **Blocker** | **A2 — `ModelSignatureProjection` is not the ADR 0010/manifest canonical projection.** It writes a custom `BinaryWriter` layout instead of the declared canonical MessagePack projection: 4-byte `int` for the format version, lengths and indices; `BinaryWriter` 7-bit string framing; inline particle/structure/energy/transition/simulation sections. The manifest declares `formatVersion` as `uint64` and the sections as `bin`; ADR 0010 requires MessagePack arrays/str/bin and fixed-width `uint64`/`int64`. The pinned golden vectors lock the non-conforming encoding in. Step 11 and the Rust/solver cross-language check cannot reproduce this from the declared contract. | `ModelSignatureProjection.cs` (`WriteScalar(int)`, `WriteText`, `WriteStructure`/`WriteEnergy`/`WriteTransitions`); `contracts/manifest.json`/`contracts/generated/signatures/model-v1.json` (`formatVersion: uint64`, sections `bin`); ADR 0010 rules 1–3; step page §11.3; only C# vectors in `GoldenModelTests`/`tests/contracts/signature-vectors.json`. |
| **Blocker** | **A3 — A caller-supplied stage cache can return a stale or wrong model.** The rules key is `ComputeSimulationSelection + ComputeStructure` and omits transition closures, particle order, and particle charges; the cache-hit guard checks only `ParticleCount`/`PositionCount`. Pairs, clusters, environments, and indexing are never cached (`StageCacheKeys.Energy`/`Clusters` are dead). A cached compile after a particle-charge edit returns the old `chargeTransportVector` and a different signature; swapping a state-change donor/acceptor returns success where a fresh compile fails `CMP005`. Violates "reuse unaffected stages ... and never change the output" and the documented invalidation graph. No one-domain-edit test exists. | `StageCacheKeys.Rules`/`Encoding`; `TransitionCompilationStage.Execute` cache guard; `StructureCompilationStage.Execute`; unused `StageCacheKeys.Energy`/`Clusters`. Scratch harness results in Evidence. `CompilationEndToEndTests` only reuses an identical project. |
| **Major** | **A4 — Rejected rule counts are never reported.** `CompilationCounts.RejectedByMass`, `RejectedByCharge`, and `RejectedByInvalidState` are declared but never assigned in `FromModel`; the transition stage's local counters are used only in the `CMP005` message. The acceptance item "rejected counts are reported" and design §13.2 are unmet; ceria silently rejects two mass-invalid combinations per forward direction but reports zero. | `CompilationCounts.cs` (`FromModel`); `TransitionCompilationStage` local `rejectionCounts`; scratch harness report for `ceria-kmc` (rules=4, rejections 0/0/0). |
| **Major** | **A5 — Hard resource limits are not enforced.** `MaxRuleCount` and `MaxEnergyTableBytes` are validated for non-negativity but never checked before enumeration/allocation; only the advisory budgets emit `CMP010`. `CMP006` cannot fire for rule count or table bytes, contrary to design §13.3 and the `CMP006` description. A large model can therefore exhaust memory instead of failing closed. | `CompilationOptions.MaxRuleCount`/`MaxEnergyTableBytes`; `TransitionCompilationStage` (only `AdvisoryRuleCount`); `EncodingCompilationStage` (only `AdvisoryEnergyTableBytes`); no `Max...` reads anywhere else. |
| **Major** | **A6 — Tracker definitions are not KMC-scoped.** `BuildGlobalTrackers`/`BuildStaticTrackers` walk every collection, including MMC collections, and `BuildSimulations` gives every KMC simulation all global trackers rather than the trackers of its active collections. In a mixed or multi-KMC project the KMC simulation declares trackers for collections it never selects; MMC rules also carry non-`0xFF` `trackerOrder` values. The acceptance item "Tracker definitions/mappings ... absent for MMC" is not met at the compiled transition-model level. | `EncodingCompilationStage.BuildGlobalTrackers`/`BuildSimulations`; mixed KMC+MMC scratch compile (global trackers `c0p1,c0p2,c1p1,c1p2`; KMC sim declares 4; MMC rule `trackerOrder=[1,0]`); `SimulationEncodingTests` only checks single-kind samples. |
| **Major** | **A7 — The 10k-position performance/memory acceptance is not evidenced.** Only a local ceria guard exists and it asserts `< 2 s`, weaker than the `< 1 s` criterion; there is no 10k-position model, no peak-managed-memory assertion, and no `Mocassin.Benchmarks/Compilation` harness (the benchmarks project has no Compilation reference). The request's own note acknowledges the gap, but an implementor note does not meet an acceptance criterion. | `CompilationPerformanceTests` (`TimeSpan.FromSeconds(2)`); `Mocassin.Benchmarks.csproj` (only Core/Symmetry references); no 10k test in `Mocassin.Compilation.Tests`; measured ceria ~1 ms. |
| **Major** | **A8 — `CMP` emission tests are missing.** Catalog completeness and documentation anchors are tested, but only `CMP006` (cluster tuple limit), `CMP009`, and `CMP010` are asserted. No test emits `CMP001`, `CMP002`, `CMP003`, `CMP004`, `CMP005`, `CMP007`, `CMP008`, `CMP011`, or `CMP012`; in particular the acceptance item "Rule ambiguity is detected as `CMP004` with both conflicting transitions/rules" has no demonstration. | `grep CMP0` over `Mocassin.Compilation.Tests` returns only CMP006/009/010 assertions; `EveryCompilationCodeHasExactlyOneDescriptor` is catalog-only; `CMP004` appears only in the catalog and docs. |
| **Major** | **A9 — The named charge-transport acceptance cases are not tested.** The endpoint-moment implementation is sound (I re-derived it and confirmed the interstitialcy vacancy/charge-state pin of `-4.0` and the neutral-vehicle `0.0`), but there is no test for a single charge `qΔx`, a distinct-charge exchange producing a non-zero vector, or a non-collinear multi-site path. The step page's Tests table names all of these. | `grep ChargeTransport` in tests finds only `TransitionCompilationTests` lines 115–117 and 138–140; no distinct-charge, single-charge, or non-collinear case. |
| Minor | **m1 — Symmetry-equivalent starts are not orbit-reduced.** `EquivalentStartPositions` is never populated for a newly created direction (only appended on a duplicate key that cannot occur when the key contains the absolute node `P`), so one direction is emitted per symmetry image instead of one direction mapped to every equivalent start; `DependentRules` is consequently always 0. No correctness failure observed, but the design §8.1/§11.1 orbit compression is not implemented. | `TransitionCompilationStage.AddDraft` (`EquivalentStarts` only updated on the early-return path); scratch ceria output: every direction has `equivalentStarts = []`; report `DependentRules = 0`. |
| Minor | **m2 — Global tracker indices can throw instead of failing closed.** `BuildTrackerOrder` casts the tracker index with `checked((byte)index)`; more than 254 global trackers throws `OverflowException` from the compiler instead of reporting `CMP006`. | `EncodingCompilationStage.BuildTrackerOrder`. |
| Minor | **m3 — The pre-existing `TryResolve` does not delegate to the new `TryResolvePath`.** The additive API was added as designed, but `TryResolve` keeps a duplicate point loop and its own diagnostics instead of delegating, contrary to design §4 and the step page. | `TransitionPathResolver.TryResolve` lines 187–256 vs `TryResolvePath` lines 149–179; design §4. |
| Minor | **m4 — The docs gate is red on the post-freeze request commit.** The step page links `audits/step-08.md` from `docs/planning/steps/`, which resolves to `docs/planning/steps/audits/step-08.md`; it should be `../audits/step-08.md`. `./tools/verify.sh docs` passes at `ebc0e46` and fails at `911408b`, so the fix belongs in the resolution/status commit. | `docs/planning/steps/step-08-model-compilation-pipeline.md:17`; docs command output in Evidence. |
| Minor | **m5 — A vehicle assertion is vacuous.** `Assert.Equal(0, ...DirectionIndices.Length % 1)` is always true regardless of the model. | `TransitionCompilationTests.CorrectedVehicleCompilesUnidirectionalRules` line 98. |
| Minor | **m6 — The movement documentation overstates charge-state pairing.** `compilation-pipeline.md` says "Identical species (including different charge states of one symbol) are paired in path order"; `ComputeTransport` pairs by compiled particle index, so different charge states are not paired for movement (only the charge-moment term covers them). | `docs/development/compilation-pipeline.md` lines 131–133; `TransitionRuleHelpers.ComputeTransport`. |
| Minor | **m7 — A self state change on an unreferenced state change is accepted.** `TransitionModelValidator` no longer rejects donor == acceptor at the state-change level; `TRN002` is emitted only by the chain-level `ValidateSpectatorPositions`, so an unused state change with donor == acceptor validates. The step-04 page says TRN002 applies "elsewhere". | `TransitionModelValidator.cs`/`.Transitions.cs`; `grep DonorEqualsAcceptor` shows only the chain-level emission. |
| Note | **N1 — Dead scaffold code and an unused option.** `UnavailableCompilationStage` is no longer referenced by the default pipeline, and `MaxDegreeOfParallelism` is validated but never used (compilation is serial). The design permits optional parallelism, so this is not a defect, but both are dead surface. | `grep UnavailableCompilationStage`; `CompilationOptions.MaxDegreeOfParallelism`; no parallel code in the stages. |
| Note | **N2 — Windows/macOS determinism is unverified.** The acceptance claim is cross-platform byte-identical output; only Ubuntu is exercised. This is an accepted risk under the manual platform policy for a step with no platform-specific code, and it should be repeated in the approval summary. | Request `Platform CI` row; step page §11.4. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** Fixes required

`Re-audit required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner or implementor.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [x] Fixes required
- [ ] Waived

Follow-up owner: Step 08 implementor (step agent)
Follow-up issue/step: Fix A1–A9 and the minor findings on a feature branch, squash into
`step/08-model-compilation-pipeline`, merge current `main`, refresh the closeout evidence, dispatch one
replacement closeout CI run, add the resolutions, and request re-audit.

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| A1 | Resolved — direction keys include the collection; forward+reverse vehicle/interstitialcy compile together | Implementor | `ba8e889` |
| A2 | Resolved — full nested manifest schema, canonical MessagePack writer, binary fixtures, Python checker | Implementor | `ba8e889` |
| A3 | Resolved for stale-output correctness — rules key includes transition closures/particle hash; charge edit test misses rules; structure/rules hit on environment edit | Implementor | `ba8e889` |
| A4 | Resolved — RejectedByMass/Charge/InvalidState populated and reported | Implementor | `ba8e889` |
| A5 | Resolved — MaxRuleCount and MaxEnergyTableBytes fail with CMP006 | Implementor | `ba8e889` |
| A6 | Resolved — trackers are KMC-collection-scoped; MMC rules use 0xFF; mixed scoping covered | Implementor | `ba8e889` |
| A7 | Resolved locally — ceria <1 s and 10k-position <30 s/<2 GiB tests; no benchmark project | Implementor | `ba8e889` |
| A8 | Partially resolved — CMP001/002/003/005/006/007/008/009/010/012 emission tests; CMP004/CMP011 still lack emission tests | Implementor | `ba8e889` |
| A9 | Partially resolved — ceria/interstitialcy/vehicle transport pins; distinct-charge and non-collinear cases still missing | Implementor | `ba8e889` |
| m1 | Open — equivalent-start orbit reduction/dependent rules remain unimplemented | Implementor | — |
| m2 | Resolved — global tracker index overflow fails closed with CMP006 | Implementor | `ba8e889` |
| m3 | Open — TryResolve still does not delegate to TryResolvePath | Implementor | — |
| m4 | Resolved — step-page audit link fixed | Implementor | `ba8e889` |
| m5 | Resolved — vacuous vehicle assertion removed | Implementor | `ba8e889` |
| m6 | Resolved — docs movement wording matches species pairing | Implementor | `ba8e889` |
| m7 | Open — unreferenced self state change still validates | Implementor | — |

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

## Audit rounds

Append each new round below. Never overwrite an earlier round.

### Audit round 1

#### Independence and frozen state

Fresh context: I did not author or implement Step 08 and have no shared conversation with the planner or
implementor. I read the repository at the audited commit `ebc0e46a052d4fb9d12dd0111f905be13ec51849`
(parent of this record commit) on `step/08-model-compilation-pipeline`. `main` at freeze is
`884ffe78f8b8f2a9fa6093afcce94856a6094c73` and is an ancestor of the audited commit; `main` did not move
after the freeze, so no merge-tree run is required. Closeout evidence binds the audited commit, the closeout
CI `head_sha`, and the merge candidate. The only commits above the audited commit are documentation-only
(`911408b`: closeout evidence, request, status/step-page rows).

#### Commands and results

- `./tools/verify.sh dotnet` — PASS, 970 tests / 0 failed, format clean.
- `./tools/verify.sh structure` — PASS.
- `./tools/verify.sh api` — PASS (ApiCompat + NuGet licence/vulnerability).
- `./tools/verify.sh contracts` — PASS (artifacts current, license/secret/dependency gates).
- `./tools/verify.sh docs` — PASS at `ebc0e46`; FAIL at the branch tip `911408b` on one step-page link (m4).
- `python tools/check_closeout_evidence.py docs/planning/closeout/step-08.json` — OK.
- GitHub API run `43520011439` — `workflow_dispatch`, `success`, `head_sha` = audited commit.
- `git diff --stat main...HEAD` — 94 files, +8967/−115; reviewed the substantive compiler, tests, contracts,
  samples, validator, and docs changes. Full command text and probe results are in `Evidence`.

#### Findings

Three Blockers and six Majors prevent a passing verdict: A1 (cross-collection direction dedup blocks an
explicit reverse transition), A2 (signature projection is not the ADR 0010/manifest canonical encoding), and
A3 (caller-cache staleness can change or falsify the compiled output). A4–A9 are acceptance/report/evidence
failures (rejected counts, hard limits, MMC trackers, 10k performance, CMP emission tests, charge-transport
cases). Minor findings m1–m7 and notes N1–N2 are recorded for completeness. The scientific core is otherwise
sound: the ceria/MMC/migration rules, the corrected vehicle/interstitialcy samples (when compiled one
transition at a time), the endpoint-moment charge transport, deterministic ordering, and the fail-closed
project boundary all behaved as documented. The defects are in the compiler's multi-transition/caching and
evidence/format layers, not in the verified per-mechanism physics.

#### Verdict

**Fixes required.** A1–A3 must be fixed and re-audited; A4–A9 and the minor findings should be fixed or
explicitly accepted with a recorded rationale. The branch must be frozen again at the fixed commit with a
replacement closeout CI run and refreshed closeout evidence.
