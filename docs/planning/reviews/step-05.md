# Step 05 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-05.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Design
> review status`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 05 — Energy Model |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Design review status | Changes required |
| Reviewed commit | `fa45192` |
| Docs branch | `main` |
| Review date | 2026-11-09 |
| Reviewer | Independent planning auditor (fresh context; no shared conversation with the planner or implementor) |

Step page: [`Step 05 — Energy Model`](../steps/step-05-energy-model.md)

## Step summary

| Field | Value |
| --- | --- |
| Phase | B. Model definition |
| Depends on | 03, 04 |
| Delivers | Stable/unstable interaction environments, cutoff filters, multi-body clusters, and site energies, with validation and cost previews. |

## Design decisions reviewed

- [x] Step page design decisions — reviewed in full; findings M1–M5 and m1–m7.
- [x] Data contracts and format changes — reviewed `02-data-contracts.md` §1.1/§1.3/§2.2 Energy; gaps in M1, m2, m3.
- [x] Architecture and module boundaries — reviewed Core/Symmetry/Compilation ownership; validation and preview boundaries are M2/M5.
- [x] ADRs and open decisions — reviewed ADR 0009/0010/0013 and `05-open-decisions.md`; additive-v1 reconciliation is m3.
- [x] Quality strategy and test plan — reviewed `03-quality-strategy.md`; test-plan gaps are m6.
- [x] Parity matrix and documentation plan — reviewed `appendix-b-parity-matrix.md` §4 and the documented page list; gaps are m5.
- [x] Licensing and provenance — no new dependency or GPL-derived content; the missing SPDX task is m7.
- [x] Post-implementation audit plan — reviewed; step-specific closeout/audit tasks are missing (m5).

## Dependency and interface checks

- [x] Dependencies are `Done` and their contracts are stable — Step 03 and Step 04 are done/tagged; Step 05 depends only on 03 and 04.
- [ ] Inputs, outputs, types, and formats are defined — partial: canonical persistence/hash (M1), chiral identity (M3), and query/preview boundary (M5) are unresolved.
- [x] Versioning and migration behavior is defined — additive project-v1; ADR 0009 reconciliation is m3.
- [ ] No unresolved cross-step interface question remains — M1, M3, M4, and M5 remain.

## Standard design-review checklist

- [ ] Scope, non-goals, and acceptance criteria are explicit and testable — there is no non-goals section, and the “consistent with step 08” criterion cannot be tested at Step 05 closeout (m5/m6).
- [ ] Design decisions are complete and internally consistent — M2/M3/M4 and m4.
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined — m6.
- [ ] Documentation, status, parity, and audit updates are planned — m5.
- [x] Licensing, provenance, and dependency checks are resolved — no new dependency and no GPL copying; the missing SPDX task is m7.
- [ ] Risks and mitigations are realistic — the risk table omits data loss (M1), validation ownership (M2), chiral identity (M3), and cost overflow (M4).
- [ ] Code layout and namespace plan follow the structure policy — m1 and m4.
- [ ] No hidden scope, stale placeholders, or unowned decisions — stale `ProjectJsonContext.cs`, Step 07 documentation overlap, ENM018 coordination, and unreconciled filter/cluster-size choices (m1, m4, m5, n1).

## Evidence

### Review round 1

**Reviewer context.** I am a fresh planning auditor. I did not author or implement this design and have no
shared conversation with the planner or implementor. I inspected the repository, the frozen design, the
dependent step pages, and the original implementation as read-only reference myself; I did not rely on the
spawn summary.

**Frozen state and worktree.** Worktree `/workspace/mocassin`, branch `main`, `HEAD` `877c9cc`. The request
names `fa45192` as the frozen design commit. `git diff fa45192 -- docs/planning/steps/step-05-energy-model.md
docs/planning/02-data-contracts.md` produced no output, so the reviewed design content is identical at
`fa45192` and at `HEAD`. `git diff --stat fa45192..HEAD` shows only
`docs/planning/reviews/step-05.md` (the reviewed-commit field) and `docs/planning/status.md` (the frozen
commit/status text). This is a process note (n3), not a design finding.

**Commands run (from `/workspace/mocassin`):**

```sh
git status --short
# (no output: clean worktree)

git log --oneline --decorate -6
# 877c9cc (HEAD -> main, origin/main) docs: finalize step-05 design freeze [skip ci]
# 9971715 docs: record step-05 design freeze update [skip ci]
# fa45192 docs: fix step-05 energy preview input
# cd5410e docs: freeze step-05 design for review [skip ci]
# 2299348 docs: request step-05 design review [skip ci]
# 424a404 docs: prepare step-05 design for review

git show --stat fa45192
# commit fa451922c5b0a8c02da270b5ed7980e4186ea98b
# docs: fix step-05 energy preview input
#  docs/planning/steps/step-05-energy-model.md | 7 ++++---
#  1 file changed, 4 insertions(+), 3 deletions(-)

git diff fa45192 -- docs/planning/steps/step-05-energy-model.md docs/planning/02-data-contracts.md
# (no output: the frozen design is byte-identical at fa45192 and HEAD)

git diff --stat fa45192..HEAD
#  docs/planning/reviews/step-05.md | 2 +-
#  docs/planning/status.md          | 4 ++--
#  2 files changed, 3 insertions(+), 3 deletions(-)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.24s
# exit 0

python3 tools/contracts/generate.py --check
# Contract artifacts are current (17 files)
# exit 0

./tools/verify.sh structure
# == Structure ==
# Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1
# exit 0

./tools/verify.sh contracts
# Contract artifacts are current (17 files)
# space-group catalog is current: 274 settings, sha256 1f3bf67d…
# SPDX headers present in 217 source files
# No secret patterns found in 524 tracked text files
# advisories ok, bans ok, licenses ok, sources ok
# exit 0

git diff --check
# (no output, exit 0)
```

**Additional read-only checks:**

```sh
git show fa45192 -- docs/planning/steps/step-05-energy-model.md
# Shows the preview input changed from StructureModel to ModelDefinition.

git show cd5410e:docs/planning/steps/step-05-energy-model.md | sed -n '245,285p'
# Confirms the pre-fix API and that IProjectQuery/canonicalization were not part of that change.

find src/dotnet/Mocassin.Core -maxdepth 3 -type f -name '*.cs' | sort
# No ProjectJsonContext.cs; ProjectJsonContext is nested in Project/ProjectJson.cs.

grep -n "ProjectJsonContext\|class ProjectJsonConverter\|Write(" \
  src/dotnet/Mocassin.Core/Project/ProjectJson.cs
# 188:public sealed class ProjectJsonConverter : JsonConverter<Project>
# 251:    public override void Write(...)
# 410:internal sealed partial class ProjectJsonContext : JsonSerializerContext;

grep -n "CanonicalizeModel\|Model = " src/dotnet/Mocassin.Core/Project/ProjectJson.cs
# 92:    internal static ModelDefinition CanonicalizeModel(...)
# 298:            Model = ProjectJson.CanonicalizeModel(value.Model),

grep -n "SerializeCanonicalModel\|CanonicalizeModel\|Compute" \
  src/dotnet/Mocassin.Core/Project/ProjectHasher.cs
# 26: public static string Compute(Project project)
# 29: var bytes = ProjectJson.SerializeCanonicalModel(project.Model);
# 73: public static string ComputeTransition(...)

wc -l src/dotnet/Mocassin.Core/Editing/ProjectEditor.cs \
      src/dotnet/Mocassin.Core/Project/ProjectJson.cs \
      src/dotnet/Mocassin.Core/Model/ModelDefinitionJsonConverter.cs
#  506 ProjectEditor.cs
#  410 ProjectJson.cs
#  158 ModelDefinitionJsonConverter.cs

grep -n "TryCreatePreview" \
  src/dotnet/Mocassin.Symmetry/Structure/IStructurePreviewService.cs \
  src/dotnet/Mocassin.Symmetry/Transitions/ITransitionPreviewService.cs
# Both established preview services use TryCreatePreview(..., out diagnostics).

grep -rn -i "chiral" docs/planning/02-data-contracts.md \
  docs/planning/steps/step-05-energy-model.md \
  docs/planning/steps/step-08-model-compilation-pipeline.md \
  docs/planning/steps/step-09-parameterization-templates.md
# Confirms the shared-table/one-ID vs mirrored-set/PRM007 inconsistency (M3).

grep -n -i "energy\|environment\|cluster\|chiral\|cutoff" \
  docs/planning/appendix-b-parity-matrix.md docs/planning/03-quality-strategy.md
# Confirms the planned rows and the property-test expectations.

grep -n "positions per stable environment\|per unstable\|group permutation\|group size" \
  docs/planning/steps/step-05-energy-model.md
# Confirms the original limits are listed but not all are assigned diagnostics.
```

**Original reference inspected (read-only, `/workspace/mocassin-original`).** `IGroupInteraction.cs` and
`GroupInteraction.cs` (`GroupSize => GeometryVectors.Count`; surrounding positions only),
`MocassinEnergySettings.InitAsDefault` (`AtomsPerGroup` 2..8, `PositionsPerStable` 0/1/500/5000,
`PositionsPerUnstable` 0/1/100/1000, `PermutationsPerGroup` 1/1/500/5000),
`GroupInteractionValidator.cs` (`GetGroupPermutationCount` multiplies the surrounding permutation count by
the center occupation-set particle count), `GeometryGroupAnalyzer.cs` (ring = duplicate full-geometry
positions), `StableEnvironmentInfoValidator.cs`, `UnstableEnvironmentValidator.cs`, and the
stable/unstable interaction-filter implementations.

**Files reviewed.** `AGENTS.md` §3.8–§3.13, §4–§12; `docs/planning/reviews/README.md`;
`docs/planning/reviews/step-design-review-template.md`; `docs/planning/reviews/step-04.md` (for the prior
review standard and Step 04 M1 precedent); `docs/planning/steps/step-05-energy-model.md` at `fa45192`;
`docs/planning/02-data-contracts.md` (§1.1, §1.3, §2.2 Model/Particles/Structure/Transitions/Energy/
Parameterization, §3.3 compiled model); `docs/planning/01-target-architecture.md`;
`docs/planning/03-quality-strategy.md`; `docs/planning/05-open-decisions.md`;
`docs/planning/appendix-b-parity-matrix.md`; `docs/planning/status.md`; `docs/planning/audits/README.md`;
`docs/planning/adr/0009-contract-manifest-and-contracts-library.md`,
`0010-canonical-messagepack-signatures.md`, `0013-format-compatibility-and-api-versioning.md`;
`docs/development/code-structure.md`; `docs/planning/steps/step-03-particle-and-structure-models.md`;
`step-04-transition-model.md`; `step-06-lattice-and-simulation-models.md`;
`step-07-validation-and-diagnostics.md`; `step-08-model-compilation-pipeline.md`;
`step-09-parameterization-templates.md`; `step-15-energy-engine.md`; `NOTICE.md`;
`contracts/manifest.json`; `zensical.toml`; `tools/verify.sh`; `tools/structure-policy.json`;
`src/dotnet/Mocassin.Core/Model/ModelDefinition.cs`, `ModelDefinitionJsonConverter.cs`,
`Project/ProjectJson.cs`, `Project/ProjectHasher.cs`, `Project/ProjectSchema.cs`,
`Project/ProjectValidator.cs`, `Editing/ProjectEditor.cs`, `Diagnostics/*`;
`src/dotnet/Mocassin.Symmetry/Structure/IStructurePreviewService.cs`,
`Structure/StructurePreviewService.cs`, `Transitions/ITransitionPreviewService.cs`,
`Transitions/TransitionPreviewService.cs`, `Reduction/*`; and the tracked fixtures under
`tests/contracts/`.

**Scope checks.** I reviewed all fourteen areas in the request: energy sections and JSON/schema round-trip;
default `stableEnvironment`; semantic cluster-offset order and sorted collections; environment/cutoff/filter/
cluster/site-energy validation; ENM001–ENM018 ownership and the Step 07 ENM018 boundary; derived
pair/chiral preview and stable geometry IDs; cost preview and permutation limits; `IEnergyQuery` and
`IEnergyPreviewService` including the `ModelDefinition` input; Step 03/04 dependency contracts;
manifest/schema and fixture tasks; PublicAPI/ApiCompat tasks; code layout against the structure policy;
documentation/nav and test plans; and unresolved cross-step interfaces. The findings record the result.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M1 — The energy sections are not wired into the canonical model/save/hash path.** The design says to update `ModelDefinitionJsonConverter`, `ProjectJsonContext`, `ProjectSchema`, and `ProjectValidator`, but it never mentions `ProjectJson.CanonicalizeModel`, `ProjectJsonConverter.Write`, `ProjectStore.SaveAsync`, or `ProjectHasher.Compute`. `Write` replaces the model with `CanonicalizeModel`, and `Compute` hashes `SerializeCanonicalModel`; today `CanonicalizeModel` reconstructs a `ModelDefinition` containing only the Step 03/04 sections. Unless it is extended, the energy sections are dropped on save and are absent from the model/document hashes, and Step 08’s cache key (“energy model hash + structure hash”) has no defined producer. This is the same defect Step 04 M1 identified for transitions. The design also does not say whether the default `stableEnvironment` is always written (changing every existing model/document hash) or omitted when empty, unlike the Step 04 rule that omits empty optional transition sections. | Step page lines 66–80 and 326–340; `ProjectJson.cs:92–121,298`; `ProjectHasher.cs:26–29,80–83`; Step 08 line 270; Step 04 page lines 101–106; `ModelDefinitionJsonConverter.cs` current `WriteArrayIfNotEmpty` behaviour. |
| **Major** | **M2 — Validation ownership across Core and Symmetry is missing, so several ENM rules cannot be implemented in the planned Core-only validator.** The design puts all ENM001–ENM018 constants in `Mocassin.Core.Diagnostics` and all validation in one `Mocassin.Core.Model.Energy.EnergyModelValidator`; it defines no Symmetry-side energy validator or composition step. ENM002 (cutoff versus cell dimensions), ENM009 (duplicate offsets after periodic trimming), ENM010 (cluster geometry versus the environment cutoff), ENM011 (ring/degenerate periodic geometry), and ENM017 (sampling estimate) need catalog/cell geometry, minimum-image distances, or the exact grid; the architecture assigns catalog-dependent validation and cell geometry to `Mocassin.Symmetry`, and Core must not reference Symmetry. Step 03 §5 and Step 04 §7 define explicit Core/Symmetry validation ownership; Step 05 does not. | Step page lines 181–205 and 282–311; `01-target-architecture.md` lines 121–142 (Core must not reference Symmetry; Symmetry owns cell geometry/catalog validation); Step 03 lines 355–361; Step 04 lines 352–363. |
| **Major** | **M3 — Chiral pair identity and linkage are inconsistent across Step 05, Step 08, Step 09, and the data contract.** Step 05 says chiral partners “share one energy table” while the parameterization editor “mirrors values”, and derives stable IDs from the two site IDs, periodic offset, and a canonical geometry hash. Step 08 says to link both directions to one energy table and record a chiral partner index. The data contract says chiral partners share one `object_id` and one energy table and refuses independent values. Step 09 derives pair-set IDs from `(centerSiteId, partnerSiteId, canonicalOffset, distanceBucket)` and still keeps a chiral partner index and a `PRM007` warning when values differ. A shared table/one ID is incompatible with two mirrored sets and with a center/partner-derived ID that differs for A–B and B–A. `PairInteractionPreview` exposes only `IsChiral`, not the partner ID/link, so the acceptance criterion “partners are detected and cannot receive independent energies” cannot be exercised through the preview API. | Step page lines 159–163, 247–252, 351; Step 08 lines 144–149, 155; Step 09 lines 78–80, 118, 180; `02-data-contracts.md` lines 500–501, 693. |
| **Major** | **M4 — Cost preview and permutation limits are incomplete and numerically incorrect.** Section 6 computes raw cluster permutations as “product of surrounding particle-set sizes”, omitting the center site’s particle count; the original multiplies the surrounding permutation count by the center occupation-set size. `ClusterCostPreview.RawPermutationCount` and `UniquePermutationCount` are `int` even though up to 8 surrounding sets of up to 63 particles plus the center can exceed `int.MaxValue`; the original uses `long`. The original stable/unstable position-count limits (warn 500/max 5000 and warn 100/max 1000) and the groups-per-position limit are not assigned to any diagnostic; ENM017 is an unquantified warning with no hard error, and ENM012/ENM013 cover only cluster permutations. The preview is also said to use “the same sampling code as compilation (step 08)” without naming where that shared kernel lives, and the acceptance criterion “consistent with step 08” cannot be tested at Step 05 closeout. | Step page lines 40–41, 167–179, 196–201, 233–245; original `GroupInteractionValidator.GetGroupPermutationCount`, `MocassinEnergySettings.InitAsDefault`, and `AtomsPerGroup`/`PositionsPerStable`/`PositionsPerUnstable`; Step 08 line 270. |
| **Major** | **M5 — `IEnergyQuery`/`IEnergyPreviewService` do not integrate with the established query and preview boundary.** Step 05 says `IEnergyQuery` is implemented by `Project` but never adds it to `IProjectQuery` (`IProjectQuery : IParticleQuery, IStructureQuery, ITransitionQuery`), so the same-snapshot query composition has no energy section. `IEnergyPreviewService` takes `ModelDefinition` and returns non-null records with no diagnostics; the Step 03 and Step 04 preview services use `TryCreatePreview(..., out ImmutableArray<Diagnostic>)`, return `null` on invalid user data, and never throw. `EnergyCostPreview.WarningCount` is not a diagnostic result, so ENM012/ENM013/ENM017 cannot be surfaced through the preview, and there is no cache key or symmetry-version input analogous to the structure/transition preview services. | Step page lines 218–275; Step 04 lines 441–499; Step 03 lines 404–440; `IStructurePreviewService.cs` lines 13–39; `ITransitionPreviewService.cs` lines 13–49. |
| **Minor** | **m1 — The code-layout/task list names nonexistent or unnecessary files and does not plan a split for a near-limit file.** It lists `Mocassin.Core/Project/ProjectJsonContext.cs`, but `ProjectJsonContext` is a nested internal type in `Project/ProjectJson.cs`; there is no such file. `ProjectSchema.cs` contains only `CurrentVersion` and `SchemaUri` and needs no change while `schemaVersion` stays 1. `ProjectEditor.cs` is already 506 lines and Step 05 adds four editing groups without a partial/aspect split; the single `EnergyModelValidator.cs` is also unplanned for the 800-line limit. | Step page lines 282–311 and 326–340; `ProjectJson.cs:410`; `ProjectSchema.cs`; `ProjectEditor.cs` 506 lines; `code-structure.md` limits. |
| **Minor** | **m2 — The data contract and step page disagree on energy defaults and collection ordering.** The contract does not state that a missing `stableEnvironment` loads the 1.0 Å/no-filter default, does not state the write/omission rule for empty energy sections, and does not state the `InteractionFilter` sort key required by Step 05 §1. The generic contract rule “all other arrays are sorted by `id`” cannot apply to filters or site-energy entries, which have no `id`. | Step page lines 71–78; `02-data-contracts.md` lines 17–20, 165–169, 382–389. |
| **Minor** | **m3 — Manifest, fixture, and version-bump tasks are incomplete.** The task updates `contracts/manifest.json` and regenerates the schema but does not update the manifest `status`/`notes`, name the new fixture IDs in `fixtures`, or say whether canonical signature vectors change. ADR 0009 requires a version bump for a manifest change; the design relies on the Step 03/04 additive-v1 precedent but does not cite or reconcile it. | Step page lines 337–340; `contracts/manifest.json` (`status: step-04`, notes still say energy is extended by later steps); ADR 0009 rules; data contract lines 20–21. |
| **Minor** | **m4 — Several validation rules are ambiguous or diverge from the original without a deviation note.** ENM001/ENM002 cover only the stable cutoff; unstable-environment cutoffs have no diagnostic even though §2 applies the positive/within-cell rule to all cutoffs. ENM008 permits one offset, while the original `AtomsPerGroup` range 2..8 applies to surrounding positions and the page’s own “size 2–8 (center + up to 8 surroundings = up to 9 positions)” wording is not reconciled. ENM011 “ring/degenerate” is not defined (a star has no ring graph), and a zero or center-equivalent offset is not explicitly rejected by ENM009. The filter range `[Start, End)` differs from the original inclusive end, and unstable filters are declared symmetric although the original unstable filter is one-directional. Site-energy duplicate entries have no rule. The original emitted warnings for several of these conditions; Step 05 makes them errors without a deviation note. | Step page lines 104–107, 120–130, 183–205; original `MocassinEnergySettings.InitAsDefault`, `GeometryGroupAnalyzer.ContainsRing`, `StableInteractionFilter.IsApplicable`, `UnstableInteractionFilter.IsApplicable`, `StableEnvironmentInfoValidator`, `UnstableEnvironmentValidator`. |
| **Minor** | **m5 — Documentation, status, parity, and audit planning is incomplete and overlaps Step 07.** `docs/troubleshooting/errors.md` is listed as a Step 05 page, but Step 04 and Step 07 explicitly assign it to Step 07; the task also mentions “building” pages that do not exist. There is no parity-matrix update task, no status-dashboard/closeout/audit task, and no update to the `docs/user-guide/index.md`, `docs/physics/index.md`, or `docs/reference/index.md` lists (Step 04 did all of these). The page has no non-goals section. | Step page lines 324–342, 357–367; Step 04 lines 644–648, 686–693; Step 07 Documentation section; `zensical.toml` current navigation. |
| **Minor** | **m6 — The test plan lacks tiers, per-code tests, and key regression/property cases.** It declares no PR-fast/PR-medium/nightly tiers and no benchmark/target for the sampling/cost-preview hot path. It does not require positive/negative tests for each Step 05 ENM code, a default-`stableEnvironment`/missing-section test, an empty-section/hash-stability test, filter-order or site-energy-duplicate tests, or a permutation-overflow test. The “consistent with step 08” criterion is not testable at Step 05 closeout, and there is no property test for permutation reduction beyond pair counts. | Step page lines 344–355, 369–383; `03-quality-strategy.md` §§5, 8, 9.3; Step 04 test-plan precedent. |
| **Minor** | **m7 — The licensing/provenance task is missing.** Step 03 and Step 04 both require `SPDX-License-Identifier: MIT` on new files and record that no new dependency is introduced. Step 05 lists no such task, even though it adds many new source and test files and derives algorithms from the original only as reference. | Step page lines 324–343; Step 03 line 569; Step 04 line 635; `NOTICE.md`. |
| **Note** | **n1 — ENM018 ownership is coherent but coordination is implicit.** Step 05 includes ENM018 in its table and code range, while saying Step 07 emits it; Step 07 says the same. The design should state explicitly that Step 05 defines the constant/help anchor and Step 07 owns emission and the consolidated diagnostics documentation, so the two steps do not both claim it. | Step page lines 202–205, 336; Step 07 lines 83–84; Step 04 lines 406–408. |
| **Note** | **n2 — The default cutoff may warn on the default cell.** A missing `stableEnvironment` defaults to a 1.0 Å cutoff, but the default structure is `a=b=c=1 Å`; ENM002 warns when the cutoff exceeds half the smallest cell dimension, so every new empty project would emit a warning. Confirm this is intended or align the default with the default cell. | Step page lines 70, 104, 186; `02-data-contracts.md` line 169. |
| **Note** | **n3 — Record parent.** `HEAD` is `877c9cc`; `fa45192..HEAD` changes only the review record’s reviewed-commit field and the status-dashboard freeze text. The record commit’s parent is therefore `HEAD`, not the reviewed commit. This is a process note for the human owner, not a design finding. | Commands and output above. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Re-review required
- [ ] Waived

Follow-up owner: Planner and Step 05 agent (design revision and finding resolution)
Follow-up step: Step 05 — Energy Model; a fresh re-review is required after the design commit

**Verdict: Changes required.** Five Major findings remain (M1–M5). They cover data persistence and
signature integrity, Core/Symmetry validation ownership, chiral pair identity and preview linkage, cost
preview and permutation limits, and the energy query/preview boundary. No Blocker is recorded, but M1 can
silently drop authored energy data on save and M2/M3/M4 leave required behaviour without an implementable
contract. The step must not start until a new design commit resolves these findings and a fresh auditor
re-reviews it. The documentation build, contract generation, structure test, contract/policy gate, and
whitespace check all pass at the frozen design; the findings are design-completeness findings, not tooling
failures.

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
