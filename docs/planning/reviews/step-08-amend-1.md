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
| Reviewed commit | `5646aec730114a9f1df0b104bf2280948641b965` |
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
