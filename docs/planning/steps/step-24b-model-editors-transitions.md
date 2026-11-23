# Step 24B — Transition Editors

**Phase:** F. GUI
**Depends on:** **24A** (the shared editor framework, table surface, reference picker, numeric expression, usage
index, navigation/inspector seams), 02 (the transition preview, path, and validation services), and 04 (the
transition model). The compile preview consumes the whole project snapshot through 05, 06, and 08 **read-only**;
24B adds no energy, lattice, or simulation editor. Not dependent on Steps 09–11 or 13–22.
**Delivers:** The state-change and state-change-group editors, the visual chain builder with the occupation-matrix
preview, the KMC reference-path editor, the MMC exchange editor, and the transition summary with a real-compiler
rule preview — on the 24A framework, with undo, inline validation, and diagnostic navigation.
**Design class:** **Full** — provisional at this draft; the planned compile preview adds one public
`Mocassin.Packages` API and the chain builder's matrix indices are a silent-wrong-physics contract with the
compiler. If the owner defers the compile preview at the 24B freeze, the class can be re-recorded **Light** before
the freeze (a class may not move down *after* a freeze without an owner decision). Justification in
[§14](#14-complexity-class-and-freeze-decision).

> **Split note.** The human owner split Step 24 into **24A** (framework, particles, structure) and **24B**
> (transitions) on 2026-11-22 under the complexity rule. 24B needs nothing from 24A beyond the delivered framework
> and contains no 24A task. The superseded unsplit design is
> [`step-24-model-editors-particle-structure-transitions.md`](step-24-model-editors-particle-structure-transitions.md).

> **Freeze sequencing (normative).** This page is a **draft**, not a frozen design. Its design is reconciled
> against the 24A framework **as built** and frozen only after 24A feature 1 (`shared-editor-framework`) is
> squash-merged on the 24A step branch. `docs/planning/reviews/step-24b.md` is created at that freeze — never
> pre-seeded — and the step then waits for its own owner start gate after 24A is merged into `main`. The same rule
> as 23B applies: 24B is not design-reviewed and not started until that point.

## Implementation status

- **Status:** Design drafted — **not design-reviewed and not started**; freeze waits for 24A feature 1
- **Step branch:** — (planned `step/24b-model-editors-transitions`; created only after this page is frozen,
  reviewed, and the owner approves the start gate)
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** **Not requested** — `reviews/step-24b.md` is created at the freeze after 24A feature 1
- **Feature list:** Draft (six features, §15) — owner approval at the 24B start gate
- **Design class:** **Full**, provisional (§14)
- **Complexity class:** **High** — cross-step interfaces and test-matrix breadth; owner acceptance as one High
  unit is required at the 24B start gate; no further split is proposed (§14)
- **Last updated:** 2026-11-23

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Make the transition model authorable from the GUI with the compiler's own semantics visible. A user must be able
to build state changes, groups, chains, KMC paths, and MMC exchanges, see why a mechanism is invalid at the exact
card/linker/path point, and see what the real compiler generates — without reading source code or running the
CLI.

## Scope

**In scope**

- State-change and state-change-group editors, including make-group-from-selection.
- The visual chain builder: pattern picker, ordered position cards with group pickers, dynamic/static linkers,
  reverse-generation toggle, and the S0/S1/S2 occupation-matrix preview built from the Step 04
  `TransitionPreview` data (`ParticleOrder`, `StateChangeMatrices`).
- The KMC reference-path editor: add/remove path points, fractional or reference-site entry, stability coding with
  non-colour markers, the 2D path preview, the symmetry-expanded path count, ring/consecutive-unstable/endpoint
  warnings, and auto-fill from sites.
- The MMC editor: 2-site chain picker, site A/B pickers, same-site exchange explanation, exchange-matrix preview.
- The transition summary and the real-compiler rule preview for the selected transition.
- The shared matrix data-shape control (§4), the 24B navigation domains, explorer subtree, and inspector
  contributions.
- One additive `Mocassin.Packages` API (§9), the 24B documentation, tests, closeout, and audit request.

**Non-goals**

- **No 24A work.** The framework is consumed as delivered; a framework gap stops 24B for an amendment rather than
  editing 24A surfaces ad hoc.
- **No energy, lattice, simulation, parameterization, job-template, build, 3D, run-monitoring, results, or analysis
  UI** (Steps 25–28 and 20–22). The compile preview is a per-transition authoring aid, not the build UI.
- **No new format, schema, version, CLI behaviour, diagnostic code, manifest, or dependency.** The Packages API is
  additive and computes an existing compiled model.
- **No new 3D path rendering.** The KMC path preview is a 2D projection, as the unsplit design said; Step 27 owns
  the viewport.
- **No cascade deletion beyond what `ProjectEditor` already supports** (`removeDependents` on state changes,
  groups, chains, and transitions). Site deletion stays blocked-with-dependents as in 24A.

## Original reference

Read `/workspace/moccasin-original` as a read-only reference (GPL-3.0-or-later). Do not copy code or data.

- State changes (donor/acceptor), groups ("make group"), chains (a connector string parsed into dynamic/static),
  MMC transitions (chain plus two sites), KMC transitions (chain plus reference path vectors).
- Validation is a separate "Model Validator" console tab; the transition editors do not show inline errors, and
  there is no undo/redo.
- The transition summary in the original shows counts from a separate compile/preview tool.

**Preserved intents:** donor/acceptor pairs with void as a valid acceptor; groups as ordered role sets with a
"make group from selection" action; the chain as an ordered card sequence with visible linkers; the reference path
as an ordered point list with stability coding; a per-transition compile/preview view.

**Corrected in this step:** inline validation on the offending card/linker/path point; the occupation matrices and
expanded path count come from the same Step 04 preview service the CLI uses; the rule preview runs the real
compiler; every edit is undoable through one coordinator.

## Design

### 1. Position in the shell

24B registers five document kinds and their navigation domains, and fills the 24A seams without changing the
framework:

| 24A/23B seam | 24B use |
| --- | --- |
| `DocumentKinds` | `model.stateChanges`, `model.stateChangeGroups`, `model.chains`, `model.kmcTransitions`, `model.mmcTransitions` |
| `INavigationTargetProvider` | the `stateChange`, `stateChangeGroup`, `chain`, `kineticTransition`, and `metropolisTransition` domains |
| `IExplorerNodeProvider` | a `Transitions` subtree with the five domains |
| `IInspectorContributor` | usage counts, dependents, and the selected transition's key facts |
| `ModelEditorViewModel`/`ModelTableSurface`/`ReferencePicker`/`NumericExpression`/`ModelUsageIndex` | reused unchanged |
| `ProjectEditCoordinator` | the only write path, with the additive 24A operation overload |
| `ValidationCoordinator` | domain diagnostics filtered by `ModelPath.Domain`; no second validation |

**Domain navigation is the explorer, not a tree or nested tabs (D-1).** The layout POC rejected a tree as the
primary surface for flat domains and rejected a second tab level under the shell's document tabs. Each transition
domain is its own document tab, opened on demand from the explorer or by navigation; no "Transitions" document
with an inner domain selector is created.

### 2. State changes and state-change groups

**State changes document** (`model.stateChanges`)

- Columns: name, donor, acceptor, usage count (groups and chains), and a severity badge.
- Donor and acceptor are particle reference pickers showing symbol and charge; void is selectable as an acceptor.
- Derived badges, not invented fields (D-2): "void acceptor", "void donor (explicit reverse pair)", "charged
  vacancy". The Step 04 model has only `DonorParticleId` and `AcceptorParticleId`; the unsplit design's "vacancy
  pair"/"unstable pair" columns do not exist and must not be added as model fields.
- Inline warnings: donor equals acceptor (`TRN002` when the position is not a valid self exchange), both void
  (`TRN003`), void donor with a charged acceptor (`TRN004`).
- Delete is blocked with the dependents list (groups, chains, transitions) and a "Show usages" action; the Core
  `RemoveStateChange(..., removeDependents)` option is offered only as an explicit, confirmed cascade with the
  would-be removals listed first.

**Groups document** (`model.stateChangeGroups`)

- Columns: name, member count, usage count (chains).
- Detail: ordered members (order is semantic and never re-sorted), add/remove/move, and "Make group from
  selection" over a multi-select of state changes — one undo entry.
- Mixed physical and charge-exchange members warn with `TRN006`; an empty group errors with `TRN005`; a duplicate
  name errors with `TRN017`.

### 3. Chain builder

**Chains document** (`model.chains`)

- Pattern picker: `Metropolis2`, `Migration3`, `Interstitialcy5`, `Interstitialcy7`, `Vehicle5`, `Vehicle6`,
  `Custom`. Built-in patterns fix the position count and linker sequence; choosing one rewrites the cards only
  after confirmation and one undo entry.
- **Position cards** in order, one per chain position, each with a group reference picker and a role label
  (start/spectator/end as derived from the pattern). Cards are reorderable only for `Custom`.
- **Linkers** between cards: dynamic/static, read-only for built-in patterns, editable for `Custom`; the custom
  grammar is validated by `TRN009` and shown at the exact linker.
- **Reverse-generation toggle** (`GeneratesReverse`) with a one-line explanation of what an automatic inverse
  means for rule generation.
- **Occupation-matrix preview**: the S0/S1/S2 matrices rendered by the shared matrix control (§4) from the
  `TransitionPreview` of a chain-bound transition when one exists. The preview labels rows by position and columns
  by **`ParticleOrder` index**, exactly as the compiler reads them: the editor never re-orders or re-labels the
  published order (D-3). With no bound transition the panel explains that matrices need a KMC or MMC transition
  and shows the group-role skeleton instead.
- Chain edits that break `TRN007`/`TRN008`/`TRN009` are refused inline; the existing chain value is unchanged.

### 4. The matrix data-shape control

One small presentation control (the ADR 0003 option 6 control), first built here: row labels, column labels,
bordered cells, an automation `Table` role and name, and read-only cells. It renders a `MatrixViewModel` value
(rows, columns, cells with an optional highlight) from the view-model layer and is reused unchanged by the MMC
exchange preview and by Step 25's site-energy matrices. It is not a table surface, does not select or sort, and
stays out of `ModelTableSurface`.

### 5. KMC reference-path editor

**KMC transitions document** (`model.kmcTransitions`)

- Columns: name, chain, path length, expanded path count (advisory), stability summary, usage.
- Detail: chain reference picker (kinetic-compatible chains only), the reverse-transition picker (optional, with
  `TRN018` warning when a complex transition has no explicit reverse), and the path editor.
- **Path editor**: an ordered point list; each point is entered as three fractional fields (expression-capable) or
  picked from a reference site ("Use site"); add/remove points; the derive-from-chain helper proposes the chain
  length and node count; "Auto-fill from sites" fills the points from the selected reference sites.
- **Stability coding is never colour-only.** Each point shows a glyph, the word (`stable`/`unstable`), and the
  resolved owning reference site; the 2D projection marks the same glyphs.
- Validate with the shared services: `TransitionPreviewService` for the expanded paths, the expected rule count,
  and the resolved path; the path validator's diagnostics for `TRN010`–`TRN014`, `TRN020`, and `TRN022` appear on
  the offending point or on the path header as appropriate.
- The 2D path preview uses the same projection primitive as 24A's P1 preview; ring/consecutive-unstable/endpoint
  problems are listed with a navigation link to the point.

### 6. MMC editor

**MMC transitions document** (`model.mmcTransitions`)

- Columns: name, chain, site A, site B, usage.
- Detail: a 2-site (`Metropolis2`-compatible) chain picker, site A/B reference pickers, an explanation when A and
  B are the same site (intra-sublattice exchange is legal), the reverse-transition picker, and the
  exchange-matrix preview from `TransitionPreview` (`TRN015`, `TRN016` for invalid chains/sites).
- The same-site case is explained in place; the editor does not warn about a legal exchange.

### 7. Transition summary and compile preview

**Summary panel** (on every transition document): chain pattern and position count, the symmetry-expanded path
count and the expected rule count from `TransitionPreview`, the dependent/hidden-rule note, the charge and mass
conservation status derived from the preview matrices, and the validation state of the transition.

**Compile preview** — "Run compiler preview":

- Runs the **real** compiler over the current project snapshot through an additive `Mocassin.Packages` API (§9),
  then shows the rules whose `CompiledJumpCollection.Transition` is the selected transition: collection kind,
  direction and rule counts, the per-rule state triplets (S0/S1/S2), movement, charge-transport vector, and
  whether the rule is an automatic inverse.
- **Unused transitions are reported honestly.** The compiler does not emit rules for a transition that no
  simulation references (`CMP009`); the panel says so and links to the simulation domains (Step 25). It never
  claims "no rules" without that explanation.
- The run is explicit (button), cancellable, shows progress, and caches the last `CompiledModel` per project
  reference so repeated previews of different transitions on the same snapshot do not recompile.
- A compile failure shows the compiler's diagnostics (stage and code) without changing the project. The preview is
  advisory; the build in Step 26 remains authoritative.

### 8. Validation, navigation, and commands

- Diagnostics are filtered from the 23B validation state by `ModelPath.Domain`, as in 24A; the summary bar opens
  the Problems channel and lists the domain's entries inline.
- Navigation: `stateChange/<id>`, `stateChangeGroup/<id>`, `chain/<id>`, `kineticTransition/<id>`,
  `metropolisTransition/<id>` support `Activate`, `SelectObject`, and `FocusProperty`; a stale id returns `null`
  and the shell reports it. The 23A unknown-domain notice disappears for these domains once 24B registers them.
- Commands and gestures follow 24A §7: document-local gestures, global catalog only for opening the editors and
  "Show usages".
- No new setting. The 24A `editorFraction` covers these documents; preview state is per-document and not
  persisted.

### 9. Library change owned by this step

One additive public API, because the GUI must show what the **real** compiler generates without referencing
`Mocassin.Compilation` from the app (the app depends on `Mocassin.Packages`, which owns compilation):

```csharp
// Mocassin.Packages/Build (concrete service, no new interface — D-4)
public sealed class TransitionRulePreviewService
{
    public TransitionRulePreview Create(
        Project project,
        TransitionRef transition,
        CompilationOptions? options = null,
        CancellationToken cancellationToken = default);
}

public sealed record TransitionRulePreview(
    TransitionRef Transition,
    ImmutableArray<CompiledJumpCollection> Collections,
    ImmutableArray<CompiledJumpRule> Rules,
    int ExpectedRuleCount,
    ImmutableArray<Diagnostic> Diagnostics);
```

- The service compiles the snapshot with `IModelCompiler` and filters by `CompiledJumpCollection.Transition`; it
  performs **no I/O** and writes nothing. It is deterministic for a given snapshot.
- No interface is introduced: the rule in [`05-open-decisions.md`](../05-open-decisions.md) "No speculative
  interfaces" applies, and the GUI has one concrete consumer.
- Alternative considered and recorded for the owner (D-5): defer the compile preview to Step 26 and show only the
  Step 04 advisory preview. That keeps the class Light and adds no API, but it drops the promised
  "compile preview runs the real compiler" outcome; the owner must choose at the freeze if the API is not wanted.

### 10. Dependency and interface checks

- 24A is `Done`/frozen before this freeze; 04, 06, and 08 are merged. The API in §9 is the only new cross-package
  surface, and it is additive.
- No persisted type, schema, or file changes; no migration.
- Unresolved at this draft: the owner's choice in D-5 (compile preview API versus deferral) and the exact
  `TransitionRulePreview` shape, both settled at the 24B freeze.

### 11. Documentation plan

| Page | 24B content |
| --- | --- |
| `docs/user-guide/transitions.md` | State changes, groups, chain patterns and linkers, the matrix preview, KMC paths and stability coding, MMC exchanges, the summary and compile preview |
| `docs/tutorials/ceria-kmc.md` | First use: build the ceria oxygen migration end to end from the GUI |
| `docs/development/ui-architecture.md` | The matrix control, the chain/path preview data flow, the compile-preview service and its caching |
| `docs/troubleshooting/errors.md` | `TRN` diagnostic families in the editor, unused-transition rules, compile-preview failures |

### 12. Test plan

| Test | Assertion | Tier |
| --- | --- | --- |
| `TransitionEditorAcceptanceTests` | On the real `samples/transitions/ceria-kmc.mocprj`, build the oxygen migration from the GUI and undo to the loaded value; the base `samples/ceria` project has no transitions and is not used as a transition fixture | PR-fast |
| `StateChangeEditorTests` | Donor/acceptor rules, void acceptor, self-change `TRN002`, both-void `TRN003`, void-donor warning `TRN004`, usage counts, blocked delete with dependents, make-group from selection is one undo entry | PR-fast |
| `StateChangeGroupEditorTests` | Ordered membership, empty `TRN005`, mixed `TRN006`, duplicate name `TRN017`, cascade only with confirmation | PR-fast |
| `ChainBuilderTests` | All six built-in patterns produce the correct position and linker counts; custom grammar accepts/rejects the documented sequences (`TRN009`); reverse toggle persists; card reordering only for custom; edits are one undo entry | PR-fast |
| `ChainMatrixPreviewTests` | The matrix rows/columns equal `TransitionPreview.StateChangeMatrices` and `ParticleOrder` for a real chain; the matrix control has a `Table` role and a name; a chain with no bound transition shows the explanatory state | PR-fast |
| `KmcPathEditorTests` | Add/remove/insert points; fractional and site-picked entry agree; stability glyphs and words; expanded path count matches `TransitionPreview`; `TRN010`–`TRN014`, `TRN020`, `TRN022` appear at the right point; auto-fill from sites | PR-fast |
| `MmcEditorTests` | 2-site chain restriction (`TRN015`), site validation (`TRN016`), same-site exchange explained and accepted, exchange matrix matches the preview | PR-fast |
| `CompilePreviewTests` | The preview shows the compiled rules for the selected transition, including inverse flags, from the real compiler; an unreferenced transition reports the `CMP009` explanation; the cache avoids a second compile for the same snapshot; cancellation leaves the document unchanged | PR-fast |
| `TransitionNavigationTests` | All five domains open/select/focus; a chain/linker/point diagnostic navigates to the exact card/point; a stale id returns `null` without throwing | PR-fast |
| `TransitionUndoTests` | Undo/redo across every operation, save point intact | PR-fast |
| `TransitionAccessibilityTests` | Cards and linkers are keyboard-reachable and named; stability is glyph plus word, never colour-only; the matrix exposes a table role | PR-fast headless |
| `TransitionScreenshotTests` | Light, dark, and high-contrast captures of the chain builder, the matrix, the KMC path, and the compile preview | PR-medium (Ubuntu) |
| `TransitionPerformanceTests` | A 7-position chain with many groups previews within the recorded budget; path preview is debounced and superseded results are discarded; a compile preview reports progress and is cancellable | PR-fast |

### 13. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Matrix indices drift from the compiler's interpretation | Read `ParticleOrder`/`StateChangeMatrices` from `TransitionPreview`; never re-sort; the acceptance test compares against the preview service |
| The chain builder becomes a form with hidden state | Cards are the source of truth; every mutation is one `ProjectEditResult` and one undo entry; the pattern picker rewrites only after confirmation |
| Compile preview is slow or blocks the UI | Explicit button, cancellation, progress, off-thread, cache per snapshot, size guard with a clear message |
| Unused transitions confuse the preview | Explicit `CMP009` explanation with a link to the simulation domain |
| Path editor excludes keyboard users | Every drag/pick has a field and a button equivalent; the 24A interaction rules apply |
| The additive API grows | One concrete method; no interface; a second consumer must justify an interface then |
| 24B is designed against an unbuilt framework | Freeze is gated on 24A feature 1; the reconciliation is recorded in this page |

### 14. Complexity class and freeze decision

**Design class: Full (provisional at this draft).** [`design-proportionality.md`](../../development/design-proportionality.md)
§1 triggers:

1. **Public API and cross-package interface.** The additive `TransitionRulePreviewService` (§9) is a public
   `Mocassin.Packages` surface consumed by the app; it wraps the compiler.
2. **Irreversibility of a silent-wrong-physics contract.** The chain builder's matrix indices, linker order, and
   path-point semantics are read by the compiler; an editor that mislabels them produces plausible but wrong
   rules. That is a design-review concern, not a code-review one.

No format, dependency, migration, or security/concurrency trigger applies. If the owner chooses the D-5 deferral
at the freeze (no API), the class is re-recorded **Light** before the freeze with that reason; a move down after a
freeze needs an owner decision.

**Complexity class: High.** Measured against [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers):

| Dimension | Class | Evidence |
| --- | --- | --- |
| Contracts, formats, and public surfaces | **Medium** | One additive Packages API; no format, manifest, diagnostic-code, or CLI change |
| New dependencies | **Low** | None |
| Cross-step interfaces | **High** | Consumes the 24A framework and the Step 04 preview/compiler contracts that must not be reinterpreted |
| Irreversibility | **Medium** | The matrix/linker/path semantics are behavioural; the model and compiler already own them |
| Test-matrix breadth and failure modes | **High** | Five documents × (view-model, headless view, keyboard, accessibility, screenshots, undo/validation) plus the compiler integration and cancellation |
| Independent acceptance stories | **Medium** | One coherent story: the ceria migration authored end to end; state changes, chains, KMC, and MMC are steps inside it |
| Evidence size for a fresh auditor | **Medium** | Six features, five documents, one API, screenshots, one compiler-preview record |

**Class: High** (highest: cross-step interfaces and test-matrix breadth).

**Owner acceptance is required** for this High step at the 24B start gate. **No further split is proposed:**
state changes → groups → chains → KMC/MMC is a strict dependency chain; splitting the editors would separate the
chain contract from the matrices and paths it produces and duplicate the harness, while each half would remain
High on test-matrix breadth.

**Freeze gate.** Freeze after 24A feature 1 is squash-merged; reconcile §1–§9 against the framework as built;
create `reviews/step-24b.md` at that commit. The start gate follows 24A's merge.

### 15. Decisions recorded, draft feature list, and owner input

| ID | Decision | Rationale |
| --- | --- | --- |
| D-1 | Domain navigation is the explorer; each transition domain is its own document tab (no tree, no inner domain selector, no nested tabs) | The layout POC rejected a tree for flat domains and a second tab level; every domain has different columns and editors |
| D-2 | State-change tables show derived badges, never new model fields for "vacancy pair"/"unstable pair" | The Step 04 model has only donor/acceptor; adding fields would be a format change and a silent physics decision |
| D-3 | The matrix preview labels columns by `ParticleOrder` index and never re-sorts positions, groups, or path points | The compiler reads that exact order; a silent re-label produces wrong rules |
| D-4 | The compile preview is one concrete `Mocassin.Packages` service, no new interface | One GUI consumer; the "no speculative interfaces" rule in `05-open-decisions.md` applies |
| D-5 | The additive Packages API is the chosen route; deferring the compile preview to Step 26 is the recorded alternative | Keeps the promised real-compiler preview; the owner decides at the freeze |

Each branch will be `feature/step-24b-model-editors-transitions/<slug>` from the 24B step branch.

| # | Feature | Slug | Scope | Done when |
| --- | --- | --- | --- | --- |
| 1 | State changes and groups | `state-change-and-group-editors` | State-change table and detail with donor/acceptor pickers, derived badges, warnings, usage, blocked delete with dependents; groups with ordered membership, make-from-selection, mixed/empty/duplicate validation | The `TRN002`–`TRN006` and `TRN017` paths pass; make-group is one undo entry |
| 2 | Chain builder | `chain-builder` | Pattern picker, position cards, dynamic/static linkers, reverse toggle, the shared matrix control, and the S0/S1/S2 preview from `TransitionPreview` | All built-in patterns and custom linkers pass; the editor and compiler agree on every matrix index |
| 3 | KMC transition editor | `kmc-transition-editor` | Chain and reverse pickers; path add/remove/insert; fractional or site entry; glyph-and-word stability coding; 2D path preview; expanded path count; `TRN` warnings; auto-fill | The path acceptance passes, including the expanded count and non-colour stability coding |
| 4 | MMC transition editor | `mmc-transition-editor` | 2-site chain picker, site A/B pickers, same-site explanation, exchange-matrix preview | The MMC acceptance passes, including the same-site case |
| 5 | Transition summary and compile preview | `transition-summary-and-compile-preview` | Summary panel; the additive `TransitionRulePreviewService`; the rule/triplet view; `CMP009` explanation; cancellation, progress, and cache | The preview shows the real compiler's rules for the selected transition and explains an unreferenced transition |
| 6 | 24B tests, documentation, and closeout | `tests-docs-and-closeout` | The §12 suite, the §11 documentation (including `tutorials/ceria-kmc.md`), parity/status updates, `docs/planning/closeout/step-24b.json` (`"step": "step-24b"`), the pre-audit gate, and the audit request | `./tools/verify.sh all --require` passes; the strict docs build passes; `check_closeout_evidence.py` validates; the audit request is complete |

**Owner input needed (at the freeze or the start gate)**

1. **D-5:** approve the additive `TransitionRulePreviewService` (keeps the real-compiler preview and the Full
   class), or defer the compile preview to Step 26 and accept that the per-transition real-compiler preview is
   dropped (then the class is re-recorded Light and this outcome is recorded as a scope change).
2. Accept **class High** as one unit with no further split (§14) — or with D-5 deferral, accept the re-recorded
   class.
3. Approve the six-feature list and the feature-branch list.

### 16. Freeze and closeout sequencing

1. 24A feature 1 (`shared-editor-framework`) is squash-merged on the 24A step branch.
2. 24B §1–§9 are reconciled against the framework as built; the design is corrected where the framework differs.
3. This page is frozen at a specific commit; `reviews/step-24b.md` is created from the template at that commit and
   the whole design is re-checked for internal consistency.
4. A fresh review agent reviews at the recorded class depth; the owner approves the verdict and the 24B start gate
   **after 24A is merged into `main`** (24B's step branch is created from the reviewed `main` commit).
5. The 24B closeout, pre-audit gate, and independent audit follow the normal rules; `closeout/step-24b.json` binds
   the audited commit.

## Acceptance criteria

- [ ] A user can author the ceria oxygen KMC migration (state changes, groups, chain, path) without the CLI.
- [ ] The chain builder supports all six built-in patterns and the custom linker grammar.
- [ ] The occupation-matrix preview and the compiler interpret `ParticleOrder` and `StateChangeMatrices` identically.
- [ ] KMC path editing shows stability by glyph and word (never colour alone) and the symmetry-expanded path count.
- [ ] Ring, consecutive-unstable, endpoint, length, and resolution problems appear at the offending card, linker,
      or path point, and block Apply.
- [ ] The MMC editor accepts a same-site exchange and rejects an invalid chain/site with the documented code.
- [ ] The compile preview shows the real compiler's rules and state triplets for the selected transition, explains
      an unreferenced transition, and changes nothing on cancellation.
- [ ] Every transition diagnostic navigates to the offending field/card/point.
- [ ] Undo/redo works across every 24B edit, and the save point stays correct.
- [ ] Documentation written (the §11 pages), including the ceria tutorial.

## Documentation

See §11. `docs/user-guide/transitions.md` and `docs/tutorials/ceria-kmc.md` are updated (they exist as reference
and tutorial skeletons); `ui-architecture.md` and `errors.md` gain the 24B sections.

## Tests

See §12. The compiler-preview test runs the real compiler; the preview/matrix tests compare against
`TransitionPreview`; the acceptance test uses `samples/transitions/ceria-kmc.mocprj` because the base ceria sample
has no transition model.

## Risks and mitigations

See §13.

## Done when

A user can author every transition mechanism in the model — state changes, groups, chains, KMC paths, and MMC
exchanges — in a responsive, keyboard-complete editor that shows the compiler's own matrices and rules, explains
every validation failure at the exact place it occurs, and is undoable end to end.
