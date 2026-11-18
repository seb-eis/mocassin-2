# Step 13 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-13.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 13 — Rust Solver Foundation and Package Reader |
| Request owner | Planner (design freeze); the step agent takes ownership of resolutions at the start gate |
| Orchestrator | Orchestrator agent (spawns the planning auditor) |
| Step agent (implementor) | Not assigned — created from the reviewed `main` commit after the start-gate approval |
| Review initiated by | Orchestrator, on request of the planner |
| Reviewed commit | `9263949589c449b063e7501f5ab254ed9bf09d7f` |
| Docs branch | `docs/step-13-design` |
| Review date | 2026-11-18 |
| Reviewer | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh context, no shared conversation with the planner, implementor, or orchestrator |

Step page: [`Step 13 — Rust Solver Foundation and Package Reader`](../steps/step-13-rust-solver-foundation-and-package-reader.md)

**Freeze note.** The design docs were committed as the freeze commit; this request record is a
documentation-only follow-up so the freeze SHA can be written. The `Reviewed commit` cell is filled with that
SHA before the orchestrator requests the review. The design freeze includes the step page, the data-contract
and architecture clarifications, the quality-strategy/decision/parity updates, `code-structure.md`, and the
solver/format/CLI reference pages. No code, test, configuration, contract, or generated file is part of the
freeze.

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
| Depends on | 11 (`Done`, merged and tagged; Phase C `XC-04` accepted 2026-11-18, so the gate is open) |
| Delivers | A Rust workspace with typed package structures, a validated read-only `.msl` reader with bounded Zstd/MessagePack decode, canonical parameterization/lattice signature re-encoding, SHA-256 seed derivation with ChaCha12 streams and a 64-byte `RngStateV1`, a `mocassin-sim` CLI skeleton with structured logging and a write-free `--dry-run` inspector, and the first .NET↔Rust contract fixtures. No Monte Carlo physics. |
| Sizing | Owner-approved 2026-11-18 as a single step at **4.5 pw** (3.5 pw base + 1.0 pw contingency) under the §3.1 single-step exception, seven features, **no split**; 13A/13B considered and rejected. Guard: stop before feature 4 if features 1–3 exceed 2.0 pw. |

## Design decisions reviewed

- [x] **Crate boundaries and dependency direction.** `mocassin-types` (payload DTOs + validated runtime types),
      `mocassin-msl` (read-only SQLite/Zstd/MessagePack + signature re-encoding), `mocassin-rng` (only crate
      touching `rand_chacha` state APIs), `mocassin-cli` (inspection path), placeholders for solver/state/
      extension. Verify no cycle, no new external crate, and no second package reader.
      — Reviewed. No cycle in the workspace; the reader/RNG edges are consistent with the existing manifests. The
      CLI→solver edge disagrees across the architecture graph, the step page, and `mocassin-cli/Cargo.toml`
      (m-8). Adding `sha2` to `mocassin-msl` and `mocassin-rng` to `mocassin-cli` is an internal edge, not a new
      external crate.
- [x] **Payload DTO and runtime model contract.** Exact snake_case keys mirror `Mocassin.Contracts.Packages` and
      the generated package schema; typed IDs; `PairLink.pair_id` vs payload `energy_id`; `EnvironmentModel`
      per position and Step 14's supercell CSR ownership; `JobPackage`/`JobSettings`/`InitialLattice`/
      `SolverJobFlags`; no `deny_unknown_fields`.
      — Reviewed. I decoded the real `model-1.bin`, `job-1.bin`, and `lattice-1.bin` payloads: every key, type,
      and optional/null field matches the design tables; `PairLink.energy_id`, `EnvironmentModel` per position,
      and the composed `SolverJobFlags` word (114 = `0x72`) match the writer. The §3.2 cross-check wording is
      ambiguous for `lattice_a/b/c` and the simulation defaults (m-1).
- [x] **Stored encoding vs canonical projection.** `.msl` payload integers use smallest-width MessagePack; ADR
      0010 fixed width applies only to the canonical projection; the v1 lattice payload is an array of `u8` while
      the projection uses `bin`. Verify the contract text and the Rust DTO/decoder tests agree, and that no
      format version changes.
      — Reviewed and independently verified. `job-1.bin` stores `target_mcsp` as `cc 96` and `format_version` as
      `01`; `lattice-1.bin` stores `lattice` as an array16 of fixint `u8` (`dc 03 60`), not `bin`. The writer's
      `LatticePayload` is `ImmutableArray<byte>`; `LatticeSignatureV1` writes `bin32`. No format version or
      fixture changes.
- [x] **`.msl` reader pipeline and limits.** Read-only open, application-id/user-version/format/provenance
      checks, no `integrity_check`, no mirrored `PKG` checks, Zstd declared-size guard, non-allocating
      MessagePack scan, element/depth/per-field caps, typed decode, runtime mapping. Verify the limit defaults
      are consistent with the .NET verifier and the 100 ms load budget.
      — Reviewed. The 512 MiB / 8 MiB / 512 MiB guards match `PackageVerificationOptions` and `PayloadEncoder`;
      `application_id = 0x4D4F4353`, `user_version = 1`, and the nine `schema_info` keys match the fixture. The
      element/position/rule caps are not derived from the .NET limits and the depth justification is
      understated (m-2, m-9, n-6); `models`/`lattices` `format_version` columns are not cross-checked (m-9).
- [x] **Validation completeness.** Particle/position/site/environment/pair/cluster/collection/direction/rule/
      tracker indices and lengths; state-code nodes; `tracker_order` vs collection path length; jump-count prefix
      sums; kind/flag consistency; finite values; normalized field; lattice length/particles. Verify every
      invariant the solver will rely on in Steps 14–18 is checked here or explicitly assigned to a later step.
      — Reviewed; the structural checks match the real payload. **M-2**: mass/charge conservation, the
      `charge_transport_vector` formula, and `path_vector`/`movement` consistency are not checked and are not
      assigned to Step 15/16/18; the `.msl` is untrusted and the parameterization signature does not cover those
      fields.
- [x] **Signature re-encoding.** Forced array32/str32/bin32/uint64/float64 markers, the C# 12-decimal
      quantization expression, domain prefixes, six-field parameterization projection, five-field lattice
      projection, `model_signature` as identity only. Verify the Rust `.bin` vector round-trip proves the
      encoder and that a mutated value is rejected.
      — Reviewed and independently reproduced in Python from the raw payloads: the six-field parameterization
      projection hashes to the stored `1845b70e…`, and the five-field lattice projection hashes to job 1's
      `880b6eac…`. The C# `CanonicalProjectionWriter`, `ParameterizationSignatureV1`, and `LatticeSignatureV1`
      match §3.6 exactly (array32/str32/bin32, `0xcf` uint64, `0xcb` float64, 12-decimal quantization, domain +
      `0x00`).
- [x] **RNG derivation, draws, and state.** Step 10 byte order and domain strings; `rand_chacha` public state
      APIs; `next_u32`/`next_u64`/`next_f64`/`next_bounded`/`fork` semantics; the 64-byte `RngStateV1` layout;
      mid-block resume; one simulation stream across pre-run/run; extension forks. Verify ADR 0005 stands, no
      new ADR is required, and the format-breaking rule is explicit.
      — Reviewed. `JobSeedDeriver` and `LatticeSeedDeriver` match the byte order/domain strings;
      `rand_chacha 0.10.0` exposes `get_seed`, `get_stream`, `set_stream`, `get_word_pos`, and `set_word_pos`,
      and `set_word_pos` + `reset_and_skip` gives exact 32-bit-word resume (verified from the crate source). The
      64-byte layout is precise; the `fork` width/config-hash details and the “format-breaking” scope need
      tightening (m-3, n-1, n-2).
- [x] **RNG performance obligation.** Draws/s, bounded calls/s, refill cost, state round-trip, and fork budgets;
      the Criterion location and evidence file; the 10 % regression rule; no algorithm change to meet a budget.
      — Reviewed; the absolute budgets are realistic. A standalone `rand_chacha 0.10.0` release loop on this
      container (AMD Ryzen 7 7840U) measured ~569 M `next_u32`/s and ~303 M `next_f64`/s, above the 250 M/120 M
      budgets. The baseline/10 % rule is not tied to the per-runner-class `tests/reference/bench/baselines.json`
      registry (m-4).
- [x] **CLI scaffold and exit codes.** `--dry-run` is write-free and required in Step 13; `--stdout` conflict;
      `--seed` override and seed hash; validated-but-unused `--threads`/`--jump-log-max-ev`; `--extension`
      rejected; parse-error remap to 10; package 11, model/data 12, I/O 16; no `--json`/batch/validate-only/
      no-state/log-file until Step 19. Verify the dry-run format is reproducible and comparable to
      `inspect-v1`/`package-summary-v1`.
      — Reviewed. The dry-run example is reproducible from the fixture: job 1 is 6×6×6 (length 864), particles 5,
      positions 4, sites 4, environments 4, pair/cluster tables 0, collections 1, directions 2, rules 4,
      trackers global 2 / static_per_cell 16, target 150, pre-run 250, field 1e7, flags 114 (`0x72`), and
      `package-summary-v1.json` agrees. **M-1**: the solver exit table contradicts the published CLI/architecture
      table (13/14) and a caught panic is mapped to 10 (invalid arguments).
- [x] **Data-oriented base structures.** Lattice indexing, positions, environment definitions, flat energy
      tables, `DirectionPool` swap-remove, jump-status container; no `HashMap` in hot structures; Step 14/15/18
      ownership. Verify Step 13 does not slide into context initialization or physics.
      — Reviewed. `DirectionPool`, `JumpStatus`, and the flat tables are containers only; population, supercell
      expansion, links, energy runtime, and the no-allocation proof stay with Step 14/15; no physics is added.
- [x] **Cross-language fixtures and tests.** `msl-v1` and `amend-1-net10` reads, raw payload decode, projection
      `.bin` round-trip, `rng-vectors.json`, dry-run golden, property/mutation tests, fuzz targets and the
      time-boxed run, load/RNG benchmarks, tiers, and real-file policy.
      — Reviewed; the fixture inventory exists (`msl-v1` + `amend-1-net10`, 4 parameterization `.bin`, 2 lattice
      `.bin`, raw payloads). Both real `.msl` fixtures have zero pair/cluster tables and only KMC jobs, so the
      populated-parameterization and MMC payload→signature paths are not exercised end-to-end (m-5). The
      closeout file/checker is not named (m-10).
- [x] **Documentation, parity, and audit plan.** New `solver-architecture.md`; updates to `msl-format.md`,
      `cli.md`, `testing-and-parity.md`, `code-structure.md`; rustdoc with `missing_docs`; parity/status/
      closeout/audit sequence; nav entry owned by the implementor.
      — Reviewed; the pages exist and `./tools/verify.sh docs --require` passes. The dashboard is stale (m-7),
      the closeout file/checker is not named (m-10), and `03-quality-strategy.md` points at `04-roadmap.md` for
      the RNG budgets (n-7).
- [x] **Sizing and feature list.** Seven features, 3.0–4.0 pw, 1.0 pw contingency, §3.1 check, no split, the
      "stop if features 1–3 exceed 2.0 pw" guard, and the owner approval at the start gate.
      — Reviewed. Seven features is inside the 6–8 range; the roadmap row is 2–4 pw. The feature breakdown sums
      to 3.5 pw and the claimed 1.0 pw contingency would make 4.5 pw, beyond the four-week trigger (m-6).

## Dependency and interface checks

- [x] Step 11 is `Done`, merged and tagged, and its `.msl` v1 schema/payload/fixture contract is stable. Verify
      the schema columns, `application_id`/`user_version`, payload keys, `msl-v1` package, and
      `package-summary-v1` used by the design.
      — Verified. `msl-v1` is 98,304 bytes, `application_id = 0x4D4F4353`, `user_version = 1`, one model row,
      six lattice rows, six job rows, 32-byte `jobs.seed`, no `results` table; `package-summary-v1.json` counts
      and signatures match the payloads. The `amend-1-net10` fixture is byte-fixed and logically equal.
- [x] Step 10's job/lattice seed derivation byte order and domain strings match the Rust design.
      — Verified. `JobSeedDeriver` hashes `master[32] || u32_le(collection) || u32_le(config) || u32_le(job) ||
      "mocassin/job/v1"`; `LatticeSeedDeriver` hashes `"mocassin/lattice/v1" || job_seed[32] ||
      request_hash[32]`. The Rust §4.1 strings and byte order match.
- [x] Step 09/08 canonical projection layouts and `.bin` vectors match the Rust writer/reader design.
      — Verified against the C# writer and the checked-in `.bin` vectors: the six-field parameterization and
      five-field lattice layouts, forced headers, and quantization match; my independent re-encode of the raw
      payloads reproduced `1845b70e…` and `880b6eac…`.
- [x] Step 12's solver exit-code table (0, 10–16) and `mocassin simulate` reservation match the CLI design.
      — **Not satisfied.** The step page §3.7 redefines 13 (RNG state/internal invariant), omits 14, and maps a
      caught panic to 10, while `cli.md`, `01-target-architecture.md` §7.3, and `solver-architecture.md` define
      13 numerical failure, 14 limit abort, 15 cancelled, and 10 invalid arguments (M-1).
- [x] Step 14 can consume `JobPackage`, `ParameterizedModel`, `InitialLattice`, `EnvironmentModel`, `Rng`, and
      `DirectionPool` without re-reading the package, and Step 13 assigns supercell expansion/links/pools/jump
      status/energy runtime to Step 14.
      — Reviewed; the types and ownership split are sufficient. The assignment omits the scientific-invariant
      checks that Step 14–18 will rely on (M-2).
- [x] Step 18/20 can consume the 64-byte `RngStateV1` and `seed_hash`; `.mcs` `rngAlgorithm`/`rngState` agree
      with the data contract.
      — Reviewed; the data contract §4.3/§4.5 now binds `rngState` to 64 bytes, `rngAlgorithm = "chacha12-v1"`,
      and the reader rule. The state layout is a new persisted format with no new ADR (n-1); the design says the
      data contract is the decision note.
- [x] No unresolved cross-step interface question remains. The deliberate asymmetries (Rust never writes `.msl`;
      no `integrity_check` in the solver; no physics in Step 13) are stated in the contract and tests.
      — **Not satisfied.** M-2 leaves the conservation/transport-vector validation unassigned; m-1 leaves the
      column cross-check wording ambiguous; m-8 leaves the CLI→solver crate edge contradictory.

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable
      — The scope and non-goals are unusually clear; the acceptance criteria are testable against the real
      fixture. M-2 and m-5 leave two validation/test obligations implicit.
- [x] Design decisions are complete and internally consistent
      — Mostly; M-1 (exit codes), M-2 (validation assignment), m-1 (cross-check), and m-6 (sizing) are the
      inconsistencies.
- [x] Required tests, property tests, golden fixtures, and performance targets are defined
      — The test table, tiers, property/mutation plan, fuzz targets, and budgets are defined; m-5 (populated
      parameterization/MMC fixture), m-4 (baseline registry), and m-10 (closeout file/checker) are the gaps.
- [x] Documentation, status, parity, and audit updates are planned
      — The pages and parity plan are complete; the dashboard is stale (m-7) and the closeout file/checker is not
      named (m-10).
- [x] Licensing, provenance, and dependency checks are resolved
      — `cargo-deny` passes; no new production external crate; `rand_chacha`/`sha2`/`rusqlite`/`zstd`/`clap` are
      already in the workspace and licence-checked; no GPL code path; the spglib catalog provenance is untouched.
- [x] Risks and mitigations are realistic
      — The RNG budget is achievable (measured 569 M/303 M draws/s here) and the reader's fuzz/mutation plan is
      reasonable; the “large package inside 100 ms” wording overclaims (n-8).
- [x] Code layout and namespace plan follow the structure policy
      — The Rust layout rules are documented and consistent with the C# structure page; the CLI→solver edge
      contradicts the architecture graph (m-8).
- [x] No hidden scope, stale placeholders, or unowned decisions
      — No Phase D/E/F scope is hidden; the placeholders are explicit. The dashboard says Step 13 is “Not
      started”/design review “Not requested”, and the Active-work paragraph still describes an XC-04 remediation
      gate that the step page says is open (m-7).

## Evidence

Files reviewed:

- `AGENTS.md` §3.8–§3.11, §8–§12; `docs/planning/reviews/README.md`; `docs/development/orchestration.md`.
- `docs/planning/reviews/step-13.md` (request); `docs/planning/steps/step-13-rust-solver-foundation-and-package-reader.md`;
  Step 10, 11, 12, 14, 18, and 20 pages where the design names an interface.
- `docs/planning/02-data-contracts.md` §1.3, §3.2–§3.5, §3.4 (solver bounds), §4.1–§4.5, §5.2, §6;
  `docs/planning/01-target-architecture.md` §3, §5.2, §7.3, §7.5, §7.6; `docs/planning/03-quality-strategy.md`
  §5, §8, §9.11, §10, §13; `docs/planning/04-roadmap.md` §3, §3.1; `docs/planning/05-open-decisions.md`;
  `docs/planning/appendix-b-parity-matrix.md`; `docs/planning/status.md`.
- ADR 0002, 0004, 0005, 0010, 0011, 0013, 0014; `docs/development/code-structure.md` §2.2;
  `docs/development/solver-architecture.md`; `docs/development/testing-and-parity.md`;
  `docs/reference/msl-format.md`; `docs/reference/cli.md`; `docs/development/index.md`; `docs/reference/index.md`.
- `src/rust/Cargo.toml`, `Cargo.lock`, each crate `Cargo.toml`, and the placeholder `lib.rs`/`main.rs` files.
- `src/dotnet/Mocassin.Contracts/Packages/`: `LatticePayload.cs`, `LatticeSignatureV1.cs`,
  `ModelPayloadDtos.cs`, `StructurePayloadDtos.cs`, `EnergyPayloadDtos.cs`, `TransitionPayloadDtos.cs`,
  `JobPayloadDtos.cs`, `SimulationPayloadDtos.cs`; `src/dotnet/Mocassin.Contracts/Parameterization/`:
  `ParameterizationSignatureV1.cs`; `src/dotnet/Mocassin.Compilation/Signatures/CanonicalProjectionWriter.cs`,
  `.../Transitions/TransitionRuleHelpers.cs`, `.../Transitions/TransitionCompilationStage.Rules.cs`;
  `src/dotnet/Mocassin.Packages/SimulationPackage/`: `SimulationPackageWriter.cs`, `Payloads/PayloadEncoder.cs`,
  `Payloads/LatticePayloadMapper.cs`, `Payloads/StructurePayloadMapper.cs`, `Payloads/EnergyPayloadMapper.cs`;
  `src/dotnet/Mocassin.Packages/Jobs/`: `JobSeedDeriver.cs`, `LatticeSeedDeriver.cs`,
  `SolverJobFlagsMapper.cs`, `SupercellGenerator.cs`; `src/dotnet/Mocassin.Symmetry/Geometry/UnitCellVectorEncoder.cs`;
  `src/dotnet/Mocassin.Compilation/Compiler/CompilationOptions.cs`; `src/dotnet/Mocassin.Results/Packages/`:
  `PackageInspection.cs`, `Internal/PackageReader.cs`, `Internal/PackageVerificationChecks.cs`.
- `tests/contracts/packages/msl-v1/` and `amend-1-net10/` (README, `.msl`, summaries, raw payloads);
  `tests/contracts/projections/parameterization/*.bin` and `lattice/*.bin`; `tests/contracts/signature-vectors.json`;
  `tests/reference/bench/baselines.json`; `.gitignore`; `tools/verify.sh`.
- `/workspace/mocassin-original` read-only: `Simulator/Data/Jobs/JobDbModel.h`,
  `Libraries/JobLoader/Sqlite3JobLoader.c`, `Libraries/Simulator/Logic/Routines/JumpSelectionRoutines.c`.

Commands run:

- `git rev-parse HEAD` → `306238ae2c437b44b3da94790c176905cc5d3ce9`; `git diff --name-status
  b2e4b4fdf141f28411c0d51878f12c26fe1ac09b 306238ae2c437b44b3da94790c176905cc5d3ce9` → only
  `A docs/planning/reviews/step-13.md`. The reviewed design tree is the freeze `b2e4b4f` plus the request record;
  no code, test, configuration, or design doc changed between them.
- `./tools/verify.sh docs --require` → exit 0. Math check 171 files OK (the separate `check_doc_math.py` below);
  link check OK; Zensical strict build `No issues found`; DocFX 0 warnings / 0 errors; `cargo doc --workspace
  --no-deps` succeeded.
- `./tools/verify.sh policy --require` → exit 0. NuGet licence policy and vulnerability audit OK; ApiCompat no
  breaking changes for every project; contract artifacts current (26 files); model projection 5/5,
  parameterization projection 4/4, lattice projection 2/2; space-group catalog current (`1f3bf67d…`);
  process consistency OK; 58 tool tests OK; SPDX headers in 706 files; no secret patterns in 1,110 files;
  `cargo-deny` advisories/bans/licenses/sources OK.
- `python tools/check_doc_math.py` → exit 0, 171 files scanned, no delimiter warnings.
- Read-only SQLite queries against `tests/contracts/packages/msl-v1/ceria-temperature-sweep.msl`:
  `PRAGMA application_id` = `1297040211` (`0x4D4F4353`), `PRAGMA user_version` = 1; `schema_info` has the nine
  keys; `jobs` has 6 rows with 32-byte `seed` and the documented columns; `models`/`lattices` have the
  documented columns and signatures.
- Independent MessagePack decode of `payloads/model-1.bin`, `job-1.bin`, and `lattice-*.bin` (small Python
  reader): model keys/types match §2.1; `jump_count` length 20, `sum = 4 = jump_mapping.len()`;
  `static_tracker_mapping` length 20, values `-1`/0–15; `global_tracker_mapping` length 5
  (`collection_count × particle_count`); `tracker_order` `[1, 255, 0]`; `interaction_range` `[2,2,2,0]`;
  lattice `[6,6,6]`/`[8,8,8]` with the expected particle lengths.
- Independent canonical-projection re-encode in Python from the raw payload: the six-field parameterization
  projection (array32/6, uint64 1, str32, nested tables, site energies, rule frequencies; 12-decimal
  quantization) hashes to the stored `1845b70e82320d75f768b33d1a9a46a8ae6170c33ad96013317f89fe9189a74b`;
  the five-field lattice projection (array32/5, uint64 1, str32 `model_signature`, array32/3 uint64 sizes,
  uint64 `position_count`, bin32 particles) hashes to `880b6eac79e9e920b6ea448c15f21b06be772f4bad774f12ea45c07420d25845`
  for job 1. This proves §3.6 is implementable from the stored payload.
- Standalone `rand_chacha 0.10.0` release benchmark in `/tmp/opencode` (outside the repository):
  `next_u32` ≈ 569 M/s, `next_f64` ≈ 303 M/s on the container's AMD Ryzen 7 7840U; above the §4.5 budgets.
- Read-only source inspection of `rand_chacha-0.10.0`: `get_seed`, `get_stream`, `set_stream`, `get_word_pos`,
  `set_word_pos` are public; `set_word_pos` uses `reset_and_skip`, so mid-block resume is exact.
- `git check-ignore -v tests/contracts/packages/msl-v1/ceria-temperature-sweep.msl` → not ignored (the
  `!tests/contracts/**/*.msl` exception is present).
- Read-only original-reference spot checks: `JumpSelectionRoutines.c` uses `1 << environment->ParticleId`
  (32-bit mask), and `Sqlite3JobLoader.c` divides by `sizeof(MoveSequence_t)` (a 16-byte span, not the 24-byte
  `Vector3_t` entry), matching the step page's “Original reference” claims.

No repository file was modified by these checks; the only write is this record. The `site/`, `target/`, and
`artifacts/` outputs are ignored build products.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M-1 — The solver exit-code table contradicts the published CLI/architecture contract, and a caught panic is mapped to the invalid-arguments code.** Step 13 §3.7 maps `13` to “RNG state/internal invariant failure”, omits `14`, and §5 maps a caught panic to `10`. The published tables define `10` = invalid arguments/unknown job/unsupported option/extension, `13` = numerical failure, `14` = aborted by a limit, `15` = cancelled, `16` = I/O. The same freeze therefore gives `13` two meanings and `10` two meanings. A GUI/HPC consumer cannot map the code reliably. | Step page lines 760–768 and 930; `docs/reference/cli.md` “Solver exit codes” lines 468–481; `01-target-architecture.md` §7.3 lines 375–377; `docs/development/solver-architecture.md` lines 168–170; `02-data-contracts.md` §5.2 points at the architecture table. |
| **Major** | **M-2 — The reader does not validate the scientific invariants the solver relies on, and the design does not assign them to a later step.** §3.5 checks every index/length/finite/kind/tracker value but not mass/charge conservation per rule, the `charge_transport_vector` formula, or `path_vector`/`movement` against the cell vectors. The `.NET` compiler checks mass/charge in `TransitionRuleHelpers`, and the `.NET` verifier checks only that the transport vector has three components (plus that the frequency is finite). The `.msl` is untrusted input (AGENTS §11); the parameterization signature covers only energies/frequencies and `model_signature`, so a corrupted `state0`/`state2`, `movement`, `path_vector`, or `charge_transport_vector` passes signature and structural validation. The request’s “Validation completeness” item is not met: §3.5 and §12 do not assign these checks to Step 15/16/18. | Step page §3.5 lines 625–645; `src/dotnet/Mocassin.Compilation/Transitions/TransitionRuleHelpers.cs` `MassConserved`/`ChargeConserved` lines 22–55 and `TransitionCompilationStage.Rules.cs` lines 34–40; `src/dotnet/Mocassin.Results/Packages/Internal/PackageVerificationChecks.cs` lines 240–250; `ParameterizationSignatureV1.WriteProjection` lines 76–110; AGENTS §10/§11; request “Validation completeness”. |
| **Minor** | **m-1 — The column/payload cross-check wording is ambiguous and wrong for `lattice_a/b/c`.** `lattice_a/b/c` are derived from the lattice payload, not the job payload; and a job’s resolved settings need not equal the referenced `simulations[].default_settings`. The fixture shows job 1 `target_mcsp = 150` while simulation 0’s default is 200, so an implementer reading “job payload/model values” as the simulation defaults would reject the only acceptance fixture. | Step page lines 521–524 and 665–670; `tests/contracts/packages/msl-v1/payloads/job-1.bin`; `model-1.bin` simulation 0 `default_settings.target_mcsp = 200`; `package-summary-v1.json` job 1 `targetMcsp = 150`. |
| **Minor** | **m-2 — The reader limits are not derived from the .NET build/verifier limits.** `max_positions` 100,000,000 is 200× `SupercellGenerator.MaximumPositions` (500,000); `max_position_count` 1,000,000 is 100× `UnitCellVectorEncoder.MaxP1Positions` (10,000); `max_rules` 10,000,000 is 10× `CompilationOptions.MaxRuleCount` (1,000,000). Looser caps do not reject a valid .NET package, but the design calls 100M the “documented solver cap” with no source; align or cite the .NET limits. | Step page lines 540–546; `src/dotnet/Mocassin.Packages/Jobs/SupercellGenerator.cs:41`; `src/dotnet/Mocassin.Symmetry/Geometry/UnitCellVectorEncoder.cs:35`; `src/dotnet/Mocassin.Compilation/Compiler/CompilationOptions.cs:36`. |
| **Minor** | **m-3 — The `fork` derivation is under-specified for implementation.** It hashes `stream_le || word_pos_le` without stating the 8-byte/16-byte widths, and the extension `config_hash` type is unspecified; `next_bounded`’s half-open `[0, upper)` range is implied by “`upper == 0` returns 0” rather than stated. The golden vectors would pin the implementer’s choice, but the format is described as “part of the format”. | Step page lines 826–834 and 865; `RngStateV1` lines 843–847 (64-bit stream, 128-bit word position). |
| **Minor** | **m-4 — The RNG baseline/10 % rule is not tied to the per-runner-class baseline registry.** The design records `step-13-rng.json` but does not add a same-class entry to `tests/reference/bench/baselines.json`; the quality strategy requires per-class baselines and forbids cross-machine comparisons. Step 29 can add the registry entry, but the step’s “recorded baseline” should say where it lives. | Step page lines 890–894; `03-quality-strategy.md` §13 (“Baselines are stored per runner class … in `tests/reference/bench/baselines.json`; comparisons use the same class only”). |
| **Minor** | **m-5 — The real `.msl` fixtures do not exercise populated pair/cluster tables or MMC.** Both `msl-v1` and `amend-1-net10` have `pairTableCount = 0`, `clusterTableCount = 0`, and all six jobs are `kmc`. The payload→signature path for populated parameterizations and the MMC validation path are covered only by projection-only `.bin` vectors and synthetic unit tests. | `tests/contracts/packages/{msl-v1,amend-1-net10}/package-summary-v1.json`; `tests/contracts/projections/parameterization/*.bin`; step page lines 1023–1029. |
| **Minor** | **m-6 — The sizing arithmetic contradicts the four-week claim.** The feature breakdown sums to 3.5 pw; “~0.5 pw integration/contingency (1.0 pw total contingency)” gives 4.0, but a true 1.0 pw contingency on 3.5 pw is 4.5 pw, beyond the four-week trigger. The page and `05-open-decisions.md` ask the owner to approve “3.0–4.0 pw with 1.0 pw contingency”. | Step page lines 1095–1103; `04-roadmap.md` §3.1; `05-open-decisions.md` Step 13 sizing row. |
| **Minor** | **m-7 — The status dashboard is stale.** `status.md` lists Step 13 as “Not started” with design review “Not requested”; the Active-work paragraph still says the XC-04 remediation branch must merge before the gate opens to Step 13, while the step page says “Ready for review” and the gate is open, and the XC-04 remediation table records F1–F5 fixed at `73bb608`. This is hygiene, not a design defect, but the freeze should not leave two status statements. | `docs/planning/status.md:63` and `:85–90`; step page lines 11–21; `docs/planning/reviews/cross-cutting/xc-04-phase-c.md` remediation table lines 832–836. |
| **Minor** | **m-8 — The crate graph contradicts the step page and the existing manifest.** `01-target-architecture.md` shows `mocassin-cli └──► mocassin-solver (placeholder in Step 13)`; the step page says the CLI “never reaches into `mocassin-solver` for the inspection path”; `solver-architecture.md` has no CLI→solver edge; the actual `mocassin-cli/Cargo.toml` already declares `mocassin-solver`. Decide whether the edge stays (and say why) or is removed. | `01-target-architecture.md` crate graph; step page lines 153–156; `docs/development/solver-architecture.md` crate graph; `src/rust/crates/mocassin-cli/Cargo.toml`. |
| **Minor** | **m-9 — `models.format_version` and `lattices.format_version` are read but never cross-checked against the payload.** The design’s cross-check list covers the job columns and the lattice size, but not the two version columns; a mismatched column would pass. | Step page lines 521–524 and 665–670; `.msl` schema `models`/`lattices` columns; `PackageVerificationChecks` version handling. |
| **Minor** | **m-10 — The closeout plan does not name the closeout evidence file or its checker.** The step page says “closeout evidence” generically; the process requires `docs/planning/closeout/step-13.json` and `python tools/check_closeout_evidence.py` with the audited commit, closeout CI `head_sha`, `main` SHA at freeze, and merge candidate. | Step page §8 and task line 1147; AGENTS §3.4/§3.10; `docs/planning/closeout/README.md`; `tools/check_closeout_evidence.py`. |
| **Note** | **n-1 — The `.mcs` RNG state is a new persisted format with “no new ADR”.** `02-data-contracts.md` §4.3/§4.5 is an explicit decision note, which AGENTS §11 permits, but the owner should confirm that a 64-byte `rngState` layout change is a data-contract decision rather than an ADR-worthy format change. | Step page lines 855–858 and 1120–1122; `02-data-contracts.md` lines 1361–1362 and 1432; ADR 0005. |
| **Note** | **n-2 — “Format-breaking” conflates seed derivation with the algorithm id.** A derivation change alters reproducibility but not the `rngAlgorithm` draw semantics or the 64-byte state layout; state the exact trigger for a new algorithm id versus a new ADR. | Step page lines 855–858; ADR 0005. |
| **Note** | **n-3 — `RngStateV1` carries the raw 32-byte key/seed.** The “raw seed is never logged” statement is correct for logs, but state files carry the key by design; the design should say so explicitly so a future `.mcs` consumer does not treat it as a leak. | Step page lines 792–795 and 843–847; `.mcs` `rngState`. |
| **Note** | **n-4 — `docs/reference/msl-format.md` omits `signature` from the model-payload row.** The payload has both `signature` (parameterization) and `model_signature`; the row lists only the latter. Pre-existing text, not introduced by this freeze, but the page is part of the Step 13 doc update. | `docs/reference/msl-format.md:130`; `ModelPayloadDto` keys. |
| **Note** | **n-5 — The dry-run “settings” line is job settings, not simulation defaults.** The fixture’s simulation 0 default `target_mcsp` is 200 while job 1 is 150; the design should state that the line prints the resolved job settings (and the composed flags), so the difference is not read as a bug. | Step page line 943; `model-1.bin`; `job-1.bin`. |
| **Note** | **n-6 — The depth-limit justification is understated.** The model payload nests map→structure→positions→position→particle_ids→scalar, at least five container levels, not “four”. The cap 8 still passes. | Step page line 539; `StructurePayloadDtos.cs`; `model-1.bin`. |
| **Note** | **n-7 — `03-quality-strategy.md` points at the wrong document for the RNG budgets.** It says “the RNG budgets in `04-roadmap.md`/the step page”; `04-roadmap.md` has only step estimates. The budgets are in `03-quality-strategy.md` §13 and the step page. | `03-quality-strategy.md:565`; `04-roadmap.md` §3/§3.1. |
| **Note** | **n-8 — The “large package inside the 100 ms budget” sentence overclaims.** §3.1 step 8 says skipping `integrity_check` keeps “a large package’s load” inside 100 ms; §10 defines the 100 ms budget for the ceria fixture only, and a 512 MiB payload cannot load in 100 ms. Reword to “keeps the normal path within the fixture budget”. | Step page lines 492–494 and 1042–1053. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Changes required

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

Follow-up owner: planner/step agent — resolve M-1 and M-2 and either fix or explicitly accept the Minors before
the start gate.
Follow-up step: 13 — design re-review at the new freeze commit; earlier rounds stay in this record.

## Approval summary

The request owner (step agent or planner) completes this section before the orchestrator presents the approval
request. It must cover the full history, not only the latest round.

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

- [ ] Accepted: Ready for implementation
- [ ] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by:
Date:

## Resolution status

Round-1 reviewer text, findings, evidence, and verdict are unchanged. The design was corrected on
`docs/step-13-design` at freeze `9263949589c449b063e7501f5ab254ed9bf09d7f`; this request-record update is a
documentation-only follow-up. No code, test, configuration, contract, or generated artifact changed.

| Finding | Status | Owner | Commit | Resolution |
| --- | --- | --- | --- | --- |
| **M-1** | Fixed | Planner | `113b546` | One canonical solver table in `01-target-architecture.md` §7.3: 0 completed, 10 invalid arguments, 11 package, 12 model/data, 13 numerical failure **or internal invariant violation (caught panic)**, 14 limit abort, 15 cancelled, 16 I/O. `cli.md`, `solver-architecture.md`, `02-data-contracts.md`, Step 19, and the Step 13 page mirror it; panic now exits 13, not 10. Feature 5 adds a doc-drift check for the rows. |
| **M-2** | Fixed + assigned | Planner | `113b546` | Reader now checks allowed occupations per path node (state1 skipped for MMC), per-rule non-void symbol multiset and charge conservation (`1e-9`), `path_vector`/`length` against `movement` and the cell vectors, and zero transport when no node changes charge. The full positional `Σ Δq x` formula is assigned to Step 15 in the Step 15 page with the trust-boundary rationale (v1 payload has no fractional P1 coordinates); the residual risk is recorded. A populated/MMC fixture exercises the paths. |
| **m-1** | Fixed | Planner | `113b546` | Cross-check now separates job columns vs job payload, `lattice_a/b/c` vs lattice payload, `models`/`lattices` `format_version` vs their payloads. A job's resolved settings are explicitly not compared with `simulations[].default_settings` (fixture 150 vs default 200); the data contract says the same. |
| **m-2** | Fixed | Planner | `113b546` | Limits aligned with the .NET build: 500,000 positions (`SupercellGenerator.MaximumPositions`), 10,000 P1 positions (`UnitCellVectorEncoder.MaxP1Positions`), 1M rules (`CompilationOptions.MaxRuleCount`); directions 1M. A future writer that raises a limit must raise `ReaderLimits` in the same release. |
| **m-3** | Fixed | Planner | `113b546` | `fork` now states `stream_le64` (8 bytes) and `word_pos_le128` (16 bytes), domain 1..255 bytes, and `next_bounded` returns half-open `[0, upper)`; the extension `config_hash` is the 32-byte SHA-256 of UTF-8 `config_json`. |
| **m-4** | Fixed | Planner | `113b546` | The RNG/load baseline is a same-runner-class entry in `tests/reference/bench/baselines.json`; the 10 % rule is measured only against that class. Step 13 records the first entry; Step 29 automates the regression. |
| **m-5** | Fixed | Planner | `113b546` | New real-writer `tests/contracts/packages/step-13-populated/` package with non-empty pair/cluster tables and an MMC job, canonical summary, extracted payloads, README, and regeneration command. It is a test artifact, not a schema change; feature 6 builds it from a committed sample. |
| **m-6** | Fixed (owner-approved) | Planner | `11f089f` | Honest arithmetic: 3.5 pw base + 1.0 pw contingency = 4.5 pw, beyond the four-week trigger. The no-split claim is withdrawn. Owner approved 2026-11-18: single step at 4.5 pw as a §3.1 exception, no split; 13A/13B considered and rejected; guard: stop before feature 4 if features 1–3 exceed 2.0 pw. Recorded in `04-roadmap.md`, `05-open-decisions.md`, and step page §13. |
| **m-7** | Deferred | Step agent | — | Status-dashboard drift is step-agent-owned per AGENTS §3.8; the planner did not edit `status.md`. It is fixed at the start gate when the step branch is created. |
| **m-8** | Fixed | Planner | `113b546` | The existing `mocassin-cli → mocassin-solver` edge stays for the future run path and is shown in the architecture/step page/solver-architecture graphs; Step 13's inspection path calls `mocassin-msl`/`mocassin-rng` directly and does not call the placeholder solver. |
| **m-9** | Fixed | Planner | `113b546` | `models.format_version` and `lattices.format_version` are cross-checked against their payloads; the job payload version is checked on decode. A schema/version test covers a mismatched column. |
| **m-10** | Fixed | Planner | `113b546` | Closeout plan names `docs/planning/closeout/step-13.json` and `python tools/check_closeout_evidence.py`, binding the audited commit, closeout CI `head_sha`, `main` SHA at freeze, and merge candidate, in §8, the tasks, and the acceptance criteria. |
| **n-1** | Resolved | Human owner / planner | `11f089f` | Owner decision 2026-11-18: **data-contract note only, no ADR**. The 64-byte `rngState` layout, the `chacha12-v1` binding, and the state/layout-change-is-format-breaking rule live in `02-data-contracts.md` §4.3/§4.5; no schema bump. The ADR option is removed from the step page and the gated register. |
| **n-2** | Fixed | Planner | `113b546` | Format-breaking scope split: draw semantics/state layout/algorithm id = new id + ADR; seed/fork derivation = reproducibility-contract change (ADR/version note) without a state-layout change. |
| **n-3** | Fixed | Planner | `113b546` | `RngStateV1` explicitly carries the raw 32-byte key by design; `.mcs` is seed-sensitive, never log `rngState`/key, expose only `seedHash`. Recorded in the step page and data contract. |
| **n-4** | Fixed | Planner | `113b546` | `docs/reference/msl-format.md` model row now lists both `signature` (parameterization) and `model_signature` (compiled identity). |
| **n-5** | Fixed | Planner | `113b546` | Dry-run `settings:` line is documented as resolved job settings/composed flags, not simulation defaults; the fixture 150 vs 200 example is recorded in the step page and `cli.md`. |
| **n-6** | Fixed | Planner | `113b546` | Depth justification corrected: the model nests five container levels; cap 8 leaves headroom. |
| **n-7** | Fixed | Planner | `113b546` | `03-quality-strategy.md` no longer points at `04-roadmap.md` for RNG budgets; it points at §13 and the step page. |
| **n-8** | Fixed | Planner | `113b546` | The 100 ms claim is scoped to the ceria fixture; a large payload is guard-bounded but not promised at 100 ms. |

The step agent fills any further status after the next verdict. Do not alter the reviewer-authored findings,
evidence, or verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

I am a fresh context. I did not author, design, or implement the reviewed work and have no shared conversation
with the planner, implementor, or orchestrator. I read the repository at
`HEAD = 306238ae2c437b44b3da94790c176905cc5d3ce9` and confirmed with
`git diff --name-status b2e4b4fdf141f28411c0d51878f12c26fe1ac09b HEAD` that the only difference from the frozen
design commit is the added request record `docs/planning/reviews/step-13.md`. No code, test, configuration, or
design doc changed between the freeze and the request. The request commit is documentation-only; this round's
record commit is its child and the reviewed commit is its parent, as recorded in the request.

#### Commands and results

- `./tools/verify.sh docs --require` — pass (math/link checks, Zensical strict build, DocFX, rustdoc).
- `./tools/verify.sh policy --require` — pass (ApiCompat, contract artifacts, projections, catalog, process
  consistency, tool tests, SPDX, secret scan, `cargo-deny`).
- `python tools/check_doc_math.py` — pass, 171 files.
- Independent read-only verification of the raw `.msl` payloads and the SQLite rows; an independent Python
  re-encode of both canonical projections reproduced the stored signatures; a standalone `rand_chacha 0.10.0`
  release benchmark measured ~569 M `next_u32`/s and ~303 M `next_f64`/s.
- Read-only source inspection of the .NET writer/verifier, the Rust skeleton, `rand_chacha`, and the original
  reference. No repository file was modified; the only write is this record.

The headline claims hold: the v1 lattice payload really is a MessagePack array of `u8` (not `bin`); stored
payload integers really use smallest-width encoding; the canonical writer uses the forced fixed widths; the
six-field parameterization and five-field lattice projections re-encode byte-for-byte from the real payloads;
and the `RngStateV1` 64-byte key/stream/word-position layout is implementable through the public `rand_chacha`
APIs with exact mid-block resume.

#### Findings

M-1 (exit codes) and M-2 (scientific-invariant validation) are Majors and must be corrected before
implementation. m-1–m-10 are Minors; n-1–n-8 are Notes. Details and evidence are in the `Findings` section
above. The two Majors are contract issues, not implementation preferences:

- M-1: the same freeze gives `13` two meanings (RNG state vs numerical failure), omits `14`, and maps a caught
  panic to `10` (invalid arguments).
- M-2: the reader does not check mass/charge conservation, the charge-transport-vector formula, or
  `path_vector`/`movement` consistency, and the design does not assign those checks to Step 15/16/18; the
  signatures do not cover those fields, so a corrupt untrusted `.msl` would pass.

The Minors are fixable in the design docs without changing the step’s scope. The Notes are observations for the
planner/step agent.

#### Verdict

**Changes required.** The scope, non-goals, crate split, payload/runtime mapping, stored-vs-canonical encoding,
signature re-encoding, RNG derivation/state, dry-run format, and both required gates are sound, and I
independently reproduced the two stored signatures from the real payloads. The design is not ready for
implementation until M-1 and M-2 are corrected and the Minors are either fixed or explicitly accepted by the
human owner.

### Re-review round 2

#### Independence and frozen state

I am a fresh context. I did not author, design, or implement the reviewed work and have no shared conversation
with the planner, implementor, round-1 reviewer, or orchestrator. I read the repository at the record HEAD
`f8aa70f1714cc5c436b4446448a804e6e15ebb54` and re-checked the design at the reviewed commit
`9263949589c449b063e7501f5ab254ed9bf09d7f`.

Frozen-state delta: `git diff --name-status 9263949 f8aa70f` is only
`M docs/planning/reviews/step-13.md` (the metadata-only record update). `git diff --name-status 306238a 9263949`
is docs only: `solver-architecture.md`, `testing-and-parity.md`, `01-target-architecture.md`,
`02-data-contracts.md`, `03-quality-strategy.md`, `04-roadmap.md`, `05-open-decisions.md`, the Step 13/15/19
pages, `cli.md`, and `msl-format.md`. No code, test, configuration, contract, generated artifact, or design
page outside the review scope changed. The reviewed design tree is `9263949`; this record commit is a
documentation-only child of `f8aa70f`.

I re-read `AGENTS.md` §3/§8–§12, `docs/planning/reviews/README.md`, the full record, the Step 13 page, the
Step 11/12 records, Step 10/14/15/16/18/19/20 pages, ADRs 0002/0004/0005/0010/0011/0013/0014,
`02-data-contracts.md`, `01-target-architecture.md`, `03-quality-strategy.md`, `04-roadmap.md` §3/§3.1,
`05-open-decisions.md`, `code-structure.md` §2.2, `solver-architecture.md`, `testing-and-parity.md`,
`msl-format.md`, `cli.md`, `status.md`, the parity matrix, the Rust workspace/crates, and the .NET writer,
compiler, verifier, and payload DTOs named in round 1.

#### Commands and results

- `python3 tools/check_doc_math.py` → exit 0, 171 files, no delimiter warnings.
- `./tools/verify.sh docs --require` → exit 0; math/link checks, Zensical strict build, DocFX, and
  `cargo doc --workspace --no-deps` all clean; `Closeout gate completed with required checks enabled`.
- `./tools/verify.sh policy --require` → exit 0; NuGet licence/vulnerability audit, ApiCompat for every project
  (no breaking changes), contract artifacts current (26 files), model projection 5/5, parameterization 4/4,
  lattice 2/2, space-group catalog `1f3bf67d…`, process consistency OK, 58 tool tests OK, SPDX in 706 files, no
  secret patterns in 1,110 files, `cargo-deny` advisories/bans/licenses/sources OK.
- Independent read-only SQLite queries and MessagePack decode of the `msl-v1` fixture (`models`, `lattices`,
  `jobs`, `payloads/model-1.bin`, `job-1.bin`, `lattice-1.bin`). The only write is this record; `site/`,
  `target/`, and `artifacts/` are ignored build products.

I did not re-run the full .NET/Rust test suites because the frozen delta contains no code, test, or
configuration; the design gate is `docs` + `policy` + the contract/projection checks inside `policy`, all of
which pass.

#### Format and signature reproduction (independent)

- Stored integers are smallest-width, canonical widths are fixed: `job-1.bin` stores `target_mcsp = 150` as
  `cc 96` and `format_version` as `01`; the canonical writer forces `0xdd` array32, `0xdb` str32, `0xc6` bin32,
  `0xcf` uint64, `0xd3` int64, and `0xcb` float64 (`CanonicalProjectionWriter.cs`).
- The v1 lattice payload is an array, not `bin`: `lattice-1.bin` stores the `lattice` value as
  `dc 03 60` (array16, 864 `u8`) and the lattice projection uses `bin32` (`LatticeSignatureV1.cs`).
- I re-encoded both canonical projections in Python from the raw payload bytes: the six-field
  parameterization projection (array32/6, uint64 1, str32 `model_signature`, empty pair/cluster tables, the
  20-value `defect_background`, four rule frequencies, 12-decimal quantization) hashes to
  `1845b70e82320d75f768b33d1a9a46a8ae6170c33ad96013317f89fe9189a74b`, equal to `models.signature` and the
  payload's `signature`. The five-field lattice projection (array32/5, uint64 1, str32 `model_signature`,
  array32/3 sizes, uint64 position count, bin32 particles) hashes to
  `880b6eac79e9e920b6ea448c15f21b06be772f4bad774f12ea45c07420d25845`, equal to the job 1 `lattices.signature`.
  This proves §3.6 is implementable from the stored payload without a .NET dependency.

#### Closure of the round-1 findings

| Finding | Round-1 severity | Round-2 status | Verification |
| --- | --- | --- | --- |
| **M-1** | Major | **Closed** | `01-target-architecture.md` §7.3 (lines 377–383) is the single canonical table: 0, 10, 11, 12, 13 (numerical failure **or internal invariant violation**, caught panic included), 14, 15, 16. `cli.md` lines 470–486, `solver-architecture.md` lines 175–178, `02-data-contracts.md` lines 1549–1561, the Step 13 page §3.7 lines 811–832, and Step 19 lines 100–113 mirror it; Step 19 adds only 17/18 and says “do not redefine 13 as numerical only”. The step page maps a caught panic to **13** (lines 1004–1006), not 10. No code in the 0/10–16 table has two meanings. |
| **M-2** | Major | **Not fully closed — new M-3** | Mass conservation, charge conservation, `path_vector`/`length`, the zero-transport rule, and the Step 15 assignment are sound and match the .NET writer (details below). The new **allowed-occupation** check is not implementable from the v1 payload and rejects the only real fixture (M-3). |
| **m-1** | Minor | Closed | Step page §3.2 lines 527–540 separates job columns vs job payload, `lattice_a/b/c` vs the lattice payload, and `models`/`lattices` `format_version` vs their payloads; a job's resolved settings are explicitly not compared with `simulations[].default_settings` (fixture job 1 `target_mcsp = 150`, simulation 0 default 200). `02-data-contracts.md` lines 1047–1050 adds the same rule. |
| **m-2** | Minor | Closed for the named limits; new m-12 for the element budget | Step page §3.3 lines 569–571 aligns positions 500,000 (`SupercellGenerator.MaximumPositions = 500_000`), P1 positions 10,000 (`UnitCellVectorEncoder.MaxP1Positions = 10_000`), and rules 1,000,000 (`CompilationOptions.MaxRuleCount = 1_000_000`); directions 1,000,000. The `.NET` constants were re-read. The model **element** budget is not aligned (m-12). |
| **m-3** | Minor | Closed | Step page lines 893–899 states `stream_le64` (8 bytes), `word_pos_le128` (16 bytes), domain 1..255 bytes, half-open `[0, upper)`, and the 32-byte SHA-256 `config_hash` of the UTF-8 `config_json` (line 936–938). |
| **m-4** | Minor | Closed | Step page §4.5 lines 965–968 and §10 lines 1146–1147 name the same-runner-class entry in `tests/reference/bench/baselines.json`; `03-quality-strategy.md` lines 562–575 repeats it. The 10 % rule is class-relative, not cross-machine. |
| **m-5** | Minor | Closed in design; fixture is a Step 13 deliverable | §7 table and feature 6 add `tests/contracts/packages/step-13-populated/` with non-empty pair/cluster tables and an MMC job, built by the real writer from a committed sample; §7 lines 1077–1081 say to add a small sample under `samples/` if none combines both. The fixture is not present at the freeze (`tests/contracts/packages/` has only `msl-v1`, `amend-1`, `amend-1-net10`), so this is a design commitment, not evidence. It must exercise the fixed M-3 check when built. |
| **m-6** | Minor | Closed | Owner-approved 2026-11-18 at **4.5 pw** (3.5 base + 1.0 contingency), single-step exception, no split, 13A/13B considered and rejected, guard “stop before feature 4 if features 1–3 exceed 2.0 pw”. Recorded consistently in `04-roadmap.md` §3.1 (row and exception paragraph), `05-open-decisions.md` line 91, and the Step 13 page status block + §13. No unqualified no-split claim remains: the exception and guard are recorded. The request checklist still carries the old 3.0–4.0 pw text (n-10). |
| **m-7** | Minor | **Open — deferred to the step agent** | `status.md` line 63 still has Step 13 “Not started”/“Not requested”; the Active work paragraph (lines 85–90) still says XC04-F1–F5 must merge before the gate opens to Step 13, while the step page says the design review is `Re-review required` and Phase C/XC-04 is accepted (F1–F5 fixed at `73bb608`). Step-agent-owned; hygiene only (n-9). |
| **m-8** | Minor | Closed | The architecture graph (lines 170–178), `solver-architecture.md` (lines 17–46), and the Step 13 page §1 (lines 143–161) all show the existing `mocassin-cli → mocassin-solver` edge for the future run path, with the inspection path calling `mocassin-msl`/`mocassin-rng` directly. `src/rust/crates/mocassin-cli/Cargo.toml` declares `mocassin-solver`. |
| **m-9** | Minor | Closed | Step page §3.2 lines 531–532 cross-checks `models.format_version`/`lattices.format_version` against their payloads and checks the job payload version on decode; the Step 13 test table line 1114 adds the schema/version test. |
| **m-10** | Minor | Closed | Step page §8 (lines 1098–1101), the task list (line 1261–1262), and acceptance criteria name `docs/planning/closeout/step-13.json` and `python tools/check_closeout_evidence.py` with the audited commit, closeout CI `head_sha`, `main` SHA at freeze, and merge candidate. |
| **n-1** | Note | Closed | Owner decision 2026-11-18: data-contract note only, no ADR. The gated register row is removed; `05-open-decisions.md` line 90 records the resolved decision, and `02-data-contracts.md` lines 1364–1365/1435 plus Step 13 page §4.3/§14 carry the layout, `chacha12-v1` binding, key-by-design rule, and format-breaking rule. |
| **n-2** | Note | Closed | Step page lines 924–929 split draw semantics/state layout/algorithm id (new id + ADR) from seed/fork derivation (reproducibility-contract change without a state-layout change). |
| **n-3** | Note | Closed | Step page lines 914–917 and `02-data-contracts.md` line 1365 state the raw 32-byte key is in the state by design, `.mcs` is seed-sensitive, and only `seedHash` may be logged. |
| **n-4** | Note | Closed | `msl-format.md` line 130 lists both `signature` (parameterization) and `model_signature` (compiled identity). |
| **n-5** | Note | Closed | Step page lines 1025–1027 and `cli.md` state the `settings:` line is the resolved job settings/flags, not `simulations[].default_settings`; the 150 vs 200 fixture is named. |
| **n-6** | Note | Closed | Step page line 553 and `02-data-contracts.md` line 1130 correct the depth justification to five container levels with cap 8. |
| **n-7** | Note | Closed | `03-quality-strategy.md` line 574 points at §13 and the step page, not `04-roadmap.md`, for the RNG budgets. |
| **n-8** | Note | Closed | Step page lines 499–500 and 1144–1145 and `03-quality-strategy.md` lines 573–575 scope the 100 ms claim to the ceria fixture; a large payload is guard-bounded, not promised at 100 ms. |

**M-2 detail that is sound.** The mass check matches `TransitionRuleHelpers.MassConserved` (non-void symbol
multiset, `state0` vs `state2`); the charge check matches `ChargeConserved` (`1e-9` absolute, void omitted).
`CellGeometry.ToCartesian` is `fA·a + fB·b + fC·c` with A/B/C as columns, which matches the Step 13 `Σ movement`
formula and the payload's `cell_vectors_ang`; the 1e-9 relative/absolute tolerance absorbs the writer's
12-decimal `PathVector` quantization (`IndexingCompilationStage.Quantize`). The zero-transport rule matches
`ComputeTransport` (it adds nothing when `q(state0[node]) == q(state2[node])`). The Step 15 assignment is
recorded in the Step 15 page and has a sound rationale: `PositionPayload.Vector4` is
`[cellA, cellB, cellC, P]` and `StructurePayloadMapper.MapPosition` never writes a fractional P1 coordinate, so
`Σ Δq·x` cannot be recomputed from the package; the residual trust boundary is recorded in §3.5, §11, §12, and
the Step 15 page. The new populated/MMC fixture is the right vehicle for the non-empty and MMC paths, but it
does not exist at the freeze and must be built with the corrected rule check.

#### New findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M-3 — The new M-2 allowed-occupation check is not implementable from the v1 payload and rejects the only real fixture.** Step page lines 669–672 require every `state0`/`state1`/`state2` node particle to be in `positions[path[node].P].allowed_mask`, with “Void is particle 0”. The writer never includes void in `particle_ids`: `StructureCompilationStage.BuildMask` rejects a particle set entry with compiled index 0; the fixture has `particle_ids = [3,4]` at P0 and `[1,2]` at P1/P2/P3, so no position allows void. Yet every one of the four `msl-v1` rules carries void at path nodes: rule 0 is `state0=[2,0,1]`, `state1=[0,1,0]`, `state2=[1,0,2]` on path P `[1,2,3]`; rules 1–3 are the same/reversed. A literal check fails 4/4 rules (P2 void in `state0`/`state2`, P1/P3 void in `state1`). Separately, a rule has no stored representative path: `JumpRulePayload` has no direction index, `CompiledJumpRule.DirectionIndex`/`DirectionIndices` are dropped by `TransitionPayloadMapper`, and the Step 11 review R2-3 already records that the payload carries no representative-direction marker. Therefore `positions[path[node].P]` has no defined `path` for a rule. The check must at least exempt void (or check only non-void particles) and define the path source; the current text is a material design contradiction. | Step page §3.5 lines 669–672; `src/dotnet/Mocassin.Compilation/Structure/StructureCompilationStage.cs:127-131` (`index == 0` rejected); `tests/contracts/packages/msl-v1/payloads/model-1.bin` (decoded positions and rules); `src/dotnet/Mocassin.Contracts/Packages/TransitionPayloadDtos.cs:103`; `src/dotnet/Mocassin.Packages/SimulationPackage/Payloads/TransitionPayloadMapper.cs:91-107`; `src/dotnet/Mocassin.Compilation/Transitions/CompiledTransitionModel.cs:74-75`; `docs/planning/reviews/step-11.md:392` (R2-3); `02-data-contracts.md:936` (“including void … when the position may be empty”) is wrong for the current writer. |
| **Minor** | **m-11 — The `jump_mapping` check rejects a valid package with symmetry-equivalent start positions.** Step page lines 657–659 require a mapped direction's `position_id` to equal the bucket position. `EncodingCompilationStage.BuildMappings` inserts a direction into the buckets of every `direction.EquivalentStartPositions` entry, and `TransitionCompilationStage.AddDraft` populates that set when the same relative path resolves at another start P. The payload does not store `EquivalentStartPositions`, so a valid package can have a direction whose `position_id` differs from the bucket position. Narrow the check (accept an equivalent-start witness from `jump_mapping`) or record why equivalent starts cannot occur in v1. | Step page lines 657–659; `src/dotnet/Mocassin.Compilation/Simulations/EncodingCompilationStage.cs:202-250`; `src/dotnet/Mocassin.Compilation/Transitions/TransitionCompilationStage.cs:480-482`; `TransitionPayloadDtos.cs:71-89` (no equivalent-start field). |
| **Minor** | **m-12 — `max_model_elements` 16,000,000 is not aligned with the reader's own rule/link caps or with a writer-valid 512 MiB payload.** One `JumpRulePayload` map has nine keys/values plus nested arrays; 1,000,000 rules alone exceed 16M elements, and 16M pair links are far more than 16M elements. The writer's model-payload guard is 512 MiB (`PayloadEncoder.DefaultMaxModelPayloadBytes`), and a 512 MiB MessagePack document can carry far more than 16M elements, so the writer can produce a payload above the element cap. The step page's claim that “every package the current writer can produce is readable” (lines 569–571) is therefore not met for the element budget. Raise/derive the element cap or state explicitly that it is a deliberate tighter solver limit (and then do not make the “every package” claim). | Step page §3.3 line 551 and lines 569–571; `src/dotnet/Mocassin.Packages/SimulationPackage/Payloads/PayloadEncoder.cs:22`; `src/dotnet/Mocassin.Compilation/Compiler/CompilationOptions.cs:36`; `TransitionPayloadDtos.cs:103-122`. |
| **Minor** | **m-13 — Step 19's batch policy uses solver exit 5, which is not in the canonical solver table.** The Step 19 exit table (lines 100–113) mirrors 0/10–16 and adds 17/18, but the batch failure policy (line 130) says “exit 5 if any failed”. 5 is the .NET partial-success code, not a solver code; `01-target-architecture.md` §7.3 says the solver table is separate and must not reuse the .NET table. This is the one remaining “code with two meanings” in the solver docs, outside the round-1 M-1 scope. | `docs/planning/steps/step-19-solver-cli-hpc-and-extensions.md:100-113,130`; `01-target-architecture.md:369-383`. |
| **Note** | **n-9 — The status dashboard is still stale (m-7).** `status.md` line 63 has Step 13 “Not started”/“Not requested”, and the Active work paragraph (lines 85–90) still says XC04-F1–F5 are assigned to a maintenance branch to be merged before the gate opens to Step 13, although the XC-04 record says F1–F5 are fixed at `73bb608` and accepted 2026-11-18. Step-agent-owned; fix at the start gate. | `docs/planning/status.md:63,85-90`; `docs/planning/reviews/cross-cutting/xc-04-phase-c.md:832-836`. |
| **Note** | **n-10 — The request checklist and the step-page “seven features” wording still carry the pre-decision text.** The request's “Sizing and feature list” checklist item still says “3.0–4.0 pw … owner approval at the start gate”; the reconciled decision is 4.5 pw and the owner approved the sizing. The step page now correctly says “Seven features proposed; the owner approved the 4.5 pw single-step sizing … start gate pending”, but the request Step summary still reads “Owner-approved … seven features”. The request owner should refresh its own item text/status; the reviewer may not edit it. | `docs/planning/reviews/step-13.md` checklist item (round-1 text) and Step summary line 46; step page lines 19–22; `04-roadmap.md` §3.1; `05-open-decisions.md:91`. |
| **Note** | **n-11 — `02-data-contracts.md` line 936 is factually wrong about `particle_ids` and void.** It says `particle_ids` includes void “when the position may be empty”, but `StructureCompilationStage.BuildMask` rejects a particle-set entry with compiled index 0, and the fixture has no void in any mask. Correct the contract text; this is the same root cause as M-3. | `02-data-contracts.md:936`; `StructureCompilationStage.cs:127-131`; `model-1.bin` positions. |
| **Note** | **n-12 — The data-contract and DTO descriptions of `charge_transport_vector` differ from the .NET writer for charge-state changes.** The contract says the particle-identity sum `Σ q_p (x_p(S2) − x_p(S0))`; `TransitionRuleHelpers.ComputeTransport` computes the node-wise endpoint-moment difference `Σ (q(S2,node) − q(S0,node))·x(node)`, which is not the same when a particle changes charge at a fixed site. Step 13's Step 15 assignment uses the node form (`Σ Δq·x`), which matches the writer; reconcile the contract text so Step 15 does not implement the wrong formula. Pre-existing text, not introduced by this freeze. | `02-data-contracts.md:1023`; `src/dotnet/Mocassin.Compilation/Transitions/TransitionRuleHelpers.cs:54-108`; `TransitionPayloadDtos.cs:92-95` (XML doc); Step 13 page lines 682–688; Step 15 page trust-boundary paragraph. |
| **Note** | **n-13 — Two small dependency/limit wording issues.** The Step 13 crate table says `mocassin-types` depends on `serde`; the manifest also has `serde_bytes` (an existing workspace crate, so the “no new external crate” claim still holds). `CompilationOptions.MaxRuleCount` is an `init` option with a 1,000,000 default, not a hard compile-time cap; a caller that raises it can produce a package above `ReaderLimits.max_rules`. State the default/override rule explicitly. | Step 13 page §1 table line 132; `src/rust/crates/mocassin-types/Cargo.toml`; `CompilationOptions.cs:36`; Step 13 page lines 557,569–571. |

#### Verdict

**Changes required.** M-1 is genuinely closed: one canonical solver table, mirrored in every named document,
with a caught panic mapped to 13. The m-1–m-6 (m-2 for the named limits; m-12 covers the element budget),
m-8–m-10 and n-1–n-8 fixes are real, and the format/signature claims hold independently. M-2 is mostly
resolved, but its new allowed-occupation check (M-3) is a material design contradiction: the writer never puts
void in `particle_ids`, the real fixture carries void at path nodes, and a rule has no stored representative
path. The design is not ready for implementation until M-3 is corrected (and the new Minors are fixed or
explicitly accepted). The format, RNG, CLI, and crate-boundary decisions remain sound; no code, test,
configuration, contract, or generated artifact changed in the round-2 delta. m-7 remains open as a step-agent
status-dashboard fix.
