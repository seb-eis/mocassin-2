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
| Design review status | Re-review required |
| Reviewed commit | `0b2a571` |
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

- [x] Requested decision and frozen commit recorded — approve the Step 05 start gate at `0b2a571`
- [x] Every review/audit round recorded (round, date, commit, reviewer, verdict)
- [x] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status)
- [ ] Accepted notes and waivers recorded with rationale — m14, m15, and n12 were verified in round 4; m16 is
  fixed at `0b2a571` and the round-5 re-review is pending; n11/n13 are process notes; no waiver is requested
- [x] Residual risks, deferred items, and follow-up owners recorded
- [x] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived — no Blocker or Major
  remains; six Majors (M1–M6) are closed
- [x] Final CI and documentation-build results recorded — design phase; strict docs, contracts, structure,
  and contract/policy gates pass at `0b2a571`; no step CI exists yet

Round history:

| Round | Date | Commit | Reviewer | Verdict |
| --- | --- | --- | --- | --- |
| 1 | 2026-11-09 | `fa45192` | Independent planning auditor (fresh context) | Changes required |
| 2 | 2026-11-09 | `0000f3f` | Fresh independent planning auditor | Changes required |
| 3 | 2026-11-09 | `6b8d78c` | Fresh independent planning auditor | Ready with notes |
| 4 | 2026-11-09 | `2f43a19` | Fresh independent planning auditor (focused delta) | Ready with notes |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| M1 | Major | Energy sections not wired into canonical save/model/document hash | Extended `CanonicalizeModel`/`SerializeCanonicalModel`, added `ComputeEnergy`, omission rules | `0000f3f` | Closed |
| M2 | Major | No Core/Symmetry validation ownership split | Split validators; `ProjectValidationService` composes; codes stay in Core | `0000f3f` | Closed |
| M3 | Major | Chiral identity inconsistent across Steps 05/08/09 and contract | Shared `EnergyId`; per-direction `GeometryId`; Step 08/09/contract aligned; `PRM007` retired | `0000f3f` | Closed |
| M4 | Major | Cost preview omitted center factor, used `int`, missed limits | Center factor, `long` counts, Burnside count, full limit table, diagnostics | `0000f3f` | Closed |
| M5 | Major | Query/preview boundary not integrated | `IProjectQuery : IEnergyQuery`; `TryCreatePreview(..., out diagnostics)`; cache key | `0000f3f` | Closed |
| m1 | Minor | Layout named nonexistent/unnecessary files | Corrected `ProjectJsonContext`, removed `ProjectSchema` change, added partials | `0000f3f` | Closed |
| m2 | Minor | Contract lacked defaults/omission/filter ordering | Added defaults, omission, sort keys, filter semantics | `0000f3f` | Closed |
| m3 | Minor | Manifest/fixture/version task incomplete | Added status/notes, named fixtures, vectors, ADR 0009 reconciliation | `0000f3f` | Closed |
| m4 | Minor | Validation semantics ambiguous/divergent | Defined unstable cutoffs, 2..8 offsets, encoder dedup, ring, inclusive filters | `0000f3f` | Closed |
| m5 | Minor | Documentation/status/parity/audit plan incomplete | Added non-goals, index/nav/status/parity/audit tasks | `0000f3f` | Closed |
| m6 | Minor | Test plan lacked tiers/per-code/property cases | Added tiers, per-ENM, default/hash/order/overflow, benchmark | `0000f3f` | Closed |
| m7 | Minor | SPDX/licensing task missing | Added SPDX/no-dependency task and criterion | `0000f3f` | Closed |
| n1 | Note | ENM018 ownership implicit | Documented Step 05 constant/help anchor, Step 07 emission | `0000f3f` | Closed |
| n2 | Note | Default cutoff warns on default cell | Suppressed ENM002/sampling warnings for empty model | `0000f3f` | Closed |
| n3 | Note | Record parent process note | Acknowledged; round-1 text unchanged | `0000f3f` | Acknowledged |
| M6 | Major | `GeometryId` omitted P1 position indices | Added `centerP`/`partnerP` to projection, preview, compiled contract | `6b8d78c` | Closed |
| m8 | Minor | ENM007 contradicted stable/unstable centers | Corrected ENM007; only surroundings must be stable | `6b8d78c` | Closed |
| m9 | Minor | Stale chiral mirroring/conflict requirements | Replaced with shared-`EnergyId`/linked-editing wording | `6b8d78c` | Closed in live design; recorded as m14 |
| m10 | Minor | Step 07 statistics still `int` | Changed P1/stable/unstable/pair/cluster counts to `long` | `6b8d78c` | Closed |
| m11 | Minor | No validation-deviation record | Added §7.1, including promoted and retained warnings | `6b8d78c` | Closed; omission recorded as m15 |
| m12 | Minor | Docs linked to nonexistent pages / duplicated ENM catalog | Removed premature links; reference page defers ENM catalog to Step 07 | `6b8d78c` | Closed |
| m13 | Minor | Diagnostics test tier conflicted with PR-fast definition | Split Core (PR-fast) and Symmetry (PR-medium) rows | `6b8d78c` | Closed |
| n4 | Note | Generic §1 ordering rule over-broad | Qualified identifiable arrays and energy-specific keys | `6b8d78c` | Closed |
| n5 | Note | Schema-generation wording inaccurate | Stated canonicalization applies to save/hash; schema comes from manifest | `6b8d78c` | Closed |
| n6 | Note | Status dashboard stale at frozen commit | Request commit updates dashboard; process note | `6b8d78c` | Acknowledged |
| n7 | Note | Bare `double` energy limits | Changed to `EnergyEv` `MinEnergy`/`MaxEnergy` | `6b8d78c` | Closed |
| n8 | Note | Sampler invalid-input contract unspecified | `TrySamplePairs(..., out pairs, out diagnostics)`; false/empty on invalid | `6b8d78c` | Closed |
| n9 | Note | ENM009/ENM011 overlap | Split duplicate-offset vs center-equivalent/full-geometry ownership | `6b8d78c` | Closed |
| n10 | Note | P1PositionCount description ambiguous | Clarified §6 as P1-expanded positions | `6b8d78c` | Closed; naming note n12 remains |
| m14 | Minor | Pre-seeded Step 05/09/26 audit checklists stale | Refreshed all three checklists to the revised acceptance criteria | `2f43a19` | Closed — round 4 verified |
| m15 | Minor | §7.1 omits ENM013 and misstates site-energy-range origin | Added ENM003/ENM010/ENM013 rows; distinguished promoted warnings from new v1 checks and corrected the site-energy origin | `2f43a19` | Closed — round 4 verified |
| n12 | Note | `P1PositionCount` naming ambiguous across Steps 05/07 | Renamed to `StableP1PositionCount`; Step 07 documents the total-vs-stable distinction | `2f43a19` | Closed — round 4 verified |
| m16 | Minor | §7.1 promoted-vs-new classification internally inconsistent | Split ENM014/ENM015 rows, added ENM001/ENM002, made the new-check list explicitly illustrative | `0b2a571` | Resolved — round-5 re-review pending |
| n11 | Note | Record parent process note | Acknowledged; round-3 record commit parent is `1eb29ef` | `76ae25f` | Acknowledged |
| n13 | Note | Record parent process note | Acknowledged; round-4 record commit parent is `9484eec` | `e57bbf3` | Acknowledged |

Accepted notes and waivers:

- **m14, m15, n12:** fixed at `2f43a19` and verified in round 4. No waiver is requested.
- **m16:** fixed at `0b2a571`; the focused round-5 re-review verifies the correction. No waiver is requested.
- **n11, n13:** process notes only; no action.

Residual risks, deferred items, and follow-up owners:

- **m14:** resolved by refreshing the pre-seeded audit-request checklists in `docs/planning/audits/step-05.md`,
  `step-09.md`, and `step-26.md`. The Step 05/09/26 agents must still keep them current at audit freeze.
- **m15:** resolved by the corrected §7.1 table and verified in round 4; no further action.
- **n12:** resolved by renaming to `StableP1PositionCount` and documenting the Step 07 total-vs-stable
  distinction; no further action.
- **m16:** resolved by the §7.1 classification split at `0b2a571`; the round-5 review verifies it.
- **n11, n13:** process notes about the record parent; no action.
- **Deferred to later steps:** compiled tables and indices (08), template binding (09), solver engine (15),
  editors (25), consolidated diagnostics/troubleshooting pages and ENM018 emission (07).

CI and documentation results at the frozen design `0b2a571`:

- `zensical build --strict` — pass, exit 0.
- `python3 tools/contracts/generate.py --check` — pass, 17 artifacts current.
- `./tools/verify.sh structure` — pass, 1/1.
- `./tools/verify.sh contracts` — pass (catalog, SPDX, secret scan, advisories, licences, sources).
- `git diff --check` — pass.
- No step CI exists; this is the pre-implementation design gate.

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
| M1 | Resolved — awaiting re-review | Planner / Step 05 agent | `0000f3f` |
| M2 | Resolved — awaiting re-review | Planner / Step 05 agent | `0000f3f` |
| M3 | Resolved — awaiting re-review | Planner / Step 05 agent | `0000f3f` |
| M4 | Resolved — awaiting re-review | Planner / Step 05 agent | `0000f3f` |
| M5 | Resolved — awaiting re-review | Planner / Step 05 agent | `0000f3f` |
| m1 | Resolved — awaiting re-review | Planner / Step 05 agent | `0000f3f` |
| m2 | Resolved — awaiting re-review | Planner / Step 05 agent | `0000f3f` |
| m3 | Resolved — awaiting re-review | Planner / Step 05 agent | `0000f3f` |
| m4 | Resolved — awaiting re-review | Planner / Step 05 agent | `0000f3f` |
| m5 | Resolved — awaiting re-review | Planner / Step 05 agent | `0000f3f` |
| m6 | Resolved — awaiting re-review | Planner / Step 05 agent | `0000f3f` |
| m7 | Resolved — awaiting re-review | Planner / Step 05 agent | `0000f3f` |
| n1 | Resolved — awaiting re-review | Planner / Step 05 agent | `0000f3f` |
| n2 | Resolved — awaiting re-review | Planner / Step 05 agent | `0000f3f` |
| n3 | Acknowledged — awaiting re-review | Planner / Step 05 agent | `0000f3f` |

Resolution summary (design content at `0000f3f`):

- **M1.** `ProjectJson.CanonicalizeModel`/`SerializeCanonicalModel` now own the energy sections; save and both
  document/model hashes include them. Added `ProjectHasher.ComputeEnergy` for Step 08's cache key. The default
  `stableEnvironment` and empty sections are omitted on write, preserving pre-Step-05 hashes.
- **M2.** Split validation ownership: Core owns `ENM001`, `ENM003`–`ENM008`, `ENM014`–`ENM016`, `ENM020`,
  `ENM021`; Symmetry owns `ENM002`, `ENM009`–`ENM013`, `ENM017`, `ENM019`; `ProjectValidationService` composes
  them. All codes remain defined once in Core.
- **M3.** Every directed pair has a `GeometryId`; chiral partners share one `EnergyId` and one energy table.
  `PairInteractionPreview` exposes `GeometryId`, `EnergyId`, and `ChiralPartnerGeometryId`. Step 08/09 and the
  data contract now use the shared `EnergyId`; Step 09 stores one value per chiral pair and retires `PRM007`.
- **M4.** Cost preview uses `centerParticleCount × Π(surrounding sizes)`, `long` counts, Burnside unique
  permutations, the full warning/hard limit table, and diagnostic results. Named the shared
  `IEnergyGeometrySampler` kernel and the Step 08 deferral.
- **M5.** `IProjectQuery` now extends `IEnergyQuery`; the preview service uses
  `TryCreatePreview(..., out diagnostics)`, returns `null` on invalid data, and caches by energy hash,
  structure hash, cell counts, and catalog/contract versions.
- **m1.** Corrected the layout: `ProjectJsonContext` is nested in `Project/ProjectJson.cs`,
  `ProjectSchema.cs` is unchanged, `ProjectEditor` gains a partial energy file, and `EnergyModelValidator`
  may split.
- **m2.** Data contract now states the default/omission rules, canonical sort keys, stable/unstable filter
  semantics, inclusive boundaries, and surrounding-offset range.
- **m3.** Manifest task now sets `status`/`notes`, names the `project-energy-minimal` and
  `project-energy-populated` fixtures, updates vectors, and reconciles ADR 0009 with the additive-v1 precedent.
- **m4.** Defined unstable-cutoff validation, 2..8 surrounding offsets, encoder-based periodic dedup,
  center-equivalent/ring semantics, inclusive filter boundaries, one-directional unstable filters, duplicate
  site-energy entries, and the deviation notes.
- **m5.** Added non-goals; moved error guidance into the user guide, kept `troubleshooting/errors.md` with
  Step 07; added nav/index/status/parity/audit tasks.
- **m6.** Added PR-fast/PR-medium/nightly tiers, per-ENM coverage, default/hash/order/overflow tests, and an
  energy-preview benchmark target.
- **m7.** Added the SPDX/no-new-dependency task and acceptance criterion.
- **n1.** Documented that Step 05 defines the `ENM018` constant/help anchor and Step 07 owns emission and
  documentation; updated the Step 07 page.
- **n2.** Suppressed `ENM002` and sampling warnings for the empty default model; documented and tested.
- **n3.** Process note only; the reviewer's round-1 text is unchanged.

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Re-review round 2

### Re-review request

| Field | Value |
| --- | --- |
| Step | 05 — Energy Model |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Design review status | Re-review required |
| Reviewed commit | `0000f3f` |
| Docs branch | `main` |
| Review date | 2026-11-09 |
| Reviewer | Fresh independent planning auditor (round 2; no shared conversation with the planner or implementor) |

Step page: [`Step 05 — Energy Model`](../steps/step-05-energy-model.md)

The design content for round 2 is frozen at `0000f3f`. The delta from the round-1 commit `fa45192` is the fix
round recorded in the Resolution status section, the data-contract update, and the Step 07/08/09 interface
updates. The review request and status changes are metadata only. Verify the delta yourself with
`git diff --stat fa45192..0000f3f` and inspect the files at `0000f3f`.

Reviewer: append your independence statement, evidence, findings, and verdict for this round below. Keep the
round-1 text unchanged. This is a re-review of the full design at the new commit, not only a diff check; verify
that M1–M5, m1–m7, and n1–n2 are resolved and that no new contradiction was introduced.

### Reviewer context

I am a fresh planning auditor for round 2. I did not author or implement this design and have no shared
conversation with the planner, the implementor, or the round-1 reviewer. I reviewed the full design at the
frozen commit `0000f3f`, not only the delta. I read `AGENTS.md` §3.8–§3.13 and §4–§12,
`docs/planning/reviews/README.md`, the request record, the Step 05 page, the Step 02/03/04/06/07/08/09/15/25/26
pages, the data contract, target architecture, quality strategy, open decisions, parity matrix, status
dashboard, audit plan, ADR 0001/0009/0010/0013, `NOTICE.md`, `contracts/manifest.json`, `zensical.toml`,
`tools/verify.sh`, `tools/structure-policy.json`, the relevant Core/Symmetry/Packages source, the structure
checker, and the original `GroupInteractionValidator`/`GeometryGroupAnalyzer`/`MocassinEnergySettings` as
read-only reference. I did not rely on the round-1 summary or on the resolution text alone.

**Frozen state.** Worktree `/workspace/mocassin`, branch `main`, `HEAD` `4c1cd2f` at review time; the worktree
was clean. The frozen design is `0000f3f`; `fa45192..0000f3f` changes the Step 05 page, data contract, parity
row, status freeze text, and Step 07/08/09 pages, plus the review-record resolution/request metadata. The
round-2 request commit `4c1cd2f` sits after the frozen commit, so this record commit's parent is `4c1cd2f`,
not `0000f3f` (process note n6).

### Round 2 checklist

- [x] ModelDefinition energy sections and JSON/schema round-trip — the flat sections, converter,
  canonicalization, manifest/schema, and fixtures are specified.
- [x] Default `stableEnvironment`, omission rules, and hash stability — verified against the actual
  `ProjectJson`/`ProjectHasher`/`ProjectStore` path; default and no-energy models keep their hashes.
- [x] Semantic cluster-offset order and sorted collections — `offsetVectors` order is semantic; the other sort
  keys are stated. The generic §1 ordering rule is still over-broad (n4).
- [ ] Environment, cutoff, filter, cluster, and site-energy validation — substantially complete, but ENM007
  contradicts the stable-or-unstable center rule (m8), the original-warning-to-error changes have no recorded
  deviation note (m11), and ENM009/ENM011 overlap for duplicate offsets (n9).
- [x] ENM001–ENM021 ownership, Step 07 ENM018 boundary, and Core/Symmetry split — ownership is coherent;
  ENM018 is Step 07-owned and Step 05 defines only the constant/help anchor.
- [ ] Derived pair/chiral preview, GeometryId/EnergyId, and stable geometry IDs — the identity model is coherent,
  but the `GeometryId` projection omits the P1 position index (M6).
- [x] Cost preview, center-particle factor, long counts, and permutation/position limits — fixed in Step 05;
  Step 07's statistics types are stale (m10).
- [x] IEnergyQuery, IProjectQuery, IEnergyPreviewService, and IEnergyGeometrySampler (including
  ModelDefinition input) — matches the established query/preview boundary.
- [x] Step 03 structure/site and Step 04 transition dependencies — reused without a new Core→Symmetry edge.
- [ ] Step 08/09 cross-step alignment for shared `EnergyId` and retired `PRM007` — Step 08/09 and the data
  contract use the shared ID, but Step 09/26 and the quality strategy still require chiral mirroring and
  conflict detection (m9).
- [x] Manifest/schema and fixture tasks — status/notes/fields/fixtures/vectors/schema are named.
- [x] PublicAPI/ApiCompat tasks — named for Core, Packages, and Symmetry.
- [x] Code layout against the structure policy — `ProjectEditor.Energy.cs` and
  `EnergyModelValidator.Clusters.cs` satisfy the `Type.Aspect.cs` rule; folder counts are under the limits.
- [ ] Documentation/nav, status, parity, and audit plans — mostly planned, but the Step 05 user guide would
  link to a Step 07 page that does not exist yet, and the ENM catalog is duplicated (m12).
- [ ] No unresolved cross-step interface question — M6, m9, and m10 contradict the “no unresolved” statement.

### Evidence

**Commands run (from `/workspace/mocassin`):**

```sh
git status --short
# (no output: clean worktree)

git log --oneline --decorate -8
# 4c1cd2f (HEAD -> main) docs: request step-05 round-2 design re-review [skip ci]
# 0000f3f docs: revise step-05 energy design for review findings [skip ci]
# 4bf7f66 docs: record step-05 design review Changes required [skip ci]
# 877c9cc (origin/main) docs: finalize step-05 design freeze [skip ci]
# 9971715 docs: record step-05 design freeze update [skip ci]
# fa45192 docs: fix step-05 energy preview input
# cd5410e docs: freeze step-05 design for review [skip ci]
# 2299348 docs: request step-05 design review [skip ci]

git show --stat 0000f3f
# docs/planning/02-data-contracts.md                 |  45 +-
# docs/planning/appendix-b-parity-matrix.md          |   2 +-
# docs/planning/steps/step-05-energy-model.md        | 493 +++++++++++++++------
# docs/planning/steps/step-07-validation-and-diagnostics.md |   7 +-
# docs/planning/steps/step-08-model-compilation-pipeline.md |  17 +-
# docs/planning/steps/step-09-parameterization-templates.md |   9 +-
# 6 files changed, 426 insertions(+), 147 deletions(-)

git diff --stat fa45192..0000f3f
# docs/planning/02-data-contracts.md                 |  45 +-
# docs/planning/appendix-b-parity-matrix.md          |   2 +-
# docs/planning/reviews/step-05.md                   | 244 ++++++++--
# docs/planning/status.md                            |   4 +-
# docs/planning/steps/step-05-energy-model.md        | 493 +++++++++++++++------
# docs/planning/steps/step-07-validation-and-diagnostics.md |   7 +-
# docs/planning/steps/step-08-model-compilation-pipeline.md |  17 +-
# docs/planning/steps/step-09-parameterization-templates.md |   9 +-
# 8 files changed, 641 insertions(+), 180 deletions(-)

git diff fa45192..0000f3f -- docs/planning
# Reviewed in full: Step 05 page, data contract, parity row, status freeze text,
# Step 07/08/09 interface updates, and the review-record resolution/request metadata.

zensical build --strict
# Build started
# No issues found
# Build finished in 0.25s
# exit 0

python3 tools/contracts/generate.py --check
# Contract artifacts are current (17 files)
# exit 0

./tools/verify.sh structure
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

**Read-only checks.**

```sh
sed -n '80,130p' src/dotnet/Mocassin.Core/Project/ProjectJson.cs
# CanonicalizeModel currently rebuilds only the Step 03/04 sections; the M1 fix target is real.
sed -n '250,310p' src/dotnet/Mocassin.Core/Project/ProjectJson.cs
# ProjectJsonConverter.Write replaces the model with CanonicalizeModel.
sed -n '130,180p' src/dotnet/Mocassin.Packages/Projects/ProjectStore.cs
# ProjectStore.SaveAsync serializes through ProjectJson.SerializeToUtf8Bytes, so the converter applies.
cat src/dotnet/Mocassin.Core/Project/ProjectHasher.cs
# Compute/ComputeDocument use the canonical model/document; ComputeStructure exists.
cat src/dotnet/Mocassin.Core/Project/IProjectQuery.cs
# IProjectQuery is currently IParticleQuery, IStructureQuery, ITransitionQuery.
sed -n '1,80p' src/dotnet/Mocassin.Symmetry/Structure/IStructurePreviewService.cs
sed -n '1,80p' src/dotnet/Mocassin.Symmetry/Transitions/ITransitionPreviewService.cs
# Both established previews use TryCreatePreview(..., out diagnostics).
wc -l src/dotnet/Mocassin.Core/Editing/ProjectEditor.cs
# 506 src/dotnet/Mocassin.Core/Editing/ProjectEditor.cs
sed -n '100,180p' src/dotnet/Mocassin.Architecture.Tests/StructureChecks/StructureChecker.cs
# Type.Aspect.cs names (stem.StartsWith(type + ".")) are accepted by the structure policy.
sed -n '45,70p' src/dotnet/Mocassin.Core/Model/Structure/StructureModel.cs
# StructureModel.Default has no sites, so the n2 suppression is effective for the empty model.
git show 0000f3f:docs/planning/reviews/step-05.md | sed -n '250,270p'
# Round-1 verdict text is unchanged at 0000f3f.
git show 0000f3f:docs/planning/status.md | sed -n '50,54p;79,84p'
# At 0000f3f the dashboard still says Ready for review at fa45192; 4c1cd2f updates it.
grep -n "chiral mirroring\|Mirrored values and conflict\|Chiral conflict\|chiral conflicts" \
  docs/planning/steps/step-09-parameterization-templates.md \
  docs/planning/steps/step-26-parameterization-job-templates-and-build-ui.md \
  docs/planning/03-quality-strategy.md
# Confirms stale mirroring/conflict requirements at Step 09:193,207,217,230,241; Step 26:31,79,187;
# quality strategy:112.
grep -n "EstimatedUniqueClusterPermutations\|EstimatedPairInteractionCount\|P1PositionCount\|StablePositionCount" \
  docs/planning/steps/step-07-validation-and-diagnostics.md
# Step 07 still uses int for counts Step 05 now defines as long.
ls docs/troubleshooting
# index.md only; docs/troubleshooting/errors.md does not exist yet (Step 07 creates it).
grep -n "GeometryId\|targetP\|vector4" docs/planning/steps/step-02-symmetry-and-geometry.md \
  docs/planning/steps/step-08-model-compilation-pipeline.md docs/planning/02-data-contracts.md
# Pair shells use Vector4I (cellA, cellB, cellC, P); Step 08 orders pair tables by targetP; the
# Step 05 GeometryId tuple has no P component.
```

**Original reference (read-only, `/workspace/mocassin-original`).** `GroupInteractionValidator.cs`
(`AddDefinedPositionValidation` accepts both `PositionStability.Stable` and `PositionStability.Unstable`;
`GetGroupEnvironmentPositionValidity` rejects surrounding positions that are not stable; Note 1 says
“except for the center”), `GeometryGroupAnalyzer.ContainsRing` (pairwise full-geometry duplicate check),
`MocassinEnergySettings.InitAsDefault` (`AtomsPerGroup` 2..8, `GroupsPerPosition` 0/0/4/10,
`PermutationsPerGroup` 1/1/500/5000, `PositionsPerStable` 0/1/500/5000, `PositionsPerUnstable` 0/1/100/1000),
and `GetGroupPermutationCount` (surrounding permutations × center particle count).

**Files reviewed.** `AGENTS.md`; `docs/planning/reviews/README.md`; `docs/planning/steps/step-05-energy-model.md`
at `0000f3f`; `docs/planning/02-data-contracts.md`; `docs/planning/01-target-architecture.md`;
`docs/planning/03-quality-strategy.md`; `docs/planning/05-open-decisions.md`;
`docs/planning/appendix-b-parity-matrix.md`; `docs/planning/status.md`; `docs/planning/audits/README.md`;
ADR 0001/0009/0010/0013; `docs/planning/steps/step-02-symmetry-and-geometry.md`,
`step-03-particle-and-structure-models.md`, `step-04-transition-model.md`,
`step-06-lattice-and-simulation-models.md`, `step-07-validation-and-diagnostics.md`,
`step-08-model-compilation-pipeline.md`, `step-09-parameterization-templates.md`,
`step-15-energy-engine.md`, `step-25-energy-lattice-and-simulation-editors.md`,
`step-26-parameterization-job-templates-and-build-ui.md`; `docs/development/code-structure.md`;
`docs/development/compilation-pipeline.md`; `docs/user-guide/index.md`; `docs/physics/index.md`;
`docs/reference/index.md`; `docs/troubleshooting/index.md`; `zensical.toml`; `contracts/manifest.json`;
`tools/contracts/generate.py`; `tools/structure-policy.json`; `tools/verify.sh`; `src/dotnet/Mocassin.Core`
(`Model/ModelDefinition.cs`, `Model/ModelDefinitionJsonConverter.cs`, `Project/ProjectJson.cs`,
`Project/ProjectHasher.cs`, `Project/ProjectCanonicalizer.cs`, `Project/IProjectQuery.cs`, `Project/Project.cs`,
`Editing/ProjectEditor.cs`, `Units/UnitValues.cs`, `Diagnostics/*`), `Mocassin.Symmetry`
(`Structure/IStructurePreviewService.cs`, `Transitions/ITransitionPreviewService.cs`,
`Reduction/PairReduction.cs`), `Mocassin.Packages` (`Projects/ProjectStore.cs`,
`Projects/ProjectValidationService.cs`), `Mocassin.Architecture.Tests/StructureChecks/*`, and
`Mocassin.Compilation/Mocassin.Compilation.csproj`.

### Round-1 finding status

| Finding | Round-2 status | Verification |
| --- | --- | --- |
| M1 | Resolved | `CanonicalizeModel`/`SerializeCanonicalModel`/`ComputeEnergy` are now the stated contract; `ProjectStore.SaveAsync` uses the converter, and default/empty sections are omitted to preserve hashes. |
| M2 | Resolved | Core/Symmetry ownership is explicit and matches the Step 03/04 pattern; composition is in `ProjectValidationService`; codes stay in Core. |
| M3 | Resolved in the core model; residual m9 | Shared `EnergyId`, per-direction `GeometryId`, and `ChiralPartnerGeometryId` are aligned in Step 05/08 and the data contract; Step 09/26 and the quality strategy still contain stale mirroring/conflict requirements. |
| M4 | Resolved in Step 05; residual m10 | Center-particle factor, `long` counts, Burnside count, full limit table, shared sampler, and the Step 08 deferral are present; Step 07's statistics types remain `int`. |
| M5 | Resolved | `IProjectQuery` extends `IEnergyQuery`; preview methods use `TryCreatePreview(..., out diagnostics)`; cache key and symmetry-version inputs are named. |
| m1 | Resolved | `ProjectJsonContext` nested, `ProjectSchema` unchanged, `ProjectEditor.Energy.cs`/`EnergyModelValidator.Clusters.cs` are structure-policy-valid. |
| m2 | Resolved in §2.2; residual n4 | Specific filter/entry sort keys and default/omission rules are present; the generic §1 “all other arrays are sorted by id” rule is still unqualified. |
| m3 | Resolved | Manifest `status`/`notes`, named fixtures, vectors, schema regeneration, and the additive-v1 reconciliation are planned. |
| m4 | Partially resolved | Inclusive/unstable-filter semantics, 2..8 offsets, encoder dedup, and ring definition are fixed; ENM007 still contradicts the page (m8), and the original-warning-to-error deviations are not recorded (m11). |
| m5 | Resolved except m12 | Non-goals, nav/index/status/parity/audit tasks, and the Step 07 troubleshooting ownership are added; the new user-guide link and duplicated ENM catalog remain. |
| m6 | Resolved except m13 | Tiers, per-ENM coverage, default/hash/order/overflow tests, and a benchmark target are added; the diagnostics row's PR-fast tier conflicts with the “no symmetry sampling” definition. |
| m7 | Resolved | SPDX/no-new-dependency task and acceptance criterion are present. |
| n1 | Resolved | Step 05 defines the ENM018 constant/help anchor; Step 07 owns emission and consolidated diagnostics. |
| n2 | Resolved | `StructureModel.Default` has no sites and ENM002/sampling warnings are suppressed for the empty model. |
| n3 | Acknowledged | The round-2 request commit makes the record parent `4c1cd2f`, not the frozen `0000f3f` (n6). |

### Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M6 — The `GeometryId` projection omits the P1 position index, so it is not a unique directed-geometry identity.** Step 05 hashes `(siteAId, siteBId, offsetA, offsetB, offsetC, distanceBucketFm)`. The Step 02 shell representative is a `Vector4I` `(cellA, cellB, cellC, P)`, and Step 08 orders pair tables by `(distanceBucket, targetP, cellA, cellB, cellC)`. A reference site can expand to multiple P1 positions; two distinct shells from the same site pair, cell offset, and distance bucket but different target `P` therefore hash to the same `GeometryId`. Because an achiral `EnergyId` equals its `GeometryId` and a chiral `EnergyId` is the minimum of two `GeometryId`s, a collision silently merges parameterization values. Include the full center/target encoded positions (or explicit `centerP`/`targetP`) in the projection, and align Step 08/09 and the compiled-model contract. | Step 05 §5 lines 216–218 and `PairInteractionPreview` lines 377–384; Step 02 `PairReduction`/`PairShell.Representative` and pair-shell order lines 425–437; Step 08 §3 line 137; `02-data-contracts.md` `positions.vector4` line 686 and pair-table note lines 705–713. |
| **Minor** | **m8 — `ENM007` contradicts the stable-or-unstable cluster-center rule.** Step 05 §3 says the center is an existing stable **or unstable** reference site, and Step 07 says every cluster center is a site with an environment; the rule table says “Cluster center unknown or unstable” and would reject a legal unstable-centered cluster. The original accepts both stabilities and requires only the surrounding positions to be stable. Change the rule to “center unknown or not a stable/unstable reference site”. | Step 05 lines 160–161 and 282; Step 07 lines 90–91; original `GroupInteractionValidator.AddDefinedPositionValidation` and `GetGroupEnvironmentPositionValidity` (Note 1 “except for the center”); Step 05 ENM010 lines 173–175. |
| **Minor** | **m9 — Step 09/26 and the quality strategy still require chiral mirroring and conflict detection after `PRM007` was retired.** Step 09 still has the implementation task “chiral mirroring”, the acceptance criterion “values are mirrored”, a documentation bullet, a test row “Mirrored values and conflict detection”, and a “Chiral divergence” risk; Step 26 still exposes “chiral conflicts”; the quality strategy still says “chiral mirroring is consistent”. With one shared `EnergyId`, independent values are unrepresentable and a conflict cannot occur, so those requirements/tests are unsatisfiable as written. Replace them with shared-value/linked-display wording and remove conflict detection. | Step 09 lines 193, 207, 217, 230, 241; Step 26 lines 31, 79, 187; `03-quality-strategy.md` line 112; Step 05 lines 219–225; Step 09 `PRM007` line 183; `02-data-contracts.md` lines 519–521. |
| **Minor** | **m10 — Step 07's `ModelStatistics` remains `int` for counts Step 05 now defines as `long`.** `EstimatedUniqueClusterPermutations`, `EstimatedPairInteractionCount`, `P1PositionCount`, `StablePositionCount`, and `UnstablePositionCount` are `int`, while Step 07 says statistics use the same sampling/preview code as Step 05 and Step 05's cost preview fields are `long` (the 63⁹ case exceeds `int`). Update the statistics record to `long` or document and test an explicit cap. | Step 07 lines 107–114 and 120; Step 05 §6 lines 246 and §9 lines 364–375. |
| **Minor** | **m11 — The design makes several original warnings into errors without a recorded validation-deviation note.** Round-1 m4 asked for this; the resolution says “deviation notes” were added, but the Step 05 page has no deviations section. Step 03 records its stricter rules explicitly; Step 05 should state why the original warning cases (`ENM004`–`ENM011`, etc.) are errors in v1. | Step 05 §7 lines 274–296; Step 03 “Validation deviations from the original” lines 371+; Step 04 precedent; `AGENTS.md` §4.8. |
| **Minor** | **m12 — The documentation plan links to a page that does not exist at Step 05 closeout and duplicates the ENM catalog.** `docs/user-guide/energy.md` is to link to `docs/troubleshooting/errors.md`, but only `docs/troubleshooting/index.md` exists; Step 07 creates `errors.md`. A strict documentation build at Step 05 closeout would see a broken link. Separately, `docs/reference/energy.md` is to list `ENM001`–`ENM021`, while Step 07 owns the complete catalog in `docs/reference/diagnostics.md`; link to the Step 07 page instead of duplicating the codes. | Step 05 documentation lines 579–593; `ls docs/troubleshooting` (index only); Step 07 documentation lines 201–206; `docs/reference/index.md` “Diagnostics … added with Step 07”. |
| **Minor** | **m13 — The “Diagnostics” test row is tagged PR-fast although it covers Symmetry sampling codes.** The tier definition says PR-fast has “No symmetry sampling”; `ENM012`/`ENM013`/`ENM017`/`ENM019` need the `Mocassin.Symmetry` sampler/validator and the tier table puts Symmetry geometry/preview tests in PR-medium. Split the diagnostics row by owner/tier or move it to PR-fast/PR-medium. | Step 05 test tiers and table lines 597–609. |
| **Note** | **n4 — The data contract's generic ordering rule still says “all other arrays are sorted by `id`”, but filters and site-energy entries have no `id`.** The §2.2 specific keys mitigate this; qualify §1 to say arrays of identifiable objects or explicitly state that the §2.2 keys override the generic rule. | `02-data-contracts.md` lines 17–20 vs 384–402. |
| **Note** | **n5 — “CanonicalizeModel applies these rules before … schema generation” is inaccurate.** The generated project schema comes from `contracts/manifest.json` via `tools/contracts/generate.py`, not from `CanonicalizeModel`. Say “before save and hashing”; schema generation is a separate manifest task. | `02-data-contracts.md` lines 399–400; `tools/contracts/generate.py`; `contracts/manifest.json`. |
| **Note** | **n6 — Status dashboard and record parent at the frozen commit.** At `0000f3f` the dashboard still says “Ready for review at `fa45192`”; the request commit `4c1cd2f` corrects it to “Re-review required at `0000f3f`”. The reviewer record commit's parent is therefore `4c1cd2f`, not the reviewed commit. Process note for the human owner, not a design finding. | `git show 0000f3f:docs/planning/status.md` lines 52 and 82; `git show 4c1cd2f:docs/planning/status.md`. |
| **Note** | **n7 — `EnergyLimits` exposes bare `double` energy limits at a public API boundary.** `EnergyMinEv`/`EnergyMaxEv` are `double`; the conventions prefer explicit unit types (`EnergyEv`) at API boundaries. Use `EnergyEv` constants or document why the limit constants are unit-suffixed bare doubles. | Step 05 lines 359–360; `AGENTS.md` §8 “.NET”. |
| **Note** | **n8 — The sampler's invalid-input contract is unspecified.** `IEnergyGeometrySampler.SamplePairs` returns a non-null `ImmutableArray` plus diagnostics, while the preview `TryCreate…` methods return `null` on invalid data. State whether the sampler returns an empty array plus diagnostics or also fails, so Step 08 and the preview share one contract. | Step 05 lines 386–410. |
| **Note** | **n9 — ENM009 and ENM011 overlap for duplicate offsets.** ENM009 rejects duplicate offsets; ENM011 rejects any duplicate in the full geometry, which includes duplicate offsets. This can emit both codes for one defect. Clarify that ENM011 covers center-equivalent/other duplicates and ENM009 owns duplicate offsets, or explicitly accept both. | Step 05 lines 284–286. |
| **Note** | **n10 — The cost-preview count name does not match its description.** §6 says “number of stable positions in the P1 cell”, while `EnergyCostPreview` exposes `P1PositionCount`; Step 07 distinguishes `P1PositionCount` from `StablePositionCount`. Clarify which count the field reports. | Step 05 §6 line 234 and `EnergyCostPreview` line 371; Step 07 lines 107–109. |

### Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Re-review required
- [ ] Waived

**Verdict: Changes required.** Round-1 findings M1–M5 and m1–m3/m5–m7/n1–n2 are substantively resolved,
and the Core/Symmetry split, persistence/hash path, long-count cost preview, shared `EnergyId`, and query/
preview boundary are now coherent. One new Major remains: M6, the `GeometryId` projection does not include
the P1 position index, so it is not unique for multi-position sites and can merge parameterization values.
Six minor and six note findings remain (m8–m13, n4–n10); none is a Blocker. The step must not start until M6
is corrected and the minors are fixed or explicitly accepted, then a fresh reviewer re-reviews the new design
commit. The documentation build, contract generation, structure test, contract/policy gate, and whitespace
check all pass at `0000f3f`; the findings are design-completeness findings, not tooling failures.

## Round 2 resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| M6 | Resolved — awaiting re-review | Planner / Step 05 agent | `6b8d78c` |
| m8 | Resolved — awaiting re-review | Planner / Step 05 agent | `6b8d78c` |
| m9 | Resolved — awaiting re-review | Planner / Step 05 agent | `6b8d78c` |
| m10 | Resolved — awaiting re-review | Planner / Step 05 agent | `6b8d78c` |
| m11 | Resolved — awaiting re-review | Planner / Step 05 agent | `6b8d78c` |
| m12 | Resolved — awaiting re-review | Planner / Step 05 agent | `6b8d78c` |
| m13 | Resolved — awaiting re-review | Planner / Step 05 agent | `6b8d78c` |
| n4 | Resolved — awaiting re-review | Planner / Step 05 agent | `6b8d78c` |
| n5 | Resolved — awaiting re-review | Planner / Step 05 agent | `6b8d78c` |
| n6 | Acknowledged — awaiting re-review | Planner / Step 05 agent | `6b8d78c` |
| n7 | Resolved — awaiting re-review | Planner / Step 05 agent | `6b8d78c` |
| n8 | Resolved — awaiting re-review | Planner / Step 05 agent | `6b8d78c` |
| n9 | Resolved — awaiting re-review | Planner / Step 05 agent | `6b8d78c` |
| n10 | Resolved — awaiting re-review | Planner / Step 05 agent | `6b8d78c` |

Resolution summary (design content at `6b8d78c`):

- **M6.** `GeometryId` now hashes `(centerSiteId, centerP, partnerSiteId, partnerP, offsetA–C,
  distanceBucketFm)`. `PairInteractionPreview` exposes `CenterP`/`PartnerP`; Step 08 pair sets and the compiled
  contract carry the P1 indices. A collision test is added.
- **m8.** `ENM007` now rejects an unknown/non-reference center or an unstable center without an unstable
  environment; stable and unstable centers are both legal, and only the surrounding positions must be stable.
- **m9.** Step 09/26, the quality strategy, and the parity matrix now use shared-`EnergyId`/linked-editing
  wording; chiral mirroring and conflict detection are removed because independent values are unrepresentable.
- **m10.** Step 07 `ModelStatistics` uses `long` for P1/stable/unstable positions, pair interactions, and
  cluster permutations, with the 63⁹ rationale.
- **m11.** Added §7.1 “Validation deviations from the original”, classifying every promoted warning and every
  retained warning with its rationale.
- **m12.** The documentation plan no longer links to `troubleshooting/errors.md` or `reference/diagnostics.md`
  before Step 07 creates them, and `reference/energy.md` no longer duplicates the ENM catalog.
- **m13.** Diagnostics tests are split into Core (PR-fast) and Symmetry (PR-medium), plus the ENM018 boundary
  row.
- **n4.** The data-contract §1 ordering rule now qualifies identifiable arrays and defers to the energy-specific
  keys for filters/entries.
- **n5.** `CanonicalizeModel` is stated to apply before save and hashing; the generated schema is stated to come
  from `contracts/manifest.json` via the generator.
- **n6.** Process note only; the request commit updates the dashboard and the reviewer's round-2 text is
  unchanged.
- **n7.** `EnergyLimits` exposes `MinEnergy`/`MaxEnergy` as `EnergyEv`; count limits remain `int`.
- **n8.** The sampler uses `TrySamplePairs(..., out pairs, out diagnostics)` and returns `false` with an empty
  array plus diagnostics on invalid input.
- **n9.** `ENM009` owns surrounding-surrounding duplicate offsets; `ENM011` owns center-equivalent and other
  full-geometry duplicates and does not double-report the same pair.
- **n10.** §6 now names `P1PositionCount` as the P1-expanded position count, distinct from the stable
  reference-site count.

## Re-review round 3

### Re-review request

| Field | Value |
| --- | --- |
| Step | 05 — Energy Model |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Design review status | Re-review required |
| Reviewed commit | `6b8d78c` |
| Docs branch | `main` |
| Review date | 2026-11-09 |
| Reviewer | Fresh independent planning auditor (round 3; no shared conversation with the planner or implementor) |

Step page: [`Step 05 — Energy Model`](../steps/step-05-energy-model.md)

The design content for round 3 is frozen at `6b8d78c`. The delta from the round-2 commit `0000f3f` is the
round-2 fix round recorded in the Round 2 resolution status section plus the affected Step 07/08/09/26, data
contract, quality-strategy, parity, and status updates. The review request and status changes are metadata
only. Verify the delta yourself with `git diff --stat 0000f3f..6b8d78c` and inspect the files at `6b8d78c`.

Reviewer: append your independence statement, evidence, findings, and verdict for this round below. Keep all
earlier round text unchanged. This is a re-review of the full design at the new commit, not only a diff check;
verify that M6, m8–m13, and n4–n10 are resolved and that no new contradiction was introduced.

### Reviewer context

I am a fresh planning auditor for round 3. I did not author or implement this design and have no shared
conversation with the planner, the implementor, or the round-1/round-2 reviewers. I reviewed the full design at
the frozen commit `6b8d78c`, not only the delta. I read `AGENTS.md` §3.8–§3.13 and §4–§12,
`docs/planning/reviews/README.md`, the complete request record, the Step 05 page, the data contract, target
architecture, quality strategy, open decisions, parity matrix, status dashboard, audit plan, ADR 0001/0009/
0010/0013, `NOTICE.md`, `contracts/manifest.json`, `zensical.toml`, `tools/verify.sh`,
`tools/structure-policy.json`, the relevant Core/Symmetry/Packages source, and the original
`GroupInteractionValidator`/`StableEnvironmentInfoValidator`/`MocassinEnergySettings`/`ValueSetting` as
read-only reference. I did not rely on the resolution text alone.

**Frozen state.** Worktree `/workspace/mocassin`, branch `main`, `HEAD` `1eb29ef` at review time; the worktree
was clean before this record was written. The frozen design is `6b8d78c`. `0000f3f..6b8d78c` changes the Step
05 page, data contract, quality strategy, parity row, status text, and the Step 07/08/09/26 pages. The round-3
request commit `1eb29ef` sits after the frozen commit; this record commit's parent is therefore `1eb29ef`, not
`6b8d78c` (process note n11). `git diff --stat 6b8d78c -- . ':!docs/planning/reviews/step-05.md'` shows only
`docs/planning/status.md` changed between the frozen design and the request commit.

### Round 3 checklist

- [x] ModelDefinition energy sections and JSON/schema round-trip — the flat sections, converter, canonical
  save, source-generated context, manifest, schema, and fixtures are specified.
- [x] Default `stableEnvironment`, omission rules, and hash stability — verified against the actual
  `ProjectJson`/`ProjectStore`/`ProjectHasher` path; default and empty energy sections are omitted, so
  pre-Step-05 hashes remain stable.
- [x] Semantic cluster-offset order and sorted collections — `offsetVectors` definition order is semantic;
  the filters, unstable environments, clusters, and site-energy keys are stated.
- [ ] Environment, cutoff, filter, cluster, and site-energy validation, including the validation-deviations
  record — the rules and Core/Symmetry ownership are coherent, but §7.1 still omits the promoted `ENM013`
  cluster-permutation limit and misattributes the site-energy range to the original site-energy validator
  (m15).
- [x] ENM001–ENM021 ownership, Step 07 ENM018 boundary, and the Core/Symmetry split — Core, Symmetry, and
  Step 07 ownership is explicit; `ENM018` remains a Step 07-owned emission with a Step 05 constant/help anchor.
- [x] Derived pair/chiral preview, GeometryId/EnergyId including `centerP`/`partnerP`, and stable geometry IDs —
  the P1 position indices are in the projection, the preview record, the Step 08 compiled pair set, and the
  compiled contract.
- [x] Cost preview, center-particle permutation factor, `long` counts, and permutation/position limits — the
  center factor, Burnside count, `long` fields, warning/hard limits, and the Step 08 sampler deferral are
  present. The `P1PositionCount` naming against Step 07 is a note (n12), not a blocking finding.
- [x] IEnergyQuery, IProjectQuery, IEnergyPreviewService, and IEnergyGeometrySampler (including the `Try`
  contract and `ModelDefinition` input) — matches the established query/preview boundary, returns `null`/false
  with diagnostics on invalid data, and never throws for user data.
- [x] Step 03 structure/site and Step 04 transition dependencies — reused without a new Core→Symmetry edge;
  `ENM018` is the only Step 04-facing rule and remains in Step 07.
- [ ] Step 07/08/09/26 cross-step alignment for shared `EnergyId`, retired `PRM007`, and long statistics — the
  step pages, data contract, quality strategy, and parity matrix are aligned and Step 07 uses `long`, but the
  pre-seeded audit-request checklists for Steps 05, 09, and 26 still carry the superseded acceptance wording
  (m14).
- [x] Manifest/schema and fixture tasks — `status`/`notes`, energy fields, the two named fixtures, signature
  vectors, and schema regeneration are named.
- [x] PublicAPI/ApiCompat tasks — Core, Packages, and Symmetry API files and the ApiCompat baseline are named.
- [x] Code layout against the structure policy — `ProjectEditor.Energy.cs` and
  `EnergyModelValidator.Clusters.cs` are valid `Type.Aspect.cs` names; current folder counts leave room for the
  planned files.
- [ ] Documentation/nav, status, parity, and audit plans, including no links to pages that do not yet exist —
  the Step 05 documentation plan no longer links to the Step 07 pages before they exist and the nav/status/
  parity tasks are present, but the pre-seeded audit-request checklist is stale (m14).
- [x] No unresolved cross-step interface question remains — the code/data interfaces are aligned. m14 and n12
  are planning-record and naming notes, not unresolved interfaces.

### Evidence

**Commands run (from `/workspace/mocassin`):**

```sh
git status --short
# (no output: clean worktree before this record was written)

git log --oneline --decorate -6
# 1eb29ef (HEAD -> main) docs: request step-05 round-3 design re-review [skip ci]
# 6b8d78c docs: resolve step-05 round-2 review findings [skip ci]
# 9150367 docs: record step-05 design re-review Changes required [skip ci]
# 4c1cd2f docs: request step-05 round-2 design re-review [skip ci]
# 0000f3f docs: revise step-05 energy design for review findings [skip ci]
# 4bf7f66 docs: record step-05 design review Changes required [skip ci]

git show --stat 6b8d78c
# docs/planning/02-data-contracts.md                 | 15 ++--
# docs/planning/03-quality-strategy.md               |  2 +-
# docs/planning/appendix-b-parity-matrix.md          |  2 +-
# docs/planning/status.md                            |  7 +-
# docs/planning/steps/step-05-energy-model.md        | 87 +++++++++++++++++-----
# docs/planning/steps/step-07-validation-and-diagnostics.md | 12 +--
# docs/planning/steps/step-08-model-compilation-pipeline.md |  4 +-
# docs/planning/steps/step-09-parameterization-templates.md | 10 +--
# docs/planning/steps/step-26-parameterization-job-templates-and-build-ui.md | 6 +-
# 9 files changed, 101 insertions(+), 44 deletions(-)

git diff --stat 0000f3f..6b8d78c
# docs/planning/02-data-contracts.md                 |  15 +-
# docs/planning/03-quality-strategy.md               |   2 +-
# docs/planning/appendix-b-parity-matrix.md          |   2 +-
# docs/planning/reviews/step-05.md                   | 328 ++++++++++++++++++++-
# docs/planning/status.md                            |   8 +-
# docs/planning/steps/step-05-energy-model.md        |  87 ++++--
# docs/planning/steps/step-07-validation-and-diagnostics.md |  12 +-
# docs/planning/steps/step-08-model-compilation-pipeline.md |   4 +-
# docs/planning/steps/step-09-parameterization-templates.md |  10 +-
# docs/planning/steps/step-26-parameterization-job-templates-and-build-ui.md |   6 +-
# 10 files changed, 427 insertions(+), 47 deletions(-)

git diff 0000f3f..6b8d78c -- docs/planning
# Reviewed in full: Step 05 page, data contract, quality strategy, parity row, status text,
# Step 07/08/09/26 pages, and the review-record resolution/request metadata.

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
# Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1
# exit 0

./tools/verify.sh contracts
# Contract artifacts are current (17 files)
# space-group catalog is current: 274 settings, sha256 1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395
# SPDX headers present in 217 source files
# No secret patterns found in 524 tracked text files
# advisories ok, bans ok, licenses ok, sources ok
# exit 0

git diff --check
# (no output, exit 0)
```

**Read-only checks.**

```sh
git diff --stat 6b8d78c -- . ':!docs/planning/reviews/step-05.md'
# docs/planning/status.md | 8 ++++----
# 1 file changed, 4 insertions(+), 4 deletions(-)

git show 6b8d78c:docs/planning/steps/step-05-energy-model.md | nl -ba | sed -n '286,346p'
# ENM001-ENM021 table and §7.1; ENM013 is Error at line 296, but the §7.1 table has no ENM013 row.

git show 6b8d78c:docs/planning/steps/step-07-validation-and-diagnostics.md | sed -n '103,122p'
# P1/stable/unstable positions, pair interactions, cluster permutations, and memory are long.

git show 6b8d78c:docs/planning/steps/step-08-model-compilation-pipeline.md | sed -n '132,168p'
# Pair sets carry CenterP/PartnerP; the GeometryId hash includes both P1 indices.

git show 6b8d78c:docs/planning/steps/step-09-parameterization-templates.md | sed -n '74,80p;183p;193,241p'
# Pair-set identity is the shared EnergyId; PRM007 is reserved; mirroring/conflict tests are removed.

git show 6b8d78c:docs/planning/steps/step-26-parameterization-job-templates-and-build-ui.md | sed -n '28,32p;76,80p;184,188p'
# Shared chiral-set editing and linked editing wording.

git show 6b8d78c:docs/planning/03-quality-strategy.md | sed -n '108,113p;210,214p'
# Parameterization property test now says chiral partners share one EnergyId and one value.

git show 6b8d78c:docs/planning/appendix-b-parity-matrix.md | sed -n '78,82p;102,107p'
# Chiral pair linking and chiral linked editing rows carry the shared-EnergyId wording.

git show 6b8d78c:docs/planning/audits/step-05.md | nl -ba | sed -n '36,44p'
# Stale checklist: 8-offset-only, "consistent with step 08", and no unstable-filter wording.

git show 6b8d78c:docs/planning/audits/step-09.md | nl -ba | sed -n '38,46p'
# Stale checklist: "Chiral pair values are mirrored and cannot diverge."

git show 6b8d78c:docs/planning/audits/step-26.md | nl -ba | sed -n '38,48p'
# Stale checklist: "Chiral pair values mirror and conflicts are flagged."

grep -n "PRM007\|mirrored and\|mirror and\|chiral conflicts\|chiral mirroring" \
  docs/planning/steps/step-09-parameterization-templates.md \
  docs/planning/steps/step-26-parameterization-job-templates-and-build-ui.md
# Only the original-reference sentence ("were linked and mirrored values") and the reserved PRM007 row remain
# in the step pages; the live requirements use shared EnergyId wording.

grep -n "Chiral pair values\|Chiral pair values are mirrored\|Chiral pair values mirror" \
  docs/planning/audits/step-09.md docs/planning/audits/step-26.md
# Confirms the stale audit-request checklists.

wc -l src/dotnet/Mocassin.Core/Editing/ProjectEditor.cs
# 506 src/dotnet/Mocassin.Core/Editing/ProjectEditor.cs

for d in src/dotnet/Mocassin.Core/Model src/dotnet/Mocassin.Core/Diagnostics \
         src/dotnet/Mocassin.Core/Editing src/dotnet/Mocassin.Core/Project \
         src/dotnet/Mocassin.Symmetry/Energy; do
  find "$d" -maxdepth 1 -name '*.cs' | wc -l
done
# 5, 7, 7, 12, 0 direct files; adding the planned files stays under the 20-file limit.

sed -n '80,140p' src/dotnet/Mocassin.Core/Project/ProjectJson.cs
sed -n '140,160p' src/dotnet/Mocassin.Packages/Projects/ProjectStore.cs
sed -n '1,90p' src/dotnet/Mocassin.Core/Project/ProjectHasher.cs
# CanonicalizeModel/SerializeCanonicalModel exist; SaveAsync serializes through ProjectJson; ComputeDocument
# uses the canonical project; ComputeEnergy is the planned addition.

sed -n '1,80p' src/dotnet/Mocassin.Symmetry/Structure/IStructurePreviewService.cs
sed -n '1,60p' src/dotnet/Mocassin.Symmetry/Transitions/ITransitionPreviewService.cs
# Both established previews use TryCreatePreview(..., out diagnostics).

sed -n '1,30p' src/dotnet/Mocassin.Core/Project/IProjectQuery.cs
# IProjectQuery is IParticleQuery, IStructureQuery, ITransitionQuery; the planned IEnergyQuery addition is additive.

sed -n '1,40p' src/dotnet/Mocassin.Core/Editing/ProjectEditor.cs
# The editor accepts a Func<Project, ImmutableArray<Diagnostic>> validator, so Packages can inject the
# composed Core + Symmetry validator without a Core->Symmetry reference.
```

**Original reference (read-only, `/workspace/mocassin-original`).**

```sh
nl -ba src/ModelBuilder/ICon.Model/ModelProject/Manager/Settings/Modules/MocassinEnergySettings.cs | sed -n '62,72p'
# PermutationsPerGroup is 1/1/500/5000; PairEnergies/GroupEnergies are -100..100; site energies are not range-limited.

nl -ba src/ModelBuilder/ICon.Model/Energies/Manager/Services/Validators/GroupInteractionValidator.cs | sed -n '74,81p'
# AtomsPerGroup and PermutationsPerGroup violations are added as warnings.

nl -ba src/ModelBuilder/ICon.Model/ModelProject/Manager/Objects/ValueSetting.cs | sed -n '92,116p'
# ParseValue returns 2 and a restriction-violation warning above MaxValue.

nl -ba src/ModelBuilder/ICon.Model/Energies/Manager/Services/Validators/StableEnvironmentInfoValidator.cs | sed -n '134,151p'
# Defect/site-energy validation warns on duplicates, void-on-stable, and NaN/Inf; no -100..100 range check.
```

**Files reviewed.** `AGENTS.md`; `docs/planning/reviews/README.md`; `docs/planning/reviews/step-05.md` at
`6b8d78c`; `docs/planning/steps/step-05-energy-model.md` at `6b8d78c`; `docs/planning/02-data-contracts.md`;
`docs/planning/01-target-architecture.md`; `docs/planning/03-quality-strategy.md`;
`docs/planning/05-open-decisions.md`; `docs/planning/appendix-a-original-inventory.md`;
`docs/planning/appendix-b-parity-matrix.md`; `docs/planning/status.md`; `docs/planning/audits/README.md`;
`docs/planning/audits/step-05.md`, `step-07.md`, `step-09.md`, `step-26.md`; ADR 0001/0009/0010/0013;
`step-02-symmetry-and-geometry.md`, `step-03-particle-and-structure-models.md`,
`step-04-transition-model.md`, `step-06-lattice-and-simulation-models.md`,
`step-07-validation-and-diagnostics.md`, `step-08-model-compilation-pipeline.md`,
`step-09-parameterization-templates.md`, `step-15-energy-engine.md`,
`step-25-model-editors-energy-lattice-simulations.md`, `step-26-parameterization-job-templates-and-build-ui.md`;
`docs/development/code-structure.md`; `docs/development/compilation-pipeline.md`; `docs/user-guide/index.md`;
`docs/physics/index.md`; `docs/reference/index.md`; `docs/troubleshooting/index.md`; `zensical.toml`;
`contracts/manifest.json`; `tools/contracts/generate.py`; `tools/structure-policy.json`; `tools/verify.sh`;
`src/dotnet/Mocassin.Core` (`Model/ModelDefinition.cs`, `Model/ModelDefinitionJsonConverter.cs`,
`Project/ProjectJson.cs`, `Project/ProjectHasher.cs`, `Project/ProjectValidator.cs`, `Project/IProjectQuery.cs`,
`Project/Project.cs`, `Editing/ProjectEditor.cs`, `Editing/ProjectDocument.cs`, `Diagnostics/*`),
`Mocassin.Symmetry` (`Structure/IStructurePreviewService.cs`, `Transitions/ITransitionPreviewService.cs`,
`PublicAPI.Unshipped.txt`), `Mocassin.Packages` (`Projects/ProjectStore.cs`,
`Projects/ProjectValidationService.cs`), `Mocassin.Architecture.Tests/StructureChecks/*`, and
`Mocassin.Compilation/Mocassin.Compilation.csproj`.

### Round-2 finding status

| Finding | Round-3 status | Verification |
| --- | --- | --- |
| M6 | Resolved | `GeometryId` now hashes `(centerSiteId, centerP, partnerSiteId, partnerP, offsetA–C, distanceBucketFm)`; `PairInteractionPreview` exposes `CenterP`/`PartnerP`; Step 08 pair sets and the compiled contract carry the P1 indices; the collision test is in the test table. |
| m8 | Resolved | `ENM007` now accepts stable and unstable centers and rejects only an unknown/non-reference center or an unstable center without an unstable environment; §3 says only the surrounding positions must be stable. |
| m9 | Resolved in the live design; residual m14 | Step 09/26, the quality strategy, the data contract, and the parity matrix use shared-`EnergyId`/linked-editing wording and retire `PRM007`. The pre-seeded Step 09/26 audit-request checklists still require mirroring/conflict detection. |
| m10 | Resolved | Step 07 `ModelStatistics` uses `long` for P1/stable/unstable positions, pair interactions, cluster permutations, and memory; the 63⁹ rationale is recorded. |
| m11 | Partially resolved — m15 | §7.1 exists and covers most promoted warnings, but it omits the promoted `ENM013` cluster-permutation hard limit and its site-energy-range row does not match the original defect-energy validator. |
| m12 | Resolved | The Step 05 documentation plan no longer links to `troubleshooting/errors.md` or `reference/diagnostics.md` before Step 07 creates them, and `reference/energy.md` no longer duplicates the ENM catalog. The strict docs build passes. |
| m13 | Resolved | Diagnostics tests are split into Core (PR-fast) and Symmetry (PR-medium), with a separate ENM018 boundary row. |
| n4 | Resolved | The generic §1 ordering rule now applies only to arrays of identifiable objects and defers to the §2.2 energy-specific keys for filters and site-energy entries. |
| n5 | Resolved | `CanonicalizeModel` is stated to apply before save and hashing; the generated schema is stated to come from `contracts/manifest.json` via `tools/contracts/generate.py`. |
| n6 | Acknowledged | Process note only; the record parent is `1eb29ef`, not the frozen `6b8d78c` (n11). |
| n7 | Resolved | `EnergyLimits` exposes `MinEnergy`/`MaxEnergy` as `EnergyEv`; count limits remain `int`. |
| n8 | Resolved | `IEnergyGeometrySampler.TrySamplePairs` returns `false` with an empty array and diagnostics on invalid input and never throws; `IEnergyPreviewService` returns `null` with diagnostics. |
| n9 | Resolved | `ENM009` owns surrounding-surrounding duplicate offsets; `ENM011` owns center-equivalent and other full-geometry duplicates and does not double-report the same pair. |
| n10 | Resolved, with naming note n12 | §6 names `EnergyCostPreview.P1PositionCount` as the P1-expanded position count, distinct from the stable reference-site count. The name still differs from Step 07's total `P1PositionCount`; see n12. |

### Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **m14 — The pre-seeded audit-request checklists still carry the superseded acceptance criteria.** The Step 05 audit request still says “A cluster with 8 offsets is accepted; 9 offsets are rejected”, “The cost preview reports pair and permutation counts consistent with step 08”, and omits the unstable one-directional filter; the revised Step 05 page accepts 2/8 and rejects 1/9, records the Step 08 cross-check as a Step 08 follow-up, and specifies one-directional unstable filters. The Step 09 and Step 26 audit requests still require chiral mirroring/conflict detection after `PRM007` was retired and independent chiral values became unrepresentable. These records are the closeout audit gate, so they must be updated with the step pages before their audits. | `docs/planning/audits/step-05.md` lines 38–44; `step-09.md` line 42; `step-26.md` line 40; Step 05 page lines 137–147, 596–602; Step 09 page lines 193, 207, 230, 241; Step 26 page line 187; Step 05 page lines 230–232. |
| **Minor** | **m15 — §7.1 omits the promoted `ENM013` hard limit and misattributes the site-energy range.** The new deviations table lists the promoted `ENM019` sampling hard limit and `ENM021` cluster limit, but not `ENM013` (cluster raw permutations > 5000), although the original `PermutationsPerGroup` maximum is 5000 and `GroupInteractionValidator` reports an over-limit value as a warning. The table also says “Site energy outside −100..100 eV | Warning”, while the original defect/site-energy validator only warns on duplicates, void-on-stable, and NaN/Inf; the −100..100 setting applies to pair/group energies. The table should add `ENM013` (and clarify the `ENM010` cluster-cutoff promotion) and distinguish new ID-reference checks from promoted warnings. | Step 05 page lines 296, 324–340; original `MocassinEnergySettings.cs` lines 67, 70–71; `GroupInteractionValidator.cs` lines 79–80; `ValueSetting.cs` lines 107–115; `StableEnvironmentInfoValidator.cs` lines 134–150. |
| **Note** | **n12 — `EnergyCostPreview.P1PositionCount` has the same name as Step 07's total P1 position count but counts only stable reference-site P1 positions.** §6 says it counts “every stable reference-site orbit expanded to P1 positions”, while Step 07's `ModelStatistics` separates `P1PositionCount`, `StablePositionCount`, and `UnstablePositionCount`. The description is clear, but the cross-step name is ambiguous; rename the preview field (for example `StableP1PositionCount`) or state the relationship explicitly. | Step 05 page lines 241–242, 403; Step 07 page lines 104–121. |
| **Note** | **n11 — Record parent.** `HEAD` is `1eb29ef`; the frozen design is `6b8d78c` and `1eb29ef` contains only the round-3 request/status metadata. This record commit's parent is therefore `1eb29ef`, not the reviewed commit. This is a process note for the human owner, not a design finding. | `git log`/`git diff --stat 6b8d78c -- . ':!docs/planning/reviews/step-05.md'` above. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

### Verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Re-review required
- [ ] Waived

**Verdict: Ready with notes.** M6 is resolved: `GeometryId` now includes the canonical `centerP`/`partnerP`
P1 indices, and Step 08 and the compiled contract carry the same identity. m8, m10, m12, m13, and n4–n10 are
resolved or carry only the naming note n12. m9 is resolved in the live design, but its pre-seeded Step 09/26
audit checklists are stale (m14); m11 is only partially resolved because §7.1 omits the promoted `ENM013`
limit and misstates the site-energy-range origin (m15). No Blocker or Major remains, and the persistence/hash
path, Core/Symmetry split, query/preview boundary, cost preview, and cross-step `EnergyId` model are coherent.
The remaining findings are documentation/planning-record consistency issues; the human owner must explicitly
accept them, or ask for a small follow-up design commit, before the start gate. The strict documentation build,
contract generation, structure test, contract/policy gate, and whitespace check all pass at `6b8d78c`; the
findings are not tooling failures.

Follow-up owner: Planner and Step 05 agent for m14/m15 (and the n12 naming note)
Follow-up step: Step 05 — Energy Model; a further re-review is required only if the human owner does not accept
the notes and the design is changed.

## Round 3 resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| m14 | Resolved — awaiting focused re-review | Planner / Step 05 agent | `2f43a19` |
| m15 | Resolved — awaiting focused re-review | Planner / Step 05 agent | `2f43a19` |
| n12 | Resolved — awaiting focused re-review | Planner / Step 05 agent | `2f43a19` |
| n11 | Acknowledged — awaiting focused re-review | Planner / Step 05 agent | `2f43a19` |

Resolution summary (design content at `2f43a19`):

- **m14.** Refreshed the pre-seeded Step 05 audit checklist to the revised acceptance criteria (stable-filter
  symmetry and one-directional unstable filters, inclusive boundaries, 2/8 accepted and 1/9 rejected,
  center-equivalent/filtered/out-of-range clusters, brute-force preview counts with the Step 08 deferral,
  shared `EnergyId`, canonical hashing, schema/fixtures, ENM coverage, SPDX, PublicAPI/ApiCompat, and docs).
  Updated the Step 09 and Step 26 audit checklists to shared-`EnergyId`/linked-editing wording.
- **m15.** Rewrote §7.1 to add `ENM003`, `ENM010`, and `ENM013`; distinguished promoted warnings from new v1
  checks; corrected the site-energy −100..100 eV origin (the original did not range-check site energies; the
  pair/group settings use that range); and recorded the new unknown-reference/duplicate-entry checks.
- **n12.** Renamed `EnergyCostPreview.P1PositionCount` to `StableP1PositionCount`; Step 07 now documents that
  `ModelStatistics.P1PositionCount` is the total P1 count while the Step 05 field is the stable subset.
- **n11.** Process note only; no design change. The round-4 record parent will be the request commit, not the
  frozen design commit.

## Re-review round 4 (focused)

### Re-review request

| Field | Value |
| --- | --- |
| Step | 05 — Energy Model |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Design review status | Re-review required |
| Reviewed commit | `2f43a19` |
| Docs branch | `main` |
| Review date | 2026-11-09 |
| Reviewer | Fresh independent planning auditor (round 4, focused delta review; no shared conversation with the planner or implementor) |

Step page: [`Step 05 — Energy Model`](../steps/step-05-energy-model.md)

This is a focused re-review of the round-3 fix delta at `2f43a19`, not a full re-read of every design area.
The full design was reviewed at `6b8d78c` and returned `Ready with notes` (m14, m15, n12, n11). The delta is
`git diff 6b8d78c..2f43a19 -- docs/planning`:

- `docs/planning/audits/step-05.md` — refreshed acceptance checklist;
- `docs/planning/audits/step-09.md` — shared-`EnergyId` wording;
- `docs/planning/audits/step-26.md` — shared-`EnergyId` wording;
- `docs/planning/steps/step-05-energy-model.md` — §7.1 correction and `StableP1PositionCount` rename;
- `docs/planning/steps/step-07-validation-and-diagnostics.md` — total-vs-stable P1 count clarification.

Reviewer: verify m14, m15, and n12 are resolved and that the delta introduces no unrelated design change or
new contradiction. Keep all earlier round text unchanged and append your reviewer-authored round-4 content
below.

### Reviewer context

I am a fresh planning auditor for round 4. I did not author or implement this design and have no shared
conversation with the planner, the implementor, or the round-1/round-2/round-3 reviewers. This is the focused
delta re-review requested above: I verified m14, m15, and n12 against the revised files and checked that the
`6b8d78c..2f43a19` delta introduces no unrelated design change or new contradiction. I read `AGENTS.md`
§3.8–§3.13, `docs/planning/reviews/README.md`, the complete request record, the Step 05 and Step 07 pages and the
Step 05/09/26 audit records at `2f43a19`, the frozen Step 05/07 design at `6b8d78c`, `docs/planning/status.md`,
and the original `StableEnvironmentInfoValidator`/`GroupInteractionValidator`/`MocassinEnergySettings` as
read-only reference. I did not rely on the resolution text alone.

**Frozen state.** Worktree `/workspace/mocassin`, branch `main`, `HEAD` `9484eec`; the worktree was clean before
this record was written. The reviewed commit is `2f43a19`, and `git show --stat 2f43a19` shows only the five
in-scope files. The round-4 request commit `9484eec` sits after it; `git diff 2f43a19..9484eec` changes only the
review record, `status.md`, and the Step 05 page's one-line "Design review" status field, so the reviewed design
content is unchanged (process note n13). The wider range `6b8d78c..2f43a19` also contains the round-3 record
commit `76ae25f` and the approval-summary commit `c1d8109`; those are record/status commits, not design changes.

### Round 4 checklist

- [x] **m14 — Step 05 audit checklist refreshed to the revised acceptance criteria.** Every item now matches the
  Step 05 page: stable-filter symmetry versus one-directional unstable filters with `SiteAId` = environment
  site; inclusive 1e-5 Å boundaries; 2/8 offsets accepted and 1/9 rejected (`ENM008`); duplicate,
  center-equivalent, unknown/unstable/filtered, ring/degenerate, and out-of-range clusters (`ENM009`–`ENM011`);
  brute-force preview counts with the center factor, `long` limits, and the Step 08 cross-check deferred;
  shared `EnergyId`/`ChiralPartnerGeometryId`; round-trip, default, omission, and semantic order;
  `ComputeEnergy`; schema and fixtures; `ENM001`–`ENM021` coverage including the `ENM018` Step 07 boundary;
  SPDX; PublicAPI/ApiCompat; docs/nav/status/parity.
- [x] **m14 — Step 09 and Step 26 audit checklists use shared-`EnergyId`/linked-editing wording.** Both match the
  live Step 09/26 requirements, and no mirroring or chiral-conflict requirement remains.
- [x] **m15 — §7.1 adds `ENM003`, `ENM010`, and `ENM013`, clarifies the cluster-cutoff promotion, and corrects
  the site-energy −100..100 eV origin.** The promoted/new "Type" column is present. One residual
  classification inaccuracy is recorded as m16.
- [x] **n12 — `StableP1PositionCount` rename.** §6 and `EnergyCostPreview` use the new name, and Step 07 now
  states that `ModelStatistics.P1PositionCount` is the total (stable + unstable) count while the Step 05 field
  is the stable subset. No stale `EnergyCostPreview.P1PositionCount` reference remains.
- [x] **Delta scope.** The five in-scope files only; no unrelated design change beyond the review/status
  metadata already in the range.
- [x] **No new contradiction outside m16.** The m14/m15/n12 edits agree with the rule table, the Step 07
  clarification, and the Step 09/26 wording.

### Evidence

**Commands run (from `/workspace/mocassin`):**

```sh
git status --short
# (no output: clean worktree before this record was written)

git log --oneline --decorate -6
# 9484eec (HEAD -> main) docs: request step-05 focused round-4 re-review [skip ci]
# 2f43a19 docs: resolve step-05 round-3 review notes [skip ci]
# c1d8109 docs: record step-05 approval summary awaiting acceptance [skip ci]
# 76ae25f docs: record step-05 design re-review Ready with notes [skip ci]
# 1eb29ef docs: request step-05 round-3 design re-review [skip ci]
# 6b8d78c docs: resolve step-05 round-2 review findings [skip ci]

git show --stat 2f43a19
# docs/planning/audits/step-05.md                    | 27 ++++++++++---
# docs/planning/audits/step-09.md                    |  2 +-
# docs/planning/audits/step-26.md                    |  2 +-
# docs/planning/steps/step-05-energy-model.md        | 45 ++++++++++++++--------
# .../steps/step-07-validation-and-diagnostics.md    |  3 ++
# 5 files changed, 54 insertions(+), 25 deletions(-)

git diff --stat 6b8d78c..2f43a19
# docs/planning/audits/step-05.md                    |  27 +-
# docs/planning/audits/step-09.md                    |   2 +-
# docs/planning/audits/step-26.md                    |   2 +-
# docs/planning/reviews/step-05.md                   | 464 ++++++++++++++++++++-
# docs/planning/status.md                            |   8 +-
# docs/planning/steps/step-05-energy-model.md        |  47 ++-
# .../steps/step-07-validation-and-diagnostics.md    |   3 +
# 7 files changed, 512 insertions(+), 41 deletions(-)

git diff 6b8d78c..2f43a19 -- docs/planning
# Reviewed in full. The record/status changes come from the intervening round-3 record and approval-summary
# commits; the design delta is the five files named by `git show --stat 2f43a19`.

zensical build --strict
# Build started
# No issues found
# Build finished in 0.24s
# exit 0

python3 tools/contracts/generate.py --check
# Contract artifacts are current (17 files)
# exit 0

git diff --check
# (no output, exit 0)
```

**Read-only checks.**

```sh
git show 2f43a19:docs/planning/steps/step-05-energy-model.md | nl -ba | sed -n '283,352p'
# ENM001–ENM021 rule table and §7.1: ENM003/ENM010/ENM013 rows present, the site-energy origin is corrected,
# and the Type column distinguishes Promoted, Promoted + new, and New v1.

git show 2f43a19:docs/planning/steps/step-05-energy-model.md | nl -ba | sed -n '237,244p;410,418p'
# §6 and EnergyCostPreview use StableP1PositionCount; no P1PositionCount alias remains in Step 05.

git show 2f43a19:docs/planning/steps/step-07-validation-and-diagnostics.md | nl -ba | sed -n '119,125p'
# States that ModelStatistics.P1PositionCount is the total count and Step 05's field is the stable subset.

git show 2f43a19:docs/planning/audits/step-05.md | nl -ba | sed -n '36,59p'
# Refreshed Step 05 checklist; each item traceable to the page's acceptance criteria.

git show 2f43a19:docs/planning/audits/step-09.md | nl -ba | sed -n '38,44p'
git show 2f43a19:docs/planning/audits/step-26.md | nl -ba | sed -n '38,42p'
# Shared-EnergyId/linked-editing wording matches Step 09 line 207 and Step 26 line 187.

git show 2f43a19:docs/planning/steps/step-09-parameterization-templates.md | nl -ba | sed -n '74,76p;120p;183p;207p;230p'
git show 2f43a19:docs/planning/steps/step-26-parameterization-job-templates-and-build-ui.md | nl -ba | sed -n '31p;60p;79p;187p'
# Live step pages use one shared EnergyId/one value and linked editing; PRM007 is reserved, not emitted.

grep -rn "P1PositionCount" docs/planning --include=*.md | grep -v reviews/step-05.md
# docs/planning/steps/step-07...:107,122-123  (total field and the clarification)
# docs/planning/steps/step-05...:242-243,414   (StableP1PositionCount only)

grep -rn "consistent with step 08" docs/planning/steps/step-05-energy-model.md
# (no output: the Step 05 closeout criterion is now a brute-force oracle; Step 08 is a follow-up)

git diff 2f43a19..9484eec --stat
# docs/planning/reviews/step-05.md            | 87 +++++++---
# docs/planning/status.md                     |  6 +-
# docs/planning/steps/step-05-energy-model.md |  2 +-
# 3 files changed, 74 insertions(+), 21 deletions(-)   # record/status plus the one-line review-status field
```

**Original reference (read-only, `/workspace/mocassin-original`).**

```sh
nl -ba src/ModelBuilder/ICon.Model/Energies/Manager/Services/Validators/StableEnvironmentInfoValidator.cs | sed -n '28,105p;134,151p'
# Filter-range violations are warnings; duplicate defect/site energies, void-on-stable, and NaN/Inf warn;
# no −100..100 site-energy range check exists.

nl -ba src/ModelBuilder/ICon.Model/Energies/Manager/Services/Validators/GroupInteractionValidator.cs | sed -n '40,135p'
# Unknown/unsupported center, missing environment, atoms/permutations per group, duplicate geometry, and the
# environment-range/geometry-validity checks are all warnings (ENM007–ENM010 and ENM013 promotions).

grep -rn "MaxInteractionRange" src/ModelBuilder/ICon.Model --include=*.cs
# A bare double with no validator rule, so ENM001 is a new v1 check rather than a promotion.
```

**Files reviewed.** `AGENTS.md` §3.8–§3.13 and §4–§12; `docs/planning/reviews/README.md`; the complete
`docs/planning/reviews/step-05.md`; `docs/planning/steps/step-05-energy-model.md` at `2f43a19` and `6b8d78c`;
`docs/planning/steps/step-07-validation-and-diagnostics.md` at `2f43a19` and `6b8d78c`;
`docs/planning/audits/step-05.md`, `step-09.md`, and `step-26.md` at `2f43a19` and `6b8d78c`;
`docs/planning/steps/step-09-parameterization-templates.md`; `docs/planning/steps/step-26-parameterization-
job-templates-and-build-ui.md`; `docs/planning/status.md`; and the original validators/settings listed above.

### Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **m16 — §7.1's new promoted-vs-new classification is still internally inconsistent.** The new paragraph lists "duplicate site-energy entries (`ENM014`)" among the "new v1 checks without a direct original counterpart", but the ENM014 row itself records "Duplicate/void warnings" as the original behavior and the next sentence of the same paragraph says the original `StableEnvironmentInfoValidator` "warned on duplicates". The enumeration also reads as exhaustive while omitting `ENM001`, which is likewise a new v1 error because the original `MaxInteractionRange` is a bare double with no validator rule. In addition, the `ENM015` row is labelled `New v1` although its non-finite half was an original NaN/Inf warning. Correct the ENM014 wording to promoted (or "Promoted + new" for the unknown-reference part), label `ENM015` "Promoted + new", and make the new-check list exhaustive or state that it is illustrative. | Step 05 page lines 343–351; rule table lines 285 and 299; original `StableEnvironmentInfoValidator.cs` lines 139–150; `MocassinEnergySettings.cs` lines 70–71. |
| **Note** | **n13 — Record parent and request-commit metadata.** `HEAD` is `9484eec`; the reviewed commit is `2f43a19`. `git diff 2f43a19..9484eec` touches the review record, `status.md`, and the Step 05 page's one-line "Design review" status field only. This record commit's parent is therefore `9484eec`, not the reviewed commit. Process note for the human owner, not a design finding. | Commands above. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

### Verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Re-review required
- [ ] Waived

**Verdict: Ready with notes.** m14 is resolved: the Step 05 audit checklist now matches the revised acceptance
criteria, and the Step 09/26 checklists use the shared-`EnergyId`/linked-editing wording of their live step
pages. m15 is resolved: §7.1 now carries `ENM003`, `ENM010`, and `ENM013` rows, a promoted/new "Type" column, a
clarified cluster-cutoff promotion, and the corrected site-energy −100..100 eV origin (the original validator
warned only on duplicates, void-on-stable, and NaN/Inf; the pair/group settings use the range). n12 is resolved
by the `StableP1PositionCount` rename and the Step 07 total-vs-stable clarification. The delta contains no
unrelated design change. One new Minor remains (m16): the promoted/new classification introduced by the m15 fix
still mislabels duplicate site-energy entries as new, omits the new `ENM001` check from an apparently exhaustive
list, and labels the mixed `ENM015` rule as `New v1` only. No Blocker or Major remains; m16 is a documentation
accuracy issue in the deviations record and does not destabilize the energy model, persistence/hash path,
validation split, cost preview, or cross-step `EnergyId` model. The human owner must explicitly accept m16 (or
request a one-line follow-up correction) before the start gate.
The strict documentation build, contract generation, and whitespace check all pass at `2f43a19`; the finding is
not a tooling failure.

Follow-up owner: Planner and Step 05 agent for m16
Follow-up step: Step 05 — Energy Model; the human owner decides whether to accept m16 as a note or require a
follow-up correction. n13 is a process note only.

## Round 4 resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| m16 | Resolved — awaiting focused re-review | Planner / Step 05 agent | `0b2a571` |
| n13 | Acknowledged — awaiting focused re-review | Planner / Step 05 agent | `0b2a571` |

Resolution summary (design content at `0b2a571`):

- **m16.** Split the `ENM014` row into promoted duplicate/void cases and the new unknown-reference check,
  split the `ENM015` row into promoted non-finite and new range checks, added `ENM001` and `ENM002` rows, and
  made the new-check paragraph explicitly illustrative rather than exhaustive. Duplicate site-energy entries
  are now correctly labelled as promoted, not new.
- **n13.** Process note only; no design change. The round-5 record parent will be the request commit, not the
  frozen design commit.

## Re-review round 5 (focused)

### Re-review request

| Field | Value |
| --- | --- |
| Step | 05 — Energy Model |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Design review status | Re-review required |
| Reviewed commit | `0b2a571` |
| Docs branch | `main` |
| Review date | 2026-11-09 |
| Reviewer | Fresh independent planning auditor (round 5, focused §7.1 correction; no shared conversation with the planner or implementor) |

Step page: [`Step 05 — Energy Model`](../steps/step-05-energy-model.md)

This is a focused re-review of the m16 correction only. The full design at `6b8d78c` and the round-3 fixes at
`2f43a19` were already reviewed. The delta is `git diff 2f43a19..0b2a571 -- docs/planning`, and it changes only
§7.1 of the Step 05 page.

Reviewer: verify that m16 is resolved — the `ENM014`/`ENM015` promoted-vs-new split, the added `ENM001`/
`ENM002` rows, and the explicitly illustrative new-check list — and that the delta introduces no unrelated
change or new contradiction. Keep all earlier round text unchanged and append your reviewer-authored round-5
content below.
