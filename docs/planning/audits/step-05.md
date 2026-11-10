# Step 05 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-05.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Audit
> status`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 05 — Energy Model |
| Request owner | Step agent |
| Audit initiated by | Human owner |
| Audit status | Passed with notes |
| Audited commit | `e6a7839` |
| Step branch | `step/05-energy-model` |
| Step tag | — |
| Closeout CI run | [`42881426273`](https://github.com/seb-eis/mocassin-2/actions/runs/42881426273) — green at `e6a7839` |
| Platform CI | Not run (Ubuntu-only policy; no platform-specific code) |
| Audit date | 2026-11-10 |
| Auditor | Independent final re-audit agent (fresh context; no shared conversation with the planner, implementor, or the round-1/round-2 auditors) |

Step page: [`Step 05 — Energy Model`](../steps/step-05-energy-model.md)

## Audit request summary

- Frozen code commit: `d23fbd6` on `step/05-energy-model`.
- Feature sequence: energy model and persistence (`5361adf`), core validation (`a44f4dc`), symmetry geometry
  and validation (`3205be0`), queries and editing (`d3cb8bb`), preview and benchmark (`1804b51`), contracts and
  fixtures (`e1987c4`), samples and docs (`f7b1276`), closeout (`d23fbd6`).
- Local closeout gate: `./tools/verify.sh all` passed at `d23fbd6`; Zensical strict, DocFX, rustdoc, Rust,
  Python, contracts, API compatibility, and structure gates all passed.
- Closeout CI: run `42874483715`, one manual Ubuntu dispatch; .NET, Contracts and policy, Public API
  compatibility, and change detection passed; Rust/Python jobs were path-filtered and skipped.
- Benchmark: local short-run mean synthetic ceria-like proxy 88.9 µs and BZO-like proxy 77.4 µs, below the
  Step 05 targets.
- Known deferrals: compiled pair/cluster tables and the preview-to-compiler cross-check belong to Step 08;
  consolidated `ENM` diagnostics and `ENM018` emission belong to Step 07; energy GUI editors belong to Step 25.

## Re-audit request

Round 1 returned **Passed with notes** at code commit `d23fbd6` (record `5c61d88`). The step agent fixed F1–F4
on `feature/step-05-energy-model/fix-audit-findings` and squashed the result as `c3c48c4`:

- **F1:** cluster duplicate and centre-equivalence checks now compare the full encoded `(cellA, cellB, cellC, P)`
  and reserve the zero-shift encoding for centre equivalence.
- **F2:** omitted unstable environments now inherit the stable cutoff for unstable–stable pair sampling; the
  user guide and reference were reconciled, and cluster centres still require an explicit environment.
- **F3:** tests now cover inclusive/tolerance filter boundaries, stable symmetric versus unstable directional
  filters, inherited unstable sampling, and a Burnside unique count against a brute-force orbit count.
- **F4:** the cost preview now counts unique `EnergyId` tables for `UniquePairInteractionCount` and pair-table
  memory, so chiral directions are not double-counted.
- **N1:** benchmark evidence is labelled as synthetic proxies. **N3:** a null in-memory stable environment now
  returns null plus diagnostics. **N4:** the Step 08 page records the preview-to-compiler cross-check
  follow-up. **N2:** acknowledged as the docs-only record-parent process note.

`./tools/verify.sh all` passed at `c3c48c4`, and replacement closeout CI run
[`42878275463`](https://github.com/seb-eis/mocassin-2/actions/runs/42878275463) is green. A fresh re-audit of
the complete step at `c3c48c4` is requested; append a new round and preserve round 1 unchanged.

## Step summary

| Field | Value |
| --- | --- |
| Phase | B. Model definition |
| Depends on | 03, 04 |
| Delivers | Stable/unstable interaction environments, cutoff filters, multi-body clusters, and site energies, with validation and cost previews. |

## Step-specific acceptance checklist

- [x] Ceria's stable environment and oxygen migration-edge cluster validate. — `EnergySampleTests` validates the
      ceria sample and the BZO sample with the Symmetry validator; the ceria cluster is the oxygen migration edge.
- [x] A stable filter that removes a specific sublattice pair is applied symmetrically; an unstable filter is
      one-directional and keeps `SiteAId` equal to the environment site. — `IsFiltered` is symmetric for stable
      environments and forward-only for unstable environments; `EnvironmentTests` covers the miscentered
      unstable filter, and `EnergyGeometryTests` exercises a stable filter on a cluster. The pair-sampling
      direction case is part of F3's test-coverage gap.
- [x] Filter boundaries are inclusive and use the 1e-5 Å distance tolerance. — `IsFiltered` uses
      `DistanceEqualityTolerance` on both edges; no committed test exercises a boundary value (F3).
- [x] A cluster with 2 or 8 offsets is accepted; 1 or 9 offsets are rejected (`ENM008`). — `EnergyGeometryTests`
      uses an 8-offset cluster and `ClusterTests` rejects 1 and 9.
- [x] Duplicate, center-equivalent, unknown/unstable/filtered-surrounding, ring/degenerate, and out-of-range
      clusters are rejected with paths (`ENM009`–`ENM011`). — covered by `EnergyGeometryTests` and
      `EnergySampleTests`; F1 records the over-rejection of distinct periodic images of the same site.
- [x] The cost preview reports pair counts and permutation counts that match a brute-force oracle, including
      the center-particle factor and `long` overflow-safe limits; the Step 08 cross-check is recorded as a
      Step 08 follow-up, not a Step 05 closeout claim. — pair-image counting is independently enumerated and the
      63⁹ case yields `ENM013` without overflow; the Burnside/brute-force and unstable-pair coverage is F3, and
      the chiral table count is F4. The Step 08 cross-check appears in the step page and audit request (N4).
- [x] Chiral pair partners are detected, share one `EnergyId`, and expose `ChiralPartnerGeometryId`;
      independent A→B/B→A energy values are unrepresentable. — `ChiralDirectionsShareEnergyIdAndPartnerGeometry`
      and the ceria preview probe show shared `EnergyId` and reciprocal partner IDs.
- [x] The energy sections round-trip; the default `stableEnvironment` loads the 1.0 Å/no-filter default; empty
      sections are omitted; semantic arrays preserve order. — `EnergyModelTests`, `ProjectSchemaTests`, and the
      contract fixtures cover save/load, defaults, omission, and offset order.
- [x] Canonical collection ordering and `ProjectHasher.ComputeEnergy` are stable under array shuffles and
      change when energy data changes. — `EnergyHashTests` and `EnergyModelTests` cover shuffles, unrelated
      edits, and every energy-section change.
- [x] The generated project schema accepts the energy sections, and fixtures cover empty and populated models.
      — `project-energy-minimal`/`project-energy-populated` are in the schema-fixture list and the schema
      rejects an unknown energy field.
- [x] Every `ENM001`–`ENM021` code has a positive or negative test as applicable, including the ENM018
      Step 07 boundary. — all 21 constants are exercised by the Core/Symmetry tests; `ENM018` is asserted to be
      defined but not emitted by Step 05.
- [x] New files carry SPDX headers and introduce no new dependency. — the contract gate reports SPDX headers
      present in 250 source files; no project/package file changed.
- [x] Public API files and the ApiCompat baseline are current. — `./tools/verify.sh api` passed all nine
      ApiCompat checks; the new Core, Symmetry, and Packages surface is in `PublicAPI.Unshipped.txt`.
- [x] Documentation pages, navigation, index links, status dashboard, and parity matrix are updated. — the
      strict docs build passes; `docs/user-guide/energy.md`, `docs/physics/energies.md`,
      `docs/reference/energy.md`, `docs/reference/project-format.md`, `docs/development/compilation-pipeline.md`,
      the nav, the section indexes, `status.md`, and the parity rows are current.

Re-audit round 2 note: every acceptance item above was re-checked at `c3c48c4`; the F1–F4 fixes are present
and the round-1 F3 test gaps are closed. A new Major cache finding (F5) and two Minor documentation/step-page
findings (F6, F7) are recorded in round 2.

## Standard audit checklist

- [x] Required tests from the step page pass. — the committed suite is green (Core 383, Symmetry 208,
      Packages 42, Architecture 1); F3 records missing filter-boundary, unstable-pair, and Burnside/brute-force
      test rows.
- [x] Public/protected XML documentation policy is followed. — DocFX builds 209 files with 0 warnings/0 errors;
      the new public types and members carry XML docs; ApiCompat passes.
- [x] Documentation, status dashboard, feature log, and parity matrix are current. — the strict docs build
      passes and the step page, dashboard, feature log, parity rows, and published pages match the closeout
      state.
- [x] Closeout CI is green at the audited commit. — GitHub Actions run `42874483715` is `completed/success` at
      `d23fbd6`; Detect changes, .NET (Ubuntu), Contracts and policy, and Public API compatibility passed;
      Rust/Python were skipped by path filters after the local `verify.sh all` pass.
- [x] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code. — the diff contains no P/Invoke, native interop, GUI, path/case/locking, or OS-conditional code;
      the Ubuntu-only policy applies.
- [x] Format/version/fixture changes are documented and tested. — additive project-v1 energy fields;
      manifest status/notes, generated schema/descriptors, and the minimal/populated energy fixtures are
      current; default and empty sections are omitted to preserve pre-Step-05 hashes.
- [x] Security, dependency, licence, and secret-scan gates pass. — NuGet licence/vulnerability policy passes;
      SPDX headers present (250 files); no secret patterns (562 files); cargo-deny advisories/bans/licenses/
      sources OK.
- [x] Code structure policy passes; no new root-level or catch-all folders. — `./tools/verify.sh structure`
      passes; the new `Energy/` concept folders and partial classes are within the file-count/depth limits.
- [x] No unrelated changes or unapproved scope. — the 74-file code diff is limited to Step 05 energy code,
      tests, contracts, samples, benchmark, and documentation; no Step 06/08/09/15/25 work, legacy import, or
      GUI implementation.
- [x] Branch, tag, and status hygiene are correct. — branch `step/05-energy-model`; no `step-05` tag and no
      main merge; the only step-branch CI run is the closeout run; no per-feature runs. The record parent is the
      docs-only audit-request commit `617cd3a`; the audited code is frozen at `d23fbd6` (N2).

Re-audit round 2 note: all standard items above were re-checked at `c3c48c4`; the replacement closeout run
`42878275463` is green at that commit, no workflow run was created for the docs-only `18e01e3`, and the full
local gate passes. See round 2 for F5–F7 and N5–N7.

## Evidence

### Audit round 1

**Reviewer context.** I am a fresh audit agent. I did not plan, implement, or review Step 05 and have no shared
conversation with the planner or implementor. I read `AGENTS.md` (§3.8, §3.10, §3.12, §4, §5, §7–§12, §16),
`docs/planning/audits/README.md`, the audit template, this request, the Step 05 page, the Step 05 design review,
the energy section of `docs/planning/02-data-contracts.md`, `docs/planning/status.md`,
`docs/planning/appendix-b-parity-matrix.md`, ADRs 0001/0009/0010/0013, the manifest/generated schema/fixtures,
the samples, the public API files, and the full Step 05 implementation and test diff. I did not rely on the
request summary for the verdict.

**Frozen state.** Worktree `/workspace/mocassin`, branch `step/05-energy-model`, `HEAD` `617cd3a`. The audited
code commit is `d23fbd6` (`d23fbd6f97d9be7c965bd6d998395e2c3e96f2ca`); `git status --short` is empty.
`git diff --name-only d23fbd6..617cd3a` lists only `docs/planning/audits/step-05.md`,
`docs/planning/status.md`, and `docs/planning/steps/step-05-energy-model.md`, so the delta is documentation-only.
The record commit parent is therefore the docs-only request commit `617cd3a`, while the code audited is frozen
at `d23fbd6` (N2).

**Commands and results (from `/workspace/mocassin`):**

```text
./tools/verify.sh all
# EXIT:0
# .NET: build succeeded (0 warnings / 0 errors), dotnet format clean;
#       Core 383, Symmetry 208, Packages 42, Architecture 1, support suites pass
# Public API: all nine ApiCompat checks pass; no vulnerable NuGet packages
# Rust: fmt/clippy/tests pass
# Python: pytest and package build pass
# Documentation: Zensical strict ("No issues found"), DocFX (0 warnings / 0 errors), rustdoc pass
# Contracts: artifacts current (17 files), catalog current, SPDX 250 files, no secret patterns (562 files),
#            cargo-deny advisories/bans/licenses/sources OK

git diff --check
# (no output; exit 0)

git diff --stat 40bfb30...step/05-energy-model
# 75 files changed, 6899 insertions(+), 89 deletions(-)
git diff --stat 40bfb30...d23fbd6
# 74 files changed, 6869 insertions(+), 81 deletions(-)

git log --oneline 40bfb30..d23fbd6
# d23fbd6 step-05: closeout status, parity, and final checks
# f7b1276 step-05: energy samples and documentation
# e1987c4 step-05: energy contracts and fixtures
# 1804b51 step-05: energy preview and benchmark
# d3cb8bb step-05: energy queries and editing
# 3205be0 step-05: symmetry energy geometry validation
# a44f4dc step-05: core energy model validation
# 5361adf step-05: energy model and persistence

GitHub Actions run 42874483715 (verified by API)
# CI completed/success at d23fbd6; event workflow_dispatch; branch step/05-energy-model
# jobs: Detect changes, Public API compatibility, Contracts and policy, .NET (ubuntu-latest) all success;
#       Python and Rust skipped by path filters
```

**Code and artifact review.** I inspected the Core energy records, validator, query, editor, canonicalization,
and hasher; the Symmetry context, sampler, geometry validator, cost/pair preview, and cache; the Packages
composition; the Core/Symmetry/Packages tests; the benchmark; the manifest, generated schema, descriptors, and
energy fixtures; the ceria/BZO samples; the user-guide, physics, reference, development, and project-format
pages; `zensical.toml`; the status dashboard and parity rows; and the public API/baseline changes. I traced the
acceptance items to concrete code and test evidence rather than relying on the feature log.

**Scratch probes (not committed).** I compiled a temporary console harness under `/tmp/opencode/negstart`
against the release `Mocassin.Core`/`Mocassin.Symmetry` assemblies to exercise edge cases without modifying the
repository:

```text
ceria cluster, offsets (0.25,0.25,0.25) and (1.25,0.25,0.25)
# ENM009 Error "duplicate surrounding position after periodic encoding" (F1)
ceria cluster, offsets (1,0,0) and a partner offset
# ENM011 Error "position equivalent to its centre" (F1)
ceria with UnstableEnvironments = [] vs authored environment
# sampler pairs: 12 vs 36; omitted environment adds no unstable-stable pairs (F2)
ceria cost/pair preview
# StableP1PositionCount=12, PairImageCount=112, UniquePairInteractionCount=36,
# distinct EnergyId=35, EstimatedMemoryBytes=4656, one cluster cost (F4)
model with StableEnvironment = null!
# TryCreatePreview threw ArgumentNullException instead of returning null + diagnostics (N3)
```

**Checklist verification.** I marked the request checklists above and added only short notes; the item text is
unchanged. The 21 `ENM` constants are all referenced by a committed positive or negative test, including the
`ENM018` "defined but not emitted" boundary test. The closeout CI run was re-queried and matches the request
record.

## Known notes for the auditor

- The closeout `verify.sh all` and CI cover the frozen code commit `d23fbd6`; later commits may be
  documentation-only audit-record/status commits.
- Step 08 must cross-check the Step 05 preview against the compiled pair/cluster tables. This is recorded as a
  Step 08 follow-up, not a Step 05 closeout claim.
- `ENM018` is defined with help text but is emitted by Step 07; the consolidated diagnostic catalog and
  troubleshooting page are Step 07 deliverables.
- Energy GUI editors are Step 25 deliverables; Step 05 exposes the model, query, editing, and preview APIs.
- Windows and macOS were not run because Step 05 has no platform-specific code; this is the Ubuntu-only policy.
- The benchmark values are local short-run measurements, not an official reference-runner certification.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **F1 — Cluster duplicate and centre-equivalence checks ignore the periodic cell shift.** The frozen design defines duplicate/centre-equivalent offsets by the full encoded position `(cellA, cellB, cellC, P)` and says offsets may lie outside `[0,1)`. `EnergyGeometryValidator` keys both checks on `targetP` only, so two offsets that resolve to the same site in different cells are rejected even though their full encodings differ. Scratch ceria probe: offsets `(0.25,0.25,0.25)` and `(1.25,0.25,0.25)` produce `ENM009`; `(1,0,0)` plus a partner produces `ENM011`. This over-rejects a valid cluster; it is not silent data corruption. Compare the full `Vector4I` encoding and reserve `targetP` for the zero-shift case. | `EnergyGeometryValidator.cs:290,300`; step page lines 176–185; `02-data-contracts.md:395–398`; scratch probe. |
| **Minor** | **F2 — Omitted unstable-environment semantics contradict the frozen step page.** The step page says an omitted unstable environment inherits the stable cutoff and samples unstable-stable pairs only. The sampler and sampling validator iterate only authored `UnstableEnvironments`; the user guide says the stable environment is not reused. Scratch ceria probe: 36 sampled pairs with the environment versus 12 without, i.e. no unstable-stable pairs for the omitted environment. If the step page is authoritative, those interactions are silently lost; if the user guide is authoritative, the step-page sentence is stale. Reconcile the design text or implement inheritance explicitly. | Step page lines 143–144; `EnergyGeometrySampler.cs:76–91,232–269`; `EnergyGeometryValidator.cs:118–141`; `docs/user-guide/energy.md:17–19`; scratch probe. |
| **Minor** | **F3 — Required filter-boundary, pair-filter, and Burnside/brute-force test rows are missing or weak.** The step page Tests table requires inclusive filter boundaries with the 1e-5 Å tolerance, stable/unstable pair-filter behaviour, and a Burnside unique count matching brute force. The committed tests cover invalid filter ranges and duplicate filters plus a cluster filter path, but no distance sits exactly on a filter boundary or tolerance edge; the pair-count oracle covers stable images only and reuses production `RadialSearch`/`IsFiltered`; and the only cost-preview permutation assertion is a P1 identity case (`Raw == Unique == 12`), so `BurnsideCount`/`FixedCount` never run with a non-trivial induced permutation group. No test compares `UniquePermutationCount` with a brute-force orbit count. | Step page lines 678–679, 688–693; `EnvironmentTests.cs:46–166`; `EnergyGeometryTests.cs:75–128`; `EnergyPreviewTests.cs:40–43,78–105,239–288`; `EnergyPreviewService.cs:314–369`. |
| **Minor** | **F4 — Pair-table memory estimate double-counts chiral directions.** Step 08 and the Step 05 design say chiral directions share one `EnergyId` and one energy table. The preview sets `UniquePairInteractionCount = pairs.Length` (directed geometries) and reuses that count in `pairBytes = pairs.Length × particleCount² × 8`. For chiral pairs the shared table is counted twice; ceria reports 36 directed geometries but 35 distinct `EnergyId`s, and a P1 model would be 2×. The estimate is advisory and conservative, not a data-loss risk, but it does not match the design's "one energy table" model. | `EnergyPreviewService.cs:118–130`; step page lines 236–262; Step 08 lines 144–149; scratch ceria preview. |
| **Note** | **N1 — Benchmark fixtures are synthetic P1 proxies.** `EnergyPreviewBenchmarks.Setup` builds `CreateFixture("Ceria-like", 5.41, 3.0)` and `CreateFixture("Barium-zirconate-like", 4.19, 2.0)`; the step-page row and audit request label the values "ceria"/"BZO". The target is met by a wide margin on the proxies, and the values are local short-run measurements not re-run in this audit. The record should say the benchmark uses synthetic proxies. | `EnergyPreviewBenchmarks.cs:34–35,84–85`; step page row 26; audit request line 38. |
| **Note** | **N2 — Record parent is the docs-only request commit.** The audited code is `d23fbd6`, but the record commit's parent is `617cd3a` because the audit request/status/step-page update sits between them. `git diff --name-only d23fbd6..617cd3a` lists only documentation files, so the audited code is unchanged. This is the same process note as prior steps. | `git diff --name-only d23fbd6..617cd3a`; command output above. |
| **Note** | **N3 — In-memory null `StableEnvironment` makes the preview throw.** `TryCreatePreview` builds the cache key (`ProjectHasher.ComputeEnergy`) before validation; a public-record `with { StableEnvironment = null! }` throws `ArgumentNullException` instead of returning null plus diagnostics, contrary to the preview contract. Persisted JSON rejects null, so this is in-memory robustness only (same class as Step 04 F4). | `EnergyPreviewService.cs:75–82`; `ProjectHasher.cs:72–84`; `ProjectJson.cs:93–143`; `docs/reference/energy.md:78–79`; scratch probe. |
| **Note** | **N4 — Step 08 cross-check follow-up is not on the Step 08 page.** The deferred preview-versus-compiled-table cross-check is recorded in the Step 05 page and this audit request, but Step 08 only names the shared sampler kernel; the Step 08 agent should carry the cross-check explicitly. | Step page lines 624–626, 706; audit request line 101; `step-08-model-compilation-pipeline.md:129–132`. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Verdict

- [ ] Passed
- [x] Passed with notes
- [ ] Fixes required
- [ ] Re-audit required
- [ ] Waived

Follow-up owner: Step 05 agent (reconcile F1–F4 or record explicit human acceptance of the notes); human owner
for the approval gate.
Follow-up issue/step: Step 05 — Energy Model. If fixes are made, use a new feature branch, squash into the step
branch, dispatch one new closeout CI run, and request a fresh re-audit. If the notes are accepted, proceed to the
Step 05 merge approval with the §3.12 cumulative approval report.

**Verdict: Passed with notes.** No Blocker or Major finding remains. The energy model and persistence, canonical
energy hashing, Core/Symmetry validation split, query/editing surface, shared sampler, cost and pair previews,
contract schema/fixtures, samples, documentation, parity rows, and status dashboard satisfy the frozen design;
the full local gate and closeout CI are green at `d23fbd6`; no Step 06/08/09/15/25 implementation, GUI, legacy
import, or preview persistence was added. F1–F4 are Minor: a cluster over-rejection for distinct periodic
images, an unresolved omission-semantics sentence for unstable environments, missing/weak required test rows,
and an advisory chiral-table memory overestimate. N1–N4 are notes: synthetic benchmark proxies, the docs-only
record parent, an in-memory null-environment throw, and the Step 08 cross-check follow-up. The human owner must
explicitly accept F1–F4 and N1–N4 or request fixes; a fix commit requires a fresh re-audit. The audit result is
evidence, not merge authorization.

### Re-audit round 2

#### Independence and frozen state (round 2)

I am a fresh independent re-audit agent. I did not plan, implement, or review Step 05 and have no shared
conversation with the planner, implementor, or round-1 auditor. I read `AGENTS.md` (§3.8, §3.10, §3.12, §3.13,
§4, §5, §7–§12), the audit README and template, this record's round 1 and re-audit request, the Step 05 page,
the Step 05 design review, the energy section of `docs/planning/02-data-contracts.md`, ADRs 0001/0009/0010/0013,
the status dashboard, the parity matrix, the manifest/schema/fixtures, the samples, the published energy pages,
the public API files, and the complete Step 05 implementation and test diff. I did not rely on the request
summary or the step-agent resolutions for the verdict.

Worktree `/workspace/mocassin`, branch `step/05-energy-model`, `HEAD` `18e01e3`. The audited code commit is
`c3c48c4` (`c3c48c4774bf4e965c11ab24393a4211f8efa492`); `git status --short` is empty. `git diff --name-only
c3c48c4..18e01e3` lists only `docs/planning/audits/step-05.md`, `docs/planning/status.md`, and
`docs/planning/steps/step-05-energy-model.md`, so the HEAD delta is documentation-only. The record commit
parent is therefore the docs-only re-audit-request commit `18e01e3`, while the audited code is frozen at
`c3c48c4` (the same process note as N2 in round 1).

`git diff --stat 40bfb30...step/05-energy-model` is 76 files, 7375 insertions(+), 128 deletions(-); `git show
--stat c3c48c4` is 9 files, 288 insertions(+), 19 deletions(-). The fix commit touches the energy sampler,
geometry context/validator, preview service, the Symmetry energy tests, the user-guide/reference text, and the
Step 08 page; the full step diff is limited to Step 05 energy code, tests, contracts, samples, benchmark, and
documentation. No Step 06/08/09/15/25 implementation, GUI, legacy import, or preview persistence was added.

#### Commands and results (round 2, from `/workspace/mocassin`)

```text
./tools/verify.sh all
# EXIT:0
# .NET: build succeeded (0 warnings / 0 errors), dotnet format clean;
#       Core 383, Symmetry 213, Packages 42, Architecture 1, and the support suites pass
# Public API: all nine ApiCompat checks pass; no vulnerable NuGet packages
# Rust: fmt/clippy/tests pass
# Python: pytest and package build pass
# Documentation: Zensical strict ("No issues found"), DocFX (0 warnings / 0 errors), rustdoc pass
# Contracts: artifacts current (17 files), catalog current, SPDX 250 files, no secret patterns (562 files),
#            cargo-deny advisories/bans/licenses/sources OK

git diff --check
# (no output; exit 0)
git diff --check 40bfb30...c3c48c4
# (no output; exit 0)
git diff --stat 40bfb30...step/05-energy-model
# 76 files changed, 7375 insertions(+), 128 deletions(-)
git show --stat c3c48c4
# 9 files changed, 288 insertions(+), 19 deletions(-)

dotnet test src/dotnet/Mocassin.Symmetry.Tests/Mocassin.Symmetry.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~PeriodicCellShiftIsNotADuplicateOrCentreEquivalentOffset|\
FullyQualifiedName~StableFilterBoundariesAreInclusiveAndSymmetric|\
FullyQualifiedName~UnstableFilterIsDirectionalAndOmittedEnvironmentInheritsStableCutoff|\
FullyQualifiedName~BurnsideUniqueCountMatchesBruteForceOrbitCount|\
FullyQualifiedName~NullStableEnvironmentReturnsNullWithoutThrowing"
# Total tests: 5; Passed: 5 (the five new round-2 regression tests)
```

**Closeout CI (GitHub API; no token printed).** Run `42878275463`: name `CI`, event `workflow_dispatch`,
branch `step/05-energy-model`, `head_sha` `c3c48c4774bf4e965c11ab24393a4211f8efa492`, status `completed`,
conclusion `success`. Jobs `Detect changes`, `Public API compatibility`, `.NET (ubuntu-latest)`, and
`Contracts and policy` are `success`; `Python` and `Rust` are `skipped` by the path filters. The recent-runs
query shows no workflow run for the docs-only `18e01e3` and no per-feature step runs.

**Scratch probes (not committed).** A read-only console harness under `/tmp/opencode/energycache` loaded the
real release `Mocassin.Core`/`Mocassin.Symmetry` assemblies and constructed in-memory models. It confirmed the
F1 periodic-shift boundary, the F2 inherited unstable environment, the N3 null-environment guard, the F4
distinct-`EnergyId` count, and the two cache/diagnostic behaviours recorded as F5 and N7 below. The probe did
not modify the repository.

#### Round-1 finding verification (round 2)

| Finding | Result |
| --- | --- |
| F1 | **Fixed.** `EnergyGeometryValidator` keys duplicate detection on the full `(cellA, cellB, cellC, P)` encoding and reserves `(0, 0, 0, centerP)` for centre equivalence (`EnergyGeometryValidator.cs:226–306`). `PeriodicCellShiftIsNotADuplicateOrCentreEquivalentOffset` passes; the scratch probe confirms distinct periodic images no longer raise `ENM009`/`ENM011`. |
| F2 | **Fixed.** `EnergyGeometryContext.GetEffectiveUnstableEnvironments` adds the inherited stable cutoff for every unstable site; the sampler and sampling validator consume it (`EnergyGeometryContext.cs:125–143`; `EnergyGeometrySampler.cs:84–91`; `EnergyGeometryValidator.cs:118–141`). `UnstableFilterIsDirectionalAndOmittedEnvironmentInheritsStableCutoff` passes, and the user guide/reference now agree. |
| F3 | **Fixed.** The new tests cover inclusive filter boundaries with the 1e-5 Å tolerance, stable symmetric versus unstable directional filtering, inherited unstable sampling, and a Burnside unique count against a brute-force orbit count; all five targeted tests pass. The full Symmetry suite grew from 208 to 213 tests. |
| F4 | **Fixed in code.** `UniquePairInteractionCount` is now the distinct `EnergyId` count and the pair-table memory uses it (`EnergyPreviewService.cs:121–138`); `ValidModelProducesCostAndPairPreviews` compares the count with the distinct `EnergyId` set. The documentation wording is stale (F6). |
| N1 | **Addressed.** The step-page feature row and benchmark code identify the fixtures as synthetic ceria-like/BZO-like proxies. |
| N2 | **Acknowledged.** The audited code is `c3c48c4`; the later `18e01e3` re-audit request is documentation-only, so the record parent differs from the audited code commit. |
| N3 | **Fixed.** Both preview entry points return `null` plus an `ENM001` diagnostic for a null in-memory `StableEnvironment` before building the cache key (`EnergyPreviewService.cs:70–78, 157–165`); `NullStableEnvironmentReturnsNullWithoutThrowing` passes. |
| N4 | **Addressed.** `step-08-model-compilation-pipeline.md` now records the preview-versus-compiled-table cross-check as a Step 08 follow-up. |

#### Findings (round 2)

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **F5 — The energy preview cache key omits the particle model, so a cached preview can report stale counts/memory and suppress the `ENM013` hard-limit diagnostic.** `CreateCacheKey` hashes only `ProjectHasher.ComputeEnergy(model)` (the energy sections) and `ProjectHasher.ComputeStructure(model.Structure)` (`EnergyPreviewService.cs:437–447`; `ProjectHasher.cs:72–84`). The cost preview's cluster estimates and memory depend on `model.ParticleSets`/`model.Particles` (`EnergyPreviewService.cs:128, 244–334`), and `Validate` depends on particle-set membership for cluster permutations. The step-page cache-key design (lines 274–276) omits the particle model for the same reason. After a particle/particle-set edit with unchanged energy/structure, the service returns the old cached entry instead of re-validating. Scratch probe: a one-particle model previewed `cluster raw=1 unique=1 mem=104`; after adding a particle to the centre set the same service returned `raw=1 unique=1 mem=104`, while a fresh service returned `raw=8 unique=8 mem=280`. A 9-site/8-offset cluster that should fail after a 63-particle set edit returned `preview=ok raw=1 errors=0` from the cached service and `preview=null errors=1 codes=ENM013` from a fresh service. This violates the `TryCreatePreview` null-plus-diagnostics contract and the cost-preview acceptance criterion; the preview is advisory, but the stale diagnostic can hide a hard limit. Include a particle-model hash (or a whole-model hash) in the key, and/or revalidate on cache hit; add a regression test and update the cache-key documentation. | `EnergyPreviewService.cs:81–82, 114–143, 244–334, 437–447`; `ProjectHasher.cs:72–84`; `EnergyPreviewTests.CostPreviewIsCachedByEnergyAndCellCounts` (lines 160–178, no particle-change case); step page lines 274–276; `docs/reference/energy.md:82`; `docs/development/compilation-pipeline.md:133–134`; scratch output above. |
| **Minor** | **F6 — The F4 fix left the public XML doc and user guide describing the old directed-geometry meaning of `UniquePairInteractionCount`.** The property now counts distinct `EnergyId` tables, so chiral directions count once. The XML parameter doc still says "the symmetry-reduced directed pair geometry count" (`EnergyCostPreview.cs:12`), and the user guide still says the preview reports "unique directed pair geometries" (`docs/user-guide/energy.md:74`). For a chiral model these differ; update both to the unique pair energy-table/`EnergyId` count. | `EnergyPreviewService.cs:121–138`; `EnergyCostPreview.cs:12`; `docs/user-guide/energy.md:74`; round-1 F4. |
| **Minor** | **F7 — Three implemented Step 05 tasks remain unchecked on the step page.** `docs/planning/steps/step-05-energy-model.md:580–587` still has `[ ]` for the `ModelDefinition`/JSON extension, `ProjectHasher.ComputeEnergy`, and the energy records, although the acceptance criteria (lines 619–620), feature log, code, and tests show them complete. Mark them done or record why they are deferred. | Step page lines 580–587 vs 619–620; `ModelDefinition.cs`, `ProjectJson.cs`, `ProjectHasher.cs`, `EnergyModelTests.cs`, `EnergyHashTests.cs`. |
| **Note** | **N5 — The user-guide cluster wording still says "distinct stable site".** `docs/user-guide/energy.md:41` says every surrounding position must resolve to a "distinct stable site", while F1 now accepts distinct periodic images of the same reference site when their full encodings differ. Recommend "distinct stable position/encoded position" to match the step page and validator. | `docs/user-guide/energy.md:41`; step page lines 178–185; `EnergyGeometryValidator.cs:226–306`; F1 test. |
| **Note** | **N6 — The Step 05 deviation table combines void-on-stable with `ENM014` Error, but the code emits `ENM016` Warning.** `docs/planning/steps/step-05-energy-model.md:356` lists "Duplicate site-energy entry or void-on-stable" as `ENM014` Error, while the §7 table (line 311) and `EnergyModelValidator` emit `ENM016` Warning for a particle the site cannot occupy (the void case). The normative §7 table and the code are consistent; the §7.1 row is misleading. | Step page lines 311, 356; `EnergyModelValidator.cs:393–398`; `SiteEnergyTests.ForbiddenOccupancyProducesEnm016Warning`. |
| **Note** | **N7 — Two zero offsets report `ENM009` plus two `ENM011` diagnostics, despite the step page's "do not double-report" wording.** A cluster with offsets `[0, 0]` and `[0, 0]` produces `ENM009`, `ENM011`, and a second `ENM011` in the scratch probe. The diagnostics are not wrong, but the step page says `ENM009` owns surrounding-surrounding duplicates and `ENM011` adds centre-involving cases without double-reporting. Consider suppressing the redundant duplicate diagnostic for two centre-equivalent offsets or clarifying the wording. | `EnergyGeometryValidator.cs:289–306`; scratch probe output `ENM009 ... ENM011 ... ENM011`; step page lines 196–200. |

#### Verdict (round 2)

- [ ] Passed
- [ ] Passed with notes
- [x] Fixes required
- [ ] Re-audit required
- [ ] Waived

Follow-up owner: Step 05 agent for F5–F7 and N5–N7; human owner for the approval gate.
Follow-up issue/step: Step 05 — Energy Model. F5 is Major; F6–F7 are Minor; N5–N7 are Notes. No Blocker was
found. The human owner must require a fix commit or explicitly waive the findings. A fix commit requires a new
feature branch, squash integration into the step branch, one replacement closeout CI run, and a fresh re-audit.

**Verdict (round 2): Fixes required.** The F1–F4 fixes are verified and covered by the committed regression
tests; N1–N4 are addressed; the full local gate and replacement closeout CI are green at `c3c48c4`; and the
HEAD delta is documentation-only. However, F5 is a Major correctness/contract finding in the public preview
service: the cache key omits the particle model, so a cached cost preview can report stale permutation/memory
values and can suppress the `ENM013` hard-limit diagnostic after a particle-set edit. This violates the
`TryCreatePreview` null-plus-diagnostics contract and the step's cost-preview acceptance criterion. F6–F7 are
Minor documentation/step-page defects; N5–N7 are notes. The energy model, persistence, hashing, validation
split, sampler, contracts/fixtures, samples, documentation, parity rows, and dashboard are otherwise current.
The audit result is evidence, not merge authorization.

## Re-audit request (round 3)

Round 2 returned **Fixes required** at code commit `c3c48c4` (record `fa40d6d`) with F5 (Major) and F6/F7
(Minor) plus N5–N7 (Notes). The step agent fixed them on
`feature/step-05-energy-model/fix-reaudit-findings` and squashed the result as `e6a7839`:

- **F5:** the preview cache key now includes the full canonical model hash in addition to the energy and
  structure hashes, so particle/particle-set edits invalidate cached counts and diagnostics. A regression test
  verifies cluster raw counts change after a particle-set edit.
- **F6:** the `UniquePairInteractionCount` XML doc and user guide now describe unique pair energy tables
  (distinct `EnergyId` values).
- **F7:** the three completed implementation-task checkboxes on the Step 05 page are marked done.
- **N5:** user-guide wording now says distinct stable position/full periodic encoding.
- **N6:** the §7.1 row now separates duplicate site-energy entries (`ENM014` Error) from void-on-stable
  (`ENM016` Warning).
- **N7:** repeated centre-equivalent offsets now report one `ENM011` instead of one per offset; the
  surrounding duplicate still reports `ENM009`.

`./tools/verify.sh all` passed at `e6a7839`, and replacement closeout CI run
[`42881426273`](https://github.com/seb-eis/mocassin-2/actions/runs/42881426273) is green. A fresh re-audit of
the complete step at `e6a7839` is requested; append a new round and preserve rounds 1–2 unchanged.

### Re-audit round 3

#### Independence and frozen state (round 3)

I am a fresh independent final re-audit agent. I did not plan, implement, or review Step 05 and have no shared
conversation with the planner, implementor, or the round-1/round-2 auditors. I read `AGENTS.md` (§3.8, §3.10,
§3.12, §3.13, §4, §5, §7–§12, §16), the audit README and template, the complete audit record (rounds 1–2,
resolutions, and the round-3 request), the Step 05 page, the design review, the energy contract
(`docs/planning/02-data-contracts.md` §2.2 Energy), ADRs 0001/0009/0010/0013, the status dashboard, the parity
matrix, the manifest/generated schema/fixtures, the samples, the published energy pages, the public API files,
and the whole Step 05 implementation and test diff. I did not rely on the request summary or the step-agent
resolutions for the verdict.

Worktree `/workspace/mocassin`, branch `step/05-energy-model`, `HEAD` `8a59107`. The audited code commit is
`e6a7839` (`e6a78394359949e81ff2978433670b57fb12621e`); `git status --short` is empty. `git diff --name-only
e6a7839..HEAD` lists only `docs/planning/audits/step-05.md`, `docs/planning/status.md`, and
`docs/planning/steps/step-05-energy-model.md`, so the HEAD delta is documentation-only and the audited code is
unchanged. The record commit parent is therefore the docs-only final re-audit-request commit `8a59107`, not the
audited code commit (the same process note as N2).

#### Commands and results (round 3, from `/workspace/mocassin`)

```text
./tools/verify.sh all
# EXIT:0
# .NET: build 0 warnings / 0 errors, dotnet format clean;
#       Core 383, Symmetry 215, Packages 42, Architecture 1, and App/Results/Compilation/Contracts 1 each
# Public API: all nine ApiCompat checks pass; no vulnerable NuGet packages
# Rust: fmt/clippy/tests pass
# Python: pytest and package build pass
# Documentation: Zensical strict ("No issues found"), DocFX (0 warnings / 0 errors), rustdoc pass
# Contracts: artifacts current (17 files), catalog current, SPDX 250 files, no secret patterns (562 files),
#            cargo-deny advisories/bans/licenses/sources OK

git diff --check
# (no output; exit 0)
git diff --check 40bfb30...e6a7839
# (no output; exit 0)
git diff --stat 40bfb30...step/05-energy-model
# 76 files changed, 7605 insertions(+), 136 deletions(-)
git show --stat e6a7839
# 9 files changed, 87 insertions(+), 18 deletions(-)

dotnet test src/dotnet/Mocassin.Symmetry.Tests/Mocassin.Symmetry.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~TwoCentreEquivalentOffsetsReportOneEnm011|\
FullyQualifiedName~CacheInvalidatesWhenParticleModelChanges"
# 1 test file matched; Passed: 2, Failed: 0 (the two round-3 regression tests)
```

**Closeout CI (GitHub API; token not printed).** Run `42881426273`: name `CI`, event `workflow_dispatch`,
branch `step/05-energy-model`, `head_sha` `e6a78394359949e81ff2978433670b57fb12621e`, status `completed`,
conclusion `success`. Jobs `Detect changes`, `Public API compatibility`, `.NET (ubuntu-latest)`, and
`Contracts and policy` are `success`; `Rust` and `Python` are `skipped` by the path filters. The recent-runs
query shows no workflow run for the docs-only `fa40d6d` or `8a59107`, and no per-feature step runs.

**Scratch probes (not committed).** A read-only console harness under `/tmp/opencode/step05probe` referenced the
release `Mocassin.Core`/`Mocassin.Symmetry`/`Mocassin.Packages` assemblies and constructed in-memory models. It
did not modify the repository. Outputs:

```text
F5: base model preview raw=12; after editing the same model to 63-particle centre and surrounding sets,
    the same service instance returned preview=null with error ENM013 (a fresh service agreed). The pre-fix
    energy+structure key would have returned the stale "ok" entry.
N7: two identical zero offsets produced ENM009=1 and ENM011=1.
F1: offsets (0.1,0,0) and (1.2,0,0) produced ENM009=0 and ENM011=0 (distinct periodic images accepted).
Full composed validation of samples/ceria and samples/barium-zirconate: 0 errors, 0 warnings.
```

#### Round-2 finding verification (round 3)

| Finding | Result |
| --- | --- |
| F5 | **Fixed.** `CreateCacheKey` now includes `ComputeModelHash(model)`, a SHA-256 over `ProjectJson.SerializeCanonicalModel(model)` (`EnergyPreviewService.cs:438–452`). The canonical projection covers particles, particle sets, structure, transitions, and energy sections, so any model edit rekeys the cache. `CacheInvalidatesWhenParticleModelChanges` passes, and the scratch probe confirms the pre-fix stale-cache/`ENM013`-suppression scenario is closed for both the cost and pair entry points (the key is shared). |
| F6 | **Fixed.** `EnergyCostPreview` now documents `UniquePairInteractionCount` as the number of unique shared pair energy tables (distinct `EnergyId` values), and the user guide says "unique pair energy tables (distinct `EnergyId` values)" (`EnergyCostPreview.cs:12`; `docs/user-guide/energy.md:75–76`). The code counts distinct `EnergyId` values (`EnergyPreviewService.cs:125–128`). |
| F7 | **Fixed.** The three completed implementation-task checkboxes (`ModelDefinition`/JSON extension, `ProjectHasher.ComputeEnergy`, energy records) are marked done on the Step 05 page (lines 583–590); no unchecked implementation task remains. |
| N5 | **Addressed.** The user guide now says "distinct stable position (full periodic encoding)", matching the validator and F1 (`docs/user-guide/energy.md:41–43`). |
| N6 | **Fixed.** The §7.1 table now separates duplicate site-energy entries (`ENM014` Error) from forbidden occupancy/void-on-stable (`ENM016` Warning), matching the normative §7 table and `EnergyModelValidator` (`step-05-energy-model.md:312,357–358`; `EnergyModelValidator.cs:390–401`). |
| N7 | **Fixed.** `centerEquivalentReported` suppresses repeated centre-equivalent `ENM011` diagnostics while `ENM009` still reports the surrounding-surrounding duplicate; `TwoCentreEquivalentOffsetsReportOneEnm011` passes and the scratch probe gives one of each (`EnergyGeometryValidator.cs:228,300–309`). |

#### Whole-step re-check (round 3)

I re-checked the full step, not only the delta. Every Step 05 acceptance and standard-audit item remains
supported at `e6a7839`:

- **Environments and filters.** Inclusive `[start, end]` with the 1e-5 Å tolerance, symmetric stable filtering,
  one-directional unstable filtering, and the inherited unstable cutoff for omitted environments are implemented
  and covered by `EnergyPreviewTests` and `EnvironmentTests`.
- **Clusters and ENM coverage.** The 2/8-accept, 1/9-reject boundary, full-encoding duplicate/centre rules,
  invalid surroundings, cutoff checks, and raw-permutation limits are implemented. All 21 `ENM001`–`ENM021`
  constants are referenced by a committed positive/negative test; `ENM018` is asserted defined-but-not-emitted.
- **Preview and identities.** `GeometryId` includes `centerP`/`partnerP`; chiral directions share one `EnergyId`
  and expose `ChiralPartnerGeometryId`; the Burnside unique count is compared with a brute-force orbit count; the
  63⁹ case returns `ENM013` without overflow. Both preview entry points return `null` plus diagnostics on invalid
  input.
- **Persistence and hashing.** Energy sections survive canonical save/hash; the default environment and empty
  sections are omitted; `offsetVectors` order is preserved; sorted collections are stable; `ComputeEnergy` is
  shuffle-invariant and changes for every energy section. The generated schema accepts the energy sections and
  rejects an unknown energy field; the minimal/populated fixtures and the ceria/BZO samples match.
- **Editing and composition.** `ProjectEditor.Energy.cs` adds the environment/cluster/site-energy operations with
  cascade removal and rebase; `ProjectValidationService` composes the Core and Symmetry validators; packages,
  public API files, ApiCompat baseline, SPDX headers, and the structure policy are current.
- **Documentation and status.** The user-guide, physics, reference, project-format, and compilation-pipeline pages,
  navigation, section indexes, status dashboard, feature log, and parity rows match the implementation; the strict
  docs build passes.
- **Scope and hygiene.** The step diff is limited to Step 05 energy code, tests, contracts, samples, benchmark, and
  documentation; no Step 06/08/09/15/25 implementation, GUI, legacy import, preview persistence, or new dependency
  was added. Workflow-run history has only the three manual closeout dispatches for the step and no run for the
  documentation-only commits. The branch is unmerged and untagged; the audited commit is frozen and the HEAD delta
  is documentation-only.

One documentation-precision defect introduced by the F5 fix is recorded below as F8. It is advisory; the runtime
behaviour is correct.

#### Findings (round 3)

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **F8 — The F5 cache-key documentation update missed the public XML remark and names a nonexistent method on the step page.** The `EnergyPreviewService` class remark still says the key contains only "the canonical energy and structure hashes, the requested cell counts, and the catalog and symmetry contract versions"; it omits the new full canonical model hash. The step page says the service caches by `ProjectHasher.Compute(model)`, but `ProjectHasher.Compute` only accepts a `Project`; the preview actually uses the private `ComputeModelHash(ModelDefinition)` helper (equivalent to hashing `SerializeCanonicalModel(model)`). The user-facing docs, reference, and compilation-pipeline pages were updated correctly, and the behaviour is verified; this is a documentation-only consistency defect, not a data or contract failure. | `EnergyPreviewService.cs:23–27,438–452`; `ProjectHasher.cs:26–31`; `docs/planning/steps/step-05-energy-model.md:274–277`; updated `docs/reference/energy.md:82–84`, `docs/development/compilation-pipeline.md:134–135`, and `docs/user-guide/energy.md:75–76`; F5 above. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

#### Verdict (round 3)

- [ ] Passed
- [x] Passed with notes
- [ ] Fixes required
- [ ] Re-audit required
- [ ] Waived

Follow-up owner: Step 05 agent for F8 (fix or request explicit human acceptance); human owner for the approval
gate.
Follow-up issue/step: Step 05 — Energy Model. No Blocker or Major finding remains. F5–F7 are fixed and N5–N7 are
addressed; F8 is a Minor documentation-precision issue. The human owner must explicitly accept F8 or request a
small documentation fix; a code fix is not required. The audit result is evidence, not merge authorization.

**Verdict (round 3): Passed with notes.** The round-2 Major cache-key defect is fixed and the full stale-cache
and `ENM013`-suppression scenario is closed for both preview entry points; the round-2 Minor/Note items are fixed
or addressed. The full local gate and replacement closeout CI are green at `e6a7839`; the HEAD delta is
documentation-only. The energy model, persistence, canonical hashing, validation split, sampler, preview,
contracts/fixtures, samples, documentation, parity rows, and dashboard satisfy the frozen design, with no
out-of-scope implementation. F8 is a Minor documentation-precision defect (the public XML cache-key remark and
the step-page method name were not fully reconciled with the F5 implementation). Residual notes remain: the
benchmark is a local synthetic-proxy measurement rather than a reference-runner certification (N1); Windows and
macOS were not run under the Ubuntu-only policy because the step has no platform-specific code; the Step 08
preview-to-compiler cross-check and `ENM018` emission/GUI editors remain deferred to Steps 08/07/25; and the
record parent is the docs-only request commit `8a59107` (N2). The audit result is evidence, not merge
authorization.

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| F1 | Fixed — full encoded duplicate/centre check; regression test added | Step 05 agent | `c3c48c4` |
| F2 | Fixed — omitted unstable environment inherits stable cutoff for pair sampling; docs reconciled | Step 05 agent | `c3c48c4` |
| F3 | Fixed — boundary/filter-direction/inheritance tests and brute-force Burnside test added | Step 05 agent | `c3c48c4` |
| F4 | Fixed — unique `EnergyId` count used for pair-table count and memory | Step 05 agent | `c3c48c4` |
| N1 | Addressed — benchmark evidence labelled as synthetic proxies | Step 05 agent | `c3c48c4` |
| N2 | Acknowledged — docs-only record-parent process note | Step 05 agent | `c3c48c4` |
| N3 | Fixed — null in-memory stable environment returns diagnostics | Step 05 agent | `c3c48c4` |
| N4 | Addressed — Step 08 page records the cross-check follow-up | Step 05 agent | `c3c48c4` |
| F5 | Fixed — full canonical model hash added to cache key; particle-set invalidation test added | Step 05 agent | `e6a7839` |
| F6 | Fixed — XML doc and user guide now describe unique `EnergyId` pair tables | Step 05 agent | `e6a7839` |
| F7 | Fixed — completed task checkboxes marked done | Step 05 agent | `e6a7839` |
| N5 | Addressed — user-guide wording uses distinct stable position/full encoding | Step 05 agent | `e6a7839` |
| N6 | Fixed — §7.1 separates `ENM014` duplicate entries from `ENM016` void-on-stable warning | Step 05 agent | `e6a7839` |
| N7 | Fixed — repeated centre-equivalent offsets report one `ENM011` | Step 05 agent | `e6a7839` |
| F8 | Accepted by human owner 2026-11-10 — documentation precision only; step-page wording corrected, public XML remark retained | Step 05 agent | `315aa19` |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Approval summary

The step agent completes this section before requesting human approval. It must cover the full history, not
only the latest round.

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

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:
