# Step 24 — Particle, Structure, and Transition Editors

**Phase:** F. GUI  
**Depends on:** 23B, 03, 04
**Delivers:** Production editors for particles/sets, crystal structure/sites, and the full transition model,
with live validation, previews, and undo/redo.
**Design class:** **Full** — the §0 layout pick is an irreversible, ADR-recorded decision that shapes every
editor, and the shared editor framework is a cross-step GUI architecture boundary (Step 25 builds on it;
Steps 26–28 inherit its conventions). No format change and no new dependency; full justification in the
[classification section](#design-and-complexity-classification).

## Implementation status

- **Status:** Not started — the pre-step §0 layout gate is open (spike specification in §0; gate sequence in §0.7)
- **Step branch:** — (created only after the §0.7 gate sequence completes)
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** Not requested — the record `reviews/step-24.md` is created at the design freeze, after the
  owner picks the layout (§0.7); it is never pre-seeded
- **Design class:** **Full** (header above; justification in the classification section)
- **Complexity class:** **High** — cross-step interfaces, test-matrix breadth, more than one independent
  acceptance story, and an evidence set too large for one audit sitting. The split triggers in
  [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers) fire; a **24A/24B split is
  proposed** (classification section) and the owner decides it at the §0.7 layout-pick gate.
- **Layout spike:** owner-reviewed POC of all five §0.1 options; branch `fix/step-24-layout-poc`; build assigned
  to the paid `dev` binding per the owner decision of 2026-11-22
- **Last updated:** 2026-11-22

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Give users a productive way to author the first half of the model. The editors must make symmetry and
transition semantics visible, prevent invalid states where possible, and explain errors precisely.

## Original reference

- **Particles:** two WPF DataGrids (particles: name, symbol, charge, vacancy; sets: name + drag-and-drop particles
  from a row header). Void is implicit at index 0.
- **Structure:** space-group table with 274 entries and operations; cell parameter editor that locks dependent
  fields; cell-site table (A,B,C, stability, particle set) with a live P1 expansion preview; dummy positions.
- **Transitions:** state changes (donor/acceptor), groups (make group), chains (connector string parsed into
  dynamic/static), MMC transitions (chain + two sites), KMC transitions (chain + path vectors).
- Validation is a separate "Model Validator" console tab; editors do not show inline errors consistently.
- No undo/redo.

## Design

### 0. Editor layout spike (owner-reviewed POC, gated)

Before the final editor design is frozen, build an owner-reviewable POC of the candidate layouts. This is a
gated decision: the owner sees every option with representative particle, site, and transition data and picks
the layout before Step 24 implementation starts. The owner decided on 2026-11-22 that the spike covers **all
five layout options** below, and that the custom matrix control (option 6) is evaluated as a data-shape
control inside whichever layout wins, not as a sixth layout. The same owner decision assigns the spike build to
the paid `dev` binding (the `dev-free` allowance was reported exhausted for that day); the lead records the
fallback as [`model-economics.md`](../../development/model-economics.md) §2 requires.

**Hard constraint — repeat of the standing ADR 0003 rule: no data-grid dependency while the spike is open.** The
spike adds no package and no third-party control. It must not adopt `Avalonia.Controls.DataGrid` (deprecated),
`TreeDataGrid` Pro/paid controls, or any third-party grid. If an option appears to require a new dependency,
stop and relay the question to the owner: a new control dependency needs its own ADR recording the owner
decision, licence, maintenance risk, and Avalonia 12 compatibility, and it is not part of this step.

#### 0.1 Options in the spike

1. **ListBox table + detail editor** (baseline) — header `Grid`, virtualized `ListBox` rows, and a
   `ContentControl` that shows the selected item's type-specific editor.
2. **TreeView + detail editor** — hierarchy for model/domain objects, editor on the right.
3. **TabControl + list + detail** — one tab per domain with a table/list and editor.
4. **SplitView / GridSplitter master-detail** — resizable selection and editor panes.
5. **Custom `TableView` `TemplatedControl`** — reusable table surface with headers, selection, sorting hooks,
   and virtualization.
6. **Custom matrix control** — *not* a layout option (owner decision, 2026-11-22). It is a data-shape control
   for matrix-shaped data — the chain builder's S0/S1/S2 occupation matrices, the MMC exchange-matrix preview,
   and Step 25's site-energy matrices — and is evaluated where those surfaces appear **inside** the five
   layouts, not as a competitor window shape.

#### 0.2 What every option must demonstrate

Every option is exercised as the *selection surface* of the same master-detail editor: the table/list only
selects; the type-specific editor owns the fields, validation, reference pickers, and undo/redo. Per option,
the spike must demonstrate and record:

- **Selection, keyboard navigation, focus, and multi-select where needed.** Real-key headless input
  (`HeadlessSession.DispatchAsync`, `window.KeyPress`) proves arrow/Home/End navigation, Tab order into the
  editor pane, and Enter/Escape behaviour; multi-select is required only where bulk delete is safe (particles,
  sites).
- **Virtualization with 1,000+ rows.** The generated project value of §0.4 must scroll, select, and filter
  without losing rows or alignment; the spike records the realized-container behavior (and any option that
  cannot virtualize fails here).
- **Stable header/row column alignment.** The known failure mode is two independently auto-sized grids drifting;
  each option must show columns stay aligned on window resize and long content.
- **Search/filter/sort behavior.** A search box, a filter, and a sort over the real ceria particle and site
  lists, recording how each option wires them (built-in support vs view-model code).
- **Opening a type-specific editor with validation, reference pickers, and undo/redo.** Selecting a particle,
  a site, and a transition opens that type's editor; an invalid edit shows an inline error and changes nothing;
  a valid edit goes through the real `ProjectEditCoordinator.Apply` and is undoable. Reference pickers are
  searchable name-plus-details combos, not raw IDs.
- **Theme, high-contrast, and screen-reader labeling constraints.** Every option is captured in light, dark, and
  high-contrast themes using the production theme system (§0.5); every interactive control must carry an
  automation name and role; status is never colour-only (glyph plus text, as in the 23B status bar).
- **Implementation size and maintenance cost.** A per-option estimate of new files/lines, what the six domains
  reuse, what each later domain must repeat, and who owns the sorting/filtering/column/a11y code afterwards.

#### 0.3 Per-option requirements

Each option must additionally answer the weakness ADR 0003 records for it:

| Option | Must additionally demonstrate |
| --- | --- |
| 1. ListBox table + detail | how sorting, filtering, and column sizing work without a grid: one shared column-width policy (fixed/star), header-click sort, view-model filter; the cost of having no built-in column resize/reorder |
| 2. TreeView + detail | whether a hierarchy earns its place for flat domains (particles, sites): dense columnar comparison inside tree rows, cross-object sort/filter, expansion and keyboard cost; where a tree genuinely helps (domain navigation, chain patterns) |
| 3. TabControl + list + detail | the cost of nested tabs under the shell's document tabs (two tab levels, gesture conflicts), how many tabs six domains produce, and cross-domain context loss (editing a particle while checking a transition reference) |
| 4. SplitView / GridSplitter | pane reflow when narrow and wide, keyboard focus across the splitter, minimum pane sizes, whether resizable panes beat fixed star columns enough to justify the chrome, and interaction with the shell's persisted panel sizes |
| 5. Custom `TableView` | the real cost of owning selection, keyboard, a11y, headers, and a column policy in a `TemplatedControl`; that it composes with the merged theme/accent tokens; an LOC/maintenance estimate against the composed options; whether resize/reorder can be added later without a rewrite |

#### 0.4 Representative data

Real data through the real store — the spike does not invent model shapes:

- **Particles, sets, sites, structure:** `samples/ceria/ceria.mocprj`, loaded through `ProjectStore`
  (the `ProjectFixtures` pattern): 3 particles including the pinned read-only vacancy, 3 particle sets, 3 cell
  sites, space group 225, the cubic cell, and 1 dummy position.
- **Transitions:** `samples/transitions/ceria-kmc.mocprj` and `samples/transitions/ceria-mmc.mocprj` — the ceria
  structure with a KMC oxygen migration and an MMC exchange; pattern breadth from
  `samples/transitions/interstitialcy5.mocprj`, `vehicle5.mocprj`, and `polaron-hop.mocprj`. **The base ceria
  sample has no transition model — do not demonstrate transitions from it.**
- **1,000+ row virtualization:** generated toy data — an in-memory project value with 1,000+ generated sites
  (and a padded particle list) from a spike-local generator. Breadth only, not a plausible physical model, per
  the testing strategy's "generated toy data for breadth".
- **Edits and undo:** the spike binds real documents and the real 23B `ProjectEditCoordinator`/`ProjectDocument`
  for the validation/undo/redo demonstrations; it must not fake an undo stack in spike-local code.

#### 0.5 Capture and accessibility evidence

The spike uses the repo's existing headless/screenshot infrastructure, not a new harness:

- Build the spike surfaces against the same composition and theming as the shell (`AppHost.Build`, the shell
  window or a spike window using the merged theme resources) and capture with
  `window.CaptureRenderedFrame()` after `HeadlessSession.Pump`, as `Visual/ScreenshotTests.cs` does.
- Per option, capture at least: the list with a selection, the type-specific editor open, and a filtered state —
  each in **light**, **dark**, and **high contrast** (`ThemeSelection.Light`, `ThemeSelection.Dark`, and the
  dark + high-contrast + compact + reduced-motion settings the existing baselines use).
- Keyboard and a11y evidence: real-key navigation (`window.KeyPress`) and automation-name/role assertions on
  every interactive control, following `Visual/AccessibilityTests.cs`; record any control that cannot be named
  or driven by keyboard.
- The spike writes PNGs to a spike-local output folder, **not** to `Visual/Baselines/` — no committed baseline
  is added or changed, and no cross-OS pixel comparison is attempted (the existing screenshot policy holds).
- The fixed accent palette (`#007ACC`) and the merged theme tokens are used as-is; the spike restyles nothing
  and must not reopen the accent/palette maintenance work.

#### 0.6 Spike execution, deliverable, and disposition

- **Branch:** `fix/step-24-layout-poc`, a short-lived out-of-step maintenance branch from current `main`
  (`AGENTS.md` §3.5). No step branch exists yet; the spike is not Step 24 feature work.
- **Binding:** the paid `dev` binding builds the spike (owner decision, 2026-11-22).
- **Code location:** spike code is test-only — under `Mocassin.App.Tests` (a spike folder), so the production
  projects gain nothing and discarding is a folder delete. No production project, contract, settings, or CLI
  change is merged from the spike.
- **Deliverable:** a short POC report — one section per option with the screenshots, the §0.2 checklist
  results, the §0.3 answers, the measured/estimated size and maintenance cost, a comparison table, and a
  **recommended** option with rationale. The recommendation is input only: the owner picks the layout, and the
  spike must not pre-write a "chosen layout" into ADR 0003 or this step page.
- **Durable location of the report:** `docs/planning/maintenance/<date>-step-24-layout-poc.md`, with a bounded
  committed PNG subset under `docs/planning/maintenance/assets/step-24-layout-poc/`. Rationale: the spike runs
  as an out-of-step `fix/` branch exactly like the GUI maintenance passes (visual refresh, fixed accent, icon
  pass), whose records already live there; the report is decision-input evidence, not a normative design (the
  pick itself belongs in ADR 0003) and not an implementation authorization, so `docs/planning/drafts/` is the
  wrong home. Planning records stay out of the published site, which is correct for internal decision
  evidence. The committed PNG subset is bounded — at minimum the light and high-contrast editor-open captures
  per option plus the interaction states that justify the recommendation; the full capture matrix stays on the
  spike branch for the owner's review.
- **Disposition (owner decision, 2026-11-22):** after the owner records the pick in ADR 0003, the spike **code
  is discarded** — the spike folder is deleted and the branch is deleted locally and remotely. The durable
  artifacts are (a) the maintenance-record report with its PNG subset and (b) the ADR 0003 update; both land on
  `main` through the branch's owner-approved `--no-ff` merge (`[skip ci]`). The implementation does not seed
  from spike code; it re-derives from the frozen design.

#### 0.7 Gate sequence (explicit)

No step branch, feature branch, or design-review record exists until this sequence completes:

1. **Spike** — the five options are built, captured, and documented on `fix/step-24-layout-poc` (§0.1–§0.6).
2. **Owner picks the layout** from the POC report; the owner also decides the split question
   ([classification section](#design-and-complexity-classification)) at the same gate.
3. **The pick is recorded in ADR 0003** — the option table gains the chosen layout and the reasons. If the
   split is approved, the step index, the `04-roadmap.md` §3 forecasts, the dependency graph, and the
   milestone rows are updated in the same change, before any design freeze
   ([`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers)).
4. **Design freeze** — the step page is completed against the chosen layout and reconciled with the 23A/23B
   seams as built (see the §1 reconciliation note); the docs are frozen at a specific commit.
5. **Design review** — `docs/planning/reviews/step-24.md` is created **at the freeze** from the template (never
   pre-seeded); a fresh review agent reviews at Full depth and stops when no Blocker or Major remains.
6. **Owner start-gate approval** — the owner approves the verdict and the feature list.
7. **Step branch** — created from the reviewed `main` commit; implementation starts.

**Avoid (unchanged from ADR 0003):** Avalonia `DataGrid` (deprecated), `TreeDataGrid` Pro/paid controls,
third-party grid dependencies, inline cell editing for complex model objects, non-virtualized `ItemsControl`
in a `ScrollViewer`, and independently auto-sized header/row grids.

### 1. Shared editor framework

```csharp
public abstract class ModelEditorViewModel<TModel> : ObservableValidator
{
    public ProjectDocument Document { get; }
    public IReadOnlyList<ModelObjectListItem> Items { get; }
    public ModelObjectListItem? SelectedItem { get; set; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public IRelayCommand AddCommand { get; }
    public IRelayCommand RemoveCommand { get; }
    public IRelayCommand DuplicateCommand { get; }
}
```

- Master-detail layout: a searchable table/list selection surface and a type-specific editor in a
  `ContentControl`. The table is only a selection surface; the editor owns the fields, validation, reference
  pickers, and undo/redo.
- The selected layout comes from the Step 24 layout spike. Until then, use the `ListBox` table + detail baseline
  from ADR 0003.
- Add/Remove/Duplicate/Revert commands with undo integration.
- Inline validation: fields show errors/warnings with tooltips; a summary bar links to the Problems panel.
- Dirty fields are highlighted; "Apply" is immediate (undoable) or explicit for multi-field forms, configured
  per editor.
- Reference pickers are searchable combo boxes showing name + key details, not raw IDs.
- Empty states explain what to do next.
- Keyboard: Tab order, Enter to apply, Esc to revert, Ctrl+D duplicate, Delete removes with confirmation.
- Multi-select delete where safe.
- **Reconciliation note (pre-freeze):** the sketch above is **indicative**. At the §0.7 design freeze it is
  reconciled against the 23B seams as built: `ProjectSession` owns the one `ProjectEditCoordinator`, editors
  apply only through `Apply` (never mutating a `Project` directly), subscribe to `ProjectChanged`/
  `EditRejected`, and consume the existing Problems channel and validation surface rather than duplicating them.
  Any shell seam signature change beyond the documented 23A/23B fills stops the step for an amendment, as in
  23B §1.

### 2. Particle editor

**Particles table**

- Columns: name, symbol, charge, vacancy, index (computed), usage count (sets/transitions).
- Void particle is shown pinned at the top, read-only, with an explanation.
- Add creates a particle with a unique default name and charge 0.
- Editing charge/symbol updates validation and dependent previews immediately.
- Vacancy toggle warns if the default occupation is charged.
- Delete is blocked if referenced, with a list of dependents and a "show usages" action.
- Bulk actions: duplicate, set charge, assign symbol group.

**Particle sets**

- List of sets with member count and usage.
- Member editor:
  - drag particles from the particle list into the set (and reorder);
  - first member badge "default occupation";
  - add/remove buttons for keyboard users;
  - void cannot be added;
  - empty set is invalid and shows an inline error.
- Set preview shows the allowed occupations as colored chips.
- Duplicating a set copies members and gets a new ID.

### 3. Structure editor

**Space group**

- Searchable picker by number, Hermann–Mauguin, Hall symbol, crystal system.
- Settings selector for origin choice/unique axis/rhombohedral axes.
- Details panel: operation count, general multiplicity, crystal system, operation list (matrix/translation and
  human-readable form).
- Changing the space group warns that site orbits and dependent objects may change; the editor previews the
  new orbit sizes before applying.
- A "compatible with current sites" indicator.

**Cell parameters**

- Six fields with units (Å/degrees) and a numeric expression evaluator (`2*pi`, `ln`, `sqrt`, etc.).
- Dependent fields lock according to the crystal system; a tooltip explains why.
- Live cell vectors/volume display.
- Invalid angle combinations show the violated inequality.
- "Reset to cubic" / "reset to defaults" actions.

**Cell sites**

- Table: name, fractional A/B/C, stability, particle set, orbit size, multiplicity, Cartesian position.
- Add site: choose stability and particle set; coordinate fields accept fractions/expressions.
- P1 preview panel:
  - list of expanded positions with fractional/Cartesian coordinates and symmetry operation;
  - 2D projection or mini 3D preview (full 3D in step 27);
  - overlap warnings highlighted.
- Dummy positions tab: visual-only points, add/edit/remove, distinct styling.
- Site deletion shows dependents (transitions, environments, clusters, dopings) and offers cascade removal
  with confirmation.
- Stability change warns if the site is used by a KMC/MMC transition.

**Structure summary**

- P1 position count, stable/unstable counts, cell volume, particle-number density (`count/volume`), orbit table.
- Performance warning if the P1 count is large.

### 4. Transition editor

Tabs or a master-detail tree:

1. **State changes**
   - Table: name, donor, acceptor, vacancy pair, unstable pair, usage count.
   - Donor/acceptor pickers show charge/symbol; void is selectable as acceptor.
   - Inline warning for donor == acceptor, both void, or charged vacancy pitfalls.
2. **State-change groups**
   - List of groups with member state changes.
   - Add/remove members; "make group from selection" action.
   - Mixed physical/charge-exchange warning.
3. **Chains**
   - Visual chain builder:
     - choose a pattern (2-site, 3-site, 5/7 interstitialcy, 5/6 vehicle, custom);
     - position cards in order, each with a group picker;
     - linker selector between cards (dynamic/static) for custom;
     - reverse-generation toggle with explanation;
     - preview of the S0/S1/S2 occupation matrices, using the Step 04 `StateChangeMatrices` and
       `ParticleOrder` contract so the editor and compiler interpret matrix indices identically.
   - Validation messages directly on the offending card/linker.
4. **KMC transitions**
   - Chain picker; reference path editor:
     - add/remove path points;
     - enter fractional coordinates or pick a reference site;
     - show stability sequence with color coding;
     - 3D/2D path preview and symmetry-expanded path count;
     - warnings for rings, consecutive unstable, wrong endpoints.
   - "Auto-fill from sites" helper.
5. **MMC transitions**
   - Chain picker (2-site only), site A/B pickers, same-site exchange explanation.
   - Preview of the exchange matrix.

**Transition summary**

- Expected rule count (from the compiler preview), number of symmetry-equivalent paths, dependent/hidden
  rules, charge/mass conservation status.
- A "compile preview" button runs the compiler for this transition only and shows the generated state
  triplets.

### 5. Validation and navigation

- Every editor shows a validation summary for its domain and the whole model.
- Clicking a diagnostic opens the editor and selects the object/property.
- Invalid fields are marked; the object list shows error badges.
- The Problems panel groups by domain/code and supports filtering.
- "Fix" actions for common issues (e.g. mark site unstable, choose a valid particle set).

### 6. Performance

- The particle/site tables virtualize for large models.
- P1 preview and transition rule preview are debounced (250 ms) and cancellable.
- Editing a field does not rebuild the 3D scene until the edit is committed.
- Structure changes show a progress indicator if the orbit expansion takes > 100 ms.

## Non-goals

- No energy, lattice, or simulation editors — Step 25 builds those domains on the same framework delivered here.
- No parameterization, job-template, or build/deploy UI (Step 26); no 3D viewport (Step 27 — the P1 preview is a
  2D projection here and the KMC path preview is 2D); no run monitoring or charts (Step 28).
- No `.mocprj`/`.msl`/`.msr`/`.mcs` format, schema, version, CLI-behaviour, diagnostic-code, or settings change.
- No new package dependency — in particular **no data-grid dependency while the §0 spike is open** (ADR 0003);
  any later control dependency is a separate ADR and owner decision.
- No shell/workspace/lifecycle redesign: the editors fill the 23A/23B seams (`INavigationTargetProvider`,
  `IExplorerNodeProvider`, `IInspectorContributor`, `DocumentViewModel`, `ProjectEditCoordinator`, the Problems
  channel, undo/redo). Any seam change beyond the documented fills stops the step for an amendment.
- The §0 spike's POC code is discarded (§0.6); no spike code seeds the implementation.

## Design and complexity classification

Recorded 2026-11-22 at the pre-spike design pass. If a `Full` trigger appears during the design freeze, the
class is re-recorded before the freeze; a class never moves down after the freeze without an owner decision.

### Design class: **Full**

The step lives inside the existing GUI boundary and touches no `.mocprj`/`.msl`/`.msr`/`.mcs` format, but two
[`design-proportionality.md`](../../development/design-proportionality.md) §1 triggers apply:

1. **Irreversible decision.** The §0 layout pick is owner-gated, recorded in ADR 0003, and shapes the shared
   framework and every domain editor view. Reversing it after the editors are built rewrites the framework and
   all six domains' surfaces — the exact cost the gated spike exists to avoid. ADR 0003 already names this
   decision as a gate.
2. **Architecture boundary.** The shared model-editor framework (selection surface, `ModelEditorViewModel`
   base, reference pickers, undo/validation integration) is the editing boundary that Step 25 builds on and
   Steps 26–28 inherit interaction conventions from; a late change rewrites editor code across later steps.

A third trigger is likely rather than certain: **additive public library APIs** — 23B needed three additive
Core/Packages APIs when the shell required them, and the editors may need additive helpers (previews, usage
queries). No new dependency is proposed (the spike forbids one), so the dependency trigger does not apply.
Review depth: the full checklist, stopping when no Blocker or Major remains; Minor and Note findings are recorded
and accepted or deferred without another round.

### Complexity class: **High**; the split triggers fire

| Dimension | Class | Evidence |
| --- | --- | --- |
| Contracts, formats, and public surfaces | **Medium** | No format change; editors go through the existing `ProjectEditor`/`ProjectEditCoordinator` path; likely small additive public APIs only (23B precedent: three additive APIs) |
| New dependencies | **Low** | None — the spike forbids a grid dependency; the custom `TableView` option is in-repo; any dependency would be a separate ADR and owner decision |
| Cross-step interfaces | **High** | The shared editor framework, reference pickers, and the ADR 0003 layout are consumed by Step 25 and constrain Steps 26–28 |
| Irreversibility | **Medium–High** | The ADR-recorded layout pick is expensive to reverse after the editors exist; the master-detail split itself (table selects, editor edits) is already fixed by ADR 0003 regardless of which option wins |
| Test-matrix breadth and failure modes | **High** | Six domains × (headless view-model, headless view, screenshot baselines, a11y/keyboard, drag-and-drop plus keyboard equivalents, undo/validation integration) on the 23A/23B harness |
| Independent acceptance stories | **High** | Particle/set editing, structure editing, and transition editing are separately demonstrable and rejectable outcomes |
| Evidence size for a fresh auditor | **High** | A 10–12-feature list, six domains, screenshot baselines, and compiler-preview fixtures exceed one audit sitting |

**Class: High**, and the split triggers in
[`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers) fire: more than one independent
acceptance story, more than the 6–8-feature band, and an evidence set too large for one sitting. The step is
**not silently kept oversized** — the owner decides at the §0.7 layout-pick gate between the proposed split below
and an explicit one-unit acceptance.

### Proposed split: 24A (particles and structure) / 24B (transitions)

The boundary is dependency-clean and mirrors the Phase B library split (Step 03 = particles + structure; Step 04 =
transitions):

- **24A — particle and structure editors:** the shared editor framework and the chosen layout surface;
  particles and sets; space group, cell parameters, cell sites, P1 preview, dummy positions, structure summary.
- **24B — transition editors:** state changes, groups, chains, KMC, MMC, transition summary and compile preview;
  it consumes 24A's delivered framework and reference pickers unchanged.
- The halves stay clean at deletion time: 24A lists transition/environment dependents as **usage counts from
  the project value** without any transition editor existing; 24B's pickers reference particle/site **data**,
  not 24A's views.
- Both halves remain High on cross-step interfaces and test-matrix breadth, like the 23A/23B precedent — each
  needs its own owner acceptance at its own start gate. The split reduces the acceptance-story count and the
  evidence size, which are the dimensions the triggers name.
- If approved, the step index, the `04-roadmap.md` §3 forecasts, the dependency graph (Steps 25–27 chain from
  24B), and the milestone rows are updated **before** the design review freezes. If the owner instead accepts
  one unit, the one-unit case below is recorded and no roadmap change is made.

**One-unit fallback (only on explicit owner acceptance):** the twelve-feature list below on a single step
branch `step/24-model-editors-particle-structure-transitions`, with a recorded stop rule — if the shared
framework feature needs a shell seam beyond the documented 23A/23B fills, or the layout pick forces an interface
change after the framework lands, the step stops and records an amendment before the transition features start.

### Proposed feature-branch list

Proposed for owner approval at the start gate (§0.7 step 6); the §0 spike itself is not a feature branch. The
per-half prefixes become `feature/step-24a-model-editors-particles-and-structure/<slug>` and
`feature/step-24b-model-editors-transitions/<slug>` if the split is approved, or
`feature/step-24-model-editors-particle-structure-transitions/<slug>` for the unsplit fallback. The final
branch and step-branch slugs are fixed when the owner approves the list.

| # | Half | Slug | Scope | Done when |
| --- | --- | --- | --- | --- |
| 1 | 24A | `shared-editor-framework` | The chosen layout surface from ADR 0003; the `ModelEditorViewModel` base, list/item view models, reference pickers; Add/Remove/Duplicate/Revert through `ProjectEditCoordinator.Apply`; inline validation plus summary link; empty states; keyboard map; `particle`/`structure` navigation targets and explorer nodes | CRUD and undo work headlessly on a real ceria project; a diagnostic navigates into an editor and focuses the offending field |
| 2 | 24A | `particle-and-set-editors` | Particles table (pinned read-only vacancy, usage counts, charge/symbol, vacancy warnings, blocked delete with dependents, bulk actions); sets with drag/drop plus keyboard add/reorder, default-occupation badge, empty-set error, occupation preview chips | The particle/set acceptance criteria pass; drag and keyboard-only flows both work |
| 3 | 24A | `space-group-and-cell-parameters` | Searchable space-group picker (number, Hermann–Mauguin, Hall, crystal system), settings, operation details, compatibility indicator, orbit-size preview before applying; cell parameters with expressions, dependent locking, live vectors/volume, invalid-angle display, resets | A space-group change previews the new orbit sizes before applying; cell locking follows the crystal system |
| 4 | 24A | `site-table-and-p1-preview` | Cell-site table; add site with fraction/expression coordinates; P1 expansion preview (list plus 2D projection) with overlap warnings; deletion dependents with cascade confirmation; stability warnings | Overlapping sites are blocked inline; the P1 preview matches the engine orbit list |
| 5 | 24A | `dummy-positions-and-structure-summary` | Dummy positions tab with distinct visual-only styling; structure summary (P1 count, stable/unstable counts, volume, density, orbit table, large-P1 performance warning) | The summary matches the model statistics; dummy positions render distinctly |
| 6 | 24A | `tests-docs-and-closeout` | 24A documentation column, parity/status updates, closeout evidence, audit request | `./tools/verify.sh all --require` passes; the audit request is complete |
| 7 | 24B | `state-change-and-group-editors` | State-change table (donor/acceptor pickers with charge/symbol, void as acceptor, inline warnings, usage counts); groups with membership, make-group-from-selection, mixed-mode warning | State-change validation and usage counts pass their criteria |
| 8 | 24B | `chain-builder` | Pattern picker (2-site, 3-site, 5/7 interstitialcy, 5/6 vehicle, custom); position cards with group pickers; dynamic/static linkers; reverse-generation toggle; S0/S1/S2 occupation-matrix preview (the matrix data-shape control) using the Step 04 `StateChangeMatrices`/`ParticleOrder` contracts; card/linker validation | All built-in patterns and custom linkers work; the editor and compiler interpret matrix indices identically |
| 9 | 24B | `kmc-transition-editor` | Chain picker; path editor with add/remove points, fractional or reference-site entry, stability coding with non-colour markers, 2D path preview, symmetry-expanded path count, ring/consecutive-unstable/endpoint warnings, auto-fill helper | KMC path editing shows stability coding and the expanded path count |
| 10 | 24B | `mmc-transition-editor` | 2-site chain picker, site A/B pickers, same-site exchange explanation, exchange-matrix preview | MMC editing and its exchange preview pass |
| 11 | 24B | `transition-summary-and-compile-preview` | Expected rule count, symmetry-equivalent path count, dependent/hidden rules, charge/mass conservation status; compile-preview of one transition's generated triplets through the real compiler | The compile preview runs the real compiler for the selected transition |
| 12 | 24B | `tests-docs-and-closeout` | 24B documentation column, parity/status updates, closeout evidence, audit request | `./tools/verify.sh all --require` passes; the audit request is complete |

## Implementation tasks

- [ ] **Gate (§0, pre-branch):** the five-option layout POC spike is built on `fix/step-24-layout-poc`, the owner
      has reviewed the report and picked the layout, the split decision is recorded, and the maintenance record
      plus the ADR 0003 update are merged. This gate precedes the design freeze and the step branch; it is not
      step feature work.
- [ ] Implement the shared model editor framework and reference pickers using the chosen layout.
- [ ] Implement particle and particle-set editors with drag/drop.
- [ ] Implement space-group picker, cell parameters, and site table.
- [ ] Implement P1 preview and overlap diagnostics.
- [ ] Implement dummy positions.
- [ ] Implement state change/group/chain editors.
- [ ] Implement KMC path and MMC site editors.
- [ ] Implement transition preview and compile-preview.
- [ ] Wire diagnostics/navigation and undo/redo.
- [ ] Add headless view-model tests and screenshot baselines.

## Acceptance criteria

- [ ] The owner has reviewed the layout POCs and the chosen layout is recorded in ADR 0003, with the POC report
      kept as the durable maintenance record (§0.6) and the spike code discarded.
- [ ] A user can create the ceria particles, sets, structure, and oxygen migration without the CLI.
- [ ] Drag-and-drop and keyboard-only flows both work for particle sets.
- [ ] Changing the space group previews the new orbit sizes before applying.
- [ ] Overlapping sites and invalid paths are blocked with inline errors.
- [ ] The chain builder supports all built-in patterns and custom linkers.
- [ ] KMC path editing shows stability colors and symmetry-expanded path count.
- [ ] Every diagnostic navigates to the offending field.
- [ ] Undo/redo works across all edits.
- [ ] Large tables and previews remain responsive.
- [ ] Documentation written

## Documentation

At this point, document the model editors:

- `docs/user-guide/particles.md`, `structure.md`, `transitions.md` — editor workflows, previews, validation.
- `docs/tutorials/ceria-kmc.md` — first use of the structure/transition editors.
- `docs/development/ui-architecture.md` — editor framework, reference pickers, undo/redo.
- `docs/troubleshooting/errors.md` — editor validation failures.

## Tests

| Test | Assertion |
| --- | --- |
| Editor acceptance | Real project documents and `ProjectEditor` operations drive CRUD/undo; the domain is not mocked |
| Particle CRUD | Add/edit/delete/duplicate, void rules |
| Sets | Drag/drop order, default badge, empty error |
| Space group | Search/settings/change preview |
| Cell params | Locking, expressions, invalid angles |
| Sites | Coordinates, stability, overlap, deletion dependents |
| P1 preview | Orbit list matches engine |
| State changes | Validation and usage counts |
| Chains | Pattern/linker builder and matrices |
| KMC paths | Add/remove/pick, stability, preview |
| Navigation | Diagnostic → editor/property |
| Undo/redo | Every editor operation |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Symmetry preview performance | Debounce, cancel, cache by hash |
| Complex transition UI | Visual chain cards + compile preview |
| Drag/drop accessibility | Keyboard add/reorder equivalents |
| Cascading deletions | Dependency list + explicit confirmation |
| Editor/API divergence | Editors call the same `ProjectEditor` operations as the CLI |

## Done when

Users can author particles, structure, sites, and every transition mechanism in a responsive, validated GUI
with undo/redo and precise navigation from errors.
