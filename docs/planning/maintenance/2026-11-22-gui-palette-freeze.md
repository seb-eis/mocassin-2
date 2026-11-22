# GUI palette freeze — negative reproduction (2026-11-22)

An out-of-step maintenance investigation under `AGENTS.md` §3.5. It records a bounded reproduction attempt for
the owner-reported command-palette freeze. **The freeze was not reproduced and no fix is shipped on this
branch.** The record is evidence for a future run: it lists what was tried, the code paths that could plausibly
cycle, and the diagnostic data needed to capture the fault if it recurs.

| Field | Value |
| --- | --- |
| Type | Maintenance investigation (negative reproduction, no code change) |
| Branch | `fix/gui-palette-freeze` |
| Base | `main` at `19cb6ffe190e02449ce8b827706f365f2ea7afe2` |
| Worktree | `/workspace` (clean, in place) |
| Status | Open — no fix, no merge, no tag. The owner decides whether to close the branch unmerged or keep it as evidence. |
| Scope | Command-palette freeze only. The separate amber-accent propagation issue is out of scope. |

## 1. Owner report

On Linux, the owner resized the main window smaller and then opened the command palette. The process pegged a
core at 100 % CPU and had to be killed. The owner could not reproduce it again afterwards. The report remains a
real intermittent freeze; this record does not classify it as fixed, cosmetic, or user error.

## 2. What was tried

### 2.1 Headless bounded matrix

The real `ShellWindow`, real production composition, real settings store, and the existing `HeadlessSession`
were used. The probe forced the window below its minimum by clearing `MinWidth`/`MinHeight` on the test window
only; production `ShellWindow.axaml:12-15` keeps 900 × 600.

| Dimension | Values |
| --- | --- |
| Window size (DIP) | 1200×800, 1000×700, 900×600, 901×601, 800×520, 700×450, 620×580, 640×600, 620×460, 500×400, 900×300, 620×130, 620×120, 620×100, 300×100, 100×100, 50×50 |
| Font scale | 1.0×, 1.25×, 1.5× |
| Density | comfortable, compact |
| Theme | light, dark |
| Palette open path | `ShowPaletteCommand` and the `Ctrl+Shift+P` catalog gesture |
| Focus before open | console filter text box (a control inside the subtree the palette disables) |
| Palette state | empty results (`zzz-no-command`), a real query (`toggle`), 8 × Down, 8 × Up (selection moves and list scrolling), 600-character query (field overflow) |
| Resize while open | all 17 sizes applied again with the palette open |
| Close | `Escape`, then focus restore to the captured console filter |

That is 408 cases (17 × 3 × 2 × 2 × 2), each with the full palette-state sequence. All completed. No hang, no
exception, and no runaway render. The full matrix took 1 min 58 s.

**Layout-pass bound.** Every operation was wrapped in a `LayoutUpdated` counter. The maximum number of layout
passes observed for any single operation in any of the 408 cases was **2**. A self-perpetuating
measure/arrange cycle would have produced a climbing count or exceeded the 40-pass assertion; it never did.

Two additional headless probes covered:

- splitter drags of −200, −100, 0, +100, and +200 DIP, then a resize to 900 × 600, then palette open, query,
  and `Escape` (the `GridSplitter` pixel-star conversion path);
- command vs gesture open on the same resize sequence.

Both passed with no hang.

### 2.2 Real X11 under Xvfb

1. **In-process real-platform probe.** `AppBuilder … UsePlatformDetect()` under `Xvfb`, real `ShellWindow`,
   client sizes 900×600, 620×580, 500×400, and 900×300 DIP with `MinWidth`/`MinHeight` cleared. Palette opened
   by command, search focused, query typed, palette closed. Run once at scaling 1.0 and once with
   `AVALONIA_GLOBAL_SCALE_FACTOR=1.5` (`RenderScaling` reported 1.5). No hang.
2. **Live application.** The Release `Mocassin.App` was launched under Xvfb at 1600×1200 and at 1024×600, with
   `AVALONIA_GLOBAL_SCALE_FACTOR=1.5`. `Ctrl+Shift+P` was sent with XTest after setting input focus; the palette
   was confirmed open by the scrim darkening the sampled frame (mean channel value 244.65 closed → 147.67 open),
   and `Escape` restored 244.65. With the palette open the window was resized externally to 900×300, 600×400,
   1350×900, 1000×500, 700×400, and 500×300 physical pixels. CPU stayed at 0.0 % for every step. X11 clamped
   the programmatic resize to the 900 × 600 DIP minimum (1350 × 900 physical at 1.5 scaling).
3. No entry was written to any application log during any run (`app*.log` empty), and no exception was thrown.

### 2.3 Framework baseline

The project pins Avalonia 12.1.6 (`Directory.Packages.props:7`). That version already contains the upstream
fixes that match this symptom class:

| Fixed in | Change |
| --- | --- |
| 12.1.1 | `#21808` line-break enumerator infinite loop; `#21864` infinite loop in tab-stop search with `TabNavigation=Once` |
| 12.1.2 | `#22119` infinite loop when a text run shapes to no glyphs |
| 12.1.5 | `#22488` `Window` arrange when its size is platform constrained |

A framework-level loop of these kinds is therefore unlikely on this pin, but not impossible; the pinned version
remains part of the diagnostic capture below.

## 3. Code paths that could plausibly cycle

No path was proven to cycle. These are the candidates a future run should instrument first.

| Path | Location | Why it is plausible |
| --- | --- | --- |
| Fixed overlay geometry | `src/dotnet/Mocassin.App.Views/Commands/CommandPaletteView.axaml:10-14` — `Width="620"`, `MaxHeight="460"`, `Margin="0,120,0,0"`, `VerticalAlignment="Top"` | The palette is the only fixed-size card that can overhang a constrained client area narrower than 620 DIP or shorter than 580 DIP. This is the deferred responsive-overlay direction (L-7); a future framework layout regression would show here first. No loop was observed at 50×50 DIP in headless or at the smallest real client sizes. |
| Layer state machine | `src/dotnet/Mocassin.App.Views/Shell/ShellLayerCoordinator.cs:186-232` (`HandleStateChange`), `:254-262` (`ApplyModality`), `:287-293` (`SetEnabled`), `:295-319` (`PostInitialFocus`), `:321-334` (`PostFocusRestore`) | Opening the palette disables the shell root, re-enables the palette, and posts focus at `DispatcherPriority.Loaded`. The guards are: `next == activeLayer` early return, the `updating` reentrancy flag, `SetEnabled` only on a real change, and a `ComputeLayer()` re-check inside the posted focus job. A broken guard here would be a spin rather than a layout loop. |
| Layout apply ↔ fraction capture | `src/dotnet/Mocassin.App.Views/Shell/ShellView.axaml.cs:72-111` (`ApplyLayout`), `:123-159` (`CaptureFractions`) | `ApplyLayout` writes star lengths/minima from fractions; `CaptureFractions` reads actual sizes only from splitter `DragCompleted` and key-up. A resize does not feed sizes back into fractions, so no measure↔fraction cycle exists in the current code. A future size handler on `SizeChanged` would create one. |
| Layout persistence ↔ settings apply | `src/dotnet/Mocassin.App.ViewModels/Shell/ShellViewModel.Layout.cs:20-36` (`OnRegionPropertyChanged`), `:76-91` (`ScheduleLayoutPersist`/`CaptureLayout`), `:110-127` (`ApplySettings`), `:129-150` (`PersistLayoutAsync`) | A fraction change schedules a persist; the persist sets `suppressSettingsApply`, and `ApplySettings` sets fractions under `applyingLayout`. Both guards are what prevent a settings↔layout loop. If either guard is removed, a non-idempotent normalization could oscillate. |
| Palette result refresh | `src/dotnet/Mocassin.App.ViewModels/Commands/CommandPaletteViewModel.cs:36-49` (`Refresh`) | Every keystroke clears and re-adds `Results` and writes `SelectedCommand`, with a two-way `ListBox.SelectedItem` binding. With zero or many results and list scroll-into-view this is reentrant, though the headless matrix bounded it. |
| Focus/visibility ordering | `src/dotnet/Mocassin.App.Views/Shell/ShellWindow.axaml:20-28` and `ShellLayerCoordinator.cs:295-319` | The palette is shown by an `IsVisible` binding on a hidden sibling grid, then focus is posted after layout. A future change that focuses synchronously during the visibility change would reintroduce a measure/focus interaction. |

## 4. Diagnostic capture needed if it recurs

Record all of the following at the freeze; the issue is otherwise not actionable.

1. **Window geometry:** client size in DIPs and physical pixels, `RenderScaling`/display scaling, window state
   (normal/maximized/fullscreen), and whether the client area is actually below the 900 × 600 minimum (a WM can
   ignore the hints). Include the WM/compositor and session type (X11 vs Wayland/XWayland).
2. **Appearance:** theme (system/light/dark), high contrast, density (comfortable/compact), accent, and font
   scale from the settings file.
3. **Palette state:** opened by gesture (`Ctrl+Shift+P`) or command/menu; query text; number of results;
   selection index; whether the freeze happened on open, after typing, after arrow keys, after `Enter`, or
   after `Escape`; whether the window was resized before, during, or after opening.
4. **Process evidence:** `ps -o pid,pcpu,stat,etime -p <pid>`, whether it was killed, and a UI-thread stack while
   it is spinning (`dotnet-stack report -p <pid>`, or `gdb -p <pid> -batch -ex "thread apply all bt"`). A
   `dotnet-trace collect -p <pid> --providers Microsoft-DotNETCore-SampleProfiler` or `perf record -p <pid>`
   capture is decisive: it distinguishes a managed layout/measure loop from a render-thread or X11 wait loop.
5. **Avalonia/layout evidence:** the number of `LayoutUpdated` passes per second during the freeze, and whether
   `Dispatcher.UIThread` is running jobs or blocked. A short screen recording or a frame dump also confirms
   whether the UI painted at all.
6. **Settings and layout file:** a copy of `Mocassin.Settings.json` (fractions, visibility, console channel,
   theme, density, scale, accent) so the exact state can be replayed.

## 5. Local gates

Run from `/workspace` on the final tree (this docs-only note; no code or test change):

| Command | Result |
| --- | --- |
| `./tools/verify.sh all --require` | Exit 0; final line `Closeout gate completed with required checks enabled.` The `Mocassin.App.Tests` suite passed 278/278. The existing non-gating `Mocassin.Cli` AOT publish evidence (62 IL2xxx/IL3xxx diagnostics) was recorded exactly as on `main`. |
| `./tools/verify.sh docs --require` | Exit 0; strict Zensical, DocFX, and rustdoc stages completed. |
| `./tools/verify.sh policy --require` | Exit 0; 60 policy tests passed; SPDX/secret/cargo-deny checks clean. |

The 278 App tests are the unchanged `main` count; this investigation adds no test because it adds no proven
failure path. The scratch probes described in §2 were deleted before the gate run and are not part of the branch.

## 6. Outcome and next action

- **No fix is shipped on this branch.** No source, test, contract, status, or parity file is changed.
- The freeze is **unresolved and not marked resolved**. The negative result is evidence, not a verdict that the
  defect is gone.
- The branch can be closed unmerged, or kept as the carrier for the diagnostic checklist above.
- If a future run captures a UI-thread stack showing `LayoutManager`/`Measure`/`Arrange` recursion, a
  `Grid`/`ScrollViewer`/text-shaping loop, or the layer/focus state machine, use that stack to build the failing
  test first; do not apply a speculative clamp to the palette width.

## 7. Commit list

| Commit | Content |
| --- | --- |
| (this commit) | `docs: record the negative GUI palette freeze reproduction [skip ci]` |

No code commit, no merge, no tag. The branch base is `19cb6ffe190e02449ce8b827706f365f2ea7afe2`.
