// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Mocassin.App.ViewModels.Editors.Structure;

namespace Mocassin.App.Views.Editors.Structure;

/// <summary>
/// The cell-parameter form view: six expression fields, crystal-system locks, live vectors/volume, geometry
/// diagnostics, and the confirmed reset actions. <c>Enter</c> in a single-line field applies the form.
/// </summary>
internal sealed partial class CellParametersView : UserControl
{
    /// <summary>
    /// Initializes the cell-parameter view.
    /// </summary>
    public CellParametersView()
    {
        InitializeComponent();
        EditorKeyGestures.ApplyOnEnter(this, () => (DataContext as CellParameterEditorViewModel)?.ApplyCommand);
    }
}
