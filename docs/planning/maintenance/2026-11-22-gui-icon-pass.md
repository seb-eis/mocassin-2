# GUI icon pass — explorer/projects tree and document tabs (2026-11-22)

An out-of-step maintenance change under `AGENTS.md` §3.5. This record is the design and test plan for one
`fix/gui-icon-pass` branch and one owner `--no-ff` merge approval. It populates the collapsed `TabIconSlot`
from the 2026-11-22 tab-header pass and adds matching icons to the explorer/projects tree, by vendoring a
small MIT-licensed Phosphor Icons subset as committed geometry.

| Field | Value |
| --- | --- |
| Type | Maintenance fix / icon assets, mapping, and slot population |
| Branch | `fix/gui-icon-pass` |
| Base | `main` at `2d9d5f487706ba946d6a027a1d668677b2535d19` |
| Worktree | `/workspace` (clean, in place) |
| Normative contract | [`docs/development/ui-visual-contract.md`](../../development/ui-visual-contract.md) §2, §5.2, §7.4, §7.6, §8, §11.3, §12, §13 |
| Design review | [`step-gui-icon-pass.md`](../reviews/step-gui-icon-pass.md) — Light class, `Ready for review` |
| Status | Design frozen for review; not implemented |
| Tracking | One branch; local gates; no per-feature CI; no step tag; no ADR |

## 1. Owner request and fixed decisions

The owner requested: add matching icons (Phosphor Icons, MIT) to the explorer/projects tree and to every open
document/control tab, by populating the collapsed 16 × 16 `TabIconSlot` that the tab-header pass reserved on
`DocumentViewModel`/`ConsoleChannelViewModel`.

Owner decisions already taken:

| ID | Decision |
| --- | --- |
| C1 | Coverage: explorer/projects tree nodes and document tabs only. Console/Validation tabs, the welcome recent-project list, settings, dialogs, and notifications stay text-only. |
| L1 | Licensing: Phosphor Icons are MIT. Vendor a subset as committed assets with provenance — no runtime font download, no network, no icon package dependency. Record the Phosphor release/commit, the licence copy, and a `NOTICE.md` attribution. |
| S1 | Scope guard: no new dependencies, no custom control templates beyond what the slot contract already defines, no restyle, no Step 24 work. |

**Owner interpretation (recorded for review and veto).** The phrase "explorer/projects tree" is read to cover
both content surfaces of the projects panel: the open-projects list rows and the explorer tree nodes. The
supporting reading is C1's exclusion list, which names the *welcome* recent-project list but not the projects
panel list. If the owner intended tree and tabs only, strike the `project` key's consumer row — it is one
binding, one geometry, and one test row; nothing else in the design changes.

## 2. Design class

**Light.** The change vendoring happens entirely inside the existing UI boundary: internal view models in
`Mocassin.App.ViewModels`, slot templates and resource styles in `Mocassin.App.Views`, and one geometry resource
dictionary merged like the palette. No public API (the touched types are internal; no `PublicAPI.*.txt` change),
no `.mocprj`/`.msl`/`.msr`/`.mcs` format or `contracts/manifest.json` change, no package dependency (committed
assets only), no architecture boundary, no migration, and no irreversible decision — removing the vendored
files reverses the whole change. The one candidate Full trigger, third-party licensing, is already resolved by
the repository's established pattern: ADR 0007 rule "files with third-party content carry their own SPDX
identifier and attribution", the spglib catalog precedent (pinned source, checksums, provenance file, NOTICE
entry — no separate ADR), and the owner's fixed L1 decision. The upstream licence was verified before this
design relied on it (§7): Phosphor Icons is MIT, © 2023 Phosphor Icons. The vendoring pipeline is manual and
recorded here — no `tools/` generator and no drift-check script; the provenance invariants are enforced by an
ordinary test (§9). If implementation finds a generator with drift checking is genuinely required (for example
the icon set must grow beyond about twenty glyphs), stop: that is a Full-trigger reclassification and an owner
decision, not a silent extension.

## 3. Current state

The tab-header pass (merged as `df1067e`, decision I1/O3) reserved the slot and left it collapsed:

| Surface | Location | State today |
| --- | --- | --- |
| Document tab header | `src/dotnet/Mocassin.App.Views/Shell/DocumentsRegionView.axaml:17-25` | 16 × 16 `TabIconSlot` `ContentControl`, `Content="{Binding Icon}"`, collapsed while `Icon` is null, `IsHitTestVisible="False"`, `Focusable="False"`, `AccessibilityView="Raw"` |
| Console tab header | `src/dotnet/Mocassin.App.Views/Consoles/ConsoleView.axaml` (item template) | the same `TabIconSlot`, collapsed |
| Document view model | `src/dotnet/Mocassin.App.ViewModels/Documents/DocumentViewModel.cs:35` | `public virtual object? Icon => null;` — no subclass overrides it |
| Console channel view model | `src/dotnet/Mocassin.App.ViewModels/Consoles/ConsoleChannelViewModel.cs:58` | `public object? Icon { get; init; }` — never set |
| Explorer node | `src/dotnet/Mocassin.App.ViewModels/Navigation/ExplorerNode.cs:15-26` | record with `IconKey` (`string?`) already populated by the provider; no icon is rendered |
| Node provider | `src/dotnet/Mocassin.App.ViewModels/Navigation/ProjectExplorerNodeProvider.cs:16-26` | sets `IconKey` `"overview"` and `"json"` on the two nodes |
| Explorer tree template | `src/dotnet/Mocassin.App.Views/Shell/ProjectsRegionView.axaml:62-66` | `TreeDataTemplate` with a `TextBlock` on `Label` only — no slot |
| Open-projects list | `src/dotnet/Mocassin.App.Views/Shell/ProjectsRegionView.axaml:25-44` | two-line row (name, path, lock state); no slot |
| Document kinds | `src/dotnet/Mocassin.App.ViewModels/Documents/DocumentKinds.cs` | `welcome`, `project.overview`, `project.json` — the three open-document types today |
| Realized documents | `WelcomeDocumentViewModel` (`Welcome`), `ProjectOverviewViewModel` (`Overview — {name}`), `ProjectJsonViewModel` (`Project JSON — {name}`) | the complete set of open-document types today |

No icon asset, geometry resource, or icon template exists anywhere in the application today. The accessibility
slot contract (`AccessibilityTests.cs:338-366`, `ConsoleTabsTests.cs:60`) asserts the collapsed, `Raw`,
non-focusable, non-hit-testable slot and the unchanged tab names.

## 4. Icon keys and mapping

One icon key names one semantic target; the tree node and the document tab for the same target share one
glyph. The keys are stable application-local strings (an `IconKeys` constants class in the view models), the
geometries are `Mocassin.Icon.<Name>` `StreamGeometry` resources, and the mapping table is normative in the
visual contract §5.2.

| Icon key | Resource | Phosphor icon (regular) | Consumers |
| --- | --- | --- | --- |
| `welcome` | `Mocassin.Icon.Welcome` | `hand-waving` | welcome document tab |
| `overview` | `Mocassin.Icon.Overview` | `info` | `project.overview` explorer node; project overview tab |
| `json` | `Mocassin.Icon.Json` | `brackets-curly` | `project.json` explorer node; project JSON tab |
| `project` | `Mocassin.Icon.Project` | `cube` | open-projects rows; future project-scope nodes |
| — | `Mocassin.Icon.Missing` | `warning-circle` | the missing-asset marker; never mapped from a kind or node |

Choice rationale and veto: `info` reads as a summary surface, `brackets-curly` as canonical JSON,
`hand-waving` as the welcome page, `cube` as a solid-state simulation project, and `warning-circle` as a
defect marker. The specific glyph for a target is an owner-vetoable aesthetic choice: swapping one is a
one-geometry change with a checksum update, not a design change. Recorded alternatives: `house` for
`welcome`, `package` for `project`. No icon choice requires an owner decision before implementation; the
owner vetoes at review or merge if desired.

The console channels keep `Icon` null (C1), so their slot stays collapsed and the console strip stays
text-only. No slot is added to the welcome recent-project list, settings, dialogs, or notifications.

## 5. Colour and state rules

The geometry carries no colour. The fill is a §3 role of the visual contract per surface and state:

| Surface / state | Fill role |
| --- | --- |
| Tab, inactive | `Mocassin.TabInactiveForeground` |
| Tab, selected | `Mocassin.TabActiveForeground` |
| Tree row and project row, unselected | `Mocassin.ForegroundMutedBrush` |
| Tree row and project row, selected | `Mocassin.SelectedForeground` |
| Missing-asset marker, any surface | `Mocassin.ErrorForeground` |

This realizes the owner's fixed rule — muted by default, strong/active on the selected tab — using only
existing roles: the dark inactive tab foreground `#9D9D9D` is the muted value, and the active tab foreground
`#FFFFFF`/`#1F1F1F` is the strong role. High contrast resolves through the same roles; no icon-specific colour
keys are added. Every pair is an existing §11.1 contrast row. The missing marker additionally meets the 3:1
non-text floor on every fill it can appear on (dark `#F48771` on `#252526` is 6.2:1, on `#2D2D30` 5.6:1; light
`#A1260D` on `#FFFFFF` is 7.5:1, on `#ECECEC` 6.3:1; HC `#FFFFFF` on `#000000` is 21:1) — the test asserts
these four pairs.

## 6. Slot contract and accessibility

The three slots share one contract, frozen by the tab-header pass and extended by §5.2: 16 × 16 logical pixels
(`Mocassin.IconSize`), vertically centred, collapsed while the bound icon value is null,
`IsHitTestVisible="False"`, `Focusable="False"`, `AutomationProperties.AccessibilityView="Raw"`, no accessible
name, no text. Slots are named `TabIconSlot` (existing), `NodeIconSlot` (explorer node content), and
`ProjectRowIconSlot` (open-projects row). A slot adds no focus stop and never contributes text to a name; the
tab's name stays its title, the node's name stays its label, the row's name stays the project name block. An
icon is never the sole carrier of meaning: every icon surface keeps its visible text label.

**Recommended delivery mechanism (implementation latitude within these bounds).** The view models expose the
icon as a first-party descriptor record (`AppIcon(string Key)`); `ExplorerNode` gains a computed
`Icon => IconKey is null ? null : new AppIcon(IconKey)`, and the three document view models override `Icon`
with their key. One implicit `DataTemplate` for `AppIcon` (registered with the application) renders a `Path`
with `Data` resolved from the key by a single converter and `Stretch="Uniform"`. Tab fills follow the tab's
inherited foreground (the same delivery that colours the title text), so the active/inactive roles follow
every state and theme with no extra styles. Row fills are delivered by targeted styles on the slot's
`Foreground` (`ForegroundMutedBrush` at rest; `SelectedForeground` through the existing selected-state
delivery), following the §3.6 targeted-style precedent. Probe the selector reach headlessly before
implementing; if the selected-state fill cannot be delivered without a control template, stop and ask — a
template is out of bounds (S1). Any equivalent single mechanism (converter-only, per-surface class styles)
is acceptable if the observable rules above are met and tested.

## 7. Vendoring pipeline

Manual, recorded, and offline at build and run time. No `tools/` generator, no CI job, no network access
after vendoring.

**Source pin (verified 2026-11-22).**

| Field | Value |
| --- | --- |
| Upstream | `https://github.com/phosphor-icons/core` |
| Package | `@phosphor-icons/core` 2.1.1 |
| Pinned commit | `2b75f3ad12b420c9504ef05df8d2564a28f8500e` (repository `main` head, 2026-01-06) |
| Licence | MIT, © 2023 Phosphor Icons — `LICENSE` at the pinned commit, SHA-256 `b5b1f1da112d18ea2147decfd48ddc1bf2b5aeb6c265381579340e95b15a2bb2` |
| Weight | regular (the six-weights catalogue exposes `assets/regular/<name>.svg`) |

**Subset and checksums.** Five upstream SVGs, fetched from the pinned commit. Each is a single
256 × 256 `viewBox` path with `fill="currentColor"`; the path data is copied verbatim into one
`StreamGeometry` per icon (Avalonia's path mini-language supports the arcs these icons use; the parsing is
asserted by the realized tests). The design-verified SHA-256 of each upstream file:

| Icon | Upstream path (at the pinned commit) | SHA-256 |
| --- | --- | --- |
| `info` | `assets/regular/info.svg` | `438a1f319d53d86e840af73fb7b1d224c9224a437b9fa449a4e2cb45a82a3552` |
| `brackets-curly` | `assets/regular/brackets-curly.svg` | `170562ad2b247d86bf2b5fd727cf3f96681b0cbef92aab49346ec6e235802ca9` |
| `hand-waving` | `assets/regular/hand-waving.svg` | `07c4c9d18c9a4c86508fde52cddaf95aa1499552c99bfeebc404d5b991fdf41f` |
| `cube` | `assets/regular/cube.svg` | `7c0f13cb492d0dd212e48ab259dabec4627df32d71aa00ce31b3228c0f35112b` |
| `warning-circle` | `assets/regular/warning-circle.svg` | `e48a90760a68659fd3f06dd9187e471aead4877c72e915339071b2c7f7bd714c` |

The dev re-hashes the exact bytes fetched from the pinned commit at vendoring time and records them in the
provenance file; if any hash differs from the table above, stop — the upstream moved or the fetch is wrong.
Recorded alternative for `welcome` if the owner vetoes `hand-waving`: `house`,
SHA-256 `180e9259e99e23da197b59b19394d006d6a5d33e384cc2b9c8381cb0214a5716`.

**Committed files.**

| File | Content |
| --- | --- |
| `src/dotnet/Mocassin.App.Views/Icons/Icons.axaml` | one `StreamGeometry` per key (`Mocassin.Icon.Welcome/Overview/Json/Project/Missing`), path data verbatim from the pinned SVGs, plus a leading `SPDX-License-Identifier: MIT` header and a provenance comment (upstream URL, package 2.1.1, pinned commit, licence MIT, fetch date). No URL is dereferenced at build or run time. |
| `src/dotnet/Mocassin.App.Views/Icons/IconsProvenance.md` | the pinned source table (upstream, package, commit, licence, licence SHA-256), the per-icon table above (key, Phosphor name/weight, upstream path, SHA-256, conversion note), and the procedure for adding a glyph. |
| `src/dotnet/Mocassin.App.Views/Icons/PhosphorLicense.txt` | the verbatim MIT licence text from the pinned commit. |

`Icons.axaml` is merged from `Mocassin.App/App.axaml` next to the palette (the geometry is
theme-independent), and `Mocassin.IconSize` (`16`) is added to the theme-independent dimensions in
`App.axaml`.

**Licence-header scope note.** `tools/check-license-headers.py` covers `.cs`/`.rs`/`.py`/`.sh`/`.ps1` only, so
the `.axaml`/`.md`/`.txt` provenance requirements are enforced by the new provenance test, not by the existing
gate. Any `.cs` file the dev adds (converter, `AppIcon`, `IconKeys`) carries the normal MIT SPDX header.

**NOTICE entry (exact text for the dev to add under "Third-party dependencies").**

> ### Phosphor Icons (vendored icon subset)
>
> The desktop application embeds a small subset of Phosphor Icons as Avalonia `StreamGeometry` resources
> (`src/dotnet/Mocassin.App.Views/Icons/Icons.axaml`). Phosphor Icons is distributed under the MIT licence
> (https://github.com/phosphor-icons/core, package `@phosphor-icons/core` 2.1.1, pinned commit
> `2b75f3ad12b420c9504ef05df8d2564a28f8500e`). The pinned source, per-icon checksums, and the licence copy are
> recorded in `src/dotnet/Mocassin.App.Views/Icons/IconsProvenance.md`; the licence text is
> `src/dotnet/Mocassin.App.Views/Icons/PhosphorLicense.txt`. © 2023 Phosphor Icons. No icon font, package
> dependency, or runtime download is used.

No GPL content is involved: Phosphor Icons is an independent MIT project, the vendored files are its MIT
assets, and nothing from the original GPL-licensed MOCASSIN repository or `/workspace/mocassin-original` is
copied.

## 8. Expected implementation files

Assets and resources (new): `Icons/Icons.axaml`, `Icons/IconsProvenance.md`, `Icons/PhosphorLicense.txt`,
the `AppIcon` record and `IconKeys` constants, and the key-to-geometry converter (recommended home:
`src/dotnet/Mocassin.App.Views/Icons/` for the converter; the record/constants in
`src/dotnet/Mocassin.App.ViewModels`).

Views and view models: `App.axaml` (merge, `Mocassin.IconSize`, the implicit `AppIcon` template registration);
`ProjectsRegionView.axaml` (`NodeIconSlot` in the tree template, `ProjectRowIconSlot` in the row template);
`DocumentsRegionView.axaml` and `ConsoleView.axaml` (slot size from the token; no other change);
`ShellStyles.axaml` (row-slot fill styles); `ExplorerNode.cs` (computed `Icon`); the three document view
models (`Icon` overrides); `DocumentViewModel.cs`/`ConsoleChannelViewModel.cs` unchanged beyond what exists.

Tests: new `Mocassin.App.Tests/Visual/IconAssetTests.cs`; updates to `AccessibilityTests.cs`,
`ConsoleTabsTests.cs`, `ThemeContrastTests.cs`, and `ScreenshotTests.cs`; regenerated and new baselines under
`Visual/Baselines/`.

Documentation: `NOTICE.md` (the entry above) and `docs/development/testing-and-parity.md` (the icon test and
baseline rows). The visual contract is updated by this design commit, not by the implementation.

## 9. Tests

All headless tests use the real shell, palette, high-contrast dictionary, and resource dictionaries; the
screenshot rows are PR-medium. The assertions read realized controls, resources, and files, not only
`TryGetResource`.

1. **Geometry and mapping** (`IconAssetTests`, PR-fast): the five `Mocassin.Icon.*` resources resolve as
   `StreamGeometry` in the application scope; a welcome tab, an overview tab, a JSON tab, both explorer
   nodes, and an open-projects row realize a visible slot whose `Path.Data` equals the mapped geometry; a
   node/row with a null icon value renders no visible slot.
2. **Fill roles per state and theme** (`IconAssetTests`, PR-fast): inactive tab icon fill equals
   `Mocassin.TabInactiveForeground`, selected tab icon fill equals `Mocassin.TabActiveForeground`;
   unselected tree/project row icon fill equals `Mocassin.ForegroundMutedBrush` and selected equals
   `Mocassin.SelectedForeground`; in light, dark, and high contrast.
3. **Missing-asset marker** (`IconAssetTests`, PR-fast): a slot bound to an unresolved key renders the
   `Mocassin.Icon.Missing` geometry with an `ErrorForeground` fill; no shipped surface (three tabs, two
   nodes, projects row) realizes the marker or any `ErrorForeground` fill.
4. **Slot contract** (`AccessibilityTests`, PR-fast): the three slots are 16 × 16, `Raw`, not focusable, not
   hit-testable, unnamed; the tab, node, and row accessible names are unchanged; no focus stop is added or
   removed; the slot stays 16 × 16 at compact density and 1.5 × font scale with no clipping.
5. **Console coverage** (`ConsoleTabsTests`, PR-fast): the console channels' `Icon` is null and the console
   slots stay collapsed — console tabs are text-only.
6. **Excluded surfaces** (`IconAssetTests`, PR-fast): the welcome recent-project list, settings, dialogs,
   and notifications contain no icon slot.
7. **Contrast** (`ThemeContrastTests`, PR-fast): add the four missing-marker pairs (`ErrorForeground` on
   `TabActiveBackground`, `TabInactiveBackground`, and the row surfaces) at the 3:1 non-text floor; the muted
   and selected icon pairs are existing §11.1 rows and are not re-specified.
8. **Provenance** (`IconAssetTests`, PR-fast unit): `Icons.axaml` contains the MIT SPDX header and the pinned
   commit line; `IconsProvenance.md` lists exactly the five mapped keys with a 64-hex SHA-256 each and no URL
   is dereferenced from the application; `PhosphorLicense.txt` exists and contains "MIT"; the `NOTICE.md`
   entry exists.
9. **Screenshots** (`ScreenshotTests`, PR-medium): regenerate the existing shell/overlay baselines (the
   welcome-tab icon changes pixels in every capture that shows the shell); add `light-project.png`,
   `dark-project.png`, and `highcontrast-project.png` with an active project, realizing both explorer node
   icons, a projects-row icon, and the Overview and JSON tab icons, so every icon surface is visible in at
   least one light, one dark, and one high-contrast capture (the existing captures start from an empty
   workspace and cannot show them). Baseline regeneration uses
   `MOCASSIN_REGENERATE_VISUAL_BASELINES=1 dotnet test ... --filter "FullyQualifiedName~ScreenshotTests"`;
   the 5 % tolerance and Linux-only comparison are unchanged.
10. **Existing tests**: `TabTreatmentTests`, `ScrollGutterTests`, `AccentDeliveryTests`, and
    `WorkspaceRenderTests` must pass unchanged (no tab geometry, gutter, accent, or focus behaviour changes).

## 10. Documentation plan

- `docs/development/ui-visual-contract.md` — updated by this design commit: §2 token home, §5.2 (the icon
  section), §7.4 (slot populated), §7.6 (row/node icons), §8 (console slot stays collapsed), §11.3 (three
  decorative slots), §12 (provenance boundary), §13 (verification rows).
- `docs/development/testing-and-parity.md` — the dev adds the icon test rows and the new baselines to the
  inventory.
- `NOTICE.md` — the dev adds the entry in §7.
- No user-guide change: the icons are decorative; no user-facing behaviour, name, or workflow changes.
- No ADR (§11 gates), no `contracts/manifest.json`, no interaction-contract change, no public API.

## 11. Acceptance criteria

1. The five `Mocassin.Icon.*` geometries resolve from `Icons.axaml`, and the §5.2 mapping realizes a visible
   16 × 16 glyph on every shipped icon surface: welcome/overview/JSON tabs, both explorer nodes, and
   open-projects rows.
2. The fill follows the §5.2 roles per state and theme, including high contrast, with no icon-specific colour
   keys.
3. A null icon value collapses the slot (console tabs stay text-only); an unresolved key renders the
   `Mocassin.Icon.Missing` marker in `ErrorForeground`, and no shipped surface shows the marker.
4. The slots keep the accessibility contract (`Raw`, unnamed, not focusable, not hit-testable); tab, node, and
   row accessible names are unchanged; no focus stop is added or removed; the slot stays 16 × 16 at both
   densities and 1.5 × font scale.
5. The vendored subset is committed with the MIT SPDX header, the pinned upstream release/commit, per-icon
   SHA-256 checksums matching §7, the licence copy, and the `NOTICE.md` entry; no icon package, font,
   dependency, or network access is added.
6. The excluded surfaces (console tabs, welcome recent list, settings, dialogs, notifications) stay text-only
   and are tested as such.
7. No custom control template is added; the existing tab/gutter/accent/focus tests pass unchanged; the
   screenshot baselines are regenerated with the three open-project captures added and the inventory updated.
8. `./tools/verify.sh all --require` passes on the implementation commit; `./tools/verify.sh docs --require`
   and `./tools/verify.sh policy --require` pass on the docs commits.
9. One branch, one owner `--no-ff` merge approval; no step tag, parity row, or amendment is created.

## 12. Risks

| Risk | Mitigation |
| --- | --- |
| The selected-state fill for tab or row icons cannot be delivered without a control template | Probe the selector reach headlessly first, following the §3.6 precedent. If it cannot be reached, stop and ask; a template is out of bounds (S1), and the fallback is an owner decision, not a silent one. |
| An icon's arc-heavy path data fails to parse as a `StreamGeometry` | The realized geometry test parses and renders every glyph headlessly before anything else lands. If one command fails, re-derive that one geometry with a recorded conversion note in the provenance file; do not substitute a different icon silently. |
| The owner vetoes an icon choice (`hand-waving`, `cube`) | One-geometry swap with a checksum update; §4 records the alternatives (`house`, `package`). No process change. |
| The projects-row interpretation (§1) is wrong | Strike the `project` row: one binding, one geometry, one test row. Recorded for the reviewer and owner. |
| A 16 px fixed icon looks small at 1.5 × font scale | Deliberate: the glyph is fixed structure like the scrollbar; the header grows around it. The alternative (scaling with `CaptionFontSize`) is recorded and rejected for seam stability. Recorded as an owner-vetoable aesthetic note. |
| Screenshot churn: every shell baseline regenerates | The same regeneration procedure as the tab-header pass; the three new captures are the only additions. |
| A future icon set grows beyond a hand-vendored size and tempts a generator | Stop and reclassify: a generator with drift checking is a Full trigger and an owner decision (§2). |
| Provenance rot (upstream moves, keys drift from the table) | The provenance test asserts set equality between `Icons.axaml` keys and the provenance table, plus the checksum format; the design-pinned checksums in §7 catch a moved upstream at vendoring time. |
| The `Missing` marker doubles as a shipped glyph by mistake | The mapping table forbids mapping it from a kind or node, and the test asserts its absence on every shipped surface. |

## 13. Gates and owner decisions

| Gate | Decision |
| --- | --- |
| ADR | **Not required.** Vendored MIT assets inside the existing UI boundary; the ADR 0007 third-party pattern (SPDX + attribution + NOTICE) applies and the spglib catalog precedent shows no separate ADR for pinned third-party data/assets. |
| `ui-interaction-contract.md` | **No change.** No focus, `Enter`, `Escape`, layer, or layout behaviour changes; slots add no focus stops. |
| Control templates | **None.** Stock Fluent controls; the slot delivery uses data templates, converters, and targeted styles only (S1). |
| New dependency | **None.** Vendored committed assets; no NuGet package, no font, no network. `verify.sh api` (NuGet audit) is unchanged. |
| Format / contracts | **None.** No `contracts/manifest.json`, project/package/results/state change. |
| Public API | **None.** All touched types are internal; no `PublicAPI.*.txt` change. |
| Design review | **Required — Light class.** Recorded in [`step-gui-icon-pass.md`](../reviews/step-gui-icon-pass.md). The reviewer binding can only write `step-*.md` records, so the record uses the `step-` prefix while remaining a maintenance review, not a roadmap step. |
| Step tag / parity / amendment | **None.** Out-of-step maintenance under §3.5. |
| Merge | One owner `--no-ff` approval after the local gates and the evidence below. No per-feature CI. |
| Platform runs | Not a merge gate; screenshots are Linux-only, as today. Dispatch the focused Windows/macOS platform runs only if the dev or owner wants the platform evidence, as in the tab-header pass. |

## 14. In scope and deferred

**In scope**

- The vendored Phosphor subset (five regular-weight glyphs), the geometry resources, the provenance and
  licence files, and the `NOTICE.md` entry.
- The `Mocassin.IconSize` token, the slot population on document tabs, and the `NodeIconSlot` /
  `ProjectRowIconSlot` on the projects panel surfaces.
- The fill rules, the missing-asset marker, the mapping table, and the accessibility contract of §5.2.
- The realized, provenance, contrast, and screenshot tests; the visual contract updates (this commit) and
  the test-inventory/NOTICE updates (implementation).

**Deferred**

- Icons for Console/Validation tabs, the welcome recent-project list, settings, dialogs, notifications, and
  any Step 24+ model-editor surface; future document kinds add a mapping row and a vendored geometry when
  their steps land.
- A general icon-vendoring generator or drift-check tooling (a Full trigger; §2).
- Icon fonts, icon packages, runtime theming of glyphs, per-density or per-scale icon variants.
- Tab-strip scrolling, drag-reorder, and the parked `fix/gui-palette-freeze` branch; frozen records; the
  `main` history; completed step pages.

## 15. Done when

- Every shipped icon surface realizes its mapped glyph at 16 × 16 in the §5.2 roles, in light, dark, and high
  contrast, with the slots decorative and unnamed.
- The console tabs and every excluded surface are text-only and tested as such; no shipped surface shows the
  `Missing` marker.
- The vendored subset is committed with the §7 provenance (SPDX, pin, checksums, licence copy) and the
  `NOTICE.md` entry; no dependency, font, template, or network access was added.
- The tests in §9 pass, the baselines are regenerated with the three open-project captures, and
  `./tools/verify.sh all --require` passes.
- The branch is pushed, the design review verdict is accepted by the owner, and the owner approves the
  `--no-ff` merge. No step tag is created.
- This record is updated with the implementation commit SHAs, exact commands/results, the baseline list, and
  any deviation.

## 16. Evidence

To be completed by the implementor:

| Field | Value |
| --- | --- |
| Design commit | `34e7c18d71ee29ab5d81885ba13bb761758f1993` — `docs: define GUI icon pass contract and maintenance plan [skip ci]` |
| Review verdict | — (pending; see the review record) |
| Implementation commits | — |
| Full gate | — |
| App tests | — |
| Baselines | — |
| Platform runs | — (optional, not a merge gate) |
| Merge commit | — (owner `--no-ff` approval pending) |
