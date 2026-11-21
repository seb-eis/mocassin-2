# Step 11 Amendment 2 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-11-amend-2.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 11 — Simulation Package and `.msl` Writer (amendment 2) |
| Request owner | Planner (design freeze); the implementor is assigned after the verdict |
| Orchestrator | Orchestrator agent (spawns the planning auditor) |
| Step agent (implementor) | Not assigned — post-merge maintenance branch after approval |
| Review initiated by | Orchestrator, on request of the planner |
| Reviewed commit | `6ff913671351798cb64678b506986ea6649364a4` |
| Docs branch | `docs/step-11-amend-2` |
| Review date | 2026-11-21 |
| Reviewer | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh context, no shared conversation with the planner, implementor, or orchestrator |

Step page: [`Step 11 — Simulation Package and `.msl` Writer`](../steps/step-11-simulation-package-and-msl-writer.md)
Amendment page: [`Step 11 Amendment 2 — Cluster Occupation Code Expansion Map`](../amendments/step-11-amend-2-cluster-code-expansion.md)

**Freeze note.** The design docs, including this request record, were committed as the design freeze
`6ff913671351798cb64678b506986ea6649364a4`; the `Reviewed commit` cell is filled with that SHA by this
documentation-only request follow-up. The only delta between the freeze commit and this request commit is this
cell and the freeze SHA in this sentence. The freeze includes the amendment page, the data-contract,
architecture, ADR, open-decision, reference, step-page, index, and status updates. No code, test, configuration,
contract, generated artifact, or fixture is part of the freeze.

> Orchestration: the orchestrator spawns a fresh planning auditor after the planner has frozen the design and
> completed this request, and relays the verdict and owner decisions. The planner and implementor update the
> design docs, apply fixes on the design docs or a maintenance branch, add resolutions without altering reviewer
> text, and ask the orchestrator for re-review until the auditor no longer identifies issues. The orchestrator
> does not edit the design or the review record. The human owner approves the final verdict and the start of
> implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation (post-merge amendment; Step 11 is `Done`, merged at `84ceb76`, tagged `step-11`) |
| Depends on | Step 11 (`Done`, tagged; amendment 1 integrated) and Step 13 (`Done`, tagged; Rust reader exists). Step 14 design-review round 3 (record `1ea8090`, reviewed design `87be9de`, branch `review/step-14-design-round-3`, not merged) records F-29. No released `.msl` exists. |
| Delivers | One optional per-cluster-table `code_expansions` member in the `.msl` model payload, mapping every raw slot-tuple code to its canonical occupation index and applied energy; writer and verifier support; Rust reader validation and lookup; a new two-member cluster fixture, a regenerated singleton fixture, and a byte-fixed pre-map fixture; the documentation, signature/version policy, and post-merge `step-11.1` audit/tag plan. No solver physics code. |

## Design decisions reviewed

- [x] **F-29 problem and evidence.** The `.msl` cluster table stores only canonical orbit codes; the solver
      builds the raw slot tuple and cannot resolve non-canonical members. Verify the eight-to-six ceria
      reduction, the two failing raw codes, and that the original stored every equivalent code.
      *Reviewed: reproducible on the frozen tree — see Evidence; the original `GroupLookupCodes` behaviour was
      read read-only and is described accurately.*
- [x] **Additive map shape.** One optional `code_expansions` member per table with strictly ascending
      `expanded_codes:[u64]` and parallel `canonical_indices:[u32]`; `null`/absent means singleton orbits.
      Verify the shape is exact, compact, and resolves every raw tuple without a sort/minimum heuristic.
      *Reviewed: exact for lookup and compact; the coverage proof's "exactly" is a superset claim — see A2-05;
      rule 2's "exactly two keys" is over-strict — see A2-04.*
- [x] **Producer coverage and duplicate control.** `EnergyPayloadMapper` builds the map from the existing
      `CompiledClusterOccupation.Expansions`; no second symmetry pass. Verify the `GroupLookupCodes`-equivalent
      coverage proof, the canonical self-entry rule, strict ordering, and the internal invariant on duplicates.
      *Reviewed: producer data and the canonical self-entry rule hold; the proof's set equality does not hold
      for asymmetric allowed sets (A2-05).*
- [x] **Signature and canonical-projection impact.** The map is not a new signature input; `signatures.model`
      already projects the expansions, the parameterization projection has no cluster codes, and the lattice
      projection is untouched. Verify no projection/vector/generated-artifact change and that
      `package-summary-v1` is unchanged.
      *Reviewed: confirmed in `ModelSignatureProjection`, `ParameterizationSignatureV1`, the manifest, and
      `contracts/generated/schema/package-payloads-v1.schema.json`; the map is derivable from signed data.*
- [x] **Version policy.** Optional additive member at `format_version = 1` while `.msl` is unreleased, with the
      ADR 0013 addendum, the ADR 0009 pre-release basis, defined absence semantics, a stop condition at first
      release, and current-major rather than previous-major fixtures.
      *Reviewed: the v1 basis is documented in ADR 0009/0010/0013; the addendum's "safe" condition is broader
      than the fail-closed guarantee — see A2-11; the Rust scan layer is unspecified — see A2-02.*
- [x] **Reader bounds and Step 14 semantics.** No new `ReaderLimits` field; the map is bounded by the model
      payload guard, element budget, and declared-length check. The lookup uses the map when present, the direct
      canonical table when absent, and fails closed with a recompile-required error for a non-canonical tuple in
      an old package.
      *Reviewed: lookup semantics are sound; the bound arithmetic is wrong and the "no new limit" conclusion is
      unsupported — see A2-01; the scan's all-known-keys rule needs an explicit optional-key decision — A2-02.*
- [x] **Fixtures and regeneration.** A new ceria-derived two-member package with an expectation document;
      `step-13-populated` regenerated with an unchanged summary; a byte-fixed pre-map package; byte-identity
      assertions for the packages without cluster tables; deterministic regeneration.
      *Reviewed: fixture inventory verified (`msl-v1` 0, `step-13-populated` 1/12, `step-13-centered` 0
      cluster tables); the pre-map fixture is singleton-only — see A2-06; byte-identity fragility — A2-12.*
- [x] **Post-merge `step-11.1` process.** Maintenance branch from `main`, focused design review, separate audit,
      one manual closeout CI run, `--no-ff` merge, tag only after green `main` CI, and no movement of `step-11`.
      Verify the closeout-evidence ID gap is recorded, not silently worked around.
      *Reviewed: the path follows `AGENTS.md` §3.11.8 and amendments README §7; the checker gap is recorded,
      but the audit template/READMEs still mandate the evidence file — see A2-07.*
- [x] **Documentation, status, parity, and audit plan.** Data contracts §3.3/§3.4, architecture §3.1/§7.6, ADR
      0013 addendum, open decisions, Step 11 page, Step 14 cross-step note, `msl-format.md`, fixture READMEs,
      parity evidence after implementation.
      *Reviewed: pages and anchors are present and the docs gate is green; the Step 13 "API note" pointer does
      not exist (A2-09) and the new sample is not in the sample index (A2-10).*
- [x] **Licensing and provenance.** No GPL-derived content; the new fixture derives from the MIT-licensed ceria
      sample; the original `GroupLookupCodes` behaviour is re-implemented from the documented design, not copied.
      *Reviewed: no GPL text or code is copied; the original was inspected read-only; the new fixture derives
      from the MIT sample.*
- [x] **Complexity and split.** Class Low, one maintenance delivery, no split; the hard orbit reasoning stays in
      the existing Step 02/08 reduction and is proven by a property test.
      *Reviewed: Low is justified; no split trigger is present.*

## Dependency and interface checks

- [x] Step 11 is `Done`, merged and tagged; amendment 1 is integrated; the parent audit is closed, so the
      post-merge fallback is the correct path and no parent audit is running.
      *Reviewed: `status.md`, the Step 11 page, and the tag confirm the state.*
- [x] Step 13 is `Done` and tagged; the Rust reader and `ReaderLimits` exist; adding an optional payload field
      and a runtime accessor is a compatible evolution under the documented unknown-field policy.
      *Reviewed: the reader, scan, cursor, and limits exist; the known-key rule is the caveat (A2-02).*
- [x] The Step 14 design's F-29 is addressed by this amendment without editing the unmerged Step 14 design; the
      cross-step note and the re-review path are explicit.
      *Reviewed: the pointer is explicit; formal closure is an external Step 14 gate — see A2-08.*
- [x] Inputs, outputs, types, and formats are defined: the stored map shape, the `.NET` DTO/API, the Rust
      payload/runtime types, the canonical index semantics, and the Step 14 lookup contract.
      *Reviewed: all named; the generated `Deconstruct` surface is not preserved (A2-03) and the scan layer is
      unspecified (A2-02).*
- [x] Versioning and migration are defined: optional v1 member, absent-field behaviour, the pre-map fixture,
      the forward-read check, and the first-release stop condition.
      *Reviewed: defined at the serde/storage layer; the Rust scan gate needs the explicit optional-key rule
      (A2-02); the ADR "safe" wording is A2-11.*
- [x] No unresolved cross-step interface question remains, or the open items (closeout-evidence ID, manifest
      granularity) are recorded with owners.
      *Reviewed: both are recorded in `05-open-decisions.md`; the evidence-ID gap conflicts with the audit
      template/READMEs (A2-07).*

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable
      *Reviewed: yes; the Step 14 closure criterion is externally gated (A2-08).*
- [x] Complexity class and split decision are recorded and justified per
      [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers)
      *Reviewed: Low, one delivery, no split; justified.*
- [x] Design decisions are complete and internally consistent
      *Reviewed: not fully — the bound is wrong (A2-01) and the optional-key scan layer is missing (A2-02).*
- [x] Required tests, property tests, golden fixtures, and performance targets are defined
      *Reviewed: broad matrix; the bound test is absent (A2-01), the real pre-map non-singleton case is absent
      (A2-06), and the one-time forward read is mis-tiered (A2-13).*
- [x] Documentation, status, parity, and audit updates are planned
      *Reviewed: planned; sample docs and the fixture README size table are gaps (A2-10/A2-15).*
- [x] Licensing, provenance, and dependency checks are resolved
      *Reviewed: MIT-compatible; no GPL copying; no new dependency.*
- [x] Risks and mitigations are realistic
      *Reviewed: mostly, but the payload-growth mitigation repeats the wrong bound (A2-01).*
- [x] Code layout and namespace plan follow the structure policy
      *Reviewed: additive members only; no new folders or namespaces.*
- [x] No hidden scope, stale placeholders, or unowned decisions
      *Reviewed: no hidden implementation scope; the Step 13 "API note" pointer is stale (A2-09).*

## Evidence

Files reviewed:

- `AGENTS.md` §3.8–§3.13; `docs/planning/reviews/README.md` §3–§10; `docs/planning/amendments/README.md` §1–§8;
  `docs/planning/amendments/step-amendment-template.md`.
- `docs/planning/amendments/step-11-amend-2-cluster-code-expansion.md` (all 555 lines);
  `docs/planning/reviews/step-11-amend-2.md` (this request);
  `docs/planning/steps/step-11-simulation-package-and-msl-writer.md` (the A2 row and the post-merge section);
  `docs/planning/steps/step-14-simulation-context-initialization.md` (the cross-step pointer);
  `docs/planning/steps/step-13-rust-solver-foundation-and-package-reader.md`.
- `docs/planning/02-data-contracts.md` §3.3/§3.4 (the diff from `bbc2d08`);
  `docs/planning/01-target-architecture.md` §3.1/§7.6; `docs/planning/05-open-decisions.md`;
  `docs/planning/status.md`; `docs/reference/msl-format.md`; `docs/planning/closeout/README.md`;
  `docs/planning/audits/README.md`; `docs/planning/audits/step-audit-template.md`.
- ADRs 0009, 0010, 0011, 0013 (including the new addendum).
- `src/dotnet/Mocassin.Symmetry/Reduction/ClusterReduction.cs`;
  `src/dotnet/Mocassin.Compilation/Energy/ClusterCompilationStage.cs`;
  `src/dotnet/Mocassin.Compilation/Energy/CompiledClusterTable.cs`;
  `src/dotnet/Mocassin.Compilation/Signatures/ModelSignatureProjection.cs`;
  `src/dotnet/Mocassin.Contracts/Parameterization/ParameterizationSignatureV1.cs`;
  `src/dotnet/Mocassin.Contracts/Packages/EnergyPayloadDtos.cs`;
  `src/dotnet/Mocassin.Packages/SimulationPackage/Payloads/EnergyPayloadMapper.cs`;
  `src/dotnet/Mocassin.Packages/SimulationPackage/Payloads/ModelPayloadMapper.cs`;
  `src/dotnet/Mocassin.Results/Packages/Internal/PackageVerificationChecks.cs`;
  `src/dotnet/Mocassin.Results/Packages/Internal/PackageVerifierEngine.cs`;
  `src/dotnet/Mocassin.Results/Packages/Internal/PackagePayloadDecoder.cs`;
  `src/dotnet/Mocassin.Results/Packages/PackageInspection.cs`;
  `src/dotnet/Mocassin.Contracts/PublicAPI.Unshipped.txt`.
- `src/rust/crates/mocassin-types/src/limits.rs`;
  `src/rust/crates/mocassin-types/src/payload/energy.rs`;
  `src/rust/crates/mocassin-types/src/runtime/energy.rs`;
  `src/rust/crates/mocassin-msl/src/scan/mod.rs`;
  `src/rust/crates/mocassin-msl/src/scan/model.rs`;
  `src/rust/crates/mocassin-msl/src/scan/cursor.rs`;
  `src/rust/crates/mocassin-msl/src/scan/facts.rs`;
  `src/rust/crates/mocassin-msl/src/map/model.rs`.
- `contracts/manifest.json` (the `signatures.model` cluster-table fields and the open `energy` object);
  `contracts/generated/schema/package-payloads-v1.schema.json`.
- `tests/contracts/packages/{msl-v1,step-13-populated,step-13-centered,amend-1,amend-1-net10}` READMEs, payload
  indexes, and model payloads; `samples/README.md`; `samples/ceria/ceria.mocprj`;
  `tools/check_closeout_evidence.py`; `tools/check_process_consistency.py`.

Commands run:

| Command | Result |
| --- | --- |
| `git diff --name-status 6ff913671351798cb64678b506986ea6649364a4..HEAD` | `M docs/planning/reviews/step-11-amend-2.md` only — the documented request follow-up; the design docs are unchanged. |
| `git diff --stat bbc2d08..6ff9136` | 11 files, 917 insertions, 8 deletions — the freeze set the request note names. |
| `./tools/verify.sh docs --require` | exit 0; Zensical strict build, DocFX 0 warnings/0 errors, rustdoc all crates. |
| `./tools/verify.sh policy --require` | exit 0; ApiCompat clean, 26 contract artifacts current, model/parameterization/lattice projection checks OK, 60 process tests OK, SPDX 780 files, secrets 1218 files, cargo-deny OK. |
| `python3 tools/check_doc_math.py` | exit 0; 180 files scanned, conventions OK. |
| `dotnet test src/dotnet/Mocassin.Compilation.Tests/Mocassin.Compilation.Tests.csproj -c Release --filter "FullyQualifiedName~CeriaClusterCompilesToSortedCanonicalOccupations"` | Passed 1/1. |
| Scratch .NET program in `/tmp/opencode/ceria-check` (never committed) using the public `ModelCompiler` on `samples/ceria/ceria.mocprj` | 1 cluster table, centre site 2, 3 cluster slots (centre + 2 members), `OccupationCount == 6`; canonical codes `8322, 8323, 12418, 12419, 12482, 12483`; expansions `8322:{8322}`, `8323:{8323}`, `12418:{8386,12418}`, `12419:{8387,12419}`, `12482:{12482}`, `12483:{12483}`; 8 expansion pairs, 8 distinct; global map `8322→0, 8323→1, 8386→2, 8387→3, 12418→2, 12419→3, 12482→4, 12483→5`. |
| Python `msgpack` inspection of `tests/contracts/packages/*/payloads/model-1.bin` | `msl-v1`: 0 cluster tables; `step-13-populated`: 1 table, 12 codes `[4161,4162,4225,4226,8257,8258,8321,8322,12353,12354,12417,12418]`, mapping `[1,2]`; `step-13-centered`: 0 cluster tables. |
| Raw-byte inspection of `step-13-populated/payloads/model-1.bin` around `occupation_codes` | `9c cd 10 41 cd 10 42 ...` — codes are MessagePack `uint16` (3 bytes), not 8-byte values; the smallest-width encoding is confirmed. |
| `git status --short` before the record edit | Empty; I changed no repository file. |

**Positive verifications not turned into findings.** The ceria 8→6 case is exactly as the amendment states:
six canonical occupations, the two non-canonical raw codes `8386`/`8387`, canonical `12418`/`12419`, and the
canonical self-entry present for every orbit. The original's `CreateAllGroupLookupCodesOnModel` does store every
`OccupationState` code (`GroupEnergyModelBuilder` lines 104–124, read read-only), so the "regression for the
non-P1 multi-member case" framing is accurate. `signatures.model` writes each occupation's `canonicalCode`,
`canonicalTuple`, and every expansion `occupationCode`/`permutationIndex` (`ModelSignatureProjection` 193–226;
manifest `clusterTables[].occupations[].expansions`), and `signatures.parameterization` is the six-field
`[formatVersion, modelSignature, pairTables, groupSets, siteEnergies, transitionRules]` projection with no
cluster codes. The manifest `energy` member and the generated schema are open objects
(`additionalProperties: true`), and `package-summary-v1` carries `clusterTableCount` only. The `PKG010`
mapping is real: `PackageVerifierEngine.CheckModelInvariants` wraps every `PackageVerificationChecks.ValidateModel`
issue into `PackageDiagnosticCodes.OutOfBounds`. The v1 basis is genuine: ADR 0009's "Pre-release package-shape
finalization" says the version-bump rule applies "from the first released `.msl`", and ADR 0010's signature
finalization says the same. The fixture inventory is accurate: `msl-v1` and `step-13-centered` have no cluster
table, and `step-13-populated` has one P1 table with 12 singleton codes.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **A2-01 — The reader-bound argument for `code_expansions` is arithmetically wrong, so the "no new `ReaderLimits` field" decision is unsupported.** The design says "Every `expanded_codes` entry occupies at least 8 payload bytes, so `N <= model_payload_bytes / 8` ... the typed allocation is at most `12N ≤ 1.5 × model_payload_bytes`, inside the existing 512 MiB guard". The amendment itself says the writer uses MessagePack's **smallest-width** integer encoding, and the checked fixture proves it: a code like 4161 is `0xcd 0x10 0x41` (3 bytes), values below 128 are 1 byte, and values in 65536..2^32-1 are 5 bytes; `canonical_indices` is 1–5 bytes. A hostile package can keep `G` small (so every index is a 1-byte fixint) and fill `expanded_codes` with distinct ascending 5-byte codes; for a 64-particle package that permits the full code space, a 512 MiB payload can carry roughly 89M entries and the typed `Vec<u64>` + `Vec<u32>`/`ImmutableArray` pair is `12N` ≈ 1.07 GiB — about **2×** the 512 MiB guard, not 1.5×. The .NET side has the same exposure: `PackagePayloadDecoder` decompresses under a 512 MiB guard, `MessagePackSerializer.Deserialize<ModelPayload>` allocates the typed arrays before `PackageVerificationChecks.ValidateModel` runs, and `PackageInspectionOptions.MaxModelPayloadBytes` is 512 MiB. The existing `occupation_codes` already has an `8N` typed allocation, so part of this is a pre-existing class; the new map adds a second `4N` array, and the design's fixed "8 bytes per entry" premise and 1.5× conclusion are still false, so the no-cap decision needs revisiting either way. The phrase also appears in `02-data-contracts.md` §3.4 and `msl-format.md`, so the error is in three normative places. Either add an explicit expansion-entry cap (a `ReaderLimits` field such as `max_cluster_expansion_entries`, enforced in the scan/typed map, plus an equivalent `.NET` option) or correct the bound and accept the amplification explicitly with owner sign-off; do not ship the 1.5× claim. | Amendment §6.3, §4 (smallest-width), `02-data-contracts.md` §3.4, `msl-format.md` ("1.5 ×"), and the §9.2/risk table; `src/rust/crates/mocassin-types/src/limits.rs:108-129` (`max_model_elements = 512_000_000`); `src/rust/crates/mocassin-msl/src/scan/cursor.rs:97-106` (`check_declared`), `:251-372` (`skip_value`); `src/rust/crates/mocassin-msl/src/scan/model.rs:350-424`; `src/dotnet/Mocassin.Results/Packages/Internal/PackagePayloadDecoder.cs:20-21,43-57`; `src/dotnet/Mocassin.Results/Packages/PackageInspection.cs:105`; `tests/contracts/packages/step-13-populated/payloads/model-1.bin` bytes `9c cd 10 41 ...`. |
| **Minor** | **A2-02 — The Rust scan's optional-key rule is unspecified; a literal implementation of "validate §4 in the scan" breaks the pre-map package.** The Rust reader is two passes: `scan_model` (a non-allocating walk) and then the typed decode. `scan_map` **requires every key in its `known` list** and only skips unknown keys; `CLUSTER_TABLE_KEYS` is the fixed four-key list. If the new reader adds `code_expansions` to that list (the natural reading of "the reader validates §4 in a single pass"), `amend-2-pre-map`/`amend-1-net10` fail at the scan with "required key absent" before `#[serde(default)]` can apply; if the key is left out, the scan skips it (bounded by `skip_value`) and the typed mapper must validate it. The design only describes the serde layer and never names which layer owns the structural validation. State the choice explicitly: either add an optional-known-key path to `scan_map` (with a test), or keep `code_expansions` out of `CLUSTER_TABLE_KEYS` and validate in `map/model.rs`; the pre-map fixture test must assert the scan accepts absence. | `src/rust/crates/mocassin-msl/src/scan/mod.rs:31-78` (`scan_map`, `seen != required`, `None => cursor.skip_value()`); `src/rust/crates/mocassin-msl/src/scan/model.rs:55-60` (`CLUSTER_TABLE_KEYS`), `:383-417`; amendment §6.1/§6.3. |
| **Minor** | **A2-03 — The public API plan preserves the four-argument constructor but not the generated four-argument `Deconstruct`.** `ClusterTablePayload` is a positional record; its generated `Deconstruct(out string, out ImmutableArray<ulong>, out ImmutableArray<double>, out ImmutableArray<byte>)` is a public API member in `PublicAPI.Unshipped.txt:58`. Adding a fifth primary-constructor parameter removes it, and the design only says "the existing four-argument primary constructor is preserved as an overload". Add a manual four-argument `Deconstruct` (which coexists with the synthesized five-argument one) or add the new member as a non-positional `[Key]` property so the whole generated surface is preserved; the API gate and the payload round-trip should cover the chosen shape. | `EnergyPayloadDtos.cs:49-53`; `PublicAPI.Unshipped.txt:55-67` (constructor, `Deconstruct`, properties); amendment §5; the `policy` ApiCompat/PublicAPI gate. |
| **Minor** | **A2-04 — Rule 2's "exactly the two keys" is unenforceable with the documented `ignore` policy and would block future additive nested members.** The payload policy is `unknownFields: ignore`; the Rust scanner skips unknown keys; MessagePack-CSharp ignores them. A verifier that rejects a third key inside `code_expansions` needs custom raw-map inspection (the typed DTO has already discarded it), and the rule contradicts ADR 0013's additive direction. Say "the two keys are required; unknown keys are ignored" (the normal policy), or explicitly justify why the nested map is stricter. | `contracts/manifest.json` `formats.package.payloads.unknownFields: "ignore"`; `scan_map`'s `None => cursor.skip_value()`; `PackagePayloadDecoder` (`UntrustedData`, no unknown-key rejection); amendment §4 rule 2. |
| **Minor** | **A2-05 — The coverage proof's "exactly the set of enumerated tuples" is a superset claim, not an equality, when a member site's allowed particle set is not invariant under the induced permutations.** `ClusterReduction.AddTuple` adds `O(canonical) = {permute(canonical, p)}` for every induced permutation, without filtering by the per-slot allowed sets. If two geometry sites are swapped by the group but have different particle sets (e.g. `{O,Vc}` vs `{O,Vc,H}`), the expansion list contains codes whose particle is not allowed at that slot. Those codes are harmless for exact-code lookup (the runtime never builds them), but the map is a **superset** of the writer's enumerated tuples, the "exactly" proof is wrong, and the map can be larger than the "compact" claim implies. The producer cannot filter from `Expansions` alone (the member allowed sets are not in `CompiledClusterTable`), so state the superset semantics and add a property case with asymmetric allowed sets; do not assert exact set equality in the oracle. | `ClusterReduction.cs:203-241` (`AddTuple`), `:257-273` (`ClusterBuilder`); `ClusterCompilationStage.cs:127-204` (per-slot `surroundingAllowed`); `SpaceGroupOperations.GetPointGroup` (permutations are geometry-derived); amendment §3 coverage proof and §9.3 property test. |
| **Minor** | **A2-06 — The byte-fixed pre-map fixture is singleton-only, so the real F-29 absence case has no real fixture.** `amend-2-pre-map` is a copy of `step-13-populated`, which is P1 with 12 singleton codes; its absence path is trivially complete and cannot exercise the recompile-required branch. The actual F-29 case is a **pre-map non-singleton** table. The design covers it only with a synthetic non-canonical code (`Rust reader: absent map`) and defers the real behaviour to Step 14. Either build a byte-fixed pre-map two-member package at the parent commit (the same temporary-worktree technique as the one-time forward read) and commit it, or record why the synthetic test is the accepted evidence and who owns the real-package check. | `tests/contracts/packages/step-13-populated/README.md` (P1, 12 codes), `payloads/model-1.bin` (12 codes, mapping `[1,2]`); amendment §6.2/§9.2/§9.3; F-29's "Add a non-P1 two-member cluster fixture ... and a fail-closed test". |
| **Minor** | **A2-07 — The `step-11.1` evidence gap is recorded against the checker but not against the normative audit/closeout pages that still require a `step-NN.json`.** `check_closeout_evidence.py` rejects `step-11.1` (`STEP_RE = ^step-\d{2}[a-z]?$`, verified), and the design records that in open decision 7. But `docs/planning/audits/README.md` §3.3/§3.4 require "a link to the closeout evidence file" and "Fill `docs/planning/closeout/step-NN.json`", and `docs/planning/audits/step-audit-template.md` says "Do not restate them here: one source, one place"; the design instead puts the audited commit, CI, `main` SHA, merge, and tag in the audit record. Add a short "post-merge amendment audits" note to the audits/closeout READMEs (and the template) that names the audit record as the evidence source for a non-step ID, or extend the checker/template for `step-NN.N`. Do not leave the normative instruction unamended. | `tools/check_closeout_evidence.py` `STEP_RE` (line 22); `docs/planning/closeout/README.md` "Step ID form"/"From Step 11 ... single source"; `docs/planning/audits/README.md` §3; `docs/planning/audits/step-audit-template.md` request table; amendment §10.6/§10.9 and open decision 7. |
| **Minor** | **A2-08 — F-29's formal closure is an external Step 14 gate, but the design lists it as a step-11.1 acceptance criterion and the `main` Step 14 page still says "Design review: Not requested".** The authoritative design and the F-29 record are on unmerged branches (`87be9de`, `1ea8090`); the freeze only adds a pointer to `main`'s Step 14 page, which still has "Design review: Not requested" and "Audit: Not requested". The step-11.1 branch cannot satisfy "The Step 14 design re-review references this amendment and F-29 is closed by reference" by itself. Record the closure as a Step 14 gate owned by the Step 14 planner/step agent, have the step-11.1 audit treat the pointer as a follow-up rather than a completed criterion, and name the commits/record in the page note (or update the header state) so a `main` reader can find them. | `docs/planning/steps/step-14-simulation-context-initialization.md` (cross-step note and header); `review/step-14-design-round-3` `1ea8090`; design `87be9de`; amendment §9.3 "Step 14 lookup", §10.9, Acceptance criterion; request dependency bullet. |
| **Minor** | **A2-09 — The "record the change in ... the Step 13 page's API note" pointer does not exist.** The Step 13 page has no API-note section; the only API mention is the feature list ("the Rust workspace crate boundaries ... public API documentation"). Name the actual location (a new post-merge note on the Step 13 page, or the amendment only) and include the `missing_docs`/rustdoc consequence. | `docs/planning/steps/step-13-rust-solver-foundation-and-package-reader.md` (no API-note section); amendment §6.1. |
| **Minor** | **A2-10 — The new sample is absent from the sample documentation plan.** `samples/README.md` lists every sample directory, and each sample has a README (e.g. `samples/step-13-populated/README.md`); the design adds `samples/step-11-cluster-expansion/step-11-cluster-expansion.mocprj` but only plans the fixture README under `tests/contracts/packages/step-11-amend-2/`. Add the `samples/README.md` row and the sample README (purpose, derivation from `samples/ceria`, job, build/regeneration hint) to §8/§9. | `samples/README.md`; `samples/step-13-populated/README.md`; amendment §9.1; §8 documentation table. |
| **Minor** | **A2-11 — ADR 0013's "safe for every document written before it existed" is broader than the first application's guarantee.** For a pre-map **non-singleton** table, absence is not functionally readable; it is fail-closed with a rebuild message. The amendment §7 says so, but the ADR addendum's condition can be read as requiring full readability. Define "safe" in the addendum as "fails closed with an actionable error and never yields a wrong value; a rebuild may be required", and note that the first application is exactly that case. | `docs/planning/adr/0013-format-compatibility-and-api-versioning.md` addendum bullet 2; amendment §7 "Old packages with a non-singleton cluster"; `02-data-contracts.md` §3.3. |
| **Note** | **A2-12 — "Assert byte identity on rebuild" for the 0-cluster fixtures is host-sensitive.** The `.msl` records `schema_info.app_version`/`generator` from the entry assembly; `step-13-populated/README.md` says "Package bytes are a writer+runtime artifact" and may change with a runtime patch, while `msl-v1/README.md` says the canonical **golden** (not the bytes) is stable across hosts. Keep the byte-identity assertion as a same-host observation, or normalize the build metadata / compare the extracted payloads and summary; do not let a runtime patch look like a writer regression. | `tests/contracts/packages/step-13-populated/README.md` ("Package bytes are a writer+runtime artifact"); `tests/contracts/packages/msl-v1/README.md` (golden stability); amendment §9.2. |
| **Note** | **A2-13 — The one-time forward read is labelled `PR-medium`; it is a recorded manual check.** The table lists "Forward read (one-time) PR-medium", but it is run once at a parent commit in a temporary worktree and recorded in the feature log. Retier it as evidence (like the previous amendment's one-time cross-TFM check) and keep the recurring fixtures PR-fast. | Amendment §9.3, §9.4; `docs/planning/03-quality-strategy.md` tiers. |
| **Note** | **A2-14 — The ten-slot check permits a nonzero field 9 for tables with fewer than ten slots.** `code >> 60 == 0` plus ten fields `< particle_count` matches the existing `occupation_codes` rule (and the Rust reader), but a table with 3 slots accepts a code with particles in slots 3..9; such a code is unreachable at runtime and harmless. Keep the existing convention for consistency; do not tighten one side only. | `src/rust/crates/mocassin-msl/src/map/model.rs:224-240`; `ClusterReduction.Encode/Decode` (`MaxClusterSites = 9`); amendment §4 rule 6. |
| **Note** | **A2-15 — The existing `step-13-populated/README.md` size table is already stale and the regeneration plan should refresh it.** The README says `payloads/model-1.bin` is 6,518 bytes, while the file and `payloads/index.json` say 7,009; the design's §9.2 only names the payload/index/summary files. Add the README size/SHA table to the regeneration step. | `tests/contracts/packages/step-13-populated/README.md`; `payloads/index.json` (`rawBytes: 7009`); the file size; amendment §9.2. |
| **Note** | **A2-16 — `PKG010`'s text is narrower than the new checks.** "A payload value is out of bounds for its declared dimensions" is used for non-ascending/duplicate/coverage failures, which are structural rather than out-of-bounds. The reuse is consistent with the existing duplicate-pair-link checks and the design's no-new-code decision; a later docs pass could broaden the descriptor help text without changing the code. | `PackageDiagnosticDescriptors.cs` `OutOfBounds`; `PackageVerifierEngine.cs:194-231`; amendment §4/§5 ("no new `PKG` diagnostic code"). |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The implementor adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Changes required

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

I am a fresh context spawned by the orchestrator for this focused amendment review. I did not author or implement
the amendment or the parent Step 11 design, I did not participate in the Step 11 or Step 14 design reviews, and I
have no shared conversation with the planner, implementor, or orchestrator. I read the repository at the frozen
design commit `6ff913671351798cb64678b506986ea6649364a4` and verified that the only later commit
`d78747d838a2c558b044185d77482bbbf2940be1` changes this record alone
(`git diff --name-status 6ff9136..HEAD` -> `M docs/planning/reviews/step-11-amend-2.md`); the design docs are
unchanged. I edited only this record; I changed no code, test, configuration, design page, status page, or other
file, and `git status --short` was empty before this record was edited. The human owner approves the final verdict and the
implementation gate; this record is evidence, not an approval.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

**Verdict: Changes required.** The amendment's technical core is correct and well-evidenced: the ceria two-member
cluster really reduces 8 raw tuples to 6 canonical occupations, the two failing raw codes `8386`/`8387` really
resolve to `12418`/`12419` in the existing `CompiledClusterOccupation.Expansions`, the producer needs no second
symmetry pass, `signatures.model` already projects every expansion code and permutation index, the
parameterization projection has no cluster codes, `package-summary-v1` has no code-level fields, and the
ADR 0009/0010 pre-release basis genuinely supports `format_version = 1` while `.msl` is unreleased. The
**Major** finding is not a feasibility problem: the reader bound that justifies "no new `ReaderLimits` field"
is false under the smallest-width MessagePack encoding the design itself specifies, and the worst-case typed
allocation is about twice the 512 MiB guard on both the Rust and .NET read paths. Ten Minor findings cover
the API surface, the coverage proof's superset semantics, the singleton-only pre-map fixture, the post-merge
evidence/template conflict, the Step 14 closure sequencing, the missing sample docs, and ADR wording; five Notes
record lower-risk observations. No code exists, so no implementation gate is open.

**Severity counts:** 1 Major, 10 Minor, 5 Note, 0 Blocker. The Major must be corrected (or explicitly waived by
the human owner) before the implementation gate; the Minor findings should be fixed or explicitly accepted.

Follow-up owner: Planner (design corrections); the step agent takes the implementation after the owner's
decision.
Follow-up step: Re-review `step-11-amend-2` at a new frozen commit after the Major/Minor corrections; Step 11
remains `Done`/tagged and `step-11` is not moved. The Step 14 design branch (`87be9de`) and F-29 record
(`1ea8090`) remain the separate consumers.

## Approval summary

The request owner (planner or implementor) completes this section before the orchestrator presents the merge/tag
approval request. It is not part of this design-review request.

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

The implementor fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

Fresh context, no shared conversation with the planner, implementor, or orchestrator; I did not author or
implement the amendment or the parent Step 11 design. The frozen design is
`6ff913671351798cb64678b506986ea6649364a4`; the only later commit is
`d78747d838a2c558b044185d77482bbbf2940be1`, which changes this record alone
(`git diff --name-status 6ff9136..HEAD`). I read the amendment page, the parent Step 11 page and review
records, the Step 14 pointer and the F-29 round-3 text, the Step 13 page, the amendment/review/audit/closeout
READMEs and template, `AGENTS.md` §3.8–§3.13, the data contracts, architecture, open decisions, `msl-format.md`,
ADRs 0009/0010/0011/0013, the real writer/reduction/verifier/Rust-reader code, the manifest and generated schema,
and the checked fixtures.

#### Commands and results

The command/result table is in `Evidence`. Key results: `./tools/verify.sh docs --require` exit 0 (Zensical
strict, DocFX 0 warnings/0 errors, rustdoc); `./tools/verify.sh policy --require` exit 0 (ApiCompat, 26 contract
artifacts, projection checks, 60 process tests, SPDX 780, secrets 1218, cargo-deny); `check_doc_math.py` exit 0
(180 files); the ceria cluster test passes; a scratch public-API program reproduced 6 canonical occupations and
the 8-entry map (`8386→2`, `8387→3`); the fixture payloads confirm `msl-v1`/`step-13-centered` have 0 cluster
tables and `step-13-populated` has 1/12; the raw `occupation_codes` bytes confirm 3-byte smallest-width
encoding; `git status --short` was empty before this record was edited.

#### Findings

A2-01 (Major) the 8-bytes-per-entry / 1.5× reader bound is false, so the "no new `ReaderLimits`" basis and the
hostile-map allocation story do not hold. A2-02 (Minor) the Rust scan's all-known-keys rule needs an explicit
optional-key decision or the pre-map fixture fails. A2-03 (Minor) the generated four-argument `Deconstruct` is
not preserved by the five-argument primary constructor. A2-04 (Minor) rule 2's "exactly two keys" conflicts
with the `ignore` policy and future additive members. A2-05 (Minor) the coverage proof's "exactly" is a
superset claim for asymmetric allowed sets. A2-06 (Minor) the byte-fixed pre-map fixture is singleton-only, so
the real F-29 absence case is synthetic. A2-07 (Minor) the `step-11.1` evidence gap is recorded against the
checker but not the audit/closeout READMEs and template. A2-08 (Minor) F-29's formal closure is an external
Step 14 gate, and `main`'s Step 14 page still says "Design review: Not requested". A2-09 (Minor) the Step 13
"API note" pointer does not exist. A2-10 (Minor) the new sample is missing from `samples/README.md` and has no
sample README. A2-11 (Minor) ADR 0013's "safe" condition is broader than the fail-closed guarantee.
A2-12..A2-16 (Notes) byte-identity host sensitivity, the one-time forward-read tier, the ten-slot looseness,
the stale fixture README size, and the `PKG010` text. Details and evidence are in the top-level `Findings` table.

#### Verdict

**Changes required** — 1 Major, 10 Minor, 5 Note, 0 Blocker. The amendment's technical core is accepted:
F-29 is real, the ceria 8→6 case and the two failing codes are reproduced, the producer data exists, the
signature/summary non-impact claims hold, and the pre-release v1 basis is documented. The Major is a false
reader-bound justification that must be corrected (or explicitly waived) before implementation; the Minor
findings are design, API-surface, fixture, process-record, and documentation corrections. Re-review at a new
frozen commit after the corrections; earlier rounds are not overwritten.
