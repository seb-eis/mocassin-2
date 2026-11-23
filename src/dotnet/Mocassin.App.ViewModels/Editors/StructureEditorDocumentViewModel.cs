// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Editors;

/// <summary>
/// The structure document is a form, not a master-detail: it hosts the space-group and cell-parameter forms and the
/// summary, sharing the framework's detail/validation/coordinator plumbing but with no table or row selection.
/// </summary>
internal sealed class StructureEditorDocumentViewModel : ModelEditorDocumentViewModel
{
    /// <summary>
    /// Initializes the structure form document.
    /// </summary>
    /// <param name="descriptor">The structure descriptor.</param>
    /// <param name="editor">The structure editor.</param>
    public StructureEditorDocumentViewModel(ModelEditorDescriptor descriptor, ModelEditorViewModel editor)
        : base(descriptor, editor)
    {
    }
}
