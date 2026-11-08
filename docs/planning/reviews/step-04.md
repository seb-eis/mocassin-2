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
| Design review status | Changes required |
| Reviewed commit | `a3d8641` |
| Docs branch | `main` |
| Review date | 2026-11-08 |
| Reviewer | Independent planning auditor (fresh context; no shared conversation with the planner or implementor) |

> Round 1 reviewed `b426a13`, round 2 reviewed `4f4c888`, and round 3 reviewed `43bad05` and returned
> `Changes required`; round 4 reviewed `fe2331c` and returned `Ready with notes`. The design was revised in
> `a3d8641`; the request metadata now points at that commit for round 5. The earlier reviewer text is
> unchanged.

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
| Design review status | Changes required |
| Reviewed commit | `4f4c888` |
| Docs branch | `main` |
| Review date | 2026-11-08 |
| Reviewer | Independent planning auditor (fresh context, round 2; no shared conversation with the planner or implementor) |

Step page: [`Step 04 — Transition Model`](../steps/step-04-transition-model.md)

The design content for round 2 is frozen at `4f4c888`. The delta from the round-1 commit `b426a13` is the fix
round recorded above; the review request and status changes are metadata only. Verify the delta yourself with
`git diff --stat b426a13..4f4c888`.

Reviewer: append your independence statement, evidence, findings, and verdict for this round below. Keep the
round-1 text unchanged.

### Re-review evidence

**Independence.** I am a fresh planning auditor for round 2. I did not author or implement the Step 04 design,
the fix round, or any implementation; I have no shared conversation with the planner or implementor. I read the
frozen design content and the repository myself; I did not rely on the request summary or the resolution table.

**Frozen state and worktree.** Worktree `/workspace/mocassin`, branch `main`, `HEAD` `3338963`. The request names
`4f4c888` (`4f4c888a191c1e2c75eb9b71afe783cdb3d55e6e`) as the frozen design commit. `git diff 4f4c888..3338963
--stat` shows only `docs/planning/reviews/step-04.md`, `docs/planning/status.md`, and the step-page design-review
link; the design content is otherwise identical. The record commit's parent will be `3338963`, not `4f4c888`,
because the re-review request sits between the design commit and the record; this is the same process note as
round 1 (n2).

**Commands run (from `/workspace/mocassin`):**

```sh
git status --short
# (no output: clean worktree)

git log --oneline --decorate -6
# 3338963 (HEAD -> main) docs: request step-04 design re-review [skip ci]
# 4f4c888 docs: revise step-04 design after review [skip ci]
# 12c8107 docs: record step-04 design review Changes required [skip ci]
# 7d0757b (origin/main) docs: freeze step-04 design for review [skip ci]
# c0928f1 docs: request step-04 design review [skip ci]
# b426a13 docs: prepare step-04 design for review

git rev-parse 4f4c888
# 4f4c888a191c1e2c75eb9b71afe783cdb3d55e6e

git show --stat 4f4c888
# docs/planning/02-data-contracts.md                 |  30 +-
# docs/planning/steps/step-04-transition-model.md    | 470 ++++++++++++++++-----
# docs/planning/steps/step-05-energy-model.md        |   4 +
# .../steps/step-06-lattice-and-simulation-models.md |   4 +
# .../steps/step-07-validation-and-diagnostics.md    |   4 +
# docs/planning/steps/step-08-model-compilation-pipeline.md | 7 +-
# ...model-editors-particle-structure-transitions.md |   3 +-
# 7 files changed, 399 insertions(+), 123 deletions(-)

git diff --stat b426a13..4f4c888 -- docs/planning
# 9 files changed, 673 insertions(+), 127 deletions(-)

git diff b426a13..4f4c888 -- docs/planning
# Read in full: the Step 04 revision, the data-contract delta, the dependent Step 05/06/07/08/24 changes,
# the review-record request, and the status change.

git show 4f4c888:docs/planning/steps/step-04-transition-model.md
# Read in full (651 lines).

git show 4f4c888:docs/planning/02-data-contracts.md
# Read the model envelope (149-170), transition section (262-336), simulation/parameterization sections, and
# the canonical-hash rules (1189 lines total).

git diff 4f4c888..3338963 --stat
# docs/planning/reviews/step-04.md                | 92 +++++++++++++--
# docs/planning/status.md                         | 10 +--
# docs/planning/steps/step-04-transition-model.md |  2 +-
# 3 files changed, 91 insertions(+), 13 deletions(-)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.23s

python3 tools/contracts/generate.py --check
# Contract artifacts are current (17 files)

git diff --check b426a13..4f4c888
# (no output)
```

**Additional read-only checks.**

```sh
wc -l src/dotnet/Mocassin.Core/Editing/ProjectEditor.cs
# 506

grep -n "StateChangeId\|StateChangeGroupId\|TransitionChainId\|KineticTransitionId\|MetropolisTransitionId" \
  src/dotnet/Mocassin.Core/Ids/TypedIds.cs
# 123/142/161/180/199: all five typed IDs already exist
git log --oneline -- src/dotnet/Mocassin.Core/Ids/TypedIds.cs
# dd642d6 step-03: add crystal structure domain model
# 90b4497 step-01: add typed ids, units, and diagnostics

grep -rn "PathMatchTolerance\|TrimToUnitCell" src/dotnet --include=*.cs | grep -v '/bin/' | grep -v '/obj/'
# TrimToUnitCell is used in Fractional3, UnitCellVectorEncoder, SpaceGroupOperations, tests;
# PathMatchTolerance does not exist yet.

grep -n "CreateCacheKey" -A 6 src/dotnet/Mocassin.Symmetry/Structure/StructurePreviewService.cs
# The key includes SpaceGroupDataVersion, SpaceGroupDataSha256, SymmetryContractVersion, structureHash, A, B, C.

ls docs/reference docs/troubleshooting
# docs/reference: file-layout.md index.md project-format.md (no diagnostics.md)
# docs/troubleshooting: index.md (no errors.md)
```

**Files reviewed.** `AGENTS.md` (§3.8–§3.13, §4, §5, §7, §8, §9, §10, §11, §12, §16);
`docs/planning/reviews/README.md`; `docs/planning/reviews/step-design-review-template.md`; this record;
`docs/planning/steps/step-04-transition-model.md` at `4f4c888`; `docs/planning/02-data-contracts.md` at
`4f4c888`; `docs/planning/03-quality-strategy.md`; `docs/planning/05-open-decisions.md`;
`docs/planning/ci-policy.md`; `docs/planning/appendix-b-parity-matrix.md`; `docs/planning/status.md` at HEAD;
`docs/planning/steps/step-03-particle-and-structure-models.md`, `step-05-energy-model.md`,
`step-06-lattice-and-simulation-models.md`, `step-07-validation-and-diagnostics.md`,
`step-08-model-compilation-pipeline.md`, and
`step-24-model-editors-particle-structure-transitions.md` at `4f4c888`;
`docs/planning/adr/0009-contract-manifest-and-contracts-library.md`,
`0010-canonical-messagepack-signatures.md`, and `0013-format-compatibility-and-api-versioning.md`;
`docs/development/code-structure.md`; `docs/reference/project-format.md`; `docs/reference/index.md`;
`contracts/manifest.json`; `contracts/generated/schema/project-v1.schema.json`;
`contracts/generated/signatures/model-v1.json`; `contracts/generated/fixtures/descriptors.json`;
`tools/contracts/generate.py`; `tools/verify.sh`; `tools/structure-policy.json`;
`tools/check-api-compat.sh`.

**Code and test inventory inspected.** `Mocassin.Core`: `Model/ModelDefinition.cs`,
`Model/ModelDefinitionJsonConverter.cs`, `Model/Structure/CellSite.cs`, `Model/Structure/SiteStability.cs`,
`Model/Particles/Particle.cs`, `Model/Particles/IParticleQuery.cs`, `Model/ViewSettings.cs` (the camel-case enum
converter), `Project/Project.cs`, `Project/ProjectJson.cs`, `Project/ProjectHasher.cs`,
`Project/ProjectCanonicalizer.cs`, `Project/ProjectSchema.cs`, `Project/ProjectValidator.cs`,
`Editing/ProjectEditor.cs`, `Geometry/Fractional3.cs`, `Geometry/GeometryTolerances.cs`, `Ids/TypedIds.cs`.
`Mocassin.Symmetry`: `Structure/IStructurePreviewService.cs`, `Structure/StructurePreview.cs`,
`Structure/StructurePreviewService.cs`, `Geometry/StructureSiteReference.cs`,
`Geometry/UnitCellVectorEncoder.cs`, `Geometry/ExpandedSite.cs`, `Geometry/SiteOrbitInfo.cs`.
`Mocassin.Packages`: `Projects/ProjectStore.cs`. `Mocassin.Benchmarks`: `Program.cs`,
`Structure/StructurePreviewBenchmarks.cs`; `tests/reference/bench/baselines.json`. Original reference
(read-only, no copy): `KineticTransitionValidator.cs`, `TransitionAnalyzer.cs`,
`AbstractTransitionValidator.cs`, `StateExchangeGroupValidator.cs`.

**Required scope checks.** All thirteen required review-scope areas were inspected: scope/non-goals/acceptance,
ModelDefinition and JSON/schema round-trip, semantic ordering, every built-in pattern and `Custom`,
path/stability validation, query/preview boundaries, TRN001–TRN027, manifest/schema/fixtures,
PublicAPI/ApiCompat, code layout, Step 03 dependency stability, documentation/navigation and test plan, and
Steps 05/06/07/08/24 interfaces. The findings below record the result.

### Re-review checklist

Marks and notes apply to round 2 only; item text is unchanged.

**Design decisions reviewed**

- [x] Step page design decisions — reviewed the frozen page; M1, M3–M6, M8, M9 and m1–m16 are substantially
  addressed; R2-1–R2-5 are substantive gaps.
- [x] Data contracts and format changes — reviewed the model envelope and transition section; the flat/nested
  shape is contradictory (R2-1).
- [x] Architecture and module boundaries — Core/Symmetry/Packages separation is sound; the preview-query
  boundary still lacks particle access (R2-2).
- [x] ADRs and open decisions — ADR 0009/0010/0013 and the Step 03 additive-v1 precedent are cited; n1 is
  resolved.
- [x] Quality strategy and test plan — property tests, fixtures, tiers, and the benchmark are present; the
  pattern/vehicle/ring cases are missing (R2-3–R2-5).
- [x] Parity matrix and documentation plan — the parity/status/closeout/audit tasks are present; diagnostics
  page ownership is still ambiguous (R2-10).
- [x] Licensing and provenance — no new dependency; MIT/SPDX applies; no GPL source or data is introduced.
- [x] Post-implementation audit plan — the standard AGENTS §3.10 audit applies; the closeout task is present.

**Dependency and interface checks**

- [x] Dependencies are `Done` and their contracts are stable — Step 03 is complete (`step-03` tag; audit
  passed); the required Core/Symmetry types exist.
- [ ] Inputs, outputs, types, and formats are defined — partial: the project JSON shape (R2-1) and the
  `ITransitionQuery` particle access (R2-2) are unresolved.
- [x] Versioning and migration behavior is defined — additive project-v1; Step 03 precedent; ADR 0009 wording
  is addressed (n1).
- [ ] No unresolved cross-step interface question remains — R2-1 (Steps 05/06/08/24) and R2-2 (Steps 06/08/24)
  are open.

**Standard design-review checklist**

- [x] Scope, non-goals, and acceptance criteria are explicit and testable — non-goals are now present; most
  criteria are testable, but the pattern/vehicle/ring cases need added criteria (R2-3–R2-5).
- [ ] Design decisions are complete and internally consistent — the pattern stability/vehicle rules and the
  ring definition contradict the original or each other (R2-3–R2-5).
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined — the framework is
  present; the cases that would catch R2-3–R2-5 are missing.
- [x] Documentation, status, parity, and audit updates are planned — yes; R2-10 is a page-ownership detail.
- [x] Licensing, provenance, and dependency checks are resolved — no new dependency; MIT/SPDX; no GPL copying.
- [x] Risks and mitigations are realistic — the table is plausible; it does not list the pattern/vehicle/ring
  contradictions (R2-3–R2-5).
- [x] Code layout and namespace plan follow the structure policy — the new folders/files fit the limits;
  R2-6/R2-7 are task/layout accuracy issues, not policy violations.
- [ ] No hidden scope, stale placeholders, or unowned decisions — R2-1, R2-2, and R2-7 leave an unowned
  format/interface/public-surface decision.

### Re-review findings

#### Round-1 resolution verification

| Finding | Severity | Status | Verification |
| --- | --- | --- | --- |
| M1 | Major | Resolved | Step page 91–96 and task 531–534 extend `ProjectJson.CanonicalizeModel`; the current method (`ProjectJson.cs` 91–104) still rebuilds only particles, particle sets, and structure, and `ProjectJson.cs` 262 calls it from `ProjectJsonConverter.Write`. `ProjectStore.SaveAsync` (`ProjectStore.cs` 151, 163) uses `ProjectJson.SerializeToUtf8Bytes`, `ProjectHasher.Compute` (`ProjectHasher.cs` 20–25) uses `SerializeCanonicalModel`, and `ProjectEditor.Apply` (`ProjectEditor.cs` 55–62) compares `ComputeDocument`; `ProjectCanonicalizer.ToCanonicalUtf8` delegates to the same serializer. The persistence/hash path is correctly identified. |
| M2 | Major | Partially resolved — R2-2 | The new signature (`TryCreatePreview(KineticTransition/MetropolisTransition, ITransitionQuery, StructureModel, A, B, C, out diagnostics)`, step page 403–419) returns `TransitionPreview?` and follows the Step 03 null+diagnostics boundary (427–428). But `ITransitionQuery` (372–385) exposes no particles, while `ParticleOrder` (391, 437–438) is defined from the model's `Particles`; the service cannot build it as documented. |
| M3 | Major | Resolved | `ReverseTransitionId` is on both transition records (236–249); `TRN027` covers unknown/cross-kind/asymmetric/self links (343); `TRN018` is the missing-reverse warning (334); the data contract persists `reverseTransitionId` (291, 301, 329–330); Step 08 treats the explicit link as a separate transition (201–203); the acceptance criterion is testable (574). |
| M4 | Major | Resolved | The interaction-range rule is removed from Step 04 (352–354); `ENM018` is defined in Step 05 (170–173) and owned by the Step 07 cross-domain stage (Step 07 82–84); Step 07 depends on 04, 05, and 06. No stale `TRN020` range reference remains. |
| M5 | Major | Resolved | The charge-transfer exception is removed; groups are unconditionally homogeneous (137–140; data contract 320–322); charge-transfer mechanisms use homogeneous charge-state groups; Step 08 total-charge conservation remains (198). The original `StateExchangeGroupValidator` hybrid rule is reflected. |
| M6 | Major | Resolved | `TransitionRef`/`TransitionKind` are defined in `Mocassin.Core.Model.Transitions` (211–230); Step 06 now uses `TransitionRef` and keeps the persisted kind-specific arrays (Step 06 128–131; data contract 333–334). |
| M7 | Major | Partially resolved — R2-5, R2-9 | `TransitionPathResolver`, periodic canonicalization, `PathMatchTolerance = 1e-6`, zero/ambiguous `TRN020`, and periodic-image ring detection are specified (293–311). But the ring rule is first/last only (306–307; data contract 326–327) while the original checks every pair, and the canonicalization uses `x - floor(x)` rather than the repository's `TrimToUnitCell` snap. |
| M8 | Major | Resolved as a contract — input gap R2-2 | `StateChangeMatrices` now defines length, `ParticleOrder`, row-major indexing, binary cells, stable/unstable cardinality, and the empty/default contract (435–444). The only remaining issue is the query input needed to build `ParticleOrder` (R2-2). |
| M9 | Major | Resolved | `GeneratesReverse` is constrained to true for `Metropolis2`/`Migration3` and false otherwise (196–199); `TRN024` reports a mismatch (340); the data contract says the same (328); Step 08 reads the flag (201–203). |
| m1 | Minor | Resolved | Non-goals are present (28–37); the `TRN018` acceptance criterion is now testable (574). |
| m2 | Minor | Resolved | Property tests (632), contract fixtures (630), tiers (609–613), and the `TransitionPreviewCeria` benchmark (633) are present; the benchmark file is in `Mocassin.Benchmarks/Transitions/` (524). |
| m3 | Minor | Resolved — R2-10 | Parity (560), status/closeout/audit (561–563), and diagnostics-reference tasks (548–549, 600) are present; the page-ownership detail remains R2-10. |
| m4 | Minor | Resolved | The layout now says `ProjectJson.cs` owns the nested context and `ProjectSchema` is unchanged (89–96); no `ProjectJsonContext.cs` is listed. |
| m5 | Minor | Resolved | Transition editing uses partial `ProjectEditor.Transitions.*.cs` files (204–205, 497–501); `Editing/` has 4 existing files, so 7 total is below the 20-file limit; partial naming follows `code-structure.md` §4. |
| m6 | Minor | Resolved — R2-1 | The manifest task now specifies `status`/`notes`, the two fixtures, schema regeneration, and the `model` signature relationship (550–552). The flat/nested field shape is the remaining ambiguity (R2-1). |
| m7 | Minor | Not resolved — R2-6 | The layout and task add `Ids/TransitionIds.cs` for five IDs (505–506, 535–536), but all five already exist in `Ids/TypedIds.cs` 123–211, added by Step 03 (`dd642d6`). |
| m8 | Minor | Resolved — R2-12 | The 2-position custom Metropolis case, the 3–8 grammar, and static-linker placement are defined (187–195); `TRN015`/`TRN023` scoping remains a wording issue (R2-12). |
| m9 | Minor | Partially resolved — R2-3, R2-4 | `TRN001`–`TRN027` now cover unknown groups, no-unstable chains, cross-kind use, reverse flags, group reuse, the vehicle restriction, and reverse links (317–343). `TRN026` is mistranslated (R2-3), and no rule enforces the built-in pattern stability sequence (R2-4). |
| m10 | Minor | Resolved | The data-contract example is now `[[0.25, 0.25, 0.25], [0.5, 0.25, 0.25], [0.25, 0.75, 0.25]]`, whose first and last points differ (data contract 290). |
| m11 | Minor | Resolved — R2-8 | The preview echoes the transition reference and `(A, B, C)` (388–399, 429–430) and documents the null+diagnostics failure and cache contracts (427–434); the cache key still omits the symmetry versions (R2-8). |
| m12 | Minor | Resolved | `IsAssociation` is intentionally not persisted and Step 08 derives the flag with a test (453–455; Step 08 222–224). |
| m13 | Minor | Partially resolved — R2-3 | The original-defect table and the warning-to-error severity note are present (348–349, 456–468), but the vehicle row and `TRN026` mistranslate the original single-unstable-group restriction (R2-3). |
| m14 | Minor | Resolved | The dedicated ceria transition-state occupation-set task is present (557–558); the current sample still uses the shared oxygen set (`samples/ceria/ceria.mocprj`). |
| m15 | Minor | Resolved | The design names `CamelCaseEnumJsonConverter<T>` (97–99); the converter (`Model/ViewSettings.cs` 382–411) writes camelCase and rejects numeric, empty, and undefined values. |
| m16 | Minor | Resolved | `TRN019` is the total of kinetic plus metropolis transitions (335); names are unique per collection, case-insensitive, and kinetic/metropolis names may coincide (142–146). |
| n1 | Note | Resolved | The additive-v1/pre-release rationale and Step 03 precedent are recorded (108–111); the manifest status/notes and fixtures are updated; ADR 0009 is not waived generally. |
| n2 | Note | Acknowledged — R2-13 | The round-2 record parent is `3338963`; the design content is identical to `4f4c888` except the review record, status, and step-page link. Process note. |
| n3 | Note | Resolved | The single Ubuntu closeout CI task is present (561–563). |

#### New findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **R2-1 — The persisted project shape is contradictory: flat transition sections vs a nested `model.transitions` object.** The step page says "The model stays flat on disk: `model.stateChanges`, `model.stateChangeGroups`, `model.chains`, `model.kineticTransitions`, and `model.metropolisTransitions`" (66, 83–84; task 531–534). `02-data-contracts.md` 149–160 instead shows a nested `"transitions": {}` property inside `model`, and the `#### Transitions` block (262–304) is the value of that nested object. The manifest `model` field has only `particles`, `particleSets`, and `structure` (`contracts/manifest.json`; `project-v1.schema.json`), so the task "add the transition fields" (550–552) does not say whether to add five flat fields or one nested `transitions` object. This changes the converter, `CanonicalizeModel`, fixtures, the generated schema, and the Step 05/06/08/24 model shape. | Step page 66, 83–84, 531–534, 550–552; `02-data-contracts.md` 149–160, 262–304; `contracts/manifest.json` project `model` field; `contracts/generated/schema/project-v1.schema.json` `/properties/model`; `docs/reference/project-format.md` 174. |
| **Major** | **R2-2 — `ITransitionQuery` exposes no particles, so `TransitionPreview.ParticleOrder` and `StateChangeMatrices` cannot be built as specified.** The query has only transition-domain arrays and `Find` overloads (372–385). `ParticleOrder` is defined as "the model's `Particles` sorted by ID, followed by the implicit void particle" (391, 437–438), and the matrices are indexed by that order (439–441). The preview receives only `ITransitionQuery`, `StructureModel`, and counts (403–419); the text claims the query lets the service "resolve ... particles" (424–426), which is false. A service that derives the order from `StateChanges` omits unreferenced particles (for example ceria Ce) and disagrees with Step 08/24. | Step page 372–385, 391, 403–419, 424–426, 435–444; `IParticleQuery.cs` (Project already implements `IParticleQuery`); `Project.cs` 12; `StateChange.cs` (model particles are separate). |
| **Major** | **R2-3 — `TRN026` reverses the original vehicle restriction and would reject the supported `Vehicle6` pattern.** `TRN026` says "Vehicle chain has more than one unstable state-change pair" (342). `Vehicle6` is a documented built-in pattern with stability sequence `S,U,S,S,U,S` (179), i.e. two unstable positions. The original `AbstractTransitionValidator.AddNormalVehicleRestrictionValidation` returns early when `unstableStateGroups.Count > 1` and only warns when exactly one unstable group has `StatePairCount > 1`. The m13 row repeats the mistranslation (468). | Step page 179, 342, 468; original `AbstractTransitionValidator.cs` `AddNormalVehicleRestrictionValidation`. |
| **Major** | **R2-4 — No rule enforces the built-in pattern stability sequence, so paths with the wrong stable/unstable shape pass validation.** The step page requires the resolved path sequence to match the pattern table (176–179, 304–305). `TRN012` only checks the site against the group at the same index (328); `TRN011`/`TRN013`/`TRN022` only check endpoints, consecutive unstables, and at-least-one-unstable (327, 329, 338). An `Interstitialcy5` chain with groups and path `S,S,U,S,S` passes all four, although the table requires `S,U,S,U,S`. The test plan's "Patterns" row only checks linker sequences (616) and the acceptance criteria only cover the valid cases (569–573). | Step page 176–179, 304–305, 327–329, 338, 569–573, 616. |
| **Major** | **R2-5 — Ring detection only compares the first and last canonical positions; the original rejects any repeated position.** The step page (306–307) and data contract (326–327) define a ring as first/last canonical equality. The original `TransitionAnalyzer.ContainsRingTransition` checks every pair of positions (`for i` / `for j = i + 1`). An interior repeat such as `0→1→0→2` passes the new rule: endpoints are stable, at least one position is unstable, no two unstables are consecutive, and `TRN012` passes if the groups match. The "Paths" test row only names literal and periodic-image rings (618). | Step page 306–307, 618; `02-data-contracts.md` 326–327; original `TransitionAnalyzer.cs` `ContainsRingTransition`. |
| **Minor** | **R2-6 — The code layout and task add `Ids/TransitionIds.cs` for five typed IDs that already exist.** `StateChangeId`, `StateChangeGroupId`, `TransitionChainId`, `KineticTransitionId`, and `MetropolisTransitionId` are already in `Ids/TypedIds.cs` 123–211, added by Step 03 (`dd642d6`). A new file would duplicate the type definitions. The task should say "reuse the existing IDs; no new file". | Step page 505–506, 535–536; `Ids/TypedIds.cs` 123–211; `git log -- src/dotnet/Mocassin.Core/Ids/TypedIds.cs`; `tools/structure-policy.json` `allowedMultiTypeSuffixes` includes `Ids`. |
| **Minor** | **R2-7 — Several referenced public surfaces are absent from the code layout and task list.** `ProjectHasher.ComputeTransition` (431–433) is not in the `Project/` layout (502–504) or tasks; `Project` implementing `ITransitionQuery` (423) is not in the layout (no `Project/Project.cs`); `TransitionRefJsonConverter` (214) is not listed; the benchmark baseline in `tests/reference/bench/baselines.json` is implied by the test row (633) but not tasked. This leaves the PublicAPI/ApiCompat and benchmark work incomplete on paper. | Step page 214, 423, 431–434, 502–506, 540–541, 553–554, 633. |
| **Minor** | **R2-8 — The preview cache key omits the symmetry catalog and contract versions.** The key is `(transitionRef, transitionModelHash, structureHash, A, B, C)` (431–434). `StructurePreviewService.CreateCacheKey` includes `SpaceGroupDataVersion`, `SpaceGroupDataSha256`, and `SymmetryContractVersion` (`StructurePreviewService.cs` 200–207). The transition preview expands paths through the symmetry engine, so a catalog/contract upgrade can change the preview without changing either hash; the cache-separation test (623) does not cover it. | Step page 431–434, 623; `StructurePreviewService.cs` 200–207; `SymmetryVersionInfo.cs`. |
| **Minor** | **R2-9 — Path canonicalization uses `x - floor(x)` instead of the repository's `TrimToUnitCell` snap.** `Fractional3.TrimToUnitCell()` (`Fractional3.cs` 114–150) snaps components within `GeometryTolerances.FractionalTrimTolerance` (1e-10) to the integer before wrapping and is used by `UnitCellVectorEncoder` (158, 297) and `SpaceGroupOperations`. With `x - floor(x)`, a point at `-1e-9` becomes `0.999999999` and does not match an origin site at `0.0`, although the periodic distance is 1e-9. The design should use `TrimToUnitCell()` or a wrapped-difference comparison and add a boundary test. | Step page 296–299; `Fractional3.cs` 114–150; `GeometryTolerances.cs` 23; `UnitCellVectorEncoder.cs` 158, 297. |
| **Minor** | **R2-10 — The diagnostics and troubleshooting pages do not exist, but the design says to "update" them; Step 07 owns the catalog.** `docs/reference/` has only `file-layout.md`, `index.md`, and `project-format.md`; `docs/troubleshooting/` has only `index.md`. `docs/reference/index.md` says diagnostics are added with Step 07, and Step 07's Documentation section (200) owns the complete catalog. The Step 04 task (548–549) and Documentation section (600–601) should say whether Step 04 creates the TRN section or Step 07 creates the page. | Step page 548–549, 600–601; `docs/reference/index.md`; `docs/reference/` and `docs/troubleshooting/` listings; Step 07 200. |
| **Minor** | **R2-11 — `TryCreatePreview` repeats the Step 03 CA1716 parameter-name problem.** The signature uses `StructureModel structure` (406, 415). Step 03 recorded that `structure` triggers CA1716 and renamed the parameter to `structureModel` (`step-03` 43–44). Warnings are errors (`AGENTS.md` §8; `.editorconfig`), so the Step 04 signature as written will fail the build unless it is renamed or carries the same documented suppression. | Step page 406, 415; step-03 43–44; `AGENTS.md` §8. |
| **Minor** | **R2-12 — `TRN015`/`TRN023` do not clearly cover a 2-position `Custom` Metropolis chain.** The design says a 2-position `Custom` chain is a Metropolis chain (189–190), but `TRN015` says a Metropolis transition must use a "2-position Metropolis chain" (331) and `TRN023` only rejects `Metropolis2` on the kinetic side (339). If `TRN015` is implemented as `Pattern == Metropolis2`, a valid 2-position `Custom` Metropolis transition is rejected; if it accepts any 2-position chain, the wording should say so. | Step page 189–190, 331, 339. |
| **Note** | **R2-13 — Record parent is the re-review request, not the design commit.** The record commit's parent is `3338963`; `git diff 4f4c888..3338963 --stat` shows only the review record, `status.md`, and the step-page design-review link. The design content is otherwise identical. Process note for the human owner, not a design finding. | Commands in Evidence. |
| **Note** | **R2-14 — Sample location wording is ambiguous.** The page says "Samples live under `samples/transitions/`" (526), while the task adds ceria KMC/MMC/polaron-hop samples and updates `samples/ceria/ceria.mocprj` (555–558). Clarify which files are new under `samples/transitions/` and which update the existing ceria sample. | Step page 526, 555–558; `samples/ceria/ceria.mocprj`. |
| **Note** | **R2-15 — `TRN018` is described as a chain rule in one place and a transition rule in another.** Section 3 says "TRN018 warns when a complex chain has no reverse link" (201–202), while the table (334) and acceptance criterion (574) are transition-scoped. The reverse link lives on the transition record, so the table/acceptance are correct; reword section 3. | Step page 201–202, 334, 574. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

### Re-review verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Re-review required
- [ ] Waived

Follow-up owner: Planner and Step 04 agent (design revision and findings resolution)
Follow-up step: Step 04 — Transition Model; a fresh re-review is required after the design commit

**Changes required.** Five Major findings remain (R2-1–R2-5). The fix round resolved M1, M3–M6, M8, M9 and
most minors/notes, and the design is substantially stronger: canonical persistence, reverse links,
`TransitionRef`, `StateChangeMatrices`, the custom grammar, and the test plan are now present. However, the
persisted project shape is still contradictory (R2-1), the preview query still cannot supply the particle order
(R2-2), `TRN026` would reject the supported `Vehicle6` pattern (R2-3), no rule enforces the built-in pattern
stability sequences (R2-4), and the ring rule is narrower than the original (R2-5). Each is a material design or
cross-step interface issue, so the step cannot start as written. No Blocker is recorded, and the human owner may
treat any Major as blocking. A re-review requires a new design commit with fixes; do not create the step branch
or start implementation before that.

## Round 2 resolutions

Design revision: `43bad05` — design and data-contract corrections for round 2. This section is owned by the
planner/step agent and does not alter the reviewer's round-2 findings, evidence, or verdict.

| Finding | Status | Resolution |
| --- | --- | --- |
| R2-1 | Resolved — awaiting re-review | Kept the flat model shape established by Step 03 and removed the nested `model.transitions` sketch from the data contract; the envelope now lists the five flat transition sections. The manifest task adds five flat fields. |
| R2-2 | Resolved — awaiting re-review | Added an `IParticleQuery` parameter to both `TryCreatePreview` overloads and defined `ParticleOrder` from `Particles` plus `VoidParticle`. |
| R2-3 | Resolved — awaiting re-review | Replaced TRN026 with "an unstable position's group contains more than one state change"; it applies per unstable position and no longer limits the number of unstable positions, so `Vehicle6` is valid. |
| R2-4 | Resolved — awaiting re-review | Added TRN028 for the resolved path stability sequence against the built-in pattern table, plus acceptance and test cases for `Interstitialcy5`/vehicle sequences. |
| R2-5 | Resolved — awaiting re-review | Ring detection now rejects any repeated canonical resolved position, including interior returns and periodic images, and the data contract says the same. |
| R2-6 | Resolved — awaiting re-review | Removed `Ids/TransitionIds.cs`; the five typed IDs already exist in `Ids/TypedIds.cs` from Step 03 and are reused. |
| R2-7 | Resolved — awaiting re-review | Added `Project/Project.cs` (implements `ITransitionQuery`), `ProjectHasher.cs` (`ComputeTransition`), `TransitionRefJsonConverter.cs`, and the benchmark-baseline task to the layout/tasks. |
| R2-8 | Resolved — awaiting re-review | Preview cache key now includes catalog data version/SHA-256 and symmetry contract version, with injectable versions and a cache-separation test. |
| R2-9 | Resolved — awaiting re-review | Path canonicalization now uses `Fractional3.TrimToUnitCell()` after a finite check, with `PathMatchTolerance` for site matching and a boundary test. |
| R2-10 | Resolved — awaiting re-review | Added `docs/reference/transitions.md` as the Step 04 page; Step 07 owns the consolidated diagnostics/troubleshooting pages and links to it. |
| R2-11 | Resolved — awaiting re-review | Renamed the preview parameter to `structureModel`, matching the Step 03 CA1716 resolution. |
| R2-12 | Resolved — awaiting re-review | TRN015 now accepts `Metropolis2` or a 2-position `Custom` chain; TRN023 rejects both on the kinetic side. |
| R2-13 | Acknowledged — awaiting re-review | Process note only; the round-2 text is unchanged and the revision is a new commit. |
| R2-14 | Resolved — awaiting re-review | New standalone samples live under `samples/transitions/`; `samples/ceria/ceria.mocprj` is updated in place for the dedicated unstable-site set. |
| R2-15 | Resolved — awaiting re-review | Section 3 now states that TRN018 is transition-scoped, matching the table and acceptance criterion. |

## Re-review round 3

### Re-review request

| Field | Value |
| --- | --- |
| Step | 04 — Transition Model |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Design review status | Changes required |
| Reviewed commit | `43bad05` |
| Docs branch | `main` |
| Review date | 2026-11-08 |
| Reviewer | Independent planning auditor (fresh context, round 3; no shared conversation with the planner or implementor) |

Step page: [`Step 04 — Transition Model`](../steps/step-04-transition-model.md)

The design content for round 3 is frozen at `43bad05`. The delta from the round-2 commit `4f4c888` is the
round-2 fix; the review request and status changes are metadata only. Verify the delta yourself with
`git diff --stat 4f4c888..43bad05`.

Reviewer: append your independence statement, evidence, findings, and verdict for this round below. Keep the
round-1 and round-2 text unchanged.

### Re-review checklist (round 3)

Marks and notes apply to round 3 only; item text is unchanged.

**Design decisions reviewed**

- [x] Step page design decisions — reviewed the frozen page; R2-1–R2-15 are resolved or acknowledged; R3-1–R3-10 are new.
- [x] Data contracts and format changes — the flat model envelope is now consistent (R2-1); the transition section, semantic ordering, and canonical-hash rules were re-read.
- [x] Architecture and module boundaries — Core/Symmetry/Packages separation is sound in general, but the `TransitionPathResolver` placement is the R3-1 blocker.
- [x] ADRs and open decisions — ADR 0009/0010/0013 and the Step 03 additive-v1 precedent are cited; no open decision blocks the step.
- [x] Quality strategy and test plan — property tests, contract fixtures, tiers, and the benchmark are present; the orbit-member path case is missing (R3-1).
- [x] Parity matrix and documentation plan — the parity/status/closeout/audit and `transitions.md` tasks are present; R3-3/R3-8 are page-reference details.
- [x] Licensing and provenance — no new dependency; MIT/SPDX applies; no GPL source or data is introduced.
- [x] Post-implementation audit plan — the standard AGENTS §3.10 audit and closeout task apply.

**Dependency and interface checks**

- [x] Dependencies are `Done` and their contracts are stable — Step 03 is complete (`step-03` tag; audit passed); `IParticleQuery`, `Project`, `CellSite`, `StructureModel`, and `Fractional3.TrimToUnitCell` exist.
- [ ] Inputs, outputs, types, and formats are defined — partial: the path-to-site resolution contract is not implementable as placed (R3-1).
- [x] Versioning and migration behavior is defined — additive project-v1; Step 03 precedent; ADR 0009 wording is addressed.
- [ ] No unresolved cross-step interface question remains — R3-1 (Steps 04/08/24) and R3-4 (preview cache) remain; the Step 05/06/07/08/24 interfaces otherwise line up.

**Standard design-review checklist**

- [ ] Scope, non-goals, and acceptance criteria are explicit and testable — non-goals are present; the ceria and vehicle criteria are not achievable under the literal matcher (R3-1).
- [ ] Design decisions are complete and internally consistent — R3-1, R3-2, R3-3, R3-6 remain.
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined — the framework is present; the orbit-member acceptance/property case is missing (R3-1).
- [x] Documentation, status, parity, and audit updates are planned — yes; R3-3/R3-8 are page-reference details.
- [x] Licensing, provenance, and dependency checks are resolved — no new dependency; MIT/SPDX; no GPL copying.
- [x] Risks and mitigations are realistic — the table is plausible; it omits the orbit-member resolution and particle-order cache risks (R3-1/R3-4).
- [x] Code layout and namespace plan follow the structure policy — new folders/files fit the 20-file, depth-4, 800-line limits; no policy violation.
- [ ] No hidden scope, stale placeholders, or unowned decisions — R3-1 leaves the resolver placement/ownership open; R3-2/R3-3 are stale text.

### Re-review evidence

**Independence.** I am a fresh planning auditor for round 3. I did not author or implement the Step 04 design, the fix rounds, or any implementation; I have no shared conversation with the planner or implementor. I read the frozen design content and the repository myself; I did not rely on the request summary or the resolution tables.

**Frozen state and worktree.** Worktree `/workspace/mocassin`, branch `main`, `HEAD` `ff4cf81`. The request names `43bad05` as the frozen design commit. `git rev-parse 43bad05` =
`43bad0525dd36ed575f97da903d2c16a0865b274`; `git rev-parse 4f4c888` =
`4f4c888a191c1e2c75eb9b71afe783cdb3d55e6e`. `git diff 43bad05..ff4cf81 --stat` shows only
`docs/planning/reviews/step-04.md`, `docs/planning/status.md`, and the step-page design-review link; `git diff
43bad05..ff4cf81 -- docs/planning/steps/step-04-transition-model.md docs/planning/02-data-contracts.md` shows
only the one-line link change (`Re-review required at 43bad05`). The frozen design content is therefore
identical at `43bad05` and `HEAD`. The record commit's parent will be `ff4cf81`, not `43bad05`, because the
round-3 request sits between the design commit and the record; this is the same process note as rounds 1 and 2
(R2-13).

**Commands run (from `/workspace/mocassin`):**

```sh
git status --short
# (no output: clean worktree)

git log --oneline --decorate -6
# ff4cf81 (HEAD -> main) docs: request step-04 design re-review round 3 [skip ci]
# 43bad05 docs: resolve step-04 re-review findings [skip ci]
# 3a50206 docs: record step-04 design re-review Changes required [skip ci]
# 3338963 docs: request step-04 design re-review [skip ci]
# 4f4c888 docs: revise step-04 design after review [skip ci]
# 12c8107 docs: record step-04 design review Changes required [skip ci]

git rev-parse 43bad05
# 43bad0525dd36ed575f97da903d2c16a0865b274

git rev-parse 4f4c888
# 4f4c888a191c1e2c75eb9b71afe783cdb3d55e6e

git show --stat 43bad05
# docs/planning/02-data-contracts.md              |  20 ++--
# docs/planning/steps/step-04-transition-model.md | 129 +++++++++++++++---------
# 2 files changed, 91 insertions(+), 58 deletions(-)

git diff --stat 4f4c888..43bad05 -- docs/planning
# docs/planning/02-data-contracts.md              |  20 +-
# docs/planning/reviews/step-04.md                | 336 +++++++++++++++++++++++-
# docs/planning/status.md                         |  10 +-
# docs/planning/steps/step-04-transition-model.md | 131 +++++----
# 4 files changed, 426 insertions(+), 71 deletions(-)

git diff 4f4c888..43bad05 -- docs/planning
# Read in full: the step-page revision, the data-contract delta, the status change, and the round-2 record updates.

git show 43bad05:docs/planning/steps/step-04-transition-model.md
# Read in full (680 lines): sections 1-11, code layout, tasks, acceptance, documentation, tests, risks.

git show 43bad05:docs/planning/02-data-contracts.md
# Read in full (1,193 lines): model envelope 149-173, transition section 268-340, canonical hash 135-147,
# simulation shape 410-433.

git diff --check 4f4c888..43bad05
# (no output)

git diff --stat 43bad05..ff4cf81
# docs/planning/reviews/step-04.md                | 73 ++++++++++++++++++++++++-
# docs/planning/status.md                         | 11 ++--
# docs/planning/steps/step-04-transition-model.md |  2 +-
# 3 files changed, 77 insertions(+), 9 deletions(-)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.22s

python3 tools/contracts/generate.py --check
# Contract artifacts are current (17 files)
```

**Additional read-only checks.**

```sh
wc -l src/dotnet/Mocassin.Core/Editing/ProjectEditor.cs
# 506

grep -n "StateChangeId\|StateChangeGroupId\|TransitionChainId\|KineticTransitionId\|MetropolisTransitionId" \
  src/dotnet/Mocassin.Core/Ids/TypedIds.cs
# 123/142/161/180/199: all five typed IDs already exist (added by Step 03, dd642d6)

grep -n "public const double" src/dotnet/Mocassin.Core/Geometry/GeometryTolerances.cs
# FractionalTrimTolerance = 1e-10; PathMatchTolerance does not exist yet

grep -n "CanonicalizeModel" -A 14 src/dotnet/Mocassin.Core/Project/ProjectJson.cs
# The current method rebuilds only Particles, ParticleSets, and Structure; ProjectJsonConverter.Write
# (ProjectJson.cs 262) calls it, ProjectHasher.Compute uses SerializeCanonicalModel, and
# ProjectCanonicalizer.ToCanonicalUtf8 uses the same serializer.

cat src/dotnet/Mocassin.Core/Mocassin.Core.csproj
# No ProjectReference to Mocassin.Symmetry.

grep -n "TrimToUnitCell" -A 30 src/dotnet/Mocassin.Core/Geometry/Fractional3.cs
# TrimComponent snaps within FractionalTrimTolerance (1e-10), then maps into [0,1).

grep -n "IsInsideUnitCell" -A 10 src/dotnet/Mocassin.Core/Model/Structure/StructureModelValidator.cs
# Reference sites must be finite and inside [0,1) within the fractional trim tolerance.

python3 - <<'PY'
import json
p=json.load(open('samples/ceria/ceria.mocprj'))
for s in p['model']['structure']['sites']:
    print(s['name'], s['fractional'], s['stability'])
PY
# Ce [0, 0, 0] stable
# O [0.25, 0.25, 0.25] stable
# O transition [0.5, 0.25, 0.25] unstable
# There is no O reference site at [0.75, 0.25, 0.25].

grep -n "GetEntryValueAt" -A 4 /workspace/mocassin-original/src/ModelBuilder/ICon.Framework.Symmetry/Analysis/CellAnalysis/UnitCellAdapter.cs
# GetEntryValueAt uses VectorEncoder.TryEncode, i.e. it resolves any fractional position (including a
# symmetry-equivalent P1 image) to its unit-cell entry/reference site.

grep -n "ContainsRingTransition" -A 12 \
  /workspace/mocassin-original/src/ModelBuilder/ICon.Model/Transitions/Modeling/Logic/TransitionAnalyzer/TransitionAnalyzer.cs
# Any-pair check; raw Fractional3D coordinates with the space-group comparer.

grep -n "AddNormalVehicleRestrictionValidation" -A 12 \
  /workspace/mocassin-original/src/ModelBuilder/ICon.Model/Transitions/Manager/Services/Validators/AbstractTransitionValidator.cs
# Returns early when unstableStateGroups.Count > 1; only a single unstable group with more than one
# pair is restricted. Vehicle6 is therefore supported.

grep -n "5-Site 2-Species Vehicle\|6-Site 2-Species Vehicle\|5-Site Interstitialcy\|7-Site Interstitialcy" -A 4 \
  /workspace/mocassin-original/docs/userguide-md/transition-model.md
# Confirms S,S,U,S,S (Vehicle5), S,U,S,S,U,S (Vehicle6), S,U,S,U,S (Interstitialcy5),
# and S,U,S,U,S,U,S (Interstitialcy7).
```

**Files reviewed.** `AGENTS.md` (§3.8–§3.13, §4, §5, §7, §8, §9, §10, §11, §12, §16); `docs/planning/reviews/README.md`;
`docs/planning/reviews/step-design-review-template.md`; this record; `docs/planning/steps/step-04-transition-model.md`
at `43bad05`; `docs/planning/02-data-contracts.md` at `43bad05`; `docs/planning/03-quality-strategy.md`;
`docs/planning/05-open-decisions.md`; `docs/planning/ci-policy.md`; `docs/planning/appendix-b-parity-matrix.md`;
`docs/planning/status.md` at HEAD; `docs/planning/steps/step-03-particle-and-structure-models.md`,
`step-05-energy-model.md`, `step-06-lattice-and-simulation-models.md`,
`step-07-validation-and-diagnostics.md`, `step-08-model-compilation-pipeline.md`, and
`step-24-model-editors-particle-structure-transitions.md` at `43bad05`;
`docs/planning/adr/0009-contract-manifest-and-contracts-library.md`,
`0010-canonical-messagepack-signatures.md`, and `0013-format-compatibility-and-api-versioning.md`;
`docs/development/code-structure.md`; `docs/reference/project-format.md`; `docs/reference/index.md`;
`contracts/manifest.json`; `contracts/generated/schema/project-v1.schema.json`;
`contracts/generated/signatures/model-v1.json`; `contracts/generated/fixtures/descriptors.json`;
`tools/contracts/generate.py`; `tools/verify.sh`; `tools/structure-policy.json`;
`tools/check-api-compat.sh`; `tools/update-api-baseline.sh`.

**Code and test inventory inspected.** `Mocassin.Core`: `Model/ModelDefinition.cs`,
`Model/ModelDefinitionJsonConverter.cs`, `Model/Structure/CellSite.cs`,
`Model/Structure/StructureModelValidator.cs`, `Model/Structure/IStructureQuery.cs`,
`Model/Particles/IParticleQuery.cs`, `Model/ViewSettings.cs` (camel-case enum converter),
`Project/Project.cs`, `Project/ProjectJson.cs`, `Project/ProjectHasher.cs`,
`Project/ProjectCanonicalizer.cs`, `Project/ProjectSchema.cs`, `Project/ProjectValidator.cs`,
`Editing/ProjectEditor.cs`, `Geometry/Fractional3.cs`, `Geometry/GeometryTolerances.cs`,
`Ids/TypedIds.cs`, `Diagnostics/Diagnostic.cs`, `Diagnostics/ModelPath.cs`.
`Mocassin.Symmetry`: `Structure/IStructurePreviewService.cs`, `Structure/StructurePreview.cs`,
`Structure/StructurePreviewService.cs` (cache-key construction), `Geometry/UnitCellVectorEncoder.cs`,
`Geometry/StructureSiteReference.cs`. `Mocassin.Architecture.Tests/StructureChecks/StructureChecker.cs`.
`Mocassin.Packages/Projects/ProjectStore.cs` and `ProjectValidationService.cs` from the round-2 inventory.
Original reference (read-only, no copy): `KineticTransitionValidator.cs`, `TransitionAnalyzer.cs`,
`AbstractTransitionValidator.cs`, `StateExchangeGroupValidator.cs`, `UnitCellAdapter.cs`,
`ConnectorPattern.cs`, and `docs/userguide-md/transition-model.md`.

**Required scope checks.** All thirteen required review-scope areas were re-checked against the frozen
content: scope/non-goals/acceptance, `ModelDefinition` and JSON/schema round-trip, semantic ordering, every
built-in pattern and `Custom`, path/stability validation, query/preview boundaries, TRN001–TRN028, manifest/
schema/fixture tasks, PublicAPI/ApiCompat, code layout, Step 03 dependency stability, documentation/navigation
and the test plan, and the Step 05/06/07/08/24 interfaces. The findings below record the result.

### Re-review findings

#### Round-2 resolution verification

| Finding | Severity | Status | Verification |
| --- | --- | --- | --- |
| R2-1 | Major | Resolved | The data contract's model envelope now lists the five flat sections (`02-data-contracts.md` 149-167), and the step page says there is no nested `model.transitions` object (83-86). The manifest task adds five flat fields (575-577); the manifest `model` field currently has only `particles`, `particleSets`, and `structure` (`contracts/manifest.json`, `project-v1.schema.json`). The flat shape is consistent with Step 03 and the converter. |
| R2-2 | Major | Resolved | Both `TryCreatePreview` overloads now take `IParticleQuery particles` (412-430); `ParticleOrder` is defined from `IParticleQuery.Particles` sorted by ID plus `VoidParticle` (453-454). `IParticleQuery` exists in `Mocassin.Core/Model/Particles/IParticleQuery.cs` with exactly `Particles`, `VoidParticle`, and the `Find` overloads; `Project` already implements it (`Project.cs` 12). |
| R2-3 | Major | Resolved | `TRN026` is now "An unstable position's group contains more than one state change" (350), and §7 says the unstable group must be exactly one `[D, ∅]` pair (308-310). The original `AddNormalVehicleRestrictionValidation` returns early when `unstableStateGroups.Count > 1`, so `Vehicle6` (two unstable positions) is supported; the defect table now says the rule does not limit the number of unstable positions (484). |
| R2-4 | Major | Resolved | `TRN028` is in the table (352) and §7 (311-313); the pattern table gives S,U,S for `Migration3`, S,U,S,U,S for `Interstitialcy5`, S,U,S,U,S,U,S for `Interstitialcy7`, S,S,U,S,S for `Vehicle5`, and S,U,S,S,U,S for `Vehicle6` (176-179). The original user guide confirms those sequences, so the new rule is scientifically consistent. |
| R2-5 | Major | Resolved | "A `referencePath` contains a ring when any two canonical resolved positions are equal, including when one point is a periodic image of another" (`02-data-contracts.md` 330-331); the step page rejects an interior return as well as an endpoint ring (263-265, 314-315). The original `ContainsRingTransition` checks every pair; the new rule catches the interior `0→1→0→2` case. |
| R2-6 | Minor | Resolved | `Ids/TransitionIds.cs` is removed from the layout; the page says the five typed IDs already exist in `Ids/TypedIds.cs` and must not be duplicated (525, 555-557). `TypedIds.cs` 123-211 contains all five. |
| R2-7 | Minor | Resolved | The layout adds `Project/Project.cs` (implements `ITransitionQuery`), `ProjectHasher.cs` (`ComputeTransition`), and `Model/Transitions/TransitionRefJsonConverter.cs` (504, 520-522); the benchmark-baseline task is present (580-581). |
| R2-8 | Minor | Resolved | The cache key now includes `catalogDataVersion`, `catalogDataSha256`, and `symmetryContractVersion` (446-450), matching `StructurePreviewService.CreateCacheKey` (`StructurePreviewService.cs` 200-207); the test row covers symmetry versions (652). |
| R2-9 | Minor | Resolved with residual boundary gap | The design uses `Fractional3.TrimToUnitCell()` (300-304), which snaps within `FractionalTrimTolerance` (1e-10) and maps into `[0,1)` (`Fractional3.cs` 114-150). The `PathMatchTolerance = 1e-6` match is specified. The boundary discontinuity remains as R3-5. |
| R2-10 | Minor | Resolved with a stale cross-reference | The task and Documentation section now add `docs/reference/transitions.md` and assign the consolidated `diagnostics.md`/`troubleshooting/errors.md` to Step 07 (572-574, 628-630). The validation section still says the help anchor is in `diagnostics.md` (356); see R3-3. |
| R2-11 | Minor | Resolved | Both overloads use `StructureModel structureModel` (416, 426), matching the Step 03 CA1716 resolution. |
| R2-12 | Minor | Resolved | `TRN015` accepts `Metropolis2` or a 2-position `Custom` chain (339); `TRN023` rejects both on the kinetic side (347). |
| R2-13 | Note | Acknowledged | The round-2 record parent is `3338963`, not `4f4c888`; the design content is identical except the review record, status, and step-page link. Process note only. |
| R2-14 | Note | Resolved | New standalone samples live under `samples/transitions/`; `samples/ceria/ceria.mocprj` is updated in place (545-546, 582-586). |
| R2-15 | Note | Resolved | Section 3 now says `TRN018` is transition-scoped, matching the table and the acceptance criterion (202-206, 342, 602). |

#### New findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Blocker** | **R3-1 — Path resolution matches only literal `CellSite.Fractional` coordinates, so symmetry-equivalent P1 positions cannot be resolved; the ceria 3-site migration and the vehicle acceptance criteria are not implementable as written.** `TransitionPathResolver` is placed in `Mocassin.Core.Model.Transitions` (297-299, 509), and the match is "compared component-wise to `CellSite.Fractional`" after `TrimToUnitCell` (300-304). `CellSite.Fractional` holds only the symmetry-reduced reference site (one O site in ceria), while a valid 3-site O→U→O path must use a different physical O site, which is an orbit member. In the ceria sample the only O reference site is `[0.25, 0.25, 0.25]` and the unstable site is `[0.5, 0.25, 0.25]`; a natural path endpoint `[0.75, 0.25, 0.25]` is an Fm-3m orbit member (8c) but matches no `CellSite`, so it is rejected with `TRN020`. The data contract's own example endpoint `[0.25, 0.75, 0.25]` is likewise an 8c orbit member of `[0.25, 0.25, 0.25]` (`02-data-contracts.md` 293). The original accepted any P1 position: `KineticTransitionValidator.cs` 86 calls `unitCellProvider.GetEntryValueAt`, and `UnitCellAdapter.GetEntryValueAt` uses `VectorEncoder.TryEncode` to resolve the fractional position to its unit-cell entry/reference site. Because `Mocassin.Core.csproj` has no reference to `Mocassin.Symmetry`, the Core resolver cannot build the P1 map; the design does not define an abstraction or a precomputed P1/site map. The step's acceptance criteria (592-601) therefore cannot be satisfied for the ceria sample. | Step page 297-315, 592-601; `02-data-contracts.md` 293, 326-331; `CellSite.cs` 23; `StructureModelValidator.cs` 90, 153-161; `Mocassin.Core.csproj`; `UnitCellVectorEncoder.cs`; `samples/ceria/ceria.mocprj`; original `KineticTransitionValidator.cs` 86 and `UnitCellAdapter.cs` 121. |
| **Minor** | **R3-2 — The TRN ownership sentence still stops at `TRN027`.** The table now defines `TRN028` (352), but the validation section says "`TRN001`–`TRN027` are owned by `Mocassin.Core`" (354). The task and code layout use TRN001–TRN028 (513, 572). | Step page 352-356, 513, 572. |
| **Minor** | **R3-3 — The help-anchor target is stale.** The validation section says each code has a help anchor "documented in `docs/reference/diagnostics.md`" (356), while the task and Documentation section create `docs/reference/transitions.md` and assign `diagnostics.md` to Step 07 (572-574, 628-630). | Step page 356, 572-574, 628-630; `docs/reference/index.md` 10. |
| **Minor** | **R3-4 — The preview cache key does not cover the full `ParticleOrder`.** `transitionModelHash` is the SHA-256 of the closure "(transition, chain, groups, state changes, and referenced particles)" (446-450), but `ParticleOrder` is built from all `IParticleQuery.Particles` sorted by ID plus `VoidParticle` (453-454). Adding or removing an unreferenced particle changes the matrix dimensions and index order while leaving the closure hash unchanged, so a cached preview can be stale. The cache-separation test (652) does not cover this. | Step page 446-454, 652; `IParticleQuery.cs`; `StateChangeMatrices` contract 451-460. |
| **Minor** | **R3-5 — `PathMatchTolerance` is not honored at the unit-cell boundary.** `TrimToUnitCell` snaps only within `1e-10`; a path point at `0.9999995` and a site at `0.0` are a periodic distance of `5e-7` apart, within the stated `1e-6` tolerance, but the component-wise comparison sees `0.9999995` and reports zero matches (`TRN020`). A wrapped component difference (`min(abs(d), 1-abs(d))`) or a boundary-aware comparer is needed. | Step page 300-304, 648; `Fractional3.cs` 114-150; `GeometryTolerances.cs` 22. |
| **Note** | **R3-6 — `TRN026` wording differs across the table, §7, and the defect table.** The table says "contains more than one state change" (350), §7 and the defect table say "exactly one `[D, ∅]` pair" (308-310, 484). The rule set is complete because `TRN005` covers an empty group and `TRN012` covers a single non-void pair, but the table should state the exact condition or reference those codes. | Step page 308-310, 350, 484. |
| **Note** | **R3-7 — Non-positive cell counts are not covered by the preview failure contract.** The service "never throws for invalid user data" (442-443), but the Step 03 `StructurePreviewService.TryCreatePreview` throws `ArgumentOutOfRangeException` for non-positive `(A, B, C)` (`StructurePreviewService.cs` 79-82). The Step 04 contract should say whether `(A, B, C)` are diagnostics or argument errors, especially for the Step 24 UI. | Step page 410-443; `StructurePreviewService.cs` 78-82; step-03 failure boundary. |
| **Note** | **R3-8 — The documentation task does not update the reference/user-guide/physics index pages.** The task adds the new pages to `zensical.toml` (587) but not to `docs/reference/index.md` (which currently lists only file layout, project format, and future sections) or the user-guide/physics indexes. `zensical build --strict` does not flag an orphaned page, so the link plan is incomplete. | Step page 617-634; `docs/reference/index.md`; `zensical.toml` 38-42. |
| **Note** | **R3-9 — `ITransitionQuery` and `IParticleQuery` are separate parameters without a same-snapshot invariant.** The preview can be called with two different project snapshots, producing a `ParticleOrder` that disagrees with the transition closure. Document that both must come from the same immutable `Project` snapshot, or pass one combined query. | Step page 410-437. |
| **Note** | **R3-10 — The preview-cache risk row is stale.** The risk table still says the key includes only "transition hash, structure hash, and `(A,B,C)`" (674), while the design now adds the symmetry catalog/contract versions and should also mention the particle domain (R3-4). | Step page 446-450, 674. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

### Re-review verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Re-review required
- [ ] Waived

Follow-up owner: Planner and Step 04 agent (design revision and finding resolution)
Follow-up step: Step 04 — Transition Model; a fresh re-review is required after the design commit

**Changes required.** The round-2 fixes are real: the flat project shape is now consistent (R2-1), the
particle query supplies the matrix order (R2-2), `TRN026` no longer rejects `Vehicle6` (R2-3), `TRN028`
enforces the correct built-in sequences (R2-4), ring detection covers interior repeats (R2-5), and the layout,
cache, tolerance, and documentation fixes are present. However, R3-1 is a Blocker: the Core-side
`TransitionPathResolver` can only compare a path point to a literal `CellSite.Fractional`, so it cannot map
the symmetry-equivalent P1 positions that the ceria oxygen migration (and the data contract's own example)
requires. As written, the first acceptance criterion cannot pass for the ceria sample, and the 5-site vehicle
criterion has the same risk when its end groups share one reference site. The design must either resolve paths
through the Symmetry P1 encoder (with a Core-side abstraction or a precomputed site map) or explicitly restrict
and document the reference-path coordinates; the tests and acceptance criteria must include an orbit-member
path. The remaining minors/notes (R3-2–R3-10) do not independently block the step but should be corrected in
the same revision. A re-review requires a new design commit with fixes; do not create the step branch or start
implementation before that. The human owner may treat the Blocker as the gate; if they reclassify it as a Major,
the verdict remains `Changes required`.

## Round 3 resolutions

Design revision: `fe2331c` — design and data-contract corrections for round 3. This section is owned by the
planner/step agent and does not alter the reviewer's round-3 findings, evidence, or verdict.

| Finding | Status | Resolution |
| --- | --- | --- |
| R3-1 | Resolved — awaiting re-review | Moved `TransitionPathResolver` and the path validator to `Mocassin.Symmetry.Transitions`. The resolver expands each reference site to its P1 orbit with `UnitCellVectorEncoder`, maps a path point to a `(P, cell-offset)` encoding and owning reference site, and accepts any symmetry-equivalent orbit member. Core keeps only the definition-level validator; `ProjectValidationService` composes Core and Symmetry diagnostics. The ceria acceptance criterion and tests now require an orbit-member endpoint. |
| R3-2 | Resolved — awaiting re-review | The ownership sentence now says `TRN001`–`TRN028`. |
| R3-3 | Resolved — awaiting re-review | Help anchors now point at `docs/reference/transitions.md`; Step 07 owns the consolidated diagnostics/troubleshooting pages. |
| R3-4 | Resolved — awaiting re-review | `ProjectHasher.ComputeTransition` now hashes the full `ParticleOrder` (all particles plus void), not only referenced particles; the cache test covers an unreferenced-particle change. |
| R3-5 | Resolved — awaiting re-review | Path matching uses a periodic component-wise difference over every P1 position with `PathMatchTolerance`, so `0.9999995` matches `0.0`; `TrimToUnitCell` is used for canonicalization, not as the only boundary rule. |
| R3-6 | Resolved — awaiting re-review | TRN026 now says an unstable position's group must contain exactly one `[D, ∅]` state change in the table, section 7, and the defect table. |
| R3-7 | Resolved — awaiting re-review | Non-positive `(A,B,C)` counts throw `ArgumentOutOfRangeException` as a programmer contract violation, matching Step 03; invalid model data still returns null plus diagnostics. |
| R3-8 | Resolved — awaiting re-review | Added the `docs/user-guide/index.md`, `docs/physics/index.md`, and `docs/reference/index.md` update task. |
| R3-9 | Resolved — awaiting re-review | Added `IProjectQuery : IParticleQuery, IStructureQuery, ITransitionQuery`; the preview service takes that single same-snapshot query instead of separate query objects. |
| R3-10 | Resolved — awaiting re-review | The cache risk row now lists the transition closure hash including the full particle order, structure hash, `(A,B,C)`, and symmetry catalog/contract versions. |

## Re-review round 4

### Re-review request

| Field | Value |
| --- | --- |
| Step | 04 — Transition Model |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Design review status | Ready with notes |
| Reviewed commit | `fe2331c` |
| Docs branch | `main` |
| Review date | 2026-11-08 |
| Reviewer | Independent planning auditor (fresh context, round 4; no shared conversation with the planner or implementor) |

Step page: [`Step 04 — Transition Model`](../steps/step-04-transition-model.md)

The design content for round 4 is frozen at `fe2331c`. The delta from the round-3 commit `43bad05` is the
round-3 fix; the review request and status changes are metadata only. Verify the delta yourself with
`git diff --stat 43bad05..fe2331c`.

Reviewer: append your independence statement, evidence, findings, and verdict for this round below. Keep the
earlier rounds unchanged.

### Re-review evidence

**Independence.** I am a fresh planning auditor for round 4. I did not author or implement the Step 04 design, any fix round, or any implementation; I have no shared conversation with the planner or implementor. I read the frozen design content and the repository myself; I did not rely on the request summary or the resolution table.

**Frozen state and worktree.** Worktree `/workspace/mocassin`, branch `main`, `HEAD` `30b8b41`. The request names `fe2331c` (`fe2331c6bdb83d176679cf91c23d3dfdd78fb1ab`) as the frozen design commit. `git diff --stat fe2331c..30b8b41` shows only `docs/planning/reviews/step-04.md`, `docs/planning/status.md`, and the step-page design-review link; `git diff fe2331c..30b8b41 -- docs/planning/steps/step-04-transition-model.md docs/planning/02-data-contracts.md` shows only the one-line link change (`Re-review required at fe2331c`). The frozen design content is therefore identical at `fe2331c` and `HEAD`. The record commit's parent will be `30b8b41`, not `fe2331c`, because the round-4 request sits between the design commit and the record; this is the same process note as rounds 1–3.

**Commands run (from `/workspace/mocassin`):**

```sh
git status --short
# (no output: clean worktree)

git log --oneline --decorate -6
# 30b8b41 (HEAD -> main) docs: request step-04 design re-review round 4 [skip ci]
# fe2331c docs: resolve step-04 round-3 design findings [skip ci]
# d4e99cf docs: record step-04 design re-review Changes required [skip ci]
# ff4cf81 docs: request step-04 design re-review round 3 [skip ci]
# 43bad05 docs: resolve step-04 re-review findings [skip ci]
# 3a50206 docs: record step-04 design re-review Changes required [skip ci]

git rev-parse fe2331c
# fe2331c6bdb83d176679cf91c23d3dfdd78fb1ab

git show --stat fe2331c
# docs/planning/02-data-contracts.md              |  14 ++-
# docs/planning/steps/step-04-transition-model.md | 150 +++++++++++++++---------
# 2 files changed, 101 insertions(+), 63 deletions(-)

git diff --stat 43bad05..fe2331c -- docs/planning
# docs/planning/02-data-contracts.md              |  14 +-
# docs/planning/reviews/step-04.md                | 325 +++++++++++++++++++++++-
# docs/planning/status.md                         |  11 +-
# docs/planning/steps/step-04-transition-model.md | 152 ++++++-----
# 4 files changed, 429 insertions(+), 73 deletions(-)

git diff 43bad05..fe2331c -- docs/planning
# Read in full: the round-3 resolution text, the step-page revision, the data-contract delta, and the status change.

git show fe2331c:docs/planning/steps/step-04-transition-model.md
# Read in full (716 lines): sections 1-11, code layout, tasks, acceptance, documentation, tests, risks.

git show fe2331c:docs/planning/02-data-contracts.md
# Read in full (1,195 lines): model envelope 149-173, transition section 268-342,
# canonical-hash rules 135-147, simulations 409-436.

git diff --stat fe2331c..30b8b41
# docs/planning/reviews/step-04.md                | 69 +++++++++++++++++++++++--
# docs/planning/status.md                         |  6 +--
# docs/planning/steps/step-04-transition-model.md |  2 +-
# 3 files changed, 69 insertions(+), 8 deletions(-)

git diff fe2331c..30b8b41 -- docs/planning/steps/step-04-transition-model.md docs/planning/02-data-contracts.md
# Only the one-line `Design review` link change; no design change.

git diff --check 43bad05..fe2331c
# (no output)

zensical build --strict
# Build started
# No issues found
# Build finished in 0.23s

python3 tools/contracts/generate.py --check
# Contract artifacts are current (17 files)
```

**Additional read-only checks.**

```sh
wc -l src/dotnet/Mocassin.Core/Editing/ProjectEditor.cs
# 506

cat src/dotnet/Mocassin.Core/Mocassin.Core.csproj
# No ProjectReference to Mocassin.Symmetry.

cat src/dotnet/Mocassin.Symmetry/Mocassin.Symmetry.csproj
# ProjectReference to Mocassin.Core.

cat src/dotnet/Mocassin.Packages/Mocassin.Packages.csproj
# References Core, Compilation, and Contracts. Compilation references Symmetry, so the
# ProjectValidationService composition point can see Symmetry types.

grep -n "public ImmutableArray<Fractional3> Positions\|public ImmutableArray<ExpandedSite> ExpandedSites\|public static bool TryCreate\|public bool TryIndexOf\|public Vector4I Encode" \
  src/dotnet/Mocassin.Symmetry/Geometry/UnitCellVectorEncoder.cs
# 62 Positions; 67 ExpandedSites; 90/116 TryCreate; 289 TryIndexOf; 312 Encode.

sed -n '20,90p' src/dotnet/Mocassin.Symmetry/Geometry/UnitCellVectorEncoder.cs
# Each reference site is expanded with SpaceGroupOperations.ExpandGridOrbit, deduplicated on
# the exact grid, sorted by fractional (a,b,c), and assigned P = 0..Pcount-1.
# ExpandedSite carries ReferenceSiteIndex, so a P can be mapped back to its owning site.

grep -n "public sealed record Project" src/dotnet/Mocassin.Core/Project/Project.cs
# public sealed record Project : IParticleQuery, IStructureQuery

grep -n "StateChangeId\|StateChangeGroupId\|TransitionChainId\|KineticTransitionId\|MetropolisTransitionId" \
  src/dotnet/Mocassin.Core/Ids/TypedIds.cs
# 123/142/161/180/199: all five typed IDs already exist (added by Step 03, dd642d6).

grep -rn "PathMatchTolerance" src/dotnet
# (no output; the design adds the new named tolerance.)

grep -n "CanonicalizeModel" -A 14 src/dotnet/Mocassin.Core/Project/ProjectJson.cs
# The current method rebuilds only Particles, ParticleSets, and Structure; ProjectJsonConverter.Write
# (ProjectJson.cs 262) calls it, ProjectHasher.Compute uses SerializeCanonicalModel, and
# ProjectCanonicalizer.ToCanonicalUtf8 uses the same serializer.

grep -n "defined more than once\|Unknown model property" src/dotnet/Mocassin.Core/Model/ModelDefinitionJsonConverter.cs
# Duplicate and unknown model properties are rejected as the design states.

python3 - <<'PY'
import json
p=json.load(open('samples/ceria/ceria.mocprj'))
for s in p['model']['structure']['sites']:
    print(s['name'], s['fractional'], s['stability'])
PY
# Ce [0, 0, 0] stable
# O [0.25, 0.25, 0.25] stable
# O transition [0.5, 0.25, 0.25] unstable

sed -n '10,45p' src/dotnet/Mocassin.Symmetry.Tests/Structure/SampleStructureTests.cs
# ceriaOrbitSizes = [4, 8, 24]; ceriaOrbit1 = [9, 11, 15, 17, 27, 29, 33, 35]; 36 P1 positions.
# The stable O 8c orbit therefore includes symmetry-equivalent members such as (0.75, 0.25, 0.25).

grep -n "GetBasicKineticPattern\|GetMetropolisPattern\|GetBasicVehiclePattern\|GetSplitVehiclePattern" -A 8 \
  /workspace/mocassin-original/src/ModelBuilder/ICon.Model/Transitions/Modeling/Logic/ConnectorPattern/ConnectorPattern.cs
# Basic kinetic D{2,}; Metropolis D; basic vehicle D+ S S D+; split vehicle D D (S D D)+.

grep -n "ContainsRingTransition" -A 12 \
  /workspace/mocassin-original/src/ModelBuilder/ICon.Model/Transitions/Modeling/Logic/TransitionAnalyzer/TransitionAnalyzer.cs
# Any-pair check over raw fractional coordinates with the space-group comparer.

grep -n "GetEntryValueAt" -A 4 \
  /workspace/mocassin-original/src/ModelBuilder/ICon.Framework.Symmetry/Analysis/CellAnalysis/UnitCellAdapter.cs
# Resolves any fractional position (including a symmetry-equivalent P1 image) to its unit-cell entry.
```

**Files reviewed.** `AGENTS.md` (§3.8–§3.13, §4, §5, §7, §8, §9, §10, §11, §12, §16);
`docs/planning/reviews/README.md`; `docs/planning/reviews/step-design-review-template.md`; this record;
`docs/planning/steps/step-04-transition-model.md` at `fe2331c`; `docs/planning/02-data-contracts.md` at
`fe2331c`; `docs/planning/03-quality-strategy.md`; `docs/planning/05-open-decisions.md`;
`docs/planning/ci-policy.md`; `docs/planning/appendix-b-parity-matrix.md`; `docs/planning/status.md` at HEAD;
`docs/planning/steps/step-03-particle-and-structure-models.md`, `step-05-energy-model.md`,
`step-06-lattice-and-simulation-models.md`, `step-07-validation-and-diagnostics.md`,
`step-08-model-compilation-pipeline.md`, and `step-24-model-editors-particle-structure-transitions.md`;
`docs/planning/adr/0009-contract-manifest-and-contracts-library.md`,
`0010-canonical-messagepack-signatures.md`, and `0013-format-compatibility-and-api-versioning.md`;
`docs/development/code-structure.md`; `docs/reference/project-format.md`; `docs/reference/index.md`;
`docs/user-guide/index.md`; `docs/physics/index.md`; `zensical.toml`; `contracts/manifest.json`;
`contracts/generated/schema/project-v1.schema.json`; `contracts/generated/signatures/model-v1.json`;
`contracts/generated/fixtures/descriptors.json`; `tools/contracts/generate.py`; `tools/verify.sh`;
`tools/structure-policy.json`; `tools/check-api-compat.sh`; `tools/update-api-baseline.sh`.

**Code and test inventory inspected.** `Mocassin.Core`: `Model/ModelDefinition.cs`,
`Model/ModelDefinitionJsonConverter.cs`, `Model/Structure/CellSite.cs`,
`Model/Structure/IStructureQuery.cs`, `Model/Structure/StructureModelValidator.cs`,
`Model/Particles/IParticleQuery.cs`, `Model/ViewSettings.cs` (the camel-case enum converter),
`Project/Project.cs`, `Project/ProjectJson.cs`, `Project/ProjectHasher.cs`,
`Project/ProjectCanonicalizer.cs`, `Project/ProjectSchema.cs`, `Project/ProjectValidator.cs`,
`Editing/ProjectEditor.cs`, `Geometry/Fractional3.cs`, `Geometry/GeometryTolerances.cs`,
`Geometry/Vector4I.cs`, `Ids/TypedIds.cs`, `Diagnostics/Diagnostic.cs`, `Diagnostics/ModelPath.cs`.
`Mocassin.Symmetry`: `Geometry/UnitCellVectorEncoder.cs`, `Geometry/ExpandedSite.cs`,
`Geometry/StructureSiteReference.cs`, `Structure/IStructurePreviewService.cs`,
`Structure/StructurePreviewService.cs` (cache-key construction), `Contracts/SymmetryVersionInfo.cs`,
`SpaceGroups/SpaceGroupService.cs`. `Mocassin.Packages`: `Projects/ProjectValidationService.cs`,
`Projects/ProjectStore.cs`. `Mocassin.Compilation`, `Mocassin.Contracts` project references.
`Mocassin.Symmetry.Tests/Structure/SampleStructureTests.cs`, `Mocassin.Symmetry.Tests/UnitCellVectorEncoderTests.cs`,
`Mocassin.Packages.Tests/Projects/ProjectValidationServiceTests.cs`,
`Mocassin.Benchmarks/Structure/StructurePreviewBenchmarks.cs`, `tests/reference/bench/baselines.json`.
Original reference (read-only, no copy): `KineticTransitionValidator.cs`, `TransitionAnalyzer.cs`,
`AbstractTransitionValidator.cs`, `StateExchangeGroupValidator.cs`, `UnitCellAdapter.cs`,
`ConnectorPattern.cs`, and `docs/userguide-md/transition-model.md`.

**Required scope checks.** All thirteen required review-scope areas were re-checked against the frozen
content: scope/non-goals/acceptance, `ModelDefinition` and JSON/schema round-trip, semantic ordering, every
built-in pattern and `Custom`, path/stability validation, query/preview boundaries, TRN001–TRN028,
manifest/schema/fixture tasks, PublicAPI/ApiCompat, code layout, Step 03 dependency stability,
documentation/navigation and the test plan, and the Step 05/06/07/08/24 interfaces. The findings below record
the result.

### Re-review checklist (round 4)

Marks and notes apply to round 4 only; item text is unchanged.

**Design decisions reviewed**

- [x] Step page design decisions — reviewed the frozen page; R3-1–R3-10 are resolved or acknowledged;
  R4-1–R4-5 are new minor/notes.
- [x] Data contracts and format changes — the flat model envelope is consistent; the transition section,
  semantic ordering, and canonical-hash rules were re-read; the periodic-cell/offset wording is R4-1.
- [x] Architecture and module boundaries — Core/Symmetry/Packages split is sound; the resolver is now in
  Symmetry and `Mocassin.Core.csproj` has no Symmetry reference.
- [x] ADRs and open decisions — ADR 0009/0010/0013 and the Step 03 additive-v1 precedent are cited; no open
  decision blocks the step.
- [x] Quality strategy and test plan — property tests, contract fixtures, tiers, and the benchmark are
  present; the periodic-cell offset test is the R4-1 gap.
- [x] Parity matrix and documentation plan — parity/status/closeout/audit and `transitions.md` tasks are
  present; the index-page updates are present.
- [x] Licensing and provenance — no new dependency; MIT/SPDX applies; no GPL source or data is introduced.
- [x] Post-implementation audit plan — the standard AGENTS §3.10 audit and closeout task apply.

**Dependency and interface checks**

- [x] Dependencies are `Done` and their contracts are stable — Step 03 is complete (`step-03` tag; audit
  passed); `IParticleQuery`, `IStructureQuery`, `Project`, `CellSite`, `StructureModel`,
  `Fractional3.TrimToUnitCell`, and `UnitCellVectorEncoder` exist.
- [x] Inputs, outputs, types, and formats are defined — the flat model shape, `IProjectQuery`, preview, and
  matrices are defined; the offset detail is R4-1.
- [x] Versioning and migration behavior is defined — additive project-v1; Step 03 precedent; ADR 0009
  wording is addressed.
- [x] No unresolved cross-step interface question remains — Steps 05/06/07/08/24 line up; the
  `SimulationKind`/`TransitionKind` mapping is a note (R4-5), not a blocker.

**Standard design-review checklist**

- [x] Scope, non-goals, and acceptance criteria are explicit and testable — non-goals and the orbit-member
  criterion are present.
- [x] Design decisions are complete and internally consistent — the R3-1 boundary is fixed; R4-1–R4-4 are
  minor clarifications.
- [x] Required tests, property tests, golden fixtures, and performance targets are defined — yes; add the
  periodic-cell offset case (R4-1).
- [x] Documentation, status, parity, and audit updates are planned — yes.
- [x] Licensing, provenance, and dependency checks are resolved — no new dependency; MIT/SPDX; no GPL
  copying.
- [x] Risks and mitigations are realistic — the table is plausible; add the offset/ownership notes.
- [x] Code layout and namespace plan follow the structure policy — new folders/files fit the 20-file,
  depth-4, 800-line limits; `Int32`/`ImmutableArray`/`UnitCellVectorEncoder` usage is consistent.
- [x] No hidden scope, stale placeholders, or unowned decisions — R4-2/R4-4 are small ownership
  clarifications.

### Re-review findings

#### Round-3 resolution verification

| Finding | Severity | Status | Verification |
| --- | --- | --- | --- |
| R3-1 | Blocker | Resolved | `TransitionPathResolver` and `TransitionPathValidator` are now in `Mocassin.Symmetry.Transitions` (step page 299-302, 553-559, 594-596; task 594-596). The resolver builds a `UnitCellVectorEncoder` from the resolved setting and the authored sites and matches against `UnitCellVectorEncoder.Positions`, whose `ExpandedSite.ReferenceSiteIndex` maps every P1 orbit member back to its owning reference site (`UnitCellVectorEncoder.cs` 62, 67, 90-116, 220-244). The ceria golden P order has 36 positions and O 8c orbit `[9, 11, 15, 17, 27, 29, 33, 35]` (`SampleStructureTests.cs` 14-16, 31-39), so an endpoint such as `(0.75, 0.25, 0.25)` is an 8c orbit member and resolves to the O site. The boundary is correct: `Mocassin.Core.csproj` has no Symmetry reference, `Mocassin.Symmetry.csproj` references Core, and the preview service is in Symmetry. The acceptance criterion now requires orbit-member endpoints (629-631), and the path-matching test row requires orbit members (684). The original `KineticTransitionValidator.cs` 86 / `UnitCellAdapter.GetEntryValueAt` behavior is matched. |
| R3-2 | Minor | Resolved | The ownership sentence now says `TRN001`–`TRN028` (375). The partition itself omits `TRN018`; see R4-2. |
| R3-3 | Minor | Resolved | The help anchor is now `docs/reference/transitions.md` (378); the task and Documentation section create that page and assign the consolidated `diagnostics.md`/`troubleshooting/errors.md` to Step 07 (606-608, 664-666). |
| R3-4 | Minor | Resolved | `transitionModelHash` now covers the full `ParticleOrder`, including unreferenced particles and void (470-474); the cache test and risk row were updated (688, 710). |
| R3-5 | Minor | Resolved | Matching uses a periodic component-wise difference over every P1 position with `PathMatchTolerance = 1e-6` (306-312), and the test row covers boundary points (684). The offset derivation is R4-1. |
| R3-6 | Note | Resolved | `TRN026` now says "does not contain exactly one `[D, ∅]` state change" in the table (371), §7 (317-318), and the defect table (510). |
| R3-7 | Note | Resolved | Non-positive `(A, B, C)` counts throw `ArgumentOutOfRangeException`; invalid model data returns `null` plus diagnostics (463-467), matching Step 03. |
| R3-8 | Note | Resolved | The task updates `docs/user-guide/index.md`, `docs/physics/index.md`, and `docs/reference/index.md` (621-622), not only `zensical.toml`. |
| R3-9 | Note | Resolved | `IProjectQuery : IParticleQuery, IStructureQuery, ITransitionQuery` is the single same-snapshot query (419, 457-459); the preview service takes it. |
| R3-10 | Note | Resolved | The cache risk row now lists the transition closure hash (with the full particle order), structure hash, `(A,B,C)`, and symmetry catalog/contract versions (710). |

#### New findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Minor** | **R4-1 — The periodic-cell offset in `ResolvedPathPosition.Encoding` is not defined precisely enough, and the data contract's `TrimToUnitCell` wording can be read as discarding it.** The step page says `Encoding` is the full `Vector4I` (cell offset plus P) and that a point in a different periodic cell is a distinct position (303-305, 322-324). The matching text canonicalizes each point with `Fractional3.TrimToUnitCell()`, which maps into `[0,1)`, then compares with a periodic difference "minimum over integer shifts" and requires "exactly one `(P, cell-offset)` candidate" (306-315). The data contract repeats "canonicalizes each point with `Fractional3.TrimToUnitCell()`" (327-329) and then says a different periodic cell is distinct (330-331). `TrimToUnitCell` discards the integer part (`Fractional3.cs` 114-153); the periodic difference is shift-invariant and at a boundary such as `0.9999995` versus `0.0` selects the shift `1`, not the raw cell `0`. The design does not state whether the offset is the raw integer part, the minimizing shift, or a separate convention, and the test plan covers boundary matching (684) but not "same P, different cell offset is not a duplicate". Pin the offset derivation and add that test. | Step page 303-315, 322-324, 684; `02-data-contracts.md` 326-331; `Fractional3.cs` 114-153; `GeometryTolerances.cs` 23. |
| **Minor** | **R4-2 — `TRN018` has no owner in the Core/Symmetry validation-ownership split.** The rule table defines `TRN018` as a Warning (363), the task says "Implement reverse-link validation and `TRN018`" (602), and the acceptance criterion tests it (638), but the ownership list gives Core `TRN001–TRN009, TRN017, TRN019, and TRN021–TRN027` and Symmetry `TRN010–TRN016, TRN020, and TRN028` (332-335). `TRN018` belongs to neither. Add it to the Core definition-level list (it needs only the chain and reverse link). | Step page 332-335, 363, 602, 638. |
| **Minor** | **R4-3 — The `Custom` `D+ S+ D+` grammar is broader than the original regex-validated patterns, although the text says it preserves them.** The original `ConnectorPattern` accepts basic kinetic `D{2,}`, Metropolis `D`, basic vehicle `D+ S S D+`, and split vehicle `D D (S D D)+` (`ConnectorPattern.cs` 111-157). The new `D+ S+ D+` also accepts `D S D` (one static) and `D D S S D D` (two static), which the original does not. The design says `Custom` "preserves the original regex-validated connector grammar" (189) and the risk row says rule generation rejects unsupported cases (706), but Step 08 has no such rejection. Either state that `D+ S+ D+` is an intentional generalization and define Step 08 support, or restrict the grammar to the original shapes. | Step page 189-197, 706; original `ConnectorPattern.cs` 111-157; Step 08 179-205. |
| **Minor** | **R4-4 — `TRN025` is assigned to Core, but its wording can require the resolved path for `Custom` chains.** The table says "Group is used at both stable and unstable positions" (370), and §3 says "the group's stability role is fixed by its pairs (TRN025)" (209-210). For built-in patterns the stability sequence is in the table; for `Custom` it is "inferred from the resolved reference path" (196, 319-321), which Core cannot do. Clarify whether `TRN025` is a pair-level group check (all pairs unstable-role or all stable-role, Core) or a chain-usage check (Symmetry for `Custom`); if it is the latter, move it to the Symmetry list. | Step page 196, 209-210, 319-321, 332-335, 370. |
| **Note** | **R4-5 — Step 06's `SimulationKind` (`Mmc`/`Kmc`) to `TransitionKind` (`Metropolis`/`Kinetic`) mapping is implicit.** Step 06 says `TransitionRef` carries a `TransitionKind` (`Kinetic` or `Metropolis`) and "the simulation validates that the kind matches `Kind`" (129), and then says "`Kind` is `Mmc` or `Kmc`" (132). The mapping is obvious but not written; state it once in Step 04 or Step 06 to avoid a cross-step ambiguity. | Step 04 212-233; Step 06 99-132. |

### Re-review verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Re-review required
- [ ] Waived

Follow-up owner: Planner and Step 04 agent (accept R4-1–R4-5 or make a focused clarification)
Follow-up step: Step 04 — Transition Model; the human owner approves the `Ready with notes` gate or
requests a small clarification round

**Ready with notes.** The round-3 Blocker R3-1 is genuinely removed, not reworded. The path resolver and
validator now live in `Mocassin.Symmetry.Transitions`; the resolver expands the authored reference sites to
their P1 orbits with the existing `UnitCellVectorEncoder`, maps a point to a `(P, cell-offset)` encoding and
owning reference site, and accepts symmetry-equivalent orbit members. The Core boundary is intact
(`Mocassin.Core.csproj` has no Symmetry reference), the ceria 8c orbit is confirmed by the existing golden P
order, and the ceria acceptance criterion and path-matching test now require orbit-member endpoints. R3-2–R3-10
are also resolved as recorded above.

The remaining findings are four Minors and one Note: the precise periodic-cell offset convention (R4-1), the
missing `TRN018` owner (R4-2), the broader-than-original `Custom` grammar (R4-3), the `TRN025` owner/semantics
(R4-4), and the implicit `SimulationKind`/`TransitionKind` mapping (R4-5). None is a material contradiction
with the revised architecture; they are clarifications and test-coverage details. They should be addressed in
the same step revision or explicitly accepted by the human owner. Per the review process, `Ready with notes`
requires the human owner to accept these notes explicitly; this reviewer does not approve the gate.

Reviewed commit: `fe2331c` (`fe2331c6bdb83d176679cf91c23d3dfdd78fb1ab`). Documentation gate:
`zensical build --strict` — no issues found; `python3 tools/contracts/generate.py --check` — 17 artifacts
current. Record parent: `30b8b41` (the round-4 request commit); the design content is identical at `fe2331c`
and `HEAD`.

## Round 4 resolutions

Design revision: `a3d8641` — focused clarifications for R4-1–R4-5. This section is owned by the
planner/step agent and does not alter the reviewer's round-4 findings, evidence, or verdict.

| Finding | Status | Resolution |
| --- | --- | --- |
| R4-1 | Resolved — awaiting re-review | The cell offset is now explicitly the minimizing integer shift between the raw point and the matched P1 position (ties toward zero); `TrimToUnitCell` is not the offset rule. The test plan covers boundary matching and "same P, different offset is not a duplicate". |
| R4-2 | Resolved — awaiting re-review | `TRN018` is now in the Core definition-level ownership list (TRN001–TRN009, TRN017–TRN019, TRN021–TRN027). |
| R4-3 | Accepted — awaiting re-review | The broader `D+ S+ D+` grammar is an intentional generalization, not a claim that the original regex set is exhaustive. `D S D` is explicitly invalid (TRN009) because a single static connector between two single dynamic connectors links two transitions without the required intermediate arrangement. Step 08 must support or reject every accepted shape at compile time. |
| R4-4 | Resolved — awaiting re-review | TRN025 is now a pair-level Core check: a group must not mix stable-role (acceptor not void) and unstable-role (`[D, ∅]`) state changes. Position/group stability agreement remains TRN012 in Symmetry. |
| R4-5 | Resolved — awaiting re-review | Step 04 and Step 06 now state the exact mapping: `SimulationKind.Kmc` ↔ `TransitionKind.Kinetic`, `SimulationKind.Mmc` ↔ `TransitionKind.Metropolis`. |

## Re-review round 5

### Re-review request

| Field | Value |
| --- | --- |
| Step | 04 — Transition Model |
| Request owner | Step agent / planner |
| Review initiated by | Human owner |
| Design review status | Re-review required |
| Reviewed commit | `a3d8641` |
| Docs branch | `main` |
| Review date | — |
| Reviewer | — |

Step page: [`Step 04 — Transition Model`](../steps/step-04-transition-model.md)

The design content for round 5 is frozen at `a3d8641`. The delta from the round-4 commit `fe2331c` is the
focused R4-1–R4-5 clarification; the review request and status changes are metadata only. Verify the delta
yourself with `git diff --stat fe2331c..a3d8641`.

This is a focused follow-up. The human owner asked for a brief review with minimal checks: confirm that
R4-1–R4-5 are addressed, no new Blocker or Major was introduced, and the documentation gate still passes. Do
not repeat the full round-1–round-4 review unless the delta introduces a contradiction.

Reviewer: append your independence statement, evidence, findings, and verdict for this round below. Keep the
earlier rounds unchanged.

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
