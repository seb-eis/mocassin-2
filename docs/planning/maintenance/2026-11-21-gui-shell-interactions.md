# GUI shell interaction fix (2026-11-21)

An out-of-step maintenance change under `AGENTS.md` §3.5. It is not a new step and has no step branch or step
tag. The owner recorded the decisions on 2026-11-21 and declined an independent design review; the contract is
developer guidance for this fix. Merge approval follows §3.5 after the local gates and the two manual platform
runs.

| Field | Value |
| --- | --- |
| Type | Maintenance fix (three owner-reported GUI defects) |
| Branch | `fix/gui-shell-interactions` |
| Base | `main` at `542d569` |
| Worktree | `/workspace` (clean, in place) |
| Owner decisions | Recorded 2026-11-21 — see §11 |
| Tracking | One branch, one implementor, both manual platform runs, then one owner `--no-ff` merge approval under §3.5 |
| Status | Ready for implementation — no independent design review |
| Normative behaviour | [`docs/development/ui-interaction-contract.md`](../../development/ui-interaction-contract.md) |

The interaction contract is the durable design. This record holds the defect evidence, the implementation
scope, the test plan, the acceptance criteria, and the latent-issue triage. Do not restate the contract here;
when the two disagree, the contract page wins and this record is corrected.

## 1. Owner-reported defects

1. **Splitters make empty space instead of rebalancing panels.**
2. **Overlay popups do not receive focus when they open, so `Escape` does not close them.**
3. **Most controls do not confirm on `Enter` and require a mouse click.**

## 2. Verified root causes

### 2.1 Splitters

`Shell/ShellView.axaml` builds the workspace as `ColumnDefinitions="Auto,Auto,*,Auto,Auto"` (line 20) with the
side panels in `Auto` tracks at a fixed bound `Width` (lines 22 and 45) and the console in an `Auto` row at a
fixed bound `Height` (line 36). The three `GridSplitter`s sit in the `Auto` tracks (lines 24, 31, and 40).

- `GridSplitter` only rebalances two **star** tracks. With a pixel/Auto neighbour it changes that one fixed
  size, and the star centre absorbs everything else. The result is fixed side panels in a growing window, not a
  ratio change.
- `Width`/`Height` bindings default to one-way. A drag changes the local value but does not write back to
  `ShellRegionsViewModel`; the next layout or settings apply snaps the panel back. Persistence therefore records
  the pre-drag size.
- `GridSplitter` converts every star track to a pixel-valued star during a drag. Even a correct star layout
  would stop scaling after the first drag unless the fraction is recomputed at `DragCompleted`.
- `ShellViewModel` persists the pixel properties (`LeftWidth`, `RightWidth`, `ConsoleHeight`) at
  `ShellViewModel.cs:555-558`, `579-581`, and `602-614`, so the persisted model is pixel-based and
  window-size dependent.
- `Shell/ProjectsRegionView.axaml:12` defines `Auto,Auto,*,Auto,*` rows, puts the "Explorer" label in a star
  row and the `TreeView` in an `Auto` row (line 43), and the "Open selected" button in the last star row
  (line 58). The leftover height is split between two empty star rows; this is the same visible empty-space
  symptom in the same window.

### 2.2 Overlay focus and `Escape`

`Shell/ShellWindow.axaml:18-32` hosts the palette, cheat sheet, settings, `DialogHost`, and notification host
as sibling grids whose visibility is bound to view-model flags. No layer owns a focus lifecycle.

- `Commands/CommandPaletteView.axaml.cs:20` focuses the search box from `AttachedToVisualTree`, which fires
  once when the window loads while the palette is hidden. The fallback at lines 21-27 watches the palette
  control's own `IsVisible`, which does not change when the ancestor grid (line 19) is hidden; it never fires.
- `CommandPaletteView.axaml.cs:30-57` handles `Escape`/`Enter` only from the search box's `KeyDown`. Focus in
  the results list, or focus left behind in the shell, makes `Escape` fall through to nothing.
- `Commands/ShortcutCheatSheetView.axaml:38` has a Close button with no `IsCancel`, no `IsDefault`, no initial
  focus, and no key handler.
- `Settings/SettingsDialogView.axaml` has no initial focus. Its Cancel/Save buttons carry `IsCancel`/`IsDefault`
  (lines 107-108), and Avalonia registers those on the window root, so they work regardless of focus while the
  settings layer is the only enabled overlay. Focus restoration and layer exclusivity are still missing.
- `Dialogs/DialogHost.axaml.cs:74` captures the previous focus, lines 79-92 post focus to the first focusable
  descendant, and lines 118-124 handle `Escape` on the host's tunnel route. The tunnel route only runs when
  focus is inside the host, and the first-focusable heuristic has no explicit choice. The existing restore path
  at line 110 is correct and must be preserved.
- `Dialogs/ProgressDialogView.axaml:13` has Cancel as its only focusable control, so the first-focusable
  heuristic focuses it and a stray `Enter` requests cancellation of the running operation.
- Shell command gestures remain active while an overlay is open. A shortcut can open a second layer in the same
  window; two visible `IsDefault`/`IsCancel` buttons in one top-level both fire.

### 2.3 `Enter`

- `CommandPaletteView.axaml.cs:37-41` executes the selected command on `Enter` even when the results are empty
  (`SelectedCommand` is null) and closes the palette anyway. The results `ListBox` (lines 23-27) has no `Enter`
  handler, so `Enter` with focus in the list does nothing.
- `ShortcutCheatSheetView.axaml:38` has no default action; `Enter` does nothing.
- Confirmation, choice, prompt, error, and settings dialogs already carry `IsDefault`/`IsCancel`; the fix is to
  make their initial focus explicit and test the mapping, not to add buttons.
- `Documents/Project/ProjectOverviewView.axaml.cs:90-108` commits all four metadata fields on `Enter`. The
  fields are single-line today, so this is correct; the Notes field owns the multiline question for a later
  editor step.
- `Shell/ProjectsRegionView.axaml.cs:21-31` and `Consoles/ProblemsView.axaml.cs:21-31` already activate on
  `Enter`. They need regression tests, not code changes.

## 3. Design summary

The normative rules are in the [interaction contract](../../development/ui-interaction-contract.md). The
implementation shape for this branch is:

- **Layout.** Star tracks with fractions in `ShellRegionsViewModel`; a pure fraction/clamp helper; 6 px
  splitters in `Auto` tracks; minimum sizes on the definitions; `DragCompleted` posts a fraction recompute
  after the final layout pass and reapplies star weights; window resize needs no handler.
- **Persistence.** `LayoutSettings` gains `leftFraction`, `rightFraction`, and `consoleFraction` (double) and
  keeps the visibility and console-channel fields. The reader migrates the pre-fix pixel fields once; the
  writer emits only fractions. This is app-local settings, not a contract format, so no ADR or manifest change.
- **Layers.** One interactive overlay at a time. The shell, notification host, and lower overlays are disabled
  while a layer is open; catalog gestures are inert. The palette and cheat sheet are exclusive; settings closes
  them; a dialog disables the layer beneath it and restores it on close.
- **Focus.** The window captures focus before opening, focuses a declared target after layout, and restores the
  captured element after re-enabling the lower layer. `DialogHost` gains an explicit
  `DialogFocus.InitialFocus` marker (`True` on a control, `Container` on the progress root), an open/closed
  notification, and the container fallback.
- **`Escape`.** One window-level bubble handler with the priority dialog → settings → palette → cheat sheet.
  Handled events and open popups keep first refusal. Non-dismissible dialogs consume `Escape` without
  fall-through.
- **`Enter`.** Rely on `IsDefault`/`IsCancel` for dialogs and settings; add the palette root handler, the cheat
  sheet Close default, and the explicit initial-focus markers. Multiline `Ctrl+Enter` is a documented tunnel
  rule for Steps 24–28; no editable multiline field exists today.
- **Projects panel.** Correct the row definitions so the tree fills and the button stays pinned.

## 4. Expected files

| Area | Files |
| --- | --- |
| Layout view | `Mocassin.App.Views/Shell/ShellView.axaml`, `ShellView.axaml.cs`, `ShellWindow.axaml`, `ShellWindow.axaml.cs` |
| Layout state | `Mocassin.App.ViewModels/Shell/ShellRegionsViewModel.cs`, `ShellViewModel.cs`, new pure `ShellLayoutFractions.cs` |
| Settings | `Mocassin.App.ViewModels/Settings/LayoutSettings.cs`, `SettingsDefaults.cs`, `AppSettingsValidator.cs`, `Settings/Json/AppSettingsJsonReader.cs` |
| Overlays | `Views/Commands/CommandPaletteView.axaml(.cs)`, `ShortcutCheatSheetView.axaml(.cs)`, `Views/Settings/SettingsDialogView.axaml(.cs)` |
| Dialogs | `Views/Dialogs/DialogHost.axaml(.cs)`, `ConfirmDialogView.axaml`, `ProgressDialogView.axaml`, and the focus markers on the other dialog views |
| Projects panel | `Views/Shell/ProjectsRegionView.axaml` |
| Configuration | `zensical.toml` nav entry for the new contract page (the planner does not edit configuration) |
| Tests | new `Views/ShellLayoutTests.cs` and `Views/ShellInteractionTests.cs`; extensions to `Views/DialogOverlayTests.cs`, `Views/WorkspaceRenderTests.cs`, `Visual/AccessibilityTests.cs`, `Settings/AppSettingsReaderTests.cs`, `Settings/AppSettingsStoreTests.cs`, `Visual/ScreenshotTests.cs` and its baselines |
| Docs | the new contract page, `ui-architecture.md`, `docs/development/index.md`, `testing-and-parity.md`, this record |

`Mocassin.App.ViewModels` types are internal, so no `PublicAPI.Unshipped.txt` entry is expected. If a helper is
made public for tests, it is a public API change and needs the usual baseline update.

## 5. Behaviour changes

| # | Defect | After the fix |
| --- | --- | --- |
| 1 | Splitter drag leaves a fixed panel and unused space | Drag rebalances the adjacent pair; the third panel keeps its share; total width is unchanged; window resize preserves the ratio; restart restores it |
| 1b | Projects tree and button sit in empty star rows | Explorer fills its row; the button is pinned at the bottom; no empty star row |
| 2 | Palette opens without focus; `Escape` does nothing | Search box focused on every open; `Escape` closes regardless of focus; focus returns to the invoking control |
| 2b | Cheat sheet and settings have no focus lifecycle | Close button / Theme field focused on open; `Escape` closes; focus returns |
| 2c | Overlapping layers can expose two default buttons | Only the top layer is enabled; catalog gestures are inert; lower layers are restored on close |
| 2d | Progress dialog's first focus is Cancel | Container focused; `Enter` cannot cancel the operation |
| 3 | Palette `Enter` closes with no results; list `Enter` does nothing | Empty result is a no-op; list `Enter` executes the selection; cheat sheet `Enter` closes |
| 3b | Single-line fields and dialogs are not uniformly mapped | Documented mapping; dialogs tested for `Enter`/`Escape`; multiline rule published for Steps 24–28 |

## 6. Tests

All new interaction and layout tests are **PR-fast** and headless, using the real `ShellWindow`, real settings
store, real project files, and the existing `HeadlessSession`. The only doubles remain dialogs, pickers,
clipboard, dispatcher, and time, as in 23A/23B.

### 6.1 Layout

- Fractions from actual sizes: left/centre/right and documents/console; hidden tracks excluded.
- Persisted-range clamp and degenerate inputs (0, negative, NaN, sum > 1).
- Headless geometry at 1200 × 800: actual panel shares match defaults within 0.005; the sum of tracks equals
  the workspace width (no gap).
- Drag the left splitter by +120 px with headless mouse input: left and centre change, right does not, the
  total is unchanged; the same for the right splitter and the console splitter.
- Resize to 1000 × 700 after a drag: the new ratio is preserved within 0.005.
- Toggle projects off: `ActualWidth` is zero, the splitter is not visible, the centre expands; toggle on: the
  fraction returns within 0.005.
- Projects panel rows: the explorer tree occupies the star row and "Open selected" is pinned at the bottom; no
  empty star row remains.
- Keyboard: splitters are focusable, named, arrow keys resize by 16 px, and a focus indicator is present.
- Persistence: drag → `PendingLayoutPersist` → reload the settings file; fractions match; a legacy document
  with pixel fields loads as a fraction and rewrites without the pixel keys; a second load is idempotent.

### 6.2 Overlays

- Palette: open → `FocusManager.GetFocusedElement()` is the search box; `Escape` with focus in the search box
  closes; re-open, Tab to the results, `Escape` closes; `Enter` with no results keeps it open; `Enter` with one
  result executes exactly that command and closes; focus returns to the previously focused control.
- Cheat sheet: open → Close focused; `Escape` and `Enter` close; focus returns.
- Settings: open → Theme focused; `Escape` closes without a settings write; `Enter` in the autosave interval
  saves and closes; focus returns.
- Dialog: confirmation → Cancel focused; `Escape` cancels; `Enter` cancels; text prompt → text box focused,
  `Enter` confirms; choice → list focused, `Enter` confirms the selected choice; progress → container focused,
  `Enter` does not cancel, `Escape` does not close.
- Layer exclusivity: while settings is open, `Ctrl+Shift+P` and `F1` do not open the palette/cheat sheet; while
  a dialog is open over settings, the settings overlay is disabled and its Save default does not fire.
- Accessibility: overlay roots and splitters have accessible names; the dialog host exposes the active title;
  the deterministic Tab order test still passes and stays inside the open layer.

### 6.3 Regression and visual

- The existing `DialogOverlayTests.FocusIsRestoredAfterADialog` and `EscapeCancelsTheDialog` stay green.
- Explorer `Enter`, Problems `Enter`, and ProjectOverview field `Enter` get explicit regression tests.
- The three screenshot baselines are regenerated once because the default panel sizes change at the 1000 × 700
  capture size. No other visual change is intended.
- The manual Windows and macOS runs execute `Mocassin.App.Tests` through the focused platform job, which covers
  the headless shell, layout, focus, and interaction tests. Their run IDs and conclusions are recorded in §11.

## 7. Acceptance criteria

1. A splitter drag changes only the adjacent ratio, leaves no unused space, survives a window resize, and
   survives a restart; hidden panels restore their share.
2. Every overlay opens with a defined focus, closes on `Escape` regardless of where focus sits, and restores
   focus on close.
3. `Enter` follows the mapping table for dialogs, pickers, palette, lists/trees, single-line fields, and search;
   multiline fields are documented as newline plus `Ctrl+Enter` and no current surface violates it.
4. No two layers are interactive at once; a lower layer's default/cancel button cannot fire.
5. Splitter and overlay accessible names, focus indicators, and the deterministic Tab order pass the headless
   accessibility tests.
6. `./tools/verify.sh dotnet`, `./tools/verify.sh docs`, and `./tools/verify.sh policy` pass locally. No
   per-feature CI is dispatched; the only CI runs on this branch are the two manual platform runs in item 9.
7. The screenshot baselines are updated and the diff is recorded in the commit message.
8. No new dependency, theme restyle, `contracts/manifest.json` change, or frozen-record edit.
9. The manual **Windows** and **macOS** platform runs are dispatched on this fix branch, one at a time, and both
   pass. Both run IDs, `head_sha` values, and conclusions are recorded in §11. A failing or missing platform
   run blocks the merge approval.

## 8. Risks

| Risk | Mitigation |
| --- | --- |
| Persisted settings shape changes | App-local additive fields; one-time migration from pixels; validator clamps; reader/store tests; no manifest or ADR because it is not a cross-language format |
| `GridSplitter` pixel-star rewrite causes a jump on release | Recompute once at `DragCompleted`; assert total width and ratio in the headless drag test |
| Disabling the shell on overlay open changes focus/undo behaviour | The focused editor already commits on focus loss; capture focus before disabling and restore after re-enabling; test the metadata commit path |
| `Escape` router steals `Escape` from a combo box or flyout | Bubble phase plus `e.Handled` check; open popups are separate top-levels; add a settings combo box test if headless popups are reliable, otherwise record the manual check |
| Focus restore to a detached element (document closed while an overlay is open) | Null/attachment/enabled guard; fall back to the document tabs, then the menu |
| Two overlays in one top-level fire both defaults | Layer disabling plus the catalog-gesture gate; explicit test with settings + dialog |
| Screenshot churn from the default fraction change | One regeneration; PR-medium; document the change in the commit |
| A required platform run is missing or red | The merge is blocked. Dispatch the missing run with `platforms=windows` or `platforms=macos` and `force_all=true`, record both run IDs, and re-run the local gate after any fix |
| No human visual pass on Windows/macOS | The platform runs execute the headless shell, layout, and interaction tests; screenshot comparison stays Ubuntu-only. A manual keyboard spot-check is optional evidence, not a substitute for the required runs |

## 9. Out of scope

- Restyling, theme resources, density, font scale, motion, or chart work.
- New dependencies, `Directory.Packages.props` changes, or a new test framework.
- Step 24 implementation or any later editor.
- Changes to `contracts/`, `docs/planning/02-data-contracts.md`, or public library APIs.
- Frozen Step 23A/23B pages, reviews, audits, closeout, parity, or status records.
- Notification z-order, live-region announcements, progress/crash `Escape` policy, a dirty-settings prompt,
  multiline Notes, responsive overlay widths, and the "Open projects" list activation. These are classified in
  the appendix.

## 10. Similar latent issues

The owner asked whether the XAML contains other instances of the same three defect classes. The table records
every candidate found in `src/dotnet/Mocassin.App.Views` and its classification. "In scope" means the same
owner-reported class and cheap enough for this branch; "deferred" is a different class, a larger change, or a
product decision.

### 10.1 Sizing that can absorb space

| ID | Evidence | Verdict | Reason |
| --- | --- | --- | --- |
| L-1 | `Shell/ShellView.axaml:20-47` — `Auto,Auto,*,Auto,Auto`, fixed `Width`/`Height` on three panels, splitters in `Auto` tracks | **In scope** | The reported splitter defect |
| L-2 | `Shell/ShellView.axaml:29-38` — console row splitter between a `*` documents row and an `Auto` console row with a fixed bound `Height` | **In scope** | Same class as L-1; shares the fraction model |
| L-3 | `Shell/ProjectsRegionView.axaml:12,43,58` — two empty star rows, tree in an `Auto` row | **In scope** | Same empty-space symptom; one row-definition change, no new UI decision |
| L-4 | `Documents/Project/ProjectOverviewView.axaml:52` — fixed 180 px label column next to `*` | Not a defect | A form label column, not a draggable region; it shrinks the editor but creates no dead space |
| L-5 | `Settings/SettingsDialogView.axaml:21` — fixed 200 px label column next to `*` | Not a defect | Same as L-4; the dialog has a fixed width by design |
| L-6 | `Documents/Project/ProjectOverviewView.axaml:14` — fixed `Width="420"` name field inside a `ScrollViewer` | Deferred | Different class (fixed control in a scrolling form); the window minimum keeps it usable; revisit with the Step 24 layout |
| L-7 | `Commands/CommandPaletteView.axaml:12`, `SettingsDialogView.axaml:13`, `ShortcutCheatSheetView.axaml:11` — fixed overlay widths | Deferred | No splitter and no dead space; responsive overlay sizing is a separate product decision |

### 10.2 Overlays without a focus or `Escape` lifecycle

| ID | Evidence | Verdict | Reason |
| --- | --- | --- | --- |
| L-8 | `Shell/ShellWindow.axaml:18-32` — four overlays with no owner for focus or keys | **In scope** | The reported focus defect |
| L-9 | `Commands/CommandPaletteView.axaml.cs:20-27` — focus only at attach / own `IsVisible` | **In scope** | The direct cause of the palette never receiving focus |
| L-10 | `Commands/CommandPaletteView.axaml.cs:30-57` — `Escape` only from the search box | **In scope** | `Escape` must work from anywhere in the layer |
| L-11 | `Commands/ShortcutCheatSheetView.axaml:38` — no initial focus, no `IsCancel` | **In scope** | Same class; one attribute plus a focus call |
| L-12 | `Settings/SettingsDialogView.axaml` — no initial focus or restore; `IsCancel`/`IsDefault` at 107-108 | **In scope** | Same class; the buttons already exist |
| L-13 | `Dialogs/DialogHost.axaml.cs:74-92,118-124` — first-focusable heuristic; host-only tunnel `Escape` | **In scope** | Make the choice explicit and route `Escape` at the window |
| L-14 | `Dialogs/ProgressDialogView.axaml:13` — first focus lands on Cancel | **In scope** | Focus only; the operation-cancel semantics are unchanged |
| L-15 | `Dialogs/CrashDialogView.axaml:26` — Exit default, no cancel, no `Escape` | Deferred | Terminal dialog; a cancel route is a product decision |
| L-16 | `Shell/ShellWindow.axaml:29-32` — notifications sit above `DialogHost`, are focusable, and have no live region | Deferred | Different class (z-order and live-region policy); notifications are now disabled while a layer is open, which removes the input hazard |
| L-17 | Shell catalog gestures remain active while an overlay is open | **In scope** | Without a gate, a shortcut can create a second layer and double-fire defaults; it is part of the layer contract |

### 10.3 Interactive surfaces without an `Enter`/`Escape` mapping

| ID | Evidence | Verdict | Reason |
| --- | --- | --- | --- |
| L-18 | `Commands/CommandPaletteView.axaml.cs:37-41` — `Enter` closes with no results | **In scope** | The reported `Enter` defect |
| L-19 | `Commands/CommandPaletteView.axaml:23-27` — results list has no `Enter` | **In scope** | Same class; the root handler covers both focus locations |
| L-20 | `Commands/ShortcutCheatSheetView.axaml:38` — no default/cancel | **In scope** | `Enter`/`Escape` must close the informational overlay |
| L-21 | `Dialogs/ProgressDialogView.axaml:13` — Cancel is not `IsDefault`/`IsCancel` | Not a defect (focus only) | Cancellation is an explicit operation action, not a dialog dismissal; keep it button-only |
| L-22 | `Dialogs/CrashDialogView.axaml:26` — Exit default, no cancel | Deferred | Terminal dialog; see L-15 |
| L-23 | `Shell/ProjectsRegionView.axaml.cs:21-31` — explorer `Enter`/`Space` | Compliant | Add a regression test |
| L-24 | `Consoles/ProblemsView.axaml.cs:21-31` — Problems `Enter` navigates | Compliant | Add a regression test |
| L-25 | `Shell/ProjectsRegionView.axaml:13-30` — "Open projects" list has no `Enter` | Deferred | Selection already activates the project; a distinct "open last document" action needs a product decision |
| L-26 | `Consoles/ApplicationConsoleView.axaml:11-15`, `Consoles/ProblemsView.axaml:29,39` — live filter fields | Not a defect | The filter is applied on change; there is no pending commit for `Enter` to perform |
| L-27 | `Documents/Project/ProjectOverviewView.axaml:19,61,71,81` — single-line fields commit on `Enter` | Compliant | Correct for single-line; the multiline rule applies if Notes becomes multiline |
| L-28 | `Documents/Project/ProjectJsonView.axaml:12-19` — read-only multiline | Compliant | `Enter` changes no state |
| L-29 | `Shell/DocumentsRegionView.axaml:9-12` — tabs switch on selection; `Enter` does nothing | Deferred | There is no activation action distinct from selection; revisit if tab previews or pinning are added |
| L-30 | `Dialogs/ChoiceDialogView.axaml:6-19` — list plus default Continue | Compliant | The selected choice is the activation target; test it |
| L-31 | `Dialogs/TextPromptDialogView.axaml:9-12` — text box plus default OK | Compliant | The focus marker makes the text box the explicit target; test it |
| L-32 | `Settings/SettingsDialogView.axaml:23-29,37` — ComboBoxes | Not a defect | `Enter` on a closed combo box opens the list; on an open one it commits the item. The contract states the exception |

### 10.4 Split assessment

The in-scope list is one coherent change: the three reported defects plus the layer gate and the projects row
that share the same root cause. The expected diff is one view pair, one view-model pair, the settings layout
model, four overlay views, two dialog views, tests, and docs. It is still readable in one sitting; no split is
recommended. The owner confirmed on 2026-11-21 that L-3 (projects rows) and L-17 (gesture gate) are included in
this branch; they are no longer optional.

## 11. Owner decisions (2026-11-21)

The owner resolved every open question. The contract is developer guidance for this §3.5 fix; no independent
design review is required.

| ID | Decision | Consequence |
| --- | --- | --- |
| Q1 | Keep Cancel initial focus. Plain `Enter` cancels a confirmation dialog; Tab + `Enter` confirms. | Matches the contract. Destructive confirmations stay safe by default and remain keyboard-only. |
| Q2 | Accept both safe defaults: settings `Escape` silently discards unapplied edits; the progress dialog focuses its container, `Enter` cannot cancel, `Escape` does not close, and cancellation stays the explicit button. | No dirty-settings prompt and no keyboard cancellation of a running operation in this fix. |
| Q3 | Include L-3 (projects rows) and L-17 (gesture gate) in this branch. | Both are required scope; see §10.1 and §10.2. |
| Q4 | No independent design review. The contract is developer guidance for this §3.5 fix. | No review record is created and no reviewer verdict is pending. Harness limitation recorded: the reviewer binding permits only `step-*`/`xc-*` record paths, so a `fix/` branch cannot host a reviewer record under the current binding. |
| Q5 | Legacy pixel layout conversion uses the 1200 × 800 reference and accepts the one-time shift; do not reset old layouts to defaults. | Migration is exactly the contract's §6.4. |
| Q6 | Dispatch the manual **Windows** and **macOS** platform runs on this fix branch and record both run IDs. A failing or missing platform run blocks the merge approval. | Added to §7 item 9 and §12. The owner approves the merge only after both runs are green, under the normal §3.5 approval. |

Owner numbering is the 2026-11-21 message. In the earlier draft, the settings-`Escape` and progress-focus
questions were separate (draft Q2/Q3) and the include-L-3/L-17 question was draft Q6; they are consolidated
above as Q2 and Q3. The owner's new Q6 is the platform-run gate.

### Platform run record

Dispatch `CI` → `Run workflow` on `fix/gui-shell-interactions`, one platform at a time (the workflow's
concurrency group cancels a second run for the same ref). Use `platforms=windows` and `platforms=macos` with
`force_all=true` so the focused .NET job cannot be path-filtered away. Windows and macOS do not pixel-compare
screenshots; they run `Mocassin.App.Tests` and the other platform-sensitive test projects.

| Platform | Run ID | `head_sha` | Conclusion | Recorded by |
| --- | --- | --- | --- | --- |
| Windows | _pending_ | _pending_ | _pending_ | implementor |
| macOS | _pending_ | _pending_ | _pending_ | implementor |

The merge approval request must quote both rows. `_pending_` is not a passing state.

## 12. Done when

- The three defects are fixed and the acceptance criteria in §7 pass.
- The contract page is linked from `ui-architecture.md` and the development index, and the nav entry is added by
  the implementor (the planner does not edit configuration).
- The local gates named in §7.6 pass and the branch is pushed for owner review.
- Both manual platform runs in §7 item 9 are green and both rows in §11's platform run record contain the run
  ID, `head_sha`, and conclusion. No design review is pending; Q4 records that none is required.
- The owner approves the merge only after those runs, under the normal §3.5 approval; the merge is `--no-ff`.
- This record is updated with the commit, the exact commands/results, the platform run evidence, and any
  deviation.
