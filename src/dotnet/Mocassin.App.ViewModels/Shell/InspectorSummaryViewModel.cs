// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Simple inspector summary rendered for the active document.
/// </summary>
/// <param name="Title">The summary title.</param>
/// <param name="Details">The summary detail lines.</param>
internal sealed record InspectorSummaryViewModel(string Title, IReadOnlyList<string> Details);
