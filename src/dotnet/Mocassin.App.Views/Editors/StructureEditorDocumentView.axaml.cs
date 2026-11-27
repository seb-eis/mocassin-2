// SPDX-License-Identifier: MIT

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Mocassin.App.Views.Editors;

/// <summary>
/// The structure form document view. It hosts the space-group/cell-parameter forms and the structure summary; the
/// framework supplies the summary bar, the read-only banner, and the focus routing.
/// </summary>
/// <remarks>
/// Initial focus: the space-group search field, the first editable control of the first form
/// (<c>ui-interaction-contract.md</c> §8 rule 1).
/// </remarks>
internal sealed partial class StructureEditorDocumentView : UserControl
{
    /// <summary>
    /// Initializes the structure form view.
    /// </summary>
    public StructureEditorDocumentView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => Dispatcher.UIThread.Post(FocusInitial);
    }

    private void FocusInitial()
    {
        var search = this.GetVisualDescendants()
            .OfType<TextBox>()
            .FirstOrDefault(box => string.Equals(
                AutomationProperties.GetName(box),
                "Space-group search",
                StringComparison.Ordinal));
        search?.Focus();
    }
}
