# Step 04 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-04.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Design
> review status`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 04 — Transition Model |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Design review status | Re-review required |
| Reviewed commit | `4f4c888` |
| Docs branch | `main` |
| Review date | 2026-11-08 |
| Reviewer | Independent planning auditor (fresh context; no shared conversation with the planner or implementor) |

> Round 1 reviewed `b426a13` and returned `Changes required`. The design was revised in `4f4c888`; the request
> metadata now points at that commit for round 2. The round-1 reviewer text below is unchanged.

Step page: [`Step 04 — Transition Model`](../steps/step-04-transition-model.md)

## Step summary

| Field | Value |
| --- | --- |
| Phase | B. Model definition |
| Depends on | 03 |
| Delivers | State changes, state-change groups, abstract chains, and KMC/MMC transitions bound to reference geometry, with validation of transition semantics. |

## Design decisions reviewed

- [x] Step page design decisions — reviewed; see majors M3–M9 and minors below
- [x] Data contracts and format changes — reviewed `02-data-contracts.md` transition section (lines 260–314) and the frozen diff
- [x] Architecture and module boundaries — reviewed Core/Symmetry/Packages boundaries; preview/query boundary has majors M2/M8
- [x] ADRs and open decisions — reviewed ADR 0009/0010/0013 and `05-open-decisions.md`; no open decision blocks the step, but the manifest version-bump rule is not reconciled (note n1)
- [x] Quality strategy and test plan — reviewed `03-quality-strategy.md`; test plan is materially incomplete (minor m2)
- [x] Parity matrix and documentation plan — reviewed `appendix-b-parity-matrix.md` and the step Documentation section; no parity/status/audit task is planned (minor m3)
- [x] Licensing and provenance — no new dependency; MIT/SPDX applies; no GPL source or data is introduced
- [x] Post-implementation audit plan — reviewed; no step-specific audit plan is present; the standard AGENTS §3.10 audit still applies

## Dependency and interface checks

- [x] Dependencies are `Done` and their contracts are stable — Step 03 is complete (`step-03` tag; audit passed); this step depends only on 03
- [ ] Inputs, outputs, types, and formats are defined — partial: the preview/query signatures and `StateChangeMatrices` are incomplete (M2, M8)
- [x] Versioning and migration behavior is defined — additive project-v1 fields; Step 03 pre-release precedent; ADR 0009 version-bump wording is a note (n1)
- [ ] No unresolved cross-step interface question remains — unresolved: `TransitionRef` (M6), `TRN020` range (M4), charge-transfer flag (M5), reverse link (M3), path-to-site mapping (M7)

## Standard design-review checklist

- [ ] Scope, non-goals, and acceptance criteria are explicit and testable — scope and most criteria are present; there is no non-goals section and the `TRN018` criterion is not testable (m1, M3)
- [ ] Design decisions are complete and internally consistent — contradictions remain (M3–M9, m8)
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined — unit/acceptance cases exist; property tests, golden fixtures, tiers, and a preview benchmark are missing (m2)
- [ ] Documentation, status, parity, and audit updates are planned — user-guide/physics page list exists; no parity/status/closeout/audit task and no diagnostics-reference update (m3)
- [x] Licensing, provenance, and dependency checks are resolved — no new dependency; MIT/SPDX; no GPL copying
- [x] Risks and mitigations are realistic — risk table is plausible; it omits the data-loss (M1), reverse-link (M3), and path-mapping (M7) risks
- [x] Code layout and namespace plan follow the structure policy — mostly; `ProjectJsonContext.cs` does not exist as a file and the `ProjectEditor` split is not planned (m4, m5)
- [ ] No hidden scope, stale placeholders, or unowned decisions — typed IDs, `IsAssociation`, canonical model sorting, `TransitionRef`, and the diagnostics page are unowned (M1, M6, m6, m7, m12)

## Evidence

### Review round 1

**Reviewer context.** I am a fresh planning auditor. I did not author or implement this design and have no shared
conversation with the planner or implementor. I inspected the repository at the frozen commit myself; I did not
rely on the request summary.

**Frozen state and worktree.** Worktree `/workspace/mocassin`, branch `main`, `HEAD` `7d0757b`. The request
names `b426a13` as the frozen design commit. `git diff b426a13 7d0757b -- docs/planning/steps/step-04-transition-model.md docs/planning/02-data-contracts.md`
produced no output, so the reviewed design content is identical at `b426a13` and at the frozen merge. The record
commit's parent is `7d0757b` because the request record (`c0928f1`) and freeze merge (`7d0757b`) sit between the
design commit and the record; this is a process note (n2), not a finding about the design.

**Commands run (from `/workspace/mocassin`):**

```sh
git status --short
# (no output: clean worktree)

git log --oneline --decorate -8
# 7d0757b (HEAD -> main, origin/main) docs: freeze step-04 design for review [skip ci]
# c0928f1 docs: request step-04 design review [skip ci]
# b426a13 docs: prepare step-04 design for review
# a4270be docs: record step-03 audit human approval [skip ci]
# ccd2333 docs: mark step-03 complete and record merge [skip ci]
# 162ebe4 (tag: step-03) step-03: particle and structure models
# ef4de06 docs: record step-03 re-audit passed and approval summary [skip ci]
# 1d61837 docs: record step-03 re-audit Passed with notes [skip ci]

git show --stat b426a13
# docs/planning/02-data-contracts.md              |   5 +
# docs/planning/steps/step-04-transition-model.md | 169 ++++++++++++++++++++++--
# 2 files changed, 164 insertions(+), 10 deletions(-)

git diff b426a13^ b426a13 -- docs/planning
# Read in full. The diff adds the stateChangeIds ordering bullet and semantic-order/versioning paragraph to
# 02-data-contracts.md, and adds the ModelDefinition composition/persistence section, PublicAPI section, code
# layout, persistence/manifest/PublicAPI/navigation tasks, round-trip/schema/semantic-order acceptance criteria,
# and the model-envelope/schema/semantic-order test rows to the Step 04 page. No other planning file changed in
# this commit.

git show b426a13:docs/planning/steps/step-04-transition-model.md
# Read in full (415 lines).

git show b426a13:docs/planning/02-data-contracts.md
# Read in full (1,167 lines); transition section lines 260-314, general ordering lines 12-23, canonical-hash
# rules lines 136-145.

git diff --stat b426a13 7d0757b -- docs/planning/steps/step-04-transition-model.md docs/planning/02-data-contracts.md
# (no output: design is identical at both commits)
```

**Additional read-only checks:**

```sh
wc -l src/dotnet/Mocassin.Core/Editing/ProjectEditor.cs \
      src/dotnet/Mocassin.Core/Project/ProjectValidator.cs \
      src/dotnet/Mocassin.Core/Model/ModelDefinitionJsonConverter.cs \
      src/dotnet/Mocassin.Core/Project/ProjectJson.cs
#  506 ProjectEditor.cs
#  414 ProjectValidator.cs
#  107 ModelDefinitionJsonConverter.cs
#  358 ProjectJson.cs

grep -rn "TransitionRef" docs src --include=*.md --include=*.cs | grep -v '/bin/' | grep -v '/obj/'
# docs/planning/steps/step-06-lattice-and-simulation-models.md:110: ImmutableArray<TransitionRef> TransitionIds);

grep -rn "GeneratesReverse" docs src --include=*.md --include=*.cs | grep -v '/bin/' | grep -v '/obj/'
# docs/planning/steps/step-04-transition-model.md:133: bool GeneratesReverse);
# docs/planning/steps/step-04-transition-model.md:158: - `GeneratesReverse` is true only for ...

grep -rn "ProjectJsonContext" src/dotnet/Mocassin.Core --include=*.cs | grep -v '/bin/' | grep -v '/obj/'
# src/dotnet/Mocassin.Core/Project/ProjectJson.cs:136,358

find src/dotnet -path '*/bin' -prune -o -path '*/obj' -prune -o -name '*Enum*Converter*.cs' -print
# (none; CamelCaseEnumJsonConverter<T> is inside Model/ViewSettings.cs:382-411)
```

**Files reviewed.** `AGENTS.md` (§3.8–§3.13, §4, §5, §7, §8, §9, §10, §11, §12); `docs/planning/reviews/README.md`;
`docs/planning/reviews/step-design-review-template.md`; `docs/planning/reviews/step-04.md`;
`docs/planning/steps/step-04-transition-model.md` at `b426a13`; `docs/planning/02-data-contracts.md` at `b426a13`;
`docs/planning/03-quality-strategy.md`; `docs/planning/05-open-decisions.md`; `docs/planning/ci-policy.md`;
`docs/planning/appendix-b-parity-matrix.md`; `docs/planning/status.md`; `docs/planning/steps/step-03-particle-and-structure-models.md`;
`step-05-energy-model.md`; `step-06-lattice-and-simulation-models.md`; `step-07-validation-and-diagnostics.md`;
`step-08-model-compilation-pipeline.md`; `step-24-model-editors-particle-structure-transitions.md`;
`docs/planning/adr/0009-contract-manifest-and-contracts-library.md`, `0010-canonical-messagepack-signatures.md`,
`0013-format-compatibility-and-api-versioning.md`; `docs/development/code-structure.md`;
`contracts/manifest.json`; `contracts/generated/schema/project-v1.schema.json`;
`contracts/generated/signatures/model-v1.json`; `tools/contracts/generate.py`; `tools/verify.sh`;
`tools/check-api-compat.sh`; `tools/update-api-baseline.sh`; `tools/structure-policy.json`.

**Code and test inventory inspected.** `Mocassin.Core`: `Model/ModelDefinition.cs`,
`Model/ModelDefinitionJsonConverter.cs`, `Model/Structure/StructureModel.cs`, `Model/Structure/CellSite.cs`,
`Model/Particles/*`, `Project/Project.cs`, `Project/ProjectJson.cs`, `Project/ProjectHasher.cs`,
`Project/ProjectSchema.cs`, `Project/ProjectValidator.cs`, `Project/ProjectDocument.cs` (via `ProjectEditor`),
`Editing/ProjectEditor.cs`, `Diagnostics/Diagnostic.cs`, `Geometry/Fractional3JsonConverter.cs`,
`Geometry/GeometryTolerances.cs`, `Model/ViewSettings.cs` (enum converter), `Ids/TypedIds.cs`,
`Ids/TypedIdJsonConverter.cs`. `Mocassin.Symmetry`: `Structure/IStructurePreviewService.cs`,
`Structure/StructurePreview.cs`, `Structure/StructurePreviewService.cs`, `Geometry/StructureSiteReference.cs`.
`Mocassin.Packages`: `Projects/ProjectStore.cs`, `Projects/ProjectValidationService.cs`.
Tests: `Mocassin.Core.Tests/Model/ModelEnvelopeTests.cs`, `ProjectFormatTests.cs`, `ModelPropertyTests.cs`;
`Mocassin.Symmetry.Tests/Structure/*`; `tests/contracts/*` fixture inventory.

**Required scope checks.** All thirteen required review-scope areas were inspected: scope/non-goals/acceptance,
ModelDefinition and JSON/schema round-trip, semantic ordering, every built-in pattern and `Custom`, path/stability
validation, query/preview boundaries, TRN001–TRN020, manifest/schema/fixture tasks, PublicAPI/ApiCompat, code
layout, Step 03 dependency stability, Step 05/06/07/08/24 transition dependencies, documentation/navigation and
test plan, and unresolved cross-step interfaces. The findings below record the result.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **M1 — Persistence plan omits the canonical model rebuild, so transition data can be dropped and edits treated as no-ops.** `ProjectJsonConverter.Write` replaces `value.Model` with `ProjectJson.CanonicalizeModel(value.Model)`; `CanonicalizeModel` currently rebuilds a `ModelDefinition` containing only `Particles`, `ParticleSets`, and `Structure`. `ProjectHasher.Compute` uses the same path, and `ProjectEditor.Apply` returns the original project when the document hash is unchanged. The task list names `ModelDefinitionJsonConverter`/`ProjectJsonContext`/`ProjectSchema`/`ProjectValidator` but not `CanonicalizeModel` or the `ProjectJson`/`ProjectStore` save path. Unless extended, `ProjectStore.Save` drops the new sections and transition edits are no-ops. | `ProjectJson.cs:82,91-104,262`; `ProjectHasher.cs:20-25,52`; `ProjectEditor.cs:55-62`; step page lines 338-339; `ProjectStore.cs:151,203`. |
| **Major** | **M2 — `ITransitionPreviewService` lacks the data needed to build its result and does not follow the Step 03 failure boundary.** `CreatePreview` receives the transition plus `StructureModel`, but `TransitionPreview` contains a `TransitionChain` and `StateChangeMatrices`, and the transition records expose only `ChainId`; there is no chain, group, state-change, or query argument and no documented service constructor. It returns a non-null record without diagnostics, whereas Step 03's `IStructurePreviewService.TryCreatePreview(..., out diagnostics)` returns null with diagnostics and never throws for invalid user data. | Step page lines 258-287; `IStructurePreviewService.cs:13-39`; `StructurePreview.cs:24-32`; step-03 page lines 266-276. |
| **Major** | **M3 — `TRN018` and the "complex transition warn when no reverse is defined" criterion are not implementable.** Neither `TransitionChain` nor the transition records carry a reverse-transition reference or a reverse-defined flag; `GeneratesReverse` is the automatic-inverse flag and the design and Step 08 restrict automatic inverse rules to `Metropolis2` and `Migration3`. "Explicitly defined reverse" therefore has no representation and the acceptance criterion cannot be tested. | Step page lines 127-133,158-159,223,366; step-08 line 201. |
| **Major** | **M4 — `TRN020` ("transition path leaves the interaction range") depends on a Step 05 contract that does not exist yet.** The range is `StableEnvironment.Cutoff`/`UnstableEnvironment.Cutoff`; Step 05 defines those and depends on Step 04. Step 04 has no range input, no defined maximum, and no owner for the rule. It must move to Step 05/07/08 or Step 04 must define a range source and diagnostic path. | Step page line 225; step-05 lines 4,53-75,153-162; step page line 4 ("Depends on: 03"). |
| **Major** | **M5 — `TRN006`'s charge-transfer exception has no model representation.** The rule says a group may not mix physical and charge-state exchanges "unless the chain is explicitly a charge-transfer chain", but `TransitionChain` has no charge-transfer flag/kind and groups are shared, so a group validator cannot know which chain uses it. Step 08's charge-transfer conservation rule and the polaron-hop sample rely on this concept. | Step page lines 95-107,127-133,354; step-08 line 198. |
| **Major** | **M6 — `TransitionRef` is required by Step 06 but undefined.** Step 06 declares `ImmutableArray<TransitionRef> TransitionIds` for simulations; Step 04 defines separate `KineticTransitionId` and `MetropolisTransitionId` and no common reference/discriminator. A repository grep finds `TransitionRef` only in Step 06. Either Step 04 must own the type or Step 06 must define it and the dependency must state which. | Step-06 line 110; step page lines 166-177; grep output in Evidence. |
| **Major** | **M7 — Path-to-site stability mapping is undefined.** `KineticTransition.ReferencePath` is a bare `Fractional3` list; stability is a property of `CellSite`. The design says the path's stability sequence must match the pattern and that endpoints/intermediates are checked, but never defines how a point maps to a reference site (tolerance, exact grid, periodic images, positions outside `[0,1)`), or a diagnostic for an unmatched point. The ring rule is literal first != last, which misses a path ending at a periodic image of its start. The original validator resolves each coordinate against the unit-cell provider and compares group stability. | Step page lines 166-186,216-219; `CellSite.cs:20-25`; `StructureSiteReference.cs`; `GeometryTolerances.cs`; original `KineticTransitionValidator.cs:82-130,141-169`. |
| **Major** | **M8 — `TransitionPreview.StateChangeMatrices` is not a defined interface.** It is `ImmutableArray<ImmutableArray<int>>` with no dimensions, index meaning, donor/acceptor encoding, empty/default contract, or relationship to the S0/S1/S2 state triplet. Step 08 rule generation and Step 24's S0/S1/S2 and exchange-matrix previews depend on the same information. | Step page lines 259-264,287; step-08 lines 182-192; step-24 lines 146,158. |
| **Major** | **M9 — `GeneratesReverse` is contradictory across the step page and the data contract.** The step page says it is true only for `Metropolis2` and `Migration3`, while the pattern table marks `Custom` reverse as "Explicit" and the data contract says "custom ... reverse generation is explicit". There is no diagnostic for a flag that disagrees with the pattern, and Step 08 ignores the flag and uses the pattern. Define whether the flag is user-editable, which patterns may set it true, and the validation rule/severity. | Step page lines 138-159; data contract lines 299-307; step-08 line 201. |
| **Minor** | **m1 — No explicit non-goals; one acceptance criterion is not testable.** The page has Goal/Original reference/Design but no Non-goals section; rule generation, conservation, compiled rules, and preview persistence are only implied. The `TRN018` criterion is untestable for the reason in M3. | Step page lines 20-30,358-372. |
| **Minor** | **m2 — Test plan lacks property tests, golden fixtures, tiers, and a performance target.** The test table is unit/acceptance only. The quality strategy requires property/invariant tests for critical algorithms, tier declarations, and performance as a test; Step 03's comparable preview had a BenchmarkDotNet case and a recorded baseline. Transition path validation and symmetry preview are critical algorithms. | Step page lines 386-401; quality-strategy §§2,5,8; step-03 page lines 291-299,619,652-653. |
| **Minor** | **m3 — Documentation/status/parity/audit updates are not fully planned.** The page lists user-guide/physics/reference pages and nav, but has no parity-matrix task, no status-dashboard/closeout/audit task, and no `docs/reference/diagnostics.md` or help-anchor update. AGENTS §7 item 8 and §7.1 require the parity/status updates; Step 03 made them explicit. | Step page lines 333-384; AGENTS §7 item 8 and §7.1; step-03 page line 570. |
| **Minor** | **m4 — The code-layout table names a file that does not exist.** It lists `Mocassin.Core/Project/ProjectJsonContext.cs`, but `ProjectJsonContext` is a nested internal type inside `ProjectJson.cs:358`; there is no such file. `ProjectSchema.cs` contains only `CurrentVersion`/`SchemaUri` and needs no change while `schemaVersion` stays 1. | `ProjectJson.cs:330-358`; `ProjectSchema.cs:8-19`; step page lines 313-316,338-339. |
| **Minor** | **m5 — `ProjectEditor.cs` is already 506 lines and the split is not planned.** Adding add/update/remove for five object types, duplicate/remove-dependents, and make-group with XML docs is likely to exceed the 800-line file limit. The design claims structure-policy compliance but does not plan an aspect/partial split. | `ProjectEditor.cs` 506 lines; `code-structure.md` lines 16-20,106; step page lines 291-292,311-312. |
| **Minor** | **m6 — Manifest/fixture tasks are incomplete.** The task says update `contracts/manifest.json` and regenerate, but not update the manifest `status`/`notes` (still `step-03` and "later steps"), not name the new fixture IDs/paths in `fixtures.items`, and not say whether the `model` signature skeleton/vectors change. The generator and `verify.sh contracts` enforce artifact freshness, so this must be explicit. | `contracts/manifest.json:5-6,1325-1357`; `tools/contracts/generate.py:441-503`; step page lines 349-351. |
| **Minor** | **m7 — The five typed IDs and their JSON conversion are not in the task list or code layout.** `StateChangeId`, `StateChangeGroupId`, `TransitionChainId`, `KineticTransitionId`, and `MetropolisTransitionId` are used by the records but no `*Ids.cs`/converter task or PublicAPI entry is listed. Existing typed IDs and `TypedIdJsonConverter` establish the pattern. | Step page lines 89-98,127-133,166-177,338-353; `TypedIds.cs`; `TypedIdJsonConverter.cs`. |
| **Minor** | **m8 — `Custom` rules are under-specified and partly contradictory.** A 2-position custom chain is declared Metropolis, but the text says custom stability is inferred from the reference path and a Metropolis transition has no path. "Static linkers must connect positions that exist in the reference geometry" is vacuous because every linked position exists. There is no rule/code for a group used at both stable and unstable positions. | Step page lines 146-157,172-177; original `KineticTransitionValidator.cs:141-169`. |
| **Minor** | **m9 — The TRN table omits several rules the design depends on.** There is no code for an unknown chain-to-group reference (TRN007 only checks counts), a path point that is not a reference site, a kinetic chain with no unstable position, a kinetic transition using a `Metropolis2` chain (TRN015 only checks the Metropolis side), a `GeneratesReverse`/pattern mismatch, a group used at both stable and unstable positions, or invalid Metropolis site stability. The acceptance criterion covers only three path errors. | Step page lines 204-229; step-07 line 80; original `KineticTransitionValidator.cs` and `MetropolisTransitionValidator.cs`. |
| **Minor** | **m10 — The data-contract `referencePath` example is a ring and contradicts `TRN014`.** The example is `[[0.25,0.25,0.25],[0.5,0.25,0.25],[0.25,0.25,0.25]]`, so first and last points are identical; the step page says a path must not be a ring and rejects it. Fixture/sample authors will copy the invalid example. | Data contract lines 285-291; step page lines 185,219. |
| **Minor** | **m11 — `TransitionPreview` is not self-describing and has no failure contract.** Unlike `StructurePreview`, it does not echo the requested `(A,B,C)` counts, does not identify the transition (only the chain), and has no diagnostics/caching contract. A preview can therefore be confused with a different supercell or a different transition sharing the chain. | Step page lines 259-264; `StructurePreview.cs:24-32`; step-03 page lines 266-267. |
| **Minor** | **m12 — `IsAssociation` is omitted without a decision.** The original reference on the step page lists `IsAssociation`; the original rule generator and `RuleMovementFlags` use it, but the new design has no field or note saying it is intentionally dropped or deferred. Step 08 does not mention association/dissociation either. | Step page line 35; original `AbstractTransition.cs:31`; `RuleMovementFlags.cs`; no mention in step-08. |
| **Minor** | **m13 — Original defects and severity changes are not fully resolved or documented.** The page lists the empty-list indexing defect and the "ambiguous end-state" issue but does not state the corrected rule or add a test. The original `AddNormalVehicleRestrictionValidation` restriction (multiple transition pairs on a single-transition vehicle) has no new rule. The table upgrades most original warnings to Errors and adds `TRN004` without a deviation note; AGENTS §4.8 and §10 require documenting chosen interpretations. | Step page lines 33-38; original `AbstractTransitionValidator.cs:118-130`; original `StateExchangeGroupValidator.cs:58-72`; AGENTS §4.8 and §10. |
| **Minor** | **m14 — A Step 03 deferred item is not scheduled.** Step 03 defers the ceria dedicated transition-state occupation set to Step 04 "when the transition model exists"; Step 04 only says "Add ceria KMC/MMC and polaron-hop samples" and does not mention updating the ceria unstable site/set. | Step-03 page lines 44-48; step page lines 354-355; `samples/ceria/ceria.mocprj` unstable O site uses the shared oxygen set. |
| **Minor** | **m15 — The camelCase enum mechanism is not named.** `TransitionPattern`/`TransitionLinker` must serialize as camelCase strings; the repository already has `CamelCaseEnumJsonConverter<T>` in `Model/ViewSettings.cs`, but the design does not say to annotate the enums with it or how unknown values are rejected. | Step page lines 76-77; `ViewSettings.cs:382-411`; `SiteStability.cs:14`. |
| **Minor** | **m16 — Transition-count and duplicate-name scope are ambiguous.** `TRN019` says "more than 100 transitions" without saying per kind or total; `TRN017` says duplicate transition names within their kind, but chain/group/state-change name uniqueness is not specified. | Step page lines 189,224. |
| **Note** | **n1 — Additive project-v1 vs ADR 0009.** The design keeps `schemaVersion` at 1 because the sections are additive. This matches the data contract's additive-v1 rule and the Step 03 pre-release precedent, but ADR 0009's rule says a manifest change requires a version bump. Step 03's audit accepted the additive-v1 interpretation; Step 04 should cite that precedent and the pre-release rationale, or reconcile ADR 0009. | ADR 0009 line 64; data contract lines 122,313; step-03 page lines 93-94. |
| **Note** | **n2 — Record parent is the frozen merge, not the design commit.** The record commit's parent is `7d0757b`; the design content is byte-identical at `b426a13` and `7d0757b` (`git diff` produced no output), and the request record (`c0928f1`) sits between them. This is a process note for the human owner, not a design finding. | Commands and output in Evidence. |
| **Note** | **n3 — Closeout CI is not stated on the step page.** The step is platform-neutral (no P/Invoke, GUI, path/case/locking, or OS-specific code), so the single closeout Ubuntu run is sufficient; the plan should say so with the parity/status task in m3. | `docs/planning/ci-policy.md` §§2-4; step page Implementation status lines 8-16. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Re-review required
- [ ] Waived

Follow-up owner: Planner and Step 04 agent (design revision and findings resolution)
Follow-up step: Step 04 — Transition Model; a fresh re-review is required after the design commit

**Verdict: Changes required.** Nine Major findings remain (M1–M9) and each affects implementation or a
cross-step interface. The design cannot be implemented as written. No Blocker is recorded, but the human
owner may choose to treat any Major as blocking. The highest-priority corrections are the canonical persistence
path (M1), the preview service contract and `StateChangeMatrices` (M2/M8), the reverse/charge-transfer/range
rules (M3–M5), `TransitionRef` (M6), and path-to-site stability mapping (M7). A re-review requires a new design
commit with fixes; do not create the step branch or start implementation before that.

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
| 1 | 2026-11-08 | `b426a13` | Independent planning auditor (fresh context) | Changes required |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| M1–M9 | Major | See round 1 | See `Resolution status` below; design revision is `4f4c888` | `4f4c888` | Resolved — awaiting re-review |
| m1–m16 | Minor | See round 1 | See `Resolution status` below; design revision is `4f4c888` | `4f4c888` | Resolved — awaiting re-review |
| n1–n3 | Note | See round 1 | See `Resolution status` below; design revision is `4f4c888` | `4f4c888` | Addressed — awaiting re-review |

## Human approval

- [ ] Accepted: Ready for implementation
- [ ] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by:
Date:

## Resolution status

Design revision: `4f4c888` — design, data-contract, and dependent-step corrections. This section is owned by
the planner/step agent and does not alter the reviewer's findings, evidence, or verdict.

| Finding | Status | Resolution |
| --- | --- | --- |
| M1 | Resolved — awaiting re-review | Added explicit `ProjectJson.CanonicalizeModel` extension, save/write/hash coverage, and a persistence/document-hash test. |
| M2 | Resolved — awaiting re-review | `ITransitionPreviewService.TryCreatePreview` now takes the transition, `ITransitionQuery`, structure, and counts, returns `TransitionPreview?` with diagnostics, and never returns a partial preview. |
| M3 | Resolved — awaiting re-review | Added same-kind `ReverseTransitionId` links, symmetric-link validation (`TRN027`), and `TRN018` for a complex chain without a reverse link. |
| M4 | Resolved — awaiting re-review | Removed the interaction-range rule from Step 04; assigned `ENM018` to the Step 07 cross-domain pipeline and documented the dependency direction in Step 05. |
| M5 | Resolved — awaiting re-review | Removed the charge-transfer exception; groups are unconditionally homogeneous, matching the original group validator. Charge-transfer chains use homogeneous charge-state groups. |
| M6 | Resolved — awaiting re-review | Defined `TransitionRef`/`TransitionKind` in Step 04 and updated Step 06 to use them; the persisted simulation JSON keeps kind-specific arrays. |
| M7 | Resolved — awaiting re-review | Added `TransitionPathResolver`, periodic canonicalization, the named `PathMatchTolerance`, zero/ambiguous-match `TRN020`, periodic-image ring detection, and resolved stability roles. |
| M8 | Resolved — awaiting re-review | Defined `StateChangeMatrices` dimensions, `ParticleOrder`, row-major indexing, binary encoding, unstable/stable cardinality, and the empty/default contract. |
| M9 | Resolved — awaiting re-review | Constrained `GeneratesReverse` to the two automatic patterns, added `TRN024`, and updated the data contract to say custom transitions use `reverseTransitionId`. |
| m1 | Resolved — awaiting re-review | Added a Non-goals section and made the reverse-warning acceptance criterion testable. |
| m2 | Resolved — awaiting re-review | Added property tests, contract/golden fixtures, test tiers, and a `TransitionPreviewCeria` benchmark with a baseline. |
| m3 | Resolved — awaiting re-review | Added parity, status/dashboard, closeout, audit, and diagnostics-reference tasks. |
| m4 | Resolved — awaiting re-review | Corrected the layout: `ProjectJson.cs` owns the nested context; `ProjectSchema.cs` is unchanged while v1 remains. |
| m5 | Resolved — awaiting re-review | Planned partial `ProjectEditor.Transitions.*.cs` files to stay below the 800-line limit. |
| m6 | Resolved — awaiting re-review | Specified manifest `status`/`notes`, the two transition fixtures, generated-schema refresh, and the Step 08 model-signature relationship. |
| m7 | Resolved — awaiting re-review | Added `Ids/TransitionIds.cs` for the five typed IDs and their JSON converter task. |
| m8 | Resolved — awaiting re-review | Defined the 2-position custom Metropolis case, the 3–8-position custom grammar, and static-linker placement semantics. |
| m9 | Resolved — awaiting re-review | Expanded the diagnostic table to `TRN001`–`TRN027`, including unknown groups, no-unstable chains, cross-kind use, reverse flags, group reuse, vehicle restriction, and reverse links. |
| m10 | Resolved — awaiting re-review | Replaced the ring example in the data contract with a non-ring path. |
| m11 | Resolved — awaiting re-review | Preview now echoes the transition reference and `(A,B,C)`, and has documented failure and cache contracts. |
| m12 | Resolved — awaiting re-review | Recorded that `IsAssociation` is intentionally not persisted; Step 08 derives movement flags and adds a test. |
| m13 | Resolved — awaiting re-review | Added the original-defect resolution table and documented the warning-to-error severity change. |
| m14 | Resolved — awaiting re-review | Added the ceria dedicated transition-state occupation-set task. |
| m15 | Resolved — awaiting re-review | Named `CamelCaseEnumJsonConverter<T>` and specified rejection of unknown/numeric enum strings. |
| m16 | Resolved — awaiting re-review | Defined TRN019 as the total of kinetic plus metropolis transitions and name uniqueness as per-collection, case-insensitive. |
| n1 | Addressed — awaiting re-review | Cited the Step 03 additive-v1 precedent and the pre-release rationale; the manifest status/notes/fixtures are updated. |
| n2 | Acknowledged — awaiting re-review | Process note only; the round-1 text is unchanged and the revision is a new commit. |
| n3 | Addressed — awaiting re-review | Added the single Ubuntu closeout CI task to the implementation tasks. |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Re-review round 2

### Re-review request

| Field | Value |
| --- | --- |
| Step | 04 — Transition Model |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Design review status | Re-review required |
| Reviewed commit | `4f4c888` |
| Docs branch | `main` |
| Review date | — |
| Reviewer | — |

Step page: [`Step 04 — Transition Model`](../steps/step-04-transition-model.md)

The design content for round 2 is frozen at `4f4c888`. The delta from the round-1 commit `b426a13` is the fix
round recorded above; the review request and status changes are metadata only. Verify the delta yourself with
`git diff --stat b426a13..4f4c888`.

Reviewer: append your independence statement, evidence, findings, and verdict for this round below. Keep the
round-1 text unchanged.

### Re-review evidence

_Reviewer to complete._

### Re-review findings

_Reviewer to complete._

### Re-review verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Re-review required
- [ ] Waived

Follow-up owner:
Follow-up step:
