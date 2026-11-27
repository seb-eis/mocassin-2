// SPDX-License-Identifier: MIT

using System.ComponentModel;

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// The shell-owned editor selection-pane share. Exactly one writer (the shell's serialized layout path) exists;
/// every open editor observes the same value.
/// </summary>
internal interface IEditorPaneLayout : INotifyPropertyChanged
{
    /// <summary>
    /// Gets or sets the selection pane's share of the editor width.
    /// </summary>
    public double EditorFraction { get; set; }
}
