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
| Reviewed commit | `f04bd3cb5f424bff0e4f8d736d84185370775308` |
| Round 1 frozen commit | `d8ade7a29b287ef96bda537f5cd3102d9caa1b41` |
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
| Sizing | 3.8–4.2 person-weeks, seven features; the upper bound exceeds the roadmap §3.1 four-week trigger, so the request presents the single-step exception (recommended, as for Step 09) and the 11A/11B split alternative |

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
- [x] No unresolved cross-step interface question remains — the Step 06 field/frequency text, Step 13's payload
      struct, Step 12's `inspect`/`verify` ownership, and the Step 12 build sample are reconciled — round 2:
      M-1, M-2, and m-4 are closed in the design; the remaining R2-3 verifier-derivation detail is recorded in
      the round-2 findings, not an open interface question.
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
- [x] Design decisions are complete and internally consistent — round 2: M-1, M-2, and M-3 are closed in the
      design; R2-1, R2-2, R2-3, and R2-4 are documentation/verifier details recorded in the round-2 findings.
- [x] Required tests, property tests, golden fixtures, and performance targets are defined — reviewed; the
      tiered table covers mapping, property, golden, corruption, cross-language, and performance tests; m-2.
- [x] Documentation, status, parity, and audit updates are planned — reviewed; the named pages, status row,
      parity rows, and audit sequence are present.
- [x] Licensing, provenance, and dependency checks are resolved — reviewed; no GPL-derived content; n-5.
- [x] Risks and mitigations are realistic — reviewed; the risk table covers bloat, drift, pre-release deltas,
      transaction locking, cancellation, determinism, extension tokens, dedup provenance, and verifier bounds.
- [x] Code layout and namespace plan follow the structure policy — round 2: Step 11 §9 supplies the layout
      table; the folders, namespaces, file counts, depth, and `Dtos`/`Options` suffixes match
      `tools/structure-policy.json` (m-6 closed).
- [x] No hidden scope, stale placeholders, or unowned decisions — round 2: m-1, m-2, m-5, and m-6 are closed;
      R2-4 (stale `jump_collections.object_id` delta) and R2-5 (stale request-summary sizing) are recorded as
      documentation findings.

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

**State:** Ready with notes

Round 2 verdict: `Ready with notes` — see `### Re-review round 2` below. No Blocker or Major finding remains;
four Minor and five Note findings are recorded for acceptance or correction before implementation.

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
- [x] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by: Human owner
Date: 2026-11-15

## Resolution status

> Amendment: [`step-11-amend-1`](../amendments/step-11-amend-1-dotnet-11-native-zstd.md) — .NET 11 toolchain and
> native Zstandard — is a late design change to this step, planned while the step is still active and before the
> audit freeze. Its focused design review is [`step-11-amend-1.md`](step-11-amend-1.md). It does not reopen any
> finding above.

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| M-1 | Closed in design — environment pair entries are now one row per periodic image with `{target_position, offset, energy_id}`; the compiler emits one `CompiledPairLink` per image from `PairShell.EquivalentImages` (exposed additively on `PairInteractionPreview`), so the existing four-field model-signature link projection signs every runtime image. The model-signature values change for multi-image shells, so the Step 08/09 signature fixtures and sample `modelSignature` values are regenerated. Step 08/13/14/15 and the data contract are aligned. The cross-step Symmetry/Compilation change needs start-gate/owner approval and an ApiCompat refresh. | Planner; owner for the cross-step change; step agent implements | `f04bd3c` |
| M-2 | Closed in design — `tracker_order` is a variable-length `u8[]` with one global tracker index or `0xFF` per path node and length equal to the representative direction's path length; data contract, Step 13, Step 18, and the model-signature note are aligned. | Planner | `f04bd3c` |
| M-3 | Closed in design — `PKG` descriptors are Contracts-owned; `Mocassin.Packages` composes them into `DiagnosticCatalog.Default` for docs drift/build-side lookup; `Mocassin.Results` builds diagnostics from the same descriptors and gains no `Mocassin.Packages` reference. | Planner | `f04bd3c` |
| m-1 | Closed — `PKG001`–`PKG010` are defined with severity, check, and docs anchor; the verifier check list is numbered and mapped. | Planner | `f04bd3c` |
| m-2 | Closed — `package-summary-v1` is defined in data contracts §3.5 (schema, model/lattice/job entries, `seedHex`, normalization, sort order) and is the deterministic/golden projection. | Planner | `f04bd3c` |
| m-3 | Closed — ADR 0009 gains the "pre-release package-shape finalization" section; Step 11 §6 and `05-open-decisions.md` record the basis and the first-release resumption. | Planner | `f04bd3c` |
| m-4 | Closed — Step 14 is in the dependency reconciliation table; Step 14 §4 now names the payload source fields and the exact `cell + offset` expansion. | Planner | `f04bd3c` |
| m-5 | Closed — `generate.py` emits a separate `contracts/generated/schema/package-payloads-v1.schema.json`; `package-v1.schema.json` stays the row schema; payload schemas use `additionalProperties: true`. | Planner | `f04bd3c` |
| m-6 | Closed — Step 11 §9 defines the folder/namespace layout for the DTOs, writer, payload mappers, signature, descriptors, inspector, tests, and benchmark. | Planner | `f04bd3c` |
| n-1 | Closed — `PackageInspection`, `PackageVerificationReport`, `PackageVerificationCheck`, `PackageVerificationOptions`, `IMslPackageInspector`, and `IPackageVerifier` are defined; `PackageWarning` is replaced by `Diagnostic`. | Planner | `f04bd3c` |
| n-2 | Closed — orphaned temp files are removed by `mocassin clean` (Step 28), stated explicitly. | Planner | `f04bd3c` |
| n-3 | Closed — the `usePrerun` check is qualified by kind (KMC only, iff `pre_run_mcsp > 0`; MMC requires it clear and `pre_run_mcsp = 0`). | Planner | `f04bd3c` |
| n-4 | Closed — the verifier checks only payload-derived columns and checks authored/build metadata for presence/format/uniqueness. | Planner | `f04bd3c` |
| n-5 | Closed in design — feature 3/6 records the MIT licence result for the new `ZstdSharp.Port`/`Microsoft.Data.Sqlite` reference edges; verification owner is the step agent at implementation. | Step agent | `f04bd3c` |
| R2-1 | Closed — the Step 13 page has a balanced fence around the `JumpRule` and `JobPackage` blocks, and the `### 3. .msl reader` section renders again. | Step agent (start gate) | this commit |
| R2-2 | Closed — the `PairLink.offset` comment now reads “periodic image offset from the source cell”, matching the one-entry-per-image contract. | Step agent (start gate) | this commit |
| R2-3 | Closed — Step 11 check 5 and the tracker acceptance line now state only what the payload proves: `tracker_order` non-empty, sentinel/index bounds, and length within the documented bound; the writer keeps the representative-path-length assertion, and the shell-representative presence check is dropped because the payload carries no marker. | Step agent (start gate) | this commit |
| R2-4 | Closed — §6 item 4 now states that `jump_collections` has no `object_id` and none is added; only `jump_directions.object_id` is retained. | Step agent (start gate) | this commit |
| R2-5 | Closed — the request `Step summary` now records the 3.8–4.2 pw estimate, the four-week-trigger overrun, and the single-step exception/start-gate choice. | Step agent (start gate) | this commit |
| R2-6 | Closed — the `PublicAPI.Unshipped.txt`/ApiCompat task now names Contracts, Core (`JobExtension.ToCanonicalJson`), Symmetry (`PairInteractionPreview.EquivalentImages`), Compilation, Packages, and Results. | Step agent (start gate) | this commit |
| R2-8 | Closed — the payload unknown-key policy is now `formats.package.payloads.unknownFields = "ignore"` in the manifest; `generate.py` derives each payload object's `additionalProperties` from it, while `formats.package.unknownFields = "reject"` remains the SQLite row-schema policy. Nothing is hard-coded in the generator. | Step agent (feature 2) | `9b754af` |
| R2-7 | Closed — the file fsync uses `FileStream.Flush(true)`; the containing-directory fsync uses a minimal Unix libc `open`/`fsync`/`close` interop (`DllImport`, `UnmanagedType.LPUTF8Str`, `SetLastError`, `CharSet.Ansi`). Windows has no equivalent directory-durability primitive in this path, so it skips the directory call and relies on the file fsync plus atomic replace. This is a §3.13 platform trigger: the closeout must dispatch the manual Windows platform job; macOS uses the same Unix path exercised on Ubuntu, so it is optional/manual. | Step agent (feature 3) | `c0d8d64` |
| R2-3 | Closed — the verifier no longer asserts "the shell representative is present" (the payload carries no representative marker or `distance_bucket_fm`) and no longer claims an independent tracker node count. It checks `tracker_order` non-empty, length within the documented ten six-bit node slots, and non-sentinel values below `global_tracker_count`; the writer retains the representative-path-length assertion. No new payload field was invented. | Step agent (feature 6) | `cbe345f` |
| R2-9 | Closed — `DiagnosticCatalogTests.DefaultIsTheUnionOfTheDomainCatalogsAndStep07Codes` now includes `PackageDiagnosticDescriptors.All`, and `DiagnosticDocumentationTests` covers the ten new PKG codes and headings. | Step agent (feature 6) | `cbe345f` |

Resolution notes:

- Round 2 should review the frozen design at `f04bd3cb5f424bff0e4f8d736d84185370775308`; the only later diff on
  this branch is this record update.
- M-1 was deeper than the finding text: the compiled pair link is one entry per symmetry shell, while the
  runtime needs every periodic image. The design therefore emits one compiled link per image (signed by the
  existing four-field model-signature projection) and records the additive preview field plus the regenerated
  Step 08/09 signature fixtures as a cross-step prerequisite. If the owner rejects that change, the alternative
  (solver-side reconstruction from a stored bucket/geometry identity) needs a documented uniqueness proof for
  chiral or coincident shells; the design recommends the per-image compiled link.
- The review noted that the earlier 3.5–4.0 pw estimate already exceeded the roadmap's 3–4 pw row. The M-1 fix
  makes the honest estimate 3.8–4.2 pw, whose upper bound exceeds the four-week trigger; Step 11 §10 now records
  the single-step-exception request (recommended, as for Step 09) and the 11A/11B split alternative.

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

### Re-review round 2

#### Independence and frozen state

I am a fresh context. I did not author or implement the reviewed work, I did not perform review round 1, and I
have no shared conversation with the planner, implementor, or orchestrator. I read the repository myself rather
than relying on a summary. Round-2 reviewer: DeepSeek V4.1 Flash 1017 (opencode-go).

- `git rev-parse HEAD` → `ebd7e5d9a30796e3dc8ddc033ce18fa22b0d19c9` (`docs/step-11-design`).
- `git diff --name-status f04bd3cb5f424bff0e4f8d736d84185370775308 HEAD` → `M docs/planning/reviews/step-11.md`
  only. The frozen design content under review is `f04bd3c`; `ebd7e5d` is the round-1 resolution/record commit
  and differs from the frozen design only by this record. `git status --porcelain` was empty before and after
  the gates; no repository file was modified by this review.
- The reviewed tree therefore matches the frozen commit except for the review record that this round appends to.

#### Commands and results

- `./tools/verify.sh docs` → **exit 0**. `Documentation math conventions OK (159 files scanned)`; `Documentation
  links OK (162 files scanned)`; Zensical strict build `No issues found`; DocFX `0 warning(s) 0 error(s)`;
  rustdoc succeeded.
- `./tools/verify.sh policy` → **exit 0**. ApiCompat ran without breaking changes for all projects; contract
  artifacts current (18 files); model projection check OK (5 fixtures); parameterization projection check OK
  (4 fixtures); space-group catalog current (274 settings, sha256
  `1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395`); process consistency OK; 37 Python unit
  tests OK; SPDX headers present in 528 source files; no secret patterns in 890 tracked text files; cargo-deny
  advisories/bans/licenses/sources OK.
- Read-only code/contract inspections (no repository edits):
  - `PairShell.EquivalentImages` exists as `ImmutableArray<Vector4I>` (`src/dotnet/Mocassin.Symmetry/Reduction/PairReduction.cs:24`);
    `PairInteractionPreview` has no image list (`src/dotnet/Mocassin.Symmetry/Energy/PairInteractionPreview.cs:19-32`);
    `CompiledPairLink` is four fields (`src/dotnet/Mocassin.Compilation/Structure/CompiledStructureModel.cs:53-60`);
    `PairCompilationStage` adds one link per shell candidate (`src/dotnet/Mocassin.Compilation/Energy/PairCompilationStage.cs:120-126`);
    the model-signature link projection writes four fields including the full `Vector4I` offset
    (`src/dotnet/Mocassin.Compilation/Signatures/ModelSignatureProjection.cs:129-136`).
  - `CompiledJumpRule.TrackerOrder` is `ImmutableArray<byte>` (`src/dotnet/Mocassin.Compilation/Transitions/CompiledTransitionModel.cs:70-82`);
    `BuildTrackerOrder` allocates `rule.Movement.Length` bytes and emits `0xFF` for non-mobile nodes
    (`src/dotnet/Mocassin.Compilation/Simulations/EncodingCompilationStage.cs:173-206`); the model projection
    writes the variable length.
  - `DiagnosticCatalog.Default` is in `Mocassin.Packages.Projects.Validation` and composes Core/Symmetry/
    Compilation/Step 07 (`src/dotnet/Mocassin.Packages/Projects/Validation/DiagnosticCatalog.cs:48-55`);
    `Mocassin.Results.csproj` references only `Mocassin.Core` and `Mocassin.Contracts`;
    `DiagnosticCatalogTests.DefaultIsTheUnionOfTheDomainCatalogsAndStep07Codes` pins the exact composition and
    `DiagnosticDocumentationTests` pins the code set to `docs/reference/diagnostics.md`.
  - `contracts/manifest.json`: `signatures.lattice` is still the Step 00 skeleton with `dopingInfo`; `formats.package`
    has no `payloads` key; `formats.package.tables.jobs` still has `norm_factor`/`base_frequency_hz`;
    `formats.package.unknownFields` is `reject`. The generated `package-v1.schema.json` is the row schema.
  - `tools/contracts/generate.py` validates `fields`/`tables`/`events`, derives `additionalProperties` from
    `unknownFields`, and emits one schema artifact per format; `check_model_projection.py` globs
    `tests/contracts/projections/*.bin` non-recursively (`:21`), so a `lattice/` subdirectory is excluded.
  - `tests/contracts/signature-vectors.json` has 9 vectors (5 model, 4 parameterization, no lattice);
    `samples/jobs/ceria-temperature-sweep.mocprj` has `parameterizations: []` and six jobs (2+2+1+1);
    `Directory.Packages.props` versions `Microsoft.Data.Sqlite`, `MessagePack`, and `ZstdSharp.Port`, and no
    project currently references the latter two; `.gitignore` has the `.msl.tmp-*` and `.msl` fixture exceptions.
  - Fence audit of the changed pages: `step-13-rust-solver-foundation-and-package-reader.md` has **11** fence
    lines (odd/unbalanced); the other changed step pages and `02-data-contracts.md` are balanced.

#### Round-1 finding closure

| Finding | Round-2 status | Evidence |
| --- | --- | --- |
| M-1 | **Closed in design.** Pair entries are one row per periodic image with `{target_position, offset:[i32;3], energy_id}`; `PairShell.EquivalentImages` is the real image list; the compiler change is feasible because every shell candidate carries a `PairShell`; the four-field model-signature link projection signs each image, so the regenerated Step 08/09 vectors and sample `modelSignature` values are the correct consequence. Step 08 §6, Step 14 §4, and Step 15 §4 are aligned. | `02-data-contracts.md:926-939`; Step 11 `:249-254`, `:463-473`; `PairReduction.cs:24`; `EnergyGeometrySampler.cs:222-290`; `ModelSignatureProjection.cs:129-136`; Step 08 `:316-320`; Step 14 `:132-145`. Residual R2-3. |
| M-2 | **Closed in design.** `tracker_order` is a variable `u8[]`/`Vec<u8>` with one global tracker index or `0xFF` per node; the compiled source is the variable `CompiledJumpRule.TrackerOrder`; Step 13, Step 18, and the model-signature note are aligned. | `02-data-contracts.md:990,1014-1018`; Step 11 `:267-270`; Step 13 `:86,112-113,160-162`; Step 18 `:83-88`; `CompiledTransitionModel.cs:82`; `EncodingCompilationStage.cs:173-206`. Residual R2-2/R2-3. |
| M-3 | **Closed in design.** `PKG` descriptors are Contracts-owned; `Mocassin.Packages` composes them into `DiagnosticCatalog.Default`; `Mocassin.Results` uses the same descriptors directly and keeps `Core + Contracts` only. | Step 11 `:424-429`; `01-target-architecture.md:134-140`; `DiagnosticCatalog.cs:48-55`; `Mocassin.Results.csproj`; `05-open-decisions.md` package-inspection row. R2-9 notes the exact-union test consequence. |
| m-1 | **Closed.** `PKG001`–`PKG010` have severities, checks, and docs anchors; the check list is numbered and mapped. | Step 11 `:370-417`. |
| m-2 | **Closed.** `package-summary-v1` is defined with schema, model/lattice/job entries, normalization, sort order, and golden location. | `02-data-contracts.md:1115-1159`; Step 11 `:305-314`. |
| m-3 | **Closed.** ADR 0009 now records the pre-release package-shape finalization and the first-release resumption. | ADR 0009 `:68-80`; Step 11 `:433-437`; `05-open-decisions.md` package-shape row. |
| m-4 | **Closed.** Step 14 is in the dependency reconciliation and §4 states the exact `cell + offset` expansion. | Step 11 `:105-106,111-121`; Step 14 `:132-145`. |
| m-5 | **Closed.** A separate `contracts/generated/schema/package-payloads-v1.schema.json` is specified; the row schema stays separate; payload schemas use `additionalProperties: true`. | Step 11 `:441-448`; `generate.py:373-412,508-524`; `package-v1.schema.json`. R2-8 notes the manifest-policy gap. |
| m-6 | **Closed.** Step 11 §9 gives the folder/namespace/file plan, and it respects the 20-file, depth-4, 800-line, and suffix rules. | Step 11 `:520-538`; `tools/structure-policy.json`. R2-6 notes the PublicAPI task wording. |
| n-1 | **Closed.** `PackageInspection`, `PackageVerificationReport`, `PackageVerificationCheck`, `PackageVerificationOptions`, `IMslPackageInspector`, and `IPackageVerifier` are defined; `PackageWarning` is gone. | Step 11 `:322-363`. |
| n-2 | **Closed.** Orphaned temp files are removed by `mocassin clean` (Step 28), stated explicitly. | Step 11 `:223-225`; Step 12 `:62`. |
| n-3 | **Closed.** The `usePrerun` rule is qualified by kind; MMC requires `pre_run_mcsp = 0`, which the resolver and validation permit for valid jobs. | Step 11 `:400-401`; `SolverJobFlagsMapper.cs:59-96`; `JobSettingsResolver.cs:70-96`. |
| n-4 | **Closed.** Only payload-derived columns are compared; authored/build metadata is checked for presence/format/uniqueness. | Step 11 `:395-399`; `02-data-contracts.md:839-840`. |
| n-5 | **Closed in design.** The new `ZstdSharp.Port`/`Microsoft.Data.Sqlite` edges are explicitly licence-gated in the feature that adds them; neither is currently referenced. | Step 11 `:480-482,580`; `Directory.Packages.props:18,30`; csproj grep. |

#### Findings

| ID | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| **R2-1** | **Minor** | The Step 13 page has an unbalanced code fence introduced by the M-2 edit: the `JumpRule` block is closed at line 106, the `JobPackage` struct then sits outside a fence, and the original closing fence at line 124 is orphaned. The page has 11 fence lines (odd); the rendering swallows/loses the `### 3. .msl reader` block. | `docs/planning/steps/step-13-rust-solver-foundation-and-package-reader.md:106,115-124`; `grep -c '^```'` = 11; `./tools/verify.sh docs` does not detect fence balance. |
| **R2-2** | **Minor** | The new `PairLink.offset` comment still says “representative relative cell image”, contradicting the per-image M-1 contract (one entry per periodic image, not only the shell representative). | Step 13 `:89-93`; Step 11 `:249-254`; `02-data-contracts.md:926-939`. |
| **R2-3** | **Minor** | Two verifier/reader validations reference facts the payload does not carry: (a) “the shell representative is present” (check 5) cannot be evaluated because `distance_bucket_fm`, the representative marker, and any symmetry reference are absent and `Mocassin.Results` references only `Core + Contracts`; (b) “`tracker_order.Length` equals the rule's node count” has no independent source because `jump_rules` has no node count or representative-direction id and the state codes do not uniquely encode trailing void nodes. Either persist a node count/representative marker (and define the derivation), or narrow the checks to what the payload proves. | Step 11 `:384-388,600`; Step 13 `:160-162`; `02-data-contracts.md:988-990`; `Mocassin.Results.csproj`. |
| **R2-4** | **Minor** | Step 11 §6 item 4 records removal of `jump_collections.object_id`, but §3.3 never contained that field at either the round-1 or the frozen commit; only `jump_directions.object_id` exists. The delta is stale and will confuse the implementation/audit. | Step 11 `:451-453`; `02-data-contracts.md:988` (also at `d8ade7a`); `grep -n object_id` finds only energy tables and `jump_directions`. |
| **R2-5** | **Note** | The request's `Step summary` sizing line still says “3.5–4.0 person-weeks … within the roadmap §3.1 … trigger”, while the revised design §10 says the honest estimate is 3.8–4.2 pw and the upper bound exceeds the four-week trigger. The request owner should update the summary so the start-gate decision is presented consistently. | `docs/planning/reviews/step-11.md` `Step summary`; Step 11 `:540-557`; `04-roadmap.md` §3.1 Step 09 precedent. |
| **R2-6** | **Note** | The task “Update `PublicAPI.Unshipped.txt` and the ApiCompat baseline for all three projects” is narrower than the actual API surface: the change also adds a public member to `Mocassin.Symmetry.PairInteractionPreview` and to `Mocassin.Core` (`JobExtension.ToCanonicalJson`). §6 item 10 mentions the Symmetry/Compilation baseline, but the task line should name Core/Symmetry explicitly. | Step 11 `:531,579`; `Mocassin.Symmetry/PublicAPI.Unshipped.txt:494-501`; `src/dotnet/Mocassin.Core/Model/Jobs/JobExtension.cs`; Step 11 §6 item 10. |
| **R2-7** | **Note** | The publication barrier requires an `fsync` of the containing directory (`:221`) without naming the mechanism or the platform consequence. .NET has no managed directory-fsync API; a Unix P/Invoke is a platform-specific trigger under `AGENTS.md` §3.13 and needs a recorded platform-gate/CI decision (Windows has no equivalent). | Step 11 `:208-225,571`; `AGENTS.md` §3.13; risk table `:676`. |
| **R2-8** | **Note** | The new payload schemas are specified as `additionalProperties: true`, while `formats.package.unknownFields` is `reject` and `generate.py` derives `additionalProperties` from that field. The manifest does not express the payload unknown-key policy; the implementation must hard-code the exception in the generator, which weakens the “manifest is the single source” claim. | Step 11 `:441-448`; `contracts/manifest.json` `formats.package.unknownFields`; `tools/contracts/generate.py:373-412`. |
| **R2-9** | **Note** | Adding `PackageDiagnosticDescriptors` to `DiagnosticCatalog.Default` will break the exact-union test `DefaultIsTheUnionOfTheDomainCatalogsAndStep07Codes`; the design's diagnostic-ownership test row mentions appearance in `Default` and the docs page but not the exact-composition expectation. The implementation should update that expectation explicitly (it is not a defect in the design, just an unlisted test change). | `src/dotnet/Mocassin.Packages.Tests/Projects/Validation/DiagnosticCatalogTests.cs:17-31`; `DiagnosticDocumentationTests.cs:17-45`; Step 11 `:656`. |

No Blocker or Major finding remains. R2-1–R2-4 are small documentation or verifier-derivation corrections; R2-5–R2-9 are observations for the implementation and start gate.

#### Verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Waived

**Ready with notes.** The three round-1 Majors (M-1, M-2, M-3) are closed in the design with evidence in the
shipped code and contracts, the remaining round-1 Minors and Notes are resolved, and both required gates pass.
The four Minor findings are documentation or verifier-detail corrections that do not change the package format,
the writer/verifier boundary, or the cross-step solver contract; the five Notes are for the implementation and
the start-gate presentation. The human owner may accept the notes, or the planner may correct R2-1–R2-4 and
append a further round; this reviewer does not approve the start gate.

Follow-up owner: Human owner (accept `Ready with notes`); planner/step agent for R2-1–R2-4 if corrected.
Follow-up step: Step 11 — start-gate decision; no re-review is required for the Notes.
