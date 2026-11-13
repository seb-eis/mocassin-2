# Step 08 Amendment 1 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-08-amend-1.md`. You may edit only your own metadata (`Reviewer`, `Review date`,
> `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 08 — Model Compilation Pipeline — Amendment 1 |
| Request owner | Step agent |
| Orchestrator (step agent / planner) | Step 08 implementor |
| Review initiated by | Human owner |
| Reviewed commit | `4036100f8fae658ce4fc11587aa88eb454aa84d9` |
| Docs branch | `step/08-model-compilation-pipeline` |
| Review date | 2026-11-13 |
| Reviewer | Fresh independent design-review context (DeepSeek V4.1 Flash 1017, OpenCode review subagent); no shared conversation with the planner or implementor |

Amendment page: [`Step 08 Amendment 1 — Canonical Compiled-Model Projection`](../amendments/step-08-amend-1-canonical-projection.md)

Parent step page: [`Step 08 — Model Compilation Pipeline`](../steps/step-08-model-compilation-pipeline.md)

> Orchestration: the step agent or planner freezes the design, requests the reviewer, applies fixes
> autonomously on the design docs or a maintenance branch, adds resolutions without altering reviewer text,
> and requests re-review until the reviewer no longer identifies issues. The human owner approves the final
> verdict and the start of implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | Steps 02–07 |
| Delivers | A canonical, manifest-driven compiled-model signature projection and encoder that replaces the temporary non-conforming layout rejected by audit finding A2. |

## Design decisions reviewed

- [x] Amendment rationale and scope — A2 is correctly identified; scope is limited to the projection and encoder.
- [x] Manifest projection schema and canonical encoding rules — reviewed; the nested schema is still deferred (F1) and the byte rules are ambiguous (F2, F3).
- [x] ADR 0010 compliance and versioning — reviewed; ADR intent is followed, but float/integer rules need restating (F2, F3); `model-v1` retention is justified (N2).
- [x] Dependency and licensing impact — reviewed; the MessagePack choice is acceptable, but the "already used" claim is inaccurate and the Python `msgpack` dependency is undeclared (F7).
- [x] Cross-language vector and test plan — reviewed; the Python vector input, independence, coverage, and gate are underspecified (F4, F5, F11).
- [x] Parent step status, audit, and integration plan — reviewed; the combined-audit re-run is stated, but the closeout CI/evidence refresh is missing (F6, F12).

## Dependency and interface checks

- [x] Dependencies are `Done` and their contracts are stable — Steps 02–07 are `Done`; Step 08 is in audit, not complete, which matches the amendment process.
- [ ] Inputs, outputs, types, and formats are defined — not fully: the nested fields and canonical byte rules are deferred or ambiguous (F1–F3, F8).
- [x] Versioning and migration behavior is defined — `model-v1` is retained pre-release, no compatibility shim, and caller caches must be invalidated; the rationale should cite the pre-release precedent (N2).
- [x] No unresolved cross-step interface question remains — Step 11 reuse is stated; Python is interim evidence and Step 11 must own the Rust vector (N3).

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable — explicit and mostly testable; the central schema acceptance is not met by the design itself (F1).
- [ ] Design decisions are complete and internally consistent — not complete: the nested schema is deferred and the dependency/public-API statements contradict the design (F1, F7, F10).
- [x] Required tests, property tests, golden fixtures, and performance targets are defined — a test table exists; cross-language and descriptor-conformance coverage is insufficient (F4, F5, F11).
- [x] Documentation, status, parity, and audit updates are planned — planned, but the closeout CI/evidence steps are missing (F6); the inherited docs-link failure (m4) remains.

## Amendment-specific checklist

- [ ] The manifest schema is complete enough for an independent encoder. Not met: the nested sections are `array<...>` placeholders and the page defers the field lists to implementation (F1).
- [ ] Every nested section field has a name, type, and order. Not met: only top-level fields have names/types/order (F1).
- [ ] The fixed-width integer, float64, string/bin, nil, and array/map rules are unambiguous. Not met: "fixed-width raw integers", float rounding, bool, and nil are underspecified (F2, F3, F8).
- [x] The standard MessagePack package is the right encoder boundary and the dependency is acceptable. — MessagePack 3.1.12 is MIT and centrally pinned; the page's "already used" claim is wrong and the Python `msgpack` tooling dependency is undeclared (F7).
- [x] `model-v1` versioning is justified for an unreleased format. — justified: no `.msl`/compiled model has been released and `02-data-contracts.md` §3.3 already keeps `format_version` at 1 pre-release; cite that precedent (N2).
- [ ] The Python independent vector is sufficient cross-language evidence. Not met: input, descriptor-to-model mapping, coverage, dependency pin, and gate are undefined; the inspection JSON omits projection fields (F4).
- [x] The exclusion of energy values, runtime settings, and inspection-only fields is preserved. — preserved in intent; the design states only energy/runtime and does not enumerate or test the inspection-only exclusions (F9).
- [ ] The audit finding A2 and parent audit re-run are explicitly addressed. — A2 and re-audit are named, but A2 is not closed by the design (F1) and the integration/CI checklist is incomplete (F6, F12).

## Evidence

Reviewed tree: `db52abbe8f22bb2987797bd49cc2549137bad63a` on
`step/08-model-compilation-pipeline`; the working tree was clean. The request records `ca1582e...` as the
reviewed commit, but `git diff --stat ca1582e db52abb` is the single-line `Reviewed commit` metadata change in
this record, so the design content reviewed is `ca1582e` and the frozen tree is `db52abb`. There is no
implementation of the amendment in the tree; this is a design review only.

Commands run:

```text
$ git rev-parse HEAD
db52abbe8f22bb2987797bd49cc2549137bad63a

$ git status --short
(clean)

$ git log --oneline -4
db52abb docs: freeze step 08 amendment design for review [skip ci]
ca1582e docs: propose step 08 canonical projection amendment [skip ci]
288933e docs: record step 08 audit Fixes required [skip ci]
911408b docs: record step 08 closeout evidence and audit request [skip ci]

$ git diff --stat ca1582e db52abb
 docs/planning/reviews/step-08-amend-1.md | 2 +-
 1 file changed, 1 insertion(+), 1 deletion(-)

$ python tools/contracts/generate.py --check
Contract artifacts are current (18 files)

$ python tools/check_docs_links.py
Broken documentation links:
  docs/planning/steps/step-08-model-compilation-pipeline.md:17: missing link target: audits/step-08.md
(exit 1; this is the inherited parent-audit m4, not introduced by the amendment)

$ grep -rn "MessagePack" src/dotnet --include=*.csproj
(no matches; the only pin is Directory.Packages.props:27-28)

$ python -c 'import msgpack; print(msgpack.version)'
(1, 2, 4)
(installed in this review environment, but not declared in src/python/pyproject.toml and only listed for
 Step 22 in docs/planning/06-toolchain-requirements.md:126)
```

Files reviewed in addition to the amendment page:

- `AGENTS.md` §3.4, §3.8, §3.9, §3.11, §3.12, §5, §11, §13.
- `docs/planning/amendments/README.md` §3–§6 and the amendment index.
- `docs/planning/reviews/README.md` §5, §8, §10.
- `docs/planning/audits/step-08.md` — request and findings A1–A9, m1–m7, N1–N2 (A2 at line 182; the verdict is
  `Fixes required` for the whole step, not only A2).
- `docs/planning/adr/0010-canonical-messagepack-signatures.md` rules 1–8 and Tooling.
- `contracts/manifest.json` `signatures.model` (line 1905) and `fixtures.signatureVectors` (line 2047);
  `contracts/generated/signatures/model-v1.json`.
- `tools/contracts/generate.py` (`validate_field`, `build_artifacts`) and `tools/contracts/README.md`.
- `docs/planning/02-data-contracts.md` §1.3 and §3.3 (the pre-release `format_version` precedent at lines
  768–770).
- `docs/planning/steps/step-08-model-compilation-pipeline.md` §11.3–§11.4 and the implementation-status block.
- `docs/development/compilation-pipeline.md` §Signatures and the compiler purity statement.
- `docs/planning/06-toolchain-requirements.md` Python table (`msgpack` 1.2.4, Step 22).
- `src/dotnet/Mocassin.Compilation/Signatures/ModelSignatureProjection.cs` — the current custom
  `BinaryWriter` encoder, including `WriteDouble` rounding at lines 380–390 and the `?? string.Empty`
  chiral-partner encoding at line 182.
- The compiled-model records used to define the projection:
  `Structure/CompiledStructureModel.cs`, `Energy/CompiledEnergyModel.cs`,
  `Energy/CompiledClusterTable.cs`, `Transitions/CompiledTransitionModel.cs`,
  `Simulations/CompiledSimulation.cs`.
- `src/dotnet/Mocassin.Compilation/Signatures/CompiledModelJson.cs` — the `compiled-inspection-v1` projection
  available to a Python checker.
- `src/dotnet/Mocassin.Compilation.Tests/Compiler/GoldenModelTests.cs` and
  `tests/contracts/signature-vectors.json` — the current golden pins and their bookkeeping.
- `Directory.Packages.props` — `MessagePack` and `MessagePack.Annotations` 3.1.12.
- `docs/planning/reviews/step-08.md` — the parent review links to this amendment review.

The key design inconsistency is visible by comparison: the amendment promises "the exact nested field lists"
but the Design section uses `array<...>` placeholders for every nested section (lines 63–67, 69–79), while the
compiled records contain fields the prose does not mention and the current encoder includes or omits without a
written rule (for example `CompiledSite.Name`, `CompiledPairGeometry.CenterSiteId`/`PartnerSiteId`/
`DistanceAngstrom`, `CompiledClusterTable.SitePIndices`/`GeometryEncodings`, `CompiledJumpDirection.PathLength`,
`CompiledJumpRule.FrequencyHz`/`StaticCorrection`, and `CompiledSimulation.Name`/`Intent`).

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Blocker** | **F1 — The nested projection schema is still not defined; the amendment defers it to implementation.** The Design table gives only top-level names/types and `array<...>` placeholders for `structure.sites/environments/positions`, `energy.pairGeometries/pairTables/clusterTables`, `transitions.collections/directions/rules/trackers/jump tables`, and `simulations`; line 69 then says "The implementation adds the exact nested field lists to the manifest." That is the same definition gap A2 rejected: a second language cannot derive field order/types, and the design cannot review the bytes. The prose also conflicts with the compiled records and the current encoder on named fields (site name/fractional/particle set; pair centre/partner site IDs and distance; cluster site P indices/geometry encodings; direction path length; rule frequency/static correction; simulation name/intent). Add the complete ordered tables (name, manifest type, fixed length, nullable/nil, include/exclude rationale) for every nested section, and make the manifest the written definition before implementation starts. | Amendment lines 53–79; `ModelSignatureProjection.cs` (structure/energy/transitions/simulations); `CompiledStructureModel.cs`, `CompiledEnergyModel.cs`, `CompiledClusterTable.cs`, `CompiledTransitionModel.cs`, `CompiledSimulation.cs`; parent audit A2 at `audits/step-08.md:182`. |
| **Major** | **F2 — Canonical integer and framing rules are ambiguous.** "fixed-width raw integers" does not state whether `uint64` is a MessagePack `0xcf` marker plus 8 big-endian bytes, or an unwrapped 8-byte value; whether `int64` is `0xd3`; whether `bool` is MessagePack true/false or uint64 `0`/`1`; whether `nil` is `0xc0`; or whether string/array/bin headers use standard minimal widths. The Python sentence ("`msgpack` for framing plus fixed-width integer headers") hints at markers but is not normative. A second language cannot encode identical bytes from this. Specify the exact marker/width/byte order for each manifest type, state that no raw unwrapped integers are used, and pin the first bytes in a test. | Amendment lines 54, 85–88, 95–97; ADR 0010 rules 2–6; data contracts §1.3. |
| **Major** | **F3 — The float64 rule omits the existing 12-decimal quantization/rounding.** `WriteDouble` rounds every double to 12 decimals with `MidpointRounding.AwayFromZero` and normalizes `-0.0`; the amendment mentions only `-0.0` and non-finite rejection. `volume`, cell vectors, fractional positions, path vectors, and charge-transport vectors will differ if Python does not apply the same rounding. State whether quantization applies to all float64 fields, only charge-transport components (parent step §11.4), or none; specify the rounding mode and its order relative to `-0.0` normalization; add a non-trivial fractional vector. If quantization is removed, update the parent design and explain the byte change. | `ModelSignatureProjection.WriteDouble` lines 380–390; amendment lines 87–88; parent step §11.4; `CompiledStructureModel.CellVolumeAngstrom3`. |
| **Major** | **F4 — The independent Python vector is not specified enough to be independent, reproducible, or gated.** The page does not say what input the Python encoder consumes. The available `compiled-inspection-v1` JSON omits projection fields (site `fractional`/`particleSetId`, pair centre/partner site indices, cluster `sitePIndices`/`geometryEncodings`, and the model `formatVersion` — the JSON's `formatVersion` is the `compiled-inspection-v1` version, not `CompiledModel.FormatVersion`), so Python cannot re-encode the projection from it without a new fixture/mapping. If Python merely decodes C# projection bytes and re-encodes, it is a canonicality check, not an independent encoder. Coverage is ceria only, so MMC/chiral/cluster/tracker shapes are not cross-checked. `msgpack` is not declared/pinned for Step 08 (toolchain says Step 22) and contradicts the "no new dependency outside MessagePack 3.1.12" non-goal. Define the input fixture and descriptor-to-model mapping, add at least ceria KMC + ceria MMC (or all golden samples), pin `msgpack`, and add the checker to `verify.sh contracts`/`python` with a documented independence claim. | Amendment lines 39, 47, 93–98, 137–143; `CompiledModelJson.cs` fields; `06-toolchain-requirements.md:126`; `tools/contracts/README.md:20`; no `msgpack` in `src/python/pyproject.toml`. |
| **Major** | **F5 — No test binds the hand-written C# encoder to the manifest descriptor.** ADR 0010 permits a small hand-written encoder, but the test plan only validates the manifest and pins C# golden values. Nothing asserts that the C# field order/types match `contracts/generated/signatures/model-v1.json`; a manifest edit that the encoder misses would only be caught if the Python vector exercises that field (ceria only). Add a C# descriptor-conformance test (ordered field/type list) or generate the order/type table, and cover a second sample with MMC/chiral/cluster shapes. | Amendment lines 85–86, 95–98, 137–143; `ModelSignatureProjection.cs`; `tools/contracts/generate.py`; no descriptor-conformance test. |
| **Major** | **F6 — The integration/audit plan omits the required closeout CI, evidence refresh, and combined-commit process.** The page says the parent audit must be re-run at the combined commit but does not list: the squash commit, merge current `main`, `verify.sh all --require`, the replacement closeout CI run, refreshed `docs/planning/closeout/step-08.json`, the merge-tree rule, or the parent audit/step-page status updates. AGENTS §3.4.4 requires the closeout CI to be re-run after the final amendment; §3.11.7 and amendments README §6 require the combined audit. Add the full checklist and state that the parent audit still has A1/A3–A9 open, so the combined re-audit must cover them. | Amendment lines 162–183; AGENTS §3.4/§3.11; amendments README §6; parent audit `Fixes required`. |
| Minor | **F7 — Dependency statements are inaccurate/contradictory.** No `src/dotnet/**/*.csproj` references MessagePack; it is only centrally pinned for future Steps 11/18/20. Adding it to `Mocassin.Compilation` is acceptable (MIT, central pin), but the page should call it a new project dependency and record that `verify.sh api`/`check_nuget_licenses.py` audits it. The Python `msgpack` checker is a new tooling dependency (Apache-2.0, 1.2.4) and contradicts "No new dependency outside the centrally pinned MessagePack 3.1.12 package"; declare and pin it, and update `06-toolchain-requirements.md`/`tools/contracts/README.md`. | `Directory.Packages.props:27-28`; no csproj match; amendment lines 47, 83–84, 159; `06-toolchain-requirements.md:126`; `tools/contracts/README.md:20`. |
| Minor | **F8 — Optional/nil and Boolean encodings are untested and the current encoder contradicts the nil rule.** The amendment says absent optional values are `nil`, but `ModelSignatureProjection` writes `ChiralPartnerGeometryId ?? string.Empty`; it also writes `bool` flags as `? 1 : 0`. The page does not list optional fields or say whether `isVacancy`/`isChiral`/`isInverse` are MessagePack bools or uint64 `0`/`1`. List the optional fields, encode nil per ADR rule 6, and add byte-level nil/bool tests. | Amendment lines 63–67, 87; `ModelSignatureProjection.cs:182` and the bool writes; test table lines 137–143. |
| Minor | **F9 — The exclusion list is incomplete in the design and tests.** The acceptance names energy values, runtime settings, timings, diagnostics, file names, and cache state, but the Design states only runtime settings and energy/frequency. The schema must mark display-only/derived fields as excluded (`CompiledSite.Name`, `CompiledPosition.Cartesian`, `CompiledPairGeometry.DistanceAngstrom`, `CompiledJumpDirection.PathLength`, `CompiledSimulation.Name`, `CompiledRuntimeSettings`, `CompiledSolverIntent` fields) and the tests must assert that diagnostics/timings/cache/name edits do not change the signature. The parent §11.3 already requires the exclusions, so this is a delta-completeness gap. | Amendment lines 78–79, 130, 141; compiled records; parent step §11.3. |
| Minor | **F10 — The public-API statement contradicts the named writer type.** The page names `CanonicalProjectionWriter` but says "No PublicAPI change; no ApiCompat baseline change." If the writer is internal, state that; if public, add XML docs, `PublicAPI.Unshipped.txt` entries, and the ApiCompat baseline. Also state that no MessagePack type appears in the public surface. | Amendment lines 85–86, 110–112. |
| Minor | **F11 — Golden-vector bookkeeping is inconsistent.** `tests/contracts/signature-vectors.json` is generated from `contracts/manifest.json` (not hand-maintained) and currently has ceria/vehicle/interstitialcy only, while `GoldenModelTests` also pins ceria MMC and barium zirconate. The amendment says keep the C# golden signature and inspection hash in that file, and the acceptance names ceria/MMC/vehicle/interstitialcy but not BaZrO₃. Clarify which vectors live in the manifest-generated file vs the C# tests, update the manifest fixture list and regenerate, and include BaZrO₃ if it remains a golden acceptance. | `tests/contracts/signature-vectors.json`; `GoldenModelTests.cs:52-55`; `generate.py` `build_artifacts`; amendment lines 98, 142. |
| Minor | **F12 — The audit scope and status bookkeeping understate the parent audit.** The Audit section says "Fixes required (A2)", but the parent audit verdict is `Fixes required` for A1–A9 (A2 is one of three Blockers plus six Majors). The amendment closes only A2; the combined re-audit must cover A1/A3–A9 and the minors. Also, the amendment index/request still say `Planned` while the design is in review. State the combined scope explicitly and advance the status when the review starts. | Amendment lines 177–183; amendments README §3/index; parent audit findings A1–A9. |
| Note | **N1 — Inherited docs-link failure.** `python tools/check_docs_links.py` fails at `docs/planning/steps/step-08-model-compilation-pipeline.md:17` (`audits/step-08.md` should be `../audits/step-08.md`). This is parent-audit m4, not caused by the amendment; the amendment's own links resolve. The integration/status commit must fix it so the combined docs gate is green. | Command output in Evidence. |
| Note | **N2 — `model-v1` retention is justified but should cite the precedent.** `02-data-contracts.md` §3.3 (lines 768–770) already keeps `format_version` at 1 before the first released `.msl`; ADR 0010's version-bump consequence applies to released signed types. Add that citation and the fact that no `.msl`/compiled-model bytes were released. No version bump is required. | Data contracts §3.3; ADR 0010 "Consequences"; amendment lines 100–104. |
| Note | **N3 — Python is a proxy; Step 11 owns the Rust vector.** The production second language is Rust. The amendment says Step 11 reuses the projection; the approval summary should track that Step 11 must implement the Rust encoder and cross-language vector against the same descriptor. | Amendment lines 162–166; parent audit A2 evidence; parent step §11.3. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**.

## Record state

**State:** Changes required

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner or implementor.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

Follow-up owner: Step 08 implementor / planner
Follow-up issue/step: Revise `docs/planning/amendments/step-08-amend-1-canonical-projection.md` with the complete
nested projection tables and canonical byte rules; specify the Python vector input, mapping, dependency, and
gate; add the C# descriptor-conformance test and the closeout CI/evidence integration checklist; then request a
fresh re-review at the new frozen commit.

## Resolutions

The step agent records resolutions here without altering reviewer text.

| Finding | Resolution | Commit |
| --- | --- | --- |
| F1 | The amendment now lists every nested field, field order, and type for particles, structure, energy, transitions, and simulations; no `array<...>` placeholders remain. | this commit |
| F2 | The encoding table fixes array `0xdd`+u32, str `0xdb`+u32, bin `0xc6`+u32, uint64 `0xcf`+8, int64 `0xd3`+8, float64 `0xcb`+8, bool `0xc3/0xc2`, and nil `0xc0`; no maps are used in v1. | this commit |
| F3 | The float64 rule now specifies 12-decimal `MidpointRounding.AwayFromZero` quantization, `-0.0` normalization, and NaN/Inf rejection. | this commit |
| F4 | `CompiledModelJson` gains a complete `projectionInput` object; Python re-encodes it, `msgpack` is declared in `tools/contracts/requirements.txt`, and vectors cover ceria, MMC, vehicle, and interstitialcy. | this commit |
| F5 | A descriptor-binding test compares recursive `projectionInput` keys/types with the generated manifest descriptor. | this commit |
| F6 | The integration plan now includes closeout evidence refresh, `verify.sh all --require`, one replacement closeout CI run, the merge-tree rule, the combined commit, and re-audit of A1/A3–A9. | this commit |
| F7 | Dependency wording is corrected: no project references MessagePack yet; the .NET package is added and the Python `msgpack` dev requirement is declared. | this commit |
| F8 | Bool/nil encoding and optional chiral-partner string-vs-nil are explicit and covered by tests. | this commit |
| F9 | Energy values, runtime settings, diagnostics, timings, cache state, file names, and display-only distances are explicitly excluded and tested. | this commit |
| F10 | `CanonicalProjectionWriter` is stated to be internal; no public API or ApiCompat change. | this commit |
| F11 | Golden bookkeeping now names ceria, MMC, vehicle, interstitialcy, and BaZrO₃ consistently. | this commit |
| F12 | The audit section now records `Fixes required (A1–A9)`. | this commit |
| N1 | The inherited `../audits/step-08.md` step-page link will be fixed in the resolution/status commit. | this commit |
| N2 | The `model-v1` rationale cites the pre-release `02-data-contracts.md` §3.3 precedent. | this commit |
| N3 | Step 11 remains the production Rust encoder/vector owner; Python is the independent pre-Step-11 check. | this commit |
| R2-1 | Replaced ambiguous `projectionInput` JSON with binary `tests/contracts/projections/<id>.bin` fixtures generated by the compiler and a descriptor-driven Python checker in `verify.sh contracts`; the fixture preserves uint64/int64/float64. | this commit |
| R2-2 | `StaticCorrection` is explicitly excluded; direction `pathLength` is excluded as display-only; outer movement length equals path node count; derived collection rule indices are excluded because rules carry collection identity. | this commit |
| R2-3 | Integer rule corrected: counts/indices/masks/state/tracker are `uint64`; spatial offsets, enums, jump tables/mappings, and canonical tuples are `int64`. | this commit |
| R2-4 | Quantization now explicitly covers every projected float64, with round-half-away-from-zero examples; parent §11.4/pipeline doc are updated in the amendment. | this commit |
| R2-5 | `pathLength` removed from the projection; only the integer distance bucket and signed path vector remain. | this commit |
| R2-6 | Golden set is consistently ceria/MMC/vehicle/interstitialcy/BaZrO₃ in C#, Python, and the manifest. | this commit |
| R2-7 | Amendment index is `Re-review required`; re-audit scope names A1, A3–A9 and m1–m7. | this commit |
| R2-8 | `msgpack` is declared in `tools/contracts/requirements.txt`; checker uses it for decoding and `struct.pack` for fixed-width headers; toolchain README is required. | this commit |
| R2-9 | GUID text is fixed to lowercase `Guid.ToString("D", InvariantCulture)`. | this commit |
| R2-10 | Fixture metadata uses `fixtureFormat: canonical-projection-v1`, distinct from model/inspection format versions. | this commit |
| R2-11 | ADR 0010/§3.3 pre-release `model-v1` exception is required in docs. | this commit |

| R3-1 | `StaticCorrection` is explicitly excluded in v1; movement outer length equals path node count and inner length is three; dropped/derived field rationale table added; `pathVector`/`pathLength` are excluded as display-only. | this commit |
| R3-2 | Per-field table is authoritative; jump tables, canonical tuples, distance buckets, interaction range, and pair/cluster site indices are `uint64`; signed `int64` is limited to cell offsets, enums, and the `-1` environment sentinel. | this commit |
| R3-3 | Language-neutral quantization operation added, midpoint/fractional vectors pinned, and parent §11.4/`compilation-pipeline.md` updated to all projected float64. | this commit |
| R3-4 | Authoritative store ownership and regeneration split defined; all five samples required across manifest, signature vectors, fixtures, and C# goldens. | this commit |
| R3-5 | `msgpack==1.2.4` requirements file, `verify.sh contracts` install step, README, and toolchain-row updates specified. | this commit |
| R3-6 | GUID-valued IDs and SHA-256 content-hash IDs are split; content hashes are used exactly as generated. | this commit |
| R3-7 | `ProjectionFixtureTests.WriteProjectionFixtures` and regeneration command named; `projectionSha256` defined; descriptor reuses `model-v1.json`; `ProjectionDescriptorTests` binding defined. | this commit |
| R3-8 | Writer primitive set fixed to `MessagePackWriter` buffer + `WriteRaw`/`Advance`; compact convenience methods are forbidden. | this commit |
| R3-9 | Python checker is qualified as a canonicality/type conformance verifier; schema review and Step 11 Rust encoder are the completeness gates. | this commit |
| R3-10 | ADR 0010 pre-release `model-v1` exception and §3.3 note added; `status: final` is not a release marker. | this commit |

## Design-review rounds

### Review round 1

#### Independence and frozen state

Fresh context: I did not author or implement Step 08 or this amendment and have no shared conversation with the
planner or implementor. I read the tree at the frozen commit
`db52abbe8f22bb2987797bd49cc2549137bad63a` on `step/08-model-compilation-pipeline`; the working tree was clean
and no amendment implementation exists. The request records `ca1582e...` as the reviewed commit; `db52abb` only
changes the request's `Reviewed commit` line, so the design content reviewed is `ca1582e` and the frozen tree is
`db52abb`. The parent audit is at `Fixes required` (A1–A9), and this amendment addresses only A2.

#### Commands and results

- `git rev-parse HEAD` — `db52abbe8f22bb2987797bd49cc2549137bad63a`.
- `git status --short` — clean.
- `git diff --stat ca1582e db52abb` — one file, the record's `Reviewed commit` line.
- `python tools/contracts/generate.py --check` — PASS, contract artifacts current (18 files).
- `python tools/check_docs_links.py` — FAIL on the inherited parent step-page link (m4); the amendment's links
  resolve. This is recorded as N1, not as a new amendment finding.
- `grep -rn "MessagePack" src/dotnet --include=*.csproj` — no matches; the package is only centrally pinned.
- Read the amendment page, parent step §11.3–§11.4, parent audit, ADR 0010, the manifest and generated
  `model-v1` descriptor, the generator, the data contracts, the current encoder and compiled records, the
  inspection JSON, the golden tests/vectors, the amendment/review processes, and the toolchain requirements.

#### Findings

One Blocker (F1: the nested schema is still deferred, so A2 is not closed at design level), five Majors
(F2: canonical integer/framing ambiguity; F3: missing float64 quantization rule; F4: Python vector input,
independence, dependency, and gate undefined; F5: no C#-to-descriptor conformance test; F6: closeout
CI/evidence integration missing), six Minors (F7–F12: dependency claims, nil/bool, exclusion enumeration,
public-API statement, golden-vector bookkeeping, audit scope/status), and three Notes (N1 inherited docs link,
N2 versioning precedent, N3 Rust vector ownership). The MessagePack package choice and the `model-v1`
retention are acceptable; the core defect is that the amendment promises the manifest-defined nested layout but
still leaves the exact field lists and byte rules to implementation.

#### Verdict

**Changes required.** Add the complete nested projection schema and canonical byte rules to the design, make
the Python vector independently specified and gated, add the C# descriptor-conformance test, and complete the
closeout CI/evidence integration checklist. The parent audit must then be re-run at the combined frozen commit
with A1/A3–A9 still in scope.

### Review round 2

#### Independence and frozen state

Fresh context: I did not author or implement Step 08 or this amendment and had no shared conversation with the
planner or implementor. I reviewed the round-2 delta at the frozen commit
`bf83509d0f569870d676c4dbaa4d527b0c79fb73` on `step/08-model-compilation-pipeline`; the working tree was clean.
The design content is `5646aec730114a9f1df0b104bf2280948641b965`: the freeze commit only changes the
request's `Reviewed commit` line (`git diff --stat 5646aec bf83509` = one file, one line), so the delta reviewed
is `db52abb..5646aec`. The parent audit is still `Fixes required` (A1–A9); the amendment addresses only A2.

The round-2 changes are real: the amendment now names every top-level and nested field with an order and a
type, replaces the ambiguous integer/float prose with an explicit marker table, adds the 12-decimal
quantization rule, adds `projectionInput` and a descriptor-binding test, adds four cross-language vectors, and
adds closeout evidence/CI/merge-tree steps. F1–F3, F6, and N1–N3 are substantially addressed. The remaining
material gap is the cross-language evidence chain: the Python checker's type source, fixtures, and gate are
still not defined, and the named descriptor test does not bind the hand-written byte encoder.

#### Commands and results

```text
$ git rev-parse HEAD
bf83509d0f569870d676c4dbaa4d527b0c79fb73

$ git status --short
(clean)

$ git log --oneline -3
bf83509 docs: freeze step 08 amendment re-review [skip ci]
5646aec docs: resolve step 08 amendment review findings [skip ci]
512d696 docs: record step 08 amendment 1 design review [skip ci]

$ git diff --stat 5646aec bf83509
 docs/planning/reviews/step-08-amend-1.md | 2 +-
 1 file changed, 1 insertion(+), 1 deletion(-)

$ git diff --stat db52abb bf83509
 .../step-08-amend-1-canonical-projection.md | 169 +++++++++++++++-----
 docs/planning/reviews/step-08-amend-1.md    | 200 +++++++++++++++++----
 docs/planning/steps/step-08-model-compilation-pipeline.md | 2 +-
 3 files changed, 294 insertions(+), 77 deletions(-)

$ python tools/contracts/generate.py --check
Contract artifacts are current (18 files)

$ python tools/check_docs_links.py
Documentation links OK (152 files scanned).

$ python tools/check_process_consistency.py
Process consistency: OK

$ python -c 'import msgpack; print(msgpack.version)'
(1, 2, 4)

$ ls tools/contracts/requirements.txt
ls: cannot access 'tools/contracts/requirements.txt': No such file or directory

$ grep -n "msgpack" docs/planning/06-toolchain-requirements.md
126:| `msgpack` 1.2.4 | Required | Step 22 | `.mcs` reader. |

$ grep -n "step-08-amend-1" docs/planning/amendments/README.md
113:| `step-08-amend-1` (`canonical-projection`) | 08 — Model Compilation Pipeline | **Planned** | ... |

$ python - <<'EOF'
import json
v = json.load(open('tests/contracts/signature-vectors.json'))
print([x['id'] for x in v['vectors']])          # ceria, vehicle5, interstitialcy5
m = json.load(open('contracts/manifest.json'))['signatures']['model']
print([(f['name'], f['type']) for f in m['fields']])  # nine flat fields, sections still `bin`
EOF
['model-ceria-kmc', 'model-vehicle5', 'model-interstitialcy5']
[('formatVersion', 'uint64'), ..., ('particles', 'bin'), ('structure', 'bin'), ('energy', 'bin'),
 ('transitions', 'bin'), ('simulations', 'bin')]
```

Serialization check of the proposed `projectionInput` scalar forms (scratch console app under
`/tmp/opencode/jsontest`, not committed):

```text
$ dotnet run
{"charge":2,"zero":-0,"volume":40,"x":5E-13}

$ python -c 'import json; print(type(json.loads("2")), type(json.loads("-0")))'
<class 'int'> <class 'int'>
```

`System.Text.Json` writes integer-valued `double`s without a fractional part (`2.0` → `2`) and `-0.0` as `-0`,
so a JSON-only reader cannot recover the declared `float64` type. This is the evidence for R2-1.

Files and sections read for round 2 in addition to the round-1 set: the complete delta `db52abb..5646aec` for
the amendment page, the step-page link line, and this record's resolution table; `docs/planning/amendments/README.md`
§3, §6, and the index; `docs/planning/reviews/README.md` §6–§7; `docs/planning/steps/step-08-model-compilation-pipeline.md`
§11.3–§11.4; `docs/development/compilation-pipeline.md` "Signatures"/"Caches and determinism";
`docs/planning/02-data-contracts.md` §1.3/§3.3; `docs/planning/adr/0010-canonical-messagepack-signatures.md`;
`contracts/manifest.json` `signatures.model` and `fixtures.signatureVectors`;
`contracts/generated/signatures/model-v1.json`; `tools/contracts/generate.py` (`validate_field`,
`build_artifacts`); `tools/contracts/README.md`; `tools/verify.sh` `run_contracts`;
`docs/planning/06-toolchain-requirements.md`; `src/dotnet/Mocassin.Compilation/Signatures/ModelSignatureProjection.cs`
and `CompiledModelJson.cs`; the compiled records (`CompiledStructureModel.cs`, `CompiledParticleTable.cs`,
`CompiledClusterTable.cs`, `CompiledTransitionModel.cs`, `CompiledSimulation.cs`, `CompiledModel.cs`);
`Energy/ClusterCompilationStage.cs`, `Transitions/TransitionCompilationStage.cs`, and
`Simulations/EncodingCompilationStage.cs`; `GoldenModelTests.cs`; `tests/contracts/signature-vectors.json`.

#### Round-1 finding status

| Finding | Status | Evidence and residual |
| --- | --- | --- |
| F1 (Blocker) | **Partially resolved** | Every nested section now has a named, ordered, typed field list (amendment lines 72–131); no `array<...>` placeholders remain. Residuals: `CompiledJumpRule.StaticCorrection` is neither signed nor declared excluded, and the `movement ... (3)` length notation is ambiguous; see R2-2. |
| F2 (Major) | **Resolved** | The encoding table fixes array `0xdd`, str `0xdb`, bin `0xc6`, uint64 `0xcf`, int64 `0xd3`, float64 `0xcb`, bool `0xc3`/`0xc2`, nil `0xc0`, and no maps in v1 (lines 139–149). This is unambiguous and matches ADR 0010 rules 2–6. |
| F3 (Major) | **Partially resolved** | 12-decimal `MidpointRounding.AwayFromZero`, `-0.0` normalization, and NaN/Inf rejection are stated for all float64 (lines 152–153). Residuals: the parent step §11.4 and `compilation-pipeline.md` still say only charge-transport vectors are quantized; no quantization/fractional vector is added; the C#-specific rounding rule needs a language-neutral definition; see R2-4. |
| F4 (Major) | **Not resolved** | `projectionInput` and four vectors are added, but the checker's scalar-type source, input fixtures, dependency file, and gate are not defined, and the round-1 statement that the checker reads the generated descriptor was removed; see R2-1. |
| F5 (Major) | **Partially resolved** | A descriptor-binding test is added, but it compares `projectionInput` (built by `CompiledModelJson`) with the descriptor, not the byte writer; the claim that it catches an encoder miss is only true through the un-gated Python vector; see R2-1. |
| F6 (Major) | **Resolved** | Integration now requires the closeout-evidence refresh, `./tools/verify.sh all --require`, one replacement closeout CI run, the merge-tree rule, the squash/combined commit, and the parent audit request update (lines 247–256). |
| F7 (Minor) | **Partially resolved** | The "already used" claim and public-API statement are corrected. Residual: `msgpack` is called an "existing" dependency although `tools/contracts/requirements.txt` does not exist, and `tools/contracts/README.md` and `06-toolchain-requirements.md` are not in the documentation plan; see R2-8. |
| F8 (Minor) | **Partially resolved** | Bool/nil markers are explicit and the chiral string-vs-nil test is added; `StaticCorrection` is the remaining optional value that is neither listed nor excluded; see R2-2. |
| F9 (Minor) | **Partially resolved** | Energy values, runtime settings, timings, diagnostics, cache state, file names, and `DistanceAngstrom` are excluded and tested. `direction.pathLength` is a signed raw distance while the text says only the integer bucket is signed; see R2-5. |
| F10 (Minor) | **Resolved** | `CanonicalProjectionWriter` is internal; no public API or ApiCompat change, and no MessagePack type appears in the public surface. |
| F11 (Minor) | **Partially resolved** | Four Python vectors and BaZrO₃ are named, but the C#/manifest/Python ownership is still inconsistent: the C# goldens do not pin interstitialcy, BaZrO₃ is not in the Python set, and the manifest fixture currently lists three vectors; see R2-6. |
| F12 (Minor) | **Partially resolved** | The amendment page status is `Re-review required` and the audit section records `Fixes required (A1–A9)`. The amendments index still says `Planned`, and the combined re-audit scope (A1, A3–A9, m1–m7) is not stated explicitly; see R2-7. |
| N1 (Note) | **Resolved** | The step-page link is now `../audits/step-08.md` and `python tools/check_docs_links.py` passes (152 files). |
| N2 (Note) | **Resolved** | The versioning rationale cites `02-data-contracts.md` §3.3 (lines 768–770), which records the pre-release additive-v1 precedent; no released `.msl` exists. See R2-11 for the remaining record-keeping note. |
| N3 (Note) | **Resolved** | Step 11 remains the production Rust encoder/vector owner; Python is the interim check. |

#### Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **R2-1 — The independent-vector input, type mapping, fixture lifecycle, gate, and byte-level descriptor binding are still undefined (F4/F5 residual).** `projectionInput` fixes the value tree, but the round-2 text no longer says the Python checker reads the generated descriptor. JSON does not carry the declared scalar type: `System.Text.Json` writes `2.0` as `2` and `-0.0` as `-0` (verified), so integer-valued `float64` fields (`charge`, `volume`) are indistinguishable from `uint64`/`int64`; the existing inspection JSON also uses display forms (for example the string `stability`), so the design must say `projectionInput` uses the projection scalar forms, not the inspection forms. Without a declared type source a second encoder cannot choose `0xcf`/`0xd3`/`0xcb`. In addition: no path or regeneration rule is defined for the four checked-in `projectionInput` fixtures or their expected signatures; `tools/contracts/requirements.txt` does not exist; and `verify.sh contracts` runs only `generate.py --check`, process consistency, licence headers, secrets, and cargo-deny, so the Python vector is not gated. The descriptor test itself serializes `projectionInput` (produced by `CompiledModelJson`, not `CanonicalProjectionWriter`) and compares keys/types, so a manifest edit applied to the JSON but missed in the byte writer still passes; the stated guarantee is true only if the Python vector runs. Add: the checker loads `contracts/generated/signatures/model-v1.json` and coerces each `projectionInput` value to the declared scalar type; the four fixture files and how they are generated/refreshed; the requirements file and the `contracts` gate entry; and a byte-level encoder-conformance test (or generated order/type table) that checks the encoder itself. | Amendment lines 160–172; `CompiledModelJson.cs` `charge`/`volume`/`stability` writes; `ModelSignatureProjection.cs`; `tools/verify.sh:151-163`; `ls tools/contracts/requirements.txt`; serialization check above. |
| Minor | **R2-2 — The nested schema still has omissions and length ambiguity (F1/F8 residual).** `CompiledJumpRule.StaticCorrection` (`Cartesian3?`, documented `null` in v1) is neither in the `rule` tuple nor named in the exclusion prose. `movement array<array<float64>> (3)` is ambiguous: the outer array has one entry per path node (the node count varies), while each inner vector has three components. `collection.ruleIndices`, `rule.directionIndex`, `clusterTable.index`, `pairGeometry.centerSiteId`/`partnerSiteId`, and `simulation.intent` are dropped without the include/exclude rationale F1 requested (they are mostly redundant/derivable, but the design should say so). State the `StaticCorrection` rule (nil in v1, excluded until a versioned change), write the movement length rule, and add a one-line rationale for each redundant field. | Amendment lines 103–125; `Transitions/CompiledTransitionModel.cs` `CompiledJumpRule` (`StaticCorrection`, `FrequencyHz`); `CompiledClusterTable.Index`; `CompiledJumpCollection.RuleIndices`; `CompiledJumpDirection.Movement`; `EncodingCompilationStage.cs:245-254` (`Intent.Kind` duplicates `Kind`); `TransitionCompilationStage.cs:180-191`. |
| Minor | **R2-3 — The general scalar-type rule contradicts the explicit field table.** "Every count and non-negative index is `uint64`" (line 151) conflicts with `jumpCounts`/`jumpOffsets`/`jumpMappings array<int64>` (lines 112–113), `particleTableMapping array<int64>` (line 105), `canonicalTuple array<int64>` (line 106), and `distanceBucketFm int64` (a non-negative bucket). A second language cannot tell which rule wins. State that the per-field table is authoritative and scope the sentence (for example, "signed where the source value can be negative or where the `int` model is retained"), or make the non-negative fields `uint64`. | Amendment lines 105–113, 151. |
| Minor | **R2-4 — The quantization rule is broader than the parent design and has no test or language-neutral definition (F3 residual).** The amendment quantizes every `float64`; parent step §11.4 and `compilation-pipeline.md` say only charge-transport vectors are quantized to 12 decimals, so the two normative pages will describe different bytes unless the parent pages are updated. The test table has no rounding/fractional vector (F3 asked for one). `MidpointRounding.AwayFromZero` is stated in C# terms; Python/Rust must perform decimal rounding on the same representation to match at midpoints (`round()`/binary formatting is not equivalent). State the scope, align §11.4/`compilation-pipeline.md`, define the decimal operation, and add a non-trivial fractional and midpoint vector. | Amendment lines 152–153, 215; parent step §11.4 (lines 631–634); `compilation-pipeline.md:209`; `ModelSignatureProjection.cs:380-390`. |
| Minor | **R2-5 — `direction.pathLength` contradicts the "only the integer distance bucket is signed" rule (F9 residual).** `pathLength` is a raw Cartesian distance in ångström, yet the amendment includes it and says no display-only distances are encoded and "Only the integer distance bucket is signed". Parent step §11.4 says the signature records buckets and indices, not raw computed distances. Either exclude `pathLength` (it is derived from the signed `pathVector`) or state why it is canonical geometry and qualify the sentence; also enumerate the excluded display-only fields (`Name`, `Cartesian`, `DistanceAngstrom`, runtime settings) so the rule is reviewable. | Amendment lines 117–119, 157–158; parent step §11.4 (lines 632–634); `ModelSignatureProjection.cs:277`; `CompiledJumpDirection.PathLength`. |
| Minor | **R2-6 — Golden-vector bookkeeping is still inconsistent (F11 residual).** The amendment says "the C# golden tests pin the same set [ceria, MMC, vehicle, interstitialcy] plus BaZrO₃", but `GoldenModelTests` pins ceria, vehicle, ceria-MMC, and BaZrO₃ and does not pin interstitialcy; the interstitialcy signature exists only in the manifest-generated `tests/contracts/signature-vectors.json` (three vectors today). The test table also omits BaZrO₃. State which vectors are manifest-declared/regenerated and which are C#-test-only, and reconcile the BaZrO₃/interstitialcy sets. | Amendment lines 170–171, 217–221; `GoldenModelTests.cs:16-30,39-50,53-55`; `tests/contracts/signature-vectors.json`; `contracts/manifest.json` `fixtures.signatureVectors`. |
| Minor | **R2-7 — Status and combined-audit scope bookkeeping (F12 residual).** The amendment page is `Re-review required`, but the index row in `docs/planning/amendments/README.md:113` still says `Planned`. The Audit section records `Fixes required (A1–A9)` but does not state that the combined re-audit scope explicitly includes A1, A3–A9, and m1–m7 (the parent audit's verdict covers the whole step). Advance the index status and state the combined scope. | Amendment lines 258–263; `docs/planning/amendments/README.md:113`; parent audit findings A1–A9, m1–m7. |
| Minor | **R2-8 — Dependency wording and documentation are still inaccurate (F7 residual).** The amendment calls `msgpack` the "existing" development dependency, but no `tools/contracts/requirements.txt` exists and the toolchain lists `msgpack` only for Step 22. `tools/contracts/README.md` still says the generator uses the standard library only, and neither it nor `06-toolchain-requirements.md` is in the documentation checklist. Also, msgpack-python always writes minimal-width arrays/strings/integers, so it cannot emit the specified `0xdd`/`0xdb`/`0xcf`/`0xd3` headers; the checker must hand-write those headers (msgpack only supplies the float64 marker and UTF-8 handling) or the design should say why the dependency is needed. | Amendment lines 47–48, 169–170, 224–229; `ls tools/contracts/requirements.txt`; `06-toolchain-requirements.md:126`; `tools/contracts/README.md`; msgpack-python `Packer` behavior. |
| Note | **R2-9 — The canonical text form of `id`/`transitionId`/`objectId` strings is not stated.** The current encoder uses `Guid.ToString("D", InvariantCulture)` (lowercase, hyphenated); a Rust encoder must reproduce the same text. `projectionInput` hides this for the Python check, so specify the rendering (for example lowercase `D` format, invariant culture). | `ModelSignatureProjection.cs:94,122,125,157,197,248,333`; amendment `id string` fields. |
| Note | **R2-10 — `projectionInput.formatVersion` is ambiguous.** `compiled-inspection-v1` already has its own `formatVersion`; the projection field must be `CompiledModel.FormatVersion`, not the inspection format version. State this explicitly (round 1 F4 named the collision). | `CompiledModelJson.cs` `FormatVersion`; `CompiledModel.FormatVersion`; amendment line 162. |
| Note | **R2-11 — Record the pre-release `model-v1` exception.** The retention is justified by the §3.3 pre-release precedent and ADR 0010's bump rule applies to released signed types. Add a sentence to ADR 0010/§3.3 that this redefinition is the pre-release exception, and note that `signatures.model.status = "final"` is not a release marker. | `contracts/manifest.json` `signatures.model.status`; ADR 0010 "Consequences"; data contracts §3.3. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**.

#### Verdict

**Changes required.** The round-2 design substantially closes F1–F3, F6, and N1–N3 and the encoding table is now
unambiguous, but the central cross-language evidence is still not implementable as written: `projectionInput`
has no declared scalar-type source, fixture lifecycle, or gate, and the descriptor test does not bind the byte
encoder (R2-1, Major). The remaining Minors (R2-2–R2-8) should be corrected in the same pass. After those
changes, request a fresh re-review at a new frozen commit; the parent audit must then be re-run at the combined
commit with A1, A3–A9, and m1–m7 still in scope.

### Review round 3

#### Independence and frozen state

Fresh context: I did not author or implement Step 08 or this amendment and have no shared conversation with the
planner or implementor. I reviewed the round-3 delta at the frozen tree
`a51344fe0ac72c4bb68f3a236c9bd1d431c4f483` on `step/08-model-compilation-pipeline`; the working tree was clean.
The design content is `ebbdd708f09c8d0dba9dc0a6c62374efb2ec1ba0`: `git diff --stat ebbdd70 a51344f` is the
single-line `Reviewed commit` metadata change in this record, so the delta reviewed is `bf83509..ebbdd70`.
The parent audit is still `Fixes required` (A1–A9); the amendment addresses only A2.

Round 3 replaces the round-2 `projectionInput` JSON with tracked binary projection fixtures and a
descriptor-driven Python checker in `verify.sh contracts`. That is the right direction: the `.bin` fixture is
the exact canonical byte stream, so the MessagePack marker width survives, and the descriptor supplies the
`uint64`/`int64`/`float64` type that JSON lost. I verified the reason the descriptor is required: `msgpack`
decodes `0xcf` (uint64 2) and `0xd3` (int64 2) to the same Python `int` and re-packs both as the minimal
`0x02`, so a decode-only checker cannot recover the declared width. The checker as specified (decode, walk the
descriptor, re-encode with `struct.pack`, compare bytes, recompute the signature) is a sound drift gate, but it
is a canonicality/type verifier over C# bytes, not an independent model-to-projection encoder (R3-9).

The encoding table, the GUID rendering, and the fixture-format version are now explicit, and the parent audit
scope is stated. The remaining defects are documentation-consistency gaps rather than byte-encoding
ambiguities: R2-2 and R2-3 are not actually resolved (the resolution table overstates them), R2-4/R2-6/R2-8/
R2-11 are partial, and the parent quantization pages still describe the old charge-transport-only scope.

#### Commands and results

```text
$ git rev-parse HEAD
a51344fe0ac72c4bb68f3a236c9bd1d431c4f483

$ git status --short
(clean)

$ git branch --show-current
step/08-model-compilation-pipeline

$ git log --oneline -4
a51344f docs: freeze step 08 amendment round 3 review [skip ci]
ebbdd70 docs: resolve step 08 amendment round 2 findings [skip ci]
31361fe docs: record step 08 amendment 1 re-review [skip ci]
bf83509 docs: freeze step 08 amendment re-review [skip ci]

$ git diff --stat ebbdd70 a51344f
 docs/planning/reviews/step-08-amend-1.md | 2 +-
 1 file changed, 1 insertion(+), 1 deletion(-)

$ git diff --stat bf83509 ebbdd70
 docs/planning/amendments/README.md                 |  2 +-
 .../step-08-amend-1-canonical-projection.md        | 80 +++++++++++++++-------
 docs/planning/reviews/step-08-amend-1.md           | 11 +++
 3 files changed, 218 insertions(+), 25 deletions(-)

$ python tools/contracts/generate.py --check
Contract artifacts are current (18 files)

$ python tools/check_docs_links.py
Documentation links OK (152 files scanned).

$ python tools/check_process_consistency.py
Process consistency: OK

$ python -c 'import msgpack; print(msgpack.version)'
(1, 2, 4)

$ ls tools/contracts/requirements.txt
ls: cannot access 'tools/contracts/requirements.txt': No such file or directory

$ grep -n "msgpack" docs/planning/06-toolchain-requirements.md
126:| `msgpack` 1.2.4 | Required | Step 22 | `.mcs` reader. |

$ grep -n "StaticCorrection" docs/planning/amendments/step-08-amend-1-canonical-projection.md
(no matches)

$ grep -n "quantiz" docs/planning/steps/step-08-model-compilation-pipeline.md docs/development/compilation-pipeline.md
.../step-08-model-compilation-pipeline.md:633:- Charge transport vectors are computed with deterministic scalar arithmetic and quantized to 12 decimal
docs/development/compilation-pipeline.md:209:  floating-point reductions. Charge transport vectors are quantized before persistence/signing.

$ python - <<'EOF'
import msgpack, struct
for name, b in [('uint64 2', b'\xcf' + struct.pack('>Q', 2)),
                ('int64 2', b'\xd3' + struct.pack('>q', 2)),
                ('float64 2.0', b'\xcb' + struct.pack('>d', 2.0))]:
    v = msgpack.unpackb(b)
    print(name, '->', repr(v), type(v).__name__, 'msgpack repack:', msgpack.packb(v).hex())
EOF
uint64 2 -> 2 int msgpack repack: 02
int64 2 -> 2 int msgpack repack: 02
float64 2.0 -> 2.0 float msgpack repack: cb4000000000000000

$ cd /tmp/opencode/round3 && dotnet run   # scratch console app, not committed
5E-13 -> 1E-12
-5E-13 -> -1E-12
-4E-13 -> -0
-0 -> -0
0.0012345678901235 -> 0.00123456789
0.30000000000000004 -> 0.3

$ python -c 'print(round(5e-13, 12))'
0.0
```

Files and sections read for round 3 in addition to the round-1/round-2 set: the full delta
`bf83509..ebbdd70`; the amendment page; `docs/planning/amendments/README.md` §3 and §8; the parent step page
§8.4 and §11.3–§11.4; `docs/development/compilation-pipeline.md` "Signatures" and "Caches and determinism";
`docs/planning/adr/0010-canonical-messagepack-signatures.md`; `docs/planning/02-data-contracts.md` §1.3/§3.3;
`contracts/manifest.json` `signatures.model` and `fixtures.signatureVectors`;
`contracts/generated/signatures/model-v1.json`; `tools/contracts/generate.py`; `tools/contracts/README.md`;
`tools/verify.sh` `run_contracts`; `docs/planning/06-toolchain-requirements.md`;
`src/dotnet/Mocassin.Compilation/Signatures/ModelSignatureProjection.cs`; the compiled records
(`CompiledStructureModel.cs`, `CompiledEnergyModel.cs`, `CompiledClusterTable.cs`,
`CompiledTransitionModel.cs`, `CompiledSimulation.cs`); `GoldenModelTests.cs`;
`tests/contracts/signature-vectors.json`; the sample list; and the scratch C# console under
`/tmp/opencode/round3` (not committed).

#### Round-2 finding status

| Finding | Status | Evidence and residual |
| --- | --- | --- |
| R2-1 (Major) | **Resolved** | The binary `.bin` fixtures preserve the exact bytes and the descriptor restores the scalar type; the checker and `verify.sh contracts` gate are defined. Residuals are fixture lifecycle (R3-7), C# header widths (R3-8), and the independence claim (R3-9). |
| R2-2 (Minor) | **Not resolved** | `StaticCorrection` is still absent from the `rule` tuple and the exclusion prose; `movement ... (3)` is unchanged; the dropped-field rationale is still missing; see R3-1. The resolution table says all three are fixed. |
| R2-3 (Minor) | **Not resolved** | The rule still says counts are `uint64` and jump count/offset/mapping values are `int64`; several dense indices are `int64` without the sentinel rationale; ADR 0010 rule 3 is not reconciled; see R3-2. |
| R2-4 (Minor) | **Partially resolved** | All-float scope and midpoint examples are added, but the parent §11.4/pipeline pages still say charge-transport-only, no midpoint/fractional vector exists, and the operation is not defined; see R3-3. |
| R2-5 (Minor) | **Resolved** | `pathLength` is removed from `direction`; the exclusion prose names it; the integer bucket and signed path vector remain. `pathVector` itself is noted in R3-1. |
| R2-6 (Minor) | **Partially resolved** | Five sample names are consistent in the amendment, but the manifest vector list still has three, two golden stores remain, and the C# interstitialcy pin is not defined; see R3-4. |
| R2-7 (Minor) | **Resolved** | The index row is `Re-review required` and the combined re-audit scope names A1, A3–A9, and m1–m7. |
| R2-8 (Minor) | **Partially resolved** | `tools/contracts/requirements.txt` and `verify.sh contracts` are named, but the install step, the README/toolchain updates, and the "existing" wording are unresolved; see R3-5. |
| R2-9 (Note) | **Resolved** | Lowercase `D`, invariant culture, is stated; the wording overgeneralizes to SHA-256 content IDs (R3-6). |
| R2-10 (Note) | **Resolved** | `fixtureFormat: "canonical-projection-v1"` is distinct from the model and inspection format versions. |
| R2-11 (Note) | **Partially resolved** | The versioning section cites `02-data-contracts.md` §3.3, but the ADR/§3.3 exception sentence and the `status = "final"` note are not tracked; see R3-10. |

#### Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| Minor | **R3-1 — R2-2 is still open: the excluded/derived-field rules are inconsistent.** `CompiledJumpRule.StaticCorrection` (`Cartesian3?`, documented `null` in v1) is neither in the `rule` tuple nor named in the exclusion prose; the resolution table says it is explicitly excluded. `movement array<array<float64>> (3)` is still ambiguous: the outer array is one entry per path node and each inner vector has three components. The dropped fields (`collection.ruleIndices`, `rule.directionIndex`, `clusterTable.index`, pair `CenterSiteId`/`PartnerSiteId`, `simulation.intent`) still have no include/exclude rationale. `direction.pathVector` remains signed although §8.4 calls it display/tracking only and it is derivable from `movement` plus the cell vectors. State the `StaticCorrection` rule (absent in v1; a versioned field when the S2 correction lands), write the movement lengths, add one-line rationales, and justify or drop `pathVector`. | Amendment lines 108–125, 158–163; `CompiledTransitionModel.cs` `CompiledJumpRule`/`CompiledJumpDirection`; parent step §8.4 line 428; `grep StaticCorrection` in the amendment returns no matches; resolutions table lines 221–222. |
| Minor | **R3-2 — The integer/float type rule is still self-contradictory and conflicts with ADR 0010 rule 3.** “Counts, dense indices, … are `uint64`” is followed by “jump count/offset/mapping values … are `int64`”; `jumpCounts` is a count and `jumpOffsets` are indices. `distanceBucketFm` fits neither category (and `interactionRange` only if “spatial cell components” is read broadly). `site.environmentIndex` and `position.environmentIndex` are dense indices encoded `int64`, while `environment.siteIndex` can be `-1` (the only clear sentinel) and is not explained as such; `pairGeometry.centerSiteIndex`/`partnerSiteIndex` are `uint64` for the same concept. ADR 0010 rule 3 says non-negative counts and indices use `uint64`. Add “the per-field table is authoritative”, scope the general sentence (signed for sentinels/source-`int` retention), and either amend ADR 0010 or make the non-negative fields `uint64`. | Amendment lines 111–113, 151–153; `CompiledStructureModel.cs:35` (`SiteIndex` `-1`), lines 21–30, 81–92; `CompiledClusterTable.cs:39–50`; `CompiledEnergyModel.cs:26–40`; ADR 0010 line 32. |
| Minor | **R3-3 — The quantization rule is not consistently reflected in the parent docs and is not adequately testable.** The amendment quantizes every projected `float64`, but parent §11.4 (line 633) and `compilation-pipeline.md` (line 209) still say only charge-transport vectors are quantized, and the amendment’s present-tense “the parent page/pipeline doc are updated in the amendment” is false at `ebbdd70`; the Documentation checklist does not include the alignment. The test table has no fractional/midpoint vector. The rule does not state the scaling operation: C# `Math.Round(5e-13, 12, AwayFromZero)` returns `1E-12` while Python `round(5e-13, 12)` returns `0.0`, so the example pins the result but not the operation. Add the parent-page updates, a language-neutral operation (for example multiply by `10^12`, round half away from zero on the binary64 product, divide), and a synthetic midpoint/fractional vector. | Amendment lines 154–157; parent step lines 631–634; `compilation-pipeline.md:209`; test table lines 245–246; scratch `dotnet run`/Python output above. |
| Minor | **R3-4 — Golden-vector bookkeeping is still split between two stores.** The design names five projection fixtures (ceria KMC, ceria MMC, vehicle5, interstitialcy5, BaZrO₃), but `contracts/manifest.json` `fixtures.signatureVectors` still lists three (`model-ceria-kmc`, `model-vehicle5`, `model-interstitialcy5`), and `tests/contracts/signature-vectors.json` is still the three-vector generated scaffold. `GoldenModelTests` pins ceria/vehicle/MMC/BaZrO₃ but not interstitialcy. The design does not say how the manifest vector list is refreshed to five, which store is authoritative, or how the `.json` fixture metadata relates to `signature-vectors.json`; both carry `signatureHex`. State the ownership/regeneration split, update the manifest fixture list, and pin interstitialcy in the C# goldens. | Amendment lines 198, 249, 253; `contracts/manifest.json` `fixtures.signatureVectors`; `tests/contracts/signature-vectors.json`; `GoldenModelTests.cs:19–70`; samples list. |
| Minor | **R3-5 — The `msgpack` requirements/gate lifecycle is incomplete.** `tools/contracts/requirements.txt` is named but does not exist; `tools/verify.sh` `run_contracts` installs only `tools/symmetry/requirements.txt`, so the new checker depends on an ambient `msgpack` install. `tools/contracts/README.md` still says the generator uses the standard library only and is not in the Documentation checklist; `06-toolchain-requirements.md:126` still lists `msgpack` for Step 22. The Non-goals still call it the “existing” development dependency. Add the requirements install (or an explicit environment check), the README/toolchain updates, and correct the wording. | Amendment lines 47–48, 191, 197, 256–261; `tools/verify.sh:151–163`; `tools/contracts/README.md:20`; `06-toolchain-requirements.md:126`. |
| Minor | **R3-6 — The GUID text rule overgeneralizes to content-hash identifiers.** “GUID/identifier strings are encoded as lowercase `Guid.ToString("D", …)`” covers every `id` field, but `GeometryId`, `EnergyId`, `ClusterEnergyId`, `ObjectId`, and `RuleGeometryId` are SHA-256 hex strings, not GUIDs, and the current encoder applies `"D"` only to typed GUID values (`CellSiteId`, `ParticleSetId`, `ParticleId`, `ClusterId`, `TransitionId`, `SimulationId`). State the split: GUID-valued IDs use lowercase `D`; content-hash IDs are used exactly as generated. | Amendment lines 201–203; `ModelSignatureProjection.cs:94, 122, 125, 157, 197, 248, 333`; `CompiledEnergyModel.cs` `GeometryId`/`EnergyId`; `Transitions` `ObjectId`/`RuleGeometryId`. |
| Minor | **R3-7 — The fixture/descriptor lifecycle is not fully defined.** The “golden fixture generator” is unnamed and has no invocation/regeneration command; `projectionSha256` is not defined (raw projection, `.bin`, or something else); the “Descriptor binding” test compares against an undefined “encoder’s projection tree” now that `projectionInput` is gone; and `contracts/generated/signatures/model-v1.projection.json` duplicates `model-v1.json`, which is generated from the same manifest `fields` array through `generate.py`’s `build_artifacts` and will carry the same nested tree. Name the generator, define the hash, define the C# test mechanism (or rely on the Python byte comparison), and reuse `model-v1.json` or state how the projection descriptor differs. | Amendment lines 170–199, 245; `tools/contracts/generate.py:525–538`; `contracts/generated/signatures/model-v1.json`. |
| Minor | **R3-8 — The C# MessagePack framing statement hides a canonical-width trap.** `MessagePackWriter.WriteArrayHeader` and `WriteString` choose the most compact form (fix/array16/str8/str16), not the required `0xdd` and `0xdb`; the writer must hand-write those headers (for example with `WriteRaw`) and use `WriteUInt64`/`WriteInt64`/`WriteDouble` for the scalars. The design says the writer “uses the … package for MessagePack framing”, which invites the convenience methods and a non-canonical encoder. State the exact primitive set, as the Python side already does with `struct.pack`. | Amendment lines 135–149, 186–194; MessagePack-CSharp `MessagePackWriter` primitive API (`WriteArrayHeader`/`WriteString` compact form, `WriteRaw` for forced bytes). |
| Note | **R3-9 — The Python checker is a conformance verifier, not an independent encoder.** It decodes C#-produced bytes and re-encodes them from the descriptor, so it catches marker-width, order, nil/bool, quantization, and signature drift, but a shared manifest/encoder misreading of the model-to-projection mapping is invisible. The design and approval summary should qualify “independent” and keep the manifest schema review as the completeness gate. This is not a defect in the fixture choice. | Amendment lines 186–199, 235; ADR 0010 Tooling (“Python may verify signatures but is not required to write them”). |
| Note | **R3-10 — The `model-v1` pre-release exception is still not recorded in its normative homes.** The versioning section cites `02-data-contracts.md` §3.3 and states no `.msl` has been released, but the Documentation checklist only asks ADR 0010 to “note the shared encoder and descriptor”. ADR 0010 line 69 still says a signed-type field addition requires a format version bump, and the requested note that `signatures.model.status = "final"` is not a release marker is absent. Add the exception sentence to ADR 0010/§3.3 or state why the ADR is intentionally unchanged. | Amendment lines 205–210, 256–261; ADR 0010 line 69; `02-data-contracts.md:768–770`; `contracts/manifest.json` `signatures.model.status`. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**.

#### Verdict

**Ready with notes.** The round-3 delta closes the round-2 Major. The checked-in `.bin` fixtures are the exact
canonical projection, the descriptor restores the `uint64`/`int64`/`float64` type that JSON could not carry,
and `verify.sh contracts` gates the byte comparison and signature recomputation; `uint64`, `int64`, and
`float64` are preserved by construction. The checker is a canonicality/type conformance gate, not an
independent model-to-projection encoder (R3-9), which should be stated in the approval summary.

No Blocker or Major remains, but the design should not be implemented unchanged: R2-2 and R2-3 are still open
(R3-1, R3-2), R2-4/R2-6/R2-8/R2-11 are partial (R3-3, R3-4, R3-5, R3-10), and the new fixture chain needs the
lifecycle and framing details in R3-7/R3-8. These are all Minor/Note findings; apply them to the design docs
and the implementation plan and correct the R2-2/R2-3 resolution entries, which currently claim fixes that the
amendment page does not contain. They are mandatory corrections, not waivers.

The top-level `Record state` and round-1 `Verdict` above are the round-1 snapshot and are preserved unaltered;
this round's verdict is authoritative. The amendment request metadata (`Status` “Re-review required (round 2)”,
`Design review` “round 1 … re-review requested”) should be advanced to round 3 by the step agent.

Follow-up owner: Step 08 implementor / planner
Follow-up issue/step: Apply R3-1–R3-8 and note R3-10 to
`docs/planning/amendments/step-08-amend-1-canonical-projection.md` (and the parent §11.4/pipeline pages), add the
`msgpack` requirements/gate details, and request a fresh re-review at the new frozen commit if the owner
requires the Minor corrections to be re-verified. The parent audit still has A1/A3–A9 open and must be re-run at
the combined commit.

### Review round 4

#### Independence and frozen state

Fresh context: I did not author or implement Step 08 or this amendment and have no shared conversation with the
planner or implementor. I reviewed the round-4 delta at the frozen tree
`26a17ced7fe014614831e45c63294924364f4192` on `step/08-model-compilation-pipeline`; the working tree was clean.
The design content is `4036100f8fae658ce4fc11587aa88eb454aa84d9`: `git diff --stat 4036100 26a17ce` is the
single-line `Reviewed commit` metadata change in this record, so the design delta reviewed is `e5d18a0..4036100`
(and `a51344f..4036100` overall). The parent audit is still `Fixes required` (A1–A9); the amendment addresses
only A2.

Round 4 closes most of the round-3 notes and, importantly, moves the quantisation and versioning rules into
their normative parent pages: `docs/development/compilation-pipeline.md` and Step 08 §11.4 now say that all
projected float64 values are quantised with round-half-away-from-zero on the binary64 product, and ADR 0010
gained a "Pre-release model layout finalization" section plus a `02-data-contracts.md` §3.3 note recording that
`status = "final"` is not a release marker. The encoding table, GUID vs content-hash split, `MessagePackWriter`
primitive set, fixture generator/command, `projectionSha256`, and the five-sample acceptance are now explicit.
What remains is a small set of internal contradictions in the type table and the descriptor path plus
bookkeeping. There is no Blocker or Major, and the bytes are implementable from the authoritative per-field
table.

#### Commands and results

```text
$ git rev-parse HEAD
26a17ced7fe014614831e45c63294924364f4192

$ git status --short
(clean)

$ git branch --show-current
step/08-model-compilation-pipeline

$ git log --oneline -5
26a17ce docs: freeze step 08 amendment round 4 review [skip ci]
4036100 docs: clear step 08 amendment round 3 notes [skip ci]
e5d18a0 docs: record step 08 amendment ready-with-notes status [skip ci]
6cec29d docs: record step 08 amendment 1 round 3 review [skip ci]
a51344f docs: freeze step 08 amendment round 3 review [skip ci]

$ git diff --stat 4036100 26a17ce
 docs/planning/reviews/step-08-amend-1.md | 2 +-
 1 file changed, 1 insertion(+), 1 deletion(-)

$ git diff --stat e5d18a0 4036100
 docs/development/compilation-pipeline.md           |   3 +-
 docs/planning/02-data-contracts.md                 |   2 +
 .../adr/0010-canonical-messagepack-signatures.md   |   8 ++
 docs/planning/amendments/README.md                 |   2 +-
 .../step-08-amend-1-canonical-projection.md        | 106 +++++++++++++++------
 docs/planning/reviews/step-08-amend-1.md           |  11 +++
 .../steps/step-08-model-compilation-pipeline.md    |   4 +-
 7 files changed, 102 insertions(+), 34 deletions(-)

$ python tools/contracts/generate.py --check
Contract artifacts are current (18 files)

$ python tools/check_docs_links.py
Documentation links OK (152 files scanned).

$ python tools/check_process_consistency.py
Process consistency: OK

$ python -c 'import msgpack; print(msgpack.version)'
(1, 2, 4)

$ ls tools/contracts/requirements.txt
ls: cannot access 'tools/contracts/requirements.txt': No such file or directory
```

`tools/contracts/requirements.txt` and the `verify.sh` install step are still pending implementation, which is
consistent with this being a design-only commit.

Quantisation rule check (round-3 residual R3-3). I re-implemented the specified language-neutral operation
(`scaled = value * 1e12`; `floor(scaled + 0.5)` when `scaled >= 0`, else `ceil(scaled - 0.5)`;
`result = rounded / 1e12`; zero normalised to `+0.0`) and it reproduces every pinned example:

```text
5e-13                 -> 1e-12      (scaled = 0.5)
-5e-13                -> -1e-12
-4e-13                -> 0.0
0.30000000000000004   -> 0.3
-0.0                  -> 0.0
0.0012345678901235    -> 0.00123456789
python round(5e-13, 12) = 0.0        (confirms "not the language round function")
```

Contract and descriptor inspection:

```text
$ python - <<'EOF'
import json
m = json.load(open('contracts/manifest.json'))
print([(f['name'], f['type']) for f in m['signatures']['model']['fields']])
print([v['id'] for v in m['fixtures']['signatureVectors']['vectors']])
print([v['id'] for v in json.load(open('tests/contracts/signature-vectors.json'))['vectors']])
EOF
[('formatVersion', 'uint64'), ..., ('particles', 'bin'), ('structure', 'bin'), ('energy', 'bin'),
 ('transitions', 'bin'), ('simulations', 'bin')]        # projection sections still `bin` at this commit
['model-ceria-kmc', 'model-vehicle5', 'model-interstitialcy5']
['model-ceria-kmc', 'model-vehicle5', 'model-interstitialcy5']
```

`tools/contracts/generate.py` (`build_artifacts`) writes only `contracts/generated/signatures/<id>-v1.json`,
copying `descriptor["fields"]` verbatim, so a nested `model-v1.json` descriptor is expressible; `validate_field`
supports `object` fields, `array` `itemFields`, and nested `itemItems`. `msgpack` cannot emit the fixed-width
headers, which is why the checker must re-encode with `struct.pack` (round-3 evidence still holds).

MessagePack 3.1.12 package check (scratch download under `/tmp/opencode`, not committed). The pinned
`lib/net9.0/MessagePack.xml` and `MessagePackWriter.cs` expose `WriteRaw(ReadOnlySpan<byte>)`,
`WriteRaw(in ReadOnlySequence<byte>)`, `Advance(int)`, and `Flush()`, so the specified primitive set is
implementable. `WriteUInt64`/`WriteInt64` are typed writers (their XML summaries name
`MessagePackCode.Int32`/`MessagePackCode.Int64`), while minimal-width behaviour belongs to
`Write(ulong)`/`Write(long)`; `Write(double)` emits `MessagePackCode.Float64` and there is no `WriteDouble`
entry. The `WriteRaw`-only rule is therefore correct; see N4-2 on the stated reason.

Files and sections read for round 4 in addition to the round-1/round-2/round-3 sets: the full delta
`a51344f..4036100` and `e5d18a0..4036100`; the amendment page; parent Step 08 §8.4 and §11.3–§11.4; ADR 0010
"Projection rules", "Pre-release model layout finalization", and "Tooling"; `02-data-contracts.md` §1.3/§3.3;
`docs/development/compilation-pipeline.md` "Signatures"/"Caches and determinism"; `contracts/manifest.json`
`signatures.model` and `fixtures.signatureVectors`; `contracts/generated/signatures/model-v1.json`;
`tests/contracts/signature-vectors.json`; `tools/contracts/generate.py` (`VALID_FIELD_TYPES`, `validate_field`,
`build_artifacts`); `tools/contracts/README.md`; `tools/verify.sh` `run_contracts`;
`docs/planning/06-toolchain-requirements.md`; the compiled records `CompiledStructureModel.cs`,
`CompiledEnergyModel.cs`, `CompiledClusterTable.cs`, `CompiledTransitionModel.cs`,
`TransitionCompilationDtos.cs`, `TransitionCompilationStage.cs`; `GoldenModelTests.cs`; the five sample
projects (`samples/simulations/ceria-kmc.mocprj`, `samples/simulations/ceria-mmc.mocprj`,
`samples/transitions/vehicle5.mocprj`, `samples/transitions/interstitialcy5.mocprj`,
`samples/barium-zirconate/barium-zirconate.mocprj`); and the MessagePack 3.1.12 package XML/source above.

#### Round-3 finding status

| Finding | Status | Evidence and residual |
| --- | --- | --- |
| R3-1 (Minor) | **Resolved** | `StaticCorrection` is in the dropped/derived table as "excluded in v1" (line 139) and in the encoding prose with the Step 15 dynamic-path rationale (lines 183–184); `movement` lengths are stated for both directions and rules (lines 117, 120–121); the dropped-field rationale table covers `collection.ruleIndices`, `rule.directionIndex`, `clusterTable.index`, pair site IDs, `simulation.intent`, and `pathVector`/`pathLength` (lines 129–139). Residual completeness note N4-1. |
| R3-2 (Minor) | **Partially resolved** | "The per-field table above is authoritative" and the signed exceptions (spatial offsets, enums, `-1` environment sentinel) are stated (lines 167–172); distance buckets, jump tables, canonical tuples/mappings, and pair/cluster site indices are `uint64` (lines 91, 100, 103–113). Three contradictions remain: R4-3. |
| R3-3 (Minor) | **Resolved** | The language-neutral operation and four pinned examples are stated (lines 173–177, 294); I verified they reproduce with the specified formula; parent §11.4 and `compilation-pipeline.md` are updated in this commit (delta above) and no longer say charge-transport-only. |
| R3-4 (Minor) | **Resolved** | The fixtures section and acceptance require the same five samples in the manifest, `signature-vectors.json`, `.bin`/`.json` fixtures, and C# goldens (lines 239, 281, 295, 299). The manifest still lists three vectors and the C# goldens still omit interstitialcy, which is implementation work under the acceptance item, not a design gap; residual id-mapping note N4-5. |
| R3-5 (Minor) | **Resolved** | `tools/contracts/requirements.txt` with `msgpack==1.2.4`, the `pip install -r` step in `run_contracts`, the README, and the toolchain row moved to Step 08 are specified (lines 226, 232–236); the "existing dependency" wording is corrected (lines 47–48). Residual checklist note N4-3. |
| R3-6 (Minor) | **Resolved** | GUID-valued IDs use lowercase `D`/invariant; content-hash IDs are the 64-character SHA-256 strings as produced and are never parsed as GUIDs (lines 242–247). This matches the current writer's `ToString("D")` calls on typed GUID IDs. |
| R3-7 (Minor) | **Partially resolved** | `ProjectionFixtureTests.WriteProjectionFixtures`, its `dotnet test` command, `projectionSha256` (SHA-256 of the raw `.bin`), descriptor reuse of `model-v1.json`, and `ProjectionDescriptorTests` vs the internal `ProjectionLayout` are defined (lines 207–219). Residuals: R4-1 (descriptor path still contradicted) and R4-2 (writer mode/metadata comparison). |
| R3-8 (Minor) | **Resolved** | The primitive set and prohibition are exact (lines 149–153); `WriteRaw`/`Advance` exist in 3.1.12 as verified. Note N4-2 on the stated reason for the two typed writers. |
| R3-9 (Note) | **Resolved** | The checker is qualified as a byte/type conformance verifier, not the production encoder, and Step 11 remains the Rust encoder/vector owner (lines 239–240, 279–280). Note N4-4 on the remaining "independent" wording. |
| R3-10 (Note) | **Resolved** | ADR 0010 records the pre-release `model-v1` exception and that `status = "final"` means "finalized for the first release candidate"; `02-data-contracts.md` §3.3 records the same. |

#### Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| Minor | **R4-1 — The descriptor file is still defined twice (R3-7 residual).** The fixtures section says the descriptor is `contracts/generated/signatures/model-v1.json` and explicitly "no duplicate `model-v1.projection.json`" (lines 216–219), but the next paragraph still says `tools/contracts/generate.py` "emits the complete `model-v1` projection descriptor ... to `contracts/generated/signatures/model-v1.projection.json`" (lines 221–222), and the checker "reads the descriptor" without naming which file (line 225). `ProjectionDescriptorTests` reads `model-v1.json`. `generate.py` `build_artifacts` only writes `signatures/<id>-v1.json`, so the second path has no generator. The two files would be generated from the same `fields` array and contain the same tree, so the risk is duplicated/stale build output rather than wrong bytes. Delete the emitting sentence, name the file the Python checker loads, and make the `descriptor` field in the fixture JSON point at it. | Amendment lines 207–230; `tools/contracts/generate.py:525–538`; `contracts/generated/signatures/model-v1.json`; resolution R3-7 ("descriptor reuses `model-v1.json`"). |
| Minor | **R4-2 — The fixture write mode and the metadata comparison are not stated (R3-7 residual).** The regeneration command runs `dotnet test ... --filter FullyQualifiedName~ProjectionFixtureTests.WriteProjectionFixtures`, and "`ProjectionFixtureTests` also asserts that the checked-in `.bin`/`.json` files match a fresh compile" (lines 207–215). The design does not say how the writer is excluded from the default test run, although a writer that runs in a normal `dotnet test` would rewrite the tracked `.bin`/`.json` and make the freshness assertion self-healing; no golden-write/environment-gate convention exists in the current test projects. The checker step also says it "recomputes the domain-separated signature and `projectionSha256`" without saying it compares them with the `.json` fields, so the tracked metadata could drift from the bytes. State the opt-in write trigger (skipped/opt-in trait or environment variable), the default check test, and that `signatureHex`/`projectionSha256`/`fixtureFormat` are compared. | Amendment lines 207–230, 294; `src/dotnet/Mocassin.Compilation.Tests/Compiler/` and a repo-wide search (no existing write-mode, `UPDATE_GOLDEN`, or `MOCASSIN_WRITE` convention). |
| Minor | **R4-3 — Three type entries still contradict the authoritative summary and ADR 0010 rule 3 (R3-2 residual).** (a) The structure table says `interactionRange array<uint64>` (line 79) and the summary says interaction-range cell counts are `uint64` (line 168), but the bullet directly below says "exactly four `int64` values" (line 82). (b) `clusterLink`'s `siteSlot` (line 92) and `occupation.expansions`' `permutationIndex` (line 107) are non-negative dense indices (`CompiledClusterLink.SiteSlot` is the occupation slot, `0` = centre; `CompiledClusterExpansion.PermutationIndex` is an induced-permutation index), yet they are `int64`, which is outside the stated signed exceptions (spatial offsets, stability/kind enums, `-1` sentinel) and contrary to ADR 0010 rule 3. (c) The pair-ID rationale says "the structure site table and signed site indices carry the identity" (line 136) although `centerSiteIndex`/`partnerSiteIndex` are now `uint64`. Make (b) `uint64` or add an explicit "source-`int` retention" exception and amend ADR 0010, fix the `interactionRange` bullet, and correct "signed". | Amendment lines 79, 82, 91–92, 100, 107, 136, 167–172, 168; `CompiledStructureModel.cs` `CompiledClusterLink.SiteSlot`; `CompiledClusterTable.cs` `CompiledClusterExpansion.PermutationIndex`; ADR 0010 rule 3 (line 32); resolution R3-2. |
| Note | **N4-1 — The include/exclude rationale is still not exhaustive.** `direction.index` and `rule.index` are also dropped (array position is the dense index, like the `clusterTable.index` row) without a rationale row, and `position.encoding` is retained although its documented value is the base-cell `(0, 0, 0, P)`, duplicating `position.p`, while pair site IDs are dropped as redundant. Bytes are unaffected; one line per case keeps the rule reviewable. | Amendment lines 83–89, 118, 122, 129–139; `CompiledStructureModel.cs` `CompiledPosition.Encoding`/`P`; `CompiledTransitionModel.cs` `Index` fields. |
| Note | **N4-2 — The stated reason for prohibiting `WriteUInt64`/`WriteInt64` is imprecise.** The 3.1.12 XML/source shows both are typed writers (`MessagePackCode.Int32`/`Int64` are named in their summaries); the minimal-width encodings are `Write(ulong)`/`Write(long)`, and `WriteArrayHeader`/`WriteString` are the compact ones. The instruction to use `WriteRaw`/`Advance` for every forced header/value is still correct and sufficient, and `Write(double)` could be allowed since it emits `0xcb`; correct the rationale so the rule is defensible. | MessagePack 3.1.12 `lib/net9.0/MessagePack.xml` and `MessagePackWriter.cs` (downloaded to `/tmp/opencode`, not committed); amendment lines 149–153. |
| Note | **N4-3 — The Documentation checklist omits the files the body requires.** The body requires `tools/contracts/README.md` and the `06-toolchain-requirements.md` row move (lines 234–236), but the Documentation checklist lists only the pipeline doc, data contracts, ADR, and status/parity/audit bookkeeping (lines 302–307). Parent §11.4 and `compilation-pipeline.md` are already edited in this commit, so add the two remaining pages to the checklist so the closeout tick is traceable. | Amendment lines 232–236, 302–307; `tools/contracts/README.md:20`; `06-toolchain-requirements.md:126`. |
| Note | **N4-4 — "Independent" still appears unqualified in Scope and Risks.** Scope says "an independent Python re-encoding vector so C# and a second implementation agree" (line 39) and the risk row says "C# and Python encoders diverge" (line 314), while the cross-language section and acceptance correctly qualify the checker as a byte/type verifier over C#-produced bytes. Round 3 asked for the qualification in the approval summary; use the same term in the page so the claim is not overstated. | Amendment lines 39, 239–240, 279–280, 314. |
| Note | **N4-5 — Bookkeeping.** The request's `Design review` row still says "(round 1 Changes required; re-review requested)" while `Status` says "round 4 requested" (lines 9–11). The fixture ids (`ceria-kmc`, `ceria-mmc`, `vehicle5`, `interstitialcy5`, `barium-zirconate`) and the manifest vector ids (`model-ceria-kmc`, `model-vehicle5`, …) differ with no stated mapping, which makes the "same five samples" acceptance harder to check. | Amendment lines 9–11, 200, 239, 281; `contracts/manifest.json` `fixtures.signatureVectors`. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**.

#### Verdict

**Ready with notes.** No Blocker or Major remains. Round 4 resolves R3-1, R3-3, R3-4, R3-5, R3-6, R3-8, R3-9,
and R3-10, and it moves the quantisation and versioning rules into ADR 0010, §3.3, Step 08 §11.4, and
`compilation-pipeline.md`; the specified quantisation operation reproduces every pinned example, and the
`MessagePackWriter`/`WriteRaw` mechanism exists in the pinned package. The canonical encoding is implementable
from the authoritative per-field table plus the marker table.

R3-2 and R3-7 are only partially resolved (R4-1–R4-3), and the R3-2/R3-7 resolution rows overstate the fix:
`siteSlot`/`permutationIndex` are still `int64` against the stated exceptions, the `interactionRange` bullet still
says `int64`, and the descriptor is still described as both `model-v1.json` and `model-v1.projection.json`.
These are Minor documentation contradictions that must be corrected before implementation (they are not
waivers), but they do not change the conclusion that the design can be implemented. N4-1–N4-5 are bookkeeping.

The top-level `Record state` and round-1 `Verdict` above are the round-1 snapshot and are preserved unaltered;
this round's verdict is authoritative. The Python checker remains a byte/type conformance verifier over
C#-produced bytes, not an independent model-to-projection encoder: the manifest schema review and Step 11's Rust
encoder/vector are the completeness gates. The parent audit still has A1, A3–A9, and m1–m7 open and must be
re-run at the combined commit.

Follow-up owner: Step 08 implementor / planner
Follow-up issue/step: Apply R4-1–R4-3 and N4-1–N4-5 to
`docs/planning/amendments/step-08-amend-1-canonical-projection.md` (delete the stale `model-v1.projection.json`
sentence, state the fixture write mode and metadata comparison, and make `siteSlot`/`permutationIndex`
`uint64` or amend ADR 0010), then request a fresh re-review at the new frozen commit if the owner requires the
Minor corrections to be re-verified. Integrate the amendment, refresh the closeout evidence and CI, and re-run
the parent audit at the combined commit with A1, A3–A9, and m1–m7 in scope.
