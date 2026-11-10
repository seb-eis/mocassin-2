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
| Audited commit | `d23fbd6` |
| Step branch | `step/05-energy-model` |
| Step tag | — |
| Closeout CI run | [`42874483715`](https://github.com/seb-eis/mocassin-2/actions/runs/42874483715) — green at `d23fbd6` |
| Platform CI | Not run (Ubuntu-only policy; no platform-specific code) |
| Audit date | 2026-11-10 |
| Auditor | Independent audit agent (fresh context; no shared conversation with the planner or implementor) |

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
- Benchmark: local short-run mean ceria 88.9 µs and barium zirconate 77.4 µs, below the Step 05 targets.
- Known deferrals: compiled pair/cluster tables and the preview-to-compiler cross-check belong to Step 08;
  consolidated `ENM` diagnostics and `ENM018` emission belong to Step 07; energy GUI editors belong to Step 25.

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

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

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
