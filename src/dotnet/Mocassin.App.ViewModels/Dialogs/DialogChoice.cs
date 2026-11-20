// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Dialogs;

/// <summary>
/// Describes one selectable choice in a choice dialog.
/// </summary>
/// <param name="Id">The stable choice id.</param>
/// <param name="Label">The visible label.</param>
/// <param name="IsDefault">Whether the choice is the default action.</param>
/// <param name="IsCancel">Whether the choice represents cancel.</param>
internal sealed record DialogChoice(string Id, string Label, bool IsDefault = false, bool IsCancel = false);
