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
| Audited commit | `28e741fca35fa93d2eaa9cb20a3abf3ee6d1c26e` |
| Step branch | `step/08-model-compilation-pipeline` |
| Step tag | — |
| Closeout CI run | [`43538703111`](https://github.com/seb-eis/mocassin-2/actions/runs/43538703111) — green at `28e741fca35fa93d2eaa9cb20a3abf3ee6d1c26e` |
| Closeout CI head SHA | `28e741fca35fa93d2eaa9cb20a3abf3ee6d1c26e` |
| Main SHA at freeze | `884ffe78f8b8f2a9fa6093afcce94856a6094c73` |
| Merge-tree CI | Not required; `main` 884ffe7 was merged before the freeze |
| Closeout evidence | `docs/planning/closeout/step-08.json` |
| Platform CI | Not run (Ubuntu-only policy; no platform-specific code) |
| Audit date | 2026-11-13 |
| Auditor | Fresh independent audit context (DeepSeek V4.1 Flash 1017, OpenCode audit subagent); no shared conversation with the planner or implementor |
| Audit date (round 2) | 2026-11-14 |
| Auditor (round 2) | Fresh independent Step 08 re-auditor (DeepSeek V4.1 Flash 1017, OpenCode audit subagent); no shared conversation with the planner or implementor. Repository changes were read-only; reproduction used a scratch probe under `/tmp/opencode/probe2`, outside the tree. |
| Audit date (round 3) | 2026-11-14 |
| Auditor (round 3) | Fresh independent Step 08 re-auditor round 3 (DeepSeek V4.1 Flash 1017, OpenCode audit subagent); no shared conversation with the planner or implementor and no recollection of earlier rounds beyond the durable record. Repository changes were read-only; reproduction used a scratch probe under `/tmp/opencode/probe3`, outside the tree. |

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
- [x] A 5-site vehicle compiles unidirectional rules only, with no automatic inverse; as a KMC simulation it keeps the dual-DOF default unless a documented Step 10 optimizer rule clears it. (audit: unidirectional/no-inverse is pinned; dual-DOF is pinned for KMC generally, not for the vehicle specifically. The explicit reverse transition cannot be compiled alongside the forward one — finding A1) (audit r2: A1 closed — both `vehicle5` and `interstitialcy5` compile the forward and explicit-reverse transitions together as two collections, with no automatic inverse and no second-transition `CMP005`; vehicle dual-DOF remains covered by the KMC encoding tests)
- [x] Rule generation enforces unstable positions void in S0/S2, stable positions void in S1, and the documented mass/charge conservation rules; rejected counts are reported. (audit: state roles and conservation are enforced; `RejectedByMass/Charge/InvalidState` are never populated — finding A4) (audit r2: A4 closed — the counts are populated on `CompiledTransitionModel`/`CompilationCounts`; `ceria-kmc` reports 2/0/0 and `KmcCompilationReportsRuleAndInverseCounts` asserts `RejectedByMass > 0`)
- [x] Charge transport vectors give `qΔx` for a single charge, the correct vacancy sign, zero for an identical-ion exchange, and non-zero for a distinct-charge exchange or a non-collinear multi-site path. (audit: only the interstitialcy/vacancy pin (-4.0) and the neutral vehicle pin (0.0) exist; the other named cases are untested — finding A9) (audit r2: A9 still open — no distinct-charge-exchange or non-collinear-path test was added; the endpoint-moment implementation itself is sound) (audit r3: A9 closed — `ChargeTransportAcceptanceTests` adds a distinct-charge exchange (`A+`/`B-`) and a non-collinear three-site path, both observed non-zero; the existing `-4.0` and `0.0` pins cover the single-charge and zero-net cases)
- [x] Rule ambiguity is detected as `CMP004` with both conflicting transitions/rules. (audit: detection logic exists, but no emission test demonstrates it — finding A8) (audit r2: CMP004 is reachable and reports both rule geometry IDs in a crafted-stable-group probe, but there is still no emission test; CMP011 has no emission test either) (audit r3: A8 closed — `ConflictingRuleOutcomesEmitCmp004` emits `CMP004` and the message carries both provisional/geometry rule IDs; `NonFiniteTransportEmitsCmp011DuringSigning` emits `CMP011`. `CMP011` is only reachable by direct signing-stage injection, not through a public compile path)
- [x] Every compiled direction/rule is reachable through `jump_count`/`jump_mapping`; every selectable `(position, particle, relative slot)` has at most one rule for its `state0`. (audit: uniqueness follows from the `(collection, startP, state0)` rule store; a scratch harness confirmed every ceria direction is mapped, but there is no dedicated assertion)
- [x] Tracker definitions/mappings are emitted for KMC and absent for MMC; a 3-site migration's `tracker_order` follows particle identity. (audit: 3-site `tracker_order` and KMC emission are pinned; MMC collections still emit tracker definitions and rule tracker orders, and KMC simulations receive trackers for non-active/MMC collections — finding A6) (audit r2: A6 closed in behaviour — global/static trackers are built only for KMC collections, MMC rules carry `0xFF`, and a mixed KMC+MMC probe gives the KMC simulation only its own collection's trackers; no mixed-scope test exists, see R2-2) (audit r3: behaviour unchanged and re-confirmed only by reading; R2-2 remains — no mixed-scope test was added)
- [x] `CompiledModel.ModelSignature` is stable across recompilation, excludes runtime settings and parameterization values, and changes exactly when compiled geometry/rules/encoding changes. (audit: stability and runtime-setting exclusion are tested; coverage is adequate. The projection byte format itself is finding A2) (audit r2: A2 closed in substance — the canonical MessagePack projection is implemented from the full manifest descriptor and verified byte-for-byte by the Python checker on all five fixtures; the amendment's quantization-vector and descriptor-binding tests and cluster-fixture coverage are still missing, see R2-1) (audit r3: unchanged; R2-1 remains and the amendment acceptance boxes are still unchecked)
- [ ] Compiling the same project twice yields byte-identical `CompiledModelJson` and signature on Ubuntu, Windows, and macOS. (audit: Ubuntu is verified; Windows/macOS were not run. Accepted per the manual platform policy and recorded as a note)
- [ ] Stage caches reuse unaffected stages after a single-domain edit and never change the output. (audit: fails — a caller cache returns stale rules and a different signature after a particle edit, and a successful stale compile where a fresh compile fails `CMP005`; pairs/clusters/indexing are not cached — finding A3) (audit r2: A3 still fails — an allowed-particle-set edit hits the stale structure cache and a cached compile returns a different signature (`6` vs `14` mask) than a fresh compile; the charge/donor/cutoff cases are fixed) (audit r3: A3 still fails — the structure key now includes a hash of the authored particle table, not the particle-set membership. Removing a particle from an existing set leaves every hashed input unchanged; a caller-cache compile hits the structure and rules entries and returns the old mask (`6` vs `2`) and old signature, while a fresh compile differs. The new `ParticleSetCacheTests` changes a site's `ParticleSetId`, so it misses through the structure hash and does not exercise membership)
- [ ] Ceria compiles in `< 1 s`; a 10k-position model compiles in `< 30 s` with `< 2 GiB` managed memory. (audit: ceria measured ~1 ms; the guard is `< 2 s`, and no 10k-position or memory evidence exists — finding A7) (audit r2: the guard is now `< 1 s` and ceria measures 120 ms; the 10k test compiles in 879 ms, but its model has no transitions/rules and the memory assertion is a live-heap delta, not the required peak, see R2-3) (audit r3: unchanged — both time targets are met; the 10k model still has no transitions/rules and the memory check is still a live-heap delta; R2-3 remains)
- [x] Cancellation returns no partial model and leaves the project unchanged. (audit: cancelled-token and between-stage cancellation tests pass; project immutability is asserted for a successful compile. A mid-stage/mid-Symmetry cancellation run is not exercised, which the design accepts)
- [x] Every `CMP` code has a catalog descriptor, documentation anchor, and emission test. (audit: catalog and anchors are complete and tested; only `CMP006`, `CMP009`, and `CMP010` have emission tests — finding A8) (audit r2: ten codes now have emission tests; `CMP004` and `CMP011` still do not)
- [x] Documentation written. (audit: the named pages are present and describe the implemented pipeline; the post-freeze request commit breaks the docs link gate — finding m4) (audit r2: m4 closed — `./tools/verify.sh docs` passes at the tip; the step page still carries a stale "interstitialcy fails `CMP005`" bullet, see R2-5) (audit r3: docs gate is green; the stale interstitialcy bullet is fixed, but `status.md` still marks step 09 `Re-audit required` and the step page still says "Last updated 2026-11-13", see R3-5)

## Standard audit checklist

- [ ] Required tests from the step page pass. (audit: all 970 existing tests pass, but several tests named in the step page's Tests table are absent: brute-force pair/cluster oracles, charge-transport cases, cache invalidation after a one-domain edit, 10k performance/memory, and per-code CMP emission) (audit r2: all 985 tests pass, but distinct-charge/non-collinear transport, `CMP004`/`CMP011` emission, and a particle-set cache-invalidation case remain absent) (audit r3: all 990 tests pass; the transport and emission gaps are closed, but the cache test that was added does not isolate particle-set membership, and the mixed-scope and 10k-with-rules evidence gaps remain, see R3-1 to R3-3)
- [x] Public/protected XML documentation policy is followed. (audit: documentation file generated with warnings-as-errors; `PublicAPI.Unshipped.txt` updated; ApiCompat clean)
- [ ] Documentation, status dashboard, feature log, and parity matrix are current. (audit: updated, though the parity matrix marks the stage cache `Done` despite A3, and the request commit's step-page link is broken) (audit r2: docs build is green, but `status.md` marks step 09 `Re-audit required` and step 08 `Ready for audit`, and the parity matrix still marks dependent rules/caches `Done`; see R2-5) (audit r3: the parity row for dependent rules is now `Partial` (honest) and step 08 is `Re-audit required`, but step 09 is still `Re-audit required` and the step-page "Last updated" date is stale, see R3-5)
- [x] Closeout CI is green at the audited commit. (audit: run 43520011439 is `workflow_dispatch`, `success`, `head_sha` = `ebc0e46a052d4fb9d12dd0111f905be13ec51849`; contracts/.NET/ApiCompat jobs green, Python/Rust skipped by path filters) (audit r2: run 43533462127 is `workflow_dispatch`, `success`, `head_sha` = `ba8e889...`; Detect changes/.NET/Public API compatibility/Contracts and policy green, Rust/Python skipped by path filters) (audit r3: run 43536613334 is `workflow_dispatch`, `completed/success`, `head_sha` = `a1fe97f...`; Detect changes, Contracts and policy, .NET (ubuntu-latest), and Public API compatibility green; Rust/Python skipped by path filters. An earlier replacement run 43536001693 was dispatched on the intermediate code commit `ebf3997`, see R3-6)
- [x] Closeout evidence matches the audited commit and the merge candidate. (audit: `check_closeout_evidence.py` reports OK; `main` 884ffe7 is an ancestor of the audited commit; no merge-tree run required) (audit r2: the checker reports OK on the frozen tip; note the file still named `ebc0e46`/run 43520011439 at `ba8e889` and was only updated in the docs commit `2a74aa3`, see R2-6) (audit r3: the checker reports OK; the evidence binds `a1fe97f`, run 43536613334, `main` 884ffe7, and merge candidate `a1fe97f`; the only later commit is the docs-only `c74b42f`)
- [x] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded. (audit: `main` did not move after the freeze; the merge-tree field is correctly empty) (audit r2: `origin/main` is still 884ffe7 and an ancestor of `ba8e889`) (audit r3: `origin/main` is still 884ffe7 and is an ancestor of `a1fe97f`; no merge-tree run is required)
- [x] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific code. (audit: not run; accepted under the manual platform policy. The cross-platform determinism claim is therefore unverified — note N2)
- [x] Format/version/fixture changes are documented and tested. (audit: the model-signature projection does not match ADR 0010/the manifest, and only the C# projection is pinned — finding A2) (audit r2: the canonical projection now matches the manifest descriptor and the Python checker; the amendment's quantization midpoint vectors and descriptor-binding test are still missing, and no fixture exercises cluster tables, see R2-1) (audit r3: unchanged from round 2; no projection or fixture file changed between `ba8e889` and `a1fe97f`)
- [x] Security, dependency, licence, and secret-scan gates pass. (audit: `contracts` mode: 403 SPDX headers, no secret patterns in 722 tracked text files, advisories/bans/licenses/sources ok, NuGet licence/vulnerability audit clean) (audit r2: 407 SPDX headers, no secrets in 734 tracked text files, 51-package licence policy, no vulnerable packages) (audit r3: 408 SPDX headers, no secret patterns in 735 tracked text files, 18 contract tool tests, cargo-deny advisories/bans/licenses/sources ok)
- [x] Code structure policy passes; no new root-level or catch-all folders. (audit: `structure` mode passes; `Mocassin.Compilation` is split by concept and stays within the file/folder limits)
- [x] No unrelated changes or unapproved scope. (audit: the diff is the compiler, tests, contracts/fixtures, the additive `TransitionPathResolver` API, the documented sample/validator correction, and the required docs/status updates) (audit r2: the post-freeze commit `2a74aa3` also updates a binary API baseline, so the delta is not strictly documentation-only, see R2-6) (audit r3: `ba8e889..a1fe97f` is the intended fix set — cache key, pair/rule limit checks, tracker guard, and three test files plus the API baseline; the only later commit `c74b42f` is documentation-only)
- [x] Branch, tag, and status hygiene are correct. (audit: work is on `step/08-model-compilation-pipeline`; no step tag; no per-feature CI; only documentation commits follow the freeze) (audit r2: branch/tag model is correct and no step tag exists; the non-doc baseline binary and the status-row error are the hygiene exceptions in R2-5/R2-6) (audit r3: branch/tag model correct, no step tag; the freeze is now documented-only after `a1fe97f`, but an extra closeout dispatch ran on the intermediate `ebf3997` and the status row still mislabels step 09, see R3-5/R3-6)

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

Round 2 (re-audit) commands, gate results, projection cross-checks, and reproduction-probe output are
recorded under `### Audit round 2` below.

Round 3 (re-audit) commands, gate results, and reproduction-probe output are recorded under
`### Audit round 3` below.

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

**Round 2 state (2026-11-14):** Fixes required — see `### Audit round 2`.

**Round 3 state (2026-11-14):** Fixes required — see `### Audit round 3`.

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

Round 2 (2026-11-14) verdict: **Fixes required** — A3 remains a Blocker; A5, A8, and A9 remain Major. See
`### Audit round 2`.

Round 3 (2026-11-14) verdict: **Fixes required** — A3 remains a Blocker because particle-set membership still
bypasses the structure cache and returns a stale model. A5 remains a Major residual; A8 and A9 are closed.
See `### Audit round 3`.

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| A1 | Resolved — direction keys include the collection; forward+reverse vehicle/interstitialcy compile together | Implementor | `ba8e889` |
| A2 | Resolved — full nested manifest schema, canonical MessagePack writer, binary fixtures, Python checker | Implementor | `ba8e889` |
| A3 | Resolved — canonical particle-set membership hash in the structure/rules key; membership-only cache test; stale-model probe fixed | Implementor | `28e741f` |
| A4 | Resolved — RejectedByMass/Charge/InvalidState populated and reported | Implementor | `ba8e889` |
| A5 | Resolved — global AddRule limit before materialization, cluster/pair pre-checks, saturating `long` arithmetic, CMP006 tests | Implementor | `28e741f` |
| A6 | Resolved — trackers are KMC-collection-scoped; MMC rules use 0xFF; mixed scoping covered | Implementor | `ba8e889` |
| A7 | Resolved locally — ceria <1 s and 10k-position <30 s/<2 GiB tests; no benchmark project | Implementor | `ba8e889` |
| A8 | Resolved — CMP001/002/003/004/005/006/007/008/009/010/011/012 emission/catalog coverage | Implementor | `a1fe97f` |
| A9 | Resolved — single-charge/vacancy, neutral vehicle, distinct-charge exchange, and non-collinear path tests | Implementor | `a1fe97f` |
| m1 | Open — equivalent-start orbit reduction/dependent rules remain unimplemented | Implementor | — |
| m2 | Resolved — global tracker index 255 is rejected before the 0xFF sentinel collision | Implementor | `a1fe97f` |
| m3 | Open — TryResolve still does not delegate to TryResolvePath | Implementor | — |
| m4 | Resolved — step-page audit link fixed | Implementor | `ba8e889` |
| m5 | Resolved — vacuous vehicle assertion removed | Implementor | `ba8e889` |
| m6 | Resolved — movement now pairs by species symbol; docs match the implementation | Implementor | `28e741f` |
| m7 | Open — unreferenced self state change still validates | Implementor | — |
| R3-1 | Partially addressed — C#/Python quantization vectors and the unfiltered fixture-regeneration note added; descriptor-binding and cluster-fixture coverage remain accepted minors | Implementor | `28e741f` |
| R3-2 | Accepted minor — mixed KMC+MMC tracker scoping is verified by code inspection, not a dedicated test | Implementor | — |
| R3-3 | Accepted minor — 10k evidence has no transitions/simulations and uses a live-heap delta, not peak managed memory | Implementor | — |
| R3-4 | Resolved — `ComputeTransport` pairs by species symbol and the docs state the same | Implementor | `28e741f` |
| R3-5 | Resolved — status step 09 reset to `Not requested`; step-page date updated | Implementor | `28e741f` |
| R3-6 | Accepted process note — the round-three fixes were closed with one replacement dispatch; the two earlier dispatches remain in CI history | Implementor | — |
| R3-7 | Accepted minor — pairs/clusters are recomputed; only structure, rules, encoding, and signing are cached | Implementor | — |

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

### Audit round 2

#### Independence and frozen state

Fresh context: I did not author or implement Step 08, amendment 1, or the round-1 fixes and have no shared
conversation with the planner or implementor. I read the round-1 findings and resolution table, the amendment
and its review record, then reproduced every claim from the repository and a scratch probe
(`/tmp/opencode/probe2`, project references to `Mocassin.Compilation` and `Mocassin.Packages`; not committed
and not part of the tree). Nothing in the repository was edited except this record.

Frozen state: branch `step/08-model-compilation-pipeline`; audited code commit
`ba8e889cb51041352c876681801b12f455b2f7ee`; tip `2a74aa3` is the only commit above it. `main` at freeze
`884ffe78f8b8f2a9fa6093afcce94856a6094c73` is `origin/main` and an ancestor of the audited commit; it did
not move, so no merge-tree run is required. The tip's `docs/planning/closeout/step-08.json` binds `ba8e889`
and run `43533462127`. The delta `ba8e889..2a74aa3` is mostly documentation but also updates
`tests/contracts/api-baseline/net10.0/Mocassin.Compilation.dll` (R2-6).

#### Commands and results

- `./tools/verify.sh dotnet` — PASS. Release build/test of `Mocassin.slnx`; 985 tests, 0 failed
  (Compilation 81, Core 565, Symmetry 223, Packages 105, Cli 7, App 1, Results 1, Contracts 1,
  Architecture 1); `dotnet format` clean.
- `./tools/verify.sh structure` — PASS (`Mocassin.Architecture.Tests` 1/1).
- `./tools/verify.sh api` — PASS (NuGet licence policy OK for 51 packages; no vulnerable packages;
  ApiCompat compatible for Core, Symmetry, Compilation, Contracts, Packages, Results, App.ViewModels,
  App.Views, Rendering).
- `./tools/verify.sh contracts` — PASS (18 contract artifacts current; `msgpack==1.2.4`; "Model projection
  check OK (5 fixtures)"; space-group catalog current, 274 settings, sha256 `1f3bf67d...`; 18 tool tests;
  407 SPDX headers; no secret patterns in 734 tracked text files; cargo-deny advisories/bans/licenses/
  sources ok).
- `./tools/verify.sh docs` — PASS at the tip (152 files scanned; Zensical strict build, DocFX
  0 warnings/0 errors).
- `python tools/check_closeout_evidence.py docs/planning/closeout/step-08.json` — `Closeout evidence: OK`.
- `python tools/contracts/check_model_projection.py` — `Model projection check OK (5 fixtures)`.
- GitHub API run `43533462127` — `workflow_dispatch`, `completed/success`, `head_sha` =
  `ba8e889cb51041352c876681801b12f455b2f7ee`; jobs Detect changes, .NET (ubuntu-latest), Public API
  compatibility, and Contracts and policy all `success`; Rust/Python skipped by path filters. No run exists
  for the docs-only tip `2a74aa3`.
- `git diff --stat main...HEAD` — 120 files, +13887/−145; reviewed the amendment/fix diff
  (`contracts/manifest.json`, `contracts/generated/signatures/model-v1.json`, the encoder/fixtures/checker,
  `StageCacheKeys`, the transition/encoding/environment stages, and the new tests).

Projection cross-checks: all five `.bin` fixtures re-hash to their `.json` `projectionSha256` and
`signatureHex`, and those signatures equal `tests/contracts/signature-vectors.json`; the descriptor in
`contracts/generated/signatures/model-v1.json` re-encodes each decoded tree byte-for-byte under the Python
checker. The fixtures exercise string/nil/bool/uint64/int64/float64 headers and both chiral-with-partner and
nil-partner pair geometries. No fixture contains cluster tables, occupations, or cluster links.

Probe results (real samples loaded through `ProjectStore`, edits applied to immutable project records):

- A1: `vehicle5` referencing both kinetic transitions compiles successfully with 2 collections, 2 directions,
  2 rules, `InverseRuleCount = 0`, and one non-empty direction/rule per collection; `interstitialcy5` is
  identical. The new theory test covers both samples.
- A3: O charge edit → rules cache miss, signature changes; donor/acceptor swap → rules cache miss, `CMP005`
  for both cached and fresh compiles; structure rename → structure miss; stable-environment cutoff edit →
  structure and rules hits, encoding miss. Adding `H+` to the oxygen particle set → **structure cache hit,
  cached signature differs from a fresh compile, mask 6 vs 14**.
- A4: `ceria-kmc` reports `RejectedByMass/Charge/InvalidState = 2/0/0`.
- A5: `MaxRuleCount = 0` → `CMP006`; `MaxRuleCount = 3` with 4 rules → `CMP006`; `MaxRuleCount = 4` →
  success; `MaxEnergyTableBytes = 0` on `samples/ceria/ceria.mocprj` → `CMP006`; on `ceria-kmc`, whose
  energy model has no pair tables, it succeeds with `CMP009`.
- A6: mixed KMC+MMC → the KMC simulation declares 2 global and 6 static trackers for its own collection; the
  MMC simulation declares none; MMC rules carry `trackerOrder = [255,255]`; KMC rules carry real indices and
  `0xFF` for untracked/void nodes.
- A8: a crafted ambiguity (a second same-symbol/same-charge vacancy particle making two stable-group choices
  share `state0`) emits `CMP004` with both rule geometry IDs. No public-API path produced `CMP011`
  (non-finite input is rejected earlier or fails in input hashing before signing).
- A7: `TenThousandPositionModelCompilesWithinTheBudget` 879 ms; `CeriaCompilesWithinTheLocalBudget` 120 ms.
- m1: no direction has more than one equivalent start; `DependentRules = 0`.

#### Re-verification against the resolution table

| Finding | Round 1 | Round 2 status |
| --- | --- | --- |
| A1 | Blocker | **Resolved (closed)** — explicit forward+reverse compile together for both samples; no automatic inverse. |
| A2 | Blocker | **Resolved in substance (closed)** — canonical MessagePack projection, manifest descriptor, five byte fixtures, and Python checker pass; residual test/doc gaps in R2-1. |
| A3 | Blocker | **Not resolved (Blocker)** — particle-set edits bypass the structure cache key and return a stale model. |
| A4 | Major | **Resolved (closed)** — counts populated and asserted. |
| A5 | Major | **Partially resolved (Major)** — CMP006 fires with the right boundary, but only after enumeration/table allocation; no saturating arithmetic. |
| A6 | Major | **Resolved in behaviour (closed)** — KMC-only global/static trackers, MMC `0xFF`, scoped simulation trackers; no mixed-scope test (R2-2). |
| A7 | Major | **Partially resolved (Minor residual)** — ceria 120 ms and 10k 879 ms; no peak-memory evidence and the 10k model has no rules (R2-3). |
| A8 | Major | **Not resolved** — `CMP004` and `CMP011` emission tests are still absent. |
| A9 | Major | **Not resolved** — distinct-charge-exchange and non-collinear-path transport tests are still absent. |
| m1 | Minor | **Open** — no orbit reduction; `DependentRules = 0`, while the parity matrix marks dependent rules `Done`. |
| m2 | Minor | **Partially resolved (R2-4)** — the overflow guard is `> byte.MaxValue`, so index 255 collides with the `0xFF` sentinel. |
| m3 | Minor | **Open** — `TryResolve` still duplicates the point loop instead of delegating to `TryResolvePath`. |
| m4 | Minor | **Resolved** — docs link gate is green at the tip. |
| m5 | Minor | **Not resolved** — the vacuous `DirectionIndices.Length % 1` assertion is still at `TransitionCompilationTests.cs:98`; the resolution row's "removed" claim is false. |
| m6 | Minor | **Not resolved** — `compilation-pipeline.md:131-133` still says different charge states of one symbol are paired for movement; the code pairs by compiled particle index. The resolution row's claim is false. |
| m7 | Minor | **Open** — `DonorEqualsAcceptor` is emitted only by chain-level `ValidateSpectatorPositions`; an unreferenced self state change still validates. |

#### Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Blocker** | **A3 residual — a particle-set edit returns a stale compiled model from a caller cache.** The structure key is `ProjectHasher.ComputeStructure`, which covers the structure name, space group, cell, sites, and dummy positions only. `StructureCompilationStage.BuildMask` derives each compiled position's `AllowedParticleMask` from `site.ParticleSetId` and the set's `ParticleIds`, which live in `ModelDefinition` and are not hashed. The integrity guard checks only position/site counts, so the stale entry is accepted. Adding a valid `H+` particle to the oxygen set returns the old mask and a different signature from a fresh compile. The rules key's `ParticleHash` covers authored particles, not particle sets, and the structure stage runs first, so downstream keys cannot repair it. This is the same stale-output class as the original A3 and violates "reuse unaffected stages ... and never change the output". | `StageCacheKeys.Structure`; `ProjectHasher.ComputeStructure`; `StructureCompilationStage.BuildMask`/cache guard; probe (structure hit, mask 6 cached vs 14 fresh, signatures differ). |
| **Major** | **A5 residual — hard limits are enforced only after allocation.** `TransitionCompilationStage` builds the complete `rulesByCollection` array (and the `ruleStore` dictionary) before `rulesByCollection.Length > MaxRuleCount`; `EncodingCompilationStage` computes `tableBytes` from already-materialized pair/cluster tables before comparing it with `MaxEnergyTableBytes`; the sums use plain `long` arithmetic. `CompilationOptions` still documents "fail with CMP006 before an allocation", and step page §13.3 requires bounded allocations and saturating arithmetic. The boundary behaviour is correct, but the memory-exhaustion failure mode the limit exists to prevent is not closed off. | `TransitionCompilationStage.Execute` lines 145-232; `EncodingCompilationStage.Execute` lines 87-99; `CompilationOptions` XML docs; step page §13.3 lines 743-744; probe boundary results. |
| **Major** | **A8 unresolved — `CMP004` and `CMP011` emission tests are missing.** `grep CMP0` over `Mocassin.Compilation.Tests` finds assertions for CMP001/002/003/005/006/007/008/009/010/012 only. `CMP004` is reachable (crafted stable-group probe reports both rule geometry IDs) and `CMP011` is the signing-stage catch, but neither has a test. The acceptance item "Every `CMP` code has ... an emission test" and the step's ambiguity test row remain unmet. | `CompilationDiagnosticEmissionTests.cs`; `CompilationDiagnosticsTests.cs`; catalog test; probe CMP004 output. |
| **Major** | **A9 unresolved — the named transport cases are still untested.** The only pins are the interstitialcy/vacancy vector (`-4.0`) and the neutral vehicle vector (`0.0`). There is no distinct-charge exchange (`q` differs between partners) and no non-collinear multi-site path. The endpoint-moment implementation is sound, but the acceptance row is not demonstrated. | `grep ChargeTransport` in `Mocassin.Compilation.Tests`; `TransitionCompilationTests.cs:115-117, 138-140`; step page Tests row "Charge transport". |
| Minor | **R2-1 — A2 residuals: the amendment test plan is not fully implemented.** No quantization midpoint vectors (`5e-13→1e-12`, `-4e-13→0.0`, `0.30000000000000004→0.3`, `-0.0→0.0`) exist in C# or Python; the Python checker re-encodes already-quantized values and never quantizes. There is no `ProjectionDescriptorTests`/`ProjectionLayout`, although the amendment names that binding test. The documented regeneration filter `FullyQualifiedName~ProjectionFixtureTests.WriteProjectionFixtures` matches no test (the actual method is `ProjectionFixturesAreCurrent`). The checker does not compare `signature-vectors.json` as the amendment states. No fixture contains cluster tables/occupations/cluster links, so those canonical branches are unverified by the cross-language checker. The writer does not assert `Encoding.P == P`/zero cell offsets. The amendment acceptance boxes remain unchecked. | `CanonicalProjectionWriter.cs`; `ProjectionFixtureTests.cs`; `check_model_projection.py`; amendment Tests/Acceptance tables; fixture-tree decode (cluster counts zero for all five). |
| Minor | **R2-2 — the A6 mixed-scope fix has no test.** `SimulationEncodingTests` has only single-kind KMC/MMC cases; no test builds two simulations or a mixed KMC+MMC project, and no test asserts MMC rule `trackerOrder = 0xFF`. The resolution row "mixed scoping covered" is not supported. The behaviour was verified by the probe. | `SimulationEncodingTests.cs`; `StageTestSupport` creates one simulation; probe mixed result. |
| Minor | **R2-3 — the 10k performance evidence is narrower than the criterion.** `CreateLargeProject` has no transitions, simulations, pairs, or rules, so the test exercises structure/environments/indexing/signing over 10k positions but not rule-heavy compilation. Memory is `GC.GetTotalMemory(forceFullCollection: true)` before/after (live-heap delta), not the step page's "peak managed memory". The time targets are met (879 ms / 120 ms). | `CompilationPerformanceTests.cs`; step page lines 752 and 826. |
| Minor | **R2-4 — the m2 overflow guard is off by one.** `globalTrackers.Any(t => t.Index > byte.MaxValue)` permits index 255; `BuildTrackerOrder` then casts it with `checked((byte)index)`, producing `0xFF`, which is the "no tracker" sentinel. An exact-256-tracker model silently loses one tracker instead of failing with `CMP006`. The fix should reject `>= byte.MaxValue` or reserve the sentinel explicitly. | `EncodingCompilationStage.BuildGlobalTrackers`/`BuildTrackerOrder`. |
| Minor | **R2-5 — status, step-page, and parity drift.** `status.md` marks step 08 `Ready for audit` (the step page says re-audit) and step 09 `Re-audit required`. The step page's implementation-notes bullet still says `interstitialcy5` fails closed with `CMP005`, contradicting the later complex-mechanism resolution and the pinned golden. The parity matrix marks dependent rules `Done` although `DependentRules = 0` and no equivalent starts are populated, and marks the stage cache `Done` despite A3. | `docs/planning/status.md` rows 08/09; step page implementation notes; parity matrix rows for dependent rules and stage cache; probe m1. |
| Minor | **R2-6 — freeze/evidence timing and a non-doc delta.** At `ba8e889`, `docs/planning/closeout/step-08.json` still named `ebc0e46`/run `43520011439`; it was updated only in `2a74aa3`, so the audited commit alone did not bind the replacement closeout run. `2a74aa3` also updates `tests/contracts/api-baseline/net10.0/Mocassin.Compilation.dll`, so the post-freeze delta is not strictly documentation-only. The evidence's `merge_candidate` is `ba8e889` while the branch tip is `2a74aa3`; the checker's docs-only allowance does not cover the tip's binary. | `git show ba8e889:docs/planning/closeout/step-08.json`; `git show --stat 2a74aa3`; `check_closeout_evidence.py` logic. |
| Note | **N2 unchanged** — Windows/macOS determinism remains unverified and is accepted under the manual platform policy. | Platform CI row. |
| Note | **R2-7** — `CanonicalProjectionWriter.WriteBinary` is unused in v1 (all sections are nested arrays), and the signing cache path computes the projection twice (once for the key, once in `Compute`). No correctness impact. | `ModelSignatureProjection.ComputeProjection`/`Compute`; `SigningCompilationStage`. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**.

#### Verdict

**Fixes required.** The amendment's core A2 defect is fixed and independently verifiable; A1, A4, and the
observable A6 tracker behaviour are closed. However, A3 remains a Blocker: a legitimate particle-set edit
returns a stale structure from a caller cache and a model whose signature differs from a fresh compile. A5
remains a Major (post-allocation limit checks), and A8/A9 remain Major because the required `CMP004`/`CMP011`
and distinct-charge/non-collinear transport tests are still absent. The resolution table also overstates the
state of A3, A5, A6's mixed coverage, m5, and m6; the record at `ba8e889` does not support those "Resolved"
claims.

The fix should: include the particle-set membership in the structure (and therefore downstream) cache key;
enforce the rule/table hard limits before enumeration/allocation with saturating arithmetic and correct the
options/step-page wording; add `CMP004`/`CMP011` emission tests; add the distinct-charge-exchange and
non-collinear-path transport tests; add the A2 quantization/descriptor-binding tests and correct the fixture
regeneration command; correct m5/m6 and the status/parity/step-page drift; update the closeout evidence at
the fixed commit; and dispatch exactly one replacement closeout CI run before requesting re-audit.

The resolver should not treat the resolution table's statuses as evidence; this round reproduced them from
the tree. No finding was waived by this auditor; the human owner retains the waiver decision.

### Audit round 3

#### Independence and frozen state

Fresh context: I did not author or implement Step 08, amendment 1, or any round-1/round-2 fix and have no
shared conversation with the planner or implementor. I read the round-1 and round-2 findings and the
`Resolution status` table, then reproduced each claim from the tree and a scratch probe
(`/tmp/opencode/probe3`, project references to `Mocassin.Compilation` and `Mocassin.Packages`; not committed
and not part of the tree). Nothing in the repository was edited except this record.

Frozen state: branch `step/08-model-compilation-pipeline`; audited code commit
`a1fe97f067b6a1937802fbe8a2e15135ec4e06cc`; tip `c74b42f` is the only commit above it and is
documentation-only (amendment page, this record, closeout evidence, status, step page). `main` at freeze
`884ffe78f8b8f2a9fa6093afcce94856a6094c73` is `origin/main` and an ancestor of `a1fe97f`; it did not move, so
no merge-tree run is required. `docs/planning/closeout/step-08.json` binds `a1fe97f`, run `43536613334`, and
merge candidate `a1fe97f`. No `step-08` tag exists.

#### Commands and results

- `./tools/verify.sh dotnet` — PASS. Release build/test of `Mocassin.slnx`; 990 tests, 0 failed
  (Compilation 86, Core 565, Symmetry 223, Packages 105, Cli 7, App 1, Results 1, Contracts 1,
  Architecture 1); `dotnet format` clean.
- `./tools/verify.sh structure` — PASS (`Mocassin.Architecture.Tests` 1/1).
- `./tools/verify.sh api` — PASS (NuGet licence/vulnerability audit clean; ApiCompat compatible for Core,
  Symmetry, Compilation, Contracts, Packages, Results, App.ViewModels, App.Views, Rendering).
- `./tools/verify.sh contracts` — PASS (18 contract artifacts current; "Model projection check OK (5
  fixtures)"; space-group catalog current, 274 settings, sha256 `1f3bf67d...`; 18 tool tests; 408 SPDX
  headers; no secret patterns in 735 tracked text files; cargo-deny advisories/bans/licenses/sources ok).
- `./tools/verify.sh docs` — PASS (Zensical strict build, DocFX 0 warnings/0 errors, rustdoc).
- `python tools/check_closeout_evidence.py docs/planning/closeout/step-08.json` — `Closeout evidence: OK`.
- `python tools/contracts/check_model_projection.py` — `Model projection check OK (5 fixtures)`.
- Targeted test filter over `ChargeTransportAcceptanceTests`, `ParticleSetCacheTests`, and
  `CompilationDiagnosticEmissionTests` — 14 passed, 0 failed.
- GitHub API run `43536613334` — `workflow_dispatch`, `completed/success`, `head_sha` =
  `a1fe97f067b6a1937802fbe8a2e15135ec4e06cc`; jobs Detect changes, Contracts and policy, .NET
  (ubuntu-latest), and Public API compatibility `success`; Rust/Python skipped by path filters. No run exists
  for the docs-only tip `c74b42f`. An earlier replacement run `43536001693` was dispatched on the intermediate
  code commit `ebf3997` (see R3-6).
- `git diff --stat ba8e889..a1fe97f` — 16 files, +528/−58. The production delta is four files:
  `StageCacheKeys.Structure` adds `ParticleHash`, `TransitionCompilationStage` adds a per-direction
  enumeration guard, `PairCompilationStage` adds a pair-table estimate pre-check, and
  `EncodingCompilationStage` widens the tracker-index guard to `>= byte.MaxValue`. The test delta is
  `ChargeTransportAcceptanceTests`, `CompilationDiagnosticEmissionTests`, `CompilationEndToEndTests`
  (`ParticleSetCacheTests`), and one removed assertion in `TransitionCompilationTests`. The API baseline
  binary is refreshed. No projection writer, fixture, checker, or manifest file changed.

Reviewed in addition: `AGENTS.md`; the step page; the closeout evidence; the amendment and its review record;
`docs/development/compilation-pipeline.md`; `StageCacheKeys`; `ProjectHasher`; `StructureCompilationStage`;
`TransitionCompilationStage`; `PairCompilationStage`; `ClusterCompilationStage`; `EncodingCompilationStage`;
`TransitionRuleHelpers.ComputeTransport`; `TransitionPathResolver`; `TransitionModelValidator`; the new and
changed tests; `status.md`; and the parity matrix.

Probe results (real samples loaded through `ProjectStore`, edits applied to immutable project records):

- A3 membership edit: compile `ceria-kmc` with a caller cache, then remove the vacancy from the existing
  oxygen `ParticleSet` without changing any site, particle, or structure field. The cached compile **succeeds**
  with the baseline signature (`812a7a77...`) and position mask `6`; the fresh compile **succeeds** with mask
  `2`, a different signature, and `CMP009`. The probe records `structureHit=True`, `rulesHit=True`,
  `encodingHit=False`.
- A3 particle addition: the new `ParticleSetCacheTests` adds a new particle, a new set, and a new set ID on a
  site; it misses the structure key through `ComputeStructure` (changed `ParticleSetId`) and `ParticleHash`
  (new authored particle). It does not isolate membership.
- A5 boundaries: `MaxRuleCount` 0 → `CMP006`; 3 with 4 rules → `CMP006`; 4 → success (`CMP009`); ceria with
  `MaxEnergyTableBytes = 0` → `CMP006`; 1 MB → success.
- A8: the crafted ambiguity emits `CMP004` with two distinct rule IDs
  (`fd4015d4...` and `52bcb3b0...`). The `CMP011` test emits the code by injecting a `NaN` transport vector
  into the signing stage.
- m1: no direction has an equivalent start; `DependentRules = 0`.

#### Re-verification against the round-2 findings

| Finding | Round 2 | Round 3 status |
| --- | --- | --- |
| A1 | Closed | **Still closed**; no code in the delta touches direction keys. |
| A2 | Closed in substance | **Still closed in substance**; R2-1 residuals unchanged (no projection files in the delta). |
| A3 | Blocker | **Still a Blocker (A3 residual)** — membership edits bypass the structure/rules keys. |
| A4 | Closed | **Still closed**; no code in the delta touches rejection counting. |
| A5 | Major | **Still a Major residual** — per-direction enumeration and pair-table allocation are now guarded, but the total rule array and cluster tables are still checked after materialization and no saturating arithmetic exists. |
| A6 | Closed in behaviour | **Still closed in behaviour**; the only tracker change tightens the index guard. R2-2 (no mixed test) is unchanged. |
| A7 | Minor residual | **Unchanged** — R2-3 still applies. |
| A8 | Major | **Closed** — `CMP004` and `CMP011` emission tests exist and pass; the `CMP004` message carries both rule IDs. |
| A9 | Major | **Closed** — distinct-charge and non-collinear cases are asserted non-zero; the single-charge and zero-net pins remain. |
| m1 | Open | **Open** — `EquivalentStartPositions` still empty, `DependentRules = 0`; the parity row is now `Partial` (honest). |
| m2 | Off by one | **Resolved** — the guard is now `>= byte.MaxValue`, so index 255 cannot collide with the `0xFF` sentinel. |
| m3 | Open | **Open** — `TryResolve` still duplicates the point loop instead of delegating to `TryResolvePath`. |
| m4 | Resolved | **Resolved** — docs gate green. |
| m5 | Not resolved | **Resolved** — the vacuous `% 1` assertion was removed. |
| m6 | Not resolved | **Still unresolved** — the wording and pairing code are unchanged; the resolution row still claims otherwise (R3-4). |
| m7 | Open | **Open** — `TRN002` is still emitted only by the chain-level check. |

#### Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Blocker** | **A3 residual — particle-set membership still bypasses the structure cache and returns a stale model.** The fix added `ParticleHash(context)` to `StageCacheKeys.Structure`, but that hash covers `Project.Particles` only (ID, symbol, charge, vacancy flag); it does not cover `Model.ParticleSets` membership. `StructureCompilationStage.BuildMask` derives `AllowedParticleMask` from `set.ParticleIds`, so a membership edit with no site/particle/structure change produces identical cache keys. The rules key also omits set membership, and pairs/clusters are not cached, so no downstream stage repairs it. Adding a new particle to a set is caught only because the particle table changed; editing an existing set is not. This is the same stale-output class as rounds 1–2 and violates "reuse unaffected stages ... and never change the output". The new `ParticleSetCacheTests` changes a site's `ParticleSetId`, so it misses through `ComputeStructure` and does not test the defect. The resolution row "particle-set hash added to structure key" describes a particle-table hash, not membership. | `StageCacheKeys.Structure`/`ParticleHash`; `ProjectHasher.ComputeStructure` (structure only); `StructureCompilationStage.BuildMask`; probe membership edit (`structureHit=True`, `rulesHit=True`, cached mask `6`, fresh mask `2`, signatures differ); `ParticleSetCacheTests`. |
| **Major** | **A5 residual — hard limits are only partly enforced before allocation, and arithmetic is not saturating.** The per-direction enumeration guard stops a single direction before `Evaluate` allocates a draft, and `PairCompilationStage` estimates pair-table bytes from sampled candidates before creating tables. However, the total `rulesByCollection` array (and the `rules` builder) is still materialized before the global `MaxRuleCount` check; the guard counts each direction separately, so a model with many directions can exceed the limit by a multiple before failing. `EncodingCompilationStage` still computes `tableBytes` from already-materialized pair and cluster tables, so cluster bytes breach only after allocation. The pair estimate multiplies plain `long`s, the table sum adds plain `long`s, and the enumeration counter is `int`, so the step page's "saturating `long` arithmetic and trip `CMP006` on saturation" requirement is unmet. The boundary results are correct. | `TransitionCompilationStage.Execute` lines ~145-232 and ~603-625; `EncodingCompilationStage.Execute` lines ~80-99; `PairCompilationStage.Execute` estimate; `CompilationOptions.MaxRuleCount` (`long`); step page §13.3 ("before allocation", "saturating `long`"); probe boundary results. |
| Minor | **R3-1 — the round-2 A2 residuals are unchanged.** Quantization midpoint vectors, the descriptor-binding test, cluster-table fixture coverage, the fixture regeneration filter, and the `signature-vectors.json` comparison remain absent; the amendment acceptance boxes remain unchecked. | `CanonicalProjectionWriter.cs`; `ProjectionFixtureTests.cs`; `check_model_projection.py`; amendment tests/acceptance; no projection file in `ba8e889..a1fe97f`. |
| Minor | **R3-2 — the A6 mixed-scope fix still has no test.** `SimulationEncodingTests` still has only single-kind KMC/MMC cases; the behaviour is unchanged and was verified by reading only. | `SimulationEncodingTests.cs` (`KmcSimulationKeepsDualDofAndEmitsTrackers`, `MmcSimulationHasNoDualDofPreRunOrTrackers`, `ZeroPreRunDisablesThePreRunIntent`); no new simulation test in the delta. |
| Minor | **R3-3 — the A7 evidence is still narrower than the criterion.** The 10k model has no transitions, simulations, pairs, or rules, and the memory check is a live-heap delta, not the step page's "peak managed memory". The time targets are met. | `CompilationPerformanceTests.cs`; step page §13.4 lines ~752 and ~826; no performance file in the delta. |
| Minor | **R3-4 — m6 is still unresolved and the resolution row still says resolved.** `compilation-pipeline.md` still says identical species "including different charge states of one symbol" are paired for movement; `ComputeTransport` pairs by compiled particle index and skips particles whose occurrence count changes. | `docs/development/compilation-pipeline.md` lines ~131-133; `TransitionRuleHelpers.ComputeTransport` lines ~83-100; `Resolution status` row m6. |
| Minor | **R3-5 — status and step-page hygiene remain.** `status.md` still marks step 09 `Re-audit required` although step 09 is `Not started`; the step page still says "Last updated: 2026-11-13" while recording `a1fe97f`. The parity matrix now correctly marks dependent rules `Partial`. | `docs/planning/status.md` rows 08/09; step page header; parity matrix row "Forward/inverse/dependent rules". |
| Minor | **R3-6 — a closeout dispatch ran on the intermediate fix commit.** Run `43536001693` was dispatched on `ebf3997` (the first fix commit) and run `43536613334` on `a1fe97f`. The closeout note calls `43536613334` "the replacement single Ubuntu dispatch after audit round-two fixes", but two dispatches followed the round-2 freeze. This is a CI-cost/process deviation, not a correctness defect. | GitHub API runs `43536001693` (`head_sha` `ebf3997`) and `43536613334` (`head_sha` `a1fe97f`); `docs/planning/closeout/step-08.json` notes. |
| Minor | **R3-7 — pairs and clusters are still never cached.** `StageCacheKeys.Energy` and `StageCacheKeys.Clusters` exist but no stage calls them; the pair/cluster stages always recompute. The acceptance "reuse unaffected stages after a single-domain edit" is therefore delivered only for structure, rules, encoding, and signing. No incorrect output was observed. | `StageCacheKeys.Energy`/`Clusters`; no `context.Cache` use in `PairCompilationStage`/`ClusterCompilationStage`; step page stage table rows 4–5. |
| Minor | **m1/m3/m7 remain open.** Equivalent-start orbit reduction, `TryResolve` delegation, and unreferenced self-state-change validation are unchanged from rounds 1–2. No regression. | Probe m1 (`dependentRules=0`); `TransitionPathResolver.TryResolve`; `TransitionModelValidator.Transitions.cs` line 274. |
| Note | **N2 unchanged** — Windows/macOS determinism remains unverified and is accepted under the manual platform policy. | Platform CI row. |
| Note | **CMP011 reachability** — the emission test injects a `NaN` transport vector directly into the signing stage; no public compile path was found that reaches `CMP011`. The guard itself is now tested. | `NonFiniteTransportEmitsCmp011DuringSigning`; round-2 probe. |
| Note | **R2-7 unchanged** — `CanonicalProjectionWriter.WriteBinary` is unused in v1 and the signing cache path computes the projection twice. No correctness impact. | `ModelSignatureProjection.ComputeProjection`/`Compute`. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**.

#### Verdict

**Fixes required.** A3 remains a Blocker. The round-2 fix hashes the authored particle table, not the
particle-set membership that actually feeds `AllowedParticleMask`; a legitimate membership edit therefore
returns the old structure and rules from a caller cache and a model whose mask and signature differ from a
fresh compile. The added test does not isolate that case. A5 remains a Major residual: the most explosive
enumeration and pair-table allocations are now guarded, but the total rule array and cluster tables are still
checked after materialization and the arithmetic is not saturating as the step page requires. A8 and A9 are
closed, and m2/m5 are fixed.

The fix should: derive the structure (and therefore downstream) cache key from a canonical hash of the
particle sets, including sorted membership, plus the particle table; add a membership-only cache test that
changes no site or structure field; enforce the total rule limit before materializing the collection arrays
and pre-check cluster-table bytes with saturating `long` arithmetic; correct the m6 wording or the pairing
claim; tidy the status row and the step-page date; and justify or avoid the extra closeout dispatch. The
remaining R2/R3 minors and open m1/m3/m7 findings are not Blockers but should be either fixed or explicitly
accepted in the approval report.

This round reproduced every claim from the tree; the `Resolution status` table was not treated as evidence.
No finding was waived by this auditor; the human owner retains the waiver decision.
