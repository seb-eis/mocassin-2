# GUI visual refresh (2026-11-21)

An out-of-step maintenance change under `AGENTS.md` §3.5. It is not a new step, has no step branch, no step tag,
and no amendment. The owner recorded the direction and the fixed dark palette on 2026-11-21; this record is the
design and test plan for one `fix/gui-visual-refresh` branch and one owner `--no-ff` merge approval.

| Field | Value |
| --- | --- |
| Type | Maintenance fix / bounded visual refresh |
| Branch | `fix/gui-visual-refresh` |
| Base | `main` at `89dbe2222ddd71fcf24099bf08cecf44f8a6077d` |
| Worktree | `/workspace` (clean, in place) |
| Normative visual contract | [`docs/development/ui-visual-contract.md`](../../development/ui-visual-contract.md) |
| Interaction behaviour | [`docs/development/ui-interaction-contract.md`](../../development/ui-interaction-contract.md) |
| Status | Design accepted 2026-11-21; ready for implementation |
| Tracking | One branch; internal commit sequence in §4; local gates; one owner `--no-ff` merge approval |

This record holds the defect evidence, scope, migration algorithm, test plan, acceptance criteria, risks, and
owner decisions. It does not restate the palette or surface rules; the visual contract is the single home for
those. If the two disagree, the visual contract wins and this record is corrected.

## 1. Owner-reported defects and direction

1. Some welcome-page button labels are not centred: the recent-project `Open`/`Remove` buttons use
   `Padding="8,2"` under the global button `MinHeight`; the main New/Open/Shortcuts buttons may also be
   affected.
2. The console is too short and the inspector too wide in the default view.
3. The console should use tabs for the application console and validation instead of the channel combo box.
4. Each separated part should look like a box with rounded edges.
5. The dark scheme is too dark; use a dark gray.
6. General visual direction: the structure of Visual Studio Code.

Fixed owner inputs (2026-11-21):

- Dark surfaces: chrome `#1e1e1e`, workspace `#252526`, cards/panels `#2d2d30`, borders `#3c3c3c`, accent
  `#007acc`. Treat this as the reference, not "whatever VS Code currently ships".
- Shape: subtle 6 px rounded cards, 1 px border, small gaps, VS Code density.
- Scope: shell, menu/status bar, the three regions, console tabs, welcome, command palette, settings, dialogs,
  notifications.
- Console tabs: two fixed tabs `Console` and `Validation` in that order; the Problems content becomes
  Validation; active tab persists; no close or reorder; the internal id may stay `problems`.
- Light theme: restyle in parallel to a VS Code Light+ equivalent with the same cards, tabs, and spacing.
- Layout defaults: propose exact left/right/console fractions (roughly 20 %, 22 %, 32 % height) and a one-time
  migration that is idempotent and does not fight the pixel-to-fraction migration.
- Process: one branch, one owner `--no-ff` merge; no independent design review unless this design concludes it
  is needed.

## 2. Verified current state and root causes

### 2.1 Welcome button labels

- `Documents/WelcomeDocumentView.axaml:36-37` sets local `Padding="8,2"` on the recent-project `Open` and
  `Remove` buttons.
- `Styles/ShellStyles.axaml:12-16` sets a global `Button` `Padding` from `Mocassin.ControlPadding` and
  `MinHeight` from `Mocassin.ControlMinHeight` (34 comfortable / 26 compact). The local padding therefore
  changes only one of the three metrics that determine the label's position: padding, minimum height, and
  content alignment.
- The global style does not declare `HorizontalContentAlignment` or `VerticalContentAlignment`. Fluent's
  template binds both from the control, so the effective alignment is whatever the control/theme currently
  provides. The main New/Open/Shortcuts buttons have no local padding at all and can show a different optical
  centre from the recent buttons.
- Direction: one compact/secondary button style owns padding, minimum height, and both content alignments.
  Remove the local `Padding` overrides. The main buttons use the default style with the same explicit
  alignment. §6.5 of this record's test plan measures the rendered label centre against the button centre, so
  the diagnosis is confirmed by a test rather than by a screenshot alone.

### 2.2 Console length and inspector width

- `Settings/SettingsDefaults.cs:58-68` derives the defaults from the pre-fix pixel layout:
  `260/1200 = 0.2167` left, `320/1200 = 0.2667` right, `180/800 = 0.225` console.
- `Shell/ShellView.axaml` already uses star tracks and the fraction model, so only the default values and the
  persisted-layout migration need to change. The splitter behaviour is not reopened.
- At 1200 × 800 the new defaults give a nominal 240 px left, 264 px right, 684 px centre (after two 6 px gaps),
  544 px documents, and 256 px console. At the 900 × 600 minimum they give a nominal 180 left, 198 right, 510
  centre, 408 documents, 192 console; every region stays above its existing minimum. The nominal pixels are
  shares of the workspace extent before the outer `RegionGap` inset.

### 2.3 Console channel selector

- `Consoles/ConsoleView.axaml:11-26` is a `ComboBox` over `Channels` plus a `ContentControl`. The two channels
  are created by `ApplicationConsoleChannelProvider` and `ProblemsChannelProvider`; `ConsoleViewModel` inserts
  the application channel first, so the order is already `application`, `problems`.
- `Shell/ConsoleRegionView.axaml:11-18` adds a separate `Console` title row above the combo box, so the region
  has two headers.
- `Shell/ShellViewModel.Layout.cs:55` captures `Console.ActiveChannel?.Id` into `activeConsoleChannel`, but
  `ShellViewModel.cs:150` subscribes only to `Regions.PropertyChanged`. Switching the combo does not schedule a
  layout persist; the active channel is written only on the next splitter gesture or settings save. "Active tab
  persists" therefore needs a new subscription to the console's `ActiveChannel` property.
- Direction: remove the combo box and the extra title row; use a `TabControl` over the same channels with
  presentation-only tab titles. The internal ids and the persisted key remain `application` and `problems`.

### 2.4 Theme and colour

- `App.axaml:9-18` owns a small set of `Mocassin.*` resources. `Mocassin.ShellBackground` is transparent and
  `Mocassin.BorderBrush` is a low-alpha black, so the stock Fluent theme supplies almost every surface colour.
- Only `Themes/HighContrast.axaml` and `Themes/Density.axaml` are custom dictionaries. There is no light/dark
  palette, no card radius, no state palette, and no status-bar surface token.
- `ShellStyles.axaml` applies a global `Border` border brush/thickness and a few control minimums. That is the
  only shared visual style.
- Literal colours that bypass the theme and will be replaced or routed through tokens:
  `StatusBarSeverityToBrushConverter` (`#D32F2F`, `#F57C00`, `#2E7D32`, `#1565C0`, `#616161`),
  `WelcomeDocumentView` missing marker (`#C62828`), `SettingsDialogView` error/status banners
  (`#FDECEA`, `#C62828`, `#B71C1C`, `#E8F5E9`, `#2E7D32`), `ProjectOverviewView` error text/border
  (`#D32F2F`, `#33D32F2F`), and the read-only banner (`#FFF3CD`, `#E0A800`).
- Direction: a palette dictionary with light/dark theme dictionaries, a `Default` fallback that mirrors light,
  and explicit high-contrast overrides for every role and every overridden Fluent key. `App.axaml` keeps only
  theme-independent dimensions and typography.

## 3. Design summary

The normative values and surface rules are in the [visual contract](../../development/ui-visual-contract.md).
The implementation shape is:

### 3.1 Token strategy

| Layer | What it owns | Why |
| --- | --- | --- |
| `App.axaml` base tokens | font sizes, radius, padding, gap, transitions as startup fallbacks | values are needed before the first settings apply and are not theme-specific |
| `Themes/Palette.axaml` | `Mocassin.*` colour roles and the Fluent 12.1.6 keys in contract §3.4, in `Light`/`Dark`/`Default` theme dictionaries | one role vocabulary for explicit surfaces, and stock Fluent controls follow it without new templates |
| `Themes/HighContrast.axaml` | every role and every overridden Fluent key, black/yellow/white, border 2 | the existing high-contrast selection must keep winning over the palette |
| `Themes/Density.axaml` | compact spacing and heights | the existing density selection must keep winning over the base tokens |
| `AvaloniaThemeApplier` | theme variant, font scale, accent, runtime comfortable/compact values, border thickness, motion | user settings are runtime state, not theme dictionaries |
| `Styles/ShellStyles.axaml` | control padding, min-height, corner radius, content alignment, and state setters from tokens | first-party consistency without replacing Fluent control templates |

Why not one giant `App.axaml` resource set: theme variants must switch at runtime, and `App.axaml` direct
resources outrank theme dictionaries. Why not per-view colours: they cannot follow light/dark/high contrast and
would make the screenshot matrix meaningless. Why not custom control templates: the owner deferred them, and
Fluent resource overrides plus property setters cover the primary surfaces.

The implementation must not leave a colour literal in `App.axaml`; a direct colour there would silently defeat
the palette. `HighContrast.axaml` must list every role and every key the palette overrides, not a subset, or a
palette colour will leak into high contrast.

### 3.2 Default layout and one-time migration

New defaults:

| Value | New default | Nominal at 1200 × 800 | Nominal at 900 × 600 | Persisted range |
| --- | --- | --- | --- | --- |
| `LeftFraction` | `0.20` | 240 px | 180 px | 0.08–0.45 |
| `RightFraction` | `0.22` | 264 px | 198 px | 0.08–0.45 |
| centre | remainder | 684 px after gaps | 510 px after gaps | existing minima |
| `ConsoleFraction` | `0.32` | 256 px | 192 px | 0.08–0.60 |
| documents | remainder | 544 px | 408 px | existing minimum |

The existing minima (160 / 160 / 360 / 80 / 200 and the 900 × 600 window minimum) are unchanged. The splitter
rules, fraction capture, clamp, and 6 px tracks are unchanged.

Migration is one additional app-local layout field, `defaultsRevision`:

1. Read the layout as today. If no fraction key exists but the pre-fix `leftWidth`/`rightWidth`/`consoleHeight`
   fields do, convert them with the 1200 × 800 reference first. This preserves the previous migration exactly.
2. Read `defaultsRevision`; an absent field is `0`. Non-integer, negative, or otherwise invalid values produce
   the normal settings warning and fall back to `0`.
3. If the revision is below `1` and **all three** post-read fractions are equal to the old defaults within
   `1e-6` (`260/1200`, `320/1200`, `180/800`), replace all three with the new defaults. This is the
   never-dragged case.
4. If any fraction differs from the old defaults, keep the user's fractions. A user who dragged a panel keeps
   that intent. The migration therefore upgrades the never-dragged saved layouts to the new proportions and
   preserves a changed layout; this is the only way to satisfy both "existing saved layouts show the new
   proportions" and "do not discard user intent". The reset command is the explicit way to return to the new
   defaults.
5. Return `defaultsRevision = 1`. A later load sees the marker and never auto-adjusts again, even if the values
   happen to equal the old defaults. The writer emits `defaultsRevision: 1` with the fractions and never emits
   the pixel fields.
6. The reader/writer path stays idempotent: one load of an old document produces the new values once, and every
   subsequent load returns the same values.

`LayoutSettings.DefaultsRevision` defaults to the current revision (`1`) for programmatic/new documents, while
the reader's absent-field default is `0`. That asymmetry is deliberate: a new document starts at the current
defaults, an old document is detected by the missing marker.

No schema bump: the field is additive, application-local, and not in `contracts/manifest.json`, matching the
existing `autosave` and `session` blocks.

The user can always re-drag or use the new **View → Reset layout** command. Reset sets the three fractions to
the new defaults, makes all regions visible, writes the current revision, and persists. It does not reset theme,
density, recent files, or the active console tab. Reset is explicit; the migration never overrides a custom
layout silently.

### 3.3 Console tabs

- `ConsoleView` becomes a `TabControl` bound to `Console.Channels` and `Console.ActiveChannel`, with an item
  template over a presentation-only `TabTitle`. The content template keeps the existing
  `ApplicationConsoleView`/`ProblemsView` data templates.
- `ConsoleChannelViewModel` gains the presentation-only tab title. The application channel uses `Console`; the
  problems channel uses `Validation`. `Id` and content headings remain `application`/`problems` so the persisted
  setting, type names, and navigation are unchanged.
- `ConsoleRegionView` removes the separate title row; the tab strip is the card header.
- `ShellViewModel` subscribes to `Console.PropertyChanged` for `ActiveChannel` and schedules the existing
  debounced layout persist. This closes the current persistence gap.
- `ApplySettings` still selects by `ActiveChannel.Id` and falls back to the first channel for an unknown id.
- User-facing automation names in the validation content become `Validation …` (for example
  `Validation text filter`, `Validation entries`), while internal type names stay `Problems*`. This avoids a
  mixed announcement.
- Keyboard behaviour follows the interaction contract: arrows change tab selection, click selects, `Tab` leaves
  the strip, plain `Enter` is a no-op. No close/reorder controls are added.
- The old combo box focus stop and the `Console channel` automation name disappear; no overlay captures or
  restores that control.

### 3.4 Welcome buttons

- Add a compact/secondary button style that owns `Padding` (`CompactButtonPadding`), `MinHeight`
  (`CompactButtonMinHeight`), `HorizontalContentAlignment=Center`, and `VerticalContentAlignment=Center`.
- Remove the local `Padding="8,2"` from the recent Open/Remove buttons and give them that style.
- Give the default button style the same explicit content alignment. The main New/Open/Shortcuts buttons then
  share the same centring rule.
- Keep the existing recent-row grid and ellipsis; only the button metrics and colours change.

### 3.5 Surface application

- Shell backdrop and gaps: `ChromeBackground`; the region cards use `WorkspaceBackground`, radius 6, 1 px
  border, and the splitter track as the gap.
- Menu bar: `MenuBackground`; status bar: `StatusBarBackground`/`StatusBarForeground`; document and console
  tab strips: the tab tokens.
- Palette, settings, dialogs, notifications, and nested cards: `CardBackground`, radius 6, 1 px border,
  `CardPadding`, scrim `OverlayScrim`.
- Hard-coded banners and error text are routed through the semantic background/border/foreground tokens.
- Custom scrollbar, menu, tooltip, and context-menu templates remain deferred; stock Fluent templates follow
  the overridden resource keys.

## 4. Expected files and commit sequence

`Mocassin.App.ViewModels` types are internal, so no `PublicAPI.Unshipped.txt` change is expected. If a helper is
made public for tests, it is a public API change and needs the usual baseline update.

| Area | Expected files |
| --- | --- |
| Palette and styles | `Mocassin.App/App.axaml`; new `Mocassin.App.Views/Themes/Palette.axaml`; `Themes/HighContrast.axaml`; `Themes/Density.axaml`; `Styles/ShellStyles.axaml`; `Theming/AvaloniaThemeApplier.cs` |
| Shell chrome | `Views/Shell/ShellWindow.axaml`; `ShellView.axaml`; `ProjectsRegionView.axaml`; `DocumentsRegionView.axaml`; `InspectorRegionView.axaml`; `StatusBarView.axaml`; `StatusBarSeverityToBrushConverter.cs`; `ConsoleRegionView.axaml` |
| Console tabs | `Views/Consoles/ConsoleView.axaml`; `Consoles/ApplicationConsoleView.axaml`; `Consoles/ProblemsView.axaml`; `Consoles/ConsoleChannelViewModel.cs`; `Consoles/ConsoleService.cs`; `Diagnostics/ProblemsChannelViewModel.cs`; `Shell/ShellViewModel.cs`; `Shell/ShellViewModel.Layout.cs` |
| Welcome | `Views/Documents/WelcomeDocumentView.axaml`; compact button style in `Styles/ShellStyles.axaml` |
| Overlays | `Views/Commands/CommandPaletteView.axaml`; `ShortcutCheatSheetView.axaml`; `Views/Settings/SettingsDialogView.axaml`; `Views/Dialogs/DialogHost.axaml` and dialog views; `Views/Notifications/NotificationHostView.axaml`; `Views/Documents/Project/ProjectOverviewView.axaml` (error/read-only banners) |
| Layout state | `Settings/SettingsDefaults.cs`; `Settings/LayoutSettings.cs`; `Settings/AppSettingsValidator.cs`; `Settings/Json/AppSettingsJsonReader.cs`; `Settings/AccentPalette.cs`; `Shell/ShellRegionsViewModel.cs`; `Shell/ShellViewModel.cs` |
| Tests | `Settings/AppSettingsReaderTests.cs`; `Settings/AppSettingsStoreTests.cs`; `Settings/ThemeApplierTests.cs`; new `Visual/ThemeContrastTests.cs`; `Views/ShellLayoutTests.cs`; new or extended `Views/ConsoleTabsTests.cs`; `Views/ShellInteractionTests.cs`; `Views/ValidationSurfaceRenderTests.cs`; `Views/WorkspaceRenderTests.cs`; `Visual/AccessibilityTests.cs`; `Visual/ScreenshotTests.cs` and baselines |
| Docs | new visual contract; `ui-architecture.md`; `development/index.md`; `user-guide/workspace.md`; `user-guide/settings.md`; `reference/file-layout.md`; `getting-started/quickstart.md`; `getting-started/first-project.md`; `development/testing-and-parity.md`; this record |
| Configuration | `zensical.toml` nav entry for the new contract page — the implementor adds it; the planner does not edit configuration |

Recommended internal commit sequence on the one branch:

1. **Tokens and theme applier** — palette, high-contrast/density updates, styles, applier, contrast/theme tests.
2. **Layout and console tabs** — defaults, `defaultsRevision` migration, reset command, console tab VM/view,
   persistence, layout/console tests.
3. **Surfaces** — shell, regions, menu/status, welcome, palette/settings/dialogs/notifications; render,
   accessibility, and centring tests.
4. **Baselines and docs** — regenerate/add screenshots, update the user/reference docs, update this record with
   evidence.

This is one logical change (one visual system) and remains reviewable as four commits. A split into two branches
would leave an intermediate tree with either wrong colours or inconsistent surfaces and would need a second
merge approval, which the owner did not request. If implementation grows beyond the §5 scope, stop and ask
before splitting.

## 5. In scope and deferred

**In scope**

- Dark palette with the owner's five surface values and the light Light+ equivalent, plus text, state, semantic,
  and focus roles.
- High-contrast preservation and density preservation with explicit precedence tests.
- Shell backdrop, region cards, menu bar, status bar, document tab strip, console tab strip, splitters' focus
  treatment.
- Two fixed console tabs, validation labels/automation names, active-tab persistence, old combo removal.
- Welcome-page button alignment and recent-row buttons.
- Command palette, shortcut cheat sheet, settings, dialogs, notifications, error/read-only banners under the
  same tokens.
- New default fractions, `defaultsRevision` migration, and the **Reset layout** command.
- Contrast, focus, density/scale, automation-name, layout, migration, persistence, and screenshot tests.
- Documentation updates listed in §4.

**Deferred**

- Custom scrollbar, menu, tooltip, or context-menu control templates. Only Fluent resource keys and control
  properties change in this pass.
- Charts, 3D views, rendering, and any `Mocassin.Rendering` surface.
- Document drag-reorder, tab pinning, tab previews, and tab overflow menus.
- Console tab close/reorder, per-tab badges, and severity icons/strips in notifications.
- Live-region announcements, focus ring redesign beyond the existing adorner, and a third high-contrast theme.
- Responsive overlay widths and the deferred fixed-width issues from the previous shell fix (L-6, L-7).
- The Step 24 model editors and any later editor surface.
- New icons, fonts, assets, accent palette expansion, or third-party themes.

## 6. Tests

All new tests are PR-fast headless unless named PR-medium. They use the real `ShellWindow`, real settings store,
real project files, and the existing `HeadlessSession`. Fakes remain dialogs, pickers, clipboard, dispatcher,
and time.

### 6.1 Palette and theme

- Resolve every role in `Palette.axaml` under `ThemeVariant.Light` and `ThemeVariant.Dark`; assert the exact hex
  values from the visual contract.
- Assert the `Default` theme dictionary mirrors Light for hosts without a variant; apply `ThemeSelection.System`
  and assert every role resolves (the `Default` fallback is complete) while an explicit Light/Dark selection
  resolves the matching dictionary.
- Apply dark + high contrast and assert every role/Fluent key resolves to the high-contrast value, border
  thickness is 2, and compact padding survives.
- Apply light + comfortable and light + compact and assert palette and density values; assert the applier's
  direct resources do not mask a palette role.
- Assert the five non-default optional accents publish a contrast-safe `OnAccentBrush`; the default and `blue`
  are `#007ACC`.
- Assert no view XAML contains a non-token colour literal in the primary surface set (a source scan with a
  small allow-list for `Transparent`; the scrim is `Mocassin.OverlayScrim`).

### 6.2 Contrast and accessibility

- `Visual/ThemeContrastTests` computes WCAG relative luminance for every pair in visual contract §11.1 in both
  themes and all non-default optional accents; normal text ≥ 4.5:1, large text ≥ 3:1, focus ring ≥ 3:1.
- High-contrast pairs meet ≥ 7:1.
- Region cards expose `Projects panel`, `Documents panel`, `Inspector panel`, and `Console panel`; the console
  strip is `Console tabs`; tab headers are the visible labels.
- Every primary interactive control keeps an accessible name; the existing automation-role and Tab-order tests
  stay green.
- The removed `Console channel` combo is absent; `Validation text filter` and `Validation entries` are present.
- At 1.5× font scale + compact density, primary labels, tab headers, status text, and dialog buttons do not
  clip or overlap.

### 6.3 Layout and migration

- New defaults at 1200 × 800: left 0.20, right 0.22, console 0.32 within 0.005; tracks fill the workspace; the
  only chrome gaps are the 6 px splitter tracks and the `RegionGap` inset.
- At 900 × 600, every visible region stays at or above its minimum.
- Drag, window resize, hide/show, keyboard resize, and splitter focus tests from the previous fix stay green.
- Reader cases: no layout block → new defaults + revision 1; old defaults without revision → new defaults +
  revision 1; custom fractions without revision → preserved + revision 1; revision 1 + old-default values →
  unchanged; revision 1 + custom values → unchanged; legacy pixels 260/320/180 → new defaults; legacy custom
  pixels → preserved; invalid revision → warning + migration; second load idempotent.
- Store/writer: `defaultsRevision` is written, pixel keys are absent, and a rewrite does not change the values.
- **Reset layout** restores the new defaults and all regions visible, schedules the existing layout persist,
  and leaves the active console tab and other settings unchanged.
- An `ActiveChannel` switch schedules the existing layout persist; a new host built on the same data directory
  restores the tab; an unknown stored id falls back to `Console`.

### 6.4 Console tabs

- Exactly two tabs, in order `Console`, `Validation`; the content of the second is the real `ProblemsView`.
- Default active tab is `Console`; setting the active channel to `problems` selects `Validation`.
- Arrow keys move selection; click selects; `Enter` is a no-op; `Escape` does not close or change the console.
- No close/reorder UI; the tab strip has no close buttons.
- The active channel still drives the existing validation filters, navigation, and status item tests.
- The tab strip is a deterministic Tab stop and does not leave focus on a hidden control.

### 6.5 Welcome centring

- With one recent file, render the welcome document at comfortable and compact density in light and dark.
- For `New project`, `Open project`, `Keyboard shortcuts`, `Open`, and `Remove`, measure the rendered text
  centre against the button centre; horizontal and vertical deltas are ≤ 1 device-independent pixel.
- The recent buttons use the compact style; the main buttons use the default style; neither has a local
  padding override.
- The missing-project marker and path ellipsis still render.

### 6.6 Screenshots

Regenerate the existing Linux baselines and add the new ones at 1000 × 700:

| Baseline | Content |
| --- | --- |
| `light.png`, `dark.png`, `highcontrast.png` | full shell, welcome document, console visible with the two tabs |
| `light-validation.png`, `dark-validation.png` | Validation tab active, no project, empty state |
| `light-settings.png`, `dark-settings.png` | settings overlay over the shell |

Palette, dialog, notification, and cheat-sheet surfaces use the same card/token rules and are covered by the
token and structural tests; separate pixel baselines are not added, to keep the PR-medium matrix bounded. The
high-contrast capture keeps compact density. Regeneration uses
`MOCASSIN_REGENERATE_VISUAL_BASELINES=1`; the diff is reviewed against the contract, not accepted wholesale.

## 7. Acceptance criteria

1. Every primary surface consumes a `Mocassin.*` role; no literal palette colour remains in the primary-surface
   views, and `App.axaml` contains no colour roles.
2. Dark matches the owner's surface set; light matches the Light+ equivalent; high contrast still wins and
   keeps 2 px borders.
3. Contrast tests pass for both themes, the default accent, and every non-default optional accent.
4. Region cards have 6 px radius, 1 px border, and 6 px gaps; the splitter hit target, minima, fraction model,
   window resize, hide/show, and keyboard resize remain correct.
5. The console has exactly the two fixed tabs in order; labels and validation automation names are coherent;
   the active tab persists across a restart; an unknown id falls back to `Console`; `Enter` is a no-op.
6. The welcome buttons and the recent Open/Remove buttons have centred labels in both themes and both
   densities.
7. Defaults are left 0.20, right 0.22, console 0.32; the migration follows §3.2 exactly, is idempotent, and
   does not override custom fractions; **Reset layout** restores the defaults.
8. The screenshot baselines are regenerated/added and reviewed; no unintended surface changed.
9. Accessibility names, roles, focus rings, Tab order, and 1.5×/compact layout pass the headless tests.
10. `./tools/verify.sh dotnet`, `./tools/verify.sh docs`, and `./tools/verify.sh policy` pass locally; the
    implementor runs `./tools/verify.sh all --require` before requesting the merge. No per-feature CI is
    dispatched; the closeout/main CI policy in `AGENTS.md` §3.13 is unchanged.
11. No new dependency, `contracts/manifest.json` change, ADR, amendment, public API change, or frozen-record
    edit.
12. The visual contract is linked from `ui-architecture.md` and the development index; the `zensical.toml` nav
    entry is added by the implementor.

## 8. Risks

| Risk | Mitigation |
| --- | --- |
| A Fluent key is missed, leaving a control blue or light in one theme | The contract lists the expected key set; add a resolved-resource test for every key; screenshot the shell and one overlay per theme; stop and add the key to the contract before inventing a template |
| High contrast leaks a palette colour | `HighContrast.axaml` defines every role and key; a test enumerates the list and asserts HC values; density still applies |
| Overriding Fluent keys changes a control template layout | Only colours, borders, padding, and corner radius are changed; no templates are replaced; compare screenshots and control bounds; stop if a control needs a template rewrite |
| Light palette or state/semantic colours are outside the owner's five dark values | The values are the Light+ equivalent and the contrast-safe state/text derivations; the owner approved them on 2026-11-21 (Q2) |
| Welcome centring differs by font metrics | The test measures the rendered text centre, not the CSS metrics, with a 1 px tolerance; check both densities and both themes |
| One-time migration discards a deliberate layout | Upgrade only when all three values equal the old defaults within `1e-6`; preserve any custom value; add a reset command; test custom fractions and legacy pixels |
| Adding `defaultsRevision` is treated as a format change | It is additive and application-local; no schema bump, manifest, ADR, or cross-language fixture; document it in `reference/file-layout.md` |
| Console tab persistence writes settings too often | Reuse the existing debounced `PendingLayoutPersist`; test one switch and reload, not per-render writes |
| Active channel switch still does not persist | Add the `Console.PropertyChanged` subscription and test a restart with the real settings store |
| Screenshot churn obscures a real regression | Bounded baseline set; review each capture against the contract; record the regeneration command and file list in the commit |
| `TabControl` focus/arrow behaviour differs from the button/combo path | Use the interaction contract mapping and add arrow/`Enter`/Tab-order tests before changing the view |
| Scope grows into scrollbars, menus, tooltips, or chart theming | The deferred list in §5 is explicit; stop and ask for a new amendment if a primary surface cannot pass without a template |
| The documentation nav is missing the new contract page | The implementor adds the `zensical.toml` entry; the planner does not edit configuration; the docs gate is re-run after it |

## 9. Gates and process

| Gate | Decision |
| --- | --- |
| ADR | **Not required.** The change is application-local visual tokens and settings, not a project/package/results/state format, dependency, or architecture boundary. No `contracts/manifest.json` change. |
| Amendment | **Not applicable.** This is an out-of-step maintenance fix under `AGENTS.md` §3.5, not a step amendment. |
| Independent design review | **Not required by this design.** The owner fixed the direction, the change is bounded to the existing shell surfaces, and the contrast/migration/tab/screenshot tests cover the risky parts. The reviewer harness binding also cannot host a record on a `fix/` branch. If the owner wants a review, stop before implementation and use the §3.9 process. |
| `ui-interaction-contract.md` | **Updated 2026-11-21.** §5.2 now says Validation list; §6.1 shows left 0.20, right 0.22, console 0.32 with the nominal extents; §6.4 records the `defaultsRevision` migration, the reset command, and the `activeConsoleChannel` tab ids. The focus, `Escape`, `Enter`, layer, and splitter behaviour is unchanged. See Q1. |
| Step tag | **None.** No step is created and no `step-NN` tag is moved. |
| Parity matrix | **No change.** This is a visual maintenance fix, not a feature; no parity row is added or moved. |
| Merge | One owner `--no-ff` approval after the local gates and the evidence in this record. No per-feature CI. |
| Platform runs | Not a merge gate in this record. The headless suite runs on the platform matrix when a platform job is dispatched; screenshots remain Linux-only. A human Windows/macOS visual spot-check is recommended and recorded as evidence if performed. |

## 10. Owner decisions

The owner approved the design and resolved every open question on 2026-11-21. These are fixed inputs for
implementation and are not reopened.

| ID | Decision |
| --- | --- |
| D1 | Dark surfaces: `#1e1e1e` chrome, `#252526` workspace, `#2d2d30` cards/panels, `#3c3c3c` borders, `#007acc` accent. |
| D2 | 6 px rounded cards, 1 px border, small gaps, VS Code density. |
| D3 | Primary surfaces in one branch; custom scrollbar/menu/tooltip/context-menu templates deferred. |
| D4 | Console tabs `Console` then `Validation`; Problems content becomes Validation; active tab persists; no close/reorder; internal id may stay `problems`. |
| D5 | Light theme restyled in parallel to a VS Code Light+ equivalent. |
| D6 | One `fix/gui-visual-refresh` branch and one owner `--no-ff` merge; no independent review unless this design concludes one is needed. |
| Q1 | **Approved.** The interaction contract is updated in this branch: new default fractions, `defaultsRevision` migration, `activeConsoleChannel` tab ids, reset command, and the Validation wording. Splitter, focus, `Enter`/`Escape`, and layer behaviour is unchanged. |
| Q2 | **Approved — planner mapping.** Chrome `#1e1e1e`, region cards `#252526`, overlays/nested cards `#2D2D30`; the derived text, state, semantic, focus, and light-theme values in visual contract §3 are approved as written. |
| Q3 | **Approved — accent status bar.** The status bar keeps the accent background with white text/glyphs; severity is carried by glyph and text. |
| Q4 | **Approved — keep all five optional accents.** The five non-default keys keep their values; `blue` and the null/default accent are `#007ACC`; the applier uses a contrast-safe on-accent foreground. |
| Q5 | **Approved — recommended evidence, not a gate.** A human Windows/macOS visual spot-check is recommended and recorded if performed; it is not a merge gate. |

## 11. Done when

- The visual contract is accepted and linked from `ui-architecture.md` and the development index; the nav entry
  is added by the implementor.
- The 2026-11-21 decisions in §10 are recorded and the interaction contract's numeric/default, migration,
  Validation, and reset text is updated in the same branch.
- The palette, density, high-contrast, shell, region, menu/status, console-tab, welcome, palette/settings/dialog/
  notification, layout/migration, and reset work in §5 is implemented.
- The tests in §6 pass; the screenshot baselines in §6.6 are regenerated/added and reviewed.
- `./tools/verify.sh all --require` passes; the branch is pushed for owner review.
- This record is updated with the commit SHAs, exact commands/results, screenshot list, and any deviation.
- The owner approves the `--no-ff` merge under §3.5; no step tag is created.
