// SPDX-License-Identifier: MIT

using Avalonia.Controls;

namespace Mocassin.App.Views.Editors.Sites;

/// <summary>
/// The cell-site detail view: fields, reference picker, candidate diagnostics/overlap gate, dependents and the
/// energy-reference action, and the toggleable P1 preview with the deterministic 2D projection.
/// </summary>
internal sealed partial class CellSiteDetailView : UserControl
{
    /// <summary>
    /// Initializes the cell-site detail view.
    /// </summary>
    public CellSiteDetailView()
    {
        InitializeComponent();
    }
}
