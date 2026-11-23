// SPDX-License-Identifier: MIT

using Avalonia.Controls;

namespace Mocassin.App.Views.Editors.Structure;

/// <summary>
/// The structure summary view: counts, volume, reference-site density, the orbit table, and the large-P1 warning.
/// All values come from the structure model and its preview; no statistics service is called.
/// </summary>
internal sealed partial class StructureSummaryView : UserControl
{
    /// <summary>
    /// Initializes the structure summary view.
    /// </summary>
    public StructureSummaryView()
    {
        InitializeComponent();
    }
}
