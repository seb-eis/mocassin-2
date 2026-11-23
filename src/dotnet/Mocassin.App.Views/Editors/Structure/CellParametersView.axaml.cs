// SPDX-License-Identifier: MIT

using Avalonia.Controls;

namespace Mocassin.App.Views.Editors.Structure;

/// <summary>
/// The cell-parameter form view: six expression fields, crystal-system locks, live vectors/volume, geometry
/// diagnostics, and the confirmed reset actions.
/// </summary>
internal sealed partial class CellParametersView : UserControl
{
    /// <summary>
    /// Initializes the cell-parameter view.
    /// </summary>
    public CellParametersView()
    {
        InitializeComponent();
    }
}
