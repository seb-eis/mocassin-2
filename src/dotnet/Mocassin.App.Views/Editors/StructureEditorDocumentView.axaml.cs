// SPDX-License-Identifier: MIT

using Avalonia.Controls;

namespace Mocassin.App.Views.Editors;

/// <summary>
/// The structure form document view. It hosts the space-group/cell-parameter forms and the structure summary that
/// later 24A features fill in; the framework supplies the summary bar, the read-only banner, and the focus routing.
/// </summary>
internal sealed partial class StructureEditorDocumentView : UserControl
{
    /// <summary>
    /// Initializes the structure form view.
    /// </summary>
    public StructureEditorDocumentView()
    {
        InitializeComponent();
    }
}
