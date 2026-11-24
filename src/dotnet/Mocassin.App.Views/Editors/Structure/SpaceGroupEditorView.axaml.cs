// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Mocassin.App.ViewModels.Editors.Structure;

namespace Mocassin.App.Views.Editors.Structure;

/// <summary>
/// The space-group form view: searchable catalog picker, settings, operation details, and the pre-apply
/// compatibility/orbit preview. <c>Enter</c> in a single-line field applies the form.
/// </summary>
internal sealed partial class SpaceGroupEditorView : UserControl
{
    /// <summary>
    /// Initializes the space-group view.
    /// </summary>
    public SpaceGroupEditorView()
    {
        InitializeComponent();
        EditorKeyGestures.ApplyOnEnter(this, () => (DataContext as SpaceGroupEditorViewModel)?.ApplyCommand);
    }
}
