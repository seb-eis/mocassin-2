# Step 11 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-11.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 11 — Simulation Package and `.msl` Writer |
| Request owner | Planner (design freeze); the step agent takes ownership of resolutions at the start gate |
| Orchestrator | Orchestrator agent (spawns the planning auditor) |
| Step agent (implementor) | Not assigned — created from the reviewed commit after the start-gate approval |
| Review initiated by | Orchestrator, on request of the planner |
| Reviewed commit | `d8ade7a29b287ef96bda537f5cd3102d9caa1b41` |
| Docs branch | `docs/step-11-design` |
| Review date | 2026-11-15 |
| Reviewer | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh context, no shared conversation |

Step page: [`Step 11 — Simulation Package and .msl Writer`](../steps/step-11-simulation-package-and-msl-writer.md)

> Orchestration: the orchestrator spawns a fresh planning auditor after the planner has frozen the design and
> completed this request, and relays the verdict and owner decisions. The planner and step agent update the
> design docs, apply fixes on the design docs or a maintenance branch, add resolutions without altering reviewer
> text, and ask the orchestrator for re-review until the auditor no longer identifies issues. The orchestrator
> does not edit the design or the review record. The human owner approves the final verdict and the start of
> implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 08, 09, 10 |
| Delivers | The immutable `.msl` v1 package: schema, publication barrier, model/job/lattice payload DTOs and encoding, content-addressed lattices, streamed job insertion, a read-only inspector/verifier, and the checked-in contract fixtures |
| Sizing | 3.5–4.0 person-weeks, seven features; within the roadmap §3.1 four-week / roughly 6–8 feature trigger |

## Design decisions reviewed

- [x] Step page design decisions — goal, scope, non-goals, dependency reconciliation, design sections 1–9,
      tasks, acceptance criteria, tests, risks, and "Done when" — reviewed; see M-1/M-2/M-3 and the Minors.
- [x] Data contracts and format changes — `02-data-contracts.md` §1.3, §3.2–§3.5; the pre-release deltas listed
      in Step 11 §6 and their fixture/generated-artifact consequences — reviewed; M-1, M-2, m-3, m-5.
- [x] Architecture and module boundaries — `01-target-architecture.md` §3/§3.1; `Mocassin.Contracts`,
      `Mocassin.Packages`, and `Mocassin.Results` split for payload DTOs, writing, and read-only verification —
      reviewed; the split is sound, but M-3 contradicts the stated diagnostics registration path.
- [x] ADRs and open decisions — ADR 0004, 0009, 0010, 0011, 0013 and the new resolved rows in
      `05-open-decisions.md` — reviewed; ADR 0010 covers the signature finalization; ADR 0009 is not reconciled
      (m-3).
- [x] Quality strategy and test plan — `03-quality-strategy.md` §9.9, §10, §13 and the Step 11 test table with
      tiers — reviewed; m-1/m-2 leave two acceptance claims under-specified.
- [x] Parity matrix and documentation plan — `appendix-b-parity-matrix.md` package rows and the named
      reference/developer pages — reviewed; the package rows and page list are consistent with Step 11.
- [x] Licensing and provenance — no new dependency (MessagePack, ZstdSharp.Port, Microsoft.Data.Sqlite are
      already centrally versioned), no GPL-derived content, SPDX headers for new files — reviewed; note n-5.
- [x] Post-implementation audit plan — the implementation-status block, feature log, closeout evidence, and
      audit request sequence — reviewed; the plan is present and follows the standard sequence.
- [x] Feature list and sizing — seven features with distinct outcomes and a 3.5–4.0 pw estimate; whether the
      list and estimate match roadmap §3.1 — reviewed; 7 features and 3.5–4.0 pw match §3.1's 6–8 feature and
      4-week triggers, though the estimate exceeds the roadmap's 3–4 pw row by 0.5 pw.
- [x] Package schema and version metadata — `application_id`, `user_version`, `schema_info` rows, pragmas,
      batch transactions, WAL→DELETE, fsync file and directory, atomic rename, no `.bak` — reviewed; the
      SQL/data-contract text matches, subject to n-3/n-4 and the directory-fsync platform note.
- [x] Model payload mapping and signature — every §3.3 field's source, applied values versus compiled defaults,
      absolute `frequency_hz`, tracker definitions/mappings, `jump_count`/`jump_mapping` shape, and why
      `model_signature` cannot be re-encoded from the payload — reviewed; M-1 and M-2 are payload-shape gaps.
- [x] Lattice payload and signature finalization — the five-field projection, removal of `dopingInfo`, dedup
      semantics, and the ADR 0010 pre-release exception — reviewed; the five fields match §1.3 and ADR 0010,
      and no released `.msl` exists.
- [x] Job payload mapping — settings, `SolverJobFlagsMapper.Compose`, normalized Cartesian field direction,
      optimizers, `doping_info`, and the `{id, config_json}` extension encoding — reviewed; n-3/n-4.
- [x] Read-only inspection and verification — bounded decoding, the `PKG` check list, the `PKG` descriptor home
      in `Mocassin.Contracts`, and the Step 12 CLI ownership boundary — reviewed; M-3, m-1.
- [x] Contract manifest and generated artifacts — `formats.package.payloads`, removal of the undefined job
      columns, generator changes, regenerated schema/constants/descriptors, and fixture descriptors — reviewed;
      m-5.
- [x] Cross-step interfaces with Steps 12, 13, 15, 16, 18, 19, and 20 — writer/verifier consumption, the Rust
      reader contract, the field vector, per-job frequency factors, tracker ownership, extension JSON bytes, and
      `.msr` binding — reviewed; M-1/M-2/m-4 remain unresolved.

## Dependency and interface checks

- [x] Dependencies 08, 09, and 10 are `Done` and their shipped contracts are stable — `CompiledModel`,
      `ParameterizedModel`/`ParameterizationValues`/`ParameterizationSignatureV1`, `JobDescriptor`,
      `ResolvedJobSettings`, `GeneratedLattice`, `DopingReport`, `CompiledOptimizers`, `SolverJobFlagsMapper`,
      and `SymmetryVersionInfo` exist as used by the design — verified in `src/dotnet`; `CompiledOptimizers` is
      the `JobDescriptor` field and `CompiledSelectionOptimizer` is its element type.
- [x] Inputs, outputs, types, and formats are defined — model/job/lattice payload fields, the denormalized job
      columns, the lattice signature projection, and the verification report — mostly defined; M-1/M-2 leave
      two payload shapes unresolved.
- [x] Versioning and migration behavior is defined — package schema, payload, and compiled-model versions are
      independent domains; the pre-release finalizations and the current-only fixture policy are recorded —
      reviewed; m-3 records the ADR 0009 gap.
- [ ] No unresolved cross-step interface question remains — the Step 06 field/frequency text, Step 13's payload
      struct, Step 12's `inspect`/`verify` ownership, and the Step 12 build sample are reconciled — not
      satisfied: M-1, M-2, and m-4 (Step 14 is absent from the reconciliation).
- [x] The writer/verifier split matches the architecture — `Mocassin.Packages` writes and self-checks its own
      payloads, `Mocassin.Results` owns read-only `.msl` inspection/verification, and neither creates a circular
      project reference — verified against the csproj graph; M-3 is about diagnostics composition, not the split.
- [x] The `Mocassin.Packages` reference closure still allows the writer — `Compilation`, `Contracts`, and `Core`
      provide the compiled model, the applied values, the payload DTOs, the signature projections, and the cell
      vectors needed for the field conversion — verified in `Mocassin.Packages.csproj`.
- [x] The contract fixtures can be produced without hand-authored binaries — the real writer generates the
      `.msl`, the canonical summary normalizes build metadata, and the extracted model payload is copied from
      the writer's bytes — plan is sound; the canonical summary itself is undefined (m-2).
- [x] Step 12 can build the full pipeline from this API — model write, per-job lattice/descriptor pairing,
      completion result with the package SHA-256, and the verifier used before reporting success — verified
      against the Step 12 design and the writer API shape.

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable — reviewed; the scope and non-goals
      are clear; m-1/m-2 make two acceptance claims not yet testable.
- [ ] Design decisions are complete and internally consistent — not satisfied: M-1, M-2, M-3.
- [x] Required tests, property tests, golden fixtures, and performance targets are defined — reviewed; the
      tiered table covers mapping, property, golden, corruption, cross-language, and performance tests; m-2.
- [x] Documentation, status, parity, and audit updates are planned — reviewed; the named pages, status row,
      parity rows, and audit sequence are present.
- [x] Licensing, provenance, and dependency checks are resolved — reviewed; no GPL-derived content; n-5.
- [x] Risks and mitigations are realistic — reviewed; the risk table covers bloat, drift, pre-release deltas,
      transaction locking, cancellation, determinism, extension tokens, dedup provenance, and verifier bounds.
- [ ] Code layout and namespace plan follow the structure policy — not stated: m-6.
- [ ] No hidden scope, stale placeholders, or unowned decisions — not satisfied: m-1, m-2, m-5, m-6.

## Evidence

Files reviewed:

- `AGENTS.md` §3.9 and §3.12; `docs/development/orchestration.md`; `docs/planning/reviews/README.md`.
- `docs/planning/steps/step-11-simulation-package-and-msl-writer.md`; Step 06, 08, 12, 13, 14, and 15 step
  pages; `docs/planning/02-data-contracts.md` §1.3 and §3.2–§3.6; `docs/planning/01-target-architecture.md`
  §3 and §3.1; `docs/planning/03-quality-strategy.md` §9.9, §10, §13; `docs/planning/04-roadmap.md` §3.1;
  `docs/planning/05-open-decisions.md`; ADR 0004, 0009, 0010, 0011, 0013; `appendix-b-parity-matrix.md`;
  `status.md`.
- `src/dotnet/Mocassin.Compilation`: `CompiledModel.cs`, `CompiledStructureModel.cs`,
  `CompiledTransitionModel.cs`, `CompiledEnergyModel.cs`, `EncodingCompilationStage.cs`,
  `ModelSignatureProjection.cs`, `TransitionCompilationStage*.cs`, `PairCompilationStage.cs`.
- `src/dotnet/Mocassin.Contracts`: `ParameterizationValues.cs`, `ParameterizationSignatureV1.cs`, csproj.
- `src/dotnet/Mocassin.Packages`: `JobDescriptor.cs`, `ResolvedJobSettings.cs`, `GeneratedLattice.cs`,
  `DopingReport.cs`, `SolverJobFlagsMapper.cs`, `SupercellGenerator.cs`, `JobSettingsResolver.cs`, csproj;
  `Mocassin.Results.csproj`; `Mocassin.Benchmarks.csproj`; `Mocassin.Packages/Projects/Validation/DiagnosticCatalog.cs`.
- `contracts/manifest.json`; `contracts/generated/schema/package-v1.schema.json`;
  `contracts/generated/fixtures/descriptors.json`; `tools/contracts/generate.py`; `tests/contracts/`
  (README, `signature-vectors.json`, `projections/`, `api-baseline/`).
- `samples/jobs/ceria-temperature-sweep.mocprj`; `.gitignore`; `tools/verify.sh`; `Directory.Packages.props`.

Commands run:

- `git rev-parse HEAD` → `a227087492f9e63c93f172329bd944b43da63957`; `git diff --name-status
  d8ade7a29b287ef96bda537f5cd3102d9caa1b41 HEAD` → only `A docs/planning/reviews/step-11.md`. The reviewed
  design tree is `d8ade7a` plus the request record; no code or design doc changed between them.
- `./tools/verify.sh docs` → exit 0. Math check 159 files OK; link check 162 files OK; Zensical strict build
  `No issues found`; DocFX 0 warnings / 0 errors; rustdoc succeeded.
- `./tools/verify.sh policy` → exit 0. NuGet licence policy OK (51 packages); no vulnerable packages;
  ApiCompat no breaking changes for all projects; contract artifacts current (18 files); model projection
  check OK (5 fixtures); parameterization projection check OK (4 fixtures); space-group catalog current
  (274 settings); process consistency OK; 37 Python unit tests OK; SPDX headers present in 528 source files;
  no secret patterns in 890 tracked text files; cargo-deny advisories/bans/licenses/sources OK.
- Additional read-only inspections: manifest JSON (`signatures.lattice` is still the Step 00 skeleton with
  `dopingInfo`; `jobs` still has `norm_factor`/`base_frequency_hz`), generated package schema, compiled
  transition/structure models, and the project-reference graph. No repository file was modified.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M-1 — Model payload pair links drop the compiled representative offset.** The solver cannot map a periodic partner image to its pair table from the design's `{target_position, energy_id}` link. | Step 08 §6; `CompiledStructureModel.cs` `CompiledPairLink`; §3.3; Step 11 §3; Step 14 §4; Step 15 §4. |
| **Major** | **M-2 — `tracker_order` is variable-length in the compiled model but fixed `[u8;8]` in the payload contract and Step 13.** A normal 2–3-node rule emits 2–3 bytes; the fixed Rust array would reject it. | `CompiledTransitionModel.cs`; `EncodingCompilationStage.cs` `BuildTrackerOrder`; `ModelSignatureProjection.cs`; §3.3; Step 13 `JumpRule`. |
| **Major** | **M-3 — `Mocassin.Results` cannot compose `PKG` descriptors into `DiagnosticCatalog.Default`.** The catalog is in `Mocassin.Packages`, which `Results` must not reference. | Step 11 §5; `DiagnosticCatalog.cs`; `Mocassin.Results.csproj`; `01-target-architecture.md` §3.1; ADR 0009. |
| **Minor** | **m-1 — The `PKG` code set and check-to-code mapping are not defined.** Only `PKG004` appears. | Step 11 §5, acceptance line 435, tests 470–471; no `PKG` definitions in `src/dotnet`. |
| **Minor** | **m-2 — The "canonical package summary" is undefined.** Determinism, fixtures, and benchmarks rely on it. | Step 11 §7, acceptance 425–426, tests 466; `03-quality-strategy.md` §9.9. |
| **Minor** | **m-3 — ADR 0009's manifest version-bump rule is not reconciled.** The design changes table/payload shape under a signature-layout exception. | ADR 0009 Rules; Step 11 §6; ADR 0010 pre-release section; §3.2. |
| **Minor** | **m-4 — Step 14 is missing from the cross-step reconciliation.** It is the immediate consumer and defines `target_position_offset`. | Step 11 dependency table; request checklist; Step 14 §4. |
| **Minor** | **m-5 — The generated artifact for `formats.package.payloads` is ambiguous.** The current package schema is a SQLite row schema. | `contracts/generated/schema/package-v1.schema.json`; `tools/contracts/generate.py`; Step 11 §6 item 2. |
| **Minor** | **m-6 — No folder/namespace plan for the new writer, mappers, signature type, descriptors, or inspector.** | `AGENTS.md` §8; `docs/development/code-structure.md`; Step 11 feature list/tasks. |
| **Note** | **n-1 — `PackageWarning`, `PackageVerificationReport`, and the `MslPackageInspector` member shape are referenced but undefined.** | Step 11 §2/§5. |
| **Note** | **n-2 — `clean` ownership is inconsistent.** Step 11 says Step 12; Step 12 marks `clean` as Step 28. | Step 11 §2; Step 12 command list. |
| **Note** | **n-3 — The `usePrerun` verification rule ignores kind.** `Compose` sets it only for KMC. | Step 11 §5; `SolverJobFlagsMapper.Compose`. |
| **Note** | **n-4 — "Denormalized job columns equal their payload values" is overbroad.** Names/indices/`created_utc`/`seed` have no payload counterpart. | Step 11 §5; §3.2. |
| **Note** | **n-5 — `ZstdSharp.Port` and `Microsoft.Data.Sqlite` are centrally versioned but not yet referenced.** Adding them is a new dependency edge the licence gate has not exercised. | `Directory.Packages.props`; csproj grep; Step 11 licensing line. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

### Finding details

**M-1 — Model payload pair links drop the compiled representative offset.**

The design's model payload keeps only `{target_position, energy_id}` for each pair link. This is not a
one-way mechanical mapping of the compiled environment link:

- `CompiledPairLink` is `(PairTableIndex, TargetP, Offset, DistanceBucketFm)`; `Offset` is the representative
  relative cell image (`src/dotnet/Mocassin.Compilation/Structure/CompiledStructureModel.cs`, lines 33–45).
- Step 08 §6 states: “The environment link records the table index (`EnergyId`), target P, and the
  representative relative offset.” (`docs/planning/steps/step-08-model-compilation-pipeline.md`, lines
  316–318.)
- `02-data-contracts.md` §3.3 and Step 11 §3 persist only `{target_position, energy_id}` (data contracts line
  925; step page lines 239–241).
- Step 14 §4 builds runtime links with `target_position_offset` and `pair_id`
  (`docs/planning/steps/step-14-simulation-context-initialization.md`, lines 135–146); Step 15 §4 consumes
  `(target, pair_id)` links (`docs/planning/steps/step-15-energy-engine.md`, lines 149–152).
- Step 11 §3 calls the mapping “mechanical and one-way”; without `Offset`, the writer cannot preserve the
  image identity, and the solver cannot match a periodic image to its energy table without reimplementing the
  compiler's pair-geometry classification. Add `offset` (and, if needed, `distance_bucket_fm`) to the payload
  link or define and test an exact reconstruction rule; also add Step 14 to the interface reconciliation.

**M-2 — `tracker_order` length is variable in the compiled model but fixed `[u8;8]` in the payload contract and
Step 13.**

- `CompiledJumpRule.TrackerOrder` is `ImmutableArray<byte>`; `EncodingCompilationStage.BuildTrackerOrder`
  allocates exactly `nodes = rule.Movement.Length` bytes and returns one entry per path node
  (`src/dotnet/Mocassin.Compilation/Transitions/CompiledTransitionModel.cs`, line 82;
  `src/dotnet/Mocassin.Compilation/Simulations/EncodingCompilationStage.cs`, lines 173–206).
- `ModelSignatureProjection` writes `rule.TrackerOrder.Length`, so the model signature itself treats the array
  as variable-length (lines 277–281).
- `02-data-contracts.md` §3.3 says `tracker_order:[u8;8]` (line 976); Step 13's `JumpRule` uses
  `tracker_order: [u8; 8]` (step 13, line 82). A normal simple transition has two path nodes and emits two
  bytes; a fixed Rust `[u8;8]` reader would fail unless the writer pads. The design neither states padding nor
  changes the contract. Choose variable `u8[]` with `length == path node count` and update Step 13, or pad to 8
  in the writer and state/test the padding and the model-signature interaction.

**M-3 — `PKG` descriptors cannot be composed into `DiagnosticCatalog.Default` from `Mocassin.Results`.**

Step 11 §5 says: “`PKG` descriptors live in `Mocassin.Contracts.Packages` so both `Mocassin.Packages` and
`Mocassin.Results` can compose them into `DiagnosticCatalog.Default`.” But `DiagnosticCatalog.Default` is a
`Mocassin.Packages.Projects.Validation.DiagnosticCatalog`
(`src/dotnet/Mocassin.Packages/Projects/Validation/DiagnosticCatalog.cs`, lines 20–55), and
`Mocassin.Results.csproj` references only `Core` and `Contracts`. The architecture and ADR 0009 forbid
`Mocassin.Results -> Mocassin.Packages` (`01-target-architecture.md` §3/§3.1; ADR 0009 “Rules”). The verifier
therefore has no stated way to register the `PKG` descriptors. Move `DiagnosticCatalog` (or a
package-specific catalog) to `Mocassin.Contracts`/`Mocassin.Core`, or define a
`Mocassin.Results.Packages` catalog and update the architecture and docs; keep the descriptors themselves in
Contracts.

**m-1 — The `PKG` code set and check-to-code mapping are not defined.**

The design lists verification checks and says each produces a `PKG` code, but only `PKG004` appears
(decode-bound test). Acceptance and the verification test table say “each with the expected `PKG` code”
without a code list, severity, or docs anchor. Define the codes (error/warning), their check, and their
`docs/reference/diagnostics.md` anchors so the tests and drift check are testable.

**m-2 — The “canonical package summary” is undefined.**

The determinism acceptance, the checked-in fixture plan, and the test table rely on an “identical canonical
package summary” normalized for build metadata, but the design gives no schema, field list, location, or
derivation. Define the summary (for example `package-summary-v1` in the manifest or a documented inspector
projection) and state how `created_utc`, `app_version`, and `generator` are normalized.

**m-3 — ADR 0009's manifest version-bump rule is not reconciled.**

ADR 0009's Rules say: “`contracts/manifest.json` changes require a format version bump and updated fixtures.”
Step 11 removes the `jobs.norm_factor`/`base_frequency_hz` columns and adds `formats.package.payloads` while
keeping package/payload versions at 1; ADR 0010's pre-release exception covers signature layout, not these
table/payload-shape changes. Step 11 §6 says the “first released format” discipline of ADR 0009 resumes
“afterwards”, but the ADR's rule is in force now. Record the pre-release basis in ADR 0009 or a decision note
(as Step 09 did for its signature), and keep the fixture/regeneration plan current.

**m-4 — Step 14 is missing from the cross-step reconciliation.**

The request checklist names Steps 12, 13, 15, 16, 18, 19, and 20, and the step page's dependency table omits
Step 14. Step 14 §4 is the immediate consumer of the model payload and defines `target_position_offset` and
`pair_id` links. Add Step 14 to the reconciliation and resolve M-1 there.

**m-5 — The generated artifact for `formats.package.payloads` is ambiguous.**

`formats.package` currently generates `contracts/generated/schema/package-v1.schema.json` as a SQLite row
schema (`schema_info`, `models`, `lattices`, `jobs`). Adding a top-level `payloads` key to the same descriptor
would mix row and payload schemas. State whether `generate.py` emits a separate artifact (for example
`package-payloads-v1.schema.json`), a manifest section, or payload descriptors in the existing schema, and how
fixture descriptors are regenerated.

**m-6 — No folder/namespace plan for the new writer, mappers, signature type, descriptors, or inspector.**

The design names projects but not the domain folders for `SimulationPackageWriter`, the payload mappers,
`LatticeSignatureV1`, `PKG` descriptors, or `MslPackageInspector`. `AGENTS.md` §8 and
`docs/development/code-structure.md` require a plan that matches namespaces to folders and respects the
20-file/depth limits. Add a short layout table.

**n-1 — Undefined public result/report types.** `PackageWarning`, `PackageVerificationReport`, and the
`MslPackageInspector` member shape are referenced but not defined; define them in feature 1/6 so the
`PublicAPI.Unshipped.txt` and ApiCompat delta are known.

**n-2 — `clean` ownership is inconsistent.** Step 11 §2 says “Step 12's `clean` may remove them”; Step 12's
command list marks `clean` as Step 28. Name the owner explicitly.

**n-3 — `usePrerun` verification ignores kind.** `SolverJobFlagsMapper.Compose` sets `UsePrerun` only for KMC;
the verifier rule “set if and only if `pre_run_mcsp > 0`” would fail for an MMC job with a nonzero pre-run.
Qualify the check by kind or enforce zero for MMC.

**n-4 — Denormalized-column check is overbroad.** “The denormalized `jobs` columns equal their payload
values” includes `collection_name`, `config_name`, indices, `created_utc`, and `seed`, which have no payload
counterpart. Use “payload-derived columns” as `02-data-contracts.md` §3.2 already says.

**n-5 — New package-reference edges are not licence-gated yet.** `ZstdSharp.Port` and `Microsoft.Data.Sqlite`
are centrally versioned but no project references them today; adding them is a new dependency edge. Record
their MIT licence verification when the references are added (the current `check_nuget_licenses.py` output
does not exercise them).

## Record state

**State:** Changes required

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

Follow-up owner: Planner and step agent (design revision); re-review by a fresh planning auditor
Follow-up step: Step 11 — correct M-1/M-2/M-3 and the Minors, then append a re-review round.

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

I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
planner, implementor, or orchestrator. I read the repository at `HEAD = a227087492f9e63c93f172329bd944b43da63957`
and confirmed with `git diff --name-status d8ade7a29b287ef96bda537f5cd3102d9caa1b41 HEAD` that the only
difference from the frozen design commit `d8ade7a` is the added request record
`docs/planning/reviews/step-11.md`. No code, test, configuration, or design doc changed between the frozen commit
and the record. The request-record commit `a227087` is a documentation-only request commit; this round's record
commit is its child, and the frozen commit is its parent as recorded in the request.

#### Commands and results

- `./tools/verify.sh docs` — pass (math 159 files, links 162 files, Zensical strict `No issues found`, DocFX
  0 warnings/0 errors, rustdoc succeeded).
- `./tools/verify.sh policy` — pass (NuGet licence policy 51 packages, no vulnerable packages, ApiCompat no
  breaking changes, contract artifacts current 18 files, model projection 5 fixtures, parameterization
  projection 4 fixtures, catalog current, process consistency OK, 37 unit tests OK, SPDX 528 files, secret scan
  890 files, cargo-deny OK).
- Read-only code inspections of the compiled/contracts/packages/Results types, manifest, generated schema, and
  project references; no repository file was modified.

#### Findings

M-1, M-2, and M-3 are Majors; m-1 through m-6 are Minors; n-1 through n-5 are Notes. Details and evidence are in
the `Findings` section above. The three Majors are payload/architecture issues that must be corrected before
implementation starts:

- M-1: pair links omit the compiled relative offset, so the periodic image cannot be matched to its pair table
  by the solver (Step 14/15 contract).
- M-2: `tracker_order` is variable-length in the compiled model but fixed `[u8;8]` in the payload contract and
  Step 13.
- M-3: `Mocassin.Results` cannot use `DiagnosticCatalog.Default` without a forbidden `Results -> Packages`
  reference.

#### Verdict

**Changes required.** The scope, feature list, sizing, lattice-signature finalization, writer/publication
barrier, and most of the model/job mapping are sound, and both required gates pass. The design is not ready for
implementation until M-1, M-2, and M-3 are corrected and the Minors are either fixed or explicitly accepted.
