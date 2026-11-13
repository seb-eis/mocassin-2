# Step 24 — Particle, Structure, and Transition Editors

**Phase:** F. GUI  
**Depends on:** 23, 03, 04
**Delivers:** Production editors for particles/sets, crystal structure/sites, and the full transition model,
with live validation, previews, and undo/redo.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** Not requested
- **Last updated:** —

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

### 0. Editor layout spike

Before the final editor design is frozen, build a small owner-reviewable POC of the candidate layouts. This is a
gated decision: the owner sees each option with representative particle, site, and transition data and picks the
layout before Step 24 implementation starts.

POC options:

1. **ListBox table + detail editor** (baseline) — header `Grid`, virtualized `ListBox` rows, and a
   `ContentControl` that shows the selected item's type-specific editor.
2. **TreeView + detail editor** — hierarchy for model/domain objects, editor on the right.
3. **TabControl + list + detail** — one tab per domain with a table/list and editor.
4. **SplitView / GridSplitter master-detail** — resizable selection and editor panes.
5. **Custom `TableView` `TemplatedControl`** — reusable table surface with headers, selection, sorting hooks, and
   virtualization.
6. **Custom matrix control** — for site-energy and other matrix-shaped data only.

Each POC must demonstrate:

- selection, keyboard navigation, focus, and multi-select where needed;
- virtualization with 1,000+ rows;
- stable header/row column alignment;
- search/filter/sort behavior;
- opening a type-specific editor with validation, reference pickers, and undo/redo;
- theme, high-contrast, and screen-reader labeling constraints;
- implementation size and maintenance cost.

Deliverable: a short POC report with screenshots, the recommended option, and the owner decision. Record the
decision in [`ADR 0003`](../adr/0003-avalonia-mvvm-stack.md) and freeze the final layout before implementing the
real editors. Do not add a data-grid dependency while the spike is open.

**Avoid:** Avalonia `DataGrid` (deprecated), `TreeDataGrid` Pro/paid controls, third-party grid dependencies,
inline cell editing for complex model objects, non-virtualized `ItemsControl` in a `ScrollViewer`, and
independently auto-sized header/row grids.

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

## Implementation tasks

- [ ] Build and review the editor-layout POC options; record the owner decision.
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

- [ ] The owner has reviewed the layout POCs and the chosen layout is recorded in ADR 0003.
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
