# Step 24 editor-layout POC (2026-11-22)

An out-of-step maintenance spike under `AGENTS.md` §3.5 for the Step 24 layout gate. It builds all five candidate
editor layouts from the frozen §0 specification, captures them with real ceria data and a generated 1,024-row
breadth value, and records the evidence for the owner's layout pick. It does not choose the layout; the pick
belongs in ADR 0003.

| Field | Value |
| --- | --- |
| Type | Out-of-step layout POC / decision-input evidence |
| Branch | `fix/step-24-layout-poc` |
| Base | `main` at `536c8b3` |
| Specification | Step 24 §0, frozen at `b3258ac` on `docs/step-24-design` |
| Binding | paid `dev` binding (owner decision 2026-11-22; the `dev-free` allowance was exhausted) |
| Status | Spike complete — owner layout pick and split decision pending (§0.7 step 2) |
| Design class | Not a step feature; no design review. Evidence only, no normative design. |
| Dependencies | **None added.** No `Avalonia.Controls.DataGrid`, no `TreeDataGrid`, no third-party grid, no package change. |
| Durable artefacts | this record and the 15 PNGs under [`assets/step-24-layout-poc/`](assets/step-24-layout-poc/) |
| Disposition | spike code is discarded after the owner records the pick in ADR 0003 (§0.6) |

## 1. What the owner decides from this record

1. **The layout** (options 1–5 below). The recommendation in §9 is input only.
2. **The 24A/24B split** (the step page's classification section), at the same gate.
3. **The disposition confirmation**: delete the spike folder after ADR 0003 records the pick.

No step branch, feature branch, or design-review record exists yet. Nothing in this record is normative; the
step page is completed against the chosen layout at the §0.7 design freeze.

## 2. Constraints and method

The spike obeys the frozen §0 constraints:

- no dependency and no third-party control;
- spike code is test-only, under `Mocassin.App.Tests/LayoutSpike/`; no production project, contract, settings,
  CLI, or `Visual/Baselines/` file is touched, so deleting the folder discards the spike;
- the production theme and accent are used as-is; no restyle and no palette change;
- the production shell headless/screenshot infrastructure is reused, not extended;
- the real `ProjectDocument`/`ProjectEditCoordinator` is the only edit path; no fake undo stack;
- real samples are loaded through `ProjectStore`; generated data is breadth only.

Method:

| Area | Implementation |
| --- | --- |
| Windows | `AppHost.Build` + `AvaloniaThemeApplier` + a spike `Window` whose content is the stage, shown in the existing `HeadlessSession` |
| Captures | `window.CaptureRenderedFrame()` after `HeadlessSession.Pump`, saved as PNG outside `Visual/Baselines/` |
| Input | real keys through `window.KeyPress` and row containers focused via their realized `ListBoxItem`/`TreeViewItem` |
| Editing | select → type/edit → `ProjectEditCoordinator.Apply` → rebuild rows and fields from `Document.Project`; undo through the document's own stack |
| Data | ceria model samples for particles/sets/sites; `ceria-kmc` is the editable transition document; `ceria-mmc`, `interstitialcy5`, `vehicle5`, and `polaron-hop` contribute display-only breadth rows; a generated 1,024-site value proves virtualization |
| Evidence | per-option behaviour tests, editor tests, accessibility sweep, data guard, and the capture matrix (§12) |

All measurements in §7 come from one headless Linux run in this container. They are comparative, not benchmarks.

## 3. What was built

```text
src/dotnet/Mocassin.App.Tests/LayoutSpike/
├── SpikeStage.cs                    shared chrome: notice, toolbar, status bar, Enter/Escape
├── SpikeWorkspaceViewModel.cs       the six domain surfaces over the bound projects
├── SpikeDomainViewModel.cs          search/filter/sort/selection/multi-select per domain
├── SpikeInteractions.cs             control lookup and real-key helpers for the tests
├── SpikeTestHost.cs                 AppHost + theme + headless window + capture
├── Data/                            fixtures, row/column builders, toy value, datasets
├── Editing/                         editor view model, view, fields, values, mutations
├── Visuals/                         list surface, column layout, theme, matrix control
├── Options/                         the five layout options and the custom table control
└── Interaction/                     data, behaviour, editor, accessibility, and capture tests
```

Size, measured with `wc -l` on the branch:

| Group | Files | Lines |
| --- | ---: | ---: |
| Shared harness (stage, surface, editor, data, fixtures, capture host) | 31 | 4,378 |
| Option 1 — list + detail | 1 | 59 |
| Option 2 — tree + detail | 1 | 200 |
| Option 3 — tabs + list + detail | 1 | 130 |
| Option 4 — splitter master-detail | 1 | 81 |
| Option 5 — custom table | 2 | 333 (79 composition + 254 control) |
| Evidence tests (five files) | 5 | 902 |

All five options reuse the same stage, editor, field builder, mutation builder, data, and column policy. The
per-option numbers are therefore the marginal cost of the selection surface, which is what the comparison needs.

## 4. Representative data

Loaded through `ProjectStore` exactly as the spec requires:

| Source | Used for |
| --- | --- |
| `samples/ceria/ceria.mocprj` | 3 particles incl. the pinned implicit void row, 3 particle sets, 3 cell sites, space group 225, cubic cell, 1 dummy position. **No transitions**, as specified. |
| `samples/transitions/ceria-kmc.mocprj` | the editable transition document: 2 state changes, 2 groups, 1 chain, 1 KMC transition with a 3-point reference path |
| `samples/transitions/ceria-mmc.mocprj` | 1 MMC exchange row (display-only in the transition table) |
| `samples/transitions/interstitialcy5.mocprj`, `vehicle5.mocprj`, `polaron-hop.mocprj` | pattern breadth: interstitialcy-5 and vehicle-5 KMC chains, Metropolis-2 polaron hop — display-only |
| generated value | 1,024 reference sites plus 24 padded particles, for the virtualization evidence only |

Breadth rows are visibly labelled with their sample column and open a read-only explanation. Editable rows are
the bound `ceria` and `ceria-kmc` documents. The generated value deliberately exceeds
`StructureModel.MaxReferenceSites` (1,000) so the table really holds more than 1,000 rows; it is never applied
through the validator. See §10 for the finding this creates for Step 24.

## 5. Option results

Common evidence, asserted by tests for every option unless stated otherwise: selecting a particle, a site, and a
transition opens that type's editor; an empty name is rejected by the real validator with its diagnostic code and
the project object is unchanged; an unparsable charge is a local inline error; a valid rename is applied through
`ProjectEditCoordinator.Apply`, updates the row, and is undoable; Enter applies, Escape reverts; the matrix
preview appears in the transition detail; the search box, filter, and sort operate over the real data; and the
1,024-row breadth scrolls and selects.

### 5.1 Option 1 — ListBox table + detail (baseline)

Built as: shared header `Grid` + virtualized `ListBox` + fixed 364 px detail pane, header-click sort, multi-select
on particles/sets/sites, bulk-remove command.

| §0.2 item | Result |
| --- | --- |
| Selection/keyboard/focus | arrows, Home/End and multi-select work; End = last row, Home = void row; Tab from a freshly selected row reaches the editor in one press (after Home/End the list keeps Tab inside itself — see §10) |
| Virtualization | 21 realized containers for 1,024 rows; last row selectable; 84 ms to build and pump the breadth view |
| Header/row alignment | exact for all 6 columns at 1280 and 720 (shared widths + the production 20 px scroll gutter) |
| Search/filter/sort | view-model filter and sort; header click toggles ascending/descending |
| Editor/validation/undo | all three editor types; real `PAR001`, `TRN017`-class rejections; undo restores the previous name; bulk delete of referenced particles is refused by the validator |
| Themes/a11y | light, dark, high contrast; every control named; rows list is a `List` role; status is glyph + text |
| Weakness answer (§0.3) | no built-in column resize/reorder; sorting/filtering are view-model code; the header must be kept aligned by construction. The shared policy + gutter makes that one helper, not per-domain code. |

Size: 59 option lines on top of the shared surface. Maintenance: low; sorting/filtering/columns/a11y live in one
shared surface, and a new domain is a column list plus a row builder.

Captures: [`selection-light`](assets/step-24-layout-poc/01-list-detail-selection-light.png),
[`editor-light`](assets/step-24-layout-poc/01-list-detail-editor-light.png),
[`editor-highcontrast`](assets/step-24-layout-poc/01-list-detail-editor-highcontrast.png),
[`filtered-light`](assets/step-24-layout-poc/01-list-detail-filtered-light.png),
[`breadth-light`](assets/step-24-layout-poc/01-list-detail-breadth-light.png).

### 5.2 Option 2 — TreeView + detail

Built as: a `TreeView` of six domain roots with the same dense rows as children, a header above it, and a 380 px
detail pane. Children come from `TreeDataTemplate`s; the active domain is expanded.

| §0.2 item | Result |
| --- | --- |
| Selection/keyboard/focus | arrows move rows; Home/End move the tree cursor to its first/last item instead of the last row; no multi-select (TreeView is single-select here); Tab reaches the editor directly |
| Virtualization | **fails**: 1,058 realized containers for 1,024 rows and 1,521 ms to realize the breadth view (~18× option 1) |
| Header/row alignment | **fails by construction**: rows are indented, so fixed columns shift 25 px and the star name column 53 px against the header |
| Search/filter/sort | filter/sort work through the view model, but sorting is per root; there is no meaningful cross-object order |
| Editor/validation/undo | same shared editor; all flows pass |
| Themes/a11y | all three themes; tree role; named controls |
| Weakness answer (§0.3) | the hierarchy earns its place for domain navigation, not for flat domains: particles and sites lose columnar density and 1,000-row performance. The shell's existing explorer already provides domain navigation. Keep trees for chain patterns/reference browsing, not as the primary editor surface. |

Size: 200 lines. Maintenance: high for flat domains (custom indentation-aware header, no virtualization, no
multi-select); low value.

Captures: [`editor-light`](assets/step-24-layout-poc/02-tree-detail-editor-light.png),
[`editor-highcontrast`](assets/step-24-layout-poc/02-tree-detail-editor-highcontrast.png),
[`breadth-light`](assets/step-24-layout-poc/02-tree-detail-breadth-light.png).

### 5.3 Option 3 — TabControl + list + detail

Built as: an emulated document tab strip (two tabs) containing six domain tabs, each with its own list + editor.
The emulation exists because the spike cannot add a non-production view type to the shell's `ViewLocator`; the
nesting and tab count are the point of the measurement.

| §0.2 item | Result |
| --- | --- |
| Selection/keyboard/focus | arrows/Home/End/multi-select work per tab; Tab reaches the editor in one press from a fresh selection |
| Virtualization | 19 realized containers for 1,024 rows; 58 ms |
| Header/row alignment | exact (shared widths + gutter) |
| Search/filter/sort | the toolbar duplicates the tab strip: two ways to change domain; search/filter/sort state is per domain |
| Editor/validation/undo | all flows pass; each tab keeps its own editor state, so switching away preserves that domain's context |
| Themes/a11y | all three themes; two `Tab` roles; named controls |
| Weakness answer (§0.3) | two tab levels sit under the shell's document tabs: six domain tabs plus the document strip, a second `Ctrl+Tab` owner, and no way to see a particle while checking a transition reference. The per-domain context is preserved, but the cross-domain comparison the spec asks for is lost. |

Size: 130 lines. Maintenance: medium; every domain adds a tab and the tab strip duplicates the domain selector.

Captures: [`editor-light`](assets/step-24-layout-poc/03-tabs-editor-light.png),
[`editor-highcontrast`](assets/step-24-layout-poc/03-tabs-editor-highcontrast.png).

### 5.4 Option 4 — SplitView / GridSplitter master-detail

Built as: list | `GridSplitter` (6 px, keyboard increment 16) | 360 px detail, with 260/280 px minimums and the
shell's focus-adorner convention.

| §0.2 item | Result |
| --- | --- |
| Selection/keyboard/focus | as option 1; Tab visits the splitter first and the editor second, following the shell's splitter convention |
| Virtualization | 21 realized containers; 29 ms |
| Header/row alignment | exact at 1280 and 720 |
| Search/filter/sort | as option 1 |
| Editor/validation/undo | all flows pass; the matrix has more room when the user widens the pane |
| Themes/a11y | all three themes; splitter named "Resize editor pane" with a role and help text |
| Weakness answer (§0.3) | resizable panes work and keyboard resize passes (two Left presses grow the editor, still ≥ 280 px), but the shell already persists three region fractions; a fourth editor fraction needs additive `LayoutSettings` plumbing. At 720 px the fixed columns consume the list and every column truncates — a splitter does not fix narrow windows. |

Size: 81 lines. Maintenance: low-medium (plus the persisted-fraction work if the size must survive restarts).

Captures: [`editor-light`](assets/step-24-layout-poc/04-split-pane-editor-light.png),
[`editor-highcontrast`](assets/step-24-layout-poc/04-split-pane-editor-highcontrast.png),
[`narrow-light`](assets/step-24-layout-poc/04-split-pane-narrow-light.png).

### 5.5 Option 5 — Custom `TableView` `TemplatedControl`

Built as: a `TemplatedControl` with `Columns`, `ItemsSource`, `SelectionMode`, `SelectedItem`/`SelectedRows`, a
`SortRequested` hook, and its own `Table` automation peer. The template contains the shared header and a
`ListBox`; the control forwards selection and re-syncs it after a model rebuild.

| §0.2 item | Result |
| --- | --- |
| Selection/keyboard/focus | single and multi-select work through the control's own `SelectedRows`; arrows/Home/End via the inner list; Tab reaches the editor in one press |
| Virtualization | 21 realized containers; 73 ms |
| Header/row alignment | exact (one control owns both) |
| Search/filter/sort | header click raises `SortRequested`; toolbar sort also wired; filter/search in the VM |
| Editor/validation/undo | all flows pass |
| Themes/a11y | all three themes; `Table` role on the control and `List` on the inner rows; named controls |
| Weakness answer (§0.3) | owning the surface costs 333 lines even while delegating virtualization and row keyboard handling to the inner list. Multi-select plumbing, selection re-sync after every rebuild, and accessibility identity were the real work. Because columns are a value list and the header is built from it, resize/reorder can be added later without a rewrite (estimate 1–2 days plus tests) — but nothing in Step 24 needs it yet. |

Size: 333 lines (79 composition + 254 control). Maintenance: high; one owner must keep the a11y/selection/column
behaviour correct for all six domains.

Captures: [`editor-light`](assets/step-24-layout-poc/05-custom-table-editor-light.png),
[`editor-highcontrast`](assets/step-24-layout-poc/05-custom-table-editor-highcontrast.png).

## 6. Option 6 — matrix data-shape control (not a layout)

`SpikeMatrixControl` renders a `SpikeMatrix` value: row labels, column labels, bordered cells, an automation
name, and a `Table` automation role. The chain editor builds the occupation matrix from the real chain (position
× state-change group); the same control composes unchanged in the MMC exchange preview and Step 25's site-energy
matrices.

Result inside the layouts: it composes in every detail pane and is the reason the detail pane wants width. In
option 1 at 360 px it fits with three columns; in option 3 at 340 px it still fits; in option 4 the splitter gives
it room when needed; in option 2's 380 px pane it fits; option 5 is identical to option 1. It is correctly not a
sixth layout: each option renders the same control in its detail pane, and the screenshots show it there.

## 7. Comparison

| | 1 ListBox | 2 TreeView | 3 Tabs | 4 Splitter | 5 Custom table |
| --- | --- | --- | --- | --- | --- |
| Realized containers, 1,024 rows | 21 | **1,058** | 19 | 21 | 21 |
| Build + pump 1,024 rows | 84 ms | **1,521 ms** | 58 ms | 29 ms | 73 ms |
| Header/row alignment | yes | **no** (25/53 px) | yes | yes | yes |
| Multi-select | yes | no | yes | yes | yes (plumbing) |
| Home/End row navigation | yes | no | yes | yes | yes |
| Tab: row → editor | 1 press | 1 press | 1 press | splitter, then editor | 1 press |
| Column resize/reorder | no | no | no | no | no (addable) |
| New lines | 59 | 200 | 130 | 81 | 333 |
| Detail width | fixed 364 | fixed 380 | fixed 340 | resizable | fixed 364 |
| Owns sorting/filtering | view-model | view-model | view-model | view-model | view-model + header hook |
| Maintenance owner burden | low | high | medium | low-medium | high |

Options 1, 3, 4, and 5 share the same editor, the same search/filter/sort code, and the same column policy, so
the behavioural rows are equal by construction rather than by coincidence. The tree differs where the control
differs: indentation, eager realization, single selection, and tree cursor semantics.

## 8. Application-level observations

- The production 20 px scroll gutter is what keeps header and row columns aligned when the scrollbar appears; any
  option that adopts the shared table surface must keep it (the spike applied the existing `scroll-gutter-right`
  class to the list/tree/control).
- The dark and high-contrast captures use the merged production theme and the fixed `#007ACC` accent; no spike
  colour literal exists in the views. The high-contrast capture is the dark + high-contrast + compact +
  reduced-motion combination the existing baselines use.
- Bulk delete is refused by the real validator for referenced particles; the spike surfaces the refusal text.
  A production editor should pre-check usages to give the dependants list the step page requires.
- The emulated document tab strip in option 3 is a stand-in, not the shell. The nesting cost is real; the exact
  gesture conflicts need confirmation once the surface lives in a real document.

## 9. Recommendation (input only)

**Option 1, the ListBox table + detail baseline,** promoted with the shared column/header helper and the reserved
scroll gutter. Reasons, in evidence order:

1. It is the only option that meets every §0.2 row at the lowest marginal size (59 lines) and the second-fastest
   breadth build, and its behaviour is shared with the editor rather than re-owned.
2. The two options that add real value add it elsewhere: option 4's resizable pane helps the matrix preview;
   option 5's control is the right home if column resize/reorder becomes a requirement. Neither is needed to
   start Step 24, and both can be added behind the same column policy without reworking the editors.
3. Options 2 and 3 fail the spec's own tests: the tree loses virtualization and column alignment, and the tab
   layout adds a tab level and a second domain selector without solving cross-domain comparison.

The recommendation does **not** pick the layout and does not authorize implementation. If the owner wants a
resizable detail pane, option 4 composes with option 1's surface and changes only the container; if the owner
expects column resize/reorder in the first release, option 5 is the honest choice even at 333 lines.

## 10. Findings for Step 24 (not layout choices)

1. **Tab order after Home/End.** Tab from a freshly selected row reaches the editor in one press on every option
   (the splitter is the first stop on option 4, the editor the second). After Home or End, however, the list keeps
   Tab inside itself and cycles the row containers instead of leaving for the editor. Step 24 must set the list's
   keyboard tab navigation (or make containers non-tab-stops) so the detail pane is always one Tab away. This is a
   wiring fix, not an argument for another layout.
2. **The 1,000-row limit.** `StructureModel.MaxReferenceSites` is 1,000 and rejects more; the spec's "1,000+
   generated sites" therefore needs a source other than reference sites for a *valid* production virtualization
   test (for example lattice positions or a permitted generated structure). The spike's value is display-only and
   never applied through the validator.
3. **Tree virtualization.** Avalonia's `TreeView` realizes every expanded child. If a tree is used for chain
   patterns, it must stay small or use a virtualizing data surface; it cannot be the 1,000-row editor table.
4. **Split size persistence.** A durable editor fraction is additive `LayoutSettings` work; the settings block is
   app-local, so no format/contract change is involved.
5. **Reference pickers.** Avalonia has no searchable combo; the spike composes a search box + filtered combo. Step
   24 should keep that as one small reusable control rather than repeating two controls per reference field.

## 11. Known limitations of the spike

- Captures are a single headless Linux run; no cross-OS pixel comparison is attempted and no baseline is added.
- The option-3 document strip is emulated; the exact gesture conflict with the real shell tab strip is not
  measured.
- Option 5 delegates row keyboard navigation and virtualization to an inner `ListBox`; a fully self-owned
  selection model was out of scope for the spike but is part of the maintenance estimate.
- Bulk delete is exercised on referenced particles (refusal) only; deleting unreferenced objects is not
  demonstrated on real data because ceria has none.
- Column sort is ordinal-ignore-case over formatted cells; no numeric/unit-aware sorting is implemented.
- Undo/redo is exercised through the editor buttons and the coordinator. Global `Ctrl+Z`/`Ctrl+Y` shortcuts are
  shell command territory (Step 23B) and were not claimed by the spike; the headless harness did not deliver
  modified letter keys to the spike's stage handler (plain Enter/Escape did), so no shortcut evidence is claimed.

## 12. Evidence map

| Claim | Test |
| --- | --- |
| Real data: 3 particles incl. a vacancy, 3 sets, 3 sites, SG 225, 1 dummy, no base transitions | `SpikeDataTests.TheCeriaSampleProvidesTheSpecifiedParticlesSetsSitesAndStructure` |
| Transition samples and pattern breadth | `SpikeDataTests.TheTransitionSamplesProvideTheKmcMmcAndPatternBreadthRows` |
| 1,024-row generated breadth | `SpikeDataTests.TheGeneratedBreadthValueHasMoreThanOneThousandSitesAndPaddedParticles` |
| Void row pinned/read-only; transition table merges samples | `SpikeDataTests.TheParticleTablePinsTheImplicitVoidRowAndTheTransitionTableMergesSamples` |
| Selection, arrows/Home/End/Tab/Enter/Escape, multi-select, virtualization, alignment, search/filter/sort, splitter, nested tabs | `SpikeOptionBehaviourTests` (5 options + multi-select + header click + splitter + tabs) |
| Editor per domain, validator rejection, local parse error, pickers, undo, read-only rows, matrix | `SpikeEditorTests` (5 tests) |
| Automation names, roles, glyph + text status | `SpikeAccessibilityTests` (5 options) |
| Capture matrix: 3 scenarios × 3 themes per option, plus breadth and narrow | `SpikeCaptureTests` (5 options, 55 PNGs) |

Reproduce (from the repository root):

```bash
# build and the spike evidence
./tools/verify.sh dotnet
# or only the spike
dotnet test src/dotnet/Mocassin.App.Tests/Mocassin.App.Tests.csproj -c Release \
  --filter "FullyQualifiedName~LayoutSpike"

# the capture matrix lands here (gitignored)
MOCASSIN_LAYOUT_POC_OUTPUT=/tmp/step-24-poc \
  dotnet test src/dotnet/Mocassin.App.Tests/Mocassin.App.Tests.csproj -c Release \
  --filter "FullyQualifiedName~SpikeCaptureTests"

# structure and docs
./tools/verify.sh structure
./tools/verify.sh docs
```

## 13. Disposition and next gate

The spike code is discarded after the owner records the pick in ADR 0003: delete
`src/dotnet/Mocassin.App.Tests/LayoutSpike/`, keep this record and the PNG subset, and delete the branch locally
and remotely. The implementation does not seed from spike code; it re-derives from the frozen design. The branch
must not be merged until the owner's `--no-ff` approval, and it must not be merged with the spike code if the
owner has already recorded the pick.
