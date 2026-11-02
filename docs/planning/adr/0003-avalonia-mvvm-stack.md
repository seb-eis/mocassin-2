# ADR 0003 — Avalonia MVVM Stack

- **Status:** Accepted
- **Date:** 2026-11-02
- **Deciders:** Project owner

## Context

The original GUI is a 30k-line WPF application with a hand-written MVVM layer, a service-locator
(`IProjectAppControl`) passed to every view model, views stored inside view models, `MessageBox` dialogs,
`Console.WriteLine` logging, and WPF-only command/validation plumbing.

The new GUI must run on Windows, Linux, and macOS, support a large scientific editor workflow, and remain
testable without a display.

## Decision

Use:

- **Avalonia UI 11.x** as the UI framework.
- **CommunityToolkit.Mvvm 8.x** for observable properties, commands, and validation.
- **Microsoft.Extensions.DependencyInjection/Hosting/Configuration/Logging** for composition and services.
- **Serilog** (or the Microsoft logging file sink) for structured file logging.
- **ViewLocator/DataTemplate** resolution instead of storing views in view models.
- **Explicit constructor injection**; no service locator in view models.
- **Avalonia.Controls.DataGrid** for tabular editors, with explicit Add/Remove commands instead of WPF's
  implicit new-row behavior.
- **LiveCharts2** (`LiveChartsCore.SkiaSharpView.Avalonia`) for scientific charts, with SkiaSharp for headless rendering/export.
- **Silk.NET.OpenGL** behind a renderer abstraction for 3D (see ADR 0006).

## Consequences

**Positive**

- One UI codebase for all target platforms.
- Source-generated MVVM reduces boilerplate and is trimming/AOT friendly.
- Boundary services (dialogs, process launcher, clock) are replaceable; view models are testable headlessly
  against real project documents and domain objects.
- Modern async/cancellation and logging are first-class.

**Negative**

- Avalonia's ecosystem is smaller than WPF's; some controls need custom implementations.
- DataGrid and drag/drop behavior differ from WPF; editors are redesigned rather than ported.
- 3D rendering must be built or integrated, not inherited from HelixToolkit.Wpf.SharpDX.

## Rules

- No `System.Windows.*`, `Microsoft.Win32`, or WPF types in shared/view-model projects.
- View models never reference views or controls.
- All dialogs and file pickers go through interfaces (`IDialogService`, `IStorageService`).
- UI state that must persist is stored in the project's `viewSettings` or app settings, not in ad-hoc files.
- Long operations take a `CancellationToken` and report `IProgress<T>`.
- Every user-visible command has a keyboard-accessible path and an automation-friendly name.
