# Step 14 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-14.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 14 — Simulation Context Initialization |
| Request owner | Step agent (implementor) |
| Orchestrator | — (relayed by the orchestrator; do not edit) |
| Step agent (implementor) | Step 14 step agent |
| Audit initiated by | Orchestrator, on request of the step agent |
| Step branch | `step/14-simulation-context-initialization` |
| Closeout evidence | [`docs/planning/closeout/step-14.json`](../closeout/step-14.json) |
| Audit date | 2026-11-21 |
| Auditor | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh context, no shared conversation with the planner, implementors, design reviewers, or orchestrator |

The closeout evidence file holds the audited commit, closeout CI run and `head_sha`, `main` SHA at freeze, merge candidate,
merge-tree CI, platform CI, and tag. Do not restate them here: one source, one place.

Step page: [`Step 14 — Simulation Context Initialization`](../steps/step-14-simulation-context-initialization.md)

> Orchestration: the orchestrator spawns a fresh auditor when the step agent has frozen the step branch and
> completed the audit request, and relays the verdict and owner decisions. The step agent applies fixes on a new
> feature branch, reruns closeout CI, adds resolutions without altering reviewer text, and asks the orchestrator
> for re-audit until the auditor no longer identifies issues. The orchestrator does not fix findings or edit the
> audit record. The human owner approves the final verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | D. Solver |
| Depends on | 13 and `step-11.1` (the `step-11-amend-2` reader/runtime cluster-code lookup) |
| Delivers | A deterministic, allocation-bounded `SimulationContext` built once per job from the validated `JobPackage`: lattice and environment arenas, an owner-major neighbour-link CSR with an inverse routing index, the cluster runtime, the energy background and environment energy vectors, the supercell selection pools, the receiver-major KMC jump status, the mobile/static tracker index space, counters, goals, normalization, the time step, and the resume hook. No energy deltas, no KMC/MMC cycle, no tracker values, and no `.mcs` output. |

## Step-specific acceptance checklist

Copy the acceptance criteria from the step page, preserving their wording:

- [x] `msl-v1` job 1 (6×6×6 ceria) and `amend-1-net10` initialize a context whose mobile count, selectable count,
      `prerun`/`main`/`total` goals, block size, `dt`, and tracker counts are asserted against computed values.
      — *Verified for `msl-v1` (mobile 648, selectable 432, goals 162000/97200/259200, block 2592,
      `dt = 2/(1e13·432)`, sequential tracker ids); `amend-1-net10` is not opened by a Step 14 test — see F-1.*
- [x] `step-13-populated` KMC and MMC jobs initialize; the pair link set and the inverse set equal a brute-force
      expansion of the payload for every supercell cell; the clustered energy vectors equal a direct table
      lookup; the MMC context has no pools/jump status/time.
- [x] `step-13-centered` initializes; a direction mapped into another start bucket resolves its path at the
      selected bucket; the per-node allowed masks are exposed unchanged.
- [x] The multi-centre cluster fixture initializes with **one instance per slot-0 link** (four instances for the
      centred model's four-position centre site), each with its own slot positions, code, and centre energy; the
      per-instance codes equal a brute-force rebuild from the instance's slots; the committed sample, package
      summary/SHA-256/payload set, README provenance, .NET rebuild test, and regeneration command are present
      (§6.4/F-36).
- [x] The amendment-2 two-member ceria fixture (`step-11-amend-2`) resolves **all eight** raw assignments,
      including the two non-canonical ones; the pre-map fixture returns the distinct `ExpansionMapAbsent`
      (recompile-required) error for those two, and a present map that lacks a tuple returns
      `MissingOccupationCode`; the context never re-derives symmetry (review F-29).
- [x] A multi-centre member whose image is absent or ambiguous returns the documented `ClusterGeometry` (exit 12)
      naming the instance, slot, and member site; a unique rotated multi-centre image is **accepted**, not
      reported as a cross-image (review F-35).
- [x] A transition evaluated on a package whose pair table is **asymmetric** (`t[a][b] ≠ t[b][a]`) and that has
      no reverse link produces the owner-major delta; the result differs from the sender-major delta that the
      draft would have produced.
      — *Verified that the owner-major link/inverse data and the no-synthesised-reverse-link rule are tested;
      no delta computation exists in Step 14 (Step 15 owns it) — see F-2.*
- [x] A pool contains members in two different supercell cells; selecting, removing, and re-adding each member
      addresses the right environment and never collides in the slot map; no pool insert reallocates (reserved
      capacity).
- [x] A hostile package with `i32::MIN`/`i32::MAX` pair offsets initializes or fails cleanly, never panics, and
      the wrapped target is correct.
- [x] The 10×10×10 real-writer fixture initializes in `< 100 ms` (release, warm, same-runner-class) and the
      reported memory total is within budget; the populated-model 10×10×10 clone is measured in the same run
      and recorded, because the ceria fixture has no pair/cluster tables.
      — *Re-run: ceria 72.467 µs (72.005–72.945), plan 308,632 B; populated clone 319.81 µs (313.36–328.47),
      plan 234,832 B; both far below the budgets.*
- [x] The initial lattice energy matches a hand-computed site/pair/cluster system exactly on a **multi-cell**
      lattice (at least two cells), so the instance × cell cluster sum is exercised (review F-33).
- [x] A job with `target_mcsp`/`pre_run_mcsp` values whose products or sum overflow `u64` returns the documented
      exit-12 `InvalidInput` (`goal overflow`), never a wrapped goal (review F-37).
- [x] The context summary (counts, link histogram, pool sizes, jump-status sizes, goals, `dt`) is byte-identical
      across two initializations of the same package.
- [x] `MemoryPlan` rejects an over-cap package before any allocation; `plan == report` on planned lengths; the
      capacity diagnostic is recorded.
- [x] `SimulationContext` is `Send` and not `Sync` (the `compile_fail` doctest fails to compile).
- [x] Zero selectable directions, zero mobile particles, a zero total goal, a bad cluster geometry, a missing
      occupation code, a context-limit overflow, a cancelled initialization, and a mismatched resume each return
      the documented
      error/exit.
- [x] Resume restores lattice, counters, RNG, time, and normalization exactly; `RawMax` is preserved; a
      completed state returns `AlreadyCompleted`.
- [x] A counting allocator observes zero allocations in the hot accessors and the pool add/remove helpers after
      construction.
      — *Re-run: 98 allocations / 316,872 B against the 308,632 B plan; populated 103 / 11,861 B against
      2,672 B; the hot-accessor and 200-cycle `set_selectable` assertions observed zero.*
- [x] `MemoryReport` reports each array's planned length × `size_of` and equals the plan (`plan == report`);
      summed capacity bytes are a diagnostic (`>=` plan), not the equality target (review F-32).
- [x] Documentation written: `solver-architecture.md` (context section), `physics/energies.md` (total-energy
      convention), `performance/index.md` (context budget), `troubleshooting/errors.md` (context failures),
      `testing-and-parity.md` (tiers/fixtures), `msl-format.md` (cluster-link limitation and runtime expansion).

## Standard audit checklist

- [x] Required tests from the step page pass.
      — *All 10 Step 14 Rust test binaries, the doctests, and the two fixture test classes pass; two acceptance
      claims have direct-evidence gaps (F-1, F-2).*
- [x] Public/protected XML documentation policy is followed.
      — *`missing_docs` is denied workspace-wide; every public context item has a doc comment and every new file
      carries `SPDX-License-Identifier: MIT`; the C# fixture tests/builders have XML docs.*
- [x] Documentation, status dashboard, feature log, and parity matrix are current.
      — *The six named pages, the parity row, the status row, and the feature log are updated; the parity row says
      "Done" while the dashboard says "In progress" (N-1).*
- [x] Closeout CI is green at the audited commit.
      — *Run `44922414484`: `workflow_dispatch`, `conclusion=success`, `head_sha=3fd45161d5683405955f806196ca61b295066479`;
      .NET, Rust, Contracts and policy, and Public API succeeded; Python skipped by its path filter; Ubuntu only.*
- [x] Closeout evidence matches the audited commit and the merge candidate.
      — *`python tools/check_closeout_evidence.py docs/planning/closeout/step-14.json` → "Closeout evidence: OK";
      `audit_commit` = `closeout_ci.head_sha` = `merge_candidate` = `3fd4516`.*
- [x] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded.
      — *`main` is still `929327e660c7eda4cb74e438901277b5fa31aa65`, the recorded freeze SHA and an ancestor of
      `3fd4516` (merged as `8c09a8c`, parents `00fceab`/`929327e`); no post-freeze move, so no merge-tree run is
      required.*
- [x] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code.
      — *Not run. Step 14 adds platform-neutral Rust library/test/benchmark code and Markdown/JSON/.mocprj
      fixtures; no P/Invoke, native library, RID-specific package, path/case/locking behaviour, or GUI code. The
      absence is an accepted risk recorded in the step page/closeout evidence.*
- [x] Format/version/fixture changes are documented and tested.
      — *No `.msl`/manifest/version/signature change (`contracts/manifest.json` untouched; `main...3fd4516` adds
      two real-writer test artifacts only). The `DirectionPool` removal and `JumpLink` reshape are a recorded
      public Rust API break with no format change.*
- [x] Security, dependency, licence, and secret-scan gates pass.
      — *`verify.sh all --require`: 26 contract artifacts current, projections 5/4/2, space-group catalog
      current, process consistency OK, 60 tool tests, SPDX headers in 1,039 files, no secret patterns in 1,546
      files, cargo-deny advisories/bans/licenses/sources OK, NuGet licence/vulnerability audit and ApiCompat OK.*
- [x] Code structure policy passes; no new root-level or catch-all folders.
      — *`Mocassin.Architecture.Tests` 7/7 and `tools/verify.sh structure` pass; `src/context/` is a named concept
      with 12 direct files and no catch-all folder. The Rust 800-line rule is not mechanically enforced and is
      exceeded by `mod.rs`/`limits.rs` (F-3).*
- [x] No unrelated changes or unapproved scope.
      — *The diff is the context runtime, the two fixtures/samples, the approved `mocassin-types` placeholder
      retirement and `JumpLink` reshape, the approved `Cargo.toml` change, tests, evidence, and documentation;
      `3fd4516..af434f1` is four `docs/planning` files only.*
- [x] Branch, tag, and status hygiene are correct.
      — *No merge, no tag, no push to `main`; the branch is frozen at `3fd4516`; the dashboard says `In progress`
      and this audit record is `Passed with notes`; the parity "Done" wording is N-1.*

## Evidence

**Independence.** I am a fresh context. I did not author, design, implement, review, or coordinate this work and
have no shared conversation with the planner, implementors, design reviewers, or orchestrator. I received only
the frozen commit, the request record, the repository, and the required scope/commands; no desired verdict or
summary was supplied. I read the repository and ran the checks myself in an isolated worktree
(`/home/node/worktrees/step-14-audit`, detached at `af434f1`). Apart from this record I changed no repository
file: I did not merge, tag, push `main`, delete branches, fix a finding, or edit code, tests, configuration, or
design docs.

**Frozen state.**

- Audited commit `3fd45161d5683405955f806196ca61b295066479`; branch head `af434f1`. The reviewer record's parent
  is `af434f1`, but `git diff --name-status 3fd4516..af434f1` is exactly
  `docs/planning/audits/step-14.md`, `docs/planning/closeout/step-14.json`, `docs/planning/status.md`, and the
  Step 14 page — documentation/evidence only. No code, test, configuration, manifest, signature, or generated
  artifact moved after the audited commit.
- `git diff --name-status 8c09a8c..3fd4516` is `docs/planning/audits/step-14.md`, `docs/planning/status.md`,
  the Step 14 page, and `tests/reference/bench/step-14-context.json` (the benchmark-evidence `sourceCommit`
  provenance string); no code, test, fixture, configuration, or manifest file moved between the locally gated
  commit and the audited commit, so the `all --require` run at `8c09a8c` covers the audited code and fixtures
  exactly.
- `main` is `929327e660c7eda4cb74e438901277b5fa31aa65` (the recorded freeze); it is an ancestor of `3fd4516`
  and was merged as `8c09a8c38dd7b1dc4a01f7fc85dc7708ff760ebe` (parents `00fceab`/`929327e`). `main` has not moved
  since the freeze, so the merge-tree rule is satisfied by the closeout run.
- Closeout CI run `44922414484` was queried through the public GitHub API: `name=CI`, `event=workflow_dispatch`,
  `conclusion=success`, `head_sha=3fd45161d5683405955f806196ca61b295066479`, branch
  `step/14-simulation-context-initialization`. Jobs: Detect changes, Public API compatibility, .NET
  (ubuntu-latest), Contracts and policy, and Rust all `success`; Python `skipped` (path filter). This is the only
  step-branch run.

**Commands and results (all re-run in this audit at `af434f1`).**

```text
./tools/verify.sh all --require
    exit 0; "Closeout gate completed with required checks enabled."
    .NET: build, test, format clean; 2,007 tests passed, 0 failed
      (Contracts 38, Symmetry 223, Results 91, Core 685, Compilation 99, Cli 203,
       Packages 536, Architecture 7, App 125); includes the two Step 14 fixture test
      classes and their rebuild tests
    Rust: cargo fmt --all --check and clippy --workspace --all-targets -- -D warnings
      clean; cargo test --workspace all green, including:
        context_clusters 17, context_energy 8, context_fixtures 3, context_goals 15,
        context_jumps 10, context_links 13, context_pools 10, context_resume 7,
        context_skeleton 9, allocation 1, lib 38; doctests: Send positive and
        `SimulationContext: Sync` compile_fail both pass
    Python: 3 passed; sdist and wheel built
    Documentation: math conventions OK (194 files); links OK (197 files); Zensical strict;
      DocFX "Build succeeded" 0 warnings / 0 errors; rustdoc clean
    Contracts/policy: 26 artifacts current; model/parameterization/lattice projections
      5/4/2; space-group catalog current; process consistency OK; 60 tool tests;
      SPDX headers in 1,039 files; no secret patterns in 1,546 files; cargo-deny
      advisories/bans/licenses/sources OK; ApiCompat OK; NuGet licence/vulnerability OK
    AOT: zero-warning smoke host and IL2026/IL3050 positive control pass; the real
      Mocassin.Cli publish fails with 62 diagnostics (31 IL2026 + 31 IL3050 from the
      reflection-based Core JSON converters), recorded as evidence, not a gate; Step 31
      owns the AOT decision
cargo build --manifest-path src/rust/Cargo.toml --workspace --release
    exit 0; release workspace build clean (verify.sh does not run it for `all`)
cargo test --manifest-path src/rust/Cargo.toml -p mocassin-solver --test allocation -- --nocapture
    exit 0; ceria 10×10×10: 98 allocations / 316,872 B, plan 308,632 B, capacity
    308,632 B; populated 2×2×2: 103 allocations / 11,861 B, plan 2,672 B, capacity
    2,704 B; hot accessors and 200 `set_selectable` cycles: 0 allocations, capacity
    and pointer unchanged
cargo bench --manifest-path src/rust/Cargo.toml -p mocassin-solver --bench context
    exit 0; ceria 72.467 µs [72.005, 72.945]; populated clone 319.81 µs
    [313.36, 328.47]; recorded medians 75.565 µs / 315.26 µs on the declared runner
    class; both far below the <100 ms / ≤64 MiB budgets; the populated rerun is +1.4 %
    against the recorded baseline, inside the 10 % rule
sha256sum tests/contracts/packages/step-14-*/step-14-*.msl
    both match the committed `.sha256` lines (0865dd75…, ace6f38e…)
python tools/check_closeout_evidence.py docs/planning/closeout/step-14.json
    exit 0; "Closeout evidence: OK"
curl https://api.github.com/repos/seb-eis/mocassin-2/actions/runs/44922414484
    success at 3fd4516 (see frozen state); jobs queried separately
git diff --name-status 3fd4516..af434f1
    four docs/planning files only (see frozen state)
git diff --name-status main...3fd4516 -- tests/contracts samples contracts/manifest.json
    additions only under the two step-14 fixture directories and two new samples; no
    step-13 fixture, msl-v1, amend-1-net10, or manifest change; the step-13-centered
    package and sample are byte-identical
```

**Files and artefacts reviewed.** `AGENTS.md` §3.10/§7/§8–§16; `docs/planning/audits/README.md`; the Step 14 page
(§§1–24 and the feature evidence); `docs/planning/reviews/step-14.md` (four design-review rounds); the Steps 13,
15, 16, and 19 pages; the approved `step-11-amend-2` amendment; ADRs 0001/0005/0010/0011/0013; data contracts
§3.3/§4; the closeout evidence and `closeout/README.md`; the status dashboard and parity matrix;
`docs/development/{solver-architecture,testing-and-parity,code-structure}.md`; `docs/physics/energies.md`,
`docs/performance/index.md`, `docs/reference/msl-format.md`, `docs/troubleshooting/errors.md`; all
`src/rust/crates/mocassin-solver/src/context/*.rs` and `tests/*.rs`, `benches/context.rs`, `Cargo.toml`; the
`mocassin-types` `JumpLink`/`DirectionPool` change and the `ClusterTable::lookup` in
`runtime/energy.rs`; the two fixture READMEs, summaries,
payload indexes, hashes, and the .NET builders/tests; `samples/step-14-*`; the benchmark evidence and
`baselines.json`; and the reader's model/transition validation.

**Context correctness spot-checks (read from the frozen code, not the summary).**

- `Dimensions` linear order `q + c·P + b·P·C + a·P·C·B` round-trips; `i64` `rem_euclid` wrap handles
  `i32::MIN`/`i32::MAX`; `A·B·C·P ≤ i32::MAX` and the per-array caps are checked with `u64` arithmetic in
  `MemoryPlan::plan` before the first arena allocation; the jump-status and mobile-mapping entries are appended
  to the plan before their vectors are allocated. `MemoryReport` is built from the admitting plan and the
  invariant sweep checks each planned length against the built array.
- Links are owner-major and sorted by `(pair_id, partner_position, offset)` (stable); the inverse index is
  filled in ascending owner/link order and `has_valid_shape` verifies every inverse entry points back at the
  exact forward entry. `partner_of` uses the `i64` wrap; `incoming_owner` widens `-offset` before subtracting;
  `linked` is either-direction and never synthesises a reverse link.
- Clusters: one instance per slot-0 link, ordered by `(centre position, table)`; slot 0 is `[0,0,0]`; slots
  `1..n` resolve through the candidate-position link, then (multi-centre only) the unique member-site image,
  else `ClusterGeometry` naming table/instance/slot (the fallback detail names the member site); a table with
  four slot-0 links produces four instances with their own geometry/codes/energy rows. Code resolution is only
  `ClusterTable::lookup`: `Canonical` stores the id, `MissingFromMap` → `MissingOccupationCode`,
  `ExpansionMapAbsent` → the distinct recompile-required error; grep finds no solver-side symmetry, tuple sort,
  or direct `occupation_codes` resolution (only length invariants).
- Energy: `site` is the unit-cell table in kT; vectors are `site + Σ_owner linked pair_table[p][partner]`;
  cluster energy is per instance × cell at the centre, from the canonical code, and is not folded into the
  vector; the exact total is `Σ_stable site + 0.5·Σ_stable(vector − site) + Σ_instances Σ_cells cluster`, and
  `recompute_lattice_energy_kt` is the independent check. The D5-2 void-member rule holds: a void member/cell
  has `cluster_code_id == None` and a zero energy row, no lookup is performed for it, and a non-void
  non-canonical tuple still fails closed.
- Goals: checked products/sum/round-up; `total = ceil(sum/100)·100`, `cycles_per_block = total/100`; zero total
  and overflow are exit-12 `InvalidInput`; zero mobile is `NoMobileParticles` before pools. Normalization:
  `K_fix = exp(E_norm/kT)`, `K_auto = 1/RawMax`, the `K_fix` fallback, `nu_max` from the job cap or the maximum
  active-rule frequency, `dof` from the flag, `dt = dof·K_norm/(nu_max·selectable)`; non-finite values are exit
  13. MMC has zero goals, unit factors, `dt = 0`, time 0, empty pools/jumps/tracker index, and the invariant
  sweep enforces it.
- Trackers: `mobile_positions[id] = env` in ascending order (passive species included), static id
  `cell·per_cell + position·(pc−1) + particle−1`, global/static mappings exposed read-only; MMC has an empty
  index space. Resume validates the parameterized signature, recomputed seed hash, job id/kind/dimensions/
  counts/particles/counters/time/normalization, re-derives the mobile mapping, preserves `RawMax`, restores the
  RNG stream and time, drives pools/energy/goals from the resumed particles, and returns `AlreadyCompleted` for
  a completed state after the same binding validation.

**Known notes checked.** The ENM008 restriction is real (`CoreDiagnosticCatalog`: "fewer than two or more than
eight surrounding offsets"), and the multi-centre sample uses two distinct member sites (B and C at
`[0,0,0.05]`/`[0,0,0.1]` on the 12-position A/B/C structure), producing four slot-0 links/four instances; the
`step-13-centered` sample and package are untouched. The ceria 10×10×10 fixture is table-free, and the populated
clone is the pair/cluster measurement. The void-member rule, benchmark class, AOT story, platform-CI absence,
`main` freeze, and feature-6 squash are recorded as stated. The F-39–F-41 corrections and F-42–F-43 notes are
present in the step page (§22 wrapper, §20 environment wording, roadmap row).

**Limits of this audit.** The public CI API exposes run/job conclusions and SHAs but not job logs, so the CI
evidence is the run/job conclusion set above plus the locally reproduced `all --require` gate at the
code-identical commit. Benchmark and allocation numbers are machine-dependent; I reproduced them on the same
declared runner class. I did not run Windows/macOS (no platform-specific code). No repository file other than
this record was modified.

Evidence file: `docs/planning/closeout/step-14.json`

## Known notes for the auditor

- **Multi-centre fixture authoring deviation.** The project model's `ENM008` validator requires 2–8 surrounding
  offsets, so the design's literal one-member cluster cannot be authored. The implemented
  `step-14-multi-centre-cluster` sample adds a third stable site C and uses two **distinct** member sites (B and
  C) at offsets `[0,0,0.05]` and `[0,0,0.1]`; two images of one member site remain the rejected ambiguous OD-1
  case. The centred sample stays byte-identical, and the four-instance/member-site-fallback acceptance is
  unchanged. Recorded in the step page §19 and the fixture READMEs.
- **The 10×10×10 ceria fixture is table-free.** The real-writer fixture has no pair or cluster tables, so it
  measures the table-free 10×10×10 path. The `step-13-populated` model cloned onto a hand-built 10×10×10
  `InitialLattice` is the pair/cluster-scale measurement; both are recorded in the benchmark evidence.
- **Benchmark class.** The context benchmark was run on the local container class
  `linux-x64-local-container-amd-ryzen-7-7840u-rust-1.99.1` (AMD Ryzen 7 7840U, Debian 13, Rust 1.99.1,
  Criterion 0.8.2, release). The 10 % rule applies only within that class; Step 29 owns the automated
  same-class regression job. Absolute timings are not compared across CPUs/OS/Rust versions.
- **Void-member rule for Step 15.** A cluster cell with a void centre/member has no canonical occupation row and
  contributes zero (the empty cluster). Step 15 must use the zero energy row/absent `cluster_code_id`, not
  `cluster_lookup`; the non-canonical non-void `code_expansions` fail-closed behavior is unchanged.
- **Platform CI.** Not run. Step 14 adds Rust library/test/benchmark code and Markdown/JSON fixtures; it adds no
  P/Invoke, `LibraryImport`, native library, RID-specific package, Windows/macOS path/case/locking behaviour, or
  GUI code. Ubuntu is the only affected platform; the absence is an accepted risk.
- **AOT evidence.** The strict smoke host and the IL2026/IL3050 positive control pass; the real `Mocassin.Cli`
  publish is expected to fail with reflection-based JSON converter diagnostics, recorded as non-gating evidence
  (Step 31 owns the AOT decision), as in Step 13.
- **Main at freeze.** `main` `929327e` (Step 23B design start) was merged into the step branch before the
  audited commit; the closeout CI covers the merged tree. If `main` moves again, the merge-tree rule applies.
- **Feature 6 squash.** `00fceab` on the step branch; the evidence file records the audited/merged commit in
  `docs/planning/closeout/step-14.json`.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **F-1 — The `amend-1-net10` half of the first acceptance criterion and the §19 real-package row are not exercised by any Step 14 test.** The `msl-v1` context tests assert the criterion's computed values (mobile 648, selectable 432, goals 162000/97200/259200, block 2592, `dt = 2/(1e13·432)`), the environment flags, and the sequential tracker ids; the static/global tracker-count assertions are on `step-13-populated`. The two fixtures are logically identical (the reader acceptance test asserts the same job-1 signatures `1845b70e…`/`92cf375e…`, and the `msl-v1` README records the canonical-summary equality), so the expected values cannot differ and the risk is low — but the ticked claim is not literally evidenced. Adding a direct `amend-1-net10` initialization assertion (and a mobile/static/global tracker-count assertion for it) would close it. | `grep -rn amend-1-net10 src/rust/crates/mocassin-solver` → no hit; the only Rust consumer is `src/rust/crates/mocassin-msl/tests/reader_acceptance.rs:142`; Step 14 acceptance §17 item 1 and §19 "Real packages" row; `tests/contracts/packages/msl-v1/README.md` (logical equality). |
| **Minor** | **F-2 — The asymmetric-table owner-major *delta* criterion is not exercised; only the underlying link orientation is.** `the_link_orientation_uses_the_owner_table_and_never_synthesises_the_reverse` asserts that P1's table-0 link is the owner-major one and P0 has no forward link; `a_missing_reverse_link_is_not_synthesised` asserts no reverse link is created. No test computes an owner-row delta and compares it with the transposed sender-major value, although §19's "Delta orientation" row says it does. The step's non-goals correctly leave delta propagation to Step 15, so this is a test/claim gap rather than a context defect: either add the small `node_energy`/`incoming`-based owner-vs-sender delta assertion or move the criterion to Step 15 and mark it deferred. | `src/rust/crates/mocassin-solver/tests/context_links.rs` (`the_link_orientation…`, `a_missing_reverse_link…`); Step 14 §17 item 7, §19 "Delta orientation"; Step 14 Scope "No energy engine". |
| **Minor** | **F-3 — Two `src/context` files exceed the documented 800-line per-file limit.** `mod.rs` is 1,502 lines (1,385 without the inline test module) and `limits.rs` is 863 (721 production). `AGENTS.md` §8 and `docs/development/code-structure.md` §2.2 state the 800-line rule, and the round-4 design review recorded "files below 800 lines". The automated structure test is .NET-only (`Mocassin.Architecture.Tests` 7/7 pass) and the mechanical Rust layout test is deferred to Step 29, so no gate fires. The files are cohesive, but Step 15 adds the energy engine to the context; split the phase pipeline/invariant sweep (for example `context/build.rs`) and the limits tests before then. | `wc -l src/rust/crates/mocassin-solver/src/context/{mod,limits}.rs`; `docs/development/code-structure.md` §2.2; `docs/planning/reviews/step-14.md` (code-layout checklist); `tools/verify.sh structure` / `Mocassin.Architecture.Tests`. |
| **Note** | **N-1 — The parity matrix marks Step 14 `Done` while the status dashboard says `In progress` and the audit is only now complete.** The row's notes say "audit/merge pending", so the intent is visible, but "Done" is the vocabulary the other rows use for merged steps. Set it after the merge, or use a pending qualifier. | `docs/planning/appendix-b-parity-matrix.md` Step 14 row; `docs/planning/status.md` row 14. |
| **Note** | **N-2 — `ContextLimits::max_path_nodes` can be raised above the fixed 10-node scratch.** `jumps.rs:305-309` uses `[Option<EnvId>; 10]` and the counting/fill loops take at most that many nodes, while validation only rejects `path.len() > limits.max_path_nodes` (`jumps.rs:126`). With a caller-raised limit an 11+-node direction would be accepted and its status would silently omit nodes beyond index 9 instead of returning `InvalidInput`. The reader and the default cap both stop at 10, so no `.msl` reaches it; validate `max_path_nodes <= 10` (or size the scratch from the limit). | `src/rust/crates/mocassin-solver/src/context/jumps.rs:126,305-309`; `limits.rs:48,82`; `mocassin-msl/src/map/transition.rs` (reader path cap). |
| **Note** | **N-3 — Phase 0 reads model vectors before phase 1 validates their lengths.** `MemoryPlan::plan` → `pool_entries` → `pool_shape` indexes `structure.environments[position]` for every P1 position (`pools.rs:437/454`) before `validate_inputs` checks `environments.len() == positions`; a hand-built `JobPackage` with a shorter vector panics instead of returning `InvalidInput`. Similarly `check_jump_links` indexes `directions[direction_index]` for `statuses = P·direction_count` (`mod.rs:574-590`), and an MMC context skips the `JumpRuntime::build` equality check. Reader-built packages always satisfy both invariants (`mocassin-msl/src/map/model.rs:413-426`, `map/transition.rs:27`), so no real `.msl` is affected; move the length checks into `Dimensions::from_job`/the plan for defence in depth. | `src/rust/crates/mocassin-solver/src/context/{limits,pools,mod}.rs`; `mocassin-msl/src/map/{model,transition}.rs`; `validate_inputs` phase order in `mod.rs`. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** Passed with notes — accepted by the human owner 2026-11-21; F-1–F-3/N-1–N-3 deferred to `fix/step-14-structure-and-tests` before Step 15

The step branch is frozen at `3fd45161d5683405955f806196ca61b295066479`; the reviewer record commit is the
documentation-only child of `af434f1` that touches only this file. No Blocker or Major remains. The minor
findings are test/evidence-claim and file-length issues; the notes are status wording and hand-built-package
robustness. The human owner still approves the verdict, the step-to-main merge, and the `step-14` tag.

**Owner acceptance (2026-11-21).** The human owner accepted `Passed with notes`, approved the step-to-main merge
and the `step-14` tag, and assigned F-1–F-3 and N-1–N-3 to the maintenance branch
`fix/step-14-structure-and-tests` before Step 15 starts. The residual limitations (the `ENM008` two-member-site
fixture deviation, the table-free 10×10×10 real-writer fixture with the populated clone as the pair/cluster
measurement, the non-gating real-CLI AOT publish failure, and the absence of Windows/macOS platform runs) are
accepted.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Passed
- [x] Passed with notes
- [ ] Fixes required
- [ ] Waived

Follow-up owner: Step agent (resolve or explicitly accept F-1–F-3 and N-1–N-3 in the resolution/status commit) and
the human owner (accept the verdict; approve the step-to-main merge and the `step-14` tag).
Follow-up issue/step: Step 14 resolution/status commit; Step 15 for the owner-major delta test (F-2); Step 29 for
the mechanical Rust layout check and the same-class benchmark/context regression job.

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| F-1 | **Closed** — `tests/context_goals.rs` now opens `amend-1-net10` directly and asserts mobile 648, selectable 432, goals 162000/97200/259200, block 2592, `dt = 2/(1e13·432)`, and the static (16 per cell, 3456 total) and global (2) tracker counts; it also ties the values to the logically identical `msl-v1` context. | Step agent on `fix/step-14-structure-and-tests` | `29cc85e` |
| F-2 | **Re-pointed to Step 15** — the asymmetric-table owner-major delta criterion is a test/claim gap, not a context defect; the Energy Engine step owns the delta itself and its test plan carries the owner-vs-sender assertion. No Step 14 code change. | Step 15 step agent | — (Step 15 feature branch) |
| F-3 | **Closed** — `context/mod.rs` (1,502) is split into `context/build.rs` (phase pipeline and input validation) and `context/invariants.rs` (invariant sweep); `context/limits.rs` (863) keeps the plan and moves its unit tests to `context/limits/tests.rs`. Every `context` file is now under the 800-line limit; the public API surface is unchanged (internal module moves only). | Step agent on `fix/step-14-structure-and-tests` | `beb1943` |
| N-1 | **Closed** — the Step 14 parity row and the status dashboard now both record `Done` + merge/tag and the maintenance closure; the stale "implementation pending/Partial until Step 14 is implemented" wording in the "Selection optimizers" row is corrected; the dashboard's stale Step 13 "In progress" line and the two GUI maintenance notes are corrected in the same edit. | Step agent on `fix/step-14-structure-and-tests` | `fc03ba1` |
| N-2 | **Closed** — `ContextLimits::max_path_nodes` above the fixed 10-node jump-status scratch (`jumps::MAX_PATH_NODES`) is rejected by `MemoryPlan::plan` with exit-12 `InvalidInput { what: "max_path_nodes" }` before any allocation; tests cover the rejection and the 10/9 boundary. | Step agent on `fix/step-14-structure-and-tests` | `beb1943` |
| N-3 | **Closed** — the P1 position/environment and direction-count length checks moved into `Dimensions::from_job` (phase 0), so the plan's `structure.environments[position]` read and the sweep's `directions[direction_index]` read cannot panic; the jump-link sweep also uses a checked `directions.get`. Tests cover a truncated `environments`, `positions`, and `directions` vector on KMC and MMC jobs. | Step agent on `fix/step-14-structure-and-tests` | `beb1943` |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Approval summary

The step agent completes this section before a merge, tag, or Blocker/Major waiver request. It must cover the
full history, not only the latest round.

- [x] Requested decision and frozen commit recorded — merge `step-14` at
      `3fd45161d5683405955f806196ca61b295066479` into `main` with `--no-ff` and tag `step-14`.
- [x] Every review/audit round recorded (round, date, commit, reviewer, verdict) — four design-review rounds and
      audit round 1 below; earlier findings and closures are in
      [`docs/planning/reviews/step-14.md`](../reviews/step-14.md).
- [x] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status) — the audit
      findings F-1–F-3/N-1–N-3 are below; the design-review findings F-1–F-43 are closed in the review record,
      with F-39–F-43 corrected in the start-gate/step-page update.
- [x] Accepted notes and waivers recorded with rationale — F-1–F-3/N-1–N-3 are deferred by the human owner to
      `fix/step-14-structure-and-tests` before Step 15; the ENM008 fixture deviation, table-free 10×10×10,
      non-gating AOT, and platform-CI absence are accepted residual limitations.
- [x] Residual risks, deferred items, and follow-up owners recorded — see the audit findings and step page §21;
      the maintenance branch owns F-1–F-3/N-1–N-3, Step 15 owns the delta implementation (F-2), and Step 29 owns
      the mechanical Rust layout check and the same-class benchmark job.
- [x] Explicit no-Blocker/no-Major statement — no Blocker or Major remains in any round; F-1–F-3 are Minor and
      N-1–N-3 are Notes, explicitly accepted by the human owner.
- [x] Final CI and documentation-build results recorded — closeout CI `44922414484` green at `3fd4516`;
      `./tools/verify.sh all --require` exit 0 at `8c09a8c`; documentation strict green at `af434f1`; the Done
      commit re-runs the docs gate and the closeout checker.

Round history:

| Round | Date | Commit | Reviewer | Verdict |
| --- | --- | --- | --- | --- |
| Design review 1 | 2026-11-20 | `7c7ff0d` | DeepSeek V4.1 Flash 1017 (fresh planning auditor) | Changes required |
| Design review 2 | 2026-11-21 | `b6dc503` | Fresh planning auditor | Changes required |
| Design review 3 | 2026-11-21 | `87be9de` | Fresh planning auditor | Changes required |
| Design review 4 | 2026-11-21 | `a430d64` (record `620f20a`) | Fresh planning auditor | Ready with notes — owner accepted 2026-11-21 |
| Audit 1 | 2026-11-21 | `3fd4516` (record `f57314d`) | DeepSeek V4.1 Flash 1017 (fresh) | Passed with notes — owner accepted 2026-11-21 |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| Design F-1…F-43 | Major/Minor/Note | Pool representation, owner-major orientation, memory plan, cluster instance/code contract, fixtures, status/doc drift | Closed across design rounds 2–4; F-39–F-43 corrected in the start-gate/step-page update | `b6dc503`, `87be9de`, `a430d64` | Closed |
| F-1 | Minor | `amend-1-net10` not opened by a Step 14 test | Direct context test with the computed values and tracker counts, tied to the `msl-v1` context | `29cc85e` | Closed |
| F-2 | Minor | Owner-major delta criterion not exercised (Step 15 owns the delta) | Re-pointed to Step 15's test plan; no Step 14 code change | — (Step 15) | Re-pointed |
| F-3 | Minor | `context/mod.rs` and `context/limits.rs` exceed 800 lines | Split into `build.rs`/`invariants.rs` and `limits/tests.rs`; public surface unchanged | `beb1943` | Closed |
| N-1 | Note | Parity `Done` vs dashboard `In progress` | Parity/dashboard wording made consistent; stale status and GUI notes corrected | `fc03ba1` | Closed |
| N-2 | Note | Caller-raised `max_path_nodes > 10` vs fixed scratch | Plan rejects `max_path_nodes > 10` with exit-12 `InvalidInput`; boundary tested | `beb1943` | Closed |
| N-3 | Note | Phase-0 length reads before phase-1 validation | Length checks moved into `Dimensions::from_job`; truncated vectors tested on KMC and MMC | `beb1943` | Closed |

Post-audit maintenance (2026-11-22, `fix/step-14-structure-and-tests`, from `main` `536c8b3`): the five deferred
findings above are closed and F-2 is re-pointed to Step 15. The change is internal-only — no public Rust API,
format, contract, or dependency change, no CI dispatch for a maintenance branch — and the local gates are
recorded in the Step 14 page's maintenance evidence.

## Human approval

- [ ] Accepted: Passed
- [x] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by: Human owner
Date: 2026-11-21

F-1–F-3 and N-1–N-3 are assigned to `fix/step-14-structure-and-tests` before Step 15 starts; no code, test,
configuration, contract, or design change is required before this merge and tag. Accepted residual limitations:
the ENM008 two-member-site fixture deviation, the table-free 10×10×10 real-writer fixture (with the populated
clone as the pair/cluster measurement), the non-gating real-CLI AOT publish failure, and the absence of
Windows/macOS platform runs.

## Audit rounds

Append each new round below. Never overwrite an earlier round.

### Audit round 1

#### Independence and frozen state

I am a fresh context. I did not author, design, implement, review, or coordinate this work and have no shared
conversation with the planner, implementors, design reviewers, or orchestrator. I received only the frozen
commit, the request record, the repository, and the required scope/commands; no desired verdict or summary was
supplied. I read the repository at the frozen commit and ran the checks myself in an isolated worktree
(`/home/node/worktrees/step-14-audit`, detached at `af434f1`). The only file I changed is this record; I did not
fix a finding, edit code/tests/configuration/design docs, merge, tag, push `main`, or delete a branch.

The audited commit is `3fd45161d5683405955f806196ca61b295066479` and the branch head is `af434f1`.
`git diff --name-status 3fd4516..af434f1` is exactly four `docs/planning` files (the audit request, the closeout
evidence, the status row, and the step page), so no code, test, configuration, manifest, signature, or generated
artifact moved after the audited commit. `main` is `929327e660c7eda4cb74e438901277b5fa31aa65`, the recorded
freeze SHA, and is an ancestor of the audited commit via the merge `8c09a8c`; `main` has not moved since the
freeze, so the merge-tree rule needs no additional run. The closeout CI run `44922414484`
(`workflow_dispatch`) is `success` at `head_sha=3fd4516` on the step branch, with .NET, Rust, Contracts and
policy, and Public API green and Python skipped by its path filter.

#### Commands and results

- `./tools/verify.sh all --require` → exit 0 ("Closeout gate completed with required checks enabled."): 2,007
  .NET tests passed; Rust fmt/clippy/test clean, including all ten Step 14 test binaries, the `allocation`
  binary, the `context_fixtures` snapshot, and the `Send`/`!Sync` doctests; Python 3 passed and sdist/wheel
  built; documentation math 194 files, links 197 files, Zensical strict, DocFX 0 warnings/0 errors, rustdoc
  clean; contracts/policy green (26 artifacts, projections 5/4/2, 60 tool tests, SPDX 1,039 files, 1,546 files
  secret-scanned, cargo-deny OK, ApiCompat OK); AOT smoke/positive control pass and the real CLI publish fails
  with 62 (31 `IL2026` + 31 `IL3050`) diagnostics, recorded non-gating for Step 31.
- `cargo build --manifest-path src/rust/Cargo.toml --workspace --release` → exit 0 (the local `all` mode does
  not run the release build).
- `cargo test … --test allocation -- --nocapture` → 98 allocations / 316,872 B against the 308,632 B plan for
  the 10×10×10 ceria fixture; 103 / 11,861 B against 2,672 B for the populated clone; zero allocations in the
  hot accessors and 200 `set_selectable` cycles, with capacity and pointer unchanged.
- `cargo bench … --bench context` → 72.467 µs [72.005, 72.945] and 319.81 µs [313.36, 328.47] on the recorded
  runner class (recorded medians 75.565/315.26 µs), both far inside the `< 100 ms`/`≤ 64 MiB` budgets.
- `python tools/check_closeout_evidence.py docs/planning/closeout/step-14.json` → exit 0, "Closeout evidence:
  OK"; the audited commit, closeout `head_sha`, and merge candidate agree, and `main_sha_at_freeze` is an
  ancestor.
- Public GitHub API: run `44922414484` `conclusion=success` at `3fd4516`; its five required jobs succeeded and
  Python was skipped.
- Fixture SHA-256 lines match the committed `.msl` files; the `step-13-centered` sample and package and
  `contracts/manifest.json` are unchanged; the two new packages are additions only.
- Read every `src/context/*.rs` module and test, the `.NET` fixture builders/tests, the two fixture READMEs and
  summaries, the benchmark evidence, and the updated documentation; checked the D5-2 void-member rule, the
  D2/D3/D4 deviations, the owner-major/inverse orientation, the cluster lookup (no solver symmetry or direct
  `occupation_codes` fallback), the energy convention, goals/checked arithmetic, normalization/`dt`, MMC zero
  time, the tracker index space, and resume validation/`AlreadyCompleted`.

#### Findings

**Minor (3):**

- **F-1** — `amend-1-net10`, named in the first §17 acceptance criterion and the §19 real-package row, is not
  opened by any Step 14 context test; only the Step 13 reader acceptance test loads it. The logically identical
  `msl-v1` package carries the computed-value assertions, so the risk is low, but the ticked claim is not
  literal.
- **F-2** — The asymmetric-table owner-major *delta* criterion (and the §19 "Delta orientation" row) is not
  exercised: the tests assert the owner-major link orientation and the no-synthesised-reverse rule, not a delta
  value. Delta propagation is a documented Step 15 non-goal, so this is a test/claim gap; add a small
  owner-vs-sender delta assertion or move the criterion to Step 15.
- **F-3** — `src/context/mod.rs` (1,502 lines; 1,385 production) and `src/context/limits.rs` (863; 721
  production) exceed the 800-line limit in `AGENTS.md` §8 and `docs/development/code-structure.md` §2.2, which
  the design review recorded as met. The automated structure gate is .NET-only, so nothing fails; split the
  pipeline/invariant sweep and the limits tests before Step 15 grows the context.

**Notes (3):**

- **N-1** — The parity matrix marks Step 14 `Done` while the status dashboard says `In progress` (the audit has
  only just completed); the row notes "audit/merge pending", but the vocabulary is inconsistent.
- **N-2** — A caller-raised `ContextLimits::max_path_nodes` above 10 is accepted while the jump-status scratch
  array is fixed at 10 nodes, silently truncating an 11+-node status instead of failing closed; the reader and
  the default cap stop at 10, so no `.msl` reaches it.
- **N-3** — Phase 0 reads `structure.environments[position]` (and the MMC invariant sweep indexes
  `directions[direction_index]`) before the corresponding length checks, so a hand-built `JobPackage` can panic
  instead of returning `InvalidInput`; reader-built packages always satisfy the invariants.

No Blocker or Major was found. The owner-major link/inverse construction, cluster instance/geometry/code
resolution through `ClusterTable::lookup`, the exact site/pair/cluster energy convention, the checked
goals/block arithmetic, the normalization/`nu_max`/`dof`/`dt` derivation, the MMC zero-time rule, the tracker
index space, the resume validation/`AlreadyCompleted`, the D5-2 void-member rule, the real-writer fixture
provenance/rebuilds, the allocation proof, and the benchmark/closeout evidence all hold as recorded.

#### Verdict

**Passed with notes.** The implementation meets the Step 14 acceptance criteria in substance and the frozen
evidence is consistent, but F-1–F-3 (minor) and N-1–N-3 (notes) remain for the step agent to fix or the human
owner to accept explicitly before the merge and tag. No Blocker or Major finding remains, so a merge request may
be prepared after the resolution/status commit and owner approval; a fresh re-audit is required only if a fix
changes code or tests, as the process states.
