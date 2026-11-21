# Step 14 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-14.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 14 — Simulation Context Initialization |
| Request owner | Planner (design freeze); the step agent takes ownership of resolutions at the start gate |
| Orchestrator | Orchestrator agent (spawns the planning auditor) |
| Step agent (implementor) | Not assigned — created from the reviewed `main` commit after the start-gate approval |
| Review initiated by | Orchestrator, on request of the planner |
| Reviewed commit | `b6dc50361e749954480efa1d1559d987c10b4142` (round 2 freeze: design corrections after round 1) |
| Previous reviewed commit | `7c7ff0de83a4ae4c5f9849650e0601c7238494b8` (round 1 freeze) |
| Docs branch | `docs/step-14-design` (from `main` = `bbc2d083ed96fa6e26526e335304107b20a7fdcb`, `step-13` merged and tagged) |
| Review date | 2026-11-20 (round 1); re-review request 2026-11-21 |
| Reviewer | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh context, no shared conversation with the planner, implementor, or orchestrator |

Step page: [`Step 14 — Simulation Context Initialization`](../steps/step-14-simulation-context-initialization.md)

**Freeze note.** The design docs are committed as the freeze commit; this request record is a documentation-only
follow-up so the freeze SHA can be written. The round-1 freeze (`7c7ff0d`) included the step page, the
architecture, data contract, quality-strategy, roadmap, open-decision, parity, code-structure,
solver-architecture, and physics updates, plus the Step 15 interface reconciliation. The round-2 freeze
(`b6dc503`) applies the F-1…F-16 resolutions: the owner-major link CSR with an inverse routing index, the
supercell selection-pool key space, the pre-allocation memory plan and derived-array caps, the receiver-major
jump links, the repair of the resume, goal, memory, `nu_max`, `!Sync`, and crate-dependency issues, and the
Step 15/16 interface reconciliation. No code, test, configuration, contract artifact, or generated file is part
of either freeze.

**No code exists yet.** Step 14 has no branch and no implementation. The design is judged on whether the runtime
layout, the cross-step interfaces, the two gated decisions, the fixtures, the test/budget plan, and the
documentation plan are ready to implement.

> Orchestration: the orchestrator spawns a fresh planning auditor after the planner has frozen the design and
> completed this request, and relays the verdict and owner decisions. The planner and step agent update the
> design docs, apply fixes on the design docs or a maintenance branch, add resolutions without altering reviewer
> text, and ask the orchestrator for re-review until the auditor no longer identifies issues. The orchestrator
> does not edit the design or the review record. The human owner approves the final verdict and the start of
> implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | D. Solver |
| Depends on | 13 (`Done`; merged into `main` as `2613a0dcbc3ddbfe4bb5b54f47821d3ffa951789`, tagged `step-13`, accepted 2026-11-20) |
| Delivers | A deterministic, allocation-bounded simulation context built once per job from the validated `JobPackage`: lattice/environment arenas, a translation-compact per-P1 neighbour-link CSR, the cluster instance/code/energy runtime, site+pair environment energy vectors and per-centre cluster energy, the exact initial lattice energy, KMC selection pools, per-`(P1, direction)` jump status, the tracker index space, counters, goals, normalization, the time step, and the resume hook. No energy engine, KMC/MMC loop, trackers, or `.mcs` output. |
| Sizing | Complexity class **High** (cross-step interfaces, behavioural irreversibility, test-matrix breadth). **No split proposed**; owner acceptance as one High unit is requested at the start gate under [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers). Six features. API frozen at feature 3; a forced API rework is a stop-and-report re-review trigger. |

## Design decisions reviewed

- [x] **Context ownership and phasing.** One `SimulationContext` per job from the validated `JobPackage`; no
      I/O, no package re-read, no interior mutability; the twelve-phase build order; `InitOutcome`
      (`Ready`/`AlreadyCompleted`); `Send` but not `Sync`; the frozen accessor surface.
- [x] **Data layout and bounds.** `[A,B,C,P]` dimensions and the documented linear index; struct-of-arrays
      arenas; `u32`/`u8`/`u64`/`f64` choices; explicit energy stride; the `ContextLimits` table and the
      checked-arithmetic rule that a limit violation is exit 12, not a panic.
- [x] **Neighbour links.** The translation-compact per-P1 CSR versus the original per-supercell link array;
      `wrap(cell + offset) * P + target_position`; the deterministic sort (`pair_id`, target, offset) as a
      reproducibility contract; the per-link cluster association; the reader-cap consistency.
- [x] **Cluster runtime and the v1 geometry limitation (OD-1).** `byte_position` = the environment's own slot;
      slot 0 = centre; instance/membership/code representation; code-existence validation; the unique-image rule
      and the fail-closed rejection of zero/multiple images; the permanent fix as a future `.msl` revision.
- [x] **Cluster table layout correction.** One energy per occupation code with slot 0 = centre particle;
      `particle_table_mapping` as the ascending allowed-centre list, not a `[u8; 64]` row map; the Step 15
      `[center_row][code]` wording replaced.
- [x] **Energy runtime and initial lattice energy (OD-2).** eV→kT once; the site table separate from the
      site+pair vector; per-instance cluster energy; the exact total (site once, pair ×0.5, cluster once at the
      centre); the documented deviation from the original's uniform 0.5 and its hand-computed test.
- [x] **Selection pools.** Direction-count buckets ascending; the `selection_particle_mask` condition; optimizer
      removal and the re-add-after-change semantics; O(1) `slot_of`/`pool_of` swap-remove; `selectable_jump_count`
      bounds; the `-1`/`0` collapse because the payload uses `u32`.
- [x] **KMC jump status.** Status identity `direction * P + position`; `resolve_node` with node 0 replaced by the
      selected bucket (cross-bucket mapping); sender/receiver link collection with `link_id` relative to the
      sender P1 list; the structural receiver filter (deviation from the original's initial-occupation filter);
      the `L*(L-1) ≤ 90` derived bound; cluster touches per direction; the Step 16 exact-occupation data
      interface.
- [x] **Goals, counters, tracker index space.** `mobile_count` = non-void positions (passive included);
      `prerun`/`main`/`total` and the round-up-to-100 block; the mobile/static index formulas; counters zeroed
      and tracker values deferred to Step 18; the zero-mobile/zero-selectable errors.
- [x] **Normalization, time step, and `nu_max`.** `K_fix = exp(E_norm/kT)`; `K_auto = 1/RawMax`; the
      non-finite fallback; `dof`; `nu_max` derived from the job cap or the maximum active rule frequency because
      the v1 payload does not persist the original's build-time modulus; `dt` finite/positive.
- [x] **Transport-formula input contract (Step 13 assignment).** `field_scale = |E|*1e-10/kT_eV` and the
      normalized direction; the stored `charge_transport_vector` used verbatim; no recomputation from
      movements; fractional P1 coordinates unavailable; the residual risk and future-format note.
- [x] **Resume.** The plain `ResumeState`/`ResumePhase` input; no `mocassin-state` dependency; every validation;
      the re-derived mobile mapping; `RawMax` preserved; `Completed` returns `AlreadyCompleted` and is never
      resumed; exit mapping for a bad RNG state.
- [x] **Errors, exits, determinism, cancellation.** The `ContextError` → exit 12/13/15 table; no `unwrap` on
      package data; no hash iteration; fixed f64 summation order; no `rayon`; per-phase and every-4096
      cancellation; `MemoryReport`.
- [x] **Step 15/16/17/18/19 interfaces.** The §15 interface table and whether each consumer has what it needs;
      the Step 16 exact-occupation check ownership; the Step 18 tracker-value boundary; the Step 19 resume/CLI
      boundary; no hidden Step 14 physics.
- [x] **Complexity class and feature list.** High on cross-step interfaces, irreversibility, and test-matrix
      breadth; one acceptance story; the no-split case; the six feature branches with their scopes and outcomes;
      the API-freeze guard; the roadmap §3.1 record and the owner-acceptance request.
- [x] **Fixtures, tests, and budgets.** Reuse of `msl-v1`, `amend-1-net10`, `step-13-populated`,
      `step-13-centered`; the new real-writer 10×10×10 fixture and why it is not a schema change; the
      context-summary snapshot; the allocation test binary; the Criterion `context` benchmark and the
      `rustContext` same-class baseline; the `< 100 ms`/`< 20 ms`/memory budgets; the nightly 500k stress.
- [x] **Documentation, parity, and closeout plan.** `solver-architecture.md`, `physics/energies.md`,
      `performance/index.md`, `troubleshooting/errors.md`, `testing-and-parity.md`, `msl-format.md`; the new
      parity row; the closeout evidence and audit sequence; the note that the status dashboard is
      step-agent-owned.

## Known limitations and gated decisions

- **OD-1 (gated, Step 14 start gate + future format revision).** The v1 cluster link does not persist the
  member's relative cell offset and the payload has no fractional P1 coordinates. Step 14 derives the offset only
  from a unique centre→member pair image and rejects zero or multiple images. A writer-valid cross-image cluster
  is therefore rejected. The permanent fix is a future `.msl` payload field and needs its own decision,
  amendment, fixtures, and version handling. Recorded in the step page §6.4, the data contract §3.3, and
  `05-open-decisions.md` §3.
- **OD-2 (gated, Step 14 start gate).** The original applies its uniform 0.5 factor to the site and cluster
  contributions as well as to pairs. Step 14 initializes the exact total (site once, pair ×0.5, cluster once at
  the centre), consistent with `physics/energies.md`, and Step 15 continues with that decomposition. The
  deviation is recorded with a hand-computed test and assigned to Step 29 parity.
- **API-freeze guard.** The context API is frozen at feature 3; if the real-package acceptance cannot pass
  without changing it, the step agent stops and reports before feature 5.
- **Deferred by design.** No `OPT_LINK_ONLY_MOBILES`, no static S2 correction, no fast-exp, no parallel
  initialization; each is a Step 15/29 optimisation with a measurement, not a Step 14 omission.

## Dependency and interface checks

- [x] Step 13 is `Done`, merged (`2613a0dcbc3ddbfe4bb5b54f47821d3ffa951789`) and tagged `step-13`, and the
      runtime types, fixture set, exit-code model, and reader contract that Step 14 consumes are stable. Verify
      the `JobPackage`/`ParameterizedModel`/`EnvironmentModel`/`EnergyModel`/`TransitionModel` fields the design
      names, the `DirectionPool`/`JumpStatus` containers, and the real fixtures and their summaries.
      — *Reviewed. The fields exist and match the design; `DirectionPool` uses `(PositionId, ParticleId)` with
      `PositionId` = P1 (F-1), and `JumpLink.link_id` is described as the receiver’s environment link (F-2). The
      populated/centered fixtures and summaries are present.*
- [x] Steps 11/12 `.msl` v1 schema/payload contract is unchanged by Step 14 (no field, version, manifest,
      signature, or fixture change). Verify the design adds only runtime expansion and clarifications.
      — *Reviewed. The freeze changes only planning/development/physics pages; `contracts/manifest.json`, payload
      versions, signatures, and fixtures are untouched. The new 10×10×10 package is a test artifact.*
- [x] Step 15 can compute S0/S1/S2, pair/cluster deltas, the field term, state application, and the incremental
      lattice energy from the Step 14 interface, with the corrected cluster accounting; Step 15's page is
      consistent with the Step 14 freeze.
      — *Reviewed. The energy vectors, cluster vectors/codes, links, field scale, and `kT` conversion are exposed;
      the exact total matches AGENTS §10 and `energies.md`. F-2 (link orientation) and F-16 (mutability) must be
      fixed before Step 15.*
- [x] Step 16 can select from the pools, resolve the selected direction's path, run the exact per-direction
      occupation precheck, and update the pools without a Step 14 change.
      — *Reviewed. `resolve_node`, `path_len`, `allowed_mask_of`, `direction_for`, and `jump_links` are defined.
      F-1 (pool member/key space) and F-8 (Step 16’s cumulative path wording) block a clean Step 16.*
- [x] Step 17 can run MMC from the same runtime (pools, interaction range, link query, energies) with no pools/
      jump status/time.
      — *Reviewed. The MMC context has empty pools/jumps and zero time; `linked(a,b)` is named but not defined as
      a helper (implement by iterating the link range).*
- [x] Step 18 can own tracker values, histograms, and the `.mcs` writer/reader from the tracker index space,
      counters, and `ResumeState`; Step 19 can map `InitOutcome`/`ContextError`.
      — *Reviewed. The index space, counters, and `ResumeState` are defined; F-7 (signature field naming) and the
      `.mcs` `flags`/`totalGoal` validation wording need clarification.*
- [x] No unresolved cross-step interface question remains. Two decisions are gated on the owner (OD-1, OD-2);
      neither changes a persisted format, and both are recorded with a fallback, a test, and an owner.
      — *Reviewed. OD-1 is fail-closed and recorded; OD-2 is the exact total. F-13 recommends a writer/build-time
      guard for OD-1; no payload revision is required now.*
- [x] Versioning and migration behaviour: no format change; `.msl` v1 current-major support unchanged; the
      cluster limitation is documented rather than silently versioned.
      — *Reviewed. No version, manifest, signature, or fixture change; the limitation is in the data contract,
      open decisions, step page, quality strategy, and the future `msl-format.md` note.*

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable
      — *Reviewed. Scope/non-goals are explicit (no energy engine, loop, trackers, `.mcs`, CLI, format, new
      crate); §17 has eleven testable acceptance criteria.*
- [x] Complexity class and split decision are recorded and justified per
      [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers)
      — *Reviewed. High on cross-step interfaces, irreversibility, and test-matrix breadth; one acceptance story;
      no split, with the owner-acceptance request and API-freeze guard recorded.*
- [x] Design decisions are complete and internally consistent
      — *Reviewed. Mostly complete; F-1/F-2/F-3 are material inconsistencies (selection key space, link
      orientation, derived-array caps), with F-4–F-8 as smaller corrections.*
- [x] Required tests, property tests, golden fixtures, and performance targets are defined
      — *Reviewed. The PR-fast/PR-medium/nightly matrix, real packages, 10×10×10 fixture, allocation proof,
      context-summary snapshot, Criterion `context` benchmark, and `rustContext` baseline are defined; F-9/F-10
      note the coverage/arithmetic gaps.*
- [x] Documentation, status, parity, and audit updates are planned
      — *Reviewed. `solver-architecture`, `physics/energies`, `performance/index`, `troubleshooting/errors`,
      `testing-and-parity`, `msl-format`, the parity row, and the closeout/audit sequence are named.*
- [x] Licensing, provenance, and dependency checks are resolved
      — *Reviewed. MIT headers, no GPL copying, spglib provenance unchanged, no new external crate; F-15 notes the
      unused `mocassin-solver` dependencies.*
- [x] Risks and mitigations are realistic
      — *Reviewed. The OD-1, OD-2, link-memory, jump-status, pool, resume, f64, API-freeze, and error risks have
      concrete mitigations; F-3/F-9/F-10 sharpen the memory/perf risks.*
- [x] Code layout and namespace plan follow the structure policy
      — *Reviewed. `src/context/` uses domain submodules, no `utils`/`helpers`, files below 800 lines, MIT headers,
      and public docs; the `context` concept is distinct from Step 15’s `energy` concept.*
- [x] No hidden scope, stale placeholders, or unowned decisions
      — *Reviewed. The non-goals and deferrals are explicit; the two gated decisions are owner-assigned; the
      step-agent-owned status dashboard and Step 18 `.mcs` boundary are stated.*

## Evidence

Files reviewed:

- `AGENTS.md` §3 (branch, roles, design review, audit, approval summary), §8 (code structure and Rust rules),
  §10 (scientific invariants), §11 (format rules), §12 (testing expectations).
- `docs/planning/reviews/README.md`; `docs/planning/reviews/step-14.md` (request); the Step 14 page; the Step 13
  page, `docs/planning/reviews/step-13.md`, and `docs/planning/audits/step-13.md`; `docs/planning/steps/step-15-energy-engine.md`
  and `step-16-kmc-engine.md`; `docs/planning/adr/0005-statistical-parity-and-rng.md`,
  `0010-canonical-messagepack-signatures.md`, `0011-immutable-package-and-results-store.md`,
  `0013-format-compatibility-and-api-versioning.md`; `02-data-contracts.md` §3.3 and §4; `01-target-architecture.md`
  §4.1 and §7.3; `03-quality-strategy.md` §9.12 and §13; `05-open-decisions.md` §3; `04-roadmap.md` §3.1;
  `docs/development/code-structure.md` §2.2; `docs/development/solver-architecture.md`; `docs/physics/energies.md`;
  `docs/performance/index.md`, `docs/troubleshooting/errors.md`, `docs/development/testing-and-parity.md`,
  `docs/reference/msl-format.md`.
- Step 13 runtime and reader code: `src/rust/crates/mocassin-types/src/runtime/{direction_pool,jump_status,lattice,model,energy,transition,job,simulation}.rs`,
  `src/rust/crates/mocassin-types/src/{ids,limits,settings}.rs`, `src/rust/crates/mocassin-msl/src/map/{model,transition}.rs`,
  `src/rust/crates/mocassin-solver/src/lib.rs`, `src/rust/crates/mocassin-rng/src/stream.rs`.
- Real fixtures: `tests/contracts/packages/step-13-populated/{README.md,package-summary-v1.json,step-13-populated.msl}`,
  `step-13-centered/`, `msl-v1/`, `amend-1-net10/`; `samples/step-13-populated/step-13-populated.mocprj`;
  `samples/jobs/ceria-temperature-sweep.mocprj`.
- .NET writer/compiler evidence for the cluster link, pair link, and supercell index: `ClusterCompilationStage.cs`,
  `PairCompilationStage.cs`, `TransitionCompilationStage.cs`, `CompiledStructureModel.cs`, `CompiledClusterTable.cs`,
  `EnergyPayloadMapper.cs`, `StructurePayloadMapper.cs`, `TransitionPayloadMapper.cs`, `EnergyPayloadDtos.cs`,
  `StructurePayloadDtos.cs`, `SupercellGenerator.cs`, `JobSettingsResolver.cs`, `KmcJobConfiguration.cs`/`StatisticsRoutines.h`
  (original), and the original `EnvironmentRoutines.c`, `JumpStatusInititialization.c`,
  `SimulationContextInitialization.c`, `StatisticsRoutines.{c,h}`, `KmcJobConfiguration.cs` in
  `/workspace/mocassin-original` (read-only reference).

Commands run (record HEAD `b64f947`; `git diff --name-only 7c7ff0d..b64f947` is exactly
`docs/planning/reviews/step-14.md`, so the design docs are the freeze at `7c7ff0d`):

- `./tools/verify.sh docs --require` → exit 0, "Closeout gate completed with required checks enabled." Zensical
  strict, DocFX, and rustdoc built without errors.
- `./tools/verify.sh policy --require` → exit 0, "Closeout gate completed with required checks enabled.";
  contracts current (26), model/parameterization/lattice projections OK (5/4/2), Space-group catalog current,
  process consistency OK, 60 tool tests OK, SPDX headers in 780 files, no secret patterns in 1,217 files,
  ApiCompat and cargo-deny OK.
- `python tools/check_doc_math.py` → "Documentation math conventions OK (179 files scanned).", exit 0.
- `git status --short` was clean before the record edit; only `docs/planning/reviews/step-14.md` is changed by
  this record.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **F-1 — The selection-pool member and key space are inconsistent: `DirectionPool.members` is `(PositionId, ParticleId)`, but `PositionId` is the canonical **P1** index (`0..P`), while the design inserts a “cell-major absolute position” and Step 16 selects an absolute environment; `slot_of`/`pool_of` are keyed `P*pc` (one cell), so two cells collide and `set_selectable` cannot address the changed environment. The memory estimate `pools × P*pc × 4` is likewise per-cell. The pool cannot represent the supercell selection the KMC loop needs.** | Step 14 §8 (“`members` are `(PositionId, ParticleId)` … cell-major absolute position”; “`slot_of` is a flat `P*pc` key map”); Step 13 `runtime/direction_pool.rs` (`members: Vec<(PositionId, ParticleId)>`); `ids.rs` (`PositionId` = “inside the unit cell (the compiled P1 position)”); Step 16 §2 (`env = pool[r / direction_count]`, then `resolve_node(selected_position_abs, …)`); original `DirectionPool_t.EnvironmentPool` is an absolute `environmentId` list. |
| **Major** | **F-2 — The jump-link/pair-delta orientation is inverted/ambiguous. `LinkArena.links(P)` is built from the payload’s outgoing `{target_position, offset, pair_id}` list and `energy[env]` sums the environment’s **outgoing** pair table. When a sender S changes, the environment whose energy changes is a receiver R, and the original applies **R’s** link/table to S (`InvokeJumpLinkDeltas` after `DistributeEnvironmentUpdate`), including R’s cluster links. The design instead says “find the **sender’s** pair links whose target is the receiver” and `link_id` is “relative to the **sender** P1 list”; Step 15 §4 then updates `energy[target]` with `pair_table[pair_id][center][new]`, which is the sender’s table (transposed) rather than the receiver’s. Unless every forward/inverse pair table is symmetric, this changes the physics; it also contradicts the Step 13 `JumpLink` doc (“relative environment-link id whose energy changes”).** | Step 14 §6.1 (`Link { target_position, offset, pair_id }`, `energy[env]` is the outgoing sum), §7.2, §9.2 (“find the sender’s pair links … sender P1 link list”), §15; Step 15 §2/§4 (`energy[target][center_particle] += pair_table[pair_id][center][new] - …`); Step 13 `runtime/jump_status.rs` doc; original `EnvironmentRoutines.c` 91–140 and 679–717; original `JumpStatusInititialization.c` 80–90 (`TryGetEnvironmentLinkId(&JUMPPATH[senderPathId]->EnvironmentLinks, searchEnvId, …)`). |
| **Major** | **F-3 — `ContextLimits` do not bound the derived cluster and jump-status allocations, so a writer-valid package can force an allocation far beyond the stated memory budget.** `max_energy_vector_bytes` covers only `A*B*C*P*pc*8`; the cluster `codes`/`code_ids`/`energy` arrays are `cluster_table_count × A*B*C` (× `pc` × 8 for energy), and the jump-status `offsets` are `P*D+1` (the cap reason mentions links and touches but not offsets). With `max_cluster_tables = 1,000,000` and `max_positions = 500,000`, the unchecked product is unbounded by the table; a valid package can OOM before the `MemoryReport` runs. The §20 500k “≤512 MiB energy vectors + links” also ignores the 16M-link/cap size (≈512 MB alone with a 28–32-byte `Link`) and the 256 MiB jump status.** | Step 14 §2 `ContextLimits` table and “All size arithmetic is checked”; §6.3 `ClusterArena` arrays; §9.2 `offsets`; §20 budget table; reader `limits.rs` (`max_cluster_tables`, `max_positions`, `max_pair_links` are independent per-field caps); AGENTS §11 (“cap allocations”). |
| **Minor** | **F-4 — The OD-1 unique-image rule is stated for “every instance and every slot”, including slot 0. Slot 0 is the centre itself; the centre’s P1 list need not contain a self-pair, so the literal rule can reject every cluster before the geometry check. Slot 0 must be `[0,0,0]` by definition and only slots 1..n use the unique centre→member image.** | Step 14 §6.4 (“For every instance and every slot, take the unique pair link from the centre’s P1 list to `slot_positions[slot]`”); §6.2 item 2 (slot 0 = centre); `samples/step-13-populated` cluster centre site 16, members at ±0.25 (same-cell). |
| **Minor** | **F-5 — `SelectionPools.pool_of: Vec<u8>` has no pool-count bound. A model may have more than 255 distinct selectable direction counts (the original’s pool count is an `int32_t`); a `u8` pool index silently wraps. Add a `max_pools` limit and a checked cast, or use `u16`/`u32`.** | Step 14 §8 (`pool_of: Vec<u8>`, `u8::MAX`); §2 `ContextLimits` has no `max_pools`; original `JumpSelectionPool_t.DirectionPoolCount` is `int32_t`; `DirectionPool.direction_count` is `u32`. |
| **Minor** | **F-6 — The `nu_max` prose says the v1 payload “does not persist” the original build-time modulus. It does persist an optional job cap: `JobSettings.max_attempt_frequency_hz` is `Option<f64>` and the reader maps it. The intended rule (`unwrap_or(max active-collection rule frequency)`) is correct; the parenthetical should say “the payload persists only the optional job cap, not the original’s derived BuildFrequency”.** | Step 14 §11.1; `mocassin-types/src/settings.rs` `JobSettings.max_attempt_frequency_hz`; `JobPayloadDtos.MaxAttemptFrequencyHz`; `JobSettingsResolverTests` (`1.0e13`); Step 16 §4/§6; original `KmcJobConfiguration.BaseFrequency` → `AttemptFrequencyModulus` (`StatisticsRoutines.h:105`). |
| **Minor** | **F-7 — Resume signature fields are ambiguous and the `.mcs` cannot supply one of them. `ResumeState.model_signature` must be the `.mcs` `metadata.modelSignature` (the Step 09 **parameterized** `ParameterizedModel.signature`, geometry **and values**), not the Step 08 `ParameterizedModel.model_signature`. The `.mcs` has no `latticeSignature`, and its `modelSignature` is the parameterized signature, not the Step 08 `model_signature` used by `LatticeSignatureV1`; Step 18/19 must either take the **initial** lattice signature from the loaded `JobPackage` (a package-binding check, not a state check) or the `.mcs` schema must carry it (a Step 18 format decision). The design should name the compared fields and the source.** | Step 14 §12 (`model_signature`, `lattice_signature`); `02-data-contracts.md` §4.3 (`modelSignature` is the parameterized model signature; no `latticeSignature`); `mocassin-types/src/runtime/model.rs` (`signature` vs `model_signature`); §4.3 `flags`/`totalGoal` also do not appear in the `ResumeState` validation list. |
| **Minor** | **F-8 — Step 16 §3’s path formula is cumulative (`path[i] = trim(path[i-1] + offset[i-1])`), but the compiled `path` entries are relative to the start: the compiler subtracts the start encoding (`relative = absolute[i] - absolute[0]`) and Step 14 §9.1 resolves `trim(cell(selected) + path[i].cell) * P + path[i].P`. Step 16 must call `resolve_node` (or use the relative-to-start rule); its own wording would double-count the cell.** | Step 14 §9.1/§9.4; Step 16 §3; `TransitionCompilationStage.cs` 529–541 (`relative = absolute.Select(encoding => absolute[0] difference)`); `CompiledJumpDirection.Path` doc (“relative node encodings; node 0 is (0,0,0,PositionId)”). |
| **Minor** | **F-9 — The `<100 ms`/`≤64 MiB` 10×10×10 budget is measured on the ceria model, which has no pair/cluster tables in `msl-v1` (Step 13 audit: both real fixtures have zero pair/cluster tables). The populated 3,000-position clone is correctness-only and is not benchmarked. The design does not weaken the criterion, but the budget does not exercise the pair/cluster/link scale; record that coverage limitation or measure the populated clone.** | Step 14 §19 (“The populated model’s link/energy scale is covered by cloning … at 3,000 positions”), §20; Step 13 audit F-1 evidence (“Both real .msl fixtures have zero pair/cluster tables”); `msl-v1/package-summary-v1.json` has `pairTableCount=0`, `clusterTableCount=0`. |
| **Minor** | **F-10 — The overall memory budget is not arithmetically consistent. At the caps a `Link` of `{u32, [i32;3], u32, u32, u32}` is 28 bytes (32 padded); 16M links alone is ≈512 MB, plus a 500k × 64 energy vector ≈256 MB and a 256 MiB jump status, so the “≤512 MiB energy vectors + links” row cannot hold. Either state the cap as per-array (as the `ContextLimits` table does) or add a total-memory cap and revise the row.** | Step 14 §2 table; §20 “Context memory total, 500,000 positions × 64 particles (limit case) — ≤ 512 MiB energy vectors + links”; `max_pair_links=16,000,000`; `max_energy_vector_bytes=536,870,912`; `max_jump_status_bytes=268,435,456`. |
| **Minor** | **F-11 — `Send` but not `Sync` is asserted for `SimulationContext`, but every listed field (`Vec`, `Arc<ParameterizedModel>`, `mocassin_rng::Rng` wrapping `ChaCha12Rng`) is `Send + Sync`; the type would be `Sync` automatically. If the single-trajectory rule is meaningful, add an explicit `!Sync` marker (`PhantomData<Cell<()>>`) or drop the claim.** | Step 14 §1 (“`Send` but not `Sync`”); `mocassin-rng/src/stream.rs` (`Rng { inner: ChaCha12Rng }`); `mocassin-types` fields. |
| **Minor** | **F-12 — The goal arithmetic drops the original’s zero-total error. The original returns `ERR_DATACONSISTENCY` when `TotalSimulationGoalMcsCount == 0` and then uses `total / 100` (which is ≥1 after round-up); Step 14 uses `max(1, total/100)` and has no `total == 0` error. A valid-looking `target_mcsp = 0` / `pre_run = 0` KMC job would initialize instead of failing.** | Step 14 §10; original `StatisticsRoutines.c` `SetCycleCounterStateToDefault` (lines 25–43: `mobileCount == 0` → `ERR_NOMOBILES`, `TotalSimulationGoalMcsCount == 0` → `ERR_DATACONSISTENCY`, round-up, `/CYCLE_BLOCKCOUNT`); Step 13 `JobSettings.target_mcsp`/`pre_run_mcsp` are `u64`. |
| **Note** | **F-13 — OD-1 is sound for v1, but the writer can emit a package the solver rejects.** The fail-closed unique-image rule, the code-existence cross-check, the data-contract/open-decision/step-page records, and the future-format note are adequate; no payload revision is required now (Step 14 explicitly has no format change). Add a follow-up: a Step 11/12 build-time check or warning for a cross-image cluster, or at least a `msl-format.md`/`troubleshooting` note, so the failure is not discovered only at solver start. | Step 14 §6.4/§23; `02-data-contracts.md` §3.3; `05-open-decisions.md` OD-1; `ClusterCompilationStage.cs` (member offsets known to the compiler but dropped by the writer); `EnergyPayloadMapper`/`StructurePayloadMapper`; Step 13 audit F-1 fixture evidence. |
| **Note** | **F-14 — `LimitExceeded` maps to exit 12, while the architecture’s 14 is “aborted by a configured limit”. The rationale (a package the job cannot represent, detected before the run) is defensible, but state the distinction explicitly so a future reader does not reconcile it to 14.** | Step 14 §2/§13; `01-target-architecture.md` §7.3 (12 model/data, 14 aborted by a configured limit); `docs/reference/cli.md`. |
| **Note** | **F-15 — `mocassin-solver/Cargo.toml` currently depends on `mocassin-msl`, `mocassin-state`, `mocassin-extension`, and `rayon`; the design says Step 14 uses `mocassin-types`/`mocassin-rng`/`thiserror`/`tracing`, `mocassin-msl` dev-only, and `rayon` unused, and no `mocassin-state` dependency. Move `mocassin-msl` to dev-dependencies and remove/declare the unused crates in feature 1; do not leave unused dependencies after Step 13 audit N-2.** | `src/rust/crates/mocassin-solver/Cargo.toml`; `src/rust/crates/mocassin-solver/src/lib.rs` (placeholder); Step 14 §1/§22; Step 13 audit N-2. |
| **Note** | **F-16 — Step 15’s `EnergyEngine<'a> { ctx: &'a SimulationContext }` with `apply_state_change(&mut self)` cannot mutate the immutable context. Step 14’s context fields are `pub` with no interior mutability, so Step 15 needs `&mut SimulationContext` (or Step 14 must expose an explicit mutator); reconcile the Step 15 API wording in its own design.** | Step 14 §1 (no interior mutability); Step 15 §1/§6; Step 14 §15 (`EnergyEngine` consumer). |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Changes required — round 2 at record `e844a13` (frozen design `b6dc503`) closes F-1, F-2, and F-3 in substance and closes F-4–F-16 in the Step 14 freeze; it adds F-17 (Major, cluster instance per table) and the minor cross-step corrections F-18–F-22 plus notes F-23–F-28. The request owner set `Re-review required` after round 1; the reviewer-authored round-1 text, findings, and verdict remain unchanged as history.

The request owner set `Re-review required` after applying the round-1 resolutions; the round-2 reviewer set the
current state. The reviewer-authored round-1 text below is unchanged.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

Follow-up owner: Planner and step agent (design correction); the human owner approves the re-review and start gate.
Follow-up step: Step 14 — re-freeze the corrected design and append a fresh review round.

## Approval summary

The request owner (step agent or planner) completes this section before the orchestrator presents the approval
request. It must cover the full history, not only the latest round. This section is prepared for the Step 14
start gate; it is not a merge/tag/waiver request and makes no claim about round 2.

- [x] Requested decision and frozen commit recorded — re-review of the corrected design at
      `b6dc50361e749954480efa1d1559d987c10b4142`; the start gate remains the owner's decision
- [x] Every review/audit round recorded (round, date, commit, reviewer, verdict) — round 1 recorded; round 2 is
      pending a fresh reviewer and is listed as pending, not omitted
- [x] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status) — the reviewer's
      table above and the resolutions below cover F-1…F-16
- [x] Accepted notes and waivers recorded with rationale — OD-1 and OD-2 accepted 2026-11-20 (see below)
- [ ] Residual risks, deferred items, and follow-up owners recorded — the step page §21/§23 and OD-1 carry them;
      the final list is completed at the start gate
- [ ] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived — pending round 2
- [ ] Final CI and documentation-build results recorded — round-2 `docs`/`policy` results are recorded in the
      resolution section; the closeout CI belongs to the step, not this gate

Round history:

| Round | Date | Commit | Reviewer | Verdict |
| --- | --- | --- | --- | --- |
| 1 | 2026-11-20 | `7c7ff0d` | DeepSeek V4.1 Flash 1017 (fresh planning auditor) | Changes required |
| 2 | pending | `b6dc503` | pending fresh planning auditor | pending |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| F-1 | Major | Pool member/key space could not represent the supercell selection | `SelectionPool`/`PoolMember { env: EnvId, particle }`, global `slot` map, per-P1 `pool_of`, Step 16 aligned | `b6dc503` | Addressed |
| F-2 | Major | Jump/pair delta orientation was sender-relative/transposed | Owner-major link CSR + inverse index; receiver-major jump links; Step 15 §4 rewritten; asymmetric-table test | `b6dc503` | Addressed |
| F-3 | Major | Unbounded derived cluster/jump-status allocations | `MemoryPlan` before allocation, per-array formulas/caps, `max_context_bytes`, fail-closed `LimitExceeded` | `b6dc503` | Addressed |
| F-4 | Minor | Slot 0 subjected to the unique-image rule | Slot 0 = `[0,0,0]` by definition; rule applies to slots `1..n` | `b6dc503` | Addressed |
| F-5 | Minor | `pool_of: Vec<u8>` could wrap | `u32` + `max_pools` checked in the plan | `b6dc503` | Addressed |
| F-6 | Minor | `nu_max` prose denied the persisted optional cap | Prose corrected to "job cap persisted, derived modulus not" | `b6dc503` | Addressed |
| F-7 | Minor | Resume signature fields ambiguous; `.mcs` has no lattice signature | Parameterized `modelSignature` + `seedHash`; lattice signature removed; goal/flags non-validation stated | `b6dc503` | Addressed |
| F-8 | Minor | Step 16 path formula was cumulative | Step 16 uses relative-to-start `resolve_node` | `b6dc503` | Addressed |
| F-9 | Minor | 10×10×10 budget measured on a table-free model | Populated 10×10×10 clone added as the recorded pair/cluster measurement; limitation stated | `b6dc503` | Addressed |
| F-10 | Minor | 500k memory row inconsistent with the caps | Replaced by the `max_context_bytes` row with a passing example and the per-array caps | `b6dc503` | Addressed |
| F-11 | Minor | `Send`-not-`Sync` was not enforced | Explicit `PhantomData<Cell<()>>` marker + `compile_fail` doctest | `b6dc503` | Addressed |
| F-12 | Minor | Zero-total goal dropped; `max(1, total/100)` | `total == 0` is exit 12; `total/100` after round-up | `b6dc503` | Addressed |
| F-13 | Note | A writer-valid cross-image cluster is rejected only at solver start | Named follow-up in OD-1 (owner planner, writer build-time diagnostic) + docs notes | `b6dc503` | Addressed (follow-up recorded) |
| F-14 | Note | `LimitExceeded` 12 vs architecture 14 | Explicit 12-vs-14 distinction in the step page, architecture §7.3, and quality strategy | `b6dc503` | Addressed |
| F-15 | Note | Unused solver dependencies | Feature 1 makes `mocassin-msl` dev-only and removes `mocassin-state`/`mocassin-extension`/`rayon` | `b6dc503` | Addressed (implementation task) |
| F-16 | Note | `EnergyEngine` held `&SimulationContext` but mutated | Step 15 API uses `&mut SimulationContext` and `&mut self` | `b6dc503` | Addressed |

Accepted notes and waivers:

- **OD-1 accepted (2026-11-20, owner):** the v1 cluster-link limitation stands, with slot 0 = `[0,0,0]`, the
  unique centre→member link rule for slots `1..n`, fail-closed exit 12, and a named writer follow-up. No payload
  revision now.
- **OD-2 accepted (2026-11-20, owner):** the exact accounting (site once, pair ×0.5, cluster once at the centre)
  stands; AGENTS §10 and `physics/energies.md` agree; Step 29 parity covers the deviation.
- Both acceptances are recorded in `05-open-decisions.md` §2 and in the step page §§6.4 and 7.4.

## Human approval

- [ ] Accepted: Ready for implementation
- [ ] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by:
Date:

## Resolution status

| Finding | Status | Owner | What changed | Commit |
| --- | --- | --- | --- | --- |
| F-1 | Resolved | Planner | Step 14 §8/§15: `SelectionPool`/`PoolMember { env: EnvId, particle }`, one global `slot` map over `env*pc+particle`, `pool_of` over `P*pc`, `max_pools`; Step 16 §2 reads `member.env`; Step 13 page notes the placeholder retirement | `b6dc503` |
| F-2 | Resolved | Planner | Step 14 §6.1/§7.2/§9.2/§9.3: owner-major forward CSR + inverse `IncomingLink` index, owner's table/row for deltas, receiver-major `JumpLink { sender_path_id, receiver_path_id, link_id }`, link-carried cluster associations, no reverse-link synthesis; Step 15 §2–§6 rewritten; `02-data-contracts` §3.3 and `physics/energies.md` state the orientation | `b6dc503` |
| F-3 | Resolved | Planner | Step 14 §2: phase-0 `MemoryPlan` with per-array formulas (cluster `K*C`, cluster energy `C*Σ mapping_len`, jump `P*D+1`), new caps (`max_cluster_instances`, `max_cluster_state_bytes`, `max_cluster_energy_bytes`, `max_link_bytes`, `max_pool_bytes`, `max_status_count`, `max_jump_links`, `max_context_bytes`), fail-closed `LimitExceeded` | `b6dc503` |
| F-4 | Resolved | Planner | Step 14 §6.2/§6.4: slot 0 = centre and `[0,0,0]`; the unique-link rule covers slots `1..n` | `b6dc503` |
| F-5 | Resolved | Planner | Step 14 §8: `pool_of: Vec<u32>` + `max_pools` (65,536) checked in the plan | `b6dc503` |
| F-6 | Resolved | Planner | Step 14 §11.1: the payload persists the optional job cap, not the derived build-time modulus | `b6dc503` |
| F-7 | Resolved | Planner | Step 14 §12: `model_signature` = parameterized `.mcs` signature, `seed_hash` added, `lattice_signature` removed, `totalGoal`/`flags` explicitly not re-validated | `b6dc503` |
| F-8 | Resolved | Planner | Step 16 §3: relative-to-start `resolve_node`; cumulative formula removed | `b6dc503` |
| F-9 | Resolved | Planner | Step 14 §19/§20 and quality strategy §13: populated 10×10×10 clone is the recorded pair/cluster measurement; the table-free fixture limitation is stated | `b6dc503` |
| F-10 | Resolved | Planner | Step 14 §2/§20: per-array caps + 1 GiB total with a passing 500k example; the impossible 512 MiB row removed | `b6dc503` |
| F-11 | Resolved | Planner | Step 14 §1: `_not_sync: PhantomData<Cell<()>>` and a `compile_fail` doctest | `b6dc503` |
| F-12 | Resolved | Planner | Step 14 §10: zero total is exit 12; `total/100` after the round-up | `b6dc503` |
| F-13 | Resolved (follow-up recorded) | Planner | Step 14 §6.4/§21; `05-open-decisions.md` §2 OD-1: named writer build-time diagnostic, owner planner, plus the `msl-format.md`/`troubleshooting` notes in feature 6 | `b6dc503` |
| F-14 | Resolved | Planner | Step 14 §13; `01-target-architecture.md` §7.3; quality strategy §9.12: exit 12 = context cannot be represented, exit 14 = run-time abort | `b6dc503` |
| F-15 | Resolved (implementation task) | Planner | Step 14 §22; `solver-architecture.md` crate graph: `mocassin-msl` dev-only, `mocassin-state`/`mocassin-extension`/`rayon` removed in feature 1 and restored in Steps 18/19/29 | `b6dc503` |
| F-16 | Resolved | Planner | Step 15 §1: `EnergyEngine<'a> { ctx: &'a mut SimulationContext }` and `&mut self` methods; `EnvId` accessors | `b6dc503` |

**Major redesign summary.**

- **F-1 (selection space).** A pool member is now an absolute supercell environment (`PoolMember { env: EnvId,
  particle: ParticleId }`); the direction count and mask stay P1-keyed, so `pool_of` is a small `P*pc` table and
  the O(1) member slot map is one global `E*pc` array. Two cells never collide, `set_selectable(env, particle)`
  addresses the changed environment, and the plan bounds `M*8 + E*pc*4 + P*pc*4`. Step 16 selects
  `member.env` with the pool weight `members.len() * direction_count`.
- **F-2 (orientation).** The static vector is owner-major: `E(owner,p) = site + Σ table[pair_id][p][partner]`.
  A second compact CSR (partner P1 → owners) answers "who changes"; a change at `V` updates each owner's vector
  with the owner's table and row. Jump links are receiver-major (`sender_path_id`, `receiver_path_id`,
  `link_id` into the receiver's list) and carry the receiver's cluster associations, so Step 15/17 never apply a
  sender's table. A missing reverse link is legal and is not synthesised. The tests use an asymmetric pair table
  and a one-directional link so a transposed implementation fails.
- **F-3 (bounds).** A phase-0 `MemoryPlan` computes every array from the package counts with checked `u64`
  arithmetic before any `Vec` exists: `K*C` codes/code-ids, `C*Σ mapping_len` cluster energy,
  `(P*D+1)` jump offsets (with `max_status_count`), the counted jump links (`max_jump_links`), the inverse link
  index, the pool slot map, and the cluster geometry. Per-array caps plus `max_context_bytes` (1 GiB) reject
  with `LimitExceeded` (exit 12); `MemoryReport` must equal the plan.

**Gate results for the corrected freeze.**

- `./tools/verify.sh docs --require` → exit 0; Zensical strict (182 link-scanned files), math check (179 files),
  DocFX (0 warnings), rustdoc clean.
- `./tools/verify.sh policy --require` → exit 0; contracts current (26), projections 5/4/2, Space-group catalog
  current, process consistency OK, 60 tool tests OK, SPDX headers 780 files, secret scan 1,217 files, ApiCompat
  and cargo-deny OK.
- `git status --short` clean before the record edit; this record commit touches only
  `docs/planning/reviews/step-14.md`.

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

I am a fresh, independent planning auditor. I did not author or implement the Step 14 design and have no shared
conversation with the planner, the implementors, or the orchestrator. I received only the frozen commit
`7c7ff0de83a4ae4c5f9849650e0601c7238494b8`, the request record at `b64f947` on `docs/step-14-design`, the
repository, and the required commands/scope; no desired verdict or summary was supplied. I read the repository
and the Step 13 code/fixtures myself rather than relying on the request. The review was read-only apart from this
record: I edited and committed only `docs/planning/reviews/step-14.md` and did not merge, tag, push `main`,
delete branches, change code/tests/configuration/design docs, or fix a finding.

Frozen state: the reviewed commit is `7c7ff0de83a4ae4c5f9849650e0601c7238494b8` (the design freeze). The record
HEAD `b64f947` is the documentation-only request commit; `git diff --name-only 7c7ff0d..b64f947` is exactly
`docs/planning/reviews/step-14.md`, so no design, code, test, configuration, contract, or generated artifact moved
between the freeze and this record. The working tree was clean before the record edit. The frozen commit includes
the step page, `01-target-architecture.md` §4.1, `02-data-contracts.md` §3.3, `03-quality-strategy.md` §9.12/§13,
`04-roadmap.md` §3.1, `05-open-decisions.md` OD-1/OD-2/class row, the parity row, `code-structure.md` §2.2,
`solver-architecture.md`, `physics/energies.md`, and the Step 15 reconciliation; no code or fixture exists yet.

#### Commands and results

- `./tools/verify.sh docs --require` → exit 0; Zensical strict, DocFX, and rustdoc clean.
- `./tools/verify.sh policy --require` → exit 0; contracts 26 current, projections 5/4/2 OK, catalog current,
  process consistency OK, 60 tool tests OK, SPDX 780 files, 1,217 secret-scanned, ApiCompat and cargo-deny OK.
- `python tools/check_doc_math.py` → “Documentation math conventions OK (179 files scanned).”, exit 0.
- `git diff --name-only 7c7ff0d..b64f947` → only `docs/planning/reviews/step-14.md`; `git status --short` clean
  before the record edit.
- Inspected the Step 13 runtime/reader code and the .NET writer/compiler paths (see Evidence) and the original
  `EnvironmentRoutines.c`, `JumpStatusInititialization.c`, `SimulationContextInitialization.c`,
  `StatisticsRoutines.{c,h}`, and `KmcJobConfiguration.cs` as a read-only reference.

#### Findings

**Major (3):**

- **F-1** — The selection-pool member type/key space is inconsistent (`PositionId` is P1, the design needs an
  absolute environment; `slot_of`/`pool_of` keyed `P*pc` collide across cells; `set_selectable` cannot address
  the changed environment). The Step 13 `DirectionPool` cannot express the supercell selection Step 16 needs.
- **F-2** — The jump-link/pair-delta orientation is inverted: `links(P)` is the outgoing link set and
  `energy[env]` sums it, but a sender change must update a **receiver’s** link/table (as the original does), not
  the sender’s; the `JumpLink.link_id` “sender P1 list” and Step 15 §4 `pair_table[pair_id][center][new]` are the
  transposed table unless all pair tables are symmetric. This is a physics-affecting ambiguity.
- **F-3** — `ContextLimits` bound the site+pair energy and links but not the cluster codes/code_ids/energy or
  the jump-status offsets; `max_cluster_tables × max_positions` is unbounded, so a writer-valid package can OOM.
  The §20 500k memory row is also arithmetically inconsistent at the caps.

**Minor (9):** F-4 slot 0 must be `[0,0,0]` (the unique-image rule is stated for every slot); F-5 `pool_of: Vec<u8>`
has no pool-count bound; F-6 the `nu_max` prose says the optional job cap is not persisted, but
`JobSettings.max_attempt_frequency_hz` is; F-7 resume `model_signature` must be the parameterized signature and
the `.mcs` has no `latticeSignature` field; F-8 Step 16’s cumulative path formula contradicts the relative-to-start
`path` encodings and Step 14’s `resolve_node`; F-9 the 10×10×10 `<100 ms`/`≤64 MiB` budget is measured on the
ceria model with no pair/cluster tables; F-10 the 500k memory budget is inconsistent with the per-array caps;
F-11 `Send`-but-not-`Sync` is not enforced by the listed fields; F-12 the zero-total goal error is dropped and
`max(1, total/100)` replaces the original `total/100` after round-up.

**Notes (4):** F-13 OD-1 is sound for v1 (fail-closed, recorded, no format change), but add a writer/build-time
guard or follow-up; F-14 `LimitExceeded` → exit 12 vs the architecture’s 14 needs an explicit distinction;
F-15 `mocassin-solver` still declares `mocassin-state`/`mocassin-extension`/`rayon` and a normal `mocassin-msl`
dependency; F-16 Step 15’s `EnergyEngine` holds `&SimulationContext` but mutates it.

**Positives verified:** `K_fix = exp(E_norm/kT)` matches the original `1/exp(-E/kT)`; `K_auto`/`K_norm`/`dt`
match AGENTS §10 and `StatisticsRoutines.h`; the site-once/pair-×0.5/cluster-at-centre total matches AGENTS §10
and `physics/energies.md` (OD-2 is documented and defensible); D-5’s structural receiver filter and the derived
`L*(L-1)` bound are sound; the cluster `byte_position` = own slot is confirmed against `CompiledClusterLink` and
`ClusterCompilationStage`; the `.msl` v1 contract, manifest, signatures, and fixtures are unchanged; the new
10×10×10 fixture is a test artifact, not a schema change; licensing (MIT headers, no GPL copying) is clean; the
High class/no-split case and the owner-acceptance request are recorded; the design is otherwise detailed,
testable, and traceable.

#### Verdict

**Changes required.** The design is substantially complete and the two gated decisions are correctly identified
and recorded, but F-1, F-2, and F-3 are material implementability/scientific/trust-boundary contradictions that
must be corrected before implementation. No Blocker was found. After the design docs are corrected and re-frozen,
a fresh review round is required.

### Re-review round 2

#### Independence and frozen state

I am a fresh, independent planning auditor. I did not author or implement the Step 14 design and have no shared
conversation with the planner, the implementors, the round-1 reviewer, or the orchestrator. I received only the
frozen commit `b6dc50361e749954480efa1d1559d987c10b4142`, the record at `e844a13`, the repository, and the
required commands/scope; no desired verdict or summary was supplied. I read the repository, the Step 13 runtime
and reader code and fixtures, the .NET writer/compiler, and the original solver as a read-only reference rather
than relying on the request. The review was read-only apart from this record: I edited and committed only
`docs/planning/reviews/step-14.md` and did not merge, tag, push `main`, delete branches, change
code/tests/configuration/design docs, or fix a finding.

Frozen state: the reviewed commit is `b6dc50361e749954480efa1d1559d987c10b4142` (the design freeze after round 1).
The record HEAD `e844a13` is the documentation-only re-review request commit; `git diff --name-only
b6dc503..e844a13` is exactly `docs/planning/reviews/step-14.md`, so no design, code, test, configuration,
contract, or generated artifact moved between the freeze and this record. The working tree was clean before the
record edit. The freeze includes the corrected Step 14 page, the Step 13 container reconciliation, the Step
15/16 interface reconciliation, and the architecture/data-contract/quality/roadmap/open-decision/parity/
code-structure/solver-architecture/physics updates; no code or fixture exists yet.

#### Commands and results

- `./tools/verify.sh docs --require` → exit 0; Zensical strict, DocFX, and rustdoc clean; `Documentation built
  in .../site`; `Closeout gate completed with required checks enabled.`
- `./tools/verify.sh policy --require` → exit 0; contracts current (26), model/parameterization/lattice
  projections OK (5/4/2), Space-group catalog current (274 settings, sha256
  `1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395`), process consistency OK, 60 tool tests OK,
  SPDX headers in 780 files, no secret patterns in 1,217 files, ApiCompat found no breaking changes (the
  unresolved `MessagePack`/`Microsoft.Data.Sqlite` reference messages are non-fatal), cargo-deny
  advisories/bans/licenses/sources OK.
- `python tools/check_doc_math.py` → `Documentation math conventions OK (179 files scanned).`, exit 0.
- `git diff --name-only b6dc503..e844a13` → only `docs/planning/reviews/step-14.md`; `git status --short`
  clean before the record edit.
- Inspected the Step 13 runtime/reader (`runtime/{direction_pool,jump_status,model,energy,transition}.rs`,
  `limits.rs`, `settings.rs`, `mocassin-msl/src/map/{model,transition}.rs`), the .NET writer/compiler
  (`ClusterCompilationStage.cs`, `PairCompilationStage.cs`, `TransitionCompilationStage.cs`,
  `CompiledStructureModel.cs`, `EnergyPayloadMapper.cs`), the original `EnvironmentRoutines.c`,
  `JumpStatusInititialization.c`, `SimulationContextInitialization.c`, `StatisticsRoutines.{c,h}`, the
  populated/centered fixtures and payloads, and `samples/jobs/ceria-temperature-sweep.mocprj`.

#### Findings

**Round-1 finding closure:**

| Finding | Round-1 severity | Closure | Evidence |
| --- | --- | --- | --- |
| F-1 | Major | **Closed.** The pool is now supercell-addressed: `SelectionPool`/`PoolMember { env: EnvId, particle }`, one global `slot` over `E*pc`, `pool_of` over `P*pc`, `set_selectable(env, particle)`, `selectable_jump_count` over `Σ members.len()*direction_count`; Step 16 selects `member.env`. Two cells are distinct members and the slot key cannot collide. The Step 13 `DirectionPool` placeholder is retired (no reader constructed it). | Step 14 §8 and §15; Step 16 §2; `src/rust/crates/mocassin-types/src/runtime/direction_pool.rs`; `ids.rs` (`PositionId` = P1); grep over `src/rust` shows only the re-exports. |
| F-2 | Major | **Closed.** The static vector is owner-major: `E(owner,p) = site + Σ pair_table[pair_id][p][partner]`; a second CSR `inverse(partner P1) -> { owner_position, link_id }` finds the owners; a change at `V` updates each owner's vector with the owner's table and row. `JumpLink { sender_path_id, receiver_path_id, link_id }` is receiver-major; `link_id` indexes the receiver's forward list and the receiver's cluster associations travel with it. A missing reverse link is not synthesised. Step 15 §2–§6 is rewritten; the asymmetric-table/one-directional-link tests and the D-10 multi-member cluster rebuild are present. | Step 14 §6.1, §7.2, §9.2, §9.3; Step 15 §2–§6; original `EnvironmentRoutines.c` `DistributeEnvironmentUpdate`/`InvokeJumpLinkDeltas`; `physics/energies.md`; `02-data-contracts.md` §3.3. |
| F-3 | Major | **Closed.** Phase-0 `MemoryPlan` computes every array with checked `u64` arithmetic before allocation; the table covers the lattice, energy/site, forward/inverse links, cluster association, cluster instances/geometry/codes/code-ids/energy, pools, and jump offsets/links; per-array caps plus a 1 GiB `max_context_bytes`; `LimitExceeded` is exit 12; the 500k example is arithmetically consistent and the impossible 512 MiB row is gone; `plan == report` is required. | Step 14 §2, §13, §17, §20; `docs/development/solver-architecture.md` memory section; `docs/planning/03-quality-strategy.md` §9.12. The capacity-definition note is F-23. |
| F-4 | Minor | **Closed for the slot-0 rule.** Slot 0 is the centre and `[0,0,0]` by definition; the unique centre→member image rule applies only to slots `1..n`; `ClusterGeometry` is exit 12. The multi-position-site table issue is the new F-17. | Step 14 §6.2, §6.4; `step-13-populated` payload: position 1 has slot 0, positions 0/2 have slots 1/2. |
| F-5 | Minor | **Closed.** `pool_of: Vec<u32>` plus `max_pools` (65,536) checked in the plan; no silent `u8` wrap. The `pool_of` code-block key comment still says `P*pc + particle` where the prose and table say `P1*pc + particle` — a documentation nit (F-28). | Step 14 §8, §2; `pool_of` key prose. |
| F-6 | Minor | **Closed in Step 14.** §11.1 now says the payload persists only the optional job cap, not the derived build-time `AttemptFrequencyModulus`/`BaseFrequency`. The Step 16 §6 wording is stale (F-18). | Step 14 §11.1; `mocassin-types/src/settings.rs`; `JobPayloadDtos`. |
| F-7 | Minor | **Closed in Step 14.** `ResumeState.model_signature` is the parameterized `.mcs` `modelSignature`; `seed_hash` is the `.mcs` `seedHash`; no lattice signature is required; `totalGoal`/`flags`/`abortReason` are explicitly not re-validated. The Step 16 §9 wording is stale (F-18). | Step 14 §12; `02-data-contracts.md` §4.3; `mocassin-types/src/runtime/model.rs` (`signature` vs `model_signature`). |
| F-8 | Minor | **Closed.** Step 16 §3 now uses the relative-to-start encodings and `ctx.resolve_node(selected, direction, i)`; the cumulative formula is removed. The compiler subtracts the start encoding (`TransitionCompilationStage.cs` 535–541). | Step 14 §9.1/§9.4; Step 16 §3; `TransitionCompilationStage.cs`. |
| F-9 | Minor | **Closed.** The real-writer 10×10×10 fixture and the populated-model 10×10×10 clone are both budgeted; the ceria fixture is explicitly table-free and the clone is the pair/cluster-scale measurement; the limitation is recorded in §19/§20 and the quality strategy. | Step 14 §19/§20; `03-quality-strategy.md` §13; `msl-v1/package-summary-v1.json` (`pairTableCount=0`, `clusterTableCount=0`). |
| F-10 | Minor | **Closed.** The impossible 512 MiB row is replaced by the per-array caps and the 1 GiB total; the 500k example totals about 660 MiB and fits; the 16M-link forward/inverse size is stated. | Step 14 §2 and §20. |
| F-11 | Minor | **Closed.** `_not_sync: PhantomData<Cell<()>>` makes the context `!Sync` while preserving `Send`; a `compile_fail` doctest and a positive `Send` assertion are required. | Step 14 §1 and §17. |
| F-12 | Minor | **Closed in Step 14.** `goals.total == 0` is `InvalidInput` exit 12; `cycles_per_block = total/100` after the round-up; the `max(1, ...)` is removed. The Step 16 §8 wording is stale (F-18). | Step 14 §10; original `StatisticsRoutines.c` `SetCycleCounterStateToDefault`. |
| F-13 | Note | **Closed (follow-up recorded).** OD-1 names the writer build-time diagnostic, owner, trigger, and the `msl-format.md`/`troubleshooting/errors.md` notes in feature 6; no payload revision now. | Step 14 §6.4/§21; `05-open-decisions.md` OD-1; `msl-format.md`/`troubleshooting/errors.md` plan in §18. |
| F-14 | Note | **Closed.** Exit 12 is the context-cannot-be-represented/data error; exit 14 is the run-time abort; the distinction is explicit in Step 14 §2/§13, `01-target-architecture.md` §7.3, and the quality strategy. | Step 14 §13; architecture §7.3; `03-quality-strategy.md` §9.12. |
| F-15 | Note | **Closed as an implementation task.** `mocassin-solver/Cargo.toml` still declares `mocassin-msl` (normal), `mocassin-state`, `mocassin-extension`, and `rayon`; feature 1 makes `mocassin-msl` dev-only and removes the others, and the target graph is recorded in `solver-architecture.md`. No code change is part of the freeze. | `src/rust/crates/mocassin-solver/Cargo.toml`; Step 14 §22; `solver-architecture.md`. |
| F-16 | Note | **Closed for Step 15.** `EnergyEngine<'a> { ctx: &'a mut SimulationContext }` with `&mut self` methods; `environment_energy`/`lattice_energy_ev` take `&self`; `EnvId` is used at the boundary. The Step 16 `KmcEngine` composition is the new F-19. | Step 15 §1; Step 14 §15. |

**New findings:**

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **F-17 — The cluster runtime is one instance per table, but the .NET writer links the same cluster table to every P1 position owned by the centre site. A writer-valid package with a cluster on a multi-position site is therefore rejected or cannot represent the per-centre codes/energies, and the design records no v1 limitation for it.** | `src/dotnet/Mocassin.Compilation/Energy/ClusterCompilationStage.cs` 238–241: `foreach (var p in context.Structure.Sites[centerSite.Index].PositionIndices) links[p].Add(new CompiledClusterLink(tableIndex, 0));` — slot 0 is added to every position of the centre site. `tests/contracts/packages/step-13-centered/payloads/model-1.bin` decodes to 8 positions, 2 sites, `site_index` `[0,1,1,0,0,1,1,0]`; site 0 owns 4 positions (this fixture has `clusterTableCount=0`). `step-13-populated` has 3 sites/3 positions, so its one cluster (centre position 1, members 0/2) does not exercise the case. Step 14 §6.2 items 2/5 require slot 0 on exactly one P1 position and the multiset of `byte_position` to be `0..=n`; §6.3/§2 define `instance_of_table[table] -> instance` one-to-one, `K` = one instance per referenced table, and `codes`/`energy` per instance/cell. Either support multiple centres (one `ClusterInstance` per slot-0 link; `instance_of_table` becomes a `T+1`/`K` CSR; `K` is the slot-0 link count; per-instance `slot_start`/`codes`/`energy`; update the plan table and tests) or record an explicit v1 limitation (OD-1/step page) with a writer build-time diagnostic and a fixture/test; `step-13-centered` is a suitable base. |
| **Minor** | **F-18 — The Step 16 cross-step text was not updated after F-6, F-7, and F-12.** | Step 16 §6: `nu_max` is still described as `AttemptFrequencyModulus`, which Step 14 §11.1 says the payload does not persist (F-6); §8 still uses `max(1, total_goal / 100)` where Step 14 §10 removes the `max` and makes zero total exit 12 (F-12); §9 still says to validate the "model/lattice signature" although Step 14 §12 removes the lattice signature (F-7). Fix the Step 16 page or add a reconciliation note. |
| **Minor** | **F-19 — Step 16's `KmcEngine<'a>` would need two simultaneous mutable borrows of the context.** | Step 15 §1: `pub struct EnergyEngine<'a> { ctx: &'a mut SimulationContext }`; Step 16 §1: `pub struct KmcEngine<'a> { ctx: &'a mut SimulationContext, energy: EnergyEngine<'a>, ... }`. `EnergyEngine<'a>` already borrows the context mutably, so the struct cannot also hold `ctx`. The F-16 fix is correct for Step 15 but was not reconciled with Step 16. Hold only the engine (and expose the context through it) or hold `ctx` and create a short-lived `EnergyEngine<'_>` per call; update Step 16 §1/§5. |
| **Minor** | **F-20 — `nu_max` exit code is inconsistent between §11.1 and §13/quality strategy.** | Step 14 §11.1: "`nu_max > 0` and finite, else exit 12"; §13: `NonFiniteFactor { what }` → 13 "`kT`, `K_fix`, `K_norm`, `nu_max`, or `dt`"; `03-quality-strategy.md` §9.12: non-finite factor/`nu_max`/`dt` → 13. Choose one (likely 13) and align §11.1. |
| **Minor** | **F-21 — `wrap(owner_cell + link.offset)` uses unchecked index arithmetic; a hostile package can overflow `i32`.** | Step 14 §6.1: `partner = wrap(owner_cell + link.offset) * P + link.partner_position`; `Link.offset` is `[i32;3]`. `mocassin-msl/src/map/model.rs` 416–426 bounds `|offset[i]| ≤ |interaction_range[i]| + 1` but does not bound the interaction range above, so a package with a huge range/offset can overflow before the wrapped-index check. Use checked/`i64` `rem_euclid` or reject a range larger than the lattice; add a hostile-offset test. |
| **Minor** | **F-22 — `SelectionPool.members` capacity is not reserved per pool, so `set_selectable` can allocate.** | Step 14 §8: `set_selectable` swap-removes or inserts a member; §17 requires zero allocations in the pool add/remove helpers after construction. The plan sizes `pools.members` as `M*8` in total, but each `members: Vec<PoolMember>` must reserve its per-pool maximum (the number of selectable `(env, particle)` combinations with that direction count) at construction; otherwise an insert after a particle change reallocates. State the per-pool capacity in §8 and plan it. |
| **Note** | **F-23 — `plan == report` is defined against "actual vector capacities", which `Vec::with_capacity` does not guarantee to equal the plan.** | Step 14 §2 says `MemoryReport` is computed from the allocated vectors and `plan == report`; §17 says `MemoryReport` matches the actual vector capacities. `Vec::with_capacity(n)` may have capacity > n (allocator rounding), so a strict equality can fail. Define the report from the plan/lengths or assert `report >= plan`; state which. The `max_context_bytes` is the derived-array cap, not process RSS; the benchmark records peak RSS separately. |
| **Note** | **F-24 — The §9.2 parenthetical reverses the original's link storage.** | Step 14 §9.2 says "`EnvironmentLink.TargetEnvironmentId` points from the receiver back to the changed neighbour". The original `LinkEnvironmentToSurroundings` stores the link on the **target** (the changed/partner) environment with `TargetEnvironmentId` set to the centre/receiver, and `BufferJumpLinksOfJumpPath` stores `link_id` in the sender's list. The design's receiver-major `JumpLink` is the dual and the energy rule (receiver's table/row) is correct; clarify the wording so a port reviewer does not think the original stores the link on the receiver. |
| **Note** | **F-25 — Step 17's `linked(a,b)` is still named but not defined.** | Step 14 §15 lists `linked(a,b)` for Step 17; no definition is given. Implement it as a helper over `link_range(position(a))`/`incoming`, or define the exact semantics (owner's forward link to the resolved partner, no reverse synthesis). |
| **Note** | **F-26 — The `MOBILE` static/passive assumption should be confirmed and tested.** | Step 14 §3: `MOBILE = particle != void && jump_count >= 0`; `jump_count` is `u32`, so the predicate is always true for non-void and the original's `-1` (static)/`0` (passive) split is not representable. The design says the distinction is "not needed"; add a fixture or test where a particle is not in any KMC collection's mobile mask and record the assumption for Step 29 parity. |
| **Note** | **F-27 — "twelve-phase" does not match the 13-phase list (0–12).** | Request summary and Step 14 `Delivers` say "the twelve-phase build order"; §1 lists phases 0–12. Update the count to thirteen (or "phase 0 plus twelve"). |
| **Note** | **F-28 — `pool_of` code-block key comment typo.** | Step 14 §8 code block: `pub pool_of: Vec<u32>, // length P*pc; key = P*pc + particle`; the prose and §2 table use `P1*pc + particle`. Fix the comment so an implementor does not use a constant key. |

#### Closure status

No Blocker remains. The three round-1 Majors are genuinely redesigned and the round-1 minor/note set is closed
in the Step 14 freeze. F-17 is a new Major that needs a design decision (support multiple centre positions per
cluster table or record the v1 limitation and writer follow-up). F-18–F-22 are minor corrections and F-23–F-28 are
notes; none of the notes changes a persisted format or a scientific invariant.

**Other requested checks.** The `LimitExceeded` 12-vs-14 distinction is explicit and correct: `14` is the
run-time abort, `12` is a context/package the job cannot represent, and Step 14 §2/§13, architecture §7.3, and
quality strategy §9.12 agree (F-14 closed). The crate graph is the Step 14 target, not the Step 13 state:
`mocassin-solver/Cargo.toml` still has `mocassin-msl` normal plus `mocassin-state`/`mocassin-extension`/`rayon`;
feature 1 makes `mocassin-msl` dev-only and removes the others, and `solver-architecture.md` records the target
(F-15 is an implementation task, not a design gap). The `EnergyEngine<'a>` fix is correct for Step 15 (`&mut`
context, `&mut self` methods); Step 16's `KmcEngine` composition is F-19. The 10×10×10 evidence is the real-writer
ceria fixture plus the populated-model clone; the ceria model has no pair/cluster tables, the clone is the
pair/cluster-scale measurement, both are budgeted at `< 100 ms`/`≤ 64 MiB`, and the benchmark/baseline and
allocation tests are specified (F-9/F-10 closed). The High class/no-split decision is recorded in Step 14 §4 and
roadmap §3.1 with the owner-acceptance request; six features and one acceptance story are bounded; no code or
fixture exists. The docs/parity plan names `solver-architecture`, `physics/energies`, `performance/index`,
`troubleshooting/errors`, `testing-and-parity`, `msl-format`, and the new parity row; `docs --require` passes. No
format change: `contracts/manifest.json`, `.msl` schema/payload/version/signature, and fixtures are untouched;
the new 10×10×10 package is a test artifact.

**Cross-step Step 13/14 concern (judged acceptable).** The Step 14 retirement of the Step 13 `DirectionPool`
placeholder and the `JumpLink` shape change are acceptable design evolution inside Step 14 feature 1/4, not an
ADR/amendment/tag-note trigger: the Step 13 containers were explicitly type-only and no reader constructed them
(grep over `src/rust` shows only the re-exports); the Step 13 page now records the reconciliation (container note
at lines 1216–1227); the Step 14 page §8/§9.2/§22 records the replacement; no `.msl` field, manifest, signature,
fixture, or scientific invariant changes; and the `step-13` tag is the historical checkpoint and must not move.
The feature log should record the public Rust API break, and the Step 13 invariant tests should be updated when
the placeholder is removed. If the owner wants a durable process record, the Step 13 page note is sufficient; an
ADR would be warranted only if the placeholder or the state format were a persisted/contract surface, which it is
not.

#### Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

**Changes required.** F-1, F-2, and F-3 are genuinely redesigned and F-4–F-16 are closed in the Step 14 freeze.
F-17 is a new Major: the cluster runtime cannot represent a writer-valid cluster table linked to every P1
position of its centre site, and the design does not record the limitation. F-18–F-22 are minor corrections and
F-23–F-28 are notes. No Blocker was found. The Step 13 placeholder retirement is acceptable as planned type-only
evolution with the Step 13 reconciliation note; no ADR/tag note is required. A fresh round is required after the
cluster-instance decision and the Step 16 reconciliation.
