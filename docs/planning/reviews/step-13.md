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
| Reviewed commit | `b2e4b4fdf141f28411c0d51878f12c26fe1ac09b` |
| Docs branch | `docs/step-13-design` |
| Review date | 2026-11-18 |
| Reviewer | — |

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
| Sizing | 3.0–4.0 person-weeks (roadmap §3 says 2–4 pw), seven features, 1.0 pw contingency; §3.1 check and no-split recommendation in step page §13. Owner approval requested once at the start gate. |

## Design decisions reviewed

- [ ] **Crate boundaries and dependency direction.** `mocassin-types` (payload DTOs + validated runtime types),
      `mocassin-msl` (read-only SQLite/Zstd/MessagePack + signature re-encoding), `mocassin-rng` (only crate
      touching `rand_chacha` state APIs), `mocassin-cli` (inspection path), placeholders for solver/state/
      extension. Verify no cycle, no new external crate, and no second package reader.
- [ ] **Payload DTO and runtime model contract.** Exact snake_case keys mirror `Mocassin.Contracts.Packages` and
      the generated package schema; typed IDs; `PairLink.pair_id` vs payload `energy_id`; `EnvironmentModel`
      per position and Step 14's supercell CSR ownership; `JobPackage`/`JobSettings`/`InitialLattice`/
      `SolverJobFlags`; no `deny_unknown_fields`.
- [ ] **Stored encoding vs canonical projection.** `.msl` payload integers use smallest-width MessagePack; ADR
      0010 fixed width applies only to the canonical projection; the v1 lattice payload is an array of `u8` while
      the projection uses `bin`. Verify the contract text and the Rust DTO/decoder tests agree, and that no
      format version changes.
- [ ] **`.msl` reader pipeline and limits.** Read-only open, application-id/user-version/format/provenance
      checks, no `integrity_check`, no mirrored `PKG` checks, Zstd declared-size guard, non-allocating
      MessagePack scan, element/depth/per-field caps, typed decode, runtime mapping. Verify the limit defaults
      are consistent with the .NET verifier and the 100 ms load budget.
- [ ] **Validation completeness.** Particle/position/site/environment/pair/cluster/collection/direction/rule/
      tracker indices and lengths; state-code nodes; `tracker_order` vs collection path length; jump-count prefix
      sums; kind/flag consistency; finite values; normalized field; lattice length/particles. Verify every
      invariant the solver will rely on in Steps 14–18 is checked here or explicitly assigned to a later step.
- [ ] **Signature re-encoding.** Forced array32/str32/bin32/uint64/float64 markers, the C# 12-decimal
      quantization expression, domain prefixes, six-field parameterization projection, five-field lattice
      projection, `model_signature` as identity only. Verify the Rust `.bin` vector round-trip proves the
      encoder and that a mutated value is rejected.
- [ ] **RNG derivation, draws, and state.** Step 10 byte order and domain strings; `rand_chacha` public state
      APIs; `next_u32`/`next_u64`/`next_f64`/`next_bounded`/`fork` semantics; the 64-byte `RngStateV1` layout;
      mid-block resume; one simulation stream across pre-run/run; extension forks. Verify ADR 0005 stands, no
      new ADR is required, and the format-breaking rule is explicit.
- [ ] **RNG performance obligation.** Draws/s, bounded calls/s, refill cost, state round-trip, and fork budgets;
      the Criterion location and evidence file; the 10 % regression rule; no algorithm change to meet a budget.
- [ ] **CLI scaffold and exit codes.** `--dry-run` is write-free and required in Step 13; `--stdout` conflict;
      `--seed` override and seed hash; validated-but-unused `--threads`/`--jump-log-max-ev`; `--extension`
      rejected; parse-error remap to 10; package 11, model/data 12, I/O 16; no `--json`/batch/validate-only/
      no-state/log-file until Step 19. Verify the dry-run format is reproducible and comparable to
      `inspect-v1`/`package-summary-v1`.
- [ ] **Data-oriented base structures.** Lattice indexing, positions, environment definitions, flat energy
      tables, `DirectionPool` swap-remove, jump-status container; no `HashMap` in hot structures; Step 14/15/18
      ownership. Verify Step 13 does not slide into context initialization or physics.
- [ ] **Cross-language fixtures and tests.** `msl-v1` and `amend-1-net10` reads, raw payload decode, projection
      `.bin` round-trip, `rng-vectors.json`, dry-run golden, property/mutation tests, fuzz targets and the
      time-boxed run, load/RNG benchmarks, tiers, and real-file policy.
- [ ] **Documentation, parity, and audit plan.** New `solver-architecture.md`; updates to `msl-format.md`,
      `cli.md`, `testing-and-parity.md`, `code-structure.md`; rustdoc with `missing_docs`; parity/status/
      closeout/audit sequence; nav entry owned by the implementor.
- [ ] **Sizing and feature list.** Seven features, 3.0–4.0 pw, 1.0 pw contingency, §3.1 check, no split, the
      "stop if features 1–3 exceed 2.0 pw" guard, and the owner approval at the start gate.

## Dependency and interface checks

- [ ] Step 11 is `Done`, merged and tagged, and its `.msl` v1 schema/payload/fixture contract is stable. Verify
      the schema columns, `application_id`/`user_version`, payload keys, `msl-v1` package, and
      `package-summary-v1` used by the design.
- [ ] Step 10's job/lattice seed derivation byte order and domain strings match the Rust design.
- [ ] Step 09/08 canonical projection layouts and `.bin` vectors match the Rust writer/reader design.
- [ ] Step 12's solver exit-code table (0, 10–16) and `mocassin simulate` reservation match the CLI design.
- [ ] Step 14 can consume `JobPackage`, `ParameterizedModel`, `InitialLattice`, `EnvironmentModel`, `Rng`, and
      `DirectionPool` without re-reading the package, and Step 13 assigns supercell expansion/links/pools/jump
      status/energy runtime to Step 14.
- [ ] Step 18/20 can consume the 64-byte `RngStateV1` and `seed_hash`; `.mcs` `rngAlgorithm`/`rngState` agree
      with the data contract.
- [ ] No unresolved cross-step interface question remains. The deliberate asymmetries (Rust never writes `.msl`;
      no `integrity_check` in the solver; no physics in Step 13) are stated in the contract and tests.

## Standard design-review checklist

- [ ] Scope, non-goals, and acceptance criteria are explicit and testable
- [ ] Design decisions are complete and internally consistent
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined
- [ ] Documentation, status, parity, and audit updates are planned
- [ ] Licensing, provenance, and dependency checks are resolved
- [ ] Risks and mitigations are realistic
- [ ] Code layout and namespace plan follow the structure policy
- [ ] No hidden scope, stale placeholders, or unowned decisions

## Evidence

Files reviewed:

Commands run:

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Ready for review

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Waived

Follow-up owner:
Follow-up step:

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

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

#### Commands and results

#### Findings

#### Verdict
