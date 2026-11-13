# Step 08 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-08.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record
> state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 08 — Model Compilation Pipeline |
| Request owner | Design planner (Step 08) |
| Orchestrator (step agent / planner) | Design planner |
| Review initiated by | Planner |
| Reviewed commit | `b84ceadda1ba8b1715592a4f6884d76bc7c36fed` (round 6; round 5 reviewed `850e98d6788830ee8599a80df966f075f88112a1`; round 4 `9715082c1d51e8273bf7c636c22593fa0f7ba5e6`; round 3 `b030cfb7d921277f6983651fb451e736cc2f7896`; round 2 `c676716e88bcdd5e8877938e21ad6950cc2986bb`; round 1 `36fbba58182b2e769a6d59b3228a60021344248d`) |
| Docs branch | `docs/step-08-design` |
| Review date | 2026-11-13 |
| Reviewer | Independent planning auditor (round 6; fresh context; no shared conversation with the planner or implementor; rounds 1–5 were separate fresh reviewers) |

Step page: [`Step 08 — Model Compilation Pipeline`](../steps/step-08-model-compilation-pipeline.md)

> Orchestration: the step agent or planner (the orchestrator) freezes the design, requests the reviewer, applies
> fixes autonomously on the design docs or a maintenance branch, adds resolutions without altering reviewer
> text, and requests re-review until the reviewer no longer identifies issues. The human owner approves the
> final verdict and the start of implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 02, 03, 04, 05, 06, 07 |
| Delivers | A pure, synchronous, staged compiler from a validated `Project` to a deterministic `CompiledModel` with P1 structure, pair/cluster tables, transition rules, simulation encoding, dense indices, signatures, caches, and `CMP` diagnostics. Step 08 is a library plus a canonical JSON inspection projection; Step 12 owns the CLI. |

## Design decisions reviewed

- [x] Step page design decisions — reviewed; the pipeline, API, diagnostics, and feature breakdown are coherent, but M1/M2/M5 affect the stated derivation, field convention, and sizing claim. Round 2: M1/M4/M5/M6 are remediated; the M2 vacancy case remains (Major). Round 3: M2 is resolved — the particle-identity vector matches the original `Σ q_i movement_i`; the risk table still describes the removed scalar (m12). Round 4: m12 is resolved — the risk row now matches §8.4 and §3.3; the remaining m13 is a cache-graph nuance, not a design-decision issue. Round 5: the m13 Indexing/Signing halves are correct at `850e98d`; the new site-energy row's reuse claim is m14. Round 6: m14 is resolved at `b84cead` — the site-energy row now rebuilds Environments, Pairs, Clusters, Encoding, and Indexing and reuses Structure, Rules, and Signing; no new design-decision contradiction found.
- [x] Data contracts and format changes — reviewed; the existing `model` signature entry and the new `compiledModel` task conflict (M4), and the signature-scope wording differs from Step 09 (m6). Round 2: M4 resolved against `contracts/manifest.json` and `tools/contracts/generate.py`; m6 remains in summary pages (`05-open-decisions.md` row 50, `architecture.md` line 147, parity-matrix line 132). Round 3: m6 is resolved in §1.3/§3.3, Step 09, every summary page, and the parity row; `charge_transport_vector` replaces `field_factor`. Round 4: m10 is resolved in §1.3/§3.3 and the summary pages; `charge_transport_vector:[f64;3]` remains the §3.3 field and Step 15 input. Round 5: `signatures.model`, `charge_transport_vector:[f64;3]`, and Step 09's `ModelSignature` are unchanged at `850e98d`. Round 6: re-checked at `b84cead`; `signatures.model` is still the existing entry, §3.3 still stores `charge_transport_vector:[f64;3]`, and the m14 delta is cache text only.
- [x] Architecture and module boundaries — reviewed; `Mocassin.Compilation` is pure and the documented folder layout follows `code-structure.md`; the internal resolver reference is m1. Round 2: m1 resolved via public `IStructurePreviewService.TryResolve`; the additive Symmetry API's PublicAPI/ApiCompat entries are only covered generically (n5). Round 3: n5 is addressed (the task names the `Mocassin.Symmetry` members); `compilation-pipeline.md` still says indexing excludes runtime settings (m10). Round 4: m10 is resolved — `compilation-pipeline.md` now keys indexing on the simulation hash and encoding on the energy-derived hash; m13 records the site-energy sub-case of the energy graph row. Round 5: that page now also names the energy hash in the indexing key and the energy-value exclusion in signing, but its "editing energy rebuilds environments/pairs/clusters" sentence still contradicts the step page's site-energy reuse claim (m14). Round 6: `compilation-pipeline.md` now says editing energy rebuilds environments/pairs/clusters/encoding/indexing and that site-energy-only edits reuse signing, which matches the graph and the keys.
- [x] ADRs and open decisions — reviewed; ADR 0010 domain separation is followed and the open-decision rows record the compiler/signature/cache/intent boundaries. Round 2: ADR 0010 remains followed; row 50 still describes the compiled signature as `geometry/rules` rather than geometry/rules/encoding (m6). Round 3: row 50 now says geometry/rules/encoding and excludes runtime settings (m6 resolved). Round 5: ADR 0010 domain separation and the row-50 wording are unchanged at `850e98d`. Round 6: unchanged at `b84cead`; ADR 0010 domain separation and the row-50 `geometry/rules/encoding` wording still hold.
- [x] Quality strategy and test plan — reviewed; the test families are strong, but the field-factor/dual-DOF tests encode M1/M2 and the cache row does not exercise simulation-transition edits (M3). Round 2: M1 and M3 are covered; the Step 08 Tests table still has no field-factor row, and the quality strategy's `-q` vacancy assertion is not derivable (M2). Round 3: §9.3 and the Step 08 acceptance/test table now pin `qΔx`, the vacancy sign, zero identical-ion exchange, and non-zero distinct-charge/non-collinear paths; the `-q` claim is gone (M2 resolved). Round 4: m11 is resolved — Step 10 now matches the original every-mask accumulation; m13 affects the cache-test expectation, not the charge-transport coverage. Round 5: the charge-transport and Step 10 coverage is unchanged; the §9.3 recording-cache assertion still cannot cover the site-energy row as written (m14). Round 6: the corrected row is assertable — a site-energy-only edit misses Environments/Pairs/Clusters/Encoding/Indexing and reuses Structure/Rules/Signing, matching the stated keys.
- [x] Parity matrix and documentation plan — reviewed; the Step 08 parity rows and the named documentation pages are present. Round 2: the dual-DOF parity row is updated; the stable-signature row still says `geometry/rules` (m6). Round 3: the parity row now says geometry/rules/encoding (m6 resolved). Round 5: the parity rows are unchanged at `850e98d`. Round 6: unchanged at `b84cead`; the Step 08 parity rows and named documentation pages are intact.
- [x] Licensing and provenance — reviewed; no new dependency or GPL-derived content is introduced, and the SPDX/PublicAPI task is recorded. Round 2: unchanged; no new dependency or GPL-derived content. Round 3: unchanged. Round 5: unchanged. Round 6: unchanged; the SPDX/PublicAPI task and the embedded catalog provenance remain as recorded.
- [x] Post-implementation audit plan — reviewed; the `golden-models-and-closeout` feature records the audit request and closeout work. Round 2: the feature and closeout tasks remain present. Round 3: unchanged. Round 5: unchanged. Round 6: unchanged; no `docs/planning/audits/step-08.md` exists, which is correct for a step that has not started.

## Dependency and interface checks

- [x] Dependencies are `Done` and their contracts are stable — Steps 02–07 are Done and their public preview/validation APIs are implemented; the internal-resolver and path-point details are m1/M6. Round 2: re-checked the implemented APIs; the named services and signatures exist, and m1/M6 are resolved. Round 3: re-checked `ProjectHasher` (no `ComputeSimulationSelection` yet; named as a new Core producer), `ClusterReduction.MaxTupleCount`, private `TransitionPathResolver.FindMatches`/`TryGetComponentShift`, `Diagnostic.Related`, and `SolverJobFlagsMapper.Compose`. Round 4: re-checked the same APIs at `9715082`; `ProjectHasher` still has no `ComputeSimulationSelection` and it remains the named new Core producer; `TransitionPathResolver`, `ClusterReduction`, `Diagnostic.Related`, and `SolverJobFlagsMapper.Compose` are unchanged. Round 5: re-checked at `850e98d`; the public `TransitionPathResolver` surface is still `TryCreate`/`TryResolve`/`Encoder` with private `FindMatches`/`TryGetComponentShift`, `ClusterReduction.MaxTupleCount` is still 1,000,000, and `ProjectHasher` still has no `ComputeSimulationSelection`. Round 6: re-checked at `b84cead`; `ProjectHasher` has `Compute`/`ComputeStructure`/`ComputeDocument`/`ComputeEnergy`/`ComputeTransition` and still no `ComputeSimulationSelection`, `ClusterReduction.MaxTupleCount` is 1,000,000, and the resolver surface is unchanged.
- [x] Inputs, outputs, types, and formats are defined — mostly defined, but the field-factor units (M2), manifest signature identity (M4), and Step 09 `model.Signature` name (m5) are ambiguous. Round 2: M4 and m5 are resolved; the M2 path-vector/vacancy definition and m6 summary drift remain. Round 3: M2 and m6 are resolved; `charge_transport_vector:[f64;3]` is defined in §3.3 and consumed by Step 15. Step 10's dual-DOF rule misstates the original mixed case (m11). Round 4: m11 is resolved — Step 10 now clears dual-DOF only when every selection mask is reduced; `charge_transport_vector:[f64;3]` and the Step 15 consumption are unchanged. Round 5: unchanged at `850e98d`; the field, signature, and dual-DOF texts remain aligned. Round 6: unchanged at `b84cead`; `charge_transport_vector:[f64;3]` remains the §3.3 field and the Step 15 input.
- [x] Versioning and migration behavior is defined — `model_signature` is additive before the first released `.msl`; `format_version` stays `1`, and `models.signature` remains the Step 09 parameterization signature. Round 2: unchanged and consistent with `02-data-contracts.md` §3.3 and Step 11. Round 3: unchanged. Round 5: unchanged. Round 6: unchanged at `b84cead`; no format or version delta in the m14 commit.
- [x] No unresolved cross-step interface question remains — M3, M4, M6, m1, m2, m5, and m6 remain. Round 2: M3, M4, M6, m1, m2, and m5 are resolved; the M2 field-factor definition and the m6 summary drift remain. Round 3: all named interfaces exist and M2/m6 are resolved; the remaining findings are cache/summary-consistency Minors (m10–m12), not interface gaps. Round 4: m10–m12 are resolved; m13 is a cache-graph/test-expectation Minor, not a cross-step interface gap. Round 5: m14 is the same class of cache-graph expectation, not an interface gap. Round 6: m14 is resolved; no open cross-step interface question remains.

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable — scope/non-goals are explicit and the acceptance table is testable; the sizing claim is the exception (M5). Round 2: the sizing statement now records the split trigger and the owner's recorded waiver; acceptance criteria remain testable. Round 3: the five charge-transport cases are acceptance criteria and tests. Round 4: unchanged; m13 is a cache-test detail, not an acceptance gap. Round 5: unchanged; m14 is likewise a cache-graph expectation. Round 6: unchanged; all acceptance criteria remain testable and m14 is closed.
- [x] Design decisions are complete and internally consistent — M1, M2, M3, M4, and M6 are material contradictions; m5/m6 are interface wording. Round 2: M1/M3/M4/M6 are resolved; the M2 vacancy contradiction remains. Round 3: M1–M4/M6 and M2 are resolved; the stale risk-row wording (m12) and the compilation-page indexing key (m10) are small remaining inconsistencies. Round 4: m10 and m12 are resolved; m13 is the only remaining inconsistency, in the site-energy sub-case of the invalidation graph. Round 5: the Indexing/Signing halves are fixed, but the new site-energy row contradicts the environments/pairs/clusters keys (m14); the other decisions are consistent. Round 6: m14 is resolved — every invalidation row now matches its stage keys, and no new contradiction was found in the m14 delta.
- [x] Required tests, property tests, golden fixtures, and performance targets are defined — families and targets are defined, but M2's formula and M1's derivation need tests that do not merely encode the design, and M3's cache test omits simulation-transition edits. Round 2: M1/M3 tests are added and M4/M5 are covered; the field-factor coverage is not in the Step 08 test table and the `-q` vacancy assertion is unsupported (M2). Round 3: the Step 08 test table and §9.3 now cover single charge, vacancy sign, zero identical-ion exchange, non-zero distinct-charge exchange, and non-collinear paths. Round 4: the m11 correction is reflected in Step 10; m13 affects the cache row's site-energy expectation. Round 5: m14 leaves the site-energy expectation unassertable; the other test families are unchanged. Round 6: the §9.3 recording-cache test can now assert the corrected site-energy row (affected stages miss; Structure, Rules, and Signing hit).
- [x] Documentation, status, parity, and audit updates are planned — the named pages, CMP docs, parity rows, status links, and closeout/audit task are present. Round 2: the dependency pages and status are updated; the signature-scope summary drift is m6. Round 3: signature summaries and the parity row are aligned; m12 is a stale risk row, not a plan gap. Round 4: m12 is resolved and the status/page links are current. Round 5: the status and step-page links now point at `850e98d`; m14 is a documentation/key mismatch, not a plan gap. Round 6: the status and step-page links now point at `b84cead`; the documentation plan is unchanged.
- [x] Licensing, provenance, and dependency checks are resolved — no new dependency, SPDX/PublicAPI tasks recorded, and the embedded catalog provenance is unchanged. Round 2: unchanged; the Symmetry public-API baseline entry is only a Note (n5). Round 3: n5 is addressed; unchanged otherwise. Round 4: unchanged. Round 5: unchanged. Round 6: unchanged; the m14 commit introduces no dependency or licensing change.
- [x] Risks and mitigations are realistic — the table covers the main failure modes; the missing risks for M1–M3 are captured by those findings rather than by a separate risk entry. Round 2: the M1/M3 risks are resolved; the field-factor risk row remains accurate but the formula issue is M2. Round 3: the field-factor risk row still describes the removed scalar projection and a "vector change requires a contract decision" (m12); the other risks are realistic. Round 4: m12 is resolved — the risk row now describes the particle-identity vector and its tests; m13 is not a risk-table issue. Round 5: unchanged; the cache-invalidation risk row remains realistic and m14 is a row/key mismatch, not a missing risk. Round 6: unchanged; the risk table still covers the main failure modes.
- [x] Code layout and namespace plan follow the structure policy — the `Compilation/{Compiler,Context,Structure,...}` layout is domain-based and within the documented limits. Round 2: unchanged. Round 3: unchanged. Round 5: unchanged. Round 6: unchanged; the m14 commit is documentation-only.
- [x] No hidden scope, stale placeholders, or unowned decisions — the `compiledModel` manifest task (M4), the benchmark project reference (n1), and the split decision (M5) are unowned/stale. Round 2: M4/n1/M5 are addressed; Step 10's optimizer-clear rule is a Note (n3), and the m6 summary drift remains. Round 3: M4/n1/M5/m6 are addressed; Step 10's original-optimizer rule is still inaccurate (m11) and the risk row is stale (m12). Round 4: m11 and m12 are resolved; m13 is a documented cache nuance, not hidden scope. Round 5: no hidden scope; m14 is a documented cache nuance. Round 6: no hidden scope; the m14 correction is confined to the cache graph and the pipeline summary.

## Step 08-specific checks

- [x] `IModelCompiler`, `CompilationResult`, `CompilationOptions`, cancellation, parallelism, and the
      synchronous/async boundary are defined; exception policy separates user data from programmer errors. —
      API/exception policy is coherent and synchronous; the cancellation granularity is m4. Round 2: m4 is resolved
      by scoping cancellation to stage/top-level/atomic-call boundaries; the API is unchanged. Round 3: unchanged.
      Round 5: unchanged at `850e98d`. Round 6: unchanged at `b84cead`; the API and exception policy are intact.
- [x] The compiler's validation boundary is explicit: it assumes a validated `Project`, does not consume
      `ValidationResult` or duplicate `ProjectValidationPipeline`, and the `IValidationStage`/`Build` owner is
      identified. — Confirmed: Step 12 owns the Build adapter and `mocassin validate` does not compile. Round 2:
      unchanged and consistent with the implemented Step 07 boundary. Round 3: unchanged. Round 5: unchanged.
      Round 6: unchanged; the m14 delta does not touch the validation boundary.
- [x] Stages are reconciled with the actual Step 02/04/05 APIs: P1 expansion and encoders, radial search and
      periodic images, filters, `G0` reduction, chirality, cluster permutation canonicalization, transition
      path resolution, state codes, mass/charge conservation, and forward/inverse/dependent rules. — Mostly
      reconciled, but m1 (internal resolver), m2 (void-last preview order), m3 (Burnside/bound), and M6
      (point-level path resolution) remain. Round 2: m1/m2/m3/M6 are resolved against the implemented APIs;
      the field-factor vacancy case is separate (M2). Round 3: M2 is resolved — the particle-identity vector
      matches the original `Σ q_i movement_i` and Step 15 consumes it; Step 10's rule (m11) is the only
      remaining cross-step detail. Round 4: m11 is resolved — Step 10 now matches the original every-mask
      accumulation; the stage/API reconciliation is unchanged. Round 5: unchanged; the `850e98d` delta is cache text
      only. Round 6: unchanged; the `b84cead` delta is also cache text only, and the named Step 02/04/05 APIs are
      still the ones described.
- [x] `CompiledModel` is reconciled with `02-data-contracts.md` §3.3 and the Step 11 `.msl` writer; persisted,
      JSON-inspection-only, and composed-later data are separated. — The persisted/inspection/composed split
      matches §3.3, but m5 (`model.Signature` vs `ModelSignature`) and m6 (signature scope) need reconciliation.
      Round 2: m5 is resolved in Step 09; m6's normative sections are aligned but the summary pages still say
      `geometry/rules`. Round 3: m5/m6 are resolved; §3.3 stores `charge_transport_vector:[f64;3]` and the
      persisted/inspection split is unchanged. Round 4: unchanged; the §3.3 field and Step 15 consumption remain
      aligned. Round 5: unchanged. Round 6: unchanged at `b84cead`; the persisted/inspection/composed split still
      matches §3.3.
- [x] Dense indexing, stable geometry/rule IDs, model signature, the Step 09 parameterization-signature
      boundary, and byte-identical determinism are defined. — Index rules and deterministic ordering are
      defined; M4 (manifest identity) and m6 (active-collection scope) remain. Round 2: M4 is resolved; the
      normative m6 wording now includes encoding/active collections, but the summary drift remains. Round 3:
      M4/m6 are resolved; the signature is geometry/rules/encoding and excludes runtime settings. Round 4: m10
      is resolved for the simulation-hash/encoding keys; m13 records the site-energy sub-case of the graph.
      Round 5: the signature still excludes energy values and runtime settings; the remaining inconsistency is the
      cache graph's reuse column (m14), not the signature projection. Round 6: the cache graph now matches the
      signature exclusion, so the signature boundary and the graph agree.
- [x] The derived solver intent (`UseDualDofCorrection`, `UsePrerun`, kind) is defined and Step 11 owns the
      `.msl` flag word. — `UsePrerun`/kind and the composition boundary are correct; M1 is a material divergence
      for `UseDualDofCorrection`. Round 2: M1 is resolved — `UseDualDofCorrection = Kind == Kmc` matches the
      original job builder; Step 10's clear rule is a Note (n3). Round 3: M1 remains resolved; n3 is only
      partially addressed — Step 10's mixed-case rule does not match `JumpSelectionOptimizer.Run` (m11). Round 4:
      m11 is resolved — Step 10 now clears only when every selection mask is reduced and keeps the mixed/none
      case, matching the original accumulation; Step 08's `Kind == Kmc` default is unchanged. Round 5: unchanged;
      Step 10 still matches the every-mask rule. Round 6: unchanged at `b84cead`; Step 10 and Step 08 still agree.
- [x] Tracker mappings emitted by Step 08 and mutable tracker state owned by Step 18 are separated. —
      Confirmed: global/static definitions and `tracker_order` are Step 08; Step 18 owns values, mobile
      assignment, and `.mcs` output. Round 2: unchanged. Round 3: unchanged. Round 5: unchanged. Round 6: unchanged;
      the Step 18 boundary is not touched.
- [x] New compile diagnostic codes, their owner prefix, their `DiagnosticCatalog` composition, and their
      reference-docs path are defined. — `CMP001`–`CMP012`, `CompilationDiagnosticCatalog`, the composed
      catalog, and the docs-drift test are defined; m7, m8, and n2 are report/path/integrity details. Round 2:
      m7 (by-reason counts), m8 (transition-kind path) and n2 (integrity predicate) are resolved. Round 3:
      unchanged. Round 5: unchanged. Round 6: unchanged; `CMP001`–`CMP012` and the composed catalog are intact at
      `b84cead`.
- [x] The `mocassin compile` ownership question is resolved explicitly (Step 08 minimal command vs library/test
      harness; Step 12 full CLI). — Confirmed: Step 08 library plus pure `CompiledModelJson`; Step 12 owns the
      command and file output. Round 2: unchanged; Step 12 ownership remains explicit. Round 3: unchanged. Round
      5: unchanged. Round 6: unchanged; Step 08 remains library plus `CompiledModelJson` only.
- [x] Cache keys, invalidation graph, lifetime, thread safety, and tests are defined. — Lifetime/thread safety
      are defined, but the simulation-transition invalidation path and missing simulation hash are M3. Round 2:
      M3 is resolved by the active-transition-set hash; the Resolve/Indexing key wording against the graph is a
      Minor (m9). Round 3: Resolve is uncached and Indexing/Signing are keyed as stated, but m9 is only partially
      resolved — `compilation-pipeline.md` says indexing excludes runtime settings while the step page keys
      Indexing on the simulation hash (m10). Round 4: m10 is resolved; m13 is the remaining site-energy
      Indexing/Signing sub-case of the energy row. Round 5: `Indexing` and `Signing` now match the energy
      sub-case; the environments/pairs/clusters reuse claim still contradicts their keys (m14). Round 6: m14 is
      resolved — the site-energy row rebuilds Environments, Pairs, Clusters, Encoding, and Indexing (all keyed on
      the energy hash) and reuses Structure, Rules, and Signing, so the graph, keys, and pipeline page agree.
- [x] Diagnostics/report fields, stage counts/timings, and realistic performance targets are defined. — Report
      fields and targets are concrete; m7 notes the missing by-reason rejection breakdown. Round 2: m7 and m4
      are resolved; the report now splits rejected counts by reason and qualifies the cancellation target.
      Round 3: unchanged. Round 5: unchanged. Round 6: unchanged; the report fields and targets remain concrete.
- [x] The feature list has roughly 4–8 auditable features with the required branch names, all `Not started`;
      the sizing estimate and split trigger are recorded. — Eight required branches are present and all are
      `Not started`; 4–6 pw/eight features exceed the recorded split trigger (M5). Round 2: the owner waiver is
      recorded on the Step 08 page, `status.md`, and the resolution table; no split is claimed silently. Round 3:
      the owner waiver remains recorded and all eight rows are `Not started`. Round 4: unchanged. Round 5: all
      eight rows remain `Not started`, the waiver is still recorded on the page and in `status.md`, and no step
      or feature branch exists. Round 6: re-verified at `b84cead`; eight branches, all `Not started`, the waiver
      is still recorded, and `git branch -a` shows only `docs/step-08-design`, `main`, and `origin/main`.
- [x] No production compiler code, step branch, or pre-seeded review verdict exists. — Confirmed:
      `Mocassin.Compilation` contains only `CompilationModule.cs`, the test project only has `SmokeTests.cs`,
      no `step/08...` branch exists, and this record contains no verdict before this round. Round 2: re-verified
      at `c3b44f7`; all eight feature rows remain `Not started` and no step or feature branch exists. Round 3:
      re-verified at `b05c8b5`; only `CompilationModule.cs` and `SmokeTests.cs` exist and no step/feature branch
      exists. Round 4: re-verified at `053db7a`; the compiler/test projects contain only the module marker, smoke
      test, PublicAPI files, and project files, and no step or feature branch exists. Round 5: re-verified at
      `bd4046c`; the compiler/test projects contain only `CompilationModule.cs`, `SmokeTests.cs`, the PublicAPI
      files, and the two project files, and `git branch -a` has no step or feature branch. Round 6: re-verified at
      `b84cead`; the same six files remain, all eight feature rows are `Not started`, no step/feature branch or
      Step 08 audit record exists, and no implementation code or test was added after the freeze.

## Evidence

**Review round 1** — frozen design commit `36fbba58182b2e769a6d59b3228a60021344248d`; review-request/current
tip `91d74208a92de7eab1000d25de49c616c521b6ed`. I am a fresh context: I did not author or implement this
design, did not participate in its preparation, and used no handoff, summary, or prior conversation. The
worktree `/workspace/playground/step08-design` was clean at `91d7420`; all evidence is from repository
inspection at that state and from the frozen design content at `36fbba5`.

Files reviewed:

- `AGENTS.md` §3.8–§3.9, §3.15, §7.2, §8, §11, §13, §16; `docs/planning/reviews/README.md`; the review
  template; the Step 08 review record and Step 08 page.
- Steps 02–07 pages, especially Step 02 §4–§7 (P1, encoder, `G0`/`Gset`/`Gseq`, pair/cluster reduction,
  periodic images), Step 04 §2–§7 (state changes, chains, `GeneratesReverse`, path resolver), Step 05 §5–§7
  and §10 (`IEnergyGeometrySampler`, pair/cluster tables, `EnergyId`, preview limits), Step 06 §1–§7
  (simulations, `ExecutionFlags`, `SolverJobFlags`), and Step 07 §2–§7 (validation stages, Build boundary,
  statistics). Also Steps 09–12 and 14–18 for the signature, package, build, energy, KMC, and tracker
  boundaries.
- `docs/planning/reviews/step-07.md` (accepted standard and finding history).
- `docs/planning/02-data-contracts.md` §1.3, §2.3, §3.1–§3.3, §3.6, §5.1, §5.6, §6; `docs/development/architecture.md`,
  `design-decisions.md`, `compilation-pipeline.md`, `code-structure.md`, `testing-and-parity.md`;
  `docs/planning/03-quality-strategy.md` §9.3 and §13; `docs/planning/04-roadmap.md` §3.1;
  `docs/planning/05-open-decisions.md`; `docs/planning/appendix-b-parity-matrix.md`;
  `docs/planning/status.md`; `docs/reference/diagnostics.md`, `docs/reference/cli.md`.
- ADRs 0009, 0010, 0011, 0013, 0014; `NOTICE.md`; the SPDX/licence rules.
- Implemented APIs at the frozen commit: `Mocassin.Compilation/CompilationModule.cs` and both `PublicAPI` files;
  `Mocassin.Symmetry/Energy/{IEnergyGeometrySampler,EnergyGeometrySampler,EnergyPreviewService}.cs`,
  `Reduction/{PairReduction,ClusterReduction,PointGroup}.cs`, `Transitions/{TransitionPathResolver,TransitionPreviewService}.cs`,
  `Structure/{SpaceGroupReferenceResolver,StructurePreviewService}.cs`, `Geometry/{UnitCellVectorEncoder,PeriodicGeometry}.cs`,
  `Contracts/SymmetryVersionInfo.cs`, `Diagnostics/*`, `AssemblyInfo.cs`;
  `Mocassin.Core/Project/{Project,ProjectHasher,ProjectCanonicalizer}.cs`, `Diagnostics/{Diagnostic,DiagnosticBag,ModelPath,DiagnosticDescriptor}.cs`,
  `Model/Particles/Particle.cs`, `Model/Transitions/*`, `Model/Simulations/*`;
  `Mocassin.Packages/Projects/Validation/*` (`DiagnosticCatalog`, `IValidationStage`, `ValidationStage`,
  `ProjectValidationPipeline`), `Mocassin.Packages/Jobs/SolverJobFlagsMapper.cs`;
  `Mocassin.Contracts/Solver/SolverJobFlags.cs`; `Mocassin.Compilation.Tests/SmokeTests.cs`;
  `Mocassin.Packages.Tests/Projects/Validation/*`; `contracts/manifest.json`, `tools/contracts/generate.py`,
  `contracts/generated/signatures/model-v1.json`, and `contracts/generated/fixtures/descriptors.json`.
- Read-only original reference used only to check behaviour: `JobDbModelBuilder.SetSimulationJobInfoFlags`
  (dual-DOF), `JumpSelectionOptimizer`/`RunPostBuildOptimizers`, and the solver `INFO_FLG_DUALDOF` comment.

Commands run (from `/workspace/playground/step08-design`):

```sh
git status --short
# clean

python tools/check_process_consistency.py
# Process consistency: OK

python tools/check_docs_links.py
# Documentation links OK (149 files scanned).

zensical build --strict
# Build started
# No issues found
# Build finished in 0.30s

git diff --check
# no output; exit 0

python tools/contracts/generate.py --check
# Contract artifacts are current (18 files)

git diff --stat 36fbba5..HEAD
# pre-record-commit request state (the record commit below adds this review text):
# docs/planning/reviews/step-08.md                   | 187 +++++++++++++++++++++
# docs/planning/status.md                            |   8 +-
# docs/planning/steps/step-08-model-compilation-pipeline.md | 2 +-
# 3 files changed, 195 insertions(+), 2 deletions(-)

git diff --name-only 36fbba5..HEAD
# docs/planning/reviews/step-08.md
# docs/planning/status.md
# docs/planning/steps/step-08-model-compilation-pipeline.md

git branch -a
# * docs/step-08-design
# + main
#   remotes/origin/main

find src/dotnet/Mocassin.Compilation -type f -name '*.cs' -not -path '*/obj/*'
# src/dotnet/Mocassin.Compilation/CompilationModule.cs

find src/dotnet/Mocassin.Compilation.Tests -type f -name '*.cs' -not -path '*/obj/*'
# src/dotnet/Mocassin.Compilation.Tests/SmokeTests.cs
```

Targeted read-only checks:

```sh
grep -n "compiledModel\|signatures\|REQUIRED_SIGNATURES" tools/contracts/generate.py contracts/manifest.json
# `REQUIRED_SIGNATURES = {"model", "parameterization", "lattice"}`; the manifest already has `signatures.model`
# with the same `mocassin/model-signature/v1` prefix and a `model-v1` compiled-model fixture for Step 08.

grep -n "GetParticleOrder" src/dotnet/Mocassin.Symmetry/Transitions/TransitionPreviewService.cs
# lines 408-416: authored particles ascending, then `project.VoidParticle.Id` — void is last, not index 0.

grep -n "TryResolve\|FindMatches\|TryGetComponentShift" src/dotnet/Mocassin.Symmetry/Transitions/TransitionPathResolver.cs
# public `TryResolve(KineticTransition, ...)`; `FindMatches`/`TryGetComponentShift` are private.

grep -n "internal static class SpaceGroupReferenceResolver" src/dotnet/Mocassin.Symmetry/Structure/SpaceGroupReferenceResolver.cs
# line 16: internal; AssemblyInfo exposes only Mocassin.Symmetry.Tests to internals.

grep -n "MaxTupleCount\|InvalidOperationException" src/dotnet/Mocassin.Symmetry/Reduction/ClusterReduction.cs
# fixed `MaxTupleCount = 1_000_000`; `Reduce` always enumerates and has no Burnside or configurable bound.

grep -n "CancellationToken" src/dotnet/Mocassin.Symmetry/Energy/IEnergyGeometrySampler.cs \
  src/dotnet/Mocassin.Symmetry/Reduction/*.cs src/dotnet/Mocassin.Symmetry/Transitions/TransitionPathResolver.cs
# no matches: the sampler/reducers/path resolver are atomic calls without a token.

grep -n "SetSimulationJobInfoFlags\|JobFlags |= dofFlag\|UseDualDofCorrection" \
  /workspace/mocassin-original/src/ModelBuilder/ICon.Model.Translator/Database/EntityBuilder/Main/JobDbModelBuilder.cs \
  /workspace/mocassin-original/src/ModelBuilder/ICon.Model.Translator/Database/Optimization/JumpSelectionOptimizer.cs
# original sets `UseDualDofCorrection` for every KMC job; the optimizer may clear it.

git diff 36fbba5..HEAD -- docs/planning/status.md docs/planning/steps/step-08-model-compilation-pipeline.md
# status/step-page review-request links only; no design change after the freeze.
```

**Re-review round 2** — frozen design commit `c676716e88bcdd5e8877938e21ad6950cc2986bb`; request/current
tip `c3b44f7c66ed39159abe0df2505037081aa2479b`; round-1 record
`520fad1214be76b5fe48d70a83cf2a9bab569af0`. I am a fresh context: I did not author or implement this design,
did not participate in its preparation, and used no handoff, summary, or prior conversation. The worktree
`/workspace/playground/step08-design` was clean at `c3b44f7`; all evidence is from repository inspection at
that state and from the frozen design content at `c676716`.

Files re-read: `AGENTS.md` §3.8–§3.9 and §7.2; `docs/planning/reviews/README.md`; this record (round 1 and
the resolution table) and the Step 08 page at `c676716` in full; the changed sections of Steps 04, 09, 10, 11,
15, 16, 17, and 18; `docs/planning/02-data-contracts.md` §1.3/§2.3/§3.3; the changed sections of
`docs/development/architecture.md`, `design-decisions.md`, `compilation-pipeline.md`,
`testing-and-parity.md`; `03-quality-strategy.md` §9.3/§13; `05-open-decisions.md`;
`appendix-b-parity-matrix.md`; `status.md`.

APIs re-checked at the frozen commit: `TransitionPathResolver` (public `TryCreate`/`TryResolve`/`Encoder`,
private `FindMatches`/`TryGetComponentShift`), `IStructurePreviewService.TryResolve`,
`SpaceGroupReferenceResolver` (internal), `ClusterReduction` (`MaxTupleCount = 1_000_000`,
`ArgumentException`/`InvalidOperationException`), `EnergyPreviewService` (private `BurnsideCount`),
`ProjectHasher` (`Compute`, `ComputeStructure`, `ComputeEnergy`, `ComputeTransition`; no simulation hash),
`contracts/manifest.json` `signatures.model` and `tools/contracts/generate.py` (`REQUIRED_SIGNATURES`),
`TransitionPreviewService.GetParticleOrder`, `SolverJobFlagsMapper.Compose`, `Diagnostic.Related`, and the
read-only original `JobDbModelBuilder.SetSimulationJobInfoFlags`, `JumpSelectionOptimizer.Run`,
`KineticSimulationModelBuilder.GetChargeTransportVector`, and `TransitionModelBuilder.CreateChargeTransportMatrix`.

Commands run (from `/workspace/playground/step08-design`):

```sh
python tools/check_process_consistency.py
# Process consistency: OK (exit 0)

python tools/check_docs_links.py
# Documentation links OK (149 files scanned). (exit 0)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.31s (exit 0)

git diff --check
# no output; exit 0

git status --short
# no output before this record was written; exit 0

python tools/contracts/generate.py --check
# Contract artifacts are current (18 files) (exit 0)

git diff --stat c676716..HEAD
# pre-record-commit request state (this record commit adds the round-2 text):
# docs/planning/reviews/step-08.md                   | 26 +++++++++++++++++++---
# docs/planning/status.md                            |  8 ++++---
# docs/planning/steps/step-08-model-compilation-pipeline.md |  2 +-
# 3 files changed, 29 insertions(+), 7 deletions(-)

git diff --name-only c676716..HEAD
# docs/planning/reviews/step-08.md
# docs/planning/status.md
# docs/planning/steps/step-08-model-compilation-pipeline.md

git branch --list
# * docs/step-08-design
# + main

find src/dotnet/Mocassin.Compilation src/dotnet/Mocassin.Compilation.Tests -type f -name '*.cs' \
  -not -path '*/obj/*'
# src/dotnet/Mocassin.Compilation/CompilationModule.cs
# src/dotnet/Mocassin.Compilation.Tests/SmokeTests.cs
```

The frozen-state check passes: the only changes after `c676716` are this review record, `status.md`, and the
Step 08 page's review link. All eight feature rows are `Not started`, no `step/08-model-compilation-pipeline`
or `feature/step-08-model-compilation-pipeline/*` branch exists, and the compiler/test projects contain only
the module marker and smoke test. No design, contract, code, test, or configuration change was made after the
freeze.

**Re-review round 3** — frozen design commit `b030cfb7d921277f6983651fb451e736cc2f7896`; request/current tip
`b05c8b547d29756b7c65b7cf97a251ae91354a6f`; round-2 record
`eae3bf84b90d4a288599e306b9eb3ade6598ba85`; round-1 record `520fad1214be76b5fe48d70a83cf2a9bab569af0`. I am
a fresh context: I did not author or implement this design, did not participate in its preparation, and used no
handoff, summary, or prior conversation. The worktree `/workspace/playground/step08-design` was clean at
`b05c8b5`; all evidence is from repository inspection at that state and from the frozen design content at
`b030cfb`.

Files read in full or in the relevant sections:

- `AGENTS.md` §3.8–§3.9 and §3.15; `docs/planning/reviews/README.md`; this record, including rounds 1–2, the
  round-1 and round-2 findings tables, and both resolution tables; the Step 08 page at `b030cfb` in full.
- Steps 04, 09, 10, 11, 14, 15, 16, and 17 (signature, job expansion, package flags, context, field/energy,
  KMC/MCC consumption).
- `docs/planning/02-data-contracts.md` §1.3, §2.3, §3.3; `docs/development/architecture.md`,
  `design-decisions.md`, `compilation-pipeline.md`, `testing-and-parity.md`;
  `docs/planning/03-quality-strategy.md` §9.3/§13; `docs/planning/05-open-decisions.md`;
  `docs/planning/appendix-b-parity-matrix.md`; `docs/planning/status.md`.
- Implemented APIs: `Mocassin.Core/Project/ProjectHasher.cs`, `Mocassin.Core/Diagnostics/Diagnostic.cs`,
  `Mocassin.Symmetry/Reduction/ClusterReduction.cs`,
  `Mocassin.Symmetry/Transitions/TransitionPathResolver.cs`,
  `Mocassin.Packages/Projects/Validation/DiagnosticCatalog.cs`,
  `Mocassin.Packages/Jobs/SolverJobFlagsMapper.cs`, `contracts/manifest.json`,
  `contracts/generated/signatures/model-v1.json`, and `tools/contracts/generate.py`.
- Read-only original reference: `KineticSimulationModelBuilder.GetChargeTransportVector` (lines 271–278),
  `KineticTransitionModelBuilder.AddMovementMatrix` (lines 334–347) and `CreateRuleModel` (lines 255–267),
  `TransitionModelBuilder.CreateChargeTransportMatrix` (lines 30–34), `Particle.CreateVoid` (charge `0.0`),
  `JumpSelectionOptimizer.Run` (lines 37–45), and `JobDbModelBuilder.SetSimulationJobInfoFlags`
  (lines 236–250).

Commands run (from `/workspace/playground/step08-design`):

```sh
git status --short
# no output before this record was written; exit 0

python tools/check_process_consistency.py
# Process consistency: OK (exit 0)

python tools/check_docs_links.py
# Documentation links OK (149 files scanned). (exit 0)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.24s (exit 0)

git diff --check
# no output; exit 0

python tools/contracts/generate.py --check
# Contract artifacts are current (18 files) (exit 0)

git diff --stat b030cfb..HEAD
# pre-record-commit request state (this record commit adds the round-3 text):
# docs/planning/reviews/step-08.md                          | 15 +++++++++++++--
# docs/planning/status.md                                   |  7 ++++---
# docs/planning/steps/step-08-model-compilation-pipeline.md |  2 +-
# 3 files changed, 18 insertions(+), 6 deletions(-)

git diff --name-only b030cfb..HEAD
# docs/planning/reviews/step-08.md
# docs/planning/status.md
# docs/planning/steps/step-08-model-compilation-pipeline.md

git branch --list
# * docs/step-08-design
# + main

git branch -a
# * docs/step-08-design
# + main
#   remotes/origin/main

git worktree list
# /workspace/mocassin                  644ee09 [main]
# /workspace/playground/step08-design  b05c8b5 [docs/step-08-design]

find src/dotnet/Mocassin.Compilation src/dotnet/Mocassin.Compilation.Tests -type f \
  -not -path '*/obj/*' -not -path '*/bin/*'
# src/dotnet/Mocassin.Compilation.Tests/Mocassin.Compilation.Tests.csproj
# src/dotnet/Mocassin.Compilation.Tests/SmokeTests.cs
# src/dotnet/Mocassin.Compilation/CompilationModule.cs
# src/dotnet/Mocassin.Compilation/Mocassin.Compilation.csproj
# src/dotnet/Mocassin.Compilation/PublicAPI.Shipped.txt
# src/dotnet/Mocassin.Compilation/PublicAPI.Unshipped.txt
```

Targeted read-only checks:

```sh
grep -rn "field_factor\|fieldFactor\|field_projection\|fieldProjection" docs contracts tools
# only the historical Step 15 "Original reference" names (`ElectricFieldFactor`) and this review record;
# no scalar field input remains in the Step 08/15/data-contract design.

grep -rn "geometry/rules" docs
# every remaining occurrence is `geometry/rules/encoding`; no bare `geometry/rules` summary remains.

grep -n "indexing excludes runtime settings" docs/development/compilation-pipeline.md
# line 195, contradicting the step-page Indexing key and runtime-settings graph row (m10).

grep -n "isReducedMask" /workspace/mocassin-original/src/ModelBuilder/ICon.Model.Translator/Database/
Optimization/JumpSelectionOptimizer.cs
# line 41: `isReducedMask |= optimizedSetMask == environmentDefinition.SelectionParticleMask`
# line 45: returns UseDualDofCorrection when that flag is true (m11).

grep -n "Field-factor approximation" docs/planning/steps/step-08-model-compilation-pipeline.md
# line 844 still says "Documented scalar projection ... vector change requires a contract decision" (m12).
```

The frozen-state check passes: `git diff --name-only b030cfb..HEAD` is exactly this record, `status.md`, and the
Step 08 page's review-status link; the status/page edits are the documented review-request updates. `git
branch -a` has no `step/08-model-compilation-pipeline` or `feature/step-08-model-compilation-pipeline/*` branch,
all eight feature rows remain `Not started`, and the compiler/test projects contain only the module marker,
smoke test, PublicAPI files, and project files.

**Re-review round 4** — frozen design commit `9715082c1d51e8273bf7c636c22593fa0f7ba5e6`; request/current tip
`053db7ab606e6d4f3faee7c03fc7a4a83a157c03`; round-3 record `29951241021abdabccb5ee348f36acac0b5f9438`;
round-2 record `eae3bf84b90d4a288599e306b9eb3ade6598ba85`; round-1 record
`520fad1214be76b5fe48d70a83cf2a9bab569af0`. I am a fresh context: I did not author or implement this design, did
not participate in its preparation or any discussion of its decisions, and used no handoff, summary, or prior
conversation. The worktree `/workspace/playground/step08-design` was clean at `053db7a` before this record was
written; all evidence is from repository inspection at that state and from the frozen design content at
`9715082`.

Files read in full or in the relevant sections:

- `AGENTS.md` §3.8–§3.9 and §3.15; `docs/planning/reviews/README.md`; this record, including rounds 1–3 and all
  findings and resolution tables; the Step 08 page at `9715082` in full.
- Steps 04, 09, 10, 11, 14, 15, 16, and 17 (path API, signature boundary, job/dual-DOF, package, context,
  field/energy, KMC/MMC consumption).
- `docs/planning/02-data-contracts.md` §1.3, §2.3, §3.3; `docs/development/architecture.md`,
  `design-decisions.md`, `compilation-pipeline.md`, `testing-and-parity.md`;
  `docs/planning/03-quality-strategy.md` §9.3/§13; `docs/planning/05-open-decisions.md`;
  `docs/planning/appendix-b-parity-matrix.md`; `docs/planning/status.md`.
- Implemented APIs: `Mocassin.Core/Project/ProjectHasher.cs`, `Mocassin.Core/Diagnostics/{Diagnostic,ModelPath}.cs`,
  `Mocassin.Symmetry/Reduction/ClusterReduction.cs`,
  `Mocassin.Symmetry/Transitions/TransitionPathResolver.cs`,
  `Mocassin.Symmetry/Structure/{IStructurePreviewService,StructurePreviewService}.cs`,
  `Mocassin.Packages/Jobs/SolverJobFlagsMapper.cs`, `contracts/manifest.json`,
  `contracts/generated/signatures/model-v1.json`, and `tools/contracts/generate.py`.
- Read-only original reference: `JobDbModelBuilder.SetSimulationJobInfoFlags` (`IKineticSimulationModel` sets
  `UseDualDofCorrection` for every KMC job), `JumpSelectionOptimizer.Run` (lines 37–45),
  `RunPostBuildOptimizers` (subtracts the returned `removedFlags`), `KineticSimulationModelBuilder.
  GetChargeTransportVector` (lines 271–277), `KineticTransitionModelBuilder.AddMovementMatrix` (lines
  334–347), and `TransitionModelBuilder.CreateChargeTransportMatrix` (lines 30–37).

Commands run (from `/workspace/playground/step08-design`):

```sh
git status --short
# no output before this record was written; exit 0

python tools/check_process_consistency.py
# Process consistency: OK (exit 0)

python tools/check_docs_links.py
# Documentation links OK (149 files scanned). (exit 0)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.30s (exit 0)

git diff --check
# no output; exit 0

python tools/contracts/generate.py --check
# Contract artifacts are current (18 files) (exit 0)

git diff --stat 9715082..HEAD
# pre-record-commit request state (this record commit adds the round-4 text):
# docs/planning/reviews/step-08.md                          | 14 +++++++++++---
# docs/planning/status.md                                   |  8 ++++----
# docs/planning/steps/step-08-model-compilation-pipeline.md |  2 +-
# 3 files changed, 16 insertions(+), 8 deletions(-)

git diff --name-only 9715082..HEAD
# docs/planning/reviews/step-08.md
# docs/planning/status.md
# docs/planning/steps/step-08-model-compilation-pipeline.md

git branch --list
# * docs/step-08-design
# + main

git worktree list
# /workspace/mocassin                  644ee09 [main]
# /workspace/playground/step08-design  053db7a [docs/step-08-design]

find src/dotnet/Mocassin.Compilation src/dotnet/Mocassin.Compilation.Tests -type f \
  -not -path '*/obj/*' -not -path '*/bin/*'
# src/dotnet/Mocassin.Compilation/PublicAPI.Unshipped.txt
# src/dotnet/Mocassin.Compilation/PublicAPI.Shipped.txt
# src/dotnet/Mocassin.Compilation/CompilationModule.cs
# src/dotnet/Mocassin.Compilation/Mocassin.Compilation.csproj
# src/dotnet/Mocassin.Compilation.Tests/SmokeTests.cs
# src/dotnet/Mocassin.Compilation.Tests/Mocassin.Compilation.Tests.csproj
```

Targeted read-only checks:

```sh
nl -ba docs/development/compilation-pipeline.md | sed -n '192,203p'
# lines 194-196: "encoding keys on rules + simulations + energy links; indexing includes the simulation hash
# and rebuilds for runtime-only simulation edits; signing uses the canonical projection and excludes runtime
# settings." Lines 199-203: "editing energy rebuilds environments/pairs/clusters/encoding but reuses structure
# and rules".

nl -ba /workspace/mocassin-original/src/ModelBuilder/ICon.Model.Translator/Database/Optimization/
  JumpSelectionOptimizer.cs | sed -n '37,46p'
# line 41: `isReducedMask |= optimizedSetMask == environmentDefinition.SelectionParticleMask`
# line 45: returns `UseDualDofCorrection` when any mask was unchanged, so the flag is removed only when every
# mask changed.

grep -n "Charge-transport field convention\|Field-factor approximation" \
  docs/planning/steps/step-08-model-compilation-pipeline.md
# line 844: "Charge-transport field convention | Particle-identity Cartesian vector; single-charge, vacancy,
# exchange, and non-collinear tests; no scalar projection"; no "Field-factor approximation" row remains.

nl -ba docs/planning/steps/step-10-job-templates-and-supercells.md | sed -n '118,124p'
# lines 119-121: clears only when the optimizer reduces **every** selection mask and keeps it when any mask is
# unchanged (including the mixed case).

nl -ba src/dotnet/Mocassin.Core/Project/ProjectHasher.cs | sed -n '62,82p'
# ComputeEnergy includes `SiteEnergies` (lines 75-80); no `ComputeSimulationSelection` method exists yet.

nl -ba docs/planning/steps/step-08-model-compilation-pipeline.md | sed -n '618,633p'
# line 618 indexing key has no energy hash; line 619 signing key is the canonical projection; line 629 lists
# `Indexing, Signing` in the energy invalidation row (m13).
```

The frozen-state check passes: `git diff --name-only 9715082..HEAD` is exactly this record, `status.md`, and the
Step 08 page's review-status link. The status change records the round-3 `Ready with notes` result and the
round-4 request; the page change only updates the review link to `9715082`. No design, contract, code, test,
configuration, step-branch, or implementation commit was made after the freeze. `git branch --list` contains only
`docs/step-08-design` and `main`; no `step/08-model-compilation-pipeline` or
`feature/step-08-model-compilation-pipeline/*` branch exists. All eight feature rows remain `Not started`, and
the compiler/test projects contain only the module marker, smoke test, PublicAPI files, and project files.

**Re-review round 5** — frozen design commit `850e98d6788830ee8599a80df966f075f88112a1`; request/current tip
`bd4046ccb61cceb35f16fe270485c94f9a9738e3`; round-4 record `98e9a0414b0774f457f8ecb46a8a4653cf10a630`;
round-3 record `29951241021abdabccb5ee348f36acac0b5f9438`; round-2 record
`eae3bf84b90d4a288599e306b9eb3ade6598ba85`; round-1 record `520fad1214be76b5fe48d70a83cf2a9bab569af0`. I am
a fresh context: I did not author or implement this design, did not participate in its preparation or any
discussion of its decisions, and used no handoff, summary, or prior conversation. The worktree
`/workspace/playground/step08-design` was clean at `bd4046c` before this record was written; all evidence is from
repository inspection at that state and from the frozen design content at `850e98d`.

Files read in full or in the relevant sections:

- `AGENTS.md` §3.8–§3.9 and §3.15; `docs/planning/reviews/README.md`; this record, including rounds 1–4 and all
  findings and resolution tables; the Step 08 page at `850e98d` in full.
- Steps 04, 09, 10, 11, 14, 15, 16, and 17 (path API, signature boundary, job/dual-DOF, package, context,
  field/energy, KMC/MMC consumption).
- `docs/planning/02-data-contracts.md` §1.3, §2.3, §3.3; `docs/development/architecture.md`,
  `design-decisions.md`, `compilation-pipeline.md`, `testing-and-parity.md`;
  `docs/planning/03-quality-strategy.md` §9.3/§13; `docs/planning/05-open-decisions.md`;
  `docs/planning/appendix-b-parity-matrix.md`; `docs/planning/status.md`.
- Implemented APIs: `Mocassin.Core/Project/ProjectHasher.cs`, `Mocassin.Core/Diagnostics/{Diagnostic,ModelPath}.cs`,
  `Mocassin.Symmetry/Reduction/ClusterReduction.cs`,
  `Mocassin.Symmetry/Transitions/TransitionPathResolver.cs`,
  `Mocassin.Symmetry/Structure/{IStructurePreviewService,StructurePreviewService}.cs`,
  `Mocassin.Packages/Jobs/SolverJobFlagsMapper.cs`, `contracts/manifest.json`,
  `contracts/generated/signatures/model-v1.json`, and `tools/contracts/generate.py`.
- Read-only original reference re-used only to confirm the previously checked behaviour:
  `JobDbModelBuilder.SetSimulationJobInfoFlags`, `JumpSelectionOptimizer.Run`, and `RunPostBuildOptimizers`
  (dual-DOF), plus `KineticSimulationModelBuilder.GetChargeTransportVector` and
  `TransitionModelBuilder.CreateChargeTransportMatrix` (charge transport).

Commands run (from `/workspace/playground/step08-design`):

```sh
git status --short
# no output before this record was written; exit 0

python tools/check_process_consistency.py
# Process consistency: OK (exit 0)

python tools/check_docs_links.py
# Documentation links OK (149 files scanned). (exit 0)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.31s (exit 0)

git diff --check
# no output; exit 0

python tools/contracts/generate.py --check
# Contract artifacts are current (18 files) (exit 0)

git diff --stat 850e98d..HEAD
# pre-record-commit request state (this record commit adds the round-5 text):
# docs/planning/reviews/step-08.md                          | 8 +++++++-
# docs/planning/status.md                                   | 8 ++++----
# docs/planning/steps/step-08-model-compilation-pipeline.md | 2 +-
# 3 files changed, 12 insertions(+), 6 deletions(-)

git diff --name-only 850e98d..HEAD
# docs/planning/reviews/step-08.md
# docs/planning/status.md
# docs/planning/steps/step-08-model-compilation-pipeline.md

git branch --list
# * docs/step-08-design
# + main

git branch -a
# * docs/step-08-design
# + main
#   remotes/origin/main

git worktree list
# /workspace/mocassin                  644ee09 [main]
# /workspace/playground/step08-design  bd4046c [docs/step-08-design]

find src/dotnet/Mocassin.Compilation src/dotnet/Mocassin.Compilation.Tests -type f \
  -not -path '*/obj/*' -not -path '*/bin/*'
# src/dotnet/Mocassin.Compilation/PublicAPI.Shipped.txt
# src/dotnet/Mocassin.Compilation/PublicAPI.Unshipped.txt
# src/dotnet/Mocassin.Compilation/CompilationModule.cs
# src/dotnet/Mocassin.Compilation/Mocassin.Compilation.csproj
# src/dotnet/Mocassin.Compilation.Tests/SmokeTests.cs
# src/dotnet/Mocassin.Compilation.Tests/Mocassin.Compilation.Tests.csproj
```

Targeted read-only checks:

```sh
git show --stat --oneline 850e98d
# docs/development/compilation-pipeline.md                  |  5 +++--
# docs/planning/steps/step-08-model-compilation-pipeline.md | 10 ++++++----
# 2 files changed, 9 insertions(+), 6 deletions(-) — documentation only, no code/test/configuration change.

nl -ba docs/planning/steps/step-08-model-compilation-pipeline.md | sed -n '206,208p;612,634p'
# line 207: the indexing key now includes `energy hash`; line 208: signing is the canonical projection with no
# runtime settings; lines 618-619 repeat the energy hash in the §12 indexing key; line 631: the new
# "Site-energy values only" row rebuilds `Encoding`/`Indexing` and reuses `Pairs`, `Clusters`, `Signing`.

nl -ba docs/planning/steps/step-08-model-compilation-pipeline.md | sed -n '202,205p'
# rows 3-5: Environments, Pairs, and Clusters all key on the energy hash.

nl -ba src/dotnet/Mocassin.Core/Project/ProjectHasher.cs | sed -n '62,84p'
# ComputeEnergy includes `SiteEnergies` (line 80); its remark calls it the energy-model half of the Step 08
# compilation cache key, and no narrower site-energy hash exists.

nl -ba docs/development/compilation-pipeline.md | sed -n '190,200p'
# lines 195-197: indexing includes the energy and simulation hashes, signing excludes energy values; lines
# 199-200: "editing energy rebuilds environments/pairs/clusters/encoding but reuses structure and rules".

nl -ba docs/planning/03-quality-strategy.md | sed -n '246,248p'
# the recording cache must prove the invalidation graph after editing one domain at a time.

git show --name-only --format="" 98e9a04
# docs/planning/reviews/step-08.md — the round-4 record commit touched exactly one file.

grep -rn "geometry/rules" docs | grep -v "geometry/rules/encoding"
# only historical record text and one Step 11 payload-description line; no bare signature-scope summary remains.

python -c "import json; print(list(json.load(open('contracts/manifest.json'))['signatures']))"
# ['model', 'parameterization', 'lattice'] — matches `REQUIRED_SIGNATURES` in `tools/contracts/generate.py`.
```

The frozen-state check passes: `git diff --name-only 850e98d..HEAD` is exactly this record, `status.md`, and the
Step 08 page's review-status link. The `status.md` change records the round-4 `Ready with notes` result and the
round-5 request; the page change only adds the review-record link at `850e98d`. The frozen commit `850e98d`
itself is documentation-only (`compilation-pipeline.md` and the Step 08 page). No design, contract, code, test,
configuration, step-branch, or implementation commit was made after the freeze; `git branch -a` contains only
`docs/step-08-design`, `main`, and `remotes/origin/main`. All eight feature rows remain `Not started`, and the
compiler/test projects contain only the module marker, smoke test, PublicAPI files, and project files.

**Re-review round 6** — frozen design commit `b84ceadda1ba8b1715592a4f6884d76bc7c36fed`; request/current tip
`da8ab6aad84c07ab0683f799e5ab675c5511c08e`; round-5 record `8106ed830ad62c1e894dc433dcbba5c373f6e54a`. I am a
fresh context: I did not author or implement this design, did not participate in its preparation or any
discussion of its decisions, and used no handoff, summary, or prior conversation. I read the frozen repository
state at `b84cead` and the request/current tip `da8ab6a` directly. The worktree
`/workspace/playground/step08-design` was clean at `da8ab6a` before this record was written.

Files read in full or in the relevant sections:

- `AGENTS.md` §3.8–§3.9 and §3.15; `docs/planning/reviews/README.md`; this record, including rounds 1–5 and all
  findings and resolution tables; the Step 08 page at `b84cead` in full.
- Steps 04, 09, 10, 11, 14, 15, 16, and 17 (path API, signature boundary, job/dual-DOF, package, context,
  field/energy, KMC/MMC consumption).
- `docs/planning/02-data-contracts.md` §1.3, §2.3, §3.3; `docs/development/architecture.md`,
  `design-decisions.md`, `compilation-pipeline.md`, `testing-and-parity.md`;
  `docs/planning/03-quality-strategy.md` §9.3/§13; `docs/planning/05-open-decisions.md`;
  `docs/planning/appendix-b-parity-matrix.md`; `docs/planning/status.md`.
- Implemented APIs: `Mocassin.Core/Project/ProjectHasher.cs`, `Mocassin.Core/Diagnostics/{Diagnostic,ModelPath}.cs`,
  `Mocassin.Symmetry/Reduction/ClusterReduction.cs`,
  `Mocassin.Symmetry/Transitions/TransitionPathResolver.cs`,
  `Mocassin.Symmetry/Structure/{IStructurePreviewService,StructurePreviewService}.cs`,
  `Mocassin.Packages/Jobs/SolverJobFlagsMapper.cs`, `contracts/manifest.json`,
  `contracts/generated/signatures/model-v1.json`, and `tools/contracts/generate.py`.
- Read-only original reference re-used only to confirm previously checked behaviour:
  `JobDbModelBuilder.SetSimulationJobInfoFlags` (line 247 sets `dofFlag` for every KMC job),
  `JumpSelectionOptimizer.Run` (lines 37–45), `KineticSimulationModelBuilder.GetChargeTransportVector`
  (lines 271–277), `TransitionModelBuilder.CreateChargeTransportMatrix` (lines 30–37), and
  `KineticTransitionModelBuilder.AddMovementMatrix` (lines 334–347).

Commands run (from `/workspace/playground/step08-design`):

```sh
git status --short
# no output; exit 0 (before this record was written)

python tools/check_process_consistency.py
# Process consistency: OK (exit 0)

python tools/check_docs_links.py
# Documentation links OK (149 files scanned). (exit 0)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.30s (exit 0)

git diff --check
# no output; exit 0

python tools/contracts/generate.py --check
# Contract artifacts are current (18 files) (exit 0)

git diff --stat b84cead..HEAD
# pre-record-commit request state (this record commit adds the round-6 text):
# docs/planning/reviews/step-08.md                          | 8 +++++++-
# docs/planning/status.md                                   | 8 ++++----
# docs/planning/steps/step-08-model-compilation-pipeline.md | 2 +-
# 3 files changed, 12 insertions(+), 6 deletions(-)

git diff --name-only b84cead..HEAD
# docs/planning/reviews/step-08.md
# docs/planning/status.md
# docs/planning/steps/step-08-model-compilation-pipeline.md

git branch --list
# * docs/step-08-design
# + main

git branch -a
# * docs/step-08-design
# + main
#   remotes/origin/main

git worktree list
# /workspace/mocassin                  644ee09 [main]
# /workspace/playground/step08-design  da8ab6a [docs/step-08-design]

find src/dotnet/Mocassin.Compilation src/dotnet/Mocassin.Compilation.Tests -type f \
  -not -path '*/obj/*' -not -path '*/bin/*'
# src/dotnet/Mocassin.Compilation/PublicAPI.Shipped.txt
# src/dotnet/Mocassin.Compilation/PublicAPI.Unshipped.txt
# src/dotnet/Mocassin.Compilation/CompilationModule.cs
# src/dotnet/Mocassin.Compilation/Mocassin.Compilation.csproj
# src/dotnet/Mocassin.Compilation.Tests/SmokeTests.cs
# src/dotnet/Mocassin.Compilation.Tests/Mocassin.Compilation.Tests.csproj
```

Targeted read-only checks:

```sh
git show --stat b84cead
# docs/development/compilation-pipeline.md                  | 10 +++++-----
# docs/planning/steps/step-08-model-compilation-pipeline.md |  4 ++--
# 2 files changed, 7 insertions(+), 7 deletions(-) — documentation only; no code/test/configuration change.

nl -ba docs/planning/steps/step-08-model-compilation-pipeline.md | sed -n '202,208p;627,635p'
# rows 3-5 key Environments, Pairs, and Clusters on the energy hash; row 7 (Encoding) and row 8 (Indexing) also
# include the energy hash. Line 631: "Site-energy values only | Environments, Pairs, Clusters,
# Encoding (links and background), Indexing (energy hash) | Structure, Rules, Signing".

nl -ba docs/planning/steps/step-08-model-compilation-pipeline.md | sed -n '612,620p'
# environments/pairs and clusters keys include the energy hash; indexing includes the energy hash; signing is the
# canonical projection with runtime settings excluded.

nl -ba src/dotnet/Mocassin.Core/Project/ProjectHasher.cs | sed -n '62,84p'
# ComputeEnergy includes SiteEnergies (line 80), so a site-energy-only edit changes the energy hash and every
# energy-hash-keyed stage misses.

nl -ba docs/development/compilation-pipeline.md | sed -n '193,204p'
# lines 195-196: indexing includes the energy and simulation hashes and rebuilds for site-energy or
# runtime-only edits; signing excludes energy values and runtime settings. Lines 200-201: editing energy
# rebuilds environments/pairs/clusters/encoding/indexing and reuses structure and rules, while site-energy-only
# edits reuse signing.

grep -n "public static string Compute" src/dotnet/Mocassin.Core/Project/ProjectHasher.cs
# Compute, ComputeStructure, ComputeDocument, ComputeEnergy, ComputeTransition; no ComputeSimulationSelection.

grep -n "public .*TryResolve\|Encoder\|MaxTupleCount" \
  src/dotnet/Mocassin.Symmetry/Transitions/TransitionPathResolver.cs \
  src/dotnet/Mocassin.Symmetry/Reduction/ClusterReduction.cs
# public TryCreate/TryResolve/Encoder; FindMatches/TryGetComponentShift private; MaxTupleCount = 1_000_000.

python -c "import json; print(list(json.load(open('contracts/manifest.json'))['signatures']))"
# ['model', 'parameterization', 'lattice'] — matches REQUIRED_SIGNATURES in tools/contracts/generate.py.

grep -rn "geometry/rules" docs | grep -v "geometry/rules/encoding"
# only historical review text and Step 11's payload-description line; no bare signature-scope summary remains.
```

The frozen-state check passes: `git diff --name-only b84cead..HEAD` is exactly this record, `status.md`, and the
Step 08 page's review-status link. The status change records the round-5 `Ready with notes` result and the round-6
request; the page change only updates the review link to `b84cead`. The frozen commit itself is
documentation-only (`docs/development/compilation-pipeline.md` and the Step 08 page). No design, contract, code,
test, configuration, step-branch, or implementation commit was made after the freeze. `git branch -a` contains
only `docs/step-08-design`, `main`, and `remotes/origin/main`; no `step/08-model-compilation-pipeline` or
`feature/step-08-model-compilation-pipeline/*` branch exists, and no `docs/planning/audits/step-08.md` file
exists (the audit is not requested for an unstarted step). All eight feature rows remain `Not started`, and the
compiler/test projects contain only the module marker, smoke test, PublicAPI files, and project files.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M1 — `UseDualDofCorrection` is redefined without reconciling the original solver contract or recording a correction.** The design sets it only for KMC collections whose chain generates an automatic inverse (`Migration3`) and explicitly returns `false` for vehicle/complex mechanisms. The original job builder sets `UseDualDofCorrection` for every KMC job; the selection optimizer can clear it after mask optimization, and the solver comment defines it as "non-optimized with twice the actually existing degrees of freedom". Step 16 uses `dof = 2` when the flag is set, so the design changes the KMC time step for KMC mechanisms that are not `Migration3`. There is no rationale, reference, or test that distinguishes this from an accidental correction. | Step 08 page §9 lines 414–420 and acceptance lines 716–718; `docs/planning/steps/step-08-model-compilation-pipeline.md`; Step 16 line 153; original `JobDbModelBuilder.cs` lines 236–250; original `JumpSelectionOptimizer.cs` lines 38–45; solver `Constants.h` line 40. |
| **Major** | **M2 — The field-factor formula is dimensionally and mathematically inconsistent and cannot produce the stated single-charge result.** The page defines `pathVector = Σ_i movement_i` as Cartesian, but `movement_i` is defined as a fractional displacement. `pathUnit` is then dotted with `movement_i`; if `movement_i` is the fractional displacement, the result is not the claimed `q` for a single charge (the dot product is `|Δx|`), and mixing fractional movement with a Cartesian unit vector is not dimensionally valid. `rule.field_factor` is multiplied by `direction.field_projection` in Step 15, so the resulting field term is wrong or unit-dependent. The single-charge, vacancy, and collinear cases cannot validate the written formula as stated. | Step 08 page §8.1 lines 310–312 and §8.4 lines 361–376; Step 15 lines 100–112. |
| **Major** | **M3 — The cache invalidation graph reuses `Rules` after a simulation edit even though rules are generated only for transitions referenced by simulations.** The `Rules` stage processes "each kinetic or Metropolis transition referenced by at least one simulation"; the rules cache key is only "transition closure hashes + structure hash + particle order" and has no simulation/active-transition-set component. Adding a simulation that references a previously unused transition, or removing the last reference to one, therefore changes the required rule set while the graph says `Simulations/settings` rebuilds only `Encoding, Indexing, Signing` and reuses `Rules`. The result can be stale or missing rules, and `CMP009`/`CMP006` behaviour can change without a cache miss. There is also no named simulation-hash producer in `ProjectHasher`. | Step 08 page §4 line 299, §12 lines 583–595; `ProjectHasher` has `Compute`, `ComputeStructure`, `ComputeEnergy`, and `ComputeTransition`, but no simulation hash; `CMP009` rule. |
| **Major** | **M4 — The `compiledModel` manifest task conflicts with the existing `model` signature entry and the contract generator's exact required set.** The task says to add a `compiledModel` signed-projection entry. `contracts/manifest.json` already defines `signatures.model` with the same `mocassin/model-signature/v1` domain prefix, `ModelSignatureV1` projection type, a note that Step 08 finalizes its field-level types, and a `model-v1` compiled-model fixture for Step 08. `tools/contracts/generate.py` requires `signatures` to be exactly `{model, parameterization, lattice}` and emits `contracts/generated/signatures/model-v1.json`; adding a second entry would fail `--check` and duplicate the projection identity. | Step 08 page §11.3 line 528 and task line 705; `contracts/manifest.json` lines 1904–1912 and 2055–2061; `tools/contracts/generate.py` lines 30 and 222–224; `contracts/generated/signatures/model-v1.json` lines 1–10. |
| **Major** | **M5 — The step-sizing statement contradicts the roadmap split trigger.** The page records an eight-row feature list and a 4–6 person-week estimate but says this is "below the split trigger". `04-roadmap.md` §3.1 says to split before implementation when a step has more than roughly 6–8 features or is expected to take more than about four weeks for one implementor. The page's own internal "stop after 2.5 person-weeks for the first three features" trigger does not change the full-step estimate, and no owner waiver or split rationale is recorded. | Step 08 page §1 lines 85–89; `docs/planning/04-roadmap.md` §3.1 lines 93–105. |
| **Major** | **M6 — Direction expansion cannot use the named public resolver for the required transformed-point resolution.** The design says `Rules` uses the public `TransitionPathResolver`, does not duplicate the validator's path rules, and re-resolves every transformed path point to a canonical `(P, offset)`. The only public resolver operation is `TryResolve(KineticTransition, ...)`; `FindMatches` and `TryGetComponentShift` are private, and `TransitionPreviewService.ExpandPath` only transforms and trims. The compiler would have to duplicate the nearest-image/tie rule or construct synthetic transitions (which reject repeated positions), and neither approach is specified or tested. | Step 08 page §4 lines 212–213 and §8.1 lines 301–307; `TransitionPathResolver.cs` lines 88–100 and 165–182; `TransitionPreviewService.cs` lines 441–461. |
| **Minor** | **m1 — `SpaceGroupReferenceResolver` is internal to `Mocassin.Symmetry`, so `Mocassin.Compilation` cannot call it as written.** The structure stage names it directly. The public alternative is `StructurePreviewService.TryResolve` (or `SpaceGroupCatalog.GetSetting` plus a diagnostic wrapper), but the design and task list do not name it; `InternalsVisibleTo` currently covers only `Mocassin.Symmetry.Tests`. | Step 08 page §4 line 206; `SpaceGroupReferenceResolver.cs` line 16; `Mocassin.Symmetry/AssemblyInfo.cs` line 6; `Mocassin.Compilation.csproj` references only Core and Symmetry. |
| **Minor** | **m2 — The particle-index claim does not match `TransitionPreviewService.GetParticleOrder`.** The page says index 0 is void and that this "matches" `GetParticleOrder`, but the implemented order is authored particles ascending followed by `project.VoidParticle.Id` (void last). The compiled 0=void order is a deliberate difference from the preview/state-change matrix order, not a match; the mapping between `ParticleId` and compiled index must be stated and tested so state codes and Step 09/11 consumers cannot diverge. | Step 08 page §5 lines 220–222; `TransitionPreviewService.cs` lines 408–416; `ProjectHasher.cs` lines 131–134; Step 04 `StateChangeMatrices` particle-order rule. |
| **Minor** | **m3 — `ClusterReduction` has no Burnside output and no configurable tuple bound, so the cluster-limit wording is not implementable as written.** The page says not to enumerate the full product when Burnside/reducer output is available and to honor `MaxClusterTupleCount`. The implemented reducer always enumerates, computes no Burnside count, and throws only at its fixed `MaxTupleCount = 1_000_000`; the preview's `BurnsideCount` is private to `EnergyPreviewService`. The compiler can pre-estimate or catch the fixed exception, but cannot apply a lower configured threshold before enumeration. | Step 08 page §7 lines 278–280; `ClusterReduction.cs` lines 60 and 165–182; `EnergyPreviewService.cs` lines 337–353; `CompilationOptions.MaxClusterTupleCount`. |
| **Minor** | **m4 — The cancellation granularity is finer than the Step 02/04/05 APIs allow.** The page promises checks every 4096 inner enumeration steps and responsiveness within 250 ms. `IEnergyGeometrySampler.TrySamplePairs`, `PairReduction.Reduce`, `ClusterReduction.Reduce`, and `TransitionPathResolver.TryResolve` take no `CancellationToken`; a single large reduction or radial search is an atomic call. The design should scope cancellation to stage/top-level boundaries and around those calls, or add tokens to the Symmetry APIs. | Step 08 page §13.3 lines 652–661 and performance line 669; `IEnergyGeometrySampler.cs` lines 28–34; reduction/path resolver signatures. |
| **Minor** | **m5 — Step 09's signature comparison uses `model.Signature`, but the `CompiledModel` contract defines `ModelSignature`.** `ParameterizationService.Apply(CompiledModel model, ...)` says it verifies `template.ModelSignature == model.Signature`; the Step 08 record has `CompiledModel.ModelSignature` and no `Signature` member. The design should align the Step 09 dependency page or add the alias explicitly before implementation. | Step 08 page §10 lines 426–438 and §11.3 lines 513–519; Step 09 page §4 lines 108–115. |
| **Minor** | **m6 — The model-signature scope is described differently in the Step 08 design, Step 09, and the data contract.** The design includes "active-collection selection" and simulation encodings in the Step 08 projection; Step 09 calls the value "geometry/rules only"; data contract §3.3 describes geometry/rules/encoding and excludes runtime settings. It is not stated whether adding/removing a simulation (without changing geometry or rules) invalidates parameterization templates. Clarify the projection boundary and the template-invalidation rule. | Step 08 page §11.3 lines 521–525 and layout lines 533–548; Step 09 page lines 65–67; `02-data-contracts.md` lines 588–592 and 868–877. |
| **Minor** | **m7 — `CMP005` promises rejection counts by reason in the report, but the report shape only has a total rejected count.** The rule says `CMP005` includes "the rejection counts by reason in the message and the report"; `CompilationReport.Counts` lists "rejected rules" but no mass/charge/invalid-state breakdown. Add the breakdown or remove the report claim. | Step 08 page §8.2 line 329 and §13.1 lines 631–632; report contract lines 640–644. |
| **Minor** | **m8 — `CMP009` uses `transition` as its path domain, but the existing model paths distinguish `kineticTransition` and `metropolisTransition`.** The diagnostic table's path column and the per-code path test need the concrete domain for each transition kind; otherwise the docs-drift/path test cannot assert a stable `ModelPath`. | Step 08 page §13.1 line 616; existing transition paths in `TransitionPathResolver.cs` line 223 and `TransitionPathValidator`. |
| **Note** | **n1 — The benchmark task has no project-reference task.** The performance section says to run `Mocassin.Benchmarks/Compilation`, but `Mocassin.Benchmarks.csproj` currently references only Core and Symmetry and has no Compilation folder. Add the reference/feature task or record a different harness. | Step 08 page §13.4 line 670; `src/dotnet/Mocassin.Benchmarks/Mocassin.Benchmarks.csproj`. |
| **Note** | **n2 — The `CMP012` cache-integrity check has no defined integrity predicate.** The design says a cached stage that fails a "defensive integrity check" is recomputed, but does not say what is checked (shape, index bounds, hash, version) or how the test injects a bad entry. This is a future test/hardening detail, not a start blocker. | Step 08 page §12 lines 597–598; `ICompilationCache` API lines 134–141. |

### Findings — re-review round 2

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M2 (residual) — the vacancy field-factor is still not derivable and can cancel to zero.** The formula is now dimensionally coherent and reduces to `q` for a single charge, but the written `pathVector = Σ_i cartesianMovement_i` is the signed sum of every path node's movement. A standard two-node vacancy hop has S0 `[ion, void]` → S2 `[void, ion]`, so the ion and void movements are opposite and `pathVector = 0`; the page's own zero branch then sets `fieldFactor = 0`, while the original computes `Σ q_i movement_i` (the void has charge zero) and gets a non-zero `q Δx·ê`. If the void node is excluded instead, the formula returns `+q`, not the `-q` that the quality strategy promises, because `fieldFactor` is invariant when every movement and `pathVector` reverse; the sign is carried by `direction.field_projection`, not the rule scalar. The page's "the vacancy moves opposite ... so fieldFactor carries the correct sign" does not resolve either reading. | Step 08 page §8.1 step 5 (movement is per path node), §8.4 lines 384–393; `02-data-contracts.md` line 855 (path_vector is the sum of each movement); `03-quality-strategy.md` §9.3 ("`-q` for its vacancy"); original `KineticSimulationModelBuilder.GetChargeTransportVector` lines 271–278 and `TransitionModelBuilder.CreateChargeTransportMatrix` lines 30–34 use `Σ q_i movement_i`; `Particle.Void` line 41 is charge zero. |
| **Minor** | **m6 (residual) — the model-signature scope is still described as `geometry/rules` in several summary pages.** Step 08 §11.3, data-contract §1.3/§3.3, and Step 09 now include encoding/active-collection selection and exclude runtime settings, so the normative boundary is aligned. But `05-open-decisions.md` row 50, `architecture.md` line 147, `appendix-b-parity-matrix.md` line 132, and the `CompiledModel.ModelSignature` comment/task at Step 08 lines 451/486 still say `geometry/rules`. A reader can still conclude that adding/removing a simulation does not invalidate templates, which the design says it does. | Step 08 page lines 451 and 486; `05-open-decisions.md` line 50; `architecture.md` line 147; `appendix-b-parity-matrix.md` line 132; aligned normative text at Step 08 §11.3, `02-data-contracts.md` §1.3/§3.3, and Step 09 line 65. |
| **Minor** | **m9 — the `Resolve`/`Indexing` cache keys still contradict the invalidation graph.** The stage table keys `Resolve` on `model hash`, and §12 says `Resolve` may cache by whole-model hash; `ProjectHasher.Compute` hashes the whole canonical model section. Any energy or transition edit therefore changes the `Resolve` key, while the invalidation graph claims `Resolve` is reused for exactly those edits. The graph also claims `Indexing`/`Signing` are reused after a runtime-only simulation edit, while the table keys `Indexing` on "all prior stage hashes" and `Encoding` includes the runtime default snapshot. These are performance/test-expectation inconsistencies, not stale-output risks, but the recording-cache test cannot assert the graph as written. | Step 08 page §4 stage table (line 200), §12 lines 597–625; `ProjectHasher.Compute` hashes the canonical model section. |
| **Note** | **n3 — Step 10's dual-DOF clear rule is deferred but not defined.** Step 08 now matches the original job builder (`UseDualDofCorrection` for every KMC job), which resolves M1. Step 10 says it "may clear it only through a documented, tested selection-optimizer rule" but does not state the original rule (the optimizer clears the flag when it reduces a selection mask). If Step 10 never defines it, optimized KMC jobs keep `dof = 2` where the original would use `dof = 1`, changing the time step in a later step. | Step 10 page lines 118–120; original `JumpSelectionOptimizer.Run` returns `UseDualDofCorrection` when no mask was reduced and `None` when a mask was reduced. |
| **Note** | **n4 — the M5 page text points at an approval summary that is not filled.** The Step 08 page §1 says the waiver is recorded "in the Step 08 review record and approval summary"; the approval summary is still empty at `c676716` and `c3b44f7`. The waiver is recorded in the resolution table and `status.md`, so the owner decision is durable, but the page's cross-reference is premature. | Step 08 page lines 85–94; review record `Approval summary` section. |
| **Note** | **n5 — the additive `Mocassin.Symmetry` public methods need explicit PublicAPI/ApiCompat entries.** The design adds public `TransitionPathResolver.TryResolvePoint`/`TryResolvePath` and says `Mocassin.Compilation` will use them, but the task list only says "PublicAPI.Unshipped.txt entries and ApiCompat updates for every new public type"; the new members are on an existing `Mocassin.Symmetry` type. The `./tools/verify.sh api` gate would catch it, but the design should name the project and member entries. | Step 08 tasks lines 733–750; `AGENTS.md` §16. |

### Findings — re-review round 3

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **m10 — m9's cache-key correction is not carried into `compilation-pipeline.md`, and the energy invalidation row still names a stage whose key cannot change.** The step page now makes `Resolve` uncached, keys `Indexing` on `structure/particles/tables/rules/simulation`, and reuses `Signing` after runtime-only edits. But `docs/development/compilation-pipeline.md` (the normative cache/algorithm page) says "indexing excludes runtime settings", which contradicts the step page's `simulation hash` key and the "Simulation runtime settings only → Encoding, Indexing rebuild" graph row. The graph and the same page also say an energy edit rebuilds `Encoding`, while the Encoding key (`simulation hash, rule hash, structure hash`) contains no energy hash, so a recording-cache test cannot assert either statement. This is a performance/test-expectation inconsistency, not a stale-output risk. | Step 08 page §4 row 8 (line 207), §12 keys (line 618) and graph (line 632); `docs/development/compilation-pipeline.md` lines 192–202. |
| **Minor** | **m11 — n3's Step 10 rule does not match the original optimizer's mixed-case behavior.** Step 10 now says the optimizer "clears it when the selection optimizer reduces at least one environment selection mask and keeps it when no mask is reduced, matching the original `JumpSelectionOptimizer.Run` behavior." The original sets `isReducedMask \|= optimizedSetMask == environmentDefinition.SelectionParticleMask` and returns `UseDualDofCorrection` when true, so it clears only when **every** environment mask changed; when some but not all masks are reduced it keeps the flag. The new text clears in exactly that mixed case, halving `dof` where the original keeps `dof = 2`. The resolution-table entry for n3 repeats the inaccurate rule. | `docs/planning/steps/step-10-job-templates-and-supercells.md` lines 118–121; original `JumpSelectionOptimizer.cs` lines 37–45; Step 08 §9 (`Kind == Kmc` default) and Step 16 line 153. |
| **Minor** | **m12 — the Step 08 risk table still describes the removed scalar field-factor approximation.** The risk row says "Field-factor approximation \| Documented scalar projection; single-charge/vacancy/collinear tests; vector change requires a contract decision", but §8.4 now supplies a Cartesian per-rule charge transport vector and Step 15 consumes it; a scalar projection is no longer the design and a "vector change" is not pending. The stale row contradicts the normative §8.4 and the data contract. | Step 08 page line 844; §8.4 lines 380–401; Step 15 lines 107–117; `02-data-contracts.md` lines 857–860. |

### Findings — re-review round 4

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **m13 — the energy invalidation row claims `Indexing` and `Signing` rebuild for a site-energy-only edit, but neither key contains the energy hash and the signature projection excludes energy values.** `ProjectHasher.ComputeEnergy` includes `SiteEnergies`, so a site-energy edit changes the environments/pairs/clusters/encoding keys. It does not change the indexing key (`structure hash, particle hash, pair/cluster table hashes, rule hash, simulation hash`) or the signing input (the canonical projection, whose energy section is "table IDs, geometry, codes, mappings; not energy values"). A recording-cache test that edits one energy domain via site energies would therefore observe indexing and signing hits where the graph promises rebuilds. This is the residual site-energy sub-case of m10; it is a performance/test-expectation inconsistency, not a stale-output risk. | Step 08 page §4 row 8 (line 207), §12 keys (lines 618–619), §11.3 projection (line 571), and graph line 629; `ProjectHasher.ComputeEnergy` lines 68–80; `03-quality-strategy.md` §9.3 cache test (lines 246–248). |

### Findings — re-review round 5

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **m14 — the new "site-energy values only" invalidation row still contradicts the environments/pairs/clusters keys.** The `850e98d` split correctly makes `Indexing` rebuild on the energy hash and keeps `Signing` reusable, but the same row lists `Pairs` and `Clusters` as reused and omits `Environments`. Environments, pairs, and clusters key on the energy hash (§4 rows 3–5 and the §12 key list), and `ProjectHasher.ComputeEnergy` includes `SiteEnergies`, so a site-energy-only edit changes all three keys and a recording-cache test observes misses where the graph promises hits. `compilation-pipeline.md` states the dependency in the opposite direction ("editing energy rebuilds environments/pairs/clusters/encoding"). This is the residual of m13: over-rebuilding is safe, so it is a performance/test-expectation inconsistency, not a stale-output risk. | Step 08 page §4 rows 3–5 (lines 202–204), §12 keys (lines 613–620) and graph row (line 631); `docs/development/compilation-pipeline.md` lines 193–200; `ProjectHasher.ComputeEnergy` lines 72–83; `02-data-contracts.md` lines 410–412; `design-decisions.md` line 112; `03-quality-strategy.md` §9.3 lines 246–248. |

### Findings — re-review round 6

No new Blocker, Major, Minor, or Note finding is recorded. The round-5 finding m14 is closed:

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | **m14 (round 5) — resolved at the frozen commit.** The "Site-energy values only" row now reads `Environments, Pairs, Clusters, Encoding (links and background), Indexing (energy hash)` in the rebuild column and `Structure, Rules, Signing` in the reuse column. Every rebuild entry keys on the energy hash (§4 rows 3–5, 7, 8; §12 lines 613–620), and `ProjectHasher.ComputeEnergy` includes `SiteEnergies`, so a site-energy-only edit misses exactly those stages. Structure (structure hash), Rules (active-transition-set/transition/structure/particle-order key), and Signing (canonical projection that excludes energy values) contain no energy input and are correctly reused. `compilation-pipeline.md` lines 200–201 now state the same dependency and the signing exclusion. No over- or under-claim remains, so the §9.3 recording-cache test can assert the graph as written. | `b84cead` diff (two documentation files); Step 08 page lines 202–208, 612–620, 627–635; `docs/development/compilation-pipeline.md` lines 193–204; `ProjectHasher.ComputeEnergy` lines 72–84; `docs/planning/03-quality-strategy.md` lines 246–248. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Ready for implementation

> Round-6 state is `Ready for implementation`: no Blocker, Major, or Minor finding remains. The round-5 m14
> environments/pairs/clusters reuse claim is corrected at `b84cead`, and every earlier finding stays resolved or
> addressed with no regression. Round 5 returned `Ready with notes`; its only open Minor is now closed. The
> round-6 delta is documentation-only, and the cumulative approval report and human-owner acceptance remain
> required before implementation starts.

`Re-review required` is a record state, not a verdict. No open Minor or Note requires acceptance for this round.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner or implementor.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

**Verdict: Changes required.** No Blocker was found, but six Major findings remain: the `UseDualDofCorrection`
derivation (M1), the field-factor formula (M2), the simulation-transition cache invalidation gap (M3), the
`compiledModel` manifest conflict (M4), the step-sizing/split-trigger contradiction (M5), and the missing
public point-level path-resolution boundary (M6). Eight Minor findings and two Notes are recorded above. The
core architecture is sound: a pure synchronous staged compiler over the implemented Step 02/04/05/06 services,
an explicit Step 07 validation boundary, a separate compiled-model signature, a Step 18 tracker split, and a
documented `CMP` family fit the repository. The required repository checks pass. The design must be revised and
a fresh review round must verify the fixes before implementation starts. This record is evidence and does not
authorize the step.

Follow-up owner: Step agent / planner
Follow-up step: Step 08 design revision and fresh design review (round 2)

## Approval summary

The step agent completes this section before requesting human approval. It must cover the full history, not
only the latest round.

- [x] Requested decision and frozen commit recorded — accept `Ready for implementation` at
  `b84ceadda1ba8b1715592a4f6884d76bc7c36fed` (review record `195451021031272af96ee1b2ca600d4c8cd83c2c`).
- [x] Every review/audit round recorded (round, date, commit, reviewer, verdict) — six rounds, below.
- [x] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status) — below and in the
  round resolution tables.
- [x] Accepted notes and waivers recorded with rationale — M5 owner waiver; n1–n5 observations addressed.
- [x] Residual risks, deferred items, and follow-up owners recorded — below.
- [x] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived — no Blocker, Major, or Minor
  remains; M5 is an accepted owner waiver, not a finding waiver.
- [x] Final CI and documentation-build results recorded — local docs/process gates pass; no CI run was made
  because this is a docs-only `[skip ci]` change.

Round history:

| Round | Date | Commit | Reviewer | Verdict |
| --- | --- | --- | --- | --- |
| 1 | 2026-11-12 | `36fbba58182b2e769a6d59b3228a60021344248d` | fresh planning auditor | Changes required |
| 2 | 2026-11-12 | `c676716e88bcdd5e8877938e21ad6950cc2986bb` | fresh planning auditor | Changes required |
| 3 | 2026-11-13 | `b030cfb7d921277f6983651fb451e736cc2f7896` | fresh planning auditor | Ready with notes |
| 4 | 2026-11-13 | `9715082c1d51e8273bf7c636c22593fa0f7ba5e6` | fresh planning auditor | Ready with notes |
| 5 | 2026-11-13 | `850e98d6788830ee8599a80df966f075f88112a1` | fresh planning auditor | Ready with notes |
| 6 | 2026-11-13 | `b84ceadda1ba8b1715592a4f6884d76bc7c36fed` | fresh planning auditor | Ready for implementation |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| M1 | Major | Dual-DOF was redefined for `Migration3` only | KMC default true, matching the original job builder; Step 10 owns the every-mask clear rule | `c676716` | Resolved |
| M2 | Major | Field-factor formula was dimensionally inconsistent and cancelled for vacancy hops | Per-rule particle-identity Cartesian charge transport vector; Step 15 consumes it | `b030cfb` | Resolved |
| M3 | Major | Rules cache did not invalidate on simulation transition-set edits | `ProjectHasher.ComputeSimulationSelection` in the Rules key; conditional graph | `c676716` | Resolved |
| M4 | Major | `compiledModel` manifest entry conflicted with the existing `model` signature | Finalize `signatures.model`/`ModelSignatureV1`; no duplicate entry | `c676716` | Resolved |
| M5 | Major | 4–6 pw/eight features meets the roadmap split trigger | Human owner explicitly waived the split on 2026-11-12; recorded on page, status, and record | `c676716` | Waived by owner |
| M6 | Major | Point-level path resolution needed private `TransitionPathResolver` logic | Additive public `TryResolvePoint`/`TryResolvePath`; `TryResolve` delegates | `c676716` | Resolved |
| m1 | Minor | Internal `SpaceGroupReferenceResolver` was named | Use public `IStructurePreviewService.TryResolve` | `c676716` | Resolved |
| m2 | Minor | Compiled particle order was claimed to match preview order | Explicit 0=void compiled mapping; preview order is not a contract | `c676716` | Resolved |
| m3 | Minor | Cluster Burnside/configurable-bound wording was not implementable | Saturating pre-check; fixed 1,000,000 ceiling; catch bounded failure | `c676716` | Resolved |
| m4 | Minor | Cancellation was finer than the tokenless Symmetry APIs | Stage/top-level/atomic-call scope; 250 ms qualified | `c676716` | Resolved |
| m5 | Minor | Step 09 used `model.Signature` | Align to `model.ModelSignature` | `c676716` | Resolved |
| m6 | Minor | Signature scope drift in summary pages | All summaries say geometry/rules/encoding; runtime settings excluded | `b030cfb` | Resolved |
| m7 | Minor | `CMP005` report lacked rejection-by-reason counts | Added mass/charge/invalid-state counts | `c676716` | Resolved |
| m8 | Minor | `CMP009` path domain was generic | `kineticTransition`/`metropolisTransition` by kind | `c676716` | Resolved |
| m9 | Minor | Resolve/Indexing cache keys contradicted the invalidation graph | Resolve uncached; Indexing keyed on the full relevant hashes | `b030cfb` | Resolved |
| m10 | Minor | Cache summary page contradicted the step page | Simulation hash in indexing, energy hash in encoding, matching rows | `9715082` | Resolved |
| m11 | Minor | Step 10 dual-DOF rule inverted the original mixed case | Clear only when every mask is reduced; keep otherwise | `9715082` | Resolved |
| m12 | Minor | Risk row described the removed scalar field factor | Risk row describes the particle-identity vector | `9715082` | Resolved |
| m13 | Minor | Site-energy invalidation row was incomplete | Indexing key includes the energy hash; signing excludes values | `850e98d` | Resolved |
| m14 | Minor | Site-energy row contradicted the energy-hash keys | Rebuild environments/pairs/clusters/encoding/indexing; reuse structure/rules/signing | `b84cead` | Resolved |
| n1 | Note | No benchmark project reference/task | Added the benchmark/reference task | `c676716` | Addressed |
| n2 | Note | `CMP012` integrity predicate undefined | Key echo/version/shape/content hash + injected-entry test | `c676716` | Addressed |
| n3 | Note | Step 10 dual-DOF clear rule undefined | Recorded the original every-mask rule | `9715082` | Resolved |
| n4 | Note | Waiver page pointed at an empty approval summary | Page points at the review record and status | `b030cfb` | Resolved |
| n5 | Note | Additive Symmetry PublicAPI entries only generic | Task names the `TryResolvePoint`/`TryResolvePath` entries | `b030cfb` | Resolved |

No finding is waived. M5 is an explicit human-owner acceptance of the 4–6 pw/eight-feature step size, not a
finding waiver; the design itself has no open Blocker, Major, or Minor.

Residual risks and follow-ups:

- Step 08 is intentionally implemented as one owner-waived 4–6 pw/eight-feature step; the implementation must
  keep each feature independently auditable and re-open the split decision if the first three exceed 2.5 pw.
- The additive `Mocassin.Symmetry` path API and the new `ProjectHasher.ComputeSimulationSelection` must land
  with PublicAPI/ApiCompat entries and tests before dependants use them.
- The per-rule charge transport vector changes Step 15's field input from the earlier scalar design; Step 15/16
  implementation must use the vector and pin the single-charge, vacancy, exchange, and non-collinear cases.
- Step 10's dual-DOF clear rule depends on the selection optimizer; it needs a parity test against the original
  mixed-case behaviour.
- Performance and cache-hit targets are design targets on the benchmark laptop class; they are measured during
  the Step 08 features, not assumed by this review.
- Local documentation/process gates pass. No CI run was triggered because all commits are documentation-only
  with `[skip ci]`.

## Human approval

- [ ] Accepted: Ready for implementation
- [ ] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by:
Date:

## Resolution status

Round 1 findings were remediated on `docs/step-08-design` at
`c676716e88bcdd5e8877938e21ad6950cc2986bb`. M5 is resolved by an explicit human-owner waiver of the roadmap
§3.1 split trigger, recorded on the Step 08 page and in `status.md`; it is not a silent acceptance. A fresh
round 2 reviewer must re-check the changed design and the dependency pages (Steps 04, 09, 10, 11).

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| M1 | Resolved — `UseDualDofCorrection = Kind == Kmc` default matches the original job builder; Step 10 owns any documented optimizer override | Planner | `c676716` |
| M2 | Resolved — fractional movements converted to Cartesian; `fieldFactor = Σ q_i (cartesianMovement_i · pathVector)/|pathVector|²` reduces to `q` for one charge | Planner | `c676716` |
| M3 | Resolved — `ProjectHasher.ComputeSimulationSelection` added to the Rules key; invalidation graph distinguishes active-set and runtime-only simulation edits | Planner | `c676716` |
| M4 | Resolved — Step 08 finalizes the existing `signatures.model`/`ModelSignatureV1` entry; no second `compiledModel` entry | Planner | `c676716` |
| M5 | Waived by human owner on 2026-11-12 — 4–6 pw/eight features accepted as one step; waiver recorded on the Step 08 page, status, and this record | Human owner | `c676716` |
| M6 | Resolved — additive public `TransitionPathResolver.TryResolvePoint`/`TryResolvePath`; Step 04 page updated; no private nearest-image duplication | Planner | `c676716` |
| m1 | Resolved — use public `IStructurePreviewService.TryResolve`, not internal `SpaceGroupReferenceResolver` | Planner | `c676716` |
| m2 | Resolved — compiled 0=void order is explicitly not preview matrix order; particle-ID mapping and test added | Planner | `c676716` |
| m3 | Resolved — pre-check raw cluster count with saturating `long`, honor the reducer's fixed 1,000,000 ceiling, catch bounded failure as `CMP006` | Planner | `c676716` |
| m4 | Resolved — cancellation is stage/top-level/atomic-call scoped; the 250 ms target is qualified as between checks | Planner | `c676716` |
| m5 | Resolved — Step 09 now uses `model.ModelSignature`; Step 09 page updated | Planner | `c676716` |
| m6 | Resolved — signature scope explicitly includes simulation encoding and excludes runtime settings; Step 09 and data contract aligned | Planner | `c676716` |
| m7 | Resolved — report counts split `RejectedByMass`, `RejectedByCharge`, `RejectedByInvalidState` | Planner | `c676716` |
| m8 | Resolved — `CMP009` path domain is `kineticTransition` or `metropolisTransition` by kind | Planner | `c676716` |
| n1 | Addressed — benchmark/reference task added to the golden/closeout feature | Planner | `c676716` |
| n2 | Addressed — cache-integrity predicate and injected-entry test defined | Planner | `c676716` |

Round 2 findings were remediated at `b030cfb7d921277f6983651fb451e736cc2f7896`:

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| M2 (residual) | Resolved — field input is now a per-rule Cartesian charge transport vector summed over moving particle identities, not per-node movements; vacancy hops cannot cancel, identical-ion exchange is zero, distinct-charge/non-collinear paths are exact | Planner | `b030cfb` |
| m6 (residual) | Resolved — remaining `geometry/rules` summaries in `05-open-decisions`, architecture, parity matrix, and Step 08 now say `geometry/rules/encoding`; runtime-only edits are explicitly excluded | Planner | `b030cfb` |
| m9 | Resolved — `Resolve` is uncached; the graph no longer reuses it. `Indexing` is keyed on structure/particles/tables/rules/simulation and rebuilds for runtime-only edits; `Signing` excludes runtime settings and is reused | Planner | `b030cfb` |
| n3 | Addressed — Step 10 records the original optimizer rule: clear dual-DOF only when every selection mask is reduced; keep it in the mixed/none case | Planner | `b030cfb` |
| n4 | Addressed — Step 08 no longer points at the empty approval summary; the waiver is recorded in the review record and status | Planner | `b030cfb` |
| n5 | Addressed — the task names the `Mocassin.Symmetry` PublicAPI/ApiCompat entries for `TryResolvePoint`/`TryResolvePath` and the new `Mocassin.Compilation` surface | Planner | `b030cfb` |

Round 3 findings m10–m12 were corrected at `9715082c1d51e8273bf7c636c22593fa0f7ba5e6`:

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| m10 | Resolved — `compilation-pipeline.md` now includes the simulation hash in the indexing key and the energy hash in the encoding key; the energy/runtime invalidation rows match the step page | Planner | `9715082` |
| m11 | Resolved — Step 10 now clears dual-DOF only when every selection mask is reduced and keeps it in the mixed/none case | Planner | `9715082` |
| m12 | Resolved — the Step 08 risk row now describes the particle-identity charge transport vector, not a removed scalar approximation | Planner | `9715082` |

Round 4 finding m13 was corrected at `850e98d6788830ee8599a80df966f075f88112a1`:

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| m13 | Resolved — the Indexing key now includes the energy hash, and the graph separates environment/cluster edits from site-energy-only edits; Signing excludes energy values and is reused for site-energy-only changes | Planner | `850e98d` |

Round 5 finding m14 was corrected at `b84ceadda1ba8b1715592a4f6884d76bc7c36fed`:

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| m14 | Resolved — the site-energy-only row now rebuilds Environments, Pairs, Clusters, Encoding, and Indexing (all keyed on the energy hash) and reuses Structure, Rules, and Signing; `compilation-pipeline.md` matches the graph | Planner | `b84cead` |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

I am a fresh context. I did not author or implement this design, did not participate in its preparation or any
discussion of its decisions, and used no handoff, summary, or prior conversation. I read the frozen repository
state at `36fbba58182b2e769a6d59b3228a60021344248d` and the request/current tip
`91d74208a92de7eab1000d25de49c616c521b6ed` directly.

The frozen-state check passes: `git diff --stat 36fbba5..HEAD` contains only
`docs/planning/reviews/step-08.md` (the review request record), `docs/planning/status.md` (the review-status
link), and `docs/planning/steps/step-08-model-compilation-pipeline.md` (the review link). No design, contract,
code, test, configuration, step-branch, or implementation commit was made after the freeze. All eight feature
rows are `Not started` with the required `feature/step-08-model-compilation-pipeline/<feature>` names, no
`step/08-model-compilation-pipeline` branch exists, and the compiler/test projects contain only the
`CompilationModule` marker and the smoke test.

#### Commands and results

All commands were run in `/workspace/playground/step08-design`:

```sh
git status --short
# clean

python tools/check_process_consistency.py
# Process consistency: OK

python tools/check_docs_links.py
# Documentation links OK (149 files scanned).

zensical build --strict
# Build started
# No issues found
# Build finished in 0.30s

git diff --check
# no output; exit 0

python tools/contracts/generate.py --check
# Contract artifacts are current (18 files)
```

The strict documentation build, process check, link check, whitespace check, and contract-freshness check all
pass. The manifest conflict in M4 is a design/tooling inconsistency, not a current artifact failure, because
the design has not yet added the conflicting entry. The exact `git diff --stat`, branch, and file-list evidence
is recorded in `Evidence` above.

#### Findings

Round 1 records six Major findings, eight Minor findings, and two Notes in the findings table:

- **Major:** M1 (`UseDualDofCorrection` divergence), M2 (field-factor formula/units), M3 (simulation-transition
  cache invalidation), M4 (`compiledModel` vs existing `model` manifest/signature), M5 (sizing vs split
  trigger), M6 (public point-level path resolution).
- **Minor:** m1 (internal `SpaceGroupReferenceResolver`), m2 (void-last preview particle order), m3
  (cluster/Burnside bound), m4 (cancellation API granularity), m5 (`model.Signature` naming in Step 09), m6
  (model-signature scope), m7 (`CMP005` report breakdown), m8 (`CMP009` path domain).
- **Notes:** n1 (benchmark project reference), n2 (`CMP012` integrity predicate).

No Blocker was found. The core architecture, dependency direction, validation boundary, indexing plan, tracker
split, `CMP` catalog plan, and documentation/parity/audit plan are sound; the Major findings are specific
correctness and contract contradictions that must be resolved before implementation.

#### Verdict

**Changes required.** The design is not `Ready for implementation` or `Ready with notes`: six Major findings
remain, including a likely KMC time-step change, a central field-term formula error, a cache invalidation gap,
a contract-tooling conflict, a sizing-gate contradiction, and an unimplementable path-resolution boundary. A
fresh design review (round 2) is required after the planning agent and step agent update the design docs and
add resolutions. This record does not itself authorize starting Step 08.

### Re-review round 2

#### Independence and frozen state

I am a fresh context for this round. I did not author or implement this design, did not participate in its
preparation or any discussion of its decisions, and used no handoff, summary, or prior conversation. I read
the frozen repository state at `c676716e88bcdd5e8877938e21ad6950cc2986bb` and the request/current tip
`c3b44f7c66ed39159abe0df2505037081aa2479b` directly. I am not the round-1 reviewer; the round-1 text is kept
unchanged below.

The frozen-state check passes: `git diff --name-only c676716..HEAD` is exactly
`docs/planning/reviews/step-08.md`, `docs/planning/status.md`, and
`docs/planning/steps/step-08-model-compilation-pipeline.md`. The status/step-page changes are the documented
review-status and owner-waiver request updates; no design, contract, code, test, configuration, step-branch,
or implementation commit was made after the freeze. All eight feature rows are `Not started`, no
`step/08-model-compilation-pipeline` or `feature/step-08-model-compilation-pipeline/*` branch exists, and the
compiler/test projects contain only `CompilationModule.cs` and `SmokeTests.cs`.

#### Commands and results

All commands were run in `/workspace/playground/step08-design`:

```sh
python tools/check_process_consistency.py
# Process consistency: OK (exit 0)

python tools/check_docs_links.py
# Documentation links OK (149 files scanned). (exit 0)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.31s (exit 0)

git diff --check
# no output; exit 0

git status --short
# no output before this record was written; exit 0

python tools/contracts/generate.py --check
# Contract artifacts are current (18 files) (exit 0)

git diff --stat c676716..HEAD
# pre-record-commit request state (this record commit adds the round-2 text):
# docs/planning/reviews/step-08.md                   | 26 +++++++++++++++++++---
# docs/planning/status.md                            |  8 ++++---
# docs/planning/steps/step-08-model-compilation-pipeline.md |  2 +-
# 3 files changed, 29 insertions(+), 7 deletions(-)

git branch --list
# * docs/step-08-design
# + main
```

`git branch -a` has no `step/08` or `feature/step-08` branch, and the `find` check for the compiler and test
projects returns only the module marker and the smoke test. The full evidence and targeted API checks are in
`Evidence` above.

#### Findings

Round-2 status of the round-1 findings:

| Finding | Round-2 status | Basis |
| --- | --- | --- |
| M1 | **Resolved** | `UseDualDofCorrection = Kind == Kmc` matches `JobDbModelBuilder.SetSimulationJobInfoFlags`, which sets the flag for every KMC job; Step 08 no longer infers it from the mechanism shape, and Step 10 owns the documented optimizer clear. |
| M2 | **Partially resolved** | The Cartesian conversion is dimensionally coherent and the single-charge case reduces to `q`; the vacancy/reverse-direction case remains a Major because the stated `pathVector` sum cancels for a two-node vacancy hop and the claimed `-q` is not derivable from the formula. |
| M3 | **Resolved** | `ProjectHasher.ComputeSimulationSelection(Project)` is named as the new Core producer; the `Rules` key includes the active-transition-set hash and the graph distinguishes active-set from runtime-only simulation edits. The residual `Resolve`/`Indexing` key wording is m9. |
| M4 | **Resolved** | The design finalizes the existing `signatures.model`/`ModelSignatureV1`/`model-v1` entry and explicitly adds no second `compiledModel` entry; `tools/contracts/generate.py` still requires exactly `{model, parameterization, lattice}` and `--check` passes. |
| M5 | **Resolved by owner waiver** | The Step 08 page, `status.md`, and the resolution table record the human owner's 2026-11-12 waiver of the roadmap §3.1 split trigger. This reviewer does not grant or reassess the waiver; the recorded owner decision is the basis. |
| M6 | **Resolved** | The design adds public `TransitionPathResolver.TryResolvePoint`/`TryResolvePath`, keeps `FindMatches`/`TryGetComponentShift` private, delegates `TryResolve`, and the Step 04 page records the API. |
| m1 | **Resolved** | The structure stage uses public `IStructurePreviewService.TryResolve`; `SpaceGroupReferenceResolver` remains internal. |
| m2 | **Resolved** | The design states compiled `0 = void`, authored-ascending indices, deliberately not `GetParticleOrder` (which puts void last), with an explicit mapping and test. |
| m3 | **Resolved** | The design pre-checks the raw count with saturating `long`, honors the reducer's fixed 1,000,000 ceiling, catches `InvalidOperationException` as `CMP006`, and treats Burnside only as a test cross-check. |
| m4 | **Resolved** | Cancellation is scoped to stage/top-level/atomic-call boundaries; the 250 ms target is qualified, and the Symmetry APIs take no token. |
| m5 | **Resolved** | Step 09 verifies `template.ModelSignature == model.ModelSignature` and describes the parameterization signature separately. |
| m6 | **Partially resolved** | Step 08 §11.3, data-contract §1.3/§3.3, and Step 09 now include encoding and exclude runtime settings, but `05-open-decisions.md`, `architecture.md`, the parity matrix, and two Step 08 comments still say `geometry/rules`. |
| m7 | **Resolved** | `CompilationReport.Counts` splits `RejectedByMass`, `RejectedByCharge`, and `RejectedByInvalidState`; `CMP005` names the by-reason counts. |
| m8 | **Resolved** | `CMP009` names `kineticTransition` or `metropolisTransition`, matching the existing `ModelPath` domains. |
| n1 | **Addressed** | The `golden-models-and-closeout` task adds the `Mocassin.Compilation` benchmark reference and `Compilation/` benchmarks, or a recorded alternative harness. |
| n2 | **Addressed** | The cache integrity predicate is defined as key echo, format version, lengths/index bounds, and content hash, with a wrong-version/wrong-shape injected-entry test. |

The new round-2 findings are in `Findings — re-review round 2`: one Major (M2 residual), two Minor (m6
residual and m9), and three Notes (n3–n5). The Major is material: the field-factor scalar is central to the
KMC field term, the standard two-node vacancy hop yields `pathVector = 0` under the written sum, and the
quality strategy's `-q` vacancy assertion is mathematically impossible under a direction-reversal-invariant
`fieldFactor` with a zero-charge void. The original `GetChargeTransportVector` uses `Σ q_i movement_i`, which
does not normalize by the unweighted path vector. The design must define whether the vacancy/void node
contributes to `pathVector`, or use a charge-weighted net, and must pin the correct sign in a test.

#### Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

**Changes required.** No Blocker was found. M1, M3, M4, M5, M6, m1–m5, m7, m8, n1, and n2 are resolved or
addressed; m6 is partially resolved. One Major remains: the M2 vacancy field-factor contradiction. The correct
single-charge result and the dimensional fix are real progress, but the design is not implementable without
guessing the vacancy path-vector composition and sign, and the quality strategy's `-q` claim would fail as
written. The repository checks pass, the frozen-state check passes, the owner's M5 waiver is recorded, and no
implementation code or step branch exists. A round 3 review is required after the field-factor definition and
the m6 summary drift are corrected; this record is evidence and does not authorize starting Step 08.

### Re-review round 3

#### Independence and frozen state

I am a fresh context for this round. I did not author or implement this design, did not participate in its
preparation or any discussion of its decisions, and used no handoff, summary, or prior conversation. I read the
frozen repository state at `b030cfb7d921277f6983651fb451e736cc2f7896` and the request/current tip
`b05c8b547d29756b7c65b7cf97a251ae91354a6f` directly. I am not the round-1 or round-2 reviewer; both earlier
rounds are kept unchanged above.

The frozen-state check passes: `git diff --name-only b030cfb..HEAD` is exactly
`docs/planning/reviews/step-08.md`, `docs/planning/status.md`, and
`docs/planning/steps/step-08-model-compilation-pipeline.md`. The status and step-page changes are the documented
review-status request updates (the step page also drops the premature approval-summary reference for n4). No
design, contract, code, test, configuration, step-branch, or implementation commit was made after the freeze.
`git branch -a` contains only `docs/step-08-design`, `main`, and `remotes/origin/main`; no
`step/08-model-compilation-pipeline` or `feature/step-08-model-compilation-pipeline/*` branch exists. All eight
feature rows remain `Not started`, and the compiler/test projects contain only `CompilationModule.cs`,
`SmokeTests.cs`, the PublicAPI files, and project files.

#### Commands and results

All commands were run in `/workspace/playground/step08-design`:

```sh
git status --short
# no output before this record was written; exit 0

python tools/check_process_consistency.py
# Process consistency: OK (exit 0)

python tools/check_docs_links.py
# Documentation links OK (149 files scanned). (exit 0)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.24s (exit 0)

git diff --check
# no output; exit 0

python tools/contracts/generate.py --check
# Contract artifacts are current (18 files) (exit 0)

git diff --stat b030cfb..HEAD
# pre-record-commit request state (this record commit adds the round-3 text):
# docs/planning/reviews/step-08.md                          | 15 +++++++++++++--
# docs/planning/status.md                                   |  7 ++++---
# docs/planning/steps/step-08-model-compilation-pipeline.md |  2 +-
# 3 files changed, 18 insertions(+), 6 deletions(-)

git branch --list
# * docs/step-08-design
# + main
```

The strict documentation build, process check, link check, whitespace check, and contract-freshness check all
pass. The full file list, API checks, and targeted greps are in `Evidence` above.

#### Findings

Round-3 status of the round-2 findings:

| Finding | Round-3 status | Basis |
| --- | --- | --- |
| M2 (residual) | **Resolved** | §8.4 now defines `displacement_p = Cell.ToCartesian(x_p(S2) − x_p(S0))` and `chargeTransportVector = Σ_p q_p displacement_p`, and explicitly makes `pathVector` display/tracking only. This is the same quantity as the original `KineticSimulationModelBuilder.GetChargeTransportVector`: `PositionMovementMatrix` (column `i` is the displacement of the particle at node `i`, built by `AddMovementMatrix`) multiplied by `ChargeTransportMatrix` (`startState[i].Charge`) gives `Σ_i q_i movement_i`; summing over particle identities is the same sum, because each node holds one particle and `Particle.CreateVoid()` has charge `0.0`. A two-node vacancy hop gives `q(x_B − x_A)` (non-zero, ion sign), an identical-ion exchange gives `(q − q)(x₁ − x₀) = 0`, a distinct-charge exchange gives `(q₁ − q₂)(x₁ − x₀) ≠ 0`, a 3-site migration gives `q(x_C − x_A)`, and the component-wise dot product is exact for non-collinear paths. `rule.charge_transport_vector` is the exact Step 15 input (`field = −0.5(|E|/kT)(Q·ê)`, Step 15 lines 100/107–117) and is stored as `charge_transport_vector:[f64;3]` in §3.3; no scalar field factor or per-node cancellation remains. The five cases are in the Step 08 acceptance criteria and test table, and §9.3 no longer asserts `−q`. |
| m6 (residual) | **Resolved** | `grep "geometry/rules"` returns only `geometry/rules/encoding` occurrences; `05-open-decisions.md` row 50, `architecture.md` line 147, the parity row, and both Step 08 `ModelSignature` comments now match the §11.3/§1.3/§3.3 boundary and exclude runtime settings. |
| m9 | **Partially resolved** | The step page fixes all three cited points: `Resolve` is uncached (row 1), `Indexing` is keyed on structure/particles/tables/rules/simulation and rebuilds for runtime-only edits, and `Signing` excludes runtime settings. But the normative `compilation-pipeline.md` still says "indexing excludes runtime settings", and its energy row says `Encoding` rebuilds although the Encoding key has no energy hash. Recorded as m10 (Minor). |
| n3 | **Partially resolved** | Step 10 now records a rule, but it is not the original rule: the original clears dual-DOF only when every environment mask changed, and keeps it in the mixed case; Step 10 says it clears when at least one mask is reduced. Recorded as m11 (Minor). |
| n4 | **Resolved** | Step 08 §1 now says the waiver is recorded "in the Step 08 review record and status dashboard" and no longer points at the empty `Approval summary`. |
| n5 | **Resolved** | The task list names the additive `Mocassin.Symmetry.TransitionPathResolver.TryResolvePoint`/`TryResolvePath` members and the new `Mocassin.Compilation` surface (Step 08 task lines 757–759); `TransitionPathResolver` still has only the public `TryCreate`/`TryResolve`/`Encoder` and private `FindMatches`/`TryGetComponentShift`, which matches the additive plan. |

The round-1 findings M1, M3, M4, M5, M6, m1–m5, m7, m8, n1, and n2 remain resolved or addressed as recorded
in the round-2 table. I re-checked the artefacts they depend on: the `signatures.model` manifest entry and
`REQUIRED_SIGNATURES` (M4), `ProjectHasher` (M3), `ClusterReduction.MaxTupleCount` (m3), the public
`TransitionPathResolver` surface (M6), and the Step 09/10/11 signature, job, and package text (m5, m6, n3); no
regression was introduced at `b030cfb`.

The new round-3 findings are m10, m11, and m12 in `Findings — re-review round 3`. No Blocker or Major was
found. The M2 field convention is now well-defined, matches the original charge-transport computation and the
Step 15 contract, and covers every required case. m10 and m12 are documentation-consistency corrections; m11
is a Step 10 rule that must be corrected before Step 10 is implemented, but it does not affect Step 08's
`Kind == Kmc` default.

#### Verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Waived

**Ready with notes.** No Blocker or Major remains. M2, m6, n4, and n5 are resolved; m9 and n3 are partially
resolved with three Minor findings (m10–m12): the compilation-pipeline cache summary still contradicts the
step page, Step 10's dual-DOF rule misstates the original optimizer's mixed case, and the Step 08 risk row
still describes the removed scalar approximation. These are small, fixable documentation inconsistencies, not
implementation blockers. Under `docs/planning/reviews/README.md`, `Ready with notes` requires the human owner
to accept these Minor findings explicitly; the planner should correct m10–m12 and record the corrections. This
record is evidence and does not itself authorize starting Step 08.

Follow-up owner: Step agent / planner
Follow-up step: Correct m10–m12, then human-owner acceptance of `Ready with notes`

### Re-review round 4

#### Independence and frozen state

I am a fresh context for this round. I did not author or implement this design, did not participate in its
preparation or any discussion of its decisions, and used no handoff, summary, or prior conversation. I read the
frozen repository state at `9715082c1d51e8273bf7c636c22593fa0f7ba5e6` and the request/current tip
`053db7ab606e6d4f3faee7c03fc7a4a83a157c03` directly. I am not the round-1, round-2, or round-3 reviewer; every
earlier round is kept unchanged above.

The frozen-state check passes: `git diff --name-only 9715082..HEAD` is exactly
`docs/planning/reviews/step-08.md`, `docs/planning/status.md`, and
`docs/planning/steps/step-08-model-compilation-pipeline.md`. The status change records the round-3 verdict and the
round-4 request; the step-page change only updates the review link to `9715082`. No design, contract, code, test,
configuration, step-branch, or implementation commit was made after the freeze. `git branch --list` contains only
`docs/step-08-design` and `main`; no `step/08-model-compilation-pipeline` or
`feature/step-08-model-compilation-pipeline/*` branch exists. All eight feature rows remain `Not started`, and
the compiler/test projects contain only the module marker, smoke test, PublicAPI files, and project files.

#### Commands and results

All commands were run in `/workspace/playground/step08-design`:

```sh
git status --short
# no output before this record was written; exit 0

python tools/check_process_consistency.py
# Process consistency: OK (exit 0)

python tools/check_docs_links.py
# Documentation links OK (149 files scanned). (exit 0)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.30s (exit 0)

git diff --check
# no output; exit 0

python tools/contracts/generate.py --check
# Contract artifacts are current (18 files) (exit 0)

git diff --stat 9715082..HEAD
# pre-record-commit request state (this record commit adds the round-4 text):
# docs/planning/reviews/step-08.md                          | 14 +++++++++++---
# docs/planning/status.md                                   |  8 ++++----
# docs/planning/steps/step-08-model-compilation-pipeline.md |  2 +-
# 3 files changed, 16 insertions(+), 8 deletions(-)

git branch --list
# * docs/step-08-design
# + main
```

The strict documentation build, process check, link check, whitespace check, and contract-freshness check all
pass. The full file list, API checks, and targeted greps are in `Evidence` above.

#### Findings

Round-4 status of the round-3 findings:

| Finding | Round-4 status | Basis |
| --- | --- | --- |
| m10 | **Resolved** | `compilation-pipeline.md` now keys indexing on the simulation hash and encoding on the energy-derived hash ("energy links"); the runtime and energy prose no longer contradicts the step page's keys. The residual site-energy sub-case is m13. |
| m11 | **Resolved** | Step 10 now clears dual-DOF only when every environment selection mask is reduced and keeps it in the mixed/none case. The original `JumpSelectionOptimizer.Run` sets `isReducedMask` when a mask is **unchanged** and returns `UseDualDofCorrection`, and `RunPostBuildOptimizers` subtracts the returned flag, so the flag is removed only when all masks changed. |
| m12 | **Resolved** | The Step 08 risk row now reads "Charge-transport field convention \| Particle-identity Cartesian vector; single-charge, vacancy, exchange, and non-collinear tests; no scalar projection", matching §8.4 and `02-data-contracts.md` §3.3; no "vector change requires a contract decision" remains. |

The round-1 and round-2 findings remain resolved or addressed as recorded. I re-checked the artefacts they
depend on: the `signatures.model` manifest entry and `REQUIRED_SIGNATURES` (M4), `ProjectHasher` (`Compute`,
`ComputeStructure`, `ComputeEnergy`, `ComputeTransition`; `ComputeSimulationSelection` remains the named new Core
producer) (M3), `ClusterReduction.MaxTupleCount` (m3), the public `TransitionPathResolver` surface (M6),
`Diagnostic.Related`, `SolverJobFlagsMapper.Compose`, and the Step 09/10/11/15 signature, job, and field text
(m5, m6, m11). No regression was introduced at `9715082`; the m11 correction fixes Step 10 without altering
Step 08's `Kind == Kmc` default.

The new round-4 finding is m13 in `Findings — re-review round 4`. No Blocker or Major was found. m10, m11, and
m12 are resolved; m13 is a small cache-graph/test-expectation inconsistency, not an interface or correctness
gap. The core architecture remains sound: a pure synchronous staged compiler over the implemented Step 02/04/05/06
services, an explicit Step 07 validation boundary, a separate compiled-model signature, the particle-identity
charge-transport vector, the Step 18 tracker split, and the `CMP` family.

#### Verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Waived

**Round 4 verdict: Ready with notes.** No Blocker or Major remains. m10, m11, and m12 are resolved at
`9715082`; the design matches the original dual-DOF accumulation, the particle-identity charge transport vector,
and the compilation cache keys. One Minor remains: m13, the site-energy sub-case of the energy invalidation row,
where the graph promises `Indexing`/`Signing` rebuilds that the stated keys and the "not energy values" signature
projection do not produce. It is a documentation/test-expectation inconsistency, not a stale-output risk, and it
is the only open finding. Under `docs/planning/reviews/README.md`, `Ready with notes` requires the human owner to
accept m13 explicitly; the planner should split the energy row into environment/cluster and site-energy cases, or
qualify the site-energy claim. This record is evidence and does not itself authorize starting Step 08.

Follow-up owner: Step agent / planner and human owner
Follow-up step: Correct or explicitly accept m13; human-owner acceptance of `Ready with notes`

### Re-review round 5

#### Independence and frozen state

I am a fresh context for this round. I did not author or implement this design, did not participate in its
preparation or any discussion of its decisions, and used no handoff, summary, or prior conversation. I read the
frozen repository state at `850e98d6788830ee8599a80df966f075f88112a1` and the request/current tip
`bd4046ccb61cceb35f16fe270485c94f9a9738e3` directly. I am not the round-1, round-2, round-3, or round-4
reviewer; every earlier round is kept unchanged above.

The frozen-state check passes: `git diff --name-only 850e98d..HEAD` is exactly
`docs/planning/reviews/step-08.md`, `docs/planning/status.md`, and
`docs/planning/steps/step-08-model-compilation-pipeline.md`. The status change records the round-4 verdict and the
round-5 request; the step-page change only adds the review-record link at `850e98d`. The frozen commit itself is
documentation-only (`docs/development/compilation-pipeline.md` and the Step 08 page). No design, contract, code,
test, configuration, step-branch, or implementation commit was made after the freeze. `git branch -a` contains
only `docs/step-08-design`, `main`, and `remotes/origin/main`; no `step/08-model-compilation-pipeline` or
`feature/step-08-model-compilation-pipeline/*` branch exists. All eight feature rows remain `Not started`, and the
compiler/test projects contain only the module marker, smoke test, PublicAPI files, and project files.

#### Commands and results

All commands were run in `/workspace/playground/step08-design`:

```sh
git status --short
# no output before this record was written; exit 0

python tools/check_process_consistency.py
# Process consistency: OK (exit 0)

python tools/check_docs_links.py
# Documentation links OK (149 files scanned). (exit 0)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.31s (exit 0)

git diff --check
# no output; exit 0

python tools/contracts/generate.py --check
# Contract artifacts are current (18 files) (exit 0)

git diff --stat 850e98d..HEAD
# pre-record-commit request state (this record commit adds the round-5 text):
# docs/planning/reviews/step-08.md                          | 8 +++++++-
# docs/planning/status.md                                   | 8 ++++----
# docs/planning/steps/step-08-model-compilation-pipeline.md | 2 +-
# 3 files changed, 12 insertions(+), 6 deletions(-)

git branch --list
# * docs/step-08-design
# + main
```

The strict documentation build, process check, link check, whitespace check, and contract-freshness check all
pass. The full file list, API checks, and targeted greps are in `Evidence` above.

#### Findings

Round-5 status of the round-4 finding:

| Finding | Round-5 status | Basis |
| --- | --- | --- |
| m13 | **Resolved for its two named claims** | `Indexing` now keys on `energy hash` in both the stage table (line 207) and the §12 key list (lines 618–619), so a site-energy edit misses the indexing cache as the graph promised; `Signing` remains the canonical projection with no energy values and is listed as reused. The same row's over-claimed reuse for the other stages is recorded as m14. |

The round-1, round-2, and round-3 findings remain resolved or addressed as recorded. I re-checked the artefacts
they depend on at `850e98d`: the `signatures.model` manifest entry and `REQUIRED_SIGNATURES` (M4), `ProjectHasher`
(`Compute`, `ComputeStructure`, `ComputeDocument`, `ComputeEnergy`, `ComputeTransition`; `ComputeSimulationSelection`
remains the named new Core producer) (M3), `ClusterReduction.MaxTupleCount = 1_000_000` (m3), the public
`TransitionPathResolver` surface with private `FindMatches`/`TryGetComponentShift` (M6), `Diagnostic.Related`,
`SolverJobFlagsMapper.Compose`, the `geometry/rules/encoding` signature scope (m6), and the Step
09/10/11/15/16 signature, job, dual-DOF, and charge-transport text (m5, m6, m11, m12). No regression was
introduced at `850e98d`; the commit is documentation-only and the earlier resolutions stand.

The new round-5 finding is m14 in `Findings — re-review round 5`. No Blocker or Major was found. m13's two
specific claims are fixed, but splitting the energy row into "Energy environments/clusters/filters" and
"Site-energy values only" moved `Pairs` and `Clusters` into the reuse column for the site-energy case, which
contradicts their energy-hash keys and `compilation-pipeline.md`'s "editing energy rebuilds
environments/pairs/clusters/encoding"; `Environments` is in neither column of the new row. Because the stated
keys are coarser than the row, the §9.3 recording-cache test cannot assert the graph as written. Over-rebuilding
is safe, so this is a documentation/test-expectation inconsistency, not a stale-output risk.

#### Verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Waived

**Round 5 verdict: Ready with notes.** No Blocker or Major remains. The m13 correction at `850e98d` makes the
indexing key and the signing exclusion match the site-energy case, and every earlier finding remains closed. One
Minor remains: m14, the new site-energy row's reuse claim for environments/pairs/clusters, which the stated energy
hash keys and `compilation-pipeline.md` do not produce. Under `docs/planning/reviews/README.md`, `Ready with
notes` requires the human owner to accept m14 explicitly; the planner should either list `Environments`, `Pairs`,
and `Clusters` as rebuilt for a site-energy-only edit (or split the energy hash) before acceptance. This record
is evidence and does not itself authorize starting Step 08.

Follow-up owner: Step agent / planner and human owner
Follow-up step: Correct or explicitly accept m14; human-owner acceptance of `Ready with notes`

### Re-review round 6

#### Independence and frozen state

I am a fresh context for this round. I did not author or implement this design, did not participate in its
preparation or any discussion of its decisions, and used no handoff, summary, or prior conversation. I read the
frozen repository state at `b84ceadda1ba8b1715592a4f6884d76bc7c36fed` and the request/current tip
`da8ab6aad84c07ab0683f799e5ab675c5511c08e` directly. I am not the round-1, round-2, round-3, round-4, or
round-5 reviewer; every earlier round is kept unchanged above.

The frozen-state check passes: `git diff --name-only b84cead..HEAD` is exactly
`docs/planning/reviews/step-08.md`, `docs/planning/status.md`, and
`docs/planning/steps/step-08-model-compilation-pipeline.md`. The status change records the round-5 verdict and the
round-6 request; the step-page change only updates the review link to `b84cead`. The frozen commit itself is
documentation-only (`docs/development/compilation-pipeline.md` and the Step 08 page) and changes no code, test,
contract, or configuration file. `git branch -a` contains only `docs/step-08-design`, `main`, and
`remotes/origin/main`; no `step/08-model-compilation-pipeline` or
`feature/step-08-model-compilation-pipeline/*` branch exists, no `docs/planning/audits/step-08.md` exists, and all
eight feature rows remain `Not started`.

#### Commands and results

All commands were run in `/workspace/playground/step08-design`:

```sh
git status --short
# no output; exit 0 (before this record was written)

python tools/check_process_consistency.py
# Process consistency: OK (exit 0)

python tools/check_docs_links.py
# Documentation links OK (149 files scanned). (exit 0)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.30s (exit 0)

git diff --check
# no output; exit 0

python tools/contracts/generate.py --check
# Contract artifacts are current (18 files) (exit 0)

git diff --stat b84cead..HEAD
# pre-record-commit request state (this record commit adds the round-6 text):
# docs/planning/reviews/step-08.md                          | 8 +++++++-
# docs/planning/status.md                                   | 8 ++++----
# docs/planning/steps/step-08-model-compilation-pipeline.md | 2 +-
# 3 files changed, 12 insertions(+), 6 deletions(-)

git branch --list
# * docs/step-08-design
# + main
```

The strict documentation build, process check, link check, whitespace check, and contract-freshness check all
pass. The full file list, API checks, and targeted greps are in `Evidence` above.

#### Findings

Round-6 status of the round-5 finding:

| Finding | Round-6 status | Basis |
| --- | --- | --- |
| m14 | **Resolved** | The site-energy-only row now rebuilds `Environments, Pairs, Clusters, Encoding (links and background), Indexing (energy hash)` and reuses `Structure, Rules, Signing`. Environments, Pairs, and Clusters key on the energy hash (§4 rows 3–5; §12 lines 614–615), Encoding and Indexing include it (§4 rows 7–8; §12 lines 617–619), and `ProjectHasher.ComputeEnergy` includes `SiteEnergies`, so those stages miss as claimed. Structure (structure hash), Rules (no energy input), and Signing (canonical projection excluding energy values) are correctly reused. `compilation-pipeline.md` lines 200–201 now states the same graph and exclusion. |

The earlier findings remain resolved or addressed as recorded. I re-checked the artefacts they depend on at
`b84cead`: the `signatures.model` entry and `REQUIRED_SIGNATURES` (M4), `ProjectHasher`
(`Compute`, `ComputeStructure`, `ComputeDocument`, `ComputeEnergy`, `ComputeTransition`, with
`ComputeSimulationSelection` still the named new Core producer) (M3), `ClusterReduction.MaxTupleCount = 1_000_000`
(m3), the public `TransitionPathResolver` surface with private `FindMatches`/`TryGetComponentShift` (M6), the
`geometry/rules/encoding` signature scope and `charge_transport_vector:[f64;3]` (m6, M2), Step 10's every-mask
dual-DOF rule (m11), the Step 08 risk row (m12), and the Step 15/16 field and `dof` consumption. No regression was
introduced at `b84cead`; the commit is documentation-only and the earlier resolutions stand.

No new finding is recorded in `Findings — re-review round 6`. The m14 correction is complete, matches the
normative cache page, and makes the §9.3 recording-cache assertion implementable. No Blocker, Major, or Minor
remains; the Notes from earlier rounds (n1–n5) stay observations only and do not block the start gate.

#### Verdict

- [x] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Waived

**Round 6 verdict: Ready for implementation.** No Blocker, Major, or Minor finding remains. The round-5 m14
cache-graph note is resolved at `b84cead`: the site-energy invalidation row, the §12 stage keys, and
`compilation-pipeline.md` now describe the same cache behaviour, including the signing exclusion for energy
values. The core design is unchanged and remains sound: a pure synchronous staged compiler over the implemented
Step 02/04/05/06 services, an explicit Step 07 validation boundary, the separate compiled-model signature, the
particle-identity charge-transport vector, the Step 18 tracker split, and the `CMP` diagnostic family. The
repository checks pass, the frozen-state check passes, and no implementation code, test, step branch, or feature
branch exists. This record is evidence; the human owner still approves the start gate after the cumulative
approval report in §3.12.

Follow-up owner: Human owner and step agent / planner
Follow-up step: Cumulative approval report and human-owner acceptance of `Ready for implementation`, then create
`step/08-model-compilation-pipeline`
